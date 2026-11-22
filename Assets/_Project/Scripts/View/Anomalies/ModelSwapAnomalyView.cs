using UnityEngine;

namespace Anomaly.View.Anomalies
{
    // 모델 교체 이상현상. 교체 모델을 미리 같은 자리에 꺼둔 채로 두고 켜고 끄기만 함
    public sealed class ModelSwapAnomalyView : AnomalyTargetView
    {
        [SerializeField] private GameObject original;
        [SerializeField] private GameObject replacement;

        private void Awake()
        {
            if (replacement != null && !IsApplied) replacement.SetActive(false);
        }

        protected override void OnApply()
        {
            if (original != null) original.SetActive(false);
            if (replacement != null) replacement.SetActive(true);
        }

        protected override void OnRevert()
        {
            if (replacement != null) replacement.SetActive(false);
            if (original != null) original.SetActive(true);
        }
    }
}
