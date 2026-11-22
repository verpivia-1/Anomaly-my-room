using System.Collections.Generic;
using Random = System.Random;

namespace Anomaly.Model.Anomalies
{
    // 판정 메모 2장: 원본을 복사한 리스트에서 무작위로 하나 뽑고 제거.
    // 복사본이 비면 원본에서 다시 복사. 0번 맵으로 돌아가도 초기화하지 않음(다 써야만 초기화)
    public sealed class AnomalyPool
    {
        private readonly List<AnomalyData> original;
        private readonly List<AnomalyData> remaining;
        private readonly Random random;

        public int OriginalCount => original.Count;
        public int RemainingCount => remaining.Count;

        public AnomalyPool(IEnumerable<AnomalyData> source, Random random)
        {
            this.random = random;
            original = new List<AnomalyData>();
            foreach (var anomaly in source)
                if (anomaly != null) original.Add(anomaly);
            remaining = new List<AnomalyData>(original);
        }

        // 원본이 비어 있을 때만 false
        public bool TryDraw(out AnomalyData anomaly)
        {
            anomaly = null;
            if (original.Count == 0) return false;
            if (remaining.Count == 0) remaining.AddRange(original);

            int index = random.Next(remaining.Count);
            anomaly = remaining[index];
            // 순서는 의미가 없으니 마지막 값과 바꿔서 제거(O(1))
            int last = remaining.Count - 1;
            remaining[index] = remaining[last];
            remaining.RemoveAt(last);
            return true;
        }
    }
}
