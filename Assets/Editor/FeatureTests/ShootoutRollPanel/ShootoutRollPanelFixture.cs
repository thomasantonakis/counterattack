using System;
using System.Collections;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private bool timingShootoutAudit;
    public IEnumerator RunTimingShootoutReview(int side, string branch)
    {
        timingShootoutAudit = true;
        yield return RunShootoutRollPanel(side, branch);
    }
    public IEnumerator RunShootoutRollPanel(int side, string branch)
    {
        Board(side); Invoke(grid, "LoadPrecomputedShootingPaths"); AttachAuthoredPanels();
        var shot = match.shotManager;
        EnableForAuthoredPresentation(shot);
        var activeField = typeof(PenaltyShootoutManager).GetField("<ActiveShootout>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        var prior = activeField.GetValue(null);
        var restorePanels = restoreAuthoredPanelState;
        restoreAuthoredPanelState = () => { restorePanels(); activeField.SetValue(null, prior); };
        var shootout = Add<PenaltyShootoutManager>(); shootout.matchManager = match;
        shootout.currentShooter = attacker; shootout.currentDefendingGoalkeeper = awayGK;
        activeField.SetValue(null, shootout);
        Place(attacker, Hex(14 * side)); Place(awayGK, Hex(18 * side));
        ball.PlaceAtCell(attacker.currentHex); attacker.shooting = 5;
        shot.StartPenaltyShotProcess(attacker, attacker.currentHex);
        yield return Until(() => shot.isWaitingForShotRoll && duelPanel.IsShotComparisonFor(shot), "shootout shooter panel ready");
        string timingShooterId = null;
        if (timingShootoutAudit)
        {
            match.AdvanceDecisionTiming(2); timingShooterId = match.DecisionTiming.RollId;
            Assert.That(match.DecisionTiming.HasPendingRoll, Is.True);
            Assert.That(match.DecisionTiming.RollRemaining, Is.EqualTo(3));
        }
        duelPanel.MovePanel(new Vector2(0, 180));
        var position = duelPanel.Panel.anchoredPosition;
        float started = Time.unscaledTime;
        shot.StartCoroutine(shot.StartShotRoll(new RollInputOverride { hasOverride = true, roll = branch == "offtarget" ? 1 : 4 }));
        if (branch == "offtarget")
        {
            yield return null;
            Assert.That(duelPanel.IsComplete, Is.True, "Off-target result countdown starts immediately");
            Assert.That(DuelResolutionPanelController.IsHoldingContinuation, Is.False);
            Assert.That(duelPanel.IsOpen, Is.True);
            if (timingShootoutAudit)
            {
                match.RefreshDecisionTiming();
                Assert.That(match.DecisionTiming.BlockingResult || match.DecisionTiming.HasPendingRoll, Is.False);
            }
            yield break;
        }
        while (!shot.isWaitingForGKDiceRoll && Time.unscaledTime - started < 1f) yield return null;
        Assert.That(shot.isWaitingForGKDiceRoll, Is.True, "Keeper roll must be offered before the five-second result countdown");
        Assert.That(duelPanel.IsAwaitingRoll, Is.True);
        Assert.That(duelPanel.Panel.anchoredPosition, Is.EqualTo(position));
        if (timingShootoutAudit)
        {
            match.AdvanceDecisionTiming(2);
            Assert.That(match.DecisionTiming.RollId, Is.Not.EqualTo(timingShooterId));
            Assert.That(match.DecisionTiming.RollRemaining, Is.EqualTo(3));
        }
        if (branch == "keeper") yield break;
        var save = typeof(ShotManager).GetField("currentShotInteraction", Private).GetValue(shot);
        var originalPosition = ball.transform.position;
        shot.StartCoroutine((IEnumerator)typeof(ShotManager).GetMethods(Private).Single(m => m.Name == "ResolveGKSavingAttempt"
            && m.GetParameters()[1].ParameterType == typeof(RollInputOverride?)).Invoke(shot,
            new object[] { save, (RollInputOverride?)new RollInputOverride { hasOverride = true, roll = 1 } }));
        yield return null;
        Assert.That(duelPanel.IsComplete && duelPanel.IsOpen, Is.True);
        Assert.That(DuelResolutionPanelController.IsHoldingContinuation, Is.False);
        if (timingShootoutAudit)
        {
            match.RefreshDecisionTiming();
            Assert.That(match.DecisionTiming.BlockingResult || match.DecisionTiming.HasPendingRoll, Is.False);
        }
        float movementStarted = Time.unscaledTime;
        while (ball.transform.position == originalPosition && Time.unscaledTime - movementStarted < 1f) yield return null;
        Assert.That(ball.transform.position, Is.Not.EqualTo(originalPosition), "Shot animation runs during the result countdown");
        shot.StopAllCoroutines();
        Invoke(shot, "ResetShotProcess");
        Assert.That(duelPanel.IsComplete && duelPanel.IsOpen, Is.True, "Normal shot reset preserves the result countdown");
        Assert.That(duelPanel.BeginShotComparison(shot, () => true, "Penalty Kick", attacker, awayGK, "Shooting 5", "Saving 4"), Is.True);
        Assert.That(duelPanel.Panel.anchoredPosition, Is.EqualTo(position), "Next kick keeps the dragged position");
        shot.enabled = false;
        Assert.That(duelPanel.IsOpen, Is.False, "Disabling the owner cancels the panel");
    }
}
