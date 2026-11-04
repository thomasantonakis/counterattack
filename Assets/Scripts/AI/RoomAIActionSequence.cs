using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public sealed class RoomAISequenceStep
{
    public string action;
    public string actorTokenKey;
    public RoomHexCoordinates ballHex;
    public int movementCost;
    public int remainingPace;
    public float conditionalProbability = 1f;
}

[Serializable]
public sealed class RoomAIActionSequencePreview
{
    public List<RoomAISequenceStep> steps = new();
    public RoomAIPossessionChainSummary outcomes;
    public RoomAIDefensiveResponsePreview defensiveResponse;
    public RoomAISecondaryPreview secondary;
    public bool secondaryExpanded;
    public bool usesProjectedAerialFreeMoves;
    public List<string> aerialFreeMoveRespondersEvaluated = new();
    public List<string> aerialAttackingPreparationsEvaluated = new();
    public List<string> aerialHeaderParticipantTokenKeys = new();
    public float accuracyProbability = -1f;
    public RoomAIPossessionChainSummary accurateOutcomes;
    public RoomAIPossessionChainSummary scatterConditionalOutcomes;
    public List<RoomAIScatterOutcome> scatterOutcomes = new();
    public int alternativesEvaluated;
    public bool searchBounded = true;
    public string horizon = "Reception, fresh AttMP, bounded DefMP, scatter/shot/rebound and tackle-recovery branches; one follow-up Snapshot; HP attacking and defending free moves, GK rush/F3/later rebounds/tackle ties/goal exits/clock uncertainty unexpanded";
    public float value;
    internal HashSet<PlayerToken> headerParticipants;
    internal Dictionary<PlayerToken, HexCell> aerialProjectedPositions;
    internal Dictionary<PlayerToken, HexCell> aerialPreLandingPositions;
    internal float aerialOutletPreference;
}

[Serializable]
public sealed class RoomAIScatterOutcome
{
    public List<string> headerParticipantTokenKeys = new();
    public int directionIndex;
    public int distance;
    public float conditionalProbability = 1f / 36f;
    public float probability;
    public RoomHexCoordinates landing;
    public bool isOutOfBounds;
    public string resolution;
    public string firstPossibleOpponentCollector;
    public List<RoomAISequenceStep> steps = new();
    public RoomAIPossessionChainSummary outcomes;
    public RoomAISecondaryPreview secondary;
    internal HexCell landingHex;
}

[Serializable]
public sealed class RoomAIReboundBranch
{
    public string deflectorTokenKey;
    public LooseBallSourceType source;
    public RoomHexCoordinates origin;
    public RoomHexCoordinates landing;
    public int direction;
    public int distance;
    public float probability;
    public string collectorTokenKey;
    public int collectionCost = -1;
    public int remainingPace = -1;
    public string resolution;
    public RoomAIPossessionChainSummary outcomes;
}

[Serializable]
public sealed class RoomAITackleExit
{
    public RoomHexCoordinates hex;
    public List<string> eligibleStealers = new();
    public float recoveryProbability;
}

[Serializable]
public sealed class RoomAISecondaryPreview
{
    public float primaryGoalProbability;
    public float heldProbability;
    public float offTargetProbability;
    public float shotReachProbability;
    public float unexpandedReboundProbability;
    public List<RoomAIReboundBranch> rebounds = new();
    public List<RoomAITackleExit> tackleExits = new();
    public RoomHexCoordinates chosenTackleExit;
    public float successfulTackleProbability;
    public float postTackleRecoveryProbability;
    public RoomAIPossessionChainSummary postTackleOutcomes;
    public string scope = "One rebound collection and at most one Snapshot with a bounded two-hex reply; successful tackle exits/steals; later rebounds, goal exits, GK checkpoints and unknown clocks unexpanded";
}

public static class RoomAISecondaryOutcomes
{
    private static string Key(PlayerToken t) => MatchManager.GetStableTokenKey(t);
    private static int Distance(HexCell a, HexCell b) => HexGridUtils.GetHexStepDistance(a, b);
    public static int[] HandlingSpillDirections(int boxSide) => boxSide == 1 ? new[] { 1, 2 }
        : boxSide == -1 ? new[] { 4, 5 } : Array.Empty<int>();
    public static bool CanForecastFollowupSnapshot(RoomClockSnapshot clock) => clock != null && !clock.isMatchComplete
        && !clock.isWaitingForExtraActionsRoll && (!clock.isHalfExpired || clock.extraActionsDetermined && clock.extraActionsRemaining > 1);

    public static RoomAISecondaryPreview PreviewPostTackleRecovery(MatchManager match, PlayerToken loser, PlayerToken winner,
        Dictionary<PlayerToken, HexCell> board)
    {
        var result = new RoomAISecondaryPreview();
        result.postTackleOutcomes = PostTackleRecovery(match, loser, winner, board, result);
        return result;
    }

    public static RoomAIActionSequencePreview Expand(RoomAIDecisionSnapshot snapshot, RoomAIActionSequencePreview preview, HexCell exactGoal = null,
        bool liveShotContext = false)
    {
        var match = MatchManager.Instance;
        if (preview == null || preview.secondaryExpanded || match?.playerTokenManager == null || match.hexGrid == null) return preview;
        preview.secondaryExpanded = true;
        var ledger = new RoomAISecondaryPreview();
        var board = match.playerTokenManager.allTokens.Where(t => t != null && t.isPlaying && !t.isSentOff && t.GetCurrentHex() != null)
            .ToDictionary(t => t, t => t.GetCurrentHex());
        preview.outcomes = Copy(preview.outcomes);
        int defenseIndex = preview.steps.FindIndex(s => s.action.StartsWith("Defensive movement", StringComparison.Ordinal));
        var response = preview.defensiveResponse;
        if (response?.presserTokenKey != null && defenseIndex >= 0)
        {
            var loser = board.Keys.FirstOrDefault(t => Key(t) == response.carrierTokenKey);
            var winner = board.Keys.FirstOrDefault(t => Key(t) == response.presserTokenKey);
            if (loser != null && winner != null)
            {
                board[loser] = Hex(match, response.carrierHex);
                board[winner] = Hex(match, response.moves.First(m => m.tokenKey == response.presserTokenKey).to);
                var recovery = PostTackleRecovery(match, loser, winner, board, ledger, preview.headerParticipants);
                ledger.postTackleOutcomes = recovery;
                float reach = Prefix(preview.steps, defenseIndex) * response.challengeWin;
                ledger.successfulTackleProbability = reach;
                ReplaceUnknown(preview.outcomes, reach, recovery);
            }
        }
        int shotIndex = preview.steps.FindLastIndex(s => s.action == "Snapshot" || s.action == "Shot after reception/completed MP");
        if (shotIndex >= 0 && match.shotManager != null)
        {
            var step = preview.steps[shotIndex];
            var shooter = board.Keys.FirstOrDefault(t => Key(t) == step.actorTokenKey);
            var origin = Hex(match, step.ballHex);
            if (shooter != null && origin != null)
            {
                board = response != null ? new Dictionary<PlayerToken, HexCell>(response.positions)
                    : board.Keys.ToDictionary(t => t, t => t.GetCurrentHex());
                board[shooter] = origin;
                string type = step.action == "Snapshot" ? "snapshot" : "fullPower";
                if (match.shotManager.TryCalculateFutureShotResolutionPreview(shooter, origin, type, board, exactGoal, out var shot, liveShotContext)
                    && Math.Abs(shot.goal - step.conditionalProbability) <= .0001f)
                {
                    ledger.primaryGoalProbability = shot.goal; ledger.heldProbability = shot.held; ledger.offTargetProbability = shot.offTarget;
                    ledger.shotReachProbability = Prefix(preview.steps, shotIndex);
                    var miss = new RoomAIPossessionChain("Non-scoring shot outcomes")
                        .AddTerminal("Off target", shot.offTarget, RoomAIPossessionOutcomeKind.DefendingRestart)
                        .AddTerminal("Keeper holds", shot.held, RoomAIPossessionOutcomeKind.DefenseWinsPossession);
                    foreach (var rebound in shot.rebounds.Where(r => r.probability > 0f))
                        miss.AddContinuation("Rebound / handling spill", rebound.probability,
                            Rebound(match, snapshot, preview, shooter, origin, board, rebound, ledger));
                    // Shot goals were already credited. Conditionalize only the
                    // non-goal mass being replaced; never add the first goal twice.
                    var secondary = miss.Evaluate();
                    float nonGoal = 1f - shot.goal;
                    if (nonGoal > .00001f)
                    {
                        Scale(secondary, 1f / nonGoal);
                        // Missing mass from the intentionally absent Goal edge
                        // is not a secondary unresolved event.
                        secondary.unresolvedProbability = Math.Max(0f, (miss.Evaluate().unresolvedProbability - shot.goal) / nonGoal);
                        ReplaceUnknown(preview.outcomes, ledger.shotReachProbability * nonGoal, secondary);
                    }
                }
            }
        }
        preview.secondary = ledger;
        preview.value = Utility(preview.outcomes);
        return preview;
    }

    private static RoomAIPossessionChainSummary PostTackleRecovery(MatchManager match, PlayerToken loser, PlayerToken winner,
        Dictionary<PlayerToken, HexCell> board, RoomAISecondaryPreview ledger, ISet<PlayerToken> headerParticipants = null)
    {
        var winnerHex = board[winner];
        var occupied = board.Where(p => p.Key != winner).Select(p => p.Value).ToHashSet();
        int winnerSign = (winner.isHomeTeam ? match.homeTeamDirection : match.awayTeamDirection)
            == MatchManager.TeamAttackingDirection.LeftToRight ? 1 : -1;
        if (board[loser].GetNeighbors(match.hexGrid).Any(h => h != null && h.isInGoal == winnerSign
            && !occupied.Contains(h) && winnerHex.GetNeighbors(match.hexGrid).Contains(h)))
            return Terminal(RoomAIPossessionOutcomeKind.Unresolved).Evaluate();
        var exits = board[loser].GetNeighbors(match.hexGrid).Where(h => h != null && !h.isOutOfBounds && h.isInGoal == 0
            && !occupied.Contains(h)).Concat(new[] { winnerHex }).Distinct();
        float best = float.MaxValue;
        foreach (var hex in exits)
        {
            var stealers = board.Keys.Where(t => t.isHomeTeam == loser.isHomeTeam && t != loser && !Unavailable(match, t)
                && headerParticipants?.Contains(t) != true
                && Distance(board[t], hex) == 1).OrderBy(Key, StringComparer.Ordinal).ToArray();
            float recovery = ExpectedStatsCalculator.CalculateAtLeastOneRecoveryProbability(stealers);
            ledger.tackleExits.Add(new RoomAITackleExit { hex = RoomHexCoordinates.FromHex(hex), recoveryProbability = recovery,
                eligibleStealers = stealers.Select(Key).ToList() });
            if (recovery >= best) continue;
            best = recovery; ledger.chosenTackleExit = RoomHexCoordinates.FromHex(hex);
        }
        ledger.postTackleRecoveryProbability = best == float.MaxValue ? 0f : best;
        return new RoomAIPossessionChain("Successful tackle and adversarial legal exit")
            .AddTerminal("Immediate steal-back", ledger.postTackleRecoveryProbability, RoomAIPossessionOutcomeKind.AttackRetainsPossession)
            .AddTerminal("Tackler retains", 1f - ledger.postTackleRecoveryProbability, RoomAIPossessionOutcomeKind.DefenseWinsPossession).Evaluate();
    }

    private static RoomAIPossessionChain Rebound(MatchManager match, RoomAIDecisionSnapshot snapshot, RoomAIActionSequencePreview preview,
        PlayerToken shooter, HexCell shotOrigin, Dictionary<PlayerToken, HexCell> initial, ExpectedStatsCalculator.ShotReboundEvent rebound,
        RoomAISecondaryPreview ledger)
    {
        var tree = new RoomAIPossessionChain("Rebound dice");
        var board = new Dictionary<PlayerToken, HexCell>(initial);
        if (rebound.origin == null || match.looseBallManager == null
            || board.Any(p => p.Key != rebound.token && p.Value == rebound.origin)
            || rebound.source == LooseBallSourceType.GoalkeeperHandlingSpill && rebound.origin.isInPenaltyBox == 0)
        { ledger.unexpandedReboundProbability += rebound.probability; return tree; }
        int sign = (shooter.isHomeTeam ? snapshot.homeTeamDirection : snapshot.awayTeamDirection) == "LeftToRight" ? 1 : -1;
        var offside = initial.Keys.Where(t => t.isHomeTeam == shooter.isHomeTeam
            && RoomAIDefensiveResponse.IsOffside(t, shotOrigin, initial, sign)).ToHashSet();
        // The next completed movement phase clears header nominations. A shot
        // before that boundary must still exclude those hypothetical jumpers.
        int shotIndex = preview.steps.FindLastIndex(s => s.action == "Snapshot" || s.action == "Shot after reception/completed MP");
        bool movementCompleted = shotIndex >= 0 && preview.steps.Take(shotIndex).Any(s => s.action.StartsWith("Complete MP", StringComparison.Ordinal));
        var unavailable = initial.Keys.Where(t => movementCompleted
            ? match.movementPhaseManager?.stunnedTokens.Contains(t) == true
                || match.movementPhaseManager?.stunnedforNext.Contains(t) == true
            : Unavailable(match, t)).ToHashSet();
        if (!movementCompleted && preview.headerParticipants != null) unavailable.UnionWith(preview.headerParticipants);
        var failedPresser = initial.Keys.FirstOrDefault(t => Key(t) == preview.defensiveResponse?.presserTokenKey);
        if (failedPresser != null) unavailable.Add(failedPresser);
        board[rebound.token] = rebound.origin;
        var inward = HandlingSpillDirections(rebound.origin.isInPenaltyBox);
        var collectionReach = new Dictionary<PlayerToken, Dictionary<HexCell, int>>();
        for (int direction = 0; direction < 6; direction++)
        {
            bool corner = rebound.source == LooseBallSourceType.GoalkeeperHandlingSpill && inward.Length > 0 && !inward.Contains(direction);
            int maxDistance = corner ? 1 : 6;
            for (int distance = 1; distance <= maxDistance; distance++)
            {
                var branch = new RoomAIReboundBranch { deflectorTokenKey = Key(rebound.token), source = rebound.source,
                    origin = RoomHexCoordinates.FromHex(rebound.origin), direction = direction, distance = corner ? 0 : distance,
                    probability = rebound.probability * (corner ? 1f / 6f : 1f / 36f) };
                var continuation = corner ? Terminal(RoomAIPossessionOutcomeKind.AttackingRestart)
                    : RollRebound(match, snapshot, preview, shooter, shotOrigin, board, rebound, direction, distance, branch, offside, unavailable, collectionReach);
                branch.outcomes = continuation.Evaluate();
                if (corner) branch.resolution = "Handling spill directly awards a corner";
                ledger.rebounds.Add(branch);
                tree.AddContinuation("Direction / distance", corner ? 1f / 6f : 1f / 36f, continuation);
            }
        }
        return tree;
    }

