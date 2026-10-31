using System.Collections;
using System.Collections.Generic;

/// <summary>Single-board cache. Never shared with automated decisions or another match.</summary>
public sealed class RoomAIHumanShadowCache
{
    public string boardKey;
    public readonly Dictionary<string, RoomAICandidateExplanation> explanations = new();
    private readonly Dictionary<string, RoomAIExpectedGoalsPreview> goals = new();
    private readonly Dictionary<string, RoomAIReachabilityPreview> reachability = new();
    private readonly Dictionary<string, RoomAIPassRacePreview> races = new();
    private readonly Dictionary<string, Dictionary<PlayerToken, HashSet<HexCell>>> defensiveReach = new();

    public void Remember(RoomAIDecisionSnapshot snapshot)
    {
        foreach (var pair in snapshot.passRaceCache) races[pair.Key] = pair.Value;
        foreach (var pair in snapshot.expectedGoalsCache) goals[pair.Key] = pair.Value;
        foreach (var pair in snapshot.reachabilityCache) reachability[pair.Key] = pair.Value;
        foreach (var pair in snapshot.passRaceDefensiveReachCache) defensiveReach[pair.Key] = pair.Value;
    }

    public void Attach(RoomAIDecisionSnapshot snapshot)
    {
        snapshot.expectedGoalsCache = goals;
        snapshot.reachabilityCache = reachability;
        snapshot.passRaceCache = races;
        snapshot.passRaceDefensiveReachCache = defensiveReach;
    }

    public static string CandidateKey(RoomActionCandidate action)
    {
        bool passTarget = action.actionType == RoomActionType.GroundPass
            && action.step is RoomDecisionStep.ChooseTarget or RoomDecisionStep.Confirm;
        var step = passTarget ? RoomDecisionStep.ChooseTarget : action.step;
        var hex = action.targetHex ?? action.targetToken?.GetCurrentHex();
        string Token(PlayerToken token) => token == null ? "none" : token.GetEntityId().ToString();
        return $"{action.manager}|{action.actionType}|{step}|{Token(action.actor)}|{Token(action.targetToken)}|"
            + $"{hex?.coordinates.x},{hex?.coordinates.z}|{(passTarget ? "" : action.key)}";
    }

    /// <summary>Yield before doing any work, then yield whenever the budget is
    /// spent. A step is indivisible; the budget is cooperative, not a hard cap.</summary>
    public static IEnumerator InFrameBatches(IEnumerator work, double budgetMilliseconds,
        System.Func<double> milliseconds)
    {
        try
        {
            yield return null;
            while (true)
            {
                double start = milliseconds();
                do
                {
                    if (!work.MoveNext()) yield break;
                }
                while (milliseconds() - start < budgetMilliseconds);
                yield return null;
            }
        }
        finally { (work as System.IDisposable)?.Dispose(); }
    }
}
