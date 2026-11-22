using System.Collections.Generic;
using UnityEngine;

namespace Anomaly.Model.Anomalies
{
    // 이상현상 원본 목록. 게임 중에는 이 목록을 복사한 AnomalyPool을 쓰고, 원본은 건드리지 않음
    [CreateAssetMenu(fileName = "AnomalyCatalog", menuName = "Anomaly/Anomaly Catalog")]
    public sealed class AnomalyCatalog : ScriptableObject
    {
        [SerializeField] private List<AnomalyData> anomalies = new List<AnomalyData>();

        public IReadOnlyList<AnomalyData> Anomalies => anomalies;
    }
}
