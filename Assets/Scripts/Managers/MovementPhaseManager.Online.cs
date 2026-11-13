using System.Linq;
using CounterAttack.Online;

public enum OnlineMovementIntent { SelectPlayer, Move, Forfeit }

public partial class MovementPhaseManager
{
    public bool OnlineBasicMovementAvailable => isActivated && !isPlayerMoving && !IsResolvingReposition
        && !IsResolvingFoulSequence() && !(activeDuelPanel != null && activeDuelPanel.IsOpen)
        && !isWaitingForInterceptionDiceRoll && !isWaitingForTackleRoll && !isWaitingForTackleDecision
        && !isWaitingForTackleDecisionWithoutMoving && !isWaitingForNutmegDecision
        && !isWaitingForNutmegDecisionWithoutMoving && !lookingForNutmegVictim
        && !isWaitingForSnapshotDecision && !isWaitingForReposition && !isWaitingForGKWallDiveDecision
        && !isDribblerRunning && !finalThirdManager.isActivated && !goalKeeperManager.isActivated
        && !shotManager.isActivated && highPassManager?.isActivated != true && looseBallManager?.isActivated != true;

    public bool TryExecuteOnlineMovement(OnlineMovementIntent intent,PlayerToken player=null,HexCell target=null)
    {
        if(OnlineRoomSession.IsGuest || MatchManager.Instance==null || MatchManager.Instance.IsGameplayInputBlocked
            || !OnlineBasicMovementAvailable) return false;
        switch(intent)
        {
            case OnlineMovementIntent.SelectPlayer when isAwaitingTokenSelection && player!=null && GetSelectableMovementTokens().Contains(player):
                OnClickReceived(player,player.GetCurrentHex());return true;
            case OnlineMovementIntent.Move when isAwaitingHexDestination && target!=null && target.GetOccupyingToken()==null && IsHexValidForMovement(target):
                OnClickReceived(null,target);return true;
            case OnlineMovementIntent.Forfeit when isAwaitingTokenSelection:
                CommitToAction();ForfeitTeamMovementPhase();return true;
            default:return false;
        }
    }
}
