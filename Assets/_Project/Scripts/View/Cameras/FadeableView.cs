using UnityEngine;

namespace Anomaly.View.Cameras
{
    // 카메라와 플레이어 사이를 가리면 투명해지는 벽. WallFadeView가 SetFaded를 부름.
    // 평소엔 불투명 머티리얼, 투명해지는 동안만 투명 머티리얼로 바꿈(평소 렌더링 비용 그대로)
    [DisallowMultipleComponent]
    public sealed class FadeableView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer targetRenderer;
        [Tooltip("Surface Type이 Transparent인 URP 머티리얼")]
        [SerializeField] private Material fadeMaterial;
        [SerializeField, Range(0f, 1f)] private float fadedAlpha = 0.25f;
        [SerializeField, Min(0.01f)] private float fadeSpeed = 6f;

        private Material opaqueMaterial;
        private MaterialPropertyBlock block;
        private Color baseColor = Color.white;
        private float alpha = 1f;
        private bool faded;

        private void Awake()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            if (targetRenderer != null) opaqueMaterial = targetRenderer.sharedMaterial;
            if (fadeMaterial != null && fadeMaterial.HasProperty(BaseColorId)) baseColor = fadeMaterial.GetColor(BaseColorId);
            block = new MaterialPropertyBlock();
            enabled = false; // 바뀌는 동안만 Update
        }

        public void SetFaded(bool value)
        {
            if (faded == value) return;
            faded = value;
            if (targetRenderer != null && fadeMaterial != null) enabled = true;
        }

        private void Update()
        {
            float goal = faded ? fadedAlpha : 1f;
            alpha = Mathf.MoveTowards(alpha, goal, fadeSpeed * Time.deltaTime);

            if (alpha >= 1f)
            {
                targetRenderer.sharedMaterial = opaqueMaterial;
                targetRenderer.SetPropertyBlock(null);
                enabled = false;
                return;
            }

            if (targetRenderer.sharedMaterial != fadeMaterial) targetRenderer.sharedMaterial = fadeMaterial;
            var c = baseColor;
            c.a = alpha;
            block.SetColor(BaseColorId, c);
            targetRenderer.SetPropertyBlock(block);

            if (Mathf.Approximately(alpha, goal)) enabled = false;
        }

        private void Reset()
        {
            targetRenderer = GetComponent<Renderer>();
        }
    }
}
