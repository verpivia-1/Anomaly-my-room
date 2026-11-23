using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Anomaly.View.UI
{
    // 로비 메뉴(게임 시작 / 종료). 누르면 이벤트로 알리기만 함
    public sealed class LobbyMenuView : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        [Tooltip("키보드로 고를 수 있게 기본 선택을 지정할 때 사용")]
        [SerializeField] private EventSystem eventSystem;

        public event Action StartClicked;
        public event Action QuitClicked;

        private void Awake()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStart);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        }

        private void OnDestroy()
        {
            if (startButton != null) startButton.onClick.RemoveListener(OnStart);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuit);
        }

        public void SetInteractable(bool interactable)
        {
            if (startButton != null) startButton.interactable = interactable;
            if (quitButton != null) quitButton.interactable = interactable;
        }

        public void FocusStart()
        {
            if (eventSystem != null && startButton != null) eventSystem.SetSelectedGameObject(startButton.gameObject);
        }

        private void OnStart() => StartClicked?.Invoke();
        private void OnQuit() => QuitClicked?.Invoke();
    }
}
