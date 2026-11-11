using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private static OnlineCompatibility OnlineGroundVersion() => new("ground-v1", "rules-v1", "fixture-v1");
    private OnlineGroundPassAuthority OnlineGroundBoard(int side, bool hostAway, bool dangerous, out OnlineMatchProtocol protocol, out PlayerToken receiver)
    {
        Board(side);
        Place(attacker, Hex(0)); ball.PlaceAtCell(Hex(0));
        receiver = Token("Online receiver", true, false, 10); Place(receiver, Hex(0, 6));
        var backline = Token("Online backline", false, false, 6); Place(backline, Hex(8 * side, 8));
        Place(defender, dangerous ? Hex(side, 3) : Hex(8 * side, 10));
        Subscribe(ground); Subscribe(match.finalThirdManager);
        match.BroadcastSafeEndofMovementPhase(countMovementAction: false, triggerFinalThird: false);
        protocol = new OnlineMatchProtocol("ground-proof", "home-account", "away-account",
            hostAway ? "away-account" : "home-account", OnlineGroundVersion());
        Assert.That(protocol.BindAuthenticatedAccount("home-account", "home", OnlineGroundVersion()), Is.True);
        Assert.That(protocol.BindAuthenticatedAccount("away-account", "away", OnlineGroundVersion()), Is.True);
        return new OnlineGroundPassAuthority(match, protocol);
    }

    private static OnlineMatchCommand OnlineGroundCommand(OnlineGroundPassState state, long sequence, string action)
        => new(state.matchId, sequence, state.revision, state.decisionId, action);

    private static void OnlineAccepted(OnlineGroundPassAuthority authority, string connection, long sequence, string action)
    {
        var state = authority.Capture();
        Assert.That(state.legalActions, Does.Contain(action), state.stage);
        Assert.That(authority.Submit(connection, OnlineGroundCommand(state, sequence, action)).Status,
            Is.EqualTo(OnlineCommandStatus.Accepted));
    }

    public IEnumerator RunOnlineGroundOutcome(int side, string branch)
    {
        var authority = OnlineGroundBoard(side, true, branch != "safe", out _, out var receiver);
        OnlineAccepted(authority, "home", 1, OnlineGroundPassAuthority.StartAction);
        var preview = OnlineGroundCommand(authority.Capture(), 2, OnlineGroundPassAuthority.TargetAction(0, 6));
        Assert.That(authority.Submit("home", preview).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(ground.currentTargetHex, Is.SameAs(receiver.currentHex));
        Assert.That(ball.GetCurrentHex(), Is.SameAs(Hex(0)), "preview cannot move the ball");
        var confirmation = OnlineGroundCommand(authority.Capture(), 3, OnlineGroundPassAuthority.TargetAction(0, 6));
        Assert.That(authority.Submit("home", confirmation).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(authority.Submit("home", confirmation).Status, Is.EqualTo(OnlineCommandStatus.Duplicate));
        var inFlightPublication = authority.Capture().publicationSequence;
        if (branch != "safe")
        {
            yield return Until(() => ground.isWaitingForDiceRoll, "real pass enters interception");
            var rollState = authority.Capture();
            Assert.That(rollState.owner, Is.EqualTo("Away"), "defender owns the interception even while Home attacks");
            var roll = OnlineGroundCommand(rollState, 1, OnlineGroundPassAuthority.RollAction);
            Assert.That(authority.Submit("home", OnlineGroundCommand(rollState, 4, OnlineGroundPassAuthority.RollAction)).Status,
                Is.EqualTo(OnlineCommandStatus.WrongTeam));
            // Rig only the fixture's next real random draw. No wire action supplies a roll.
            var desired = branch == "intercept" ? 6 : 1;
            var seed = 0;
            for (; seed < 1000; seed++)
            {
                Random.InitState(seed);
                if (helper.DiceRoll()._roll == desired) break;
            }
            Assert.That(seed, Is.LessThan(1000)); Random.InitState(seed);
            var beforeRoll = match.TimingRollSequence;
            Assert.That(authority.Submit("away", roll).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(authority.Submit("away", roll).Status, Is.EqualTo(OnlineCommandStatus.Duplicate));
            Assert.That(match.TimingRollSequence, Is.EqualTo(beforeRoll + 1), "retries cannot draw again");
        }
        yield return Until(() => !ground.isActivated && !ball.isMoving && !ball.IsCellMoveInProgress,
            "ground outcome and continuation settle");
        var final = authority.Capture();
        Assert.That(final.publicationSequence, Is.GreaterThan(inFlightPublication), "asynchronous outcomes publish a fresh sequence");
        Assert.That(authority.Capture().publicationSequence, Is.EqualTo(final.publicationSequence), "unchanged state does not advance publications");
        Assert.That(final.rollSequence, Is.EqualTo(branch == "safe" ? 0 : 1));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesAttempted, Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesCompleted, Is.EqualTo(branch == "intercept" ? 0 : 1));
        if (branch == "intercept")
        {
            Assert.That(ball.GetCurrentHex(), Is.SameAs(defender.currentHex));
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(defender));
            Assert.That(match.gameData.stats.GetPlayerStats(defender).interceptionsMade, Is.EqualTo(1));
            Assert.That(final.groundPassAvailable && final.movementAvailable, Is.True);
        }
        else
        {
            Assert.That(ball.GetCurrentHex(), Is.SameAs(receiver.currentHex));
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Home));
            Assert.That(final.movementAvailable && final.firstTimePassAvailable, Is.True);
            Assert.That(match.gameData.stats.GetPlayerStats(defender).interceptionsMade, Is.EqualTo(0));
        }
        Assert.That(final.attackHasPossession, Is.True);
        var detached = JsonUtility.FromJson<OnlineGroundPassState>(JsonUtility.ToJson(final));
        Assert.That(detached.ballX, Is.EqualTo(ball.GetCurrentHex().coordinates.x));
        Assert.That(detached.ballZ, Is.EqualTo(ball.GetCurrentHex().coordinates.z));
        Assert.That(detached.tokens.Single(t => t.id == "home:9").passesAttempted, Is.EqualTo(1));
        detached.tokens[0].x = 999;
        Assert.That(authority.Capture().tokens[0].x, Is.Not.EqualTo(999), "snapshots never alias live state");
    }

    public IEnumerator RunOnlineGroundRejections(int side, bool hostAway)
    {
        var authority = OnlineGroundBoard(side, hostAway, false, out _, out _);
        var originalStatsCount = match.gameData.stats.playerStats.Count;
        var initial = authority.Capture();
        Assert.That(match.gameData.stats.playerStats.Count, Is.EqualTo(originalStatsCount), "capture must not create stats");
        Assert.That(authority.Submit("away", OnlineGroundCommand(initial, 1, OnlineGroundPassAuthority.StartAction)).Status, Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(authority.Submit("unbound", OnlineGroundCommand(initial, 1, OnlineGroundPassAuthority.StartAction)).Status, Is.EqualTo(OnlineCommandStatus.UnknownConnection));
        Assert.That(ground.isActivated, Is.False);
        OnlineAccepted(authority, "home", 1, OnlineGroundPassAuthority.StartAction);
        var choosing = authority.Capture();
        Assert.That(authority.Submit("home", OnlineGroundCommand(choosing, 2, "ground.target:999:999")).Status, Is.EqualTo(OnlineCommandStatus.IllegalAction));
        Assert.That(ground.currentTargetHex, Is.Null);
        var stale = OnlineGroundCommand(choosing, 3, OnlineGroundPassAuthority.TargetAction(0, 6));
        OnlineAccepted(authority, "home", 2, OnlineGroundPassAuthority.TargetAction(0, 4));
        Assert.That(authority.Submit("home", stale).Status, Is.EqualTo(OnlineCommandStatus.StaleRevision));
        Assert.That(ground.currentTargetHex, Is.SameAs(Hex(0, 4)));
        // Live occupancy changes invalidate a once-legal confirmation before admission.
        var confirm = OnlineGroundCommand(authority.Capture(), 3, OnlineGroundPassAuthority.TargetAction(0, 4));
        Place(defender, Hex(0, 4));
        Assert.That(authority.Submit("home", confirm).Status, Is.EqualTo(OnlineCommandStatus.StaleRevision));
        Assert.That(ball.GetCurrentHex(), Is.SameAs(Hex(0)));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesAttempted, Is.EqualTo(0));
        ground.CleanUpPass();
        var stopped = authority.Capture();
        Assert.That(stopped.legalActions, Is.Empty, "cleanup revokes the target boundary");
        yield break;
    }

    public IEnumerator RunOnlineGroundReconnect(int side)
    {
        var authority = OnlineGroundBoard(side, false, false, out var protocol, out _);
        OnlineAccepted(authority, "home", 1, OnlineGroundPassAuthority.StartAction);
        var preview = OnlineGroundCommand(authority.Capture(), 2, OnlineGroundPassAuthority.TargetAction(0, 6));
        Assert.That(authority.Submit("home", preview).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        var pending = authority.Capture();
        protocol.Disconnect("home");
        Assert.That(authority.Submit("away", OnlineGroundCommand(pending, 1, OnlineGroundPassAuthority.TargetAction(0, 6))).Status, Is.EqualTo(OnlineCommandStatus.Disconnected));
        Assert.That(protocol.BindAuthenticatedAccount("home-account", "home-reconnected", OnlineGroundVersion()), Is.True);
        Assert.That(protocol.TryGetTeam("home-reconnected", out var team) && team == OnlineTeam.Home, Is.True);
        Assert.That(authority.Submit("home", preview).Status, Is.EqualTo(OnlineCommandStatus.UnknownConnection));
        Assert.That(authority.Submit("home-reconnected", preview).Status, Is.EqualTo(OnlineCommandStatus.Duplicate));
        Assert.That(authority.Capture().decisionId, Is.EqualTo(pending.decisionId));
        Assert.That(ground.currentTargetHex, Is.SameAs(Hex(0, 6)));
        OnlineAccepted(authority, "home-reconnected", 3, OnlineGroundPassAuthority.TargetAction(0, 6));
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == Hex(0, 6), "reconnected confirmation resolves once");
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesAttempted, Is.EqualTo(1));
    }

    public IEnumerator RunOnlineGroundPause(int side)
    {
        var authority = OnlineGroundBoard(side, false, false, out _, out _);
        OnlineAccepted(authority, "home", 1, OnlineGroundPassAuthority.StartAction);
        OnlineAccepted(authority, "home", 2, OnlineGroundPassAuthority.TargetAction(0, 6));
        var preview = authority.Capture();
        var confirm = OnlineGroundCommand(preview, 3, OnlineGroundPassAuthority.TargetAction(0, 6));
        match.SetPauseMenuOpen(true);
        var paused = authority.Capture();
        Assert.That(paused.stage, Is.EqualTo("blocked"));
        Assert.That(paused.legalActions, Is.Empty);
        Assert.That(authority.Submit("home", confirm).Status, Is.EqualTo(OnlineCommandStatus.StaleRevision));
        Assert.That(ball.GetCurrentHex(), Is.SameAs(Hex(0)));
        match.SetPauseMenuOpen(false);
        var resumed = authority.Capture();
        Assert.That(resumed.decisionId, Is.Not.EqualTo(preview.decisionId));
        Assert.That(ground.currentTargetHex, Is.SameAs(Hex(0, 6)), "pause retains the preview");
        Assert.That(authority.Submit("home", confirm).Status, Is.EqualTo(OnlineCommandStatus.StaleRevision));
        OnlineAccepted(authority, "home", 3, OnlineGroundPassAuthority.TargetAction(0, 6));
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == Hex(0, 6), "fresh confirmation resumes after pause");
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesAttempted, Is.EqualTo(1));
    }
}
