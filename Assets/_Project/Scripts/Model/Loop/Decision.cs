namespace Anomaly.Model.Loop
{
    public enum Decision
    {
        None,
        Sleep, // 103 침대에서 잠들기 = 이상현상 없음
        Leave  // 복도 서쪽 끝에서 나가기 = 이상현상 있음
    }
}
