using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public sealed class RoomAIPassRacePreview
{
    public string collectorTokenKey;
    public int collectionPhases;
    public int pickupCost;
    public int remainingPace;
    public int continuationHexes;
    public int safeContinuationHexes;
    public int crossedZones;
    public bool opponentCollectsFirst;
    public float pickupSurvival = 1f;
    public float continuationSurvival = 1f;
    public float recoveryProbability;
    public float routeExpectedGoals = -1f;
    public float bestScoringRouteExpectedGoals = -1f;
    public RoomHexCoordinates bestContinuationHex;
}

/// <summary>Collection cost, route interactions and defender reach share one phase horizon.</summary>
public static class RoomAIPassRace
{
    public static bool IsWithinDefenderReach(RoomAITokenSnapshot defender, RoomHexCoordinates hex, int phases)
    {
        return defender?.currentHex != null && RoomAITacticalSafety.Distance(defender.currentHex, hex)
            <= Math.Max(0, defender.pace) * Math.Max(0, phases) + 1;
    }
    public static RoomAIPassRacePreview Evaluate(RoomAIDecisionSnapshot snapshot, PlayerToken collector,
        HexCell landing, bool alreadyOnLanding = false)
    {
        MatchManager match = MatchManager.Instance;
        HexGrid grid = match?.hexGrid;
        if (snapshot == null || collector == null || landing == null || grid == null) return null;
        HexCell start = collector.GetCurrentHex();
        if (start == null) return null;
        var vacated = new HashSet<HexCell> { start };
        List<HexCell> pickupPath = alreadyOnLanding || start == landing ? new List<HexCell>()
            : HexGridUtils.FindPath(start, landing, grid, vacated);
        if (!alreadyOnLanding && start != landing && pickupPath.Count == 0) return null;
        int cost = pickupPath.Count;
        int pace = Math.Max(1, collector.pace);
        int phases = Math.Max(1, (cost + pace - 1) / pace);
        var result = new RoomAIPassRacePreview
        {
            collectorTokenKey = (collector.isHomeTeam ? "Home:" : "Away:") + collector.jerseyNumber,
            pickupCost = cost, collectionPhases = phases, remainingPace = phases * pace - cost
        };
        var opponents = match.playerTokenManager.allTokens.Where(t => t != null && t.isPlaying
            && t.isHomeTeam != collector.isHomeTeam && t.GetCurrentHex() != null).ToList();
        int defensesBeforeCollection = phases - 1;
        foreach (var opponent in opponents)
        {
            var path = HexGridUtils.FindPath(opponent.GetCurrentHex(), landing, grid, vacated);
            if (!alreadyOnLanding && opponent.GetCurrentHex() == landing || defensesBeforeCollection > 0
                && path.Count > 0 && path.Count <= opponent.pace * defensesBeforeCollection)
                result.opponentCollectsFirst = true;
        }
        float RouteSurvival(IReadOnlyList<HexCell> path, out int zones)
        {
            zones = 0; float survival = 1f;
            foreach (var opponent in opponents)
            {
                if (!path.Any(hex => HexGridUtils.GetHexStepDistance(opponent.GetCurrentHex(), hex) == 1)) continue;
                zones++;
                survival *= ExpectedStatsCalculator.CalculateMovementInterceptionExpectation(opponent).continuationProbability;
            }
            return survival;
        }
        // Before pickup this is an off-ball runner, not a dribbler. Crossing a
        // ZOI cannot trigger movement interceptions until the runner has the ball.
        result.pickupSurvival = result.opponentCollectsFirst ? 0f : 1f;
        int bestZones = 0;
        float bestValue = float.NegativeInfinity;
        int defensivePhases = Math.Max(1, phases);
        string reachKey = $"{start.GetEntityId()}|{defensivePhases}|{collector.isHomeTeam}";
        if (!snapshot.passRaceDefensiveReachCache.TryGetValue(reachKey, out var defensiveReach))
        {
            defensiveReach = opponents.ToDictionary(token => token,
                token => HexGridUtils.GetReachableHexes(grid, token.GetCurrentHex(), token.pace * defensivePhases, vacated)
                    .reachableHexes.ToHashSet());
            snapshot.passRaceDefensiveReachCache[reachKey] = defensiveReach;
        }
        var routes = BuildContinuationRoutes(grid, landing, result.remainingPace, vacated);
        foreach (var entry in routes)
        {
            HexCell endpoint = entry.Key;
            if (endpoint == null || endpoint.isOutOfBounds && endpoint.isInGoal == 0) continue;
            List<HexCell> route = entry.Value;
            result.continuationHexes++;
            float survival = RouteSurvival(route, out int zones);
            float strongestLoss = 0f;
            // If pickup used two attacking phases, opponents had one movement
            // before it and get another before the following attacking action.
            bool safe = zones == 0;
            foreach (PlayerToken opponent in opponents)
            {
                if (HexGridUtils.GetHexStepDistance(opponent.GetCurrentHex(), endpoint) > opponent.pace * defensivePhases + 1) continue;
                bool canTackle = endpoint.GetNeighbors(grid).Any(hex =>
                {
                    if (hex == null || hex.isOutOfBounds) return false;
                    return hex == opponent.GetCurrentHex() || defensiveReach[opponent].Contains(hex);
                });
                if (!canTackle) continue;
                safe = false;
                strongestLoss = Math.Max(strongestLoss,
                    ExpectedStatsCalculator.CalculateGroundDuelExpectationWithDefenderSkill(collector, opponent, opponent.tackling).xTackles);
            }
            if (safe && !result.opponentCollectsFirst) result.safeContinuationHexes++;
            var support = match.playerTokenManager.allTokens.Where(t => t != null && t != collector && t.isPlaying
                && t.isHomeTeam == collector.isHomeTeam && t.GetCurrentHex() != null
                && HexGridUtils.GetHexStepDistance(t.GetCurrentHex(), endpoint) <= 2).ToArray();
            float recovery = support.Length == 0 ? 0f : ExpectedStatsCalculator.CalculateAtLeastOneRecoveryProbability(support);
            float effectiveSurvival = survival * (1f - strongestLoss * (1f - recovery));
            float xg = -1f;
            int goalSide = (collector.isHomeTeam ? match.homeTeamDirection : match.awayTeamDirection)
                == MatchManager.TeamAttackingDirection.LeftToRight ? 1 : -1;
            // A first-phase run into the box may Snapshot before DefMP. Do not
            // let a defender spend a future full Pace to erase that opportunity.
            // Current route interceptions still apply through `survival`.
            if (phases == 1 && endpoint.isInPenaltyBox == goalSide)
            {
                strongestLoss = 0f;
                effectiveSurvival = survival;
            }
            if (match.shotManager != null && endpoint.CanShootFrom && endpoint.ShootingPaths != null
                && endpoint.ShootingPaths.Keys.Any(goal => goal != null && goal.isInGoal == goalSide))
            {
                string shotKey = $"pass-race|{collector.GetEntityId()}|{endpoint.GetEntityId()}";
                if (!snapshot.expectedGoalsCache.TryGetValue(shotKey, out var shot))
                {
                    shot = new RoomAIExpectedGoalsPreview();
                    shot.calculated = match.shotManager.TryCalculateBestExpectedGoalsPreview(collector, endpoint, "snapshot",
                        out shot.expectedGoals, out shot.shootingPenalty, out shot.blockerCount, out shot.goalkeeperPenalty);
                    snapshot.expectedGoalsCache[shotKey] = shot;
                }
                if (shot.calculated) xg = shot.expectedGoals;
            }
            // Recovery is possession value, not permission for the original
            // collector to take the advertised shot after losing the ball.
            float routeXg = xg < 0f ? -1f : result.pickupSurvival * survival * (1f - strongestLoss) * xg;
            result.bestScoringRouteExpectedGoals = Math.Max(result.bestScoringRouteExpectedGoals, routeXg);
            float value = (safe ? .15f : 0f) + effectiveSurvival + Math.Max(0f, routeXg) * 4f;
            if (value <= bestValue) continue;
            bestValue = value;
            result.continuationSurvival = effectiveSurvival;
            result.recoveryProbability = recovery;
            result.routeExpectedGoals = routeXg;
            result.bestContinuationHex = RoomHexCoordinates.FromHex(endpoint);
            bestZones = zones;
        }
        result.crossedZones = bestZones;
        if (result.continuationHexes == 0) result.continuationSurvival = 0f;
        return result;
    }

    private static Dictionary<HexCell, List<HexCell>> BuildContinuationRoutes(HexGrid grid, HexCell start, int budget,
        ISet<HexCell> vacated)
    {
        var paths = new Dictionary<HexCell, List<HexCell>> { [start] = new List<HexCell>() };
        var frontier = new Queue<HexCell>(); frontier.Enqueue(start);
        while (frontier.Count > 0)
        {
            HexCell current = frontier.Dequeue();
            if (paths[current].Count >= budget || current.isInGoal != 0) continue;
            foreach (HexCell neighbor in current.GetNeighbors(grid))
            {
                if (neighbor == null || paths.ContainsKey(neighbor) || neighbor.isOutOfBounds && neighbor.isInGoal == 0
                    || (neighbor.isAttackOccupied || neighbor.isDefenseOccupied) && !vacated.Contains(neighbor)) continue;
                paths[neighbor] = new List<HexCell>(paths[current]) { neighbor };
                frontier.Enqueue(neighbor);
            }
        }
        return paths;
    }
}
