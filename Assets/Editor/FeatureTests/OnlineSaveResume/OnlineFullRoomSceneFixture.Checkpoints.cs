using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class OnlineFullRoomSceneFixture
{
    public IEnumerator RunCheckpointPause(bool homeAttacks)
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var data = Data(16, !homeAttacks, useFrontendSeats: true);
        data.openingSetup.homeHasFirstHalfKickoff = homeAttacks;
        data.gameSettings.playerAssistance = 3;
        var version = new OnlineCompatibility("ground-wire-v7", "ground-rules-v1", OnlineFullRoomBootstrap.Catalog);
        var protocol = OnlineFrontendSessionPolicy.CreateMatchProtocol("checkpoint-pause", "home", "away", version);
        protocol.BindAuthenticatedAccount("home", "host", version);
        protocol.BindAuthenticatedAccount("away", "guest", version);
        yield return LoadNativeRoom(data, false, protocol.MatchId);

        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var view = Object.FindAnyObjectByType<PauseMenuManager>(FindObjectsInactive.Include);
        var authority = new OnlineGroundPassAuthority(board.match, protocol);
        pause = new OnlineSharedPause(protocol, board.match);
        var slotFolder = Path.Combine(directory, "slots");
        var store = new OnlineCheckpointStore(slotFolder, version);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var native = typeof(OnlineGroundPassBoard).GetField("nativePause", flags).GetValue(board);
        native.GetType().GetMethod("BindCheckpointHost", flags).Invoke(native,
            new object[] { protocol, pause, authority, "away", store, version });
        board.controls.Configure(OnlineTeam.Home);
        board.controls.SetConnected(true);

        long sequence = 0;
        long controlSequence = 0;
        string guestConnection = "guest";
        void Submit(string action)
        {
            var state = pause.Capture(authority);
            var connection = state.owner == "Home" ? "host" : guestConnection;
            Assert.That(authority.Submit(connection, new OnlineMatchCommand(protocol.MatchId, ++sequence,
                state.revision, state.decisionId, action)).Status, Is.EqualTo(OnlineCommandStatus.Accepted), action);
        }
        void GuestPause(string action)
        {
            Assert.That(pause.Submit("guest", new OnlineMatchCommand(protocol.MatchId, ++controlSequence,
                pause.Revision, "shared-pause", action)).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
            board.Display(pause.Capture(authority), "Checkpoint fixture");
        }

        Submit(OnlineGroundPassAuthority.KickoffConfirmAction);
        Submit(OnlineGroundPassAuthority.KickoffConfirmAction);
        Submit(pause.Capture(authority).legalActions.First(action => action.StartsWith("kickoff.taker:")));
        float deadline = Time.realtimeSinceStartup + 10f;
        while (pause.Capture(authority).stage != "target" && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(pause.Capture(authority).stage, Is.EqualTo("target"), "Kickoff taker has reached the ball");
        GuestPause(OnlineSharedPause.RequestAction);
        Assert.That(view.saveMatchAsButton.interactable, Is.False, "An unsettled kickoff pass cannot be saved");
        Assert.That(view.pauseSaveStatusText.gameObject.activeInHierarchy, Is.True, "The host can see why saving is blocked");
        Assert.That(view.pauseSaveStatusText.text, Does.Contain("ground pass"));
        var saveLabel = view.saveMatchAsButton.transform.Find("MaskContainer")?.GetComponentInChildren<TMPro.TMP_Text>(true)
            ?? view.saveMatchAsButton.GetComponentInChildren<TMPro.TMP_Text>(true);
        var unavailableColor = saveLabel.color;
        view.saveMatchAsButton.onClick.Invoke();
        Assert.That(view.saveAsOverlay.activeSelf, Is.False);
        Assert.That(Directory.Exists(slotFolder), Is.False);
        GuestPause(OnlineSharedPause.ResumeAction);

        var targetState = pause.Capture(authority);
        var target = board.match.playerTokenManager.allTokens
            .Where(token => token.isHomeTeam == homeAttacks && token.GetCurrentHex() != null)
            .Select(token => token.GetCurrentHex())
            .Where(hex => targetState.legalActions.Contains(OnlineGroundPassAuthority.TargetAction(hex.coordinates.x, hex.coordinates.z)))
            .OrderBy(hex => Mathf.Abs(hex.coordinates.z - targetState.ballZ) + Mathf.Abs(hex.coordinates.x - targetState.ballX))
            .First();
        Submit(OnlineGroundPassAuthority.TargetAction(target.coordinates.x, target.coordinates.z));
        deadline = Time.realtimeSinceStartup + 15f;
        while (board.match.currentState != MatchManager.GameState.EndOfStandardPass && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.That(board.match.currentState, Is.EqualTo(MatchManager.GameState.EndOfStandardPass));
        GuestPause(OnlineSharedPause.RequestAction);
        Assert.That(view.pausePanel.activeSelf, Is.True);
        Assert.That(view.resumeButton.interactable, Is.False, "The guest owns the pause");
        Assert.That(view.saveMatchAsButton.IsInteractable(), Is.True, view.pauseSaveStatusText.text);
        Assert.That(saveLabel.color, Is.Not.EqualTo(unavailableColor), "The host can see that Save Match As is now available");
        Assert.That(view.pauseSaveStatusText.text, Does.Contain("Save Match As"));
        view.saveMatchAsButton.onClick.Invoke();
        Assert.That(view.saveAsOverlay.activeSelf, Is.True);
        view.saveNameInput.text = "After kickoff pass";
        view.saveAsSaveButton.onClick.Invoke();
        Assert.That(view.pauseSaveStatusText.text, Is.EqualTo("Saved After kickoff pass."));

        string saved = Directory.GetFiles(slotFolder, "*.json").Single();
        var slot = System.Guid.ParseExact(Path.GetFileNameWithoutExtension(saved), "N");
        var checkpoint = store.Load(slot);
        Assert.That(checkpoint.match.runtimeSnapshot.teamInAttack, Is.EqualTo(homeAttacks ? "Home" : "Away"));
        Assert.That(checkpoint.match.runtimeSnapshot.ball.x, Is.EqualTo(target.coordinates.x));
        Assert.That(checkpoint.match.runtimeSnapshot.ball.z, Is.EqualTo(target.coordinates.z));
        Assert.That(checkpoint.match.events.Count, Is.GreaterThan(0));

        view.saveMatchButton.onClick.Invoke();
        Assert.That(Directory.GetFiles(slotFolder, "*.json"), Has.Length.EqualTo(1));
        Assert.That(store.Load(slot).checkpointId, Is.Not.EqualTo(checkpoint.checkpointId));
        Assert.That(pause.IsPaused, Is.True);

        var sourceBytes = File.ReadAllBytes(saved);
        var restored = store.CreateRun(store.Load(slot));
        var snapshot = restored.runtimeSnapshot;
        var savedRoster = snapshot.tokens.ToDictionary(token => token.tokenKey);
        var lastSequence = restored.inheritedEventSequence;
        pause.Dispose();
        pause = null;
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity", new LoadSceneParameters(LoadSceneMode.Single));
        session.Dispose();
        session = null;

        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/OnlineModeMenu.unity", new LoadSceneParameters(LoadSceneMode.Single));
        var menu = Object.FindAnyObjectByType<OnlineModeMenu>();
        typeof(OnlineModeMenu).GetField("checkpointStore", flags).SetValue(menu, store);
        Assert.That(menu.loadMatchButton.gameObject.activeInHierarchy, Is.True);
        menu.loadMatchButton.onClick.Invoke();
        Assert.That(menu.loadMatchPanel.activeInHierarchy, Is.True);
        Assert.That(menu.hostCheckpoint.interactable, Is.True);
        Assert.That(menu.checkpointSummary.text, Does.Contain("After kickoff pass"));
        Assert.That(menu.checkpointSummary.text, Does.Contain("0–0"));
        menu.cancelCheckpoint.onClick.Invoke();
        Assert.That(menu.loadMatchPanel.activeSelf, Is.False);

        const string resumedMatch = "new-checkpoint-session";
        protocol = OnlineFrontendSessionPolicy.CreateMatchProtocol(resumedMatch, "home", "away", version);
        protocol.BindAuthenticatedAccount("home", "host", version);
        protocol.BindAuthenticatedAccount("away", "guest", version);
        yield return LoadNativeRoom(restored, false, resumedMatch, resume: true);
        board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var match = board.match;
        authority = new OnlineGroundPassAuthority(match, protocol);
        pause = new OnlineSharedPause(protocol, match);
        var held = pause.Capture(authority);
        Assert.That(held.resumePending, Is.True);
        Assert.That(held.legalActions, Is.Empty);
        Assert.That(held.clockRunning, Is.EqualTo(snapshot.clock.isClockRunning));
        Assert.That(held.regulationSeconds, Is.EqualTo(snapshot.clock.currentHalfRegulationSeconds));
        Assert.That(held.possessionTeam, Is.EqualTo(homeAttacks ? "Home" : "Away"));
        Assert.That(held.ballX, Is.EqualTo(snapshot.ball.x));
        Assert.That(held.ballZ, Is.EqualTo(snapshot.ball.z));
        Assert.That(held.homeStats.totalGoals, Is.EqualTo(snapshot.stats.homeTeamStats.totalGoals));
        Assert.That(held.awayStats.totalGoals, Is.EqualTo(snapshot.stats.awayTeamStats.totalGoals));
        Assert.That(held.homeScorers.Length, Is.EqualTo(snapshot.homeScorers.Count));
        Assert.That(held.awayScorers.Length, Is.EqualTo(snapshot.awayScorers.Count));
        Assert.That(match.gameData.events.Last().sequenceNumber, Is.EqualTo(lastSequence));
        Assert.That(match.gameData.playSessionId, Is.Not.EqualTo(checkpoint.match.playSessionId));
        foreach (var token in board.tokens)
        {
            var prior = savedRoster[(token.isHomeTeam ? "Home:" : "Away:") + token.jerseyNumber];
            Assert.That(RoomHexCoordinates.FromHex(token.GetCurrentHex())?.ToVector3Int(),
                Is.EqualTo(prior.currentHex?.ToVector3Int()));
        }
        match.AdvanceOnlineAuthorityTime(.5f);
        Assert.That(match.OnlineRegulationSeconds, Is.EqualTo(held.regulationSeconds));
        var resumedView = Object.FindAnyObjectByType<PauseMenuManager>(FindObjectsInactive.Include);
        board.controls.Configure(OnlineTeam.Home);
        board.controls.Requested += message =>
        {
            var reply = pause.Submit("host", message.ToCommand());
            Assert.That(reply.Status, Is.EqualTo(OnlineCommandStatus.Accepted));
            board.controls.Reply(new OnlineGroundPassMessage {kind="control-reply", sequence=message.sequence,
                revision=reply.Revision, status=reply.Status.ToString()});
        };
        double now = 0;
        var recovery = new OnlinePeerRecovery(protocol, version, pause, "away", () => now);
        Assert.That(recovery.BindAuthenticated("away", "first-guest", version), Is.True);
        var ready = new OnlineMatchCommand(resumedMatch, 1, pause.Revision, "resume-ready", OnlineSharedPause.ResumeReadyAction);
        Assert.That(pause.Submit("first-guest", ready).Status, Is.EqualTo(OnlineCommandStatus.NotReady));
        Assert.That(recovery.AcknowledgeSnapshot("first-guest", recovery.Generation), Is.True);
        Assert.That(pause.Submit("first-guest", ready).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(pause.Capture(authority).awayResumeReady, Is.True);
        long oldGeneration = recovery.Generation;
        recovery.TransportLost("first-guest", oldGeneration);
        Assert.That(pause.Capture(authority).awayResumeReady, Is.False);
        guestConnection = "returning-guest";
        Assert.That(recovery.BindAuthenticated("away", guestConnection, version), Is.True);
        Assert.That(recovery.AcknowledgeSnapshot(guestConnection, oldGeneration), Is.False);
        Assert.That(recovery.AcknowledgeSnapshot(guestConnection, recovery.Generation), Is.True);
        board.Display(pause.Capture(authority), "Resume");
        Assert.That(resumedView.pausePanel.activeSelf, Is.True);
        Assert.That(resumedView.resumeButton.interactable, Is.True);
        resumedView.resumeButton.onClick.Invoke();
        var oneReady = pause.Capture(authority);
        Assert.That(oneReady.resumePending && oneReady.homeResumeReady && !oneReady.awayResumeReady, Is.True);
        var stale = new OnlineMatchCommand("checkpoint-pause", 1, pause.Revision, "resume-ready", OnlineSharedPause.ResumeReadyAction);
        Assert.That(pause.Submit(guestConnection, stale).Status, Is.EqualTo(OnlineCommandStatus.WrongMatch));
        Assert.That(pause.Submit("first-guest", ready).Status, Is.EqualTo(OnlineCommandStatus.UnknownConnection));
        ready = new OnlineMatchCommand(resumedMatch, 2, pause.Revision, "resume-ready", OnlineSharedPause.ResumeReadyAction);
        Assert.That(pause.Submit(guestConnection, ready).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(pause.Submit(guestConnection, ready).Status, Is.EqualTo(OnlineCommandStatus.Duplicate));
        var playable = pause.Capture(authority);
        Assert.That(playable.resumePending, Is.False);
        Assert.That(playable.owner, Is.EqualTo(homeAttacks ? "Home" : "Away"));
        Assert.That(playable.legalActions, Does.Contain(OnlineGroundPassAuthority.MovementStartAction));
        Submit(OnlineGroundPassAuthority.MovementStartAction);
        var next = pause.Capture(authority);
        Assert.That(next.movementActive, Is.True);
        Assert.That(match.gameData.events.Last().sequenceNumber, Is.GreaterThan(lastSequence));
        Assert.That(File.ReadAllBytes(saved), Is.EqualTo(sourceBytes));

        pause.Dispose();
        pause = null;
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity", new LoadSceneParameters(LoadSceneMode.Single));
        session.Dispose();
        session = null;
        var launchType = typeof(OnlineRoomLaunchData);
        var guestData = (MatchManager.GameData)launchType.GetMethod("ResumeBootstrap", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { restored });
        yield return LoadNativeRoom(guestData, true, resumedMatch, resume: true);
        var guestBoard = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var replica = new OnlineGroundPassReplica(resumedMatch, guestBoard.tokens.Select(OnlineRollPublication.ActorId), version.Catalog);
        ApplyPublication(replica, guestBoard, held);
        var guestView = Object.FindAnyObjectByType<PauseMenuManager>(FindObjectsInactive.Include);
        Assert.That(guestView.saveMatchAsButton.interactable, Is.False);
        guestView.saveMatchAsButton.onClick.Invoke();
        Assert.That(guestView.saveAsOverlay.activeSelf, Is.False);
        Assert.That(guestBoard.match.ball.GetCurrentHex().coordinates, Is.EqualTo(snapshot.ball.ToVector3Int()));
        ApplyPublication(replica, guestBoard, next);
        Assert.That(replica.State.movementActive, Is.True);
        Assert.That(replica.State.resumePending, Is.False);
        Assert.That(File.ReadAllBytes(saved), Is.EqualTo(sourceBytes));
    }
}
