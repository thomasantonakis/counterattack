using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class Thomas6MatchReviewEditorTests
{
    private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
    private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

    [TestCase(true, false)]
    [TestCase(false, false)]
    [TestCase(true, true)]
    [TestCase(false, true)]
    public void SharedKickoffPromptReachesUnconfirmedCpuButLeavesHumanTurnAlone(bool cpuHome, bool postGoal)
    {
        var previousMatch = MatchManager.Instance;
        var root = new GameObject("Shared kickoff routing"); root.SetActive(false);
        try
        {
            var match = root.AddComponent<MatchManager>();
            MatchManager.Instance = match;
            var kickoff = root.AddComponent<KickoffManager>();
            match.kickoffManager = kickoff;
            if (postGoal) kickoff.StartPostGoalKickoffSetupPhase();
            else kickoff.StartPreKickoffPhase();
            var settings = new MatchManager.GameSettings
            {
                defaultRoomPersona = "Ask Human",
                homeRoomPersona = cpuHome ? "Codex Tactician Ultra Hard" : "Ask Human",
                awayRoomPersona = cpuHome ? "Ask Human" : "Codex Tactician Ultra Hard"
            };
            var instruction = new GameplayInstructionSnapshot
            {
                manager = nameof(KickoffManager), expectedTeam = "Neutral", isAwaitingInput = true,
                instructionText = kickoff.GetInstructions()
            };
            Assert.That(RoomDecisionManager.ResolveDecisionPersona(settings, instruction),
                Is.EqualTo(AIManager.RoomPersona.CodexTactician));

            // A human confirmation must not strand the remaining CPU at 1/2.
            typeof(KickoffManager).GetField(cpuHome ? "awaySetupConfirmed" : "homeSetupConfirmed", InstancePrivate)
                .SetValue(kickoff, true);
            Assert.That(RoomDecisionManager.ResolveDecisionPersona(settings, instruction),
                Is.EqualTo(AIManager.RoomPersona.CodexTactician));

            typeof(KickoffManager).GetField(cpuHome ? "homeSetupConfirmed" : "awaySetupConfirmed", InstancePrivate)
                .SetValue(kickoff, true);
            Assert.That(RoomDecisionManager.ResolveDecisionPersona(settings, instruction),
                Is.EqualTo(AIManager.RoomPersona.AskHuman));

            instruction.expectedTeam = cpuHome ? "Away" : "Home";
            instruction.manager = nameof(MovementPhaseManager);
            Assert.That(RoomDecisionManager.ResolveDecisionPersona(settings, instruction),
                Is.EqualTo(AIManager.RoomPersona.AskHuman));
        }
        finally { MatchManager.Instance = previousMatch; Object.DestroyImmediate(root); }
    }

    [Test]
    public void SelectedSnapshotBlockerGetsADestinationBeforeAnotherActor()
    {
        var root = new GameObject("Snapshot stage"); root.SetActive(false);
        try
        {
            var manager = root.AddComponent<RoomDecisionManager>();
            var cell = root.AddComponent<HexCell>();
            var playerObject = new GameObject("Alternative blocker"); playerObject.transform.SetParent(root.transform);
            var player = playerObject.AddComponent<PlayerToken>(); player.SetCurrentHex(cell);
            var context = new RoomDecisionContext { manager = nameof(ShotManager) };
            context.AddHexActionCandidate(nameof(ShotManager), RoomActionType.Block, RoomDecisionStep.ChooseTarget, cell, "Complete blocker move");
            context.AddTokenActionCandidate(nameof(ShotManager), RoomActionType.Block, RoomDecisionStep.ChooseActor, player, "Switch blocker");
            var result = (List<RoomActionCandidate>)typeof(RoomDecisionManager)
                .GetMethod("BuildExecutableCandidatesCore", InstancePrivate)
                .Invoke(manager, new object[] { new GameplayInstructionSnapshot { manager = nameof(ShotManager) }, context });
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].step, Is.EqualTo(RoomDecisionStep.ChooseTarget));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void FreeKickKeeperSelectionIsNotHiddenBySkip()
    {
        var root = new GameObject("Free kick keeper choices"); root.SetActive(false);
        try
        {
            var player = root.AddComponent<PlayerToken>();
            typeof(PlayerToken).GetField("_isGk", InstancePrivate).SetValue(player, true);
            var context = new RoomDecisionContext { manager = nameof(FreeKickManager) };
            context.AddTokenActionCandidate(nameof(FreeKickManager), RoomActionType.FreeKick, RoomDecisionStep.ChooseActor, player, "Select keeper");
            context.AddKeyActionCandidate(nameof(FreeKickManager), RoomActionType.FreeKick, RoomDecisionStep.InterruptionChoice,
                "X", "Keep keeper", executionCommand: "goalkeeper_reposition_skip");
            var result = (List<RoomActionCandidate>)typeof(RoomDecisionManager)
                .GetMethod("BuildFreeKickExecutableCandidates", StaticPrivate).Invoke(null, new object[] { context });
            Assert.That(result.Any(action => action.actor == player), Is.True);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void CollectKeyIsNotDiscardedWhenMovementDestinationsExist()
    {
        var root = new GameObject("Pickup choice"); root.SetActive(false);
        try
        {
            var hex = root.AddComponent<HexCell>();
            var context = new RoomDecisionContext { manager = nameof(MovementPhaseManager) };
            context.AddKeyActionCandidate(nameof(MovementPhaseManager), RoomActionType.StartMovement,
                RoomDecisionStep.InterruptionChoice, "V", "Collect ball");
            context.AddHexActionCandidate(nameof(MovementPhaseManager), RoomActionType.StartMovement,
                RoomDecisionStep.ChooseTarget, hex, "Move elsewhere");
            var result = (List<RoomActionCandidate>)typeof(RoomDecisionManager)
                .GetMethod("BuildMovementPhaseExecutableCandidates", StaticPrivate).Invoke(null, new object[] { context });
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].key, Is.EqualTo("V"));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void EmptyManagerContextDoesNotInventInstructionFallbackKeys()
    {
        var instruction = new GameplayInstructionSnapshot { manager = nameof(FreeKickManager), expectedKeys = new() { "X" } };
        var context = new RoomDecisionContext { manager = nameof(FreeKickManager) };
        var result = (List<RoomActionCandidate>)typeof(RoomDecisionManager)
            .GetMethod("BuildExecutableKeyCandidates", StaticPrivate).Invoke(null, new object[] { instruction, context });
        Assert.That(result, Is.Empty, "Animating managers deliberately expose no executable inputs.");
    }

    [Test]
    public void FreeKickSkipCannotAdvanceAnAnimatingMove()
    {
        var root = new GameObject("Free kick animation ownership"); root.SetActive(false);
        try
        {
            var manager = root.AddComponent<FreeKickManager>();
            typeof(FreeKickManager).GetField("isMovingSetupToken", InstancePrivate).SetValue(manager, true);
            typeof(FreeKickManager).GetField("movesUsed", InstancePrivate).SetValue(manager, 0);
            typeof(FreeKickManager).GetMethod("AttemptToAdvanceToNextPhase", InstancePrivate).Invoke(manager, null);
            Assert.That(typeof(FreeKickManager).GetField("movesUsed", InstancePrivate).GetValue(manager), Is.EqualTo(0));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [TestCase(true)]
    [TestCase(false)]
    public void InputOwnershipSurvivesAConsumerChangingTheTurn(bool home)
    {
        var input = new KeyPressData(KeyCode.X, false, false, false);
        typeof(KeyPressData).GetField("expectedByHomeTeam", InstancePrivate).SetValue(input, home);
        input.Consume(nameof(FreeKickManager), !home);
        Assert.That(input.consumedByHomeTeam, Is.EqualTo(home));
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void BoxThreatOutranksPursuitOfADeepLowScoringDefender(int mirror)
    {
        RoomHexCoordinates Hex(int x, int z) => new() { x = x * mirror, z = z };
        var defender = new RoomAITokenSnapshot { tokenKey = "Away:5", teamSide = "Away", jerseyNumber = 5,
            isPlaying = true, pace = 5, tackling = 5, heading = 6, currentHex = Hex(8, -3) };
        var striker = new RoomAITokenSnapshot { tokenKey = "Home:9", teamSide = "Home", jerseyNumber = 9,
            isPlaying = true, pace = 4, shooting = 6, heading = 5, currentHex = Hex(12, 0) };
        var irrelevant = new RoomAITokenSnapshot { tokenKey = "Home:4", teamSide = "Home", jerseyNumber = 4,
            isPlaying = true, pace = 4, shooting = 1, heading = 3, currentHex = Hex(-10, 0) };
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Away", teamInAttack = "Home", attackHasPossession = true,
            awayTeamDirection = mirror == 1 ? "RightToLeft" : "LeftToRight", ball = Hex(10, 3), defensiveMovesRemaining = 1 };
        snapshot.tokens.AddRange(new[] { defender, striker, irrelevant });
        var recover = Hex(12, 1);
        var reachable = new Dictionary<string, List<RoomHexCoordinates>>
            { [defender.tokenKey] = new() { defender.currentHex, recover, Hex(3, 0) } };
        var plan = RoomAITacticalSafety.AssignDefense(snapshot, reachable);
        Assert.That(plan.assignments.Single().destination, Is.SameAs(recover));
        Assert.That(plan.remainingThreatEstimate, Is.LessThan(plan.initialThreatEstimate));
    }

    [Test]
    public void BallHoldingTheOffsideLineKeepsTheReceiverLegalAfterDefenderStepsUp()
    {
        var keeper = new RoomAITokenSnapshot { tokenKey = "Away:1", teamSide = "Away", isPlaying = true,
            isGoalKeeper = true, currentHex = new() { x = 16, z = 0 } };
        var defender = new RoomAITokenSnapshot { tokenKey = "Away:4", teamSide = "Away", isPlaying = true,
            jerseyNumber = 4, currentHex = new() { x = 13, z = 0 } };
        var striker = new RoomAITokenSnapshot { tokenKey = "Home:9", teamSide = "Home", isPlaying = true,
            currentHex = new() { x = 13, z = 2 } };
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Away", teamInAttack = "Home",
            awayTeamDirection = "RightToLeft", ball = new() { x = 14, z = 0 } };
        snapshot.tokens.AddRange(new[] { keeper, defender, striker });
        var facts = new RoomAICandidateExplanation { actorTokenKey = defender.tokenKey, actorToken = defender,
            actorHex = defender.currentHex, targetHex = new() { x = 11, z = 0 } };
        RoomAITacticalSafety.EvaluateDefensiveLine(snapshot, facts);
        Assert.That(facts.opponentsMadeOffside, Is.Zero);
    }

    [Test]
    public void KnownShotRollIsConditionedRatherThanRolledAgainInKeeperEvaluation()
    {
        var root = new GameObject("Known shot roll"); root.SetActive(false);
        try
        {
            var shooter = root.AddComponent<PlayerToken>(); shooter.shooting = 6;
            Assert.That(ExpectedStatsCalculator.CalculateShotGoalProbability(shooter, 0, null, knownShooterRoll: 1), Is.Zero);
            Assert.That(ExpectedStatsCalculator.CalculateShotGoalProbability(shooter, 0, null, knownShooterRoll: 2), Is.EqualTo(1f));
            Assert.That(ExpectedStatsCalculator.CalculateShotGoalProbability(shooter, 0, null), Is.LessThan(1f));
        }
        finally { Object.DestroyImmediate(root); }
    }
}
