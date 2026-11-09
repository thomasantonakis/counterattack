using NUnit.Framework;

[Category("DecisionTimingLiveClock")]
public sealed class DecisionTimingLiveClockEditorTests
{
    [TestCase("pass interception")]
    [TestCase("movement steal")]
    [TestCase("tackle")]
    [TestCase("nutmeg")]
    [TestCase("injury")]
    [TestCase("leniency")]
    [TestCase("loose direction")]
    [TestCase("loose distance")]
    [TestCase("accuracy")]
    [TestCase("ball control")]
    [TestCase("shooting")]
    [TestCase("saving")]
    [TestCase("handling")]
    [TestCase("catch")]
    public void RollChargesExistingOwnerBudgetAndNeverHoldsClock(string stage)
    {
        var t = new DecisionTimingService();
        var phase = t.BeginDecision("phase", DecisionKind.AttackingMovement, "Home", 3); t.Select("phase");
        int calls = 0; t.BeginRoll(stage, "Home", () => true, () => calls++);
        Assert.That(t.HoldsMatchClock, Is.False);
        t.Advance(4); Assert.That(phase.remaining, Is.EqualTo(phase.budget - 4));
        Assert.That(calls, Is.Zero);
        t.Advance(1); Assert.That(calls, Is.EqualTo(1));
        Assert.That(phase.remaining, Is.EqualTo(phase.budget - 5));
        Assert.That(t.HoldsMatchClock, Is.False);
    }

    [Test] public void RollDoesNotChargeTheOtherTeamsAllowance()
    {
        var t = new DecisionTimingService(); var phase = t.BeginDecision("phase", DecisionKind.DefensiveMovement, "Away", 3);
        t.Select("phase"); t.BeginRoll("attacker", "Home", () => true, () => {}); t.Advance(2);
        Assert.That(phase.remaining, Is.EqualTo(phase.budget)); Assert.That(t.RollRemaining, Is.EqualTo(3));
    }

    [Test] public void ExpiredPhaseRunsThroughRollAutomaticReviewAndMovementUntilTacticalInputReturns()
    {
        var t = new DecisionTimingService(); var phase = t.BeginDecision("phase", DecisionKind.AttackingMovement, "Home", 3);
        t.Select("phase"); t.Advance(phase.remaining); Assert.That(t.HoldsMatchClock, Is.True);
        t.BeginRoll("roll", "Home", () => true, () => {}); Assert.That(t.HoldsMatchClock, Is.False);
        t.AutomaticResult = true; t.EndRoll(); Assert.That(t.HoldsMatchClock, Is.False);
        t.AutomaticResult = false; t.ContinuationInProgress = true; Assert.That(t.HoldsMatchClock, Is.False);
        t.ContinuationInProgress = false; Assert.That(t.HoldsMatchClock, Is.True);
    }

    [Test] public void ManualHoldAndPauseSuspendBudgetAndDeadlineButOrdinaryReviewChargesBudget()
    {
        var t = new DecisionTimingService(); var phase = t.BeginDecision("phase", DecisionKind.AttackingMovement, "Home", 3);
        t.Select("phase"); t.BeginRoll("roll", "Home", () => true, () => {});
        t.BlockingResult = true; t.Advance(100); Assert.That(t.HoldsMatchClock, Is.True);
        Assert.That(phase.remaining, Is.EqualTo(60)); Assert.That(t.RollRemaining, Is.EqualTo(5));
        t.BlockingResult = false; t.Paused = true; t.Advance(100);
        Assert.That(phase.remaining, Is.EqualTo(60)); Assert.That(t.RollRemaining, Is.EqualTo(5));
        t.Paused = false; t.EndRoll(); t.AutomaticResult = true; t.Advance(5);
        Assert.That(phase.remaining, Is.EqualTo(55)); Assert.That(t.HoldsMatchClock, Is.False);
    }
}
