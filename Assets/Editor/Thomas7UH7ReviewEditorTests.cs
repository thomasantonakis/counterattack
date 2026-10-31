using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>28 October live review. Recorded positions are fixtures, not tactical
/// targets. These tests do not enter Play Mode or dispatch gameplay inputs.</summary>
public sealed class Thomas7UH7ReviewEditorTests
{
    [Test]
    public void RecordedMarellToGoaleyRelayUsesTheFtpRange()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_QuickThrowSequence", 218, false);
        var marell = RoomHexCoordinates.FromHex(replay.tokens["Home:4"].GetCurrentHex());
        var goaley = RoomHexCoordinates.FromHex(replay.tokens["Home:6"].GetCurrentHex());
        Assert.That(RoomAITacticalSafety.IsDirectFtpRelayWithinRange(marell, goaley), Is.True);
        Assert.That(RoomAITacticalSafety.Distance(marell, goaley), Is.EqualTo(FirstTimePassManager.MaxPassDistanceForDecisionPreview));
        Assert.That(RoomAITacticalSafety.IsDirectFtpRelayWithinRange(Hex(0, 0), Hex(7, 0)), Is.False);
        Assert.That(RoomAITacticalSafety.IsDirectFtpRelayWithinRange(marell, marell), Is.False);
    }

    [Test]
    public void RecordedQuickThrowUsesTargetOnlyInterceptionsRatherThanBlockedGroundFlight()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_QuickThrowSequence", 218, false);
        var match = replay.match;
        match.currentState = MatchManager.GameState.QuickThrow;
        match.groundBallManager.isQuickThrow = true;
        var target = replay.tokens["Home:4"].GetCurrentHex();
        var path = GroundPassCommon.CalculateThickPath(match.hexGrid, match.ball.GetCurrentHex(), target, match.ball.ballRadius);
        var blocker = path.First(h => h != null && h.GetOccupyingToken() == null
            && HexGridUtils.GetHexStepDistance(h, target) > 1);
        replay.Move("Away:3", RoomHexCoordinates.FromHex(blocker));
        Assert.That(GroundPassCommon.ValidateStandardPassPath(match.hexGrid, match.ball, target, 11,
            isQuickThrow: true, logFailures: false).IsValid, Is.True);
        Assert.That(GroundPassCommon.ValidateStandardPassPath(match.hexGrid, match.ball, target, 11,
            logFailures: false).IsValid, Is.False);
        var snapshot = replay.BoardSnapshot();
        var action = new RoomActionCandidate { manager = nameof(GroundBallManager), actionType = RoomActionType.GroundPass,
            step = RoomDecisionStep.ChooseTarget, targetHex = target, targetToken = replay.tokens["Home:4"] };
        var facts = new RoomAICandidateExplanation { candidate = action };
        typeof(RoomAIDecisionSnapshotBuilder).GetMethod("PopulateGroundPassContinuationFacts", StaticPrivate)
            .Invoke(null, new object[] { snapshot, action, facts, target });
        Assert.That(facts.isGroundPassContinuation, Is.True);
        Assert.That(facts.passRace, Is.Not.Null);
        var sequence = RoomAIActionSequence.Evaluate(snapshot, facts);
        Assert.That(sequence, Is.Not.Null);
        float expectedFlight = 1f - ExpectedStatsCalculator.CalculateAtLeastOneRecoveryProbability(
            GroundPassCommon.BuildOrderedInterceptionCandidates(match.hexGrid, match.ball, target, isQuickThrow: true)
                .Where(c => c.DefenderToken != null).Select(c => c.DefenderToken).Distinct().ToArray());
        Assert.That(sequence.steps[0].conditionalProbability, Is.EqualTo(expectedFlight).Within(.00001f));
        Assert.That(expectedFlight, Is.GreaterThan(0f));
        var adjacent = target.GetNeighbors(match.hexGrid).First(h => h != null && !h.isOutOfBounds
            && h.GetOccupyingToken() == null);
        replay.Move("Away:4", RoomHexCoordinates.FromHex(adjacent));
        var pressured = RoomAIActionSequence.Evaluate(replay.BoardSnapshot(),
            new RoomAICandidateExplanation { candidate = action });
        float pressuredFlight = 1f - ExpectedStatsCalculator.CalculateAtLeastOneRecoveryProbability(
            GroundPassCommon.BuildOrderedInterceptionCandidates(match.hexGrid, match.ball, target, isQuickThrow: true)
                .Where(c => c.DefenderToken != null).Select(c => c.DefenderToken).Distinct().ToArray());
        Assert.That(pressuredFlight, Is.LessThan(expectedFlight));
        Assert.That(pressured.steps[0].conditionalProbability, Is.EqualTo(pressuredFlight).Within(.00001f));
    }

    [Test]
    public void ExistingOutletCrowdingIgnoresABlockedPassLane()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_Support", 296, false);
        var match = replay.match;
        var snapshot = replay.BoardSnapshot();
        var usable = typeof(RoomAITacticalSafety).GetMethod("HasUsableExistingGroundOutlet", StaticPrivate);
        var receiver = snapshot.tokens.Where(t => t.teamSide == snapshot.expectedTeam && !t.isGoalKeeper
                && t.currentHex != null && RoomAITacticalSafety.Distance(t.currentHex, snapshot.ball) is > 0 and <= 11)
            .First(t => (bool)usable.Invoke(null, new object[] { snapshot, t })
                && GroundPassCommon.CalculateThickPath(match.hexGrid, match.ball.GetCurrentHex(),
                    replay.tokens[t.tokenKey].GetCurrentHex(), match.ball.ballRadius)
                    .Any(h => h != null && h != match.ball.GetCurrentHex() && h.GetOccupyingToken() == null));
        var landing = replay.tokens[receiver.tokenKey].GetCurrentHex();
        var block = GroundPassCommon.CalculateThickPath(match.hexGrid, match.ball.GetCurrentHex(), landing, match.ball.ballRadius)
            .First(h => h != null && h != match.ball.GetCurrentHex() && h.GetOccupyingToken() == null);
        replay.Move("Home:9", RoomHexCoordinates.FromHex(block));
        var blocked = replay.BoardSnapshot();
        var blockedReceiver = blocked.tokens.Single(t => t.tokenKey == receiver.tokenKey);
        Assert.That((bool)usable.Invoke(null, new object[] { blocked, blockedReceiver }), Is.False,
            "An alternate behind a defender blocking its pass path is not an immediately usable outlet.");
    }

    [Test]
    public void KeeperCollectionForecastRequiresAnEligibleReachableCollector()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_KeeperCollection", 201, false);
        var snapshot = replay.BoardSnapshot();
        snapshot.expectedTeam = "Away";
        var movement = replay.match.movementPhaseManager;
        var keeper = replay.tokens["Home:1"];
        foreach (var token in replay.tokens.Values.Where(t => t.isPlaying && t.isHomeTeam && t != keeper))
            movement.stunnedTokens.Add(token);
        Assert.That(RoomAITacticalSafety.LikelyLooseBallCollector(snapshot)?.tokenKey, Is.EqualTo("Home:1"));
        movement.stunnedTokens.Add(keeper);
        Assert.That(RoomAITacticalSafety.LikelyLooseBallCollector(snapshot), Is.Null,
            "A nearby player who cannot move cannot be the expected collector.");
    }

    [Test]
    public void RecordedDelpechEscapeGetsAReachableCoverAssignment()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_DelpechEscape", 67);
        var decision = replay.Evaluate();
        Assert.That(decision.selectedCandidate.actor, Is.SameAs(replay.tokens["Away:5"]));
        Assert.That(replay.lastSnapshot.defensivePhasePlan.includesBoundedReceiverShotRoutes, Is.True);
        var cover = replay.lastSnapshot.defensivePhasePlan.assignments.Single(item => item.tokenKey == "Away:5");
        Assert.That(cover.targetTokenKey, Is.EqualTo("Home:11"));
        Assert.That(cover.threatReduction, Is.GreaterThan(0));
        Assert.That(RoomAITacticalSafety.Distance(cover.destination, Hex(8, 7)), Is.LessThanOrEqualTo(2));
    }
    [Test]
    public void ConfirmationReopensTargetsOnlyAfterRelevantBoardEvidenceChanges()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_UnsafeSpacePass", 187, false);
        var pass = replay.match.groundBallManager;
        pass.isActivated = true; pass.isAwaitingTargetSelection = true;
        pass.currentTargetHex = replay.Hex(Hex(-15, -6));
        var instruction = new GameplayInstructionSnapshot { manager = nameof(GroundBallManager) };
        var context = new RoomDecisionContext { manager = nameof(GroundBallManager), expectedTeam = "Away" };
        pass.PopulateRoomDecisionContext(context);
        var snapshot = RoomAIDecisionSnapshotBuilder.Build(instruction, context, null, false);
        var selected = new RoomActionCandidate { manager = nameof(GroundBallManager), actionType = RoomActionType.GroundPass,
            step = RoomDecisionStep.ChooseTarget, targetHex = pass.currentTargetHex };
        typeof(RoomDecisionManager).GetMethod("RememberGroundTargetSelection", Private)
            .Invoke(replay.decisions, new object[] { selected, snapshot });
        var build = typeof(RoomDecisionManager).GetMethod("BuildTacticalExecutableCandidates", Private);
        var stable = (List<RoomActionCandidate>)build.Invoke(replay.decisions, new object[] { instruction, context, true });
        Assert.That(stable.Any(candidate => candidate.step == RoomDecisionStep.ChooseTarget), Is.False);
        typeof(RoomDecisionManager).GetField("selectedGroundTargetBoardKey", Private).SetValue(replay.decisions, "changed");
        var changed = (List<RoomActionCandidate>)build.Invoke(replay.decisions, new object[] { instruction, context, true });
        Assert.That(changed.Any(candidate => candidate.step == RoomDecisionStep.Confirm), Is.True);
        Assert.That(changed.Any(candidate => candidate.step == RoomDecisionStep.ChooseTarget), Is.True);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void BlockedSelectedPassCannotBeConfirmed(bool hasSelectionEvidence)
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_UnsafeSpacePass", 187, false);
        var match = replay.match;
        var pass = match.groundBallManager;
        pass.isActivated = true;
        pass.isAwaitingTargetSelection = true;
        var target = pass.GetLegalGroundPassTargetsForDecisionPreview().First(hex =>
            GroundPassCommon.CalculateThickPath(match.hexGrid, match.ball.GetCurrentHex(), hex, match.ball.ballRadius)
                .Any(h => h != null && h != match.ball.GetCurrentHex() && h.GetOccupyingToken() == null && h != hex));
        var path = GroundPassCommon.CalculateThickPath(match.hexGrid, match.ball.GetCurrentHex(), target, match.ball.ballRadius);
        var block = path.First(h => h != null && h != match.ball.GetCurrentHex() && h.GetOccupyingToken() == null && h != target);
        var instruction = new GameplayInstructionSnapshot { manager = nameof(GroundBallManager), expectedTeam = "Away" };
        RoomDecisionContext Context()
        {
            var context = new RoomDecisionContext { manager = nameof(GroundBallManager), expectedTeam = "Away" };
            pass.PopulateRoomDecisionContext(context);
            return context;
        }
        var beforeContext = Context();
        var before = RoomAIDecisionSnapshotBuilder.Build(instruction, beforeContext, null, false);
        if (hasSelectionEvidence)
        {
            var chosen = new RoomActionCandidate { manager = nameof(GroundBallManager), actionType = RoomActionType.GroundPass,
                step = RoomDecisionStep.ChooseTarget, targetHex = target };
            typeof(RoomDecisionManager).GetMethod("RememberGroundTargetSelection", Private)
                .Invoke(replay.decisions, new object[] { chosen, before });
        }
        pass.currentTargetHex = target;
        replay.Move("Home:9", RoomHexCoordinates.FromHex(block));
        Assert.That(pass.IsGroundPassTargetLegalForDecision(target), Is.False);
        var afterContext = Context();
        var actions = (System.Collections.Generic.List<RoomActionCandidate>)typeof(RoomDecisionManager)
            .GetMethod("BuildTacticalExecutableCandidates", Private)
            .Invoke(replay.decisions, new object[] { instruction, afterContext, true });
        Assert.That(actions.Any(a => a.step == RoomDecisionStep.Confirm), Is.False);
        Assert.That(actions.Any(a => a.step == RoomDecisionStep.ChooseTarget), Is.True);
        Assert.That((string)typeof(RoomDecisionManager).GetField("groundConfirmationReplanReason", Private)
            .GetValue(replay.decisions), Does.Contain("no longer legal"));
    }

    [Test]
    public void ChangedPassRangeInvalidatesConfirmationEvenWhenTheBoardIsUnchanged()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_UnsafeSpacePass", 187, false);
        var pass = replay.match.groundBallManager;
        pass.isActivated = true; pass.isAwaitingTargetSelection = true;
        var target = pass.GetLegalGroundPassTargetsForDecisionPreview().First(hex =>
            HexGridUtils.GetHexStepDistance(replay.match.ball.GetCurrentHex(), hex) > 1);
        var instruction = new GameplayInstructionSnapshot { manager = nameof(GroundBallManager), expectedTeam = "Away" };
        var context = new RoomDecisionContext { manager = nameof(GroundBallManager), expectedTeam = "Away" };
        pass.PopulateRoomDecisionContext(context);
        var before = RoomAIDecisionSnapshotBuilder.Build(instruction, context, null, false);
        var selected = new RoomActionCandidate { manager = nameof(GroundBallManager), actionType = RoomActionType.GroundPass,
            step = RoomDecisionStep.ChooseTarget, targetHex = target };
        typeof(RoomDecisionManager).GetMethod("RememberGroundTargetSelection", Private)
            .Invoke(replay.decisions, new object[] { selected, before });
        pass.currentTargetHex = target;
        pass.imposedDistance = 1;
        var after = new RoomDecisionContext { manager = nameof(GroundBallManager), expectedTeam = "Away" };
        pass.PopulateRoomDecisionContext(after);
        var actions = (System.Collections.Generic.List<RoomActionCandidate>)typeof(RoomDecisionManager)
            .GetMethod("BuildTacticalExecutableCandidates", Private)
            .Invoke(replay.decisions, new object[] { instruction, after, true });
        Assert.That(actions.Any(a => a.step == RoomDecisionStep.Confirm), Is.False);
        Assert.That((string)typeof(RoomDecisionManager).GetField("groundConfirmationReplanReason", Private)
            .GetValue(replay.decisions), Does.Contain("no longer legal"));
    }
    [Test]
    public void RecordedHeaderSpaceDoesNotClaimRetentionBeforeDefenseResponds()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_HeadedSpace", 117, false);
        var header = replay.match.gameObject.AddComponent<HeaderManager>();
        header.hexGrid = replay.match.hexGrid; header.ball = replay.match.ball;
        header.movementPhaseManager = replay.match.movementPhaseManager; header.shotManager = replay.match.shotManager;
        replay.match.movementPhaseManager.headerManager = header;
        header.attackerWillJump.Add(replay.tokens["Away:11"]);
        header.defenderWillJump.Add(replay.tokens["Home:2"]);
        var target = replay.Hex(Hex(1, -13));
        var action = new RoomActionCandidate { manager = nameof(HeaderManager), actionType = RoomActionType.Header,
            step = RoomDecisionStep.ChooseTarget, targetHex = target };
        var facts = new RoomAICandidateExplanation { candidate = action };
        typeof(RoomAIDecisionSnapshotBuilder).GetMethod("PopulateHeaderPossessionChainFacts", StaticPrivate)
            .Invoke(null, new object[] { replay.BoardSnapshot(), action, facts, target, header, new List<HexCell>() });
        Assert.That(facts.headedPassSpaceCollectorTokenKey, Is.Not.EqualTo("Away:11"),
            "Soares participated in the header and cannot move to the distant landing.");
        Assert.That(facts.possessionChain.attackRetainsPossessionProbability, Is.Zero);
        Assert.That(facts.possessionChain.unresolvedProbability, Is.GreaterThan(0));
    }
    [Test]
    public void RecordedHeaderCannotSendItsJumperToCollectTheDistantLanding()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_HeadedSpace", 117, false);
        var header = replay.match.gameObject.AddComponent<HeaderManager>();
        header.hexGrid = replay.match.hexGrid;
        header.ball = replay.match.ball;
        header.movementPhaseManager = replay.match.movementPhaseManager;
        replay.match.movementPhaseManager.headerManager = header;
        header.attackerWillJump.Add(replay.tokens["Away:11"]);
        header.defenderWillJump.Add(replay.tokens["Home:2"]);
        var snapshot = replay.BoardSnapshot();
        snapshot.expectedTeam = snapshot.teamInAttack;
        var action = new RoomActionCandidate { manager = nameof(HeaderManager), actionType = RoomActionType.Header,
            step = RoomDecisionStep.ChooseTarget, targetHex = replay.Hex(Hex(1, -13)) };
        var facts = new RoomAICandidateExplanation { candidate = action, isHeadedPassTarget = true,
            headedPassInterceptionProbability = 0f };
        var sequence = RoomAIActionSequence.Evaluate(snapshot, facts);
        Assert.That(sequence, Is.Not.Null);
        Assert.That(sequence.steps.Any(step => step.action == "Collect" && step.actorTokenKey == "Away:11"), Is.False,
            "The recorded header participant cannot move five hexes to collect the headed pass.");
    }
    [Test]
    public void EasierDifficultyReplansOncePerActorWithoutReselectionLoops()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_ActorSelections", 144);
        replay.match.difficulty_level = 2;
        var first = replay.Evaluate();
        var manager = typeof(RoomDecisionManager);
        manager.GetMethod("RememberMovementActorAlternatives", Private).Invoke(replay.decisions,
            new object[] { first, replay.lastSnapshot });
        replay.SelectActor(first.selectedCandidate.actor);
        RoomAIProviderDecision Forfeit()
        {
            var candidate = new RoomActionCandidate { manager = nameof(MovementPhaseManager), actionType = RoomActionType.Decline,
                step = RoomDecisionStep.InterruptionChoice, key = "X", isForfeit = true };
            var evaluation = new RoomAITacticalEvaluation { candidate = candidate };
            return new RoomAIProviderDecision { rankedCandidates = new() { evaluation },
                selectedCandidate = candidate, selectedEvaluation = evaluation };
        }
        var second = Forfeit();
        manager.GetMethod("ApplyMovementActorReplan", Private).Invoke(replay.decisions, new object[] { second, replay.lastSnapshot });
        Assert.That(second.selectedCandidate.actor, Is.Not.Null.And.Not.SameAs(first.selectedCandidate.actor));
        manager.GetMethod("RememberMovementActorAlternatives", Private).Invoke(replay.decisions,
            new object[] { second, replay.lastSnapshot });
        replay.SelectActor(second.selectedCandidate.actor);
        var third = Forfeit();
        manager.GetMethod("ApplyMovementActorReplan", Private).Invoke(replay.decisions, new object[] { third, replay.lastSnapshot });
        if (!third.selectedCandidate.isForfeit)
        {
            Assert.That(third.selectedCandidate.actor, Is.Not.SameAs(first.selectedCandidate.actor));
            Assert.That(third.selectedCandidate.actor, Is.Not.SameAs(second.selectedCandidate.actor));
        }
        replay.lastSnapshot.homeScore++;
        var changed = Forfeit();
        manager.GetMethod("ApplyMovementActorReplan", Private).Invoke(replay.decisions, new object[] { changed, replay.lastSnapshot });
        Assert.That(changed.selectedCandidate.isForfeit, Is.True, "A changed board invalidates the saved alternatives.");
    }
    [Test]
    public void KeeperFirstLooseBallBuildsDefensivePlanDuringNominalAttack()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_KeeperCollection", 201);
        var decision = replay.Evaluate();
        Assert.That(replay.lastSnapshot.expectedTeam, Is.EqualTo(replay.lastSnapshot.teamInAttack));
        Assert.That(replay.lastSnapshot.expectedOpponentCollection, Is.True);
        Assert.That(replay.lastSnapshot.defensivePhasePlan, Is.Not.Null);
        Assert.That(replay.lastSnapshot.defensivePhasePlan.staticKeeperFtpRelayPairs,
            Does.Contain("Home:4->Home:6"), "Marell-to-Goaley is a concrete static relay candidate on this board.");
        Assert.That(replay.lastSnapshot.defensivePhasePlan.initialStaticKeeperFtpThreatByOutlet["Home:4"],
            Is.GreaterThan(0f), "Marell has a reachable static relay under the recorded positions.");
        Assert.That(replay.lastSnapshot.defensivePhasePlan.initialKeeperFtpThreatAfterDefendingMoveByOutlet["Home:4"],
            Is.LessThanOrEqualTo(replay.lastSnapshot.defensivePhasePlan.initialStaticKeeperFtpThreatByOutlet["Home:4"]),
            "The defender may decline its FTP free move; a projected reply cannot be worse than staying.");
        Assert.That(decision.selectedCandidate.isForfeit, Is.False);
        Assert.That(decision.selectedEvaluation.reasons, Has.Some.Contains("defensive"));
    }

    [Test]
    public void F3DoesNotAssumeAHeaderParticipantCanMoveToCollect()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_LooseBallF3", 192, false);
        var snapshot = replay.BoardSnapshot();
        var method = typeof(RoomAIDecisionSnapshotBuilder).GetMethod("OpponentCanCollectBeforeAttacker", StaticPrivate);
        Assert.That(method.Invoke(null, new object[] { snapshot, null, null }), Is.True);
        var header = replay.match.gameObject.AddComponent<HeaderManager>();
        replay.match.movementPhaseManager.headerManager = header;
        foreach (var token in replay.tokens.Values.Where(t => t.isPlaying && t.isHomeTeam && t.GetCurrentHex() != null
            && HexGridUtils.GetHexStepDistance(t.GetCurrentHex(), replay.match.ball.GetCurrentHex()) <= t.pace))
            header.defenderWillJump.Add(token);
        Assert.That(method.Invoke(null, new object[] { snapshot, null, null }), Is.False,
            "The next-phase forecast cannot assign the pickup to a player who just jumped.");
    }
    [Test]
    public void RecordedUnsupportedSpacePassCannotOverrideAFreshSaferTarget()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_UnsafeSpacePass", 187, false);
        var match = replay.match;
        match.currentState = MatchManager.GameState.EndOfMovementPhase;
        var pass = match.groundBallManager;
        pass.isActivated = true; pass.isAwaitingTargetSelection = true;
        var snapshot = replay.BoardSnapshot(); snapshot.currentState = "EndOfMovementPhase";
        snapshot.manager = nameof(GroundBallManager); snapshot.expectedTeam = "Away";
        var context = new RoomDecisionContext { manager = nameof(GroundBallManager), expectedTeam = "Away" };
        pass.PopulateRoomDecisionContext(context);
        snapshot.legalActions.AddRange(context.actions.Where(a => a.isExecutableNow));
        typeof(RoomAIDecisionSnapshotBuilder).GetMethod("BuildCandidateExplanations", StaticPrivate)
            .Invoke(null, new object[] { snapshot });
        var initial = new RoomAITacticalEvaluation { candidate = new RoomActionCandidate
            { manager = nameof(GroundBallManager), actionType = RoomActionType.GroundPass, step = RoomDecisionStep.ChooseActionType },
            explanation = new RoomAICandidateExplanation { groundPassContinuationTargetHex = Hex(-15, -6) } };
        typeof(RoomDecisionManager).GetMethod("RememberCommittedStagedDecision", Private)
            .Invoke(replay.decisions, new object[] { initial, snapshot });
        var decision = CodexTacticianProvider.Instance.Decide(snapshot, snapshot.legalActions, null,
            new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard);
        var rejected = decision.rankedCandidates.First(e => e.candidate.targetHex != null
            && e.candidate.targetHex.coordinates.x == -15 && e.candidate.targetHex.coordinates.z == -6);
        Assert.That(decision.rankedCandidates.Any(e => e.safetyPriority < rejected.safetyPriority), Is.True);
        bool retained = (bool)typeof(RoomDecisionManager).GetMethod("ApplyCommittedStagedDecision", Private)
            .Invoke(replay.decisions, new object[] { decision, snapshot });
        Assert.That(retained, Is.False);
        Assert.That(decision.selectedCandidate, Is.Not.SameAs(rejected.candidate));
    }

    [Test]
    public void RecordedPassActionCarriesFreshSafetyThroughTargetAndConfirmation()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_PassHandoff", 186, false);
        var match = replay.match;
        match.currentState = MatchManager.GameState.EndOfMovementPhase;
        match.movementPhaseManager.isActivated = false;
        var pass = match.groundBallManager;
        pass.isAvailable = true;
        var instruction = new GameplayInstructionSnapshot { manager = nameof(GroundBallManager),
            expectedTeam = "Away", isAwaitingInput = true };
        RoomDecisionContext Context()
        {
            var context = new RoomDecisionContext { manager = nameof(GroundBallManager), expectedTeam = "Away" };
            pass.PopulateRoomDecisionContext(context);
            return context;
        }
        RoomAIDecisionSnapshot Analyze(RoomDecisionContext context, System.Collections.Generic.List<RoomActionCandidate> actions)
            => RoomAIDecisionSnapshotBuilder.Build(instruction, context, actions, true);
        var rootContext = Context();
        var rootActions = rootContext.actions.Where(a => a.isExecutableNow).ToList();
        var root = Analyze(rootContext, rootActions);
        var rootDecision = CodexTacticianProvider.Instance.Decide(root, rootActions, null,
            new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard);
        Assert.That(rootDecision.selectedCandidate.actionType, Is.EqualTo(RoomActionType.GroundPass));
        typeof(RoomDecisionManager).GetMethod("RememberCommittedStagedDecision", Private)
            .Invoke(replay.decisions, new object[] { rootDecision.selectedEvaluation, root });

        pass.isActivated = true;
        pass.isAwaitingTargetSelection = true;
        var targetContext = Context();
        var targetActions = targetContext.actions.Where(a => a.isExecutableNow && a.step == RoomDecisionStep.ChooseTarget).ToList();
        var targetBoard = Analyze(targetContext, targetActions);
        var targetDecision = CodexTacticianProvider.Instance.Decide(targetBoard, targetActions, null,
            new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard);
        typeof(RoomDecisionManager).GetMethod("ApplyCommittedStagedDecision", Private)
            .Invoke(replay.decisions, new object[] { targetDecision, targetBoard });
        var chosen = targetDecision.selectedEvaluation;
        var selectedHex = chosen.candidate.targetHex ?? chosen.candidate.targetToken?.GetCurrentHex();
        Assert.That(selectedHex, Is.Not.Null);
        var unsafeRecorded = targetDecision.rankedCandidates.FirstOrDefault(e => e.candidate.targetHex != null
            && e.candidate.targetHex.coordinates.x == -15 && e.candidate.targetHex.coordinates.z == -6);
        if (unsafeRecorded != null && targetDecision.rankedCandidates.Any(e => e.safetyPriority < unsafeRecorded.safetyPriority))
            Assert.That(chosen, Is.Not.SameAs(unsafeRecorded));
        typeof(RoomDecisionManager).GetMethod("RememberGroundTargetSelection", Private)
            .Invoke(replay.decisions, new object[] { chosen.candidate, targetBoard });

        pass.currentTargetHex = selectedHex;
        var confirmContext = Context();
        var confirmActions = (System.Collections.Generic.List<RoomActionCandidate>)typeof(RoomDecisionManager)
            .GetMethod("BuildTacticalExecutableCandidates", Private)
            .Invoke(replay.decisions, new object[] { instruction, confirmContext, true });
        Assert.That(confirmActions, Is.Not.Empty);
        Assert.That(confirmActions.All(a => a.step == RoomDecisionStep.Confirm), Is.True,
            "An unchanged board should retain its selected target for confirmation.");
        var confirmBoard = Analyze(confirmContext, confirmActions);
        var confirm = CodexTacticianProvider.Instance.Decide(confirmBoard, confirmActions, null,
            new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard).selectedEvaluation;
        Assert.That(confirm.candidate.step, Is.EqualTo(RoomDecisionStep.Confirm));
        Assert.That(confirm.explanation.groundPassCollectorTokenKey, Is.EqualTo(chosen.explanation.groundPassCollectorTokenKey));
        Assert.That(confirm.explanation.groundPassCollectorPickupDistance, Is.EqualTo(chosen.explanation.groundPassCollectorPickupDistance));
        Assert.That(confirm.explanation.groundPassRetentionProbability,
            Is.EqualTo(chosen.explanation.groundPassRetentionProbability).Within(.00001f));
        Assert.That(confirm.safetyPriority, Is.EqualTo(chosen.safetyPriority));
    }
    [TestCase(144)]
    [TestCase(170)]
    [TestCase(184)]
    public void RecordedActorSelectionContinuesToALegalUsefulDestination(int number)
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_ActorSelections", number);
        replay.match.difficulty_level = 3;
        var first = replay.Evaluate();
        var actor = first.selectedCandidate.actor;
        Assert.That(actor, Is.Not.Null);
        typeof(RoomDecisionManager).GetMethod("RememberCommittedStagedDecision", Private)
            .Invoke(replay.decisions, new object[] { first.selectedEvaluation, replay.lastSnapshot });
        replay.SelectActor(actor);
        var second = replay.Evaluate();
        typeof(RoomDecisionManager).GetMethod("ApplyCommittedStagedDecision", Private)
            .Invoke(replay.decisions, new object[] { second, replay.lastSnapshot });
        Assert.That(second.selectedCandidate.isForfeit, Is.False);
        Assert.That(second.selectedCandidate.targetHex, Is.Not.Null);
        Assert.That(replay.match.hexGrid.highlightedHexes, Does.Contain(second.selectedCandidate.targetHex));
    }
    [Test]
    public void RecordedAguayoSupportKeepsASeparateOutlet()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_Support", 296);
        replay.SelectActor(replay.tokens["Away:10"]);
        var decision = replay.Evaluate();
        Assert.That(decision.selectedCandidate.targetHex, Is.Not.Null);
        Assert.That(HexGridUtils.GetHexStepDistance(decision.selectedCandidate.targetHex,
            replay.match.ball.GetCurrentHex()), Is.GreaterThan(1));
        Assert.That(decision.selectedEvaluation.explanation.supportsConcreteContinuation, Is.True);
    }
    [Test]
    public void SupportActorPreviewExcludesGoalLinePlacementsTheMovementManagerCannotOffer()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_Support", 296);
        var actor = replay.tokens["Away:10"];
        var action = new RoomActionCandidate { manager = nameof(MovementPhaseManager),
            actionType = RoomActionType.StartMovement, step = RoomDecisionStep.ChooseActor, actor = actor };
        var destinations = typeof(RoomAIDecisionSnapshotBuilder).GetMethod("GetAttackingSupportDestinations", StaticPrivate);
        var snapshot = replay.BoardSnapshot();
        var reachable = ((IEnumerable<HexCell>)destinations.Invoke(null, new object[] { snapshot, action })).ToList();
        Assert.That(reachable, Is.Not.Empty);
        var goalLine = reachable.First();
        int originalGoalFlag = goalLine.isInGoal;
        try
        {
            // Compare the actor preview with the same rule used when the
            // movement manager highlights actual destinations.
            goalLine.isInGoal = 1;
            Assert.That(((IEnumerable<HexCell>)destinations.Invoke(null, new object[] { snapshot, action }))
                .Contains(goalLine), Is.False);
            Assert.That(replay.match.movementPhaseManager.CanEnterMovementGoalHexForDecisionPreview(actor, goalLine), Is.False);
        }
        finally { goalLine.isInGoal = originalGoalFlag; }
    }
    [TestCase(false, true)]
    [TestCase(true, false)]
    public void LooseBallF3UsesLineFactsOnlyWhenOpponentCanCollectFirst(bool attackerCanCollect, bool expected)
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_LooseBallF3", 192, false);
        if (attackerCanCollect) replay.Move("Away:11", Hex(-14, -6));
        var snapshot = replay.BoardSnapshot();
        var target = replay.Hex(Hex(13, -4));
        var action = new RoomActionCandidate { actionType = RoomActionType.FinalThird,
            actor = replay.tokens["Away:4"], targetHex = target, step = RoomDecisionStep.ChooseTarget };
        var facts = new RoomAICandidateExplanation { candidate = action, actorTokenKey = "Away:4",
            actorHex = Hex(8, 0), targetHex = Hex(13, -4) };
        typeof(RoomAIDecisionSnapshotBuilder).GetMethod("PopulateRegularFinalThirdFacts", StaticPrivate)
            .Invoke(null, new object[] { snapshot, action, facts, target });
        Assert.That(facts.isRegularFinalThirdReorganization, Is.EqualTo(expected));
        if (expected) Assert.That(facts.offsideLineRaise, Is.LessThan(0));
    }
    [Test]
    public void RecordedAttackingHighPassUsesFreeMoveForReachableOutlet()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_AttackingHighPass", 110, false);
        var match = replay.match;
        match.currentState = MatchManager.GameState.HighPass;
        var hp = match.gameObject.AddComponent<HighPassManager>(); match.highPassManager = hp;
        hp.hexGrid = match.hexGrid; hp.ball = match.ball; hp.movementPhaseManager = match.movementPhaseManager;
        hp.isActivated = true; hp.isWaitingForAttackerSelection = true; hp.isWaitingForAttackerMove = true;
        hp.currentTargetHex = replay.Hex(Hex(0, -8)); hp.lockedAttacker = replay.tokens["Away:11"];
        hp.selectedToken = replay.tokens["Away:10"];
        match.hexGrid.highlightedHexes.AddRange(HexGridUtils.GetReachableHexes(match.hexGrid,
            hp.selectedToken.GetCurrentHex(), 3).reachableHexes.Where(hex => !hex.isOutOfBounds
                && !hex.isAttackOccupied && !hex.isDefenseOccupied));
        var snapshot = replay.BoardSnapshot(); snapshot.currentState = "HighPass"; snapshot.manager = nameof(HighPassManager);
        var context = new RoomDecisionContext { manager = nameof(HighPassManager), expectedTeam = "Away" };
        hp.PopulateRoomDecisionContext(context);
        snapshot.legalActions.AddRange(context.actions.Where(action => action.isExecutableNow));
        typeof(RoomAIDecisionSnapshotBuilder).GetMethod("BuildCandidateExplanations", StaticPrivate)
            .Invoke(null, new object[] { snapshot });
        var decision = CodexTacticianProvider.Instance.Decide(snapshot, snapshot.legalActions, null,
            new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard);
        Assert.That(decision.selectedCandidate.isForfeit, Is.False);
        Assert.That(decision.selectedEvaluation.reasons, Has.Some.Contains("headed-pass landing"));
    }

    [Test]
    public void HighPassCollectorMustHaveAnUnblockedPreparationRoute()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_Support", 296, false);
        var hp = replay.match.gameObject.AddComponent<HighPassManager>();
        replay.match.highPassManager = hp;
        var actor = replay.tokens["Away:10"];
        var grid = replay.match.hexGrid;
        var start = actor.GetCurrentHex();
        var reach = HexGridUtils.GetReachableHexes(grid, start, HighPassManager.AttackerMoveRange);
        var target = reach.reachableHexes.First(h => h != null && !h.isOutOfBounds
            && !h.isAttackOccupied && !h.isDefenseOccupied && reach.Item2[h].distance == 2);
        var check = typeof(RoomAIDecisionSnapshotBuilder).GetMethod("CanPrepareHighPassLanding", StaticPrivate);
        bool CanReach(RoomAIDecisionSnapshot snapshot) => (bool)check.Invoke(null, new object[] { snapshot, hp, actor, target });
        Assert.That(CanReach(replay.BoardSnapshot()), Is.True);
        foreach (var neighbour in start.GetNeighbors(grid).Where(h => h != null)) neighbour.isAttackOccupied = true;
        Assert.That(HexGridUtils.GetHexStepDistance(start, target), Is.EqualTo(2));
        Assert.That(CanReach(replay.BoardSnapshot()), Is.False,
            "A geometric two-hex distance does not overcome occupied paths.");
    }

    [Test]
    public void RecordedTaylorCanReachTheHighPassHeaderContestWithHisFreeMove()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_AerialSequence", 106, false);
        var taylor = replay.tokens["Home:2"];
        var target = replay.Hex(Hex(0, -8));
        var check = typeof(RoomAIDecisionSnapshotBuilder).GetMethod("CanEnterHighPassHeaderContest", StaticPrivate);
        bool CanReach(RoomAIDecisionSnapshot snapshot) => (bool)check.Invoke(null, new object[] { snapshot, taylor, target });
        Assert.That(HexGridUtils.GetHexStepDistance(taylor.GetCurrentHex(), target), Is.GreaterThan(HeaderManager.HEADER_SELECTION_RANGE));
        Assert.That(CanReach(replay.BoardSnapshot()), Is.True, "Taylor can use the three-hex defensive free move to enter header range.");
        foreach (var neighbour in taylor.GetCurrentHex().GetNeighbors(replay.match.hexGrid).Where(h => h != null))
            neighbour.isDefenseOccupied = true;
        Assert.That(CanReach(replay.BoardSnapshot()), Is.False, "A blocked free-move route must not create a phantom contestant.");
    }

    [Test]
    public void RecordedHighPassProjectsFreeMovesBeforeTheHeaderWithoutMovingLiveTokens()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_AerialSequence", 106, false);
        var match = replay.match;
        var hp = match.gameObject.AddComponent<HighPassManager>();
        match.highPassManager = hp;
        var target = replay.Hex(Hex(0, -8));
        var before = replay.tokens.Where(p => p.Value.GetCurrentHex() != null)
            .ToDictionary(p => p.Key, p => p.Value.GetCurrentHex());
        var action = new RoomActionCandidate { manager = nameof(HighPassManager), actionType = RoomActionType.HighPass,
            step = RoomDecisionStep.ChooseTarget, targetHex = target };
        var snapshot = replay.BoardSnapshot(); snapshot.currentState = "HighPass";
        var preview = RoomAIActionSequence.Evaluate(snapshot, new RoomAICandidateExplanation { candidate = action });
        Assert.That(preview, Is.Not.Null);
        Assert.That(preview.usesProjectedAerialFreeMoves, Is.True);
        Assert.That(preview.aerialFreeMoveRespondersEvaluated, Does.Contain("Home:2"), "Taylor's legal contest is considered.");
        Assert.That(preview.aerialAttackingPreparationsEvaluated, Is.Not.Empty,
            "When the receiver is already on the target, legal support moves are compared with declining.");
        Assert.That(preview.accurateOutcomes.defenseWinsPossessionProbability, Is.GreaterThan(0f));
        Assert.That(preview.horizon, Does.Contain("F3"));
        Assert.That(before.All(p => replay.tokens[p.Key].GetCurrentHex() == p.Value), Is.True,
            "Counterfactual movements must not alter the reconstructed board.");
        var openLanding = target.GetNeighbors(match.hexGrid).First(h => h != null && !h.isOutOfBounds
            && h.isInFinalThird == 0 && !h.isAttackOccupied && !h.isDefenseOccupied);
        action.targetHex = openLanding;
        var prepared = RoomAIActionSequence.Evaluate(replay.BoardSnapshot(), new RoomAICandidateExplanation { candidate = action });
        Assert.That(prepared, Is.Not.Null);
        Assert.That(prepared.steps.Any(step => step.action == "HP attacker prepares landing"), Is.True);
        Assert.That(before.All(p => replay.tokens[p.Key].GetCurrentHex() == p.Value), Is.True);
    }

    [Test]
    public void ProjectedHeaderOutletUsesProjectedDefenderPositionForPressure()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_AerialSequence", 106, false);
        var match = replay.match;
        var snapshot = replay.BoardSnapshot();
        var players = match.playerTokenManager.allTokens.Where(t => t != null && t.isPlaying && !t.isSentOff
            && t.GetCurrentHex() != null).ToList();
        var receiver = replay.tokens["Away:10"];
        var defender = replay.tokens["Home:2"];
        defender.tackling = 5;
        var livePosition = receiver.GetCurrentHex();
        var type = typeof(RoomAIActionSequence).GetNestedType("Search", BindingFlags.NonPublic);
        var search = type.GetConstructors().Single().Invoke(new object[] { snapshot, match, players, false });
        var method = type.GetMethod("ProjectedHeaderOutletPreference", Private);
        var destination = replay.Hex(Hex(0, 0));
        var board = new Dictionary<PlayerToken, HexCell> { [receiver] = destination, [defender] = replay.Hex(Hex(1, 0)) };
        float pressured = (float)method.Invoke(search, new object[] { receiver, destination, board });
        board[defender] = replay.Hex(Hex(12, 0));
        float open = (float)method.Invoke(search, new object[] { receiver, destination, board });
        Assert.That(open, Is.GreaterThan(pressured), "The projected defender move changes the outlet's next challenge.");
        Assert.That(receiver.GetCurrentHex(), Is.SameAs(livePosition), "The projected board must not move live tokens.");
    }

    [Test]
    public void ProjectedHeaderCanUseAReachableSpaceCollectorWhenNoDirectReceiverExists()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_AerialSequence", 106, false);
        var match = replay.match;
        match.highPassManager = match.gameObject.AddComponent<HighPassManager>();
        replay.Move("Away:10", Hex(10, -8));
        foreach (string key in new[] { "Away:2", "Away:3", "Away:4", "Away:5", "Away:6", "Away:7", "Away:8" })
        {
            var open = Enumerable.Range(-12, 25).Select(z => replay.Hex(Hex(12, z)))
                .First(h => h != null && !h.isOutOfBounds && !h.isAttackOccupied && !h.isDefenseOccupied);
            replay.Move(key, RoomHexCoordinates.FromHex(open));
        }
        var action = new RoomActionCandidate { manager = nameof(HighPassManager), actionType = RoomActionType.HighPass,
            step = RoomDecisionStep.ChooseTarget, targetHex = replay.Hex(Hex(0, -8)) };
        var preview = RoomAIActionSequence.Evaluate(replay.BoardSnapshot(), new RoomAICandidateExplanation { candidate = action });
        Assert.That(preview, Is.Not.Null);
        Assert.That(preview.usesProjectedAerialFreeMoves, Is.True);
        Assert.That(preview.steps.Any(step => step.action == "Headed pass into space"), Is.True);
        var collection = preview.steps.Single(step => step.action == "Collect after header; next defense unexpanded");
        Assert.That(collection.actorTokenKey, Is.EqualTo("Away:10"));
        Assert.That(collection.movementCost, Is.InRange(1, replay.tokens["Away:10"].pace));
        var dribble = preview.steps.SingleOrDefault(step => step.action == "Projected dribble after header collection");
        if (dribble != null)
        {
            Assert.That(dribble.movementCost, Is.InRange(1, collection.remainingPace));
            Assert.That(dribble.remainingPace, Is.EqualTo(collection.remainingPace - dribble.movementCost));
        }
        Assert.That(preview.aerialHeaderParticipantTokenKeys, Is.Not.Empty);
        if (preview.defensiveResponse != null)
        {
            Assert.That(preview.defensiveResponse.carrierTokenKey, Is.EqualTo(collection.actorTokenKey));
            Assert.That(preview.defensiveResponse.moves.All(move => !preview.aerialHeaderParticipantTokenKeys.Contains(move.tokenKey)), Is.True,
                "Nominated jumpers cannot be reused in the immediately following movement phase.");
        }
    }

    [Test]
    public void ProjectedCarrierRoutesRespectRemainingPaceOccupancyAndBoxEntry()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_AerialSequence", 106, false);
        var match = replay.match;
        var snapshot = replay.BoardSnapshot();
        var players = match.playerTokenManager.allTokens.Where(t => t != null && t.isPlaying && !t.isSentOff
            && t.GetCurrentHex() != null).ToList();
        var carrier = replay.tokens["Away:10"];
        var board = players.ToDictionary(t => t, t => t.GetCurrentHex());
        var type = typeof(RoomAIActionSequence).GetNestedType("Search", BindingFlags.NonPublic);
        var search = type.GetConstructors().Single().Invoke(new object[] { snapshot, match, players, false });
        var method = type.GetMethod("ProjectedCarrierRoutes", Private);
        var routes = (Dictionary<HexCell, List<HexCell>>)method.Invoke(search, new object[] { board, carrier, 2 });
        Assert.That(routes.Count, Is.GreaterThan(1));
        var occupied = board.Where(p => p.Key != carrier).Select(p => p.Value).ToHashSet();
        foreach (var route in routes.Values)
        {
            Assert.That(route.Count, Is.LessThanOrEqualTo(2));
            var previous = board[carrier];
            foreach (var step in route)
            {
                Assert.That(occupied.Contains(step), Is.False);
                Assert.That(HexGridUtils.GetHexStepDistance(previous, step), Is.EqualTo(1));
                Assert.That(previous.isInPenaltyBox == 0 && step.isInPenaltyBox != 0, Is.False,
                    "Box entry needs a goalkeeper reply before a continuation can be valued.");
                previous = step;
            }
        }
    }

    [Test]
    public void ProjectedScatterOnlyCreditsCollectionWhenNoHeaderContestIntervenes()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_AerialSequence", 106, false);
        var match = replay.match;
        match.highPassManager = match.gameObject.AddComponent<HighPassManager>();
        var snapshot = replay.BoardSnapshot();
        var players = match.playerTokenManager.allTokens.Where(t => t != null && t.isPlaying && !t.isSentOff
            && t.GetCurrentHex() != null).ToList();
        var board = players.ToDictionary(t => t, t => t.GetCurrentHex());
        var passer = match.ball.GetCurrentHex().GetOccupyingToken();
        var candidate = players.Where(t => !t.isHomeTeam && !t.IsGoalKeeper && t != passer
                && match.CanTokenCollectHangingPass(t)
                && !RoomAIActionSequence.OffsideForFirstAerial(snapshot, RoomHexCoordinates.FromHex(t.GetCurrentHex()), snapshot.ball))
            .SelectMany(t => HexGridUtils.GetReachableHexes(match.hexGrid, t.GetCurrentHex(), t.pace).Item2
                .Where(p => p.Value.distance > HeaderManager.HEADER_SELECTION_RANGE && !p.Key.isOutOfBounds
                    && p.Key.isInFinalThird == 0 && p.Key.isInPenaltyBox == 0
                    && board.All(existing => HexGridUtils.GetHexStepDistance(existing.Value, p.Key) > HeaderManager.HEADER_SELECTION_RANGE))
                .Select(p => (actor: t, target: p.Key))).First();
        var type = typeof(RoomAIActionSequence).GetNestedType("Search", BindingFlags.NonPublic);
        var search = type.GetConstructors().Single().Invoke(new object[] { snapshot, match, players, false });
        var evaluate = type.GetMethod("EvaluateProjectedScatter", Private);
        RoomAIActionSequencePreview Preview() => (RoomAIActionSequencePreview)evaluate.Invoke(search,
            new object[] { passer, match.ball.GetCurrentHex(), candidate.target, board });
        var clear = Preview();
        Assert.That(clear.outcomes.attackRetainsPossessionProbability, Is.EqualTo(1f).Within(.0001f));
        Assert.That(clear.steps.Single().action, Does.Contain("Collect after projected scatter"));
        var header = replay.tokens["Away:11"];
        var receiver = replay.tokens["Away:10"];
        var oldHeader = board[header]; var oldReceiver = board[receiver];
        var headerHex = candidate.target.GetNeighbors(match.hexGrid).First(h => h != null && !h.isOutOfBounds
            && !board.Values.Contains(h));
        var receiverHex = HexGrid.GetHexesInRange(match.hexGrid, candidate.target, 6).First(h => h != null
            && !h.isOutOfBounds && h.coordinates.x > 0 && h != headerHex && !board.Values.Contains(h));
        board[header] = headerHex; board[receiver] = receiverHex;
        var freeHeader = Preview();
        Assert.That(freeHeader.steps.Any(step => step.action == "Uncontested header after scatter"), Is.True);
        Assert.That(freeHeader.outcomes.attackRetainsPossessionProbability, Is.EqualTo(1f).Within(.0001f));
        var adjacent = candidate.target.GetNeighbors(match.hexGrid).First(h => h != null && !h.isOutOfBounds
            && !h.isAttackOccupied && !h.isDefenseOccupied && h != headerHex && h != receiverHex);
        replay.Move("Home:7", RoomHexCoordinates.FromHex(adjacent));
        board[replay.tokens["Home:7"]] = adjacent;
        var contested = Preview();
        Assert.That(contested.steps.Any(step => step.action == "Contested header after scatter"), Is.True);
        Assert.That(contested.outcomes.defenseWinsPossessionProbability, Is.GreaterThan(0f));
        board[header] = oldHeader; board[receiver] = oldReceiver;
        Assert.That(Preview().outcomes.unresolvedProbability, Is.EqualTo(1f).Within(.0001f),
            "A potential header contest is not a guaranteed free collection.");
    }

    [Test]
    public void HighPassBoxEntryProjectsTheGoalkeepersMoveAndRush()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_AerialSequence", 106, false);
        var match = replay.match;
        match.highPassManager = match.gameObject.AddComponent<HighPassManager>();
        var target = replay.Hex(Hex(-12, -3));
        Assert.That(target.isInPenaltyBox, Is.EqualTo(-1));
        replay.Move("Away:11", RoomHexCoordinates.FromHex(target));
        replay.Move("Home:2", Hex(-13, -2));
        var keeper = replay.tokens["Home:1"];
        var originalKeeperHex = keeper.GetCurrentHex();
        var goalkeeper = match.movementPhaseManager.goalKeeperManager;
        Assert.That(goalkeeper.HasConsumedBoxMoveForDecisionPreview(-1), Is.False);
        var action = new RoomActionCandidate { manager = nameof(HighPassManager), actionType = RoomActionType.HighPass,
            step = RoomDecisionStep.ChooseTarget, targetHex = target };
        var preview = RoomAIActionSequence.Evaluate(replay.BoardSnapshot(), new RoomAICandidateExplanation { candidate = action });
        Assert.That(preview, Is.Not.Null);
        Assert.That(preview.usesProjectedAerialFreeMoves, Is.True);
        Assert.That(preview.accurateOutcomes.defenseWinsPossessionProbability, Is.GreaterThan(0f));
        Assert.That(preview.steps.Any(step => step.action == "Goalkeeper aerial rush"), Is.True);
        Assert.That(keeper.GetCurrentHex(), Is.SameAs(originalKeeperHex), "The goalkeeper response is projected only.");
        typeof(GoalKeeperManager).GetField("consumedBoxMovePenaltyBox", Private).SetValue(goalkeeper, -1);
        Assert.That(goalkeeper.HasConsumedBoxMoveForDecisionPreview(-1), Is.True);
        Assert.That(goalkeeper.HasConsumedBoxMoveForDecisionPreview(1), Is.False);
    }

    [Test]
    public void ProjectedGoalkeeperRushUsesOnlyReachableHexesAndRespectsAnEarlierHpMove()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_AerialSequence", 106, false);
        var match = replay.match;
        var hp = match.gameObject.AddComponent<HighPassManager>(); match.highPassManager = hp;
        var snapshot = replay.BoardSnapshot();
        var players = match.playerTokenManager.allTokens.Where(t => t != null && t.isPlaying && !t.isSentOff
            && t.GetCurrentHex() != null).ToList();
        var board = players.ToDictionary(t => t, t => t.GetCurrentHex());
        var passer = match.ball.GetCurrentHex().GetOccupyingToken();
        var keeper = replay.tokens["Home:1"];
        var target = replay.Hex(Hex(-12, -3));
        var type = typeof(RoomAIActionSequence).GetNestedType("Search", BindingFlags.NonPublic);
        var search = type.GetConstructors().Single().Invoke(new object[] { snapshot, match, players, false });
        var method = type.GetMethod("ProjectedGoalkeeperReplies", Private);
        List<(Dictionary<PlayerToken, HexCell> board, HexCell boxHex, HexCell rushHex)> Replies() =>
            ((IEnumerable<(Dictionary<PlayerToken, HexCell> board, HexCell boxHex, HexCell rushHex)>)method.Invoke(search,
                new object[] { board, passer, match.ball.GetCurrentHex(), target, keeper })).ToList();
        var replies = Replies();
        Assert.That(replies.Any(r => r.boxHex != null), Is.True);
        Assert.That(replies.Any(r => r.rushHex != null), Is.True);
        Assert.That(replies.Where(r => r.rushHex != null).All(r =>
            HexGridUtils.GetHexStepDistance(r.rushHex, target) <= HeaderManager.HEADER_SELECTION_RANGE), Is.True);
        Assert.That(keeper.GetCurrentHex(), Is.SameAs(board[keeper]));
        hp.didGKMoveInDefPhase = true;
        Assert.That(Replies().Any(r => r.rushHex != null), Is.True,
            "A stale flag from a previous pass cannot consume the next pass's goalkeeper response.");
        hp.isActivated = true;
        hp.isWaitingForAccuracyRoll = true;
        Assert.That(Replies().All(r => r.rushHex == null), Is.True,
            "A goalkeeper already moved in the defending HP stage cannot also rush.");
    }

    [Test]
    public void DistantF3IsSkippedOnlyWhenItCannotChangeReachOrTheOffsideLine()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_AerialSequence", 106, false);
        var match = replay.match;
        var snapshot = replay.BoardSnapshot();
        var players = match.playerTokenManager.allTokens.Where(t => t != null && t.isPlaying && !t.isSentOff
            && t.GetCurrentHex() != null).ToList();
        var type = typeof(RoomAIActionSequence).GetNestedType("Search", BindingFlags.NonPublic);
        var search = type.GetConstructors().Single().Invoke(new object[] { snapshot, match, players, false });
        var check = type.GetMethod("F3CouldAffectAerialContinuation", Private);
        var attacker = replay.tokens["Away:10"];
        int oldPace = attacker.pace;
        attacker.pace = 5;
        var board = new Dictionary<PlayerToken, HexCell> {
            [replay.tokens["Home:1"]] = replay.Hex(Hex(-16, 0)),
            [replay.tokens["Home:2"]] = replay.Hex(Hex(-12, 0)),
            [replay.tokens["Home:3"]] = replay.Hex(Hex(10, 0)),
            [attacker] = replay.Hex(Hex(10, 4)) };
        bool Affects(HexCell landing) => (bool)check.Invoke(search, new object[] { board, landing, null });
        Assert.That(Affects(replay.Hex(Hex(-8, 0))), Is.False, "Remote moves cannot reach the bounded route or move its offside line.");
        board[attacker] = replay.Hex(Hex(8, 0));
        Assert.That(Affects(replay.Hex(Hex(-8, 0))), Is.True,
            "An F3 player may move six hexes before a headed release and collection, rather than only one.");
        board[attacker] = replay.Hex(Hex(10, 4));
        attacker.pace = 20;
        Assert.That(Affects(replay.Hex(Hex(-8, 0))), Is.True, "A fast enough remote collector prevents skipping F3.");
        attacker.pace = oldPace;
        board[replay.tokens["Home:3"]] = replay.Hex(Hex(-8, 2));
        board[attacker] = replay.Hex(Hex(-10, 4));
        Assert.That(Affects(replay.Hex(Hex(8, 0))), Is.True, "F3 can change the pass-time offside line even when the players are far away.");
    }

    [Test]
    public void HeaderReceptionKeepsTheStoredOffsideAfterTheReceiverMovesBack()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_AerialSequence", 117, false);
        var match = replay.match;
        match.difficulty_level = 3;
        var receiver = replay.tokens["Away:10"];
        var advanced = Enumerable.Range(-8, 17).Select(z => replay.Hex(Hex(-15, z)))
            .First(h => h != null && !h.isOutOfBounds && !h.isAttackOccupied && !h.isDefenseOccupied);
        replay.Move("Away:10", RoomHexCoordinates.FromHex(advanced));
        match.offsideManager.EvaluateAndStore("header_preview_test", true);
        Assert.That(match.offsideManager.IsTokenOffside(receiver), Is.True);
        var back = Enumerable.Range(-8, 17).Select(z => replay.Hex(Hex(5, z)))
            .First(h => h != null && !h.isOutOfBounds && !h.isAttackOccupied && !h.isDefenseOccupied);
        replay.Move("Away:10", RoomHexCoordinates.FromHex(back));
        Assert.That(match.offsideManager.IsTokenOffside(receiver), Is.True, "Moving back does not erase the stored assessment.");
        Assert.That(match.offsideManager.ShouldBlockCollection(receiver), Is.False, "High difficulty does not block the input.");
        var snapshot = replay.BoardSnapshot(); snapshot.currentState = "Header";
        Assert.That(RoomAIActionSequence.Offside(snapshot, RoomHexCoordinates.FromHex(back), snapshot.ball), Is.False);
        var header = match.gameObject.AddComponent<HeaderManager>();
        match.movementPhaseManager.headerManager = header;
        var action = new RoomActionCandidate { manager = nameof(HeaderManager), actionType = RoomActionType.Header,
            step = RoomDecisionStep.ChooseTarget, targetHex = back, targetToken = receiver };
        var facts = new RoomAICandidateExplanation { candidate = action };
        typeof(RoomAIDecisionSnapshotBuilder).GetMethod("PopulateHeaderPossessionChainFacts", StaticPrivate)
            .Invoke(null, new object[] { snapshot, action, facts, back, header, new List<HexCell>() });
        Assert.That(facts.headedPassTargetIsOffside, Is.True);
        Assert.That(facts.possessionChain.defendingRestartProbability, Is.EqualTo(1f).Within(.0001f));
        var sequence = RoomAIActionSequence.Evaluate(snapshot, facts);
        Assert.That(sequence, Is.Not.Null);
        Assert.That(sequence.outcomes.defendingRestartProbability, Is.EqualTo(1f).Within(.0001f));
        action.actionType = RoomActionType.GroundPass;
        action.manager = nameof(GroundBallManager);
        var ground = RoomAIActionSequence.Evaluate(snapshot, new RoomAICandidateExplanation { candidate = action });
        Assert.That(ground, Is.Not.Null);
        Assert.That(ground.steps.Any(step => step.action == "Offside collection rejected"), Is.True,
            "The detailed ground-pass forecast must retain the same stored assessment.");
    }
    [TestCase(145)]
    [TestCase(171)]
    [TestCase(185)]
    public void SelectedVrabecDoesNotForfeitTheWholeMovementPhase(int number)
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_PhaseForfeits", number);
        replay.match.difficulty_level = 3;
        replay.SelectActor(replay.tokens["Away:5"]);
        var decision = replay.Evaluate();
        Assert.That(decision.selectedCandidate.isForfeit, Is.False);
        Assert.That(decision.selectedCandidate.targetHex, Is.Not.Null);
        Assert.That(replay.match.hexGrid.highlightedHexes, Does.Contain(decision.selectedCandidate.targetHex));
    }
    [Test]
    public void SecondGoalLooseBallPrioritizesAvailableRecoveryOverDistantForwards()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_SecondGoal", 332);
        var decision = replay.Evaluate();
        Assert.That(decision.selectedCandidate.actor, Is.SameAs(replay.tokens["Away:5"]));
        replay.SelectActor(replay.tokens["Away:5"]);
        var destination = replay.Evaluate();
        Assert.That(destination.selectedCandidate.isForfeit, Is.False);
        Assert.That(destination.selectedCandidate.targetHex, Is.Not.Null);
        Assert.That(HexGridUtils.GetHexStepDistance(destination.selectedCandidate.targetHex,
            replay.match.ball.GetCurrentHex()), Is.LessThanOrEqualTo(2));
    }
    private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private MatchManager previous;
    [SetUp] public void SetUp() { previous = MatchManager.Instance; MatchManager.Instance = null; }
    [TearDown] public void TearDown() { MatchManager.Instance = previous; }
    private static RoomHexCoordinates Hex(int x, int z = 0) => new() { x = x, z = z };
    private static RoomAITokenSnapshot Token(string key, int x, int z = 0) => new()
    {
        tokenKey = key, teamSide = key.Split(':')[0], currentHex = Hex(x, z),
        isPlaying = true, pace = 5, heading = 4, tackling = 4, shooting = 4
    };
    private static void Policy(string name, params object[] args) =>
        typeof(CodexTacticianProvider).GetMethod(name, StaticPrivate).Invoke(null, args);
    private static RoomAITacticalEvaluation Move(float value = .95f)
    {
        var candidate = new RoomActionCandidate { manager = nameof(MovementPhaseManager),
            actionType = RoomActionType.StartMovement, step = RoomDecisionStep.ChooseTarget, isExecutableNow = true };
        return new RoomAITacticalEvaluation { candidate = candidate, explanation = new RoomAICandidateExplanation
        {
            candidate = candidate, actorTokenKey = "Away:8", actorToken = Token("Away:8", 3, 3),
            actorHex = Hex(3, 3), targetHex = Hex(0, 2), isAttackingOffBallMove = true,
            supportsConcreteContinuation = true, attackingContinuation = "Improves the receiving outlet",
            attackingContinuationValue = value, hasPossessionLevelPlan = true,
            possessionPlanEffectiveRetentionProbability = .667f, possessionPlanThreatExpectedGoals = 0f
        } };
    }
    private static RoomAITacticalEvaluation End()
    {
        var candidate = new RoomActionCandidate { manager = nameof(MovementPhaseManager), actionType = RoomActionType.Decline,
            step = RoomDecisionStep.InterruptionChoice, key = "X", isForfeit = true, isExecutableNow = true };
        return new RoomAITacticalEvaluation { candidate = candidate, explanation = new() { candidate = candidate } };
    }
    private static RoomAIDecisionSnapshot Attack() => new()
    {
        expectedTeam = "Away", teamInAttack = "Away", attackHasPossession = true, currentState = "MovementPhase",
        awayTeamDirection = "RightToLeft", homeTeamDirection = "LeftToRight", ball = Hex(10, -3)
    };
    private static RoomAIProviderDecision Decide(RoomAIDecisionSnapshot snapshot, params RoomAITacticalEvaluation[] choices)
    {
        snapshot.legalActions.AddRange(choices.Select(e => e.candidate));
        snapshot.candidateExplanations.AddRange(choices.Select(e => e.explanation));
        return CodexTacticianProvider.Instance.Decide(snapshot, snapshot.legalActions, null,
            new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard);
    }

    [Test]
    public void RecoverySupportDoesNotAbandonTheCentreBackCoverForASmallerBenefit()
    {
        var snapshot = Attack();
        snapshot.ball = Hex(0, 0);
        var actor = Token("Away:4", 12); actor.jerseyNumber = 4;
        var partner = Token("Away:5", 12, 2); partner.jerseyNumber = 5;
        var carrier = Token("Away:8", 0);
        var opponent = Token("Home:9", 10, 2);
        snapshot.tokens.AddRange(new[] { actor, partner, carrier, opponent });
        var target = Hex(7);
        float recovery = RoomAITacticalSafety.RecoverySupportGain(snapshot, actor, target);
        Assert.That(recovery, Is.GreaterThan(0f), "There is a real potential recovery benefit.");
        float baseline = RoomAITacticalSafety.CounterExposure(snapshot, null, null);
        Assert.That(RoomAITacticalSafety.RecoverySupportNetGain(snapshot, actor, target, recovery, baseline), Is.Zero,
            "A larger increase in counterattack exposure outweighs the recovery benefit.");
        actor.jerseyNumber = 8;
        baseline = RoomAITacticalSafety.CounterExposure(snapshot, null, null);
        Assert.That(RoomAITacticalSafety.RecoverySupportNetGain(snapshot, actor, target, recovery, baseline), Is.GreaterThan(0f),
            "Useful recovery support remains possible when it does not open the same centre-back gap.");
    }

    [Test]
    public void OutletPairRecognizesTheSameDefenderCanChallengeBothReceivers()
    {
        var snapshot = Attack();
        var defender = Token("Home:9", 0); defender.pace = 4; defender.tackling = 5;
        snapshot.tokens.Add(defender);
        float shared = RoomAITacticalSafety.SharedOutletChallengeExposure(snapshot, Hex(1), Hex(2));
        float separated = RoomAITacticalSafety.SharedOutletChallengeExposure(snapshot, Hex(1), Hex(12));
        Assert.That(shared, Is.GreaterThan(separated));
        defender.tackling = 1;
        Assert.That(RoomAITacticalSafety.SharedOutletChallengeExposure(snapshot, Hex(1), Hex(2)), Is.LessThan(shared),
            "A weak tackler is not treated like a strong one merely because both outlets are reachable.");
        defender.isSentOff = true;
        Assert.That(RoomAITacticalSafety.SharedOutletChallengeExposure(snapshot, Hex(1), Hex(2)), Is.Zero);
    }

    [Test]
    public void TwoForTwoPairUsesDistinctEligibleActorsAndTheRemainingMoveBudget()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_Support", 296, false);
        var movement = replay.match.movementPhaseManager;
        movement.isMovementPhaseAttack = false;
        movement.isMovementPhase2f2 = true;
        movement.attackersMovedIn2f2 = 0;
        movement.movedTokens.Clear();
        var snapshot = replay.BoardSnapshot();
        RoomAITacticalEvaluation Outlet(string key)
        {
            var actor = replay.tokens[key];
            var target = HexGridUtils.GetReachableHexes(replay.match.hexGrid, actor.GetCurrentHex(), movement.TwoForTwoRange)
                .reachableHexes.First(h => h != null && !h.isOutOfBounds && h.isInGoal == 0
                    && !h.isAttackOccupied && !h.isDefenseOccupied);
            var at = RoomHexCoordinates.FromHex(target);
            var candidate = new RoomActionCandidate { manager = nameof(MovementPhaseManager),
                actionType = RoomActionType.StartMovement, step = RoomDecisionStep.ChooseActor, actor = actor };
            return new RoomAITacticalEvaluation { candidate = candidate, score = 80, tier = RoomAITacticalTier.SafeProgression,
                explanation = new RoomAICandidateExplanation { candidate = candidate, actorTokenKey = key,
                    targetHex = at, groundPassContinuationTargetHex = at, isSupportingPassLaneMove = true,
                    supportsConcreteContinuation = true, groundPassRetentionProbability = .8f } };
        }
        var first = Outlet("Away:7");
        var second = Outlet("Away:11");
        var choices = new List<RoomAITacticalEvaluation> { first, second };
        Policy("ApplySeparatedTwoForTwoOutlets", snapshot, choices);
        Assert.That(first.score, Is.GreaterThan(80));
        Assert.That(first.reasons, Has.Some.Contains("reassess after the first move"));
        first.score = second.score = 80;
        first.reasons.Clear(); second.reasons.Clear();
        movement.attackersMovedIn2f2 = 1;
        Policy("ApplySeparatedTwoForTwoOutlets", snapshot, choices);
        Assert.That(first.score, Is.EqualTo(80), "One remaining move cannot buy a two-player plan.");
        movement.attackersMovedIn2f2 = 0;
        movement.movedTokens.Add(second.candidate.actor);
        Policy("ApplySeparatedTwoForTwoOutlets", snapshot, choices);
        Assert.That(first.score, Is.EqualTo(80), "A player already used in 2f2 is not an available alternate.");
    }

    [TestCase("thomas 7", "UH7", "THO", "UH7")]
    [TestCase("UH7", "thomas 7", "UH7", "THO")]
    [TestCase("thomas 4", "UH4", "THO", "UH4")]
    public void NaturalAlternativePreservesTheLiteralAlphanumericIdentity(string home, string away, string h, string a)
    {
        var pair = TeamNameAbbreviator.CreatePair(home, away);
        Assert.That(pair, Is.EqualTo((h, a)));
        Assert.That(TeamNameAbbreviator.CreatePair(home, away, pair.home, pair.away), Is.EqualTo(pair));
    }

    [Test]
    public void GeneratedLegacySuffixMigratesButAnExplicitSavedOverrideRemains()
    {
        Assert.That(TeamNameAbbreviator.CreatePair("thomas 7", "UH7", "TH7", "UM3"), Is.EqualTo(("THO", "UH7")));
        Assert.That(TeamNameAbbreviator.CreatePair("thomas 7", "UH7", "THO", "ABC"), Is.EqualTo(("THO", "ABC")));
    }

    [Test]
    public void SupportDestinationDoesNotCompeteAsIfItCarriedTheBall()
    {
        var move = Move();
        var other = Move(); other.explanation.possessionPlanEffectiveRetentionProbability = 1f;
        var choices = new List<RoomAITacticalEvaluation> { move, other };
        RoomAITacticalSafety.ProtectPossession(choices);
        Assert.That(move.safetyPriority, Is.Zero, "A hypothetical receiver's retention is not the carrier's current possession loss.");
    }

    [Test]
    public void AttackingDestinationsIgnoreAStaleDefensiveAssignment()
    {
        var snapshot = Attack(); snapshot.defensivePhasePlan = new();
        var move = Move();
        move.explanation.defensivePlanRole = "HoldShape";
        move.explanation.defensivePlanDestination = Hex(3, 3);
        var decision = Decide(snapshot, move, End());
        Assert.That(decision.selectedCandidate, Is.SameAs(move.candidate));
        Assert.That(decision.selectedEvaluation.safetyPriority, Is.Zero);
    }

    [TestCase(.02f, true)]
    [TestCase(.95f, false)]
    public void UsefulSupportBeatsForfeitButNegligibleClusteringDoesNot(float value, bool ends)
    {
        Assert.That(Decide(Attack(), Move(value), End()).selectedCandidate.isForfeit, Is.EqualTo(ends));
    }

    [TestCase(121)]
    [TestCase(140)]
    public void RecordedSavvakisSelectionProducesAUsefulLegalDestinationInsteadOfX(int number)
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_LiveReview", number);
        replay.SelectActor(replay.tokens["Away:8"]);
        var decision = replay.Evaluate();
        Assert.That(decision.selectedCandidate.isForfeit, Is.False);
        Assert.That(decision.selectedCandidate.step, Is.EqualTo(RoomDecisionStep.ChooseTarget));
        Assert.That(decision.selectedCandidate.actor, Is.SameAs(replay.tokens["Away:8"]));
        Assert.That(decision.selectedEvaluation.explanation.supportsConcreteContinuation, Is.True);
        Assert.That(decision.selectedEvaluation.safetyPriority, Is.Zero);
        Assert.That(replay.match.hexGrid.highlightedHexes, Does.Contain(decision.selectedCandidate.targetHex));
    }

    [TestCase(false, 0, true)]
    [TestCase(true, 0, false)]
    [TestCase(false, 1, false)]
    public void StagedSupportSurvivesSelectionButNotALostContinuationOrSafetyVeto(bool lost, int priority, bool expected)
    {
        var root = new GameObject("Support staging"); root.SetActive(false);
        try
        {
            var manager = root.AddComponent<RoomDecisionManager>();
            var hex = root.AddComponent<HexCell>(); hex.coordinates = new Vector3Int(0, 0, 2);
            var snapshot = Attack(); var actor = Move(); actor.candidate.step = RoomDecisionStep.ChooseActor;
            typeof(RoomDecisionManager).GetMethod("RememberCommittedStagedDecision", Private)
                .Invoke(manager, new object[] { actor, snapshot });
            var target = Move(); target.candidate.targetHex = hex;
            target.explanation.supportsConcreteContinuation = !lost; target.safetyPriority = priority;
            var decision = new RoomAIProviderDecision { rankedCandidates = new() { target, End() } };
            bool retained = (bool)typeof(RoomDecisionManager).GetMethod("ApplyCommittedStagedDecision", Private)
                .Invoke(manager, new object[] { decision, snapshot });
            Assert.That(retained, Is.EqualTo(expected));
            if (expected) Assert.That(decision.selectedCandidate, Is.SameAs(target.candidate));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void CommittedAerialDefenseContestsTheLandingRatherThanChasingTheKicker(int mirror)
    {
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Away", teamInAttack = "Home", attackHasPossession = true,
            awayTeamDirection = mirror == 1 ? "RightToLeft" : "LeftToRight", currentState = "HighPass",
            ball = Hex(0, -12), committedAerialTarget = Hex(4 * mirror, 1) };
        var header = Token("Home:8", 4 * mirror, 1); header.heading = 5; header.dribbling = 1;
        var striker = Token("Home:9", 10 * mirror, 1); striker.shooting = 6;
        var marker = Token("Away:5", 7 * mirror, 0); marker.heading = 6;
        snapshot.tokens.AddRange(new[] { header, striker, marker, Token("Home:2", 0, -12) });
        float baseline = RoomAITacticalSafety.CommittedAerialDanger(snapshot);
        float landingGain = baseline - RoomAITacticalSafety.CommittedAerialDanger(snapshot, marker.tokenKey, Hex(5 * mirror, 1));
        float kickerGain = baseline - RoomAITacticalSafety.CommittedAerialDanger(snapshot, marker.tokenKey, Hex(0, -11));
        Assert.That(landingGain, Is.GreaterThan(0f));
        Assert.That(landingGain, Is.GreaterThan(kickerGain));
        RoomAITacticalEvaluation Choice(float gain)
        {
            var candidate = new RoomActionCandidate { manager = nameof(HighPassManager), actionType = RoomActionType.SetupMove,
                step = RoomDecisionStep.ChooseTarget };
            return new() { candidate = candidate, explanation = new() { candidate = candidate,
                isCommittedAerialDefense = true, committedAerialDefenseGain = gain } };
        }
        var landing = Choice(landingGain); var chasing = Choice(kickerGain);
        Assert.That(Decide(snapshot, chasing, landing).selectedCandidate, Is.SameAs(landing.candidate));
    }

    [Test]
    public void AerialControlEstimateUsesBalancedDiceAndStrongerHeadersWithoutCertainty()
    {
        Assert.That(ExpectedStatsCalculator.CalculateHeadToHeadAerialControlProbability(5, 5), Is.EqualTo(.5f).Within(.0001f));
        float strong = ExpectedStatsCalculator.CalculateHeadToHeadAerialControlProbability(6, 2);
        float weak = ExpectedStatsCalculator.CalculateHeadToHeadAerialControlProbability(2, 6);
        Assert.That(strong, Is.GreaterThan(.5f).And.LessThan(1f));
        Assert.That(strong + weak, Is.EqualTo(1f).Within(.0001f));
    }

    [Test]
    public void HopelessAerialInterventionStillUsesANonWorseningRecoveryMove()
    {
        var candidate = new RoomActionCandidate { manager = nameof(HighPassManager), actionType = RoomActionType.SetupMove,
            step = RoomDecisionStep.ChooseTarget };
        var recovery = new RoomAITacticalEvaluation { candidate = candidate, explanation = new() { candidate = candidate,
            isCommittedAerialDefense = true, committedAerialDefenseGain = 0f, committedAerialRecoverySteps = 3 } };
        var skip = End(); skip.candidate.manager = nameof(HighPassManager);
        var snapshot = Attack(); snapshot.expectedTeam = "Home"; snapshot.currentState = "HighPass";
        var decision = Decide(snapshot, skip, recovery);
        Assert.That(decision.selectedCandidate, Is.SameAs(recovery.candidate));
        Assert.That(decision.selectedEvaluation.reasons, Has.Some.Contains("recover toward goal"));
    }

    [Test]
    public void HighPassDestinationCarriesItsSelectedActorIntoAnalysis()
    {
        var root = new GameObject("High pass actor identity"); root.SetActive(false);
        try
        {
            var manager = root.AddComponent<HighPassManager>();
            manager.hexGrid = root.AddComponent<HexGrid>();
            var hex = root.AddComponent<HexCell>();
            manager.selectedToken = root.AddComponent<PlayerToken>();
            manager.hexGrid.highlightedHexes.Add(hex);
            var context = new RoomDecisionContext();
            typeof(HighPassManager).GetMethod("AddHighlightedHexes", Private).Invoke(manager, new object[] { context });
            Assert.That(context.actions.Single().actor, Is.SameAs(manager.selectedToken));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [TestCase(75)]
    [TestCase(76)]
    public void RecordedHighPassDefenseUsesLandingFactsAtBothInputStages(int number)
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_HighPass", number, restoreMovementOwnership: false);
        var match = replay.match;
        match.currentState = MatchManager.GameState.HighPass;
        var hp = match.gameObject.AddComponent<HighPassManager>(); match.highPassManager = hp;
        hp.hexGrid = match.hexGrid; hp.ball = match.ball; hp.movementPhaseManager = match.movementPhaseManager;
        hp.isActivated = true; hp.isWaitingForDefenderSelection = true;
        hp.currentTargetHex = replay.Hex(Hex(4, 1)); hp.lockedAttacker = replay.tokens["Home:8"];
        if (number == 76)
        {
            hp.selectedToken = replay.tokens["Away:7"]; hp.isWaitingForDefenderMove = true;
            match.hexGrid.highlightedHexes.AddRange(HexGridUtils.GetReachableHexes(match.hexGrid,
                hp.selectedToken.GetCurrentHex(), HighPassManager.DefenderMoveRange).reachableHexes
                .Where(hex => !hex.isOutOfBounds && !hex.isAttackOccupied && !hex.isDefenseOccupied));
        }
        var snapshot = replay.BoardSnapshot(); snapshot.committedAerialTarget = Hex(4, 1);
        var context = new RoomDecisionContext { manager = nameof(HighPassManager), expectedTeam = "Away" };
        hp.PopulateRoomDecisionContext(context);
        snapshot.legalActions.AddRange(context.actions.Where(candidate => candidate.isExecutableNow));
        typeof(RoomAIDecisionSnapshotBuilder).GetMethod("BuildCandidateExplanations", StaticPrivate)
            .Invoke(null, new object[] { snapshot });
        var decision = CodexTacticianProvider.Instance.Decide(snapshot, snapshot.legalActions, null,
            new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard);
        Assert.That(snapshot.candidateExplanations.Where(facts => facts.candidate.step == RoomDecisionStep.ChooseTarget
            || facts.candidate.step == RoomDecisionStep.ChooseActor && facts.actorToken?.isGoalKeeper != true)
            .All(facts => facts.isCommittedAerialDefense), Is.True);
        Assert.That(decision.selectedCandidate, Is.Not.Null);
        if (!decision.selectedCandidate.isForfeit)
        {
            Assert.That(decision.selectedEvaluation.explanation.isCommittedAerialDefense, Is.True);
            Assert.That(decision.selectedEvaluation.explanation.committedAerialDefenseGain >= -.0001f
                && (decision.selectedEvaluation.explanation.committedAerialDefenseGain > .001f
                    || decision.selectedEvaluation.explanation.committedAerialRecoverySteps > 0), Is.True);
        }
        if (number == 75) Assert.That(decision.selectedCandidate.isForfeit, Is.False,
            "The recorded pre-selection position must use its free move to reduce landing/header/relay danger.");
    }

    [TestCase("LeftToRight")]
    [TestCase("RightToLeft")]
    public void DefendingKickoffPreservesEstablishedFlanksAndCompactMidfield(string direction)
    {
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Away", teamInAttack = "Home", awayTeamDirection = direction };
        var seven = Token("Away:7", 0, 9); seven.jerseyNumber = 7;
        var eleven = Token("Away:11", 0, -8); eleven.jerseyNumber = 11;
        var six = Token("Away:6", 4, -4); six.jerseyNumber = 6; six.pace = 3;
        var eight = Token("Away:8", 4, 4); eight.jerseyNumber = 8;
        snapshot.tokens.AddRange(new[] { seven, eleven, six, eight });
        RoomHexCoordinates Target(RoomAITokenSnapshot token)
        {
            object[] args = { snapshot, token, null };
            Assert.That(typeof(CodexTacticianProvider).GetMethod("TryGetHumanLedKickoffTarget", StaticPrivate).Invoke(null, args), Is.True);
            return (RoomHexCoordinates)args[2];
        }
        Assert.That(Target(seven).z, Is.GreaterThan(0));
        Assert.That(Target(eleven).z, Is.LessThan(0));
        Assert.That(Target(eleven).x, Is.Zero);
        Assert.That(RoomAITacticalSafety.Distance(Target(six), Target(eight)), Is.LessThanOrEqualTo(4));
        six.pace = 6;
        Assert.That(RoomAITacticalSafety.Distance(Target(six), Target(eight)), Is.EqualTo(4),
            "Spacing adapts to reach; it is not a player-specific coordinate.");
    }

    [Test]
    public void CloseOutletsArePenalizedComparedWithIndependentFlanks()
    {
        var snapshot = Attack(); snapshot.ball = Hex(18);
        snapshot.tokens.AddRange(new[] { Token("Away:4", 12, -3), Token("Away:5", 12, -2) });
        Assert.That(RoomAITacticalSafety.OutletCrowdingCost(snapshot, "Away:5", Hex(12, 3)),
            Is.LessThan(RoomAITacticalSafety.OutletCrowdingCost(snapshot, "Away:5", Hex(12, -2))));
    }

    private static RoomAIDecisionSnapshot EarlyDefendingKickoff(int mirror = 1, string team = "Away") => new()
    {
        expectedTeam = team, teamInAttack = team == "Away" ? "Home" : "Away", currentState = "KickOffSetup",
        awayTeamDirection = mirror == 1 ? "RightToLeft" : "LeftToRight",
        homeTeamDirection = mirror == 1 ? "LeftToRight" : "RightToLeft",
        clock = new() { currentHalf = 1 }, ball = Hex(0)
    };

    private static RoomAITacticalEvaluation KickoffPlacement(RoomAITokenSnapshot token, int x, int z,
        int pressure = 0, int support = 0)
    {
        var candidate = new RoomActionCandidate { manager = nameof(KickoffManager), actionType = RoomActionType.SetupMove,
            step = RoomDecisionStep.ChooseTarget, setupTeam = token.teamSide, isExecutableNow = true };
        return new() { candidate = candidate, explanation = new() { candidate = candidate, actorToken = token,
            actorTokenKey = token.tokenKey, actorHex = token.currentHex, targetHex = Hex(x, z),
            forwardProgressDelta = System.Math.Abs(token.currentHex.x) - System.Math.Abs(x),
            opponentsWithinTackleReach = pressure, friendlySupportWithinRange = support } };
    }

    private static RoomAITacticalEvaluation ConfirmKickoff()
    {
        var candidate = new RoomActionCandidate { manager = nameof(KickoffManager), actionType = RoomActionType.Confirm,
            step = RoomDecisionStep.Confirm, key = "Space", isExecutableNow = true };
        return new() { candidate = candidate, explanation = new() { candidate = candidate } };
    }

    [TestCase(7, 1)] [TestCase(7, -1)] [TestCase(11, 1)] [TestCase(11, -1)]
    [TestCase(9, 1)] [TestCase(9, -1)] [TestCase(10, 1)] [TestCase(10, -1)]
    public void EarlyDefendingFrontFourReachHalfwayDespiteReceiverProtectionBonuses(int jersey, int mirror)
    {
        int z = jersey switch { 7 => 8, 11 => -8, 9 => 5, _ => -5 };
        var snapshot = EarlyDefendingKickoff(mirror);
        var token = Token($"Away:{jersey}", 4 * mirror, z); token.jerseyNumber = jersey; token.dribbling = 1;
        snapshot.tokens.Add(token);
        var behind = KickoffPlacement(token, 2 * mirror, z, support: 3);
        var line = KickoffPlacement(token, 0, z, pressure: 3);
        var decision = Decide(snapshot, behind, line, ConfirmKickoff());
        Assert.That(decision.selectedCandidate, Is.SameAs(line.candidate));
        Assert.That(decision.selectedEvaluation.reasons, Has.Some.Contains("front four prioritize halfway"));
        Assert.That(decision.selectedEvaluation.safetyPriority, Is.Zero);
    }

    [TestCase("Home", 1)] [TestCase("Home", -1)] [TestCase("Away", 1)] [TestCase("Away", -1)]
    public void EarlyGoalRestartKeepsTheDefendingFrontLinePriorityForEitherTeam(string team, int mirror)
    {
        var snapshot = EarlyDefendingKickoff(mirror, team);
        snapshot.currentState = "PostGoalKickOffSetup";
        snapshot.clock.currentHalfRegulationSeconds = 299;
        snapshot.homeScore = 1;
        var token = Token($"{team}:7", (team == "Home" ? -4 : 4) * mirror, 8); token.jerseyNumber = 7;
        snapshot.tokens.Add(token);
        var line = KickoffPlacement(token, 0, 8, pressure: 3);
        Assert.That(Decide(snapshot, KickoffPlacement(token, token.currentHex.x / 2, 8, support: 3),
            line, ConfirmKickoff()).selectedCandidate, Is.SameAs(line.candidate));
    }

    [Test]
    public void EarlyFrontFourCompleteSetupWithOnePlacementEachBeforeConfirmation()
    {
        var snapshot = EarlyDefendingKickoff();
        foreach (var (jersey, x, z) in new[] { (7, 4, 8), (11, 4, -8), (9, 2, 5), (10, 2, -5) })
        {
            var token = Token($"Away:{jersey}", x, z); token.jerseyNumber = jersey;
            snapshot.tokens.Add(token);
        }
        var moved = new HashSet<string>();
        for (int step = 0; step <= 4; step++)
        {
            snapshot.legalActions.Clear(); snapshot.candidateExplanations.Clear();
            var choices = new List<RoomAITacticalEvaluation> { ConfirmKickoff() };
            foreach (var token in snapshot.tokens.Where(token => !moved.Contains(token.tokenKey)))
            {
                choices.Add(KickoffPlacement(token, token.currentHex.x / 2, token.currentHex.z, support: 3));
                choices.Add(KickoffPlacement(token, 0, token.currentHex.z, pressure: 3));
            }
            var decision = Decide(snapshot, choices.ToArray());
            if (decision.selectedCandidate.actionType == RoomActionType.Confirm)
            {
                Assert.That(moved.Count, Is.EqualTo(4), "Confirmation must not strand an unarranged forward behind halfway.");
                Assert.That(snapshot.tokens.All(token => token.currentHex.x == 0), Is.True);
                return;
            }
            var facts = decision.selectedEvaluation.explanation;
            Assert.That(moved.Add(facts.actorToken.tokenKey), Is.True, "The setup may place each actor only once.");
            Assert.That(facts.targetHex.x, Is.Zero);
            facts.actorToken.currentHex = facts.targetHex;
        }
        Assert.Fail("Four placements must be followed by finite setup confirmation.");
    }

    [Test]
    public void BlockedPreferredHalfwayHexUsesAnotherLegalHalfwayPlacement()
    {
        var snapshot = EarlyDefendingKickoff();
        var token = Token("Away:7", 4, 8); token.jerseyNumber = 7; snapshot.tokens.Add(token);
        var alternate = KickoffPlacement(token, 0, 9, pressure: 3);
        // The manager did not supply occupied (0,8). Both alternatives are one
        // hex from that role target, so halfway must break the tie.
        var decision = Decide(snapshot, KickoffPlacement(token, 1, 8, support: 3), alternate, ConfirmKickoff());
        Assert.That(decision.selectedCandidate, Is.SameAs(alternate.candidate));
    }

    [Test]
    public void NoLegalHalfwayPlacementKeepsTheClosestLegalPressOnTheEstablishedFlank()
    {
        var snapshot = EarlyDefendingKickoff();
        var token = Token("Away:11", 4, -8); token.jerseyNumber = 11; snapshot.tokens.Add(token);
        var closest = KickoffPlacement(token, 1, -8, pressure: 3);
        var otherFlank = KickoffPlacement(token, 0, 8);
        Assert.That(Decide(snapshot, KickoffPlacement(token, 2, -8, support: 3), otherFlank,
            closest, ConfirmKickoff()).selectedCandidate, Is.SameAs(closest.candidate));
    }

    [TestCase(1, 300f, "KickOffSetup")]
    [TestCase(2, 0f, "KickOffSetup")]
    [TestCase(1, 0f, "MovementPhase")]
    public void FrontLineOverrideDoesNotExtendBeyondEarlyKickoffSetup(int half, float seconds, string state)
    {
        var snapshot = EarlyDefendingKickoff(); snapshot.currentState = state;
        snapshot.clock.currentHalf = half; snapshot.clock.currentHalfRegulationSeconds = seconds;
        var token = Token("Away:7", 4, 8); token.jerseyNumber = 7; snapshot.tokens.Add(token);
        var decision = Decide(snapshot, KickoffPlacement(token, 2, 8, support: 3), KickoffPlacement(token, 0, 8, pressure: 3));
        Assert.That(decision.rankedCandidates.All(e => e.kickoffFrontLinePriority == 0), Is.True);
        Assert.That(decision.selectedEvaluation.reasons, Has.None.Contains("front four prioritize halfway"));
    }

    [TestCase(AIManager.CodexTacticianDifficulty.Easy)]
    [TestCase(AIManager.CodexTacticianDifficulty.Medium)]
    public void LowerDifficultyRandomnessStillChoosesWithinTheEarlyFrontLinePriority(AIManager.CodexTacticianDifficulty difficulty)
    {
        var snapshot = EarlyDefendingKickoff();
        var token = Token("Away:7", 4, 8); token.jerseyNumber = 7; snapshot.tokens.Add(token);
        var line = KickoffPlacement(token, 0, 8, pressure: 3);
        var behind = KickoffPlacement(token, 2, 8, support: 3);
        snapshot.legalActions.AddRange(new[] { behind.candidate, line.candidate });
        snapshot.candidateExplanations.AddRange(new[] { behind.explanation, line.explanation });
        var random = Random.state;
        try
        {
            var decision = CodexTacticianProvider.Instance.Decide(snapshot, snapshot.legalActions, null,
                new RoomAITacticalWeights(), difficulty);
            Assert.That(decision.selectedCandidate, Is.SameAs(line.candidate));
        }
        finally { Random.state = random; }
    }

    [Test]
    public void EarlyDefendingMidfieldAndAttackingSetupDoNotAcquireTheFrontLineOverride()
    {
        foreach (int jersey in new[] { 6, 8 })
        {
            var snapshot = EarlyDefendingKickoff();
            var token = Token($"Away:{jersey}", 4, 4); token.jerseyNumber = jersey; snapshot.tokens.Add(token);
            var decision = Decide(snapshot, KickoffPlacement(token, 4, 2), KickoffPlacement(token, 0, 4));
            Assert.That(decision.rankedCandidates.Select(e => e.kickoffFrontLinePriority).Distinct().Single(), Is.Zero);
        }
        var attacking = EarlyDefendingKickoff(); attacking.teamInAttack = "Away";
        var forward = Token("Away:7", 4, 8); forward.jerseyNumber = 7; attacking.tokens.Add(forward);
        var attack = Decide(attacking, KickoffPlacement(forward, 0, 8), KickoffPlacement(forward, 2, 8));
        Assert.That(attack.rankedCandidates.All(e => e.kickoffFrontLinePriority == 0), Is.True);
    }

    [Test]
    public void OrdinaryHighPassKeeperMovementCannotBypassContinuousGoalCoverage()
    {
        var snapshot = Attack(); snapshot.expectedTeam = "Home"; snapshot.homeTeamDirection = "RightToLeft";
        var wandering = Move(); var recovery = Move();
        foreach (var e in new[] { wandering, recovery })
        {
            e.explanation.actorToken.isGoalKeeper = true;
            e.candidate.actionType = RoomActionType.SetupMove; e.candidate.manager = nameof(HighPassManager);
        }
        wandering.explanation.actorHex = Hex(16); wandering.explanation.targetHex = Hex(12);
        recovery.explanation.actorHex = Hex(12); recovery.explanation.targetHex = Hex(17);
        RoomAITacticalSafety.ConstrainGoalkeeperChoices(snapshot, new() { wandering, recovery });
        Assert.That(wandering.safetyPriority, Is.GreaterThanOrEqualTo(2));
        Assert.That(recovery.safetyPriority, Is.Zero);
    }

    [TestCase(.90f, 0f, true)]
    [TestCase(.60f, 0f, false)]
    [TestCase(.90f, .3f, false)]
    public void AdvancingReceptionWinsOnlyWithoutMaterialSafetyOrFinishingLoss(float retention, float backXg, bool forwardWins)
    {
        RoomAITacticalEvaluation Pass(int progress, float safe, float xg)
        {
            var candidate = new RoomActionCandidate { manager = nameof(GroundBallManager), actionType = RoomActionType.GroundPass,
                step = RoomDecisionStep.ChooseTarget };
            return new() { candidate = candidate, explanation = new() { candidate = candidate, isGroundPassContinuation = true,
                groundPassCollectorRemainingPace = 3, groundPassRetentionProbability = safe,
                groundPassRouteExpectedGoals = xg, forwardProgressDelta = progress } };
        }
        var back = Pass(-3, .94f, backXg); var forward = Pass(4, retention, 0f);
        var choices = new List<RoomAITacticalEvaluation> { back, forward };
        Policy("ApplyAdvancingReceptionPriority", choices);
        Assert.That(back.safetyPriority > 0, Is.EqualTo(forwardWins));
    }

    [Test]
    public void DribbleRouteEnvironmentIgnoresOnlyExpectedCarrierSteps()
    {
        var snapshot = Attack(); snapshot.tokens.AddRange(new[] { Token("Away:5", 11, -3), Token("Home:9", 4, 1) });
        string Key() => (string)typeof(RoomDecisionManager).GetMethod("BuildDribbleEnvironmentKey", StaticPrivate)
            .Invoke(null, new object[] { snapshot, "Away:5" });
        string before = Key();
        snapshot.tokens[0].currentHex = Hex(10, -3); snapshot.ball = Hex(10, -3);
        Assert.That(Key(), Is.EqualTo(before));
        snapshot.tokens[1].currentHex = Hex(5, 1);
        Assert.That(Key(), Is.Not.EqualTo(before), "A real opponent/GK response must invalidate the old route.");
    }

    [TestCase(.85f, false)]
    [TestCase(.55f, true)]
    public void AdvancingReceptionCanJustifySmallButNotLargePossessionRisk(float retention, bool rejected)
    {
        var forward = Move(); var back = Move();
        foreach (var e in new[] { forward, back })
        {
            e.candidate.actionType = RoomActionType.GroundPass;
            e.explanation.isAttackingOffBallMove = false;
            e.explanation.isGroundPassContinuation = true;
            e.explanation.groundPassCollectorRemainingPace = 3;
        }
        forward.explanation.forwardProgressDelta = 4;
        forward.explanation.possessionPlanEffectiveRetentionProbability = retention;
        back.explanation.forwardProgressDelta = -3;
        back.explanation.possessionPlanEffectiveRetentionProbability = .95f;
        RoomAITacticalSafety.ProtectPossession(new[] { forward, back });
        Assert.That(forward.safetyPriority > 0, Is.EqualTo(rejected));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CommittedDribbleDoesNotReverseForRawScoreButReplansAfterAnOpponentMoves(bool opponentMoved)
    {
        var root = new GameObject("Dribble commitment"); root.SetActive(false);
        try
        {
            var match = root.AddComponent<MatchManager>(); MatchManager.Instance = match;
            var manager = root.AddComponent<RoomDecisionManager>();
            var movement = root.AddComponent<MovementPhaseManager>(); match.movementPhaseManager = movement;
            var dribbler = root.AddComponent<PlayerToken>(); dribbler.isHomeTeam = false; dribbler.jerseyNumber = 5;
            movement.selectedToken = dribbler; movement.isDribblerRunning = true;
            HexCell Cell(int x)
            {
                var child = new GameObject("Route hex"); child.transform.SetParent(root.transform);
                var cell = child.AddComponent<HexCell>(); cell.coordinates = new Vector3Int(x, 0, -3); return cell;
            }
            var here = Cell(10); var next = Cell(9); var back = Cell(11);
            typeof(PlayerToken).GetProperty("currentHex").GetSetMethod(true).Invoke(dribbler, new object[] { here });
            var snapshot = Attack(); snapshot.tokens.AddRange(new[] { Token("Away:5", 11, -3), Token("Home:9", 4, 1) });
            var original = Move(); original.explanation.isFullPaceDribbleRoute = true;
            original.explanation.fullPaceDribbleDestination = Hex(9, -3);
            original.explanation.fullPaceDribblePath.AddRange(new[] { Hex(10, -3), Hex(9, -3) });
            typeof(RoomDecisionManager).GetMethod("RememberDribbleRoute", Private)
                .Invoke(manager, new object[] { original, snapshot, false });
            snapshot.tokens[0].currentHex = Hex(10, -3); snapshot.ball = Hex(10, -3);
            if (opponentMoved) snapshot.tokens[1].currentHex = Hex(5, 1);
            var continuation = Move(); continuation.candidate.targetHex = next;
            continuation.explanation.isFullPaceDribbleRoute = true;
            continuation.explanation.fullPaceDribbleExpectedGoals = 0f;
            var reverse = Move(); reverse.candidate.targetHex = back; reverse.score = 1000;
            reverse.explanation.isFullPaceDribbleRoute = true;
            reverse.explanation.fullPaceDribbleExpectedGoals = 0f;
            var decision = new RoomAIProviderDecision { rankedCandidates = new() { reverse, continuation },
                selectedEvaluation = reverse, selectedCandidate = reverse.candidate };
            bool retained = (bool)typeof(RoomDecisionManager).GetMethod("ApplyPendingDribbleRoute", Private)
                .Invoke(manager, new object[] { decision, snapshot });
            Assert.That(retained, Is.EqualTo(!opponentMoved));
            if (retained) Assert.That(decision.selectedCandidate, Is.SameAs(continuation.candidate));
        }
        finally { MatchManager.Instance = null; Object.DestroyImmediate(root); }
    }
}
