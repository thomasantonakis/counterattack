using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using NUnit.Framework;

public sealed class ActionSequenceEditorTests
{
    [Test]
    public void FlightPickupMovementChallengeAndShotAreMultipliedExactlyOnce()
    {
        var result = RoomAIActionSequence.Compose(.8f, .75f, .6f, .1f, .4f, .5f, .1f).Evaluate();
        Assert.That(result.goalProbability, Is.EqualTo(.8f * .75f * .6f * .5f * .4f).Within(.00001f));
        Assert.That(result.attackingRestartProbability, Is.EqualTo(.8f * .75f * (.1f + .6f * .1f)).Within(.00001f));
        Assert.That(result.goalProbability + result.attackRetainsPossessionProbability + result.defenseWinsPossessionProbability
            + result.attackingRestartProbability + result.defendingRestartProbability + result.unresolvedProbability,
            Is.EqualTo(1f).Within(.00001f));
        Assert.That(result.unresolvedProbability, Is.GreaterThan(0f), "Misses/rebounds and challenge recovery are not guaranteed possession.");
    }

    [TestCase(5, 3, false, 2)]
    [TestCase(5, 8, false, 0)]
    [TestCase(5, 1, true, 1)]
    [TestCase(5, 2, true, 0)]
    public void CollectionCannotBorrowASecondFullPace(int pace, int cost, bool twoForTwo, int expected)
        => Assert.That(RoomAIActionSequence.PaceAfterCollection(pace, cost, twoForTwo), Is.EqualTo(expected));

    [TestCase(false, false, "snapshot")]
    [TestCase(true, true, "snapshot")]
    [TestCase(true, false, "fullPower")]
    public void ImmediateReceptionAndMovementHaveDifferentShotAvailability(bool moved, bool box, string expected)
        => Assert.That(RoomAIActionSequence.ShotType(moved, box), Is.EqualTo(expected));

    [Test]
    public void OffsideIsReassessedAtAerialLandingNotOriginalDistributor()
    {
        var board = KickoffOpeningEditorTests.Board();
        board.tokens.Add(new RoomAITokenSnapshot { tokenKey = "Away:1", teamSide = "Away", isPlaying = true,
            currentHex = new RoomHexCoordinates { x = 17, z = 0 } });
        board.tokens.Add(new RoomAITokenSnapshot { tokenKey = "Away:4", teamSide = "Away", isPlaying = true,
            currentHex = new RoomHexCoordinates { x = 7, z = 0 } });
        var runner = new RoomHexCoordinates { x = 9, z = 0 };
        Assert.That(RoomAIActionSequence.Offside(board, runner, board.ball), Is.True);
        Assert.That(RoomAIActionSequence.Offside(board, runner, new RoomHexCoordinates { x = 10, z = 0 }), Is.False);
        board.homeTeamDirection = "RightToLeft";
        foreach (var token in board.tokens) token.currentHex.x *= -1;
        runner.x *= -1;
        Assert.That(RoomAIActionSequence.Offside(board, runner, new RoomHexCoordinates { x = -10, z = 0 }), Is.False);
    }

    [Test]
    public void HeaderContestUsesSharedAttackerRollAndPreservesTies()
    {
        var one = RoomAIActionSequence.HeaderContest(4, 4);
        Assert.That(one.win, Is.EqualTo(one.loss).Within(.00001f));
        Assert.That(one.tie, Is.GreaterThan(0f));
        var multiple = RoomAIActionSequence.HeaderContest(4, 4, 4);
        Assert.That(multiple.win, Is.LessThan(one.win));
        Assert.That(multiple.win, Is.Not.EqualTo(one.win * one.win).Within(.00001f),
            "Pairwise multiplication incorrectly rolls the attacker again against each defender.");
        Assert.That(multiple.win + multiple.loss + multiple.tie, Is.EqualTo(1f).Within(.00001f));
        Assert.That(RoomAIActionSequence.HeaderContest(4).win, Is.EqualTo(1f).Within(.00001f));
    }

