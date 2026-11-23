namespace Anomaly.View.UI
{
    // 페이드 종류. 어떤 상황에 어떤 종류를 쓸지는 Presenter가 정함
    public enum ScreenFadeKind
    {
        Sleep, // 잠들기
        Leave, // 나가기(되돌아가기)
        Plain  // 이미지 없는 단순 암전(로비 → 게임, 게임 시작)
    }
}