    private static RoomAIPossessionChain RollRebound(MatchManager match, RoomAIDecisionSnapshot snapshot, RoomAIActionSequencePreview preview,
        PlayerToken shooter, HexCell shotOrigin, Dictionary<PlayerToken, HexCell> board, ExpectedStatsCalculator.ShotReboundEvent rebound,
        int direction, int distance, RoomAIReboundBranch trace, ISet<PlayerToken> offside, ISet<PlayerToken> unavailable,
        Dictionary<PlayerToken, Dictionary<HexCell, int>> collectionReach)
    {
        // The live resolver validates the final rolled hex before examining any
        // earlier pickups. A route which leaves the known grid cannot acquire a
        // fictitious interception, offside call or goal on its way there.
        if (match.looseBallManager.CalculateFinalInaccuracyTarget(rebound.origin, direction, distance).FinalHex == null)
        { trace.resolution = "Final rolled hex outside known grid"; return Terminal(RoomAIPossessionOutcomeKind.Unresolved); }
        var path = new List<HexCell>(); PlayerToken direct = null;
        for (int step = 1; step <= distance; step++)
        {
            var hex = match.looseBallManager.CalculateFinalInaccuracyTarget(rebound.origin, direction, step).FinalHex;
            if (hex == null) { trace.resolution = "Geometry outside known grid"; return Terminal(RoomAIPossessionOutcomeKind.Unresolved); }
            var token = board.Keys.FirstOrDefault(t => board[t] == hex);
            if (token != null && offside.Contains(token))
            { trace.landing = RoomHexCoordinates.FromHex(hex); trace.resolution = "Offside direct pickup"; return Terminal(RoomAIPossessionOutcomeKind.DefendingRestart); }
            if (token != null && !unavailable.Contains(token)) { direct = token; trace.landing = RoomHexCoordinates.FromHex(hex); break; }
            path.Add(hex);
        }
        var tree = new RoomAIPossessionChain("Flight interceptions before collection");
        float survival = 1f; var tried = new HashSet<PlayerToken>();
        foreach (var hex in path)
        foreach (var neighbor in hex.GetNeighbors(match.hexGrid))
        {
            var defender = board.Keys.FirstOrDefault(t => board[t] == neighbor && t.isHomeTeam != shooter.isHomeTeam);
            if (defender == null || defender == direct || unavailable.Contains(defender) || !tried.Add(defender)
                || defender == rebound.token && distance != 1) continue;
            float recovery = ExpectedStatsCalculator.CalculateRecoveryProbability(defender);
            tree.AddTerminal("Opponent intercepts", survival * recovery, RoomAIPossessionOutcomeKind.DefenseWinsPossession);
            survival *= 1f - recovery;
        }
        var keeper = board.Keys.FirstOrDefault(t => t.IsGoalKeeper && t.isHomeTeam != shooter.isHomeTeam);
        // The real loose-ball resolver can offer a goalkeeper checkpoint even
        // after failed flight steals. Leave that surviving branch unknown rather
        // than claiming an attacking pickup/goal before the checkpoint resolves.
        if (!rebound.token.IsGoalKeeper && keeper != null
            && path.Any(h => h.isInPenaltyBox != 0 && Distance(board[keeper], h) <= 3)
            && (direct == null || direct.isHomeTeam == shooter.isHomeTeam))
        { trace.resolution = "GK checkpoint before collection unexpanded"; tree.AddTerminal(trace.resolution, survival, RoomAIPossessionOutcomeKind.Unresolved); return tree; }
        if (direct != null)
        {
            trace.collectorTokenKey = Key(direct); trace.collectionCost = 0; trace.remainingPace = direct.pace;
            trace.resolution = "Direct pickup after flight survives";
            tree.AddContinuation("Direct pickup", survival, direct.isHomeTeam == shooter.isHomeTeam
                ? PickupEnding(match, snapshot, shooter, direct, board[direct], board, false, unavailable)
                : Terminal(RoomAIPossessionOutcomeKind.DefenseWinsPossession));
            return tree;
        }
        var goal = path.FirstOrDefault(h => h.isInGoal != 0);
        int sign = (shooter.isHomeTeam ? snapshot.homeTeamDirection : snapshot.awayTeamDirection) == "LeftToRight" ? 1 : -1;
        var landing = path.LastOrDefault(); trace.landing = RoomHexCoordinates.FromHex(landing);
        if (goal != null)
        { trace.resolution = "Ball crosses goal after failed recoveries"; tree.AddTerminal(trace.resolution, survival,
            goal.isInGoal == sign ? RoomAIPossessionOutcomeKind.Goal : RoomAIPossessionOutcomeKind.DefenseWinsPossession); }
        else if (landing?.isOutOfBounds == true)
        { trace.resolution = "Deflection out; attacking restart"; tree.AddTerminal(trace.resolution, survival, RoomAIPossessionOutcomeKind.AttackingRestart); }
        else if (landing != null)
        {
            bool inMovement = preview.steps.Any(s => s.action == "Attacking movement")
                && preview.steps.Last().action == "Snapshot" || preview.steps.Count == 1 && match.movementPhaseManager?.isActivated == true;
            bool currentMovement = inMovement && preview.steps.Count == 1;
            var spent = inMovement ? preview.steps.Where(s => s.action == "Attacking movement").Select(s => s.actorTokenKey).ToHashSet() : new HashSet<string>();
            if (inMovement) spent.Add(Key(shooter));
            if (currentMovement) foreach (var t in match.movementPhaseManager.movedTokens.Where(t => t.isHomeTeam == shooter.isHomeTeam)) spent.Add(Key(t));
            bool twoForTwo = currentMovement && match.movementPhaseManager.isMovementPhase2f2;
            int paceCap = twoForTwo ? match.movementPhaseManager.TwoForTwoRange : int.MaxValue;
            int movesLeft = twoForTwo ? match.movementPhaseManager.TwoForTwoMoveLimit - match.movementPhaseManager.attackersMovedIn2f2
                : snapshot.freshAttackingMoveLimit - spent.Count;
            if (currentMovement && match.movementPhaseManager.isMovementPhaseDef) movesLeft = 0;
            PlayerToken collector = null; int bestCost = int.MaxValue;
            if (movesLeft > 0)
                foreach (var token in board.Keys.Where(t => t.isHomeTeam == shooter.isHomeTeam && !t.IsGoalKeeper
                    && !spent.Contains(Key(t)) && !offside.Contains(t) && !unavailable.Contains(t)))
                {
                    if (!collectionReach.TryGetValue(token, out var costs))
                        collectionReach[token] = costs = Routes(match, board, token, Math.Min(token.pace, paceCap));
                    if (costs.TryGetValue(landing, out int cost) && cost < bestCost) { collector = token; bestCost = cost; }
                }
            if (collector != null)
            {
                trace.collectorTokenKey = Key(collector); trace.collectionCost = bestCost;
                trace.remainingPace = Math.Min(collector.pace, paceCap) - bestCost;
                trace.resolution = "Eligible unused actor reaches within its movement budget";
                var collected = new Dictionary<PlayerToken, HexCell>(board) { [collector] = landing };
                tree.AddContinuation("Attacker collects", survival, PickupEnding(match, snapshot, shooter, collector, landing, collected, true, unavailable));
            }
            else { trace.resolution = "No eligible collector within this phase; later race unexpanded"; tree.AddTerminal(trace.resolution, survival, RoomAIPossessionOutcomeKind.Unresolved); }
        }
        else tree.AddTerminal("No known landing", survival, RoomAIPossessionOutcomeKind.Unresolved);
        return tree;
    }

    private static RoomAIPossessionChain PickupEnding(MatchManager match, RoomAIDecisionSnapshot snapshot, PlayerToken shooter, PlayerToken collector, HexCell landing,
        Dictionary<PlayerToken, HexCell> board, bool moved, ISet<PlayerToken> unavailable)
    {
        int sign = (shooter.isHomeTeam ? match.homeTeamDirection : match.awayTeamDirection) == MatchManager.TeamAttackingDirection.LeftToRight ? 1 : -1;
        // Eligibility was captured at the shot, before the keeper moved and
        // before this onside runner advanced onto the loose ball.
        var ending = Terminal(RoomAIPossessionOutcomeKind.AttackRetainsPossession);
        bool timeAllows = CanForecastFollowupSnapshot(snapshot.clock);
        float xg = timeAllows && landing.isInPenaltyBox == sign ? SnapshotAfterReply(match, snapshot, collector, landing, board, unavailable) : -1f;
        if (xg >= 0f && xg * 180f - (1f - xg) * 8f > 35f)
            ending = new RoomAIPossessionChain("One legal follow-up Snapshot")
                .AddTerminal("Second-chance goal", xg, RoomAIPossessionOutcomeKind.Goal);
        if (!moved) return ending;
        float survival = 1f, foul = 0f;
        foreach (var foe in board.Keys.Where(t => t.isHomeTeam != shooter.isHomeTeam && !unavailable.Contains(t) && Distance(board[t], landing) == 1))
        {
            var expectation = ExpectedStatsCalculator.CalculateMovementInterceptionExpectation(foe);
            foul += survival * expectation.foulProbability; survival *= expectation.continuationProbability;
        }
        return new RoomAIPossessionChain("Pickup then arrival interactions")
            .AddTerminal("Arrival foul", foul, RoomAIPossessionOutcomeKind.AttackingRestart)
            .AddTerminal("Arrival turnover", Math.Max(0f, 1f - survival - foul), RoomAIPossessionOutcomeKind.DefenseWinsPossession)
            .AddContinuation("Collector survives", survival, ending);
    }

    private static float SnapshotAfterReply(MatchManager match, RoomAIDecisionSnapshot snapshot, PlayerToken shooter, HexCell origin,
        Dictionary<PlayerToken, HexCell> board, ISet<PlayerToken> unavailable)
    {
        string key = "rebound-snapshot-reply|" + Key(shooter) + "|" + string.Join(";", board.OrderBy(p => Key(p.Key), StringComparer.Ordinal)
            .Select(p => $"{Key(p.Key)}:{p.Value.coordinates.x},{p.Value.coordinates.z}")) + "|" + string.Join(",", unavailable.Select(Key).OrderBy(k => k, StringComparer.Ordinal));
        if (snapshot.expectedGoalsCache.TryGetValue(key, out var cached)) return cached.calculated ? cached.expectedGoals : -1f;
        var result = new RoomAIExpectedGoalsPreview();
        result.calculated = match.shotManager.TryCalculateBestFutureExpectedGoalsPreview(shooter, origin, "snapshot", board,
            out result.expectedGoals, out _, out _, out _);
        if (result.calculated)
        {
            var paths = origin.ShootingPaths?.Values.SelectMany(p => p).Where(h => h != null).Distinct().ToArray() ?? Array.Empty<HexCell>();
            var foes = board.Keys.Where(t => t.isHomeTeam != shooter.isHomeTeam && !unavailable.Contains(t)).ToList();
            var candidates = foes.Where(t => !t.IsGoalKeeper).OrderByDescending(t => t.tackling)
                .ThenBy(t => Distance(board[t], origin)).Take(2).Concat(foes.Where(t => t.IsGoalKeeper));
            foreach (var defender in candidates)
            {
                foreach (var destination in Routes(match, board, defender, ShotManager.SnapshotDefenderMovementRange)
                    .Where(p => p.Value > 0).OrderByDescending(p => paths.Count(h => h == p.Key || Distance(h, p.Key) == 1))
                    .ThenBy(p => p.Value).ThenBy(p => p.Key.coordinates.x).ThenBy(p => p.Key.coordinates.z).Take(6))
                {
                    var reply = new Dictionary<PlayerToken, HexCell>(board) { [defender] = destination.Key };
                    if (match.shotManager.TryCalculateBestFutureExpectedGoalsPreview(shooter, origin, "snapshot", reply,
                        out float xg, out _, out _, out _, defender)) result.expectedGoals = Math.Min(result.expectedGoals, xg);
                }
            }
        }
        snapshot.expectedGoalsCache[key] = result;
        return result.calculated ? result.expectedGoals : -1f;
    }

    private static Dictionary<HexCell, int> Routes(MatchManager match, Dictionary<PlayerToken, HexCell> board, PlayerToken mover, int pace)
    {
        var occupied = board.Where(p => p.Key != mover).Select(p => p.Value).ToHashSet();
        var result = new Dictionary<HexCell, int> { [board[mover]] = 0 }; var queue = new Queue<HexCell>(); queue.Enqueue(board[mover]);
        while (queue.Count > 0)
        {
            var hex = queue.Dequeue(); if (result[hex] >= pace) continue;
            foreach (var next in hex.GetNeighbors(match.hexGrid))
                if (next != null && !next.isOutOfBounds && !occupied.Contains(next) && !result.ContainsKey(next))
                { result[next] = result[hex] + 1; queue.Enqueue(next); }
        }
        return result;
    }
    private static bool Unavailable(MatchManager match, PlayerToken t) => match.movementPhaseManager?.stunnedTokens.Contains(t) == true
        || match.movementPhaseManager?.stunnedforNext.Contains(t) == true || match.movementPhaseManager?.headerManager?.attackerWillJump.Contains(t) == true
        || match.movementPhaseManager?.headerManager?.defenderWillJump.Contains(t) == true;
    private static HexCell Hex(MatchManager match, RoomHexCoordinates c) => c == null ? null : match.hexGrid.GetHexCellAt(new UnityEngine.Vector3Int(c.x, 0, c.z), false);
    private static float Prefix(List<RoomAISequenceStep> steps, int end) { float p = 1f; for (int i = 0; i < end; i++) p *= steps[i].conditionalProbability; return p; }
    private static RoomAIPossessionChain Terminal(RoomAIPossessionOutcomeKind outcome) => new RoomAIPossessionChain().AddTerminal(outcome.ToString(), 1f, outcome);
    private static RoomAIPossessionChainSummary Copy(RoomAIPossessionChainSummary s) => new() { goalProbability = s.goalProbability,
        attackRetainsPossessionProbability = s.attackRetainsPossessionProbability, defenseWinsPossessionProbability = s.defenseWinsPossessionProbability,
        attackingRestartProbability = s.attackingRestartProbability, defendingRestartProbability = s.defendingRestartProbability,
        unresolvedProbability = s.unresolvedProbability, evaluatedBranchCount = s.evaluatedBranchCount };
    private static void Scale(RoomAIPossessionChainSummary s, float p) { s.goalProbability *= p; s.attackRetainsPossessionProbability *= p;
        s.defenseWinsPossessionProbability *= p; s.attackingRestartProbability *= p; s.defendingRestartProbability *= p; s.unresolvedProbability *= p; }
    public static bool ReplaceUnknown(RoomAIPossessionChainSummary original, float mass, RoomAIPossessionChainSummary replacement)
    {
        float total = replacement.goalProbability + replacement.attackRetainsPossessionProbability + replacement.defenseWinsPossessionProbability
            + replacement.attackingRestartProbability + replacement.defendingRestartProbability + replacement.unresolvedProbability;
        if (mass <= 0f || float.IsNaN(mass) || float.IsNaN(total) || total > 1.0001f
            || original.unresolvedProbability + .0001f < mass) return false;
        mass = Math.Min(mass, original.unresolvedProbability);
        original.goalProbability += mass * replacement.goalProbability;
        original.attackRetainsPossessionProbability += mass * replacement.attackRetainsPossessionProbability;
        original.defenseWinsPossessionProbability += mass * replacement.defenseWinsPossessionProbability;
        original.attackingRestartProbability += mass * replacement.attackingRestartProbability;
        original.defendingRestartProbability += mass * replacement.defendingRestartProbability;
        original.unresolvedProbability = Math.Max(0f, original.unresolvedProbability - mass + mass * replacement.unresolvedProbability);
        original.evaluatedBranchCount += replacement.evaluatedBranchCount;
        return true;
    }
    private static float Utility(RoomAIPossessionChainSummary s) => s.goalProbability * 180f + s.attackRetainsPossessionProbability * 35f
        + s.attackingRestartProbability * 18f - s.defenseWinsPossessionProbability * 70f - s.defendingRestartProbability * 28f - s.unresolvedProbability * 8f;
}

/// <summary>
/// Bounded, read-only continuation search. The ball moves to the aerial landing
/// before headed targets/offside are evaluated; jumpers do not teleport there.
/// Candidate routes compete by complete branch value, not forward distance.
/// A fresh MP gives each actor Pace once, never another Pace in Att2f2.
/// </summary>
public static class RoomAIActionSequence
{
    public const int CandidateLimit = 8;
    private const int CollectorLimit = 3;
    private const int ShotEndpointLimit = 6;
    private const int AerialTargetLimit = 3;
    private static readonly int[] Rolls = { 1, 2, 3, 4, 5, 6, 50 };
    private static float Weight(int roll) => roll >= 6 ? 1f / 12f : 1f / 6f;
    private static float Clamp(float p) => Math.Max(0f, Math.Min(1f, p));
    private static string Key(PlayerToken token) => (token.isHomeTeam ? "Home:" : "Away:") + token.jerseyNumber;
    private static int Distance(HexCell a, HexCell b) => HexGridUtils.GetHexStepDistance(a, b);

