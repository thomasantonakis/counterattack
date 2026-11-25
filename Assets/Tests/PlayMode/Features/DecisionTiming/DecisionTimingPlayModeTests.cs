using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("DecisionTiming")]
public sealed class DecisionTimingPlayModeTests
{
    private object board;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies()
        .Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    [SetUp] public void SetUp() => board = Activator.CreateInstance(Bridge,
        new object[] {TestContext.CurrentContext.Test.MethodName == nameof(StoppageManualReview)});
    [UnityTearDown] public IEnumerator TearDown()
    {
        if (board != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(board, null);
        board = null;
    }
    [UnityTest] public IEnumerator TargetCountdownAndClock([Values(1,-1)] int side, [Values("P", "C", "L", "F")] string action)
        => (IEnumerator)Bridge.GetMethod("RunDecisionTimingPass").Invoke(board, new object[] { side, action });
    [UnityTest] public IEnumerator WholeMovementPhase([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunDecisionTimingMovement").Invoke(board, new object[] { side });
    [UnityTest] public IEnumerator AutomaticStoppageRoll()
        => (IEnumerator)Bridge.GetMethod("RunDecisionTimingAutomaticStoppage").Invoke(board, null);
    [UnityTest] public IEnumerator RestartAllowances([Values(1,-1)] int side, [Values("free", "corner", "penalty", "throw")] string restart)
        => (IEnumerator)Bridge.GetMethod("RunDecisionTimingRestart").Invoke(board, new object[] { side, restart });
    [UnityTest] public IEnumerator RealAccuracyRoll([Values(1,-1)] int side, [Values(false,true)] bool automatic)
        => (IEnumerator)Bridge.GetMethod("RunDecisionTimingAccuracy").Invoke(board, new object[] { side, automatic });
    [UnityTest] public IEnumerator AdditionalDecisions([Values(1,-1)] int side, [Values("ftp", "high", "keeper", "header", "shot", "finalThird")] string family)
        => (IEnumerator)Bridge.GetMethod("RunAdditionalTimingDecision").Invoke(board, new object[] { side, family });
    [UnityTest] public IEnumerator DuelInterruption([Values(1,-1)] int side, [Values(false,true)] bool nutmeg, [Values(false,true)] bool expired)
        => (IEnumerator)Bridge.GetMethod("RunTimingDuelInterruption").Invoke(board, new object[] {side,nutmeg,expired,false,false});
    [UnityTest] public IEnumerator RepositionForfeitXIsHandledBeforeMovementGuard()
        => (IEnumerator)Bridge.GetMethod("RunTimingDuelForfeitDuringMovementGuard").Invoke(board, null);
    [UnityTest] public IEnumerator DuelDecline([Values(1,-1)] int side, [Values(false,true)] bool nutmeg)
        => (IEnumerator)Bridge.GetMethod("RunTimingDuelInterruption").Invoke(board, new object[] {side,nutmeg,false,true,false});
    [UnityTest] public IEnumerator DuelTurnoverKeepsExpiredPhase([Values(1,-1)] int side, [Values(false,true)] bool nutmeg)
        => (IEnumerator)Bridge.GetMethod("RunTimingDuelInterruption").Invoke(board, new object[] {side,nutmeg,true,false,true});
    [UnityTest] public IEnumerator JumperSelection([Values(1,-1)] int side, [Values(false,true)] bool selectAll)
        => (IEnumerator)Bridge.GetMethod("RunTimingJumperSelection").Invoke(board, new object[] {side,selectAll,false,"normal"});
    [UnityTest] public IEnumerator ManualHeaderResult([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunTimingJumperSelection").Invoke(board, new object[] {side,false,true,"normal"});
    [UnityTest] public IEnumerator JumperContinuation([Values(1,-1)] int side, [Values("forfeit", "goal")] string continuation)
        => (IEnumerator)Bridge.GetMethod("RunTimingJumperSelection").Invoke(board, new object[] {side,false,false,continuation});
    [UnityTest] public IEnumerator GameplayTimingRestore([Values(1,-1)] int side, [Values("matching", "expired", "old", "movement")] string scenario)
        => (IEnumerator)Bridge.GetMethod("RunGameplayTimingRestore").Invoke(board, new object[] {side,scenario});
    [UnityTest] public IEnumerator InspectionTiming([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunInspectionTiming").Invoke(board, new object[] {side});
    [UnityTest] public IEnumerator GameplayRollRestore([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunGameplayRollRestore").Invoke(board, new object[] {side});
    [UnityTest] public IEnumerator RulesTiming([Values(false,true)] bool paused)
        => (IEnumerator)Bridge.GetMethod("RunRulesTiming").Invoke(board, new object[] {paused});
    [UnityTest] public IEnumerator CpuRollTiming([Values(false,true)] bool cpuFirst)
        => (IEnumerator)Bridge.GetMethod("RunCpuRollTiming").Invoke(board, new object[] {cpuFirst});
    [UnityTest] public IEnumerator DuelManualReview([Values(1,-1)] int side, [Values(false,true)] bool expired)
        => (IEnumerator)Bridge.GetMethod("RunTimingDuelReview").Invoke(board, new object[] {side,expired});
    [UnityTest] public IEnumerator JumperOverflow([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunTimingJumperOverflow").Invoke(board, new object[] {side});
    [UnityTest] public IEnumerator StoppageManualReview([Values(false,true)] bool expired, [Values(false,true)] bool dismiss)
        => (IEnumerator)Bridge.GetMethod("RunTimingStoppageReview").Invoke(board, new object[] {expired,dismiss});
    [UnityTest] public IEnumerator FileTimingRestore([Values(1,-1)] int side, [Values(false,true)] bool expired)
        => (IEnumerator)Bridge.GetMethod("RunTimingFileRestore").Invoke(board, new object[] {side,expired});
    [UnityTest] public IEnumerator InterruptedTimingRestore([Values(1,-1)] int side, [Values(false,true)] bool expired)
        => (IEnumerator)Bridge.GetMethod("RunTimingInterruptedRestore").Invoke(board, new object[] {side,expired});
    [UnityTest] public IEnumerator TargetReopening([Values(1,-1)] int side, [Values("P","C","L","F")] string action, [Values(false,true)] bool expired)
        => (IEnumerator)Bridge.GetMethod("RunTimingTargetReopening").Invoke(board, new object[] {side,action,expired});
    [UnityTest] public IEnumerator KeeperFollowup([Values(1,-1)] int side, [Values("Q","K","feet")] string choice)
        => (IEnumerator)Bridge.GetMethod("RunTimingKeeperFollowup").Invoke(board, new object[] {side,choice});
    [UnityTest] public IEnumerator ShotFollowup([Values(1,-1)] int side, [Values(false,true)] bool snapshot)
        => (IEnumerator)Bridge.GetMethod("RunTimingShotFollowup").Invoke(board, new object[] {side,snapshot});
    [UnityTest] public IEnumerator NonblockingScatter([Values(1,-1)] int side, [Values(false,true)] bool recovery, [Values("shot","duel")] string context)
        => (IEnumerator)Bridge.GetMethod("RunTimingNonblockingScatter").Invoke(board, new object[] {side,recovery,context});
    [UnityTest] public IEnumerator FoulFollowup([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunTimingFoulFollowup").Invoke(board, new object[] {side});
    [UnityTest] public IEnumerator ShootoutReview([Values(1,-1)] int side, [Values("offtarget","keeper","save")] string branch)
        => (IEnumerator)Bridge.GetMethod("RunTimingShootoutReview").Invoke(board, new object[] {side,branch});
}
