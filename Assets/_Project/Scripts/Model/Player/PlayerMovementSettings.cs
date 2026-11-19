using UnityEngine;

namespace Anomaly.Model.Player
{
    // Assets/_Project/Data/Player/PlayerMovementSettings.asset
    // json써도 무방한데 이번엔 이거쓰기로 했으니 이거 쓰세요

    [CreateAssetMenu(fileName = "PlayerMovementSettings", menuName = "Anomaly/Player Movement Settings")]
    public sealed class PlayerMovementSettings : ScriptableObject
    {
        [SerializeField, Min(0f)] private float moveSpeed = 2.5f;      // m/s
        [SerializeField, Min(0f)] private float turnSpeed = 720f;      // 초당 회전 각도

        public float MoveSpeed => moveSpeed;
        public float TurnSpeed => turnSpeed;
    }
}
