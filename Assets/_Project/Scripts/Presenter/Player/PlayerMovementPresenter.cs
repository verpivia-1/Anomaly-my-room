using System;
using Anomaly.Model.Player;
using Anomaly.View.Player;
using UnityEngine;

namespace Anomaly.Presenter.Player
{
    public sealed class PlayerMovementPresenter : IDisposable
    {
        private readonly PlayerModel model;
        private readonly PlayerMovementSettings settings;
        private readonly PlayerInputView input;
        private readonly PlayerView view;

        private Vector2 moveInput;

        public PlayerMovementPresenter(PlayerModel model, PlayerMovementSettings settings, PlayerInputView input, PlayerView view)
        {
            this.model = model;
            this.settings = settings;
            this.input = input;
            this.view = view;

            moveInput = input.MoveInput;
            input.MoveInputChanged += OnMoveInputChanged;
        }

        public void Tick(float deltaTime)
        {
            // 탑뷰 카메라가 남쪽에서 북쪽을 고정으로 보므로 입력 위 = 월드 +Z, 오른쪽 = 월드 +X
            var direction = model.CanMove ? new Vector3(moveInput.x, 0f, moveInput.y) : Vector3.zero;
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            view.Move(direction * settings.MoveSpeed, deltaTime);
            view.RotateTowards(direction, settings.TurnSpeed, deltaTime);
        }

        public void Dispose()
        {
            input.MoveInputChanged -= OnMoveInputChanged;
        }

        private void OnMoveInputChanged(Vector2 value)
        {
            moveInput = value;
        }
    }
}
