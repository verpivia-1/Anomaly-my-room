using UnityEngine;

namespace Anomaly.View.Player
{
    // 캐릭터 이동·회전·애니메이션 처리 스크립트
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Animator animator;
        [Tooltip("이동 속도를 넘길 Animator float 파라미터 이름. 사용하지 않는 다면 비워둘 것. 그럼 자동으로 사용안함")]
        [SerializeField] private string speedParameter = "";

        // 높이 차이가 없는 맵이라 진짜 중력 대신, 매 프레임 바닥 쪽으로 살짝 눌러 붙여 둔다(m/s)
        private const float GroundStickSpeed = -2f;

        private int speedHash;
        private bool hasSpeedParameter;

        public Vector3 Position => transform.position; // 상호작용 거리 체크용.

        private void Awake()
        {
            if (characterController == null) characterController = GetComponent<CharacterController>();
            hasSpeedParameter = animator != null && !string.IsNullOrEmpty(speedParameter);
            if (hasSpeedParameter) speedHash = Animator.StringToHash(speedParameter);
        }

        // 움직임로직. rigidbody를 적용 안해놔서 혹시모를 고저차로 인해 올라가면 내려오질않음. 대비용으로 바닥에서 일정높이 이상 뜨면 아래로 떨구게 설정
        public void Move(Vector3 horizontalVelocity, float deltaTime)
        {
            var velocity = new Vector3(horizontalVelocity.x, GroundStickSpeed, horizontalVelocity.z);
            characterController.Move(velocity * deltaTime);

            if (hasSpeedParameter)
                animator.SetFloat(speedHash, new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z).magnitude);
        }

        // 회전 로직
        public void RotateTowards(Vector3 direction, float degreesPerSecond, float deltaTime)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            var target = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degreesPerSecond * deltaTime);
        }

        private void Reset()
        {
            characterController = GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();
        }
    }
}