    public static List<RoomAIScatterOutcome> EnumerateScatter(LooseBallManager looseBall, HexCell intended)
    {
        var branches = new List<RoomAIScatterOutcome>();
        for (int direction = 0; direction < 6; direction++)
        for (int distance = 1; distance <= 6; distance++)
        {
            var landing = looseBall != null && intended != null
                ? looseBall.CalculateFinalInaccuracyTarget(intended, direction, distance).FinalHex : null;
            branches.Add(new RoomAIScatterOutcome { directionIndex = direction, distance = distance,
                landingHex = landing, landing = RoomHexCoordinates.FromHex(landing), isOutOfBounds = landing?.isOutOfBounds == true });
        }
        return branches;
    }

    public static RoomAIPossessionChainSummary CombineAerialOutcomes(float accuracy,
        RoomAIPossessionChainSummary accurate, RoomAIPossessionChainSummary scatter)
        => new RoomAIPossessionChain("High pass accuracy and scatter")
            .AddContinuation("Accurate delivery", Clamp(accuracy), accurate != null ? SummaryChain(accurate) : null)
            .AddContinuation("Inaccurate delivery", 1f - Clamp(accuracy), scatter != null ? SummaryChain(scatter) : null).Evaluate();

    // CDF product is O(7^2 * defenders), not exponential dice enumeration. A
    // shared attacker roll must be used against all defenders (not independent
    // pairwise win probabilities). Ties are loose-ball branches, not wins.
    public static (float win, float loss, float tie) HeaderContest(int attackerSkill, params int[] defenderSkills)
    {
        float win = 0f, loss = 0f;
        foreach (int roll in Rolls)
        {
            int total = roll == 50 ? 50 : roll + attackerSkill;
            float allBelow = 1f, allAtMost = 1f;
            foreach (int skill in defenderSkills)
            {
                float below = 0f, atMost = 0f;
                foreach (int other in Rolls)
                {
                    int opponent = other == 50 ? 50 : other + skill;
                    if (opponent < total) below += Weight(other);
                    if (opponent <= total) atMost += Weight(other);
                }
                allBelow *= below;
                allAtMost *= atMost;
            }
            win += Weight(roll) * allBelow;
            loss += Weight(roll) * (1f - allAtMost);
        }
        return (Clamp(win), Clamp(loss), Clamp(1f - win - loss));
    }

