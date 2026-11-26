using System.Collections;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineRecovery(int side, string boundary)
    {
        var authority = OnlineGroundBoard(side, true, boundary == "roll", out var protocol, out _);
        using var pause = new OnlineSharedPause(protocol, match);
        double now = 0;
        var recovery = new OnlinePeerRecovery(protocol, OnlineGroundVersion(), pause, "home-account", () => now);
        Assert.That(recovery.BindAuthenticated("home-account", "guest-generation-1", OnlineGroundVersion()), Is.True);
        recovery.AcknowledgeSnapshot("guest-generation-1", 1);
        OnlineAccepted(authority, "guest-generation-1", 1, OnlineGroundPassAuthority.StartAction);
        OnlineAccepted(authority, "guest-generation-1", 2, OnlineGroundPassAuthority.TargetAction(0, 6));
        if (boundary == "movement" || boundary == "roll")
        {
            OnlineAccepted(authority, "guest-generation-1", 3, OnlineGroundPassAuthority.TargetAction(0, 6));
            yield return Until(() => boundary == "movement" ? ball.IsCellMoveInProgress : ground.isWaitingForDiceRoll,
                "real recovery boundary " + boundary);
        }
        if (boundary == "paused")
            Assert.That(pause.Submit("guest-generation-1", SharedControl(protocol, pause, 1, OnlineSharedPause.RequestAction)).Status,
                Is.EqualTo(OnlineCommandStatus.Accepted));
        var queuedState = authority.Capture();
        var queued = OnlineGroundCommand(queuedState, boundary == "roll" ? 1 : 3,
            boundary == "roll" ? OnlineGroundPassAuthority.RollAction : OnlineGroundPassAuthority.TargetAction(0, 6));
        var beforeLoss = pause.Capture(authority);
        now = 5; recovery.Tick();
        var heldState = pause.Capture(authority);
        Assert.That(heldState.publicationSequence, Is.GreaterThan(beforeLoss.publicationSequence));
        Assert.That(heldState.connectionHeld, Is.True);
        var replica = new OnlineGroundPassReplica(protocol.MatchId);
        Assert.That(replica.Apply(beforeLoss) && replica.Apply(heldState), Is.True);
        yield return null;
        var frozen = ball.transform.position;
        var attempts = match.gameData.stats.GetPlayerStats(attacker).passesAttempted;
        var rolls = match.TimingRollSequence;
        for (var frame = 0; frame < 15; frame++) yield return null;
        Assert.That(Vector3.Distance(ball.transform.position, frozen), Is.LessThan(.0001f));
        Assert.That(match.TimingRollSequence, Is.EqualTo(rolls));
        Assert.That(pause.ConnectionHeld && !protocol.BothConnected, Is.True);
        Assert.That(recovery.BindAuthenticated("home-account", "guest-generation-2", OnlineGroundVersion()), Is.True);
        Assert.That(recovery.AcknowledgeSnapshot("guest-generation-1", 1), Is.False);
        Assert.That(recovery.AcknowledgeSnapshot("guest-generation-2", 2), Is.True);
        var returned = pause.Capture(authority);
        Assert.That(returned.publicationSequence, Is.GreaterThan(heldState.publicationSequence));
        Assert.That(replica.Apply(returned), Is.True);
        Assert.That(protocol.TryGetTeam("guest-generation-1", out _), Is.False);
        if (boundary == "paused")
        {
            Assert.That(pause.IsPaused && Time.timeScale == 0, Is.True);
            Assert.That(pause.Submit("away", SharedControl(protocol, pause, 1, OnlineSharedPause.ResumeAction)).Status,
                Is.EqualTo(OnlineCommandStatus.WrongTeam));
            pause.Submit("guest-generation-2", SharedControl(protocol, pause, 2, OnlineSharedPause.ResumeAction));
        }
        if (boundary == "roll")
        {
            Assert.That(authority.Submit("away", queued).Status, Is.EqualTo(OnlineCommandStatus.StaleRevision));
            OnlineAccepted(authority, "away", 1, OnlineGroundPassAuthority.RollAction);
        }
        else if (boundary != "movement")
            OnlineAccepted(authority, "guest-generation-2", 3, OnlineGroundPassAuthority.TargetAction(0, 6));
        yield return Until(() => !ground.isActivated, "same action continues after authenticated resync");
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesAttempted, Is.EqualTo(boundary == "movement" || boundary == "roll" ? attempts : 1));
        Assert.That(match.TimingRollSequence, Is.EqualTo(boundary == "roll" ? 1 : 0));
    }
}
