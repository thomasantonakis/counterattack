using System.Collections;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private static OnlineMatchCommand SharedControl(OnlineMatchProtocol protocol, OnlineSharedPause pause,
        long sequence, string action, long? revision = null)
        => new(protocol.MatchId, sequence, revision ?? pause.Revision, "shared-pause", action);

    public IEnumerator RunSharedPauseOwnership(int side, bool requesterHome, bool hostAway)
    {
        var authority = OnlineGroundBoard(side, hostAway, false, out var protocol, out _);
        using var pause = new OnlineSharedPause(protocol, match);
        OnlineAccepted(authority, "home", 1, OnlineGroundPassAuthority.StartAction);
        OnlineAccepted(authority, "home", 2, OnlineGroundPassAuthority.TargetAction(0, 6));
        var before = authority.Capture();
        var oldConfirm = OnlineGroundCommand(before, 3, OnlineGroundPassAuthority.TargetAction(0, 6));
        var requester = requesterHome ? "home" : "away";
        var opponent = requesterHome ? "away" : "home";
        var initialScale = Time.timeScale = .75f;
        var request = SharedControl(protocol, pause, 1, OnlineSharedPause.RequestAction);
        Assert.That(pause.Submit(requester, request).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(pause.Submit(requester, request).Status, Is.EqualTo(OnlineCommandStatus.Duplicate));
        var paused = pause.Capture(authority);
        Assert.That(paused.isSharedPaused && match.IsPauseMenuOpen, Is.True);
        Assert.That(paused.pauseRequester, Is.EqualTo(requesterHome ? "Home" : "Away"));
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(pause.Submit(opponent, SharedControl(protocol, pause, 1, OnlineSharedPause.ResumeAction)).Status, Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(pause.Submit(opponent, SharedControl(protocol, pause, 1, OnlineSharedPause.RequestAction, 0)).Status, Is.EqualTo(OnlineCommandStatus.StaleRevision),
            "simultaneous second request cannot steal ownership");
        Assert.That(authority.Submit("home", oldConfirm).Status, Is.EqualTo(OnlineCommandStatus.StaleRevision));
        Assert.That(ground.currentTargetHex, Is.SameAs(Hex(0, 6)));
        Assert.That(ball.GetCurrentHex(), Is.SameAs(Hex(0)));

        protocol.Disconnect(requester);
        Assert.That(pause.Submit(opponent, SharedControl(protocol, pause, 2, OnlineSharedPause.ResumeAction)).Status, Is.EqualTo(OnlineCommandStatus.Disconnected));
        Assert.That(pause.IsPaused && Time.timeScale == 0, Is.True, "disconnect never transfers pause ownership");
        var account = requesterHome ? "home-account" : "away-account";
        Assert.That(protocol.BindAuthenticatedAccount(account, "requester-returned", OnlineGroundVersion()), Is.True);
        Assert.That(pause.Submit(requester, request).Status, Is.EqualTo(OnlineCommandStatus.UnknownConnection));
        Assert.That(pause.Submit("requester-returned", request).Status, Is.EqualTo(OnlineCommandStatus.Duplicate));
        var resume = SharedControl(protocol, pause, 2, OnlineSharedPause.ResumeAction);
        Assert.That(pause.Submit("requester-returned", resume).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(Time.timeScale, Is.EqualTo(initialScale));
        Assert.That(pause.Submit("requester-returned", request).Status, Is.EqualTo(OnlineCommandStatus.Duplicate), "old pause retry must not pause again");
        Assert.That(pause.IsPaused || match.IsPauseMenuOpen, Is.False);
        Assert.That(authority.Submit(requesterHome ? "requester-returned" : "home", oldConfirm).Status, Is.EqualTo(OnlineCommandStatus.StaleRevision));
        OnlineAccepted(authority, requesterHome ? "requester-returned" : "home", 3, OnlineGroundPassAuthority.TargetAction(0, 6));
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == Hex(0, 6), "requester resumes the preserved preview");
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesAttempted, Is.EqualTo(1));
        Assert.That(pause.Capture(authority).sharedPauseRevision, Is.EqualTo(2));
    }

    public IEnumerator RunSharedPauseMovingBall(int side)
    {
        var authority = OnlineGroundBoard(side, true, false, out var protocol, out _);
        using var pause = new OnlineSharedPause(protocol, match);
        OnlineAccepted(authority, "home", 1, OnlineGroundPassAuthority.StartAction);
        OnlineAccepted(authority, "home", 2, OnlineGroundPassAuthority.TargetAction(0, 6));
        OnlineAccepted(authority, "home", 3, OnlineGroundPassAuthority.TargetAction(0, 6));
        yield return Until(() => ball.IsCellMoveInProgress, "real ball movement begins before pause");
        Assert.That(pause.Submit("away", SharedControl(protocol, pause, 1, OnlineSharedPause.RequestAction)).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        yield return null;
        var frozen = ball.transform.position;
        var started = Time.realtimeSinceStartupAsDouble;
        for (var frame = 0; frame < 15; frame++) yield return null;
        Assert.That(Vector3.Distance(ball.transform.position, frozen), Is.LessThan(.0001f), "scaled movement cannot progress during shared pause");
        Assert.That(Time.realtimeSinceStartupAsDouble, Is.GreaterThan(started), "unscaled networking clock remains live");
        Assert.That(ground.isActivated && ball.IsCellMoveInProgress, Is.True);
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesCompleted, Is.Zero);
        Assert.That(pause.Submit("home", SharedControl(protocol, pause, 1, OnlineSharedPause.ResumeAction)).Status, Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(pause.Submit("away", SharedControl(protocol, pause, 2, OnlineSharedPause.ResumeAction)).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == Hex(0, 6), "same in-flight pass resumes without replay");
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesAttempted, Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesCompleted, Is.EqualTo(1));
    }

    public IEnumerator RunSharedPausePendingRoll(int side)
    {
        var authority = OnlineGroundBoard(side, true, true, out var protocol, out _);
        using var pause = new OnlineSharedPause(protocol, match);
        OnlineAccepted(authority, "home", 1, OnlineGroundPassAuthority.StartAction);
        OnlineAccepted(authority, "home", 2, OnlineGroundPassAuthority.TargetAction(0, 6));
        OnlineAccepted(authority, "home", 3, OnlineGroundPassAuthority.TargetAction(0, 6));
        yield return Until(() => ground.isWaitingForDiceRoll, "interception pending before shared pause");
        var queued = OnlineGroundCommand(authority.Capture(), 1, OnlineGroundPassAuthority.RollAction);
        Assert.That(pause.Submit("home", SharedControl(protocol, pause, 1, OnlineSharedPause.RequestAction)).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(authority.Submit("away", queued).Status, Is.EqualTo(OnlineCommandStatus.StaleRevision));
        for (var frame = 0; frame < 5; frame++) yield return null;
        Assert.That(match.TimingRollSequence, Is.Zero);
        Assert.That(ground.isWaitingForDiceRoll, Is.True);
        Assert.That(pause.Submit("home", SharedControl(protocol, pause, 2, OnlineSharedPause.ResumeAction)).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(authority.Submit("away", queued).Status, Is.EqualTo(OnlineCommandStatus.StaleRevision));
        OnlineAccepted(authority, "away", 1, OnlineGroundPassAuthority.RollAction);
        yield return Until(() => !ground.isActivated, "resumed interception resolves once");
        Assert.That(match.TimingRollSequence, Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(defender).interceptionsAttempted, Is.EqualTo(1));
    }
}
