using System;
using Anomaly.Model.Anomalies;

namespace Anomaly.Model.Loop
{
    // 현재 맵 번호, 이상현상 유무(bool), 진행 상태(잠금)를 가짐. 규칙 계산은 LoopPresenter가 함
    public sealed class LoopModel
    {
        public int MapNumber { get; private set; }
        public bool HasAnomaly { get; private set; }
        public AnomalyData CurrentAnomaly { get; private set; }
        public LoopPhase Phase { get; private set; } = LoopPhase.Playing;
        public Decision PendingDecision { get; private set; } = Decision.None;

        public event Action<int> MapChanged;
        public event Action<LoopPhase> PhaseChanged;

        public void EnterMap(int mapNumber, bool hasAnomaly, AnomalyData anomaly)
        {
            MapNumber = mapNumber;
            HasAnomaly = hasAnomaly;
            CurrentAnomaly = hasAnomaly ? anomaly : null;
            MapChanged?.Invoke(mapNumber);
        }

        public void BeginDecision(Decision decision)
        {
            PendingDecision = decision;
            SetPhase(LoopPhase.Deciding);
        }

        public void EndDecision()
        {
            PendingDecision = Decision.None;
        }

        public void SetPhase(LoopPhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
