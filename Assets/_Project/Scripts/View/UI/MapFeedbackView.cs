using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Anomaly.View.UI
{
    // 맵 전환 직후 정답/오답 피드백. 화면 문구를 잠깐 띄웠다 사라지게 하고, 효과음이 있으면 재생
    public sealed class MapFeedbackView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text label;
        [SerializeField] private Color correctColor = new Color(0.55f, 0.95f, 0.6f);
        [SerializeField] private Color wrongColor = new Color(1f, 0.45f, 0.4f);
        [SerializeField, Min(0f)] private float showDuration = 1.2f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.4f;

        [Header("효과음(비워 두면 소리 없음)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip correctClip;
        [SerializeField] private AudioClip wrongClip;

        private Coroutine running;

        private void Awake()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        public void Show(bool correct, string message)
        {
            if (label != null)
            {
                label.text = message;
                label.color = correct ? correctColor : wrongColor;
            }

            var clip = correct ? correctClip : wrongClip;
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);

            if (canvasGroup == null || !isActiveAndEnabled) return;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            canvasGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(showDuration);
            for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
            {
                canvasGroup.alpha = 1f - t / fadeDuration;
                yield return null;
            }
            canvasGroup.alpha = 0f;
            running = null;
        }
    }
}
