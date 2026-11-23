using Anomaly.Presenter.Lobby;
using Anomaly.View.UI;
using UnityEngine;

namespace Anomaly.Presenter.Manager
{
    // 로비 씬 진입점. 로비에 필요한 것만 생성·연결
    public sealed class LobbyManager : MonoBehaviour
    {
        [SerializeField] private LobbyMenuView menuView;
        [SerializeField] private ScreenFadeView screenFade;
        [Tooltip("게임 시작 시 불러올 씬 이름(Build Profiles의 씬 목록에 있어야 함)")]
        [SerializeField] private string gameSceneName = "Level_Corridor";

        private LobbyPresenter lobbyPresenter;

        private void Awake()
        {
            if (menuView == null || string.IsNullOrEmpty(gameSceneName))
            {
                Debug.LogError($"[{nameof(LobbyManager)}] 인스펙터 참조가 비어 있습니다.", this);
                enabled = false;
                return;
            }
            lobbyPresenter = new LobbyPresenter(menuView, screenFade, gameSceneName);
        }

        // ScreenFadeView.Awake가 끝난 뒤 시작
        private void Start()
        {
            lobbyPresenter?.Begin();
        }

        private void OnDestroy()
        {
            lobbyPresenter?.Dispose();
            lobbyPresenter = null;
        }
    }
}
