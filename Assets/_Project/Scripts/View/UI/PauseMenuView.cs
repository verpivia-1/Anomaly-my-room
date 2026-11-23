using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Anomaly.View.UI
{
    // 일시정지 메뉴(계속하기 / 로비로 / 종료)
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button lobbyButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private EventSystem eventSystem;

        public event Action ResumeClicked;
        public event Action LobbyClicked;
        public event Action QuitClicked;

        private void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
            if (lobbyButton != null) lobbyButton.onClick.AddListener(OnLobby);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
            if (panel != null) panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveListener(OnResume);
            if (lobbyButton != null) lobbyButton.onClick.RemoveListener(OnLobby);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuit);
        }

        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            if (eventSystem != null && resumeButton != null) eventSystem.SetSelectedGameObject(resumeButton.gameObject);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            if (eventSystem != null) eventSystem.SetSelectedGameObject(null);
        }

        private void OnResume() => ResumeClicked?.Invoke();
        private void OnLobby() => LobbyClicked?.Invoke();
        private void OnQuit() => QuitClicked?.Invoke();
    }
}
