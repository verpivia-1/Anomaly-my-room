using Anomaly.Model.Anomalies;
using Anomaly.Model.Loop;
using Anomaly.Presenter.Anomalies;
using Anomaly.View.Cameras;
using Anomaly.View.Player;
using Anomaly.View.UI;
using UnityEngine;
using Random = System.Random;

namespace Anomaly.Presenter.Loop
{
    // 판정과 맵 전환
    public sealed class LoopPresenter
    {
        private readonly LoopModel loop;
        private readonly GameRuleSettings rules;
        private readonly AnomalyPool pool;
        private readonly Random random;
        private readonly AnomalyPresenter anomalies;
        private readonly PlayerView playerView;
        private readonly TopDownCameraView cameraView;
        private readonly Transform startPoint;
        private readonly MapNumberSignView mapSign;
        private readonly EndingView ending;
        private readonly ScreenFadeView fade;
        private readonly MapFeedbackView feedback;

        private const string CorrectMessageFormat = "{0}번 맵";
        private const string WrongMessage = "처음으로 돌아왔다";

        public LoopPresenter(LoopModel loop, GameRuleSettings rules, AnomalyPool pool, Random random,
            AnomalyPresenter anomalies, PlayerView playerView, TopDownCameraView cameraView,
            Transform startPoint, MapNumberSignView mapSign, EndingView ending, ScreenFadeView fade,
            MapFeedbackView feedback)
        {
            this.loop = loop;
            this.rules = rules;
            this.pool = pool;
            this.random = random;
            this.anomalies = anomalies;
            this.playerView = playerView;
            this.cameraView = cameraView;
            this.startPoint = startPoint;
            this.mapSign = mapSign;
            this.ending = ending;
            this.fade = fade;
            this.feedback = feedback;
        }

        // 0번 맵으로 시작. 암전된 화면에서 시작해 밝아진 뒤 조작 가능(로비에서 넘어올 때 이어지는 연출)
        public void StartLoop()
        {
            loop.SetPhase(LoopPhase.Transitioning);
            anomalies.RevertAll();
            EnterMap(rules.SafeMapNumber);
            MovePlayerToStart();
            if (fade != null) fade.SetCovered(ScreenFadeKind.Plain);
            FadeIn(() => loop.SetPhase(LoopPhase.Playing));
        }

        // 팝업에서 잠들기/나가기를 확인했을 때. 팝업 상태(Deciding)에서만 받음
        public void Resolve(Decision decision)
        {
            if (loop.Phase != LoopPhase.Deciding || decision == Decision.None) return;
            loop.SetPhase(LoopPhase.Transitioning); // 전환 중 잠금

            // 1) 정답/오답 판정: 이상현상이 있으면 나가기, 없으면 잠들기가 정답
            bool correct = (decision == Decision.Leave) == loop.HasAnomaly;

            // 화면을 다 덮은 뒤에 맵을 바꾼다(이상현상이 바뀌는 순간과 순간이동이 안 보이게)
            // 잠들기와 나가기는 다른 페이드(배경색·이미지)를 씀
            var kind = decision == Decision.Sleep ? ScreenFadeKind.Sleep : ScreenFadeKind.Leave;
            if (fade != null) fade.FadeOut(kind, () => ApplyTransition(correct));
            else ApplyTransition(correct);
        }

        // 2) ~ 7). 화면이 검게 덮인 동안 실행
        private void ApplyTransition(bool correct)
        {
            // 2) 현재 이상현상 원래대로 되돌리기
            anomalies.Revert(loop.CurrentAnomaly);

            // 3) 맵 번호 갱신(+1 또는 0)
            int next = correct ? loop.MapNumber + 1 : rules.SafeMapNumber;

            // 7번 맵에서 맞히면 클리어(목표 맵 도착)
            if (next >= rules.GoalMapNumber)
            {
                loop.EnterMap(next, false, null);
                loop.SetPhase(LoopPhase.Cleared);
                if (ending != null) ending.Show();
                FadeIn(null); // 엔딩 화면이 드러남
                return;
            }

            // 4) 새 맵의 bool 결정, 5) true면 풀에서 하나 뽑아 적용
            EnterMap(next);
            // 6) 플레이어를 시작 위치로
            MovePlayerToStart();
            // 7) 페이드 인이 끝나면 판정·이동 잠금 해제 + 정답/오답 피드백
            int mapNumber = loop.MapNumber;
            FadeIn(() =>
            {
                loop.SetPhase(LoopPhase.Playing);
                ShowFeedback(correct, mapNumber);
            });
        }

        private void ShowFeedback(bool correct, int mapNumber)
        {
            if (feedback != null)
                feedback.Show(correct, correct ? string.Format(CorrectMessageFormat, mapNumber) : WrongMessage);
            if (mapSign != null) mapSign.Highlight(correct);
        }

        private void FadeIn(System.Action onCleared)
        {
            if (fade != null) fade.FadeIn(onCleared);
            else onCleared?.Invoke();
        }

        public void MovePlayerToStart()
        {
            if (startPoint == null) return;
            playerView.Teleport(startPoint.position, startPoint.rotation);
            if (cameraView != null) cameraView.SnapToTarget();
        }

        private void EnterMap(int mapNumber)
        {
            bool hasAnomaly = mapNumber != rules.SafeMapNumber && random.NextDouble() < rules.AnomalyChance;

            AnomalyData anomaly = null;
            if (hasAnomaly && !pool.TryDraw(out anomaly))
            {
                Debug.LogWarning($"[{nameof(LoopPresenter)}] 이상현상 풀이 비어 있어 {mapNumber}번 맵을 정상으로 진행");
                hasAnomaly = false;
            }

            loop.EnterMap(mapNumber, hasAnomaly, anomaly);
            anomalies.Apply(loop.CurrentAnomaly);
            if (mapSign != null) mapSign.SetNumber(mapNumber);
        }
    }
}