    public static bool Offside(RoomAIDecisionSnapshot snapshot, RoomHexCoordinates receiver,
        RoomHexCoordinates ball)
    {
        if (receiver == null || ball == null) return false;
        int sign = (snapshot.expectedTeam == "Home" ? snapshot.homeTeamDirection : snapshot.awayTeamDirection)
            == "LeftToRight" ? 1 : -1;
        var defenders = snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff
            && t.currentHex != null && t.teamSide != snapshot.expectedTeam)
            .Select(t => sign * t.currentHex.x).OrderByDescending(x => x).ToArray();
        if (defenders.Length < 2) return false;
        return sign * receiver.x > Math.Max(0, Math.Max(sign * ball.x, defenders[1]));
    }

    public static bool OffsideForFirstAerial(RoomAIDecisionSnapshot snapshot, RoomHexCoordinates receiver, RoomHexCoordinates ball)
        => !snapshot.firstAerialOffsideExempt && Offside(snapshot, receiver, ball);

    public static int PaceAfterCollection(int pace, int collectionCost, bool twoForTwo = false)
        => Math.Max(0, (twoForTwo ? Math.Min(2, pace) : Math.Max(0, pace)) - collectionCost);

    public static string ShotType(bool moved, bool inOpponentPenaltyBox) => !moved || inOpponentPenaltyBox ? "snapshot" : "fullPower";

    /// <summary>Compose known conditional events exactly once. Unknown shot
    /// misses/rebounds stay unknown; surviving possession is not a guaranteed goal.</summary>
    public static RoomAIPossessionChain Compose(float flight, float collection, float movement,
        float foul, float shotXg = -1f, float defensiveSurvival = 1f, float defensiveFoul = 0f)
    {
        var end = shotXg >= 0f
            ? new RoomAIPossessionChain("Shot; miss/rebound unexpanded")
                .AddTerminal("Goal", Clamp(shotXg), RoomAIPossessionOutcomeKind.Goal)
            : new RoomAIPossessionChain("Retain after the evaluated movement")
                .AddTerminal("Controlled possession", 1f, RoomAIPossessionOutcomeKind.AttackRetainsPossession);
        var defense = new RoomAIPossessionChain("Intervening strongest reachable challenge")
            .AddTerminal("Defender fouls", defensiveFoul, RoomAIPossessionOutcomeKind.AttackingRestart)
            .AddTerminal("Challenge lost; recovery unexpanded", Math.Max(0f, 1f - Clamp(defensiveSurvival) - Clamp(defensiveFoul)), RoomAIPossessionOutcomeKind.Unresolved)
            .AddContinuation("Carrier survives", defensiveSurvival, end);
        var carry = new RoomAIPossessionChain("Following attacking movement")
            .AddTerminal("Route foul", Clamp(foul), RoomAIPossessionOutcomeKind.AttackingRestart)
            .AddTerminal("Route turnover", Math.Max(0f, 1f - Clamp(movement) - Clamp(foul)), RoomAIPossessionOutcomeKind.DefenseWinsPossession)
            .AddContinuation("Route completed", movement, defense);
        var pickup = new RoomAIPossessionChain("Collection")
            .AddTerminal("Opponent collects first", 1f - Clamp(collection), RoomAIPossessionOutcomeKind.DefenseWinsPossession)
            .AddContinuation("Teammate collects", collection, carry);
        return new RoomAIPossessionChain("Pass flight")
            .AddTerminal("Intercepted", 1f - Clamp(flight), RoomAIPossessionOutcomeKind.DefenseWinsPossession)
            .AddContinuation("Pass arrives", flight, pickup);
    }

    public static RoomAIActionSequencePreview Evaluate(RoomAIDecisionSnapshot snapshot, RoomAICandidateExplanation facts)
    {
        var match = MatchManager.Instance;
        var action = facts?.candidate;
        if (snapshot?.ball == null || action == null || action.isForfeit || snapshot.expectedTeam != snapshot.teamInAttack
            || match?.hexGrid == null || match.playerTokenManager == null || match.ball?.GetCurrentHex() == null
            || RoomAITacticalSafety.Distance(snapshot.ball, RoomHexCoordinates.FromHex(match.ball.GetCurrentHex())) != 0)
            return null;
        // Synthetic/editor facts must not accidentally consult a different live match.
        var tokens = match.playerTokenManager.allTokens.Where(t => t != null && t.isPlaying && !t.isSentOff
            && t.GetCurrentHex() != null).ToList();
        if (tokens.Count != snapshot.tokens.Count(t => t != null && t.isPlaying && !t.isSentOff && t.currentHex != null)
            || tokens.Any(t => !snapshot.tokens.Any(s => s.tokenKey == Key(t)
                && RoomAITacticalSafety.Distance(s.currentHex, RoomHexCoordinates.FromHex(t.GetCurrentHex())) == 0))) return null;
        HexCell origin = match.ball.GetCurrentHex();
        var passer = origin.GetOccupyingToken() ?? match.LastTokenToTouchTheBallOnPurpose;
        if (passer == null) return null;
        if (action.actionType == RoomActionType.Shot)
        {
            string type = match.shotManager?.isActivated == true ? match.shotManager.shotType : null;
            if (string.IsNullOrEmpty(type)) type = snapshot.currentState == "EndOfMovementPhase" ? "fullPower" : "snapshot";
            if (type != "snapshot" && type != "fullPower") return null;
            var board = tokens.ToDictionary(t => t, t => t.GetCurrentHex());
            if (match.shotManager == null || !match.shotManager.TryCalculateFutureShotResolutionPreview(passer, origin, type, board,
                action.targetHex?.isInGoal != 0 ? action.targetHex : null, out var shot, match.shotManager.isActivated)) return null;
            var direct = new RoomAIActionSequencePreview { outcomes = new RoomAIPossessionChain("Primary shot")
                .AddTerminal("Goal", shot.goal, RoomAIPossessionOutcomeKind.Goal).Evaluate() };
            direct.steps.Add(Step(type == "snapshot" ? "Snapshot" : "Shot after reception/completed MP", passer, origin, shot.goal));
            return RoomAISecondaryOutcomes.Expand(snapshot, direct, action.targetHex?.isInGoal != 0 ? action.targetHex : null, match.shotManager.isActivated);
        }
        HexCell landing = action.targetHex ?? action.targetToken?.GetCurrentHex()
            ?? Hex(match.hexGrid, facts.groundPassContinuationTargetHex ?? facts.highPassBestTargetHex ?? facts.targetHex);
        if (landing == null) return null;
        var search = new Search(snapshot, match, tokens, passer.isHomeTeam);
        if (action.actionType == RoomActionType.HighPass || action.actionType == RoomActionType.GoalkeeperKick)
        {
            var targets = new List<HexCell> { landing };
            if (action.step == RoomDecisionStep.ChooseActionType && match.highPassManager?.IsAvailableTargetEvaluationComplete == true)
                targets.AddRange(match.highPassManager.GetEvaluatedDecisionTargets()
                    .Where(h => h != null && tokens.Any(t => t.isHomeTeam == passer.isHomeTeam && t != passer
                        && Distance(t.GetCurrentHex(), h) <= HeaderManager.HEADER_SELECTION_RANGE))
                    .OrderByDescending(h => tokens.Where(t => t != passer && t.isHomeTeam == passer.isHomeTeam
                        && Distance(t.GetCurrentHex(), h) <= HeaderManager.HEADER_SELECTION_RANGE).Max(t => t.heading))
                    .ThenBy(h => h.coordinates.x).ThenBy(h => h.coordinates.z).Take(AerialTargetLimit - 1));
            var best = targets.Distinct().Select(h => search.Aerial(passer, origin, h)).Where(p => p != null)
                .OrderByDescending(p => p.value).FirstOrDefault();
            if (best != null) best.alternativesEvaluated = search.alternatives;
            return best;
        }
        if (action.actionType == RoomActionType.GroundPass || action.actionType == RoomActionType.QuickThrow)
        {
            if (match.groundBallManager?.ball == null) return null;
            // GroundBallManager labels its target-stage Quick Throw choices as
            // GroundPass, so the active manager state is also authoritative.
            bool quickThrow = action.actionType == RoomActionType.QuickThrow || match.groundBallManager.isQuickThrow;
            var path = GroundPassCommon.CalculateThickPath(match.hexGrid, origin, landing, match.groundBallManager.ball.ballRadius);
            float flight = tokens.Any(t => t.isHomeTeam != passer.isHomeTeam
                    && (quickThrow ? t.GetCurrentHex() == landing : path.Contains(t.GetCurrentHex()))) ? 0f
                : 1f - ExpectedStatsCalculator.CalculateAtLeastOneRecoveryProbability(
                    GroundPassCommon.BuildOrderedInterceptionCandidates(match.hexGrid, match.groundBallManager.ball, landing,
                        isQuickThrow: quickThrow)
                        .Where(c => c?.DefenderToken != null).Select(c => c.DefenderToken).Distinct().ToArray());
            var next = search.Receive(landing, flight, false, new HashSet<PlayerToken>(),
                offsideExempt: quickThrow, useStoredOffside: true);
            if (next != null)
            {
                next.steps.Insert(0, Step(action.actionType.ToString(), passer, landing, flight));
                next.alternativesEvaluated = search.alternatives;
            }
            return RoomAISecondaryOutcomes.Expand(snapshot, next);
        }
        if (action.actionType == RoomActionType.Header && action.step == RoomDecisionStep.ChooseTarget && facts.isHeadedPassTarget)
        {
            if (landing.isInGoal != 0) return null; // A header at goal is not a headed pass to a collector.
            float flight = 1f - Math.Max(0f, facts.headedPassInterceptionProbability);
            var jumpers = new HashSet<PlayerToken>();
            var header = match.movementPhaseManager?.headerManager;
            if (header != null)
            {
                jumpers.UnionWith(header.attackerWillJump.Where(t => t != null));
                jumpers.UnionWith(header.defenderWillJump.Where(t => t != null));
            }
            var next = search.Receive(landing, flight, true, jumpers, useStoredOffside: true);
            if (next != null)
            {
                next.steps.Insert(0, Step("Headed pass", passer, landing, flight));
                next.alternativesEvaluated = search.alternatives;
            }
            return RoomAISecondaryOutcomes.Expand(snapshot, next);
        }
        return null;
    }

    private static HexCell Hex(HexGrid grid, RoomHexCoordinates coordinates) => coordinates == null ? null
        : grid.GetHexCellAt(new UnityEngine.Vector3Int(coordinates.x, 0, coordinates.z), false);
    private static RoomAISequenceStep Step(string action, PlayerToken actor, HexCell hex, float probability = 1f,
        int cost = 0, int pace = 0) => new() { action = action, actorTokenKey = Key(actor), ballHex = RoomHexCoordinates.FromHex(hex),
            conditionalProbability = probability, movementCost = cost, remainingPace = pace };
    private static float Value(RoomAIPossessionChainSummary p) => p.goalProbability * 180f + p.attackRetainsPossessionProbability * 35f
        + p.attackingRestartProbability * 18f - p.defenseWinsPossessionProbability * 70f
        - p.defendingRestartProbability * 28f - p.unresolvedProbability * 8f;

    private sealed class Search
    {
        private readonly RoomAIDecisionSnapshot snapshot;
        private readonly MatchManager match;
        private readonly List<PlayerToken> own, opponents;
        private readonly Dictionary<PlayerToken, Dictionary<HexCell, List<HexCell>>> reach = new();
        private readonly Dictionary<PlayerToken, HashSet<HexCell>> goalkeeperRushReach = new();
        private Dictionary<PlayerToken, HexCell> projectedScatterBoard;
        private readonly Dictionary<PlayerToken, Dictionary<HexCell, int>> projectedScatterRoutes = new();
        public int alternatives;
        public Search(RoomAIDecisionSnapshot snapshot, MatchManager match, List<PlayerToken> tokens, bool home)
        {
            this.snapshot = snapshot; this.match = match;
            own = tokens.Where(t => t.isHomeTeam == home).OrderBy(Key, StringComparer.Ordinal).ToList();
            opponents = tokens.Where(t => t.isHomeTeam != home).ToList();
        }

        public RoomAIActionSequencePreview Aerial(PlayerToken passer, HexCell origin, HexCell landing)
        {
            if (landing.isOutOfBounds) return null;
            float accuracy = HighPassManager.AccuracyProbabilityForDecisionPreview(passer.highPass);
            var accurate = AerialLanding(passer, origin, landing, false);
            var branches = EnumerateScatter(match.highPassManager?.looseBallManager ?? match.looseBallManager, landing);
            var scatterTree = new RoomAIPossessionChain("Direction and distance scatter rolls");
            foreach (var branch in branches)
            {
                RoomAIActionSequencePreview continuation;
                if (accuracy >= 1f) continuation = Terminal("Zero-probability scatter branch (not expanded)", RoomAIPossessionOutcomeKind.Unresolved);
                else if (branch.landingHex == null) continuation = Terminal("Landing outside known grid / geometry unavailable", RoomAIPossessionOutcomeKind.Unresolved);
                else if (branch.isOutOfBounds) continuation = Terminal("Inaccurate delivery gives opponent a restart", RoomAIPossessionOutcomeKind.DefendingRestart);
                else if (accurate.usesProjectedAerialFreeMoves)
                    continuation = EvaluateProjectedScatter(passer, origin, branch.landingHex,
                        accurate.aerialPreLandingPositions ?? accurate.aerialProjectedPositions);
                else continuation = AerialLanding(passer, origin, branch.landingHex, true);
                branch.probability = (1f - accuracy) * branch.conditionalProbability;
                branch.steps = new List<RoomAISequenceStep>(continuation.steps);
                branch.headerParticipantTokenKeys = continuation.headerParticipants?.Select(Key)
                    .OrderBy(key => key, StringComparer.Ordinal).ToList() ?? new List<string>();
                branch.outcomes = continuation.outcomes;
                branch.secondary = continuation.secondary;
                branch.resolution = continuation.steps.FirstOrDefault()?.action ?? "Unexpanded landing continuation";
                if (accuracy < 1f && !accurate.usesProjectedAerialFreeMoves && branch.landingHex != null && !branch.isOutOfBounds)
                    branch.firstPossibleOpponentCollector = opponents.Where(t => !Stunned(t) && Routes(t).ContainsKey(branch.landingHex))
                        .OrderBy(t => Routes(t)[branch.landingHex].Count).ThenBy(Key, StringComparer.Ordinal).Select(Key).FirstOrDefault();
                scatterTree.AddContinuation($"Direction {branch.directionIndex + 1}, distance {branch.distance}",
                    branch.conditionalProbability, SummaryChain(continuation.outcomes));
            }
            // Copies prevent cached landing plans from accumulating first-action
            // steps when the same landing appears under multiple distributions.
            var result = new RoomAIActionSequencePreview { steps = new List<RoomAISequenceStep>(accurate.steps),
                defensiveResponse = accurate.defensiveResponse, accuracyProbability = accuracy,
                accurateOutcomes = accurate.outcomes, scatterConditionalOutcomes = scatterTree.Evaluate(), scatterOutcomes = branches,
                secondary = accurate.secondary, secondaryExpanded = true, horizon = accurate.horizon,
                usesProjectedAerialFreeMoves = accurate.usesProjectedAerialFreeMoves,
                aerialFreeMoveRespondersEvaluated = new List<string>(accurate.aerialFreeMoveRespondersEvaluated),
                aerialAttackingPreparationsEvaluated = new List<string>(accurate.aerialAttackingPreparationsEvaluated),
                aerialHeaderParticipantTokenKeys = new List<string>(accurate.aerialHeaderParticipantTokenKeys) };
            result.steps.Insert(0, Step("High pass", passer, landing, accuracy));
            result.outcomes = CombineAerialOutcomes(accuracy, result.accurateOutcomes, result.scatterConditionalOutcomes);
            result.value = Value(result.outcomes);
            return result;
        }

        private RoomAIActionSequencePreview AerialLanding(PlayerToken passer, HexCell origin, HexCell landing, bool scatter)
        {
            string cacheKey = $"{Key(passer)}|{landing.coordinates.x}:{landing.coordinates.z}|{scatter}";
            if (snapshot.aerialLandingCache.TryGetValue(cacheKey, out var cached))
            {
                alternatives += cached.alternativesEvaluated;
                return cached;
            }
            int before = alternatives;
            var result = EvaluateAerialLanding(passer, origin, landing, scatter);
            RoomAISecondaryOutcomes.Expand(snapshot, result);
            result.alternativesEvaluated = alternatives - before;
            snapshot.aerialLandingCache[cacheKey] = result;
            return result;
        }

        private RoomAIActionSequencePreview EvaluateAerialLanding(PlayerToken passer, HexCell origin, HexCell landing, bool scatter)
        {
            if (!scatter)
            {
                var projected = ProjectAerialFreeMoves(passer, origin, landing);
                if (projected != null) return projected;
            }
            // Movement stun is not a header-nomination restriction in the live
            // manager; movement eligibility is checked later during collection.
            var defenders = opponents.Where(t => Distance(t.GetCurrentHex(), landing) <= HeaderManager.HEADER_SELECTION_RANGE)
                .OrderByDescending(t => (t.IsGoalKeeper ? t.aerial : t.heading)
                    - (Distance(t.GetCurrentHex(), landing) > 1 ? 1 : 0)).ThenBy(Key, StringComparer.Ordinal).ToList();
            defenders = defenders.Where(t => !t.IsGoalKeeper).Take(HeaderManager.MAX_OUTFIELD_HEADER_DEFENDERS)
                .Concat(defenders.Where(t => t.IsGoalKeeper)).ToList();
            var headers = own.Where(t => t != passer
                && Distance(t.GetCurrentHex(), landing) <= HeaderManager.HEADER_SELECTION_RANGE
                && !OffsideForFirstAerial(snapshot, RoomHexCoordinates.FromHex(t.GetCurrentHex()), RoomHexCoordinates.FromHex(origin)))
                .OrderByDescending(t => t.IsGoalKeeper ? t.aerial : t.heading).ThenBy(Key, StringComparer.Ordinal).Take(scatter ? 1 : 2).ToList();
            // A possible rush from outside current header range changes the
            // pre-reception board. Do not call an unchallenged header certain.
            var keeper = opponents.FirstOrDefault(t => t.IsGoalKeeper);
            if (keeper != null && landing.isInPenaltyBox != 0 && !defenders.Contains(keeper)
                && !GoalkeeperAlreadyMovedInCurrentHighPass()
                && KeeperRushReach(keeper).Any(h => Distance(h, landing) <= HeaderManager.HEADER_SELECTION_RANGE))
                return Terminal("Possible pre-header GK rush; projected rush unexpanded", RoomAIPossessionOutcomeKind.Unresolved);
            if (headers.Count == 0)
            {
                if (defenders.Count > 0) return Terminal("Defender-only aerial opportunity", RoomAIPossessionOutcomeKind.DefenseWinsPossession);
                if (scatter && landing.GetOccupyingToken() == passer)
                    return Receive(landing, 1f, false, new HashSet<PlayerToken>(), origin, null, true);
                // No eligible headers: the live manager starts a fresh MP with
                // the initial passer still excluded from collecting the aerial.
                var dropped = Receive(landing, 1f, false, new HashSet<PlayerToken>(), origin, passer, scatter,
                    snapshot.firstAerialOffsideExempt);
                dropped.steps.Insert(0, Step("Aerial drops; collection race begins", passer, landing));
                return dropped;
            }
            RoomAIActionSequencePreview best = null;
            foreach (var header in headers)
            {
                int Attribute(PlayerToken t) => (t.IsGoalKeeper ? t.aerial : t.heading)
                    - (Distance(t.GetCurrentHex(), landing) > 1 ? 1 : 0);
                var contest = HeaderContest(Attribute(header), defenders.Select(Attribute).ToArray());
                var jumpers = new HashSet<PlayerToken>(defenders) { header };
                // Header flight is six hexes from the BALL landing, not the jumper.
                var destinations = HexGrid.GetHexesInRange(match.hexGrid, landing, 6)
                    .Where(h => h != null && !h.isOutOfBounds && !opponents.Any(d => d.GetCurrentHex() == h))
                    .Where(h => own.Any(t => Distance(t.GetCurrentHex(), h) <= Math.Max(0, t.pace)
                        && !Offside(snapshot, RoomHexCoordinates.FromHex(t.GetCurrentHex()), RoomHexCoordinates.FromHex(landing))))
                    .OrderByDescending(h => own.Any(t => t.GetCurrentHex() == h))
                    .ThenBy(h => Math.Abs(18 - AttackSign() * h.coordinates.x))
                    .ThenBy(h => h.coordinates.z).Take(scatter ? 3 : 6);
                foreach (HexCell target in destinations)
                {
                    float flight = own.Any(t => t.GetCurrentHex() == target) ? 1f
                        : 1f - ExpectedStatsCalculator.CalculateAtLeastOneRecoveryProbability(opponents
                            .Where(t => !jumpers.Contains(t) && !Stunned(t) && Distance(t.GetCurrentHex(), target) == 1).ToArray());
                    var continuation = Receive(target, flight, true, jumpers, landing, null, scatter);
                    if (continuation == null) continue;
                    var accurate = new RoomAIPossessionChain("Header contest")
                        .AddTerminal("Defender wins header", contest.loss, RoomAIPossessionOutcomeKind.DefenseWinsPossession)
                        .AddTerminal("Tied header/loose ball", contest.tie, RoomAIPossessionOutcomeKind.Unresolved)
                        .AddContinuation("Attacker wins header", contest.win, SummaryChain(continuation.outcomes));
                    continuation.outcomes = accurate.Evaluate();
                    continuation.steps.InsertRange(0, new[] { Step("Win header (reassess offside at ball landing)", header, landing, contest.win),
                        Step("Headed pass", header, target, flight) });
                    continuation.value = Value(continuation.outcomes);
                    if (best == null || continuation.value > best.value) best = continuation;
                }
            }
            if (best != null) return best;
            var unknown = new RoomAIActionSequencePreview { outcomes = new RoomAIPossessionChain("Aerial continuation outside bounded search").Evaluate() };
            unknown.value = Value(unknown.outcomes);
            return unknown;
        }

        // Project the mandatory receiver move and a bounded defending free move
        // on an independent board. Stop at a direct headed reception: F3 and
        // subsequent carrier actions need their own projected-board executor.
        private RoomAIActionSequencePreview ProjectAerialFreeMoves(PlayerToken passer, HexCell origin, HexCell landing)
        {
            var hp = match.highPassManager;
            if (hp == null || landing == null || landing.isOutOfBounds) return null;
            var keeper = opponents.FirstOrDefault(t => t.IsGoalKeeper);
            var initial = own.Concat(opponents).ToDictionary(t => t, t => t.GetCurrentHex());
            if (F3CouldAffectAerialContinuation(initial, landing))
            {
                var f3 = Terminal("F3 can precede the high-pass header; projected placements unexpanded", RoomAIPossessionOutcomeKind.Unresolved);
                f3.usesProjectedAerialFreeMoves = true;
                f3.horizon = "F3 before the high-pass header is unexpanded; no contest or post-header possession is assumed";
                return f3;
            }
            var occupant = initial.FirstOrDefault(p => p.Value == landing).Key;
            if (occupant != null && occupant.isHomeTeam != passer.isHomeTeam) return null;
            var preparers = occupant != null ? new[] { occupant } : own.Where(t => t != passer
                    && hp.CanAttackerPrepareHighPassTargetForDecisionPreview(t)
                    && !OffsideForFirstAerial(snapshot, RoomHexCoordinates.FromHex(initial[t]), RoomHexCoordinates.FromHex(origin))
                    && RoomAIDecisionSnapshotBuilder.GetCachedReachability(snapshot, match.hexGrid, initial[t], HighPassManager.AttackerMoveRange)
                        .reachableHexes.Contains(landing))
                .OrderByDescending(t => t.heading).ThenBy(Key, StringComparer.Ordinal).Take(3).ToArray();
            if (preparers.Length == 0 || occupant == passer) return null;

            var preparations = new List<(PlayerToken receiver, PlayerToken mover, HexCell destination)>();
            if (occupant == null)
                preparations.AddRange(preparers.Select(token => (token, token, landing)));
            else
            {
                // The locked receiver stays put. Compare declining the free
                // move with a small set of other attackers who can become a
                // new header participant or a new direct headed-pass outlet.
                preparations.Add((occupant, null, null));
                var support = own.Where(t => t != occupant && !t.IsGoalKeeper)
                    .SelectMany(t => ProjectedReach(initial, t, HighPassManager.AttackerMoveRange)
                        .Where(hex => hex != initial[t]
                            && (Distance(initial[t], landing) > HeaderManager.HEADER_SELECTION_RANGE
                                    && Distance(hex, landing) <= HeaderManager.HEADER_SELECTION_RANGE
                                || Distance(initial[t], landing) > 6 && Distance(hex, landing) <= 6))
                        .OrderBy(hex => Distance(hex, landing)).ThenBy(hex => hex.coordinates.x).ThenBy(hex => hex.coordinates.z)
                        .Take(2).Select(hex => (receiver: occupant, mover: t, destination: hex)))
                    .OrderByDescending(p => p.mover.heading).ThenByDescending(p => p.mover.shooting)
                    .ThenBy(p => Key(p.mover), StringComparer.Ordinal).Take(4);
                preparations.AddRange(support);
            }

            RoomAIActionSequencePreview bestPreparation = null;
            foreach (var preparation in preparations)
            {
                var prepared = new Dictionary<PlayerToken, HexCell>(initial);
                if (preparation.mover != null) prepared[preparation.mover] = preparation.destination;
                var available = opponents.Where(t => !t.IsGoalKeeper && match.CanTokenCollectHangingPass(t))
                    .SelectMany(t => ProjectedReach(prepared, t, HighPassManager.DefenderMoveRange)
                        .Where(hex => hex != prepared[t] && hex != landing
                            && Distance(hex, landing) <= HeaderManager.HEADER_SELECTION_RANGE)
                        .OrderBy(hex => Distance(hex, landing)).ThenBy(hex => hex.coordinates.x).ThenBy(hex => hex.coordinates.z)
                        .Take(3).Select(hex => (token: t, hex)))
                    .OrderByDescending(p => p.token.heading - (Distance(p.hex, landing) > 1 ? 1 : 0))
                    .ThenBy(p => Key(p.token), StringComparer.Ordinal).Take(12).ToList();
                // No defender intervention and no mandatory preparation are
                // left to the established, deeper rollout.
                if (available.Count == 0 && occupant != null && preparations.Count == 1
                    && (keeper == null || landing.isInPenaltyBox == 0)) continue;
                var replies = new List<(PlayerToken token, HexCell hex)> { (null, null) };
                replies.AddRange(available);
                RoomAIActionSequencePreview worstReply = null;
                foreach (var reply in replies)
                {
                    var beforeLanding = new Dictionary<PlayerToken, HexCell>(prepared);
                    if (reply.token != null) beforeLanding[reply.token] = reply.hex;
                    foreach (var keeperReply in ProjectedGoalkeeperReplies(beforeLanding, passer, origin, landing, keeper))
                    {
                    var board = keeperReply.board;
                    if (F3CouldAffectAerialContinuation(board, landing))
                    {
                        var unknownF3 = Terminal("Projected free move permits F3 before the header; continuation unexpanded", RoomAIPossessionOutcomeKind.Unresolved);
                        unknownF3.usesProjectedAerialFreeMoves = true;
                        if (worstReply == null || unknownF3.value < worstReply.value) worstReply = unknownF3;
                        continue;
                    }
                    var attackers = own.Where(t => t != passer && match.CanTokenCollectHangingPass(t)
                            && Distance(board[t], landing) <= HeaderManager.HEADER_SELECTION_RANGE
                            && !OffsideForFirstAerial(snapshot, RoomHexCoordinates.FromHex(initial[t]), RoomHexCoordinates.FromHex(origin)))
                        .OrderByDescending(t => t.heading - (Distance(board[t], landing) > 1 ? 1 : 0))
                        .ThenBy(Key, StringComparer.Ordinal).Take(2).ToList();
                    var defenders = opponents.Where(t => match.CanTokenCollectHangingPass(t)
                            && Distance(board[t], landing) <= HeaderManager.HEADER_SELECTION_RANGE)
                        .OrderByDescending(t => (t.IsGoalKeeper ? t.aerial : t.heading) - (Distance(board[t], landing) > 1 ? 1 : 0))
                        .ThenBy(Key, StringComparer.Ordinal).ToList();
                    defenders = defenders.Where(t => !t.IsGoalKeeper).Take(HeaderManager.MAX_OUTFIELD_HEADER_DEFENDERS)
                        .Concat(defenders.Where(t => t.IsGoalKeeper)).ToList();
                    RoomAIActionSequencePreview bestHeader = null;
                    foreach (var header in attackers)
                    {
                        int Attribute(PlayerToken t) => (t.IsGoalKeeper ? t.aerial : t.heading)
                            - (Distance(board[t], landing) > 1 ? 1 : 0);
                        var contest = HeaderContest(Attribute(header), defenders.Select(Attribute).ToArray());
                        var receiver = own.Where(t => t != header && match.CanTokenCollectHangingPass(t)
                                && Distance(board[t], landing) <= 6
                                && (snapshot.firstAerialOffsideExempt
                                    || !RoomAIDefensiveResponse.IsOffside(t, landing, board, AttackSign())))
                            .OrderByDescending(t => ProjectedHeaderOutletPreference(t, board[t], board))
                            .ThenBy(Key, StringComparer.Ordinal).FirstOrDefault();
                        var jumpers = new HashSet<PlayerToken>(defenders) { header };
                        var space = receiver == null ? FindProjectedHeaderSpaceCollector(board, landing, jumpers)
                            : (collector: (PlayerToken)null, target: (HexCell)null, cost: 0, flight: 0f);
                        var chain = new RoomAIPossessionChain("Projected HP free moves and header")
                            .AddTerminal("Defender wins header", contest.loss, RoomAIPossessionOutcomeKind.DefenseWinsPossession)
                            .AddTerminal("Header tie remains unresolved", contest.tie, RoomAIPossessionOutcomeKind.Unresolved);
                        if (receiver != null)
                            chain.AddTerminal("Direct headed reception before F3", contest.win, RoomAIPossessionOutcomeKind.AttackRetainsPossession);
                        else if (space.collector != null)
                        {
                            chain.AddTerminal("Space header intercepted", contest.win * (1f - space.flight), RoomAIPossessionOutcomeKind.DefenseWinsPossession);
                            chain.AddTerminal("Space header collected before defensive movement", contest.win * space.flight, RoomAIPossessionOutcomeKind.AttackRetainsPossession);
                        }
                        else chain.AddTerminal("Won header; further delivery unexpanded", contest.win, RoomAIPossessionOutcomeKind.Unresolved);
                        var outcome = chain.Evaluate();
                        var plan = new RoomAIActionSequencePreview { outcomes = outcome, usesProjectedAerialFreeMoves = true,
                            headerParticipants = jumpers,
                            aerialHeaderParticipantTokenKeys = jumpers.Select(Key).OrderBy(k => k, StringComparer.Ordinal).ToList(),
                            horizon = "Bounded HP preparation, defending free move, goalkeeper box move/rush, header and direct reception or one-MP space collection; relevant F3 and goalkeeper scatter replies unexpanded" };
                        plan.steps.Add(Step(occupant == null ? "HP attacker prepares landing" : "HP attacker already at landing", preparation.receiver, landing));
                        if (occupant != null)
                            plan.steps.Add(preparation.mover == null ? new RoomAISequenceStep { action = "HP attacker declines support move" }
                                : Step("HP attacker moves for support", preparation.mover, preparation.destination));
                        plan.steps.Add(reply.token == null ? new RoomAISequenceStep { action = "HP defender declines free move" }
                            : Step("HP defender free move", reply.token, reply.hex));
                        if (keeperReply.boxHex != null)
                            plan.steps.Add(Step("Goalkeeper box-entry move", keeper, keeperReply.boxHex));
                        if (keeperReply.rushHex != null)
                            plan.steps.Add(Step("Goalkeeper aerial rush", keeper, keeperReply.rushHex));
                        plan.steps.Add(Step("Win header (projected free moves)", header, landing, contest.win));
                        if (receiver != null) plan.steps.Add(Step("Direct header reception; F3 unexpanded", receiver, board[receiver]));
                        else if (space.collector != null)
                        {
                            plan.steps.Add(Step("Headed pass into space", header, space.target, space.flight));
                            plan.steps.Add(Step("Collect after header; next defense unexpanded", space.collector, space.target, 1f,
                                space.cost, Math.Max(0, space.collector.pace - space.cost)));
                        }
                        plan.value = Value(outcome);
                        var outlet = receiver ?? space.collector;
                        var outletHex = receiver != null ? board[receiver] : space.target;
                        if (outlet != null) plan.aerialOutletPreference = ProjectedHeaderOutletPreference(outlet, outletHex, board);
                        if (bestHeader == null || plan.value > bestHeader.value
                            || Math.Abs(plan.value - bestHeader.value) < .0001f
                                && plan.aerialOutletPreference > bestHeader.aerialOutletPreference) bestHeader = plan;
                    }
                    bestHeader ??= Terminal("No eligible projected attacker header; continuation unexpanded", RoomAIPossessionOutcomeKind.Unresolved);
                    bestHeader.usesProjectedAerialFreeMoves = true;
                    bestHeader.aerialProjectedPositions = board;
                    bestHeader.aerialPreLandingPositions = beforeLanding;
                    if (worstReply == null || bestHeader.value < worstReply.value
                        || Math.Abs(bestHeader.value - worstReply.value) < .0001f
                            && bestHeader.aerialOutletPreference < worstReply.aerialOutletPreference) worstReply = bestHeader;
                    alternatives++;
                    }
                }
                if (worstReply != null && (bestPreparation == null || worstReply.value > bestPreparation.value
                    || Math.Abs(worstReply.value - bestPreparation.value) < .0001f
                        && worstReply.aerialOutletPreference > bestPreparation.aerialOutletPreference))
                {
                    worstReply.aerialFreeMoveRespondersEvaluated = available.Select(p => Key(p.token)).Distinct().ToList();
                    bestPreparation = worstReply;
                }
            }
            if (bestPreparation != null)
            {
                bestPreparation.aerialAttackingPreparationsEvaluated = preparations.Where(p => p.mover != null)
                    .Select(p => Key(p.mover)).Distinct().ToList();
                ExpandProjectedHeaderCollector(bestPreparation);
            }
            return bestPreparation;
        }

        // A tie-break for equally valued projected receptions, not a goal
        // probability. It uses the projected positions after both HP moves;
        // later F3 and complete defensive responses are still unresolved.
        private float ProjectedHeaderOutletPreference(PlayerToken receiver, HexCell destination,
            Dictionary<PlayerToken, HexCell> board)
        {
            if (receiver == null || destination == null || board == null) return 0f;
            float progress = Math.Max(0f, Math.Min(1f, (AttackSign() * destination.coordinates.x + 18f) / 36f));
            float shooting = Math.Max(0f, Math.Min(1f, receiver.shooting / 6f));
            float pace = Math.Max(0f, Math.Min(1f, receiver.pace / 6f));
            float pressure = opponents.Where(t => !t.IsGoalKeeper && !Stunned(t) && board.ContainsKey(t))
                .Select(t => Math.Max(0f, Math.Min(1f,
                    (t.pace + 2f - Distance(board[t], destination)) / Math.Max(1f, t.pace + 1f)))
                    * Math.Max(0f, Math.Min(1f, t.tackling / 6f)))
                .DefaultIfEmpty(0f).Max();
            return .35f * progress + .25f * shooting + .15f * pace - .5f * pressure;
        }

        private void ExpandProjectedHeaderCollector(RoomAIActionSequencePreview plan)
        {
            var collection = plan?.steps.LastOrDefault(step => step.action == "Collect after header; next defense unexpanded");
            var collector = own.FirstOrDefault(t => Key(t) == collection?.actorTokenKey);
            var target = Hex(match.hexGrid, collection?.ballHex);
            if (collector == null || target == null || plan.aerialProjectedPositions == null || plan.outcomes == null) return;
            var highPassLanding = Hex(match.hexGrid, plan.steps.FirstOrDefault(step =>
                step.action == "HP attacker prepares landing" || step.action == "HP attacker already at landing")?.ballHex);
            // F3 can be proven irrelevant to the header or first collection
            // yet still alter the later tackle, goalkeeper or shooting layout.
            if (HasEligibleF3Mover(plan.aerialProjectedPositions, highPassLanding)
                || HasEligibleF3Mover(plan.aerialProjectedPositions, target, plan.headerParticipants))
            {
                plan.horizon += "; F3 placements before later carrier actions remain unexpanded";
                return;
            }
            var board = new Dictionary<PlayerToken, HexCell>(plan.aerialProjectedPositions);
            if (board.Any(p => p.Key != collector && p.Value == target)) return;
            board[collector] = target;
            var response = RoomAIDefensiveResponse.Build(snapshot, collector, target, plan.headerParticipants, board);
            if (response == null) return;
            var prior = plan.outcomes;
            float reached = prior.attackRetainsPossessionProbability;
            RoomAIPossessionChain Prefix() => new RoomAIPossessionChain("After projected headed-space collection")
                .AddTerminal("Earlier goal", prior.goalProbability, RoomAIPossessionOutcomeKind.Goal)
                .AddTerminal("Earlier turnover", prior.defenseWinsPossessionProbability, RoomAIPossessionOutcomeKind.DefenseWinsPossession)
                .AddTerminal("Earlier attacking restart", prior.attackingRestartProbability, RoomAIPossessionOutcomeKind.AttackingRestart)
                .AddTerminal("Earlier defending restart", prior.defendingRestartProbability, RoomAIPossessionOutcomeKind.DefendingRestart)
                .AddTerminal("Earlier unexpanded outcome", prior.unresolvedProbability, RoomAIPossessionOutcomeKind.Unresolved);
            var defended = Prefix()
                .AddTerminal("Carrier retains after defensive movement", reached * response.challengeSurvival, RoomAIPossessionOutcomeKind.AttackRetainsPossession)
                .AddTerminal("Defender wins the tackle", reached * response.challengeWin, RoomAIPossessionOutcomeKind.DefenseWinsPossession)
                .AddTerminal("Defender fouls the carrier", reached * response.challengeFoul, RoomAIPossessionOutcomeKind.AttackingRestart)
                .AddTerminal("Tackle tie remains unresolved", reached * response.challengeTie, RoomAIPossessionOutcomeKind.Unresolved).Evaluate();
            RoomAIPossessionChainSummary shot = null;
            float xg = -1f;
            if (target.isInPenaltyBox == AttackSign() && target.CanShootFrom && match.shotManager != null
                && match.shotManager.TryCalculateBestFutureExpectedGoalsPreview(collector, target, "snapshot", board,
                    out xg, out _, out _, out _))
                shot = Prefix().AddTerminal("Immediate Snapshot scores", reached * xg, RoomAIPossessionOutcomeKind.Goal)
                    .AddTerminal("Snapshot miss or rebound remains unresolved", reached * (1f - xg), RoomAIPossessionOutcomeKind.Unresolved).Evaluate();
            if (shot != null && Value(shot) > Value(defended))
            {
                plan.outcomes = shot;
                plan.steps.Add(Step("Projected immediate Snapshot; rebound unexpanded", collector, target, xg));
            }
            else
            {
                plan.outcomes = defended;
                plan.defensiveResponse = response;
                plan.steps.Add(Step("Projected defensive movement after collection", collector, target, response.challengeSurvival));
            }
            // Expand only a small set of distinct endpoints, after choosing the
            // header preparation. Collection has already spent part of Pace.
            // Box-entry goalkeeper moves and wall dives need their own reply
            // tree, so routes involving either are deliberately left unexpanded.
            var routes = ProjectedCarrierRoutes(board, collector, Math.Max(0, collection.remainingPace));
            var reachable = routes.Keys.Where(h => h != target).ToList();
            var endpoints = reachable.Where(h => h.isInPenaltyBox == AttackSign() && h.CanShootFrom)
                .OrderByDescending(h => AttackSign() * h.coordinates.x).ThenBy(h => routes[h].Count).Take(2)
                .Concat(reachable.Where(h => h.coordinates.z < target.coordinates.z)
                    .OrderByDescending(h => AttackSign() * h.coordinates.x).ThenBy(h => routes[h].Count).Take(1))
                .Concat(reachable.Where(h => h.coordinates.z > target.coordinates.z)
                    .OrderByDescending(h => AttackSign() * h.coordinates.x).ThenBy(h => routes[h].Count).Take(1))
                .Concat(reachable.OrderByDescending(h => AttackSign() * h.coordinates.x)
                    .ThenBy(h => routes[h].Count).ThenBy(h => h.coordinates.z))
                .Distinct().Take(5);
            foreach (var endpoint in endpoints)
            {
                var route = routes[endpoint];
                float survival = 1f, foul = 0f;
                foreach (var defender in opponents.Where(t => !t.IsGoalKeeper && !Stunned(t)
                    && !plan.headerParticipants.Contains(t) && board.TryGetValue(t, out var position)
                    && route.Any(h => Distance(position, h) == 1)))
                {
                    var interaction = ExpectedStatsCalculator.CalculateMovementInterceptionExpectation(defender);
                    foul += survival * interaction.foulProbability;
                    survival *= interaction.continuationProbability;
                }
                var movedBoard = new Dictionary<PlayerToken, HexCell>(board) { [collector] = endpoint };
                RoomAIPossessionChain DribblePrefix() => Prefix()
                    .AddTerminal("Dribble interception", reached * Math.Max(0f, 1f - survival - foul), RoomAIPossessionOutcomeKind.DefenseWinsPossession)
                    .AddTerminal("Foul during dribble", reached * foul, RoomAIPossessionOutcomeKind.AttackingRestart);
                float arrival = reached * survival;
                var reply = RoomAIDefensiveResponse.Build(snapshot, collector, endpoint, plan.headerParticipants, movedBoard);
                if (reply == null) continue;
                var continuation = DribblePrefix()
                    .AddTerminal("Carrier retains after defensive movement", arrival * reply.challengeSurvival, RoomAIPossessionOutcomeKind.AttackRetainsPossession)
                    .AddTerminal("Defender wins the tackle", arrival * reply.challengeWin, RoomAIPossessionOutcomeKind.DefenseWinsPossession)
                    .AddTerminal("Defender fouls the carrier", arrival * reply.challengeFoul, RoomAIPossessionOutcomeKind.AttackingRestart)
                    .AddTerminal("Tackle tie remains unresolved", arrival * reply.challengeTie, RoomAIPossessionOutcomeKind.Unresolved).Evaluate();
                float endpointXg = -1f;
                bool canShoot = endpoint.isInPenaltyBox == AttackSign() && endpoint.CanShootFrom
                    && match.shotManager != null
                    && match.shotManager.TryCalculateBestFutureExpectedGoalsPreview(collector, endpoint, "snapshot", movedBoard,
                        out endpointXg, out _, out _, out _);
                bool shoot = false;
                if (canShoot)
                {
                    var shotAfterDribble = DribblePrefix().AddTerminal("Snapshot scores after dribble", arrival * endpointXg, RoomAIPossessionOutcomeKind.Goal)
                        .AddTerminal("Snapshot miss or rebound remains unresolved", arrival * (1f - endpointXg), RoomAIPossessionOutcomeKind.Unresolved).Evaluate();
                    if (Value(shotAfterDribble) > Value(continuation)) { continuation = shotAfterDribble; shoot = true; }
                }
                if (Value(continuation) <= Value(plan.outcomes)) continue;
                // Replace the previously selected continuation, not the header
                // and collection steps that led to it.
                while (plan.steps.Count > 0 && plan.steps[plan.steps.Count - 1] != collection)
                    plan.steps.RemoveAt(plan.steps.Count - 1);
                plan.steps.Add(Step("Projected dribble after header collection", collector, endpoint, survival,
                    route.Count, collection.remainingPace - route.Count));
                plan.steps.Add(Step(shoot ? "Projected Snapshot after dribble; rebound unexpanded"
                    : "Projected defensive movement after dribble", collector, endpoint, shoot ? endpointXg : reply.challengeSurvival));
                plan.outcomes = continuation;
                plan.defensiveResponse = shoot ? null : reply;
            }
            plan.value = Value(plan.outcomes);
            plan.horizon += "; after space collection compares stopping, an immediate box Snapshot and up to five dribble endpoints; box-entry goalkeeper moves, wall dives, other routes and later F3 remain unexpanded";
        }

        private Dictionary<HexCell, List<HexCell>> ProjectedCarrierRoutes(
            Dictionary<PlayerToken, HexCell> board, PlayerToken carrier, int pace)
        {
            var occupied = new HashSet<HexCell>(board.Where(p => p.Key != carrier).Select(p => p.Value));
            var keeper = opponents.FirstOrDefault(t => t.IsGoalKeeper && board.ContainsKey(t));
            var keeperHex = keeper == null ? null : board[keeper];
            var routes = new Dictionary<HexCell, List<HexCell>> { [board[carrier]] = new List<HexCell>() };
            var queue = new Queue<HexCell>(); queue.Enqueue(board[carrier]);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (routes[current].Count >= pace) continue;
                foreach (var next in current.GetNeighbors(match.hexGrid))
                {
                    if (next == null || next.isOutOfBounds || next.isInGoal != 0 || occupied.Contains(next)
                        || routes.ContainsKey(next) || (current.isInPenaltyBox == 0 && next.isInPenaltyBox != 0)) continue;
                    // GK wall consists of the keeper's x coordinate and up to
                    // three z offsets inside the keeper's own penalty area.
                    if (keeperHex != null && next.isInPenaltyBox == AttackSign()
                        && next.coordinates.x == keeperHex.coordinates.x
                        && Math.Abs(next.coordinates.z - keeperHex.coordinates.z) <= 3) continue;
                    var route = new List<HexCell>(routes[current]) { next };
                    routes[next] = route;
                    queue.Enqueue(next);
                }
            }
            return routes;
        }

        private IEnumerable<HexCell> ProjectedReach(Dictionary<PlayerToken, HexCell> board, PlayerToken mover, int budget)
            => ProjectedReachWithCost(board, mover, budget).Select(p => p.hex);

        private RoomAIActionSequencePreview EvaluateProjectedScatter(PlayerToken passer, HexCell origin, HexCell landing,
            Dictionary<PlayerToken, HexCell> board)
        {
            if (board == null || landing == null)
                return Terminal("Scatter after projected HP free moves; board unexpanded", RoomAIPossessionOutcomeKind.Unresolved);
            var keeper = opponents.FirstOrDefault(t => t.IsGoalKeeper);
            RoomAIActionSequencePreview worst = null;
            foreach (var reply in ProjectedGoalkeeperReplies(board, passer, origin, landing, keeper))
            {
                var continuation = EvaluateProjectedScatterAfterGoalkeeper(passer, origin, landing, reply.board);
                if (reply.rushHex != null) continuation.steps.Insert(0, Step("Goalkeeper aerial rush after scatter", keeper, reply.rushHex));
                if (reply.boxHex != null) continuation.steps.Insert(0, Step("Goalkeeper box-entry move after scatter", keeper, reply.boxHex));
                if (worst == null || continuation.value < worst.value) worst = continuation;
            }
            return worst ?? Terminal("No goalkeeper scatter reply was evaluated", RoomAIPossessionOutcomeKind.Unresolved);
        }

        private RoomAIActionSequencePreview EvaluateProjectedScatterAfterGoalkeeper(PlayerToken passer, HexCell origin, HexCell landing,
            Dictionary<PlayerToken, HexCell> board)
        {
            if (F3CouldAffectAerialContinuation(board, landing))
                return Terminal("Scatter triggers possible F3 before collection; unexpanded", RoomAIPossessionOutcomeKind.Unresolved);
            var nearby = board.Where(p => Distance(p.Value, landing) <= HeaderManager.HEADER_SELECTION_RANGE
                && match.CanTokenCollectHangingPass(p.Key)).Select(p => p.Key).ToList();
            var defenders = nearby.Where(t => t.isHomeTeam != passer.isHomeTeam)
                .OrderByDescending(t => (t.IsGoalKeeper ? t.aerial : t.heading) - (Distance(board[t], landing) > 1 ? 1 : 0))
                .ThenBy(Key, StringComparer.Ordinal).ToList();
            defenders = defenders.Where(t => !t.IsGoalKeeper).Take(HeaderManager.MAX_OUTFIELD_HEADER_DEFENDERS)
                .Concat(defenders.Where(t => t.IsGoalKeeper)).ToList();
            var headers = nearby.Where(t => t != passer && t.isHomeTeam == passer.isHomeTeam
                    && !OffsideForFirstAerial(snapshot, RoomHexCoordinates.FromHex(t.GetCurrentHex()), RoomHexCoordinates.FromHex(origin)))
                .OrderByDescending(t => t.heading - (Distance(board[t], landing) > 1 ? 1 : 0))
                .ThenBy(Key, StringComparer.Ordinal).Take(2).ToList();
            RoomAIActionSequencePreview bestHeader = null;
            foreach (var header in headers)
            {
                int Attribute(PlayerToken t) => (t.IsGoalKeeper ? t.aerial : t.heading)
                    - (Distance(board[t], landing) > 1 ? 1 : 0);
                var contest = HeaderContest(Attribute(header), defenders.Select(Attribute).ToArray());
                var receiver = own.Where(t => t != header && match.CanTokenCollectHangingPass(t)
                        && Distance(board[t], landing) <= 6
                        && (snapshot.firstAerialOffsideExempt
                            || !RoomAIDefensiveResponse.IsOffside(t, landing, board, AttackSign())))
                    .OrderByDescending(t => ProjectedHeaderOutletPreference(t, board[t], board))
                    .ThenBy(Key, StringComparer.Ordinal).FirstOrDefault();
                var chain = new RoomAIPossessionChain("Projected scatter header")
                    .AddTerminal("Defender wins the header", contest.loss, RoomAIPossessionOutcomeKind.DefenseWinsPossession)
                    .AddTerminal("Header tie remains unresolved", contest.tie, RoomAIPossessionOutcomeKind.Unresolved);
                var plan = new RoomAIActionSequencePreview();
                plan.steps.Add(Step(defenders.Count == 0 ? "Uncontested header after scatter" : "Contested header after scatter",
                    header, landing, contest.win));
                if (receiver != null)
                {
                    chain.AddTerminal("Direct headed reception before F3", contest.win, RoomAIPossessionOutcomeKind.AttackRetainsPossession);
                    plan.steps.Add(Step("Direct header reception; F3 unexpanded", receiver, board[receiver]));
                }
                else
                {
                    var jumpers = new HashSet<PlayerToken>(defenders) { header };
                    var space = FindProjectedHeaderSpaceCollector(board, landing, jumpers, useScatterCache: true);
                    if (space.collector != null)
                    {
                        chain.AddTerminal("Space header intercepted", contest.win * (1f - space.flight), RoomAIPossessionOutcomeKind.DefenseWinsPossession)
                            .AddTerminal("Space header collected before defensive movement", contest.win * space.flight, RoomAIPossessionOutcomeKind.AttackRetainsPossession);
                        plan.steps.Add(Step("Headed pass into space", header, space.target, space.flight));
                        plan.steps.Add(Step("Collect after header; next defense unexpanded", space.collector, space.target, 1f,
                            space.cost, Math.Max(0, space.collector.pace - space.cost)));
                    }
                    else chain.AddTerminal("Won header; delivery unexpanded", contest.win, RoomAIPossessionOutcomeKind.Unresolved);
                }
                plan.outcomes = chain.Evaluate();
                plan.value = Value(plan.outcomes);
                if (bestHeader == null || plan.value > bestHeader.value) bestHeader = plan;
            }
            if (bestHeader != null) return bestHeader;
            if (nearby.Count > 0)
                return Terminal("Scatter has no eligible attacking header; continuation unexpanded", RoomAIPossessionOutcomeKind.Unresolved);
            if (snapshot.freshAttackingMoveLimit < 1)
                return Terminal("No attacking movement is available for scatter collection", RoomAIPossessionOutcomeKind.Unresolved);
            var collector = own.Where(t => t != passer && !t.IsGoalKeeper && !Stunned(t) && match.CanTokenCollectHangingPass(t)
                    && !OffsideForFirstAerial(snapshot, RoomHexCoordinates.FromHex(t.GetCurrentHex()), RoomHexCoordinates.FromHex(origin)))
                .Select(t => (token: t, paths: ScatterRoutes(board, t)))
                .Where(p => p.paths.TryGetValue(landing, out int steps) && steps > 0)
                .Select(p => (p.token, steps: p.paths[landing]))
                .OrderBy(p => p.steps).ThenBy(p => Key(p.token), StringComparer.Ordinal).FirstOrDefault();
            if (collector.token == null)
                return Terminal("No eligible one-MP scatter collector identified; continuation unexpanded", RoomAIPossessionOutcomeKind.Unresolved);
            var result = Terminal("Projected scatter collected before defensive movement", RoomAIPossessionOutcomeKind.AttackRetainsPossession);
            result.steps.Clear();
            result.steps.Add(Step("Collect after projected scatter; next defense unexpanded", collector.token, landing, 1f,
                collector.steps, Math.Max(0, collector.token.pace - collector.steps)));
            return result;
        }

        private Dictionary<HexCell, int> ScatterRoutes(Dictionary<PlayerToken, HexCell> board, PlayerToken token)
        {
            if (!ReferenceEquals(projectedScatterBoard, board))
            {
                projectedScatterBoard = board;
                projectedScatterRoutes.Clear();
            }
            if (!projectedScatterRoutes.TryGetValue(token, out var paths))
                projectedScatterRoutes[token] = paths = ProjectedReachWithCost(board, token, token.pace)
                    .ToDictionary(p => p.hex, p => p.steps);
            return paths;
        }

        private bool GoalkeeperBoxMoveMayIntervene(PlayerToken passer, HexCell origin, HexCell landing, PlayerToken keeper)
        {
            if (passer == null || !passer.isAttacker || keeper == null || origin == null || landing == null
                || landing.isInPenaltyBox == 0 || landing.isInPenaltyBox != AttackSign()
                || origin.isInPenaltyBox == landing.isInPenaltyBox) return false;
            var manager = match.movementPhaseManager?.goalKeeperManager;
            // A missing manager cannot establish that a box-entry move has
            // already been consumed, so keep the continuation unresolved.
            return manager == null || !manager.HasConsumedBoxMoveForDecisionPreview(landing.isInPenaltyBox);
        }

        private bool GoalkeeperAlreadyMovedInCurrentHighPass()
        {
            var hp = match.highPassManager;
            // StartDefenderMovementPhase resets this flag. While choosing a
            // new pass, a value left by the previous pass is not evidence
            // that the goalkeeper has spent the upcoming free move.
            return hp?.isActivated == true && hp.didGKMoveInDefPhase
                && (hp.isWaitingForAccuracyRoll || hp.isWaitingForDirectionRoll || hp.isWaitingForDistanceRoll
                    || hp.isWaitingForDefGKChallengeDecision);
        }

        private IEnumerable<(Dictionary<PlayerToken, HexCell> board, HexCell boxHex, HexCell rushHex)> ProjectedGoalkeeperReplies(
            Dictionary<PlayerToken, HexCell> beforeLanding, PlayerToken passer, HexCell origin, HexCell landing, PlayerToken keeper)
        {
            if (keeper == null || !beforeLanding.TryGetValue(keeper, out var keeperStart))
            {
                yield return (beforeLanding, null, null);
                yield break;
            }
            var boxDestinations = new List<HexCell> { keeperStart };
            if (GoalkeeperBoxMoveMayIntervene(passer, origin, landing, keeper))
                boxDestinations.AddRange(ProjectedReach(beforeLanding, keeper, 1).Where(h => h != keeperStart)
                    .OrderBy(h => Distance(h, landing)).ThenBy(h => h.coordinates.x).ThenBy(h => h.coordinates.z).Take(3));
            foreach (var box in boxDestinations)
            {
                var afterBox = box == keeperStart ? beforeLanding
                    : new Dictionary<PlayerToken, HexCell>(beforeLanding) { [keeper] = box };
                HexCell boxMove = box == keeperStart ? null : box;
                // The keeper may decline either optional movement.
                yield return (afterBox, boxMove, null);
                if (landing.isInPenaltyBox == 0 || GoalkeeperAlreadyMovedInCurrentHighPass()) continue;
                foreach (var rush in ProjectedReach(afterBox, keeper, 3)
                    .Where(h => Distance(h, landing) <= HeaderManager.HEADER_SELECTION_RANGE)
                    .OrderBy(h => Distance(h, landing)).ThenBy(h => h.coordinates.x).ThenBy(h => h.coordinates.z).Take(4))
                {
                    var afterRush = new Dictionary<PlayerToken, HexCell>(afterBox) { [keeper] = rush };
                    yield return (afterRush, boxMove, rush);
                }
            }
        }

        private bool F3CouldAffectAerialContinuation(Dictionary<PlayerToken, HexCell> board, HexCell landing,
            ISet<PlayerToken> additionalUnavailable = null)
        {
            if (board == null || landing == null || landing.isInFinalThird == 0) return false;
            var movement = match.movementPhaseManager;
            var header = movement?.headerManager;
            var movable = board.Where(p => p.Value != null && p.Value.isInFinalThird == -landing.isInFinalThird
                    && movement?.stunnedTokens.Contains(p.Key) != true
                    && additionalUnavailable?.Contains(p.Key) != true
                    && header?.attackerWillJump.Contains(p.Key) != true && header?.defenderWillJump.Contains(p.Key) != true)
                .Select(p => p.Key).ToHashSet();
            if (movable.Count == 0) return false;
            foreach (var token in movable)
            {
                // An ordinary F3 move can cover six hexes, followed by a
                // headed delivery and potentially a collector's full Pace.
                // This deliberately overestimates influence: only a proven
                // irrelevant move may be skipped without projecting it.
                int reach = token.isHomeTeam == own[0].isHomeTeam
                    ? FinalThirdManager.StandardMovementRange + 6 + Math.Max(0, token.pace)
                    : FinalThirdManager.StandardMovementRange + 7;
                if (Distance(board[token], landing) <= reach) return true;
            }
            int sign = AttackSign();
            var defenders = board.Where(p => p.Key.isHomeTeam != own[0].isHomeTeam && p.Value != null).ToList();
            if (defenders.Count < 2) return true;
            int Line(bool upper) => defenders.Select(p => sign * p.Value.coordinates.x
                    + (movable.Contains(p.Key) ? upper ? FinalThirdManager.StandardMovementRange : -FinalThirdManager.StandardMovementRange : 0))
                .OrderByDescending(x => x).ElementAt(1);
            int lowerLimit = Math.Max(0, Math.Max(sign * landing.coordinates.x, Line(false)));
            int upperLimit = Math.Max(0, Math.Max(sign * landing.coordinates.x, Line(true)));
            // If every possible shift leaves the pass-time line unchanged,
            // remote F3 moves cannot affect this bounded header/collection.
            return lowerLimit != upperLimit;
        }

        private bool HasEligibleF3Mover(Dictionary<PlayerToken, HexCell> board, HexCell landing,
            ISet<PlayerToken> additionalUnavailable = null)
        {
            if (board == null || landing == null || landing.isInFinalThird == 0) return false;
            var movement = match.movementPhaseManager;
            var header = movement?.headerManager;
            return board.Any(p => p.Value != null && p.Value.isInFinalThird == -landing.isInFinalThird
                && movement?.stunnedTokens.Contains(p.Key) != true && additionalUnavailable?.Contains(p.Key) != true
                && header?.attackerWillJump.Contains(p.Key) != true && header?.defenderWillJump.Contains(p.Key) != true);
        }

        private IEnumerable<(HexCell hex, int steps)> ProjectedReachWithCost(Dictionary<PlayerToken, HexCell> board, PlayerToken mover, int budget)
        {
            var occupied = new HashSet<HexCell>(board.Where(p => p.Key != mover).Select(p => p.Value));
            var seen = new HashSet<HexCell> { board[mover] };
            var queue = new Queue<(HexCell hex, int steps)>(); queue.Enqueue((board[mover], 0));
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                yield return current;
                if (current.steps >= budget) continue;
                foreach (var next in current.hex.GetNeighbors(match.hexGrid))
                    if (next != null && !next.isOutOfBounds && next.isInGoal == 0 && !occupied.Contains(next) && seen.Add(next))
                        queue.Enqueue((next, current.steps + 1));
            }
        }

        private (PlayerToken collector, HexCell target, int cost, float flight) FindProjectedHeaderSpaceCollector(
            Dictionary<PlayerToken, HexCell> board, HexCell headerLanding, HashSet<PlayerToken> jumpers, bool useScatterCache = false)
        {
            if (snapshot.freshAttackingMoveLimit < 1) return (null, null, 0, 0f);
            (PlayerToken collector, HexCell target, int cost, float flight) best = (null, null, 0, 0f);
            var occupied = new HashSet<HexCell>(board.Values);
            foreach (var collector in own.Where(t => !t.IsGoalKeeper && !jumpers.Contains(t) && !Stunned(t)
                         && match.CanTokenCollectHangingPass(t)
                         && (snapshot.firstAerialOffsideExempt || !RoomAIDefensiveResponse.IsOffside(t, headerLanding, board, AttackSign()))))
            {
                var routes = useScatterCache
                    ? ScatterRoutes(board, collector).Select(p => (hex: p.Key, steps: p.Value))
                    : ProjectedReachWithCost(board, collector, collector.pace);
                var targets = routes
                    .Where(p => p.steps > 0 && p.hex.isInGoal == 0 && !occupied.Contains(p.hex)
                        && Distance(headerLanding, p.hex) <= 6
                        && !F3CouldAffectAerialContinuation(board, p.hex, jumpers))
                    .OrderBy(p => p.steps).ThenByDescending(p => AttackSign() * p.hex.coordinates.x).Take(10);
                foreach (var candidate in targets)
                {
                    var interceptors = opponents.Where(t => !jumpers.Contains(t) && !Stunned(t)
                        && Distance(board[t], candidate.hex) == 1).ToArray();
                    float flight = 1f - ExpectedStatsCalculator.CalculateAtLeastOneRecoveryProbability(interceptors);
                    if (flight <= 0f || best.collector != null && (flight < best.flight
                        || Math.Abs(flight - best.flight) < .0001f && candidate.steps >= best.cost)) continue;
                    best = (collector, candidate.hex, candidate.steps, flight);
                }
            }
            return best;
        }


        public RoomAIActionSequencePreview Receive(HexCell landing, float flight, bool headed,
            HashSet<PlayerToken> jumpers, HexCell offsideBall = null, PlayerToken excludedCollector = null, bool boundedScatter = false,
            bool offsideExempt = false, bool useStoredOffside = false)
        {
            offsideBall ??= match.ball.GetCurrentHex();
            bool OffsideAtTouch(PlayerToken token) => !offsideExempt
                && (useStoredOffside && match.offsideManager?.HasStoredAssessment == true
                    ? match.offsideManager.IsTokenOffside(token)
                    : Offside(snapshot, RoomHexCoordinates.FromHex(token.GetCurrentHex()), RoomHexCoordinates.FromHex(offsideBall)));
            var occupant = own.FirstOrDefault(t => t.GetCurrentHex() == landing);
            if (occupant != null && OffsideAtTouch(occupant))
            {
                var tree = new RoomAIPossessionChain("Offside reception")
                    .AddTerminal("Flight intercepted", 1f - flight, RoomAIPossessionOutcomeKind.DefenseWinsPossession)
                    .AddTerminal("Offside", flight, RoomAIPossessionOutcomeKind.DefendingRestart);
                var stopped = new RoomAIActionSequencePreview { outcomes = tree.Evaluate() };
                stopped.value = Value(stopped.outcomes);
                stopped.steps.Add(Step("Offside collection rejected", occupant, landing));
                return stopped;
            }
            var eligible = own.Where(t => t != excludedCollector && (!t.IsGoalKeeper || t == occupant) && !Stunned(t)
                && (occupant == null || t == occupant)
                && !OffsideAtTouch(t))
                .Select(t => (token: t, routes: Routes(t))).Where(p => p.routes.ContainsKey(landing))
                .OrderBy(p => p.routes[landing].Count).ThenByDescending(p => p.token.shooting)
                .ThenBy(p => Key(p.token), StringComparer.Ordinal).Take(boundedScatter ? 1 : CollectorLimit);
            RoomAIActionSequencePreview best = null;
            foreach (var entry in eligible)
            {
                var collector = entry.token;
                var pickup = entry.routes[landing];
                // Header participants cannot be selected for the immediately
                // following movement phase. A stationary reception remains a
                // separate legal case and does not consume a movement choice.
                if (pickup.Count > 0 && jumpers.Contains(collector)) continue;
                if (pickup.Count > collector.pace || snapshot.freshAttackingMoveLimit < 1 && pickup.Count > 0) continue;
                // No dribble interception on the off-ball journey to collection.
                // Ball collection is first in fresh AttMP; no invented intervening
                // DefMP for routes reachable within this one actor's Pace.
                int remaining = jumpers.Contains(collector) ? 0 : PaceAfterCollection(collector.pace, pickup.Count);
                var routes = Paths(landing, remaining, collector);
                var endpoints = routes.Keys.OrderByDescending(h => h.CanShootFrom && AttackSign() * h.coordinates.x > 0)
                    .ThenBy(h => Math.Abs(18 - AttackSign() * h.coordinates.x)).ThenBy(h => routes[h].Count)
                    .ThenBy(h => h.coordinates.z).Take(boundedScatter ? 2 : ShotEndpointLimit).Concat(new[] { landing }).Distinct();
                foreach (HexCell endpoint in endpoints)
                {
                    alternatives++;
                    var route = routes[endpoint];
                    float survival = 1f, foul = 0f;
                    foreach (var defender in opponents.Where(t => !Stunned(t) && !jumpers.Contains(t)
                        && route.Any(h => Distance(t.GetCurrentHex(), h) == 1)))
                    {
                        var interaction = ExpectedStatsCalculator.CalculateMovementInterceptionExpectation(defender);
                        foul += survival * interaction.foulProbability;
                        survival *= interaction.continuationProbability;
                    }
                    bool moves = pickup.Count > 0 || route.Count > 0;
                    if (moves && snapshot.freshAttackingMoveLimit < 1) continue;
                    bool snapshotShot = !moves || endpoint.isInPenaltyBox == AttackSign();
                    float xg = Shot(collector, endpoint, ShotType(moves, endpoint.isInPenaltyBox == AttackSign()));
                    // A shot during AttMP in the opponent box precedes DefMP.
                    // Outside it, the defender can challenge before the next shot.
                    float challengeSurvival = 1f, challengeFoul = 0f;
                    RoomAIDefensiveResponsePreview response = null;
                    if (moves)
                    {
                        string unavailable = string.Join(",", jumpers.Select(Key).OrderBy(k => k, StringComparer.Ordinal));
                        string key = $"{Key(collector)}|{endpoint.coordinates.x}:{endpoint.coordinates.z}|jumped:{unavailable}";
                        if (!snapshot.defensiveResponseCache.TryGetValue(key, out response))
                            snapshot.defensiveResponseCache[key] = response = RoomAIDefensiveResponse.Build(snapshot, collector, endpoint, jumpers);
                        challengeSurvival = response?.challengeSurvival ?? 1f;
                        challengeFoul = response?.challengeFoul ?? 0f;
                    }
                    bool shotBeforeDefense = snapshotShot && xg >= 0f;
                    if (moves && !shotBeforeDefense && response != null)
                        xg = Shot(collector, endpoint, "fullPower", response);
                    var chain = Compose(flight, 1f, survival, foul, xg,
                        shotBeforeDefense ? 1f : challengeSurvival, shotBeforeDefense ? 0f : challengeFoul);
                    var preview = new RoomAIActionSequencePreview { outcomes = chain.Evaluate(),
                        defensiveResponse = shotBeforeDefense ? null : response,
                        headerParticipants = new HashSet<PlayerToken>(jumpers) };
                    preview.steps.Add(Step("Collect", collector, landing, 1f, pickup.Count, remaining));
                    if (moves) preview.steps.Add(Step("Attacking movement", collector, endpoint, survival, route.Count,
                        remaining - route.Count));
                    if (moves && (!snapshotShot || xg < 0f))
                        preview.steps.Add(Step("Defensive movement / challenge (no reuse of collector in Att2f2)", collector, endpoint, challengeSurvival));
                    if (xg >= 0f) preview.steps.Add(Step(snapshotShot ? "Snapshot" : "Shot after reception/completed MP", collector, endpoint, xg));
                    else preview.steps.Add(Step(moves ? "Retain; replan legal actions after completed MP" : "Retain after reception; next MP not yet spent", collector, endpoint));
                    preview.value = Value(preview.outcomes);
                    if (moves && !boundedScatter)
                    {
                        // EndOfStandardPass/HeaderCompleted do not permit another
                        // GP. EndOfMovementPhase does: expand it ONLY after DefMP
                        // and completion of the unused-actor Att2f2 phase.
                        var pass = NextPass(collector, endpoint, response);
                        if (pass != null)
                        {
                            var prefix = Compose(flight, 1f, survival, foul, -1f, challengeSurvival, challengeFoul).Evaluate();
                            var joined = new RoomAIPossessionChain("Complete MP before next Standard Pass")
                                .AddTerminal("Earlier route turnover", prefix.defenseWinsPossessionProbability, RoomAIPossessionOutcomeKind.DefenseWinsPossession)
                                .AddTerminal("Earlier foul", prefix.attackingRestartProbability, RoomAIPossessionOutcomeKind.AttackingRestart)
                                .AddTerminal("Earlier challenge/recovery unknown", prefix.unresolvedProbability, RoomAIPossessionOutcomeKind.Unresolved)
                                .AddContinuation("Carrier retains through completed MP", prefix.attackRetainsPossessionProbability, SummaryChain(pass.outcomes));
                            float joinedValue = Value(joined.Evaluate());
                            if (joinedValue > preview.value)
                            {
                                preview.outcomes = joined.Evaluate();
                                preview.value = joinedValue;
                                preview.defensiveResponse = response;
                                // Replace the speculative shot/hold ending, not
                                // the collection/carry/defensive-response prefix.
                                preview.steps.RemoveAt(preview.steps.Count - 1);
                                if (shotBeforeDefense)
                                    preview.steps.Add(Step("Defensive movement / challenge (no reuse of collector in Att2f2)", collector, endpoint, challengeSurvival));
                                preview.steps.Add(Step("Complete MP (unused attackers only in Att2f2)", collector, endpoint));
                                preview.steps.AddRange(pass.steps);
                            }
                        }
                    }
                    if (best == null || preview.value > best.value) best = preview;
                }
            }
            if (best != null) return best;
            var unknown = new RoomAIActionSequencePreview { outcomes = new RoomAIPossessionChain("Collection exceeds one fresh MP or has no eligible collector")
                .AddTerminal("Flight intercepted", 1f - flight, RoomAIPossessionOutcomeKind.DefenseWinsPossession).Evaluate() };
            unknown.value = Value(unknown.outcomes);
            return unknown;
        }

        private RoomAIActionSequencePreview Terminal(string label, RoomAIPossessionOutcomeKind outcome)
        {
            var preview = new RoomAIActionSequencePreview { outcomes = new RoomAIPossessionChain(label).AddTerminal(label, 1f, outcome).Evaluate() };
            preview.steps.Add(new RoomAISequenceStep { action = label });
            preview.value = Value(preview.outcomes);
            return preview;
        }

        private HashSet<HexCell> KeeperRushReach(PlayerToken keeper)
        {
            if (!goalkeeperRushReach.TryGetValue(keeper, out var reachable))
                goalkeeperRushReach[keeper] = reachable = HexGridUtils.GetReachableHexes(match.hexGrid, keeper.GetCurrentHex(), 3).reachableHexes.ToHashSet();
            return reachable;
        }

        private RoomAIActionSequencePreview NextPass(PlayerToken passer, HexCell origin, RoomAIDefensiveResponsePreview response)
        {
            if (match.groundBallManager?.ball == null) return null;
            RoomAIActionSequencePreview best = null;
            foreach (var receiver in own.Where(t => t != passer && !t.IsGoalKeeper && !Stunned(t)
                && Distance(origin, t.GetCurrentHex()) <= 11
                && (response != null ? !RoomAIDefensiveResponse.IsOffside(t, origin, response.positions, AttackSign())
                    : !Offside(snapshot, RoomHexCoordinates.FromHex(t.GetCurrentHex()), RoomHexCoordinates.FromHex(origin))))
                .OrderByDescending(t => t.GetCurrentHex().CanShootFrom).ThenByDescending(t => t.shooting)
                .ThenBy(Key, StringComparer.Ordinal).Take(3))
            {
                HexCell target = receiver.GetCurrentHex();
                var path = GroundPassCommon.CalculateThickPath(match.hexGrid, origin, target, match.groundBallManager.ball.ballRadius);
                if (opponents.Any(t => path.Contains(response != null ? response.positions[t] : t.GetCurrentHex()))) continue;
                float flight = response != null ? RoomAIDefensiveResponse.PassRetention(match, passer, origin, target, response.positions)
                    : 1f - ExpectedStatsCalculator.CalculateAtLeastOneRecoveryProbability(opponents
                        .Where(t => path.Any(h => Distance(t.GetCurrentHex(), h) == 1)).ToArray());
                float xg = Shot(receiver, target, "snapshot", response);
                if (xg < 0f) continue; // No bonus for an unrelated extra pass.
                alternatives++;
                var preview = new RoomAIActionSequencePreview { outcomes = Compose(flight, 1f, 1f, 0f, xg).Evaluate() };
                preview.steps.Add(Step("GroundPass after completed MP", passer, target, flight));
                preview.steps.Add(Step("Collect", receiver, target, 1f, 0, receiver.pace));
                preview.steps.Add(Step("Snapshot", receiver, target, xg));
                preview.value = Value(preview.outcomes);
                if (best == null || preview.value > best.value) best = preview;
            }
            return best;
        }

        private bool Stunned(PlayerToken token) => match.movementPhaseManager?.stunnedTokens.Contains(token) == true
            || match.movementPhaseManager?.stunnedforNext.Contains(token) == true;
        private int AttackSign() => (snapshot.expectedTeam == "Home" ? snapshot.homeTeamDirection : snapshot.awayTeamDirection)
            == "LeftToRight" ? 1 : -1;
        private Dictionary<HexCell, List<HexCell>> Routes(PlayerToken token)
        {
            if (!reach.TryGetValue(token, out var paths)) reach[token] = paths = Paths(token.GetCurrentHex(), token.pace, token);
            return paths;
        }
        private Dictionary<HexCell, List<HexCell>> Paths(HexCell origin, int budget, PlayerToken mover, HexCell vacated = null)
        {
            var paths = new Dictionary<HexCell, List<HexCell>> { [origin] = new() };
            var queue = new Queue<HexCell>(); queue.Enqueue(origin);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (paths[current].Count >= budget || current.isInGoal != 0) continue;
                foreach (var next in current.GetNeighbors(match.hexGrid))
                {
                    if (next == null || next.isOutOfBounds || paths.ContainsKey(next)
                        || (next.isAttackOccupied || next.isDefenseOccupied) && next != mover.GetCurrentHex() && next != vacated) continue;
                    paths[next] = new List<HexCell>(paths[current]) { next }; queue.Enqueue(next);
                }
            }
            return paths;
        }
        private float Shot(PlayerToken shooter, HexCell endpoint, string type, RoomAIDefensiveResponsePreview response = null)
        {
            if (match.shotManager == null || !endpoint.CanShootFrom || endpoint.coordinates.x * AttackSign() <= 0) return -1f;
            string layout = response == null ? "live" : $"{response.carrierTokenKey}@{response.carrierHex.x},{response.carrierHex.z}|"
                + string.Join(";", response.moves.Select(m => $"{m.tokenKey}:{m.to.x},{m.to.z}"));
            string cacheKey = $"sequence|{Key(shooter)}|{endpoint.coordinates.x}:{endpoint.coordinates.z}|{type}|{layout}";
            if (!snapshot.expectedGoalsCache.TryGetValue(cacheKey, out var shot))
            {
                shot = new RoomAIExpectedGoalsPreview();
                var positions = response?.positions ?? own.Concat(opponents).ToDictionary(t => t, t => t.GetCurrentHex());
                if (response == null) positions[shooter] = endpoint;
                // Future preview must not inherit already-spent live shot/save flags.
                shot.calculated = response != null
                    ? match.shotManager.TryCalculateBestFutureExpectedGoalsPreview(shooter, endpoint, type, response.positions,
                        out shot.expectedGoals, out shot.shootingPenalty, out shot.blockerCount, out shot.goalkeeperPenalty)
                    : match.shotManager.TryCalculateBestFutureExpectedGoalsPreview(shooter, endpoint, type, positions,
                        out shot.expectedGoals, out shot.shootingPenalty, out shot.blockerCount, out shot.goalkeeperPenalty);
                snapshot.expectedGoalsCache[cacheKey] = shot;
            }
            return shot.calculated ? shot.expectedGoals : -1f;
        }
    }

    private static RoomAIPossessionChain SummaryChain(RoomAIPossessionChainSummary p) => new RoomAIPossessionChain("Evaluated continuation")
        .AddTerminal("Goal", p.goalProbability, RoomAIPossessionOutcomeKind.Goal)
        .AddTerminal("Retained", p.attackRetainsPossessionProbability, RoomAIPossessionOutcomeKind.AttackRetainsPossession)
        .AddTerminal("Defense wins", p.defenseWinsPossessionProbability, RoomAIPossessionOutcomeKind.DefenseWinsPossession)
        .AddTerminal("Attacking restart", p.attackingRestartProbability, RoomAIPossessionOutcomeKind.AttackingRestart)
        .AddTerminal("Defending restart", p.defendingRestartProbability, RoomAIPossessionOutcomeKind.DefendingRestart)
        .AddTerminal("Unexpanded", p.unresolvedProbability, RoomAIPossessionOutcomeKind.Unresolved);
}

