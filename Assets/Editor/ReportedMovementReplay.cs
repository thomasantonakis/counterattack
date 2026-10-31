using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Headless Edit Mode reconstruction. No scores, threat values, legal
/// destinations or "useful continuation" flags are supplied by the fixture.
/// It is not a dice/input replay or a substitute for a resumed live match.</summary>
public sealed class ReportedMovementReplay : IDisposable
{
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private readonly Scene scene;
    private readonly GameObject root;
    private readonly MatchManager previousMatch;
    private readonly object previousTokenBall;
    private readonly JObject record;
    private RoomAIAttackPlan attackPlan;
    public readonly MatchManager match;
    public readonly RoomDecisionManager decisions;
    public readonly Dictionary<string, PlayerToken> tokens = new();
    public RoomAIDecisionSnapshot lastSnapshot;
    public readonly string fixture;
    public readonly int decisionNumber;

    public ReportedMovementReplay(string fixture, int number, bool restoreMovementOwnership = true)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Movement replays require stopped Edit Mode.");
        this.fixture = fixture;
        decisionNumber = number;
        string fixtureDirectory = Path.Combine(Application.dataPath, "Editor", "Fixtures");
        string fixturePath = Path.Combine(fixtureDirectory, fixture + ".json");
        if (!File.Exists(fixturePath))
            Assert.Ignore($"Local match fixture '{fixture}.json' is not available in this checkout.");

