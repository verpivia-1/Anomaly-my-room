using System;
using Anomaly.Model.Loop;
using Anomaly.Model.Player;
using Anomaly.View.Interaction;
using Anomaly.View.Player;
using Anomaly.View.UI;

namespace Anomaly.Presenter.Loop
{
    // 침대 상호작용·출구 지역 진입 → 확인 팝업 → 선택 처리, 그리고 잠금
    // 일단 임시로 하드코딩 해둔 상태. 최종 마무리 폴리싱작업때 json이나 스크립터블로 변경
    public sealed class DecisionPresenter : IDisposable
    {
        private const string SleepMessage = "잠들까요?";
        private const string SleepConfirm = "잠들기";
        private const string SleepCancel = "잠들지 않기";
        private const string LeaveMessage = "나갈까요?";
        private const string LeaveConfirm = "나가기";
        private const string LeaveCancel = "나가지 않기";

        private readonly LoopModel loop;
        private readonly PlayerModel player;
        private readonly LoopPresenter loopPresenter;
        private readonly PlayerInputView input;
        private readonly InteractZoneView bedZone;
        private readonly InteractZoneView exitZone;
        private readonly ConfirmPopupView popup;

        public DecisionPresenter(LoopModel loop, PlayerModel player, LoopPresenter loopPresenter,
            PlayerInputView input, InteractZoneView bedZone, InteractZoneView exitZone, ConfirmPopupView popup)
        {
            this.loop = loop;
            this.player = player;
            this.loopPresenter = loopPresenter;
            this.input = input;
            this.bedZone = bedZone;
            this.exitZone = exitZone;
            this.popup = popup;

            loop.PhaseChanged += OnPhaseChanged;
            input.InteractPressed += OnInteractPressed;
            exitZone.Entered += OnExitZoneEntered;
            popup.Confirmed += OnConfirmed;
            popup.Canceled += OnCanceled;
        }

        public void Dispose()
        {
            loop.PhaseChanged -= OnPhaseChanged;
            input.InteractPressed -= OnInteractPressed;
            exitZone.Entered -= OnExitZoneEntered;
            popup.Confirmed -= OnConfirmed;
            popup.Canceled -= OnCanceled;
        }

        // 팝업·전환·엔딩 중에는 이동 잠금
        private void OnPhaseChanged(LoopPhase phase)
        {
            player.SetCanMove(phase == LoopPhase.Playing);
        }

        // 잠들기: 침대 앞에서 상호작용
        private void OnInteractPressed()
        {
            if (loop.Phase != LoopPhase.Playing || !bedZone.IsInside) return;
            Open(Decision.Sleep, SleepMessage, SleepConfirm, SleepCancel);
        }

        // 나가기: 출구 지역에 들어올 때 한 번만 팝업
        private void OnExitZoneEntered()
        {
            if (loop.Phase != LoopPhase.Playing) return;
            Open(Decision.Leave, LeaveMessage, LeaveConfirm, LeaveCancel);
        }

        private void Open(Decision decision, string message, string confirm, string cancel)
        {
            loop.BeginDecision(decision); // 팝업이 뜨면 이동과 다른 상호작용 잠금
            popup.Show(message, confirm, cancel);
        }

        private void OnConfirmed()
        {
            var decision = loop.PendingDecision;
            popup.Hide();
            loop.EndDecision();
            loopPresenter.Resolve(decision); // 전환이 끝나면 LoopPresenter가 잠금 해제
        }

        private void OnCanceled()
        {
            var decision = loop.PendingDecision;
            popup.Hide();
            loop.EndDecision();
            if (decision == Decision.Leave) loopPresenter.MovePlayerToStart(); // 나가지 않기: 시작 지점으로
            loop.SetPhase(LoopPhase.Playing); // 잠들지 않기: 그 자리에서 해제
        }
    }
}