    [Test]
    public void RecordedKickoffOpeningExpandsCollectionAndMovementWithoutConsecutivePasses()
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_10-30", 162, false);
        var decision = replay.EvaluateKickoffPass();
        var previews = decision.rankedCandidates.Select(e => e.explanation?.actionSequence).Where(s => s != null).ToList();
        Assert.That(previews, Is.Not.Empty);
        foreach (var sequence in previews)
        {
            Assert.That(sequence.searchBounded, Is.True);
            Assert.That(sequence.outcomes.goalProbability, Is.InRange(0f, 1f));
            Assert.That(sequence.steps.Skip(1).Any(s => s.action == "GroundPass"), Is.False);
            foreach (var step in sequence.steps.Where(s => s.action == "Attacking movement"))
                Assert.That(step.remainingPace, Is.GreaterThanOrEqualTo(0));
        }
    }

    [Test]
    public void RecordedAerialBoardExpandsHeaderAndReleaseWithoutMutatingPositions()
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_11-30", 194, false);
        var before = replay.tokens.ToDictionary(p => p.Key, p => p.Value.GetCurrentHex());
        var board = replay.BoardSnapshot();
        board.expectedTeam = board.teamInAttack;
        var passer = replay.match.ball.GetCurrentHex().GetOccupyingToken() ?? replay.match.LastTokenToTouchTheBallOnPurpose;
        var header = replay.tokens.Values.Where(t => t.isPlaying && !t.IsGoalKeeper && t != passer
            && t.isHomeTeam == passer.isHomeTeam && t.GetCurrentHex() != null
            && !RoomAIActionSequence.Offside(board, RoomHexCoordinates.FromHex(t.GetCurrentHex()), board.ball))
            .OrderByDescending(t => t.heading).First();
        var action = new RoomActionCandidate { actionType = RoomActionType.HighPass, step = RoomDecisionStep.ChooseTarget,
            targetHex = header.GetCurrentHex(), isExecutableNow = true };
        var preview = RoomAIActionSequence.Evaluate(board, new RoomAICandidateExplanation { candidate = action });
        Assert.That(preview, Is.Not.Null);
        Assert.That(preview.steps.Any(s => s.action.StartsWith("Win header", StringComparison.Ordinal)), Is.True);
        Assert.That(preview.steps.First(s => s.action.StartsWith("Win header", StringComparison.Ordinal)).actorTokenKey,
            Is.Not.EqualTo(MatchManager.GetStableTokenKey(passer)), "An initial aerial cannot be collected by its own distributor.");
        Assert.That(preview.steps.Any(s => s.action == "Headed pass"), Is.True);
        Assert.That(preview.steps.Any(s => s.action == "Collect"), Is.True);
        Assert.That(preview.alternativesEvaluated, Is.GreaterThan(1));
        Assert.That(preview.scatterOutcomes.Count, Is.EqualTo(36));
        Assert.That(preview.scatterOutcomes.Sum(branch => branch.probability), Is.EqualTo(1f - preview.accuracyProbability).Within(.00001f));
        Assert.That(preview.outcomes.isComplete, Is.False, "GK rushes, header ties, rebounds and beyond-horizon pickups remain unresolved.");
        foreach (var token in replay.tokens) Assert.That(token.Value.GetCurrentHex(), Is.SameAs(before[token.Key]));
    }

    [Test]
    public void RecordedSpaceCollectionSpendsPaceBeforeFollowingCarry()
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_11-30", 194, false);
        var board = replay.BoardSnapshot();
        board.expectedTeam = board.teamInAttack;
        var receiver = replay.tokens["Away:10"];
        var landing = receiver.GetCurrentHex().GetNeighbors(replay.match.hexGrid).First(h => h != null
            && !h.isOutOfBounds && !h.isAttackOccupied && !h.isDefenseOccupied);
        var action = new RoomActionCandidate { actionType = RoomActionType.GroundPass,
            step = RoomDecisionStep.ChooseTarget, targetHex = landing };
        var sequence = RoomAIActionSequence.Evaluate(board, new RoomAICandidateExplanation { candidate = action });
        Assert.That(sequence, Is.Not.Null);
        var collection = sequence.steps.First(s => s.action == "Collect");
        var actualCollector = replay.tokens[collection.actorTokenKey];
        Assert.That(collection.movementCost, Is.GreaterThan(0));
        Assert.That(collection.remainingPace, Is.EqualTo(actualCollector.pace - collection.movementCost));
        var carry = sequence.steps.First(s => s.action == "Attacking movement");
        Assert.That(carry.movementCost, Is.LessThanOrEqualTo(collection.remainingPace));
        Assert.That(carry.remainingPace, Is.EqualTo(collection.remainingPace - carry.movementCost));
    }

    [Test]
    public void FutureShotForecastPreservesSnapshotPenalty()
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_11-30", 194, false);
        var shooter = replay.tokens["Away:9"];
        var keeper = replay.tokens["Home:1"];
        var origin = replay.match.hexGrid.cells.Cast<HexCell>().First(h => h != null && h.CanShootFrom
            && h.coordinates.x < 0 && h.ShootingPaths != null && h.ShootingPaths.Keys.Any(goal => goal.isInGoal == -1));
        var shots = replay.match.shotManager;
        Assert.That(shots.TryCalculateBestFutureExpectedGoalsPreview(shooter, origin, "snapshot", keeper,
            out _, out int snapshotPenalty, out _, out _), Is.True);
        Assert.That(shots.TryCalculateBestFutureExpectedGoalsPreview(shooter, origin, "fullPower", keeper,
            out _, out int fullPenalty, out _, out _), Is.True);
        Assert.That(snapshotPenalty, Is.EqualTo(fullPenalty + 1));
    }
}

