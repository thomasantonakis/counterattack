using System;
using System.IO;
using System.Linq;
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
    [TestCase("EndOfStandardPass", "pending-roll")]
    public void UnsupportedStateOrUnsettledRollCannotBeSaved(string state, string rollKey)
    {
        var checkpoint = Checkpoint();
        checkpoint.match.runtimeSnapshot.currentState = state;
        checkpoint.match.runtimeSnapshot.decisionTiming.rollKey = rollKey;
        Assert.Throws<InvalidDataException>(() => store.Save(checkpoint));
        Assert.That(Directory.Exists(directory), Is.False);
    }

    private static OnlineCheckpoint Checkpoint()
    {
        var match = new MatchManager.GameData
        {
            openingSetup = new MatchOpeningSetup(),
            runtimeSnapshot = new RoomRuntimeSnapshot
            {
                currentState = nameof(MatchManager.GameState.EndOfStandardPass),
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
        for (int jersey = 1; jersey <= 11; jersey++)
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
