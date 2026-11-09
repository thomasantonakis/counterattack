using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunThrowSelectionTimingAudit(int difficulty, bool head)
    {
        Board(1); match.difficulty_level = difficulty;
        Place(attacker, Hex(0, 12)); Place(defender, Hex(0, -10)); ball.PlaceAtCell(attacker.currentHex);
        var receiver = Token("Throw receiver", true, false, 7); Place(receiver, Hex(3, 10));
        var second = Token("Second receiver", true, false, 6); Place(second, Hex(-3, 10));
        var throwIn = CreateLegacyThrowInManager();
        yield return BeginLegacyThrowInExecution(throwIn);
        Assert.That(throwIn.isWaitingForThrowTypeSelection, Is.True);
        match.AdvanceDecisionTiming(3);
        Assert.That(match.DecisionTiming.GameClockStopped, Is.True);
        Assert.That(match.DecisionTiming.Current, Is.Null, "throw type choice belongs to the built-in stopped state");
        LegacyKey(head ? "C" : "P"); match.RefreshDecisionTiming();
        if (head || difficulty < 3)
        {
            Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.WaitingForThrowInTaker));
            Assert.That(match.DecisionTiming.Current, Is.Null);
            Assert.That(match.DecisionTiming.GameClockStopped, Is.True);
        }
        else
        {
            Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.StandardPass));
            Assert.That(match.DecisionTiming.Current.kind, Is.EqualTo(DecisionKind.StandardPassTarget));
            float before = match.DecisionTiming.Current.remaining; match.AdvanceDecisionTiming(2);
            Assert.That(match.DecisionTiming.Current.remaining, Is.EqualTo(before - 2));
        }
    }

    public IEnumerator RunPostHeaderMovementTimingAudit(int difficulty, bool possession)
    {
        Board(1); match.isClockRunning = true; match.difficulty_level = difficulty;
        if (!possession) { ball.PlaceAtCell(Hex(0)); match.UpdatePossessionAfterPass(Hex(0)); }
        Subscribe(movement);
        match.BroadcastHeaderCompleted(); match.RefreshDecisionTiming();
        if (possession)
        {
            Assert.That(match.DecisionTiming.Current.kind, Is.EqualTo(DecisionKind.SelectNextAction));
            LegacyKey("M"); match.RefreshDecisionTiming();
            if (difficulty < 3)
            {
                Assert.That(movement.isCommitted, Is.False);
                Assert.That(match.DecisionTiming.Current.kind, Is.EqualTo(DecisionKind.SelectNextAction));
                match.AdvanceDecisionTiming(3); movement.CommitToAction(); match.RefreshDecisionTiming();
            }
        }
        Assert.That(movement.isActivated && movement.isCommitted, Is.True);
        Assert.That(match.DecisionTiming.Current.kind, Is.EqualTo(DecisionKind.AttackingMovement));
        Assert.That(match.DecisionTiming.Current.remaining, Is.EqualTo(DecisionTimingConfig.GetBudget(DecisionKind.AttackingMovement, difficulty)));
        match.AdvanceDecisionTiming(2);
        Assert.That(match.DecisionTiming.Current.remaining, Is.EqualTo(DecisionTimingConfig.GetBudget(DecisionKind.AttackingMovement, difficulty) - 2));
        yield break;
    }

    public IEnumerator RunCornerTargetFeedback(int side, int difficulty)
    {
        Board(side); match.difficulty_level = difficulty; match.isClockRunning = true;
        Place(attacker, Hex(18 * side, 12)); Place(defender, Hex(-10 * side, 8));
        var first = Token("Corner receiver", true, false, 7); Place(first, Hex(14 * side, 1));
        var second = Token("Other corner receiver", true, false, 6); Place(second, Hex(15 * side, 2));
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker);
        match.currentState = MatchManager.GameState.FreeKickExecution; Subscribe(high);
        match.TriggerHighPass(isCornerKick: true);
        if (difficulty == 3)
        {
            Assert.That(grid.highlightedHexes, Is.Empty, "difficulty three keeps target assistance hidden");
            yield break;
        }
        yield return Until(() => high.isAvailableTargetsReady, "corner target map ready");
        foreach (var target in new[] {first.currentHex, second.currentHex})
        {
            Assert.That(high.GetEvaluatedDecisionTargets(), Does.Contain(target));
            Assert.That(grid.highlightedHexes, Does.Contain(target)); AssertLegacyColor(target, Color.yellow);
        }
        Assert.That(grid.highlightedHexes.Contains(attacker.currentHex), Is.False);
        Invoke(high, "OnHoverReceived", first, first.currentHex); AssertLegacyColor(first.currentHex, new Color(1f,.55f,0f));
        Invoke(high, "OnHoverReceived", null, null); AssertLegacyColor(first.currentHex, Color.yellow);
        input.TryExecuteSyntheticHexClick(first.currentHex, "Corner preview", out _);
        Assert.That(high.currentTargetHex, Is.SameAs(first.currentHex));
        AssertLegacyColor(first.currentHex, new Color(1f,.55f,0f)); AssertLegacyColor(second.currentHex, Color.yellow);
    }

    public IEnumerator RunFinalThirdHoverFeedback(int side, int difficulty)
    {
        Board(side); match.difficulty_level = difficulty; match.isClockRunning = true;
        Place(attacker, Hex(14 * side)); ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker);
        var mover = Token("F3 hover mover", true, false, 7); Place(mover, Hex(-14 * side));
        var f3 = match.finalThirdManager; Subscribe(f3); f3.TriggerFinalThirdPhase();
        yield return Until(() => (bool)typeof(FinalThirdManager).GetField("isWaitingForTokenSelection", Private).GetValue(f3), "F3 player selection");
        input.TryExecuteSyntheticHexClick(mover.currentHex, "F3 hover select", out _);
        var targets = grid.highlightedHexes.Where(h => h.GetOccupyingToken() == null && !h.isOutOfBounds).Take(2).ToArray();
        Assert.That(targets.Length, Is.EqualTo(2));
        var hover = typeof(FinalThirdManager).GetMethod("OnHoverReceived", Private);
        Assert.That(hover, Is.Not.Null, "F3 must provide selected-destination hover feedback");
        hover.Invoke(f3, new object[] {null, targets[0]}); AssertLegacyColor(targets[0], new Color(1f,.48f,.05f));
        hover.Invoke(f3, new object[] {null, targets[1]}); AssertLegacyColor(targets[0], Color.yellow); AssertLegacyColor(targets[1], new Color(1f,.48f,.05f));
        hover.Invoke(f3, new object[] {null, Hex(0)}); AssertLegacyColor(targets[1], Color.yellow);
        hover.Invoke(f3, new object[] {null, targets[0]}); LegacyKey("Return");
        Assert.That(typeof(HexCell).GetField("currentHighlightReason", Private).GetValue(targets[0]), Is.Not.EqualTo("MovementDestinationHover"));
    }
}
