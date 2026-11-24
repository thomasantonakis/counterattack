using System.Collections;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

public sealed partial class OnlineFullRoomSceneFixture
{
    public IEnumerator RunOnlineKickoffAssistance(int assistance)
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var data = Data(16, false);
        data.gameSettings.playerAssistance = assistance;
        var protocol = NativePresentationProtocol();
        yield return LoadNativeRoom(data, false, protocol.MatchId);
        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var match = board.match;
        var owner = match.teamInAttack == MatchManager.TeamInAttack.Home ? OnlineTeam.Home : OnlineTeam.Away;
        string connection = owner == OnlineTeam.Home ? "h" : "a";
        var taker = board.tokens.First(token => token.isPlaying && !token.IsGoalKeeper && token.isHomeTeam == (owner == OnlineTeam.Home));
        taker.SetCurrentHex(match.ball.GetCurrentHex());
        typeof(KickoffManager).GetMethod("DeactivateSetup", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(match.kickoffManager, null);
        match.StartMatch();
        pause = new OnlineSharedPause(protocol, match);
        var authority = new OnlineGroundPassAuthority(match, protocol);
        ConfigureFixtureTeam(board, owner);
        board.controls.Requested += message =>
        {
            var reply = authority.Submit(connection, message.ToCommand());
            Assert.That(reply.Status, Is.EqualTo(OnlineCommandStatus.Accepted));
            board.controls.Reply(new OnlineGroundPassMessage { status = reply.Status.ToString() });
            board.Display(pause.Capture(authority), "host");
        };
        board.Display(pause.Capture(authority), "host");
        Assert.That(board.boardInput.TryKey(KeyCode.P), Is.True);
        var targetState = pause.Capture(authority);
        Assert.That(targetState.stage, Is.EqualTo("target"));
        var target = board.cells.First(cell => cell.GetOccupyingToken() == null && cell.isInGoal == 0
            && targetState.legalActions.Contains(OnlineGroundPassAuthority.TargetAction(cell.coordinates.x, cell.coordinates.z)));
        var hover = typeof(OnlineBoardInput).GetMethod("PresentGroundPassHover", BindingFlags.Instance | BindingFlags.NonPublic);
        var highlight = typeof(HexCell).GetField("onlinePassHover", BindingFlags.Instance | BindingFlags.NonPublic);
        hover.Invoke(board.boardInput, new object[] { target });
        Assert.That(highlight.GetValue(target), Is.EqualTo(assistance != 3));
        hover.Invoke(board.boardInput, new object[] { null });
        Assert.That(highlight.GetValue(target), Is.False);
    }

    public IEnumerator RunGuestGoalTargetsAndStatus()
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var data = Data(16, false);
        var protocol = NativePresentationProtocol();
        yield return LoadNativeRoom(data, false, protocol.MatchId);
        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var header = board.match.highPassManager.headerManager;
        var movement = board.match.movementPhaseManager;
        var from = board.cells.First(cell => cell.HeadingPaths?.Count > 0);
        board.match.ball.PlaceAtCell(from);
        header.LiftHexesInGoal();
        var targets = from.HeadingPaths.Keys.Where(cell => cell != null).Select(cell => cell.coordinates).ToArray();
        Assert.That(targets, Is.Not.Empty);
        movement.stunnedTokens.Add(board.tokens.Single(token => token.isHomeTeam && token.jerseyNumber == 7));
        header.attackerWillJump.Add(board.tokens.Single(token => token.isHomeTeam && token.jerseyNumber == 8));
        movement.stunnedforNext.Add(board.tokens.Single(token => !token.isHomeTeam && token.jerseyNumber == 9));
        var authority = new OnlineGroundPassAuthority(board.match, protocol);
        pause = new OnlineSharedPause(protocol, board.match);
        var published = pause.Capture(authority);
        foreach (var target in targets)
            Assert.That(published.hexPreviews.Any(cell => cell.x == target.x && cell.z == target.z && cell.height >= .03f), Is.True);

