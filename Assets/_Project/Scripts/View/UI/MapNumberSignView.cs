using UnityEngine;
using UnityEngine.UI;

namespace Anomaly.View.UI
{
    // 시작 지점의 맵 번호 푯말
    public sealed class MapNumberSignView : MonoBehaviour
    {
        [SerializeField] private Text label;
        [Tooltip("{0} 자리에 맵 번호가 들어감")]
        [SerializeField] private string format = "{0}";

        public void SetNumber(int mapNumber)
        {
            if (label != null) label.text = string.Format(format, mapNumber);
        }
    }
}
