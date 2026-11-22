using System.Collections.Generic;
using UnityEngine;

namespace Anomaly.View.Cameras
{
    // 카메라에서 플레이어로 SphereCast를 쏴서 사이에 있는 FadeableView를 투명하게 함.
    // 카메라 이동(TopDownCameraView.LateUpdate) 뒤에 돌도록 실행 순서를 늦춤
    [DefaultExecutionOrder(100)]
    public sealed class WallFadeView : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("플레이어 발밑에서 이만큼 위를 겨냥(월드 단위)")]
        [SerializeField, Min(0f)] private float targetHeight = 0.8f;
        [Tooltip("레이 두께. 플레이어가 벽에 반쯤 가려져도 투명해지게")]
        [SerializeField, Min(0f)] private float radius = 0.3f;
        [SerializeField] private LayerMask layerMask = ~0;

        private readonly RaycastHit[] hits = new RaycastHit[16];
        private HashSet<FadeableView> current = new HashSet<FadeableView>();
        private HashSet<FadeableView> previous = new HashSet<FadeableView>();

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            (previous, current) = (current, previous);
            current.Clear();

            if (target != null)
            {
                var from = transform.position;
                var to = target.position + Vector3.up * targetHeight;
                var dir = to - from;
                float dist = dir.magnitude;
                if (dist > 0.001f)
                {
                    int count = Physics.SphereCastNonAlloc(from, radius, dir / dist, hits, dist, layerMask, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < count; i++)
                        if (hits[i].collider.TryGetComponent(out FadeableView fadeable)) current.Add(fadeable);
                }
            }

            foreach (var f in current) f.SetFaded(true);
            foreach (var f in previous)
                if (f != null && !current.Contains(f)) f.SetFaded(false);
        }
    }
}