        header.isActivated = true;
        header.isWaitingForHeaderAtGoal = true;
        var hoveredTarget = board.grid.GetHexCellAt(targets[0]);
        typeof(HeaderManager).GetMethod("RefreshHeaderAtGoalHover", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(header, new object[] { hoveredTarget });
        Assert.That(hoveredTarget.HighlightReason, Is.EqualTo("ShotTargetHover"));
        var hovered = pause.Capture(authority);

        header.isWaitingForHeaderAtGoal = false;
        header.isActivated = false;
        typeof(HeaderManager).GetMethod("ClearHeaderAtGoalTargets", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(header, null);
        movement.stunnedTokens.Clear();
        header.attackerWillJump.Clear();
        movement.stunnedforNext.Clear();
        var cleared = pause.Capture(authority);
        pause.Dispose();
        pause = null;
        yield return LoadPresentationGuest(data, protocol.MatchId, nativeRoom: true);
        board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var replica = new OnlineGroundPassReplica(protocol.MatchId, board.tokens.Select(OnlineRollPublication.ActorId), OnlineFullRoomBootstrap.Catalog);
        var status = Object.FindAnyObjectByType<MovementPhaseTokenStatusPanel>();
        Assert.That(status, Is.Not.Null);
        var cells = targets.Select(position => board.grid.GetHexCellAt(position)).ToArray();
        var originalHeights = cells.Select(cell => cell.transform.position.y).ToArray();
        ApplyPublication(replica, board, published);
        yield return null;
        foreach (var cell in cells) Assert.That(cell.transform.position.y, Is.GreaterThanOrEqualTo(.03f));
        var numbers = status.GetComponentsInChildren<MovementPhaseMovedTokenSlotView>(false)
            .Select(SlotNumber).ToArray();
        Assert.That(numbers, Does.Contain("7").And.Contain("8").And.Contain("9"));
        ApplyPublication(replica, board, hovered);
        foreach (var cell in cells)
        {
            Assert.That(cell.transform.position.y, Is.GreaterThanOrEqualTo(.03f), $"Host hover dropped {cell.coordinates}");
            Assert.That(cell.HighlightReason, Is.EqualTo("CanShootFrom"), $"Host hover changed the base highlight at {cell.coordinates}");
        }
        ApplyPublication(replica, board, cleared);
        Assert.That(status.GetComponent<CanvasGroup>().alpha, Is.Zero);
        Assert.That(cells.Select(cell => cell.transform.position.y).ToArray(), Is.EqualTo(originalHeights));
        Assert.That(cells.Select(cell => cell.HighlightReason), Has.None.EqualTo("CanShootFrom"));
    }

    public IEnumerator RunGoalResetWithOccupiedDestination(int goalSide, bool online)
    {
        var protocol = NativePresentationProtocol();
        yield return LoadTokenResetRoom(protocol, online, goalSide < 0);
        var match = MatchManager.Instance;
        var (home, away) = PlaceResetFormations(match);
        var blocker = home.Single(token => token.jerseyNumber == 11);
        var destination = new Vector3Int(10 * goalSide, 0, 4 * goalSide);
        var awayFormation = GoalFlowManager.GetResetFormationCoordinatesForDirection(match.awayTeamDirection);
        var defender = away[System.Array.IndexOf(awayFormation, destination)];
        var defenderStart = destination + new Vector3Int(0, 0, goalSide);
        PlaceResetToken(match, defender, defenderStart);
        PlaceResetToken(match, blocker, destination);
        blocker.pace = 1;
        var goal = match.hexGrid.cells.Cast<HexCell>().Single(cell => cell != null && cell.isInGoal == goalSide && cell.coordinates.z == 0);

        match.currentState = MatchManager.GameState.HeaderGeneric;
        yield return VerifyOccupiedReset(match, protocol, online, defender, blocker, destination, "goal.resolving", MatchManager.TeamInAttack.Away,
            () => match.goalFlowManager.StartGoalFlow(blocker, goal));
    }

    public IEnumerator RunHalfTimeResetWithOccupiedDestination(bool online)
    {
        var protocol = NativePresentationProtocol();
        yield return LoadTokenResetRoom(protocol, online);
        var match = MatchManager.Instance;
        var (home, away) = PlaceResetFormations(match);
        var returningKeeper = home.Single(token => token.IsGoalKeeper);
        var departingKeeper = away.Single(token => token.IsGoalKeeper);
        var destination = departingKeeper.currentHex.coordinates;
        PlaceResetToken(match, returningKeeper, destination - new Vector3Int(System.Math.Sign(destination.x), 0, 0));

        match.currentHalf = 1;
        match.gameData.gameSettings.numberOfHalfs = 2;
        var kickoffTeam = match.gameData.openingSetup.homeHasFirstHalfKickoff ? MatchManager.TeamInAttack.Away : MatchManager.TeamInAttack.Home;
        yield return VerifyOccupiedReset(match, protocol, online, returningKeeper, departingKeeper, destination, "match.transition", kickoffTeam,
            () => typeof(MatchManager).GetMethod("BeginHalfEndFlow", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(match, null));
        Assert.That(match.currentHalf, Is.EqualTo(2));
    }

    private IEnumerator VerifyOccupiedReset(MatchManager match, OnlineMatchProtocol protocol, bool online,
        PlayerToken arriving, PlayerToken departing, Vector3Int destination, string movingStage,
        MatchManager.TeamInAttack kickoffTeam, System.Action start)
    {
        var authority = online ? new OnlineGroundPassAuthority(match, protocol) : null;
        var replica = online ? CreateResetReplica(protocol) : null;
        if (online) pause = new OnlineSharedPause(protocol, match);
        var wire = new OnlineGroundPassWire();
        var target = match.hexGrid.GetHexCellAt(destination);
        var startHex = arriving.currentHex;
        var targetPosition = target.transform.position;
        string arrivingId = OnlineRollPublication.ActorId(arriving);
        string departingId = OnlineRollPublication.ActorId(departing);

        Time.timeScale = 4f;
        start();
        bool sawArrivalWhileOccupied = false;
        float deadline = Time.realtimeSinceStartup + 15f;
        do
        {
            var state = online ? AcceptResetPublication(replica, wire, pause.Capture(authority)) : null;
            var position = arriving.transform.position;
            if (departing.currentHex == target
                && Vector2.Distance(new Vector2(position.x, position.z), new Vector2(targetPosition.x, targetPosition.z)) < .001f)
            {
                Assert.That(arriving.currentHex, Is.SameAs(startHex));
                Assert.That(target.GetOccupyingToken(), Is.SameAs(departing));
                if (online)
                {
                    Assert.That(state.stage, Is.EqualTo(movingStage));
                    var pose = state.roomPose.players.Single(player => player.id == arrivingId);
                    Assert.That(Vector3.Distance(new Vector3(pose.x, pose.y, pose.z), position), Is.LessThan(.001f));
                    var claims = state.tokens.Where(token => token.onBoard && token.x == destination.x && token.z == destination.z);
                    Assert.That(claims.Select(token => token.id), Is.EqualTo(new[] { departingId }));
                }
                sawArrivalWhileOccupied = true;
            }

            if (match.currentState == MatchManager.GameState.PostGoalKickOffSetup) break;
            yield return null;
        } while (Time.realtimeSinceStartup < deadline);

        Assert.That(sawArrivalWhileOccupied, Is.True, "The arriving player reached the visual destination before it was vacated");
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.PostGoalKickOffSetup));
        Assert.That(match.teamInAttack, Is.EqualTo(kickoffTeam));
        Assert.That(arriving.currentHex, Is.SameAs(target));
        Assert.That(target.GetOccupyingToken(), Is.SameAs(arriving));
        var playing = match.playerTokenManager.allTokens.Where(token => token.isPlaying).ToArray();
        Assert.That(playing.All(token => token.currentHex != null && token.currentHex.GetOccupyingToken() == token), Is.True);
        if (online)
        {
            var kickoff = replica.State;
            Assert.That(kickoff.stage, Is.EqualTo("kickoff.setup"));
            Assert.That(kickoff.possessionTeam, Is.EqualTo(kickoffTeam.ToString()));
            Assert.That(kickoff.tokens.Count(token => token.onBoard), Is.EqualTo(playing.Length));
        }
    }

