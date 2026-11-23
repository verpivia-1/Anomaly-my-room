using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Anomaly.View.UI
{
    // 인트로 스토리. 검은 화면에 문장을 한 줄씩 띄움. 화면 클릭 / Enter / Space로 다음, 건너뛰기 버튼
    public sealed class StoryView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text lineText;
        [Tooltip("화면 전체를 덮는 투명 버튼. 누르면 다음 줄")]
        [SerializeField] private Button advanceButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private EventSystem eventSystem;
        [SerializeField, Min(0f)] private float lineFadeDuration = 0.4f;

        private Coroutine fading;

        public event Action Advanced;
        public event Action Skipped;

        private void Awake()
        {
            if (advanceButton != null) advanceButton.onClick.AddListener(OnAdvance);
            if (skipButton != null) skipButton.onClick.AddListener(OnSkip);
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (advanceButton != null) advanceButton.onClick.RemoveListener(OnAdvance);
            if (skipButton != null) skipButton.onClick.RemoveListener(OnSkip);
        }

        // 건너뛰기 버튼은 화면 전체 버튼(panel)과 형제로 두고 같이 켜고 끔(버튼 안에 버튼을 넣지 않음)
        private void SetVisible(bool visible)
        {
            if (panel != null) panel.SetActive(visible);
            if (skipButton != null) skipButton.gameObject.SetActive(visible);
        }

        public void Show()
        {
            SetVisible(true);
            if (eventSystem != null && advanceButton != null) eventSystem.SetSelectedGameObject(advanceButton.gameObject);
        }

        public void Hide()
        {
            SetVisible(false);
            if (eventSystem != null) eventSystem.SetSelectedGameObject(null);
        }

        public void SetInteractable(bool interactable)
        {
            if (advanceButton != null) advanceButton.interactable = interactable;
            if (skipButton != null) skipButton.interactable = interactable;
        }

        public void ShowLine(string text)
        {
            if (lineText == null) return;
            lineText.text = text;
            if (fading != null) StopCoroutine(fading);
            if (isActiveAndEnabled && lineFadeDuration > 0f) fading = StartCoroutine(FadeInLine());
            else SetLineAlpha(1f);
        }

        private IEnumerator FadeInLine()
        {
            for (float t = 0f; t < lineFadeDuration; t += Time.unscaledDeltaTime)
            {
                SetLineAlpha(t / lineFadeDuration);
                yield return null;
            }
            SetLineAlpha(1f);
            fading = null;
        }

        private void SetLineAlpha(float a)
        {
            var c = lineText.color;
            c.a = a;
            lineText.color = c;
        }

        private void OnAdvance() => Advanced?.Invoke();
        private void OnSkip() => Skipped?.Invoke();
    }
}