public sealed class ReboundRecoveryEditorTests
{
    [Test]
    public void FollowupCannotBorrowAnUnknownOrFinalExtraAction()
    {
        Assert.That(RoomAISecondaryOutcomes.CanForecastFollowupSnapshot(null), Is.False);
        var clock = new RoomClockSnapshot { isHalfExpired = true, extraActionsDetermined = true, extraActionsRemaining = 1 };
        Assert.That(RoomAISecondaryOutcomes.CanForecastFollowupSnapshot(clock), Is.False);
        clock.extraActionsRemaining = 2;
        Assert.That(RoomAISecondaryOutcomes.CanForecastFollowupSnapshot(clock), Is.True);
        clock.isWaitingForExtraActionsRoll = true;
        Assert.That(RoomAISecondaryOutcomes.CanForecastFollowupSnapshot(clock), Is.False);
    }

    [Test]
    public void OrderedShotDistributionMatchesFirstGoalAndPreservesTotalMass()
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_08-10", 83, false);
        var shooter = replay.tokens["Home:10"]; var keeper = replay.tokens["Away:1"]; var blocker = replay.tokens["Away:3"];
        foreach (bool blockFirst in new[] { true, false })
        {
            var block = new ExpectedStatsCalculator.ShotResolutionInteraction { token = blocker, tokenHex = blocker.GetCurrentHex(), requiredRoll = 5 };
            var save = new ExpectedStatsCalculator.ShotResolutionInteraction { token = keeper, tokenHex = keeper.GetCurrentHex(),
                contactHex = keeper.GetCurrentHex(), goalkeeper = true, saveModifier = -1 };
            var distribution = ExpectedStatsCalculator.CalculateShotResolutionProbabilities(shooter, 1,
                blockFirst ? new[] { block, save } : new[] { save, block });
            float expected = ExpectedStatsCalculator.CalculateShotGoalProbability(shooter, 1,
                new[] { new ExpectedStatsCalculator.ShotBlockerExpectation(blocker, 5) }, keeper, -1);
            Assert.That(distribution.goal, Is.EqualTo(expected).Within(.00001f));
            Assert.That(distribution.goal + distribution.offTarget + distribution.held + distribution.rebounds.Sum(r => r.probability),
                Is.EqualTo(1f).Within(.00001f));
        }
    }

    [Test]
    public void BetterHandlingConvertsSpillsToHoldsNotExtraGoals()
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_08-10", 83, false);
        var shooter = replay.tokens["Home:10"]; var keeper = replay.tokens["Away:1"];
        var interaction = new ExpectedStatsCalculator.ShotResolutionInteraction { token = keeper,
            tokenHex = keeper.GetCurrentHex(), contactHex = keeper.GetCurrentHex(), goalkeeper = true };
        keeper.handling = 1;
        var weak = ExpectedStatsCalculator.CalculateShotResolutionProbabilities(shooter, 0, new[] { interaction });
        keeper.handling = 6;
        var strong = ExpectedStatsCalculator.CalculateShotResolutionProbabilities(shooter, 0, new[] { interaction });
        Assert.That(strong.goal, Is.EqualTo(weak.goal));
        Assert.That(strong.held, Is.GreaterThan(weak.held));
        Assert.That(strong.rebounds.Where(r => r.source == LooseBallSourceType.GoalkeeperHandlingSpill).Sum(r => r.probability),
            Is.LessThan(weak.rebounds.Where(r => r.source == LooseBallSourceType.GoalkeeperHandlingSpill).Sum(r => r.probability)));
        Assert.That(RoomAISecondaryOutcomes.HandlingSpillDirections(1), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(RoomAISecondaryOutcomes.HandlingSpillDirections(-1), Is.EqualTo(new[] { 4, 5 }));
    }

    [Test]
    public void ReplacingUnknownMassNeverDoubleCountsTheFirstGoal()
    {
        var result = new RoomAIPossessionChain().AddTerminal("Goal", .4f, RoomAIPossessionOutcomeKind.Goal).Evaluate();
        var tail = new RoomAIPossessionChain().AddTerminal("Held", .5f, RoomAIPossessionOutcomeKind.DefenseWinsPossession)
            .AddTerminal("Recover", .3f, RoomAIPossessionOutcomeKind.AttackRetainsPossession).Evaluate();
        Assert.That(RoomAISecondaryOutcomes.ReplaceUnknown(result, .6f, tail), Is.True);
        Assert.That(result.goalProbability, Is.EqualTo(.4f));
        Assert.That(result.defenseWinsPossessionProbability, Is.EqualTo(.3f).Within(.00001f));
        Assert.That(result.unresolvedProbability, Is.EqualTo(.12f).Within(.00001f));
        Assert.That(RoomAISecondaryOutcomes.ReplaceUnknown(result, .6f, tail), Is.False);
    }

    [Test]
    public void TacklerChoosesLeastExposedLegalExitAndLoserCannotStealBack()
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_08-10", 83, false);
        var board = replay.tokens.Values.Where(t => t.isPlaying && t.GetCurrentHex() != null).ToDictionary(t => t, t => t.GetCurrentHex());
        var loser = replay.tokens["Home:10"]; var winner = replay.tokens["Away:2"];
        board[winner] = replay.Hex(new RoomHexCoordinates { x = 9, z = 0 });
        var result = RoomAISecondaryOutcomes.PreviewPostTackleRecovery(replay.match, loser, winner, board);
        Assert.That(result.tackleExits, Is.Not.Empty);
        Assert.That(result.postTackleRecoveryProbability, Is.EqualTo(result.tackleExits.Min(e => e.recoveryProbability)));
        Assert.That(result.tackleExits.SelectMany(e => e.eligibleStealers), Does.Not.Contain("Home:10"));
        Assert.That(result.tackleExits.Any(e => RoomAITacticalSafety.Distance(e.hex, RoomHexCoordinates.FromHex(board[winner])) == 0), Is.True,
            "Staying put is a legal successful-tackle choice.");
        foreach (var exit in result.tackleExits)
            if (RoomAITacticalSafety.Distance(exit.hex, RoomHexCoordinates.FromHex(board[winner])) != 0)
                Assert.That(RoomAITacticalSafety.Distance(exit.hex, RoomHexCoordinates.FromHex(board[loser])), Is.EqualTo(1));
    }

    [Test]
    public void RecordedShotExpandsSavedAndReboundMassWithoutMovingLiveTokens()
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_08-10", 83, false);
        var snapshot = replay.BoardSnapshot(); snapshot.expectedTeam = snapshot.teamInAttack; snapshot.currentState = "EndOfMovementPhase";
        snapshot.clock = new RoomClockSnapshot { currentHalf = 1, isClockRunning = true };
        replay.match.movementPhaseManager.isActivated = false;
        var before = replay.tokens.ToDictionary(t => t.Key, t => t.Value.GetCurrentHex());
        var result = RoomAIActionSequence.Evaluate(snapshot, new RoomAICandidateExplanation { candidate = new RoomActionCandidate {
            actionType = RoomActionType.Shot, step = RoomDecisionStep.ChooseActionType } });
        Assert.That(result.secondary, Is.Not.Null);
        Assert.That(result.secondary.rebounds, Is.Not.Empty);
        Assert.That(result.secondary.primaryGoalProbability + result.secondary.heldProbability + result.secondary.offTargetProbability
            + result.secondary.rebounds.Sum(r => r.probability) + result.secondary.unexpandedReboundProbability, Is.EqualTo(1f).Within(.0001f));
        Assert.That(result.outcomes.goalProbability + result.outcomes.attackRetainsPossessionProbability + result.outcomes.defenseWinsPossessionProbability
            + result.outcomes.attackingRestartProbability + result.outcomes.defendingRestartProbability + result.outcomes.unresolvedProbability,
            Is.EqualTo(1f).Within(.0001f));
        foreach (var token in replay.tokens) Assert.That(token.Value.GetCurrentHex(), Is.SameAs(before[token.Key]));
        foreach (var branch in result.secondary.rebounds.Where(b => b.collectionCost > 0))
            Assert.That(branch.remainingPace, Is.EqualTo(replay.tokens[branch.collectorTokenKey].pace - branch.collectionCost));
    }

    [Test]
    public void SpentSupportMayStealBackButStunnedSupportMayNot()
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_08-10", 83, false);
        var board = replay.tokens.Values.Where(t => t.isPlaying && t.GetCurrentHex() != null).ToDictionary(t => t, t => t.GetCurrentHex());
        var loser = replay.tokens["Home:10"]; var winner = replay.tokens["Away:2"]; var support = replay.tokens["Home:9"];
        board[winner] = replay.Hex(new RoomHexCoordinates { x = 9, z = 0 });
        board[support] = board[winner].GetNeighbors(replay.match.hexGrid).First(h => h != null && !h.isOutOfBounds && !board.Values.Contains(h));
        replay.match.movementPhaseManager.movedTokens.Add(support);
        var available = RoomAISecondaryOutcomes.PreviewPostTackleRecovery(replay.match, loser, winner, board);
        Assert.That(available.tackleExits.First(e => RoomAITacticalSafety.Distance(e.hex, RoomHexCoordinates.FromHex(board[winner])) == 0)
            .eligibleStealers, Does.Contain("Home:9"));
        replay.match.movementPhaseManager.stunnedforNext.Add(support);
        var stunned = RoomAISecondaryOutcomes.PreviewPostTackleRecovery(replay.match, loser, winner, board);
        Assert.That(stunned.tackleExits.SelectMany(e => e.eligibleStealers), Does.Not.Contain("Home:9"));
    }

    [Test]
    public void OnsideRunnerIsNotCalledOffsideAtTheLaterReboundLanding()
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_08-10", 83, false);
        replay.Move("Home:8", new RoomHexCoordinates { x = 9, z = 0 });
        var snapshot = replay.BoardSnapshot(); snapshot.expectedTeam = snapshot.teamInAttack; snapshot.currentState = "EndOfMovementPhase";
        snapshot.clock = new RoomClockSnapshot { currentHalf = 1, isClockRunning = true };
        replay.match.movementPhaseManager.isActivated = false;
        var preview = RoomAIActionSequence.Evaluate(snapshot, new RoomAICandidateExplanation { candidate = new RoomActionCandidate {
            actionType = RoomActionType.Shot, step = RoomDecisionStep.ChooseActionType } });
        var pickups = preview.secondary.rebounds.Where(b => b.collectorTokenKey == "Home:8" && b.collectionCost > 0).ToList();
        Assert.That(pickups, Is.Not.Empty);
        Assert.That(pickups.All(b => b.outcomes.defendingRestartProbability == 0f), Is.True,
            "The runner was onside when the shot was taken; running past the old line afterwards is legal.");
    }
}

