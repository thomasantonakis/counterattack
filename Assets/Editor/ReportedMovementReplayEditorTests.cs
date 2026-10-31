using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class ReportedMovementReplayEditorTests
{
    [Test]
    public void EveryReportedPositionIsAttachedToThePublicPreservationTestNotAHelper()
    {
        var methods = typeof(ReportedMatchDecisionRegressionTests).GetMethods(BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static);
        var preservation = methods.Single(method => method.Name == "ReportedPositionsPreserveRosterAndBoard");
        var clocks = preservation.GetCustomAttributes<TestCaseAttribute>().Select(test => (string)test.Arguments[0]).ToArray();
        Assert.That(clocks, Is.EquivalentTo(new[] { "05-15", "07-32", "08-10", "08-34", "10-00", "10-20",
            "10-30", "11-10", "11-30", "12-53", "13-26", "14-24", "14-59" }));
        Assert.That(methods.Where(method => method.IsPrivate)
            .Any(method => method.GetCustomAttributes<TestCaseAttribute>().Any()), Is.False);
    }

    [TestCase(116, 1)]
    [TestCase(138, 2)]
    [TestCase(148, 2)]
    public void Thomas4ProductionAnalysisKeepsUsingAvailableAttackingMoves(int number, int spent)
    {
        using var replay = new ReportedMovementReplay("Thomas4_AttackingMovement", number);
        var moved = replay.match.movementPhaseManager.movedTokens;
        Assert.That(moved.Count, Is.EqualTo(spent));
        var decision = replay.Evaluate();
        Assert.That(decision.selectedCandidate, Is.Not.Null);
        Assert.That(decision.selectedCandidate.isForfeit, Is.False,
            "A concrete continuation must beat ending after only one/two actors.");
        Assert.That(moved.Contains(decision.selectedCandidate.actor), Is.False);
        Assert.That(decision.selectedEvaluation.safetyPriority, Is.Zero);
        Assert.That(decision.selectedEvaluation.explanation.supportsConcreteContinuation
            || !replay.lastSnapshot.attackHasPossession
                && decision.selectedEvaluation.explanation.actorDistanceToBall >= 0, Is.True);
        Assert.That(decision.rankedCandidates.Where(item => item.candidate.actor != null)
            .Any(item => moved.Contains(item.candidate.actor)), Is.False,
            "Movement ownership must come from the phase events, not hard-coded test exclusions.");
        int beforeDistance = decision.selectedEvaluation.explanation.actorDistanceToBall;
        replay.SelectActor(decision.selectedCandidate.actor);
        var destination = replay.Evaluate();
        Assert.That(destination.selectedCandidate.isForfeit, Is.False,
            "Choosing an actor is not sufficient: its destination must also be useful.");
        Assert.That(destination.selectedCandidate.targetHex, Is.Not.Null);
        Assert.That(destination.selectedEvaluation.explanation.supportsConcreteContinuation
            || !replay.lastSnapshot.attackHasPossession
                && destination.selectedEvaluation.explanation.targetDistanceToBall < beforeDistance, Is.True,
            "Require a computed continuation or actual progress toward the loose ball.");
    }

    [Test]
    public void Thomas3LargerThreatReductionTakesPriorityOverRepositioningTheCoveredFlank()
    {
        using var replay = new ReportedMovementReplay("Thomas3_DefensiveSwarm", 309);
        var decision = replay.Evaluate();
        var foyle = decision.rankedCandidates.Single(item => item.explanation?.actorTokenKey == "Away:3");
        Assert.That(decision.selectedCandidate.actor, Is.Not.SameAs(replay.tokens["Away:3"]));
        Assert.That(decision.selectedCandidate.isForfeit, Is.False);
        var assignments = replay.lastSnapshot.defensivePhasePlan.assignments;
        var selected = assignments.Single(item => item.tokenKey == decision.selectedEvaluation.explanation.actorTokenKey);
        var flank = assignments.Single(item => item.tokenKey == foyle.explanation.actorTokenKey);
        Assert.That(selected.threatReduction, Is.GreaterThan(flank.threatReduction),
            "A flank player may improve central cover later, but must not displace the more useful first recovery.");
        AssertUsefulLegalPlan(replay);
    }

    [Test]
    public void AlreadyAdjacentPrimaryPresserStillCanBeSelectedForATackle()
    {
        var actor = new RoomActionCandidate { manager = nameof(MovementPhaseManager), actionType = RoomActionType.StartMovement,
            step = RoomDecisionStep.ChooseActor, isExecutableNow = true };
        var end = new RoomActionCandidate { manager = nameof(MovementPhaseManager), actionType = RoomActionType.Decline,
            step = RoomDecisionStep.InterruptionChoice, key = "X", isForfeit = true, isExecutableNow = true };
        var snapshot = new RoomAIDecisionSnapshot { currentState = "MovementPhase", expectedTeam = "Away", teamInAttack = "Home",
            ball = new RoomHexCoordinates { x = 0, z = 0 }, defensivePhasePlan = new RoomAIDefensivePhasePlan() };
        snapshot.legalActions.AddRange(new[] { actor, end });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation { candidate = actor, defensivePlanRole = "PrimaryPressure",
            actorDistanceToBall = 1, actorHex = new RoomHexCoordinates { x = 1, z = 0 },
            defensivePlanDestination = new RoomHexCoordinates { x = 1, z = 0 } });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation { candidate = end });
        var decision = CodexTacticianProvider.Instance.Decide(snapshot, snapshot.legalActions, null,
            new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard);
        Assert.That(decision.rankedCandidates.Single(item => item.candidate == actor).safetyPriority, Is.Zero);
        Assert.That(decision.selectedCandidate, Is.SameAs(actor));
    }

    [Test]
    public void Thomas3ProductionThreatsCoverRusuAndRuizWithoutAReachlessBallChase()
    {
        using var replay = new ReportedMovementReplay("Thomas3_DefensiveSwarm", 309);
        replay.Evaluate();
        var plan = replay.lastSnapshot.defensivePhasePlan;
        Assert.That(plan.primaryPressureTokenKey, Is.Null.Or.Empty);
        Assert.That(plan.legalAdjacencyAvailable, Is.False);
        // Team defense replaced fixed WeakFlank/DefensiveLine jobs. A label
        // identifies a representative threat, not every receiver covered by a
        // move or the benefit of closing a second passing lane to that receiver.
        foreach (string threat in new[] { "Home:9", "Home:11" })
        {
            Assert.That(RecoveryPhases(replay.lastSnapshot, threat, plan),
                Is.LessThan(RecoveryPhases(replay.lastSnapshot, threat)), threat + " must gain actual recovery cover.");
            Assert.That(RecoveryPhases(replay.lastSnapshot, threat, plan), Is.LessThanOrEqualTo(1f));
        }
        AssertUsefulLegalPlan(replay);
    }

    [Test]
    public void Thomas3SequentialActorAndDestinationChoicesKeepComplementaryCoverage()
    {
        using var replay = new ReportedMovementReplay("Thomas3_DefensiveSwarm", 309);
        var selectedActors = new HashSet<string>();
        var start = replay.BoardSnapshot();
        for (int step = 0; step < 5; step++)
        {
            var actorChoice = replay.Evaluate();
            if (actorChoice.selectedCandidate.isForfeit) break;
            var actorFacts = actorChoice.selectedEvaluation.explanation;
            string actor = actorFacts.actorTokenKey;
            Assert.That(selectedActors.Add(actor), Is.True);
            Assert.That(RoomAITacticalSafety.Distance(actorFacts.actorHex, actorFacts.defensivePlanDestination), Is.GreaterThan(0));
            var assignment = replay.lastSnapshot.defensivePhasePlan.assignments.Single(item => item.tokenKey == actor);
            Assert.That(assignment.threatReduction, Is.GreaterThan(0f),
                "Another defender may cover the same receiver only when the resulting team threat estimate improves.");
            AssertUsefulLegalPlan(replay);
            replay.SelectActor(actorChoice.selectedCandidate.actor);
            var destinationChoice = replay.Evaluate();
            Assert.That(destinationChoice.selectedCandidate.isForfeit, Is.False);
            Assert.That(destinationChoice.selectedCandidate.targetHex, Is.Not.Null);
            Assert.That(RoomAITacticalSafety.Distance(destinationChoice.selectedEvaluation.explanation.targetHex,
                destinationChoice.selectedEvaluation.explanation.defensivePlanDestination), Is.Zero);
            Assert.That(destinationChoice.selectedEvaluation.explanation.defensiveLineCreatesGap, Is.False);
            replay.Move(actor, RoomHexCoordinates.FromHex(destinationChoice.selectedCandidate.targetHex));
        }
        Assert.That(selectedActors.Count, Is.GreaterThanOrEqualTo(2));
        var finish = replay.BoardSnapshot();
        foreach (string threat in new[] { "Home:9", "Home:11" })
            Assert.That(RecoveryPhases(finish, threat), Is.LessThanOrEqualTo(RecoveryPhases(start, threat)),
                threat + " must not lose recovery cover while subsequent defenders reposition.");
    }

    private static float RecoveryPhases(RoomAIDecisionSnapshot snapshot, string threatKey, RoomAIDefensivePhasePlan plan = null)
    {
        var threat = snapshot.tokens.Single(token => token.tokenKey == threatKey);
        return snapshot.tokens.Where(token => token.isPlaying && !token.isGoalKeeper && token.currentHex != null
                && token.teamSide == snapshot.expectedTeam)
            .Min(token => RoomAITacticalSafety.Distance(plan?.assignments.Single(a => a.tokenKey == token.tokenKey).destination
                ?? token.currentHex, threat.currentHex) / (float)Math.Max(1, token.pace));
    }

    private static void AssertUsefulLegalPlan(ReportedMovementReplay replay)
    {
        var snapshot = replay.lastSnapshot;
        var plan = snapshot.defensivePhasePlan;
        Assert.That(plan.remainingThreatEstimate, Is.LessThan(plan.initialThreatEstimate));
        Assert.That(plan.assignments.Select(item => item.tokenKey).Distinct().Count(), Is.EqualTo(plan.assignments.Count));
        Assert.That(plan.assignments.Select(item => (item.destination.x, item.destination.z)).Distinct().Count(),
            Is.EqualTo(plan.assignments.Count));
        var moves = plan.assignments.Where(item => item.movePriority != int.MaxValue).ToList();
        int remaining = replay.match.movementPhaseManager.DefendingMoveLimit - replay.match.movementPhaseManager.defendersMoved;
        Assert.That(moves.Count, Is.LessThanOrEqualTo(remaining));
        foreach (var move in moves)
        {
            var actor = replay.tokens[move.tokenKey];
            Assert.That(replay.match.movementPhaseManager.movedTokens, Has.No.Member(actor));
            var reachable = HexGridUtils.GetReachableHexes(replay.match.hexGrid, actor.GetCurrentHex(), actor.pace).reachableHexes;
            Assert.That(reachable, Does.Contain(replay.Hex(move.destination)), move.tokenKey + " must have a legal route.");
        }
    }
}
