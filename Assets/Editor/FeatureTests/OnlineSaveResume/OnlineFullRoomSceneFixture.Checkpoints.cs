using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

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
        void Submit(string action)
        {
            var state = pause.Capture(authority);
            var connection = state.owner == "Home" ? "host" : "guest";
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
        Assert.That(view.saveMatchAsButton.interactable, Is.True, view.pauseSaveStatusText.text);
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
    }
}
