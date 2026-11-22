using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Anomaly.View.UI
{
    // 화면 전체를 덮었다가 걷어내는 페이드. 맵 전환 때 이상현상이 바뀌는 순간과 순간이동을 가림.
    // 종류(잠들기/나가기)마다 배경색과 이미지가 다름. 이미지가 비어 있으면 배경색만 나옴.
    // 다 덮었을 때 / 다 걷었을 때 넘겨받은 콜백을 부름(무엇을 할지는 Presenter가 정함)
    public sealed class ScreenFadeView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image background;
        [SerializeField] private Image picture;

        [Header("잠들기")]
        [SerializeField] private Sprite sleepImage;
        [SerializeField] private Color sleepBackground = new Color(0.02f, 0.03f, 0.08f);

        [Header("나가기")]
        [SerializeField] private Sprite leaveImage;
        [SerializeField] private Color leaveBackground = Color.black;

        [Header("시간(초)")]
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;
        [Tooltip("완전히 덮인 상태로 멈추는 시간. 이미지를 보여줄 시간")]
        [SerializeField, Min(0f)] private float holdDuration = 0.6f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.35f;

        private Coroutine running;

        public bool IsFading => running != null;

        private void Awake()
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        public void FadeOut(ScreenFadeKind kind, Action onCovered)
        {
            ApplyStyle(kind);
            Run(1f, fadeOutDuration, holdDuration, onCovered);
        }

        // 마지막 FadeOut의 모습 그대로 걷어냄
        public void FadeIn(Action onCleared) => Run(0f, fadeInDuration, 0f, onCleared);

        private void ApplyStyle(ScreenFadeKind kind)
        {
            var sprite = kind == ScreenFadeKind.Sleep ? sleepImage : leaveImage;
            if (background != null) background.color = kind == ScreenFadeKind.Sleep ? sleepBackground : leaveBackground;
            if (picture != null)
            {
                picture.sprite = sprite;
                picture.enabled = sprite != null;
            }
        }

        private void Run(float targetAlpha, float duration, float hold, Action onDone)
        {
            if (canvasGroup == null || !isActiveAndEnabled)
            {
                onDone?.Invoke(); // 연결이 없으면 페이드 없이 바로 진행
                return;
            }
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Fade(targetAlpha, duration, hold, onDone));
        }

        private IEnumerator Fade(float targetAlpha, float duration, float hold, Action onDone)
        {
            canvasGroup.blocksRaycasts = true; // 페이드 중에는 UI 클릭 막기
            float start = canvasGroup.alpha;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t / duration);
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
            if (hold > 0f) yield return new WaitForSecondsRealtime(hold);

            if (targetAlpha <= 0f) canvasGroup.blocksRaycasts = false;
            running = null;
            onDone?.Invoke();
        }
    }
}
