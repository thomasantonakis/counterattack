using CounterAttack.Online;

public enum OnlineMovementSnapshotIntent { Shoot, Continue }
public partial class MovementPhaseManager
{
    public PlayerToken OnlineMovementSnapshotActor=>isDribblerRunning?selectedToken:MatchManager.Instance?.LastTokenToTouchTheBallOnPurpose;
    public string OnlineMovementSnapshotStage=>!isActivated || !isWaitingForSnapshotDecision || OnlineDribbleBlocked || isPlayerMoving
        || IsResolvingReposition || isWaitingForInterceptionDiceRoll || isWaitingForTackleRoll || isWaitingForGKWallDiveDecision
        || isWaitingForNutmegDecision || isWaitingForNutmegDecisionWithoutMoving || lookingForNutmegVictim
        || isWaitingForTackleDecision || isWaitingForTackleDecisionWithoutMoving?null:isDribblerRunning?"running":"exhausted";
    public bool OnlineCanTakeMovementSnapshot=>OnlineMovementSnapshotStage!=null && !IsSnapshotSuppressedByFinalExtraMovement()
        && shotManager!=null && shotManager.OnlineCanStartOfferedSnapshot(OnlineMovementSnapshotActor);
    public bool TryExecuteOnlineMovementSnapshot(OnlineMovementSnapshotIntent intent)
    {
        var match=MatchManager.Instance;
        if(OnlineRoomSession.IsGuest || match==null || !match.IsOnlineAuthorityControlled || match.IsGameplayInputBlocked || OnlineMovementSnapshotStage==null)return false;
        if(intent==OnlineMovementSnapshotIntent.Shoot)
        {if(!OnlineCanTakeMovementSnapshot)return false;AcceptOfferedMovementSnapshot();return true;}
        if(intent==OnlineMovementSnapshotIntent.Continue){DeclineOfferedMovementSnapshot();return true;}
        return false;
    }
    // Physical offline input and authenticated online intentions share these source paths.
    private void AcceptOfferedMovementSnapshot()
    {
        var actor=OnlineMovementSnapshotActor;bool running=isDribblerRunning;
        isWaitingForSnapshotDecision=false;if(running)MarkDribblerMovedAfterSnapshotDecision();
        UnityEngine.Debug.Log($"{actor.name} decides to Snapshot!!!!");
        MatchManager.Instance.EnsureOffsideManager()?.EvaluateAndStore("snapshot_committed");
        shotManager.StartShotProcess(actor,"snapshot");
    }
    private void DeclineOfferedMovementSnapshot()
    {
        bool running=isDribblerRunning;if(running)CommitToAction();
        isWaitingForSnapshotDecision=false;
        UnityEngine.Debug.Log(running?"Dribbler was running. Decided to forfeit remaining pace and not shoot.":"Attacker decides not to shoot..");
        if(running || isMovementPhaseDef)AdvanceMovementPhase();
    }
}
