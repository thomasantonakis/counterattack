using NUnit.Framework;

[Category("DecisionTimingLiveClock")]
public sealed class DecisionTimingLiveClockEditorTests
{
    [TestCase(DecisionKind.SnapshotOrForfeitPace)]
    [TestCase(DecisionKind.StandaloneNutmegDecision)]
    [TestCase(DecisionKind.StandaloneTackleOrStand)]
    public void ChoiceAndPhaseChargeTogetherWithoutChargingOtherTeam(DecisionKind kind)
    {
        var t = new DecisionTimingService();
        void Register(bool choice)
        {
            t.BeginRegistration();
            t.Register("opponent", DecisionKind.DefensiveMovement, "Away", 3, false);
            t.Register("phase", DecisionKind.AttackingMovement, "Home", 3, true, choice);
            if (choice) t.Register("choice", kind, "Home", 3, true);
            t.EndRegistration();
        }
        Register(true);
        var child = t.Current;
        t.Advance(2);
        Assert.That(child.remaining, Is.EqualTo(3));
        var saved = t.Capture();
        Assert.That(saved.decisions.Find(d => d.key == "phase").remaining, Is.EqualTo(58));
        Assert.That(saved.decisions.Find(d => d.key == "opponent").remaining, Is.EqualTo(90));
        Register(true);
        Assert.That(t.Current.id, Is.EqualTo(child.id));
        t.Paused = true; t.Advance(100); t.Paused = false;
        Assert.That(t.Current.remaining, Is.EqualTo(3));
        t.Advance(3); Assert.That(t.HoldsMatchClock, Is.True);
        Register(false);
        Assert.That(t.HoldsMatchClock, Is.False, "ending the choice releases its own expiry");
        Assert.That(t.Current.remaining, Is.EqualTo(55));
        t.Advance(1); Assert.That(t.Current.remaining, Is.EqualTo(54), "phase is charged once after the choice ends");
    }

    [Test] public void ConcurrentPhaseExpirySurvivesChoiceCompletionAndRestore()
    {
        var t = new DecisionTimingService();
        t.BeginDecision("phase", DecisionKind.AttackingMovement, "Home", 3); t.Select("phase"); t.Advance(59);
        t.BeginRegistration();
        t.Register("phase", DecisionKind.AttackingMovement, "Home", 3, true, true);
        t.Register("choice", DecisionKind.SnapshotOrForfeitPace, "Home", 3, true); t.EndRegistration();
        t.Advance(1); Assert.That(t.HoldsMatchClock, Is.True);
        var restored = new DecisionTimingService(); restored.Restore(t.Capture());
        restored.BeginRegistration(); restored.Register("phase", DecisionKind.AttackingMovement, "Home", 3, true, true);
        restored.Register("choice", DecisionKind.SnapshotOrForfeitPace, "Home", 3, true); restored.EndRegistration();
        Assert.That(restored.Current.remaining, Is.EqualTo(4));
        restored.BeginRegistration(); restored.Register("phase", DecisionKind.AttackingMovement, "Home", 3, true); restored.EndRegistration();
        Assert.That(restored.HoldsMatchClock, Is.True);
        Assert.That(restored.Current.remaining, Is.Zero);
    }

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
