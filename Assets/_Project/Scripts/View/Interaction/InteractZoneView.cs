using System;
using UnityEngine;

namespace Anomaly.View.Interaction
{
    // 대상(플레이어)이 이 구역 안에 있는지 매 프레임 위치로 확인해서 들어옴/나감을 알림.
    // 트리거 메시지(OnTriggerEnter) 대신 위치로 보는 이유: 텔레포트 때 CharacterController를 껐다 켜면 Exit가 안 올 수 있음
    public sealed class InteractZoneView : MonoBehaviour
    {
        [SerializeField] private Collider zone;
        [SerializeField] private Transform target;
        [Tooltip("대상 발밑에서 이만큼 위 지점으로 판정(월드 단위)")]
        [SerializeField, Min(0f)] private float checkHeight = 0.5f;

        public bool IsInside { get; private set; }

        public event Action Entered;
        public event Action Exited;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            IsInside = false;
        }

        private void Update()
        {
            if (zone == null || target == null) return;

            var point = target.position + Vector3.up * checkHeight;
            bool inside = (zone.ClosestPoint(point) - point).sqrMagnitude < 0.0001f; // 안쪽이면 ClosestPoint가 자기 자신
            if (inside == IsInside) return;

            IsInside = inside;
            if (inside) Entered?.Invoke();
            else Exited?.Invoke();
        }

        private void Reset()
        {
            zone = GetComponent<Collider>();
        }
    }
}
