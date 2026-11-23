using System;
using UnityEngine;

namespace Anomaly.View.Interaction
{
    // 조사할 수 있는 물건. 플레이어가 조사 범위 안에 있으면 조사 후보가 됨.
    // 조사 지점(inspectPoint)이 있으면 그 바닥 지점 기준, 없으면 물건이 차지한 바닥 범위 기준으로 거리를 잼
    // → 소파 뒤 벽 액자처럼 가까이 갈 수 없는 물건은 조사 지점을 소파 앞에 두면 됨
    public sealed class InspectableView : MonoBehaviour
    {
        [SerializeField] private string displayName = "";
        [Tooltip("조사 화면에 함께 표시. 비워 두면 이름만")]
        [SerializeField, TextArea] private string description = "";
        [Tooltip("모양·크기 계산용. 모델 교체용 모델도 같이 넣어 두면 켜져 있는 쪽만 씀")]
        [SerializeField] private Renderer[] renderers = Array.Empty<Renderer>();
        [Tooltip("비워 두면 물건 범위 기준")]
        [SerializeField] private Transform inspectPoint;
        [Tooltip("조사 가능한 수평 거리(월드 단위)")]
        [SerializeField, Min(0f)] private float reach = 1.2f;

        public string DisplayName => displayName;
        public string Description => description;

        public Bounds GetBounds()
        {
            bool has = false;
            var bounds = new Bounds(transform.position, Vector3.zero);
            foreach (var r in renderers)
            {
                if (r == null || !r.gameObject.activeInHierarchy) continue;
                if (!has) { bounds = r.bounds; has = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return bounds;
        }

        // 조사 범위 안이면 수평 거리, 밖이면 +무한대
        public float DistanceFrom(Vector3 position)
        {
            float d;
            if (inspectPoint != null)
            {
                var p = inspectPoint.position;
                d = new Vector2(position.x - p.x, position.z - p.z).magnitude;
            }
            else
            {
                var b = GetBounds();
                float cx = Mathf.Clamp(position.x, b.min.x, b.max.x);
                float cz = Mathf.Clamp(position.z, b.min.z, b.max.z);
                d = new Vector2(position.x - cx, position.z - cz).magnitude;
            }
            return d <= reach ? d : float.PositiveInfinity;
        }
    }
}
