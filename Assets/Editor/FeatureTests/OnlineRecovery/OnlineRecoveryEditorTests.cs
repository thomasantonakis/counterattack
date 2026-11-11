using System;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

[Category("OnlineRecovery")]
public sealed class OnlineRecoveryEditorTests
{
    private GameObject root;
    private OnlineMatchProtocol protocol;
    private OnlineSharedPause pause;
    private OnlinePeerRecovery recovery;
    private OnlineCompatibility version;
    private double now;
    private float scale;

    [SetUp] public void Setup()
    {
        now = 0;
        scale = Time.timeScale;
        root = new GameObject("Owned recovery test"); root.SetActive(false);
        version = new OnlineCompatibility("recovery-v1", "rules", "fixture");
        protocol = new OnlineMatchProtocol("match", "home", "away", "away", version);
        protocol.BindAuthenticatedAccount("away", "host", version);
        pause = new OnlineSharedPause(protocol, root.AddComponent<MatchManager>());
        recovery = new OnlinePeerRecovery(protocol, version, pause, "home", () => now);
        Assert.That(recovery.BindAuthenticated("home", "guest-1", version), Is.True);
        Assert.That(recovery.AcknowledgeSnapshot("guest-1", 1), Is.True);
    }
    [TearDown] public void Cleanup()
    { pause?.Dispose(); if (root != null) UnityEngine.Object.DestroyImmediate(root); Time.timeScale = scale; }

    [Test] public void TimeoutRetiresPeerAndReturnKeepsAcceptedReceipt()
    {
        protocol.PublishDecision("preview", OnlineTeam.Home, new[] { "target" });
        var command = new OnlineMatchCommand("match", 1, protocol.Revision, "preview", "target");
        Assert.That(protocol.Admit("guest-1", command).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        now = 5; recovery.Tick();
        Assert.That(recovery.State, Is.EqualTo(OnlinePeerState.TransportLost));
        Assert.That(pause.ConnectionHeld && Time.timeScale == 0, Is.True);
        Assert.That(recovery.CanSubmit || protocol.BothConnected, Is.False);
        Assert.That(recovery.BindAuthenticated("other", "impostor", version), Is.False);
        Assert.That(recovery.BindAuthenticated("home", "guest-2", version), Is.True);
        Assert.That(recovery.Heartbeat("guest-1", 1, 2), Is.False);
        Assert.That(recovery.AcknowledgeSnapshot("guest-2", 1), Is.False);
        Assert.That(recovery.CanSubmit, Is.False);
        Assert.That(protocol.Admit("guest-1", command).Status, Is.EqualTo(OnlineCommandStatus.UnknownConnection));
        Assert.That(protocol.Admit("guest-2", command).Status, Is.EqualTo(OnlineCommandStatus.Duplicate));
        Assert.That(recovery.AcknowledgeSnapshot("guest-2", 2), Is.True);
        Assert.That(recovery.CanSubmit && !pause.ConnectionHeld, Is.True);
    }

    [Test] public void ReplayedHeartbeatCannotKeepDeadConnectionAlive()
    {
        now = 4; Assert.That(recovery.Heartbeat("guest-1", 1, 1), Is.True);
        now = 8; Assert.That(recovery.Heartbeat("guest-1", 1, 1), Is.False);
        now = 9; recovery.Tick();
        Assert.That(recovery.State, Is.EqualTo(OnlinePeerState.TransportLost));
        Assert.That(recovery.Heartbeat("guest-1", 1, 2), Is.False);
    }

    [TestCase(true)] [TestCase(false)]
    public void ConnectionHoldCannotReleaseRequesterPause(bool pauseFirst)
    {
        Time.timeScale = .6f;
        if (!pauseFirst) pause.SetConnectionHold(true);
        Assert.That(pause.Submit("guest-1", new OnlineMatchCommand("match", 1, 0, "shared-pause", "pause.request")).Status,
            Is.EqualTo(OnlineCommandStatus.Accepted));
        if (pauseFirst) pause.SetConnectionHold(true);
        pause.SetConnectionHold(false);
        Assert.That(pause.IsPaused && Time.timeScale == 0, Is.True);
        Assert.That(pause.Requester, Is.EqualTo(OnlineTeam.Home));
        Assert.That(pause.Submit("host", new OnlineMatchCommand("match", 1, 1, "shared-pause", "pause.resume")).Status,
            Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(pause.Submit("guest-1", new OnlineMatchCommand("match", 2, 1, "shared-pause", "pause.resume")).Status,
            Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(Time.timeScale, Is.EqualTo(.6f));
    }

    [Test] public void ResumeDuringResyncCannotReleaseConnectionHold()
    {
        pause.Submit("guest-1", new OnlineMatchCommand("match", 1, 0, "shared-pause", "pause.request"));
        pause.SetConnectionHold(true);
        pause.Submit("guest-1", new OnlineMatchCommand("match", 2, 1, "shared-pause", "pause.resume"));
        Assert.That(!pause.IsPaused && pause.ConnectionHeld && Time.timeScale == 0, Is.True);
        pause.SetConnectionHold(false); Assert.That(Time.timeScale, Is.EqualTo(scale));
    }

    [TestCase(true)] [TestCase(false)] public void LeaveAndCancelAreTerminal(bool leave)
    {
        if (leave) recovery.Leave("guest-1", 1); else recovery.Cancel();
        Assert.That(recovery.State, Is.EqualTo(leave ? OnlinePeerState.Left : OnlinePeerState.Cancelled));
        Assert.That(recovery.BindAuthenticated("home", "guest-2", version), Is.False);
        Assert.That(pause.ConnectionHeld, Is.True);
    }

    [Test] public void ClockRegressionFailsClosed()
    { now = -1; Assert.Throws<InvalidOperationException>(() => recovery.Tick()); }
}
