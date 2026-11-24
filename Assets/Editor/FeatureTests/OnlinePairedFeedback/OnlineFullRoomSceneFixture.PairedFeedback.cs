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
        ApplyPublication(replica, board, cleared);
        Assert.That(status.GetComponent<CanvasGroup>().alpha, Is.Zero);
        Assert.That(cells.Select(cell => cell.transform.position.y).ToArray(), Is.EqualTo(originalHeights));
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
