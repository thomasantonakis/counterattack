using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json;

[Category("DecisionTiming")]
public sealed class DecisionTimingEditorTests
{
    public static IEnumerable Budgets()
    {
        yield return new TestCaseData(DecisionKind.SelectNextAction, 1, 30f);
        yield return new TestCaseData(DecisionKind.SelectNextAction, 2, 20f);
        yield return new TestCaseData(DecisionKind.SelectNextAction, 3, 15f);
        yield return new TestCaseData(DecisionKind.StandardPassTarget, 1, 20f);
        yield return new TestCaseData(DecisionKind.StandardPassTarget, 2, 15f);
        yield return new TestCaseData(DecisionKind.StandardPassTarget, 3, 10f);
        yield return new TestCaseData(DecisionKind.FinalThirdAttackingMovement, 1, 30f);
        yield return new TestCaseData(DecisionKind.FinalThirdAttackingMovement, 2, 20f);
        yield return new TestCaseData(DecisionKind.FinalThirdAttackingMovement, 3, 15f);
        yield return new TestCaseData(DecisionKind.FinalThirdDefensiveMovement, 1, 30f);
        yield return new TestCaseData(DecisionKind.FinalThirdDefensiveMovement, 2, 20f);
        yield return new TestCaseData(DecisionKind.FinalThirdDefensiveMovement, 3, 15f);
        yield return new TestCaseData(DecisionKind.AttackingMovement, 1, 90f);
        yield return new TestCaseData(DecisionKind.AttackingMovement, 2, 80f);
        yield return new TestCaseData(DecisionKind.AttackingMovement, 3, 60f);
        yield return new TestCaseData(DecisionKind.DefensiveMovement, 1, 120f);
        yield return new TestCaseData(DecisionKind.DefensiveMovement, 2, 100f);
        yield return new TestCaseData(DecisionKind.DefensiveMovement, 3, 90f);
        yield return new TestCaseData(DecisionKind.AttackingTwoForTwo, 1, 30f);
        yield return new TestCaseData(DecisionKind.AttackingTwoForTwo, 2, 20f);
        yield return new TestCaseData(DecisionKind.AttackingTwoForTwo, 3, 10f);
        yield return new TestCaseData(DecisionKind.SnapshotOrForfeitPace, 1, 10f);
        yield return new TestCaseData(DecisionKind.SnapshotOrForfeitPace, 2, 10f);
        yield return new TestCaseData(DecisionKind.SnapshotOrForfeitPace, 3, 5f);
        yield return new TestCaseData(DecisionKind.TackleReposition, 1, 15f);
        yield return new TestCaseData(DecisionKind.TackleReposition, 2, 10f);
        yield return new TestCaseData(DecisionKind.TackleReposition, 3, 5f);
        yield return new TestCaseData(DecisionKind.NutmegReposition, 1, 15f);
        yield return new TestCaseData(DecisionKind.NutmegReposition, 2, 10f);
        yield return new TestCaseData(DecisionKind.NutmegReposition, 3, 5f);
        yield return new TestCaseData(DecisionKind.DefensiveBoxMovement, 1, 10f);
        yield return new TestCaseData(DecisionKind.DefensiveBoxMovement, 2, 10f);
        yield return new TestCaseData(DecisionKind.DefensiveBoxMovement, 3, 8f);
        yield return new TestCaseData(DecisionKind.StandaloneTackleOrStand, 1, 10f);
        yield return new TestCaseData(DecisionKind.StandaloneTackleOrStand, 2, 10f);
        yield return new TestCaseData(DecisionKind.StandaloneTackleOrStand, 3, 5f);
        yield return new TestCaseData(DecisionKind.StandaloneNutmegDecision, 1, 10f);
        yield return new TestCaseData(DecisionKind.StandaloneNutmegDecision, 2, 10f);
        yield return new TestCaseData(DecisionKind.StandaloneNutmegDecision, 3, 5f);
        yield return new TestCaseData(DecisionKind.FirstTimePassTarget, 1, 20f);
        yield return new TestCaseData(DecisionKind.FirstTimePassTarget, 2, 15f);
        yield return new TestCaseData(DecisionKind.FirstTimePassTarget, 3, 10f);
        yield return new TestCaseData(DecisionKind.FirstTimePassAttackerMovement, 1, 15f);
        yield return new TestCaseData(DecisionKind.FirstTimePassAttackerMovement, 2, 15f);
        yield return new TestCaseData(DecisionKind.FirstTimePassAttackerMovement, 3, 10f);
        yield return new TestCaseData(DecisionKind.FirstTimePassDefenderMovement, 1, 15f);
        yield return new TestCaseData(DecisionKind.FirstTimePassDefenderMovement, 2, 15f);
        yield return new TestCaseData(DecisionKind.FirstTimePassDefenderMovement, 3, 10f);
        yield return new TestCaseData(DecisionKind.HighPassTarget, 1, 20f);
        yield return new TestCaseData(DecisionKind.HighPassTarget, 2, 15f);
        yield return new TestCaseData(DecisionKind.HighPassTarget, 3, 10f);
        yield return new TestCaseData(DecisionKind.HighPassAttackerMovement, 1, 15f);
        yield return new TestCaseData(DecisionKind.HighPassAttackerMovement, 2, 15f);
        yield return new TestCaseData(DecisionKind.HighPassAttackerMovement, 3, 10f);
        yield return new TestCaseData(DecisionKind.HighPassDefenderMovement, 1, 15f);
        yield return new TestCaseData(DecisionKind.HighPassDefenderMovement, 2, 15f);
        yield return new TestCaseData(DecisionKind.HighPassDefenderMovement, 3, 10f);
        yield return new TestCaseData(DecisionKind.HighPassAttackingJumpers, 1, 15f);
        yield return new TestCaseData(DecisionKind.HighPassAttackingJumpers, 2, 15f);
        yield return new TestCaseData(DecisionKind.HighPassAttackingJumpers, 3, 10f);
        yield return new TestCaseData(DecisionKind.HighPassDefensiveJumpers, 1, 15f);
        yield return new TestCaseData(DecisionKind.HighPassDefensiveJumpers, 2, 15f);
        yield return new TestCaseData(DecisionKind.HighPassDefensiveJumpers, 3, 10f);
        yield return new TestCaseData(DecisionKind.HighPassGoalkeeperRush, 1, 10f);
        yield return new TestCaseData(DecisionKind.HighPassGoalkeeperRush, 2, 10f);
        yield return new TestCaseData(DecisionKind.HighPassGoalkeeperRush, 3, 5f);
        yield return new TestCaseData(DecisionKind.LongBallTarget, 1, 20f);
        yield return new TestCaseData(DecisionKind.LongBallTarget, 2, 15f);
        yield return new TestCaseData(DecisionKind.LongBallTarget, 3, 10f);
        yield return new TestCaseData(DecisionKind.LongBallGoalkeeperRush, 1, 10f);
        yield return new TestCaseData(DecisionKind.LongBallGoalkeeperRush, 2, 10f);
        yield return new TestCaseData(DecisionKind.LongBallGoalkeeperRush, 3, 5f);
        yield return new TestCaseData(DecisionKind.HeaderOrBallControl, 1, 10f);
        yield return new TestCaseData(DecisionKind.HeaderOrBallControl, 2, 10f);
        yield return new TestCaseData(DecisionKind.HeaderOrBallControl, 3, 5f);
        yield return new TestCaseData(DecisionKind.HeadedPassOrHeaderAtGoal, 1, 10f);
        yield return new TestCaseData(DecisionKind.HeadedPassOrHeaderAtGoal, 2, 10f);
        yield return new TestCaseData(DecisionKind.HeadedPassOrHeaderAtGoal, 3, 5f);
        yield return new TestCaseData(DecisionKind.HeadedPassTarget, 1, 20f);
        yield return new TestCaseData(DecisionKind.HeadedPassTarget, 2, 15f);
        yield return new TestCaseData(DecisionKind.HeadedPassTarget, 3, 10f);
        yield return new TestCaseData(DecisionKind.ShotGoalLineTarget, 1, 10f);
        yield return new TestCaseData(DecisionKind.ShotGoalLineTarget, 2, 10f);
        yield return new TestCaseData(DecisionKind.ShotGoalLineTarget, 3, 5f);
        yield return new TestCaseData(DecisionKind.SnapshotGoalLineTarget, 1, 10f);
        yield return new TestCaseData(DecisionKind.SnapshotGoalLineTarget, 2, 10f);
        yield return new TestCaseData(DecisionKind.SnapshotGoalLineTarget, 3, 5f);
        yield return new TestCaseData(DecisionKind.SnapshotDefenderMovement, 1, 15f);
        yield return new TestCaseData(DecisionKind.SnapshotDefenderMovement, 2, 15f);
        yield return new TestCaseData(DecisionKind.SnapshotDefenderMovement, 3, 10f);
        yield return new TestCaseData(DecisionKind.GoalkeeperPickupOrLeave, 1, 10f);
        yield return new TestCaseData(DecisionKind.GoalkeeperPickupOrLeave, 2, 10f);
        yield return new TestCaseData(DecisionKind.GoalkeeperPickupOrLeave, 3, 5f);
        yield return new TestCaseData(DecisionKind.GoalkeeperMovement, 1, 15f);
        yield return new TestCaseData(DecisionKind.GoalkeeperMovement, 2, 15f);
        yield return new TestCaseData(DecisionKind.GoalkeeperMovement, 3, 10f);
        yield return new TestCaseData(DecisionKind.QuickThrowOrDistribution, 1, 10f);
        yield return new TestCaseData(DecisionKind.QuickThrowOrDistribution, 2, 10f);
        yield return new TestCaseData(DecisionKind.QuickThrowOrDistribution, 3, 5f);
        yield return new TestCaseData(DecisionKind.GoalkeeperDistributionTarget, 1, 20f);
        yield return new TestCaseData(DecisionKind.GoalkeeperDistributionTarget, 2, 15f);
        yield return new TestCaseData(DecisionKind.GoalkeeperDistributionTarget, 3, 10f);
        yield return new TestCaseData(DecisionKind.ThrowInTakerSelection, 1, 20f);
        yield return new TestCaseData(DecisionKind.ThrowInTakerSelection, 2, 15f);
        yield return new TestCaseData(DecisionKind.ThrowInTakerSelection, 3, 10f);
        yield return new TestCaseData(DecisionKind.ThrowInMovementOrThrow, 1, 20f);
        yield return new TestCaseData(DecisionKind.ThrowInMovementOrThrow, 2, 15f);
        yield return new TestCaseData(DecisionKind.ThrowInMovementOrThrow, 3, 10f);
        yield return new TestCaseData(DecisionKind.ThrowInActionSelection, 1, 20f);
        yield return new TestCaseData(DecisionKind.ThrowInActionSelection, 2, 15f);
        yield return new TestCaseData(DecisionKind.ThrowInActionSelection, 3, 10f);
        yield return new TestCaseData(DecisionKind.ThrowInTarget, 1, 20f);
        yield return new TestCaseData(DecisionKind.ThrowInTarget, 2, 15f);
        yield return new TestCaseData(DecisionKind.ThrowInTarget, 3, 10f);
        yield return new TestCaseData(DecisionKind.FreeKickTakerSelection, 1, 20f);
        yield return new TestCaseData(DecisionKind.FreeKickTakerSelection, 2, 15f);
        yield return new TestCaseData(DecisionKind.FreeKickTakerSelection, 3, 10f);
        yield return new TestCaseData(DecisionKind.FreeKickAttackingSetup, 1, 30f);
        yield return new TestCaseData(DecisionKind.FreeKickAttackingSetup, 2, 20f);
        yield return new TestCaseData(DecisionKind.FreeKickAttackingSetup, 3, 15f);
        yield return new TestCaseData(DecisionKind.FreeKickDefensiveSetup, 1, 30f);
        yield return new TestCaseData(DecisionKind.FreeKickDefensiveSetup, 2, 20f);
        yield return new TestCaseData(DecisionKind.FreeKickDefensiveSetup, 3, 15f);
        yield return new TestCaseData(DecisionKind.FreeKickGoalkeeperMovement, 1, 15f);
        yield return new TestCaseData(DecisionKind.FreeKickGoalkeeperMovement, 2, 15f);
        yield return new TestCaseData(DecisionKind.FreeKickGoalkeeperMovement, 3, 10f);
        yield return new TestCaseData(DecisionKind.FreeKickFinalKickerSelection, 1, 20f);
        yield return new TestCaseData(DecisionKind.FreeKickFinalKickerSelection, 2, 15f);
        yield return new TestCaseData(DecisionKind.FreeKickFinalKickerSelection, 3, 10f);
        yield return new TestCaseData(DecisionKind.FreeKickActionSelection, 1, 20f);
        yield return new TestCaseData(DecisionKind.FreeKickActionSelection, 2, 15f);
        yield return new TestCaseData(DecisionKind.FreeKickActionSelection, 3, 10f);
        yield return new TestCaseData(DecisionKind.AdvantageOrFreeKick, 1, 10f);
        yield return new TestCaseData(DecisionKind.AdvantageOrFreeKick, 2, 10f);
        yield return new TestCaseData(DecisionKind.AdvantageOrFreeKick, 3, 5f);
        yield return new TestCaseData(DecisionKind.CornerTakerSelection, 1, 20f);
        yield return new TestCaseData(DecisionKind.CornerTakerSelection, 2, 15f);
        yield return new TestCaseData(DecisionKind.CornerTakerSelection, 3, 10f);
        yield return new TestCaseData(DecisionKind.CornerAttackingThreeMoves, 1, 20f);
        yield return new TestCaseData(DecisionKind.CornerAttackingThreeMoves, 2, 15f);
        yield return new TestCaseData(DecisionKind.CornerAttackingThreeMoves, 3, 10f);
        yield return new TestCaseData(DecisionKind.CornerDefensiveThreeMoves, 1, 20f);
        yield return new TestCaseData(DecisionKind.CornerDefensiveThreeMoves, 2, 15f);
        yield return new TestCaseData(DecisionKind.CornerDefensiveThreeMoves, 3, 10f);
        yield return new TestCaseData(DecisionKind.CornerActionSelection, 1, 20f);
        yield return new TestCaseData(DecisionKind.CornerActionSelection, 2, 15f);
        yield return new TestCaseData(DecisionKind.CornerActionSelection, 3, 10f);
        yield return new TestCaseData(DecisionKind.GoalKickAttackingSetup, 1, 30f);
        yield return new TestCaseData(DecisionKind.GoalKickAttackingSetup, 2, 20f);
        yield return new TestCaseData(DecisionKind.GoalKickAttackingSetup, 3, 15f);
        yield return new TestCaseData(DecisionKind.GoalKickDefensiveSetup, 1, 30f);
        yield return new TestCaseData(DecisionKind.GoalKickDefensiveSetup, 2, 20f);
        yield return new TestCaseData(DecisionKind.GoalKickDefensiveSetup, 3, 15f);
        yield return new TestCaseData(DecisionKind.GoalKickActionSelection, 1, 20f);
        yield return new TestCaseData(DecisionKind.GoalKickActionSelection, 2, 15f);
        yield return new TestCaseData(DecisionKind.GoalKickActionSelection, 3, 10f);
        yield return new TestCaseData(DecisionKind.GoalKickTarget, 1, 20f);
        yield return new TestCaseData(DecisionKind.GoalKickTarget, 2, 15f);
        yield return new TestCaseData(DecisionKind.GoalKickTarget, 3, 10f);
        yield return new TestCaseData(DecisionKind.PenaltyTakerSelection, 1, 20f);
        yield return new TestCaseData(DecisionKind.PenaltyTakerSelection, 2, 15f);
        yield return new TestCaseData(DecisionKind.PenaltyTakerSelection, 3, 10f);
        yield return new TestCaseData(DecisionKind.PenaltyAttackingSetup, 1, 30f);
        yield return new TestCaseData(DecisionKind.PenaltyAttackingSetup, 2, 20f);
        yield return new TestCaseData(DecisionKind.PenaltyAttackingSetup, 3, 15f);
        yield return new TestCaseData(DecisionKind.PenaltyDefensiveSetup, 1, 30f);
        yield return new TestCaseData(DecisionKind.PenaltyDefensiveSetup, 2, 20f);
        yield return new TestCaseData(DecisionKind.PenaltyDefensiveSetup, 3, 15f);
        yield return new TestCaseData(DecisionKind.ShootoutOrderSelection, 1, 90f);
        yield return new TestCaseData(DecisionKind.ShootoutOrderSelection, 2, 80f);
        yield return new TestCaseData(DecisionKind.ShootoutOrderSelection, 3, 60f);
        yield return new TestCaseData(DecisionKind.KickoffFormationSetup, 1, 90f);
        yield return new TestCaseData(DecisionKind.KickoffFormationSetup, 2, 80f);
        yield return new TestCaseData(DecisionKind.KickoffFormationSetup, 3, 60f);
        yield return new TestCaseData(DecisionKind.KickoffTakerSelection, 1, 20f);
        yield return new TestCaseData(DecisionKind.KickoffTakerSelection, 2, 15f);
        yield return new TestCaseData(DecisionKind.KickoffTakerSelection, 3, 10f);
        yield return new TestCaseData(DecisionKind.KickoffConfirmation, 1, 20f);
        yield return new TestCaseData(DecisionKind.KickoffConfirmation, 2, 15f);
        yield return new TestCaseData(DecisionKind.KickoffConfirmation, 3, 10f);
    }
    [TestCaseSource(nameof(Budgets))]
    public void ApprovedBudgetExpiresAtExactBoundary(DecisionKind kind, int difficulty, float seconds)
    {
        Assert.That(DecisionTimingConfig.GetBudget(kind, difficulty), Is.EqualTo(seconds));
        var timing = new DecisionTimingService();
        var state = timing.BeginDecision("choice", kind, "Home", difficulty);
        timing.Select("choice");
        timing.Advance(seconds - .25f);
        Assert.That(timing.HoldsMatchClock, Is.False);
        timing.BeginDecision("choice", kind, "Home", difficulty);
        Assert.That(state.remaining, Is.EqualTo(.25f), "Reselection cannot refill a logical choice.");
        timing.Advance(.25f);
        Assert.That(timing.HoldsMatchClock, Is.True);
        timing.Advance(1000);
        Assert.That(state.remaining, Is.Zero);
        timing.EndDecision("choice");
        Assert.That(timing.HoldsMatchClock, Is.False);
    }
    [Test] public void AllKindsHaveExactlyOneBudget()
        => Assert.That(DecisionTimingConfig.Budgets.Keys, Is.EquivalentTo(Enum.GetValues(typeof(DecisionKind))));

