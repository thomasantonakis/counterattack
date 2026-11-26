using CounterAttack.Online;

public enum OnlineKeeperDistributionIntent { QuickThrow, FinalThird }

public partial class ShotManager
{
    public PlayerToken OnlineDistributionKeeper => ball?.GetCurrentHex()?.GetOccupyingToken();
    public bool OnlineCanDistribute => isWaitingForSaveandHoldScenario && OnlineDistributionKeeper!=null
        && OnlineDistributionKeeper.IsGoalKeeper && MatchManager.Instance!=null
        && MatchManager.Instance.LastTokenToTouchTheBallOnPurpose==OnlineDistributionKeeper
        && MatchManager.Instance.CanGoalkeeperHandle(OnlineDistributionKeeper);

    public bool TryExecuteOnlineKeeperDistribution(OnlineKeeperDistributionIntent intent)
    {
        if(OnlineRoomSession.IsGuest || !OnlineCanDistribute || MatchManager.Instance.IsGameplayInputBlocked)return false;
        switch(intent)
        {
            case OnlineKeeperDistributionIntent.QuickThrow:QuickThrow();return true;
            case OnlineKeeperDistributionIntent.FinalThird:ActivateFinalThirds();return true;
            default:return false;
        }
    }
}
