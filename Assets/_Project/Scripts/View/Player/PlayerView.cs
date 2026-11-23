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
        [Tooltip("비워 두면 발소리 없음")]
        [SerializeField] private FootstepView footsteps;

        // 높이 차이가 없는 맵이라 진짜 중력 대신, 매 프레임 바닥 쪽으로 살짝 눌러 붙여 둔다(m/s)
        private const float GroundStickSpeed = -2f;

        private Renderer[] renderers;
        private int speedHash;
        private bool hasSpeedParameter;

        public Vector3 Position => transform.position; // 상호작용 거리 체크용.

        private void Awake()
        {
            if (characterController == null) characterController = GetComponent<CharacterController>();
            hasSpeedParameter = animator != null && !string.IsNullOrEmpty(speedParameter);
            if (hasSpeedParameter) speedHash = Animator.StringToHash(speedParameter);
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        // 조사 클로즈업 때 캐릭터가 물건을 가리지 않게 숨김
        public void SetVisible(bool visible)
        {
            foreach (var r in renderers)
                if (r != null) r.enabled = visible;
        }

        // 움직임로직. rigidbody를 적용 안해놔서 혹시모를 고저차로 인해 올라가면 내려오질않음. 대비용으로 바닥에서 일정높이 이상 뜨면 아래로 떨구게 설정
        public void Move(Vector3 horizontalVelocity, float deltaTime)
        {
            var velocity = new Vector3(horizontalVelocity.x, GroundStickSpeed, horizontalVelocity.z);
            characterController.Move(velocity * deltaTime);

            float horizontalSpeed = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z).magnitude;
            if (hasSpeedParameter)
                animator.SetFloat(speedHash, horizontalSpeed);
            if (footsteps != null) footsteps.Advance(horizontalSpeed * deltaTime);
        }

        // 회전 로직
        public void RotateTowards(Vector3 direction, float degreesPerSecond, float deltaTime)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            var target = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degreesPerSecond * deltaTime);
        }

        // 맵 전환·나가지 않기 때 시작 지점으로 순간이동. CharacterController가 켜져 있으면 위치를 덮어써서 꺼두고 옮김
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            characterController.enabled = true;
            if (hasSpeedParameter) animator.SetFloat(speedHash, 0f);
        }

        private void Reset()
        {
            characterController = GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();
        }
    }
}