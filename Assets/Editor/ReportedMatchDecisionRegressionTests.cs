using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

public class ReportedMatchDecisionRegressionTests
{
    [SetUp]
    public void RequireLocalMatchFixtures()
    {
        string fixtureDirectory = Path.Combine(Application.dataPath, "Editor", "Fixtures");
        if (!Directory.Exists(fixtureDirectory))
        {
            Assert.Ignore("This regression suite requires local match fixtures, which are not included in this checkout.");
        }
    }

    private static JObject Decision(string time, int? number = null)
    {
        var document = JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Editor", "Fixtures", $"ReportedMatch_{time}.json")));
        var decisions = document["fixtures"][0]["decisions"].Children<JObject>();
        return number.HasValue ? decisions.Single(item => (int)item["decisionNumber"] == number) : decisions.First();
    }

    private static RoomAIDecisionSnapshot Snapshot(JObject decision)
    {
        var snapshot = new RoomAIDecisionSnapshot
        {
            currentState = (string)decision["currentState"], manager = (string)decision["manager"],
            expectedTeam = (string)decision["expectedTeam"], teamInAttack = (string)decision["teamInAttack"],
            attackHasPossession = (bool)decision["attackHasPossession"],
            homeTeamDirection = (string)decision["homeTeamDirection"], awayTeamDirection = (string)decision["awayTeamDirection"],
            ball = decision["ball"].ToObject<RoomHexCoordinates>()
        };
        snapshot.tokens.AddRange(decision["tokens"].ToObject<List<RoomAITokenSnapshot>>());
        foreach (var token in snapshot.tokens) token.isAttacker = token.teamSide == snapshot.teamInAttack;
        return snapshot;
    }

    private static JObject Thomas4Decision(int number)
    {
        var document = JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Editor", "Fixtures", "Thomas4_AttackingMovement.json")));
        return document["fixtures"][0]["decisions"].Children<JObject>().Single(item => (int)item["decisionNumber"] == number);
    }

    [TestCase(116, 3)]
    [TestCase(138, 2)]
    [TestCase(148, 2)]
    public void Thomas4FixturesPreservePrematureEndWithUnusedAttackMoves(int number, int unused)
    {
        JObject decision = Thomas4Decision(number);
        Assert.That((string)decision["instruction"]["instructionText"], Does.Contain($"forfeit {unused} remaining moves of Attacking"));
        Assert.That((string)decision["recordedSelection"], Does.Contain("end Movement Phase selection"));
    }

    [TestCase(116)]
    [TestCase(148)]
    public void Thomas4UsefulRecoverySupportWinsInsteadOfEndingAttackAfterOneOrTwoPlayers(int number)
    {
        var snapshot = Snapshot(Thomas4Decision(number));
        var available = Reachable(snapshot);
        foreach (var actor in snapshot.tokens.Where(t => available.ContainsKey(t.tokenKey)))
        {
            if (number == 116 && actor.tokenKey == "Away:5" || number == 148 && actor.jerseyNumber is 3 or 7) continue;
            var move = available[actor.tokenKey].Select(hex => new { hex, gain = RoomAITacticalSafety.RecoverySupportGain(snapshot, actor, hex) })
                .OrderByDescending(item => item.gain).FirstOrDefault();
            if (move == null || move.gain <= 0f) continue;
            var candidate = Candidate(RoomActionType.StartMovement); candidate.manager = nameof(MovementPhaseManager);
            candidate.step = RoomDecisionStep.ChooseActor;
            Add(snapshot, candidate, new RoomAICandidateExplanation { actorToken = actor, actorTokenKey = actor.tokenKey,
                actorHex = actor.currentHex, targetHex = move.hex, isAttackingOffBallMove = true, supportsConcreteContinuation = true,
                attackingContinuation = "Recovery support for pressured carrier", attackingContinuationValue = move.gain });
        }
        Assert.That(snapshot.legalActions.Count, Is.GreaterThan(0), "The recorded board should offer a concrete recovery support move.");
        Add(snapshot, EndMovement(), new RoomAICandidateExplanation());
        var decision = Decide(snapshot);
        Assert.That(decision.selectedCandidate.isForfeit, Is.False);
        Assert.That(decision.selectedEvaluation.safetyPriority, Is.Zero);
        Assert.That(decision.selectedEvaluation.explanation.supportsConcreteContinuation, Is.True);
    }

