using UnityEngine;

namespace Anomaly.Model.Anomalies
{
    // 이상현상 하나 = 이 에셋 하나. 실제로 씬에서 무엇을 바꿀지는 같은 ID를 가진 AnomalyTargetView가 담당
    [CreateAssetMenu(fileName = "ANM_", menuName = "Anomaly/Anomaly Data")]
    public sealed class AnomalyData : ScriptableObject
    {
        [Tooltip("씬의 AnomalyTargetView.anomalyId와 같아야 연결됨")]
        [SerializeField] private string id = "";
        [SerializeField] private AnomalyDifficulty difficulty = AnomalyDifficulty.Normal;
        [SerializeField] private AnomalyRoom room = AnomalyRoom.Corridor;
        [SerializeField] private AnomalyKind kind = AnomalyKind.ColorChange;
        [SerializeField, TextArea] private string description = "";

        public string Id => id;
        public AnomalyDifficulty Difficulty => difficulty;
        public AnomalyRoom Room => room;
        public AnomalyKind Kind => kind;
        public string Description => description;
    }
}
