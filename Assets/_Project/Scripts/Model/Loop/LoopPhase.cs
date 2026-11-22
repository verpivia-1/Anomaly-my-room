namespace Anomaly.Model.Loop
{
    public enum LoopPhase
    {
        Playing,       // 이동·상호작용 가능
        Deciding,      // 팝업이 떠 있음. 이동과 다른 상호작용 잠금
        Transitioning, // 판정~맵 전환 중. 판정과 이동 잠금(연타로 맵이 여러 번 넘어가는 것 방지)
        Cleared        // 엔딩
    }
}
