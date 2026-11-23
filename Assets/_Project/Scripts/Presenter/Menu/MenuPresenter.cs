using System;
using Anomaly.Model.Loop;
using Anomaly.View.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Anomaly.Presenter.Menu
{
    // 일시정지 메뉴(계속 / 로비로 / 종료)와 엔딩 화면의 로비 버튼. 로비로 갈 때는 암전 후 씬 이동
    public sealed class MenuPresenter : IDisposable
    {
        private readonly LoopModel loop;
        private readonly PauseMenuView pauseMenu;
        private readonly EndingView ending;
        private readonly ScreenFadeView fade;
        private readonly string lobbySceneName;
        private bool leaving;

        public MenuPresenter(LoopModel loop, PauseMenuView pauseMenu, EndingView ending, ScreenFadeView fade, string lobbySceneName)
        {
            this.loop = loop;
            this.pauseMenu = pauseMenu;
            this.ending = ending;
            this.fade = fade;
            this.lobbySceneName = lobbySceneName;

            if (pauseMenu != null)
            {
                pauseMenu.ResumeClicked += Resume;
                pauseMenu.LobbyClicked += GoToLobby;
                pauseMenu.QuitClicked += Quit;
            }
            if (ending != null) ending.LobbyClicked += GoToLobby;
        }

        public void Dispose()
        {
            if (pauseMenu != null)
            {
                pauseMenu.ResumeClicked -= Resume;
                pauseMenu.LobbyClicked -= GoToLobby;
                pauseMenu.QuitClicked -= Quit;
            }
            if (ending != null) ending.LobbyClicked -= GoToLobby;
            Time.timeScale = 1f; // 씬을 떠날 때 정지 상태가 남지 않게
        }

        public void Pause()
        {
            if (loop.Phase != LoopPhase.Playing || pauseMenu == null) return;
            loop.SetPhase(LoopPhase.Paused);
            Time.timeScale = 0f;
            pauseMenu.Show();
        }

        public void Resume()
        {
            if (loop.Phase != LoopPhase.Paused || leaving) return;
            Time.timeScale = 1f;
            pauseMenu.Hide();
            loop.SetPhase(LoopPhase.Playing);
        }

        private void GoToLobby()
        {
            if (leaving || string.IsNullOrEmpty(lobbySceneName)) return;
            leaving = true;
            Time.timeScale = 1f;
            if (ending != null) ending.SetInteractable(false);
            if (fade != null) fade.FadeOut(ScreenFadeKind.Plain, LoadLobby);
            else LoadLobby();
        }

        private void LoadLobby()
        {
            SceneManager.LoadScene(lobbySceneName);
        }

        private void Quit()
        {
#if UNITY_EDITOR
            Debug.Log("[MenuPresenter] 종료(에디터에서는 플레이 모드가 유지됨)");
#endif
            Application.Quit();
        }
    }
}
