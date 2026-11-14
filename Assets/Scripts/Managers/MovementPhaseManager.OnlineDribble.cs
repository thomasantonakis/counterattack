using System.Linq;
using CounterAttack.Online;

public enum OnlineDribbleIntent { Move, Finish, DeclineNutmeg, Steal }

public partial class MovementPhaseManager
{
    private bool OnlineDribbleBlocked=>MatchManager.Instance?.IsWaitingForExtraActionsRoll==true
        || finalThirdManager?.isActivated==true || goalKeeperManager?.isActivated==true || goalKeeperManager?.IsResolvingMove==true
        || goalKeeperManager?.IsWaitingForPickupChoice==true || shotManager?.isActivated==true || highPassManager?.isActivated==true
        || looseBallManager?.isActivated==true || goalFlowManager?.isActivated==true || IsResolvingFoulSequence();
    public string OnlineDribbleStage=>!isActivated || !isDribblerRunning && !isWaitingForInterceptionDiceRoll && !isWaitingForNutmegDecision?null
        :OnlineDribbleBlocked || isPlayerMoving?"resolving":isWaitingForInterceptionDiceRoll?"steal"
        :isWaitingForNutmegDecision?"nutmeg":isAwaitingHexDestination && selectedToken!=null && remainingDribblerPace>0?"target":"resolving";
    public PlayerToken OnlineDribbleActor=>isWaitingForInterceptionDiceRoll?selectedDefender:selectedToken;
    public HexCell[] OnlineDribbleTargets=>OnlineDribbleStage!="target"?System.Array.Empty<HexCell>()
        :hexGrid.highlightedHexes.ToArray().Where(h=>h!=null && h.GetOccupyingToken()==null
            && HexGridUtils.GetHexStepDistance(selectedToken.currentHex,h)==1 && IsHexValidForMovement(h)).ToArray();
    public bool TryExecuteOnlineDribble(OnlineDribbleIntent intent,HexCell target=null)
    {
        var match=MatchManager.Instance;
        if(match==null || !match.IsOnlineAuthorityControlled || match.IsGameplayInputBlocked || OnlineDribbleBlocked || isPlayerMoving)return false;
        switch(intent)
        {
            case OnlineDribbleIntent.Move:
                if(!OnlineDribbleTargets.Contains(target))return false;
                OnClickReceived(null,target);return true;
            case OnlineDribbleIntent.Finish:
                if(OnlineDribbleStage!="target")return false;
                isWaitingForSnapshotDecision=false;FinishRunningDribble();return true;
            case OnlineDribbleIntent.DeclineNutmeg:
                if(OnlineDribbleStage!="nutmeg")return false;
                RecordNutmegChoiceSelected("online","allow_interceptions",IsInstructionExpectingHomeTeam());
                DeclineRunningNutmeg();return true;
            case OnlineDribbleIntent.Steal:
                if(OnlineDribbleStage!="steal" || selectedDefender==null)return false;
                StartCoroutine(PerformBallInterceptionDiceRoll((RollInputOverride?)null));return true;
            default:return false;
        }
    }
    private void DeclineRunningNutmeg()
    {
        CommitToAction();isWaitingForNutmegDecision=false;
        UnityEngine.Debug.Log("Reject Nutmeg. Check for interceptions.");
        ContinueFromRejectedNutmeg();
    }
    private void FinishRunningDribble()
    {
        CommitToAction();UnityEngine.Debug.Log("Dribbler is running. Forfeiting remaining pace.");
        AdvanceMovementPhase();
    }
}
