using UnityEngine;
using UnityEngine.UI;

namespace Anomaly.View.UI
{
    // 조사 화면 하단 패널: 물건 이름, 설명, 닫기 안내
    public sealed class InspectPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text hintText;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        public void Show(string title, string description, string hint)
        {
            if (titleText != null) titleText.text = title;
            if (descriptionText != null)
            {
                descriptionText.text = description;
                descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(description));
            }
            if (hintText != null) hintText.text = hint;
            if (panel != null) panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
