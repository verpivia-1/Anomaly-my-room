namespace Anomaly.View.UI
{
    // 페이드 종류. 어떤 선택에 어떤 종류를 쓸지는 Presenter가 정함
    public enum ScreenFadeKind
    {
        Sleep, // 잠들기
        Leave  // 나가기(되돌아가기)
    }
}
