using UnityEngine;
using UnityEngine.UI;

namespace Anomaly.View.UI
{
    // 상호작용 안내 문구(예: "E  잠들기"). 언제 띄울지와 문구는 Presenter가 정함
    public sealed class InteractPromptView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text label;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        public void Show(string text)
        {
            if (label != null) label.text = text;
            if (panel != null && !panel.activeSelf) panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null && panel.activeSelf) panel.SetActive(false);
        }
    }
}
