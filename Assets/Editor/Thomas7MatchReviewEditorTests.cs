using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public sealed class Thomas7MatchReviewEditorTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags StaticPrivate = BindingFlags.NonPublic | BindingFlags.Static;

    [Test]
    public void LoadedPlayThroughsHaveDistinctFilesAndRestoreTheSavedBaselineWithoutChangingTheSeed()
    {
        string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CounterAttackRunFork_" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(folder);
        try
        {
            string source = System.IO.Path.Combine(folder, "Thomas7.json");
            var root = new Newtonsoft.Json.Linq.JObject
            {
                ["gameSettings"] = new Newtonsoft.Json.Linq.JObject(),
                ["stats"] = new Newtonsoft.Json.Linq.JObject { ["marker"] = 9 },
                ["runtimeSnapshot"] = new Newtonsoft.Json.Linq.JObject
                {
                    ["savedAtUtc"] = "2026-10-27T10:00:00Z",
                    ["stats"] = new Newtonsoft.Json.Linq.JObject { ["marker"] = 1 },
                    ["gameLog"] = new Newtonsoft.Json.Linq.JArray("Saved baseline")
                },
                ["events"] = new Newtonsoft.Json.Linq.JArray(
                    new Newtonsoft.Json.Linq.JObject { ["sequenceNumber"] = 2, ["timestampUtc"] = "2026-10-27T09:59:00Z" },
                    new Newtonsoft.Json.Linq.JObject { ["sequenceNumber"] = 3, ["timestampUtc"] = "2026-10-27T10:01:00Z" })
            };
            string original = root.ToString();
            System.IO.File.WriteAllText(source, original);
            Assert.That(RoomSaveService.CreateLoadedRunCopy(source, out string first, out string error), Is.True, error);
            Assert.That(RoomSaveService.CreateLoadedRunCopy(source, out string second, out error), Is.True, error);
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(System.IO.File.ReadAllText(source), Is.EqualTo(original));
            var run = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(first));
            Assert.That((int)run["stats"]["marker"], Is.EqualTo(1));
            Assert.That(((Newtonsoft.Json.Linq.JArray)run["events"]).Count, Is.EqualTo(1));
            Assert.That((int)run["inheritedEventSequence"], Is.EqualTo(2));
            Assert.That((string)run["sourceSavePath"], Is.EqualTo(source));
            Assert.That((string)run["playSessionId"], Is.Not.Null.And.Not.Empty);
        }
        finally { System.IO.Directory.Delete(folder, recursive: true); }
    }

    [TestCase(MatchManager.GameState.KickOffSetup, false)]
    [TestCase(MatchManager.GameState.PostGoalKickOffSetup, false)]
    [TestCase(MatchManager.GameState.GoalKick, false)]
    [TestCase(MatchManager.GameState.MovementPhase, true)]
    public void GameStateRatherThanAiAnalysisControlsTheClock(MatchManager.GameState state, bool expected)
    {
        var old = MatchManager.Instance;
        var root = new GameObject("Clock ownership"); root.SetActive(false);
        try
        {
            var match = root.AddComponent<MatchManager>();
            match.currentState = state;
            match.isClockRunning = true;
            var method = typeof(MatchManager).GetMethod("CanAdvanceMatchClock", Private);
            Assert.That((bool)method.Invoke(match, null), Is.EqualTo(expected));
            typeof(MatchManager).GetField("isPauseMenuOpen", Private).SetValue(match, true);
            Assert.That((bool)method.Invoke(match, null), Is.False);
        }
        finally { MatchManager.Instance = old; Object.DestroyImmediate(root); }
    }

    [Test]
    public void DirectGoalKickExemptionDoesNotCarryIntoTheSubsequentHeader()
    {
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Home", homeTeamDirection = "LeftToRight", firstAerialOffsideExempt = true };
        snapshot.tokens.AddRange(new[]
        {
            new RoomAITokenSnapshot { teamSide = "Away", isPlaying = true, currentHex = new() { x = 16, z = 0 } },
            new RoomAITokenSnapshot { teamSide = "Away", isPlaying = true, currentHex = new() { x = 8, z = 0 } }
        });
        var receiver = new RoomHexCoordinates { x = 12, z = 0 };
        var ball = new RoomHexCoordinates { x = -15, z = 0 };
        Assert.That(RoomAIActionSequence.OffsideForFirstAerial(snapshot, receiver, ball), Is.False);
        Assert.That(RoomAIActionSequence.Offside(snapshot, receiver, new() { x = 9, z = 0 }), Is.True,
            "A later headed pass is a new touch and uses the ball and second-last defender normally.");
    }

    [Test]
    public void RepositionInterruptsAnExistingMovementLockAndKeepsEveryDestination()
    {
        var root = new GameObject("Reposition routing"); root.SetActive(false);
        try
        {
            var manager = root.AddComponent<RoomDecisionManager>();
            var cell = root.AddComponent<HexCell>();
            typeof(RoomDecisionManager).GetField("executionLockActionType", Private)
                .SetValue(manager, RoomActionType.StartMovement);
            var context = new RoomDecisionContext { manager = nameof(MovementPhaseManager) };
            context.AddHexActionCandidate(nameof(MovementPhaseManager), RoomActionType.SetupMove,
                RoomDecisionStep.ChooseTarget, cell, "Reposition forward");
            context.AddKeyActionCandidate(nameof(MovementPhaseManager), RoomActionType.Decline,
                RoomDecisionStep.InterruptionChoice, "X", "Stay", isForfeit: true);
            context.AddKeyActionCandidate(nameof(MovementPhaseManager), RoomActionType.StartMovement,
                RoomDecisionStep.InterruptionChoice, "X", "Stale movement forfeit", isForfeit: true);
            var result = (List<RoomActionCandidate>)typeof(RoomDecisionManager)
                .GetMethod("BuildTacticalExecutableCandidatesCore", Private)
                .Invoke(manager, new object[] { new GameplayInstructionSnapshot { manager = context.manager }, context, true });
            Assert.That(result.Any(action => action.targetHex == cell), Is.True);
            Assert.That(result.Any(action => action.actionType == RoomActionType.Decline), Is.True);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void WaitingDribblerDoesNotBlockKeeperOrAdvantageRepositionDecisions()
    {
        var old = MatchManager.Instance;
        var root = new GameObject("Interrupt ownership"); root.SetActive(false);
        try
        {
            var match = root.AddComponent<MatchManager>(); MatchManager.Instance = match;
            var movement = root.AddComponent<MovementPhaseManager>(); match.movementPhaseManager = movement;
            movement.goalKeeperManager = root.AddComponent<GoalKeeperManager>();
            movement.isPlayerMoving = true;
            movement.goalKeeperManager.isActivated = true;
            var blocked = typeof(RoomDecisionManager).GetMethod("IsMovementBlockingDecision", StaticPrivate);
            var instruction = new GameplayInstructionSnapshot { manager = nameof(GoalKeeperManager) };
            Assert.That(blocked.Invoke(null, new object[] { instruction }), Is.False);
            instruction.manager = nameof(MovementPhaseManager);
            Assert.That(blocked.Invoke(null, new object[] { instruction }), Is.True);
            movement.isWaitingForReposition = true;
            Assert.That(blocked.Invoke(null, new object[] { instruction }), Is.False);
        }
        finally { MatchManager.Instance = old; Object.DestroyImmediate(root); }
    }

    [Test]
    public void ANewAerialLandingDropsThePreviousDistantGoalkeeperNomination()
    {
        var old = MatchManager.Instance;
        var root = new GameObject("Aerial eligibility"); root.SetActive(false);
        try
        {
            var match = root.AddComponent<MatchManager>(); MatchManager.Instance = match;
            var headers = root.AddComponent<HeaderManager>();
            headers.hexGrid = root.AddComponent<HexGrid>();
            var grid = headers.hexGrid;
            grid.cells = new HexCell[grid.GridWidth, grid.GridHeight];
            var landingObject = new GameObject("Landing"); landingObject.transform.SetParent(root.transform);
            var landing = landingObject.AddComponent<HexCell>(); landing.coordinates = Vector3Int.zero;
            grid.cells[grid.GridWidth / 2, grid.GridHeight / 2] = landing;
            var keeper = root.AddComponent<PlayerToken>();
            typeof(PlayerToken).GetField("_isGk", Private).SetValue(keeper, true);
            headers.highPassManager = root.AddComponent<HighPassManager>();
            headers.defEligibleToHead.Add(keeper);
            headers.defenderWillJump.Add(keeper);
            typeof(HeaderManager).GetMethod("RefreshHeaderEligibility", Private).Invoke(headers, new object[] { landing });
            Assert.That(headers.defEligibleToHead, Is.Empty);
            Assert.That(headers.defenderWillJump, Is.Empty);
            Assert.That(headers.hasEligibleDefenders, Is.False);
        }
        finally { MatchManager.Instance = old; Object.DestroyImmediate(root); }
    }

    [Test]
    public void BraceInstructionIsConstantThroughoutTheReset()
    {
        var root = new GameObject("Brace copy"); root.SetActive(false);
        try
        {
            var goal = root.AddComponent<GoalFlowManager>();
            var phase = typeof(GoalFlowManager).GetField("instructionPhase", Private);
            phase.SetValue(goal, System.Enum.Parse(phase.FieldType, "Reset"));
            typeof(GoalFlowManager).GetField("scorerGoalCount", Private).SetValue(goal, 2);
            typeof(GoalFlowManager).GetField("goalScorerName", Private).SetValue(goal, "Terto");
            string first = goal.GetInstructions();
            Assert.That(Enumerable.Range(0, 100).Select(_ => goal.GetInstructions()).Distinct().Single(), Is.EqualTo(first));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void DuplicateStatsTitleReferenceIsReboundToItsOwnRow()
    {
        var root = new GameObject("Stats grid", typeof(RectTransform)); root.SetActive(false);
        try
        {
            var titles = new TMP_Text[21];
            for (int i = 0; i < titles.Length; i++)
            {
                var row = new GameObject($"StatRow ({i + 1})", typeof(RectTransform)); row.transform.SetParent(root.transform);
                var title = new GameObject("StatTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                title.transform.SetParent(row.transform); titles[i] = title.GetComponent<TMP_Text>();
            }
            titles[6] = titles[5];
            object[] args = { root.GetComponent<RectTransform>(), "StatTitles", "StatTitle_", titles };
            typeof(MatchStatsUI).GetMethod("BindStatsTextArray", StaticPrivate).Invoke(null, args);
            var repaired = (TMP_Text[])args[3];
            Assert.That(repaired.Distinct().Count(), Is.EqualTo(21));
            Assert.That(repaired[5].transform.parent.name, Is.EqualTo("StatRow (6)"));
            Assert.That(repaired[6].transform.parent.name, Is.EqualTo("StatRow (7)"));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void ExistingSavedGoalsRepairOnTargetOnceWithoutDoublingCorrectGoals()
    {
        var data = new MatchManager.GameData();
        var player = new MatchManager.PlayerStatsEntry { playerKey = "Home:9", teamSide = "Home", stats = new MatchManager.PlayerStats
            { shotsAttempted = 2, goals = 2, shotsOnTarget = 1 } };
        data.stats.playerStats.Add(player); data.stats.homeTeamStats.totalShotsOnTarget = 1;
        void Event(MatchManager.ActionType action) => data.events.Add(new GameplayEvent
        {
            sequenceNumber = data.events.Count + 1, kind = "stats.action", actorTokenKey = "Home:9",
            result = new GameplayEventResult { details = new() { ["actionType"] = action.ToString(), ["value"] = "1" } }
        });
        Event(MatchManager.ActionType.ShotAttempt); Event(MatchManager.ActionType.ShotOnTarget); Event(MatchManager.ActionType.GoalScored);
        Event(MatchManager.ActionType.ShotAttempt); Event(MatchManager.ActionType.GoalScored);
        ShotStatisticsRepair.RepairSavedGoals(data);
        ShotStatisticsRepair.RepairSavedGoals(data);
        Assert.That(player.stats.shotsOnTarget, Is.EqualTo(2));
        Assert.That(data.stats.homeTeamStats.totalShotsOnTarget, Is.EqualTo(2));
    }

    [Test]
    public void LooseBallDoesNotClaimCertainPossessionForAnUnrelatedMover()
    {
        var action = new RoomActionCandidate { actionType = RoomActionType.StartMovement };
        var facts = new RoomAICandidateExplanation { candidate = action };
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Away", teamInAttack = "Away", attackHasPossession = false };
        snapshot.candidateExplanations.Add(facts);
        RoomAIDecisionSnapshotBuilder.PopulatePossessionLevelPlanFacts(snapshot);
        Assert.That(facts.hasPossessionLevelPlan, Is.False);
    }

    [TestCase(2)]
    [TestCase(8)]
    public void F3ProjectionUsesSixHexesEvenWhenPaceAndTheSuspended2f2RangeDiffer(int pace)
    {
        var old = MatchManager.Instance;
        var root = new GameObject("Phase movement forecast"); root.SetActive(false);
        try
        {
            var match = root.AddComponent<MatchManager>(); MatchManager.Instance = match;
            var grid = root.AddComponent<HexGrid>(); match.hexGrid = grid;
            grid.cells = new HexCell[grid.GridWidth, grid.GridHeight];
            for (int x = 0; x <= 7; x++)
            {
                var cellObject = new GameObject($"Cell {x}", typeof(MeshRenderer)); cellObject.transform.SetParent(root.transform);
                var cell = cellObject.AddComponent<HexCell>(); cell.coordinates = new Vector3Int(x, 0, 0);
                grid.cells[x + grid.GridWidth / 2, grid.GridHeight / 2] = cell;
            }
            var origin = grid.GetHexCellAt(Vector3Int.zero);
            var actorObject = new GameObject("Runner"); actorObject.transform.SetParent(root.transform);
            var actor = actorObject.AddComponent<PlayerToken>(); actor.pace = pace;
            actor.SetCurrentHex(origin); origin.isAttackOccupied = true;
            var movement = root.AddComponent<MovementPhaseManager>(); match.movementPhaseManager = movement;
            movement.isMovementPhase2f2 = true;
            var f3 = root.AddComponent<FinalThirdManager>(); match.finalThirdManager = f3;
            f3.hexGrid = grid; f3.isActivated = true;
            typeof(FinalThirdManager).GetField("currentMovableTokens", Private).SetValue(f3, new List<PlayerToken> { actor });
            grid.highlightedHexes.Add(origin);
            var action = new RoomActionCandidate { actor = actor, step = RoomDecisionStep.ChooseActor,
                manager = nameof(FinalThirdManager), actionType = RoomActionType.FinalThird };
            var forecast = typeof(RoomAIDecisionSnapshotBuilder).GetMethod("GetAttackingSupportDestinations", StaticPrivate);
            var f3Targets = ((IEnumerable<HexCell>)forecast.Invoke(null, new object[] { new RoomAIDecisionSnapshot(), action })).ToArray();
            Assert.That(f3Targets.Select(hex => hex.coordinates.x), Does.Contain(6));
            Assert.That(f3Targets.Select(hex => hex.coordinates.x), Has.No.Member(7));
            Assert.That(grid.highlightedHexes, Is.EqualTo(new[] { origin }), "A forecast must not select or highlight a player.");
            action.manager = nameof(MovementPhaseManager); action.actionType = RoomActionType.StartMovement;
            var twoForTwoTargets = ((IEnumerable<HexCell>)forecast.Invoke(null, new object[] { new RoomAIDecisionSnapshot(), action })).ToArray();
            Assert.That(twoForTwoTargets.All(hex => HexGridUtils.GetHexStepDistance(origin, hex) <= movement.TwoForTwoRange), Is.True);
            typeof(FinalThirdManager).GetField("currentMovableTokens", Private).SetValue(f3, new List<PlayerToken>());
            Assert.That(f3.GetMovementDestinationsForDecisionPreview(actor), Is.Empty, "Spent F3 actors cannot be projected again.");
        }
        finally { MatchManager.Instance = old; Object.DestroyImmediate(root); }
    }

    [Test]
    public void HistoricalShotEventsCannotInflateAResetPlayersCurrentCounters()
    {
        var data = new MatchManager.GameData();
        var player = new MatchManager.PlayerStatsEntry { playerKey = "Home:9", teamSide = "Home",
            stats = new MatchManager.PlayerStats { shotsOnTarget = 2 } };
        data.stats.playerStats.Add(player); data.stats.homeTeamStats.totalShotsOnTarget = 2;
        void Add(string action, int time, string state) => data.events.Add(new GameplayEvent
        {
            sequenceNumber = data.events.Count + 1, currentHalf = 1, matchClockSeconds = time,
            currentGameState = state, kind = "stats.action", actorTokenKey = "Home:9",
            result = new GameplayEventResult { details = new() { ["actionType"] = action, ["value"] = "1" } }
        });
        Add("ShotAttempt", 600, "MovementPhase"); Add("GoalScored", 600, "MovementPhase");
        Add("Move", 0, "KickOffSetup");
        ShotStatisticsRepair.RepairSavedGoals(data);
        ShotStatisticsRepair.RepairSavedGoals(data);
        Assert.That(player.stats.shotsOnTarget, Is.Zero);
        Assert.That(data.stats.homeTeamStats.totalShotsOnTarget, Is.Zero);
    }

    [Test]
    public void LooseBallRollKeepsItsOwnerAndEscapesAnInheritedMovementLock()
    {
        var root = new GameObject("Spill roll routing"); root.SetActive(false);
        try
        {
            var loose = root.AddComponent<LooseBallManager>(); loose.isActivated = true; loose.isWaitingForDirectionRoll = true;
            var manager = root.AddComponent<RoomDecisionManager>();
            typeof(RoomDecisionManager).GetField("executionLockActionType", Private).SetValue(manager, RoomActionType.StartMovement);
            var context = new RoomDecisionContext { manager = "LooseBallManager,ShotManager" };
            loose.PopulateRoomDecisionContext(context);
            Assert.That(context.actions.Single().manager, Is.EqualTo(nameof(LooseBallManager)));
            var choices = (List<RoomActionCandidate>)typeof(RoomDecisionManager).GetMethod("BuildTacticalExecutableCandidatesCore", Private)
                .Invoke(manager, new object[] { new GameplayInstructionSnapshot { manager = context.manager }, context, true });
            Assert.That(choices.Single().actionType, Is.EqualTo(RoomActionType.Roll));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void CpuSearchIgnoresHoverPresentationButNotACommittedTargetChange()
    {
        var old = MatchManager.Instance; MatchManager.Instance = null;
        var root = new GameObject("CPU semantic identity"); root.SetActive(false);
        try
        {
            var manager = root.AddComponent<RoomDecisionManager>();
            var instruction = new GameplayInstructionSnapshot { manager = nameof(ShotManager), expectedTeam = "Away",
                expectedInput = "click_hex", isAwaitingInput = true, instructionText = "Choose a target" };
            instruction.details["hoveredHex"] = "1,1";
            var context = new RoomDecisionContext { manager = instruction.manager, expectedTeam = "Away" };
            var method = typeof(RoomDecisionManager).GetMethod("BuildDecisionStateKey", Private);
            string Key() => (string)method.Invoke(manager, new object[] { instruction, context,
                AIManager.RoomPersona.CodexTactician, AIManager.CodexTacticianDifficulty.UltraHard });
            string first = Key();
            instruction.instructionText = "Hovered shot preview";
            instruction.details["hoveredHex"] = "2,2";
            Assert.That(Key(), Is.EqualTo(first));
            instruction.currentTargetHex = new RoomHexCoordinates { x = 3, z = 2 };
            Assert.That(Key(), Is.Not.EqualTo(first));
        }
        finally { MatchManager.Instance = old; Object.DestroyImmediate(root); }
    }

    [Test]
    public void GoalkeeperRushDestinationsSurviveThePreviousActionLockAlongsideStaying()
    {
        var root = new GameObject("Rush options"); root.SetActive(false);
        try
        {
            var manager = root.AddComponent<RoomDecisionManager>();
            typeof(RoomDecisionManager).GetField("executionLockActionType", Private).SetValue(manager, RoomActionType.HighPass);
            var context = new RoomDecisionContext { manager = nameof(HighPassManager) };
            var cell = root.AddComponent<HexCell>();
            context.AddHexActionCandidate(nameof(HighPassManager), RoomActionType.GoalkeeperSave, RoomDecisionStep.ChooseTarget, cell, "Rush");
            context.AddKeyActionCandidate(nameof(HighPassManager), RoomActionType.Decline, RoomDecisionStep.InterruptionChoice, "X", "Stay");
            var choices = (List<RoomActionCandidate>)typeof(RoomDecisionManager).GetMethod("BuildTacticalExecutableCandidatesCore", Private)
                .Invoke(manager, new object[] { new GameplayInstructionSnapshot { manager = context.manager }, context, true });
            Assert.That(choices.Count, Is.EqualTo(2));
            Assert.That(choices.Any(action => action.targetHex == cell), Is.True);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [TestCase("Home")]
    [TestCase("Away")]
    public void AttackingF3ScoresAPassToSnapshotInsteadOfApplyingDefensiveLineOnlyScoring(string team)
    {
        var action = new RoomActionCandidate { manager = nameof(FinalThirdManager),
            actionType = RoomActionType.FinalThird, step = RoomDecisionStep.ChooseTarget };
        var stay = new RoomActionCandidate { manager = nameof(FinalThirdManager), actionType = RoomActionType.Decline, isForfeit = true };
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = team, teamInAttack = team,
            attackHasPossession = true, homeTeamDirection = "LeftToRight", awayTeamDirection = "RightToLeft" };
        var facts = new RoomAICandidateExplanation { candidate = action, isGroundPassContinuation = true,
            isSupportingPassLaneMove = true, groundPassRetentionProbability = .9f,
            groundPassRouteExpectedGoals = .3f, groundPassCreatesReachableShot = true,
            groundPassCollectorTokenKey = team + ":9" };
        typeof(RoomAIDecisionSnapshotBuilder).GetMethod("PopulateRegularFinalThirdFacts", StaticPrivate)
            .Invoke(null, new object[] { snapshot, action, facts, null });
        Assert.That(facts.isRegularFinalThirdReorganization, Is.False);
        snapshot.legalActions.AddRange(new[] { action, stay });
        snapshot.candidateExplanations.AddRange(new[] { facts, new RoomAICandidateExplanation { candidate = stay } });
        var ranked = RoomAITacticalPlanner.RankCandidates(snapshot, snapshot.legalActions, null, new RoomAITacticalWeights());
        Assert.That(ranked[0].candidate, Is.SameAs(action));
        Assert.That(ranked[0].reasons, Has.Some.Contains("pass-to-Snapshot route"));
        snapshot.expectedTeam = team == "Home" ? "Away" : "Home";
        typeof(RoomAIDecisionSnapshotBuilder).GetMethod("PopulateRegularFinalThirdFacts", StaticPrivate)
            .Invoke(null, new object[] { snapshot, action, facts, null });
        Assert.That(facts.isRegularFinalThirdReorganization, Is.True, "The defending side still uses line reorganisation.");
    }
}
