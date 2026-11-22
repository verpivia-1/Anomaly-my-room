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

        public LoopPresenter(LoopModel loop, GameRuleSettings rules, AnomalyPool pool, Random random,
            AnomalyPresenter anomalies, PlayerView playerView, TopDownCameraView cameraView,
            Transform startPoint, MapNumberSignView mapSign, EndingView ending)
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
        }

        public void StartLoop()
        {
            anomalies.RevertAll();
            EnterMap(rules.SafeMapNumber);
            MovePlayerToStart();
            loop.SetPhase(LoopPhase.Playing);
        }

        // 팝업에서 잠들기/나가기를 확인했을 때. 팝업 상태(Deciding)에서만 받음
        public void Resolve(Decision decision)
        {
            if (loop.Phase != LoopPhase.Deciding || decision == Decision.None) return;
            loop.SetPhase(LoopPhase.Transitioning); // 전환 중 잠금

            // 1) 정답/오답 판정: 이상현상이 있으면 나가기, 없으면 잠들기가 정답
            bool correct = (decision == Decision.Leave) == loop.HasAnomaly;

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
                return;
            }

            // 4) 새 맵의 bool 결정, 5) true면 풀에서 하나 뽑아 적용
            EnterMap(next);
            // 6) 플레이어를 시작 위치로
            MovePlayerToStart();
            // 7) 판정·이동 잠금 해제
            loop.SetPhase(LoopPhase.Playing);
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