    [Test]
    public void Thomas4ThirdActorCanCreateANonShootingPassingLane()
    {
        var snapshot = Snapshot(Thomas4Decision(148));
        var actor = snapshot.tokens.Single(token => token.playerName == "Delgado");
        Assert.That(RoomAITacticalSafety.Distance(actor.currentHex, snapshot.ball), Is.GreaterThan(11));
        var target = Reachable(snapshot)[actor.tokenKey].First(hex => RoomAITacticalSafety.Distance(hex, snapshot.ball) <= 11);
        var movement = Candidate(RoomActionType.StartMovement); movement.manager = nameof(MovementPhaseManager);
        movement.step = RoomDecisionStep.ChooseActor;
        Add(snapshot, movement, new RoomAICandidateExplanation { actorToken = actor, actorTokenKey = actor.tokenKey,
            actorHex = actor.currentHex, targetHex = target, isAttackingOffBallMove = true, supportsConcreteContinuation = true,
            isGroundPassContinuation = true, isSupportingPassLaneMove = true, groundPassRetentionProbability = .75f,
            groundPassCollectorTokenKey = actor.tokenKey, groundPassCollectorPickupDistance = 0,
            attackingContinuation = "Creates ordinary passing lane; immediate shooting position is not required",
            attackingContinuationValue = .3f }); // Supply the positive marginal gain this synthetic fixture asserts.
        Add(snapshot, EndMovement(), new RoomAICandidateExplanation());
        Assert.That(Decide(snapshot).selectedCandidate, Is.SameAs(movement));
    }

    [Test]
    public void Thomas4LooseBallMoveIsNotRejectedByAFutureDistributionRace()
    {
        var snapshot = Snapshot(Thomas4Decision(138));
        var movement = Candidate(RoomActionType.StartMovement); movement.manager = nameof(MovementPhaseManager);
        movement.step = RoomDecisionStep.ChooseActor;
        Add(snapshot, movement, new RoomAICandidateExplanation { actorDistanceToBall = 4,
            passRace = new RoomAIPassRacePreview { collectorTokenKey = "Away:4", pickupCost = 8,
                opponentCollectsFirst = true, continuationSurvival = .6f } });
        Add(snapshot, EndMovement(), new RoomAICandidateExplanation());
        Assert.That(Decide(snapshot).selectedCandidate, Is.SameAs(movement));
    }

    [Test]
    public void RejectedForwardOnlyMoveDoesNotDowngradeEndingThePhase()
    {
        var snapshot = Snapshot(Thomas4Decision(116));
        var run = Candidate(RoomActionType.StartMovement); run.manager = nameof(MovementPhaseManager);
        Add(snapshot, run, new RoomAICandidateExplanation { isAttackingOffBallMove = true,
            forwardProgressDelta = 12, hasPossessionLevelPlan = true, possessionPlanValue = 1000 });
        var end = EndMovement(); Add(snapshot, end, new RoomAICandidateExplanation());
        Assert.That(Decide(snapshot).selectedCandidate, Is.SameAs(end));
    }

    private static RoomActionCandidate EndMovement() => new RoomActionCandidate
    { manager = nameof(MovementPhaseManager), actionType = RoomActionType.Decline,
        step = RoomDecisionStep.InterruptionChoice, key = "X", isForfeit = true, isExecutableNow = true };

