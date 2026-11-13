using System.Linq;
using CounterAttack.Online;

public enum OnlineHeaderIntent { FreeHeader, Nominate, Confirm, Roll, HeadedPass, GoalTarget, PassTarget }

public partial class HeaderManager
{
    public string OnlineHeaderStage => isWaitingForAttackerSelection ? "attackers"
        : isWaitingForDefenderSelection ? "defenders"
        : isWaitingForHeaderAtGoal ? "goal-choice"
        : isWaitingForHeaderRoll ? "roll"
        : isWaitingForHeaderTargetSelection ? "target" : null;
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
}
