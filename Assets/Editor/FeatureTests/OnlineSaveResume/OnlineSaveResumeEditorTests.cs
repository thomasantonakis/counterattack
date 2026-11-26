using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using CounterAttack.Online;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

[Category("OnlineSaveResume")]
public sealed class OnlineSaveResumeEditorTests
{
    private string directory;
    private OnlineCheckpointStore store;

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "OnlineSaveResume-" + Guid.NewGuid().ToString("N"));
        store = new OnlineCheckpointStore(directory, new OnlineCompatibility("protocol", "rules", "catalog"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    [TestCase("Home")]
    [TestCase("Away")]
    public void RepeatedLoadsCreateIndependentRunsWithoutChangingTheSlot(string attackingTeam)
    {
        var checkpoint = Checkpoint();
        checkpoint.match.runtimeSnapshot.teamInAttack = attackingTeam;
        var slot = store.Save(checkpoint);
        var path = Directory.GetFiles(directory).Single();
        var original = File.ReadAllBytes(path);

        var saved = store.Load(slot);
        Assert.That(saved.IsHost("host"), Is.True);
        Assert.That(saved.IsHost("guest"), Is.False);
        Assert.That(saved.IsGuest("other"), Is.False);
        Assert.That(saved.IsGuest("guest"), Is.True);

        var first = store.CreateRun(saved);
        first.events.Add(new GameplayEvent { sequenceNumber = 9 });
        first.runtimeSnapshot.ball.x = 100;
        var second = store.CreateRun(store.Load(slot));

        Assert.That(second.playSessionId, Is.Not.EqualTo(first.playSessionId));
        Assert.That(second.inheritedEventSequence, Is.EqualTo(8));
        Assert.That(second.events.Select(e => e.sequenceNumber), Is.EqualTo(new[] { 8 }));
        Assert.That(second.runtimeSnapshot.ball.x, Is.EqualTo(3));
        Assert.That(second.runtimeSnapshot.teamInAttack, Is.EqualTo(attackingTeam));
        Assert.That(second.runtimeSnapshot.decisionTiming.decisions.Single().remaining, Is.EqualTo(4));
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
    }

    [TestCase("session-wire-v3", "frontend-rules-v9")]
    [TestCase("session-wire-v4", "frontend-rules-v10")]
    [TestCase("session-wire-v5", "frontend-rules-v11")]
    public void PriorOnlineCheckpointRetainsItsStatisticsAcrossTheReportUpgrade(string priorProtocol, string priorRules)
    {
        var oldStore = new OnlineCheckpointStore(directory, new OnlineCompatibility(priorProtocol, priorRules, "catalog"));
        var newStore = new OnlineCheckpointStore(directory, new OnlineCompatibility(OnlineSessionWire.Version, "frontend-rules-v11", "catalog"));
        var checkpoint = Checkpoint();
        checkpoint.protocol = priorProtocol;
        checkpoint.rules = priorRules;
        checkpoint.match.stats.homeTeamStats.totalGoals = 3;
        var selected = oldStore.Save(checkpoint);
        var resumed = newStore.CreateRun(newStore.Load(selected));
        Assert.That(resumed.stats.homeTeamStats.totalGoals, Is.EqualTo(3));
        Assert.That(resumed.inheritedEventSequence, Is.EqualTo(8));
    }

    [Test]
    public void FailedAtomicPublishKeepsThePreviousFileAndRemovesTheTemporaryFile()
    {
        var checkpoint = Checkpoint();
        var slot = store.Save(checkpoint);
        var path = Directory.GetFiles(directory).Single();
        var original = File.ReadAllBytes(path);
        var constructor = typeof(OnlineCheckpointStore).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
            null, new[] {typeof(string), typeof(OnlineCompatibility), typeof(Action<string, string, bool>)}, null);
        var failing = (OnlineCheckpointStore)constructor.Invoke(new object[]
        {
            directory, new OnlineCompatibility("protocol", "rules", "catalog"),
            (Action<string, string, bool>)((temporary, destination, replace) =>
            {
                Assert.That(File.Exists(temporary), Is.True);
                Assert.That(destination, Is.EqualTo(path));
                Assert.That(replace, Is.True);
                throw new IOException("Injected publish failure");
            })
        });
        checkpoint.displayName = "Failed update";
        Assert.Throws<IOException>(() => failing.Save(checkpoint, slot));
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
        Assert.That(Directory.GetFiles(directory), Is.EqualTo(new[] {path}));
    }

    [Test]
    public void BrowserSummarizesValidSavesAndReportsDamagedFiles()
    {
        var checkpoint = Checkpoint();
        checkpoint.match.stats.homeTeamStats.totalGoals = 2;
        var slot = store.Save(checkpoint);
        File.WriteAllText(Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json"), "{bad json");
        var entry = store.Browse(out int unreadable).Single();
        Assert.That(entry.Slot, Is.EqualTo(slot));
        Assert.That(entry.Teams, Is.EqualTo("Home vs Away"));
        Assert.That(entry.Score, Is.EqualTo("2–0"));
        Assert.That(unreadable, Is.EqualTo(1));
    }

    [Test]
    public void BrowserListsCurrentCheckpointAlongsideAnIncompatibleOnlineSave()
    {
        var older = Checkpoint();
        older.rules = "older-rules";
        var oldStore = new OnlineCheckpointStore(directory, new OnlineCompatibility("protocol", older.rules, "catalog"));
        var olderSlot = oldStore.Save(older);
        var currentSlot = store.Save(Checkpoint());

        var entries = store.Browse(out int skipped);

        Assert.That(entries.Select(entry => entry.Slot), Is.EqualTo(new[] { currentSlot }));
        Assert.That(skipped, Is.EqualTo(1));
        Assert.That(File.Exists(Path.Combine(directory, olderSlot.ToString("N") + ".json")), Is.True);
    }

    [Test]
    public void ResumeBootstrapRequiresTheAuthenticatedPeerAndMatchingAcknowledgement()
    {
        var compatibility = new OnlineCompatibility(OnlineSessionWire.Version, "rules", "catalog");
        var protocol = OnlineFrontendSessionPolicy.CreateMatchProtocol("resume-attempt", "host", "guest", compatibility);
        protocol.BindAuthenticatedAccount("host", "local", compatibility);
        var host = new OnlineSetupHostChannel(protocol, "local", "guest", OnlineSetupAgreementEditorTests.Valid(), () => 0);
        var guest = new OnlineSetupGuestChannel(protocol.MatchId, "host", compatibility, OnlineTeam.Home, () => 0);
        var launchType = typeof(OnlineRoomLaunchData);
        var source = store.CreateRun(Checkpoint());
        source.sourceSavePath = "/host-only";
        var bootstrap = (MatchManager.GameData)launchType.GetMethod("ResumeBootstrap", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] {source});
        typeof(OnlineSetupHostChannel).GetMethod("EnableResume", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(host, new object[] {bootstrap});

        var toHost = new Queue<byte[]>();
        var toGuest = new Queue<byte[]>();
        host.Outgoing += toGuest.Enqueue;
        guest.Outgoing += toHost.Enqueue;
        host.Begin();
        Receive(toGuest, guest, "host");
        foreach (byte[] packet in toHost)
            Assert.That(host.Receive("another-device", packet, packet.Length), Is.False);
        Assert.That(host.Ready, Is.False);
        Receive(toHost, host, "guest");
        Receive(toGuest, guest, "host");
        Assert.That(guest.ResumeData.rosters.home.Count, Is.EqualTo(16));
        Assert.That(guest.ResumeData.events, Is.Empty);
        Assert.That(guest.ResumeData.runtimeSnapshot, Is.Null);
        Assert.That(guest.ResumeData.sourceSavePath, Is.Null);

        foreach (var invalid in new[]
        {
            new OnlineSessionMessage {kind="resume-ack", matchId="old-attempt", nonce=host.Nonce, resumeToken=Guid.NewGuid().ToString("N")},
            new OnlineSessionMessage {kind="resume-ack", matchId=protocol.MatchId, nonce=host.Nonce, resumeToken=Guid.NewGuid().ToString("N")}
        })
        {
            foreach (byte[] packet in OnlineSessionWire.Encode(invalid))
                host.Receive("guest", packet, packet.Length);
            Assert.That(host.ResumeAcknowledged, Is.False);
        }
        Receive(toHost, host, "guest");
        Assert.That(host.ResumeAcknowledged, Is.True);
        Assert.That(host.Draft, Is.Null);

        static void Receive(Queue<byte[]> queue, OnlineSessionChannel receiver, string sender)
        {
            while (queue.Count > 0)
            {
                var packet = queue.Dequeue();
                Assert.That(receiver.Receive(sender, packet, packet.Length), Is.True);
            }
        }
    }

    [Test]
    public void RejectedOverwriteKeepsThePreviousCheckpoint()
    {
        var checkpoint = Checkpoint();
        var slot = store.Save(checkpoint);
        var path = Directory.GetFiles(directory).Single();
        var original = File.ReadAllBytes(path);

        Assert.Throws<InvalidDataException>(() => store.Save(Checkpoint(), slot));
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));

        checkpoint.checkpointId = Guid.NewGuid();
        checkpoint.displayName = "Updated";
        store.Save(checkpoint, slot);
        Assert.That(store.Load(slot).checkpointId, Is.EqualTo(checkpoint.checkpointId));
        Assert.That(Directory.GetFiles(directory), Is.EqualTo(new[] { path }));
    }

    [TestCase("schemaVersion")]
    [TestCase("catalog")]
    [TestCase("clock")]
    public void InvalidEnvelopeCannotBeLoaded(string field)
    {
        var slot = store.Save(Checkpoint());
        var path = Directory.GetFiles(directory).Single();
        var json = JObject.Parse(File.ReadAllText(path));
        if (field == "clock")
            ((JObject)json["match"]["runtimeSnapshot"]).Remove(field);
        else
            json[field] = 999;
        File.WriteAllText(path, json.ToString());
        Assert.Throws<InvalidDataException>(() => store.Load(slot));
    }

    [TestCase("MovementPhase", null)]
    [TestCase("KickOffSetup", null)]
    [TestCase("WaitingForThrowInTaker", null)]
    [TestCase("GoalKick", null)]
    [TestCase("EndOfStandardPass", "pending-roll")]
    public void UnsupportedStateOrUnsettledRollCannotBeSaved(string state, string rollKey)
    {
        var checkpoint = Checkpoint();
        checkpoint.match.runtimeSnapshot.currentState = state;
        checkpoint.match.runtimeSnapshot.decisionTiming.rollKey = rollKey;
        Assert.Throws<InvalidDataException>(() => store.Save(checkpoint));
        Assert.That(Directory.Exists(directory), Is.False);
    }

    [TestCase("EndOfMovementPhase")]
    [TestCase("EndOfFirstTimePass")]
    [TestCase("EndOfLongBall")]
    [TestCase("SuccessfulTackle")]
    [TestCase("BallControl")]
    [TestCase("HeaderCompleted")]
    [TestCase("AnyOtherScenario")]
    public void SettledActionChoiceCanBeStored(string state)
    {
        var checkpoint = Checkpoint();
        checkpoint.match.runtimeSnapshot.currentState = state;
        Assert.That(store.Load(store.Save(checkpoint)).match.runtimeSnapshot.currentState, Is.EqualTo(state));
    }

    private static OnlineCheckpoint Checkpoint()
    {
        var match = new MatchManager.GameData
        {
            openingSetup = new MatchOpeningSetup(),
            runtimeSnapshot = new RoomRuntimeSnapshot
            {
                currentState = nameof(MatchManager.GameState.EndOfStandardPass),
                attackHasPossession = true,
                teamInAttack = "Home",
                homeTeamDirection = "LeftToRight",
                awayTeamDirection = "RightToLeft",
                clock = new RoomClockSnapshot { currentHalf = 1, totalCompletedActions = 1 },
                ball = new RoomHexCoordinates { x = 3 },
                stats = new MatchManager.Stats(),
                decisionTiming = new DecisionTimingSnapshot()
            }
        };
        match.gameSettings.gameMode = ApplicationManager.OnlineGameMode;
        match.gameSettings.homeTeamName = "Home";
        match.gameSettings.awayTeamName = "Away";
        match.events.Add(new GameplayEvent { sequenceNumber = 8 });
        match.runtimeSnapshot.decisionTiming.decisions.Add(new DecisionTimingState { remaining = 4 });
        foreach (var side in new[] { "Home", "Away" })
        for (int jersey = 1; jersey <= 16; jersey++)
        {
            (side == "Home" ? match.rosters.home : match.rosters.away).Add(jersey.ToString(), new MatchManager.RosterPlayer());
            match.runtimeSnapshot.tokens.Add(new RoomTokenSnapshot { tokenKey = side + ":" + jersey });
        }
        return new OnlineCheckpoint
        {
            matchId = Guid.NewGuid(),
            checkpointId = Guid.NewGuid(),
            savedUtc = DateTime.UtcNow,
            displayName = "Checkpoint",
            protocol = "protocol",
            rules = "rules",
            catalog = "catalog",
            hostAccount = "host",
            guestAccount = "guest",
            match = match
        };
    }
}
