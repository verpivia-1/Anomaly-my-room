// 로비 캐릭터용 Animator Controller. HAON StandA(기본 서 있기)의 첫 프레임에서 멈춘 자세로 가만히 서 있게 한다.
// 게임 플레이어용 PlayerAnimator(대기·걷기)와는 별개. HAON 폴더는 수정하지 않음.
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Anomaly.Editor.Animations
{
    public static class LobbyAnimatorBuilder
    {
        public const string ControllerPath = "Assets/_Project/Animations/Lobby/LobbyAnimator.controller";
        const string StandClipPath = "Assets/Haons SD series Pack/Animation/Common/StandA.FBX";

        [MenuItem("Anomaly/Build Lobby Animator")]
        public static void BuildFromMenu()
        {
            var controller = Build();
            if (controller == null) return;
            Selection.activeObject = controller;
            Debug.Log($"[LobbyAnimatorBuilder] 완료: {ControllerPath}");
        }

        public static AnimatorController GetOrCreate()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            return controller != null ? controller : Build();
        }

        public static AnimatorController Build()
        {
            AnimationClip stand = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(StandClipPath))
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__")) { stand = clip; break; }
            if (stand == null)
            {
                Debug.LogError($"[LobbyAnimatorBuilder] 클립을 찾지 못함. HAON 팩을 임포트했는지 확인: {StandClipPath}");
                return null;
            }

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Animations")) AssetDatabase.CreateFolder("Assets/_Project", "Animations");
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Animations/Lobby")) AssetDatabase.CreateFolder("Assets/_Project/Animations", "Lobby");

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }
            else
            {
                controller.parameters = new AnimatorControllerParameter[0];
                var old = controller.layers[0].stateMachine;
                foreach (var s in old.states) old.RemoveState(s.state);
            }

            var sm = controller.layers[0].stateMachine;
            var standState = sm.AddState("Stand");
            standState.motion = stand;
            standState.speed = 0f; // 재생하지 않음 = 첫 프레임 자세로 정지
            sm.defaultState = standState;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }
    }
}
