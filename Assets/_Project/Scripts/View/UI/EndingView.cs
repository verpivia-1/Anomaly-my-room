using UnityEngine;

namespace Anomaly.View.UI
{
    // 클리어 화면. 지금은 패널 표시만 함
    public sealed class EndingView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        public void Show()
        {
            if (panel != null) panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
