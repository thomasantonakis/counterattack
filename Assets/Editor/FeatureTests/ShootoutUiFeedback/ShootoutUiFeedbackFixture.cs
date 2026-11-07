using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private PenaltyShootoutManager OwnUiFeedbackShootout()
    {
        var active = typeof(PenaltyShootoutManager).GetField("<ActiveShootout>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        var prior = active.GetValue(null); var restore = restoreAuthoredPanelState;
        restoreAuthoredPanelState = () => { restore?.Invoke(); active.SetValue(null, prior); };
        var shootout = Add<PenaltyShootoutManager>(); shootout.Configure(match);
        active.SetValue(null, shootout); return shootout;
    }

    public IEnumerator RunShootoutUiStakes(bool scores)
    {
        Board(1); var shootout = OwnUiFeedbackShootout();
        shootout.currentShooter = attacker;
        shootout.homeShootoutScore = scores ? 4 : 0; shootout.awayShootoutScore = scores ? 4 : 3;
        shootout.homeAttempts = scores ? 4 : 2; shootout.awayAttempts = scores ? 5 : 3;
        match.currentState = MatchManager.GameState.PenaltyShootoutKickExecution;
        var shot = match.shotManager; shot.shooter = attacker; shot.shotType = "penalty";
        shot.isActivated = true; shot.isWaitingForShotRoll = true;
        var monitor = Add<GameDebugMonitor>(); monitor.matchManager = match;
        monitor.shotManager = shot; monitor.penaltyShootoutManager = shootout;
        object[] args = { null, false };
        var snapshot = (GameplayInstructionSnapshot)typeof(GameDebugMonitor).GetMethod("BuildCurrentInstructionSnapshot", Private).Invoke(monitor, args);
        Assert.That(snapshot.instructionText, Does.Contain(scores ? "win if" : "lose if"));
        Assert.That(snapshot.instructionText, Does.Contain(attacker.playerName));
        Assert.That(snapshot.instructionText, Does.Contain("Press [R] to roll for the Penalty Kick"), "Keep the active roll prompt beside the stakes");
        shot.isWaitingForShotRoll = false; shot.isWaitingForGKDiceRoll = true;
        Assert.That(shootout.GetCurrentKickStakesInstruction(), Does.Contain(scores ? "scores" : "misses"));
        shootout.currentShooter = null;
        Assert.That(shootout.GetCurrentKickStakesInstruction(), Is.Empty, "Resolved kicks must not leave stale stakes");
        yield return null;
    }

    public IEnumerator RunShootoutUiOffTarget(int side)
    {
        yield return RunShootoutRollPanel(side, "offtarget");
        Assert.That(match.shotManager.totalShotPower, Is.Zero, "Do not calculate shooting power for an off-target natural one");
        var total = (duelPanel.DribblerOnLeft ? duelPanel.leftPlayer : duelPanel.rightPlayer).total.text;
        Assert.That(total, Is.EqualTo("Roll 1"));
        Assert.That(duelPanel.outcome.text, Is.EqualTo("OFF TARGET"));
    }

    public IEnumerator RunShootoutUiPreviews(bool homeShooter)
    {
        Board(1); var views = LoadPresentationViews();
        var playerSource = views.GetComponentInChildren<PlayerCard>(true);
        var keeperSource = views.GetComponentInChildren<GoalkeeperCard>(true);
        var homePlayer = Object.Instantiate(playerSource, views.transform);
        var awayPlayer = Object.Instantiate(playerSource, views.transform);
        var homeKeeper = Object.Instantiate(keeperSource, views.transform);
        var awayKeeper = Object.Instantiate(keeperSource, views.transform);
        var stats = Add<MatchStatsUI>();
        Set(stats, "homeHoverCard", homePlayer); Set(stats, "awayHoverCard", awayPlayer);
        Set(stats, "homeGoalkeeperHoverCard", homeKeeper); Set(stats, "awayGoalkeeperHoverCard", awayKeeper);
        Set(stats, "homeHoverCardAnchor", views.transform as RectTransform);
        Set(stats, "awayHoverCardAnchor", views.transform as RectTransform);
        var shootout = OwnUiFeedbackShootout(); shootout.homeOrder.Add(attacker); shootout.awayOrder.Add(defender);
        Set(shootout, "nextKickIsHome", homeShooter);
        var walk = (IEnumerator)Invoke(shootout, "PrepareNextKick");
        walk.MoveNext(); // Real walk entry must refresh both cards before movement completes.
        Assert.That(homeShooter ? homePlayer.DisplayedToken : awayPlayer.DisplayedToken, Is.SameAs(homeShooter ? attacker : defender));
        Assert.That(homeShooter ? awayKeeper.DisplayedToken : homeKeeper.DisplayedToken, Is.SameAs(homeShooter ? awayGK : homeGK));
        Invoke(stats, "HandleHoverTokenChanged", homeShooter ? homeGK : awayGK, null);
        Assert.That(homeShooter ? homeKeeper.DisplayedToken : awayKeeper.DisplayedToken, Is.SameAs(homeShooter ? homeGK : awayGK));
        shootout.StopAllCoroutines();
        Set(shootout, "nextKickIsHome", !homeShooter);
        var nextWalk = (IEnumerator)Invoke(shootout, "PrepareNextKick"); nextWalk.MoveNext();
        Assert.That(homeShooter ? awayPlayer.DisplayedToken : homePlayer.DisplayedToken, Is.SameAs(homeShooter ? defender : attacker));
        Assert.That(homeShooter ? homeKeeper.DisplayedToken : awayKeeper.DisplayedToken, Is.SameAs(homeShooter ? homeGK : awayGK));
        Assert.That((homeShooter ? awayKeeper : homeKeeper).gameObject.activeSelf, Is.False, "Hide the previous goalkeeper card when that team is shooting");
        yield return null;
    }
}
