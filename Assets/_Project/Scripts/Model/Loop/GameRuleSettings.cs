using UnityEngine;

namespace Anomaly.Model.Loop
{
    // 게임 규칙 숫자 모음.
    // 하드코딩 방지용. 여기서 찾아서 쓸 것
    [CreateAssetMenu(fileName = "GameRuleSettings", menuName = "Anomaly/Game Rule Settings")]
    public sealed class GameRuleSettings : ScriptableObject
    {
        [Tooltip("이상현상이 절대 나오지 않는 맵. 틀리면 이 맵으로 돌아감")]
        [SerializeField, Min(0)] private int safeMapNumber = 0;
        [Tooltip("도착하면 클리어인 맵 번호. 8이면 7번 맵에서 맞혔을 때 클리어")]
        [SerializeField, Min(1)] private int goalMapNumber = 8;
        [Tooltip("safe 맵이 아닌 맵(1~7번)에서 이상현상이 나올 확률")]
        [SerializeField, Range(0f, 1f)] private float anomalyChance = 0.5f;

        public int SafeMapNumber => safeMapNumber;
        public int GoalMapNumber => goalMapNumber;
        public float AnomalyChance => anomalyChance;

        private void OnValidate()
        {
            if (goalMapNumber <= safeMapNumber) goalMapNumber = safeMapNumber + 1;
        }
    }
}
