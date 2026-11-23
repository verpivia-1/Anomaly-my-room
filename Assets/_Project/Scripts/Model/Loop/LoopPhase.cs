namespace Anomaly.Model.Loop
{
    public enum LoopPhase
    {
        Playing,       // 이동·상호작용 가능
        Deciding,      // 팝업이 떠 있음. 이동과 다른 상호작용 잠금
        Transitioning, // 판정~맵 전환 중. 판정과 이동 잠금(연타로 맵이 여러 번 넘어가는 것 방지)
        Inspecting,    // 물건 조사(클로즈업) 중. 이동 잠금
        Paused,        // 일시정지 메뉴. 시간 정지
        Cleared        // 엔딩
    }
}
