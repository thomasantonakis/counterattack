using System;
using System.IO;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

[Category("OnlineGroundPassNetwork")]
public sealed class OnlineGroundPassWireEditorTests
{
    [Test]
    public void LocalPackagedGameplayRoleParsesWithoutNetworkInputs()
    {
        var output = Path.Combine(Path.GetTempPath(), "ca-local-" + Guid.NewGuid().ToString("N"));
        Assert.That(EOSProofArguments.Parse(new[] { "-ca-eos-role", "local", "-ca-eos-output", output }).Role, Is.EqualTo("local"));
    }
    private static OnlineGroundPassState State(long sequence = 1) => new()
    {
        matchId = "match", revision = 3, publicationSequence = sequence, stage = "target", owner = "Home",
        tokens = new[] { new OnlineGroundPassTokenState { id = "home:9", onBoard = true, x = 0, z = 2 } },
        legalActions = new[] { "ground.target:0:6" }
    };

    [TestCase(1)] [TestCase(100)] [TestCase(2000)]
    public void FragmentedStateReassemblesAndRoundTrips(int actions)
    {
        var state = State(); state.legalActions = Enumerable.Range(0, actions).Select(i => "ground.target:" + i + ":0").ToArray();
        var packets = OnlineGroundPassWire.Encode(new OnlineGroundPassMessage { kind = "state", matchId = "match", state = state }).ToArray();
        Assert.That(packets.All(p => p.Length <= OnlineGroundPassWire.FragmentPayload + OnlineGroundPassWire.HeaderSize && p.Length <= 1170), Is.True, "EOS packet limit is respected");
        var reader = new OnlineGroundPassWire(); OnlineGroundPassMessage decoded = null;
        for (var i = 0; i < packets.Length; i++)
        {
            decoded = reader.Receive(packets[i], packets[i].Length);
            if (i < packets.Length - 1) Assert.That(decoded, Is.Null, "partial state cannot publish");
        }
        Assert.That(JsonUtility.ToJson(decoded.state), Is.EqualTo(JsonUtility.ToJson(state)));
    }

    [Test]
    public void OutOfOrderFragmentIsRejectedAndParserCanRecover()
    {
        var message = new OnlineGroundPassMessage { kind = "hello", matchId = "match", digest = new string('x', 2000) };
        var packets = OnlineGroundPassWire.Encode(message).ToArray();
        var reader = new OnlineGroundPassWire();
        Assert.Throws<InvalidDataException>(() => reader.Receive(packets[1], packets[1].Length));
        OnlineGroundPassMessage decoded = null;
        foreach (var packet in packets) decoded = reader.Receive(packet, packet.Length);
        Assert.That(decoded.kind, Is.EqualTo("hello"));
    }

    [TestCase(0)] [TestCase(10)] [TestCase(1171)]
    public void InvalidPacketLengthsAreRejected(int length)
        => Assert.Throws<InvalidDataException>(() => new OnlineGroundPassWire().Receive(new byte[1200], length));

    [Test]
    public void OversizedMessageIsRejectedBeforeTransmission()
        => Assert.Throws<InvalidDataException>(() => OnlineGroundPassWire.Encode(new OnlineGroundPassMessage
            { kind = "hello", matchId = "match", digest = new string('x', OnlineGroundPassWire.PayloadLimit) }).ToArray());

    [Test]
    public void UnknownMessageKindIsRejected()
    {
        var packet = OnlineGroundPassWire.Encode(new OnlineGroundPassMessage { kind = "execute_anything", matchId = "match" }).Single();
        Assert.Throws<InvalidDataException>(() => new OnlineGroundPassWire().Receive(packet, packet.Length));
    }

    [Test]
    public void ReplicaRejectsOldForeignAndConflictingPublicationsWithoutMutation()
    {
        var replica = new OnlineGroundPassReplica("match");
        Assert.That(replica.Apply(State(2)), Is.True);
        var initial = JsonUtility.ToJson(replica.State);
        Assert.That(replica.Apply(State(1)), Is.False);
        var foreign = State(3); foreign.matchId = "other";
        Assert.That(replica.Apply(foreign), Is.False);
        var conflict = State(2); conflict.ballX = 99;
        Assert.That(replica.Apply(conflict), Is.False);
        Assert.That(JsonUtility.ToJson(replica.State), Is.EqualTo(initial));
        Assert.That(replica.Apply(State(2)), Is.True, "identical publication is idempotent");
    }

    [Test]
    public void AsynchronousOutcomeWithSameCommandRevisionHasNewPublication()
    {
        var replica = new OnlineGroundPassReplica("match"); replica.Apply(State(1));
        var next = State(2); next.ballZ = 6;
        Assert.That(replica.Apply(next), Is.True);
        Assert.That(replica.State.ballZ, Is.EqualTo(6));
        next.ballZ = 100; var detached = replica.State; detached.tokens[0].x = 100;
        Assert.That(replica.State.ballZ, Is.EqualTo(6));
        Assert.That(replica.State.tokens[0].x, Is.EqualTo(0));
    }

    [Test]
    public void DuplicateRosterIdentityIsRejected()
    {
        var bad = State(); bad.tokens = new[] { bad.tokens[0], bad.tokens[0] };
        Assert.That(new OnlineGroundPassReplica("match").Apply(bad), Is.False);
    }
}
