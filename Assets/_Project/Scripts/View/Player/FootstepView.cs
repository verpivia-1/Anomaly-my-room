using System;
using UnityEngine;

namespace Anomaly.View.Player
{
    // 발소리. 걸은 거리가 보폭만큼 쌓일 때마다 클립 하나를 무작위로 재생(같은 소리 연속 방지, 피치 약간 흔들기).
    // PlayerView.Move가 이동 거리를 넘겨줌. 순간이동은 Move를 거치지 않아 발소리가 나지 않음
    public sealed class FootstepView : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] clips = Array.Empty<AudioClip>();
        [Tooltip("발소리 한 번당 이동 거리(월드 단위). 작을수록 발소리가 잦아짐")]
        [SerializeField, Min(0.05f)] private float strideLength = 1.1f;
        [SerializeField, Range(0f, 1f)] private float volume = 0.6f;
        [SerializeField, Range(0f, 0.3f)] private float pitchJitter = 0.06f;

        private float travelled;
        private int lastIndex = -1;

        public void Advance(float distance)
        {
            if (distance <= 0f)
            {
                travelled = strideLength * 0.5f; // 멈췄다 다시 걸을 때 반 보폭 뒤 첫 발소리
                return;
            }
            travelled += distance;
            if (travelled < strideLength) return;
            travelled -= strideLength;
            Play();
        }

        private void Play()
        {
            if (audioSource == null || clips.Length == 0) return;
            int index = UnityEngine.Random.Range(0, clips.Length);
            if (clips.Length > 1 && index == lastIndex) index = (index + 1) % clips.Length;
            lastIndex = index;
            if (clips[index] == null) return;
            audioSource.pitch = 1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
            audioSource.PlayOneShot(clips[index], volume);
        }

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
        }
    }
}
