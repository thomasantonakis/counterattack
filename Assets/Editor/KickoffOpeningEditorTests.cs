using System.Linq;
using NUnit.Framework;

public sealed class KickoffOpeningEditorTests
{
    [Test]
    public void CooperativeKickoffOpeningKeepsTheSameScoresAsTheCompleteComparison()
    {
        var immediate = Board();
        var cooperative = Board();
        var immediateFacts = new[] { Pass(immediate, 0), Pass(immediate, 3), Pass(immediate, 8) };
        var cooperativeFacts = new[] { Pass(cooperative, 0), Pass(cooperative, 3), Pass(cooperative, 8) };
        System.Collections.Generic.List<RoomAITacticalEvaluation> Evaluations(RoomAICandidateExplanation[] facts) =>
            facts.Select(f => new RoomAITacticalEvaluation { candidate = f.candidate, explanation = f,
                tier = RoomAITacticalTier.SafeProgression, score = 100 }).ToList();
        var first = Evaluations(immediateFacts);
        var second = Evaluations(cooperativeFacts);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
        typeof(CodexTacticianProvider).GetMethod("ApplyKickoffFirstActionPlan", flags)
            .Invoke(null, new object[] { immediate, first });
        var iterator = (System.Collections.IEnumerator)typeof(CodexTacticianProvider)
            .GetMethod("ApplyKickoffFirstActionPlanIncrementally", flags)
            .Invoke(null, new object[] { cooperative, second });
        while (iterator.MoveNext()) { }
        var expected = first.OrderBy(e => e.explanation.groundPassCollectorPickupDistance)
            .Select(e => (e.score, e.safetyPriority, e.tier)).ToArray();
        var actual = second.OrderBy(e => e.explanation.groundPassCollectorPickupDistance)
            .Select(e => (e.score, e.safetyPriority, e.tier)).ToArray();
        Assert.That(actual, Is.EqualTo(expected));
        Assert.That(cooperative.kickoffPlan.receiverTokenKey, Is.EqualTo(immediate.kickoffPlan.receiverTokenKey));
    }