public sealed class HighPassScatterEditorTests
{
    [TestCase(0, 0f)]
    [TestCase(1, 0f)]
    [TestCase(4, .5f)]
    [TestCase(6, 5f / 6f)]
    [TestCase(7, 1f)]
    [TestCase(9, 1f)]
    public void AccuracyUsesTheGameplayThreshold(int skill, float expected)
        => Assert.That(HighPassManager.AccuracyProbabilityForDecisionPreview(skill), Is.EqualTo(expected).Within(.00001f));

    [TestCase(0, 0)]
    [TestCase(1, 0)]
    [TestCase(17, 11)]
    public void EveryScatterRollUsesTheLiveParityAwareLandingRules(int x, int z)
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_11-30", 194, false);
        var intended = replay.Hex(new RoomHexCoordinates { x = x, z = z });
        var branches = RoomAIActionSequence.EnumerateScatter(replay.match.looseBallManager, intended);
        Assert.That(branches.Count, Is.EqualTo(36));
        Assert.That(branches.Select(b => (b.directionIndex, b.distance)).Distinct().Count(), Is.EqualTo(36));
        Assert.That(branches.Sum(b => b.conditionalProbability), Is.EqualTo(1f).Within(.00001f));
        foreach (var branch in branches)
        {
            var expected = replay.match.looseBallManager.CalculateFinalInaccuracyTarget(intended, branch.directionIndex, branch.distance);
            if (expected.FinalHex == null) Assert.That(branch.landing, Is.Null);
            else Assert.That(RoomAITacticalSafety.Distance(branch.landing, RoomHexCoordinates.FromHex(expected.FinalHex)), Is.Zero);
            Assert.That(branch.isOutOfBounds, Is.EqualTo(expected.IsOutOfBounds));
        }
        if (x == 17) Assert.That(branches.Any(b => b.isOutOfBounds), Is.True);
    }

    [Test]
    public void AccurateAndScatterProbabilitiesAreWeightedOnce()
    {
        var accurate = new RoomAIPossessionChain().AddTerminal("Goal", .4f, RoomAIPossessionOutcomeKind.Goal)
            .AddTerminal("Retained", .6f, RoomAIPossessionOutcomeKind.AttackRetainsPossession).Evaluate();
        var scatter = new RoomAIPossessionChain().AddTerminal("Goal", .1f, RoomAIPossessionOutcomeKind.Goal)
            .AddTerminal("Lost", .2f, RoomAIPossessionOutcomeKind.DefenseWinsPossession).Evaluate();
        var result = RoomAIActionSequence.CombineAerialOutcomes(.5f, accurate, scatter);
        Assert.That(result.goalProbability, Is.EqualTo(.25f).Within(.00001f));
        Assert.That(result.attackRetainsPossessionProbability, Is.EqualTo(.3f).Within(.00001f));
        Assert.That(result.defenseWinsPossessionProbability, Is.EqualTo(.1f).Within(.00001f));
        Assert.That(result.unresolvedProbability, Is.EqualTo(.35f).Within(.00001f));
    }

    [Test]
    public void RecordedDistributionExpandsScatterWithoutChangingPositions()
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_11-30", 194, false);
        var board = replay.BoardSnapshot(); board.expectedTeam = board.teamInAttack;
        var passer = replay.match.ball.GetCurrentHex().GetOccupyingToken();
        var header = replay.tokens["Away:4"];
        var before = replay.tokens.ToDictionary(t => t.Key, t => t.Value.GetCurrentHex());
        var facts = new RoomAICandidateExplanation { candidate = new RoomActionCandidate {
            actionType = RoomActionType.HighPass, step = RoomDecisionStep.ChooseTarget, targetHex = header.GetCurrentHex() } };
        var result = RoomAIActionSequence.Evaluate(board, facts);
        Assert.That(result.scatterOutcomes.Count, Is.EqualTo(36));
        Assert.That(result.scatterConditionalOutcomes.resolvedProbability, Is.GreaterThan(0f),
            "The failed-accuracy branch is no longer wholly unresolved.");
        foreach (var branch in result.scatterOutcomes)
        {
            Assert.That(branch.outcomes, Is.Not.Null);
            if (branch.isOutOfBounds) Assert.That(branch.outcomes.defendingRestartProbability, Is.EqualTo(1f));
            foreach (var step in branch.steps.Where(s => s.action == "Collect"))
                Assert.That(step.remainingPace, Is.EqualTo(replay.tokens[step.actorTokenKey].pace - step.movementCost));
        }
        Assert.That(result.outcomes.goalProbability, Is.EqualTo(result.accuracyProbability * result.accurateOutcomes.goalProbability
            + (1f - result.accuracyProbability) * result.scatterConditionalOutcomes.goalProbability).Within(.00001f));
        foreach (var token in replay.tokens) Assert.That(token.Value.GetCurrentHex(), Is.SameAs(before[token.Key]));
        var again = RoomAIActionSequence.Evaluate(board, facts);
        Assert.That(again.steps.Count, Is.EqualTo(result.steps.Count), "Cached landings cannot accumulate duplicate prefix steps.");
        Assert.That(again.outcomes.attackRetainsPossessionProbability, Is.EqualTo(result.outcomes.attackRetainsPossessionProbability));
    }

    [Test]
    public void MissingGeometryPreservesAllUnknownScatterMass()
    {
        var branches = RoomAIActionSequence.EnumerateScatter(null, null);
        Assert.That(branches.Count, Is.EqualTo(36));
        Assert.That(branches.All(b => b.landing == null), Is.True);
        Assert.That(branches.Sum(b => b.conditionalProbability), Is.EqualTo(1f).Within(.00001f));
        var result = RoomAIActionSequence.CombineAerialOutcomes(0f, null, null);
        Assert.That(result.unresolvedProbability, Is.EqualTo(1f));
    }
}