        record = JObject.Parse(File.ReadAllText(fixturePath))
            ["fixtures"][0]["decisions"].Children<JObject>().Single(item => (int)item["decisionNumber"] == number);
        var embeddedOwnership = record["movementReplay"] as JObject;
        var ownership = restoreMovementOwnership
            ? embeddedOwnership != null
                ? new JObject { ["sequenceNumber"] = record["sequenceNumber"], ["movementReplay"] = embeddedOwnership.DeepClone() }
                : JObject.Parse(File.ReadAllText(Path.Combine(fixtureDirectory, "ReportedMovementOwnership.json")))
                ["cases"].Children<JObject>().Single(item => (string)item["fixture"] == fixture && (int)item["decisionNumber"] == number)
            : new JObject { ["sequenceNumber"] = record["sequenceNumber"], ["movementReplay"] = new JObject
                { ["section"] = "Attacking", ["remainingMoves"] = MovementPhaseManager.DefaultAttackingMoveLimit,
                    ["movedTokenKeys"] = new JArray() } };
        if ((int)ownership["sequenceNumber"] != (int)record["sequenceNumber"])
            throw new InvalidDataException("Movement ownership belongs to a different recorded position.");
        previousMatch = MatchManager.Instance;
        previousTokenBall = typeof(PlayerToken).GetField("ball", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        scene = EditorSceneManager.NewPreviewScene();
        root = new GameObject("Reported movement replay") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false); // Never run gameplay Awake/Start/input subscriptions.
        SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            match = root.AddComponent<MatchManager>();
            MatchManager.Instance = match;
            match.currentState = MatchManager.GameState.MovementPhase;
            match.teamInAttack = (MatchManager.TeamInAttack)Enum.Parse(typeof(MatchManager.TeamInAttack), (string)record["teamInAttack"]);
            match.attackHasPossession = (bool)record["attackHasPossession"];
            match.homeTeamDirection = MatchManager.TeamAttackingDirection.LeftToRight;
            match.awayTeamDirection = MatchManager.TeamAttackingDirection.RightToLeft;
            match.difficulty_level = 2;
            match.refereeLeniency = 2;
            match.hexGrid = root.AddComponent<HexGrid>();
            var grid = match.hexGrid;
            grid.cells = new HexCell[grid.GridWidth, grid.GridHeight];
            for (int x = 0; x < grid.GridWidth; x++)
                for (int z = 0; z < grid.GridHeight; z++)
                {
                    var child = new GameObject("Replay hex") { hideFlags = HideFlags.HideAndDontSave };
                    child.transform.SetParent(root.transform);
                    var hex = child.AddComponent<HexCell>();
                    hex.coordinates = new Vector3Int(x - grid.GridWidth / 2, 0, z - grid.GridHeight / 2);
                    hex.hexRadius = grid.HexRadius;
                    child.transform.position = grid.GetHexCenterForCoordinates(hex.coordinates);
                    grid.cells[x, z] = hex;
                    Invoke(grid, "AssignHexFeatures", hex);
                }
            Invoke(grid, "LoadPrecomputedShootingPaths");
            Invoke(grid, "CalculateGoalDistanceAndDangerousTackleCaches");
            match.ball = root.AddComponent<Ball>();
            SetField(match.ball, "currentCell", Hex(record["ball"].ToObject<RoomHexCoordinates>()));
            typeof(PlayerToken).GetField("ball", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, match.ball);
            grid.ball = match.ball;
            match.playerTokenManager = root.AddComponent<PlayerTokenManager>();
            foreach (JObject data in record["tokens"].Children<JObject>())
            {
                var child = new GameObject((string)data["tokenKey"]) { hideFlags = HideFlags.HideAndDontSave };
                child.transform.SetParent(root.transform);
                var token = child.AddComponent<PlayerToken>();
                foreach (FieldInfo field in typeof(PlayerToken).GetFields(BindingFlags.Instance | BindingFlags.Public)
                    .Where(field => field.FieldType == typeof(int) || field.FieldType == typeof(string)))
                    if (data[field.Name] != null) field.SetValue(token, data[field.Name].ToObject(field.FieldType));
                token.isHomeTeam = (string)data["teamSide"] == "Home";
                token.isAttacker = (string)data["teamSide"] == (string)record["teamInAttack"];
                SetField(token, "_isGk", (bool)data["isGoalKeeper"]);
                SetProperty(token, "isPlaying", (bool)data["isPlaying"]);
                SetProperty(token, "isBooked", (bool?)data["isBooked"] ?? false);
                var hex = data["currentHex"].Type == JTokenType.Null ? null : Hex(data["currentHex"].ToObject<RoomHexCoordinates>());
                SetProperty(token, "currentHex", hex);
                if (hex != null)
                {
                    if (hex.occupyingToken != null) throw new InvalidDataException("Fixture contains overlapping players.");
                    hex.occupyingToken = token;
                    hex.isAttackOccupied = token.isAttacker;
                    hex.isDefenseOccupied = !token.isAttacker;
                }
                tokens.Add((string)data["tokenKey"], token);
                match.playerTokenManager.allTokens.Add(token);
            }
            string lastTouch = (string)ownership["movementReplay"]["lastPurposefulTouchTokenKey"];
            match.LastTokenToTouchTheBallOnPurpose = match.ball.GetCurrentHex().GetOccupyingToken()
                ?? (lastTouch != null && tokens.TryGetValue(lastTouch, out var passer) ? passer : null);
            if (!match.attackHasPossession) match.hangingPassType = "ground";
            match.groundBallManager = root.AddComponent<GroundBallManager>();
            match.groundBallManager.ball = match.ball;
            match.groundBallManager.hexGrid = grid;
            match.looseBallManager = root.AddComponent<LooseBallManager>();
            match.looseBallManager.hexGrid = grid;
            match.looseBallManager.ball = match.ball;
            match.shotManager = root.AddComponent<ShotManager>();
            match.shotManager.hexGrid = grid;
            match.shotManager.ball = match.ball;
            match.shotManager.alreadyInterceptedDefs = new List<PlayerToken>();
            match.offsideManager = root.AddComponent<OffsideManager>();
            match.offsideManager.Configure(match);
            match.movementPhaseManager = root.AddComponent<MovementPhaseManager>();
            var movement = match.movementPhaseManager;
            movement.hexGrid = grid;
            movement.ball = match.ball;
            movement.groundBallManager = match.groundBallManager;
            movement.shotManager = match.shotManager;
            match.shotManager.movementPhaseManager = movement;
            var goalkeeper = root.AddComponent<GoalKeeperManager>();
            goalkeeper.hexGrid = grid;
            goalkeeper.ball = match.ball;
            goalkeeper.movementPhaseManager = movement;
            goalkeeper.shotManager = match.shotManager;
            movement.goalKeeperManager = goalkeeper;
            match.shotManager.goalKeeperManager = goalkeeper;
            movement.isActivated = true;
            movement.isAwaitingTokenSelection = true;
            movement.isMovementPhaseAttack = (string)ownership["movementReplay"]["section"] == "Attacking";
            movement.isMovementPhaseDef = (string)ownership["movementReplay"]["section"] == "Defending";
            movement.isMovementPhase2f2 = (string)ownership["movementReplay"]["section"] == "2f2";
            int remaining = (int)ownership["movementReplay"]["remainingMoves"];
            movement.attackersMoved = movement.isMovementPhaseAttack ? 4 - remaining : 4;
            movement.defendersMoved = movement.isMovementPhaseDef ? 5 - remaining : 0;
            movement.attackersMovedIn2f2 = movement.isMovementPhase2f2 ? movement.TwoForTwoMoveLimit - remaining : 0;
            foreach (string key in ownership["movementReplay"]["movedTokenKeys"].Values<string>())
                movement.movedTokens.Add(tokens[key]);
            decisions = root.AddComponent<RoomDecisionManager>();
        }
        catch { Dispose(); throw; }
    }

    public RoomAIDecisionSnapshot BoardSnapshot()
    {
        var snapshot = new RoomAIDecisionSnapshot
        {
            currentState = (string)record["currentState"], manager = (string)record["manager"],
            expectedTeam = (string)record["expectedTeam"], teamInAttack = (string)record["teamInAttack"],
            attackHasPossession = (bool)record["attackHasPossession"],
            ball = RoomHexCoordinates.FromHex(match.ball.GetCurrentHex()),
            homeTeamDirection = (string)record["homeTeamDirection"], awayTeamDirection = (string)record["awayTeamDirection"]
        };
        // Readonly collections are populated explicitly; include the current,
        // possibly replay-advanced board instead of the original positions.
        snapshot.tokens.Clear();
        snapshot.tokens.AddRange(record["tokens"].ToObject<List<RoomAITokenSnapshot>>());
        foreach (var token in snapshot.tokens)
        {
            token.currentHex = RoomHexCoordinates.FromHex(tokens[token.tokenKey].GetCurrentHex());
            token.isAttacker = token.teamSide == snapshot.teamInAttack;
        }
        snapshot.tacticalMatchup = RoomAITacticalProfileBuilder.Build(snapshot);
        snapshot.freshAttackingMoveLimit = match.movementPhaseManager.AttackingMoveLimit;
        snapshot.defensiveMovesRemaining = Math.Max(0,
            match.movementPhaseManager.DefendingMoveLimit - match.movementPhaseManager.defendersMoved);
        return snapshot;
    }

    public RoomAIProviderDecision Evaluate()
    {
        var snapshot = BoardSnapshot();
        snapshot.instruction = record["instruction"].ToObject<GameplayInstructionSnapshot>();
        snapshot.instruction.instructionText = match.movementPhaseManager.GetInstructions();
        snapshot.instruction.actorTokenKey = match.movementPhaseManager.selectedToken != null
            ? MatchManager.GetStableTokenKey(match.movementPhaseManager.selectedToken) : null;
        var context = new RoomDecisionContext { manager = snapshot.manager, expectedTeam = snapshot.expectedTeam };
        match.movementPhaseManager.PopulateRoomDecisionContext(context);
        var candidates = (List<RoomActionCandidate>)Invoke(decisions, "BuildTacticalExecutableCandidates", snapshot.instruction, context, true);
        snapshot.legalActions.Clear();
        snapshot.legalActions.AddRange(candidates);
        snapshot.candidateExplanations.Clear();
        snapshot.defensivePhasePlan = (RoomAIDefensivePhasePlan)InvokeStatic(typeof(RoomAIDecisionSnapshotBuilder), "BuildDefensivePhasePlan", snapshot);
        InvokeStatic(typeof(RoomAIDecisionSnapshotBuilder), "BuildCandidateExplanations", snapshot);
        Invoke(decisions, "PrepareCommittedDefensivePhasePlan", snapshot);
        RoomAIDecisionSnapshotBuilder.ApplyDefensivePhasePlanFacts(snapshot);
        lastSnapshot = snapshot;
        var decision = CodexTacticianProvider.Instance.Decide(snapshot, candidates, attackPlan,
            new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard);
        attackPlan = decision.plan;
        return decision;
    }

    public RoomAIProviderDecision EvaluateKickoffPass()
    {
        match.currentState = MatchManager.GameState.StandardPass;
        var ground = match.groundBallManager;
        ground.isActivated = true;
        ground.isAwaitingTargetSelection = true;
        ground.isKickoffPass = true;
        match.movementPhaseManager.isActivated = false;
        var snapshot = BoardSnapshot();
        snapshot.currentState = "StandardPass";
        snapshot.manager = nameof(GroundBallManager);
        snapshot.expectedTeam = snapshot.teamInAttack;
        var context = new RoomDecisionContext { manager = snapshot.manager, expectedTeam = snapshot.expectedTeam, expectedInput = "hex" };
        ground.PopulateRoomDecisionContext(context);
        snapshot.legalActions.AddRange(context.actions.Where(candidate => candidate.isExecutableNow));
        InvokeStatic(typeof(RoomAIDecisionSnapshotBuilder), "BuildCandidateExplanations", snapshot);
        lastSnapshot = snapshot;
        return CodexTacticianProvider.Instance.Decide(snapshot, snapshot.legalActions, null,
            new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard);
    }

    public void Move(string key, RoomHexCoordinates target)
    {
        var token = tokens[key];
        var old = token.GetCurrentHex();
        var destination = Hex(target);
        if (destination != old && (destination.isAttackOccupied || destination.isDefenseOccupied))
            throw new InvalidOperationException("Replay movement destination is occupied.");
        old.occupyingToken = null;
        old.isAttackOccupied = old.isDefenseOccupied = false;
        SetProperty(token, "currentHex", destination);
        destination.occupyingToken = token;
        destination.isAttackOccupied = token.isAttacker;
        destination.isDefenseOccupied = !token.isAttacker;
        match.movementPhaseManager.movedTokens.Add(token);
        match.movementPhaseManager.selectedToken = null;
        match.movementPhaseManager.isAwaitingHexDestination = false;
        match.hexGrid.highlightedHexes.Clear();
        if (match.movementPhaseManager.isMovementPhaseAttack) match.movementPhaseManager.attackersMoved++;
        else if (match.movementPhaseManager.isMovementPhase2f2) match.movementPhaseManager.attackersMovedIn2f2++;
        else match.movementPhaseManager.defendersMoved++;
    }

    public void SelectActor(PlayerToken actor)
    {
        match.movementPhaseManager.selectedToken = actor;
        match.movementPhaseManager.isAwaitingHexDestination = true;
        int range = actor.IsDribbler ? 1 : match.movementPhaseManager.isMovementPhase2f2
            ? match.movementPhaseManager.TwoForTwoRange : actor.pace;
        var reachable = HexGridUtils.GetReachableHexes(match.hexGrid, actor.GetCurrentHex(), range).reachableHexes;
        match.hexGrid.highlightedHexes.Clear();
        match.hexGrid.highlightedHexes.AddRange(reachable.Where(hex => !hex.isOutOfBounds
            && !hex.isAttackOccupied && !hex.isDefenseOccupied));
    }

    public HexCell Hex(RoomHexCoordinates hex) => match.hexGrid.GetHexCellAt(new Vector3Int(hex.x, 0, hex.z));
    public static object Invoke(object owner, string method, params object[] args) =>
        owner.GetType().GetMethod(method, PrivateInstance).Invoke(owner, args);
    private static object InvokeStatic(Type owner, string method, params object[] args) =>
        owner.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);
    private static void SetField(object owner, string field, object value) =>
        owner.GetType().GetField(field, PrivateInstance).SetValue(owner, value);
    private static void SetProperty(object owner, string property, object value) =>
        owner.GetType().GetProperty(property).GetSetMethod(true).Invoke(owner, new[] { value });

    public void Dispose()
    {
        MatchManager.Instance = previousMatch;
        typeof(PlayerToken).GetField("ball", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, previousTokenBall);
        if (root != null) UnityEngine.Object.DestroyImmediate(root);
        if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
    }
}