    private IEnumerator LoadTokenResetRoom(OnlineMatchProtocol protocol, bool online, bool mirror = false)
    {
        if (online)
        {
            InitializeFixture();
            SuppressLegacyRoomRunner();
            yield return LoadNativeRoom(Data(16, mirror), false, protocol.MatchId);
        }
        else
        {
            yield return RunOfflineNativeRoom(ApplicationManager.HotSeatGameMode);
        }
    }

    private static (PlayerToken[] Home, PlayerToken[] Away) PlaceResetFormations(MatchManager match)
    {
        var home = match.playerTokenManager.GetPlayingTokens(true).OrderBy(token => token.jerseyNumber).ToArray();
        var away = match.playerTokenManager.GetPlayingTokens(false).OrderBy(token => token.jerseyNumber).ToArray();
        foreach (var token in home.Concat(away)) token.SetCurrentHex(null);
        PlaceTeam(home, match.homeTeamDirection);
        PlaceTeam(away, match.awayTeamDirection);
        return (home, away);

        void PlaceTeam(PlayerToken[] team, MatchManager.TeamAttackingDirection direction)
        {
            var formation = GoalFlowManager.GetResetFormationCoordinatesForDirection(direction);
            for (int i = 0; i < team.Length; i++) PlaceResetToken(match, team[i], formation[i]);
        }
    }

