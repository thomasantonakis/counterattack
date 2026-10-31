using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public sealed class Thomas5MatchReviewEditorTests
{
    [TestCase(1, true)]
    [TestCase(0, false)]
    public void ConfirmationRespectsSaferReopenedTargetWithoutBreakingEqualSafety(int priority, bool veto)
    {
        var confirmation = new RoomAITacticalEvaluation { safetyPriority = priority, candidate = new RoomActionCandidate
            { actionType = RoomActionType.GroundPass, step = RoomDecisionStep.Confirm } };
        var alternate = new RoomAITacticalEvaluation { safetyPriority = 0, candidate = new RoomActionCandidate
            { actionType = RoomActionType.GroundPass, step = RoomDecisionStep.ChooseTarget } };
        var decision = new RoomAIProviderDecision { rankedCandidates = new() { confirmation, alternate } };
        bool result = (bool)typeof(RoomDecisionManager).GetMethod("HasFreshSafetyVeto", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { decision, confirmation });
        Assert.That(result, Is.EqualTo(veto));
    }
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    [TestCase("target_selected", true)]
    [TestCase("target_confirmed", true)]
    [TestCase("invalid_target", false)]
    [TestCase("ignored", false)]
    public void ConsumedRejectionsAreNotSuccessfulAutomatedExecution(string outcome, bool accepted)
    {
        var input = new ClickInputData(null, null, false);
        input.Consume(nameof(GroundBallManager), "standard_pass", outcome);
        var method = typeof(GameInputManager).GetMethod("WasSyntheticClickAccepted", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method.Invoke(null, new object[] { input }), Is.EqualTo(accepted));
    }

    [Test]
    public void FirstDribbleStepAlreadyHasAFullPaceDestination()
    {
        using var replay = new ReportedMovementReplay("Thomas5_MatchReview", 98, restoreMovementOwnership: false);
        var carrier = replay.tokens["Away:11"];
        replay.SelectActor(carrier);
        replay.match.movementPhaseManager.remainingDribblerPace = carrier.pace;
        replay.match.movementPhaseManager.isDribblerRunning = false;
        replay.Evaluate();
        Assert.That(replay.lastSnapshot.candidateExplanations.Any(item => item.isFullPaceDribbleRoute), Is.True,
            "The first Pace must be planned with the rest of the route, before movement starts.");
    }

    [Test]
    public void PlannedStandardPassKeepsItsReceiverWithoutInventingAnActorSelection()
    {
        var root = new GameObject("Root pass intent fixture");
        root.SetActive(false);
        try
        {
            var manager = root.AddComponent<RoomDecisionManager>();
            var target = root.AddComponent<HexCell>();
            target.coordinates = new Vector3Int(7, 0, -11);
            var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Away", teamInAttack = "Away", attackHasPossession = true };
            var rootChoice = new RoomActionCandidate { manager = nameof(GroundBallManager), actionType = RoomActionType.GroundPass,
                step = RoomDecisionStep.ChooseActionType, key = "P", isExecutableNow = true };
            var rootEvaluation = new RoomAITacticalEvaluation { candidate = rootChoice, explanation = new RoomAICandidateExplanation
                { actorTokenKey = "Away:7", groundPassContinuationTargetHex = RoomHexCoordinates.FromHex(target) } };
            typeof(RoomDecisionManager).GetMethod("RememberCommittedStagedDecision", Private)
                .Invoke(manager, new object[] { rootEvaluation, snapshot });
            var targetChoice = new RoomActionCandidate { manager = nameof(GroundBallManager), actionType = RoomActionType.GroundPass,
                step = RoomDecisionStep.ChooseTarget, targetHex = target, isExecutableNow = true };
            var targetEvaluation = new RoomAITacticalEvaluation { candidate = targetChoice };
            var decision = new RoomAIProviderDecision { rankedCandidates = new() { targetEvaluation } };
            bool retained = (bool)typeof(RoomDecisionManager).GetMethod("ApplyCommittedStagedDecision", Private)
                .Invoke(manager, new object[] { decision, snapshot });
            Assert.That(retained, Is.True, "The passer is already known; the next step is the planned target.");
            Assert.That(decision.selectedCandidate, Is.SameAs(targetChoice));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [TestCase(0, true)]
    [TestCase(1, false)]
    public void StagedPassCannotOverrideAFreshSaferTarget(int committedPriority, bool retained)
    {
        var root = new GameObject("Pass safety staging"); root.SetActive(false);
        try
        {
            var manager = root.AddComponent<RoomDecisionManager>();
            var target = root.AddComponent<HexCell>(); target.coordinates = new Vector3Int(-15, 0, -6);
            var alternative = root.AddComponent<HexCell>(); alternative.coordinates = new Vector3Int(-13, 0, -11);
            var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Away", teamInAttack = "Away", attackHasPossession = true };
            var action = new RoomActionCandidate { manager = nameof(GroundBallManager), actionType = RoomActionType.GroundPass,
                step = RoomDecisionStep.ChooseActionType, isExecutableNow = true };
            var initial = new RoomAITacticalEvaluation { candidate = action, explanation = new RoomAICandidateExplanation
                { groundPassContinuationTargetHex = RoomHexCoordinates.FromHex(target) } };
            typeof(RoomDecisionManager).GetMethod("RememberCommittedStagedDecision", Private)
                .Invoke(manager, new object[] { initial, snapshot });
            var committed = new RoomAITacticalEvaluation { safetyPriority = committedPriority, candidate = new RoomActionCandidate
                { manager = nameof(GroundBallManager), actionType = RoomActionType.GroundPass,
                    step = RoomDecisionStep.ChooseTarget, targetHex = target, isExecutableNow = true } };
            var safer = new RoomAITacticalEvaluation { safetyPriority = 0, candidate = new RoomActionCandidate
                { manager = nameof(GroundBallManager), actionType = RoomActionType.GroundPass,
                    step = RoomDecisionStep.ChooseTarget, targetHex = alternative, isExecutableNow = true } };
            var decision = new RoomAIProviderDecision { rankedCandidates = new() { safer, committed },
                selectedEvaluation = safer, selectedCandidate = safer.candidate };
            bool applied = (bool)typeof(RoomDecisionManager).GetMethod("ApplyCommittedStagedDecision", Private)
                .Invoke(manager, new object[] { decision, snapshot });
            Assert.That(applied, Is.EqualTo(retained));
            Assert.That(decision.selectedCandidate, Is.SameAs(retained ? committed.candidate : safer.candidate));

            var identity = typeof(RoomDecisionManager).GetMethod("BuildCandidateIdentity", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { committed.candidate });
            typeof(RoomDecisionManager).GetField("armedCodexCandidateIdentity", Private).SetValue(manager, identity);
            decision.selectedEvaluation = safer; decision.selectedCandidate = safer.candidate;
            typeof(RoomDecisionManager).GetMethod("RestoreArmedCodexCandidate", Private).Invoke(manager, new object[] { decision });
            Assert.That(decision.selectedCandidate, Is.SameAs(retained ? committed.candidate : safer.candidate));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void SupportBonusCannotBeatAnEquallySafeExtraForwardColumn()
    {
        var forward = new RoomAITacticalEvaluation { safetyPriority = 0,
            explanation = new RoomAICandidateExplanation { isFullPaceDribbleRoute = true,
                fullPaceDribbleReachProbability = 1f, fullPaceDribbleTurnoverProbability = 0f,
                fullPaceDribbleProgress = 5, fullPaceDribbleEndpointPressure = 0,
                fullPaceDribbleExpectedGoals = -1f, bestReachableSnapshotExpectedGoals = -1f } };
        var sideways = new RoomAITacticalEvaluation { safetyPriority = 0, score = 1000,
            explanation = new RoomAICandidateExplanation { isFullPaceDribbleRoute = true,
                fullPaceDribbleReachProbability = 1f, fullPaceDribbleTurnoverProbability = 0f,
                fullPaceDribbleProgress = 4, fullPaceDribbleEndpointPressure = 0,
                fullPaceDribbleExpectedGoals = -1f, bestReachableSnapshotExpectedGoals = -1f } };
        var evaluations = new List<RoomAITacticalEvaluation> { sideways, forward };
        typeof(CodexTacticianProvider).GetMethod("ApplyUnopposedDribbleProgress", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { evaluations });
        Assert.That(evaluations[0], Is.SameAs(forward));
    }

    [Test]
    public void DefenderStillRecoversWhenEveryReachableHexIsShortOfTheReceiver()
    {
        var defender = new RoomAITokenSnapshot { tokenKey = "Away:4", jerseyNumber = 4, teamSide = "Away", isPlaying = true,
            pace = 4, currentHex = new RoomHexCoordinates { x = 8, z = -3 } };
        var threat = new RoomAITokenSnapshot { tokenKey = "Home:9", jerseyNumber = 9, teamSide = "Home", isPlaying = true,
            pace = 4, shooting = 6, currentHex = new RoomHexCoordinates { x = 12, z = 0 } };
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Away", teamInAttack = "Home", awayTeamDirection = "RightToLeft",
            ball = new RoomHexCoordinates { x = 12, z = 9 } };
        snapshot.tokens.AddRange(new[] { defender, threat });
        var recovery = new RoomHexCoordinates { x = 10, z = -2 };
        var plan = RoomAITacticalSafety.AssignDefense(snapshot, new Dictionary<string, List<RoomHexCoordinates>>
            { [defender.tokenKey] = new() { defender.currentHex, recovery } });
        var assignment = plan.assignments.Single();
        Assert.That(assignment.destination, Is.SameAs(recovery));
        Assert.That(assignment.role, Is.EqualTo("RecoveryCover"));
    }

    [TestCase(RoomActionType.GroundPass)]
    [TestCase(RoomActionType.HighPass)]
    [TestCase(RoomActionType.LongBall)]
    [TestCase(RoomActionType.FirstTimePass)]
    [TestCase(RoomActionType.ThrowIn)]
    [TestCase(RoomActionType.Shot)]
    public void ExposedConfirmationBeatsChoosingAnotherTarget(RoomActionType actionType)
    {
        var root = new GameObject("Confirmation stage fixture");
        root.SetActive(false);
        try
        {
            var target = root.AddComponent<HexCell>();
            var confirm = new RoomActionCandidate { actionType = actionType, step = RoomDecisionStep.Confirm,
                targetHex = target, isExecutableNow = true };
            var other = new RoomActionCandidate { actionType = actionType, step = RoomDecisionStep.ChooseTarget,
                targetHex = target, isExecutableNow = true };
            var context = new RoomDecisionContext();
            context.actions.AddRange(new[] { other, confirm });
            var candidates = (List<RoomActionCandidate>)typeof(RoomDecisionManager)
                .GetMethod("BuildPendingTargetConfirmations", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { context });
            Assert.That(candidates, Is.EqualTo(new[] { confirm }));
            context.actions.Remove(confirm);
            candidates = (List<RoomActionCandidate>)typeof(RoomDecisionManager)
                .GetMethod("BuildPendingTargetConfirmations", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { context });
            Assert.That(candidates, Is.Empty, "A one-click action must not invent a confirmation stage.");
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void RepeatedRosterSwapsKeepWhiteNumbersAndMatchingBadgesIncludingEmptySlots()
    {
        var root = new GameObject("Roster swap fixture");
        root.SetActive(false);
        try
        {
            PlayerSlotDropHandler Slot(string name, int? value)
            {
                var row = new GameObject(name);
                row.transform.SetParent(root.transform);
                var wrapper = new GameObject("ContentWrapper");
                wrapper.transform.SetParent(row.transform);
                foreach (string field in new[] { "Jersey#", "PlayerNameInSlot", "PaceInSlot", "DribblingInSlot",
                    "HeadingInSlot", "HighPassInSlot", "ResilienceInSlot", "ShootingInSlot", "TacklingInSlot" })
                {
                    var child = new GameObject(field, typeof(RectTransform));
                    child.transform.SetParent(wrapper.transform);
                    var text = child.AddComponent<TextMeshProUGUI>();
                    text.text = field == "Jersey#" ? name.Split('-')[1]
                        : field == "PlayerNameInSlot" ? value.HasValue ? "Player" : "" : value?.ToString() ?? "";
                }
                foreach (string attribute in new[] { "Pace", "Dribbling", "Heading", "HighPass", "Resilience", "Shooting", "Tackling" })
                {
                    var badge = new GameObject(attribute + "Badge", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                    badge.transform.SetParent(wrapper.transform);
                }
                return row.AddComponent<PlayerSlotDropHandler>();
            }
            var first = Slot("Home-8-Player", 1);
            var second = Slot("Home-10-Player", 6);
            var empty = Slot("Home-16", null);
            var swap = typeof(PlayerSlotDropHandler).GetMethod("SwapSlotData", Private);
            for (int i = 0; i < 5; i++) swap.Invoke(first, new object[] { second.gameObject });
            swap.Invoke(empty, new object[] { first.gameObject });
            var populated = empty.transform.Find("ContentWrapper");
            var blank = first.transform.Find("ContentWrapper");
            Assert.That(populated.Find("PaceInSlot").GetComponent<TMP_Text>().text, Is.EqualTo("6"));
            Assert.That(populated.Find("PaceInSlot").GetComponent<TMP_Text>().color, Is.EqualTo(Color.white));
            Assert.That(populated.Find("PaceBadge").GetComponent<UnityEngine.UI.Image>().color,
                Is.EqualTo(new Color(0.1019608f, 0.7058824f, 0.654902f, 1f)));
            Assert.That(blank.Find("PaceInSlot").GetComponent<TMP_Text>().text, Is.Empty);
            Assert.That(blank.Find("PaceBadge").GetComponent<UnityEngine.UI.Image>().color.a, Is.Zero);
            Assert.That(blank.Find("Jersey#").GetComponent<TMP_Text>().text, Is.EqualTo("8"));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void HumanTurnsSkipAutomaticShadowSearchForBothMatchConfigurations()
    {
        var root = new GameObject("Shadow mode fixture");
        root.SetActive(false);
        try
        {
            var manager = root.AddComponent<RoomDecisionManager>();
            var method = typeof(RoomDecisionManager).GetMethod("ShouldEvaluateHumanShadow", Private);
            var settings = new MatchManager.GameSettings { homeRoomPersona = "AskHuman", awayRoomPersona = "Codex Tactician Ultra Hard" };
            Assert.That(method.Invoke(manager, new object[] { settings }), Is.False);
            settings.awayRoomPersona = "AskHuman";
            Assert.That(method.Invoke(manager, new object[] { settings }), Is.False);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [TestCase(6, -1)]
    [TestCase(8, 1)]
    [TestCase(9, 1)]
    [TestCase(10, -1)]
    public void DefendingKickoffKeepsMidfieldCoverBehindTheForwards(int jersey, int flank)
    {
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Away", teamInAttack = "Home" };
        var token = new RoomAITokenSnapshot { teamSide = "Away", jerseyNumber = jersey };
        var method = typeof(CodexTacticianProvider).GetMethod("GetDefendingKickoffTarget", BindingFlags.NonPublic | BindingFlags.Static);
        var target = ((int x, int z))method.Invoke(null, new object[] { snapshot, token });
        Assert.That(System.Math.Sign(target.z), Is.EqualTo(flank));
        if (jersey is 6 or 8)
        {
            // The 28 October review replaces fixed jersey-to-hex coordinates
            // with compact mutual support. Preserve this test's original depth
            // invariant as well: neither forward line nor centre-back line.
            (int x, int z) Target(int number) => ((int x, int z))method.Invoke(null,
                new object[] { snapshot, new RoomAITokenSnapshot { teamSide = "Away", jerseyNumber = number } });
            var forward = Target(9);
            var centreBack = Target(4);
            var partner = Target(jersey == 6 ? 8 : 6);
            Assert.That(target.x, Is.GreaterThan(forward.x).And.LessThan(centreBack.x));
            Assert.That(RoomAITacticalSafety.Distance(new() { x = target.x, z = target.z },
                new() { x = partner.x, z = partner.z }), Is.LessThanOrEqualTo(4),
                "Central midfielders must support each other instead of leaving the old six-hex gap.");
        }
        else Assert.That(target.x, Is.Zero, "The forwards retain their halfway press line.");
    }

    [Test]
    public void SelectedPassTargetChangesDecisionKeyDespiteOverwrittenInstructionSnapshot()
    {
        var previous = MatchManager.Instance;
        var root = new GameObject("Pass confirmation fixture");
        root.SetActive(false);
        try
        {
            var match = root.AddComponent<MatchManager>();
            MatchManager.Instance = match;
            match.groundBallManager = root.AddComponent<GroundBallManager>();
            var manager = root.AddComponent<RoomDecisionManager>();
            var target = root.AddComponent<HexCell>();
            var instruction = new GameplayInstructionSnapshot { manager = "MovementPhaseManager,GroundBallManager,HighPassManager", expectedTeam = "Away" };
            var identity = typeof(RoomDecisionManager).GetMethod("BuildDecisionStateKey", Private);
            string Key() => (string)identity.Invoke(manager, new object[] { instruction, null,
                AIManager.RoomPersona.CodexTactician, AIManager.CodexTacticianDifficulty.UltraHard });
            string before = Key();
            match.groundBallManager.currentTargetHex = target;
            Assert.That(Key(), Is.Not.EqualTo(before));
            var choose = new RoomActionCandidate { manager = nameof(GroundBallManager), actionType = RoomActionType.GroundPass,
                step = RoomDecisionStep.ChooseTarget, targetHex = target, isExecutableNow = true };
            var confirm = new RoomActionCandidate { manager = nameof(GroundBallManager), actionType = RoomActionType.GroundPass,
                step = RoomDecisionStep.Confirm, targetHex = target, isExecutableNow = true };
            var context = new RoomDecisionContext();
            context.actions.AddRange(new[] { choose, confirm });
            typeof(RoomDecisionManager).GetField("executionLockActionType", Private).SetValue(manager, RoomActionType.GroundPass);
            var candidates = (List<RoomActionCandidate>)typeof(RoomDecisionManager).GetMethod("BuildLockedExecutionCandidates", Private)
                .Invoke(manager, new object[] { context });
            Assert.That(candidates, Is.EqualTo(new[] { confirm }));
        }
        finally { MatchManager.Instance = previous; Object.DestroyImmediate(root); }
    }

    [Test]
    public void AvailableHighPassDoesNotEraseTheActiveStandardPassTarget()
    {
        var root = new GameObject("Combined instruction fixture");
        root.SetActive(false);
        try
        {
            var high = root.AddComponent<HighPassManager>();
            var target = new RoomHexCoordinates { x = -6, z = -3 };
            var snapshot = new GameplayInstructionSnapshot
                { manager = "GroundBallManager,HighPassManager", currentTargetHex = target };
            high.PopulateInstructionLogSnapshot(snapshot);
            Assert.That(snapshot.currentTargetHex, Is.SameAs(target));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void FasterReceiverGetsGoalSideCoverInsteadOfPassLaneOnlyMarking()
    {
        var defender = new RoomAITokenSnapshot { tokenKey = "Away:2", teamSide = "Away", jerseyNumber = 2,
            isPlaying = true, pace = 5, tackling = 4, currentHex = new RoomHexCoordinates { x = 5, z = 5 } };
        var attacker = new RoomAITokenSnapshot { tokenKey = "Home:11", teamSide = "Home", jerseyNumber = 11,
            isPlaying = true, pace = 6, shooting = 5, currentHex = new RoomHexCoordinates { x = 6, z = 5 } };
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Away", teamInAttack = "Home", awayTeamDirection = "RightToLeft", attackHasPossession = true,
            ball = new RoomHexCoordinates { x = -6, z = 0 } };
        snapshot.tokens.AddRange(new[] { defender, attacker });
        var reachable = new Dictionary<string, List<RoomHexCoordinates>> { [defender.tokenKey] = new()
            { defender.currentHex, new RoomHexCoordinates { x = 7, z = 5 }, new RoomHexCoordinates { x = 8, z = 5 } } };
        var plan = RoomAITacticalSafety.AssignDefense(snapshot, reachable);
        Assert.That(plan.assignments.Single().destination.x, Is.GreaterThan(attacker.currentHex.x));
    }

    [TestCase(28)]
    [TestCase(51)]
    [TestCase(144)]
    public void RecordedDefensiveForfeitHasAnAvailableUsefulMove(int number)
    {
        using var replay = new ReportedMovementReplay("Thomas5_MatchReview", number);
        var decision = replay.Evaluate();
        Assert.That(decision.selectedCandidate.isForfeit, Is.False,
            "The recorded board still has unused players who can improve coverage or the line.");
        Assert.That(replay.match.movementPhaseManager.movedTokens.Contains(decision.selectedCandidate.actor), Is.False);
    }
}
