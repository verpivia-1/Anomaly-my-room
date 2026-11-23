// 메뉴: Anomaly > Build Level Layout
// 복도 1개 + 방 3개(101 거실, 102 식당, 103 침실)를 만들고 KayKit 가구를 배치해 Assets/Scenes/Level_Corridor.unity 로 저장한다.
// 좌표는 KayKit 단위(의자 높이 1.26)로 적혀 있고, 마지막에 HAON SD 캐릭터 키에 맞춰 Level 루트 전체를 스케일한다.
// 다시 실행하면 씬을 새로 만든다(기존 Level_Corridor 씬은 덮어씀).
// 플레이어(HAON 캐릭터 + CharacterController), 탑뷰 카메라, GameManager까지 연결해 바로 플레이할 수 있게 만든다.
// 판정용 오브젝트(출구 지역, 침대 지역, 시작 지점, 맵 번호 푯말, 확인 팝업, 엔딩)와 규칙·이상현상 데이터 에셋도 여기서 만들고 연결한다.
using System.Collections.Generic;
using Anomaly.Editor.Animations;
using Anomaly.Editor.Art;
using Anomaly.Model.Anomalies;
using Anomaly.Model.Loop;
using Anomaly.Model.Player;
using Anomaly.Presenter.Manager;
using Anomaly.View.Anomalies;
using Anomaly.View.Cameras;
using Anomaly.View.Interaction;
using Anomaly.View.Player;
using Anomaly.View.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Anomaly.Editor.Level
{
    public static class LevelLayoutBuilder
    {
        const string KayKitDir = "Assets/KayKit_Furniture_Bits_1.0_FREE/KayKit_Furniture_Bits_1.0_FREE/Assets/fbx (unity)/";
        const string AtlasPath = KayKitDir + "furniturebits_texture.png";
        const string CharacterPath = "Assets/Haons SD series Pack/Prefab/CharacterSet/prf_Set Costume01 Unity-Chan.prefab";
        const string MatDir = "Assets/_Project/Materials";
        const string ScenePath = "Assets/Scenes/Level_Corridor.unity";
        const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        const string MoveActionName = "Player/Move";
        const string InteractActionName = "Player/Interact";
        const string DataDir = "Assets/_Project/Data";
        const string RuleSettingsPath = DataDir + "/Rules/GameRuleSettings.asset";
        const string AnomalyDir = DataDir + "/Anomalies";
        const string AnomalyCatalogPath = AnomalyDir + "/AnomalyCatalog.asset";
        // 페이드 이미지. 이 경로에 넣고(Texture Type: Sprite) Build Level Layout을 다시 실행하면 연결됨
        const string FadeImageDir = "Assets/_Project/Art/UI/Fade";
        const string FadeSleepImagePath = FadeImageDir + "/Fade_Sleep.png";
        const string FadeLeaveImagePath = FadeImageDir + "/Fade_Leave.png";
        // 정답/오답 효과음. 이 이름(.wav/.ogg/.mp3)으로 넣고 다시 실행하면 연결됨
        const string SfxDir = "Assets/_Project/Audio/SFX";
        const string SfxCorrectName = "SFX_Correct";
        const string SfxWrongName = "SFX_Wrong";
        const string FootstepDir = SfxDir + "/Footsteps"; // 이 폴더의 오디오를 전부 발소리로 씀
        const string PlayerSettingsDir = "Assets/_Project/Data/Player";
        const string PlayerSettingsPath = PlayerSettingsDir + "/PlayerMovementSettings.asset";

        // 캐릭터 키를 KayKit 단위로 몇으로 볼지. 가구가 너무 크거나 작으면 이 값만 바꿔서 다시 실행.
        const float CharacterHeightInKayKitUnits = 1.6f;

        const float WallH = 4f, WallT = 0.2f;
        // 탑뷰 규칙: 카메라는 남쪽 위에서 북쪽을 본다. 카메라 쪽(남쪽)을 막는 벽은 세우지 않는다.
        // 복도와 방 사이 벽은 전체 높이로 세우고, 플레이어를 가리면 FadeableView로 투명 처리한다.

        // 시작 지점(KayKit 단위). 출구 지역(x -12..-10.8) 밖에 둬야 나가지 않기 뒤 다시 들어올 때 팝업이 뜬다
        static readonly Vector3 StartPos = new Vector3(-9.5f, 0.05f, 0f);

        // Build 한 번 동안 만든 오브젝트를 모아 GameManager에 연결
        static InteractZoneView exitZone, bedZone;
        static Transform startPoint;
        static MapNumberSignView mapSign;
        static readonly List<AnomalyTargetView> anomalyTargets = new List<AnomalyTargetView>();
        static readonly List<InspectableView> inspectables = new List<InspectableView>();
        static Font uiFont;

        static Material wallFadeMat, atlasMat, wallMat, roomFloorMat, corridorFloorMat, corridorFloorMatB, groutMat, forwardMat, backMat;
        static float kayKitFix = 1f; // FBX 임포트 단위 보정

        [MenuItem("Anomaly/Build Level Layout")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateMaterials();
            MeasureKayKitScale();

            exitZone = bedZone = null;
            startPoint = null;
            mapSign = null;
            anomalyTargets.Clear();
            colorCounts.Clear();
            swapCounts.Clear();
            colorIndex = 0;
            inspectables.Clear();
            ClearCatalog(); // 이번 빌드에서 실제로 배치된 이상현상만 다시 채움(빠진 가구의 이상현상이 풀에 남지 않게)
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // OS 글꼴로 한글 표시

            var root = new GameObject("Level").transform;
            BuildShell(root);
            // 복도에는 소품을 두지 않는다
            BuildLiving(Group("Room_101_Living", root));
            BuildDining(Group("Room_102_Dining", root));
            BuildBedroom(Group("Room_103_Bedroom", root));
            BuildLights(Group("Lights", root));
            BuildStartPointAndSign(root);
            BuildCorridorAnomalies(root);

            float s = BuildPlayer(out var playerView, out var playerInputView);
            root.localScale = Vector3.one * s;
            foreach (var l in root.GetComponentsInChildren<Light>())
                if (l.type == LightType.Point) l.range *= s;
            // 지역 판정 높이를 월드 단위로 맞춤
            SetFloat(exitZone, "checkHeight", 0.5f * s);
            foreach (var item in inspectables) SetFloat(item, "reach", InspectReach * s);
            SetFloat(bedZone, "checkHeight", 0.5f * s);

            var cameraView = BuildCamera(s, out var wallFadeView);
            BuildUI(out var popup, out var ending, out var screenFade, out var prompt, out var feedback, out var inspectPanel, out var pauseMenu);
            BuildGameManager(playerView, playerInputView, cameraView, wallFadeView, popup, ending, screenFade, prompt, feedback, inspectPanel, pauseMenu);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.33f, 0.38f);

            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[LevelLayoutBuilder] 완료: {ScenePath} / Level 스케일 {s:0.###} / KayKit 임포트 보정 {kayKitFix:0.###}");
        }

        // ---------- 껍데기: 바닥, 벽, 출구 표시 ----------
        // 복도: x -12..12, z -2..2 / 방: z 2..10, x [-12,-4] [-4,4] [4,12] / 문: 각 방 가운데 폭 2
        static void BuildShell(Transform root)
        {
            var floors = Group("Floors", root);
            // 복도 바닥은 2x2 타일 12x2장(체크무늬). 타일 이상현상(색·무늬·개수 변화)을 타일 단위로 바꿀 수 있게 나눠 둔다
            var tiles = Group("Floor_Corridor_Tiles", floors);
            for (int ix = 0; ix < 12; ix++)
                for (int iz = 0; iz < 2; iz++)
                {
                    var m = (ix + iz) % 2 == 0 ? corridorFloorMat : corridorFloorMatB;
                    Box($"Tile_{ix:00}_{iz}", tiles, new Vector3(-11 + ix * 2, -0.05f, -1 + iz * 2), new Vector3(1.96f, 0.1f, 1.96f), m);
                }
            // 타일 사이 줄눈이 보이도록 아래에 어두운 판을 깐다
            Box("Floor_Corridor_Grout", floors, new Vector3(0, -0.12f, 0), new Vector3(24, 0.1f, 4), groutMat);
            Box("Floor_101", floors, new Vector3(-8, -0.05f, 6), new Vector3(8, 0.1f, 8), roomFloorMat);
            Box("Floor_102", floors, new Vector3(0, -0.05f, 6), new Vector3(8, 0.1f, 8), roomFloorMat);
            Box("Floor_103", floors, new Vector3(8, -0.05f, 6), new Vector3(8, 0.1f, 8), roomFloorMat);

            var walls = Group("Walls", root);
            // 복도 남쪽 벽 없음: 카메라 시야를 막기 때문
            WallX("Wall_North", walls, -12, 12, 10 + WallT / 2);
            // 복도와 방 사이 벽(문 3개: x=-8, 0, 8). 전체 높이로 세운다.
            // 플레이어가 붙으면 투명 처리하는 기능은 나중에 런타임 스크립트로 추가(그때까지는 방 앞쪽이 가려짐)
            WallX("Wall_Door_A", walls, -12, -9, 2);
            WallX("Wall_Door_B", walls, -7, -1, 2);
            WallX("Wall_Door_C", walls, 1, 7, 2);
            WallX("Wall_Door_D", walls, 9, 12, 2);
            // 방 옆벽
            WallZ("Wall_West", walls, -12 - WallT / 2, 2, 10);
            WallZ("Wall_101_102", walls, -4, 2, 10);
            WallZ("Wall_102_103", walls, 4, 2, 10);
            WallZ("Wall_East", walls, 12 + WallT / 2, 2, 10);

            // 앞으로 가기는 침대 상호작용으로 바뀌어서 복도 동쪽 끝은 막는다
            WallZ("Wall_CorridorEast", walls, 12 + WallT / 2, -2, 2);

            // 출구 지역(복도 서쪽 끝, 주황색 표시). 들어오면 나가기 팝업이 한 번 뜬다
            var exits = Group("Exits", root);
            var back = Box("Exit_Back", exits, new Vector3(-11.4f, 0.01f, 0), new Vector3(1.2f, 0.02f, 4), backMat);
            Object.DestroyImmediate(back.GetComponent<Collider>());
            exitZone = Zone("ExitZone", exits, new Vector3(-11.4f, 0, 0), new Vector3(1.2f, 2, 4));

            // 서쪽 끝 밖으로 걸어나가 떨어지지 않게 보이지 않는 벽
            var blocker = Box("Exit_Blocker", exits, new Vector3(-12 - WallT / 2, WallH / 2, 0), new Vector3(WallT, WallH, 4), wallMat);
            Object.DestroyImmediate(blocker.GetComponent<MeshRenderer>());
        }

        // 트리거 박스 + InteractZoneView. 대상(플레이어)은 GameManager가 런타임에 넣는다
        static InteractZoneView Zone(string name, Transform parent, Vector3 floorCenter, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = floorCenter;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0, size.y / 2, 0);
            col.size = size;
            var zone = go.AddComponent<InteractZoneView>();
            SetRef(zone, "zone", col);
            return zone;
        }

        // ---------- 시작 지점 + 맵 번호 푯말 ----------
        static void BuildStartPointAndSign(Transform root)
        {
            startPoint = new GameObject("StartPoint").transform;
            startPoint.SetParent(root, false);
            startPoint.localPosition = StartPos;
            startPoint.localRotation = Quaternion.Euler(0, 90, 0); // 동쪽(방 쪽)을 봄

            // 시작 지점 바로 뒤 복도 북쪽 벽(Wall_Door_A) 남쪽 면에 붙인다
            var canvasGo = new GameObject("Sign_MapNumber", typeof(RectTransform), typeof(Canvas));
            canvasGo.transform.SetParent(root, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)canvasGo.transform;
            rt.sizeDelta = new Vector2(160, 110);
            rt.localScale = Vector3.one * 0.01f;
            rt.localPosition = new Vector3(-10.5f, 2.6f, 2 - WallT / 2 - 0.01f);

            var board = new GameObject("Board", typeof(RectTransform), typeof(Image));
            board.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)board.transform);
            board.GetComponent<Image>().color = new Color(0.18f, 0.2f, 0.24f);

            var label = CreateText(canvasGo.transform, "Number", "0", 80, Vector2.zero, rt.sizeDelta);
            label.color = new Color(0.95f, 0.93f, 0.85f);

            mapSign = canvasGo.AddComponent<MapNumberSignView>();
            SetRef(mapSign, "label", label);
        }

        // ---------- 복도 이상현상(쉬움 2개) ----------
        static void BuildCorridorAnomalies(Transform root)
        {
            var tiles = root.Find("Floors/Floor_Corridor_Tiles");
            if (tiles != null)
            {
                var data = GetOrCreateAnomaly("C01_CorridorTileColor", "C01", AnomalyDifficulty.Easy, AnomalyRoom.Corridor, AnomalyKind.ColorChange, "복도 바닥 타일 색 변경");
                var view = tiles.gameObject.AddComponent<ColorAnomalyView>();
                SetString(view, "anomalyId", data.Id);
                SetRefArray(view, "renderers", tiles.GetComponentsInChildren<Renderer>());
                SetColor(view, "color", new Color(0.72f, 0.38f, 0.34f));
                anomalyTargets.Add(view);
            }

            var lightA = root.Find("Lights/Light_Corridor_A");
            var lightB = root.Find("Lights/Light_Corridor_B");
            if (lightA != null && lightB != null)
            {
                var data = GetOrCreateAnomaly("C02_CorridorLightColor", "C02", AnomalyDifficulty.Easy, AnomalyRoom.Corridor, AnomalyKind.ColorChange, "복도 조명 색 변경");
                var view = lightA.parent.gameObject.AddComponent<LightColorAnomalyView>();
                SetString(view, "anomalyId", data.Id);
                SetRefArray(view, "lights", new Object[] { lightA.GetComponent<Light>(), lightB.GetComponent<Light>() });
                SetColor(view, "color", new Color(1f, 0.3f, 0.25f));
                anomalyTargets.Add(view);
            }
        }

        // ---------- 101 거실 (x -12..-4) ----------
        // Anom(가구, 방, 교체 모델): 가구마다 색상 변경 이상현상 1개 + 교체 모델이 있으면 모델 교체 1개 (기획서 3장)
        const AnomalyRoom Living = AnomalyRoom.Living101, Dining = AnomalyRoom.Dining102, Bedroom = AnomalyRoom.Bedroom103;

        static void BuildLiving(Transform p)
        {
            Anom(Prop("rug_rectangle_A", p, -8, 0, 6.3f, 0), Living, "rug_rectangle_stripes_A");
            // KayKit 모델은 회전 0일 때 +z(북쪽)를 봄. 소파는 방 안(테이블)을 보게 돌림
            Anom(Prop("couch_pillows", p, -8, 0, 9.1f, 180), Living, "couch");
            Anom(Prop("table_low", p, -8, 0, 6.6f, 0), Living);
            Anom(Prop("armchair", p, -11, 0, 6.0f, 90), Living, "armchair_pillows");
            Anom(Prop("cabinet_medium_decorated", p, -4.6f, 0, 6.3f, -90), Living, "cabinet_medium");
            Anom(Prop("lamp_standing", p, -11.2f, 0, 9.2f, 0), Living);
            SetInspectPoint(Anom(Prop("pictureframe_large_B", p, -8, 2.6f, 10, 180), Living), p, -8f, 7.9f); // 소파 앞에서 조사
            Anom(Prop("shelf_B_large_decorated", p, -12, 2.2f, 4.0f, 90), Living, "shelf_B_large");
            Anom(Prop("cactus_medium_A", p, -4.8f, 0, 9.3f, 0), Living, "cactus_medium_B");
        }

        // ---------- 102 식당 (x -4..4) ----------
        static void BuildDining(Transform p)
        {
            Anom(Prop("rug_oval_A", p, 0, 0, 6.4f, 0), Dining, "rug_oval_B");
            Anom(Prop("table_medium_long", p, 0, 0, 6.4f, 0), Dining);
            // 의자 4개는 각각 별도 이상현상. 교체 모델은 나무 의자 / 다른 모양 의자를 번갈아 씀
            Anom(Prop("chair_A", p, -0.75f, 0, 4.9f, 0), Dining, "chair_A_wood");
            Anom(Prop("chair_A", p, 0.75f, 0, 4.9f, 0), Dining, "chair_B");
            Anom(Prop("chair_A", p, -0.75f, 0, 7.9f, 180), Dining, "chair_B");
            // 네 번째 의자는 식탁 서쪽 끝(머리 자리)에서 동쪽을 봄.
            // 북쪽에 두면 식탁과 북쪽 벽 사이가 막혀 벽 액자를 조사할 수 없음. ID(DC06, DS05)는 그대로
            Anom(Prop("chair_A", p, -1.7f, 0, 6.4f, 90), Dining, "chair_A_wood");
            Anom(Prop("cabinet_medium_decorated", p, -2.2f, 0, 9.45f, 180), Dining, "cabinet_medium");
            Anom(Prop("cabinet_medium", p, 2.2f, 0, 9.45f, 180), Dining, "cabinet_medium_decorated");
            Anom(Prop("book_set", p, 1.7f, 1.25f, 9.45f, 180), Dining);
            Anom(Prop("lamp_table", p, 2.8f, 1.0f, 9.45f, 0), Dining);
            Anom(Prop("pictureframe_medium", p, 0, 2.6f, 10, 180), Dining);
            Anom(Prop("shelf_B_small_decorated", p, -3.9f, 2.0f, 4.0f, 90), Dining, "shelf_B_small");
            Anom(Prop("cactus_small_A", p, 3.4f, 0, 2.7f, 0), Dining, "cactus_small_B");
        }

        // ---------- 103 침실 (x 4..12) ----------
        static void BuildBedroom(Transform p)
        {
            Anom(Prop("rug_oval_B", p, 8, 0, 5.2f, 0), Bedroom, "rug_oval_A");
            var bed = Prop("bed_double_A", p, 9, 0, 8.4f, 180); // 도착점이라 이상현상에서 제외
            // 잠들기 구역: 침대 둘레 전체(발치·양옆). 침대 바닥 범위에 조사 거리만큼 여유를 더함
            var bedRenderers = bed != null ? bed.GetComponentsInChildren<Renderer>() : new Renderer[0];
            var bedBounds = new Bounds(new Vector3(9, 0, 8.4f), new Vector3(2, 1, 2.5f));
            if (bedRenderers.Length > 0)
            {
                bedBounds = bedRenderers[0].bounds;
                foreach (var r in bedRenderers) bedBounds.Encapsulate(r.bounds);
            }
            var goal = new GameObject("Goal_BedInteract");
            goal.transform.SetParent(p, false);
            goal.transform.localPosition = new Vector3(bedBounds.center.x, 0, bedBounds.center.z); // 레벨 루트 스케일 전이라 로컬 = 월드
            var col = goal.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0, 1, 0);
            col.size = new Vector3(bedBounds.size.x + InspectReach * 2, 2, bedBounds.size.z + InspectReach * 2);
            bedZone = goal.AddComponent<InteractZoneView>();
            SetRef(bedZone, "zone", col);
            SetRefArray(bedZone, "targetRenderers", bedRenderers);
            Anom(Prop("table_small", p, 11.3f, 0, 9.35f, 0), Bedroom);
            Anom(Prop("lamp_table", p, 11.3f, 1.0f, 9.35f, 0), Bedroom);
            Anom(Prop("table_small", p, 6.6f, 0, 9.35f, 0), Bedroom);
            Anom(Prop("pictureframe_standing_A", p, 6.6f, 1.0f, 9.35f, 180), Bedroom, "pictureframe_standing_B");
            Anom(Prop("cabinet_small_decorated", p, 11.35f, 0, 4.0f, -90), Bedroom, "cabinet_small");
            Anom(Prop("chair_C", p, 5.2f, 0, 4.2f, 90), Bedroom, "chair_B");
            Anom(Prop("shelf_B_small", p, 4.1f, 2.0f, 5.0f, 90), Bedroom, "shelf_B_small_decorated");
            SetInspectPoint(Anom(Prop("pictureframe_small_A", p, 9, 2.6f, 10, 180), Bedroom, "pictureframe_small_C"), p, 7.4f, 7.8f); // 침대 왼쪽에서 조사
            Anom(Prop("cactus_small_B", p, 4.7f, 0, 2.7f, 0), Bedroom, "cactus_small_A");
        }

        // ---------- 방 가구 이상현상(보통) ----------
        // ID 규칙: 방 글자(L 거실, D 식당, B 침실) + 종류(C 색상, S 모델 교체) + 방 안 순번. 예: LC01, DS03
        // 색은 아래 표를 순서대로 돌려 쓴다(KayKit 아틀라스 텍스처에 곱해짐)
        static readonly Color[] AnomalyColors =
        {
            new Color(1f, 0.45f, 0.45f), new Color(0.45f, 0.6f, 1f), new Color(0.5f, 1f, 0.5f),
            new Color(0.85f, 0.5f, 1f), new Color(1f, 0.8f, 0.35f), new Color(0.4f, 1f, 1f),
        };
        static readonly Dictionary<AnomalyRoom, int> colorCounts = new Dictionary<AnomalyRoom, int>();
        static readonly Dictionary<AnomalyRoom, int> swapCounts = new Dictionary<AnomalyRoom, int>();
        static int colorIndex;

        // 조사 화면에 보이는 이름(모델 교체돼도 같은 이름 → 이름으로 이상현상이 드러나지 않음)
        static readonly Dictionary<string, string> DisplayNames = new Dictionary<string, string>
        {
            { "rug_rectangle_A", "러그" }, { "rug_oval_A", "러그" }, { "rug_oval_B", "러그" },
            { "couch_pillows", "소파" }, { "armchair", "1인용 소파" }, { "table_low", "낮은 테이블" },
            { "cabinet_medium_decorated", "장식장" }, { "cabinet_medium", "수납장" }, { "cabinet_small_decorated", "작은 수납장" },
            { "lamp_standing", "스탠드" }, { "lamp_table", "램프" },
            { "pictureframe_large_B", "액자" }, { "pictureframe_medium", "액자" }, { "pictureframe_small_A", "액자" },
            { "pictureframe_standing_A", "사진 액자" },
            { "shelf_B_large_decorated", "벽 선반" }, { "shelf_B_small_decorated", "벽 선반" }, { "shelf_B_small", "벽 선반" },
            { "cactus_medium_A", "선인장 화분" }, { "cactus_small_A", "선인장 화분" }, { "cactus_small_B", "선인장 화분" },
            { "table_medium_long", "식탁" }, { "table_small", "협탁" }, { "chair_A", "의자" }, { "chair_C", "의자" },
            { "book_set", "책" },
        };
        const float InspectReach = 1.2f; // KayKit 단위. 레벨 스케일에 맞춰 월드 단위로 바꿔 넣음

        static InspectableView Anom(GameObject prop, AnomalyRoom room, string swapTo = null)
        {
            if (prop == null) return null;
            string model = prop.name;
            var inspectable = prop.AddComponent<InspectableView>();
            SetString(inspectable, "displayName", DisplayNames.TryGetValue(model, out var dn) ? dn : model);
            SetRefArray(inspectable, "renderers", prop.GetComponentsInChildren<Renderer>(true));
            inspectables.Add(inspectable);
            string code = room == AnomalyRoom.Living101 ? "L" : room == AnomalyRoom.Dining102 ? "D" : "B";

            int c = NextCount(colorCounts, room);
            string colorId = $"{code}C{c:00}";
            GetOrCreateAnomaly($"{colorId}_{model}_Color", colorId, AnomalyDifficulty.Normal, room, AnomalyKind.ColorChange, $"{model} 색상 변경");
            var colorView = prop.AddComponent<ColorAnomalyView>();
            SetString(colorView, "anomalyId", colorId);
            SetRefArray(colorView, "renderers", prop.GetComponentsInChildren<Renderer>());
            SetColor(colorView, "color", AnomalyColors[colorIndex++ % AnomalyColors.Length]);
            anomalyTargets.Add(colorView);

            if (string.IsNullOrEmpty(swapTo)) return inspectable;

            // 교체 모델을 같은 자리에 꺼진 채로 만들어 둔다
            var t = prop.transform;
            var replacement = Prop(swapTo, t.parent, t.localPosition.x, t.localPosition.y, t.localPosition.z, t.localEulerAngles.y);
            if (replacement == null) return inspectable;
            // 조사 범위 계산에 교체 모델도 포함(켜져 있는 쪽만 씀)
            var both = new List<Object>(prop.GetComponentsInChildren<Renderer>(true));
            both.AddRange(replacement.GetComponentsInChildren<Renderer>(true));
            SetRefArray(inspectable, "renderers", both.ToArray());
            replacement.name = $"{swapTo}__Swap_{model}";
            replacement.SetActive(false);

            int sIdx = NextCount(swapCounts, room);
            string swapId = $"{code}S{sIdx:00}";
            GetOrCreateAnomaly($"{swapId}_{model}_To_{swapTo}", swapId, AnomalyDifficulty.Normal, room, AnomalyKind.ModelSwap, $"{model} → {swapTo} 모델 교체");
            var swapView = prop.AddComponent<ModelSwapAnomalyView>();
            SetString(swapView, "anomalyId", swapId);
            SetRef(swapView, "original", prop);
            SetRef(swapView, "replacement", replacement);
            anomalyTargets.Add(swapView);
            return inspectable;
        }

        // 가구에 막혀 가까이 갈 수 없는 물건(소파·침대 뒤 벽 액자)은 조사 지점을 바닥 쪽에 따로 둠(KayKit 단위, 방 기준 좌표)
        static void SetInspectPoint(InspectableView inspectable, Transform room, float x, float z)
        {
            if (inspectable == null) return;
            var point = new GameObject($"InspectPoint_{inspectable.name}").transform;
            point.SetParent(room, false);
            point.localPosition = new Vector3(x, 0f, z);
            SetRef(inspectable, "inspectPoint", point);
        }

        static void SkipAnomalyIds(AnomalyRoom room, bool withSwap)
        {
            NextCount(colorCounts, room);
            colorIndex++; // 뒤 가구들의 색도 그대로 유지
            if (withSwap) NextCount(swapCounts, room);
        }

        static void ClearCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AnomalyCatalog>(AnomalyCatalogPath);
            if (catalog == null) return;
            var so = new SerializedObject(catalog);
            so.FindProperty("anomalies").arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static int NextCount(Dictionary<AnomalyRoom, int> counts, AnomalyRoom room)
        {
            counts.TryGetValue(room, out int n);
            counts[room] = ++n;
            return n;
        }

        // ---------- 조명 ----------
        static void BuildLights(Transform p)
        {
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.transform.SetParent(p, false);
            sun.type = LightType.Directional;
            sun.intensity = 0.45f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(55, -30, 0);

            PointLight("Light_Corridor_A", p, new Vector3(-6, 3.5f, 0), 7, new Color(0.92f, 0.95f, 1f));
            PointLight("Light_Corridor_B", p, new Vector3(6, 3.5f, 0), 7, new Color(0.92f, 0.95f, 1f));
            PointLight("Light_101", p, new Vector3(-8, 3.5f, 6), 9, new Color(1f, 0.88f, 0.7f));
            PointLight("Light_102", p, new Vector3(0, 3.5f, 6), 9, new Color(1f, 0.94f, 0.82f));
            PointLight("Light_103", p, new Vector3(8, 3.5f, 6), 9, new Color(1f, 0.85f, 0.7f));
        }

        static void PointLight(string name, Transform parent, Vector3 pos, float range, Color c)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.localPosition = pos;
            l.type = LightType.Point;
            l.range = range;
            l.intensity = 2f;
            l.color = c;
            l.shadows = LightShadows.None;
        }

        // ---------- 카메라(플레이어를 따라가는 탑뷰) ----------
        static TopDownCameraView BuildCamera(float s, out WallFadeView wallFadeView)
        {
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.fieldOfView = 40;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.06f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.gameObject.AddComponent<AudioListener>(); // 소리를 듣는 귀. 씬에 하나만
            cam.transform.position = new Vector3(-9.5f, 10, -6.5f) * s;
            cam.transform.LookAt(new Vector3(-9.5f, 0, 0) * s);

            var view = cam.gameObject.AddComponent<TopDownCameraView>();
            SetVector3(view, "offset", new Vector3(0f, 10f, -6.5f) * s);
            SetFloat(view, "lookHeight", 0.5f * s);
            SetFloat(view, "closeupMinDistance", 0.9f * s);

            // 카메라와 플레이어 사이 벽 투명화
            wallFadeView = cam.gameObject.AddComponent<WallFadeView>();
            SetFloat(wallFadeView, "targetHeight", 0.8f * s);
            SetFloat(wallFadeView, "radius", 0.25f * s);
            return view;
        }

        // ---------- 화면 UI: 확인 팝업, 엔딩 ----------
        static void BuildUI(out ConfirmPopupView popup, out EndingView ending, out ScreenFadeView screenFade,
            out InteractPromptView prompt, out MapFeedbackView feedback, out InspectPanelView inspectPanel, out PauseMenuView pauseMenu)
        {
            var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var eventSystem = esGo.GetComponent<EventSystem>();

            var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 확인 팝업: 루트는 켜 두고(Awake 실행용) 안쪽 Panel만 켜고 끈다
            var popupRoot = new GameObject("ConfirmPopup", typeof(RectTransform));
            popupRoot.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)popupRoot.transform);
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(popupRoot.transform, false);
            var panelRt = (RectTransform)panel.transform;
            panelRt.sizeDelta = new Vector2(640, 280);
            panel.GetComponent<Image>().color = new Color(0.1f, 0.11f, 0.14f, 0.92f);
            var message = CreateText(panel.transform, "Message", "", 44, new Vector2(0, 50), new Vector2(600, 100));
            var confirm = CreateButton(panel.transform, "ConfirmButton", new Vector2(-150, -70), out var confirmLabel);
            var cancel = CreateButton(panel.transform, "CancelButton", new Vector2(150, -70), out var cancelLabel);

            popup = popupRoot.AddComponent<ConfirmPopupView>();
            SetRef(popup, "panel", panel);
            SetRef(popup, "messageText", message);
            SetRef(popup, "confirmButton", confirm);
            SetRef(popup, "confirmLabel", confirmLabel);
            SetRef(popup, "cancelButton", cancel);
            SetRef(popup, "cancelLabel", cancelLabel);
            SetRef(popup, "eventSystem", eventSystem);
            panel.SetActive(false);

            // 엔딩
            var endingRoot = new GameObject("Ending", typeof(RectTransform));
            endingRoot.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)endingRoot.transform);
            var endingPanel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            endingPanel.transform.SetParent(endingRoot.transform, false);
            Stretch((RectTransform)endingPanel.transform);
            endingPanel.GetComponent<Image>().color = new Color(0.03f, 0.03f, 0.05f, 0.9f);
            CreateText(endingPanel.transform, "Title", "클리어!", 96, new Vector2(0, 40), new Vector2(1200, 140));
            CreateText(endingPanel.transform, "Sub", "무사히 아침을 맞았습니다", 40, new Vector2(0, -60), new Vector2(1200, 80));

            var endingLobby = CreateButton(endingPanel.transform, "LobbyButton", new Vector2(0, -190), out var endingLobbyLabel);
            endingLobbyLabel.text = "로비로";

            ending = endingRoot.AddComponent<EndingView>();
            SetRef(ending, "panel", endingPanel);
            SetRef(ending, "lobbyButton", endingLobby);
            SetRef(ending, "eventSystem", eventSystem);
            endingPanel.SetActive(false);

            // 상호작용 안내(화면 아래 가운데)
            var promptRoot = new GameObject("InteractPrompt", typeof(RectTransform));
            promptRoot.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)promptRoot.transform);
            var promptPanel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            promptPanel.transform.SetParent(promptRoot.transform, false);
            var promptRt = (RectTransform)promptPanel.transform;
            promptRt.anchorMin = promptRt.anchorMax = new Vector2(0.5f, 0f);
            promptRt.pivot = new Vector2(0.5f, 0f);
            promptRt.anchoredPosition = new Vector2(0, 120);
            promptRt.sizeDelta = new Vector2(360, 70);
            var promptImg = promptPanel.GetComponent<Image>();
            promptImg.color = new Color(0.1f, 0.11f, 0.14f, 0.8f);
            promptImg.raycastTarget = false;
            var promptLabel = CreateText(promptPanel.transform, "Label", "", 34, Vector2.zero, promptRt.sizeDelta);
            prompt = promptRoot.AddComponent<InteractPromptView>();
            SetRef(prompt, "panel", promptPanel);
            SetRef(prompt, "label", promptLabel);
            promptPanel.SetActive(false);

            // 정답/오답 피드백(화면 위 가운데)
            var feedbackGo = new GameObject("MapFeedback", typeof(RectTransform), typeof(CanvasGroup), typeof(AudioSource));
            feedbackGo.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)feedbackGo.transform);
            var feedbackGroup = feedbackGo.GetComponent<CanvasGroup>();
            feedbackGroup.alpha = 0f;
            feedbackGroup.blocksRaycasts = false;
            feedbackGroup.interactable = false;
            var feedbackLabel = CreateText(feedbackGo.transform, "Label", "", 56, Vector2.zero, new Vector2(1200, 100));
            var feedbackRt = (RectTransform)feedbackLabel.transform;
            feedbackRt.anchorMin = feedbackRt.anchorMax = new Vector2(0.5f, 1f);
            feedbackRt.anchoredPosition = new Vector2(0, -140);
            feedbackLabel.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.7f);
            var audio = feedbackGo.GetComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
            feedback = feedbackGo.AddComponent<MapFeedbackView>();
            SetRef(feedback, "canvasGroup", feedbackGroup);
            SetRef(feedback, "label", feedbackLabel);
            SetRef(feedback, "audioSource", audio);
            SetRef(feedback, "correctClip", LoadSfx(SfxCorrectName));
            SetRef(feedback, "wrongClip", LoadSfx(SfxWrongName));

            // 조사 패널(화면 아래 가운데): 이름 / 설명 / 닫기 안내
            var inspectRoot = new GameObject("InspectPanel", typeof(RectTransform));
            inspectRoot.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)inspectRoot.transform);
            var inspectBox = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            inspectBox.transform.SetParent(inspectRoot.transform, false);
            var ibRt = (RectTransform)inspectBox.transform;
            ibRt.anchorMin = ibRt.anchorMax = new Vector2(0.5f, 0f);
            ibRt.pivot = new Vector2(0.5f, 0f);
            ibRt.anchoredPosition = new Vector2(0, 60);
            ibRt.sizeDelta = new Vector2(900, 220);
            var ibImg = inspectBox.GetComponent<Image>();
            ibImg.color = new Color(0.08f, 0.09f, 0.12f, 0.85f);
            ibImg.raycastTarget = false;
            var inspectTitle = CreateText(inspectBox.transform, "Title", "", 46, new Vector2(0, 60), new Vector2(860, 70));
            var inspectDesc = CreateText(inspectBox.transform, "Description", "", 30, new Vector2(0, -5), new Vector2(860, 70));
            var inspectHint = CreateText(inspectBox.transform, "Hint", "", 24, new Vector2(0, -75), new Vector2(860, 40));
            inspectHint.color = new Color(1f, 1f, 1f, 0.6f);
            inspectPanel = inspectRoot.AddComponent<InspectPanelView>();
            SetRef(inspectPanel, "panel", inspectBox);
            SetRef(inspectPanel, "titleText", inspectTitle);
            SetRef(inspectPanel, "descriptionText", inspectDesc);
            SetRef(inspectPanel, "hintText", inspectHint);
            inspectBox.SetActive(false);

            // 일시정지 메뉴(화면 가운데)
            var pauseRoot = new GameObject("PauseMenu", typeof(RectTransform));
            pauseRoot.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)pauseRoot.transform);
            var pausePanel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            pausePanel.transform.SetParent(pauseRoot.transform, false);
            Stretch((RectTransform)pausePanel.transform);
            pausePanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
            CreateText(pausePanel.transform, "Title", "일시정지", 72, new Vector2(0, 220), new Vector2(800, 110));
            var resumeButton = CreateButton(pausePanel.transform, "ResumeButton", new Vector2(0, 60), out var resumeLabel);
            resumeLabel.text = "계속하기";
            var pauseLobby = CreateButton(pausePanel.transform, "LobbyButton", new Vector2(0, -50), out var pauseLobbyLabel);
            pauseLobbyLabel.text = "로비로";
            var pauseQuit = CreateButton(pausePanel.transform, "QuitButton", new Vector2(0, -160), out var pauseQuitLabel);
            pauseQuitLabel.text = "종료";
            pauseMenu = pauseRoot.AddComponent<PauseMenuView>();
            SetRef(pauseMenu, "panel", pausePanel);
            SetRef(pauseMenu, "resumeButton", resumeButton);
            SetRef(pauseMenu, "lobbyButton", pauseLobby);
            SetRef(pauseMenu, "quitButton", pauseQuit);
            SetRef(pauseMenu, "eventSystem", eventSystem);
            pausePanel.SetActive(false);

            // 맵 전환 페이드. 마지막 자식이라 팝업·엔딩보다 위에 그려짐. 배경색 판 + 그 위 이미지
            var fadeGo = new GameObject("ScreenFade", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            fadeGo.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)fadeGo.transform);
            var fadeBackground = fadeGo.GetComponent<Image>();
            fadeBackground.color = Color.black;
            var group = fadeGo.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var pictureGo = new GameObject("Picture", typeof(RectTransform), typeof(Image));
            pictureGo.transform.SetParent(fadeGo.transform, false);
            Stretch((RectTransform)pictureGo.transform);
            var picture = pictureGo.GetComponent<Image>();
            picture.preserveAspect = true; // 비율 유지한 채 화면에 맞춤
            picture.raycastTarget = false;
            picture.enabled = false;

            screenFade = fadeGo.AddComponent<ScreenFadeView>();
            SetRef(screenFade, "canvasGroup", group);
            SetRef(screenFade, "background", fadeBackground);
            SetRef(screenFade, "picture", picture);
            SetRef(screenFade, "sleepImage", LoadFadeImage(FadeSleepImagePath));
            SetRef(screenFade, "leaveImage", LoadFadeImage(FadeLeaveImagePath));
            // 페이드 이미지가 흑백이라 배경도 검정으로 통일
            SetColor(screenFade, "sleepBackground", Color.black);
            SetColor(screenFade, "leaveBackground", Color.black);
        }

        static AudioClip LoadSfx(string name)
        {
            EnsureFolder(SfxDir);
            foreach (var ext in new[] { ".wav", ".ogg", ".mp3" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxDir}/{name}{ext}");
                if (clip != null) return clip;
            }
            Debug.Log($"[LevelLayoutBuilder] 효과음 없음(소리 없이 진행): {SfxDir}/{name}.wav");
            return null;
        }

        static Object[] LoadFootstepClips()
        {
            if (!AssetDatabase.IsValidFolder(FootstepDir)) return new Object[0];
            var list = new List<Object>();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { FootstepDir }))
                list.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid)));
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return list.ToArray();
        }

        static Sprite LoadFadeImage(string path)
        {
            EnsureFolder(FadeImageDir);
            SpriteImportUtil.EnsureSprite(path); // 넣기만 하면 Sprite로 임포트되게
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[LevelLayoutBuilder] 페이드 이미지 없음(배경색만 나옴): {path}" +
                                 (path == LeaveImageRenderer.OutputPath ? " — Anomaly > Render Leave Image로 만들 수 있음" : ""));
            return sprite;
        }

        static Text CreateText(Transform parent, string name, string text, int size, Vector2 pos, Vector2 box)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;
            var t = go.GetComponent<Text>();
            t.font = uiFont;
            t.text = text;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }

        static Button CreateButton(Transform parent, string name, Vector2 pos, out Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(240, 80);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.85f, 0.83f, 0.78f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = img;
            label = CreateText(go.transform, "Label", "", 34, Vector2.zero, rt.sizeDelta);
            label.color = new Color(0.12f, 0.12f, 0.14f);
            return button;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ---------- 플레이어 ----------
        // HAON 캐릭터를 자식으로 두고, 루트에 CharacterController·PlayerView·PlayerInputView를 붙인다.
        // 캐릭터 키로 Level 스케일을 정해 반환한다.
        static float BuildPlayer(out PlayerView playerView, out PlayerInputView playerInputView)
        {
            var player = new GameObject("Player");
            float h = 1f;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
            if (prefab == null)
            {
                Debug.LogWarning("[LevelLayoutBuilder] 캐릭터 프리팹을 찾지 못해 캡슐로 대신함: " + CharacterPath);
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.DestroyImmediate(capsule.GetComponent<Collider>());
                capsule.transform.SetParent(player.transform, false);
                capsule.transform.localPosition = Vector3.up;
                h = 2f;
            }
            else
            {
                var ch = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player.transform);
                ch.name = "Character";
                ch.transform.localPosition = Vector3.zero;
                ch.transform.localRotation = Quaternion.identity;
                var rs = ch.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    var b = rs[0].bounds;
                    foreach (var r in rs) b.Encapsulate(r.bounds);
                    if (b.size.y > 0.1f) h = b.size.y;
                }
                foreach (var anim in ch.GetComponentsInChildren<Animator>()) anim.applyRootMotion = false;
            }

            var cc = player.AddComponent<CharacterController>();
            cc.height = h;
            cc.radius = h * 0.2f;
            cc.center = new Vector3(0f, h * 0.5f, 0f);
            cc.skinWidth = cc.radius * 0.1f;
            cc.stepOffset = h * 0.25f;

            playerView = player.AddComponent<PlayerView>();
            SetRef(playerView, "characterController", cc);
            var playerAnimator = player.GetComponentInChildren<Animator>();
            SetRef(playerView, "animator", playerAnimator);

            // 대기·걷기 Animator Controller 연결(HAON 프리팹 원본은 그대로, 씬 인스턴스에서만 교체)
            var animController = PlayerAnimatorBuilder.GetOrCreate();
            if (playerAnimator != null && animController != null)
                playerAnimator.runtimeAnimatorController = animController;
            SetString(playerView, "speedParameter", PlayerAnimatorBuilder.SpeedParameter);

            // 발소리(2D 소리. 탑뷰라 카메라가 멀어 3D로 하면 작게 들림)
            var stepAudio = player.AddComponent<AudioSource>();
            stepAudio.playOnAwake = false;
            stepAudio.spatialBlend = 0f;
            var footsteps = player.AddComponent<FootstepView>();
            SetRef(footsteps, "audioSource", stepAudio);
            var stepClips = LoadFootstepClips();
            if (stepClips.Length == 0) Debug.LogWarning($"[LevelLayoutBuilder] 발소리 없음: {FootstepDir}");
            SetRefArray(footsteps, "clips", stepClips);
            SetRef(playerView, "footsteps", footsteps);

            playerInputView = player.AddComponent<PlayerInputView>();
            var moveRef = FindInputActionReference(InputActionsPath, MoveActionName);
            if (moveRef == null) Debug.LogWarning($"[LevelLayoutBuilder] {InputActionsPath}에서 {MoveActionName} 액션을 찾지 못함");
            SetRef(playerInputView, "moveAction", moveRef);
            var interactRef = FindInputActionReference(InputActionsPath, InteractActionName);
            if (interactRef == null) Debug.LogWarning($"[LevelLayoutBuilder] {InputActionsPath}에서 {InteractActionName} 액션을 찾지 못함");
            SetRef(playerInputView, "interactAction", interactRef);
            var cancelRef = FindInputActionReference(InputActionsPath, "UI/Cancel");
            if (cancelRef == null) Debug.LogWarning($"[LevelLayoutBuilder] {InputActionsPath}에서 UI/Cancel 액션을 찾지 못함");
            SetRef(playerInputView, "cancelAction", cancelRef);

            float s = h / CharacterHeightInKayKitUnits;
            player.transform.position = StartPos * s;
            player.transform.rotation = Quaternion.Euler(0, 90, 0);
            Debug.Log($"[LevelLayoutBuilder] 캐릭터 키 {h:0.###}m 기준으로 Level 스케일 {s:0.###}");
            return s;
        }

        static InputActionReference FindInputActionReference(string assetPath, string actionName)
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                if (obj is InputActionReference r && r.action != null &&
                    $"{r.action.actionMap?.name}/{r.action.name}" == actionName)
                    return r;
            return null;
        }

        // ---------- 진입점 ----------
        static void BuildGameManager(PlayerView playerView, PlayerInputView playerInputView, TopDownCameraView cameraView,
            WallFadeView wallFadeView, ConfirmPopupView popup, EndingView ending, ScreenFadeView screenFade,
            InteractPromptView prompt, MapFeedbackView feedback, InspectPanelView inspectPanel, PauseMenuView pauseMenu)
        {
            var manager = new GameObject("GameManager").AddComponent<GameManager>();
            SetRef(manager, "movementSettings", GetOrCreatePlayerSettings());
            SetRef(manager, "playerInputView", playerInputView);
            SetRef(manager, "playerView", playerView);
            SetRef(manager, "cameraView", cameraView);
            SetRef(manager, "wallFadeView", wallFadeView);
            SetRef(manager, "ruleSettings", GetOrCreateAsset<GameRuleSettings>(RuleSettingsPath));
            SetRef(manager, "anomalyCatalog", GetOrCreateAsset<AnomalyCatalog>(AnomalyCatalogPath));
            SetRefArray(manager, "anomalyTargets", anomalyTargets.ToArray());
            SetRef(manager, "startPoint", startPoint);
            SetRef(manager, "bedZone", bedZone);
            SetRef(manager, "exitZone", exitZone);
            SetRef(manager, "mapSign", mapSign);
            SetRef(manager, "confirmPopup", popup);
            SetRef(manager, "endingView", ending);
            SetRef(manager, "screenFade", screenFade);
            SetRef(manager, "interactPrompt", prompt);
            SetRef(manager, "mapFeedback", feedback);
            SetRef(manager, "inspectPanel", inspectPanel);
            SetRef(manager, "pauseMenu", pauseMenu);
            SetRefArray(manager, "inspectables", inspectables.ToArray());
            SetString(manager, "lobbySceneName", "Lobby");
        }

        // ---------- 데이터 에셋 ----------
        // 이미 있으면 그대로 쓰고(인스펙터에서 바꾼 값 유지), 없을 때만 만든다
        static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        // 이상현상 에셋을 만들고 카탈로그에 없으면 추가
        static AnomalyData GetOrCreateAnomaly(string fileName, string id, AnomalyDifficulty difficulty, AnomalyRoom room, AnomalyKind kind, string description)
        {
            string folder = $"{AnomalyDir}/{room}";
            string path = $"{folder}/ANM_{fileName}.asset";
            var data = AssetDatabase.LoadAssetAtPath<AnomalyData>(path);
            if (data == null)
            {
                data = GetOrCreateAsset<AnomalyData>(path);
                var so = new SerializedObject(data);
                so.FindProperty("id").stringValue = id;
                so.FindProperty("difficulty").enumValueIndex = (int)difficulty;
                so.FindProperty("room").enumValueIndex = (int)room;
                so.FindProperty("kind").enumValueIndex = (int)kind;
                so.FindProperty("description").stringValue = description;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var catalog = GetOrCreateAsset<AnomalyCatalog>(AnomalyCatalogPath);
            var cso = new SerializedObject(catalog);
            var list = cso.FindProperty("anomalies");
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == data) return data;
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = data;
            cso.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return data;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        static PlayerMovementSettings GetOrCreatePlayerSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>(PlayerSettingsPath);
            if (settings != null) return settings;
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Data")) AssetDatabase.CreateFolder("Assets/_Project", "Data");
            if (!AssetDatabase.IsValidFolder(PlayerSettingsDir)) AssetDatabase.CreateFolder("Assets/_Project/Data", "Player");
            settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
            AssetDatabase.CreateAsset(settings, PlayerSettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[LevelLayoutBuilder] {target.GetType().Name}.{field} 필드 없음"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetRefArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null || !prop.isArray) { Debug.LogWarning($"[LevelLayoutBuilder] {target.GetType().Name}.{field} 배열 필드 없음"); return; }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetColor(Object target, string field, Color value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[LevelLayoutBuilder] {target.GetType().Name}.{field} 필드 없음"); return; }
            prop.colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetVector3(Object target, string field, Vector3 value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[LevelLayoutBuilder] {target.GetType().Name}.{field} 필드 없음"); return; }
            prop.vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[LevelLayoutBuilder] {target.GetType().Name}.{field} 필드 없음"); return; }
            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[LevelLayoutBuilder] {target.GetType().Name}.{field} 필드 없음"); return; }
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- 도우미 ----------
        static void MeasureKayKitScale()
        {
            var chair = AssetDatabase.LoadAssetAtPath<GameObject>(KayKitDir + "chair_A.fbx");
            if (chair == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(chair);
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                if (b.size.y > 0.001f) kayKitFix = 1.26f / b.size.y; // 원본 의자 높이 1.26
            }
            Object.DestroyImmediate(go);
        }

        static GameObject Prop(string model, Transform parent, float x, float y, float z, float rotY)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(KayKitDir + model + ".fbx");
            if (asset == null) { Debug.LogWarning("[LevelLayoutBuilder] 모델 없음: " + model); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.Euler(0, rotY, 0);
            go.transform.localScale = go.transform.localScale * kayKitFix;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = atlasMat;
                r.sharedMaterials = mats;
            }
            // 플레이어가 통과하지 않도록 메시마다 BoxCollider. 러그는 밟고 지나가므로 제외
            if (!model.StartsWith("rug_"))
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                    if (mf.GetComponent<Collider>() == null) mf.gameObject.AddComponent<BoxCollider>();
            return go;
        }

        static Transform Group(string name, Transform parent)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }

        static void WallX(string name, Transform p, float x0, float x1, float z, float h = WallH) =>
            AddFade(Box(name, p, new Vector3((x0 + x1) / 2, h / 2, z), new Vector3(x1 - x0, h, WallT), wallMat));

        static void WallZ(string name, Transform p, float x, float z0, float z1) =>
            AddFade(Box(name, p, new Vector3(x, WallH / 2, (z0 + z1) / 2), new Vector3(WallT, WallH, z1 - z0), wallMat));

        // 북쪽 벽은 항상 플레이어 뒤쪽이라 가릴 일이 없어서 제외
        static void AddFade(GameObject wall)
        {
            if (wall.name == "Wall_North") return;
            var fade = wall.AddComponent<FadeableView>();
            SetRef(fade, "targetRenderer", wall.GetComponent<MeshRenderer>());
            SetRef(fade, "fadeMaterial", wallFadeMat);
        }

        static void CreateMaterials()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project")) AssetDatabase.CreateFolder("Assets", "_Project");
            if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/_Project", "Materials");

            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            atlasMat = GetMat("M_KayKit_Atlas", Color.white, atlas, 0.1f);
            wallMat = GetMat("M_Wall", new Color(0.86f, 0.83f, 0.76f), null, 0.05f);
            wallFadeMat = GetFadeMat("M_Wall_Fade", new Color(0.86f, 0.83f, 0.76f));
            roomFloorMat = GetMat("M_Floor_Room", new Color(0.62f, 0.5f, 0.38f), null, 0.15f);
            corridorFloorMat = GetMat("M_Floor_Corridor", new Color(0.55f, 0.58f, 0.56f), null, 0.15f);
            corridorFloorMatB = GetMat("M_Floor_Corridor_B", new Color(0.66f, 0.68f, 0.65f), null, 0.15f);
            groutMat = GetMat("M_Floor_Grout", new Color(0.25f, 0.26f, 0.26f), null, 0f);
            forwardMat = GetMat("M_Exit_Forward", new Color(0.36f, 0.79f, 0.65f), null, 0f);
            backMat = GetMat("M_Exit_Back", new Color(0.94f, 0.6f, 0.48f), null, 0f);
            AssetDatabase.SaveAssets();
        }

        static Material GetMat(string name, Color color, Texture2D tex, float smooth)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit"); // CLAUDE.md 7: URP 셰이더만
                if (shader == null) { Debug.LogError("[LevelLayoutBuilder] URP Lit 셰이더를 찾지 못함"); return null; }
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (tex != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(m);
            return m;
        }

        // URP Lit을 Transparent(Alpha 블렌드)로 설정한 머티리얼
        static Material GetFadeMat(string name, Color color)
        {
            var m = GetMat(name, color, null, 0.05f);
            if (m == null) return null;
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_AlphaClip", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
