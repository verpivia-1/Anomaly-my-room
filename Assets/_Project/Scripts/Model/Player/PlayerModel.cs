using System;

namespace Anomaly.Model.Player
{
    // 상호작용 가능한 물체 클릭시 팝업되는 상태에서 움직임 방지용
    // 여기서 움직임 가능 불가능 여부 만들고 불가능이면 방향벡터 0,0,0 강제 고정(처리는 다른 cs파일서 진행). 계산식상

    public sealed class PlayerModel
    {
        public bool CanMove { get; private set; } = true;

        public event Action<bool> CanMoveChanged;

        public void SetCanMove(bool canMove)
        {
            if (CanMove == canMove) return;
            CanMove = canMove;
            CanMoveChanged?.Invoke(canMove);
        }
    }
}