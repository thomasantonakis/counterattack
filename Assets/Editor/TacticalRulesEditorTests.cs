using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class TacticalRulesEditorTests
{
    private static RoomHexCoordinates Hex(int x, int z = 0) => new() { x = x, z = z };
    private static RoomAIDecisionSnapshot Defense(int mirror, string state = "MovementPhase") => new()
    {
        expectedTeam = "Away", teamInAttack = "Home", attackHasPossession = true,
        awayTeamDirection = mirror == 1 ? "RightToLeft" : "LeftToRight",
        homeTeamDirection = mirror == 1 ? "LeftToRight" : "RightToLeft",
        currentState = state, ball = Hex(12 * mirror), defensiveMovesRemaining = 1
    };

    private static RoomAITacticalEvaluation Keeper(int x, RoomDecisionStep step = RoomDecisionStep.ChooseTarget, int z = 0)
    {
        var candidate = new RoomActionCandidate { manager = nameof(FreeKickManager),
            actionType = RoomActionType.SetupMove, step = step };
        return new RoomAITacticalEvaluation
        {
            candidate = candidate,
            explanation = new RoomAICandidateExplanation { candidate = candidate,
                actorToken = new RoomAITokenSnapshot { isGoalKeeper = true }, actorHex = Hex(x, z),
                targetHex = step == RoomDecisionStep.ChooseTarget ? Hex(x, z) : null }
        };
    }

    private static RoomAIProviderDecision Decide(RoomAIDecisionSnapshot snapshot,
        IEnumerable<RoomAITacticalEvaluation> choices, AIManager.CodexTacticianDifficulty difficulty)
    {
        var old = MatchManager.Instance;
        MatchManager.Instance = null;
        try
        {
            foreach (var choice in choices)
            {
                snapshot.legalActions.Add(choice.candidate);
                snapshot.candidateExplanations.Add(choice.explanation);
            }
            return CodexTacticianProvider.Instance.Decide(snapshot, snapshot.legalActions, null,
                new RoomAITacticalWeights(), difficulty);
        }
        finally { MatchManager.Instance = old; }
    }

    [TestCase(AIManager.CodexTacticianDifficulty.Easy)]
    [TestCase(AIManager.CodexTacticianDifficulty.Medium)]
    [TestCase(AIManager.CodexTacticianDifficulty.Hard)]
    [TestCase(AIManager.CodexTacticianDifficulty.UltraHard)]
    public void ProviderCannotSelectTheKeeperDuringTheOrdinarySetPieceSequence(AIManager.CodexTacticianDifficulty difficulty)
    {
        var keeper = Keeper(18, RoomDecisionStep.ChooseActor);
        var skip = new RoomAITacticalEvaluation { candidate = new RoomActionCandidate {
            manager = nameof(FreeKickManager), actionType = RoomActionType.FreeKick,
            step = RoomDecisionStep.InterruptionChoice, key = "X" } };
        skip.explanation = new RoomAICandidateExplanation { candidate = skip.candidate };
        var result = Decide(Defense(1, "FreeKickDef2"), new[] { keeper, skip }, difficulty);
        Assert.That(result.selectedCandidate, Is.SameAs(skip.candidate));
        Assert.That(result.rankedCandidates.Any(e => e.candidate == keeper.candidate), Is.False);
    }

    [Test]
    public void ProviderReevaluatesTheLaterKeeperMoveInsideTheRadius()
    {
        var central = Keeper(18);
        var inside = Keeper(16);
        var outside = Keeper(14);
        foreach (var (choice, xg) in new[] { (central, .4f), (inside, .2f), (outside, .01f) })
        {
            choice.explanation.isGoalkeeperPositioning = true;
            choice.explanation.goalkeeperPositioningMaximumExpectedGoals = xg;
        }
        var result = Decide(Defense(1, "FreeKickDefGK2"), new[] { central, inside, outside },
            AIManager.CodexTacticianDifficulty.UltraHard);
        Assert.That(result.selectedCandidate, Is.SameAs(inside.candidate));
        Assert.That(result.rankedCandidates.Any(e => e.candidate == outside.candidate), Is.False);
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void FirstFreeKickKeeperMoveReturnsToExactCenterRatherThanBetterWallEstimate(int mirror)
    {
        var central = Keeper(18 * mirror);
        var wide = Keeper(16 * mirror, z: 3);
        central.safetyPriority = 4; // A wall-only xG forecast favored the wide cell.
        var choices = new List<RoomAITacticalEvaluation> { wide, central, Keeper(14 * mirror, RoomDecisionStep.InterruptionChoice),
            Keeper(14 * mirror, RoomDecisionStep.ChooseActor) };
        RoomAITacticalSafety.ConstrainGoalkeeperChoices(Defense(mirror, "FreeKickDefGK1"), choices);
        Assert.That(choices, Is.EqualTo(new[] { central }));
        Assert.That(central.safetyPriority, Is.Zero);
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void FirstKeeperMoveUsesNearestLegalCellWhenCenterIsUnavailable(int mirror)
    {
        var nearest = Keeper(17 * mirror);
        var choices = new List<RoomAITacticalEvaluation> { nearest, Keeper(14 * mirror), Keeper(12 * mirror, RoomDecisionStep.InterruptionChoice) };
        RoomAITacticalSafety.ConstrainGoalkeeperChoices(Defense(mirror, "FreeKickDefGK1"), choices);
        Assert.That(choices, Is.EqualTo(new[] { nearest }));
        Assert.That(nearest.reasons, Has.Some.Contains("unavailable"));
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void CenteredKeeperStaysWithoutReSelectingItself(int mirror)
    {
        var stay = Keeper(18 * mirror, RoomDecisionStep.InterruptionChoice);
        var choices = new List<RoomAITacticalEvaluation> { stay, Keeper(18 * mirror, RoomDecisionStep.ChooseActor) };
        RoomAITacticalSafety.ConstrainGoalkeeperChoices(Defense(mirror, "FreeKickDefGK1"), choices);
        Assert.That(choices, Is.EqualTo(new[] { stay }));
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void SubsequentKeeperMoveKeepsAllInRadiusOptionsForThreatReevaluation(int mirror)
    {
        var central = Keeper(18 * mirror);
        var inside = Keeper(16 * mirror);
        inside.score = 123;
        var boundary = Keeper(15 * mirror);
        foreach (var (choice, xg) in new[] { (central, .2f), (inside, .1f), (boundary, .3f) })
        {
            choice.explanation.isGoalkeeperPositioning = true;
            choice.explanation.goalkeeperPositioningMaximumExpectedGoals = xg;
        }
        var choices = new List<RoomAITacticalEvaluation> { central, inside, boundary, Keeper(14 * mirror),
            Keeper(10 * mirror, RoomDecisionStep.InterruptionChoice), Keeper(10 * mirror, RoomDecisionStep.ChooseActor) };
        RoomAITacticalSafety.ConstrainGoalkeeperChoices(Defense(mirror, "FreeKickDefGK2"), choices);
        Assert.That(choices, Is.EqualTo(new[] { central, inside, boundary }));
        Assert.That(inside.score, Is.EqualTo(123), "Legal positions keep their evaluated threat scores.");
        Assert.That(inside.safetyPriority, Is.LessThan(central.safetyPriority), "Re-evaluate the strongest threat within the permitted area.");
    }

    [TestCase("FreeKickDef1")]
    [TestCase("FreeKickDef2")]
    [TestCase("FreeKickDef3")]
    [TestCase("FreeKickDefMovement3")]
    public void DedicatedKeeperMovesExcludeOrdinaryKeeperActorsAndDestinations(string state)
    {
        var defender = new RoomAITacticalEvaluation { candidate = new RoomActionCandidate { manager = nameof(FreeKickManager) },
            explanation = new RoomAICandidateExplanation { actorToken = new RoomAITokenSnapshot() } };
        var choices = new List<RoomAITacticalEvaluation> { Keeper(18, RoomDecisionStep.ChooseActor), Keeper(15), defender };
        RoomAITacticalSafety.ConstrainGoalkeeperChoices(Defense(1, state), choices);
        Assert.That(choices, Is.EqualTo(new[] { defender }));
    }

    [Test]
    public void LaterKeeperCannotUseAnOutsideDestinationWhenTheCentralAreaIsBlocked()
    {
        var stay = Keeper(12, RoomDecisionStep.InterruptionChoice);
        var choices = new List<RoomAITacticalEvaluation> { stay, Keeper(13), Keeper(14) };
        RoomAITacticalSafety.ConstrainGoalkeeperChoices(Defense(1, "FreeKickDefGK2"), choices);
        Assert.That(choices, Is.EqualTo(new[] { stay }));
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void OptionalKeeperExcursionNeedsAMaterialSaveOrCollection(int mirror)
    {
        var wandering = Keeper(12 * mirror);
        wandering.candidate.actionType = RoomActionType.StartMovement;
        wandering.explanation.actorHex = Hex(16 * mirror);
        var recovery = Keeper(17 * mirror);
        recovery.candidate.actionType = RoomActionType.StartMovement;
        recovery.explanation.actorHex = Hex(12 * mirror);
        var emergency = Keeper(12 * mirror);
        emergency.candidate.actionType = RoomActionType.StartMovement;
        emergency.explanation.actorHex = Hex(16 * mirror);
        emergency.explanation.goalkeeperPositioningCurrentMaximumExpectedGoals = .6f;
        emergency.explanation.goalkeeperPositioningMaximumExpectedGoals = .4f;
        RoomAITacticalSafety.ConstrainGoalkeeperChoices(Defense(mirror), new() { wandering, recovery, emergency });
        Assert.That(wandering.safetyPriority, Is.GreaterThan(0));
        Assert.That(recovery.safetyPriority, Is.Zero);
        Assert.That(emergency.safetyPriority, Is.Zero);
    }

    [TestCase(0f, true)]
    [TestCase(.20f, false)]
    public void AdditionalPossessionRiskNeedsAnAttackingReward(float extraExpectedGoals, bool rejected)
    {
        RoomAITacticalEvaluation Route(float retention, float threat) => new()
        {
            candidate = new RoomActionCandidate { actionType = RoomActionType.GroundPass, step = RoomDecisionStep.ChooseTarget },
            explanation = new RoomAICandidateExplanation { hasPossessionLevelPlan = true,
                possessionPlanEffectiveRetentionProbability = retention, possessionPlanThreatExpectedGoals = threat }
        };
        var safe = Route(.95f, .1f);
        var risky = Route(.8f, .1f + extraExpectedGoals);
        risky.explanation.forwardProgressDelta = 20;
        RoomAITacticalSafety.ProtectPossession(new[] { safe, risky });
        Assert.That(risky.safetyPriority > 0, Is.EqualTo(rejected));
        Assert.That(safe.safetyPriority, Is.Zero);
        var smallRisk = Route(.92f, .11f);
        RoomAITacticalSafety.ProtectPossession(new[] { safe, smallRisk });
        Assert.That(smallRisk.safetyPriority, Is.Zero);
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void NonStandardDefenderNumbersCannotLeaveAnIsolatedAnchor(int mirror)
    {
        var snapshot = Defense(mirror);
        RoomAITokenSnapshot Back(int jersey, int x) => new() { tokenKey = "Away:" + jersey, teamSide = "Away",
            jerseyNumber = jersey, isPlaying = true, pace = 4, currentHex = Hex(x * mirror) };
        var anchor = Back(14, 10);
        var moving = Back(15, 9);
        snapshot.tokens.AddRange(new[] { anchor, moving, Back(12, 8) });
        var facts = new RoomAICandidateExplanation { actorTokenKey = moving.tokenKey, actorToken = moving,
            actorHex = moving.currentHex, targetHex = Hex(6 * mirror) };
        // Move the other covering defender too: the proposed advance leaves a
        // 4-column gap, irrespective of inherited formation/jersey roles.
        snapshot.tokens[2].currentHex = Hex(6 * mirror, 4);
        RoomAITacticalSafety.EvaluateDefensiveLine(snapshot, facts);
        Assert.That(facts.defensiveLineCreatesGap, Is.True);
        facts.targetHex = Hex(9 * mirror, 1);
        RoomAITacticalSafety.EvaluateDefensiveLine(snapshot, facts);
        Assert.That(facts.defensiveLineCreatesGap, Is.False);
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void HopelessChanceStillRecoversInsteadOfHoldingOrMovingForward(int mirror)
    {
        var snapshot = Defense(mirror);
        var defender = new RoomAITokenSnapshot { tokenKey = "Away:14", teamSide = "Away", isPlaying = true,
            pace = 1, currentHex = Hex(-10 * mirror) };
        var striker = new RoomAITokenSnapshot { tokenKey = "Home:9", teamSide = "Home", isPlaying = true,
            pace = 6, shooting = 6, currentHex = Hex(12 * mirror) };
        snapshot.tokens.AddRange(new[] { defender, striker });
        var retreat = Hex(-7 * mirror);
        var plan = RoomAITacticalSafety.AssignDefense(snapshot, new Dictionary<string, List<RoomHexCoordinates>>
            { [defender.tokenKey] = new() { defender.currentHex, Hex(-13 * mirror), retreat } });
        Assert.That(plan.assignments.Single().destination, Is.SameAs(retreat));
        Assert.That(plan.assignments.Single().role, Is.EqualTo("EmergencyRecovery"));
        Assert.That(plan.remainingThreatEstimate, Is.EqualTo(plan.initialThreatEstimate).Within(.0001f));
    }

    [Test]
    public void StoredOffsideDoesNotPreventMovementButStillBlocksCollectionAfterReturningOnside()
    {
        var old = MatchManager.Instance;
        var root = new GameObject("Offside movement contract"); root.SetActive(false);
        try
        {
            var match = root.AddComponent<MatchManager>(); MatchManager.Instance = match; match.difficulty_level = 1;
            match.currentState = MatchManager.GameState.MovementPhase;
            var offside = root.AddComponent<OffsideManager>(); match.offsideManager = offside; offside.Configure(match);
            var token = root.AddComponent<PlayerToken>(); token.isHomeTeam = true;
            var grid = root.AddComponent<HexGrid>(); match.hexGrid = grid;
            grid.cells = new HexCell[grid.GridWidth, grid.GridHeight];
            var origin = new GameObject("Offside hex"); origin.transform.SetParent(root.transform);
            var start = origin.AddComponent<HexCell>(); start.coordinates = new Vector3Int(12, 0, 0); token.SetCurrentHex(start);
            grid.cells[12 + grid.GridWidth / 2, grid.GridHeight / 2] = start;
            // Use the manager's persisted pass-time assessment. Movement must
            // never erase that judgment before legal collection or a restart.
            typeof(OffsideManager).GetField("hasStoredAssessment", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(offside, true);
            typeof(OffsideManager).GetField("assessedTeamIsHome", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(offside, true);
            ((List<PlayerToken>)typeof(OffsideManager).GetField("offsideTokens", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(offside)).Add(token);
            var returned = new GameObject("Returned hex"); returned.transform.SetParent(root.transform);
            var destination = returned.AddComponent<HexCell>(); destination.coordinates = new Vector3Int(8, 0, 0);
            grid.cells[8 + grid.GridWidth / 2, grid.GridHeight / 2] = destination;
            for (int x = 9; x <= 11; x++)
            {
                var cellObject = new GameObject("Route " + x); cellObject.transform.SetParent(root.transform);
                var cell = cellObject.AddComponent<HexCell>(); cell.coordinates = new Vector3Int(x, 0, 0);
                grid.cells[x + grid.GridWidth / 2, grid.GridHeight / 2] = cell;
            }
            Assert.That(HexGridUtils.GetReachableHexes(grid, start, 4).reachableHexes, Does.Contain(destination));
            token.SetCurrentHex(destination);
            Assert.That(token.GetCurrentHex(), Is.SameAs(destination));
            Assert.That(offside.ShouldBlockCollection(token), Is.True);
            Assert.That(match.CanTokenCollectHangingPass(token), Is.False, "Returning onside does not clear the pass-time offense.");
        }
        finally { MatchManager.Instance = old; Object.DestroyImmediate(root); }
    }
}
