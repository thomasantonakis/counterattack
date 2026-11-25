using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed partial class OnlineFullRoomSceneFixture
{
    private const BindingFlags FullTimeFields = BindingFlags.Instance | BindingFlags.NonPublic;

    public IEnumerator RunNativeFullTimeReport(string ending)
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var data = Data(16, false, useFrontendSeats: true);
        var protocol = NativePresentationProtocol();
        yield return LoadNativeRoom(data, false, protocol.MatchId);
        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var match = board.match;
        var authority = new OnlineGroundPassAuthority(match, protocol);
        pause = new OnlineSharedPause(protocol, match);
        var actor = board.tokens.First(token => token.isHomeTeam && token.jerseyNumber == 9);
        match.currentHalf = ending == "extra-time" ? 4 : 2;
        match.gameData.gameSettings.tiebreaker = ending == "extra-time" ? "Extra Time" : "None";
        if (ending == "extra-time")
            match.gameData.extraTimeOpeningSetup = new MatchOpeningSetup {homeAttacksLeftToRight = false};
        match.gameData.gameLog.LogEvent(actor, MatchManager.ActionType.GoalScored);
        match.gameData.stats.awayTeamStats.totalGoals = ending is "draw" or "penalties" ? 1 : 0;
        match.ResolveGameplayDiceRoll("high_pass_accuracy", actor: actor,
            details: new Dictionary<string, string> {["highPassAttribute"] = "4", ["threshold"] = "8"});
        match.gameData.events.Add(new GameplayEvent
        {
            sequenceNumber = match.gameData.events.Last().sequenceNumber + 1,
            kind = "snapshot.action_boundary", currentHalf = match.currentHalf, teamInAttack = "Home",
            attackHasPossession = true, snapshot = new GameplaySnapshotSummary
            {
                currentState = "EndOfStandardPass",
                tokens = new List<GameplayTokenStateSummary>
                {
                    new() {tokenKey = "Home:9", status = "playing", hex = new RoomHexCoordinates {x = 3, z = 1}}
                }
            }
        });
        if (ending == "penalties")
        {
            var shootout = match.OnlineShootoutManager;
            match.gameData.penaltyShootoutSetup = new PenaltyShootoutSetup {homeTakesFirstPenalty = true};
            shootout.ShowOrderSelection();
            Assert.That(shootout.OnlineOrderSelection, Is.True);
            // Seed a completed kick series, then run its normal Room completion
            // and player celebration before admitting the terminal publication.
            shootout.homeAttempts = shootout.awayAttempts = 3;
            shootout.homeShootoutScore = 3;
            shootout.winnerIsHome = true;
            ((List<bool>)typeof(PenaltyShootoutManager).GetField("homeResults", FullTimeFields).GetValue(shootout))
                .AddRange(new[] {true, true, true});
            ((List<bool>)typeof(PenaltyShootoutManager).GetField("awayResults", FullTimeFields).GetValue(shootout))
                .AddRange(new[] {false, false, false});
            var finish = (IEnumerator)typeof(PenaltyShootoutManager).GetMethod("CompleteShootout", FullTimeFields)
                .Invoke(shootout, null);
            yield return shootout.StartCoroutine(finish);
            Assert.That(pause.Capture(authority).shootout?.homeScore, Is.EqualTo(3));
        }
        else typeof(MatchManager).GetMethod("BeginHalfEndFlow", FullTimeFields).Invoke(match, null);
        Assert.That(match.OnlineMatchComplete, Is.True);
        yield return VerifyFullTimeReport(board, authority, protocol, data, expectHistory: true);
    }

    private IEnumerator VerifyFullTimeReport(OnlineGroundPassBoard host, OnlineGroundPassAuthority authority,
        OnlineMatchProtocol protocol, MatchManager.GameData guestBootstrap, bool expectHistory)
    {
        var match = host.match;
        var expectedRows = PostMatchRatingData.Build(match.gameData.stats).Select(r => (r.Key, r.DisplayRating)).ToArray();
        var expectedLuck = PostMatchLuckData.Build(match.gameData);
        var expectedPitch = PostMatchPitchData.Build(match.gameData).Select(p => (p.Key, p.Positions.Count)).ToArray();
        Assert.That(expectedRows.Any(r => r.Key.StartsWith("Home:")), Is.True);
        Assert.That(expectedLuck.TotalRolls, Is.GreaterThan(0));
        Assert.That(expectedPitch, Is.Not.Empty);

        var exchangeType = typeof(OnlineGroundPassBoard).Assembly.GetType("CounterAttack.Online.EOSGroundPassExchange", true);
        var ctor = exchangeType.GetConstructors(FullTimeFields).Single(c => c.GetParameters().Length == 7);
        var incoming = new Queue<OnlineGroundPassMessage>();
        var outgoing = new List<OnlineGroundPassMessage>();
        object exchange = null;
        double time = 0;
        void Send(OnlineGroundPassMessage message) => outgoing.Add(Copy(message));
        void Poll()
        {
            var queue = (Queue<OnlineGroundPassMessage>)exchangeType.GetField("messages", FullTimeFields).GetValue(exchange);
            while (incoming.Count > 0) queue.Enqueue(Copy(incoming.Dequeue()));
        }

        exchange = ctor.Invoke(new object[] { host, directory, (Action<OnlineGroundPassMessage>)Send,
            (Action)Poll, "away", protocol.MatchId, (Func<double>)(() => time) });
        var hostFlow = (IEnumerator)exchangeType.GetMethod("ManualHost", FullTimeFields)
            .Invoke(exchange, new object[] {authority, protocol, pause});
        var digest = exchangeType.GetMethod("Digest", BindingFlags.Static | BindingFlags.NonPublic);
        var nonce = Guid.NewGuid().ToString("N");
        OnlineGroundPassMessage terminal;
        OnlineGroundPassMessage[] parts;
        string[] expectedScore;
        try
        {
            incoming.Enqueue(new OnlineGroundPassMessage {kind="hello", peerNonce=nonce});
            Assert.That(hostFlow.MoveNext(), Is.True);
            var resync = outgoing.Last(message => message.kind == "state");
            incoming.Enqueue(new OnlineGroundPassMessage {kind="hello", status="resync-ack", peerNonce=nonce,
                connectionGeneration=resync.connectionGeneration, digest=(string)digest.Invoke(null, new object[] {resync.state})});
            for (int i = 0; i < 8; i++) { time += .15; Assert.That(hostFlow.MoveNext(), Is.True); }
            terminal = outgoing.Last(message => message.kind == "state" && OnlineManualSessionPolicy.CanFinish(message.state, true));
            parts = outgoing.Where(message => message.kind == OnlineFullTimeReport.Part).ToArray();
            Assert.That(parts, Is.Not.Empty);
            Assert.That(parts.All(part => part.revision == terminal.state.publicationSequence));
            var hostPanel = GetPanel();
            expectedScore = ReadScore(hostPanel);
            Assert.That(hostPanel.gameObject.scene.name, Is.EqualTo("Room"));
            var report = hostPanel.GetComponent<PostMatchReportController>();
            report.ShowPlayers(); Assert.That(report.ShowingPlayers, Is.True);
            report.ShowRatings(); Assert.That(report.ShowingRatings, Is.True);
            report.ShowPitch(); Assert.That(report.ShowingPitch, Is.True);
            report.ShowLuck(); Assert.That(report.ShowingLuck, Is.True);
        }
        finally { (hostFlow as IDisposable)?.Dispose(); }

        pause.Dispose();
        pause = null;
        yield return LoadPresentationGuest(guestBootstrap, protocol.MatchId, nativeRoom: true);
        var guest = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var replica = new OnlineGroundPassReplica(protocol.MatchId, guest.tokens.Select(OnlineRollPublication.ActorId), OnlineFullRoomBootstrap.Catalog);
        incoming.Clear();
        outgoing.Clear();
        exchange = ctor.Invoke(new object[] { guest, directory, (Action<OnlineGroundPassMessage>)Send,
            (Action)Poll, "home", protocol.MatchId, (Func<double>)(() => time) });
        var guestFlow = (IEnumerator)exchangeType.GetMethod("ManualGuest", FullTimeFields).Invoke(exchange, new object[] {replica});
        try
        {
            Assert.That(guestFlow.MoveNext(), Is.True);
            string guestNonce = outgoing.Single(message => message.kind == "hello" && message.status == null).peerNonce;
            terminal.peerNonce = guestNonce;
            incoming.Enqueue(terminal);
            Assert.That(guestFlow.MoveNext(), Is.True);
            var guestPanel = GetPanel();
            Assert.That(ReadScore(guestPanel), Is.EqualTo(expectedScore));
            var report = guestPanel.GetComponent<PostMatchReportController>();
            report.ShowPitch();
            Assert.That(report.ShowingPitch, Is.False, "Pitch waits for complete history");
            report.ShowPlayers();
            Assert.That(report.ShowingPlayers, Is.True);
            Assert.That(outgoing.Any(message => message.kind == "done"), Is.False);
            if (!expectHistory)
            {
                time += 26;
                Assert.That(guestFlow.MoveNext(), Is.True);
                report.ShowLuck();
                Assert.That(report.ShowingLuck, Is.False);
                Assert.That(report.ShowingPlayers, Is.True);
                Assert.That(guestPanel.gameObject.activeInHierarchy, Is.True);
            }
            else
            {
                foreach (var part in parts)
                {
                    part.peerNonce = guestNonce;
                    incoming.Enqueue(part);
                }
                var resync = Copy(terminal);
                resync.status = "resync";
                incoming.Enqueue(resync);
                Assert.That(guestFlow.MoveNext(), Is.True);
                Assert.That(outgoing.Any(message => message.kind == "done"), Is.False, "Wait for the host to confirm the terminal state after resync");
                incoming.Enqueue(terminal);
                Assert.That(guestFlow.MoveNext(), Is.True);
                Assert.That(report.ShowingPlayers, Is.True, "Report arrival must preserve the local tab");
                var projected = (MatchManager.GameData)typeof(PostMatchReportController).GetField("matchData", FullTimeFields).GetValue(report);
                Assert.That(PostMatchRatingData.Build(projected.stats).Select(r => (r.Key, r.DisplayRating)), Is.EqualTo(expectedRows));
                Assert.That(PostMatchLuckData.Build(projected).TotalRolls, Is.EqualTo(expectedLuck.TotalRolls));
                Assert.That(PostMatchLuckData.Build(projected).ForcedRolls, Is.EqualTo(expectedLuck.ForcedRolls));
                Assert.That(PostMatchPitchData.Build(projected).Select(p => (p.Key, p.Positions.Count)), Is.EqualTo(expectedPitch));
                Assert.That(guest.match.gameData.events, Is.Empty, "Report history must never become the guest's gameplay history");
                var secondView = Object.Instantiate(Resources.Load<GameObject>("UI/EndGamePanel")).GetComponent<PostMatchReportController>();
                try
                {
                    secondView.ShowReport(projected, true);
                    secondView.ShowPitch();
                    report.ShowLuck();
                    Assert.That(secondView.ShowingPitch && report.ShowingLuck, Is.True);
                    report.ShowRatings();
                    Assert.That(secondView.ShowingPitch && report.ShowingRatings, Is.True);
                }
                finally { Object.DestroyImmediate(secondView.gameObject); }
                Assert.That(outgoing.Any(message => message.kind == "done"), Is.True);
            }
            typeof(OnlineGroundPassBoard).GetMethod("PresentSessionDeparture", FullTimeFields)
                .Invoke(guest, new object[] {"The host left."});
            Assert.That(guestPanel.gameObject.activeInHierarchy, Is.True);
        }
        finally { (guestFlow as IDisposable)?.Dispose(); }

        var leave = (Button)typeof(EndGamePanelManager).GetField("mainMenuButton", FullTimeFields).GetValue(GetPanel());
        leave.onClick.Invoke();
        yield return null;
        yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));

        EndGamePanelManager GetPanel()
        {
            var panel = Object.FindAnyObjectByType<EndGamePanelManager>();
            Assert.That(panel, Is.Not.Null, "Both native managers use the authored Full Time panel");
            return panel;
        }

        string[] ReadScore(EndGamePanelManager panel) =>
            new[] {"homeTeamScoreText", "centerScoreText", "awayTeamScoreText", "homeScorersText", "awayScorersText"}
                .Select(field => ((TMPro.TMP_Text)typeof(EndGamePanelManager).GetField(field, FullTimeFields).GetValue(panel)).text)
                .ToArray();

        OnlineGroundPassMessage Copy(OnlineGroundPassMessage message)
        {
            message.matchId = protocol.MatchId;
            var wire = new OnlineGroundPassWire();
            OnlineGroundPassMessage decoded = null;
            foreach (var packet in OnlineGroundPassWire.Encode(message)) decoded = wire.Receive(packet, packet.Length);
            Assert.That(decoded, Is.Not.Null);
            return decoded;
        }
    }
}