[Serializable]
public sealed class RoomAIProjectedDefensiveMove
{
    public string tokenKey;
    public string responsibility;
    public RoomHexCoordinates from;
    public RoomHexCoordinates to;
    public int cost;
}

[Serializable]
public sealed class RoomAIDefensiveResponsePreview
{
    public List<RoomAIProjectedDefensiveMove> moves = new();
    public int moveLimit;
    public string presserTokenKey;
    public string carrierTokenKey;
    public RoomHexCoordinates carrierHex;
    public float challengeSurvival = 1f;
    public float challengeFoul;
    public float challengeWin;
    public float challengeTie;
    public float threatBefore;
    public float threatAfter;
    public bool bounded = true;
    public string scope = "One greedy complementary DefMP; no double-team, dice relocation or attacking off-ball reply expanded";
    internal Dictionary<PlayerToken, HexCell> positions;
    public IReadOnlyDictionary<PlayerToken, HexCell> CopyProjectedPositions()
        => new Dictionary<PlayerToken, HexCell>(positions);
}

/// <summary>Concrete, legal projected responses rather than a reach-radius
/// penalty on a shot against an unchanged defense. All board changes are local
/// dictionary entries; live scene objects and occupancy flags are never changed.</summary>
public static class RoomAIDefensiveResponse
{
    private static string Key(PlayerToken t) => MatchManager.GetStableTokenKey(t);
    private static int Distance(HexCell a, HexCell b) => HexGridUtils.GetHexStepDistance(a, b);

