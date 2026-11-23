using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Anomaly.View.Player
{
    /// <summary>Input System의 Move 액션을 읽어 이벤트로 알린다. 판단은 하지 않는다.</summary>
    public sealed class PlayerInputView : MonoBehaviour
    {
        [SerializeField] private InputActionReference moveAction;
        [Tooltip("Player/Interact. 침대 앞에서 누르면 잠들기 팝업")]
        [SerializeField] private InputActionReference interactAction;

        public Vector2 MoveInput { get; private set; }

        public event Action<Vector2> MoveInputChanged;
        public event Action InteractPressed;

        [Tooltip("UI/Cancel(Esc, 게임패드 B). 팝업 취소·조사 닫기·일시정지")]
        [SerializeField] private InputActionReference cancelAction;
        public event Action CancelPressed;

        // 안내 문구에 쓸 상호작용 키 이름(첫 번째 바인딩, Hold 같은 표시는 뺌). 연결이 없으면 "E"
        public string InteractKeyName =>
            interactAction != null && interactAction.action != null && interactAction.action.bindings.Count > 0
                ? interactAction.action.GetBindingDisplayString(0, InputBinding.DisplayStringOptions.DontIncludeInteractions)
                : "E";
        
        private void OnEnable()
        {
            // inputsystem 인스펙터에 null이면 발생. 확인 후 넣을것. inputsystem 설치 안했으면 제발 해 제발 기존꺼 쓰지마
            // 설정서 입력식 both 설정 하지마 그냥 이거 써.
            // 몰래 기존 입력식 코드 넣어서 꼬이지 말고 그냥 inputsystem쓰라고 
            // 헷갈리면 Player/Move 이거 그냥 넣어

            if (moveAction == null || moveAction.action == null)
            {
                Debug.LogError($"[{nameof(PlayerInputView)}] moveAction이 연결되지 않았습니다.", this);
                return;
            }
            var action = moveAction.action;
            action.performed += OnMove;
            action.canceled += OnMove;
            action.Enable();

            // Interact 액션에는 Hold가 걸려 있어서 performed는 길게 눌러야 옴. 누르는 순간(started)을 씀
            if (interactAction == null || interactAction.action == null)
            {
                Debug.LogError($"[{nameof(PlayerInputView)}] interactAction이 연결되지 않았습니다.", this);
                return;
            }
            interactAction.action.started += OnInteract;
            interactAction.action.Enable();

            if (cancelAction != null && cancelAction.action != null)
            {
                cancelAction.action.performed += OnCancel;
                cancelAction.action.Enable();
            }
            else Debug.LogError($"[{nameof(PlayerInputView)}] cancelAction이 연결되지 않았습니다.", this);
        }

        private void OnDisable()
        {
            if (interactAction != null && interactAction.action != null)
            {
                interactAction.action.started -= OnInteract;
                interactAction.action.Disable();
            }
            // UI 입력 모듈도 같은 Cancel 액션을 쓰므로 Disable하지 않고 구독만 해제
            if (cancelAction != null && cancelAction.action != null)
                cancelAction.action.performed -= OnCancel;

            if (moveAction == null || moveAction.action == null) return;
            var action = moveAction.action;
            action.performed -= OnMove;
            action.canceled -= OnMove;
            action.Disable();
            SetMoveInput(Vector2.zero);
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            SetMoveInput(context.ReadValue<Vector2>());
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            InteractPressed?.Invoke();
        }

        private void OnCancel(InputAction.CallbackContext context)
        {
            CancelPressed?.Invoke();
        }

        private void SetMoveInput(Vector2 value)
        {
            MoveInput = value;
            MoveInputChanged?.Invoke(value);
        }
    }
}