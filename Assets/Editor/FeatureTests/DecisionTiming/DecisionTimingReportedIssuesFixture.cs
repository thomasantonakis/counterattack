using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunFinalThirdMotionTiming(int side, bool expired, bool defense)
    {
        Board(side); match.isClockRunning = true;
        Place(attacker, Hex(14 * side)); Place(defender, Hex(13 * side, 4));
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker);
        var mover = Token("F3 mover", !defense, false, 6); Place(mover, Hex(-14 * side));
        var f3 = match.finalThirdManager; Subscribe(f3); f3.TriggerFinalThirdPhase();
        yield return Until(() => (bool)typeof(FinalThirdManager).GetField("isWaitingForTokenSelection", Private).GetValue(f3), "F3 attack input");
        if (defense)
        {
            LegacyKey("Return");
            yield return Until(() => (string)typeof(FinalThirdManager).GetField("currentTeamMoving", Private).GetValue(f3) == "defense"
                && (bool)typeof(FinalThirdManager).GetField("isWaitingForTokenSelection", Private).GetValue(f3), "F3 defense input");
        }
        match.RefreshDecisionTiming(); var decision = match.DecisionTiming.Current;
        Assert.That(decision.kind, Is.EqualTo(defense ? DecisionKind.FinalThirdDefensiveMovement : DecisionKind.FinalThirdAttackingMovement));
        Assert.That(decision.owner, Is.EqualTo(defense ? "Away" : "Home"));
        match.AdvanceDecisionTiming(expired ? decision.remaining : 2);
        input.TryExecuteSyntheticHexClick(mover.currentHex, "F3 token", out _);
        var target = grid.highlightedHexes.First(h => h != mover.currentHex && h.GetOccupyingToken() == null && !h.isOutOfBounds);
        input.TryExecuteSyntheticHexClick(target, "F3 target", out _);
        yield return Until(() => (bool)typeof(FinalThirdManager).GetField("isMovingToken", Private).GetValue(f3), "real F3 animation");
        float before = decision.remaining;
        match.AdvanceDecisionTiming(2);
        Assert.That(match.DecisionTiming.Current, Is.Not.Null);
        Assert.That(match.DecisionTiming.Current.id, Is.EqualTo(decision.id));
        Assert.That(decision.remaining, Is.EqualTo(Mathf.Max(0, before - 2)));
        AssertPaceTimingDisplay(match.DecisionTiming, defense ? "Final Third Defensive Movement" : "Final Third Attacking Movement");
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.False);
        yield return Until(() => !(bool)typeof(FinalThirdManager).GetField("isMovingToken", Private).GetValue(f3), "F3 movement completes");
    }

    public IEnumerator RunTacklePendingMovementPanel(int side, bool accept)
    {
        Board(side); Place(attacker, Hex(0)); Place(defender, Hex(2 * side));
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker);
        Subscribe(movement); movement.ActivateMovementPhase(); movement.CommitToAction(); movement.ForfeitTeamMovementPhase();
        input.TryExecuteSyntheticHexClick(defender.currentHex, "Tackle mover", out _);
        input.TryExecuteSyntheticHexClick(Hex(side), "Tackle move", out _);
        yield return Until(() => movement.isWaitingForTackleDecision && !movement.isPlayerMoving, "T/N owns the same mover");
        var root = new GameObject("Pending tackle movement panel", typeof(RectTransform), typeof(CanvasGroup)); root.SetActive(false);
        try
        {
            var panel = root.AddComponent<MovementPhaseMovedTokensPanel>();
            var slots = Enumerable.Range(0, 5).Select(i => new GameObject("Slot " + i).AddComponent<MovementPhaseMovedTokenSlotView>()).ToArray();
            foreach (var slot in slots) slot.transform.SetParent(root.transform);
            typeof(MovementPhaseMovedTokensPanel).GetField("movementPhaseManager", Private).SetValue(panel, movement);
            typeof(MovementPhaseMovedTokensPanel).GetField("canvasGroup", Private).SetValue(panel, root.GetComponent<CanvasGroup>());
            typeof(MovementPhaseMovedTokensPanel).GetField("defensiveMovementSlots", Private).SetValue(panel, slots);
            Invoke(panel, "Refresh", true);
            Assert.That(slots.Count(s => s.State == MovementPhaseMovedTokenSlotView.PresentationState.Completed), Is.EqualTo(1));
            Assert.That(slots.Any(s => s.State == MovementPhaseMovedTokenSlotView.PresentationState.Active), Is.False, "T/N has not offered the next mover");
            if (accept)
            {
                LegacyKey("T");
                yield return Until(() => movement.isWaitingForTackleRoll, "accepted tackle roll");
                Invoke(panel, "Refresh", false);
                Assert.That(slots.Any(s => s.State == MovementPhaseMovedTokenSlotView.PresentationState.Active), Is.False, "roll still belongs to the same mover");
                yield break;
            }
            LegacyKey("N");
            yield return Until(() => !movement.isWaitingForTackleDecision && movement.isAwaitingTokenSelection, "declined tackle releases next mover");
            Invoke(panel, "Refresh", false);
            Assert.That(slots.Any(s => s.State == MovementPhaseMovedTokenSlotView.PresentationState.Active), Is.True);
        }
        finally { Object.DestroyImmediate(root); }
    }

    public IEnumerator RunImmediatePenaltyKeeper(int side, bool shootoutMode)
    {
        Board(side); Invoke(grid, "LoadPrecomputedShootingPaths"); AttachAuthoredPanels();
        var shot = match.shotManager; EnableForAuthoredPresentation(shot);
        var active = typeof(PenaltyShootoutManager).GetField("<ActiveShootout>k__BackingField", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var prior = active.GetValue(null); var cleanup = restoreAuthoredPanelState;
        restoreAuthoredPanelState = () => { cleanup(); active.SetValue(null, prior); };
        if (shootoutMode)
        {
            var shootout = Add<PenaltyShootoutManager>(); shootout.matchManager = match;
            shootout.currentShooter = attacker; shootout.currentDefendingGoalkeeper = awayGK; active.SetValue(null, shootout);
        }
        else active.SetValue(null, null);
        Place(attacker, Hex(14 * side)); Place(awayGK, Hex(18 * side));
        ball.PlaceAtCell(attacker.currentHex); attacker.shooting = 5;
        shot.StartPenaltyShotProcess(attacker, attacker.currentHex);
        if (!shootoutMode) input.TryExecuteSyntheticHexClick(Hex(19 * side), "Penalty target", out _);
        yield return Until(() => shot.isWaitingForShotRoll && duelPanel.IsShotComparisonFor(shot), "penalty shooter roll");
        shot.StartCoroutine(shot.StartShotRoll(4));
        float until = Time.unscaledTime + .5f;
        while (!shot.isWaitingForGKDiceRoll && Time.unscaledTime < until) yield return null;
        Assert.That(shot.isWaitingForGKDiceRoll, Is.True, "keeper should be available without a five-second shooter review");
        Assert.That(duelPanel.IsAwaitingRoll, Is.True);
        match.RefreshDecisionTiming(); Assert.That(match.DecisionTiming.HasPendingRoll, Is.True);
    }

    public IEnumerator RunSnapshotMovementAccounting(int side, bool exhausted, bool shoot)
    {
        Board(side); match.isClockRunning = true;
        Place(attacker, Hex(13 * side)); Place(defender, Hex(-10 * side, 8));
        Place(awayGK, Hex(16 * side, 1)); attacker.dribbling = 6; attacker.pace = exhausted ? 1 : 3;
        attacker.shooting = 3; awayGK.saving = 1;
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker);
        Invoke(grid, "LoadPrecomputedShootingPaths");
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        EnableForAuthoredPresentation(match.shotManager);
        movement.ActivateMovementPhase(); movement.CommitToAction();
        input.TryExecuteSyntheticHexClick(attacker.currentHex, "Snapshot accounting", out _);
        input.TryExecuteSyntheticHexClick(Hex(14 * side, 1), "Snapshot accounting", out _);
        yield return Until(() => movement.isWaitingForSnapshotDecision && !movement.isPlayerMoving, "actual box dribble offers snapshot");
        Assert.That(movement.isDribblerRunning, Is.EqualTo(!exhausted));
        LegacyKey(shoot ? "S" : "X");
        yield return null;
        if (!shoot)
        {
            yield return Until(() => movement.isAwaitingTokenSelection, "decline resumes token selection");
            Assert.That(movement.attackersMoved, Is.EqualTo(1));
            yield break;
        }
        var shot = match.shotManager;
        Assert.That(shot.isWaitingforBlockerSelection, Is.True);
        Assert.That(movement.attackersMoved, Is.Zero, "shot owns the pending mover until its outcome resolves");
        input.TryExecuteSyntheticHexClick(defender.currentHex, "Snapshot blocker", out _);
        var destination = shot.GetValidSnapshotBlockerDestinationsForDecisionPreview(defender).First();
        input.TryExecuteSyntheticHexClick(destination, "Snapshot blocker move", out _);
        yield return Until(() => shot.isWaitingForTargetSelection, "shot target selection");
        input.TryExecuteSyntheticHexClick(Hex(19 * side, 1), "Snapshot target", out _);
        yield return Until(() => keeper.isActivated || shot.isWaitingForShotRoll, "shot rolling ready");
        if (keeper.isActivated) LegacyKey("X");
        yield return Until(() => shot.isWaitingForShotRoll, "shooter roll");
        shot.StartCoroutine(shot.StartShotRoll(2));
        yield return Until(() => shot.isWaitingForGKDiceRoll, "keeper save roll");
        var interaction = CurrentLegacyShotInteraction();
        int penalty = (int?)interaction.GetType().GetField("gkPenalty").GetValue(interaction) ?? 0;
        int saveRoll = shot.totalShotPower - awayGK.saving - penalty;
        Assert.That(saveRoll, Is.InRange(1, 6));
        shot.StartCoroutine(InvokeLegacyShotRoll("ResolveGKSavingAttempt", saveRoll, interaction));
        yield return Until(() => loose.isWaitingForDirectionRoll, "tied save scatters");
        var origin = awayGK.currentHex;
        int direction = Enumerable.Range(0, 6).First(d =>
        {
            var target = loose.CalculateDirectionalTarget(origin, d, 2);
            return target != null && !target.isOutOfBounds && target.isInGoal == 0
                && target.GetOccupyingToken() == null && !target.GetNeighbors(grid).Any(h => h?.GetOccupyingToken() != null);
        });
        loose.PerformDirectionRoll(direction + 1);
        yield return Until(() => loose.isWaitingForDistanceRoll, "scatter distance");
        loose.PerformDistanceRoll(2);
        yield return Until(() => !loose.isActivated && !shot.isActivated, "uncollected scatter resumes MP");
        Assert.That(movement.attackersMoved, Is.EqualTo(1), "one snapshot mover consumes exactly one slot");
        Assert.That(movement.MovedTokenEntries.Count(e => e.section == MovementPhaseMovedTokenSection.AttMP), Is.EqualTo(1));
        Assert.That(movement.isMovementPhaseAttack && movement.isAwaitingTokenSelection, Is.True);
    }
}