    [Test]
    public void RecordedOneGoalKickoffComparesBackLineToAguayoOpening()
    {
        using var replay = new ReportedMovementReplay("Thomas7_UH7_Kickoff", 277, false);
        var snapshot = replay.BoardSnapshot();
        snapshot.kickoffPlan = new RoomAIKickoffPlan { team = "Away", takerTokenKey = "Away:6",
            receiverTokenKey = "Away:10", landingHex = new RoomHexCoordinates { x = 2, z = -3 },
            receiverSetupHex = new RoomHexCoordinates { x = 2, z = -3 }, isSetupProjection = true,
            firstPassEvaluated = true, firstActionReady = true };
        var method = typeof(CodexTacticianProvider).GetMethod("CalculateKickoffFormationPenalty",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        int recorded = (int)method.Invoke(null, new object[] { snapshot, "Away:5", new RoomHexCoordinates { x = 9, z = 3 } });
        int compact = (int)method.Invoke(null, new object[] { snapshot, "Away:5", new RoomHexCoordinates { x = 7, z = 3 } });
        var kickoff = replay.match.gameObject.AddComponent<KickoffManager>();
        kickoff.hexGrid = replay.match.hexGrid; kickoff.ball = replay.match.ball;
        var legal = typeof(KickoffManager).GetMethod("IsValidPostGoalSetupDecisionDestination",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.That(legal.Invoke(kickoff, new object[] { replay.tokens["Away:5"],
            replay.Hex(new RoomHexCoordinates { x = 7, z = 3 }), null }), Is.True);
        Assert.That(compact, Is.LessThan(recorded));
    }
    [TestCase(1)]
    [TestCase(-1)]
    public void KickoffRecoveryExposureRespondsToPressAndBothCentreBacks(int direction)
    {
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Home", teamInAttack = "Home",
            homeTeamDirection = direction == 1 ? "LeftToRight" : "RightToLeft" };
        var first = new RoomAITokenSnapshot { tokenKey = "Home:4", teamSide = "Home", jerseyNumber = 4,
            isPlaying = true, pace = 4, currentHex = new RoomHexCoordinates { x = -9 * direction, z = 0 } };
        var second = new RoomAITokenSnapshot { tokenKey = "Home:5", teamSide = "Home", jerseyNumber = 5,
            isPlaying = true, pace = 4, currentHex = new RoomHexCoordinates { x = -9 * direction, z = 2 } };
        var presser = new RoomAITokenSnapshot { tokenKey = "Away:9", teamSide = "Away", isPlaying = true,
            pace = 5, tackling = 5, currentHex = new RoomHexCoordinates { x = -1 * direction, z = 0 } };
        snapshot.tokens.AddRange(new[] { first, second, presser });
        snapshot.kickoffPlan = new RoomAIKickoffPlan { team = "Home", receiverTokenKey = "Home:10",
            firstPassEvaluated = true, firstActionReady = true, landingHex = new RoomHexCoordinates { x = -2 * direction, z = 0 } };
        float deep = RoomAITacticalSafety.KickoffOpeningRecoveryExposure(snapshot, null, null);
        float closer = RoomAITacticalSafety.KickoffOpeningRecoveryExposure(snapshot, "Home:4",
            new RoomHexCoordinates { x = -6 * direction, z = 0 });
        Assert.That(deep, Is.GreaterThan(closer));
        presser.currentHex = new RoomHexCoordinates { x = 16 * direction, z = 0 };
        Assert.That(RoomAITacticalSafety.KickoffOpeningRecoveryExposure(snapshot, null, null), Is.Zero,
            "A remote opponent must not be invented as an immediate press on the receiver.");
    }

    [Test]
    public void KickoffSetupDoesNotTreatAMovedDirectReceiverAsStillAtTheLanding()
    {
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Home", teamInAttack = "Home", homeTeamDirection = "LeftToRight" };
        snapshot.tokens.Add(new RoomAITokenSnapshot { tokenKey = "Home:10", teamSide = "Home", jerseyNumber = 10,
            isPlaying = true, pace = 4, currentHex = new RoomHexCoordinates { x = -2, z = -3 } });
        snapshot.kickoffPlan = new RoomAIKickoffPlan { team = "Home", receiverTokenKey = "Home:10",
            firstPassEvaluated = true, firstActionReady = true, collectionCost = 0,
            landingHex = new RoomHexCoordinates { x = -2, z = -3 } };
        var method = typeof(CodexTacticianProvider).GetMethod("CalculateKickoffFormationPenalty",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        int stay = (int)method.Invoke(null, new object[] { snapshot, null, null });
        int move = (int)method.Invoke(null, new object[] { snapshot, "Home:10", new RoomHexCoordinates { x = -3, z = -3 } });
        Assert.That(move, Is.GreaterThan(stay), "Moving the chosen receiver away cannot preserve the same direct opening.");
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void KickoffShapeAccountsForGapToTheActualPlannedReceiver(int direction)
    {
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Home", teamInAttack = "Home",
            homeTeamDirection = direction == 1 ? "LeftToRight" : "RightToLeft" };
        snapshot.tokens.Add(new RoomAITokenSnapshot { tokenKey = "Home:4", teamSide = "Home", jerseyNumber = 4,
            isPlaying = true, pace = 4, currentHex = new RoomHexCoordinates { x = -9 * direction, z = -3 } });
        snapshot.kickoffPlan = new RoomAIKickoffPlan { team = "Home", receiverTokenKey = "Home:10",
            firstPassEvaluated = true, firstActionReady = true, landingHex = new RoomHexCoordinates { x = -2 * direction, z = -3 } };
        var method = typeof(CodexTacticianProvider).GetMethod("CalculateKickoffFormationPenalty",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        int deep = (int)method.Invoke(null, new object[] { snapshot, null, null });
        int closer = (int)method.Invoke(null, new object[] { snapshot, "Home:4", new RoomHexCoordinates { x = -7 * direction, z = -3 } });
        snapshot.kickoffPlan.firstPassEvaluated = false;
        int unevaluated = (int)method.Invoke(null, new object[] { snapshot, null, null });
        Assert.That(deep, Is.GreaterThan(closer));
        Assert.That(deep, Is.GreaterThan(unevaluated));
    }
    public static RoomAIDecisionSnapshot Board(int movementLimit = 4)
    {
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Home", teamInAttack = "Home", attackHasPossession = true,
            currentState = "StandardPass", ball = new RoomHexCoordinates { x = 0, z = 0 },
            homeTeamDirection = "LeftToRight", awayTeamDirection = "RightToLeft", freshAttackingMoveLimit = movementLimit };
        snapshot.tokens.AddRange(new[] {
            new RoomAITokenSnapshot { tokenKey = "Home:6", teamSide = "Home", jerseyNumber = 6, isPlaying = true,
                currentHex = new RoomHexCoordinates { x = 0, z = 0 }, pace = 4 },
            new RoomAITokenSnapshot { tokenKey = "Home:12", teamSide = "Home", jerseyNumber = 12, isPlaying = true,
                currentHex = new RoomHexCoordinates { x = -4, z = 1 }, pace = 5, highPass = 6, shooting = 4, dribbling = 2 } });
        return snapshot;
    }

    public static RoomAICandidateExplanation Pass(RoomAIDecisionSnapshot snapshot, int cost, float xg = .2f)
    {
        var candidate = new RoomActionCandidate { manager = nameof(GroundBallManager), actionType = RoomActionType.GroundPass,
            step = RoomDecisionStep.ChooseTarget, isExecutableNow = true, label = "Kickoff pass" };
        var facts = new RoomAICandidateExplanation { candidate = candidate, isKickoffDecision = true,
            isGroundPassContinuation = true, groundPassCollectorTokenKey = "Home:12", groundPassCollectorPickupDistance = cost,
            groundPassContinuationTargetHex = cost == 0 ? snapshot.tokens[1].currentHex : new RoomHexCoordinates { x = -6, z = 1 },
            groundPassRetentionProbability = .9f, groundPassRouteExpectedGoals = xg,
            passRace = new RoomAIPassRacePreview { pickupCost = cost, continuationSurvival = .8f,
                bestContinuationHex = new RoomHexCoordinates { x = 1, z = 1 }, routeExpectedGoals = xg } };
        snapshot.legalActions.Add(candidate);
        snapshot.candidateExplanations.Add(facts);
        return facts;
    }

    [TestCase(0, 5, 5, 0, 0, 6, true)]
    [TestCase(3, 2, 2, 1, 1, 5, true)]
    [TestCase(8, 2, 0, 2, 1, 5, false)]
    public void CollectionUsesActualCostPaceAndConfiguredMovementLimit(int cost, int paceAfter, int paceNext,
        int phases, int consumed, int movesLeft, bool fits)
    {
        var snapshot = Board(6);
        var plan = RoomAIKickoffPlanner.FromPassFacts(snapshot, Pass(snapshot, cost));
        Assert.That(plan.collectionCost, Is.EqualTo(cost));
        Assert.That(plan.remainingPace, Is.EqualTo(paceAfter));
        Assert.That(plan.remainingPaceThisMovement, Is.EqualTo(paceNext));
        Assert.That(plan.collectionPhases, Is.EqualTo(phases));
        Assert.That(plan.collectionMovesConsumed, Is.EqualTo(consumed));
        Assert.That(plan.remainingAttackingMoves, Is.EqualTo(movesLeft));
        Assert.That(plan.collectionFitsNextMovement, Is.EqualTo(fits));
    }

    [Test]
    public void MultiPhasePickupDoesNotAdvertiseAnUnreachableFirstMovementTarget()
    {
        var snapshot = Board();
        var plan = RoomAIKickoffPlanner.FromPassFacts(snapshot, Pass(snapshot, 8));
        Assert.That(plan.collectionFitsNextMovement, Is.False);
        Assert.That(plan.secondActionTargetHex, Is.Null,
            "Without a runtime route, leave the intermediate target unknown rather than pretending the landing is reachable.");
        Assert.That(plan.continuationEvaluated, Is.False);
    }

    [Test]
    public void EqualThreatSpacePassLosesToReceptionWithoutRetrieval()
    {
        var snapshot = Board();
        var direct = RoomAIKickoffPlanner.FromPassFacts(snapshot, Pass(snapshot, 0));
        var space = RoomAIKickoffPlanner.FromPassFacts(snapshot, Pass(snapshot, 3));
        Assert.That(RoomAIKickoffPlanner.SelectOpening(new[] { space, direct }), Is.SameAs(direct));
        var decision = CodexTacticianProvider.Instance.Decide(snapshot, snapshot.legalActions, null,
            new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard);
        Assert.That(decision.selectedCandidate, Is.SameAs(snapshot.legalActions[0]));
        Assert.That(decision.rankedCandidates.Single(item => item.candidate == snapshot.legalActions[1]).safetyPriority, Is.GreaterThan(0));
    }

    [Test]
    public void SpacePassCanWinWhenItsMeasuredContinuationCompensatesForRetrieval()
    {
        var snapshot = Board();
        var direct = RoomAIKickoffPlanner.FromPassFacts(snapshot, Pass(snapshot, 0, .1f));
        var space = RoomAIKickoffPlanner.FromPassFacts(snapshot, Pass(snapshot, 3, .8f));
        Assert.That(space.value, Is.GreaterThan(direct.value));
        Assert.That(RoomAIKickoffPlanner.SelectOpening(new[] { direct, space }), Is.SameAs(space));
    }

    [Test]
    public void HighPassAttributeDoesNotInventALegalSecondAction()
    {
        var snapshot = Board();
        var facts = Pass(snapshot, 0, -1f);
        facts.passRace = null;
        var plan = RoomAIKickoffPlanner.FromPassFacts(snapshot, facts);
        Assert.That(snapshot.tokens[1].highPass, Is.EqualTo(6));
        Assert.That(plan.secondActionType, Is.EqualTo(RoomActionType.Unknown));
        Assert.That(plan.continuationEvaluated, Is.False);
        Assert.That(plan.secondAction, Does.Contain("no supported second action"));
    }

    [Test]
    public void ProjectedReceiverRelocationDoesNotClaimFreeCollectionBeforePlacement()
    {
        var snapshot = Board();
        var plan = RoomAIKickoffPlanner.Costed(snapshot, snapshot.tokens[0], snapshot.tokens[1],
            new RoomHexCoordinates { x = -7, z = 2 }, -1);
        Assert.That(plan.collectionCost, Is.EqualTo(-1));
        Assert.That(plan.remainingPace, Is.EqualTo(-1));
        Assert.That(plan.remainingAttackingMoves, Is.EqualTo(-1));
        Assert.That(plan.firstPassEvaluated, Is.False);
        Assert.That(plan.firstActionReady, Is.False);
    }

    [Test]
    public void ReportedKickoffUsesManagerGeneratedPassesAndActualCollectionRaces()
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_10-30", 162, false);
        var decision = replay.EvaluateKickoffPass();
        var plan = replay.lastSnapshot.kickoffPlan;
        Assert.That(decision.selectedCandidate, Is.Not.Null);
        Assert.That(plan, Is.Not.Null);
        Assert.That(plan.firstPassEvaluated, Is.True);
        Assert.That(plan.collectionCost, Is.Zero,
            "The recorded retrieval kickoff should now lose to a legal direct receiver.");
        Assert.That(plan.remainingAttackingMoves, Is.EqualTo(replay.match.movementPhaseManager.AttackingMoveLimit));
        Assert.That(plan.remainingPace, Is.EqualTo(replay.tokens[plan.receiverTokenKey].pace));
        Assert.That(plan.continuationEvaluated, Is.True);
        Assert.That(plan.secondActionType, Is.Not.EqualTo(RoomActionType.Unknown));
        Assert.That(plan.secondActionType, Is.Not.EqualTo(RoomActionType.GroundPass),
            "EndOfStandardPass does not offer another Standard Pass immediately.");
        Assert.That(plan.secondActionTargetHex, Is.Not.Null);
        if (plan.secondActionType == RoomActionType.StartMovement)
        {
            var mover = replay.tokens[plan.secondActionMoverTokenKey];
            var reachable = HexGridUtils.GetReachableHexes(replay.match.hexGrid, mover.GetCurrentHex(), mover.pace).reachableHexes;
            Assert.That(reachable, Does.Contain(replay.Hex(plan.secondActionTargetHex)));
        }
    }

    [Test]
    public void SetupReceiverGoalIsConnectedToTheIntendedFirstPass()
    {
        using var replay = new ReportedMovementReplay("ReportedMatch_10-30", 162, false);
        var snapshot = replay.BoardSnapshot();
        snapshot.currentState = "KickOffSetup";
        snapshot.kickoffPlan = CodexTacticianProvider.BuildKickoffPlan(snapshot);
        var plan = snapshot.kickoffPlan;
        Assert.That(plan, Is.Not.Null);
        Assert.That(plan.isSetupProjection, Is.True);
        var receiver = snapshot.tokens.Single(token => token.tokenKey == plan.receiverTokenKey);
        var target = typeof(CodexTacticianProvider).GetMethod("TryGetHumanLedKickoffTarget",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var arguments = new object[] { snapshot, receiver, null };
        Assert.That((bool)target.Invoke(null, arguments), Is.True);
        Assert.That(RoomAITacticalSafety.Distance((RoomHexCoordinates)arguments[2], plan.landingHex), Is.Zero);
        Assert.That(plan.firstPassEvaluated, Is.True);
        Assert.That(plan.continuationEvaluated, Is.True);
    }

    [Test]
    public void CostedTraceFollowsTheActuallySelectedPassNotAFixedReceiverBonus()
    {
        var snapshot = Board();
        Pass(snapshot, 0);
        Pass(snapshot, 3);
        var decision = CodexTacticianProvider.Instance.Decide(snapshot, snapshot.legalActions, null,
            new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard);
        Assert.That(snapshot.kickoffPlan, Is.SameAs(RoomAIKickoffPlanner.FromPassFacts(snapshot, decision.selectedEvaluation.explanation)));
        Assert.That(decision.selectedEvaluation.reasons, Has.Some.Contains("Collection cost 0"));
        Assert.That(decision.selectedEvaluation.reasons, Has.None.Contains("all four attacking moves"));
    }
}