    public static bool IsOffside(PlayerToken receiver, HexCell ball,
        IReadOnlyDictionary<PlayerToken, HexCell> positions, int attackSign)
    {
        if (receiver == null || ball == null || !positions.TryGetValue(receiver, out var hex) || hex == null) return false;
        var line = positions.Where(p => p.Key.isHomeTeam != receiver.isHomeTeam && p.Key.isPlaying && !p.Key.isSentOff && p.Value != null)
            .Select(p => p.Value.coordinates.x * attackSign).OrderByDescending(x => x).ToArray();
        return line.Length >= 2 && hex.coordinates.x * attackSign > Math.Max(0, Math.Max(ball.coordinates.x * attackSign, line[1]));
    }

    public static float PassRetention(MatchManager match, PlayerToken passer, HexCell origin, HexCell landing,
        IReadOnlyDictionary<PlayerToken, HexCell> positions)
    {
        if (match?.groundBallManager?.ball == null) return 0f;
        var path = GroundPassCommon.CalculateThickPath(match.hexGrid, origin, landing, match.groundBallManager.ball.ballRadius);
        var defenders = positions.Where(p => p.Key.isPlaying && !p.Key.isSentOff && p.Key.isHomeTeam != passer.isHomeTeam && p.Value != null).ToList();
        if (defenders.Any(p => path.Contains(p.Value))) return 0f;
        return 1f - ExpectedStatsCalculator.CalculateAtLeastOneRecoveryProbability(defenders
            .Where(p => path.Any(h => Distance(h, p.Value) == 1)).Select(p => p.Key).ToArray());
    }

