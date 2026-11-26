using System.Collections;
using CounterAttack.Online;

public enum OnlineLooseBallIntent { Direction, Distance, Recovery }

public partial class LooseBallManager
{
    private int onlineLoosePhaseVersion, onlineLooseRollVersion=-1;
    private bool IsLoosePhaseCurrent(int version)=>version==onlineLoosePhaseVersion && isActivated;
    public string OnlineDecisionStage => !isActivated || onlineLooseRollVersion==onlineLoosePhaseVersion ? "resolving"
        : isWaitingForDirectionRoll ? "direction" : isWaitingForDistanceRoll ? "distance"
        : isWaitingForInterceptionRoll ? "recovery" : "resolving";
    public PlayerToken OnlineRollActor => isWaitingForInterceptionRoll ? potentialInterceptor : causingDeflection;

    public bool TryExecuteOnlineLooseBall(OnlineLooseBallIntent intent)
    {
        if(OnlineRoomSession.IsGuest || MatchManager.Instance==null || MatchManager.Instance.IsGameplayInputBlocked || OnlineRollActor==null)return false;
        IEnumerator routine=intent switch
        {
            OnlineLooseBallIntent.Direction when OnlineDecisionStage=="direction"=>PresentDirectionRoll(null),
            OnlineLooseBallIntent.Distance when OnlineDecisionStage=="distance"=>PresentDistanceRoll(null),
            OnlineLooseBallIntent.Recovery when OnlineDecisionStage=="recovery"=>PresentInterceptionRoll(null),
            _=>null
        };
        if(routine==null)return false;
        onlineLooseRollVersion=onlineLoosePhaseVersion;
        StartCoroutine(ResolveOnlineLooseBall(routine,onlineLoosePhaseVersion));return true;
    }
    private IEnumerator ResolveOnlineLooseBall(IEnumerator routine,int version)
    {
        try { yield return routine; }
        finally { if(onlineLooseRollVersion==version)onlineLooseRollVersion=-1; }
    }
}
