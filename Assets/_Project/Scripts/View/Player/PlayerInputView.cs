using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Anomaly.View.Player
{
    /// <summary>Input System의 Move 액션을 읽어 이벤트로 알린다. 판단은 하지 않는다.</summary>
    public sealed class PlayerInputView : MonoBehaviour
    {
        [SerializeField] private InputActionReference moveAction;

        public Vector2 MoveInput { get; private set; }

        public event Action<Vector2> MoveInputChanged;
        
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
        }

        private void OnDisable()
        {
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

        private void SetMoveInput(Vector2 value)
        {
            MoveInput = value;
            MoveInputChanged?.Invoke(value);
        }
    }
}