    public static RoomAIDefensiveResponsePreview Build(RoomAIDecisionSnapshot snapshot, PlayerToken collector, HexCell endpoint,
        ISet<PlayerToken> unavailableMovers = null, IReadOnlyDictionary<PlayerToken, HexCell> initialPositions = null)
    {
        var match = MatchManager.Instance;
        if (snapshot == null || collector == null || endpoint == null || match?.playerTokenManager == null
            || match.hexGrid == null || match.groundBallManager?.ball == null) return null;
        var board = initialPositions != null ? new Dictionary<PlayerToken, HexCell>(initialPositions)
            : match.playerTokenManager.allTokens.Where(t => t != null && t.isPlaying && !t.isSentOff && t.GetCurrentHex() != null)
                .ToDictionary(t => t, t => t.GetCurrentHex());
        if (!board.ContainsKey(collector) || board.Any(p => p.Key != collector && p.Value == endpoint)) return null;
        board[collector] = endpoint;
        var response = new RoomAIDefensiveResponsePreview { positions = board,
            carrierTokenKey = Key(collector), carrierHex = RoomHexCoordinates.FromHex(endpoint),
            moveLimit = match.movementPhaseManager?.DefendingMoveLimit ?? MovementPhaseManager.DefaultDefendingMoveLimit };
        var planner = new Planner(snapshot, match, collector, response, unavailableMovers);
        planner.Run();
        return response;
    }

