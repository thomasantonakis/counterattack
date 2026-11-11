using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text;

[Category("OnlineSharedPause")]
public sealed class OnlineSharedPauseEditorTests
{
    private GameObject root;
    private MatchManager match;
    private OnlineMatchProtocol protocol;
    private OnlineSharedPause pause;
    private float scale;
    [SetUp] public void SetUp()
    {
        scale = Time.timeScale;
        root = new GameObject("Owned shared pause test"); root.SetActive(false); match = root.AddComponent<MatchManager>();
        var version = new OnlineCompatibility("v2", "rules", "fixture");
        protocol = new OnlineMatchProtocol("match", "home", "away", "away", version);
        protocol.BindAuthenticatedAccount("home", "h", version); protocol.BindAuthenticatedAccount("away", "a", version);
        pause = new OnlineSharedPause(protocol, match);
    }
    [TearDown] public void TearDown()
    { pause?.Dispose(); if (root != null) Object.DestroyImmediate(root); Time.timeScale = scale; }

    [TestCase("unknown", "match", 1, 0, "shared-pause", "pause.request", OnlineCommandStatus.UnknownConnection)]
    [TestCase("h", "wrong", 1, 0, "shared-pause", "pause.request", OnlineCommandStatus.WrongMatch)]
    [TestCase("h", "match", 0, 0, "shared-pause", "pause.request", OnlineCommandStatus.InvalidCommand)]
    [TestCase("h", "match", 1, 1, "shared-pause", "pause.request", OnlineCommandStatus.StaleRevision)]
    [TestCase("h", "match", 1, 0, "wrong", "pause.request", OnlineCommandStatus.WrongDecision)]
    [TestCase("h", "match", 1, 0, "shared-pause", "anything", OnlineCommandStatus.IllegalAction)]
    [TestCase("h", "match", 1, 0, "shared-pause", "pause.resume", OnlineCommandStatus.NotReady)]
    public void InvalidControlCannotMutatePause(string connection, string matchId, long sequence, long revision,
        string decision, string action, OnlineCommandStatus expected)
    {
        Assert.That(pause.Submit(connection, new OnlineMatchCommand(matchId, sequence, revision, decision, action)).Status, Is.EqualTo(expected));
        Assert.That(pause.IsPaused || match.IsPauseMenuOpen, Is.False);
        Assert.That(pause.Revision, Is.Zero); Assert.That(Time.timeScale, Is.EqualTo(scale));
    }

    [Test] public void DisposeRestoresOwnedPauseAndScaleWithoutTouchingGameState()
    {
        Time.timeScale = .6f;
        var state = match.currentState;
        Assert.That(pause.Submit("h", new OnlineMatchCommand("match", 1, 0, "shared-pause", "pause.request")).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        pause.Dispose(); pause.Dispose();
        Assert.That(Time.timeScale, Is.EqualTo(.6f)); Assert.That(match.IsPauseMenuOpen, Is.False);
        Assert.That(match.currentState, Is.EqualTo(state));
    }

    [Test] public void ControlReceiptsAreBoundedAndCannotReplayAnExpiredPause()
    {
        for (long sequence = 1; sequence <= 66; sequence++)
            Assert.That(pause.Submit("h", new OnlineMatchCommand("match", sequence, pause.Revision, "shared-pause",
                sequence % 2 == 1 ? "pause.request" : "pause.resume")).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(pause.Submit("h", new OnlineMatchCommand("match", 1, 0, "shared-pause", "pause.request")).Status, Is.EqualTo(OnlineCommandStatus.StaleSequence));
        Assert.That(pause.IsPaused, Is.False);
        Assert.That(pause.Submit("h", new OnlineMatchCommand("match", 66, 65, "shared-pause", "pause.request")).Status, Is.EqualTo(OnlineCommandStatus.SequenceConflict));
    }

    [Test] public void VersionTwoControlFramePreservesItsSeparateRevisionAndSequence()
    {
        var message = new OnlineGroundPassMessage { kind = "control", matchId = "match", sequence = 7,
            revision = 3, decisionId = "shared-pause", actionId = OnlineSharedPause.RequestAction };
        var packet = OnlineGroundPassWire.Encode(message).Single();
        var decoded = new OnlineGroundPassWire().Receive(packet, packet.Length);
        Assert.That(decoded.kind, Is.EqualTo("control")); Assert.That(decoded.version, Is.EqualTo("ground-wire-v2"));
        Assert.That(decoded.ToCommand().Sequence, Is.EqualTo(7)); Assert.That(decoded.ToCommand().Revision, Is.EqualTo(3));
        Assert.That(decoded.ToCommand().DecisionId, Is.EqualTo("shared-pause"));
    }

    [Test] public void OldWireVersionCannotSilentlySkipSharedPause()
    {
        var packet = OnlineGroundPassWire.Encode(new OnlineGroundPassMessage { kind = "hello", matchId = "match" }).Single();
        var oldJson = Encoding.UTF8.GetString(packet, 10, packet.Length - 10).Replace("ground-wire-v2", "ground-wire-v1");
        var oldBytes = Encoding.UTF8.GetBytes(oldJson); System.Buffer.BlockCopy(oldBytes, 0, packet, 10, oldBytes.Length);
        Assert.Throws<InvalidDataException>(() => new OnlineGroundPassWire().Receive(packet, packet.Length));
    }
}
