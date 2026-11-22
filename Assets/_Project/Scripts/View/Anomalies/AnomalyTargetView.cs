using UnityEngine;

namespace Anomaly.View.Anomalies
{
    // 씬에서 이상현상을 실제로 보여주는 부분. anomalyId가 AnomalyData.Id와 같으면 연결됨.
    // 새 종류(직접 제작 등)는 이 클래스를 상속해서 OnApply/OnRevert만 구현
    public abstract class AnomalyTargetView : MonoBehaviour
    {
        [SerializeField] private string anomalyId = "";

        public string AnomalyId => anomalyId;
        public bool IsApplied { get; private set; }

        public void Apply()
        {
            if (IsApplied) return;
            OnApply();
            IsApplied = true;
        }

        public void Revert()
        {
            if (!IsApplied) return;
            OnRevert();
            IsApplied = false;
        }

        protected abstract void OnApply();
        protected abstract void OnRevert();
    }
}
