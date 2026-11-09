using System.Collections;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunInspectionTiming(int side)
    {
        Board(side); Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);
        match.SetLastToken(attacker); match.isClockRunning = true;
        match.currentState = MatchManager.GameState.EndOfMovementPhase;
        match.RefreshAvailableActionsForCurrentState();
        var views = LoadPresentationViews();
        yield return null;
        var inspection = views.GetComponent<RoomInspectionCard>();
        inspection.InspectPreview(attacker);
        Assert.That(inspection.IsOpen, Is.True);
        match.RefreshDecisionTiming();
        var decision = match.DecisionTiming.Current;
        Assert.That(decision, Is.Not.Null);
        float before = decision.remaining;
        match.AdvanceDecisionTiming(3);
        Assert.That(decision.remaining, Is.EqualTo(before - 3));
        var physical = new KeyPressData(KeyCode.R, false, false, false);
        Assert.That(RoomInputOwnership.ConsumeKey(physical, true, attacker), Is.True);
        Subscribe(match); Invoke(match, "EnterExtraActionsRollPrompt");
        match.AdvanceDecisionTiming(2);
        string roll = match.DecisionTiming.RollId;
        Assert.That(match.DecisionTiming.RollRemaining, Is.EqualTo(3));
        match.SetPauseMenuOpen(true); match.AdvanceDecisionTiming(100);
        Assert.That(match.IsWaitingForExtraActionsRoll, Is.True);
        Assert.That(match.DecisionTiming.RollRemaining, Is.EqualTo(3));
        match.SetPauseMenuOpen(false); match.AdvanceDecisionTiming(3);
        Assert.That(match.IsWaitingForExtraActionsRoll, Is.False);
        Assert.That(match.TimingRollSequence, Is.EqualTo(1));
        Assert.That(inspection.IsOpen, Is.True);
        Assert.That(match.DecisionTiming.SubmitRoll(roll, "Home"), Is.False);
        inspection.Close();
    }
}
