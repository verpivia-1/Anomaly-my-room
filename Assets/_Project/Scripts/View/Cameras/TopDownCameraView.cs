using UnityEngine;

namespace Anomaly.View.Cameras
{
    // 플레이어 추적형 카메라. 탑뷰 시점
    public sealed class TopDownCameraView : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -4f);
        [SerializeField] private float lookHeight = 0.5f;
        [SerializeField, Min(0f)] private float followSharpness = 8f;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            SnapToTarget();
        }

        // 타켓플레이어 위치로 텔포
        public void SnapToTarget()
        {
            if (target == null) return;
            transform.position = target.position + offset;
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, target.position + offset, t); // 선형보간으로 끊김 방지
            transform.LookAt(transform.position - offset + Vector3.up * lookHeight);
        }
    }
}