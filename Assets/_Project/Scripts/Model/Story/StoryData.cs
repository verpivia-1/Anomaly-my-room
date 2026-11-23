using System.Collections.Generic;
using UnityEngine;

namespace Anomaly.Model.Story
{
    // 로비에서 게임을 시작할 때 보여주는 인트로 문장들. 한 줄씩 넘겨 가며 표시
    [CreateAssetMenu(fileName = "IntroStory", menuName = "Anomaly/Story Data")]
    public sealed class StoryData : ScriptableObject
    {
        [SerializeField, TextArea(2, 4)] private List<string> lines = new List<string>();
        [Tooltip("켜면 처음 한 번만 재생(다음부터는 바로 게임 시작)")]
        [SerializeField] private bool showOnlyFirstTime = true;

        public IReadOnlyList<string> Lines => lines;
        public bool ShowOnlyFirstTime => showOnlyFirstTime;
    }
}
