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
        [Tooltip("거리를 잴 대상(예: 침대 모델). 비워 두면 구역 중심까지의 거리")]
        [SerializeField] private Renderer[] targetRenderers = Array.Empty<Renderer>();

        public bool IsInside { get; private set; }

        public event Action Entered;
        public event Action Exited;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            IsInside = false;
        }

        // 대상 바닥 범위(XZ)까지의 수평 거리. 다른 상호작용 대상과 누가 더 가까운지 비교할 때 씀
        public float DistanceToTarget(Vector3 position)
        {
            bool has = false;
            var bounds = new Bounds(transform.position, Vector3.zero);
            foreach (var r in targetRenderers)
            {
                if (r == null || !r.gameObject.activeInHierarchy) continue;
                if (!has) { bounds = r.bounds; has = true; }
                else bounds.Encapsulate(r.bounds);
            }
            float cx = Mathf.Clamp(position.x, bounds.min.x, bounds.max.x);
            float cz = Mathf.Clamp(position.z, bounds.min.z, bounds.max.z);
            return new Vector2(position.x - cx, position.z - cz).magnitude;
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
