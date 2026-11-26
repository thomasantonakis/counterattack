using System;
using System.Linq;
using CounterAttack.Online;

public enum OnlineFinalThirdIntent { SelectPlayer, Move, Forfeit, DropBall, GoalkeeperKick, GoalKickPass }

public partial class FinalThirdManager
{
    private PlayerToken onlineAcknowledgedSelection;
    public string OnlineDecisionStage => !isActivated || isMovingToken || forfeitWasPressed ? "resolving"
        : isWaitingForWhatToDo ? "keeper-choice" : isWaitingForTargetHex && selectedToken!=null && onlineAcknowledgedSelection==selectedToken ? "target"
        : isWaitingForTokenSelection ? "select" : "resolving";
    public PlayerToken OnlineSelectedPlayer => selectedToken;
    public PlayerToken[] OnlineSelectablePlayers => currentMovableTokens?.Where(token=>token!=null).ToArray() ?? Array.Empty<PlayerToken>();
    public bool OnlineCanForfeit => isActivated && !isMovingToken && !forfeitWasPressed && !isWaitingForWhatToDo && CanForfeitCurrentF3Move(false);

    public bool TryExecuteOnlineFinalThird(OnlineFinalThirdIntent intent,PlayerToken player=null,HexCell target=null)
    {
        if(OnlineRoomSession.IsGuest || MatchManager.Instance==null || MatchManager.Instance.IsGameplayInputBlocked
            || !isActivated || isMovingToken || forfeitWasPressed) return false;
        switch(intent)
        {
            case OnlineFinalThirdIntent.SelectPlayer when !isWaitingForWhatToDo && isWaitingForTokenSelection && OnlineSelectablePlayers.Contains(player):
                HandleTokenSelectionForF3(player);return true;
            case OnlineFinalThirdIntent.Move when OnlineDecisionStage=="target" && !isWaitingForWhatToDo && isWaitingForTargetHex && selectedToken!=null
                && target!=null && GetMovementDestinationsForDecisionPreview(selectedToken).Contains(target) && hexGrid.highlightedHexes.Contains(target):
                StartCoroutine(ConfirmTokenMove(target));return true;
            case OnlineFinalThirdIntent.Forfeit when OnlineCanForfeit:
                ForfeitTurn();return true;
            case OnlineFinalThirdIntent.DropBall when isWaitingForWhatToDo && !isWaitingForSetPieceGoalKickChoice:
                DropBall();return true;
            case OnlineFinalThirdIntent.GoalkeeperKick when isWaitingForWhatToDo:
                GKKick();return true;
            case OnlineFinalThirdIntent.GoalKickPass when isWaitingForWhatToDo && isWaitingForSetPieceGoalKickChoice:
                StandardGoalKickPass();return true;
            default:return false;
        }
    }
}
