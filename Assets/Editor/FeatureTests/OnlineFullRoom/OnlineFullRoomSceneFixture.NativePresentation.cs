using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using Newtonsoft.Json;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class OnlineFullRoomSceneFixture
{
    private IEnumerator LoadNativeRoom(MatchManager.GameData data, bool guest, string matchId)
    {
        string path = Path.Combine(directory, guest ? "native-replica.json" : "native-authority.json");
        File.WriteAllText(path, JsonConvert.SerializeObject(data));
        session = new OnlineRoomSession(matchId, guest, data, path);
        app.SetActiveSaveFilePath(path);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
            $"Assets/Scenes/{OnlineRoomStartup.SceneName}.unity", new LoadSceneParameters(LoadSceneMode.Single));
        var startup = Object.FindAnyObjectByType<OnlineRoomStartup>();
        Assert.That(startup, Is.Not.Null);
        float deadline = Time.realtimeSinceStartup + 15f;
        while (startup.Board == null && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(startup.Board, Is.Not.Null, "Room must bind the online role");
    }

    private IEnumerator LoadPresentationGuest(MatchManager.GameData data, string matchId, bool nativeRoom)
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity", new LoadSceneParameters(LoadSceneMode.Single));
        session.Dispose();
        session = null;
        MatchManager.Instance = null;
        yield return nativeRoom ? LoadNativeRoom(data, true, matchId) : Load(data, true);
    }

    private static void ConfigureFixtureTeam(OnlineGroundPassBoard board, OnlineTeam team)
    {
        board.localTeam = team;
        board.controls.Configure(team);
    }

    public IEnumerator RunNativeSeatOwnership(bool homeKicksOff)
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var data = Data(16, !homeKicksOff, useFrontendSeats: true);
        data.openingSetup.homeHasFirstHalfKickoff = homeKicksOff;
        var version = new OnlineCompatibility("ground-wire-v7", "ground-rules-v1", OnlineFullRoomBootstrap.Catalog);
        var protocol = OnlineFrontendSessionPolicy.CreateMatchProtocol("native-seat-ownership", "host-account", "guest-account", version);
        Assert.That(protocol.BindAuthenticatedAccount("host-account", "host", version), Is.True);
        Assert.That(protocol.BindAuthenticatedAccount("guest-account", "guest", version), Is.True);
        yield return LoadNativeRoom(data, false, protocol.MatchId);

        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        Assert.That(board.localTeam, Is.EqualTo(OnlineTeam.Home));
        var authority = new OnlineGroundPassAuthority(board.match, protocol);
        pause = new OnlineSharedPause(protocol, board.match);
        var home = pause.Capture(authority);
        Assert.That(home.owner, Is.EqualTo("Home"));
        Assert.That(Confirm("guest", home, 1), Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(Confirm("host", home, 1), Is.EqualTo(OnlineCommandStatus.Accepted));
        var away = pause.Capture(authority);
        Assert.That(away.owner, Is.EqualTo("Away"));
        Assert.That(Confirm("host", away, 2), Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(Confirm("guest", away, 2), Is.EqualTo(OnlineCommandStatus.Accepted));
        var taker = pause.Capture(authority);
        Assert.That(taker.stage, Is.EqualTo("kickoff.taker"));
        Assert.That(taker.owner, Is.EqualTo(homeKicksOff ? "Home" : "Away"));

        pause.Dispose();
        pause = null;
        yield return LoadPresentationGuest(data, protocol.MatchId, nativeRoom: true);
        var guest = Object.FindAnyObjectByType<OnlineRoomStartup>();
        Assert.That(guest.Board.localTeam, Is.EqualTo(OnlineTeam.Away));
        Assert.That(guest.GuestConfigured, Is.True);
        Assert.That(guest.Board.match.kickoffManager.gameObject.activeSelf, Is.False);
        var replica = new OnlineGroundPassReplica(protocol.MatchId, guest.Board.tokens.Select(OnlineRollPublication.ActorId), version.Catalog);
        ApplyPublication(replica, guest.Board, taker);
        Assert.That(guest.Board.match.teamInAttack, Is.EqualTo(data.openingSetup.KickoffTeam));

        OnlineCommandStatus Confirm(string connection, OnlineGroundPassState state, long sequence)
            => authority.Submit(connection, new OnlineMatchCommand(protocol.MatchId, sequence, state.revision,
                state.decisionId, OnlineGroundPassAuthority.KickoffConfirmAction)).Status;
    }

    public IEnumerator RunNativeProjection(string presentation)
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var data = Data(16, false);
        var version = new OnlineCompatibility("ground-wire-v7", "ground-rules-v1", OnlineFullRoomBootstrap.Catalog);
        var protocol = new OnlineMatchProtocol("native-room-scene", "home", "away", "away", version);
        protocol.BindAuthenticatedAccount("home", "h", version);
        protocol.BindAuthenticatedAccount("away", "a", version);
        yield return LoadNativeRoom(data, false, protocol.MatchId);
        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        pause = new OnlineSharedPause(protocol, board.match);
        yield return presentation switch
        {
            "stoppage" => RunSharedRollPresentation(board, protocol, data, timeout: false, nativeRoom: true),
            "duel" => RunSharedDuelOrHeader(board, protocol, data, header: false, nativeRoom: true),
            "header" => RunSharedDuelOrHeader(board, protocol, data, header: true, nativeRoom: true),
            "half-time" => RunNormalOnlineHalf(board, protocol, 1),
            _ => RunNativeMovementProjection(board, protocol, data)
        };
    }

    private IEnumerator RunNativeMovementProjection(OnlineGroundPassBoard board, OnlineMatchProtocol protocol, MatchManager.GameData data)
    {
        var authority = new OnlineGroundPassAuthority(board.match, protocol);
        var movement = board.match.movementPhaseManager;
        movement.ActivateMovementPhase();
        var open = pause.Capture(authority);
        Assert.That(open.movementPanel?.selected, Is.Null);
        Assert.That(open.movementPanel?.attacking, Is.Empty);
        yield return null;

        var panel = Object.FindAnyObjectByType<MovementPhaseMovedTokensPanel>();
        var slot = ActiveMovementSlot(panel);
        var teamFace = slot.GetComponentInChildren<RawImage>(true).texture;
        Assert.That(teamFace, Is.Not.Null);
        Assert.That(SlotNumber(slot), Is.Empty);
        var player = movement.GetSelectableMovementTokensForDecisionPreview().First(token => !token.IsGoalKeeper);
        Assert.That(movement.TryExecuteOnlineMovement(OnlineMovementIntent.SelectPlayer, player), Is.True);
        yield return null;
        var selected = pause.Capture(authority);
        Assert.That(selected.movementPanel?.selected, Is.EqualTo(OnlineRollPublication.ActorId(player)));
        var number = player.jerseyNumber.ToString();
        Assert.That(SlotNumber(ActiveMovementSlot(panel)), Is.EqualTo(number));

        pause.Dispose();
        pause = null;
        yield return LoadPresentationGuest(data, protocol.MatchId, nativeRoom: true);
        board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        panel = Object.FindAnyObjectByType<MovementPhaseMovedTokensPanel>();
        board.Display(open, "guest");
        yield return null;
        slot = ActiveMovementSlot(panel);
        var face = slot.GetComponentInChildren<RawImage>(true);
        Assert.That(face.texture, Is.SameAs(teamFace));
        Assert.That(SlotNumber(slot), Is.Empty);
        float initialAlpha = face.color.a;
        float deadline = Time.realtimeSinceStartup + 1.5f;
        while (Mathf.Abs(face.color.a - initialAlpha) < .03f && Time.realtimeSinceStartup < deadline)
        {
            board.Display(open, "guest");
            yield return null;
        }
        Assert.That(Mathf.Abs(face.color.a - initialAlpha), Is.GreaterThanOrEqualTo(.03f),
            "The guest pulse advances through repeated state updates");
        Assert.That(board.match.IsGameplayInputBlocked, Is.True);
        board.Display(selected, "guest");
        Assert.That(SlotNumber(ActiveMovementSlot(panel)), Is.EqualTo(number));
    }

    public IEnumerator RunNativeTossAnimation(bool shootout)
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var data = Data(16, false);
        var protocol = NativePresentationProtocol();
        yield return LoadNativeRoom(data, false, protocol.MatchId);
        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var authority = new OnlineGroundPassAuthority(board.match, protocol);
        pause = new OnlineSharedPause(protocol, board.match);
        var panel = board.match.coinTossPanel;
        Assert.That(panel.OpenForRoom(board.match, shootout ? CoinTossPurpose.PenaltyShootout : CoinTossPurpose.FirstHalf), Is.True);
        var snapshots = new System.Collections.Generic.List<OnlineGroundPassState>();
        foreach (bool home in new[] { true, false })
        {
            var current = pause.Capture(authority);
            snapshots.Add(current);
            board.Display(current, "host");
            Assert.That(panel.dicePanel.homeDie.IsAnimating, Is.EqualTo(home));
            Assert.That(panel.dicePanel.awayDie.IsAnimating, Is.EqualTo(!home));
            Assert.That(current.coin.rollSeconds, Is.EqualTo(5));
            var holdEnd = Time.realtimeSinceStartup + .3f;
            pause.SetConnectionHold(true);
            while (Time.realtimeSinceStartup < holdEnd)
            {
                Assert.That(pause.Capture(authority).coin.rollSeconds, Is.EqualTo(5));
                yield return null;
            }
            pause.SetConnectionHold(false);
            float deadline = Time.realtimeSinceStartup + 7f;
            while (current.coin.stage == (home ? "home" : "away") && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                current = pause.Capture(authority);
            }
            Assert.That(home ? current.coin.homeRoll : current.coin.awayRoll, Is.InRange(1, 6));
        }
        var result = pause.Capture(authority);
        Assert.That(result.coin.stage, Is.EqualTo("result"));
        snapshots.Add(result);
        pause.Dispose();
        pause = null;
        yield return LoadPresentationGuest(data, protocol.MatchId, nativeRoom: true);
        board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var replica = new OnlineGroundPassReplica(protocol.MatchId, board.tokens.Select(OnlineRollPublication.ActorId), OnlineFullRoomBootstrap.Catalog);
        foreach (var snapshot in snapshots)
        {
            ApplyPublication(replica, board, snapshot);
            panel = board.match.coinTossPanel;
            Assert.That(panel.dicePanel.visuals.activeInHierarchy, Is.True);
            Assert.That(panel.dicePanel.homeDie.IsAnimating, Is.EqualTo(snapshot.coin.stage == "home"));
            Assert.That(panel.dicePanel.awayDie.IsAnimating, Is.EqualTo(snapshot.coin.stage == "away"));
            if (snapshot.coin.rollSeconds.HasValue)
                Assert.That(panel.dicePanel.footer.text, Does.Contain("Auto-roll in 5s"));
        }
    }

    private static MovementPhaseMovedTokenSlotView ActiveMovementSlot(MovementPhaseMovedTokensPanel panel)
    {
        Assert.That(panel, Is.Not.Null);
        Assert.That(panel.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));
        return panel.GetComponentsInChildren<MovementPhaseMovedTokenSlotView>(true)
            .Single(slot => slot.State == MovementPhaseMovedTokenSlotView.PresentationState.Active);
    }

    private static string SlotNumber(MovementPhaseMovedTokenSlotView slot)
    {
        var text = (TMP_Text)typeof(MovementPhaseMovedTokenSlotView)
            .GetField("numberText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(slot);
        return text.text;
    }

    public IEnumerator RunNativeInputOwnership()
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var data = Data(16, false);
        var protocol = NativePresentationProtocol();
        yield return LoadNativeRoom(data, false, protocol.MatchId);
        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var authority = new OnlineGroundPassAuthority(board.match, protocol);
        pause = new OnlineSharedPause(protocol, board.match);
        var home = pause.Capture(authority);
        Assert.That(home.owner, Is.EqualTo("Home"));
        ConfigureFixtureTeam(board, OnlineTeam.Away);
        board.Display(home, "host");
        AssertExpectedTeamPalette(board, home);
        Assert.That(board.boardInput.TryKey(KeyCode.Return), Is.False);
        VerifyLocalPresentation(board);
        yield return VerifyNativePointer(board);
        var command = OnlineGroundPassMessage.Command(home, 1, OnlineGroundPassAuthority.KickoffConfirmAction).ToCommand();
        Assert.That(authority.Submit("a", command).Status, Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(authority.Submit("h", command).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        var away = pause.Capture(authority);
        Assert.That(away.owner, Is.EqualTo("Away"));
        board.Display(away, "host");
        AssertExpectedTeamPalette(board, away);
        pause.Dispose();
        pause = null;

        yield return LoadPresentationGuest(data, protocol.MatchId, nativeRoom: true);
        board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        ConfigureFixtureTeam(board, OnlineTeam.Home);
        var replica = new OnlineGroundPassReplica(protocol.MatchId, board.tokens.Select(OnlineRollPublication.ActorId), OnlineFullRoomBootstrap.Catalog);
        ApplyPublication(replica, board, home);
        AssertExpectedTeamPalette(board, home);
        VerifyLocalPresentation(board);
        yield return VerifyNativePointer(board);
        ApplyPublication(replica, board, away);
        AssertExpectedTeamPalette(board, away);
        Assert.That(board.boardInput.TryKey(KeyCode.Return), Is.False);
    }

    private static IEnumerator VerifyNativePointer(OnlineGroundPassBoard board)
    {
        Assert.That(board.pointers, Is.Not.Null);
        Assert.That(board.pointers.gameObject.scene.path, Is.EqualTo("Assets/Scenes/Room.unity"));
        var center = board.match.ball.GetCurrentHex().GetHexCenter();
        board.pointers.ShowRemote(new OnlineRoomPointer { visible = true, x = center.x, z = center.z, icon = 1 }, 1);
        yield return null;
        Assert.That(board.pointers.remoteIcon.gameObject.activeInHierarchy, Is.True);
        Assert.That(board.pointers.remoteIcon.texture, Is.SameAs(board.pointers.grabIcon));
        var settings = board.match.gameData.gameSettings;
        var remoteKit = board.localTeam == OnlineTeam.Home ? settings.awayKit : settings.homeKit;
        Assert.That(board.pointers.remoteIcon.color, Is.EqualTo(TokenKitCatalog.ResolveInstructionPalette(remoteKit, Color.white, Color.black).Primary));
    }

    private static void AssertExpectedTeamPalette(OnlineGroundPassBoard board, OnlineGroundPassState state)
    {
        Assert.That(state.instruction?.owner, Is.EqualTo(state.owner));
        var settings = board.match.gameData.gameSettings;
        var palette = TokenKitCatalog.ResolveInstructionPalette(state.owner == "Home" ? settings.homeKit : settings.awayKit, Color.white, Color.black);
        Assert.That(board.instructionMonitor.instructionText.color, Is.EqualTo(palette.Secondary));
        var background = (Image)typeof(GameDebugMonitor).GetField("instructionPanelImage", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(board.instructionMonitor);
        var backgroundColor = palette.Primary;
        backgroundColor.a = .82f;
        Assert.That(background.color, Is.EqualTo(backgroundColor));
    }

    private static void VerifyLocalPresentation(OnlineGroundPassBoard board)
    {
        int sent = 0;
        void Count(OnlineGroundPassMessage _) => sent++;
        board.controls.Requested += Count;
        try
        {
            var before = board.match.currentState;
            float timeScale = Time.timeScale;
            var camera = board.boardCamera.GetComponent<CameraController>();
            Assert.That(camera.TryHandlePresetKey(new KeyPressData(KeyCode.Alpha2, shift: false, ctrl: false, alt: false)), Is.True);
            var stats = Object.FindAnyObjectByType<MatchStatsUI>();
            stats.TogglePanel();
            board.boardInput.PresentHover(default, false);
            var input = Object.FindAnyObjectByType<GameInputManager>();
            input.SetLineupHoverToken(board.tokens[1]);
            Assert.That(board.boardInput.TryKey(KeyCode.Space), Is.True);
            Assert.That(RoomInspectionCard.Instance.IsOpen, Is.True);
            board.controls.Escape();
            Assert.That(RoomInspectionCard.Instance.IsOpen, Is.False);
            input.SetLineupHoverToken(null);
            stats.TogglePanel();
            Assert.That(board.match.currentState, Is.EqualTo(before));
            Assert.That(Time.timeScale, Is.EqualTo(timeScale));
            Assert.That(sent, Is.Zero);
        }
        finally { board.controls.Requested -= Count; }
    }
}
