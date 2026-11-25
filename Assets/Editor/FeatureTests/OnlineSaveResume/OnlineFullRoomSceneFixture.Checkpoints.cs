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
    public IEnumerator RunCheckpointPause(bool homeAttacks, bool afterMovement, bool finishReport, bool expectHistory)
        => RunCheckpointRoundTrip(homeAttacks, afterMovement, finishReport, expectHistory, null);

    public IEnumerator RunCheckpointAtNewBoundary(bool homeAttacks, string boundary)
        => RunCheckpointRoundTrip(homeAttacks, false, false, false, boundary);

    private IEnumerator RunCheckpointRoundTrip(bool homeAttacks, bool afterMovement, bool finishReport, bool expectHistory, string boundary)
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
        Assert.That(view.pauseSaveStatusText.text, Does.Contain("next action"));
        var saveLabel = view.saveMatchAsButton.transform.Find("MaskContainer")?.GetComponentInChildren<TMPro.TMP_Text>(true)
            ?? view.saveMatchAsButton.GetComponentInChildren<TMPro.TMP_Text>(true);
        var unavailableColor = saveLabel.color;
        view.saveMatchAsButton.onClick.Invoke();
        Assert.That(view.saveAsOverlay.activeSelf, Is.False);
        Assert.That(Directory.Exists(slotFolder), Is.False);
        GuestPause(OnlineSharedPause.ResumeAction);

        var targetState = pause.Capture(authority);
        var occupied = board.match.playerTokenManager.allTokens
            .Where(token => token.isHomeTeam == homeAttacks && token.GetCurrentHex() != null)
            .Select(token => token.GetCurrentHex());
        var targets = boundary == "loose" ? board.cells.Where(hex => hex.GetOccupyingToken() == null) : occupied;
        var target = targets
            .Where(hex => targetState.legalActions.Contains(OnlineGroundPassAuthority.TargetAction(hex.coordinates.x, hex.coordinates.z)))
            .OrderBy(hex => Mathf.Abs(hex.coordinates.z - targetState.ballZ) + Mathf.Abs(hex.coordinates.x - targetState.ballX))
            .First();
        Submit(OnlineGroundPassAuthority.TargetAction(target.coordinates.x, target.coordinates.z));
        deadline = Time.realtimeSinceStartup + 15f;
        var expectedAfterPass = boundary == "loose" ? MatchManager.GameState.MovementPhase : MatchManager.GameState.EndOfStandardPass;
        while (board.match.currentState != expectedAfterPass && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.That(board.match.currentState, Is.EqualTo(expectedAfterPass));
        if (boundary == "loose")
        {
            Assert.That(board.match.attackHasPossession, Is.False);
            Assert.That(board.match.movementPhaseManager.isMovementPhaseAttack, Is.True);
            Assert.That(board.match.movementPhaseManager.movedTokens, Is.Empty);
        }
        if (boundary == "stoppage")
        {
            board.match.isHalfExpired = true;
            Assert.That(board.match.TryEnterExtraActionsRollBeforeRestart(board.match.RefreshAvailableActionsForCurrentState), Is.True);
            GuestPause(OnlineSharedPause.RequestAction);
            Assert.That(view.saveMatchAsButton.interactable, Is.False, "The stoppage roll must finish first");
            GuestPause(OnlineSharedPause.ResumeAction);
            Submit("stoppage.roll");
            deadline = Time.realtimeSinceStartup + 12f;
            while (StoppageActionsPanelController.IsHoldingContinuation && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(StoppageActionsPanelController.IsHoldingContinuation, Is.False);
        }
        if (afterMovement)
        {
            Submit(OnlineGroundPassAuthority.MovementStartAction);
            Assert.That(pause.Capture(authority).movementActive, Is.True);
            GuestPause(OnlineSharedPause.RequestAction);
            Assert.That(view.saveMatchAsButton.interactable, Is.False, "An active Movement Phase cannot be saved.");
            GuestPause(OnlineSharedPause.ResumeAction);
            for (int phase = 0; phase < 3; phase++)
                Submit(OnlineGroundPassAuthority.MovementForfeitAction);
            deadline = Time.realtimeSinceStartup + 10f;
            while (pause.Capture(authority).stage != "action" && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(board.match.currentState, Is.EqualTo(MatchManager.GameState.EndOfMovementPhase));
            Assert.That(board.match.attackHasPossession, Is.True);
        }
        var scorer = board.tokens.Single(token => token.isHomeTeam && token.jerseyNumber == 2);
        string scorerName = scorer.playerName;
        scorer.ReceiveYellowCard();
        scorer.ReceiveInjury();
        var log = board.match.gameData.gameLog;
        var opponent = board.tokens.Single(token => !token.isHomeTeam && token.jerseyNumber == 2);
        log.LogEvent(scorer, MatchManager.ActionType.YellowCardShown, connectedToken: opponent);
        log.LogEvent(scorer, MatchManager.ActionType.Injured, connectedToken: opponent);
        log.LogEvent(scorer, MatchManager.ActionType.ShotAttempt);
        log.LogEvent(scorer, MatchManager.ActionType.ShotOnTarget);
        log.LogEvent(scorer, MatchManager.ActionType.GoalScored);
        if (finishReport)
            board.match.ResolveGameplayDiceRoll("high_pass_accuracy", actor: scorer,
                details: new System.Collections.Generic.Dictionary<string, string> {["highPassAttribute"] = "4", ["threshold"] = "8"});
        var beforeSave = pause.Capture(authority);
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
        Assert.That(checkpoint.match.runtimeSnapshot.attackHasPossession, Is.EqualTo(boundary != "loose"));
        Assert.That(checkpoint.match.runtimeSnapshot.movementOpening != null, Is.EqualTo(boundary == "loose"));
        Assert.That(checkpoint.match.runtimeSnapshot.clock.isHalfExpired, Is.EqualTo(boundary == "stoppage"));
        Assert.That(checkpoint.match.events.Count, Is.GreaterThan(0));
        Assert.That(checkpoint.match.runtimeSnapshot.stats.homeTeamStats.totalGoals, Is.EqualTo(1));
        var savedPlayerStats = checkpoint.match.runtimeSnapshot.stats.playerStats
            .Single(entry => entry.playerKey == MatchManager.GetStableTokenKey(scorer)).stats;
        Assert.That(savedPlayerStats.goals, Is.EqualTo(1));
        Assert.That(savedPlayerStats.yellowCards, Is.EqualTo(1));
        Assert.That(savedPlayerStats.injuries, Is.EqualTo(1));

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
        Assert.That(menu.lobbyCode.characterLimit, Is.EqualTo(8));
        Assert.That(menu.loadMatchButton.gameObject.activeInHierarchy, Is.True);
        void BindTestStore(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "CreateLoadRoom") return;
            var sceneBrowser = Object.FindAnyObjectByType<CreateLoadRoomManager>();
            typeof(CreateLoadRoomManager).GetField("onlineStore", flags).SetValue(sceneBrowser, store);
        }
        SceneManager.sceneLoaded += BindTestStore;
        try
        {
            menu.loadMatchButton.onClick.Invoke();
            yield return null;
            yield return null;
        }
        finally { SceneManager.sceneLoaded -= BindTestStore; }
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("CreateLoadRoom"));
        var browser = Object.FindAnyObjectByType<CreateLoadRoomManager>();
        var rows = (Transform)typeof(CreateLoadRoomManager).GetField("rowsContent", flags).GetValue(browser);
        var row = rows.Cast<Transform>().Single(child => child.gameObject.activeInHierarchy && child.name == "SaveRow_After kickoff pass");
        Assert.That(row.GetComponent<UnityEngine.UI.Button>().IsInteractable(), Is.True);
        Assert.That(row.GetComponentsInChildren<TMPro.TMP_Text>().Select(label => label.text),
            Does.Contain("After kickoff pass").And.Contain("1–0"));
        var cancel = (UnityEngine.UI.Button)typeof(CreateLoadRoomManager).GetField("loadBrowserCancelButton", flags).GetValue(browser);
        cancel.onClick.Invoke();
        yield return null;
        yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(OnlineModeMenu.SceneName));

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
        AssertRestoredStats(board);
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
        Assert.That(playable.legalActions, Is.EquivalentTo(beforeSave.legalActions));
        if (boundary == "loose")
        {
            Submit(playable.legalActions.First(action => action.StartsWith("movement.player:")));
            Assert.That(pause.Capture(authority).stage, Is.EqualTo("movement.attack.target"));
            Assert.That(File.ReadAllBytes(saved), Is.EqualTo(sourceBytes));
            yield break;
        }
        if (finishReport)
        {
            match.ResolveGameplayDiceRoll("high_pass_accuracy", actor: board.tokens.Single(token =>
                    token.isHomeTeam && token.jerseyNumber == 2),
                details: new System.Collections.Generic.Dictionary<string, string> {["highPassAttribute"] = "4", ["threshold"] = "8"});
            Assert.That(match.gameData.events.Count(entry => entry.kind == "dice.roll"), Is.EqualTo(2));
            match.gameData.gameSettings.tiebreaker = "None";
            match.currentHalf = match.gameData.gameSettings.numberOfHalfs;
            typeof(MatchManager).GetMethod("BeginHalfEndFlow", flags).Invoke(match, null);
            Assert.That(match.OnlineMatchComplete, Is.True);
            yield return VerifyFullTimeReport(board, authority, protocol, data, expectHistory);
            Assert.That(File.ReadAllBytes(saved), Is.EqualTo(sourceBytes));
            yield break;
        }
        var continuation = afterMovement ? OnlineGroundPassAuthority.StartAction : OnlineGroundPassAuthority.MovementStartAction;
        Assert.That(playable.legalActions, Does.Contain(continuation));
        Submit(continuation);
        var next = pause.Capture(authority);
        Assert.That(afterMovement ? next.passActive && next.awaitingTarget : next.movementActive, Is.True);
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
        AssertRestoredStats(guestBoard);
        var guestView = Object.FindAnyObjectByType<PauseMenuManager>(FindObjectsInactive.Include);
        Assert.That(guestView.saveMatchAsButton.interactable, Is.False);
        guestView.saveMatchAsButton.onClick.Invoke();
        Assert.That(guestView.saveAsOverlay.activeSelf, Is.False);
        Assert.That(guestBoard.match.ball.GetCurrentHex().coordinates, Is.EqualTo(snapshot.ball.ToVector3Int()));
        ApplyPublication(replica, guestBoard, next);
        Assert.That(afterMovement ? replica.State.passActive && replica.State.awaitingTarget : replica.State.movementActive, Is.True);
        Assert.That(replica.State.resumePending, Is.False);
        Assert.That(File.ReadAllBytes(saved), Is.EqualTo(sourceBytes));

        void AssertRestoredStats(OnlineGroundPassBoard restoredBoard)
        {
            var restoredMatch = restoredBoard.match;
            var player = restoredBoard.tokens.Single(token => token.isHomeTeam && token.jerseyNumber == 2);
            var stats = restoredMatch.gameData.stats.GetPlayerStats(player);
            Assert.That(stats.goals, Is.EqualTo(1));
            Assert.That(stats.yellowCards, Is.EqualTo(1));
            Assert.That(stats.injuries, Is.EqualTo(1));
            Assert.That(player.isBooked && player.isInjured, Is.True);
            Assert.That(restoredMatch.homeScorers.Single().scorer, Is.EqualTo(scorerName));
            Assert.That(restoredBoard.liveScoreboard.homeScoreText.text, Is.EqualTo("1"), "Scoreboard refreshes before Ready");
            var statsView = restoredMatch.matchStatsUI;
            var titles = (TMPro.TMP_Text[])typeof(MatchStatsUI).GetField("statTitleTexts", flags).GetValue(statsView);
            var home = (TMPro.TMP_Text[])typeof(MatchStatsUI).GetField("homeStatsTexts", flags).GetValue(statsView);
            int cards = System.Array.FindIndex(titles, text => text.text == "Yellow / Red Cards");
            int injuries = System.Array.FindIndex(titles, text => text.text == "Injuries / Subs Used");
            Assert.That(cards, Is.GreaterThanOrEqualTo(0));
            Assert.That(injuries, Is.GreaterThanOrEqualTo(0));
            Assert.That(home[cards].text, Is.EqualTo("1/0"), "The stats panel refreshes while held");
            Assert.That(home[injuries].text, Is.EqualTo("1/0"));
        }
    }
}