    [TestCase(false)] [TestCase(true)]
    public void ChildAndRollSuspendParentAndPreserveExpiredHold(bool expired)
    {
        var t = new DecisionTimingService();
        var parent = t.BeginDecision("phase", DecisionKind.AttackingMovement, "Home", 3);
        t.Select("phase"); t.Advance(expired ? 60 : 12);
        var child = t.BeginDecision("child", DecisionKind.TackleReposition, "Away", 3);
        t.Select("child"); t.Advance(2);
        Assert.That(parent.remaining, Is.EqualTo(expired ? 0 : 48));
        t.BeginRoll("roll", "Away", () => true, () => {});
        t.Advance(3);
        Assert.That(child.remaining, Is.EqualTo(3));
        t.EndRoll(); t.EndDecision("child"); t.Select("phase");
        Assert.That(t.HoldsMatchClock, Is.EqualTo(expired));
        Assert.That(parent.remaining, Is.EqualTo(expired ? 0 : 48));
    }
    [TestCase(false)] [TestCase(true)]
    public void ManualAutomaticRaceClaimsOnlyOnce(bool manualFirst)
    {
        var t = new DecisionTimingService(); int calls = 0;
        t.BeginRoll("one", "Away", () => true, () => calls++);
        var id = t.RollId;
        t.Advance(4.75f); Assert.That(calls, Is.Zero);
        if (manualFirst) Assert.That(t.SubmitRoll(id, "Away"), Is.True);
        t.Advance(.25f);
        Assert.That(t.SubmitRoll(id, "Away"), Is.False);
        Assert.That(calls, Is.EqualTo(1));
        t.BeginRoll("two", "Home", () => true, () => calls++);
        Assert.That(t.SubmitRoll(id, "Away"), Is.False);
        t.Advance(4.75f); Assert.That(calls, Is.EqualTo(1));
        t.Advance(.25f); Assert.That(calls, Is.EqualTo(2));
    }
    [Test] public void PauseReviewAndAuthoritySuspendExecution()
    {
        var t = new DecisionTimingService(); int calls = 0; bool ready = false;
        t.BeginRoll("roll", "Home", () => ready, () => calls++);
        t.Advance(100); Assert.That(t.RollRemaining, Is.EqualTo(5));
        ready = true; t.Paused = true; t.Advance(100);
        t.Paused = false; t.BlockingResult = true; t.Advance(100);
        Assert.That(t.RollRemaining, Is.EqualTo(5));
        t.BlockingResult = false; t.HasCommandAuthority = false; t.Advance(5);
        Assert.That(calls, Is.Zero);
        t.HasCommandAuthority = true; t.Advance(0); Assert.That(calls, Is.EqualTo(1));
    }
    [Test] public void RestoreMatchesOnlyResumedDecisionAndKeepsRollDeadline()
    {
        var t = new DecisionTimingService();
        t.BeginDecision("phase", DecisionKind.DefensiveMovement, "Away", 2);
        t.Select("phase"); t.Advance(20); t.Suspend("phase");
        t.BeginRoll("roll", "Away", () => true, () => {}); t.Advance(2);
        var snapshot = JsonConvert.DeserializeObject<DecisionTimingSnapshot>(JsonConvert.SerializeObject(t.Capture()));
        var restored = new DecisionTimingService(); restored.Restore(snapshot);
        restored.BeginRegistration(); restored.Register("phase", DecisionKind.DefensiveMovement, "Away", 1, true);
        restored.BeginRoll("roll", "Away", () => true, () => {}); restored.EndRegistration();
        Assert.That(restored.Current.remaining, Is.EqualTo(80));
        Assert.That(restored.RollRemaining, Is.EqualTo(3));
        restored.EndRoll(); restored.Advance(10); Assert.That(restored.Current.remaining, Is.EqualTo(80));
        restored.Resume("phase"); restored.Advance(10); Assert.That(restored.Current.remaining, Is.EqualTo(70));
        restored.Clear(); Assert.That(restored.HoldsMatchClock, Is.False);
        restored.Restore(snapshot); restored.BeginRegistration();
        restored.Register("different", DecisionKind.DefensiveMovement, "Away", 3, true); restored.EndRegistration();
        Assert.That(restored.Current.remaining, Is.EqualTo(90));
        Assert.That(JsonConvert.DeserializeObject<RoomRuntimeSnapshot>("{}").decisionTiming, Is.Null);
    }
    [Test] public void CancelAndReopenTargetRetainsIdentityAndAllowance()
    {
        var t = new DecisionTimingService();
        t.BeginRegistration(); t.Register("target", DecisionKind.StandardPassTarget, "Home", 2, true); t.EndRegistration();
        string id = t.Current.id; t.Advance(7);
        t.BeginRegistration(); t.EndRegistration();
        t.BeginRegistration(); t.Register("target", DecisionKind.StandardPassTarget, "Home", 2, true); t.EndRegistration();
        Assert.That(t.Current.id, Is.EqualTo(id)); Assert.That(t.Current.remaining, Is.EqualTo(8));
        t.ClearUnresolvedChoices(); t.EndDecision("target");
        Assert.That(t.BeginDecision("target", DecisionKind.StandardPassTarget, "Home", 2).remaining, Is.EqualTo(15));
    }
    [Test] public void ForcedChildTransitionCannotChargeParent()
    {
        var t = new DecisionTimingService();
        t.BeginRegistration();
        t.Register("parent", DecisionKind.AttackingMovement, "Home", 2, true);
        t.Register("child", DecisionKind.TackleReposition, "Away", 2, false);
        t.EndRegistration(); t.Advance(20);
        Assert.That(t.Current, Is.Null);
        Assert.That(t.Capture().decisions.Find(d => d.key == "parent").remaining, Is.EqualTo(80));
        Assert.That(t.Capture().decisions.Find(d => d.key == "child").remaining, Is.EqualTo(10));
    }
    [Test] public void BuiltInStoppageSkipsBudgetsButStillRollsAndRetainsInterruptedTime()
    {
        var t = new DecisionTimingService();
        t.BeginRegistration(); t.Register("live", DecisionKind.AttackingMovement, "Home", 2, true); t.EndRegistration();
        t.Advance(10); t.GameClockStopped = true;
        t.BeginRegistration(); t.Register("live", DecisionKind.AttackingMovement, "Home", 2, true);
        t.Register("restart", DecisionKind.CornerTakerSelection, "Home", 2, true); t.EndRegistration();
        t.Advance(100); Assert.That(t.Current, Is.Null);
        Assert.That(t.Capture().decisions.Count, Is.EqualTo(1));
        int calls = 0; t.BeginRoll("roll", "Home", () => true, () => calls++); t.Advance(5);
        Assert.That(calls, Is.EqualTo(1)); t.EndRoll(); t.GameClockStopped = false;
        t.BeginRegistration(); t.Register("live", DecisionKind.AttackingMovement, "Home", 2, true); t.EndRegistration();
        Assert.That(t.Current.remaining, Is.EqualTo(70));
    }
    [Test] public void NestedRollRegistrationDoesNotRearmTheChildEveryFrame()
    {
        using var fixture = new GoalkeeperBoxEntryFixture();
        var match = fixture.match;
        var timing = match.DecisionTiming;
        void Register()
        {
            var r = new DecisionTimingRegistration(match, timing);
            r.Scope("outer", true); r.Roll(match, "tackle", true, () => true, _ => {});
            r.Scope("child", false); r.Roll(match, "scatter", true, () => true, _ => {});
            r.ApplyRoll();
        }
        Register(); string id = timing.RollId; timing.Advance(2);
        Register(); Assert.That(timing.RollId, Is.EqualTo(id));
        Assert.That(timing.RollRemaining, Is.EqualTo(3));
    }
}
