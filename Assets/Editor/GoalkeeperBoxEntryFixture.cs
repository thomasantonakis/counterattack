using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Disposable deterministic board. No Room bootstrap, save load, file writes,
/// expected outcome flags, or authored UI changes. Disabled managers still run explicit
/// gameplay coroutines on active hosts; only the tested input handlers are subscribed.</summary>
public sealed partial class GoalkeeperBoxEntryFixture : IDisposable
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly MatchManager previousMatch = MatchManager.Instance;
    private readonly object previousBall = typeof(PlayerToken).GetField("ball", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
    private readonly UnityEngine.Random.State random = UnityEngine.Random.state;
    private readonly float previousCaptureDeltaTime = Time.captureDeltaTime;
    private readonly float previousTimeScale = Time.timeScale;
    private readonly Scene previousScene = SceneManager.GetActiveScene();
    private readonly Scene scene;
    private readonly GameObject root;
    private readonly Material material;
    private readonly GameInputManager input;
    private readonly HelperFunctions helper;
    private readonly HashSet<MonoBehaviour> manuallySubscribed = new();
    private readonly object previousKeyHandlers = typeof(GameInputManager)
        .GetField("OnKeyPress", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
    public readonly MatchManager match;
    public readonly HexGrid grid;
    public readonly Ball ball;
    public readonly GoalKeeperManager keeper;
    public readonly MovementPhaseManager movement;
    public readonly GroundBallManager ground;
    public readonly FirstTimePassManager ftp;
    public readonly HighPassManager high;
    public readonly LongBallManager longBall;
    public readonly LooseBallManager loose;
    public readonly PlayerToken attacker, defender, homeGK, awayGK;
    private bool disposed;
    private AsyncOperation unload;

    public GoalkeeperBoxEntryFixture()
    {
        scene = Application.isPlaying ? SceneManager.CreateScene("Goalkeeper box entry fixture")
            : EditorSceneManager.NewPreviewScene();
        root = new GameObject("Goalkeeper test board");
        SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            // Keep MatchManager inactive: its normal Awake/Start loads the user's match.
            var matchObject = Child("Isolated MatchManager");
            match = matchObject.AddComponent<MatchManager>();
            MatchManager.Instance = match;
            match.gameData = new MatchManager.GameData();
            match.gameData.gameSettings.homeTeamName = "Fixture Home";
            match.gameData.gameSettings.awayTeamName = "Fixture Away";
            match.difficulty_level = 2;
            match.teamInAttack = MatchManager.TeamInAttack.Home;
            match.currentState = MatchManager.GameState.AnyOtherScenario;
            grid = Add<HexGrid>(); match.hexGrid = grid;
            material = new Material(Shader.Find("Sprites/Default"));
            grid.cells = new HexCell[grid.GridWidth, grid.GridHeight];
            var assignFeatures = typeof(HexGrid).GetMethod("AssignHexFeatures", Private);
            for (int x = 0; x < grid.GridWidth; x++)
                for (int z = 0; z < grid.GridHeight; z++)
                {
                    var obj = Child($"Hex {x},{z}");
                    var renderer = obj.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                    var hex = obj.AddComponent<HexCell>(); hex.enabled = false;
                    hex.hexRenderer = renderer;
                    hex.coordinates = new Vector3Int(x - grid.GridWidth / 2, 0, z - grid.GridHeight / 2);
                    hex.hexRadius = grid.HexRadius;
                    obj.transform.position = grid.GetHexCenterForCoordinates(hex.coordinates);
                    grid.cells[x, z] = hex;
                    assignFeatures.Invoke(grid, new object[] { hex });
                    obj.SetActive(true);
                }
            ball = Add<Ball>(); grid.ball = ball; match.ball = ball; ball.hexGrid = grid;
            PlayerToken.SetBallReference(ball);
            match.playerTokenManager = Add<PlayerTokenManager>();
            movement = Add<MovementPhaseManager>(); match.movementPhaseManager = movement;
            ground = Add<GroundBallManager>(); match.groundBallManager = ground;
            ftp = Add<FirstTimePassManager>(); match.firstTimePassManager = ftp;
            high = Add<HighPassManager>(); match.highPassManager = high;
            longBall = Add<LongBallManager>(); match.longBallManager = longBall;
            loose = Add<LooseBallManager>(); match.looseBallManager = loose;
            loose.defendersTriedToIntercept = new List<PlayerToken>();
            keeper = Add<GoalKeeperManager>(); ball.goalKeeperManager = keeper;
            match.shotManager = Add<ShotManager>();
            Add<HeaderManager>();
            match.finalThirdManager = Add<FinalThirdManager>();
            match.freeKickManager = Add<FreeKickManager>();
            helper = Add<HelperFunctions>();
            input = Add<GameInputManager>();
            // Wire by dependency type, just as scene references do. No gameplay flags
            // or outcomes are configured by this wiring.
            var components = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var owner in components)
                foreach (var field in owner.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                    if (typeof(MonoBehaviour).IsAssignableFrom(field.FieldType)
                        && field.FieldType != typeof(HexCell) && field.FieldType != typeof(PlayerToken))
                    {
                        var dependency = components.FirstOrDefault(c => field.FieldType.IsInstanceOfType(c));
                        if (dependency != null) field.SetValue(owner, dependency);
                    }
            attacker = Token("Home carrier", true, false, 9);
            defender = Token("Away recovering player", false, false, 8);
            homeGK = Token("Home keeper", true, true, 1);
            awayGK = Token("Away keeper", false, true, 1);
            ball.PlaceAtCell(Hex(0));
            // Real event subscriptions without unrelated Update/Start loops.
            Subscribe(keeper);
        }
        catch { Dispose(); throw; }
    }

    private GameObject Child(string name)
    {
        var obj = new GameObject(name); obj.SetActive(false); obj.transform.SetParent(root.transform); return obj;
    }
    private T Add<T>() where T : MonoBehaviour
    {
        var obj = Child(typeof(T).Name); var component = obj.AddComponent<T>();
        component.enabled = false; obj.SetActive(true); return component;
    }
    private PlayerToken Token(string name, bool home, bool gk, int jersey)
    {
        var token = Add<PlayerToken>(); token.name = name; token.playerName = name;
        token.isHomeTeam = home; token.isAttacker = home; token.jerseyNumber = jersey;
        token.pace = 5; token.dribbling = 4; token.tackling = 4; token.saving = 4;
        Set(token, "_isGk", gk); match.playerTokenManager.allTokens.Add(token); return token;
    }
    public HexCell Hex(int x, int z = 0) => grid.GetHexCellAt(new Vector3Int(x, 0, z));
    public void Place(PlayerToken token, HexCell hex)
    {
        var old = token.GetCurrentHex();
        if (old != null) { old.occupyingToken = null; old.isAttackOccupied = old.isDefenseOccupied = false; }
        hex.isAttackOccupied = token.isAttacker; hex.isDefenseOccupied = !token.isAttacker;
        token.SetCurrentHex(hex); token.transform.position = hex.GetHexCenter();
    }
    public static object Invoke(object owner, string method, params object[] args)
        => owner.GetType().GetMethod(method, Private).Invoke(owner, args);
    public static void Set(object owner, string field, object value)
        => owner.GetType().GetField(field, Private).SetValue(owner, value);

    private void Subscribe(MonoBehaviour owner)
    {
        if (manuallySubscribed.Add(owner)) Invoke(owner, "OnEnable");
    }

    public void Board(int side)
    {
        match.homeTeamDirection = side == 1 ? MatchManager.TeamAttackingDirection.LeftToRight : MatchManager.TeamAttackingDirection.RightToLeft;
        match.awayTeamDirection = side == 1 ? MatchManager.TeamAttackingDirection.RightToLeft : MatchManager.TeamAttackingDirection.LeftToRight;
        Place(homeGK, Hex(-16 * side)); Place(awayGK, Hex(16 * side));
        Place(attacker, Hex(11 * side)); Place(defender, Hex(0, 10));
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker); match.attackHasPossession = true;
    }

    public IEnumerator Run(string scenario)
    {
        Assert.That(Application.isPlaying, Is.True, "This fixture must execute in framework PlayMode.");
        string[] parts = scenario.Split('/'); int side = parts[0] == "Right" ? 1 : -1;
        string branch = parts[1]; bool move = parts[2] == "move";
        Board(side);
        if (branch == "loose_own_box") { yield return LooseAttractionIntoOwnBox(side); yield break; }
        if (branch.Contains("scatter")) { yield return Scatter(branch, side, move); yield break; }
        if (branch.StartsWith("lost_")) { yield return LostGoalkeeperDuel(branch, side, move); yield break; }
        if (branch is "tackle_stay" or "tackle_reposition") { yield return WonTackle(branch, side, move); yield break; }
        bool recovery = branch.EndsWith("recovery");
        HexCell target = Hex(12 * side);
        bool inaccurate = branch.Contains("_inaccurate_");
        bool aerial = !recovery && (branch.StartsWith("high") || branch.StartsWith("long"));
        if (inaccurate) target = Hex((branch.EndsWith("_inside") ? 12 : 10) * side, 1);
        bool nonqualifying = branch is "own_box" or "outside" or "same_box_recovery" || branch.EndsWith("_outside");
        IEnumerator flow;
        if (recovery)
        {
            target = Hex(-12 * side);
            Place(attacker, Hex(-11 * side));
            if (branch == "same_box_recovery") Place(attacker, Hex(-13 * side));
            Place(defender, target); ball.PlaceAtCell(attacker.currentHex);
            match.SetLastToken(attacker);
            if (branch == "header_recovery")
            {
                var header = match.shotManager.headerManager;
                header.interceptingDefenders = new List<HexCell> { target };
                flow = HeaderRecovery(header);
            }
            else if (branch == "long_recovery")
            {
                Set(longBall, "interceptingDefenders", new List<HexCell> { target });
                flow = (IEnumerator)typeof(LongBallManager).GetMethod("PerformInterceptionCheck", Private, null,
                    new[] { typeof(HexCell), typeof(int?) }, null).Invoke(longBall, new object[] { attacker.currentHex, (int?)6 });
            }
            else if (branch == "loose_recovery") flow = (IEnumerator)Invoke(loose, "MoveToSuccessfulLooseBallInterceptor", defender, target);
            else
            {
                // The winning roll owns this purposeful touch before its movement continuation.
                match.SetLastToken(defender);
                object owner = branch == "ftp_recovery" ? ftp : branch == "steal_recovery" ? movement : ground;
                flow = (IEnumerator)Invoke(owner, "HandleBallInterception", target);
            }
        }
        else if (branch == "dribble") flow = movement.MoveTokenToHex(target, attacker, false, false);
        else if (branch == "reposition")
        {
            movement.repositionWinner = attacker;
            movement.repositionLoser = defender;
            Place(defender, Hex(11 * side, -1));
            Invoke(movement, "FindRepositionHexes");
            Assert.That(movement.repositionHexes,
                Does.Contain(target), "Reposition must use a legal destination calculated by gameplay.");
            movement.isWaitingForReposition = true;
            var task = (Task)Invoke(movement, "AsyncRepositionTokenToHex", target);
            flow = Await(task);
        }
        else if (branch == "ftp") flow = ftp.HandleGroundBallMovement(target);
        else if (branch == "long_direct")
        {
            Place(defender, target);
            flow = longBall.HandleLongBallMovement(target);
        }
        else if (aerial) flow = AerialAccuracy(target, branch.StartsWith("high"), inaccurate);
        else if (branch == "own_box")
        {
            target = Hex(-12 * side); Place(attacker, Hex(-11 * side)); ball.PlaceAtCell(attacker.currentHex);
            flow = ground.HandleGroundBallMovement(target);
        }
        else if (branch == "outside") { target = Hex(10 * side); flow = ground.HandleGroundBallMovement(target); }
        else flow = ground.HandleGroundBallMovement(target);

        bool complete = false;
        if (branch == "loose_recovery")
        {
            // Exercise arrival just short of the box-line destination. The final
            // positional snap is still an entry and must run the normal GK decision.
            Time.captureDeltaTime = 0.03206f;
            yield return null; // Apply the controlled clock before starting movement.
        }
        Assert.That(grid.CheckPenaltyBox(ball.transform.position), Is.EqualTo(ball.GetCurrentHex().isInPenaltyBox),
            "Deterministic board's visual ball position and source-hex box classification must agree before movement.");
        TestContext.WriteLine($"Starting {scenario}: source={ball.GetCurrentHex().coordinates}, position={ball.transform.position}, "
            + $"physicalBox={grid.CheckPenaltyBox(ball.transform.position)}, target={target.coordinates}, frame={Time.frameCount}.");
        ground.StartCoroutine(Observe(flow, () => complete = true));
        if (nonqualifying)
        {
            yield return Until(() => complete || keeper.isActivated, "same-box recovery completes",
                aerial && branch.StartsWith("high") ? 120 : 0);
            Assert.That(keeper.isActivated, Is.False, "No new entry into the defending team's box.");
            Assert.That(complete, Is.True);
            Assert.That(ball.GetCurrentHex(), Is.SameAs(target)); yield break;
        }
        yield return Until(() => keeper.isActivated, $"{branch} offers a GK move",
            aerial && branch.StartsWith("high") ? 120 : 0);
        PlayerToken expectedGK = recovery ? homeGK : awayGK;
        Assert.That(keeper.GetCurrentDefendingGoalkeeper(), Is.SameAs(expectedGK));
        Assert.That(expectedGK.isAttacker, Is.False);
        if (recovery)
        {
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away), "Possession precedes attraction.");
            Assert.That(defender.isAttacker, Is.True);
        }
        if (branch == "long_direct")
        {
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Home), "The attack's landing entry precedes passive recovery.");
            Assert.That(defender.isAttacker, Is.False);
        }
        Assert.That(complete, Is.False, "Continuation must wait for the goalkeeper decision.");
        if (aerial) Assert.That(ball.GetCurrentHex(), Is.SameAs(target), "Aerial free move follows landing.");
        yield return Choose(expectedGK, move);
        // High/long have a distinct subsequent GK challenge/pace decision. That
        // continuation is observed here, then stopped by fixture cleanup.
        if (aerial && branch.StartsWith("high"))
            yield return Until(() => high.isWaitingForDefGKChallengeDecision,
                "high pass continues after free move");
        else if (aerial)
            yield return Until(() => longBall.isWaitingForDefLBMove || complete, "long ball continues after free move");
        else yield return Until(() => complete, $"{branch} completes after decision");
        Assert.That(ball.GetCurrentHex(), Is.SameAs(target));
        if (branch == "long_direct")
        {
            Assert.That(complete, Is.True, "Direct defender landing must finish after the free move.");
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
            Assert.That(defender.isAttacker && !attacker.isAttacker, Is.True, "Passive recovery changes possession exactly once.");
        }
        Assert.That(keeper.HasConsumedBoxMoveForDecisionPreview(target.isInPenaltyBox), Is.True);
        Assert.That(keeper.ShouldGKMove(target), Is.False, "The same entry must not be offered twice.");
    }

    private IEnumerator LooseAttractionIntoOwnBox(int side)
    {
        Place(attacker, Hex(-11 * side)); Place(defender, Hex(-12 * side));
        ball.PlaceAtCell(attacker.currentHex);
        loose.StartCoroutine(loose.ResolveLooseBall(defender, LooseBallSourceType.GroundDeflection,
            true, LooseBallPresentationContext.GroundDuelResolution));
        yield return Until(() => loose.isWaitingForDirectionRoll, "own-box deflection reaches rolls without a goalkeeper interruption");
        Assert.That(ball.GetCurrentHex(), Is.SameAs(defender.currentHex));
        Assert.That(keeper.isActivated, Is.False);
        Assert.That(keeper.HasConsumedBoxMoveForDecisionPreview(-side), Is.False);
        Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Home));
        Assert.That(homeGK.isAttacker && !defender.isAttacker, Is.True);
        Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(attacker));
    }

    private IEnumerator HeaderRecovery(HeaderManager header)
    {
        bool complete = false;
        header.StartCoroutine(Observe((IEnumerator)Invoke(header, "PerformInterceptionCheck"), () => complete = true));
        yield return Until(() => header.isWaitingForInterceptionRoll, "header recovery roll");
        header.PerformInterceptionRoll(6);
        yield return Until(() => complete, "header recovery completes");
    }

    private IEnumerator AerialAccuracy(HexCell target, bool isHigh, bool inaccurate = false)
    {
        attacker.highPass = 4;
        int side = Math.Sign(target.coordinates.x);
        HexCell intended = inaccurate ? Hex((target.isInPenaltyBox != 0 ? 10 : 12) * side) : target;
        HexCell origin = ball.GetCurrentHex();
        if (isHigh)
        {
            high.isActivated = true; high.isWaitingForAccuracyRoll = true;
            high.currentTargetHex = high.intendedTargetHex = intended;
            high.PerformAccuracyRoll(inaccurate ? 1 : 6);
        }
        else
        {
            longBall.isActivated = true; longBall.isWaitingForAccuracyRoll = true;
            longBall.currentTargetHex = intended;
            LongRoll("PerformAccuracyRoll", inaccurate ? 1 : 6);
        }
        if (inaccurate)
        {
            yield return Until(() => isHigh ? high.isWaitingForDirectionRoll : longBall.isWaitingForDirectionRoll,
                "inaccurate aerial pass waits for direction");
            Assert.That(keeper.isActivated, Is.False, "Intended destination cannot offer a move before accuracy/scatter resolves.");
            Assert.That(ball.GetCurrentHex(), Is.SameAs(origin));
            int direction = target.isInPenaltyBox != 0 ? (side == 1 ? 5 : 3) : (side == 1 ? 3 : 5);
            if (isHigh) high.PerformDirectionRoll(direction); else LongRoll("PerformDirectionRoll", direction);
            yield return Until(() => isHigh ? high.isWaitingForDistanceRoll : longBall.isWaitingForDistanceRoll,
                "inaccurate aerial pass waits for distance");
            Assert.That(keeper.isActivated, Is.False);
            Assert.That(ball.GetCurrentHex(), Is.SameAs(origin));
            if (isHigh) high.PerformDistanceRoll(2); else LongRoll("PerformDistanceRoll", 2);
        }
        if (target.isInPenaltyBox == 0)
            yield return Until(() => ball.GetCurrentHex() == target, "resolved outside destination lands without a box move", isHigh ? 120 : 0);
        else if (isHigh)
            yield return Until(() => high.isWaitingForDefGKChallengeDecision, "high pass continues to GK rush decision", 120);
        else yield return Until(() => longBall.isWaitingForDefLBMove, "long ball continues to GK pace decision");
    }

    private void LongRoll(string method, int roll)
    {
        object result = typeof(LongBallManager).GetMethod(method, Private, null, new[] { typeof(int?) }, null)
            .Invoke(longBall, new object[] { (int?)roll });
        if (result is IEnumerator flow) longBall.StartCoroutine(flow);
    }

    private IEnumerator WonTackle(string branch, int side, bool move)
    {
        HexCell target = Hex(-12 * side);
        Place(attacker, Hex(-11 * side));
        Place(defender, branch == "tackle_stay" ? target : Hex(-11 * side, -1));
        ball.PlaceAtCell(attacker.currentHex);
        match.currentState = MatchManager.GameState.MovementPhase;
        movement.isActivated = true; movement.isMovementPhaseDef = true;
        movement.selectedToken = movement.selectedDefender = defender;
        movement.isWaitingForTackleDecision = true;
        Invoke(movement, "OnKeyReceived", new KeyPressData(KeyCode.T, false, false, false));
        movement.PerformTackleDiceRoll(true, 6);
        movement.PerformTackleDiceRoll(false, 2);
        Task reposition = null;
        if (branch == "tackle_reposition")
        {
            yield return Until(() => movement.isWaitingForReposition, "outside tackle winner can reposition");
            Assert.That(keeper.isActivated, Is.False);
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
            Assert.That(defender.isAttacker, Is.True);
            reposition = (Task)Invoke(movement, "AsyncRepositionTokenToHex", target);
        }
        yield return Until(() => keeper.isActivated, "won tackle enters newly defending team's box");
        Assert.That(keeper.GetCurrentDefendingGoalkeeper(), Is.SameAs(homeGK));
        Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
        Assert.That(homeGK.isAttacker, Is.False);
        Assert.That(movement.IsSuccessfulTackleRepositionPendingForDecision || branch == "tackle_stay", Is.True);
        yield return Choose(homeGK, move);
        if (branch == "tackle_stay")
        {
            yield return Until(() => movement.isWaitingForReposition, "tackle attraction completes before reposition");
            Invoke(movement, "OnKeyReceived", new KeyPressData(KeyCode.X, false, false, false));
        }
        else
        {
            yield return Until(() => reposition.IsCompleted, "tackle reposition completes after nested GK decision");
            Assert.That(reposition.IsFaulted, Is.False, reposition.Exception?.ToString());
        }
        yield return Until(() => match.currentState == MatchManager.GameState.SuccessfulTackle && !movement.isActivated,
            "won tackle finishes its continuation");
        Assert.That(ball.GetCurrentHex(), Is.SameAs(target));
        Assert.That(defender.isAttacker && !attacker.isAttacker, Is.True);
        Assert.That(keeper.ShouldGKMove(target), Is.False);
    }

    private IEnumerator LostGoalkeeperDuel(string branch, int side, bool move)
    {
        // Real defensive movement creates the outside-box challenge; public dice
        // entry points then resolve it before the attacker selects a reposition.
        match.currentState = MatchManager.GameState.MovementPhase;
        movement.isActivated = true; movement.isMovementPhaseDef = true;
        movement.selectedToken = awayGK;
        movement.HighlightValidMovementHexes(awayGK, awayGK.pace);
        yield return movement.MoveTokenToHex(Hex(12 * side), awayGK, true, false);
        yield return Until(() => movement.isWaitingForTackleDecision, "outside-box goalkeeper challenge");
        Invoke(movement, "OnKeyReceived", new KeyPressData(branch == "lost_dive" ? KeyCode.D : KeyCode.T, false, false, false));
        yield return Until(() => movement.isWaitingForTackleRoll, "outside-box duel rolls");
        movement.PerformTackleDiceRoll(true, 3);
        movement.PerformTackleDiceRoll(false, 6);
        yield return Until(() => movement.isWaitingForReposition, "attacker wins and can reposition");
        Assert.That(keeper.isActivated, Is.False);
        HexCell target = Hex(12 * side, 1);
        var task = (Task)Invoke(movement, "AsyncRepositionTokenToHex", target);
        yield return Until(() => keeper.isActivated, "beaten goalkeeper remains eligible after entry");
        Assert.That(keeper.GetCurrentDefendingGoalkeeper(), Is.SameAs(awayGK));
        Assert.That(ball.GetCurrentHex(), Is.SameAs(target));
        yield return Choose(awayGK, move);
        yield return Until(() => task.IsCompleted && !movement.isWaitingForReposition, "reposition resumes after GK choice");
        Assert.That(task.IsFaulted, Is.False, task.Exception?.ToString());
        Assert.That(keeper.ShouldGKMove(target), Is.False);
    }

    private IEnumerator Scatter(string branch, int side, bool move)
    {
        bool header = branch.StartsWith("header"); bool shot = branch.StartsWith("shot");
        bool inside = branch.EndsWith("inside");
        Place(defender, Hex(12 * side));
        var type = header ? LooseBallSourceType.HeaderDeflection : LooseBallSourceType.GroundDeflection;
        var context = shot ? LooseBallPresentationContext.ShotResolution
            : header ? LooseBallPresentationContext.Standalone : LooseBallPresentationContext.GroundDuelResolution;
        loose.StartCoroutine(loose.ResolveLooseBall(defender, type, true, context));
        if (shot)
        {
            yield return Until(() => keeper.isActivated, "blocked shot offers before scatter rolls");
            Assert.That(loose.isWaitingForDirectionRoll, Is.False);
            yield return Choose(awayGK, move);
        }
        yield return Until(() => loose.isWaitingForDirectionRoll, "scatter direction opens");
        Assert.That(keeper.isActivated, Is.False, "Initial tackle/header attraction must not offer a move.");
        Assert.That(keeper.HasConsumedBoxMoveForDecisionPreview(side), Is.EqualTo(shot));
        Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Home));
        Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(attacker));
        Vector3 beforeFlight = ball.transform.position;
        // Explicit hex expectations, with mirrored NE/NW and SW/SE dice.
        int direction = inside ? (side == 1 ? 5 : 3) : (side == 1 ? 2 : 6);
        HexCell destination = Hex((inside ? 14 : 10) * side, inside ? 1 : -1);
        loose.PerformDirectionRoll(direction);
        yield return Until(() => loose.isWaitingForDistanceRoll, "scatter distance opens");
        loose.PerformDistanceRoll(2);
        if (inside && !shot)
        {
            yield return Until(() => keeper.isActivated, "rolled destination offers before flight");
            Assert.That(ball.transform.position, Is.EqualTo(beforeFlight), "No movement to the boundary before the GK decision.");
            Assert.That(ball.GetCurrentHex(), Is.SameAs(defender.currentHex));
            Assert.That(keeper.GetCurrentDefendingGoalkeeper(), Is.SameAs(awayGK));
            yield return Choose(awayGK, move);
        }
        yield return Until(() => ball.GetCurrentHex() == destination || keeper.isActivated, "scatter reaches rolled destination");
        Assert.That(keeper.isActivated, Is.False, "No extra offer after a shot entry, or for outside scatter.");
        Assert.That(ball.GetCurrentHex(), Is.SameAs(destination));
        Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Home));
    }

    private IEnumerator Choose(PlayerToken gk, bool move)
    {
        yield return Until(() => grid.highlightedHexes.Count > 0, "GK destinations highlighted");
        var origin = gk.currentHex;
        if (move)
        {
            HexCell destination = grid.highlightedHexes.First(h => h != origin);
            Assert.That(HexGridUtils.GetHexStepDistance(origin, destination), Is.EqualTo(1));
            Assert.That(input.TryExecuteSyntheticHexClick(destination, "Test Framework", out string error), Is.True, error);
            yield return Until(() => !keeper.isActivated && !keeper.IsResolvingMove, "GK movement completes");
            Assert.That(gk.currentHex, Is.SameAs(destination));
        }
        else
        {
            Assert.That(input.TryExecuteSyntheticKeyPress("X", "Test Framework", out string error), Is.True, error);
            yield return Until(() => !keeper.isActivated, "GK skips");
            Assert.That(gk.currentHex, Is.SameAs(origin));
        }
    }
    private static IEnumerator Observe(IEnumerator flow, Action complete) { yield return flow; complete(); }
    private static IEnumerator Await(Task task)
    {
        while (!task.IsCompleted) yield return null;
        if (task.IsFaulted) throw task.Exception;
    }
    public IEnumerator Until(Func<bool> condition, string state, int minimumFrames = 0)
    {
        var waitClock = System.Diagnostics.Stopwatch.StartNew();
        int startFrame = Time.frameCount;
        while (!condition())
        {
            // High Pass flight is authored as 91 rendered frames. Preserve the
            // normal eight-second bound while allowing that sequence at a slow
            // Editor frame rate; every wait still has a 30-second watchdog.
            int frames = Time.frameCount - startFrame;
            if (waitClock.Elapsed.TotalSeconds >= 30 || waitClock.Elapsed.TotalSeconds >= 8 && frames >= minimumFrames)
                Assert.Fail($"Timed out: {state}. elapsed={waitClock.Elapsed.TotalSeconds:0.00}s, frames={frames}, "
                    + $"Ball={ball.GetCurrentHex()?.coordinates}, position={ball.transform.position}, "
                    + $"moving={ball.isMoving}, cellMove={ball.IsCellMoveInProgress}, GK={keeper.isActivated}, "
                    + $"touch={match.LastTokenToTouchTheBallOnPurpose?.name}, attack={match.teamInAttack}, "
                    + $"loose={loose.GetDebugStatus()}, movement={movement.GetDebugStatus()}");
            yield return null;
        }
    }
    public void Dispose()
    {
        if (disposed) return; disposed = true;
        if (root != null)
        {
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true)) component.StopAllCoroutines();
            foreach (var owner in manuallySubscribed)
                if (owner != null) Invoke(owner, "OnDisable");
            manuallySubscribed.Clear();
            // Enabled presentation owners can restore board highlights in OnDisable.
            // Run that lifecycle while their match and tokens still exist, before
            // removing the singleton to protect the user's save from OnDestroy.
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (component != null && component.enabled) component.enabled = false;
            // MatchManager.OnDestroy must never flush this in-memory match through
            // an ApplicationManager that may still point at the user's active save.
            MatchManager.Instance = null;
            var ownedMaterials = root.GetComponentsInChildren<Renderer>(true)
                .Select(r => r.sharedMaterial)
                .Where(m => m != null && m != material && !UnityEditor.EditorUtility.IsPersistent(m))
                .Distinct().ToArray();
            Object.DestroyImmediate(root);
            foreach (var owned in ownedMaterials) Object.DestroyImmediate(owned);
        }
        if (material != null) Object.DestroyImmediate(material);
        restoreAuthoredPanelState?.Invoke();
        restoreAuthoredPanelState = null;
        MatchManager.Instance = previousMatch;
        typeof(PlayerToken).GetField("ball", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, previousBall);
        UnityEngine.Random.state = random;
        Time.captureDeltaTime = previousCaptureDeltaTime;
        Time.timeScale = previousTimeScale;
        if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
        if (scene.IsValid())
        {
            if (Application.isPlaying) unload = SceneManager.UnloadSceneAsync(scene);
            else EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    public IEnumerator Cleanup()
    {
        Dispose();
        if (unload != null) yield return unload;
    }

    public IEnumerator VerifyFailureCleanup()
    {
        var previousDuel = DuelResolutionPanelController.Instance;
        var previousHeader = HeaderChallengePanelController.Instance;
        var previousStoppage = StoppageActionsPanelController.Instance;
        AttachAuthoredPanels();
        Assert.That(DuelResolutionPanelController.Instance, Is.SameAs(duelPanel));
        Time.timeScale = 0.75f;
        Task pending = helper.StartCoroutineAndWait(NeverCompletes());
        yield return null;
        Assert.That(pending.IsCompleted, Is.False);
        UnityEngine.Random.Range(0, 100);
        yield return Cleanup();
        Assert.That(pending.IsCanceled, Is.True, "Fixture destruction must release async waiters even on failure.");
        Assert.That(MatchManager.Instance, Is.SameAs(previousMatch));
        Assert.That(typeof(PlayerToken).GetField("ball", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null), Is.SameAs(previousBall));
        Assert.That(typeof(GameInputManager).GetField("OnKeyPress", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null), Is.EqualTo(previousKeyHandlers));
        Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(previousScene));
        Assert.That(scene.isLoaded, Is.False);
        Assert.That(UnityEngine.Random.state, Is.EqualTo(random));
        Assert.That(Time.timeScale, Is.EqualTo(previousTimeScale));
        Assert.That(DuelResolutionPanelController.Instance, Is.SameAs(previousDuel));
        Assert.That(HeaderChallengePanelController.Instance, Is.SameAs(previousHeader));
        Assert.That(StoppageActionsPanelController.Instance, Is.SameAs(previousStoppage));
    }
    private static IEnumerator NeverCompletes() { while (true) yield return null; }
}
