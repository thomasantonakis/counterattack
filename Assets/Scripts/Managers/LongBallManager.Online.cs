using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using CounterAttack.Online;

public enum OnlineLongBallIntent { Target, Accuracy, Direction, Distance, Interception, GoalkeeperMove, GoalkeeperDecline, GoalkeeperClaim }

public partial class LongBallManager
{
    private string onlineTargetFingerprint;
    private HexCell[] onlineTargets = Array.Empty<HexCell>();
    private string onlineRollingPhase;
    private int onlineRollingVersion;
    private int onlineFlightVersion;
    private bool onlineFlightInProgress;
    public bool OnlineFlightInProgress => onlineFlightInProgress && onlineFlightVersion == longBallResolutionVersion;

    private string OnlineRawStage => OnlineFlightInProgress || isResolvingGkPaceMove || isResolvingLandingInterception ? "resolving"
        : isWaitingForDefLBMove ? "keeper" : isWaitingForInterceptionRoll ? "interception"
        : isWaitingForAccuracyRoll ? "accuracy" : isWaitingForDirectionRoll ? "direction"
        : isWaitingForDistanceRoll ? "distance" : isAwaitingTargetSelection ? "target" : "resolving";
    public string OnlineDecisionStage => onlineRollingVersion == longBallResolutionVersion && OnlineRawStage == onlineRollingPhase ? "resolving" : OnlineRawStage;
    public bool OnlineGoalkeeperCanClaim => CanGoalkeeperClaimLongBallLandingHex();
    public HexCell[] OnlineGoalkeeperDestinations => defenderLongBallMoveHexes.ToArray();

    // Pure preview. It shares the existing optimized candidate/path rules and
    // does not start the Editor/input precompute coroutine or alter highlights.
    public HexCell[] GetLegalOnlineLongPassTargets()
    {
        var source = ball?.GetCurrentHex();
        if (source == null || hexGrid?.cells == null) return Array.Empty<HexCell>();
        var occupied = hexGrid.cells.Cast<HexCell>().Where(h => h != null && (h.isAttackOccupied || h.isDefenseOccupied));
        string key = hexGrid.GetEntityId() + ":" + source.coordinates + ":" + ball.ballRadius + ":"
            + string.Join(";", occupied.Select(h => h.coordinates + ":" + h.isAttackOccupied + ":" + h.isDefenseOccupied));
        if (key != onlineTargetFingerprint)
        {
            var candidates = BuildInboundsLongBallCandidates();
            var defenders = hexGrid.GetDefenderHexes(); var attackers = hexGrid.GetAttackerHexes();
            candidates.ExceptWith(defenders); candidates.ExceptWith(hexGrid.GetDefenderNeighbors(defenders));
            candidates.ExceptWith(attackers); candidates.ExceptWith(hexGrid.GetAttackerHexesinRange(attackers, 5));
            var blockers = BuildAdjacentDefenderBlockers(source);
            onlineTargets = candidates.Where(h => !IsLongBallPathBlockedByAdjacentDefender(source,h,blockers))
                .OrderBy(h=>h.coordinates.x).ThenBy(h=>h.coordinates.z).ToArray();
            onlineTargetFingerprint = key;
        }
        return onlineTargets.ToArray();
    }

    public bool TryExecuteOnlineLongBall(OnlineLongBallIntent intent, HexCell target = null)
    {
        if (OnlineRoomSession.IsGuest || MatchManager.Instance == null || MatchManager.Instance.IsGameplayInputBlocked
            || !isActivated || ball.isMoving || ball.IsCellMoveInProgress || OnlineDecisionStage == "resolving") return false;
        switch (intent)
        {
            case OnlineLongBallIntent.Target when isAwaitingTargetSelection && ValidateLongBallTargetForPreview(target).isValid:
                HandleLongBallProcess(target); return true;
            case OnlineLongBallIntent.Accuracy when isWaitingForAccuracyRoll:
                StartOnlineRoll("accuracy",PresentAccuracyRoll(null)); return true;
            case OnlineLongBallIntent.Direction when isWaitingForDirectionRoll:
                StartOnlineRoll("direction",PresentInaccuracyDirection(null)); return true;
            case OnlineLongBallIntent.Distance when isWaitingForDistanceRoll:
                StartOnlineRoll("distance",PerformDistanceRoll((RollInputOverride?)null)); return true;
            case OnlineLongBallIntent.Interception when isWaitingForInterceptionRoll && !isResolvingLandingInterception:
                StartOnlineRoll("interception",PerformInterceptionCheck(finalHex,(RollInputOverride?)null)); return true;
            case OnlineLongBallIntent.GoalkeeperMove when isWaitingForDefLBMove && !isResolvingGkPaceMove && defenderLongBallMoveHexes.Contains(target):
                _ = MoveGKForLB(target); return true;
            case OnlineLongBallIntent.GoalkeeperClaim when isWaitingForDefLBMove && !isResolvingGkPaceMove && CanGoalkeeperClaimLongBallLandingHex():
                _ = MoveGKForLB(finalHex); return true;
            case OnlineLongBallIntent.GoalkeeperDecline when isWaitingForDefLBMove && !isResolvingGkPaceMove:
                hexGrid.ClearHighlightedHexes(); defenderLongBallMoveHexes.Clear(); isWaitingForDefLBMove=false; return true;
            default: return false;
        }
    }

    private void StartOnlineRoll(string phase,IEnumerator resolution)
    { onlineRollingPhase=phase;onlineRollingVersion=longBallResolutionVersion;StartCoroutine(ResolveOnlineRoll(phase,onlineRollingVersion,resolution)); }
    private IEnumerator ResolveOnlineRoll(string phase,int version,IEnumerator resolution)
    {
        try { yield return resolution; }
        finally { if(onlineRollingPhase==phase && onlineRollingVersion==version) onlineRollingPhase=null; }
    }
}
