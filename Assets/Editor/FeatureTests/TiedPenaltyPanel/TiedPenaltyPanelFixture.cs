using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunTiedPenaltyPanel(int side, bool shootoutMode)
    {
        Board(side); Invoke(grid, "LoadPrecomputedShootingPaths"); AttachAuthoredPanels();
        var shot = match.shotManager; EnableForAuthoredPresentation(shot); EnableForAuthoredPresentation(loose);
        var activeField = typeof(PenaltyShootoutManager).GetField("<ActiveShootout>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        var prior = activeField.GetValue(null); var restorePanels = restoreAuthoredPanelState;
        restoreAuthoredPanelState = () => { restorePanels(); activeField.SetValue(null, prior); };
        if (shootoutMode)
        {
            var shootout = Add<PenaltyShootoutManager>(); shootout.matchManager = match;
            shootout.currentShooter = attacker; shootout.currentDefendingGoalkeeper = awayGK;
            activeField.SetValue(null, shootout);
        }
        else activeField.SetValue(null, null);
        Place(attacker, Hex(14 * side)); Place(awayGK, Hex(18 * side));
        ball.PlaceAtCell(attacker.currentHex); attacker.shooting = 5; awayGK.saving = 8;
        shot.StartPenaltyShotProcess(attacker, attacker.currentHex);
        if (!shootoutMode) input.TryExecuteSyntheticHexClick(Hex(19 * side), "Tied penalty feature", out _);
        yield return Until(() => shot.isWaitingForShotRoll && duelPanel.IsShotComparisonFor(shot), "penalty comparison ready");
        shot.StartCoroutine(shot.StartShotRoll(4));
        yield return null;
        if (duelPanel.IsComplete) duelPanel.Dismiss(); // Normal penalties retain their shooter result countdown.
        yield return Until(() => shot.isWaitingForGKDiceRoll, "keeper roll ready");
        var save = typeof(ShotManager).GetField("currentShotInteraction", Private).GetValue(shot);
        int modifier = LegacyShotField<int?>(save, "gkPenalty") ?? 0;
        int tieRoll = shot.totalShotPower - awayGK.saving - modifier;
        Assert.That(tieRoll, Is.InRange(1, 6));
        shot.StartCoroutine((IEnumerator)typeof(ShotManager).GetMethods(Private).Single(m => m.Name == "ResolveGKSavingAttempt"
            && m.GetParameters()[1].ParameterType == typeof(RollInputOverride?)).Invoke(shot,
            new object[] { save, (RollInputOverride?)new RollInputOverride { hasOverride = true, roll = tieRoll } }));
        yield return Until(() => loose.isWaitingForDirectionRoll, "tied penalty continues to loose-ball direction");
        Assert.That(duelPanel.scatterContinuationContent.activeSelf, Is.True, "Expand the existing panel");
        Assert.That(duelPanel.playerColumns.activeSelf, Is.True, "Keep shot and save columns visible");
        Assert.That(duelPanel.continuationDirection.die.IsAnimating, Is.True);
        Assert.That(duelPanel.IsAwaitingRoll, Is.True);
        string shotTotal = (duelPanel.DribblerOnLeft ? duelPanel.leftPlayer : duelPanel.rightPlayer).total.text;
        Assert.That(shotTotal, Does.Contain("Total: 9"));
        loose.PerformDirectionRoll(shootoutMode ? 1 : side == 1 ? 2 : 5);
        if (shootoutMode)
        {
            yield return Until(() => duelPanel.IsComplete, "fixed shootout distance shown with result countdown");
            Assert.That(duelPanel.continuationDistance.result.text, Does.Contain("Fixed distance: 2 hexes"));
            Assert.That(loose.isWaitingForDistanceRoll, Is.False, "Shootouts must not invent a distance roll");
            Assert.That(DuelResolutionPanelController.IsHoldingContinuation, Is.False);
        }
        else
        {
            yield return Until(() => loose.isWaitingForDistanceRoll, "normal penalty distance die");
            Assert.That(duelPanel.continuationDistance.die.IsAnimating, Is.True);
            loose.PerformDistanceRoll(2);
            yield return Until(() => !loose.isWaitingForDistanceRoll, "normal penalty distance consumed");
            Assert.That(duelPanel.continuationDistance.result.text, Does.Contain("2 hexes"));
        }
        Assert.That((duelPanel.DribblerOnLeft ? duelPanel.leftPlayer : duelPanel.rightPlayer).total.text, Is.EqualTo(shotTotal));
    }
}
