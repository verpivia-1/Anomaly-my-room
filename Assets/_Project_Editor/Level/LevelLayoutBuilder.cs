// 메뉴: Anomaly > Build Level Layout
// 복도 1개 + 방 3개(101 거실, 102 식당, 103 침실)를 만들고 KayKit 가구를 배치해 Assets/Scenes/Level_Corridor.unity 로 저장한다.
// 좌표는 KayKit 단위(의자 높이 1.26)로 적혀 있고, 마지막에 HAON SD 캐릭터 키에 맞춰 Level 루트 전체를 스케일한다.
// 다시 실행하면 씬을 새로 만든다(기존 Level_Corridor 씬은 덮어씀).
// 플레이어(HAON 캐릭터 + CharacterController), 탑뷰 카메라, GameManager까지 연결해 바로 플레이할 수 있게 만든다.
using Anomaly.Editor.Animations;
using Anomaly.Model.Player;
using Anomaly.Presenter.Manager;
using Anomaly.View.Cameras;
using Anomaly.View.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

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
        const string PlayerSettingsDir = "Assets/_Project/Data/Player";
        const string PlayerSettingsPath = PlayerSettingsDir + "/PlayerMovementSettings.asset";

        // 캐릭터 키를 KayKit 단위로 몇으로 볼지. 가구가 너무 크거나 작으면 이 값만 바꿔서 다시 실행.
        const float CharacterHeightInKayKitUnits = 1.6f;

        const float WallH = 4f, WallT = 0.2f;
        // 탑뷰 규칙: 카메라는 남쪽 위에서 북쪽을 본다. 카메라 쪽(남쪽)을 막는 벽은 세우지 않거나 낮게 둔다.
        const float LowWallH = 0.4f; // 복도와 방 사이 벽 높이(방 안이 가리지 않게)

        static Material atlasMat, wallMat, roomFloorMat, corridorFloorMat, corridorFloorMatB, groutMat, forwardMat, backMat;
        static float kayKitFix = 1f; // FBX 임포트 단위 보정

        [MenuItem("Anomaly/Build Level Layout")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateMaterials();
            MeasureKayKitScale();

            var root = new GameObject("Level").transform;
            BuildShell(root);
            // 복도에는 소품을 두지 않는다
            BuildLiving(Group("Room_101_Living", root));
            BuildDining(Group("Room_102_Dining", root));
            BuildBedroom(Group("Room_103_Bedroom", root));
            BuildLights(Group("Lights", root));

            float s = BuildPlayer(out var playerView, out var playerInputView);
            root.localScale = Vector3.one * s;
            foreach (var l in root.GetComponentsInChildren<Light>())
                if (l.type == LightType.Point) l.range *= s;

            var cameraView = BuildCamera(s);
            BuildGameManager(playerView, playerInputView, cameraView);

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

            // 뒤로 출구 표시(복도 서쪽 끝은 열려 있음)
            var exits = Group("Exits", root);
            var back = Box("Exit_Back", exits, new Vector3(-11.5f, 0.01f, 0), new Vector3(1, 0.02f, 4), backMat);
            Object.DestroyImmediate(back.GetComponent<Collider>());
        }

        // ---------- 101 거실 (x -12..-4) ----------
        static void BuildLiving(Transform p)
        {
            Prop("rug_rectangle_A", p, -8, 0, 6.3f, 0);
            Prop("couch_pillows", p, -8, 0, 9.1f, 0);
            Prop("table_low", p, -8, 0, 6.6f, 0);
            Prop("armchair", p, -11, 0, 6.0f, -90);
            Prop("cabinet_medium_decorated", p, -4.6f, 0, 6.3f, -90);
            Prop("lamp_standing", p, -11.2f, 0, 9.2f, 0);
            Prop("pictureframe_large_B", p, -8, 2.6f, 10, 180);
            Prop("shelf_B_large_decorated", p, -12, 2.2f, 4.0f, 90);
            Prop("cactus_medium_A", p, -4.8f, 0, 9.3f, 0);
        }

        // ---------- 102 식당 (x -4..4) ----------
        static void BuildDining(Transform p)
        {
            Prop("rug_oval_A", p, 0, 0, 6.4f, 0);
            Prop("table_medium_long", p, 0, 0, 6.4f, 0);
            Prop("chair_A", p, -0.75f, 0, 4.9f, 0);
            Prop("chair_A", p, 0.75f, 0, 4.9f, 0);
            Prop("chair_A", p, -0.75f, 0, 7.9f, 180);
            Prop("chair_A", p, 0.75f, 0, 7.9f, 180);
            Prop("cabinet_medium_decorated", p, -2.2f, 0, 9.45f, 180);
            Prop("cabinet_medium", p, 2.2f, 0, 9.45f, 180);
            Prop("book_set", p, 1.7f, 1.25f, 9.45f, 180);
            Prop("lamp_table", p, 2.8f, 1.0f, 9.45f, 0);
            Prop("pictureframe_medium", p, 0, 2.6f, 10, 180);
            Prop("shelf_B_small_decorated", p, -3.9f, 2.0f, 4.0f, 90);
            Prop("cactus_small_A", p, 3.4f, 0, 2.7f, 0);
        }

        // ---------- 103 침실 (x 4..12) ----------
        static void BuildBedroom(Transform p)
        {
            Prop("rug_oval_B", p, 8, 0, 5.2f, 0);
            Prop("bed_double_A", p, 9, 0, 8.4f, 180);
            // 도착점: 침대 상호작용 위치(빈 오브젝트). 상호작용 스크립트는 나중에 여기에 붙인다
            var goal = new GameObject("Goal_BedInteract");
            goal.transform.SetParent(p, false);
            goal.transform.localPosition = new Vector3(9, 0, 6.5f); // 침대 발치 앞
            var col = goal.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0, 1, 0);
            col.size = new Vector3(3, 2, 1.5f);
            Prop("table_small", p, 11.3f, 0, 9.35f, 0);
            Prop("lamp_table", p, 11.3f, 1.0f, 9.35f, 0);
            Prop("table_small", p, 6.6f, 0, 9.35f, 0);
            Prop("pictureframe_standing_A", p, 6.6f, 1.0f, 9.35f, 180);
            Prop("cabinet_small_decorated", p, 11.35f, 0, 4.0f, -90);
            Prop("chair_C", p, 5.2f, 0, 4.2f, 90);
            Prop("shelf_B_small", p, 4.1f, 2.0f, 5.0f, 90);
            Prop("pictureframe_small_A", p, 9, 2.6f, 10, 180);
            Prop("cactus_small_B", p, 4.7f, 0, 2.7f, 0);
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
        static TopDownCameraView BuildCamera(float s)
        {
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.fieldOfView = 40;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.06f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(-9.5f, 10, -6.5f) * s;
            cam.transform.LookAt(new Vector3(-9.5f, 0, 0) * s);

            var view = cam.gameObject.AddComponent<TopDownCameraView>();
            SetVector3(view, "offset", new Vector3(0f, 10f, -6.5f) * s);
            SetFloat(view, "lookHeight", 0.5f * s);
            return view;
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

            playerInputView = player.AddComponent<PlayerInputView>();
            var moveRef = FindInputActionReference(InputActionsPath, MoveActionName);
            if (moveRef == null) Debug.LogWarning($"[LevelLayoutBuilder] {InputActionsPath}에서 {MoveActionName} 액션을 찾지 못함");
            SetRef(playerInputView, "moveAction", moveRef);

            float s = h / CharacterHeightInKayKitUnits;
            player.transform.position = new Vector3(-9.5f, 0.05f, 0) * s;
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
        static void BuildGameManager(PlayerView playerView, PlayerInputView playerInputView, TopDownCameraView cameraView)
        {
            var manager = new GameObject("GameManager").AddComponent<GameManager>();
            SetRef(manager, "movementSettings", GetOrCreatePlayerSettings());
            SetRef(manager, "playerInputView", playerInputView);
            SetRef(manager, "playerView", playerView);
            SetRef(manager, "cameraView", cameraView);
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
            Box(name, p, new Vector3((x0 + x1) / 2, h / 2, z), new Vector3(x1 - x0, h, WallT), wallMat);

        static void WallZ(string name, Transform p, float x, float z0, float z1) =>
            Box(name, p, new Vector3(x, WallH / 2, (z0 + z1) / 2), new Vector3(WallT, WallH, z1 - z0), wallMat);

        static void CreateMaterials()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project")) AssetDatabase.CreateFolder("Assets", "_Project");
            if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/_Project", "Materials");

            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            atlasMat = GetMat("M_KayKit_Atlas", Color.white, atlas, 0.1f);
            wallMat = GetMat("M_Wall", new Color(0.86f, 0.83f, 0.76f), null, 0.05f);
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
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (tex != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
