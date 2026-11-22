using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Anomaly.View.UI
{
    // 시작 지점의 맵 번호 푯말. 맵 전환 직후 정답이면 초록, 오답이면 빨강으로 잠깐 깜빡임
    public sealed class MapNumberSignView : MonoBehaviour
    {
        [SerializeField] private Text label;
        [Tooltip("{0} 자리에 맵 번호가 들어감")]
        [SerializeField] private string format = "{0}";
        [SerializeField] private Color correctColor = new Color(0.45f, 1f, 0.5f);
        [SerializeField] private Color wrongColor = new Color(1f, 0.35f, 0.3f);
        [SerializeField, Min(0.01f)] private float highlightDuration = 1.5f;
        [Tooltip("깜빡이는 횟수")]
        [SerializeField, Min(1)] private int blinkCount = 3;

        private Color normalColor = Color.white;
        private Coroutine running;

        private void Awake()
        {
            if (label != null) normalColor = label.color;
        }

        public void SetNumber(int mapNumber)
        {
            if (label != null) label.text = string.Format(format, mapNumber);
        }

        public void Highlight(bool correct)
        {
            if (label == null || !isActiveAndEnabled) return;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(HighlightRoutine(correct ? correctColor : wrongColor));
        }

        private IEnumerator HighlightRoutine(Color color)
        {
            float half = highlightDuration / (blinkCount * 2);
            for (int i = 0; i < blinkCount; i++)
            {
                label.color = color;
                yield return new WaitForSecondsRealtime(half);
                label.color = normalColor;
                yield return new WaitForSecondsRealtime(half);
            }
            label.color = normalColor;
            running = null;
        }
    }
}
