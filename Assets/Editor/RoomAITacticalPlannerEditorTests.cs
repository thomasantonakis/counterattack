using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public class RoomAITacticalPlannerEditorTests
{
    [Test]
    public void SupportAdjacentToCarrierHasAMarginalCrowdingCost()
    {
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Away", ball = new RoomHexCoordinates { x = 5, z = 0 } };
        float near = RoomAITacticalSafety.OutletCrowdingCost(snapshot, "Away:10", new RoomHexCoordinates { x = 4, z = 0 });
        float wider = RoomAITacticalSafety.OutletCrowdingCost(snapshot, "Away:10", new RoomHexCoordinates { x = 2, z = 0 });
        Assert.That(near, Is.GreaterThan(wider));
        Assert.That(near - wider, Is.LessThan(20f), "A strong concrete outlet may still justify close support.");
    }
    [Test]
    public void DefendingLooseBallDoesNotRewardOwnShootingPosition()
    {
        var move = CreateCandidate(RoomActionType.StartMovement);
        move.step = RoomDecisionStep.ChooseTarget;
        var snapshot = CreateAttackingSnapshot(move);
        snapshot.expectedTeam = "Away"; snapshot.teamInAttack = "Home"; snapshot.attackHasPossession = false;
        var facts = snapshot.candidateExplanations[0];
        facts.receiverCanReachShootingHexNextMovement = true;
        facts.minimumStepsToShootingHex = 1;
        facts.targetCanShootFrom = true;
        facts.forwardProgressDelta = 5;
        var ranked = RoomAITacticalPlanner.RankCandidates(snapshot, snapshot.legalActions, null, new RoomAITacticalWeights());
        Assert.That(ranked[0].reasons, Has.None.Contains("receiver can reach a shooting hex"));
        Assert.That(ranked[0].reasons, Has.None.Contains("target is a shooting position"));
        Assert.That(ranked[0].reasons, Has.None.Contains("advances 5 hex"));
    }

    [Test]
    public void GroundPassToBetterSnapshot_RanksAboveLowerExpectedGoalsShot()
    {
        RoomActionCandidate directShot = CreateCandidate(RoomActionType.Shot);
        RoomActionCandidate pass = CreateCandidate(RoomActionType.GroundPass);
        pass.step = RoomDecisionStep.ChooseTarget;
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(directShot, pass);
        snapshot.candidateExplanations[0].expectedGoals = 0.199f;
        snapshot.candidateExplanations[1].isGroundPassContinuation = true;
        snapshot.candidateExplanations[1].groundPassCollectorTokenKey = "Away:11";
        snapshot.candidateExplanations[1].groundPassCollectorPickupDistance = 0;
        snapshot.candidateExplanations[1].groundPassCollectorRemainingPace = 3;
        snapshot.candidateExplanations[1].groundPassRetentionProbability = 0.75f;
        snapshot.candidateExplanations[1].groundPassImmediateSnapshotExpectedGoals = 0.299f;
        snapshot.candidateExplanations[1].groundPassRouteExpectedGoals = 0.22425f;
        snapshot.candidateExplanations[1].groundPassCreatesReachableShot = true;

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(pass));
        Assert.That(ranked[0].tier, Is.EqualTo(RoomAITacticalTier.ImmediateFinish));
        Assert.That(ranked[0].reasons, Has.Some.Contains("pass-to-Snapshot route"));
    }

    [Test]
    public void GroundPassIntoSpace_PenalizesDefendersWhoCanArriveBeforeCollection()
    {
        RoomActionCandidate safe = CreateCandidate(RoomActionType.GroundPass);
        safe.step = RoomDecisionStep.ChooseTarget;
        RoomActionCandidate contested = CreateCandidate(RoomActionType.GroundPass);
        contested.step = RoomDecisionStep.ChooseTarget;
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(safe, contested);
        foreach (RoomAICandidateExplanation explanation in snapshot.candidateExplanations)
        {
            explanation.isGroundPassContinuation = true;
            explanation.groundPassCollectorTokenKey = "Away:9";
            explanation.groundPassCollectorPickupDistance = 3;
            explanation.groundPassCollectorRemainingPace = 2;
            explanation.groundPassRetentionProbability = 0.8f;
            explanation.groundPassCreatesReachableShot = true;
        }
        snapshot.candidateExplanations[1].groundPassPrePickupChallengeCount = 1;

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(safe));
        Assert.That(ranked[1].reasons, Has.Some.Contains("before collection"));
    }

    [Test]
    public void SafeTwoPhaseSpacePass_RecognizesSamMoveAndRanksItAboveContestedAlternative()
    {
        RoomActionCandidate samMove = CreateCandidate(RoomActionType.GroundPass);
        samMove.step = RoomDecisionStep.ChooseTarget;
        RoomActionCandidate contested = CreateCandidate(RoomActionType.GroundPass);
        contested.step = RoomDecisionStep.ChooseTarget;
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(samMove, contested);
        foreach (RoomAICandidateExplanation explanation in snapshot.candidateExplanations)
        {
            explanation.isGroundPassContinuation = true;
            explanation.groundPassCollectorTokenKey = "Home:8";
            explanation.groundPassCollectorPickupDistance = 2;
            explanation.groundPassRetentionProbability = 1f;
            explanation.groundPassCreatesSecondMovementShot = true;
        }

        snapshot.candidateExplanations[0].groundPassIsSafeTwoPhaseSpacePlan = true;
        snapshot.candidateExplanations[1].groundPassPrePickupChallengeCount = 1;

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(samMove));
        Assert.That(ranked[0].reasons, Has.Some.Contains("Sam move"));
    }

    [Test]
    public void SupportingPassLaneMove_RanksAboveOptionalAttackingGoalkeeperMove()
    {
        RoomActionCandidate supportingMove = CreateCandidate(RoomActionType.StartMovement);
        supportingMove.manager = nameof(MovementPhaseManager);
        supportingMove.step = RoomDecisionStep.ChooseActor;
        RoomActionCandidate goalkeeperMove = CreateCandidate(RoomActionType.StartMovement);
        goalkeeperMove.manager = nameof(MovementPhaseManager);
        goalkeeperMove.step = RoomDecisionStep.ChooseActor;
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(supportingMove, goalkeeperMove);
        snapshot.candidateExplanations[0].isGroundPassContinuation = true;
        snapshot.candidateExplanations[0].isSupportingPassLaneMove = true;
        snapshot.candidateExplanations[0].groundPassCollectorTokenKey = "Away:11";
        snapshot.candidateExplanations[0].groundPassCollectorPickupDistance = 0;
        snapshot.candidateExplanations[0].groundPassCollectorRemainingPace = 4;
        snapshot.candidateExplanations[0].groundPassRetentionProbability = 0.8f;
        snapshot.candidateExplanations[0].groundPassImmediateSnapshotExpectedGoals = 0.3f;
        snapshot.candidateExplanations[0].groundPassRouteExpectedGoals = 0.24f;
        snapshot.candidateExplanations[0].groundPassCreatesReachableShot = true;
        snapshot.candidateExplanations[1].isGoalkeeperPositioning = true;
        snapshot.candidateExplanations[1].actorToken = new RoomAITokenSnapshot
        {
            tokenKey = "Away:1",
            teamSide = "Away",
            jerseyNumber = 1,
            isPlaying = true,
            isGoalKeeper = true
        };

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.UltraHard);

        Assert.That(decision.selectedCandidate, Is.SameAs(supportingMove));
        RoomAITacticalEvaluation goalkeeperEvaluation = decision.rankedCandidates.Find(
            evaluation => evaluation.candidate == goalkeeperMove);
        Assert.That(goalkeeperEvaluation.tier, Is.EqualTo(RoomAITacticalTier.Risky));
        Assert.That(goalkeeperEvaluation.reasons, Has.Some.Contains("no previewed sweeper/support continuation"));
    }

    [Test]
    public void GoalkeeperActorWhoseBestDestinationIsForfeit_IsNotSelectedFirst()
    {
        RoomActionCandidate goalkeeper = CreateCandidate(RoomActionType.StartMovement);
        goalkeeper.manager = nameof(MovementPhaseManager);
        goalkeeper.step = RoomDecisionStep.ChooseActor;
        RoomActionCandidate forfeit = CreateCandidate(RoomActionType.StartMovement);
        forfeit.manager = nameof(MovementPhaseManager);
        forfeit.step = RoomDecisionStep.ChooseActor;
        forfeit.isForfeit = true;
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(goalkeeper, forfeit);
        snapshot.candidateExplanations[0].isGoalkeeperPositioning = true;
        snapshot.candidateExplanations[0].goalkeeperActorDestinationPreviewed = true;
        snapshot.candidateExplanations[0].goalkeeperActorBestContinuationIsForfeit = true;
        snapshot.candidateExplanations[0].goalkeeperActorBestDestinationHex
            = new RoomHexCoordinates { x = -16, z = 0 };

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(forfeit));
        Assert.That(ranked[1].reasons, Has.Some.Contains("forfeit the phase before selecting the goalkeeper"));
    }

    [Test]
    public void PossessionChain_EvaluatesNestedCompleteRoute()
    {
        RoomAIPossessionChain accurateHeader = new RoomAIPossessionChain("Accurate HP")
            .AddTerminal("Goal", 0.4f, RoomAIPossessionOutcomeKind.Goal)
            .AddTerminal("Attack controls", 0.6f, RoomAIPossessionOutcomeKind.AttackRetainsPossession);
        RoomAIPossessionChain inaccuratePass = new RoomAIPossessionChain("Inaccurate HP")
            .AddTerminal("Defense collects", 1f, RoomAIPossessionOutcomeKind.DefenseWinsPossession);
        RoomAIPossessionChain route = new RoomAIPossessionChain("High Pass route")
            .AddContinuation("Accurate", 0.5f, accurateHeader)
            .AddContinuation("Inaccurate", 0.5f, inaccuratePass);

        RoomAIPossessionChainSummary summary = route.Evaluate();

        Assert.That(summary.goalProbability, Is.EqualTo(0.2f).Within(0.0001f));
        Assert.That(summary.attackRetainsPossessionProbability, Is.EqualTo(0.3f).Within(0.0001f));
        Assert.That(summary.defenseWinsPossessionProbability, Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(summary.unresolvedProbability, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(summary.isComplete, Is.True);
    }

    [Test]
    public void PossessionChain_ReportsMissingProbabilityAsUnresolved()
    {
        RoomAIPossessionChain route = new RoomAIPossessionChain("Partial shot model")
            .AddTerminal("Goal", 0.35f, RoomAIPossessionOutcomeKind.Goal);

        RoomAIPossessionChainSummary summary = route.Evaluate();

        Assert.That(summary.goalProbability, Is.EqualTo(0.35f).Within(0.0001f));
        Assert.That(summary.unresolvedProbability, Is.EqualTo(0.65f).Within(0.0001f));
        Assert.That(summary.isComplete, Is.False);
    }

    [Test]
    public void PossessionChain_StopsCyclesAsUnresolved()
    {
        RoomAIPossessionChain first = new RoomAIPossessionChain("First MP");
        RoomAIPossessionChain second = new RoomAIPossessionChain("Second MP");
        first.AddContinuation("Continue", 1f, second);
        second.AddContinuation("Loop", 1f, first);

        RoomAIPossessionChainSummary summary = first.Evaluate();

        Assert.That(summary.unresolvedProbability, Is.EqualTo(1f));
        Assert.That(summary.isComplete, Is.False);
    }

    [Test]
    public void AttributeCheckProbability_IncludesJackpotAndDistancePenalty()
    {
        float adjacentControl = ExpectedStatsCalculator.CalculateAttributeCheckSuccessProbability(5);
        float twoHexControl = ExpectedStatsCalculator.CalculateAttributeCheckSuccessProbability(5, modifier: -1);

        Assert.That(adjacentControl, Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(twoHexControl, Is.EqualTo(1f / 3f).Within(0.0001f));
    }

    [Test]
    public void SequentialHeaderInterceptions_UseEveryEligibleDefender()
    {
        GameObject firstObject = new GameObject("First header interceptor");
        GameObject secondObject = new GameObject("Second header interceptor");
        try
        {
            PlayerToken first = firstObject.AddComponent<PlayerToken>();
            PlayerToken second = secondObject.AddComponent<PlayerToken>();
            first.tackling = 4;
            second.tackling = 4;

            float probability = ExpectedStatsCalculator.CalculateAtLeastOneRecoveryProbability(
                new[] { first, second });

            Assert.That(probability, Is.EqualTo(11f / 36f).Within(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(firstObject);
            Object.DestroyImmediate(secondObject);
        }
    }

    [Test]
    public void MovementInterceptionExpectation_ResolvesFoulBeforeHighTacklingRecovery()
    {
        GameObject defenderObject = new GameObject("Movement interceptor");
        try
        {
            PlayerToken defender = defenderObject.AddComponent<PlayerToken>();
            defender.tackling = 6;

            ExpectedStatsCalculator.MovementInterceptionExpectation expectation =
                ExpectedStatsCalculator.CalculateMovementInterceptionExpectation(defender);

            Assert.That(expectation.foulProbability, Is.EqualTo(1f / 6f).Within(0.0001f));
            Assert.That(expectation.recoveryProbability, Is.EqualTo(3f / 6f).Within(0.0001f));
            Assert.That(expectation.continuationProbability, Is.EqualTo(2f / 6f).Within(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(defenderObject);
        }
    }

    [Test]
    public void RankCandidates_ValuesImmediateSnapshotAfterHeadedPassToPlayer()
    {
        RoomActionCandidate passIntoSpace = CreateCandidate(RoomActionType.Header);
        RoomActionCandidate passToShooter = CreateCandidate(RoomActionType.Header);
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(passIntoSpace, passToShooter);
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = passIntoSpace,
            isHeadedPassTarget = true,
            headedPassTargetsSpace = true,
            headedPassInterceptionProbability = 0f
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = passToShooter,
            isHeadedPassTarget = true,
            headedPassTargetsPlayer = true,
            headedPassImmediateSnapshotExpectedGoals = 0.35f
        });

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            new RoomAIAttackPlan { planType = RoomAIAttackPlanType.CreateShot, team = "Away" },
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(passToShooter));
        Assert.That(ranked[0].reasons, Has.Some.Contains("immediate Snapshot xG=0.350"));
    }

    [TestCase(0.40f, true)]
    [TestCase(0.20f, false)]
    public void RankCandidates_ComparesDirectHeaderSnapshotWithSpaceCollectionRoute(float routeXg, bool preferSpace)
    {
        RoomActionCandidate directPass = CreateCandidate(RoomActionType.Header);
        RoomActionCandidate passIntoSpace = CreateCandidate(RoomActionType.Header);
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(directPass, passIntoSpace);
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = directPass,
            isHeadedPassTarget = true,
            headedPassTargetsPlayer = true,
            targetCanShootFrom = true,
            headedPassImmediateSnapshotExpectedGoals = 0.30f
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = passIntoSpace,
            isHeadedPassTarget = true,
            headedPassTargetsSpace = true,
            headedPassSpaceCollectorTokenKey = "Away:9",
            headedPassSpaceSnapshotHex = new RoomHexCoordinates { x = 16, z = 1 },
            headedPassSpaceRemainingPace = 3,
            headedPassSpaceReachSnapshotProbability = routeXg / 0.50f,
            headedPassSpaceSnapshotExpectedGoals = 0.50f,
            headedPassSpaceRouteExpectedGoals = routeXg
        });

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            new RoomAIAttackPlan { planType = RoomAIAttackPlanType.CreateShot, team = "Away" },
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(preferSpace ? passIntoSpace : directPass));
        Assert.That(ranked.Find(item => item.candidate == passIntoSpace).reasons,
            Has.Some.Contains($"complete route xG={routeXg:0.000}"));
        Assert.That(ranked.Find(item => item.candidate == directPass).reasons,
            Has.None.Contains("target is a shooting position"), "Do not count the measured shot a second time.");
    }

    [Test]
    public void RoomDecisionManager_CachesStableStateAndReevaluatesChangedLegalChoice()
    {
        GameObject managerObject = new GameObject("RoomDecisionManager cache test");
        GameObject textObject = new GameObject(
            "RoomDecisionManager cache test text",
            typeof(RectTransform));
        MatchManager previousMatchManager = MatchManager.Instance;
        try
        {
            MatchManager.Instance = null;
            RoomDecisionManager manager = managerObject.AddComponent<RoomDecisionManager>();
            manager.decisionText = textObject.AddComponent<TextMeshProUGUI>();
            GameplayInstructionSnapshot snapshot = new GameplayInstructionSnapshot
            {
                isAwaitingInput = true,
                manager = nameof(MovementPhaseManager),
                expectedTeam = "Home",
                expectedInput = "key",
                instructionText = "Choose an action."
            };
            RoomDecisionContext context = new RoomDecisionContext
            {
                manager = snapshot.manager,
                expectedTeam = snapshot.expectedTeam,
                expectedInput = snapshot.expectedInput
            };
            context.AddKeyActionCandidate(
                snapshot.manager,
                RoomActionType.StartMovement,
                RoomDecisionStep.ChooseActionType,
                "M",
                "Start Movement Phase");

            var settings = new MatchManager.GameSettings { homeRoomPersona = "Random" };
            manager.UpdateDecision(snapshot, context, settings);
            // CPU evaluation is cached after execution, whereas Ask Human now
            // deliberately performs no tactical analysis. Simulate completion
            // without dispatching an input or entering Play Mode.
            var fields = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(RoomDecisionManager).GetField("lastAutomatedExecutionFingerprint", fields).SetValue(manager,
                typeof(RoomDecisionManager).GetField("evaluatingDecisionStateKey", fields).GetValue(manager));
            manager.UpdateDecision(snapshot, context, settings);

            Assert.That(ReadPrivateInt(manager, "tacticalEvaluationCount"), Is.EqualTo(1));
            Assert.That(ReadPrivateInt(manager, "tacticalEvaluationCacheHits"), Is.EqualTo(1));

            context.actions[0].label = "Start a different legal Movement Phase";
            manager.UpdateDecision(snapshot, context, settings);

            Assert.That(ReadPrivateInt(manager, "tacticalEvaluationCount"), Is.EqualTo(1),
                "Presentation changes must not repeat CPU tactical analysis.");
            Assert.That(ReadPrivateInt(manager, "tacticalEvaluationCacheHits"), Is.EqualTo(2));

            context.actions[0].key = "X";
            manager.UpdateDecision(snapshot, context, settings);
            Assert.That(ReadPrivateInt(manager, "tacticalEvaluationCount"), Is.EqualTo(2));
            Assert.That(ReadPrivateInt(manager, "tacticalEvaluationCacheHits"), Is.EqualTo(2));
        }
        finally
        {
            MatchManager.Instance = previousMatchManager;
            Object.DestroyImmediate(textObject);
            Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void RetiredCpuPersonas_ResolveToCodexTactician()
    {
        var settings = new MatchManager.GameSettings
        {
            awayRoomPersona = "1 step ultra hard",
            homeRoomPersona = "CPU-Random"
        };

        Assert.That(AIManager.ResolveRoomPersona(settings, "Away"), Is.EqualTo(AIManager.RoomPersona.CodexTactician));
        Assert.That(AIManager.ResolveCodexTacticianDifficulty(settings, "Away"), Is.EqualTo(AIManager.CodexTacticianDifficulty.UltraHard));
        Assert.That(AIManager.ResolveRoomPersona(settings, "Home"), Is.EqualTo(AIManager.RoomPersona.CodexTactician));
    }

    [Test]
    public void RankCandidates_PutsLegalShotAheadOfMovement()
    {
        RoomActionCandidate movement = CreateCandidate(RoomActionType.StartMovement);
        RoomActionCandidate shot = CreateCandidate(RoomActionType.Shot);
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(movement, shot);

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            new RoomAIAttackPlan { planType = RoomAIAttackPlanType.FinishChance, team = "Away" },
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(shot));
        Assert.That(ranked[0].tier, Is.EqualTo(RoomAITacticalTier.ImmediateFinish));
    }

    [Test]
    public void RankCandidates_PrefersSafeProgressTowardGoal()
    {
        RoomActionCandidate safeProgress = CreateCandidate(RoomActionType.SelectHex);
        RoomActionCandidate pressuredRetreat = CreateCandidate(RoomActionType.SelectHex);
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(safeProgress, pressuredRetreat);
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = safeProgress,
            targetHex = new RoomHexCoordinates { x = 4, z = 0 },
            forwardProgressDelta = 3,
            advancesTowardOpponentGoal = true,
            opponentsWithinTackleReach = 0
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = pressuredRetreat,
            targetHex = new RoomHexCoordinates { x = -1, z = 0 },
            forwardProgressDelta = -2,
            opponentsWithinTackleReach = 2
        });

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            new RoomAIAttackPlan { planType = RoomAIAttackPlanType.ProgressPossession, team = "Away" },
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(safeProgress));
        Assert.That(ranked[0].tier, Is.EqualTo(RoomAITacticalTier.SafeProgression));
        Assert.That(ranked[1].tier, Is.EqualTo(RoomAITacticalTier.Risky));
    }

    [Test]
    public void RankCandidates_PrefersHigherExpectedGoalsBetweenShotTargets()
    {
        RoomActionCandidate lowerQualityTarget = CreateCandidate(RoomActionType.Shot);
        RoomActionCandidate higherQualityTarget = CreateCandidate(RoomActionType.Shot);
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(lowerQualityTarget, higherQualityTarget);
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = lowerQualityTarget,
            expectedGoals = 0.18f,
            shotBlockerCount = 1
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = higherQualityTarget,
            expectedGoals = 0.43f,
            shotBlockerCount = 0
        });

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            new RoomAIAttackPlan { planType = RoomAIAttackPlanType.FinishChance, team = "Away" },
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(higherQualityTarget));
        Assert.That(ranked[0].reasons, Has.Some.Contains("shot xG=0.430"));
    }

    [Test]
    public void RankCandidates_DeclinesSnapshotWhenSafeFullShotHasHigherExpectedValue()
    {
        RoomActionCandidate snapshotShot = CreateCandidate(RoomActionType.Shot);
        RoomActionCandidate continueForFullShot = CreateCandidate(RoomActionType.ContinueWithoutShot);
        continueForFullShot.isForfeit = true;
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(snapshotShot, continueForFullShot);
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = snapshotShot,
            expectedGoals = 0.30f,
            isSnapshotDecision = true,
            fullShotExpectedGoals = 0.46f,
            estimatedBallRetentionProbability = 1f,
            timeAllowsFollowUpShot = true
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = continueForFullShot,
            expectedGoals = 0.46f,
            isSnapshotDecision = true,
            fullShotExpectedGoals = 0.46f,
            estimatedBallRetentionProbability = 1f,
            timeAllowsFollowUpShot = true
        });

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            new RoomAIAttackPlan { planType = RoomAIAttackPlanType.FinishChance, team = "Away" },
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(continueForFullShot));
        Assert.That(ranked[0].tier, Is.EqualTo(RoomAITacticalTier.ImmediateFinish));
    }

    [Test]
    public void RankCandidates_TakesSnapshotWhenChallengeRiskErasesFullShotBonus()
    {
        RoomActionCandidate snapshotShot = CreateCandidate(RoomActionType.Shot);
        RoomActionCandidate continueForFullShot = CreateCandidate(RoomActionType.ContinueWithoutShot);
        continueForFullShot.isForfeit = true;
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(snapshotShot, continueForFullShot);
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = snapshotShot,
            expectedGoals = 0.30f,
            isSnapshotDecision = true
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = continueForFullShot,
            expectedGoals = 0.18f,
            isSnapshotDecision = true,
            fullShotExpectedGoals = 0.46f,
            estimatedBallRetentionProbability = 0.39f,
            timeAllowsFollowUpShot = true
        });

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            new RoomAIAttackPlan { planType = RoomAIAttackPlanType.FinishChance, team = "Away" },
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(snapshotShot));
    }

    [Test]
    public void RankCandidates_ValuesSecondBookingOnPreferredWalkInContinuation()
    {
        RoomActionCandidate snapshotShot = CreateCandidate(RoomActionType.Shot);
        RoomActionCandidate continueForBooking = CreateCandidate(RoomActionType.ContinueWithoutShot);
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(snapshotShot, continueForBooking);
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = snapshotShot,
            expectedGoals = 0.20f,
            isSnapshotDecision = true
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = continueForBooking,
            expectedGoals = 0.19f,
            isSnapshotDecision = true,
            pursuesSecondBooking = true,
            secondBookingProbability = 0.50f
        });

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            new RoomAIAttackPlan { planType = RoomAIAttackPlanType.FinishChance, team = "Away" },
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(continueForBooking));
        Assert.That(ranked[0].reasons, Has.Some.Contains("second booking"));
    }

    [Test]
    public void RankCandidates_ComparesSetPieceHeaderRouteAgainstDirectFreeKickXG()
    {
        RoomActionCandidate directShot = CreateCandidate(RoomActionType.Shot);
        RoomActionCandidate highPass = CreateCandidate(RoomActionType.HighPass);
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(directShot, highPass);
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = directShot,
            expectedGoals = 0.28f
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = highPass,
            expectedGoals = 0.36f,
            isSetPieceHighPassPlan = true,
            setPieceHighPassAccuracyProbability = 0.67f,
            setPieceHeaderGoalProbability = 0.54f
        });

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            new RoomAIAttackPlan { planType = RoomAIAttackPlanType.FinishChance, team = "Away" },
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(highPass));
        Assert.That(ranked[0].tier, Is.EqualTo(RoomAITacticalTier.ImmediateFinish));
    }

    [Test]
    public void RankCandidates_PrefersCornerFreeMoveThatCreatesTwoHeaderCongestion()
    {
        RoomActionCandidate isolatedMove = CreateCandidate(RoomActionType.SetupMove);
        RoomActionCandidate congestionMove = CreateCandidate(RoomActionType.SetupMove);
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(isolatedMove, congestionMove);
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = isolatedMove,
            isSetPieceSetup = true,
            isCornerSetup = true,
            isCornerFreeThreeMove = true,
            setPieceActorIsAttacking = true
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = congestionMove,
            isSetPieceSetup = true,
            isCornerSetup = true,
            isCornerFreeThreeMove = true,
            setPieceActorIsAttacking = true,
            adjacentVeryGoodAttackingHeaders = 1
        });

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            new RoomAIAttackPlan { planType = RoomAIAttackPlanType.WideAerialAttack, team = "Away" },
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(congestionMove));
        Assert.That(ranked[0].reasons, Has.Some.Contains("heading congestion"));
    }

    [Test]
    public void RankCandidates_GoalkeeperChoosesLowerHeaderExpectedGoalsAgainst()
    {
        RoomActionCandidate stay = CreateCandidate(RoomActionType.Decline);
        RoomActionCandidate rush = CreateCandidate(RoomActionType.GoalkeeperSave);
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(stay, rush);
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = stay,
            isHighPassGoalkeeperDecision = true,
            goalkeeperRushesForHighPass = false,
            defensiveExpectedGoalsAgainst = 0.38f
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = rush,
            isHighPassGoalkeeperDecision = true,
            goalkeeperRushesForHighPass = true,
            defensiveExpectedGoalsAgainst = 0.21f
        });

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            new RoomAIAttackPlan { planType = RoomAIAttackPlanType.None, team = "Home" },
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(rush));
        Assert.That(ranked[0].reasons, Has.Some.Contains("opponent header xG=0.210"));
    }

    [Test]
    public void RankCandidates_TreatsReachableWalkInAsImmediateFinish()
    {
        RoomActionCandidate ordinaryMove = CreateCandidate(RoomActionType.StartMovement);
        RoomActionCandidate walkIn = CreateCandidate(RoomActionType.StartMovement);
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(ordinaryMove, walkIn);
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = ordinaryMove,
            targetHex = new RoomHexCoordinates { x = 16, z = 0 },
            advancesTowardOpponentGoal = true,
            forwardProgressDelta = 1
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = walkIn,
            targetHex = new RoomHexCoordinates { x = 18, z = 0 },
            targetIsInOpponentGoal = true,
            advancesTowardOpponentGoal = true,
            forwardProgressDelta = 3
        });

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            new RoomAIAttackPlan { planType = RoomAIAttackPlanType.FinishChance, team = "Away" },
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(walkIn));
        Assert.That(ranked[0].tier, Is.EqualTo(RoomAITacticalTier.ImmediateFinish));
    }

    [Test]
    public void UpdateAttackPlan_DoesNotSelectAerialOnlyBecauseHighPasserAndHeaderExist()
    {
        RoomActionCandidate highPass = CreateCandidate(RoomActionType.HighPass);
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(highPass);
        snapshot.tokens.Add(CreateToken("Away", 7, highPass: 6, heading: 2, dribbling: 3, shooting: 5));
        snapshot.tokens.Add(CreateToken("Away", 9, highPass: 3, heading: 6, dribbling: 3, shooting: 4));
        snapshot.tokens.Add(CreateToken("Home", 4, highPass: 3, heading: 6, dribbling: 2, shooting: 1));
        snapshot.tacticalMatchup = RoomAITacticalProfileBuilder.Build(snapshot);

        RoomAIAttackPlan plan = RoomAITacticalPlanner.UpdateAttackPlan(
            snapshot,
            null,
            new RoomAITacticalWeights());

        Assert.That(plan.planType, Is.Not.EqualTo(RoomAIAttackPlanType.WideAerialAttack));
        Assert.That(plan.team, Is.EqualTo("Away"));
    }

    [Test]
    public void UpdateAttackPlan_SelectsAerialWhenNoShooterIsGoodEnough()
    {
        RoomActionCandidate highPass = CreateCandidate(RoomActionType.HighPass);
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(highPass);
        snapshot.tokens.Add(CreateToken("Away", 7, highPass: 6, heading: 2, dribbling: 5, shooting: 3));
        snapshot.tokens.Add(CreateToken("Away", 9, highPass: 3, heading: 6, dribbling: 3, shooting: 3));
        snapshot.tokens.Add(CreateToken("Home", 4, highPass: 3, heading: 6, dribbling: 2, shooting: 1));
        snapshot.tacticalMatchup = RoomAITacticalProfileBuilder.Build(snapshot);

        RoomAIAttackPlan plan = RoomAITacticalPlanner.UpdateAttackPlan(
            snapshot,
            null,
            new RoomAITacticalWeights());

        Assert.That(plan.planType, Is.EqualTo(RoomAIAttackPlanType.WideAerialAttack));
    }

    [Test]
    public void TacticalProfile_UsesFourAsFallbackWhenSpecialistsAreScarce()
    {
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot();
        RoomAITokenSnapshot fallbackShooter = CreateToken(
            "Away",
            10,
            highPass: 5,
            heading: 3,
            dribbling: 4,
            shooting: 4);
        snapshot.tokens.Add(fallbackShooter);
        snapshot.tokens.Add(CreateToken("Away", 9, highPass: 4, heading: 5, dribbling: 2, shooting: 3));

        RoomAIMatchupTacticalProfile matchup = RoomAITacticalProfileBuilder.Build(snapshot);

        Assert.That(matchup.ownTeam.effectiveShootingThreshold, Is.EqualTo(4));
        Assert.That(matchup.ownTeam.effectiveDribblingThreshold, Is.EqualTo(4));
        Assert.That(matchup.ownTeam.primaryShooter, Is.SameAs(fallbackShooter));
        Assert.That(matchup.ownTeam.primaryBallCarrier, Is.SameAs(fallbackShooter));
    }

    [Test]
    public void TacticalProfile_IdentifiesSpecialistDefensiveThreats()
    {
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot();
        snapshot.tokens.Add(CreateToken("Home", 9, highPass: 3, heading: 4, dribbling: 2, shooting: 5, pace: 6));
        snapshot.tokens.Add(CreateToken("Home", 10, highPass: 4, heading: 3, dribbling: 6, shooting: 3));
        snapshot.tokens.Add(CreateToken("Home", 11, highPass: 3, heading: 6, dribbling: 2, shooting: 3));

        RoomAIMatchupTacticalProfile matchup = RoomAITacticalProfileBuilder.Build(snapshot);

        Assert.That(matchup.opponentHasFastVeryGoodShooter, Is.True);
        Assert.That(matchup.opponentHasEliteDribbler, Is.True);
        Assert.That(matchup.opponentHasEliteHeader, Is.True);
        Assert.That(matchup.defensiveThreats, Has.Some.Contains("fast 5+ shooters"));
        Assert.That(matchup.defensiveThreats, Has.Some.Contains("elite dribbler"));
        Assert.That(matchup.defensiveThreats, Has.Some.Contains("elite header"));
        Assert.That(matchup.defensiveThreats, Has.Some.Contains("coordinated line"));
        Assert.That(matchup.requiresRoleReassignment, Is.True, "This fixture has no defending outfield XI.");
        Assert.That(matchup.defensiveThreats, Has.Some.Contains("rebuild roles"));
    }

    [Test]
    public void TacticalProfile_PoorHandlingWithAerialToolsSuggestsShootingForCorners()
    {
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot();
        snapshot.tokens.Add(CreateToken("Away", 7, highPass: 6, heading: 2, dribbling: 5, shooting: 5));
        snapshot.tokens.Add(CreateToken("Away", 9, highPass: 3, heading: 6, dribbling: 2, shooting: 4));
        RoomAITokenSnapshot goalkeeper = CreateToken(
            "Home",
            1,
            highPass: 4,
            heading: 0,
            dribbling: 2,
            shooting: 0);
        goalkeeper.isGoalKeeper = true;
        goalkeeper.handling = 3;
        snapshot.tokens.Add(goalkeeper);

        RoomAIMatchupTacticalProfile matchup = RoomAITacticalProfileBuilder.Build(snapshot);

        Assert.That(matchup.opponentGoalkeeperHasPoorHandling, Is.True);
        Assert.That(matchup.hasCornerHeadingDepthAdvantage, Is.True);
        Assert.That(matchup.suggestedAttackingIdentity, Is.EqualTo(RoomAIAttackingIdentity.ShootForCorners));
    }

    [Test]
    public void TacticalProfile_DoesNotTargetCornersWhenDefendingTopSixAreBetterHeaders()
    {
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot();
        for (int jersey = 2; jersey <= 7; jersey++)
        {
            snapshot.tokens.Add(CreateToken("Away", jersey, highPass: 5, heading: 3, dribbling: 3, shooting: 5));
            snapshot.tokens.Add(CreateToken("Home", jersey, highPass: 3, heading: 5, dribbling: 2, shooting: 2));
        }

        RoomAITokenSnapshot goalkeeper = CreateToken(
            "Home",
            1,
            highPass: 4,
            heading: 0,
            dribbling: 2,
            shooting: 0);
        goalkeeper.isGoalKeeper = true;
        goalkeeper.handling = 3;
        snapshot.tokens.Add(goalkeeper);

        RoomAIMatchupTacticalProfile matchup = RoomAITacticalProfileBuilder.Build(snapshot);

        Assert.That(matchup.opponentGoalkeeperHasPoorHandling, Is.True);
        Assert.That(matchup.ownTopSixHeadingTotal, Is.EqualTo(18));
        Assert.That(matchup.opponentTopSixHeadingTotal, Is.EqualTo(30));
        Assert.That(matchup.hasCornerHeadingDepthAdvantage, Is.False);
        Assert.That(matchup.suggestedAttackingIdentity, Is.Not.EqualTo(RoomAIAttackingIdentity.ShootForCorners));
    }

    [Test]
    public void TacticalProfile_TargetsSingleFavorableCornerPairEvenWhenTotalHeadingIsLower()
    {
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot();
        int[] attackingHeading = { 6, 1, 1, 1, 1, 1 };
        for (int index = 0; index < attackingHeading.Length; index++)
        {
            snapshot.tokens.Add(CreateToken(
                "Away",
                index + 2,
                highPass: 5,
                heading: attackingHeading[index],
                dribbling: 4,
                shooting: 5));
            snapshot.tokens.Add(CreateToken(
                "Home",
                index + 2,
                highPass: 3,
                heading: 5,
                dribbling: 2,
                shooting: 2));
        }

        RoomAITokenSnapshot goalkeeper = CreateToken(
            "Home",
            1,
            highPass: 4,
            heading: 0,
            dribbling: 2,
            shooting: 0);
        goalkeeper.isGoalKeeper = true;
        goalkeeper.handling = 3;
        snapshot.tokens.Add(goalkeeper);

        RoomAIMatchupTacticalProfile matchup = RoomAITacticalProfileBuilder.Build(snapshot);

        Assert.That(matchup.ownTopSixHeadingTotal, Is.EqualTo(11));
        Assert.That(matchup.opponentTopSixHeadingTotal, Is.EqualTo(30));
        Assert.That(matchup.favorableCornerHeadingMatchups, Is.EqualTo(1));
        Assert.That(matchup.bestCornerTarget.heading, Is.EqualTo(6));
        Assert.That(matchup.suggestedAttackingIdentity, Is.EqualTo(RoomAIAttackingIdentity.ShootForCorners));
    }

    [TestCase(2, RoomAIFormationRole.Fullback)]
    [TestCase(4, RoomAIFormationRole.CentreBack)]
    [TestCase(6, RoomAIFormationRole.CentralMidfielder)]
    [TestCase(7, RoomAIFormationRole.Winger)]
    [TestCase(9, RoomAIFormationRole.Striker)]
    [TestCase(10, RoomAIFormationRole.AttackingMidfielder)]
    public void TacticalProfile_ResolvesDraftFormationRole(int jerseyNumber, RoomAIFormationRole expectedRole)
    {
        RoomAITokenSnapshot token = CreateToken(
            "Away",
            jerseyNumber,
            highPass: 3,
            heading: 3,
            dribbling: 3,
            shooting: 3);

        Assert.That(RoomAITacticalProfileBuilder.ResolveRole(token), Is.EqualTo(expectedRole));
    }

    [Test]
    public void TacticalProfile_SesayTypeThreatPullsHighLineInsideFive()
    {
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot();
        RoomAITokenSnapshot recoveryDefender = CreateToken(
            "Away",
            4,
            highPass: 2,
            heading: 5,
            dribbling: 2,
            shooting: 1,
            pace: 5);
        recoveryDefender.tackling = 5;
        snapshot.tokens.Add(recoveryDefender);
        snapshot.tokens.Add(CreateToken(
            "Home",
            9,
            highPass: 2,
            heading: 3,
            dribbling: 2,
            shooting: 5,
            pace: 6));

        RoomAIMatchupTacticalProfile matchup = RoomAITacticalProfileBuilder.Build(snapshot);

        Assert.That(matchup.fastestScoringThreat.pace, Is.EqualTo(6));
        Assert.That(matchup.bestRecoveryDefender, Is.SameAs(recoveryDefender));
        Assert.That(matchup.defensiveRunBuffer, Is.EqualTo(2));
        Assert.That(matchup.recommendedDefensiveLineAbsoluteX, Is.EqualTo(4));
    }

    [Test]
    public void TacticalProfile_UsesSecondDeepestPlayerForOffsideLine()
    {
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot();
        snapshot.homeTeamDirection = "RightToLeft";
        snapshot.awayTeamDirection = "LeftToRight";
        snapshot.ball = new RoomHexCoordinates { x = -3, z = 0 };
        RoomAITokenSnapshot goalkeeper = CreateToken(
            "Away",
            1,
            highPass: 3,
            heading: 0,
            dribbling: 2,
            shooting: 0);
        goalkeeper.isGoalKeeper = true;
        goalkeeper.currentHex = new RoomHexCoordinates { x = -16, z = 0 };
        RoomAITokenSnapshot forgottenDefender = CreateToken(
            "Away",
            4,
            highPass: 2,
            heading: 5,
            dribbling: 2,
            shooting: 1);
        forgottenDefender.currentHex = new RoomHexCoordinates { x = -10, z = 0 };
        RoomAITokenSnapshot higherDefender = CreateToken(
            "Away",
            5,
            highPass: 2,
            heading: 5,
            dribbling: 2,
            shooting: 1);
        higherDefender.currentHex = new RoomHexCoordinates { x = -5, z = 0 };
        snapshot.tokens.Add(goalkeeper);
        snapshot.tokens.Add(forgottenDefender);
        snapshot.tokens.Add(higherDefender);

        RoomAIMatchupTacticalProfile matchup = RoomAITacticalProfileBuilder.Build(snapshot);

        Assert.That(matchup.deepestDefendingPlayer, Is.SameAs(goalkeeper));
        Assert.That(matchup.offsideLineDefiningPlayer, Is.SameAs(forgottenDefender));
        Assert.That(matchup.secondDeepestDefenderLineX, Is.EqualTo(-10));
        Assert.That(matchup.currentOffsideLineX, Is.EqualTo(-10));
        Assert.That(matchup.ballDefinesOffsideLine, Is.False);
    }

    [Test]
    public void TacticalProfile_UsesBallWhenItIsBeyondSecondDeepestDefender()
    {
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot();
        snapshot.homeTeamDirection = "RightToLeft";
        snapshot.awayTeamDirection = "LeftToRight";
        snapshot.ball = new RoomHexCoordinates { x = -12, z = 0 };

        RoomAITokenSnapshot goalkeeper = CreateToken("Away", 1, highPass: 3, heading: 0, dribbling: 2, shooting: 0);
        goalkeeper.isGoalKeeper = true;
        goalkeeper.currentHex = new RoomHexCoordinates { x = -16, z = 0 };
        RoomAITokenSnapshot secondDeepest = CreateToken("Away", 4, highPass: 2, heading: 5, dribbling: 2, shooting: 1);
        secondDeepest.currentHex = new RoomHexCoordinates { x = -8, z = 0 };
        RoomAITokenSnapshot higherDefender = CreateToken("Away", 5, highPass: 2, heading: 5, dribbling: 2, shooting: 1);
        higherDefender.currentHex = new RoomHexCoordinates { x = -5, z = 0 };
        snapshot.tokens.Add(goalkeeper);
        snapshot.tokens.Add(secondDeepest);
        snapshot.tokens.Add(higherDefender);

        RoomAIMatchupTacticalProfile matchup = RoomAITacticalProfileBuilder.Build(snapshot);

        Assert.That(matchup.secondDeepestDefenderLineX, Is.EqualTo(-8));
        Assert.That(matchup.currentOffsideLineX, Is.EqualTo(-12));
        Assert.That(matchup.ballDefinesOffsideLine, Is.True);
    }

    [Test]
    public void TacticalProfile_DoesNotLetShootingFourSetLineAgainstSavingFiveGoalkeeper()
    {
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot();
        RoomAITokenSnapshot goalkeeper = CreateToken(
            "Away",
            1,
            highPass: 3,
            heading: 0,
            dribbling: 2,
            shooting: 0);
        goalkeeper.isGoalKeeper = true;
        goalkeeper.saving = 5;
        snapshot.tokens.Add(goalkeeper);
        snapshot.tokens.Add(CreateToken(
            "Home",
            9,
            highPass: 2,
            heading: 3,
            dribbling: 2,
            shooting: 4,
            pace: 6));

        RoomAIMatchupTacticalProfile matchup = RoomAITacticalProfileBuilder.Build(snapshot);

        Assert.That(matchup.primaryShootingThreat.shooting, Is.EqualTo(4));
        Assert.That(matchup.minimumMaterialShootingThreat, Is.EqualTo(5));
        Assert.That(matchup.fastestScoringThreat, Is.Null);
        Assert.That(matchup.recommendedDefensiveLineAbsoluteX, Is.EqualTo(-1));
    }

    [Test]
    public void DefensivePhasePlan_PrefersCommittedAdjacentPressureOverStoppingShort()
    {
        RoomActionCandidate adjacent = CreateCandidate(RoomActionType.StartMovement);
        adjacent.step = RoomDecisionStep.ChooseTarget;
        RoomActionCandidate stopsShort = CreateCandidate(RoomActionType.StartMovement);
        stopsShort.step = RoomDecisionStep.ChooseTarget;
        RoomAIDecisionSnapshot snapshot = CreateDefendingSnapshot(adjacent, stopsShort);
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = adjacent,
            defensivePlanRole = "PrimaryPressure",
            defensivePlanIntent = "DenyShot",
            defensivePlanDestination = new RoomHexCoordinates { x = 8, z = 0 },
            targetHex = new RoomHexCoordinates { x = 8, z = 0 },
            targetDistanceToBall = 1,
            defensiveLegalAdjacencyAvailable = true,
            defensiveMoveEndsAdjacentToBall = true
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = stopsShort,
            defensivePlanRole = "PrimaryPressure",
            defensivePlanIntent = "DenyShot",
            defensivePlanDestination = new RoomHexCoordinates { x = 8, z = 0 },
            targetHex = new RoomHexCoordinates { x = 7, z = 0 },
            targetDistanceToBall = 2,
            defensiveLegalAdjacencyAvailable = true,
            defensiveMoveEndsAdjacentToBall = false
        });

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(adjacent));
        Assert.That(ranked[0].reasons, Has.Some.Contains("executes committed PrimaryPressure destination"));
        Assert.That(ranked[1].reasons, Has.Some.Contains("stops one hex short"));
    }

    [Test]
    public void CodexTactician_RejectsLowThreatFromBehindTackleAfterDribblerRepositionForecast()
    {
        RoomActionCandidate tackle = CreateCandidate(RoomActionType.Tackle);
        tackle.manager = nameof(MovementPhaseManager);
        tackle.step = RoomDecisionStep.InterruptionChoice;
        tackle.executionCommand = "tackle";
        RoomActionCandidate standGround = CreateCandidate(RoomActionType.Decline);
        standGround.manager = nameof(MovementPhaseManager);
        standGround.step = RoomDecisionStep.InterruptionChoice;
        standGround.executionCommand = "no_tackle";

        RoomAIDecisionSnapshot snapshot = CreateDefendingSnapshot(tackle, standGround);
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = tackle,
            isTackleDecision = true,
            tackleIsAttempted = true,
            tackleIsFromBehind = true,
            tackleSuccessProbability = 0.30f,
            tackleFoulProbability = 1f / 3f,
            tackleSendOffProbability = 2f / 9f,
            dribblerRetainsProbability = 0.37f,
            defensiveExpectedGoalsAgainst = 0.10f,
            dribblerBestRepositionExpectedGoals = 0.40f,
            dribblerBestRepositionProgress = 2,
            dribblerBestRepositionHex = new RoomHexCoordinates { x = 13, z = -1 }
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = standGround,
            isTackleDecision = true,
            tackleIsAttempted = false,
            defensiveExpectedGoalsAgainst = 0.10f
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.UltraHard);

        Assert.That(decision.selectedCandidate, Is.SameAs(standGround));
        Assert.That(decision.rankedCandidates[0].reasons, Has.Some.Contains("stand ground"));
        Assert.That(decision.rankedCandidates[1].reasons, Has.Some.Contains("best failed-tackle reposition"));
    }

    [Test]
    public void CodexTactician_SelectsSuccessfulTackleRepositionByPlannedNextActionValue()
    {
        RoomActionCandidate safePassSetup = CreateCandidate(RoomActionType.SetupMove);
        safePassSetup.manager = nameof(MovementPhaseManager);
        safePassSetup.step = RoomDecisionStep.ChooseTarget;
        RoomActionCandidate weakContinuation = CreateCandidate(RoomActionType.SetupMove);
        weakContinuation.manager = nameof(MovementPhaseManager);
        weakContinuation.step = RoomDecisionStep.ChooseTarget;

        RoomAIDecisionSnapshot snapshot = CreateDefendingSnapshot(weakContinuation, safePassSetup);
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = weakContinuation,
            isSuccessfulTackleReposition = true,
            plannedNextAction = "Movement",
            plannedNextActionValue = 20f,
            plannedNextActionTargetHex = new RoomHexCoordinates { x = 10, z = 0 }
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = safePassSetup,
            isSuccessfulTackleReposition = true,
            plannedNextAction = "StandardPass",
            plannedNextReceiverTokenKey = "Home:8",
            plannedNextActionValue = 180f,
            plannedNextActionTargetHex = new RoomHexCoordinates { x = 12, z = -2 },
            plannedNextActionInterceptors = 0
        });

        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights(),
            AIManager.CodexTacticianDifficulty.UltraHard);

        Assert.That(decision.selectedCandidate, Is.SameAs(safePassSetup));
        Assert.That(decision.selectedEvaluation.reasons, Has.Some.Contains("choose next action first: StandardPass"));
    }

    [Test]
    public void RankCandidates_RejectsHighPassAfterCompletedEvaluationFindsNoViableOutcome()
    {
        RoomActionCandidate highPass = CreateCandidate(RoomActionType.HighPass);
        RoomActionCandidate groundPass = CreateCandidate(RoomActionType.GroundPass);
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(highPass, groundPass);
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = highPass,
            highPassAccuracyProbability = 0.50f,
            highPassTargetEvaluationComplete = true,
            highPassTargetsEvaluated = 0
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = groundPass
        });

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            new RoomAIAttackPlan { planType = RoomAIAttackPlanType.ProgressPossession, team = "Away" },
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(groundPass));
        Assert.That(ranked[1].reasons, Has.Some.Contains("no viable collector/target outcome"));
    }

    [Test]
    public void PossessionLevelPlan_UsesContinuationValueInsteadOfGenericAerialBonuses()
    {
        RoomActionCandidate backwardHighPass = CreateCandidate(RoomActionType.HighPass);
        RoomActionCandidate progressiveGroundPass = CreateCandidate(RoomActionType.GroundPass);
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(backwardHighPass, progressiveGroundPass);
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = backwardHighPass,
            hasPossessionLevelPlan = true,
            possessionPlanValue = 40,
            possessionPlanRetentionProbability = 0.72f,
            possessionPlanReceiverSurvivalProbability = 0.48f,
            possessionPlanThreatExpectedGoals = 0.02f,
            possessionPlanNextAction = "Movement"
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = progressiveGroundPass,
            hasPossessionLevelPlan = true,
            possessionPlanValue = 90,
            possessionPlanRetentionProbability = 0.84f,
            possessionPlanReceiverSurvivalProbability = 0.76f,
            possessionPlanThreatExpectedGoals = 0.12f,
            possessionPlanNextAction = "Shot"
        });

        RoomAITacticalWeights weights = new RoomAITacticalWeights
        {
            highPassAction = 1000,
            groundPassAction = 0,
            wideAerialActionPlan = 1000
        };
        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            new RoomAIAttackPlan { planType = RoomAIAttackPlanType.WideAerialAttack, team = "Away" },
            weights);

        Assert.That(ranked[0].candidate, Is.SameAs(progressiveGroundPass));
        Assert.That(ranked[0].reasons, Has.Some.Contains("possession plan"));
        Assert.That(ranked[1].reasons, Has.None.Contains("matches wide aerial plan"));
    }

    [Test]
    public void PossessionLevelPlan_PenalizesStrongerTacklerAndValuesSupportRecovery()
    {
        RoomActionCandidate exposedReceiver = CreateCandidate(RoomActionType.GroundPass);
        RoomActionCandidate supportedReceiver = CreateCandidate(RoomActionType.GroundPass);
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(exposedReceiver, supportedReceiver);
        snapshot.homeTeamDirection = "LeftToRight";
        snapshot.awayTeamDirection = "RightToLeft";
        RoomAITokenSnapshot exposedTarget = CreateToken("Away", 9, 3, 3, 3, 4);
        exposedTarget.tokenKey = "Away:9";
        exposedTarget.currentHex = new RoomHexCoordinates { x = 0, z = 0 };
        RoomAITokenSnapshot supportedTarget = CreateToken("Away", 11, 3, 3, 5, 5);
        supportedTarget.tokenKey = "Away:11";
        supportedTarget.currentHex = new RoomHexCoordinates { x = 10, z = 0 };
        RoomAITokenSnapshot exactSupport = CreateToken("Away", 8, 3, 3, 4, 3);
        exactSupport.tokenKey = "Away:8";
        exactSupport.currentHex = new RoomHexCoordinates { x = 12, z = 0 };
        exactSupport.tackling = 5;
        RoomAITokenSnapshot exposedTackler = CreateToken("Home", 4, 2, 3, 3, 2);
        exposedTackler.tokenKey = "Home:4";
        exposedTackler.currentHex = new RoomHexCoordinates { x = 0, z = 1 };
        exposedTackler.tackling = 6;
        exposedTackler.pace = 3;
        RoomAITokenSnapshot supportedTackler = CreateToken("Home", 5, 2, 3, 3, 2);
        supportedTackler.tokenKey = "Home:5";
        supportedTackler.currentHex = new RoomHexCoordinates { x = 10, z = 1 };
        supportedTackler.tackling = 5;
        supportedTackler.pace = 3;
        snapshot.tokens.AddRange(new[]
        {
            exposedTarget,
            supportedTarget,
            exactSupport,
            exposedTackler,
            supportedTackler
        });
        RoomAICandidateExplanation exposed = snapshot.candidateExplanations[0];
        exposed.groundPassRetentionProbability = 0.8f;
        exposed.opponentsWithinTackleReach = 1;
        exposed.tackleToDribblingMismatch = 3;
        exposed.targetTokenKey = exposedTarget.tokenKey;
        exposed.targetTokenHex = exposedTarget.currentHex;
        RoomAICandidateExplanation supported = snapshot.candidateExplanations[1];
        supported.groundPassRetentionProbability = 0.8f;
        supported.opponentsWithinTackleReach = 1;
        supported.tackleToDribblingMismatch = -2;
        supported.targetTokenKey = supportedTarget.tokenKey;
        supported.targetTokenHex = supportedTarget.currentHex;

        RoomAIDecisionSnapshotBuilder.PopulatePossessionLevelPlanFacts(snapshot);
        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights());

        Assert.That(supported.possessionPlanReceiverSurvivalProbability,
            Is.GreaterThan(exposed.possessionPlanReceiverSurvivalProbability));
        Assert.That(supported.possessionPlanSupportBlocksTackleWinnerExit, Is.True);
        Assert.That(supported.possessionPlanLikelyTackleWinnerTokenKey, Is.EqualTo("Home:5"));
        Assert.That(supported.possessionPlanSupportTokenKey, Is.EqualTo("Away:8"));
        Assert.That(supported.possessionPlanSupportDestinationHex.x, Is.EqualTo(11));
        Assert.That(supported.possessionPlanRecoveryProbability, Is.GreaterThan(0f));
        Assert.That(ranked[0].candidate, Is.SameAs(supportedReceiver));
        Assert.That(ranked[0].reasons, Has.Some.Contains("support recovery"));
    }

    [Test]
    public void RankCandidates_KickoffPrefersSecureGroundPassThatCreatesNextMovementShot()
    {
        RoomActionCandidate highPass = CreateCandidate(RoomActionType.HighPass);
        RoomActionCandidate groundPass = CreateCandidate(RoomActionType.GroundPass);
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(highPass, groundPass);
        snapshot.currentState = "KickOff";
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = highPass,
            isRootActionPreview = true,
            isKickoffDecision = true,
            highPassTargetEvaluationComplete = true,
            highPassTargetsEvaluated = 8,
            highPassCollectorDistance = 2,
            highPassDefendersContesting = 1,
            highPassExpectedPossessionProbability = 0.44f,
            highPassTargetOutcomeValue = 120f
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = groundPass,
            isRootActionPreview = true,
            isKickoffDecision = true,
            targetToken = CreateToken("Away", 9, 3, 3, 5, 5),
            targetTokenKey = "Away:9",
            receiverCanReachShootingHexNextMovement = true,
            minimumStepsToShootingHex = 3,
            groundPassInterceptionCandidates = 0
        });

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            new RoomAIAttackPlan { planType = RoomAIAttackPlanType.CreateShot, team = "Away" },
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(groundPass));
        Assert.That(ranked[0].reasons, Has.Some.Contains("secure kickoff pass"));
    }

    [Test]
    public void RankCandidates_GoalkeeperStaysWhenTwoDefendersCoverAndRushExposesOtherThreats()
    {
        RoomActionCandidate stay = CreateCandidate(RoomActionType.StartMovement);
        RoomActionCandidate rush = CreateCandidate(RoomActionType.StartMovement);
        RoomAIDecisionSnapshot snapshot = CreateDefendingSnapshot(stay, rush);
        snapshot.candidateExplanations.Clear();
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = stay,
            isGoalkeeperPositioning = true,
            goalkeeperPositioningCurrentMaximumExpectedGoals = 0.25f,
            goalkeeperPositioningMaximumExpectedGoals = 0.25f,
            goalkeeperPositioningCurrentOutfieldBlockerCount = 2,
            goalkeeperPositioningOffBallThreatCount = 1,
            goalkeeperOwnGoalDistanceDelta = 0
        });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation
        {
            candidate = rush,
            isGoalkeeperPositioning = true,
            goalkeeperPositioningCurrentMaximumExpectedGoals = 0.25f,
            goalkeeperPositioningMaximumExpectedGoals = 0.30f,
            goalkeeperPositioningCurrentOutfieldBlockerCount = 2,
            goalkeeperPositioningOffBallThreatCount = 1,
            goalkeeperOwnGoalDistanceDelta = 2
        });

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(stay));
        Assert.That(ranked[1].reasons, Has.Some.Contains("two or more outfield blockers"));
    }

    [Test]
    public void LongBallGoalkeeperRecovery_ClaimsWhenFreePaceWinsRace()
    {
        RoomActionCandidate claim = CreateCandidate(RoomActionType.LongBall);
        claim.manager = nameof(LongBallManager);
        RoomActionCandidate decline = CreateCandidate(RoomActionType.Decline);
        decline.manager = nameof(LongBallManager);
        decline.isForfeit = true;
        RoomAIDecisionSnapshot snapshot = CreateDefendingSnapshot(claim, decline);
        foreach (RoomAICandidateExplanation explanation in snapshot.candidateExplanations)
        {
            explanation.isLongBallGoalkeeperRecovery = true;
            explanation.longBallRecoveryMode = "ClaimLooseBall";
            explanation.longBallGoalkeeperMovementPhases = 0;
            explanation.longBallGoalkeeperFreePace = 4;
            explanation.longBallGoalkeeperRemainingMovementPhasesAfterFreeMove = 0;
            explanation.longBallGoalkeeperCanReachBallWithFreeMove = true;
            explanation.longBallAttackerMovementPhases = 1;
        }

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(claim));
        Assert.That(ranked[0].reasons, Has.Some.Contains("immediate free-Pace"));
    }

    [Test]
    public void LongBallGoalkeeperRecovery_SupportsRecoveringDefenderFromSixOrSevenHexes()
    {
        RoomActionCandidate supported = CreateCandidate(RoomActionType.SetupMove);
        supported.manager = nameof(LongBallManager);
        RoomActionCandidate crowding = CreateCandidate(RoomActionType.SetupMove);
        crowding.manager = nameof(LongBallManager);
        RoomAIDecisionSnapshot snapshot = CreateDefendingSnapshot(supported, crowding);
        RoomAICandidateExplanation supportedExplanation = snapshot.candidateExplanations[0];
        supportedExplanation.isLongBallGoalkeeperRecovery = true;
        supportedExplanation.longBallRecoveryMode = "SupportRecovery";
        supportedExplanation.longBallRecoveringDefenderTokenKey = "Home:4";
        supportedExplanation.longBallGoalkeeperSupportDistance = 7;
        RoomAICandidateExplanation crowdingExplanation = snapshot.candidateExplanations[1];
        crowdingExplanation.isLongBallGoalkeeperRecovery = true;
        crowdingExplanation.longBallRecoveryMode = "SupportRecovery";
        crowdingExplanation.longBallRecoveringDefenderTokenKey = "Home:4";
        crowdingExplanation.longBallGoalkeeperSupportDistance = 1;

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(supported));
        Assert.That(ranked[0].reasons, Has.Some.Contains("7 hex(es) away"));
    }

    [Test]
    public void LongBallGoalkeeperRecovery_PreservesFarPostCoverageInsteadOfBlindRush()
    {
        RoomActionCandidate covered = CreateCandidate(RoomActionType.SetupMove);
        covered.manager = nameof(LongBallManager);
        RoomActionCandidate exposed = CreateCandidate(RoomActionType.SetupMove);
        exposed.manager = nameof(LongBallManager);
        RoomAIDecisionSnapshot snapshot = CreateDefendingSnapshot(covered, exposed);
        RoomAICandidateExplanation coveredExplanation = snapshot.candidateExplanations[0];
        coveredExplanation.isLongBallGoalkeeperRecovery = true;
        coveredExplanation.longBallRecoveryMode = "CoverGoalAndFarPost";
        coveredExplanation.longBallFarPostPassThreat = true;
        coveredExplanation.goalkeeperWallShotTargetCount = 4;
        coveredExplanation.goalkeeperWallCoveredShotTargets = 4;
        coveredExplanation.goalkeeperWallCoverageQuality = 10;
        RoomAICandidateExplanation exposedExplanation = snapshot.candidateExplanations[1];
        exposedExplanation.isLongBallGoalkeeperRecovery = true;
        exposedExplanation.longBallRecoveryMode = "CoverGoalAndFarPost";
        exposedExplanation.longBallFarPostPassThreat = true;
        exposedExplanation.goalkeeperWallShotTargetCount = 4;
        exposedExplanation.goalkeeperWallCoveredShotTargets = 1;
        exposedExplanation.goalkeeperWallCoverageQuality = 2;
        exposedExplanation.goalkeeperOwnGoalDistanceDelta = 2;

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(covered));
        Assert.That(ranked[0].reasons, Has.Some.Contains("far-post option"));
    }

    [Test]
    public void FirstTimePassExtraPace_RanksShotUnlockAboveUnproductiveSetupMove()
    {
        RoomActionCandidate shotUnlock = CreateCandidate(RoomActionType.SetupMove);
        shotUnlock.manager = nameof(FirstTimePassManager);
        shotUnlock.step = RoomDecisionStep.ChooseTarget;
        RoomActionCandidate ordinaryMove = CreateCandidate(RoomActionType.SetupMove);
        ordinaryMove.manager = nameof(FirstTimePassManager);
        ordinaryMove.step = RoomDecisionStep.ChooseTarget;
        RoomAIDecisionSnapshot snapshot = CreateAttackingSnapshot(shotUnlock, ordinaryMove);
        RoomAICandidateExplanation unlock = snapshot.candidateExplanations[0];
        unlock.isFirstTimePassShotSetup = true;
        unlock.firstTimePassExtraPaceUnlocksShot = true;
        unlock.firstTimePassSetupShooterTokenKey = "Away:11";
        unlock.firstTimePassSetupShooterPace = 3;
        unlock.firstTimePassSetupShooterShooting = 6;
        unlock.firstTimePassShotDistanceBefore = 4;
        unlock.firstTimePassShotDistanceAfter = 3;
        unlock.firstTimePassPassRetentionProbability = 0.75f;
        unlock.firstTimePassReachShotProbability = 1f;
        unlock.firstTimePassRawShotExpectedGoals = 0.24f;
        unlock.firstTimePassShotRouteExpectedGoals = 0.24f;
        unlock.firstTimePassCombinedExpectedGoals = 0.18f;

        List<RoomAITacticalEvaluation> ranked = RoomAITacticalPlanner.RankCandidates(
            snapshot,
            snapshot.legalActions,
            null,
            new RoomAITacticalWeights());

        Assert.That(ranked[0].candidate, Is.SameAs(shotUnlock));
        Assert.That(ranked[0].tier, Is.EqualTo(RoomAITacticalTier.CreateFinish));
        Assert.That(ranked[0].reasons, Has.Some.Contains("free attacker hex"));
        Assert.That(ranked[0].reasons, Has.Some.Contains("combined xG"));
    }

    [Test]
    public void MovementDestinationWithActorMetadata_IsExecutedAsHexNotTokenSelection()
    {
        GameObject actorObject = new GameObject("Movement actor metadata test");
        GameObject hexObject = new GameObject("Movement destination metadata test", typeof(MeshRenderer));
        hexObject.SetActive(false); // Metadata only: do not run HexCell's rendering lifecycle in Edit Mode.
        try
        {
            RoomActionCandidate destination = new RoomActionCandidate
            {
                manager = nameof(MovementPhaseManager),
                actionType = RoomActionType.StartMovement,
                step = RoomDecisionStep.ChooseTarget,
                actor = actorObject.AddComponent<PlayerToken>(),
                targetHex = hexObject.AddComponent<HexCell>(),
                isExecutableNow = true
            };
            RoomDecisionContext context = new RoomDecisionContext();
            context.AddAction(destination);

            Assert.That(
                InvokePrivateStaticBool(typeof(RoomDecisionManager), "IsExecutableHexCandidate", destination),
                Is.True);
            Assert.That(
                InvokePrivateStaticBool(typeof(RoomDecisionManager), "IsExecutableTokenCandidate", destination),
                Is.False);
            Assert.That(
                InvokePrivateStaticBool(typeof(RoomDecisionManager), "IsDownstreamCommittedBranchCandidate", destination),
                Is.True);
            Assert.That(context.validTokens.Contains(destination.actor), Is.False);
            Assert.That(context.validHexes.Contains(destination.targetHex), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(hexObject);
            Object.DestroyImmediate(actorObject);
        }
    }

    [Test]
    public void StagedDecisionCandidateIdentity_UsesStableTeamAndJerseyKey()
    {
        GameObject firstObject = new GameObject("Committed actor first name");
        GameObject renamedObject = new GameObject("Committed actor renamed");
        try
        {
            PlayerToken first = firstObject.AddComponent<PlayerToken>();
            first.isHomeTeam = false;
            first.jerseyNumber = 9;
            first.playerName = "Original Name";
            PlayerToken renamed = renamedObject.AddComponent<PlayerToken>();
            renamed.isHomeTeam = false;
            renamed.jerseyNumber = 9;
            renamed.playerName = "Changed Display Name";
            RoomActionCandidate firstCandidate = CreateCandidate(RoomActionType.HighPass);
            firstCandidate.actor = first;
            RoomActionCandidate renamedCandidate = CreateCandidate(RoomActionType.HighPass);
            renamedCandidate.actor = renamed;

            string firstIdentity = InvokePrivateStaticString(
                typeof(RoomDecisionManager),
                "BuildCandidateIdentity",
                firstCandidate);
            string renamedIdentity = InvokePrivateStaticString(
                typeof(RoomDecisionManager),
                "BuildCandidateIdentity",
                renamedCandidate);

            Assert.That(firstIdentity, Is.EqualTo(renamedIdentity));
            Assert.That(firstIdentity, Does.Contain("Away:9"));
            Assert.That(firstIdentity, Does.Not.Contain("Original Name"));
        }
        finally
        {
            Object.DestroyImmediate(renamedObject);
            Object.DestroyImmediate(firstObject);
        }
    }

    [Test]
    public void CommittedPassTargetOverlay_CoversEveryMovementAndResolutionStage()
    {
        GameObject ftpObject = new GameObject("FTP overlay lifecycle test");
        GameObject highPassObject = new GameObject("HP overlay lifecycle test");
        GameObject hexObject = new GameObject("Committed orange target", typeof(MeshRenderer));
        hexObject.SetActive(false); // Overlay state only; no rendered pitch or runtime material is needed.
        try
        {
            HexCell target = hexObject.AddComponent<HexCell>();
            FirstTimePassManager ftp = ftpObject.AddComponent<FirstTimePassManager>();
            ftp.currentTargetHex = target;
            ftp.isActivated = true;
            foreach (string stage in new[]
            {
                nameof(ftp.isWaitingForAttackerSelection),
                nameof(ftp.isWaitingForAttackerMove),
                nameof(ftp.isWaitingForDefenderSelection),
                nameof(ftp.isWaitingForDefenderMove),
                nameof(ftp.isWaitingForDiceRoll)
            })
            {
                SetPublicBoolFieldsFalse(ftp);
                typeof(FirstTimePassManager).GetField(stage).SetValue(ftp, true);
                Assert.That(InvokePrivateInstanceBool(ftp, "ShouldShowCommittedTargetOverlay"), Is.True, stage);
            }

            HighPassManager highPass = highPassObject.AddComponent<HighPassManager>();
            highPass.currentTargetHex = target;
            highPass.isActivated = true;
            foreach (string stage in new[]
            {
                nameof(highPass.isWaitingForAttackerSelection),
                nameof(highPass.isWaitingForAttackerMove),
                nameof(highPass.isWaitingForDefenderSelection),
                nameof(highPass.isWaitingForDefenderMove),
                nameof(highPass.isWaitingForAccuracyRoll),
                nameof(highPass.isWaitingForDefGKChallengeDecision)
            })
            {
                SetPublicBoolFieldsFalse(highPass);
                typeof(HighPassManager).GetField(stage).SetValue(highPass, true);
                Assert.That(InvokePrivateInstanceBool(highPass, "ShouldShowCommittedTargetOverlay"), Is.True, stage);
            }
        }
        finally
        {
            Object.DestroyImmediate(hexObject);
            Object.DestroyImmediate(highPassObject);
            Object.DestroyImmediate(ftpObject);
        }
    }

    private static RoomAIDecisionSnapshot CreateAttackingSnapshot(params RoomActionCandidate[] candidates)
    {
        RoomAIDecisionSnapshot snapshot = new RoomAIDecisionSnapshot
        {
            expectedTeam = "Away",
            teamInAttack = "Away",
            attackHasPossession = true,
            currentState = "EndOfMovementPhase"
        };

        foreach (RoomActionCandidate candidate in candidates)
        {
            snapshot.legalActions.Add(candidate);
            snapshot.candidateExplanations.Add(new RoomAICandidateExplanation { candidate = candidate });
        }

        return snapshot;
    }

    private static RoomAIDecisionSnapshot CreateDefendingSnapshot(params RoomActionCandidate[] candidates)
    {
        RoomAIDecisionSnapshot snapshot = new RoomAIDecisionSnapshot
        {
            expectedTeam = "Home",
            teamInAttack = "Away",
            attackHasPossession = true,
            currentState = "MovementPhase"
        };

        foreach (RoomActionCandidate candidate in candidates)
        {
            snapshot.legalActions.Add(candidate);
            snapshot.candidateExplanations.Add(new RoomAICandidateExplanation { candidate = candidate });
        }

        return snapshot;
    }

    private static RoomActionCandidate CreateCandidate(RoomActionType actionType)
    {
        return new RoomActionCandidate
        {
            actionType = actionType,
            step = RoomDecisionStep.ChooseActionType,
            isExecutableNow = true
        };
    }

    private static RoomAITokenSnapshot CreateToken(
        string team,
        int jerseyNumber,
        int highPass,
        int heading,
        int dribbling,
        int shooting,
        int pace = 4)
    {
        return new RoomAITokenSnapshot
        {
            teamSide = team,
            jerseyNumber = jerseyNumber,
            isPlaying = true,
            highPass = highPass,
            heading = heading,
            dribbling = dribbling,
            shooting = shooting,
            pace = pace
        };
    }

    private static int ReadPrivateInt(RoomDecisionManager manager, string fieldName)
    {
        FieldInfo field = typeof(RoomDecisionManager).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing private field {fieldName}.");
        return (int)field.GetValue(manager);
    }

    private static bool InvokePrivateStaticBool(
        System.Type type,
        string methodName,
        RoomActionCandidate candidate)
    {
        MethodInfo method = type.GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Missing private method {methodName}.");
        return (bool)method.Invoke(null, new object[] { candidate });
    }

    private static string InvokePrivateStaticString(
        System.Type type,
        string methodName,
        RoomActionCandidate candidate)
    {
        MethodInfo method = type.GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Missing private method {methodName}.");
        return (string)method.Invoke(null, new object[] { candidate });
    }

    private static bool InvokePrivateInstanceBool(object instance, string methodName)
    {
        MethodInfo method = instance.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Missing private method {methodName}.");
        return (bool)method.Invoke(instance, null);
    }

    private static void SetPublicBoolFieldsFalse(object instance)
    {
        foreach (FieldInfo field in instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            if (field.FieldType == typeof(bool) && field.Name.StartsWith("isWaiting", System.StringComparison.Ordinal))
            {
                field.SetValue(instance, false);
            }
        }
    }
}
