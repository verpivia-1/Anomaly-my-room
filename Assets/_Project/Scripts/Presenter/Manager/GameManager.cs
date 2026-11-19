using Anomaly.Model.Player;
using Anomaly.Presenter.Player;
using Anomaly.View.Cameras;
using Anomaly.View.Player;
using UnityEngine;

namespace Anomaly.Presenter.Manager
{   // 게임매니저 스크립트. awake 타임서 필요한거 여기서 처리
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private PlayerMovementSettings movementSettings;
        [SerializeField] private PlayerInputView playerInputView;
        [SerializeField] private PlayerView playerView;
        [SerializeField] private TopDownCameraView cameraView;

        private PlayerModel playerModel;
        private PlayerMovementPresenter playerMovement;

        public PlayerModel PlayerModel => playerModel;

        // 인스펙터 연결 체크용. 에러가 뜬다면 확인해볼것
        private void Awake()
        {
            if (movementSettings == null || playerInputView == null || playerView == null)
            {
                Debug.LogError($"[{nameof(GameManager)}] 인스펙터 참조가 비어 있습니다.", this);
                enabled = false;
                return;
            }

            playerModel = new PlayerModel();
            playerMovement = new PlayerMovementPresenter(playerModel, movementSettings, playerInputView, playerView);

            if (cameraView != null) cameraView.SetTarget(playerView.transform);
        }

        private void Update()
        {
            playerMovement?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            playerMovement?.Dispose();
            playerMovement = null;
        }
    }
}
