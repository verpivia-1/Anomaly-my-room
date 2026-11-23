using UnityEngine;

namespace Anomaly.View.Cameras
{
    // 플레이어 추적형 카메라. 탑뷰 시점
    // 조사 중에는 FocusOn으로 물건을 비추는 자리로 옮겨 가고, ClearFocus로 다시 플레이어를 따라감
    public sealed class TopDownCameraView : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -4f);
        [SerializeField] private float lookHeight = 0.5f;
        [SerializeField, Min(0f)] private float followSharpness = 8f;

        [Header("조사 클로즈업")]
        [Tooltip("물건 크기(대각선 반)에 곱해 카메라 거리를 정함")]
        [SerializeField, Min(0.5f)] private float closeupDistanceScale = 2.8f;
        [Tooltip("작은 물건도 이보다 가까이 붙지 않음(월드 단위)")]
        [SerializeField, Min(0.05f)] private float closeupMinDistance = 1f;
        [Tooltip("물건을 내려다보는 각도(도)")]
        [SerializeField, Range(0f, 80f)] private float closeupElevation = 30f;

        private bool hasFocus;
        private Vector3 focusPosition;
        private Quaternion focusRotation;

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

        // 물건(bounds)을 viewer 쪽에서 비스듬히 내려다보는 자리로 이동
        public void FocusOn(Bounds bounds, Vector3 viewerPosition)
        {
            var center = bounds.center;
            var flat = new Vector3(viewerPosition.x - center.x, 0f, viewerPosition.z - center.z);
            if (flat.sqrMagnitude < 0.0001f) flat = Vector3.back;
            flat.Normalize();
            float rad = closeupElevation * Mathf.Deg2Rad;
            var dir = flat * Mathf.Cos(rad) + Vector3.up * Mathf.Sin(rad);
            float distance = Mathf.Max(bounds.extents.magnitude * closeupDistanceScale, closeupMinDistance);
            focusPosition = center + dir * distance;
            focusRotation = Quaternion.LookRotation(center - focusPosition, Vector3.up);
            hasFocus = true;
        }

        public void ClearFocus()
        {
            hasFocus = false;
        }

        private void LateUpdate()
        {
            if (target == null && !hasFocus) return;
            float t = 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime);

            Vector3 desiredPosition;
            Quaternion desiredRotation;
            if (hasFocus)
            {
                desiredPosition = focusPosition;
                desiredRotation = focusRotation;
            }
            else
            {
                desiredPosition = target.position + offset;
                desiredRotation = Quaternion.LookRotation(-offset + Vector3.up * lookHeight, Vector3.up);
            }
            transform.position = Vector3.Lerp(transform.position, desiredPosition, t); // 선형보간으로 끊김 방지
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, t);
        }
    }
}
