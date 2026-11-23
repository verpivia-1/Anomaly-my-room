// 메뉴: Anomaly > Debug Window (플레이 중에만 동작)
// 지금 맵 상태 확인, 맵 이동, 이상현상 지정·클로즈업, 정답/오답 처리.
// 에디터 전용이라 씬에서 GameManager를 찾아 씀(CLAUDE.md 2: _Project_Editor는 전역 검색 금지 대상 아님)
using System.Collections.Generic;
using Anomaly.Model.Anomalies;
using Anomaly.Model.Loop;
using Anomaly.Presenter.Manager;
using UnityEditor;
using UnityEngine;

namespace Anomaly.Editor.Debugging
{
    public sealed class AnomalyDebugWindow : EditorWindow
    {
        static readonly string[] RoomFilterLabels = { "전체", "복도", "거실", "식당", "침실" };

        GameManager manager;
        int targetMap;
        int roomFilter;
        int cursor = -1; // 이전/다음으로 훑을 때 위치(필터된 목록 기준)
        Vector2 scroll;
        string lastMessage = "";

        [MenuItem("Anomaly/Debug Window")]
        static void Open() => GetWindow<AnomalyDebugWindow>("Anomaly Debug");

        void OnInspectorUpdate() => Repaint(); // 플레이 중 상태 갱신

        void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 중에만 동작합니다. Level_Corridor(또는 로비에서 시작) 씬을 플레이하세요.", MessageType.Info);
                manager = null;
                return;
            }
            if (manager == null) manager = Object.FindFirstObjectByType<GameManager>();
            if (manager == null || manager.LoopModel == null)
            {
                EditorGUILayout.HelpBox("GameManager가 없는 씬입니다(로비라면 게임 시작 후 사용).", MessageType.Info);
                return;
            }

            var loop = manager.LoopModel;
            DrawStatus(loop);
            EditorGUILayout.Space();
            DrawMapControls(loop);
            EditorGUILayout.Space();
            DrawAnomalyList(loop);
        }

        // ---------- 상태 ----------
        void DrawStatus(LoopModel loop)
        {
            EditorGUILayout.LabelField("현재 상태", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("맵", $"{loop.MapNumber}번  (상태: {loop.Phase})");
            var a = loop.CurrentAnomaly;
            EditorGUILayout.LabelField("이상현상", loop.HasAnomaly && a != null ? $"{a.Id}  {a.Description}" : "없음");
            EditorGUILayout.LabelField("정답", loop.HasAnomaly ? "나가기 (복도 서쪽 끝)" : "잠들기 (침대)");
            EditorGUILayout.LabelField("풀에 남은 수", manager.DebugPoolRemaining.ToString());
            if (loop.Phase != LoopPhase.Playing && loop.Phase != LoopPhase.Inspecting)
                EditorGUILayout.HelpBox("팝업·전환·일시정지·엔딩 중에는 조작이 무시됩니다.", MessageType.None);
            if (!string.IsNullOrEmpty(lastMessage)) EditorGUILayout.HelpBox(lastMessage, MessageType.None);
        }

        // ---------- 맵 이동 / 판정 ----------
        void DrawMapControls(LoopModel loop)
        {
            EditorGUILayout.LabelField("맵", EditorStyles.boldLabel);
            int last = manager.DebugGoalMapNumber - 1;
            using (new EditorGUILayout.HorizontalScope())
            {
                targetMap = EditorGUILayout.IntSlider("이동할 맵", targetMap, 0, last);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("이상현상 없이 이동")) { manager.DebugSetMap(targetMap, null); lastMessage = $"{targetMap}번 맵(이상현상 없음)"; }
                if (GUILayout.Button("규칙대로 다시 뽑기")) { manager.DebugRerollMap(targetMap); lastMessage = $"{targetMap}번 맵(확률·풀 규칙대로)"; }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button($"마지막 맵({last}번)으로")) { targetMap = last; manager.DebugSetMap(last, null); lastMessage = $"{last}번 맵. '정답 처리'를 누르면 엔딩"; }
                if (GUILayout.Button("정답 처리")) { manager.DebugAnswer(true); lastMessage = "정답 처리(전환·피드백 진행)"; }
                if (GUILayout.Button("오답 처리")) { manager.DebugAnswer(false); lastMessage = "오답 처리(0번 맵으로)"; }
            }
        }

        // ---------- 이상현상 목록 ----------
        void DrawAnomalyList(LoopModel loop)
        {
            EditorGUILayout.LabelField("이상현상 (지금 맵에 적용)", EditorStyles.boldLabel);
            int newFilter = GUILayout.Toolbar(roomFilter, RoomFilterLabels);
            if (newFilter != roomFilter) { roomFilter = newFilter; cursor = -1; }

            var list = Filtered();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = list.Count > 0;
                if (GUILayout.Button("◀ 이전 적용+보기")) Step(list, -1, loop);
                if (GUILayout.Button("다음 적용+보기 ▶")) Step(list, +1, loop);
                GUI.enabled = true;
            }
            EditorGUILayout.LabelField($"{list.Count}개" + (cursor >= 0 && cursor < list.Count ? $"  ·  {cursor + 1}번째: {list[cursor].Id}" : ""));

            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                bool current = loop.CurrentAnomaly == a;
                using (new EditorGUILayout.HorizontalScope(current ? EditorStyles.helpBox : GUIStyle.none))
                {
                    EditorGUILayout.LabelField(a.Id, GUILayout.Width(44));
                    EditorGUILayout.LabelField(a.Description, GUILayout.MinWidth(120));
                    if (GUILayout.Button("적용", GUILayout.Width(44))) { cursor = i; Apply(a, loop, false); }
                    if (GUILayout.Button("적용+보기", GUILayout.Width(72))) { cursor = i; Apply(a, loop, true); }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        List<AnomalyData> Filtered()
        {
            var result = new List<AnomalyData>();
            var catalog = manager.DebugCatalog;
            if (catalog == null) return result;
            foreach (var a in catalog)
            {
                if (a == null) continue;
                if (roomFilter == 0 || (int)a.Room == roomFilter - 1) result.Add(a);
            }
            return result;
        }

        void Step(List<AnomalyData> list, int dir, LoopModel loop)
        {
            cursor = cursor < 0 ? (dir > 0 ? 0 : list.Count - 1) : (cursor + dir + list.Count) % list.Count;
            Apply(list[cursor], loop, true);
        }

        // 지금 맵 번호는 그대로 두고 이상현상만 바꿈(0번 맵이면 1번으로 옮겨서 적용)
        void Apply(AnomalyData anomaly, LoopModel loop, bool view)
        {
            int map = Mathf.Max(loop.MapNumber, 1);
            manager.DebugSetMap(map, anomaly);
            bool viewed = view && manager.DebugView(anomaly);
            lastMessage = $"{map}번 맵에 {anomaly.Id} 적용" + (view ? (viewed ? " · 클로즈업(E/Esc로 닫기)" : " · 복도 이상현상은 클로즈업 대상 없음") : "");
        }
    }
}
