// 메뉴: Anomaly > Build Lobby
// 엘리베이터 안에 HAON 캐릭터가 서 있는 로비 씬을 만들어 Assets/Scenes/Lobby.unity 로 저장한다(실행할 때마다 덮어씀).
// 엘리베이터는 보이기만 하는 고정 모델(상자 조합). 게임 시작 → 암전 → Level_Corridor(0번 맵 복도) 로드.
// Build Profiles 씬 목록을 Lobby(0번) → Level_Corridor(1번) 순서로 맞춘다.
using System.Collections.Generic;
using System.Linq;
using Anomaly.Editor.Animations;
using Anomaly.Presenter.Manager;
using Anomaly.View.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Anomaly.Editor.Lobby
{
    public static class LobbyBuilder
    {
        const string ScenePath = "Assets/Scenes/Lobby.unity";
        const string GameScenePath = "Assets/Scenes/Level_Corridor.unity";
        const string GameSceneName = "Level_Corridor";
        const string CharacterPath = "Assets/Haons SD series Pack/Prefab/CharacterSet/prf_Set Costume01 Unity-Chan.prefab";
        // 원본을 읽어 표시만 함(복사·임포트 설정 변경 없음 — CLAUDE.md 4)
        const string UclLogoPath = "Assets/Haons SD series Pack/_License/UCL3.0/License Logo/Others/png/Light_Frame.png";
        const string MatDir = "Assets/_Project/Materials/Lobby";

        // 좌표는 레벨과 같은 단위(캐릭터 키 = 1.6). 마지막에 캐릭터 키에 맞춰 루트를 스케일한다
        const float CharacterHeightInUnits = 1.6f;
        const float HalfW = 1.2f, HalfD = 1.2f, CabH = 2.8f, WallT = 0.1f;

        static Font uiFont;

        [MenuItem("Anomaly/Build Lobby")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var root = new GameObject("Elevator").transform;
            BuildCab(root);

            float s = BuildCharacter();
            root.localScale = Vector3.one * s;
            foreach (var l in root.GetComponentsInChildren<Light>()) l.range *= s;

            BuildCamera(s);
            BuildUI(out var menu, out var fade);

            var manager = new GameObject("LobbyManager").AddComponent<LobbyManager>();
            SetRef(manager, "menuView", menu);
            SetRef(manager, "screenFade", fade);
            SetString(manager, "gameSceneName", GameSceneName);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.34f, 0.37f);

            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            UpdateBuildScenes();
            Debug.Log($"[LobbyBuilder] 완료: {ScenePath} / 스케일 {s:0.###}");
        }

        // ---------- 엘리베이터 내부(고정) ----------
        // 카메라는 문 쪽(-z)에서 안쪽(+z)을 본다. 뒷벽에 버튼 패널·층 표시, 양옆과 뒤에 손잡이
        static void BuildCab(Transform root)
        {
            var steel = Mat("M_Lobby_Steel", new Color(0.70f, 0.72f, 0.75f), 0.65f, 0.8f);
            var steelDark = Mat("M_Lobby_SteelDark", new Color(0.42f, 0.44f, 0.47f), 0.6f, 0.8f);
            var seam = Mat("M_Lobby_Seam", new Color(0.2f, 0.2f, 0.22f), 0.2f, 0f);
            var floor = Mat("M_Lobby_Floor", new Color(0.22f, 0.22f, 0.24f), 0.3f, 0f);
            var floorInlay = Mat("M_Lobby_FloorInlay", new Color(0.36f, 0.33f, 0.30f), 0.3f, 0f);
            var ceiling = Mat("M_Lobby_Ceiling", new Color(0.85f, 0.85f, 0.86f), 0.1f, 0f);
            var lightPanel = Mat("M_Lobby_LightPanel", Color.white, 0f, 0f, new Color(1f, 0.97f, 0.9f) * 1.6f);
            var brass = Mat("M_Lobby_Brass", new Color(0.85f, 0.68f, 0.38f), 0.75f, 1f);
            var button = Mat("M_Lobby_Button", new Color(0.9f, 0.9f, 0.88f), 0.5f, 0.3f);
            var buttonLit = Mat("M_Lobby_ButtonLit", new Color(1f, 0.7f, 0.3f), 0.5f, 0f, new Color(1f, 0.55f, 0.15f) * 2f);
            var black = Mat("M_Lobby_Display", new Color(0.04f, 0.04f, 0.05f), 0.8f, 0f);

            // 바닥·천장
            Box("Floor", root, new Vector3(0, -0.05f, 0), new Vector3(HalfW * 2, 0.1f, HalfD * 2), floor);
            Box("Floor_Inlay", root, new Vector3(0, 0.003f, 0.1f), new Vector3(1.6f, 0.006f, 1.6f), floorInlay);
            Box("Ceiling", root, new Vector3(0, CabH + 0.05f, 0), new Vector3(HalfW * 2, 0.1f, HalfD * 2), ceiling);
            Box("Ceiling_Light", root, new Vector3(0, CabH - 0.01f, 0.1f), new Vector3(1.4f, 0.02f, 1.4f), lightPanel);

            // 벽: 뒤·양옆·앞(문, 카메라 뒤쪽)
            Box("Wall_Back", root, new Vector3(0, CabH / 2, HalfD + WallT / 2), new Vector3(HalfW * 2, CabH, WallT), steel);
            Box("Wall_Left", root, new Vector3(-HalfW - WallT / 2, CabH / 2, 0), new Vector3(WallT, CabH, HalfD * 2), steel);
            Box("Wall_Right", root, new Vector3(HalfW + WallT / 2, CabH / 2, 0), new Vector3(WallT, CabH, HalfD * 2), steel);
            Box("Wall_Front", root, new Vector3(0, CabH / 2, -HalfD - WallT / 2), new Vector3(HalfW * 2, CabH, WallT), steel);
            Box("Door_L", root, new Vector3(-0.45f, 1.1f, -HalfD + 0.01f), new Vector3(0.88f, 2.2f, 0.02f), steelDark);
            Box("Door_R", root, new Vector3(0.45f, 1.1f, -HalfD + 0.01f), new Vector3(0.88f, 2.2f, 0.02f), steelDark);

            // 벽 패널 이음매
            foreach (float x in new[] { -0.4f, 0.4f })
                Box("Seam_Back", root, new Vector3(x, CabH / 2, HalfD - 0.004f), new Vector3(0.02f, CabH, 0.008f), seam);
            foreach (float z in new[] { -0.4f, 0.4f })
            {
                Box("Seam_Left", root, new Vector3(-HalfW + 0.004f, CabH / 2, z), new Vector3(0.008f, CabH, 0.02f), seam);
                Box("Seam_Right", root, new Vector3(HalfW - 0.004f, CabH / 2, z), new Vector3(0.008f, CabH, 0.02f), seam);
            }

            // 걸레받이
            Box("Kick_Back", root, new Vector3(0, 0.06f, HalfD - 0.006f), new Vector3(HalfW * 2, 0.12f, 0.012f), steelDark);
            Box("Kick_Left", root, new Vector3(-HalfW + 0.006f, 0.06f, 0), new Vector3(0.012f, 0.12f, HalfD * 2), steelDark);
            Box("Kick_Right", root, new Vector3(HalfW - 0.006f, 0.06f, 0), new Vector3(0.012f, 0.12f, HalfD * 2), steelDark);

            // 손잡이
            Box("Rail_Back", root, new Vector3(0, 0.9f, HalfD - 0.1f), new Vector3(2.0f, 0.04f, 0.04f), brass);
            foreach (float x in new[] { -0.9f, 0.9f })
                Box("Rail_Back_Bracket", root, new Vector3(x, 0.9f, HalfD - 0.05f), new Vector3(0.03f, 0.03f, 0.08f), brass);
            foreach (float sx in new[] { -1f, 1f })
                Box(sx < 0 ? "Rail_Left" : "Rail_Right", root, new Vector3(sx * (HalfW - 0.1f), 0.9f, 0.3f), new Vector3(0.04f, 0.04f, 1.4f), brass);

            // 버튼 패널(뒷벽 오른쪽)
            var panelPos = new Vector3(0.85f, 1.15f, HalfD - 0.012f);
            Box("ButtonPanel", root, panelPos, new Vector3(0.34f, 0.9f, 0.02f), steelDark);
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 2; col++)
                {
                    bool lit = row == 0 && col == 0;
                    var b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    b.name = lit ? "Button_Lit" : "Button";
                    Object.DestroyImmediate(b.GetComponent<Collider>());
                    b.transform.SetParent(root, false);
                    b.transform.localPosition = new Vector3(panelPos.x - 0.07f + col * 0.14f, 1.42f - row * 0.15f, HalfD - 0.025f);
                    b.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    b.transform.localScale = new Vector3(0.07f, 0.006f, 0.07f);
                    b.GetComponent<MeshRenderer>().sharedMaterial = lit ? buttonLit : button;
                }

            // 층 표시판(뒷벽 위 가운데)
            Box("FloorDisplay", root, new Vector3(0, 2.4f, HalfD - 0.012f), new Vector3(0.6f, 0.25f, 0.02f), black);
            var canvasGo = new GameObject("FloorDisplay_Text", typeof(RectTransform), typeof(Canvas));
            canvasGo.transform.SetParent(root, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)canvasGo.transform;
            rt.sizeDelta = new Vector2(60, 25);
            rt.localScale = Vector3.one * 0.01f;
            rt.localPosition = new Vector3(0, 2.4f, HalfD - 0.025f);
            var t = CreateText(canvasGo.transform, "Text", "0", 22, Vector2.zero, rt.sizeDelta, TextAnchor.MiddleCenter);
            t.color = new Color(1f, 0.6f, 0.15f);

            // 조명
            var l = new GameObject("Light_Ceiling").AddComponent<Light>();
            l.transform.SetParent(root, false);
            l.transform.localPosition = new Vector3(0, CabH - 0.3f, 0.1f);
            l.type = LightType.Point;
            l.range = 6f;
            l.intensity = 1.8f;
            l.color = new Color(1f, 0.96f, 0.9f);
            l.shadows = LightShadows.None;
        }

        // ---------- 캐릭터(대기 모션) ----------
        static float BuildCharacter()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
            float h = 1.6f;
            if (prefab == null)
            {
                Debug.LogWarning("[LobbyBuilder] 캐릭터 프리팹을 찾지 못함: " + CharacterPath);
                return 1f;
            }
            var ch = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            ch.name = "Character";
            var rs = ch.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                if (b.size.y > 0.1f) h = b.size.y;
            }
            float s = h / CharacterHeightInUnits;
            ch.transform.position = new Vector3(0, 0, 0.35f) * s;
            ch.transform.rotation = Quaternion.Euler(0, 180, 0); // 카메라(문 쪽)를 봄

            var animController = LobbyAnimatorBuilder.GetOrCreate(); // 서 있는 자세로 정지(대기 모션 없음)
            foreach (var anim in ch.GetComponentsInChildren<Animator>())
            {
                anim.applyRootMotion = false;
                if (animController != null) anim.runtimeAnimatorController = animController;
            }
            return s;
        }

        static void BuildCamera(float s)
        {
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.fieldOfView = 55;
            cam.nearClipPlane = 0.03f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.transform.position = new Vector3(0, 1.75f, -HalfD + 0.1f) * s;
            cam.transform.LookAt(new Vector3(0, 0.85f, 0.35f) * s);
        }

        // ---------- UI: 제목, 메뉴, 크레딧, 암전 ----------
        static void BuildUI(out LobbyMenuView menu, out ScreenFadeView fade)
        {
            var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var eventSystem = esGo.GetComponent<EventSystem>();

            var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 제목(가제)
            var title = CreateText(canvasGo.transform, "Title", "Anomaly my room", 96, Vector2.zero, new Vector2(1000, 140), TextAnchor.MiddleLeft);
            Anchor(title.rectTransform, new Vector2(0, 1), new Vector2(120, -110));
            title.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.6f);

            // 메뉴
            var startButton = CreateButton(canvasGo.transform, "StartButton", "게임 시작", new Vector2(120, -300));
            var quitButton = CreateButton(canvasGo.transform, "QuitButton", "종료", new Vector2(120, -400));
            var menuGo = new GameObject("LobbyMenu");
            menuGo.transform.SetParent(canvasGo.transform, false);
            menu = menuGo.AddComponent<LobbyMenuView>();
            SetRef(menu, "startButton", startButton);
            SetRef(menu, "quitButton", quitButton);
            SetRef(menu, "eventSystem", eventSystem);

            // 크레딧(라이선스 표기 필수: UCL 로고 + 문구)
            var logoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(UclLogoPath);
            float textX = 40;
            if (logoTex != null)
            {
                var logoGo = new GameObject("UCL_Logo", typeof(RectTransform), typeof(RawImage));
                logoGo.transform.SetParent(canvasGo.transform, false);
                var logo = logoGo.GetComponent<RawImage>();
                logo.texture = logoTex;
                logo.raycastTarget = false;
                float hgt = 90f, wid = hgt * logoTex.width / Mathf.Max(1, logoTex.height);
                var lrt = logo.rectTransform;
                lrt.anchorMin = lrt.anchorMax = lrt.pivot = Vector2.zero;
                lrt.anchoredPosition = new Vector2(40, 36);
                lrt.sizeDelta = new Vector2(wid, hgt);
                textX = 40 + wid + 24;
            }
            else Debug.LogWarning("[LobbyBuilder] UCL 로고를 찾지 못함(크레딧에 반드시 필요): " + UclLogoPath);

            var credit = CreateText(canvasGo.transform, "Credits",
                "This work is provided under Unity-Chan License Terms\n© Unity Technologies Japan/UCL\nFurniture by Kay Lousberg (www.kaylousberg.com)",
                22, Vector2.zero, new Vector2(900, 90), TextAnchor.LowerLeft);
            var crt = credit.rectTransform;
            crt.anchorMin = crt.anchorMax = crt.pivot = Vector2.zero;
            crt.anchoredPosition = new Vector2(textX, 36);
            credit.color = new Color(1, 1, 1, 0.85f);
            credit.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.6f);

            // 암전(마지막 자식 = 가장 위)
            var fadeGo = new GameObject("ScreenFade", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            fadeGo.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)fadeGo.transform);
            var bg = fadeGo.GetComponent<Image>();
            bg.color = Color.black;
            var group = fadeGo.GetComponent<CanvasGroup>();
            group.alpha = 1f; // 씬 시작은 암전 상태
            group.interactable = false;
            fade = fadeGo.AddComponent<ScreenFadeView>();
            SetRef(fade, "canvasGroup", group);
            SetRef(fade, "background", bg);
        }

        // Build Profiles 씬 목록: Lobby(0) → Level_Corridor(1) → 나머지 기존 순서
        static void UpdateBuildScenes()
        {
            var list = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath) != null)
                list.Add(new EditorBuildSettingsScene(GameScenePath, true));
            else
                Debug.LogWarning("[LobbyBuilder] 게임 씬이 없음. Anomaly > Build Level Layout 먼저 실행: " + GameScenePath);
            list.AddRange(EditorBuildSettings.scenes.Where(sc => sc.path != ScenePath && sc.path != GameScenePath));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ---------- 도우미 ----------
        static Material Mat(string name, Color color, float smooth, float metallic, Color? emission = null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials")) AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/_Project/Materials", "Lobby");
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit"); // URP 셰이더만
                if (shader == null) { Debug.LogError("[LobbyBuilder] URP Lit 셰이더를 찾지 못함"); return null; }
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>()); // 로비는 이동이 없어 충돌체 불필요
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }

        static Text CreateText(Transform parent, string name, string text, int size, Vector2 pos, Vector2 box, TextAnchor align)
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
            t.alignment = align;
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }

        static Button CreateButton(Transform parent, string name, string label, Vector2 topLeftPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Anchor(rt, new Vector2(0, 1), topLeftPos);
            rt.sizeDelta = new Vector2(320, 80);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.12f, 0.13f, 0.16f, 0.85f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
            colors.selectedColor = new Color(1f, 0.85f, 0.6f);
            button.colors = colors;
            var t = CreateText(go.transform, "Label", label, 38, Vector2.zero, rt.sizeDelta, TextAnchor.MiddleCenter);
            return button;
        }

        static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[LobbyBuilder] {target.GetType().Name}.{field} 필드 없음"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[LobbyBuilder] {target.GetType().Name}.{field} 필드 없음"); return; }
            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
