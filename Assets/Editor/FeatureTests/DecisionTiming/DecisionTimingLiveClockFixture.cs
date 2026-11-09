using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunLiveTimingDifficultyThree(string action)
    {
        Board(1); Place(attacker,Hex(action == "S" ? 8 : 0)); Place(defender,Hex(-10,8)); ball.PlaceAtCell(attacker.currentHex);
        match.SetLastToken(attacker); match.isClockRunning=true; match.difficulty_level=3;
        Invoke(grid,"LoadPrecomputedShootingPaths");
        match.currentState=action == "F" ? MatchManager.GameState.EndOfStandardPass : MatchManager.GameState.EndOfMovementPhase;
        match.RefreshAvailableActionsForCurrentState();
        Subscribe(ground); Subscribe(high); Subscribe(longBall); Subscribe(ftp); Subscribe(movement); Subscribe(match.shotManager);
        match.AdvanceDecisionTiming(2); var selection=match.DecisionTiming.Current;
        Assert.That(selection.kind,Is.EqualTo(DecisionKind.SelectNextAction)); LegacyKey(action);
        match.RefreshDecisionTiming();
        string committed=action == "P" ? "StandardPass" : action == "C" ? "HighPass" : action == "L" ? "LongBall" : action == "F" ? "FirstTimePass" : "Shot";
        Assert.That(match.CreateRuntimeSnapshot().clock.currentCommittedActionKind,Is.EqualTo(committed));
        Assert.That(match.DecisionTiming.Current.id,Is.Not.EqualTo(selection.id));
        Assert.That(match.DecisionTiming.Current.kind,Is.Not.EqualTo(DecisionKind.SelectNextAction));
        Assert.That(match.DecisionTiming.Current.remaining,Is.EqualTo(match.DecisionTiming.Current.budget));
        Assert.That(input.TryExecuteSyntheticKeyPress("M","Committed selection",out _),Is.False);
        yield break;
    }

    public IEnumerator RunLiveTimingPreview(int difficulty, bool expired)
    {
        Board(1); Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex); Place(defender, Hex(-10,8));
        match.SetLastToken(attacker); match.difficulty_level = difficulty; match.isClockRunning = true;
        match.currentState = MatchManager.GameState.EndOfMovementPhase; match.RefreshAvailableActionsForCurrentState();
        Subscribe(ground); Subscribe(high); Subscribe(longBall); Subscribe(movement);
        match.RefreshDecisionTiming(); var selection = match.DecisionTiming.Current;
        Assert.That(selection.kind, Is.EqualTo(DecisionKind.SelectNextAction));
        match.AdvanceDecisionTiming(expired ? selection.remaining : 3);
        foreach (string key in new[] {"P","L","C","P","M"})
        {
            float before = selection.remaining;
            LegacyKey(key); match.AdvanceDecisionTiming(1);
            Assert.That(match.DecisionTiming.Current.id, Is.EqualTo(selection.id), key);
            Assert.That(match.DecisionTiming.Current.remaining, Is.EqualTo(Mathf.Max(0,before - 1)), key);
            Assert.That(match.DecisionTiming.Capture().decisions.Count, Is.EqualTo(1), "no separate preview allowances");
        }
        movement.CommitToAction(); match.RefreshDecisionTiming();
        Assert.That(match.DecisionTiming.Current.kind, Is.EqualTo(DecisionKind.AttackingMovement));
        Assert.That(match.DecisionTiming.Current.remaining, Is.EqualTo(DecisionTimingConfig.GetBudget(DecisionKind.AttackingMovement,difficulty)));
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.False);
        yield break;
    }

    public IEnumerator RunLiveTimingMovement(int difficulty, bool expired)
    {
        Board(1); Place(attacker, Hex(0)); Place(defender, Hex(-10,8)); ball.PlaceAtCell(attacker.currentHex);
        var mover = Token("Live timing mover",true,false,10); Place(mover,Hex(0,4));
        match.SetLastToken(attacker); match.difficulty_level = difficulty; match.isClockRunning = true;
        match.currentState = MatchManager.GameState.EndOfMovementPhase; match.RefreshAvailableActionsForCurrentState(); Subscribe(movement);
        match.RefreshDecisionTiming(); string selectionId = match.DecisionTiming.Current.id;
        LegacyKey("M");
        if (difficulty < 3) movement.CommitToAction();
        Assert.That(movement.isCommitted, Is.True);
        match.RefreshDecisionTiming(); var parent = match.DecisionTiming.Current;
        Assert.That(parent.kind, Is.EqualTo(DecisionKind.AttackingMovement)); Assert.That(parent.id, Is.Not.EqualTo(selectionId));
        match.AdvanceDecisionTiming(expired ? parent.remaining : parent.budget / 2 + 1);
        input.TryExecuteSyntheticHexClick(mover.currentHex, "Live timing select", out _);
        input.TryExecuteSyntheticHexClick(Hex(2,4), "Live timing move", out _);
        yield return Until(() => movement.isPlayerMoving, "actual MP token animation");
        float before = parent.remaining, possession = match.gameData.stats.homeTeamStats.totalPossessionSeconds;
        match.AdvanceDecisionTiming(2); Invoke(match,"AdvanceMatchTime",2f);
        Assert.That(match.DecisionTiming.Current.id, Is.EqualTo(parent.id));
        Assert.That(parent.remaining, Is.EqualTo(Mathf.Max(0,before - 2)));
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.False, "movement does not introduce a hold");
        Assert.That(match.gameData.stats.homeTeamStats.totalPossessionSeconds, Is.GreaterThan(possession));
        yield return Until(() => !movement.isPlayerMoving, "MP token finishes");
        match.RefreshDecisionTiming(); Assert.That(match.DecisionTiming.HoldsMatchClock, Is.EqualTo(expired));
    }

    public IEnumerator RunLiveTimingAccuracy(int side, bool automatic)
    {
        Board(side); Place(attacker,Hex(0)); Place(defender,Hex(-10*side,8)); ball.PlaceAtCell(attacker.currentHex);
        match.SetLastToken(attacker); match.isClockRunning=true; match.difficulty_level=3; attacker.highPass=10;
        AttachAuthoredPanels(); EnableForAuthoredPresentation(longBall);
        match.currentState=MatchManager.GameState.EndOfMovementPhase; match.RefreshAvailableActionsForCurrentState();
        LegacyKey("L"); Invoke(longBall,"OnClickReceived",null,Hex(8*side,3));
        yield return Until(()=>longBall.isWaitingForAccuracyRoll,"real accuracy wait");
        match.AdvanceDecisionTiming(2); float before=match.gameData.stats.homeTeamStats.totalPossessionSeconds;
        Invoke(match,"AdvanceMatchTime",2f);
        Assert.That(match.gameData.stats.homeTeamStats.totalPossessionSeconds,Is.GreaterThan(before));
        Assert.That(match.DecisionTiming.HoldsMatchClock,Is.False);
        int seed=1; for(;seed<100;seed++){Random.InitState(seed);if(helper.DiceRoll()==(2,false))break;} Random.InitState(seed);
        if(automatic) match.AdvanceDecisionTiming(3); else LegacyKey("R");
        yield return Until(()=>duelPanel.IsComplete,"ordinary automatic result display");
        match.RefreshDecisionTiming(); Assert.That(match.DecisionTiming.BlockingResult,Is.False);
        before=match.gameData.stats.homeTeamStats.totalPossessionSeconds; Invoke(match,"AdvanceMatchTime",2f);
        Assert.That(match.gameData.stats.homeTeamStats.totalPossessionSeconds,Is.GreaterThan(before));
        LegacyKey("Return"); match.RefreshDecisionTiming(); Assert.That(match.DecisionTiming.BlockingResult,Is.True);
        before=match.gameData.stats.homeTeamStats.totalPossessionSeconds; Invoke(match,"AdvanceMatchTime",2f);
        Assert.That(match.gameData.stats.homeTeamStats.totalPossessionSeconds,Is.EqualTo(before));
        LegacyKey("Return"); LegacyKey("Escape");
        Assert.That(longBall.isWaitingForDirectionRoll,Is.False);
    }

    public IEnumerator RunLiveTimingSnapshot(int side)
    {
        yield return PrepareLegacySoaresStealFoul(side); match.isClockRunning=true;
        match.RefreshDecisionTiming(); var parent=match.DecisionTiming.Capture().decisions.Single(d=>d.kind==DecisionKind.AttackingMovement);
        movement.PerformLeniencyTest(1); yield return Until(()=>movement.isWaitingForInjuryRoll,"injury follows card");
        movement.PerformInjuryTest(1); yield return DismissLegacyDuel(false);
        yield return Until(()=>movement.isWaitingForFoulDecision,"advantage choice"); LegacyKey("A");
        yield return Until(()=>movement.isWaitingForSnapshotDecision,"snapshot-or-forfeit pace");
        match.RefreshDecisionTiming(); Assert.That(match.DecisionTiming.Current.kind,Is.EqualTo(DecisionKind.AttackingMovement));
        Assert.That(match.DecisionTiming.Current.id,Is.EqualTo(parent.id));
        float before=match.DecisionTiming.Current.remaining; match.AdvanceDecisionTiming(3);
        Assert.That(match.DecisionTiming.Current.remaining,Is.EqualTo(before-3));
        Assert.That(match.DecisionTiming.Capture().decisions.Any(d=>d.kind==DecisionKind.SnapshotOrForfeitPace),Is.False);
        Assert.That(match.DecisionTiming.HoldsMatchClock,Is.False); LegacyKey("X");
        yield return Until(()=>!movement.isWaitingForSnapshotDecision,"forfeit pace continues the phase");
    }
}
