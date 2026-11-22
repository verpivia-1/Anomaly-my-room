using System.Collections.Generic;
using Anomaly.Model.Anomalies;
using Anomaly.View.Anomalies;
using UnityEngine;

namespace Anomaly.Presenter.Anomalies
{
    // AnomalyData(ID) ↔ 씬의 AnomalyTargetView를 이어서 적용/원래대로 되돌리기를 시킴
    public sealed class AnomalyPresenter
    {
        private readonly Dictionary<string, AnomalyTargetView> targets = new Dictionary<string, AnomalyTargetView>();

        public AnomalyPresenter(IEnumerable<AnomalyTargetView> views, IEnumerable<AnomalyData> catalog)
        {
            foreach (var view in views)
            {
                if (view == null) continue;
                if (string.IsNullOrEmpty(view.AnomalyId))
                {
                    Debug.LogWarning($"[{nameof(AnomalyPresenter)}] anomalyId가 비어 있음: {view.name}", view);
                    continue;
                }
                if (!targets.TryAdd(view.AnomalyId, view))
                    Debug.LogWarning($"[{nameof(AnomalyPresenter)}] 같은 anomalyId가 두 번 있음: {view.AnomalyId}", view);
            }

            foreach (var data in catalog)
                if (data != null && !targets.ContainsKey(data.Id))
                    Debug.LogWarning($"[{nameof(AnomalyPresenter)}] 씬에 연결된 대상이 없는 이상현상: {data.Id} ({data.name})", data);
        }

        public void Apply(AnomalyData anomaly)
        {
            if (anomaly == null) return;
            if (targets.TryGetValue(anomaly.Id, out var view)) view.Apply();
            else Debug.LogWarning($"[{nameof(AnomalyPresenter)}] 적용할 대상 없음: {anomaly.Id}");
        }

        public void Revert(AnomalyData anomaly)
        {
            if (anomaly == null) return;
            if (targets.TryGetValue(anomaly.Id, out var view)) view.Revert();
        }

        public void RevertAll()
        {
            foreach (var view in targets.Values) view.Revert();
        }
    }
}
