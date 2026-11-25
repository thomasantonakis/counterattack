using System;
using System.IO;
using System.Linq;
using System.Text;
using CounterAttack.Online;
using NUnit.Framework;

public sealed class OnlineSpectatorRoomStreamEditorTests
{
    private const string Match = "spectator-test";

    [Test]
    public void ReassemblesAFullSnapshotAndDropsOlderPublications()
    {
        var stream = new OnlineSpectatorRoomStream(Match);
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
        var stream = new OnlineSpectatorRoomStream(Match);
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
        var stream = new OnlineSpectatorRoomStream(Match);
        Assert.Throws<InvalidDataException>(() => Deliver(stream, Snapshot("another-match", 99)));
        var command = new OnlineGroundPassMessage { matchId = Match, kind = "command", actionId = "ground.start" };
        var packet = OnlineGroundPassWire.Encode(command).First();
        Assert.Throws<InvalidDataException>(() => stream.Receive(packet, packet.Length));
        Assert.Throws<InvalidDataException>(() => Deliver(stream,
            Encoding.UTF8.GetBytes("{\"version\":99,\"matchId\":\"spectator-test\",\"stage\":\"room\",\"sequence\":99}")));
        Assert.That(stream.Sequence, Is.Zero);
        Assert.That(Deliver(stream, Snapshot(Match, 1)), Is.True);
    }

    private static bool Deliver(OnlineSpectatorRoomStream stream, byte[] snapshot)
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
