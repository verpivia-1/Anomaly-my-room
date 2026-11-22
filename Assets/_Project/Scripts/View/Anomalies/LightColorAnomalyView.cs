using System;
using UnityEngine;

namespace Anomaly.View.Anomalies
{
    // 조명 색 변경 이상현상
    public sealed class LightColorAnomalyView : AnomalyTargetView
    {
        [SerializeField] private Light[] lights = Array.Empty<Light>();
        [SerializeField] private Color color = new Color(1f, 0.3f, 0.25f);

        private Color[] originalColors = Array.Empty<Color>();

        protected override void OnApply()
        {
            originalColors = new Color[lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null) continue;
                originalColors[i] = lights[i].color;
                lights[i].color = color;
            }
        }

        protected override void OnRevert()
        {
            for (int i = 0; i < lights.Length && i < originalColors.Length; i++)
                if (lights[i] != null) lights[i].color = originalColors[i];
        }
    }
}
