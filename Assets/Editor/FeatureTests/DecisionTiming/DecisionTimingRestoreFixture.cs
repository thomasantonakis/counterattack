using System.Collections;
using NUnit.Framework;
using Newtonsoft.Json;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunGameplayRollRestore(int side)
    {
        Board(side); Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);
        match.SetLastToken(attacker); Subscribe(match);
        Invoke(match, "EnterExtraActionsRollPrompt");
        match.AdvanceDecisionTiming(2);
        string id = match.DecisionTiming.RollId;
        var snapshot = JsonConvert.DeserializeObject<RoomRuntimeSnapshot>(JsonConvert.SerializeObject(match.CreateRuntimeSnapshot()));
        match.AdvanceDecisionTiming(3);
        Assert.That(match.TimingRollSequence, Is.EqualTo(1));
        Invoke(match, "ApplyRuntimeSnapshot", snapshot);
        match.RefreshDecisionTiming();
        Assert.That(match.IsWaitingForExtraActionsRoll, Is.True);
        Assert.That(match.DecisionTiming.RollId, Is.EqualTo(id));
        Assert.That(match.DecisionTiming.RollRemaining, Is.EqualTo(3));
        Assert.That(match.TimingRollSequence, Is.EqualTo(0));
        match.SetPauseMenuOpen(true); match.AdvanceDecisionTiming(100);
        Assert.That(match.DecisionTiming.RollRemaining, Is.EqualTo(3));
        match.SetPauseMenuOpen(false); match.AdvanceDecisionTiming(3);
        Assert.That(match.IsWaitingForExtraActionsRoll, Is.False);
        Assert.That(match.TimingRollSequence, Is.EqualTo(1));
        Invoke(match, "ResetMatchClockForNewMatch");
        match.RefreshDecisionTiming();
        Assert.That(match.DecisionTiming.HasPendingRoll, Is.False);
        Assert.That(match.DecisionTiming.Capture().decisions, Is.Empty);
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.False);
        Assert.That(match.TimingRollSequence, Is.EqualTo(0));
        yield break;
    }

    public IEnumerator RunGameplayTimingRestore(int side, string scenario)
    {
        Board(side); Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);
        Place(defender, Hex(-10 * side, 8));
        match.SetLastToken(attacker); match.isClockRunning = true;
        match.currentState = MatchManager.GameState.EndOfMovementPhase;
        match.RefreshAvailableActionsForCurrentState();
        if (scenario == "movement") movement.ActivateMovementPhase();
        match.RefreshDecisionTiming();
        var original = match.DecisionTiming.Current;
        Assert.That(original, Is.Not.Null);
        match.AdvanceDecisionTiming(scenario == "expired" ? original.remaining : 4);
        float remaining = original.remaining;
        var snapshot = JsonConvert.DeserializeObject<RoomRuntimeSnapshot>(JsonConvert.SerializeObject(match.CreateRuntimeSnapshot()));
        if (scenario == "old") snapshot.decisionTiming = null;
        match.AdvanceDecisionTiming(1000); // Time in the discarded session is not charged to the save.
        Invoke(match, "ApplyRuntimeSnapshot", snapshot);
        match.RefreshDecisionTiming();
        var restored = match.DecisionTiming.Current;
        Assert.That(restored, Is.Not.Null);
        if (scenario == "matching" || scenario == "expired")
        {
            Assert.That(restored.id, Is.EqualTo(original.id));
            Assert.That(restored.remaining, Is.EqualTo(remaining));
            Assert.That(match.DecisionTiming.HoldsMatchClock, Is.EqualTo(scenario == "expired"));
        }
        else
        {
            Assert.That(restored.kind, Is.EqualTo(DecisionKind.SelectNextAction));
            Assert.That(restored.remaining, Is.EqualTo(DecisionTimingConfig.GetBudget(restored.kind, match.difficulty_level)));
            Assert.That(match.DecisionTiming.HoldsMatchClock, Is.False, "unsupported active actions must drop stale timing");
        }
        Assert.That(movement.isActivated, Is.False);
        Subscribe(ground); LegacyKey("P");
        match.RefreshDecisionTiming();
        Assert.That(ground.isActivated, Is.True, "restored gameplay must accept the next real action");
        Assert.That(match.DecisionTiming.Current.kind, Is.EqualTo(DecisionKind.StandardPassTarget));
        yield break;
    }
}
