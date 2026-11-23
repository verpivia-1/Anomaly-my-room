using Anomaly.Model.Loop;
using Anomaly.View.Cameras;
using Anomaly.View.Interaction;
using Anomaly.View.Player;
using Anomaly.View.UI;

namespace Anomaly.Presenter.Inspect
{
    // 물건 조사: 카메라를 물건 앞으로 옮기고(클로즈업), 캐릭터를 숨기고, 이름·설명 패널을 띄움
    public sealed class InspectPresenter
    {
        private const string CloseHintFormat = "{0} / Esc  닫기";

        private readonly LoopModel loop;
        private readonly TopDownCameraView cameraView;
        private readonly PlayerView playerView;
        private readonly PlayerInputView input;
        private readonly InspectPanelView panel;

        public InspectPresenter(LoopModel loop, TopDownCameraView cameraView, PlayerView playerView,
            PlayerInputView input, InspectPanelView panel)
        {
            this.loop = loop;
            this.cameraView = cameraView;
            this.playerView = playerView;
            this.input = input;
            this.panel = panel;
        }

        public void Open(InspectableView target)
        {
            if (loop.Phase != LoopPhase.Playing || target == null) return;
            loop.SetPhase(LoopPhase.Inspecting);
            if (cameraView != null) cameraView.FocusOn(target.GetBounds(), playerView.Position);
            playerView.SetVisible(false);
            // 지금은 "소파다", "책이다"처럼 사물 이름만 보여줌(설명 문구는 나중에)
            if (panel != null) panel.Show(Declare(target.DisplayName), string.Empty, string.Format(CloseHintFormat, input.InteractKeyName));
        }

        // 마지막 글자에 받침이 있으면 "이다", 없으면 "다"(소파다 / 책이다)
        private static string Declare(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            char last = name[name.Length - 1];
            bool hasFinal = last >= '가' && last <= '힣' && (last - '가') % 28 != 0;
            return name + (hasFinal ? "이다" : "다");
        }

        public void Close()
        {
            if (loop.Phase != LoopPhase.Inspecting) return;
            if (cameraView != null) cameraView.ClearFocus();
            playerView.SetVisible(true);
            if (panel != null) panel.Hide();
            loop.SetPhase(LoopPhase.Playing);
        }
    }
}
