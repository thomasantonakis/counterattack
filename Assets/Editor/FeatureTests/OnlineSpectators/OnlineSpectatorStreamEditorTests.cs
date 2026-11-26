using System;
using System.IO;
using System.Linq;
using System.Text;
using CounterAttack.Online;
using NUnit.Framework;

public sealed class OnlineSpectatorStreamEditorTests
{
    private const string Match = "spectator-test";

    [Test]
    public void ReassemblesAFullSnapshotAndDropsOlderPublications()
    {
        var stream = new OnlineSpectatorStream(Match);
        var packets = stream.Encode(Snapshot(Match, 4, large: true)).ToArray();
        Assert.That(packets.Length, Is.GreaterThan(1));
        foreach (var packet in packets.Take(packets.Length - 1))
        {
            Assert.That(stream.Receive(packet, packet.Length), Is.False);
            Assert.That(stream.Latest, Is.Null);
        }
        Assert.That(stream.Receive(packets.Last(), packets.Last().Length), Is.True);
        Assert.That(stream.Sequence, Is.EqualTo(4));
        Assert.That((int)stream.Latest["home"]["stats"]["totalGoals"], Is.EqualTo(2));
        Assert.That(Deliver(stream, Snapshot(Match, 3)), Is.False);
        Assert.That(Deliver(stream, Snapshot(Match, 4)), Is.False);
        Assert.That(stream.Sequence, Is.EqualTo(4));
    }

    [Test]
    public void CompleteUpdateSupersedesAnInterruptedSnapshot()
    {
        var stream = new OnlineSpectatorStream(Match);
        var old = stream.Encode(Snapshot(Match, 1, large: true)).ToArray();
        Assert.That(old.Length, Is.GreaterThan(1));
        Assert.That(stream.Receive(old[0], old[0].Length), Is.False);
        Assert.That(Deliver(stream, Snapshot(Match, 2)), Is.True);
        Assert.That(stream.Receive(old[1], old[1].Length), Is.False);
        Assert.That(stream.Sequence, Is.EqualTo(2));
    }

    [Test]
    public void OtherMatchesAndManagerCommandsNeverBecomeSpectatorState()
    {
        var stream = new OnlineSpectatorStream(Match);
        Assert.Throws<InvalidDataException>(() => Deliver(stream, Snapshot("another-match", 99)));
        var command = new OnlineGroundPassMessage { matchId = Match, kind = "command", actionId = "ground.start" };
        var packet = OnlineGroundPassWire.Encode(command).First();
        Assert.Throws<InvalidDataException>(() => stream.Receive(packet, packet.Length));
        Assert.Throws<InvalidDataException>(() => Deliver(stream,
            Encoding.UTF8.GetBytes("{\"version\":99,\"matchId\":\"spectator-test\",\"stage\":\"room\",\"sequence\":99}")));
        Assert.That(stream.Sequence, Is.Zero);
        Assert.That(Deliver(stream, Snapshot(Match, 1)), Is.True);
    }

    [Test]
    public void FollowsPregameStagesWithTheSameMatchIdentityAsRoom()
    {
        var stream = new OnlineSpectatorStream(Match);
        var settings = new GameSettings
        {
            homeTeamName = "Home club", homeKit = "kit-home", halfDuration = 45,
            referee = "Random", homeRoomPersona = "private-persona"
        };
        var setup = new OnlineSetupState { matchId = "manager-match", settings = settings, homeSequence = 85 };
        var draft = new OnlineDraftState
        {
            matchId = "manager-match", phase = "pick", settings = settings, roomSessionId = "private-save-session",
            homeRoll = 4, awayRoll = 2, selection = "private-selection",
            cards = new[] { new OnlineDraftCard { name = "private-offer" } },
            home = new[] { null, new OnlineDraftCard { id = "o0042", name = "Revealed Pick" } }
        };

        Assert.That(OnlineSpectatorPregameWire.TryEncode(Match, 1, settings, null, null, out var lobby), Is.True);
        Assert.That(Deliver(stream, lobby), Is.True);
        Assert.That((string)stream.Latest["stage"], Is.EqualTo("lobby"));
        Assert.That(OnlineSpectatorPregameWire.TryEncode(Match, 2, settings, setup, null, out var setupPacket), Is.True);
        Assert.That(Deliver(stream, setupPacket), Is.True);
        Assert.That((string)stream.Latest["stage"], Is.EqualTo("setup"));
        Assert.That((string)stream.Latest["teams"]["home"]["kit"], Is.EqualTo("kit-home"));
        Assert.That((int)stream.Latest["settings"]["halfDuration"], Is.EqualTo(45));
        Assert.That(stream.Latest.ToString(), Does.Not.Contain("homeSequence").And.Not.Contain("private-persona"));

        Assert.That(OnlineSpectatorPregameWire.TryEncode(Match, 3, settings, setup, draft, out var draftPacket), Is.True);
        Assert.That(Deliver(stream, draftPacket), Is.True);
        Assert.That((string)stream.Latest["stage"], Is.EqualTo("draft"));
        Assert.That((string)stream.Latest["draft"]["home"][1]["name"], Is.EqualTo("Revealed Pick"));
        Assert.That((int)stream.Latest["draft"]["homeRoll"], Is.EqualTo(4));
        Assert.That(stream.Latest.ToString(),
            Does.Not.Contain("private-offer").And.Not.Contain("private-save-session").And.Not.Contain("private-selection"));
        Assert.That(Deliver(stream, Snapshot(Match, 4)), Is.True);
        Assert.That(Deliver(stream, draftPacket), Is.False);
        Assert.That(OnlineSpectatorPregameWire.TryEncode(Match, 5, settings, setup, draft, out var oldStage), Is.True);
        Assert.That(Deliver(stream, oldStage), Is.False);
        Assert.That((string)stream.Latest["stage"], Is.EqualTo("room"));
    }

    private static bool Deliver(OnlineSpectatorStream stream, byte[] snapshot)
    {
        bool applied = false;
        foreach (var packet in stream.Encode(snapshot))
            applied |= stream.Receive(packet, packet.Length);
        return applied;
    }

    private static byte[] Snapshot(string match, int sequence, bool large = false)
    {
        var state = new OnlineGroundPassState
        {
            matchId = match, catalogId = OnlineFullRoomBootstrap.Catalog,
            homeStats = new MatchManager.TeamStats { totalGoals = 2 }
        };
        var random = new Random(151);
        var noise = new byte[2700];
        random.NextBytes(noise);
        var settings = new MatchManager.GameSettings
        {
            homeTeamName = large ? Convert.ToBase64String(noise) : "Home", awayTeamName = "Away"
        };
        Assert.That(OnlineSpectatorRoomWire.TryEncode(state, settings, sequence, out var payload), Is.True);
        return payload;
    }
}