    [Test]
    public void PhaseEndTraceIncludesRejectedMovementAndItsSafetyReason()
    {
        var end = new RoomAITacticalEvaluation { candidate = EndMovement(), tier = RoomAITacticalTier.Neutral, score = -165 };
        var movement = new RoomAITacticalEvaluation { candidate = Candidate(RoomActionType.StartMovement),
            tier = RoomAITacticalTier.Risky, score = 500, safetyPriority = 1 };
        movement.reasons.Add("Off-ball move has no improved continuation");
        var method = typeof(RoomAIDecisionTraceService).GetMethod("SelectCompetitiveAlternatives",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var retained = (List<RoomAITacticalEvaluation>)method.Invoke(null, new object[] { new List<RoomAITacticalEvaluation> { end, movement } });
        Assert.That(retained, Does.Contain(movement));
    }

    private static RoomAIDecisionSnapshot DefensiveSwarmSnapshot(int number)
    {
        var document = JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "Editor", "Fixtures", "Thomas3_DefensiveSwarm.json")));
        return Snapshot(document["fixtures"][0]["decisions"].Children<JObject>().Single(item => (int)item["decisionNumber"] == number));
    }

    private static Dictionary<string, float> AdvancedThreats() => new Dictionary<string, float>
    { ["Home:9"] = .8f, ["Home:11"] = .5f, ["Home:8"] = .3f, ["Home:10"] = .2f, ["Home:7"] = .1f };

    [Test]
    public void EkstromBackPassUsesOnlyLegalMovesThatReduceTheWholeThreatEstimate()
    {
        var snapshot = DefensiveSwarmSnapshot(309);
        var reachable = Reachable(snapshot);
        var plan = RoomAITacticalSafety.AssignDefense(snapshot, reachable, AdvancedThreats());
        AssertCoordinatedPlan(snapshot, reachable, plan);
        Assert.That(plan.remainingThreatEstimate, Is.LessThan(plan.initialThreatEstimate));
    }

    [Test]
    public void ReplanningDoesNotSpendAnAlreadyUsedDefendersMove()
    {
        var moved = new HashSet<string>();
        string[] sequence = { "Away:11", "Away:9", "Away:8", "Away:2" };
        int index = 0;
        foreach (int number in new[] { 311, 313, 315, 317 })
        {
            moved.Add(sequence[index++]);
            var snapshot = DefensiveSwarmSnapshot(number);
            var reachable = Reachable(snapshot);
            foreach (string key in moved) reachable.Remove(key);
            var plan = RoomAITacticalSafety.AssignDefense(snapshot, reachable, AdvancedThreats());
            AssertCoordinatedPlan(snapshot, reachable, plan);
            foreach (var assignment in plan.assignments.Where(item => moved.Contains(item.tokenKey)))
                Assert.That(RoomAITacticalSafety.Distance(assignment.destination,
                    snapshot.tokens.Single(token => token.tokenKey == assignment.tokenKey).currentHex), Is.Zero);
        }
    }

    [Test]
    public void DecisionManagerCarriesPhaseOwnershipAndClearsItForTheAttackersTurn()
    {
        var snapshot = DefensiveSwarmSnapshot(311);
        var reachable = Reachable(snapshot); reachable.Remove("Away:11");
        var cacheField = typeof(RoomAIDecisionSnapshot).GetField("defensiveMovementDestinations",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var cache = (Dictionary<string, List<RoomHexCoordinates>>)cacheField.GetValue(snapshot);
        foreach (var entry in reachable) cache[entry.Key] = entry.Value;
        snapshot.defensivePhasePlan = RoomAITacticalSafety.AssignDefense(snapshot, reachable, AdvancedThreats());
        var managerObject = new GameObject("Defensive phase regression manager");
        var manager = managerObject.AddComponent<RoomDecisionManager>();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var planField = typeof(RoomDecisionManager).GetField("committedDefensivePhasePlan", flags);
        var prior = new RoomAIDefensivePhasePlan { primaryPressureTokenKey = "Away:11" };
        prior.assignments.Add(new RoomAIDefensiveAssignment { tokenKey = "Away:11", role = "PrimaryPressure" });
        try
        {
            planField.SetValue(manager, prior);
            typeof(RoomDecisionManager).GetField("committedDefensivePhaseTeam", flags).SetValue(manager, "Away");
            typeof(RoomDecisionManager).GetField("committedDefensivePhaseBall", flags).SetValue(manager, snapshot.ball);
            var prepare = typeof(RoomDecisionManager).GetMethod("PrepareCommittedDefensivePhasePlan", flags);
            prepare.Invoke(manager, new object[] { snapshot });
            Assert.That(planField.GetValue(manager), Is.SameAs(snapshot.defensivePhasePlan));
            Assert.That(planField.GetValue(manager), Is.Not.SameAs(prior));
            snapshot.expectedTeam = snapshot.teamInAttack;
            prepare.Invoke(manager, new object[] { snapshot });
            Assert.That(planField.GetValue(manager), Is.Null);
        }
        finally { Object.DestroyImmediate(managerObject); }
    }

    [Test]
    public void ConsecutiveDefensiveDecisionsKeepImprovingWithoutDuplicateOrStationaryMoves()
    {
        var snapshot = DefensiveSwarmSnapshot(309);
        var moved = new HashSet<string>();
        for (int step = 0; step < 5; step++)
        {
            var reachable = Reachable(snapshot);
            foreach (string key in moved) reachable.Remove(key);
            var plan = RoomAITacticalSafety.AssignDefense(snapshot, reachable, AdvancedThreats());
            AssertCoordinatedPlan(snapshot, reachable, plan);
            var choice = plan.assignments.Where(item => item.threatReduction > 0f).OrderBy(item => item.movePriority).FirstOrDefault();
            if (choice == null) break;
            Assert.That(moved.Add(choice.tokenKey), Is.True);
            var actor = snapshot.tokens.Single(token => token.tokenKey == choice.tokenKey);
            Assert.That(RoomAITacticalSafety.Distance(actor.currentHex, choice.destination), Is.GreaterThan(0));
            actor.currentHex = choice.destination;
        }
        Assert.That(moved.Count, Is.GreaterThan(0));
    }

    [TestCase("05-15")]
    [TestCase("07-32")]
    [TestCase("08-10")]
    [TestCase("08-34")]
    [TestCase("10-00")]
    [TestCase("10-20")]
    [TestCase("10-30")]
    [TestCase("11-10")]
    [TestCase("11-30")]
    [TestCase("12-53")]
    [TestCase("13-26")]
    [TestCase("14-24")]
    [TestCase("14-59")]
    public void ReportedPositionsPreserveRosterAndBoard(string time)
    {
        var snapshot = Snapshot(Decision(time));
        Assert.That(snapshot.tokens.Count, Is.EqualTo(32));
        Assert.That(snapshot.tokens.Select(token => token.tokenKey).Distinct().Count(), Is.EqualTo(32));
        Assert.That(snapshot.tokens.Count(token => token.isPlaying && token.currentHex != null), Is.EqualTo(22));
        Assert.That(snapshot.tokens.Count(token => token.isPlaying && token.isGoalKeeper), Is.EqualTo(2));
        Assert.That(snapshot.tokens.Single(token => token.playerName == "Dionte").heading, Is.EqualTo(6));
        Assert.That(snapshot.tokens.Single(token => token.playerName == "Sesay").pace, Is.EqualTo(6));
    }

    [Test]
    public void RecordedF3MoveRejectsBothCentreBacksLeavingFullbackAsLineHolder()
    {
        var snapshot = Snapshot(Decision("05-15", 20));
        var actor = snapshot.tokens.Single(token => token.playerName == "Andersen");
        var facts = new RoomAICandidateExplanation { actorTokenKey = actor.tokenKey, actorToken = actor,
            actorHex = actor.currentHex, targetHex = new RoomHexCoordinates { x = 3, z = 1 } };
        RoomAITacticalSafety.EvaluateDefensiveLine(snapshot, facts);
        Assert.That(facts.defensiveLineLeavesFullback, Is.True);
    }

    [Test]
    public void DefensiveLineCountsEveryNewlyOnsideAttackerAndRespectsBallLine()
    {
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Away", teamInAttack = "Home",
            awayTeamDirection = "RightToLeft", ball = new RoomHexCoordinates { x = 4, z = 0 } };
        snapshot.tokens.AddRange(new[] { Token("Away", 1, 16, 0), Token("Away", 4, 8, 1), Token("Away", 5, 7, -1),
            Token("Home", 9, 9, 2), Token("Home", 10, 9, -2), Token("Home", 11, 12, 0) });
        var actor = snapshot.tokens[1];
        var facts = new RoomAICandidateExplanation { actorTokenKey = actor.tokenKey, actorToken = actor,
            actorHex = actor.currentHex, targetHex = new RoomHexCoordinates { x = 11, z = 1 } };
        RoomAITacticalSafety.EvaluateDefensiveLine(snapshot, facts);
        Assert.That(facts.newlyOnsideOpponents, Is.EqualTo(2));
        snapshot.ball.x = 12;
        RoomAITacticalSafety.EvaluateDefensiveLine(snapshot, facts);
        Assert.That(facts.newlyOnsideOpponents, Is.Zero);
    }

    [Test]
    public void OnsideOpponentCountAloneDoesNotInventAScoringThreat()
    {
        var snapshot = Snapshot(Decision("05-15", 20));
        var safe = Candidate(RoomActionType.FinalThird); var exposed = Candidate(RoomActionType.FinalThird);
        Add(snapshot, safe, new RoomAICandidateExplanation { isRegularFinalThirdReorganization = true, offsideLineRaise = 1 });
        Add(snapshot, exposed, new RoomAICandidateExplanation { isRegularFinalThirdReorganization = true,
            offsideLineRaise = 1, reachableOnsideOpponents = 3 });
        var ranked = RoomAITacticalPlanner.RankCandidates(snapshot, snapshot.legalActions, null, new RoomAITacticalWeights());
        Assert.That(ranked[0].candidate, Is.SameAs(safe));
        Assert.That(ranked[0].score, Is.EqualTo(ranked[1].score),
            "The current team-defense policy evaluates delivery, recovery and finishing routes. "
            + "An onside count without a demonstrated threat must not invent a scoring penalty.");
    }

    [Test]
    public void AndersenKeepsContainmentAfterTheCompleteRecordedTackleComparison()
    {
        JObject record = Decision("07-32", 65);
        var snapshot = Snapshot(record);
        JToken comparison = record["alternatives"][0]["tackle"];
        float threat = (float)record["alternatives"][0]["expectedGoalsAgainst"];
        var tackle = Candidate(RoomActionType.Tackle); var stand = Candidate(RoomActionType.Decline);
        foreach (var candidate in new[] { tackle, stand })
            Add(snapshot, candidate, new RoomAICandidateExplanation { isTackleDecision = true,
                tackleIsAttempted = candidate == tackle, tackleIsFromBehind = (bool)comparison["fromBehind"],
                defensiveExpectedGoalsAgainst = threat, tackleSuccessProbability = (float)comparison["success"],
                tackleFoulProbability = (float)comparison["foul"], tackleSendOffProbability = (float)comparison["sendOff"],
                tackleCardProbability = (float)comparison["foul"], dribblerRetainsProbability = (float)comparison["retained"],
                dribblerBestRepositionExpectedGoals = (float)comparison["repositionXg"],
                dribblerBestRepositionProgress = (int)comparison["repositionProgress"],
                dribblerBestRepositionHex = comparison["repositionHex"].ToObject<RoomHexCoordinates>() });
        var decision = Decide(snapshot);
        Assert.That(decision.selectedCandidate, Is.SameAs(stand));
        Assert.That(decision.selectedEvaluation.reasons, Has.Some.Contains("alternative tackle value="));
        Assert.That(decision.selectedEvaluation.reasons, Has.Some.Contains("reposition cost="));
    }

    [TestCase("13-26")]
    [TestCase("14-59")]
    public void DefenseReplansLegalCoverageAfterItsFirstUsefulMove(string time)
    {
        var snapshot = Snapshot(Decision(time));
        snapshot.expectedTeam = "Away"; snapshot.teamInAttack = "Home";
        var reachable = Reachable(snapshot);
        var plan = RoomAITacticalSafety.AssignDefense(snapshot, reachable);
        AssertCoordinatedPlan(snapshot, reachable, plan);
        var first = plan.assignments.Where(item => item.threatReduction > 0f).OrderBy(item => item.movePriority).First();
        snapshot.tokens.Single(token => token.tokenKey == first.tokenKey).currentHex = first.destination;
        reachable = Reachable(snapshot); reachable.Remove(first.tokenKey);
        var next = RoomAITacticalSafety.AssignDefense(snapshot, reachable);
        AssertCoordinatedPlan(snapshot, reachable, next);
        Assert.That(next, Is.Not.SameAs(plan));
        Assert.That(next.assignments.Single(item => item.tokenKey == first.tokenKey).destination, Is.SameAs(first.destination));
    }

    [Test]
    public void GoalkeeperRejectsRecorded542To833IncreaseDespiteArbitraryWallBonus()
    {
        var snapshot = Snapshot(Decision("08-10", 85));
        var stay = Candidate(RoomActionType.Decline); var rush = Candidate(RoomActionType.GoalkeeperSave);
        Add(snapshot, stay, new RoomAICandidateExplanation { isGoalkeeperPositioning = true,
            goalkeeperPositioningMaximumExpectedGoals = .542f, goalkeeperPositioningCurrentMaximumExpectedGoals = .542f });
        Add(snapshot, rush, new RoomAICandidateExplanation { isGoalkeeperPositioning = true,
            goalkeeperPositioningMaximumExpectedGoals = .833f, goalkeeperPositioningCurrentMaximumExpectedGoals = .542f,
            goalkeeperWallCoverageQuality = 100000 });
        var decision = Decide(snapshot);
        Assert.That(decision.selectedCandidate, Is.SameAs(stay));
        Assert.That(decision.rankedCandidates.Last().safetyPriority, Is.GreaterThan(0));
    }

    [Test]
    public void GoalkeeperSafetyWinsEvenWhenTheSaferMoveHasLessWallCoverage()
    {
        var snapshot = Snapshot(Decision("08-10", 85));
        var safer = Candidate(RoomActionType.GoalkeeperSave); var stay = Candidate(RoomActionType.Decline);
        Add(snapshot, safer, new RoomAICandidateExplanation { isGoalkeeperPositioning = true, goalkeeperPositioningMaximumExpectedGoals = .2f });
        Add(snapshot, stay, new RoomAICandidateExplanation { isGoalkeeperPositioning = true,
            goalkeeperPositioningMaximumExpectedGoals = .3f, goalkeeperWallCoverageQuality = 100000 });
        Assert.That(Decide(snapshot).selectedCandidate, Is.SameAs(safer));
    }

    [Test]
    public void QuickThrowWithNoSafeContinuationLosesToKickDistribution()
    {
        var snapshot = Snapshot(Decision("08-34", 96));
        var quick = Candidate(RoomActionType.QuickThrow); var kick = Candidate(RoomActionType.FinalThird);
        Add(snapshot, quick, new RoomAICandidateExplanation { isGroundPassContinuation = true,
            groundPassRetentionProbability = 1f, passRace = new RoomAIPassRacePreview
            { collectorTokenKey = "Away:3", remainingPace = 4, continuationHexes = 61, safeContinuationHexes = 0 } });
        Add(snapshot, kick, new RoomAICandidateExplanation());
        Assert.That(Decide(snapshot).selectedCandidate, Is.SameAs(kick));
    }

    [Test]
    public void FoylesEntireFourHexAreaIsInsideYanevasTwoPhaseReach()
    {
        var snapshot = Snapshot(Decision("08-34", 103));
        var foyle = snapshot.tokens.Single(token => token.playerName == "Foyle");
        var yaneva = snapshot.tokens.Single(token => token.playerName == "Yaneva");
        var landing = new RoomHexCoordinates { x = 12, z = -7 };
        int collectionCost = RoomAITacticalSafety.Distance(foyle.currentHex, landing);
        Assert.That(foyle.pace * 2 - collectionCost, Is.EqualTo(4));
        var area = Enumerable.Range(-17, 35).SelectMany(x => Enumerable.Range(-12, 25)
            .Select(z => new RoomHexCoordinates { x = x, z = z }))
            .Where(hex => RoomAITacticalSafety.Distance(landing, hex) <= 4).ToList();
        Assert.That(area.Count, Is.EqualTo(61));
        Assert.That(area.Count(hex => !RoomAIPassRace.IsWithinDefenderReach(yaneva, hex, 2)), Is.Zero);
    }

    [TestCase(-5, 0, -4, -1)]
    [TestCase(-4, 0, -3, 0)]
    [TestCase(3, 2, 8, -1)]
    public void PlannerDistanceMatchesUnityGridOffsetCoordinates(int ax, int az, int bx, int bz)
    {
        Assert.That(RoomAITacticalSafety.Distance(new RoomHexCoordinates { x = ax, z = az }, new RoomHexCoordinates { x = bx, z = bz }),
            Is.EqualTo(HexGridUtils.GetHexStepDistance(new Vector3Int(ax, 0, az), new Vector3Int(bx, 0, bz))));
    }

    [Test]
    public void OffBallForwardProgressNeedsAConcreteContinuation()
    {
        var snapshot = Snapshot(Decision("11-30")); snapshot.expectedTeam = snapshot.teamInAttack = "Away";
        var run = Candidate(RoomActionType.StartMovement); var hold = Candidate(RoomActionType.Decline);
        Add(snapshot, run, new RoomAICandidateExplanation { isAttackingOffBallMove = true, forwardProgressDelta = 12 });
        Add(snapshot, hold, new RoomAICandidateExplanation());
        Assert.That(Decide(snapshot).selectedCandidate, Is.SameAs(hold));
    }

    [Test]
    public void RecordedRosterProvidesAerialCandidatesWithoutPrescribingANamedPair()
    {
        var snapshot = Snapshot(Decision("11-30")); snapshot.expectedTeam = snapshot.teamInAttack = "Away";
        var plan = RoomAIAerialReleasePlan.Build(snapshot);
        Assert.That(plan, Is.Not.Null);
        var header = snapshot.tokens.Single(token => token.tokenKey == plan.headerTokenKey);
        var runner = snapshot.tokens.Single(token => token.tokenKey == plan.runnerTokenKey);
        Assert.That(header.isPlaying && runner.isPlaying, Is.True);
        Assert.That(plan.headerHeading, Is.GreaterThanOrEqualTo(4));
        Assert.That(plan.runnerPace, Is.GreaterThanOrEqualTo(4));
        Assert.That(header.tokenKey, Is.Not.EqualTo(runner.tokenKey));
        Assert.That(plan.requiresOffsideReassessment, Is.True);
    }

    [Test]
    public void KickoffPlanPlacesReceiverOnLandingWithFullMovementBudget()
    {
        var snapshot = Snapshot(Decision("10-30", 162));
        var plan = CodexTacticianProvider.BuildKickoffPlan(snapshot);
        var receiver = snapshot.tokens.Single(token => token.tokenKey == plan.receiverTokenKey);
        Assert.That(RoomAITacticalSafety.Distance(receiver.currentHex, plan.landingHex), Is.Zero);
        Assert.That(plan.collectionCost, Is.Zero); Assert.That(plan.remainingPace, Is.EqualTo(receiver.pace));
        Assert.That(plan.remainingAttackingMoves, Is.EqualTo(4)); Assert.That(plan.secondAction, Is.Not.Empty);
    }

    [TestCase(false, "Second goal by Rusu!")]
    [TestCase(true, "A brace by Rusu!")]
    public void SecondGoalCopyPreservesAssistAndScoreWithoutConsumingGameplayRandomness(bool brace, string expected)
    {
        var randomState = Random.state;
        string text = GoalFlowManager.BuildSecondGoalInstruction("Rusu", "Ruiz", "Thomas 3 2 - 0 Ultra Hard 3", brace);
        Assert.That(text, Is.EqualTo(expected + " Assisted by Ruiz! Thomas 3 2 - 0 Ultra Hard 3"));
        Assert.That(Random.state, Is.EqualTo(randomState));
    }

    private static Dictionary<string, List<RoomHexCoordinates>> Reachable(RoomAIDecisionSnapshot snapshot)
    {
        var occupied = snapshot.tokens.Where(t => t.isPlaying && t.currentHex != null).Select(t => (t.currentHex.x, t.currentHex.z)).ToHashSet();
        return snapshot.tokens.Where(t => t.isPlaying && !t.isGoalKeeper && t.teamSide == snapshot.expectedTeam)
            .ToDictionary(t => t.tokenKey, t => Enumerable.Range(-17, 35).SelectMany(x => Enumerable.Range(-12, 25)
                .Select(z => new RoomHexCoordinates { x = x, z = z }))
                .Where(h => RoomAITacticalSafety.Distance(t.currentHex, h) <= t.pace
                    && (!occupied.Contains((h.x, h.z)) || RoomAITacticalSafety.Distance(t.currentHex, h) == 0)).ToList());
    }

    private static RoomAITokenSnapshot Token(string side, int jersey, int x, int z) => new RoomAITokenSnapshot
    { tokenKey = $"{side}:{jersey}", teamSide = side, jerseyNumber = jersey, isPlaying = true,
        currentHex = new RoomHexCoordinates { x = x, z = z }, pace = 5 };
    private static RoomActionCandidate Candidate(RoomActionType type) => new RoomActionCandidate
    { actionType = type, step = RoomDecisionStep.ChooseTarget, isExecutableNow = true };
    private static void Add(RoomAIDecisionSnapshot snapshot, RoomActionCandidate candidate, RoomAICandidateExplanation facts)
    { facts.candidate = candidate; snapshot.legalActions.Add(candidate); snapshot.candidateExplanations.Add(facts); }
    private static RoomAIProviderDecision Decide(RoomAIDecisionSnapshot snapshot) => CodexTacticianProvider.Instance.Decide(
        snapshot, snapshot.legalActions, null, new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard);
    // The Thomas 6 interview replaces fixed role/jersey expectations with
    // coordinated threat reduction, while preserving legality and move budgets.
    private static void AssertCoordinatedPlan(RoomAIDecisionSnapshot snapshot,
        Dictionary<string, List<RoomHexCoordinates>> reachable, RoomAIDefensivePhasePlan plan)
    {
        Assert.That(plan, Is.Not.Null);
        Assert.That(plan.remainingThreatEstimate, Is.LessThanOrEqualTo(plan.initialThreatEstimate + .00001f));
        Assert.That(plan.assignments.Select(item => item.tokenKey).Distinct().Count(), Is.EqualTo(plan.assignments.Count));
        Assert.That(plan.assignments.Select(item => (item.destination.x, item.destination.z)).Distinct().Count(), Is.EqualTo(plan.assignments.Count));
        var moves = plan.assignments.Where(item => item.threatReduction > 0f).ToList();
        Assert.That(moves.Count, Is.LessThanOrEqualTo(snapshot.defensiveMovesRemaining));
        foreach (var move in moves)
        {
            Assert.That(reachable.ContainsKey(move.tokenKey), Is.True);
            Assert.That(reachable[move.tokenKey].Any(hex => RoomAITacticalSafety.Distance(hex, move.destination) == 0), Is.True);
        }
    }

}
