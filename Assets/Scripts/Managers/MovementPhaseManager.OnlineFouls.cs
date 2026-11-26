public enum OnlineFoulIntent { Leniency, Injury, Advantage, Take }

public partial class MovementPhaseManager
{
    private int? onlineFoulVersion;
    public string OnlineFoulStage=>!isActivated || onlineFoulVersion!=duelPresentationVersion?null
        :isWaitingForYellowCardRoll?"leniency":isWaitingForInjuryRoll?"injury":isWaitingForFoulDecision?"choice":"resolving";
    public PlayerToken OnlineFoulActor=>isWaitingForYellowCardRoll?selectedDefender:ResolveFouledAttacker();
    public bool OnlineCanPlayAdvantage=>isWaitingForFoulDecision && !ShouldForceFreeKickAfterFoul(ResolveFouledAttacker(),selectedDefender);
    public bool TryExecuteOnlineFoul(OnlineFoulIntent intent)
    {
        var match=MatchManager.Instance;
        if(match==null || !match.IsOnlineAuthorityControlled || match.IsGameplayInputBlocked || OnlineFoulStage==null)return false;
        switch(intent)
        {
            case OnlineFoulIntent.Leniency:
                if(OnlineFoulStage!="leniency")return false;
                PerformLeniencyTest((RollInputOverride?)null);return true;
            case OnlineFoulIntent.Injury:
                if(OnlineFoulStage!="injury")return false;
                PerformInjuryTest((RollInputOverride?)null);return true;
            case OnlineFoulIntent.Advantage:
                if(!OnlineCanPlayAdvantage)return false;
                _=PlayAdvantage();return true;
            case OnlineFoulIntent.Take:
                if(OnlineFoulStage!="choice")return false;
                TakeAwardedFoul();return true;
            default:return false;
        }
    }
}
