using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Anomaly.View.UI
{
    // 클리어 화면. 로비로 돌아가는 버튼이 있음
    public sealed class EndingView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button lobbyButton;
        [SerializeField] private EventSystem eventSystem;

        public event Action LobbyClicked;

        private void Awake()
        {
            if (lobbyButton != null) lobbyButton.onClick.AddListener(OnLobby);
            if (panel != null) panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (lobbyButton != null) lobbyButton.onClick.RemoveListener(OnLobby);
        }

        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            if (eventSystem != null && lobbyButton != null) eventSystem.SetSelectedGameObject(lobbyButton.gameObject);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        public void SetInteractable(bool interactable)
        {
            if (lobbyButton != null) lobbyButton.interactable = interactable;
        }

        private void OnLobby() => LobbyClicked?.Invoke();
    }
}