public sealed class DefensiveResponseEditorTests
{
    [Test]
    public void HeaderParticipantsCannotMoveInTheImmediatelyFollowingDefensivePhase()
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_08-10", 83, false);
        var snapshot = replay.BoardSnapshot();
        snapshot.expectedTeam = snapshot.teamInAttack;
        var carrier = replay.tokens["Home:10"];
        var baseline = RoomAIDefensiveResponse.Build(snapshot, carrier, carrier.GetCurrentHex());
        Assert.That(baseline.moves, Is.Not.Empty, "The fixture needs an available defensive move.");
        var jumper = replay.tokens[baseline.moves[0].tokenKey];
        var before = jumper.GetCurrentHex();
        var response = RoomAIDefensiveResponse.Build(snapshot, carrier, carrier.GetCurrentHex(),
            new HashSet<PlayerToken> { jumper });
        Assert.That(response.moves.Any(m => m.tokenKey == MatchManager.GetStableTokenKey(jumper)), Is.False);
        Assert.That(response.CopyProjectedPositions()[jumper], Is.SameAs(before),
            "A jumper stays on the board and can still obstruct paths.");
        Assert.That(jumper.GetCurrentHex(), Is.SameAs(before));
    }

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(5)]
    public void ProjectedUnitRespectsOwnershipOccupancyPaceAndConfiguredLimit(int limit)
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_08-10", 83, false);
        typeof(MovementPhaseManager).GetField("maxDefenderMoves", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(replay.match.movementPhaseManager, limit);
        var snapshot = replay.BoardSnapshot();
        snapshot.expectedTeam = snapshot.teamInAttack;
        var carrier = replay.tokens["Home:10"];
        var before = replay.tokens.ToDictionary(p => p.Key, p => p.Value.GetCurrentHex());
        var response = RoomAIDefensiveResponse.Build(snapshot, carrier, carrier.GetCurrentHex());
        Assert.That(response, Is.Not.Null);
        Assert.That(response.moveLimit, Is.EqualTo(limit));
        Assert.That(response.moves.Count, Is.LessThanOrEqualTo(limit));
        Assert.That(response.moves.Select(m => m.tokenKey).Distinct().Count(), Is.EqualTo(response.moves.Count));
        Assert.That(response.moves.Count(m => m.responsibility == "Primary pressure"), Is.LessThanOrEqualTo(1));
        var board = response.CopyProjectedPositions();
        Assert.That(board.Values.Distinct().Count(), Is.EqualTo(board.Count));
        Assert.That(response.threatAfter * response.challengeSurvival, Is.LessThanOrEqualTo(response.threatBefore + .00001f));
        foreach (var move in response.moves)
        {
            Assert.That(move.tokenKey.StartsWith("Away:"), Is.True);
            var token = replay.tokens[move.tokenKey];
            Assert.That(move.cost, Is.InRange(0, token.pace));
            Assert.That(RoomAITacticalSafety.Distance(move.from, move.to), Is.LessThanOrEqualTo(move.cost));
            Assert.That(board[token].isOutOfBounds, Is.False);
            if (move.responsibility != "Primary pressure")
                Assert.That(HexGridUtils.GetHexStepDistance(board[token], board[carrier]), Is.Not.EqualTo(1));
        }
        foreach (var token in replay.tokens) Assert.That(token.Value.GetCurrentHex(), Is.SameAs(before[token.Key]));
    }

    [Test]
    public void AllUnchangedProjectedPositionsMatchTheFutureShotPreview()
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_08-10", 83, false);
        var shooter = replay.tokens["Home:10"];
        var keeper = replay.tokens["Away:1"];
        var origin = shooter.GetCurrentHex();
        var board = PlayingBoard(replay);
        Assert.That(replay.match.shotManager.TryCalculateBestFutureExpectedGoalsPreview(shooter, origin, "fullPower", keeper,
            out float current, out int currentPenalty, out int currentBlocks, out int currentSave), Is.True);
        Assert.That(replay.match.shotManager.TryCalculateBestFutureExpectedGoalsPreview(shooter, origin, "fullPower", board,
            out float projected, out int penalty, out int blocks, out int save), Is.True);
        Assert.That(projected, Is.EqualTo(current).Within(.00001f));
        Assert.That(penalty, Is.EqualTo(currentPenalty));
        Assert.That(blocks, Is.EqualTo(currentBlocks));
        Assert.That(save, Is.EqualTo(currentSave));
    }

    [Test]
    public void PassingLaneAndOffsideReadProjectedPositionsNotLiveFlags()
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_08-10", 83, false);
        var board = PlayingBoard(replay);
        var carrier = replay.tokens["Home:10"];
        var receiver = replay.tokens["Home:9"];
        var defenders = board.Keys.Where(t => !t.isHomeTeam).ToArray();
        var origin = replay.Hex(new RoomHexCoordinates { x = 0, z = 0 });
        var target = replay.Hex(new RoomHexCoordinates { x = 6, z = 0 });
        foreach (var defender in defenders) board[defender] = replay.Hex(new RoomHexCoordinates { x = -15, z = defender.jerseyNumber - 6 });
        board[carrier] = origin; board[receiver] = target;
        float clear = RoomAIDefensiveResponse.PassRetention(replay.match, carrier, origin, target, board);
        board[defenders[0]] = replay.Hex(new RoomHexCoordinates { x = 3, z = 0 });
        Assert.That(clear, Is.EqualTo(1f).Within(.00001f));
        Assert.That(RoomAIDefensiveResponse.PassRetention(replay.match, carrier, origin, target, board), Is.Zero);
        board[defenders[0]] = replay.Hex(new RoomHexCoordinates { x = 17, z = 0 });
        board[defenders[1]] = replay.Hex(new RoomHexCoordinates { x = 4, z = 0 });
        Assert.That(RoomAIDefensiveResponse.IsOffside(receiver, origin, board, 1), Is.True);
        board[defenders[1]] = replay.Hex(new RoomHexCoordinates { x = 8, z = 0 });
        Assert.That(RoomAIDefensiveResponse.IsOffside(receiver, origin, board, 1), Is.False);
    }

    [Test]
    public void SequenceUsesTheProjectedResponseAfterCollectionMovement()
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_11-30", 194, false);
        var snapshot = replay.BoardSnapshot(); snapshot.expectedTeam = snapshot.teamInAttack;
        var receiver = replay.tokens["Away:10"];
        var landing = receiver.GetCurrentHex().GetNeighbors(replay.match.hexGrid).First(h => h != null
            && !h.isOutOfBounds && !h.isAttackOccupied && !h.isDefenseOccupied);
        var facts = new RoomAICandidateExplanation { candidate = new RoomActionCandidate {
            actionType = RoomActionType.GroundPass, step = RoomDecisionStep.ChooseTarget, targetHex = landing } };
        var sequence = RoomAIActionSequence.Evaluate(snapshot, facts);
        Assert.That(sequence.defensiveResponse, Is.Not.Null);
        var carry = sequence.steps.First(s => s.action == "Attacking movement");
        Assert.That(RoomAITacticalSafety.Distance(carry.ballHex, sequence.defensiveResponse.carrierHex), Is.Zero);
        Assert.That(sequence.steps.Any(s => s.action.StartsWith("Defensive movement")), Is.True);
    }

    private static Dictionary<PlayerToken, HexCell> PlayingBoard(ReportedMovementReplay replay) => replay.tokens.Values
        .Where(t => t.isPlaying && !t.isSentOff && t.GetCurrentHex() != null).ToDictionary(t => t, t => t.GetCurrentHex());
}