    private static void PlaceResetToken(MatchManager match, PlayerToken token, Vector3Int coordinates)
    {
        var cell = match.hexGrid.GetHexCellAt(coordinates);
        Assert.That(cell.GetOccupyingToken(), Is.Null);
        token.SetCurrentHex(cell);
        var pose = cell.transform.position;
        pose.y = token.transform.position.y;
        token.transform.position = pose;
    }

    private static OnlineGroundPassReplica CreateResetReplica(OnlineMatchProtocol protocol)
    {
        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        return new OnlineGroundPassReplica(protocol.MatchId, board.tokens.Select(OnlineRollPublication.ActorId), OnlineFullRoomBootstrap.Catalog);
    }

    private static OnlineGroundPassState AcceptResetPublication(OnlineGroundPassReplica replica, OnlineGroundPassWire wire,
        OnlineGroundPassState state)
    {
        OnlineGroundPassMessage decoded = null;
        foreach (var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage
                 { kind = "state", matchId = state.matchId, state = state }))
            decoded = wire.Receive(packet, packet.Length) ?? decoded;
        Assert.That(decoded, Is.Not.Null);
        if (!replica.Apply(decoded.state))
        {
            var duplicates = decoded.state.tokens.Where(token => token.onBoard).GroupBy(token => (token.x, token.z))
                .Where(group => group.Count() > 1)
                .Select(group => $"{group.Key} claimed by {string.Join(", ", group.Select(token => token.id))}");
            Assert.Fail($"Guest rejected {state.gameState}/{state.stage} publication {state.publicationSequence}. {string.Join("; ", duplicates)}");
        }
        return decoded.state;
    }

    public IEnumerator RunPenaltyTossPauseFromFinalWhistle()
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var data = Data(16, false);
        data.gameSettings.tiebreaker = "Penalties";
        var protocol = NativePresentationProtocol();
        yield return LoadNativeRoom(data, false, protocol.MatchId);
        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var authority = new OnlineGroundPassAuthority(board.match, protocol);
        pause = new OnlineSharedPause(protocol, board.match);
        ConfigureFixtureTeam(board, OnlineTeam.Home);
        board.controls.Requested += message =>
        {
            var command = message.ToCommand();
            var reply = message.kind == "control" ? pause.Submit("h", command) : authority.Submit("h", command);
            Assert.That(reply.Status, Is.EqualTo(OnlineCommandStatus.Accepted));
            board.controls.Reply(new OnlineGroundPassMessage { status = reply.Status.ToString() });
            board.Display(pause.Capture(authority), "host");
        };

        typeof(MatchManager).GetMethod("CompleteMatchAfterFinalHalf", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(board.match, null);
        var toss = pause.Capture(authority);
        Assert.That(toss.stage, Is.EqualTo("coin.home"));
        Assert.That(OnlineManualSessionPolicy.CanFinish(toss, true), Is.False);
        board.Display(toss, "host");
        var panel = Object.FindAnyObjectByType<PauseMenuManager>(FindObjectsInactive.Include);
        board.controls.Escape();
        Assert.That(pause.IsPaused, Is.True);
        Assert.That(panel.pausePanel.activeInHierarchy && panel.resumeButton.interactable, Is.True);
        Assert.That(board.boardInput.TryKey(KeyCode.R), Is.False);
        panel.resumeButton.onClick.Invoke();
        Assert.That(pause.IsPaused, Is.False);
        Assert.That(panel.pausePanel.activeSelf, Is.False);

        var focus = new GameObject("Owned stale keyboard focus fixture", typeof(RectTransform), typeof(TMP_InputField));
        try
        {
            EventSystem.current.SetSelectedGameObject(focus);
            Assert.That(OnlineBoardInput.KeyboardOwnedByUI(KeyCode.R), Is.True);
            Assert.That(board.boardInput.TryKey(KeyCode.R), Is.True);
            Assert.That(pause.Capture(authority).coin.homeRoll, Is.InRange(1, 6));
        }
        finally
        {
            EventSystem.current.SetSelectedGameObject(null);
            Object.Destroy(focus);
        }

        var awayTurn = pause.Capture(authority);
        Assert.That(awayTurn.stage, Is.EqualTo("coin.away"));
        Assert.That(board.boardInput.TryKey(KeyCode.R), Is.False, "Home cannot roll for Away");
        board.controls.Escape();
        Assert.That(pause.IsPaused, Is.True);
        board.controls.Escape();
        Assert.That(pause.IsPaused, Is.False);
        float deadline = Time.realtimeSinceStartup + 7f;
        while (awayTurn.coin.awayRoll == 0 && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
            awayTurn = pause.Capture(authority);
        }
        Assert.That(awayTurn.coin.awayRoll, Is.InRange(1, 6), "Away's automatic roll must continue after resuming");
    }

    public IEnumerator RunShootoutStagingWithSwappedTokens(bool online)
    {
        var protocol = NativePresentationProtocol();
        yield return LoadTokenResetRoom(protocol, online);
        var match = MatchManager.Instance;
        var (home, away) = PlaceResetFormations(match);
        var playing = home.Concat(away).ToArray();
        var outfield = home.Where(token => !token.IsGoalKeeper).ToArray();
        var swapped = outfield.Take(4).ToArray();
        for (int i = 0; i < swapped.Length; i++)
        {
            int otherSpot = i % 2 == 0 ? i + 1 : i - 1;
            PlaceResetToken(match, swapped[i], new Vector3Int(0, 0, -otherSpot - 1));
        }
        outfield.Last().shooting = 12;
        match.gameData.penaltyShootoutSetup = new PenaltyShootoutSetup { homeTakesFirstPenalty = true, useRightGoal = true };
        var shootout = match.EnsurePenaltyShootoutManager();
        shootout.ShowOrderSelection();

        OnlineGroundPassAuthority authority = null;
        OnlineGroundPassReplica replica = null;
        if (online)
        {
            authority = new OnlineGroundPassAuthority(match, protocol);
            pause = new OnlineSharedPause(protocol, match);
            replica = CreateResetReplica(protocol);
            Assert.That(pause.Submit("h", new OnlineMatchCommand(protocol.MatchId, 1, pause.Revision, "shootout-order", "shootout.ready")).Status,
                Is.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(pause.Submit("a", new OnlineMatchCommand(protocol.MatchId, 1, pause.Revision, "shootout-order", "shootout.ready")).Status,
                Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        else
        {
            shootout.BeginShootoutFromOrderPanel(shootout.homeOrder, shootout.awayOrder);
        }

        Time.timeScale = 5f;
        var wire = new OnlineGroundPassWire();
        Assert.That(shootout.isActive, Is.True);
        float deadline = Time.realtimeSinceStartup + 15f;
        do
        {
            if (online) AcceptResetPublication(replica, wire, pause.Capture(authority));
            Assert.That(playing.Where(token => token.currentHex != null)
                .All(token => token.currentHex.GetOccupyingToken() == token), Is.True);
            if (match.shotManager.isWaitingForShotRoll) break;
            yield return null;
        } while (Time.realtimeSinceStartup < deadline);

        Assert.That(match.shotManager.isWaitingForShotRoll, Is.True, "Staging finishes and the first penalty can be taken");
        Assert.That(shootout.currentShooter, Is.SameAs(outfield.Last()));
        for (int i = 0; i < swapped.Length; i++)
        {
            var target = match.hexGrid.GetHexCellAt(new Vector3Int(0, 0, -i - 1));
            Assert.That(swapped[i].currentHex, Is.SameAs(target), $"Jersey {swapped[i].jerseyNumber} reaches its assigned spot");
            Assert.That(target.GetOccupyingToken(), Is.SameAs(swapped[i]));
        }
    }

    public IEnumerator RunPenaltyTossCountdownPublication()
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var data = Data(16, false);
        data.gameSettings.numberOfHalfs = 1;
        data.gameSettings.halfDuration = 15;
        data.gameSettings.tiebreaker = "Penalties";
        var protocol = NativePresentationProtocol();
        yield return LoadNativeRoom(data, false, protocol.MatchId);
        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var authority = new OnlineGroundPassAuthority(board.match, protocol);
        pause = new OnlineSharedPause(protocol, board.match);
        var replica = new OnlineGroundPassReplica(protocol.MatchId, board.tokens.Select(OnlineRollPublication.ActorId),
            OnlineFullRoomBootstrap.Catalog);

        AcceptThroughWire(pause.Capture(authority));
        typeof(MatchManager).GetMethod("BeginHalfEndFlow", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(board.match, null);
        var initial = pause.Capture(authority);
        Assert.That(initial.stage, Is.EqualTo("coin.home"));
        Assert.That(initial.coin.rollSeconds, Is.EqualTo(5));
        AcceptThroughWire(initial);
        AcceptThroughWire(pause.Capture(authority));

        OnlineGroundPassState countdown;
        float deadline = Time.realtimeSinceStartup + 3f;
        do
        {
            yield return null;
            countdown = pause.Capture(authority);
        } while (countdown.coin.rollSeconds == initial.coin.rollSeconds && Time.realtimeSinceStartup < deadline);

        Assert.That(countdown.coin.rollSeconds, Is.InRange(1, 4));
        Assert.That(countdown.revision, Is.EqualTo(initial.revision));
        Assert.That(countdown.decisionId, Is.EqualTo(initial.decisionId));
        AcceptThroughWire(countdown);
        Assert.That(countdown.publicationSequence, Is.GreaterThan(initial.publicationSequence));
        Assert.That(replica.State.coin.rollSeconds, Is.EqualTo(countdown.coin.rollSeconds));
        AcceptThroughWire(countdown);

        void AcceptThroughWire(OnlineGroundPassState state)
        {
            var reader = new OnlineGroundPassWire();
            OnlineGroundPassMessage decoded = null;
            foreach (var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage
            {
                kind = "state", matchId = state.matchId, state = state
            })) decoded = reader.Receive(packet, packet.Length);
            Assert.That(decoded, Is.Not.Null);
            Assert.That(replica.Apply(decoded.state), Is.True,
                $"Guest rejected {state.stage} with {state.coin?.rollSeconds}s left at publication {state.publicationSequence}");
        }
    }
}
