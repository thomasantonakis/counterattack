using System.Linq;
using System.Collections;
using CounterAttack.Online;

public enum OnlineHeaderIntent { FreeHeader, Nominate, Confirm, Roll, HeadedPass, GoalTarget, PassTarget, KeeperHands, KeeperFeet, KeeperCatch, Interception }

public partial class HeaderManager
{
    private int onlineInterceptionVersion=-1;
    private bool OnlineHeaderInterceptionBusy=>onlineInterceptionVersion==challengePresentationVersion;
    public string OnlineHeaderStage => isWaitingForInterceptionRoll ? OnlineHeaderInterceptionBusy ? "interception.pending" : "interception"
        : isWaitingForHeadedBackPassChoice ? "backpass.choice"
        : isWaitingForHeadedBackPassRoll ? headedBackPassRollInFlight ? "backpass.pending" : "backpass.roll"
        : isWaitingForAttackerSelection ? "attackers"
        : isWaitingForDefenderSelection ? "defenders"
        : isWaitingForHeaderAtGoal ? "goal-choice"
        : isWaitingForHeaderRoll ? "roll"
        : isWaitingForHeaderTargetSelection ? "target" : null;
    public PlayerToken OnlineHeaderActor => isWaitingForInterceptionRoll ? interceptingDefender
        : isWaitingForHeadedBackPassChoice || isWaitingForHeadedBackPassRoll ? headedBackPassKeeper
        : isWaitingForHeaderRoll ? tokenRolling : challengeWinner;
    public PlayerToken[] OnlineHeaderPlayers => (isWaitingForAttackerSelection ? attEligibleToHead : defEligibleToHead)
        .Where(t => t != null).ToArray();
    public bool OnlineHeaderNominated(PlayerToken player) => (isWaitingForAttackerSelection ? attackerWillJump : defenderWillJump).Contains(player);
    public HexCell[] OnlineHeaderTargets => (isWaitingForHeaderAtGoal ? GetHeaderAtGoalDecisionTargets()
        : hexGrid.highlightedHexes.Where(h => h != null && !h.isDefenseOccupied && headerTargetThreatByHex.ContainsKey(h))).ToArray();

    public bool TryExecuteOnlineHeader(OnlineHeaderIntent intent, PlayerToken player = null, HexCell target = null)
    {
        if (OnlineRoomSession.IsGuest || MatchManager.Instance == null || MatchManager.Instance.IsGameplayInputBlocked || !isActivated) return false;
        switch (intent)
        {
            case OnlineHeaderIntent.Interception when isWaitingForInterceptionRoll && !OnlineHeaderInterceptionBusy && interceptingDefender!=null:
                onlineInterceptionVersion=challengePresentationVersion;
                StartCoroutine(ResolveOnlineHeaderInterception(challengePresentationVersion));return true;
            case OnlineHeaderIntent.KeeperHands when isWaitingForHeadedBackPassChoice && headedBackPassKeeper!=null:
                ChooseHeadedBackPassHands(true); return true;
            case OnlineHeaderIntent.KeeperFeet when isWaitingForHeadedBackPassChoice && headedBackPassKeeper!=null:
                ChooseHeadedBackPassHands(false); return true;
            case OnlineHeaderIntent.KeeperCatch when isWaitingForHeadedBackPassRoll && !headedBackPassRollInFlight && headedBackPassKeeper!=null:
                PerformHeadedBackPassRoll(); return true;
            case OnlineHeaderIntent.FreeHeader when isWaitingForControlOrHeaderDecision:
                attackFreeHeader = true; _ = StartAttackHeaderSelection(); return true;
            case OnlineHeaderIntent.FreeHeader when isWaitingForControlOrHeaderDecisionDef:
                DefenseFreeHeader(); return true;
            case OnlineHeaderIntent.Nominate when (isWaitingForAttackerSelection || isWaitingForDefenderSelection) && OnlineHeaderPlayers.Contains(player):
                OnClickReceived(player, player.GetCurrentHex()); return true;
            case OnlineHeaderIntent.Confirm when isWaitingForAttackerSelection && attackerWillJump.Count > 0:
                _ = ConfirmAttackerHeaderSelection(); return true;
            case OnlineHeaderIntent.Confirm when isWaitingForDefenderSelection:
                ConfirmDefenderHeaderSelection(); return true;
            case OnlineHeaderIntent.Roll when isWaitingForHeaderRoll && tokenRolling != null:
                PerformHeaderRoll((RollInputOverride?)null); return true;
            case OnlineHeaderIntent.HeadedPass when isWaitingForHeaderAtGoal:
                isWaitingForHeaderAtGoal = false; headerAtGoalDeclared = false; ClearHeaderAtGoalTargets(); return true;
            case OnlineHeaderIntent.GoalTarget when isWaitingForHeaderAtGoal && OnlineHeaderTargets.Contains(target):
                HandleHeaderAtGoalClick(target); return true;
            case OnlineHeaderIntent.PassTarget when isWaitingForHeaderTargetSelection && OnlineHeaderTargets.Contains(target):
                OnClickReceived(target.GetOccupyingToken(), target); return true;
            default: return false;
        }
    }
    private IEnumerator ResolveOnlineHeaderInterception(int version)
    {
        try { yield return PresentHeaderInterception(null); }
        finally { if(onlineInterceptionVersion==version)onlineInterceptionVersion=-1; }
    }
}
