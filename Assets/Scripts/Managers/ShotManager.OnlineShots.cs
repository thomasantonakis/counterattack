using CounterAttack.Online;
using System.Linq;
using System.Collections;

public enum OnlineShotIntent { Select, Confirm, CancelPreview, Target, SelectBlocker, MoveBlocker, Shoot, Save, HeaderSave, Handling, Block }

public partial class ShotManager
{
    private int onlineShotBlockVersion=-1;
    private bool IsOnlineShotContinuationCurrent(int version)=>this!=null && version==headerSavePresentationVersion
        && (isActiveAndEnabled || MatchManager.Instance?.IsOnlineAuthorityControlled==true && gameObject.activeInHierarchy);
    private bool OnlineShotContextBlocked=>MatchManager.Instance?.IsWaitingForExtraActionsRoll==true || finalThirdManager?.isActivated==true
        || !isActivated && MatchManager.Instance?.currentState==MatchManager.GameState.FreeKickExecution;
    public bool OnlineCanSelectShot=>isAvailable && !isActivated && !IsMovingSnapshotBlocker && !OnlineShotContextBlocked
        && MatchManager.Instance!=null && !IsSnapshotSuppressedByFinalExtraMovement()
        && CanStartShotFromCurrentPossession(MatchManager.Instance.LastTokenToTouchTheBallOnPurpose);
    public string OnlineShotStage => OnlineShotContextBlocked ? "pending" : isWaitingForShotCommitConfirmation && !isActivated ? "confirm" : !isActivated ? null
        : IsMovingSnapshotBlocker ? "resolving" : isWaitingforBlockerSelection ? "snapshot.select" : isWaitingforBlockerMovement ? "snapshot.target"
        : isWaitingForTargetSelection ? "target" : isWaitingForBlockDiceRoll ? onlineShotBlockVersion==headerSavePresentationVersion ? "pending" : "block"
        : isWaitingForShotRoll ? "roll"
        : isWaitingForGKDiceRoll ? isHeaderAtGoal ? "header.save" : "save" : isWaitingforHandlingTest ? "handling" : "pending";
    public PlayerToken OnlineShotActor => isWaitingForBlockDiceRoll ? currentShotInteraction?.defender
        : isWaitingForGKDiceRoll || isWaitingforHandlingTest ? hexGrid.GetDefendingGK()
        : isWaitingforBlockerSelection || isWaitingforBlockerMovement || IsMovingSnapshotBlocker ? tokenMoveforDeflection
        : shooter ?? MatchManager.Instance?.LastTokenToTouchTheBallOnPurpose;
    public PlayerToken[] OnlineSnapshotBlockers=>GetSelectableSnapshotBlockers()
        .Where(t=>isWaitingforBlockerSelection || CanSwitchSnapshotBlocker(t)).ToArray();
    public HexCell[] OnlineSnapshotDestinations=>tokenMoveforDeflection==null?System.Array.Empty<HexCell>()
        :GetValidSnapshotBlockerDestinations(tokenMoveforDeflection).ToArray();
    public HexCell[] OnlineShotTargets=>shotTargetSelectionTargets.Where(h=>h!=null && GetShotOriginHex()?.ShootingPaths?.ContainsKey(h)==true).ToArray();

    public bool TryExecuteOnlineShot(OnlineShotIntent intent,PlayerToken player=null,HexCell target=null)
    {
        if(OnlineRoomSession.IsGuest || MatchManager.Instance==null || MatchManager.Instance.IsGameplayInputBlocked || OnlineShotContextBlocked)return false;
        switch(intent)
        {
            case OnlineShotIntent.Block when OnlineShotStage=="block" && currentShotInteraction!=null && !currentShotInteraction.IsGK:
                onlineShotBlockVersion=headerSavePresentationVersion;StartCoroutine(ResolveOnlineShotBlock(headerSavePresentationVersion));return true;
            case OnlineShotIntent.Select when OnlineCanSelectShot && !isWaitingForShotCommitConfirmation:
                BeginOnlineShotSelection();return true;
            case OnlineShotIntent.Confirm when OnlineShotStage=="confirm" && OnlineCanSelectShot:
                isWaitingForShotCommitConfirmation=false;ClearShotCommitPreview();IdentifyShotType();return true;
            case OnlineShotIntent.CancelPreview when OnlineShotStage=="confirm":
                CancelShotCommitPreview();return true;
            case OnlineShotIntent.Target when OnlineShotStage=="target" && OnlineShotTargets.Contains(target):
                HandleTargetClick(target);return true;
            case OnlineShotIntent.SelectBlocker when (OnlineShotStage is "snapshot.select" or "snapshot.target") && OnlineSnapshotBlockers.Contains(player):
                SelectSnapshotBlocker(player);return true;
            case OnlineShotIntent.MoveBlocker when OnlineShotStage=="snapshot.target" && OnlineSnapshotDestinations.Contains(target):
                _=HandleClicksForSnapMovement(null,target);return true;
            case OnlineShotIntent.Shoot when OnlineShotStage=="roll":
                StartCoroutine(StartShotRoll((RollInputOverride?)null));return true;
            case OnlineShotIntent.Save when OnlineShotStage=="save" && interceptors.Count>0 && interceptors[0].IsGK:
                StartCoroutine(ResolveGKSavingAttempt(interceptors[0],(RollInputOverride?)null));return true;
            case OnlineShotIntent.HeaderSave when OnlineShotStage=="header.save":
                PerformGKHeaderSave((RollInputOverride?)null);return true;
            case OnlineShotIntent.Handling when OnlineShotStage=="handling":
                StartCoroutine(ResolveHandlingTest((RollInputOverride?)null));return true;
            default:return false;
        }
    }
    private IEnumerator ResolveOnlineShotBlock(int version)
    {
        try { yield return StartShotBlockRoll((RollInputOverride?)null); }
        finally { if(onlineShotBlockVersion==version)onlineShotBlockVersion=-1; }
    }
    private void BeginOnlineShotSelection()
    {
        var match=MatchManager.Instance;
        if(isWaitingForSnapshotDecisionFromLoose)
        {
            isWaitingForSnapshotDecisionFromLoose=false;isWaitingForShotCommitConfirmation=false;ClearShotCommitPreview();
            StartShotProcess(match.LastTokenToTouchTheBallOnPurpose,"snapshot");return;
        }
        if(movementPhaseManager?.isCommitted!=true)match.ClearNonShotActionPreviews();
        if(ShouldRequireShotCommitConfirmation())
        {
            match.RecordActionSelection("shot",match.LastTokenToTouchTheBallOnPurpose,ball?.GetCurrentHex(),
                new System.Collections.Generic.Dictionary<string,string>{{"selection","preview"}});
            PreviewShotCommit();
        }
        else IdentifyShotType();
    }
}
