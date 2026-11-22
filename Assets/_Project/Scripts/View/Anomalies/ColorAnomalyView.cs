using System;
using UnityEngine;

namespace Anomaly.View.Anomalies
{
    // 색상 변경 이상현상. 머티리얼 원본은 건드리지 않고 MaterialPropertyBlock으로 _BaseColor만 덮어씀
    // (KayKit 아틀라스는 _BaseColor가 흰색이라 텍스처에 이 색이 곱해짐)
    public sealed class ColorAnomalyView : AnomalyTargetView
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer[] renderers = Array.Empty<Renderer>();
        [SerializeField] private Color color = new Color(0.8f, 0.3f, 0.3f);

        private MaterialPropertyBlock block;

        protected override void OnApply()
        {
            block ??= new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                r.SetPropertyBlock(block);
            }
        }

        protected override void OnRevert()
        {
            foreach (var r in renderers)
                if (r != null) r.SetPropertyBlock(null);
        }
    }
}