    private sealed class Planner
    {
        private readonly RoomAIDecisionSnapshot snapshot;
        private readonly MatchManager match;
        private readonly PlayerToken carrier;
        private readonly RoomAIDefensiveResponsePreview result;
        private readonly Dictionary<PlayerToken, HexCell> board;
        private readonly List<PlayerToken> defenders, outlets;
        private readonly HashSet<PlayerToken> used = new();
        private readonly int sign;
        private readonly Dictionary<PlayerToken, List<HexCell>> shotPaths = new();
        private readonly Dictionary<PlayerToken, List<HexCell>> passPaths = new();
        private readonly Dictionary<HexCell, Dictionary<PlayerToken, float>> coverGeometry = new();
        private readonly Dictionary<PlayerToken, float> threatWeights = new();

        public Planner(RoomAIDecisionSnapshot snapshot, MatchManager match, PlayerToken carrier, RoomAIDefensiveResponsePreview result,
            ISet<PlayerToken> unavailableMovers)
        {
            this.snapshot = snapshot; this.match = match; this.carrier = carrier; this.result = result; board = result.positions;
            sign = (snapshot.expectedTeam == "Home" ? snapshot.homeTeamDirection : snapshot.awayTeamDirection) == "LeftToRight" ? 1 : -1;
            defenders = board.Keys.Where(t => t.isHomeTeam != carrier.isHomeTeam && !Stunned(t)
                && unavailableMovers?.Contains(t) != true).OrderBy(Key, StringComparer.Ordinal).ToList();
            outlets = board.Keys.Where(t => t != carrier && t.isHomeTeam == carrier.isHomeTeam && !t.IsGoalKeeper
                && !Stunned(t) && Distance(board[carrier], board[t]) <= 11)
                .OrderByDescending(t => board[t].CanShootFrom && board[t].coordinates.x * sign > 0)
                .ThenByDescending(t => t.shooting).ThenBy(Key, StringComparer.Ordinal).Take(3).ToList();
            foreach (var shooter in outlets.Concat(new[] { carrier }))
            {
                shotPaths[shooter] = board[shooter].ShootingPaths?.Where(p => p.Key.isInGoal == sign)
                    .SelectMany(p => p.Value).Distinct().ToList() ?? new List<HexCell>();
                if (shooter != carrier) passPaths[shooter] = GroundPassCommon.CalculateThickPath(match.hexGrid,
                    board[carrier], board[shooter], match.groundBallManager.ball.ballRadius);
            }
        }

        public void Run()
        {
            result.threatBefore = WorstThreat();
            result.threatAfter = result.threatBefore;
            if (result.moveLimit <= 0) return;
            // One reachable primary presser; each subsequent actor covers a
            // different continuation instead of duplicating carrier pressure.
            PlayerToken presser = null;
            HexCell pressureHex = null;
            int pressureCost = 0;
            float strongestLoss = 0f;
            foreach (var defender in defenders.Where(t => !t.IsGoalKeeper))
            {
                var reachable = InitialPressureRoutes(defender);
                var target = board[carrier].GetNeighbors(match.hexGrid).Where(h => h != null && reachable.ContainsKey(h)
                    && !reachable[h].Contains(board[carrier]))
                    .OrderBy(h => reachable[h].Count).ThenBy(h => h.coordinates.x).ThenBy(h => h.coordinates.z).FirstOrDefault();
                if (target == null) continue;
                var duel = ExpectedStatsCalculator.CalculateGroundDuelExpectation(carrier, defender);
                if (duel.xTackles <= strongestLoss) continue;
                strongestLoss = duel.xTackles; presser = defender; pressureHex = target; pressureCost = reachable[target].Count;
            }
            if (presser != null)
            {
                var duel = ExpectedStatsCalculator.CalculateGroundDuelExpectation(carrier, presser);
                HexCell before = board[presser];
                board[presser] = pressureHex;
                float threat = WorstThreat();
                // A move which opens a much worse continuation is not justified
                // by the mere existence of an adjacent tackle.
                if (threat * duel.dribbleContinuationProbability <= result.threatBefore + .00001f)
                {
                    used.Add(presser);
                    result.presserTokenKey = Key(presser);
                    result.challengeSurvival = duel.attackerWinProbability;
                    result.challengeFoul = duel.defenderFoulProbability;
                    result.challengeWin = duel.defenderWinProbability;
                    result.challengeTie = duel.tieProbability;
                    AddMove(presser, before, pressureHex, "Primary pressure", pressureCost);
                    result.threatAfter = threat;
                }
                else board[presser] = before;
            }
            bool keeperAvailable = defenders.Any(t => t.IsGoalKeeper);
            int outfieldBudget = Math.Max(0, result.moveLimit - (keeperAvailable ? 1 : 0));
            while (used.Count < outfieldBudget)
            {
                RefreshThreatWeights();
                if (threatWeights.Values.All(value => value <= 0f)) break;
                PlayerToken selected = null; HexCell destination = null; int cost = 0;
                float bestGain = .00001f;
                foreach (var defender in defenders.Where(t => !t.IsGoalKeeper && !used.Contains(t)))
                {
                    float originalCover = Cover(board[defender]);
                    foreach (var candidate in Paths(defender).Where(p => p.Key != board[defender]
                        && Distance(p.Key, board[carrier]) != 1))
                    {
                        float gain = Cover(candidate.Key) - originalCover - candidate.Value * .005f;
                        if (gain <= bestGain) continue;
                        bestGain = gain; selected = defender; destination = candidate.Key; cost = candidate.Value;
                    }
                }
                if (selected == null) break;
                HexCell before = board[selected];
                board[selected] = destination;
                float threat = WorstThreat();
                used.Add(selected); // rejected actor is not retried indefinitely
                if (threat <= result.threatAfter + .00001f)
                {
                    AddMove(selected, before, destination, "Shot / outlet coverage", cost);
                    result.threatAfter = threat;
                }
                else board[selected] = before;
            }
            var keeper = defenders.FirstOrDefault(t => t.IsGoalKeeper);
            if (keeper != null && result.moves.Count < result.moveLimit && result.threatAfter > 0f)
            {
                RefreshThreatWeights();
                HexCell before = board[keeper], best = before;
                int bestCost = 0; float bestThreat = result.threatAfter;
                // Exact worst-shot value decides GK placement; wall count only
                // shortlists legal candidates, never overrides a worse threat.
                foreach (var candidate in Paths(keeper).Where(p => p.Key.isInPenaltyBox == sign && Distance(p.Key, board[carrier]) != 1)
                    .OrderByDescending(p => Cover(p.Key)).ThenBy(p => p.Value).ThenBy(p => p.Key.coordinates.x)
                    .ThenBy(p => p.Key.coordinates.z).Take(6))
                {
                    board[keeper] = candidate.Key;
                    float threat = WorstThreat();
                    if (threat >= bestThreat - .00001f) continue;
                    bestThreat = threat; best = candidate.Key; bestCost = candidate.Value;
                }
                board[keeper] = best;
                if (best != before) AddMove(keeper, before, best, "Minimize carrier / one-pass scoring threat", bestCost);
                result.threatAfter = bestThreat;
            }
        }

        private void AddMove(PlayerToken token, HexCell before, HexCell after, string responsibility, int cost)
            => result.moves.Add(new RoomAIProjectedDefensiveMove { tokenKey = Key(token), responsibility = responsibility,
                from = RoomHexCoordinates.FromHex(before), to = RoomHexCoordinates.FromHex(after), cost = cost });
        private bool Stunned(PlayerToken t) => match.movementPhaseManager?.stunnedTokens.Contains(t) == true
            || match.movementPhaseManager?.stunnedforNext.Contains(t) == true;
        private Dictionary<HexCell, List<HexCell>> InitialPressureRoutes(PlayerToken mover)
        {
            string key = $"{Key(mover)}|{Key(carrier)}";
            if (snapshot.defensiveResponsePrimaryReach.TryGetValue(key, out var cached)) return cached;
            // Shared across projected carry endpoints. Ignore the collector's
            // vacated source; reject any cached route crossing its new endpoint
            // at selection time. Alternate detours remain outside this shortlist.
            var occupied = match.playerTokenManager.allTokens.Where(t => t != null && t.isPlaying && !t.isSentOff
                && t != mover && t != carrier && t.GetCurrentHex() != null).Select(t => t.GetCurrentHex()).ToHashSet();
            var start = mover.GetCurrentHex();
            var paths = new Dictionary<HexCell, List<HexCell>> { [start] = new() };
            var queue = new Queue<HexCell>(); queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (paths[current].Count >= mover.pace) continue;
                foreach (var next in current.GetNeighbors(match.hexGrid))
                {
                    if (next == null || next.isOutOfBounds || occupied.Contains(next) || paths.ContainsKey(next)) continue;
                    paths[next] = new List<HexCell>(paths[current]) { next }; queue.Enqueue(next);
                }
            }
            snapshot.defensiveResponsePrimaryReach[key] = paths;
            return paths;
        }
        private Dictionary<HexCell, int> Paths(PlayerToken mover) => PathsFrom(board[mover], mover);
        private Dictionary<HexCell, int> PathsFrom(HexCell origin, PlayerToken mover)
        {
            var occupied = board.Where(p => p.Key != mover).Select(p => p.Value).ToHashSet();
            var paths = new Dictionary<HexCell, int> { [origin] = 0 };
            var queue = new Queue<HexCell>(); queue.Enqueue(origin);
            while (queue.Count > 0)
            {
                HexCell current = queue.Dequeue();
                if (paths[current] >= mover.pace) continue;
                foreach (var next in current.GetNeighbors(match.hexGrid))
                {
                    if (next == null || next.isOutOfBounds || occupied.Contains(next) || paths.ContainsKey(next)) continue;
                    paths[next] = paths[current] + 1; queue.Enqueue(next);
                }
            }
            return paths;
        }
        private float Cover(HexCell hex)
        {
            if (!coverGeometry.TryGetValue(hex, out var geometry))
            {
                geometry = new Dictionary<PlayerToken, float>();
                foreach (var shooter in shotPaths.Keys)
                {
                    float value = shotPaths[shooter].Contains(hex) ? 1f
                        : shotPaths[shooter].Any(h => Distance(h, hex) == 1) ? .3f : 0f;
                    if (passPaths.TryGetValue(shooter, out var path))
                        value += path.Contains(hex) ? 1.5f : path.Any(h => Distance(h, hex) == 1) ? .4f : 0f;
                    geometry[shooter] = value;
                }
                coverGeometry[hex] = geometry;
            }
            return geometry.Sum(p => p.Value * threatWeights.GetValueOrDefault(p.Key));
        }
        private void RefreshThreatWeights()
        {
            threatWeights[carrier] = Shot(carrier, "fullPower");
            foreach (var receiver in outlets)
                threatWeights[receiver] = IsOffside(receiver, board[carrier], board, sign) ? 0f
                    : PassRetention(match, carrier, board[carrier], board[receiver], board) * Shot(receiver, "snapshot");
        }
        private float WorstThreat()
        {
            float worst = Shot(carrier, "fullPower");
            foreach (var receiver in outlets)
            {
                if (IsOffside(receiver, board[carrier], board, sign)) continue;
                float flight = PassRetention(match, carrier, board[carrier], board[receiver], board);
                if (flight <= 0f) continue;
                worst = Math.Max(worst, flight * Shot(receiver, "snapshot"));
            }
            return worst;
        }
        private float Shot(PlayerToken shooter, string type) => match.shotManager != null && board[shooter].coordinates.x * sign > 0
            && match.shotManager.TryCalculateBestFutureExpectedGoalsPreview(shooter, board[shooter], type, board,
                out float xg, out _, out _, out _) ? Math.Max(0f, xg) : 0f;
    }
}
