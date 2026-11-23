// 메뉴: Anomaly > Render Leave Image
// 나가기 페이드 이미지(Fade_Leave.png)를 만든다(흑백): 검은 배경 + 흰 테두리 표지판 + 왼쪽 화살표 + HAON 캐릭터가 왼쪽으로 달려가는 모습.
// 캐릭터는 흰 실루엣(모양) + 컬러 렌더의 밝기를 3단계 회색으로 단순화(안쪽 구분)해서 넣는다.
// 확인용 컬러 렌더는 Temp/LeaveImage_Debug.png 에 남는다.
// 캐릭터는 임시 미리보기 씬에서 달리기 클립의 한 프레임으로 자세를 잡아 찍는다(프리팹·HAON 폴더는 수정하지 않음).
// 자세가 마음에 안 들면 PoseTime01만 바꿔 다시 실행 → Anomaly > Build Level Layout.
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Anomaly.Editor.Art
{
    public static class LeaveImageRenderer
    {
        const string CharacterPath = "Assets/Haons SD series Pack/Prefab/CharacterSet/prf_Set Costume01 Unity-Chan.prefab";
        const string RunClipPath = "Assets/Haons SD series Pack/Animation/Common/RunA_Front.FBX";
        public const string OutputPath = "Assets/_Project/Art/UI/Fade/Fade_Leave.png";

        // 달리기 한 주기 중 찍을 지점(0~1). 다리를 크게 벌린 프레임이 되도록 조절
        const float PoseTime01 = 0.25f;

        const int ImageW = 1920, ImageH = 1080, MaskSize = 1024;
        static readonly Color32 Backdrop = new Color32(0, 0, 0, 255); // ScreenFadeView.leaveBackground(검정)
        static readonly Color32 White = new Color32(255, 255, 255, 255);

        [MenuItem("Anomaly/Render Leave Image")]
        public static void Render()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
            var clip = LoadClip(RunClipPath);
            if (prefab == null || clip == null)
            {
                Debug.LogError($"[LeaveImageRenderer] 캐릭터나 달리기 클립을 찾지 못함: {CharacterPath}, {RunClipPath}");
                return;
            }

            var mask = RenderSilhouette(prefab, clip, out var color);
            if (mask == null) return;
            var image = Compose(mask, color);
            Object.DestroyImmediate(mask);
            Object.DestroyImmediate(color);

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllBytes(OutputPath, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(OutputPath);
            SpriteImportUtil.EnsureSprite(OutputPath);

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(OutputPath);
            Debug.Log($"[LeaveImageRenderer] 완료(v6, 정면 {lastFacing}, 상하반전 {flipped}): {OutputPath} — Anomaly > Build Level Layout을 다시 실행하면 연결됨");
        }

        // ---------- 1. 캐릭터 실루엣 찍기(흰색 캐릭터, 검은 배경) ----------
        static Texture2D RenderSilhouette(GameObject prefab, AnimationClip clip, out Texture2D color)
        {
            color = null;
            var scene = EditorSceneManager.NewPreviewScene();
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogError("[LeaveImageRenderer] URP Unlit 셰이더를 찾지 못함");
                EditorSceneManager.ClosePreviewScene(scene);
                return null;
            }
            var white = new Material(shader);
            white.SetColor("_BaseColor", Color.white);

            var rt = new RenderTexture(MaskSize, MaskSize, 24, RenderTextureFormat.ARGB32);
            var prevActive = RenderTexture.active;
            GameObject ch = null, camGo = null;
            Texture2D mask = null;
            try
            {
                ch = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                ch.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); // 방향은 카메라 쪽에서 맞춤

                AnimationMode.StartAnimationMode();
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(ch, clip, clip.length * Mathf.Clamp01(PoseTime01));
                AnimationMode.EndSampling();

                // 넉넉하게 잡고, 찍은 뒤 실루엣 영역만 잘라 씀
                var bounds = new Bounds(ch.transform.position, Vector3.zero);
                foreach (var r in ch.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
                float size = Mathf.Max(bounds.size.y, bounds.size.x, bounds.size.z, 0.5f) * 0.85f;

                camGo = new GameObject("LeaveImageCamera");
                SceneManager.MoveGameObjectToScene(camGo, scene);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = scene;
                cam.orthographic = true;
                cam.orthographicSize = size;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 100f;
                // 애니메이션 샘플링이 루트 회전을 덮어쓰므로, 자세를 잡은 뒤의 실제 정면 방향으로 카메라를 옆에 세움.
                // 캐릭터 정면이 화면 왼쪽이 되도록: 카메라 오른쪽 = 캐릭터 뒤쪽
                var facing = FindFacing(ch);
                lastFacing = facing;
                var camForward = Vector3.Cross(Vector3.up, facing).normalized;
                cam.transform.SetPositionAndRotation(bounds.center - camForward * 20f, Quaternion.LookRotation(camForward, Vector3.up));
                cam.targetTexture = rt;

                // 확인용 컬러 렌더(조명 하나 켜고 원래 머티리얼 그대로) → Temp/LeaveImage_Debug.png
                var lightGo = new GameObject("DebugLight");
                SceneManager.MoveGameObjectToScene(lightGo, scene);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                lightGo.transform.rotation = Quaternion.LookRotation(camForward + Vector3.down * 0.5f);
                cam.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
                cam.Render();
                color = ReadRenderTexture(rt);
                File.WriteAllBytes("Temp/LeaveImage_Debug.png", color.EncodeToPNG());
                Object.DestroyImmediate(lightGo);

                // 실루엣 렌더: 전부 흰색, 배경 검정
                foreach (var r in ch.GetComponentsInChildren<Renderer>())
                {
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = white;
                    r.sharedMaterials = mats;
                }
                cam.backgroundColor = Color.black;
                cam.Render();
                mask = ReadRenderTexture(rt);

                // 읽은 이미지의 위아래가 그래픽 API에 따라 뒤집힐 수 있음 → 머리 뼈가 찍혀야 할 자리로 판정
                var head = FindHead(ch);
                if (head.HasValue)
                {
                    var vp = cam.WorldToViewportPoint(head.Value);
                    bool atExpected = IsWhiteAround(mask, vp.x, vp.y);
                    bool atFlipped = IsWhiteAround(mask, vp.x, 1f - vp.y);
                    flipped = !atExpected && atFlipped;
                    if (flipped)
                    {
                        FlipVertically(mask);
                        FlipVertically(color);
                        color.Apply();
                    }
                }
                mask.Apply();
            }
            finally
            {
                RenderTexture.active = prevActive;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                if (camGo != null) Object.DestroyImmediate(camGo);
                if (ch != null) Object.DestroyImmediate(ch);
                EditorSceneManager.ClosePreviewScene(scene);
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(white);
            }
            return mask;
        }

        static Vector3 lastFacing;
        static bool flipped;

        static Texture2D ReadRenderTexture(RenderTexture rt)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            return tex;
        }

        static Vector3? FindHead(GameObject ch)
        {
            var animator = ch.GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman) return null;
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            return head != null ? head.position : (Vector3?)null;
        }

        // 뷰포트 좌표(0~1, 아래가 0) 주변 7x7 픽셀 중 절반 이상이 흰색인지
        static bool IsWhiteAround(Texture2D tex, float vx, float vy)
        {
            int cx = Mathf.RoundToInt(vx * tex.width), cy = Mathf.RoundToInt(vy * tex.height), hits = 0, total = 0;
            for (int y = cy - 3; y <= cy + 3; y++)
            for (int x = cx - 3; x <= cx + 3; x++)
            {
                if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) continue;
                total++;
                if (tex.GetPixel(x, y).r > 0.5f) hits++;
            }
            return total > 0 && hits * 2 >= total;
        }

        static void FlipVertically(Texture2D tex)
        {
            var p = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            var row = new Color32[w];
            for (int y = 0; y < h / 2; y++)
            {
                System.Array.Copy(p, y * w, row, 0, w);
                System.Array.Copy(p, (h - 1 - y) * w, p, y * w, w);
                System.Array.Copy(row, 0, p, (h - 1 - y) * w, w);
            }
            tex.SetPixels32(p);
        }

        // 몸이 실제로 향하는 방향: 휴머노이드면 양 어깨(왼→오른쪽) 벡터로 계산, 아니면 루트 정면
        static Vector3 FindFacing(GameObject ch)
        {
            var animator = ch.GetComponentInChildren<Animator>();
            if (animator != null && animator.isHuman)
            {
                var l = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                var r = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                if (l != null && r != null)
                {
                    var right = Vector3.ProjectOnPlane(r.position - l.position, Vector3.up);
                    if (right.sqrMagnitude > 0.0001f) return Vector3.Cross(right.normalized, Vector3.up).normalized;
                }
            }
            var f = Vector3.ProjectOnPlane(ch.transform.forward, Vector3.up);
            return f.sqrMagnitude < 0.001f ? Vector3.forward : f.normalized;
        }

        // ---------- 2. 표지판 합성 ----------
        // 밝기 → 3단계 회색. 가장 어두운 단계도 배경(검정)과 구분되게 회색으로 둠
        static float Tone(Color c)
        {
            float lum = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            return lum < 0.3f ? 0.42f : lum < 0.6f ? 0.72f : 1f;
        }

        static Texture2D Compose(Texture2D mask, Texture2D color)
        {
            var px = new Color32[ImageW * ImageH];
            for (int i = 0; i < px.Length; i++) px[i] = Backdrop;

            int cx = ImageW / 2, cy = ImageH / 2;
            // 표지판: 흰 테두리만(안은 배경과 같은 검정)
            RoundRect(px, cx, cy, 640, 310, 44, White);
            RoundRect(px, cx, cy, 626, 296, 32, Backdrop);

            // 왼쪽 화살표(복도 서쪽 출구 방향)
            int tipX = cx - 560, baseX = cx - 395, shaftEnd = cx - 205;
            Triangle(px, new Vector2(tipX, cy), new Vector2(baseX, cy + 150), new Vector2(baseX, cy - 150), White);
            FillRect(px, baseX - 2, cy - 50, shaftEnd, cy + 50, White);

            // 실루엣: 마스크에서 캐릭터가 있는 영역만 잘라 오른쪽 칸에 맞춤
            if (!FindMaskBounds(mask, out var mb))
            {
                Debug.LogWarning("[LeaveImageRenderer] 실루엣이 찍히지 않음. 표지판과 화살표만 저장함");
            }
            else
            {
                float areaX0 = cx - 140, areaX1 = cx + 570, areaH = 500;
                float scale = Mathf.Min(areaH / mb.height, (areaX1 - areaX0) / mb.width);
                float dw = mb.width * scale, dh = mb.height * scale;
                float dx0 = (areaX0 + areaX1) / 2 - dw / 2, dy0 = cy - dh / 2;
                for (int y = Mathf.FloorToInt(dy0); y < dy0 + dh; y++)
                for (int x = Mathf.FloorToInt(dx0); x < dx0 + dw; x++)
                {
                    if (x < 0 || y < 0 || x >= ImageW || y >= ImageH) continue;
                    float mx = mb.xMin + (x - dx0) / scale, my = mb.yMin + (y - dy0) / scale;
                    float a = Mathf.Clamp01(mask.GetPixelBilinear(mx / MaskSize, my / MaskSize).r / 0.8f);
                    if (a <= 0f) continue;
                    byte v = (byte)(255 * Tone(color.GetPixelBilinear(mx / MaskSize, my / MaskSize)));
                    Blend(px, x, y, new Color32(v, v, v, 255), a);
                }
            }

            var tex = new Texture2D(ImageW, ImageH, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        static bool FindMaskBounds(Texture2D mask, out Rect rect)
        {
            var p = mask.GetPixels32();
            int x0 = MaskSize, y0 = MaskSize, x1 = -1, y1 = -1;
            for (int y = 0; y < MaskSize; y++)
            for (int x = 0; x < MaskSize; x++)
            {
                if (p[y * MaskSize + x].r < 25) continue;
                if (x < x0) x0 = x;
                if (x > x1) x1 = x;
                if (y < y0) y0 = y;
                if (y > y1) y1 = y;
            }
            rect = x1 < 0 ? Rect.zero : Rect.MinMaxRect(x0, y0, x1 + 1, y1 + 1);
            return x1 >= 0;
        }

        // ---------- 그리기 도우미(가장자리 1px 부드럽게) ----------
        static void RoundRect(Color32[] px, int cx, int cy, int halfW, int halfH, int radius, Color32 c)
        {
            for (int y = cy - halfH - 1; y <= cy + halfH + 1; y++)
            for (int x = cx - halfW - 1; x <= cx + halfW + 1; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - cx) - (halfW - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y - cy) - (halfH - radius), 0f);
                float edge = radius - Mathf.Sqrt(dx * dx + dy * dy);
                float ex = halfW - Mathf.Abs(x - cx), ey = halfH - Mathf.Abs(y - cy);
                float a = Mathf.Clamp01(Mathf.Min(edge, Mathf.Min(ex, ey)) + 0.5f);
                if (a > 0f) Blend(px, x, y, c, a);
            }
        }

        static void FillRect(Color32[] px, int x0, int y0, int x1, int y1, Color32 c)
        {
            for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                Blend(px, x, y, c, 1f);
        }

        static void Triangle(Color32[] px, Vector2 a, Vector2 b, Vector2 c, Color32 col)
        {
            int minX = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))) - 1, maxX = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))) + 1;
            int minY = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))) - 1, maxY = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))) + 1;
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                var p = new Vector2(x, y);
                float d = Mathf.Min(EdgeDist(p, a, b, c), Mathf.Min(EdgeDist(p, b, c, a), EdgeDist(p, c, a, b)));
                float alpha = Mathf.Clamp01(d + 0.5f);
                if (alpha > 0f) Blend(px, x, y, col, alpha);
            }
        }

        // 점 p가 변 ab의 안쪽(세 번째 꼭짓점 c 쪽)으로 떨어진 거리. 바깥이면 음수
        static float EdgeDist(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            var e = b - a;
            var n = new Vector2(-e.y, e.x).normalized;
            if (Vector2.Dot(c - a, n) < 0) n = -n;
            return Vector2.Dot(p - a, n);
        }

        static void Blend(Color32[] px, int x, int y, Color32 c, float a)
        {
            if (x < 0 || y < 0 || x >= ImageW || y >= ImageH) return;
            int i = y * ImageW + x;
            px[i] = Color32.Lerp(px[i], c, a);
        }

        static AnimationClip LoadClip(string path)
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            return null;
        }
    }
}
