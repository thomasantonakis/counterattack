using System.Linq;
using CounterAttack.Online;

public enum OnlineHeaderControlIntent { Control, SelectPlayer, Roll }

public partial class HeaderManager
{
    public string OnlineControlStage=>isWaitingForControlOrHeaderDecision||isWaitingForControlOrHeaderDecisionDef?"choice"
        :iswaitingForChallengeWinnerSelection?"player":isWaitingForControlRoll?"roll":"pending";
    public PlayerToken[] OnlineControlPlayers=>GetChallengeWinnerCandidates().Where(t=>t!=null).ToArray();
    private bool IsOnlineControlContinuationCurrent(int version)=>version==controlPresentationVersion
        && (isActiveAndEnabled || MatchManager.Instance?.IsOnlineAuthorityControlled==true && gameObject.activeInHierarchy);
    public bool TryExecuteOnlineHeaderControl(OnlineHeaderControlIntent intent,PlayerToken player=null)
    {
        if(OnlineRoomSession.IsGuest || MatchManager.Instance==null || MatchManager.Instance.IsGameplayInputBlocked || !isActivated)return false;
        switch(intent)
        {
            case OnlineHeaderControlIntent.Control when isWaitingForControlOrHeaderDecision:
                attackControlBall=true;StartAttackControlSelection();return true;
            case OnlineHeaderControlIntent.Control when isWaitingForControlOrHeaderDecisionDef:
                DefenseContolBall();return true;
            case OnlineHeaderControlIntent.SelectPlayer when iswaitingForChallengeWinnerSelection && OnlineControlPlayers.Contains(player):
                OnClickReceived(player,player.GetCurrentHex());return true;
            case OnlineHeaderControlIntent.Roll when isWaitingForControlRoll && challengeWinner!=null:
                _=PerformControlRoll((RollInputOverride?)null);return true;
            default:return false;
        }
    }
}
