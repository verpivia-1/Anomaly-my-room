using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Anomaly.View.UI
{
    // 확인 팝업(잠들기/잠들지 않기, 나가기/나가지 않기). 문구는 Presenter가 정해서 넘김
    public sealed class ConfirmPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Text confirmLabel;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Text cancelLabel;
        [Tooltip("키보드로 고를 수 있게 기본 선택을 지정할 때 사용. 비워도 마우스로는 동작")]
        [SerializeField] private EventSystem eventSystem;

        public bool IsOpen => panel != null && panel.activeSelf;

        public event Action Confirmed;
        public event Action Canceled;

        private void Awake()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
            if (panel != null) panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirm);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancel);
        }

        public void Show(string message, string confirmText, string cancelText)
        {
            if (messageText != null) messageText.text = message;
            if (confirmLabel != null) confirmLabel.text = confirmText;
            if (cancelLabel != null) cancelLabel.text = cancelText;
            if (panel != null) panel.SetActive(true);
            // 실수로 결정되지 않게 기본 선택은 취소 쪽
            if (eventSystem != null && cancelButton != null) eventSystem.SetSelectedGameObject(cancelButton.gameObject);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            if (eventSystem != null) eventSystem.SetSelectedGameObject(null);
        }

        private void OnConfirm()
        {
            if (IsOpen) Confirmed?.Invoke();
        }

        private void OnCancel()
        {
            if (IsOpen) Canceled?.Invoke();
        }
    }
}
