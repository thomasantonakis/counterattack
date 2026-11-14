using CounterAttack.Online;

public enum OnlineShotIntent { HeaderSave, Handling }

public partial class ShotManager
{
    private bool IsOnlineShotContinuationCurrent(int version)=>this!=null && version==headerSavePresentationVersion
        && (isActiveAndEnabled || MatchManager.Instance?.IsOnlineAuthorityControlled==true && gameObject.activeInHierarchy);
    public string OnlineShotStage => !isActivated ? null : isHeaderAtGoal && isWaitingForGKDiceRoll ? "header.save"
        : isWaitingforHandlingTest ? "handling" : "pending";
    public PlayerToken OnlineShotActor => isWaitingForGKDiceRoll || isWaitingforHandlingTest ? hexGrid.GetDefendingGK() : shooter;

    public bool TryExecuteOnlineShot(OnlineShotIntent intent)
    {
        if(OnlineRoomSession.IsGuest || MatchManager.Instance==null || MatchManager.Instance.IsGameplayInputBlocked)return false;
        switch(intent)
        {
            case OnlineShotIntent.HeaderSave when OnlineShotStage=="header.save":
                PerformGKHeaderSave((RollInputOverride?)null);return true;
            case OnlineShotIntent.Handling when OnlineShotStage=="handling":
                StartCoroutine(ResolveHandlingTest((RollInputOverride?)null));return true;
            default:return false;
        }
    }
}
