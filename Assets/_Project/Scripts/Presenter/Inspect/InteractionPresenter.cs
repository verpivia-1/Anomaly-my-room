using System;
using Anomaly.Model.Loop;
using Anomaly.Presenter.Loop;
using Anomaly.Presenter.Menu;
using Anomaly.View.Interaction;
using Anomaly.View.Player;
using Anomaly.View.UI;
using UnityEngine;

namespace Anomaly.Presenter.Inspect
{
    // E·Esc 입력을 지금 상태에 맞게 한 곳에서 나눠 줌.
    //  E  : 플레이 중이면 가장 가까운 대상(침대 = 잠들기 / 물건 = 조사), 조사 중이면 닫기
    //  Esc: 팝업이면 취소, 조사 중이면 닫기, 플레이 중이면 일시정지, 일시정지면 계속
    // 플레이 중에는 매 프레임 가장 가까운 대상을 골라 안내 문구("E  잠들기" / "E  조사: 액자")를 띄움
    public sealed class InteractionPresenter : IDisposable
    {
        private const string InspectLabelFormat = "조사: {0}";

        private readonly LoopModel loop;
        private readonly PlayerInputView input;
        private readonly PlayerView playerView;
        private readonly InteractZoneView bedZone;
        private readonly InspectableView[] inspectables;
        private readonly InteractPromptView prompt;
        private readonly DecisionPresenter decision;
        private readonly InspectPresenter inspect;
        private readonly MenuPresenter menu;

        private bool targetIsBed;
        private InspectableView targetInspectable;

        public InteractionPresenter(LoopModel loop, PlayerInputView input, PlayerView playerView,
            InteractZoneView bedZone, InspectableView[] inspectables, InteractPromptView prompt,
            DecisionPresenter decision, InspectPresenter inspect, MenuPresenter menu)
        {
            this.loop = loop;
            this.input = input;
            this.playerView = playerView;
            this.bedZone = bedZone;
            this.inspectables = inspectables ?? Array.Empty<InspectableView>();
            this.prompt = prompt;
            this.decision = decision;
            this.inspect = inspect;
            this.menu = menu;

            input.InteractPressed += OnInteract;
            input.CancelPressed += OnCancel;
        }

        public void Dispose()
        {
            input.InteractPressed -= OnInteract;
            input.CancelPressed -= OnCancel;
        }

        public void Tick()
        {
            if (loop.Phase != LoopPhase.Playing)
            {
                ClearTarget();
                return;
            }
            FindTarget();
            if (prompt == null) return;
            if (targetIsBed) prompt.Show($"{input.InteractKeyName}  {decision.SleepLabel}");
            else if (targetInspectable != null) prompt.Show($"{input.InteractKeyName}  {string.Format(InspectLabelFormat, targetInspectable.DisplayName)}");
            else prompt.Hide();
        }

        // 침대 구역 안이면 침대 가장자리까지의 거리, 물건은 각자의 조사 거리로 비교해 가장 가까운 것
        private void FindTarget()
        {
            targetIsBed = false;
            targetInspectable = null;
            var pos = playerView.Position;
            float best = float.PositiveInfinity;

            if (bedZone != null && bedZone.IsInside)
            {
                best = bedZone.DistanceToTarget(pos);
                targetIsBed = true;
            }
            foreach (var item in inspectables)
            {
                if (item == null) continue;
                float d = item.DistanceFrom(pos);
                if (d >= best) continue;
                best = d;
                targetIsBed = false;
                targetInspectable = item;
            }
        }

        private void ClearTarget()
        {
            targetIsBed = false;
            targetInspectable = null;
            if (prompt != null) prompt.Hide();
        }

        private void OnInteract()
        {
            switch (loop.Phase)
            {
                case LoopPhase.Playing:
                    FindTarget();
                    if (targetIsBed) decision.OpenSleep();
                    else if (targetInspectable != null) inspect.Open(targetInspectable);
                    break;
                case LoopPhase.Inspecting:
                    inspect.Close();
                    break;
            }
        }

        private void OnCancel()
        {
            switch (loop.Phase)
            {
                case LoopPhase.Deciding: decision.Cancel(); break;
                case LoopPhase.Inspecting: inspect.Close(); break;
                case LoopPhase.Playing: menu.Pause(); break;
                case LoopPhase.Paused: menu.Resume(); break;
            }
        }
    }
}
