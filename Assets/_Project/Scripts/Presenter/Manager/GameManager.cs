using Anomaly.Model.Anomalies;
using Anomaly.Model.Loop;
using Anomaly.Model.Player;
using Anomaly.Presenter.Anomalies;
using Anomaly.Presenter.Inspect;
using Anomaly.Presenter.Loop;
using Anomaly.Presenter.Menu;
using Anomaly.Presenter.Player;
using Anomaly.View.Anomalies;
using Anomaly.View.Cameras;
using Anomaly.View.Interaction;
using Anomaly.View.Player;
using Anomaly.View.UI;
using UnityEngine;

namespace Anomaly.Presenter.Manager
{   // 게임매니저 스크립트. awake 타임서 필요한거 여기서 처리
    public sealed class GameManager : MonoBehaviour
    {
        [Header("플레이어")]
        [SerializeField] private PlayerMovementSettings movementSettings;
        [SerializeField] private PlayerInputView playerInputView;
        [SerializeField] private PlayerView playerView;
        [SerializeField] private TopDownCameraView cameraView;
        [SerializeField] private WallFadeView wallFadeView;

        [Header("규칙·이상현상")]
        [SerializeField] private GameRuleSettings ruleSettings;
        [SerializeField] private AnomalyCatalog anomalyCatalog;
        [SerializeField] private AnomalyTargetView[] anomalyTargets = new AnomalyTargetView[0];

        [Header("맵")]
        [SerializeField] private Transform startPoint;
        [SerializeField] private InteractZoneView bedZone;
        [SerializeField] private InteractZoneView exitZone;
        [SerializeField] private MapNumberSignView mapSign;
        [SerializeField] private InspectableView[] inspectables = new InspectableView[0];

        [Header("UI")]
        [SerializeField] private ConfirmPopupView confirmPopup;
        [SerializeField] private EndingView endingView;
        [SerializeField] private ScreenFadeView screenFade;
        [SerializeField] private InteractPromptView interactPrompt;
        [SerializeField] private MapFeedbackView mapFeedback;
        [SerializeField] private InspectPanelView inspectPanel;
        [SerializeField] private PauseMenuView pauseMenu;

        [Header("씬")]
        [Tooltip("일시정지 메뉴·엔딩에서 돌아갈 로비 씬 이름")]
        [SerializeField] private string lobbySceneName = "Lobby";

        private PlayerModel playerModel;
        private LoopModel loopModel;
        private PlayerMovementPresenter playerMovement;
        private LoopPresenter loopPresenter;
        private DecisionPresenter decisionPresenter;
        private MenuPresenter menuPresenter;
        private InteractionPresenter interactionPresenter;

        public PlayerModel PlayerModel => playerModel;
        public LoopModel LoopModel => loopModel;

        // 인스펙터 연결 체크용. 에러가 뜬다면 확인해볼것
        private void Awake()
        {
            if (movementSettings == null || playerInputView == null || playerView == null ||
                ruleSettings == null || anomalyCatalog == null || startPoint == null ||
                bedZone == null || exitZone == null || confirmPopup == null)
            {
                Debug.LogError($"[{nameof(GameManager)}] 인스펙터 참조가 비어 있습니다.", this);
                enabled = false;
                return;
            }

            var random = new System.Random();

            playerModel = new PlayerModel();
            loopModel = new LoopModel();
            var pool = new AnomalyPool(anomalyCatalog.Anomalies, random);
            var anomalies = new AnomalyPresenter(anomalyTargets, anomalyCatalog.Anomalies);

            playerMovement = new PlayerMovementPresenter(playerModel, movementSettings, playerInputView, playerView);
            loopPresenter = new LoopPresenter(loopModel, ruleSettings, pool, random, anomalies,
                playerView, cameraView, startPoint, mapSign, endingView, screenFade, mapFeedback);
            decisionPresenter = new DecisionPresenter(loopModel, playerModel, loopPresenter, exitZone, confirmPopup);
            var inspectPresenter = new InspectPresenter(loopModel, cameraView, playerView, playerInputView, inspectPanel);
            menuPresenter = new MenuPresenter(loopModel, pauseMenu, endingView, screenFade, lobbySceneName);
            interactionPresenter = new InteractionPresenter(loopModel, playerInputView, playerView, bedZone, inspectables,
                interactPrompt, decisionPresenter, inspectPresenter, menuPresenter);

            bedZone.SetTarget(playerView.transform);
            exitZone.SetTarget(playerView.transform);
            if (cameraView != null) cameraView.SetTarget(playerView.transform);
            if (wallFadeView != null) wallFadeView.SetTarget(playerView.transform);
        }

        // 다른 View들의 Awake(팝업 숨기기 등)가 끝난 뒤 0번 맵 시작
        private void Start()
        {
            loopPresenter?.StartLoop();
        }

        private void Update()
        {
            playerMovement?.Tick(Time.deltaTime);
            interactionPresenter?.Tick();
        }

        private void OnDestroy()
        {
            playerMovement?.Dispose();
            playerMovement = null;
            decisionPresenter?.Dispose();
            decisionPresenter = null;
            interactionPresenter?.Dispose();
            interactionPresenter = null;
            menuPresenter?.Dispose();
            menuPresenter = null;
        }
    }
}
