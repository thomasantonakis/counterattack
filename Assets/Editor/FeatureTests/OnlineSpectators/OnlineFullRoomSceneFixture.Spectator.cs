using System.Collections;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed partial class OnlineFullRoomSceneFixture
{
    private OnlineFrontendSession testSpectator;

    public IEnumerator CleanupSpectatorView()
    {
        if (testSpectator != null) testSpectator.Close();
        yield return Cleanup();
    }

    public IEnumerator RunSpectatorView(bool late)
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var data = Data(16, false, useFrontendSeats: true);
        const string matchId = "spectator-native-room";
        var compatibility = new OnlineCompatibility("ground-wire-v7", "ground-rules-v1", OnlineFullRoomBootstrap.Catalog);
        var protocol = OnlineFrontendSessionPolicy.CreateMatchProtocol(matchId, "home", "away", compatibility);
        protocol.BindAuthenticatedAccount("home", "h", compatibility);
        protocol.BindAuthenticatedAccount("away", "a", compatibility);
        yield return LoadNativeRoom(data, false, matchId);
        var host = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var room = new OnlineGroundPassAuthority(host.match, protocol).Capture();
        var hostStoppage = Object.FindAnyObjectByType<StoppageActionsPanelController>(FindObjectsInactive.Include);
        typeof(MatchManager).GetField("isWaitingForExtraActionsRoll", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(host.match, true);
        Assert.That(hostStoppage.Begin(host.match), Is.True);
        var roll = host.rollPanels.Capture(1);
        Assert.That(roll.diceAnimating, Does.Contain(true));
        session.Dispose();
        session = null;
        app.ClearActiveSaveContext();

        yield return SceneManager.LoadSceneAsync(OnlineModeMenu.SceneName);
        var menu = Object.FindAnyObjectByType<OnlineModeMenu>();
        var viewer = testSpectator = Object.Instantiate(menu.sessionPrefab);
        SetViewer("IsSpectator", true);
        const BindingFlags members = BindingFlags.NonPublic | BindingFlags.Instance;
        // Production transport sets the latest trusted publication before following its stage.
        var advance = typeof(OnlineFrontendSession).GetMethod("FollowSpectatorStage", members);
        var stream = new OnlineSpectatorStream(matchId);
        void Publish(byte[] payload)
        {
            foreach (var packet in stream.Encode(payload)) stream.Receive(packet, packet.Length);
            SetViewer("SpectatorPublication", stream.Latest);
            advance.Invoke(viewer, null);
        }

        if (!late)
        {
            var setup = OnlineModeMenu.HostDefaults();
            setup.homeTeamName = "First Home";
            Assert.That(OnlineSpectatorPregameWire.TryEncode(matchId, 1, setup,
                new OnlineSetupState { settings = setup }, null, out var payload), Is.True);
            Publish(payload);
            yield return WaitForScene("CreateNewGameScene");
            var setupView = Object.FindAnyObjectByType<OnlineSetupNetworkPanel>();
            Assert.That(setupView.setup.homeTeamInputField.text, Is.EqualTo("First Home"));
            Assert.That(setupView.setup.homeTeamInputField.interactable, Is.False);
            Assert.That(setupView.setup.createGameButton.gameObject.activeSelf, Is.False);
            Assert.That(setupView.backButton.IsInteractable(), Is.True);
            setup.homeTeamName = "Public Home";
            Assert.That(OnlineSpectatorPregameWire.TryEncode(matchId, 2, setup,
                new OnlineSetupState { settings = setup }, null, out payload), Is.True);
            Publish(payload);
            yield return null;
            yield return null;
            Assert.That(setupView.setup.homeTeamInputField.text, Is.EqualTo("Public Home"));

            var picks = OnlineDraftCatalog.Load();
            var draft = new OnlineDraftState
            {
                settings = setup, phase = "pick", turn = OnlineTeam.Away, batch = 1, totalBatches = 7,
                home = new OnlineDraftCard[16], away = new OnlineDraftCard[16],
                cards = new[] { picks.First(card => !card.goalkeeper) }
            };
            Assert.That(OnlineSpectatorPregameWire.TryEncode(matchId, 3, setup, null, draft, out payload), Is.True);
            Publish(payload);
            yield return WaitForScene("Draft");
            var draftView = Object.FindAnyObjectByType<OnlineDraftView>();
            Assert.That(draftView.Client, Is.Null);
            Assert.That(draftView.source.enabled, Is.False);
            Assert.That(draftView.actionButton.gameObject.activeSelf, Is.False);
            Assert.That(draftView.cards.All(card => !card.gameObject.activeSelf), Is.True);
            Assert.That(draftView.source.homeTeamName.text, Is.EqualTo("Public Home"));
            var pick = new OnlineDraftCard
            {
                id = picks.First(card => !card.goalkeeper).id,
                name = "Public pick from a different catalog",
                country = "Scotland",
                pace = 6
            };
            draft.home[1] = pick;
            Assert.That(OnlineSpectatorPregameWire.TryEncode(matchId, 4, setup, null, draft, out payload), Is.True);
            Publish(payload);
            yield return null;
            yield return null;
            Assert.That(draftView.homeSlots[1].GetComponentsInChildren<TMPro.TMP_Text>(true)
                .Any(label => label.text.Contains(pick.name)), Is.True);
        }

        room.homeStats.totalGoals = 2;
        room.half = 2;
        room.regulationSeconds = 120;
        room.homeScorers = new[] { new MatchManager.GoalEvent { scorer = "Published scorer", minute = 48 } };
        if (late) data.gameSettings.homeKit = "kit-absent-from-viewer";
        Assert.That(OnlineSpectatorRoomWire.TryEncode(room, data.gameSettings, 5, out var roomPayload), Is.True);
        Publish(roomPayload);
        yield return WaitForScene("Room");
        var startup = Object.FindAnyObjectByType<OnlineRoomStartup>();
        float deadline = Time.realtimeSinceStartup + 15f;
        while (startup.Board == null && startup.Failure == null && Time.realtimeSinceStartup < deadline)
            yield return null;
        yield return null;
        Assert.That(startup.Board, Is.Not.Null, startup.Failure ?? "Room startup timed out");
        var board = startup.Board;
        AssertDecisionPanelHidden(board);
        if (late)
            Assert.That(board.match.gameData.gameSettings.homeKit, Is.EqualTo(OnlineModeMenu.HostDefaults().homeKit));
        Assert.That(board.controls, Is.Null);
        Assert.That(board.match.IsGameplayInputBlocked, Is.True);
        Assert.That(OnlineRoomSession.Current.AuthoritySavePath, Is.Null);
        Assert.That(app.HasExplicitSaveContext, Is.False);
        Assert.That(board.match.gameData.stats.homeTeamStats.totalGoals, Is.EqualTo(2));
        Assert.That(board.match.currentHalf, Is.EqualTo(2));
        Assert.That(board.match.OnlineRegulationSeconds, Is.EqualTo(120));
        Assert.That(board.match.homeScorers.Single().scorer, Is.EqualTo("Published scorer"));
        var player = board.tokens.First(token => token.currentHex != null);
        Physics.SyncTransforms();
        Assert.That(board.boardInput.PresentHover(new Ray(player.transform.position + 10f * Vector3.up, Vector3.down), true), Is.SameAs(player));
        Assert.That(board.boardInput.TryKey(KeyCode.Space), Is.True);
        Assert.That(RoomInspectionCard.Instance.IsOpen, Is.True);
        Assert.That(board.boardInput.TryKey(KeyCode.R), Is.False);
        Assert.That(board.boardInput.TryToken(player), Is.False);
        RoomInspectionCard.Instance.Close();

        room.rollPanel = roll;
        Assert.That(OnlineSpectatorRoomWire.TryEncode(room, data.gameSettings, 6, out roomPayload), Is.True);
        Publish(roomPayload);
        yield return null;
        yield return null;
        var stoppage = Object.FindAnyObjectByType<StoppageActionsPanelController>(FindObjectsInactive.Include);
        Assert.That(stoppage.visuals.activeInHierarchy && stoppage.die.IsAnimating, Is.True);
        AssertDecisionPanelHidden(board);
        roll.dice[0] = 4;
        roll.diceAnimating[0] = false;
        Assert.That(OnlineSpectatorRoomWire.TryEncode(room, data.gameSettings, 7, out roomPayload), Is.True);
        Publish(roomPayload);
        yield return null;
        yield return null;
        Assert.That(stoppage.die.DisplayedRoll, Is.EqualTo(4));

        room.rollPanel = null;
        room.coin = new OnlineCoinTossSnapshot { purpose = nameof(CoinTossPurpose.ExtraTime), stage = "away", homeRoll = 3 };
        Assert.That(OnlineSpectatorRoomWire.TryEncode(room, data.gameSettings, 8, out roomPayload), Is.True);
        Publish(roomPayload);
        yield return null;
        yield return null;
        var coin = board.match.coinTossPanel;
        Assert.That(coin.dicePanel.visuals.activeInHierarchy, Is.True);
        Assert.That(coin.dicePanel.homeDie.DisplayedRoll, Is.EqualTo(3));
        Assert.That(coin.dicePanel.awayDie.IsAnimating, Is.True);

        room.coin = null;
        room.isSharedPaused = true;
        room.homeStats.totalGoals = 3;
        var stats = board.match.matchStatsUI;
        var expanded = typeof(MatchStatsUI).GetField("isExpanded", members);
        bool wasExpanded = (bool)expanded.GetValue(stats);
        stats.toggleButton.onClick.Invoke();
        Assert.That((bool)expanded.GetValue(stats), Is.EqualTo(!wasExpanded));
        Assert.That(OnlineSpectatorRoomWire.TryEncode(room, data.gameSettings, 9, out roomPayload), Is.True);
        Publish(roomPayload);
        yield return null;
        yield return null;
        var pauseMenu = Object.FindAnyObjectByType<PauseMenuManager>();
        Assert.That(pauseMenu.pausePanel.activeInHierarchy, Is.True);
        Assert.That(pauseMenu.resumeButton.IsInteractable(), Is.False);
        Assert.That(board.match.gameData.stats.homeTeamStats.totalGoals, Is.EqualTo(3));
        var pauseRules = pauseMenu.pausePanel.GetComponentInChildren<RulesViewerOpener>();
        Assert.That(pauseRules, Is.Not.Null);
        pauseRules.OpenRules();
        Assert.That(RulesViewer.IsOpen, Is.True);
        Object.FindAnyObjectByType<RulesViewer>(FindObjectsInactive.Include).Close();
        Assert.That(pauseMenu.pausePanel.activeInHierarchy, Is.True);
        stats.toggleButton.onClick.Invoke();
        Assert.That((bool)expanded.GetValue(stats), Is.EqualTo(wasExpanded));
        Assert.That(board.match.gameData.stats.homeTeamStats.totalGoals, Is.EqualTo(3));
        pauseMenu.backToMainMenuButton.onClick.Invoke();
        yield return WaitForScene(OnlineModeMenu.SceneName);
        Assert.That(OnlineRoomSession.Current, Is.Null);

        void SetViewer(string property, object value) =>
            typeof(OnlineFrontendSession).GetProperty(property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(viewer, value);
    }

    private static IEnumerator WaitForScene(string name)
    {
        float deadline = Time.realtimeSinceStartup + 15;
        while (SceneManager.GetActiveScene().name != name && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(name));
        yield return null;
        yield return null;
    }
}
