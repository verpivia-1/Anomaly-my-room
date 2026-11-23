using System;
using Anomaly.View.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Anomaly.Presenter.Lobby
{
    // 로비: 화면이 밝아지면 메뉴 사용 가능 → 게임 시작을 누르면 암전 후 게임 씬(0번 맵 복도) 로드
    public sealed class LobbyPresenter : IDisposable
    {
        private readonly LobbyMenuView menu;
        private readonly ScreenFadeView fade;
        private readonly string gameSceneName;
        private bool starting;

        public LobbyPresenter(LobbyMenuView menu, ScreenFadeView fade, string gameSceneName)
        {
            this.menu = menu;
            this.fade = fade;
            this.gameSceneName = gameSceneName;
            menu.StartClicked += OnStartClicked;
            menu.QuitClicked += OnQuitClicked;
        }

        public void Dispose()
        {
            menu.StartClicked -= OnStartClicked;
            menu.QuitClicked -= OnQuitClicked;
        }

        public void Begin()
        {
            menu.SetInteractable(false);
            if (fade == null)
            {
                EnableMenu();
                return;
            }
            fade.SetCovered(ScreenFadeKind.Plain);
            fade.FadeIn(EnableMenu);
        }

        private void EnableMenu()
        {
            menu.SetInteractable(true);
            menu.FocusStart();
        }

        private void OnStartClicked()
        {
            if (starting) return; // 연타 방지
            starting = true;
            menu.SetInteractable(false);
            if (fade != null) fade.FadeOut(ScreenFadeKind.Plain, LoadGame);
            else LoadGame();
        }

        private void LoadGame()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            Debug.Log("[LobbyPresenter] 종료(에디터에서는 플레이 모드가 유지됨)");
#endif
            Application.Quit();
        }
    }
}
