using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Board-level constraints shared by live planning and saved-position regressions.</summary>
public static class RoomAITacticalSafety
{
    /// <summary>Setup-only pressure and centre-back recovery estimate at a
    /// ready kickoff receiver. Geometric distances intentionally do not claim
    /// that a tackle, turnover or recovery will occur.</summary>
    public static float KickoffOpeningRecoveryExposure(RoomAIDecisionSnapshot snapshot,
        string movedKey, RoomHexCoordinates destination)
    {
        var opening = snapshot?.kickoffPlan;
        if (opening?.firstPassEvaluated != true || !opening.firstActionReady || opening.landingHex == null
            || opening.team != snapshot.expectedTeam || snapshot.expectedTeam != snapshot.teamInAttack
            || snapshot.tokens == null) return 0f;
        RoomHexCoordinates At(RoomAITokenSnapshot token) => token.tokenKey == movedKey && destination != null
            ? destination : token.currentHex;
        float pressure = snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff && !t.isGoalKeeper
                && t.teamSide != snapshot.expectedTeam && t.currentHex != null)
            .Select(t => Math.Max(0f, Math.Min(1f,
                (t.pace + 2f - Distance(t.currentHex, opening.landingHex)) / Math.Max(1f, t.pace + 1f)))
                * (.35f + .65f * Math.Max(0f, Math.Min(1f, t.tackling / 6f))))
            .DefaultIfEmpty(0f).Max();
        if (pressure <= 0f) return 0f;
        var gaps = snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff
                && t.teamSide == snapshot.expectedTeam && t.currentHex != null && t.jerseyNumber is 4 or 5
                && t.tokenKey != opening.receiverTokenKey)
            .Select(t => Math.Min(6, Math.Max(0, Distance(At(t), opening.landingHex) - Math.Max(1, t.pace) - 1)))
            .ToList();
        return pressure * gaps.Sum();
    }

    /// <summary>Geometric overlap in opponents able to challenge either outlet
    /// after reception. This is a modest pair-ranking signal, not a prediction
    /// that a defender can tackle both players in one movement phase.</summary>
    public static float SharedOutletChallengeExposure(RoomAIDecisionSnapshot snapshot,
        RoomHexCoordinates first, RoomHexCoordinates second)
    {
        if (snapshot?.tokens == null || first == null || second == null) return 0f;
        float largest = 0f;
        foreach (var opponent in snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff
            && !t.isGoalKeeper && t.teamSide != snapshot.expectedTeam && t.currentHex != null))
        {
            int reach = Math.Max(1, opponent.pace + 1);
            float firstReach = Math.Max(0f, (reach + 1f - Distance(opponent.currentHex, first)) / reach);
            float secondReach = Math.Max(0f, (reach + 1f - Distance(opponent.currentHex, second)) / reach);
            float quality = Math.Min(1f, Math.Max(0f, opponent.tackling) / 6f);
            largest = Math.Max(largest, Math.Min(firstReach, secondReach) * quality);
        }
        return Math.Min(1f, largest);
    }

    public static bool IsDirectFtpRelayWithinRange(RoomHexCoordinates origin, RoomHexCoordinates target)
    {
        int distance = Distance(origin, target);
        return distance > 0 && distance <= FirstTimePassManager.MaxPassDistanceForDecisionPreview;
    }

    private sealed class KeeperFtpRelay
    {
        public RoomAITokenSnapshot outlet;
        public RoomAITokenSnapshot receiver;
        public List<HexCell> path;
        public HashSet<HexCell> interceptionHexes;
    }

    /// <summary>Bounded landing/header/relay danger, not a tackle of the committed
    /// passer. Used only during the defender's special high-pass movement.</summary>
    public static float CommittedAerialDanger(RoomAIDecisionSnapshot snapshot,
        string movedKey = null, RoomHexCoordinates destination = null)
    {
        var landing = snapshot?.committedAerialTarget;
        if (landing == null) return 0f;
        var players = snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff
            && t.currentHex != null).ToList();
        RoomHexCoordinates At(RoomAITokenSnapshot token) => token.tokenKey == movedKey && destination != null
            ? destination : token.currentHex;
        var defenders = players.Where(t => t.teamSide == snapshot.expectedTeam && !t.isGoalKeeper).ToList();
        var attackers = players.Where(t => t.teamSide != snapshot.expectedTeam && !t.isGoalKeeper).ToList();
        int header = attackers.Where(t => Distance(t.currentHex, landing) <= HeaderManager.HEADER_SELECTION_RANGE)
            .Select(t => t.heading).DefaultIfEmpty(0).Max();
        var contestants = defenders.Where(t => Distance(At(t), landing) <= HeaderManager.HEADER_SELECTION_RANGE).ToList();
        float attackerWins = 1f;
        if (contestants.Count > 0)
        {
            int defendingHeader = contestants.Max(t => t.heading);
            attackerWins = ExpectedStatsCalculator.CalculateHeadToHeadAerialControlProbability(header, defendingHeader);
        }
        int sign = GoalCenter(snapshot)?.x > 0 ? 1 : -1;
        float relayDanger = 0f;
        foreach (var receiver in attackers.Where(t => Distance(t.currentHex, landing) <= 6))
        {
            float phases = Math.Max(0, 12 - sign * receiver.currentHex.x) / (float)Math.Max(1, receiver.pace);
            float cover = defenders.Select(t => Math.Min(1f, .20f
                + Math.Max(0, Distance(At(t), receiver.currentHex) - 1) / (float)Math.Max(1, t.pace) * .5f
                + Math.Max(0, sign * (receiver.currentHex.x - At(t).x)) * .14f))
                .DefaultIfEmpty(1f).Min();
            relayDanger = Math.Max(relayDanger, receiver.shooting / 6f / (1f + phases * phases) * cover);
        }
        var backs = defenders.Where(t => t.jerseyNumber is 4 or 5).ToList();
        int gap = backs.Count == 2 ? Math.Abs(At(backs[0]).x - At(backs[1]).x) : 0;
        return attackerWins * (.45f + relayDanger) + gap * gap * .012f;
    }

    public static RoomHexCoordinates GoalCenter(RoomAIDecisionSnapshot snapshot)
    {
        string direction = snapshot.expectedTeam == "Home" ? snapshot.homeTeamDirection : snapshot.awayTeamDirection;
        return direction == "LeftToRight" ? new() { x = -18, z = 0 }
            : direction == "RightToLeft" ? new() { x = 18, z = 0 } : null;
    }

    /// <summary>Tactical restrictions, applied after search at every difficulty.
    /// Keep the managers' legal destinations as the authority, including when the
    /// central recovery cell is occupied or inside the restart exclusion area.</summary>
    public static void ConstrainGoalkeeperChoices(RoomAIDecisionSnapshot snapshot, List<RoomAITacticalEvaluation> choices)
    {
        bool Keeper(RoomAITacticalEvaluation e) => e.explanation?.actorToken?.isGoalKeeper == true
            || e.candidate.actor?.IsGoalKeeper == true;
        bool first = snapshot.currentState == "FreeKickDefGK1";
        bool later = snapshot.currentState == "FreeKickDefGK2";
        bool ordinary = snapshot.currentState is "FreeKickDef1" or "FreeKickDef2"
            or "FreeKickDef3" or "FreeKickDefMovement3";
        if (ordinary)
        {
            choices.RemoveAll(e => Keeper(e) && e.candidate.manager == nameof(FreeKickManager));
            return;
        }
        var center = GoalCenter(snapshot);
        if (center == null) return;
        RoomHexCoordinates Position(RoomAITacticalEvaluation e) => e.explanation?.targetHex
            ?? RoomHexCoordinates.FromHex(e.candidate.targetHex) ?? e.explanation?.actorHex
            ?? RoomHexCoordinates.FromHex(e.candidate.actor?.GetCurrentHex());
        if (first || later)
        {
            var keepers = choices.Where(Keeper).ToList();
            bool hasTargets = keepers.Any(e => e.candidate.step == RoomDecisionStep.ChooseTarget);
            bool hasActor = keepers.Any(e => e.candidate.step == RoomDecisionStep.ChooseActor);
            bool centeredStay = first && keepers.Any(e => e.candidate.step != RoomDecisionStep.ChooseActor
                && Distance(Position(e), center) == 0);
            // Once destinations exist, switching back to the same actor is not
            // another useful action and can otherwise cause a selection loop.
            if (hasTargets || centeredStay) choices.RemoveAll(e => Keeper(e) && e.candidate.step == RoomDecisionStep.ChooseActor);
            keepers = choices.Where(Keeper).ToList();
            int nearest = keepers.Where(e => e.candidate.step != RoomDecisionStep.ChooseActor)
                .Select(e => Distance(Position(e), center)).DefaultIfEmpty(int.MaxValue).Min();
            foreach (var e in keepers)
            {
                int distance = Distance(Position(e), center);
                bool actor = e.candidate.step == RoomDecisionStep.ChooseActor;
                bool allowed = first ? distance == nearest : distance <= 3;
                // Before selection, offer the keeper rather than skipping his
                // initial return. A keeper already at center can stay there.
                if (actor) allowed = true;
                if (first && !hasTargets && !actor && distance > 0) allowed = false;
                if (later && nearest > 3 && !actor)
                    allowed = e.candidate.step == RoomDecisionStep.InterruptionChoice && (hasTargets || !hasActor);
                e.safetyPriority = allowed ? 0 : 100;
                e.reasons.Insert(0, first
                    ? $"Initial defensive GK returns to ({center.x}, 0); legal recovery distance={distance}"
                    : $"Subsequent defensive GK re-evaluates within 3 hexes of ({center.x}, 0); distance={distance}");
                if (nearest > (first ? 0 : 3) && !actor)
                    e.reasons.Insert(0, first ? "Central recovery area unavailable: prefer the closest manager-legal position"
                        : "No legal destination within the central area: keep the current position instead of inventing a move");
            }
            // Remove forbidden destinations from the selection pool, including
            // Easy/Medium randomness. Retain the evaluated legal fallback only
            // when the requested area cannot be reached legally.
            if (choices.Any(e => e.safetyPriority == 0)) choices.RemoveAll(e => Keeper(e) && e.safetyPriority == 100);
            if (later) AssignSafetyPriorities(choices);
            return;
        }
        foreach (var e in choices.Where(Keeper))
        {
            if (e.candidate.actionType is not (RoomActionType.StartMovement or RoomActionType.FinalThird)
                && !(e.candidate.manager == nameof(HighPassManager) && e.candidate.actionType == RoomActionType.SetupMove)
                && !(e.candidate.manager == nameof(GoalKeeperManager) && e.candidate.actionType == RoomActionType.GoalkeeperSave)) continue;
            var facts = e.explanation;
            if (facts == null) continue;
            var target = facts.targetHex ?? facts.goalkeeperActorBestDestinationHex;
            if (target == null) continue;
            if (IsGoalkeeperRecovery(snapshot, facts))
            {
                e.score += (Distance(facts.actorHex, center) - Distance(target, center)) * 150;
                e.tier = RoomAITacticalTier.SafeProgression;
                e.reasons.Insert(0, "Recover the displaced goalkeeper toward the central goal area");
                continue;
            }
            if (Distance(target, center) <= 3) continue;
            bool carries = snapshot.attackHasPossession && snapshot.teamInAttack == snapshot.expectedTeam
                && Distance(facts.actorHex, snapshot.ball) == 0;
            bool winsLooseBall = !snapshot.attackHasPossession && Distance(target, snapshot.ball) == 0
                && facts.passRace is { collectionPhases: 1, opponentCollectsFirst: false };
            bool savesChance = facts.goalkeeperPositioningCurrentMaximumExpectedGoals >= 0f
                && facts.goalkeeperPositioningMaximumExpectedGoals >= 0f
                && facts.goalkeeperPositioningCurrentMaximumExpectedGoals - facts.goalkeeperPositioningMaximumExpectedGoals >= .10f;
            if (carries || winsLooseBall || savesChance) continue;
            e.safetyPriority = Math.Max(2, e.safetyPriority);
            e.reasons.Insert(0, "Keep goalkeeper near the central goal area; no collection or material save benefit justifies this excursion");
        }
    }

    public static bool IsGoalkeeperRecovery(RoomAIDecisionSnapshot snapshot, RoomAICandidateExplanation facts)
    {
        var center = GoalCenter(snapshot);
        var target = facts?.targetHex ?? facts?.goalkeeperActorBestDestinationHex;
        return center != null && facts?.actorHex != null && target != null
            && Distance(facts.actorHex, center) > 3 && Distance(target, center) < Distance(facts.actorHex, center)
            && (facts.goalkeeperPositioningMaximumExpectedGoals < 0f
                || facts.goalkeeperPositioningCurrentMaximumExpectedGoals < 0f
                || facts.goalkeeperPositioningMaximumExpectedGoals <= facts.goalkeeperPositioningCurrentMaximumExpectedGoals + .05f);
    }

    public static void ProtectPossession(IReadOnlyList<RoomAITacticalEvaluation> choices)
    {
        bool Comparable(RoomAITacticalEvaluation e) => e.safetyPriority == 0
            && e.explanation?.isAttackingOffBallMove != true
            && e.candidate.step is (RoomDecisionStep.ChooseTarget or RoomDecisionStep.Confirm)
            && e.candidate.actionType is (RoomActionType.GroundPass or RoomActionType.HighPass
                or RoomActionType.GoalkeeperKick or RoomActionType.QuickThrow or RoomActionType.StartMovement)
            && e.explanation?.hasPossessionLevelPlan == true
            && e.explanation.possessionPlanEffectiveRetentionProbability >= 0f
            && e.explanation.possessionPlanThreatExpectedGoals >= 0f;
        var routes = choices.Where(Comparable).ToList();
        foreach (var e in routes)
        {
            var facts = e.explanation;
            var safer = routes.FirstOrDefault(other => other != e
                && (other.candidate.step == e.candidate.step
                    || other.candidate.actionType == RoomActionType.GroundPass
                        && e.candidate.actionType == RoomActionType.GroundPass)
                && other.explanation.possessionPlanEffectiveRetentionProbability - facts.possessionPlanEffectiveRetentionProbability > .05f
                && facts.possessionPlanThreatExpectedGoals - other.explanation.possessionPlanThreatExpectedGoals
                    + ProgressionReward(facts, other.explanation)
                    < (other.explanation.possessionPlanEffectiveRetentionProbability - facts.possessionPlanEffectiveRetentionProbability) * .35f);
            if (safer == null) continue;
            e.safetyPriority = Math.Max(1, e.safetyPriority);
            e.reasons.Insert(0, $"Possession protection: retention={facts.possessionPlanEffectiveRetentionProbability:0.000}, "
                + $"route xG={facts.possessionPlanThreatExpectedGoals:0.000}; safer alternative retains "
                + $"{safer.explanation.possessionPlanEffectiveRetentionProbability:0.000} without sacrificing a material chance");
        }
    }

    private static float ProgressionReward(RoomAICandidateExplanation route, RoomAICandidateExplanation safer)
    {
        if (!route.isGroundPassContinuation || route.groundPassCollectorRemainingPace < 0
            || route.possessionPlanEffectiveRetentionProbability < .65f
            || safer.possessionPlanEffectiveRetentionProbability - route.possessionPlanEffectiveRetentionProbability > .15f)
            return 0f;
        // A reachable advancing reception is a real attacking reward before it
        // produces a shot. It can justify a small risk, never a forced turnover.
        return Math.Min(.06f, Math.Max(0, route.forwardProgressDelta - safer.forwardProgressDelta) * .01f);
    }

    public static float OutletCrowdingCost(RoomAIDecisionSnapshot snapshot, string actorKey, RoomHexCoordinates at)
    {
        if (snapshot?.ball == null || at == null || Distance(snapshot.ball, at) > 11) return 0f;
        // Two receivers in the same tiny pocket are not two independent outlets.
        // This is a marginal penalty; width alone does not prove a safe pass.
        float carrierPocket = Distance(snapshot.ball, at) == 1 ? 10f : 0f;
        return carrierPocket + snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff && !t.isGoalKeeper
                && t.teamSide == snapshot.expectedTeam && t.tokenKey != actorKey && t.currentHex != null
                && Distance(t.currentHex, snapshot.ball) is > 0 and <= 11
                && HasUsableExistingGroundOutlet(snapshot, t))
            .Select(t => Math.Max(0, 4 - Distance(t.currentHex, at)) * 8f).DefaultIfEmpty(0f).Max();
    }

    private static bool HasUsableExistingGroundOutlet(RoomAIDecisionSnapshot snapshot, RoomAITokenSnapshot receiver)
    {
        if (snapshot.existingGroundOutletUsability.TryGetValue(receiver.tokenKey, out bool cached)) return cached;
        var match = MatchManager.Instance;
        var ball = match?.ball;
        var grid = match?.hexGrid;
        var live = match?.playerTokenManager?.allTokens;
        // Synthetic snapshots may have no matching live board. Preserve the
        // conservative geometry estimate instead of consulting another match.
        if (ball?.GetCurrentHex() == null || grid == null || live == null
            || Distance(snapshot.ball, RoomHexCoordinates.FromHex(ball.GetCurrentHex())) != 0)
            return true;
        if (!snapshot.existingGroundOutletBoardMatchesLive.HasValue)
        {
            var livePositions = live.Where(t => t != null && t.isPlaying && !t.isSentOff && t.GetCurrentHex() != null)
                .GroupBy(MatchManager.GetStableTokenKey).Where(g => !string.IsNullOrEmpty(g.Key))
                .ToDictionary(g => g.Key, g => g.First());
            var recorded = snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff && t.currentHex != null).ToList();
            snapshot.existingGroundOutletBoardMatchesLive = livePositions.Count == recorded.Count
                && recorded.All(t => livePositions.TryGetValue(t.tokenKey, out var player)
                    && Distance(t.currentHex, RoomHexCoordinates.FromHex(player.GetCurrentHex())) == 0);
        }
        if (snapshot.existingGroundOutletBoardMatchesLive != true) return true;
        var token = live.FirstOrDefault(t => t != null && MatchManager.GetStableTokenKey(t) == receiver.tokenKey);
        if (token?.GetCurrentHex() == null
            || Distance(receiver.currentHex, RoomHexCoordinates.FromHex(token.GetCurrentHex())) != 0)
            return true;
        bool usable = !RoomAIActionSequence.Offside(snapshot, receiver.currentHex, snapshot.ball)
            && GroundPassCommon.ValidateStandardPassPath(grid, ball, token.GetCurrentHex(), 11,
                logFailures: false).IsValid;
        snapshot.existingGroundOutletUsability[receiver.tokenKey] = usable;
        return usable;
    }

    public static int Distance(RoomHexCoordinates a, RoomHexCoordinates b)
    {
        if (a == null || b == null) return int.MaxValue;
        int az = a.z - (a.x - (a.x & 1)) / 2;
        int bz = b.z - (b.x - (b.x & 1)) / 2;
        int dx = a.x - b.x;
        int dz = az - bz;
        return (Math.Abs(dx) + Math.Abs(dz) + Math.Abs(dx + dz)) / 2;
    }

    public static void EvaluateDefensiveLine(RoomAIDecisionSnapshot snapshot, RoomAICandidateExplanation facts)
    {
        if (snapshot == null || facts == null) return;
        bool left = (snapshot.expectedTeam == "Home" ? snapshot.homeTeamDirection : snapshot.awayTeamDirection) == "LeftToRight";
        var defenders = snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff && t.currentHex != null && t.teamSide == snapshot.expectedTeam).ToList();
        if (defenders.Count < 2) return;
        int sign = left ? 1 : -1;
        RoomHexCoordinates At(RoomAITokenSnapshot t) => t.tokenKey == facts.actorTokenKey && facts.targetHex != null ? facts.targetHex : t.currentHex;
        var before = defenders.OrderBy(t => sign * t.currentHex.x).ToList();
        var after = defenders.OrderBy(t => sign * At(t).x).ToList();
        int oldLine = before[1].currentHex.x;
        int newLine = At(after[1]).x;
        facts.currentSecondDefenderLineX = oldLine;
        facts.proposedSecondDefenderLineX = newLine;
        facts.offsideLineRaise = sign * (newLine - oldLine);
        facts.actorDefinesSecondDefenderLine = before[1].tokenKey == facts.actorTokenKey;
        // The ball can hold the line, and attackers in their own half cannot be offside.
        int oldLimit = sign * oldLine;
        int newLimit = sign * newLine;
        if (snapshot.ball != null) { oldLimit = Math.Min(oldLimit, sign * snapshot.ball.x); newLimit = Math.Min(newLimit, sign * snapshot.ball.x); }
        facts.reachableOnsideOpponents = 0;
        facts.newlyOnsideOpponents = 0;
        facts.opponentsMadeOffside = 0;
        foreach (var attacker in snapshot.tokens.Where(t => t.isPlaying && t.currentHex != null && t.teamSide != snapshot.expectedTeam))
        {
            bool ownHalf = sign * attacker.currentHex.x >= 0;
            bool wasOnside = ownHalf || sign * attacker.currentHex.x >= oldLimit;
            bool isOnside = ownHalf || sign * attacker.currentHex.x >= newLimit;
            if (isOnside && !wasOnside) facts.newlyOnsideOpponents++;
            if (!isOnside && wasOnside) facts.opponentsMadeOffside++;
            if (isOnside && Distance(facts.targetHex ?? facts.actorHex, attacker.currentHex) <= (facts.actorToken?.pace ?? 0)) facts.reachableOnsideOpponents++;
        }
        var centreBacks = defenders.Where(t => t.jerseyNumber is 4 or 5).ToList();
        facts.defensiveLineLeavesFullback = centreBacks.Count == 2
            && after[1].jerseyNumber is 2 or 3
            && centreBacks.All(t => sign * At(t).x > sign * At(after[1]).x)
            && facts.actorToken?.jerseyNumber is 4 or 5
            && facts.targetHex != null && sign * facts.targetHex.x > sign * facts.actorHex.x;
        var outfield = defenders.Where(t => !t.isGoalKeeper).ToList();
        int Gap(bool moved)
        {
            var depths = outfield.Select(t => sign * (moved ? At(t) : t.currentHex).x).OrderBy(x => x).ToList();
            return depths.Count < 2 ? 0 : depths[1] - depths[0];
        }
        facts.defensiveLineCreatesGap = facts.actorHex != null && facts.targetHex != null
            && sign * facts.targetHex.x > sign * facts.actorHex.x
            && Gap(true) >= 3 && Gap(true) > Gap(false)
            && CounterExposure(snapshot, facts.actorTokenKey, facts.targetHex) > CounterExposure(snapshot, null, null) + .05f;
    }

    /// <summary>Allocate the available moves by the reduction of the whole unit's
    /// remaining scoring threats. Position/route forecasts are estimates, never
    /// an assertion that an offside position itself is an offence.</summary>
    public static RoomAIDefensivePhasePlan AssignDefense(RoomAIDecisionSnapshot snapshot,
        IReadOnlyDictionary<string, List<RoomHexCoordinates>> reachable,
        IReadOnlyDictionary<string, float> threatValues = null,
        RoomAIDefensivePhasePlan previousPlan = null)
    {
        RoomAIDefensivePhasePlan result = null;
        var work = AssignDefenseIncrementally(snapshot, reachable, threatValues, plan => result = plan);
        while (work.MoveNext()) { }
        return result;
    }

    /// <summary>Find an opponent who can actually reach a loose ball in one
    /// move on the current board. A missing or mismatched live board falls back
    /// to the geometric estimate used for synthetic snapshots.</summary>
    public static RoomAITokenSnapshot LikelyLooseBallCollector(RoomAIDecisionSnapshot snapshot)
    {
        if (snapshot?.ball == null || snapshot.tokens == null) return null;
        var candidates = snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff
            && t.teamSide != snapshot.expectedTeam && t.currentHex != null
            && Distance(t.currentHex, snapshot.ball) <= t.pace).ToList();
        var match = MatchManager.Instance;
        var grid = match?.hexGrid;
        var ball = match?.ball?.GetCurrentHex();
        var live = match?.playerTokenManager?.allTokens;
        if (grid == null || ball == null || live == null
            || Distance(RoomHexCoordinates.FromHex(ball), snapshot.ball) != 0)
            return candidates.OrderBy(t => Distance(t.currentHex, snapshot.ball) / (float)Math.Max(1, t.pace))
                .ThenByDescending(t => t.shooting + t.dribbling).FirstOrDefault();

        var byKey = live.Where(t => t != null && t.isPlaying && !t.isSentOff && t.GetCurrentHex() != null)
            .GroupBy(MatchManager.GetStableTokenKey).Where(g => !string.IsNullOrEmpty(g.Key))
            .ToDictionary(g => g.Key, g => g.First());
        // Never use another board's occupancy or legality for this snapshot.
        if (snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff && t.currentHex != null)
            .Any(t => !byKey.TryGetValue(t.tokenKey, out var token)
                || Distance(t.currentHex, RoomHexCoordinates.FromHex(token.GetCurrentHex())) != 0))
            return candidates.OrderBy(t => Distance(t.currentHex, snapshot.ball) / (float)Math.Max(1, t.pace))
                .ThenByDescending(t => t.shooting + t.dribbling).FirstOrDefault();

        var movement = match.movementPhaseManager;
        var header = movement?.headerManager;
        var reachable = new List<(RoomAITokenSnapshot token, int cost)>();
        foreach (var candidate in candidates)
        {
            if (!byKey.TryGetValue(candidate.tokenKey, out var token) || !match.CanTokenCollectHangingPass(token)) continue;
            var start = token.GetCurrentHex();
            if (start == ball) { reachable.Add((candidate, 0)); continue; }
            if (movement?.stunnedTokens.Contains(token) == true || movement?.stunnedforNext.Contains(token) == true
                || header?.attackerWillJump.Contains(token) == true || header?.defenderWillJump.Contains(token) == true) continue;
            if (!snapshot.f3OpponentCollectionPaths.TryGetValue(token, out var route))
                snapshot.f3OpponentCollectionPaths[token] = route = HexGridUtils.FindPath(start, ball, grid);
            if (route != null && route.Count > 0 && route.Count <= token.pace)
                reachable.Add((candidate, route.Count));
        }
        return reachable.OrderBy(p => p.cost / (float)Math.Max(1, p.token.pace))
            .ThenByDescending(p => p.token.shooting + p.token.dribbling).Select(p => p.token).FirstOrDefault();
    }

    public static System.Collections.IEnumerator AssignDefenseIncrementally(RoomAIDecisionSnapshot snapshot,
        IReadOnlyDictionary<string, List<RoomHexCoordinates>> reachable,
        IReadOnlyDictionary<string, float> threatValues, Action<RoomAIDefensivePhasePlan> completed)
    {
        if (snapshot?.ball == null || reachable == null || reachable.Count == 0)
        { completed(null); yield break; }
        var defenders = snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff && !t.isGoalKeeper && t.currentHex != null
            && t.teamSide == snapshot.expectedTeam).ToList();
        var threats = snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff && !t.isGoalKeeper && t.currentHex != null
            && t.teamSide != snapshot.expectedTeam).ToList();
        int goalSign = (snapshot.expectedTeam == "Home" ? snapshot.homeTeamDirection : snapshot.awayTeamDirection) == "LeftToRight" ? -1 : 1;
        var positions = defenders.ToDictionary(t => t.tokenKey, t => t.currentHex);
        var liveDefenders = MatchManager.Instance?.playerTokenManager?.allTokens?
            .Where(token => token != null && token.isPlaying && !token.isSentOff && token.GetCurrentHex() != null)
            .GroupBy(MatchManager.GetStableTokenKey).Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .ToDictionary(group => group.Key, group => group.First()) ?? new Dictionary<string, PlayerToken>();
        var plan = new RoomAIDefensivePhasePlan();
        var used = new HashSet<string>();
        bool loose = !snapshot.attackHasPossession;
        var likelyCollector = loose ? LikelyLooseBallCollector(snapshot) : null;
        float Threat(RoomAITokenSnapshot attacker)
        {
            float known = threatValues != null && threatValues.TryGetValue(attacker.tokenKey, out float value) ? value : 0f;
            int depth = goalSign * attacker.currentHex.x;
            float stepsToBox = Math.Max(0, 12 - depth);
            float phases = stepsToBox / Math.Max(1, attacker.pace);
            float shooting = attacker.shooting / 6f;
            float heading = attacker.heading / 6f;
            // A dangerous receiver within one movement matters even before
            // their current cell becomes a shooting cell. Deep defenders do not.
            float finish = (shooting * shooting * .65f + heading * heading * .18f) / (1f + phases * phases);
            float delivery = Distance(snapshot.ball, attacker.currentHex) <= 11 ? .75f : .30f;
            float distribution = 0f;
            if (Distance(snapshot.ball, attacker.currentHex) == 0 && attacker.highPass >= 5)
                distribution = threats.Where(t => t != attacker && Distance(attacker.currentHex, t.currentHex) <= 15)
                    .Select(t => t.heading / 6f * t.shooting / 6f
                        / (1f + Math.Max(0, 12 - goalSign * t.currentHex.x) / (float)Math.Max(1, t.pace)))
                    .DefaultIfEmpty(0f).Max() * HighPassManager.AccuracyProbabilityForDecisionPreview(attacker.highPass);
            return Math.Max(known, finish * delivery) + (Distance(snapshot.ball, attacker.currentHex) == 0 ? .12f : 0f)
                + distribution * .25f;
        }
        threats = threats.OrderByDescending(Threat).Take(6).ToList();
        var threatWeights = threats.ToDictionary(token => token.tokenKey, Threat);
        var escapeRoutes = new Dictionary<string, List<List<RoomHexCoordinates>>>();
        var escapeExposureCache = new Dictionary<(string defender, int x, int z, string threat, int route), float>();
        var grid = MatchManager.Instance?.hexGrid;
        var keeperOutlets = new List<RoomAITokenSnapshot>();
        var keeperRelays = new List<KeeperFtpRelay>();
        var match = MatchManager.Instance;
        bool matchingBoard = grid != null && match?.ball?.GetCurrentHex() != null
            && Distance(snapshot.ball, RoomHexCoordinates.FromHex(match.ball.GetCurrentHex())) == 0
            && liveDefenders.Count == snapshot.tokens.Count(t => t != null && t.isPlaying && !t.isSentOff && t.currentHex != null)
            && snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff && t.currentHex != null)
                .All(t => !string.IsNullOrWhiteSpace(t.tokenKey) && liveDefenders.TryGetValue(t.tokenKey, out var live)
                    && Distance(t.currentHex, RoomHexCoordinates.FromHex(live.GetCurrentHex())) == 0
                    && t.teamSide == (live.isHomeTeam ? "Home" : "Away")
                    && t.pace == live.pace && t.tackling == live.tackling && t.shooting == live.shooting
                    && t.dribbling == live.dribbling && t.heading == live.heading && t.highPass == live.highPass);
        if (loose && likelyCollector?.isGoalKeeper == true && matchingBoard)
        {
            var attackingPlayers = snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff
                && !t.isGoalKeeper && t.teamSide != snapshot.expectedTeam && t.currentHex != null).ToList();
            keeperOutlets = attackingPlayers.Where(t => Distance(snapshot.ball, t.currentHex) <= 11).ToList();
            var occupiedByAttackers = new HashSet<HexCell>(attackingPlayers.Select(t => grid.GetHexCellAt(
                new UnityEngine.Vector3Int(t.currentHex.x, 0, t.currentHex.z), false)).Where(h => h != null));
            occupiedByAttackers.Add(match.ball.GetCurrentHex()); // keeper's projected pickup
            foreach (var outlet in keeperOutlets)
            {
                var origin = grid.GetHexCellAt(new UnityEngine.Vector3Int(outlet.currentHex.x, 0, outlet.currentHex.z), false);
                foreach (var receiver in attackingPlayers.Where(t => t != outlet && IsDirectFtpRelayWithinRange(outlet.currentHex, t.currentHex)))
                {
                    var target = grid.GetHexCellAt(new UnityEngine.Vector3Int(receiver.currentHex.x, 0, receiver.currentHex.z), false);
                    if (origin == null || target == null) continue;
                    var path = GroundPassCommon.CalculateThickPath(grid, origin, target, match.ball.ballRadius);
                    // A goalkeeper-wall interaction requires its own preview.
                    // Do not claim a normal interception-only FTP in that area.
                    if (path.Count == 0 || path.Any(h => h != null && h.isInPenaltyBox == goalSign)) continue;
                    keeperRelays.Add(new KeeperFtpRelay { outlet = outlet, receiver = receiver, path = path,
                        interceptionHexes = path.Where(h => h != null && !occupiedByAttackers.Contains(h)).ToHashSet() });
                }
                yield return null;
            }
        }
        plan.includesStaticKeeperFtpRelays = keeperRelays.Count > 0;
        plan.staticKeeperFtpRelayPairs.AddRange(keeperRelays.Select(r => r.outlet.tokenKey + "->" + r.receiver.tokenKey));
        if (!loose && grid != null)
        {
            foreach (var threat in threats)
            {
                if (!liveDefenders.TryGetValue(threat.tokenKey, out var live) || live.GetCurrentHex() == null) continue;
                var start = live.GetCurrentHex();
                var destinations = HexGridUtils.GetReachableHexes(grid, start, Math.Max(0, live.pace)).reachableHexes
                    .Where(hex => hex != null && !hex.isOutOfBounds && hex.CanShootFrom && hex.ShootingPaths != null
                        && hex.ShootingPaths.Keys.Any(goal => goal != null && goal.isInGoal == goalSign))
                    .OrderByDescending(hex => goalSign * hex.coordinates.x).ThenBy(hex => Math.Abs(hex.coordinates.z))
                    .Take(6);
                var paths = new List<List<RoomHexCoordinates>>();
                foreach (var destination in destinations)
                {
                    var path = destination == start ? new List<HexCell>()
                        : HexGridUtils.FindPath(start, destination, grid, new HashSet<HexCell> { start });
                    if (path == null || path.Count == 0 || path.Count > live.pace) continue;
                    paths.Add(path.Select(RoomHexCoordinates.FromHex).ToList());
                }
                if (paths.Count > 0) escapeRoutes[threat.tokenKey] = paths;
            }
        }
        plan.includesBoundedReceiverShotRoutes = escapeRoutes.Count > 0;
        var relayExposureCache = new Dictionary<(string defender, int x, int z, int route), float>();
        var relayDefenders = snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff
            && t.teamSide == snapshot.expectedTeam && t.currentHex != null).ToList();
        float StaticFtpRelayThreat(RoomAITokenSnapshot outlet, bool includeDefendingMove = false)
        {
            if (keeperRelays.Count == 0 || grid == null) return 0f;
            RoomHexCoordinates At(RoomAITokenSnapshot player, RoomAITokenSnapshot moved = null, RoomHexCoordinates destination = null)
                => player == moved ? destination : positions.TryGetValue(player.tokenKey, out var projected)
                    ? projected : player.currentHex;
            int[] line = relayDefenders.Select(t => goalSign * At(t).x).OrderByDescending(x => x).ToArray();
            float best = 0f;
            for (int routeIndex = 0; routeIndex < keeperRelays.Count; routeIndex++)
            {
                var relay = keeperRelays[routeIndex];
                if (relay.outlet != outlet) continue;
                // Offside is assessed again at the FTP touch, not at the
                // keeper's exempt Quick Throw.
                if (line.Length >= 2 && goalSign * relay.receiver.currentHex.x
                    > Math.Max(0, Math.Max(goalSign * outlet.currentHex.x, line[1]))) continue;
                float ValueAfterReply(RoomAITokenSnapshot moved = null, RoomHexCoordinates destination = null)
                {
                    float flight = 1f;
                    foreach (var defender in relayDefenders)
                    {
                        var at = At(defender, moved, destination);
                        var key = (defender.tokenKey, at.x, at.z, routeIndex);
                        if (!relayExposureCache.TryGetValue(key, out float factor))
                        {
                            var hex = grid.GetHexCellAt(new UnityEngine.Vector3Int(at.x, 0, at.z), false);
                            factor = 1f;
                            if (hex != null && relay.path.Contains(hex)) factor = 0f;
                            else if (hex != null && relay.interceptionHexes.Any(h => HexGridUtils.GetHexStepDistance(hex, h) == 1)
                                && liveDefenders.TryGetValue(defender.tokenKey, out var live)
                                && match.movementPhaseManager?.stunnedTokens.Contains(live) != true
                                && match.movementPhaseManager?.stunnedforNext.Contains(live) != true)
                                factor = 1f - ExpectedStatsCalculator.CalculateRecoveryProbability(live);
                            relayExposureCache[key] = factor;
                        }
                        flight *= factor;
                        if (flight <= 0f) return 0f;
                    }
                    int depth = goalSign * relay.receiver.currentHex.x;
                    float phases = Math.Max(0, 12 - depth) / (float)Math.Max(1, relay.receiver.pace);
                    float cover = defenders.Select(d => Math.Min(1f, .2f
                        + Math.Max(0, Distance(At(d, moved, destination), relay.receiver.currentHex) - 1) / (float)Math.Max(1, d.pace) * .5f))
                        .DefaultIfEmpty(1f).Min();
                    return flight * relay.receiver.shooting / 6f / (1f + phases * phases) * cover;
                }
                float threat = ValueAfterReply();
                if (includeDefendingMove && threat > 0f)
                {
                    // The FTP target and offside assessment are fixed before
                    // this optional move. Compare declining with a small set
                    // of reachable moves that can affect this route or receiver.
                    var occupied = snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff && t.currentHex != null)
                        .Select(t => At(t)).Select(c => (c.x, c.z)).ToHashSet();
                    foreach (var defender in relayDefenders.Where(d => !d.isGoalKeeper
                        && liveDefenders.TryGetValue(d.tokenKey, out var live)
                        && match.movementPhaseManager?.stunnedTokens.Contains(live) != true
                        && match.movementPhaseManager?.stunnedforNext.Contains(live) != true)
                        .OrderBy(d => relay.path.Select(h => Distance(At(d), RoomHexCoordinates.FromHex(h)))
                            .Append(Distance(At(d), relay.receiver.currentHex)).Min()).Take(3))
                    {
                        var at = At(defender);
                        var start = grid.GetHexCellAt(new UnityEngine.Vector3Int(at.x, 0, at.z), false);
                        if (start == null) continue;
                        foreach (var hex in start.GetNeighbors(grid).Where(h => h != null && !h.isOutOfBounds && h.isInGoal == 0
                            && !occupied.Contains((h.coordinates.x, h.coordinates.z))))
                            threat = Math.Min(threat, ValueAfterReply(defender, RoomHexCoordinates.FromHex(hex)));
                    }
                }
                best = Math.Max(best, threat);
            }
            return best;
        }
        float TeamDanger()
        {
            if (loose)
            {
                float failure = 1f;
                foreach (var defender in defenders)
                {
                    int distance = Distance(positions[defender.tokenKey], snapshot.ball);
                    float recovery = distance == 0 ? 1f : distance == 1 ? Math.Max(1f / 6f, (defender.tackling - 3) / 6f) : 0f;
                    // A spent player's failed interception cannot stand in for
                    // another available player's recovery opportunity.
                    if (!reachable.ContainsKey(defender.tokenKey)) recovery *= .15f;
                    failure *= 1f - recovery;
                }
                float arrival = defenders.Select(t => (float)Math.Max(0, Distance(positions[t.tokenKey], snapshot.ball) - 1)
                    / Math.Max(1, t.pace)).DefaultIfEmpty(4f).Min();
                // A failed recovery is not the end of the attack. Estimate the
                // most immediate opponent collector and the cover around the
                // landing cell, so that denying their next run has value even
                // when no defender can improve the initial collection race.
                var collector = likelyCollector;
                float followup = 0f;
                if (collector != null)
                {
                    float cover = 1f;
                    foreach (var defender in defenders)
                    {
                        int distance = Distance(positions[defender.tokenKey], snapshot.ball);
                        float reach = Math.Max(0f, 1f - Math.Max(0, distance - 1) / (float)Math.Max(1, defender.pace));
                        float quality = .3f + Math.Min(6, defender.tackling) / 6f * .4f;
                        if (!reachable.ContainsKey(defender.tokenKey)) quality *= .5f;
                        cover *= 1f - reach * quality * .6f;
                    }
                    int depth = goalSign * snapshot.ball.x;
                    float phasesToShot = Math.Max(0, 12 - depth) / (float)Math.Max(1, collector.pace);
                    float finishing = collector.shooting / 6f / (1f + phasesToShot * phasesToShot);
                    followup = failure * cover * (.25f + finishing * .75f);
                    if (collector.isGoalKeeper)
                    {
                        // The bounded relay includes FTP range, next-touch
                        // offside and one defending free move, but not the
                        // attacking free move, F3, GK-wall or the later carry.
                        var outlets = keeperOutlets.Count > 0 ? keeperOutlets
                            : threats.Where(t => Distance(snapshot.ball, t.currentHex) <= 11).ToList();
                        float relay = outlets
                            .Select(t =>
                            {
                                float throwSurvival = 1f;
                                foreach (var defender in defenders.Where(d => Distance(positions[d.tokenKey], t.currentHex) == 1))
                                {
                                    if (!liveDefenders.TryGetValue(defender.tokenKey, out var token)) continue;
                                    var movement = MatchManager.Instance?.movementPhaseManager;
                                    if (movement?.stunnedTokens.Contains(token) == true
                                        || movement?.stunnedforNext.Contains(token) == true) continue;
                                    throwSurvival *= 1f - ExpectedStatsCalculator.CalculateRecoveryProbability(token);
                                }
                                return Math.Max(Threat(t), StaticFtpRelayThreat(t, true) * .6f) * throwSurvival;
                            }).DefaultIfEmpty(0f).Max();
                        followup += failure * relay;
                    }
                }
                return failure * .8f + arrival * .2f + followup;
            }
            float sum = 0f, worst = 0f;
            int deepest = int.MinValue, secondDeepest = int.MinValue;
            foreach (var token in snapshot.tokens.Where(t => t.isPlaying && t.teamSide == snapshot.expectedTeam && t.currentHex != null))
            {
                int column = goalSign * (positions.TryGetValue(token.tokenKey, out var at) ? at.x : token.currentHex.x);
                if (column > deepest) { secondDeepest = deepest; deepest = column; }
                else if (column > secondDeepest) secondDeepest = column;
            }
            foreach (var threat in threats)
            {
                float control = 1f;
                foreach (var defender in defenders)
                {
                    var at = positions[defender.tokenKey];
                    int distance = Distance(at, threat.currentHex);
                    int wrongSide = Math.Max(0, goalSign * (threat.currentHex.x - at.x));
                    float recovery = Math.Max(0, distance - 1) / (float)Math.Max(1, defender.pace);
                    float escape = wrongSide * .14f + Math.Max(0, threat.pace - defender.pace) * wrongSide * .06f;
                    bool aerialReceiver = threat.heading >= 5 && threat.dribbling <= 2;
                    float quality = aerialReceiver ? defender.heading / 6f : defender.tackling / 6f;
                    control = Math.Min(control, Math.Min(1f, .18f + recovery * .50f + escape
                        - quality * (aerialReceiver ? .16f : .08f)));
                }
                float passSurvival = 1f;
                int length = Distance(snapshot.ball, threat.currentHex);
                bool offsideAtCurrentPass = length > 0 && secondDeepest != int.MinValue
                    && goalSign * threat.currentHex.x > Math.Max(0, Math.Max(goalSign * snapshot.ball.x, secondDeepest));
                // A pass now cannot legally reach this receiver. They can still
                // reposition, or the carrier can dribble: retain that future risk.
                if (offsideAtCurrentPass) passSurvival *= .5f;
                if (length > 0)
                    foreach (var defender in defenders)
                    {
                        var at = positions[defender.tokenKey];
                        // Price the failed-interception branch too: this only
                        // reduces delivery probability, never substitutes for cover.
                        int detour = Distance(snapshot.ball, at) + Distance(at, threat.currentHex) - length;
                        if (detour <= 1 && Distance(at, snapshot.ball) > 0)
                            passSurvival *= 1f - Math.Max(0f, Math.Min(.67f, (defender.tackling - 1) / 6f)) * .65f;
                    }
                float escapeThreat = 0f;
                if (escapeRoutes.TryGetValue(threat.tokenKey, out var paths))
                {
                    for (int routeIndex = 0; routeIndex < paths.Count; routeIndex++)
                    {
                        var path = paths[routeIndex];
                        float survival = 1f;
                        foreach (var defender in defenders)
                        {
                            var at = positions[defender.tokenKey];
                            var cacheKey = (defender.tokenKey, at.x, at.z, threat.tokenKey, routeIndex);
                            if (!escapeExposureCache.TryGetValue(cacheKey, out float factor))
                            {
                                factor = 1f;
                                if (path.Any(hex => Distance(at, hex) == 0)) factor = 0f;
                                else if (path.Any(hex => Distance(at, hex) == 1)
                                    && liveDefenders.TryGetValue(defender.tokenKey, out var token))
                                {
                                    var movement = MatchManager.Instance?.movementPhaseManager;
                                    if (movement?.stunnedTokens.Contains(token) != true
                                        && movement?.stunnedforNext.Contains(token) != true)
                                        factor = ExpectedStatsCalculator.CalculateMovementInterceptionExpectation(token).continuationProbability;
                                }
                                escapeExposureCache[cacheKey] = factor;
                            }
                            survival *= factor;
                            if (survival <= 0f) break;
                        }
                        escapeThreat = Math.Max(escapeThreat, survival * threat.shooting / 6f);
                    }
                }
                float deliveryAccess = length <= 11 ? 1f : .3f;
                float danger = (threatWeights[threat.tokenKey] * control + escapeThreat * .4f * deliveryAccess) * passSurvival;
                worst = Math.Max(worst, danger); sum += danger;
            }
            // Prevent abandoning central cover while an isolated CB holds the
            // line. Raising the unit is useful only if its resulting threats fall.
            var backs = defenders.Where(t => t.jerseyNumber is 4 or 5).ToList();
            int lineGap = backs.Count == 2 ? Math.Abs(positions[backs[0].tokenKey].x - positions[backs[1].tokenKey].x) : 0;
            // A widening gap gets progressively more expensive. A real recovery
            // emergency can still justify it through the resulting threat drop.
            float cohesion = lineGap * lineGap * .012f;
            return worst + sum * .35f + cohesion;
        }
        foreach (var outlet in keeperOutlets)
        {
            plan.initialStaticKeeperFtpThreatByOutlet[outlet.tokenKey] = StaticFtpRelayThreat(outlet);
            plan.initialKeeperFtpThreatAfterDefendingMoveByOutlet[outlet.tokenKey] = StaticFtpRelayThreat(outlet, true);
        }
        float baseline = TeamDanger();
        plan.initialThreatEstimate = baseline;
        int budget = Math.Min(snapshot.defensiveMovesRemaining, reachable.Count);
        for (int move = 0; move < budget; move++)
        {
            RoomAITokenSnapshot bestActor = null;
            RoomHexCoordinates bestHex = null;
            float bestGain = .0001f;
            float bestActualGain = 0f;
            RoomAITokenSnapshot recoveryActor = null;
            RoomHexCoordinates recoveryHex = null;
            float recoveryGain = 0f;
            var center = new RoomHexCoordinates { x = goalSign * 18, z = 0 };
            foreach (var defender in defenders.Where(t => reachable.ContainsKey(t.tokenKey) && !used.Contains(t.tokenKey)))
            {
                var origin = positions[defender.tokenKey];
                foreach (var hex in reachable[defender.tokenKey])
                {
                    if (hex == null || Distance(origin, hex) == 0
                        || positions.Any(pair => pair.Key != defender.tokenKey && Distance(pair.Value, hex) == 0)
                        || snapshot.tokens.Any(t => t != null && t.isPlaying && !t.isSentOff && t.currentHex != null
                            && !positions.ContainsKey(t.tokenKey) && Distance(t.currentHex, hex) == 0)) continue;
                    positions[defender.tokenKey] = hex;
                    float actualGain = baseline - TeamDanger();
                    float gain = actualGain;
                    // Prefer a safe-side tackle arrival when both close the ball.
                    var carrier = threats.FirstOrDefault(t => Distance(t.currentHex, snapshot.ball) == 0);
                    if (gain > .0001f && carrier != null && Distance(hex, snapshot.ball) == 1 && goalSign * (hex.x - snapshot.ball.x) >= 0)
                        gain += .015f;
                    positions[defender.tokenKey] = origin;
                    if (actualGain > .0001f && gain > bestGain)
                    { bestGain = gain; bestActualGain = actualGain; bestActor = defender; bestHex = hex; }
                    // A saturated threat estimate must not freeze a stranded
                    // defense. Recover toward goal without worsening the modeled
                    // threat, surrendering goal-side depth, or chasing forward.
                    int homeGain = Distance(origin, center) - Distance(hex, center);
                    if (!loose && homeGain > 0 && goalSign * hex.x >= goalSign * origin.x
                        && actualGain >= -.0001f && homeGain > recoveryGain)
                    { recoveryGain = homeGain; recoveryActor = defender; recoveryHex = hex; }
                }
                yield return null;
            }
            bool fallbackRecovery = bestActor == null && recoveryActor != null;
            if (fallbackRecovery) { bestActor = recoveryActor; bestHex = recoveryHex; }
            if (bestActor == null) break;
            var old = positions[bestActor.tokenKey];
            positions[bestActor.tokenKey] = bestHex;
            baseline = TeamDanger(); used.Add(bestActor.tokenKey);
            bool pressure = Distance(bestHex, snapshot.ball) <= 1;
            var marked = threats.OrderByDescending(t => Threat(t)
                * Math.Max(0, Distance(old, t.currentHex) - Distance(bestHex, t.currentHex)))
                .ThenBy(t => Distance(bestHex, t.currentHex)).FirstOrDefault();
            string role = fallbackRecovery ? "EmergencyRecovery" : pressure ? "PrimaryPressure" : "RecoveryCover";
            plan.assignments.Add(new RoomAIDefensiveAssignment { tokenKey = bestActor.tokenKey,
                destination = bestHex, role = role, targetTokenKey = marked?.tokenKey,
                threatReduction = bestActualGain, movePriority = move });
            if (pressure && plan.primaryPressureTokenKey == null)
            {
                plan.primaryPressureTokenKey = bestActor.tokenKey; plan.primaryPressureDestination = bestHex;
                plan.primaryPressureIntent = loose ? "RecoverLooseBall" : "ContainCarrier";
            }
        }
        foreach (var defender in defenders.Where(t => !used.Contains(t.tokenKey)))
            plan.assignments.Add(new RoomAIDefensiveAssignment { tokenKey = defender.tokenKey,
                destination = defender.currentHex, role = "HoldShape", movePriority = int.MaxValue });
        plan.legalAdjacencyAvailable = reachable.Values.Any(hexes => hexes.Any(hex => Distance(hex, snapshot.ball) == 1));
        plan.remainingThreatEstimate = baseline;
        completed(plan);
    }

    public static float CounterExposure(RoomAIDecisionSnapshot snapshot, string movedKey, RoomHexCoordinates destination)
        => CounterExposure(snapshot, movedKey, destination, null, null);

    public static float CounterExposure(RoomAIDecisionSnapshot snapshot, string movedKey, RoomHexCoordinates destination,
        string secondMovedKey, RoomHexCoordinates secondDestination)
    {
        if (snapshot?.ball == null) return 0f;
        int sign = (snapshot.expectedTeam == "Home" ? snapshot.homeTeamDirection : snapshot.awayTeamDirection) == "LeftToRight" ? -1 : 1;
        var own = snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff && !t.isGoalKeeper && t.currentHex != null && t.teamSide == snapshot.expectedTeam).ToList();
        RoomHexCoordinates At(RoomAITokenSnapshot token) => token.tokenKey == movedKey && destination != null ? destination
            : token.tokenKey == secondMovedKey && secondDestination != null ? secondDestination : token.currentHex;
        float result = 0f;
        foreach (var foe in snapshot.tokens.Where(t => t != null && t.isPlaying && !t.isSentOff && !t.isGoalKeeper && t.currentHex != null && t.teamSide != snapshot.expectedTeam))
        {
            float recover = own.Select(t => Math.Max(0, Distance(At(t), foe.currentHex) - 1) / (float)Math.Max(1, t.pace)
                + Math.Max(0, sign * (foe.currentHex.x - At(t).x)) * .12f).DefaultIfEmpty(2f).Min();
            float horizon = Math.Max(0, 12 - sign * foe.currentHex.x) / (float)Math.Max(1, foe.pace);
            result += (foe.shooting * foe.shooting + foe.heading * 2) / 48f / (1f + horizon * horizon) * recover;
        }
        var line = own.Select(t => sign * At(t).x).OrderByDescending(x => x).ToList();
        if (line.Count >= 2) result += (line[0] - line[1]) * .035f;
        var centreBacks = own.Where(t => t.jerseyNumber is 4 or 5).ToList();
        if (centreBacks.Count == 2)
        {
            int gap = Math.Abs(At(centreBacks[0]).x - At(centreBacks[1]).x);
            result += gap * gap * .012f;
        }
        return result;
    }

    public static int CompareGoalkeeperSafety(RoomAITacticalEvaluation a, RoomAITacticalEvaluation b)
    {
        return (a?.safetyPriority ?? 0).CompareTo(b?.safetyPriority ?? 0);
    }

    public static float RecoverySupportGain(RoomAIDecisionSnapshot snapshot, RoomAITokenSnapshot actor, RoomHexCoordinates target)
    {
        if (snapshot?.ball == null || actor?.currentHex == null || target == null || actor.isGoalKeeper) return 0f;
        // A concrete continuation: support a carrier/collector against a reachable
        // tackle, then recover the ball. Do not value unopposed or duplicate support.
        bool contested = snapshot.tokens.Any(token => token.isPlaying && token.currentHex != null
            && token.teamSide != actor.teamSide && Distance(token.currentHex, snapshot.ball) <= token.pace * 2 + 1);
        int pace = Math.Max(1, actor.pace);
        int before = (Math.Max(0, Distance(actor.currentHex, snapshot.ball) - 2) + pace - 1) / pace;
        int after = (Math.Max(0, Distance(target, snapshot.ball) - 2) + pace - 1) / pace;
        if (!contested || after >= before || after > 1) return 0f;
        int existingSupport = snapshot.tokens.Count(token => token.isPlaying && !token.isGoalKeeper && token.currentHex != null
            && token.teamSide == actor.teamSide && token.tokenKey != actor.tokenKey
            && Distance(token.currentHex, snapshot.ball) is 1 or 2);
        if (existingSupport >= 2 || actor.tackling <= 0) return 0f;
        return (float)Math.Min(1f, actor.tackling / 6f) / (1 + existingSupport + after);
    }

    public static float RecoverySupportNetGain(RoomAIDecisionSnapshot snapshot, RoomAITokenSnapshot actor,
        RoomHexCoordinates target, float recoveryGain, float baselineExposure)
    {
        if (snapshot == null || actor == null || target == null || recoveryGain <= 0f) return 0f;
        // Do not credit improved cover a second time; this is the recovery
        // branch, and only its additional counterattack exposure is a cost.
        float lostCover = Math.Max(0f, CounterExposure(snapshot, actor.tokenKey, target) - baselineExposure);
        return Math.Max(0f, recoveryGain - lostCover);
    }

    public static void AssignSafetyPriorities(IReadOnlyList<RoomAITacticalEvaluation> evaluations)
    {
        var known = evaluations.Where(e => e.explanation?.isGoalkeeperPositioning == true
            && e.explanation.goalkeeperPositioningMaximumExpectedGoals >= 0f).ToList();
        bool goalkeeperChoice = known.Count == evaluations.Count && known.Count > 0;
        var threats = known.Select(e => e.explanation.goalkeeperPositioningMaximumExpectedGoals).Distinct().OrderBy(x => x).ToList();
        foreach (var evaluation in known)
        {
            var facts = evaluation.explanation;
            bool worsens = facts.goalkeeperPositioningCurrentMaximumExpectedGoals >= 0f
                && facts.goalkeeperPositioningMaximumExpectedGoals > facts.goalkeeperPositioningCurrentMaximumExpectedGoals;
            evaluation.safetyPriority = goalkeeperChoice ? threats.IndexOf(facts.goalkeeperPositioningMaximumExpectedGoals) : worsens ? 1 : 0;
            if (evaluation.safetyPriority > 0)
                evaluation.reasons.Insert(0, $"GK safety priority {evaluation.safetyPriority}: worst threat xG={facts.goalkeeperPositioningMaximumExpectedGoals:0.000}; shooter={facts.goalkeeperPositioningWorstThreatTokenKey}, action={facts.goalkeeperPositioningThreatAction}, wall={facts.goalkeeperPositioningWallApplies}, save modifier={facts.goalkeeperPositioningGoalkeeperPenalty}");
        }
    }
}
