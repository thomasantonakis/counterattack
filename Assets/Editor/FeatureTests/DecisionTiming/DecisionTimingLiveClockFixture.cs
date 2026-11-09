using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunDribblePaceTiming(int side, int difficulty, bool inBox)
    {
        Board(side); match.difficulty_level = difficulty; match.isClockRunning = true;
        Place(attacker, Hex(inBox ? 13 * side : 0)); Place(defender, Hex(-10 * side, 8));
        Place(awayGK, Hex(18 * side, -4)); attacker.pace = 6; attacker.dribbling = 6;
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker);
        match.currentState = MatchManager.GameState.EndOfMovementPhase; match.RefreshAvailableActionsForCurrentState();
        Subscribe(movement); LegacyKey("M"); if (difficulty < 3) movement.CommitToAction();
        match.RefreshDecisionTiming(); var phase = match.DecisionTiming.Current;
        match.AdvanceDecisionTiming(phase.budget / 2 + 1);
        input.TryExecuteSyntheticHexClick(attacker.currentHex, "Pace timing select", out _);
        input.TryExecuteSyntheticHexClick(Hex(inBox ? 14 * side : side, inBox ? 1 : 0), "Pace timing first step", out _);
        yield return Until(() => !movement.isPlayerMoving && movement.isDribblerRunning && movement.isAwaitingHexDestination, "live mid-pace input");
        Assert.That(movement.isWaitingForSnapshotDecision, Is.EqualTo(inBox));
        match.RefreshDecisionTiming(); var choice = match.DecisionTiming.Current;
        Assert.That(choice.kind, Is.EqualTo(DecisionKind.SnapshotOrForfeitPace), "move-or-forfeit needs an allowance even without S");
        Assert.That(choice.id, Is.Not.EqualTo(phase.id));
        float phaseBefore = phase.remaining;
        input.TryExecuteSyntheticHexClick(Hex(-15 * side, 8), "Invalid pace destination", out _);
        match.AdvanceDecisionTiming(choice.budget / 2 - .25f);
        Assert.That(match.DecisionTiming.Current.id, Is.EqualTo(choice.id));
        AssertPaceTimingDisplay(match.DecisionTiming, DecisionTimingConfig.CountdownDisplaySpentFraction <= 0 ? "Snapshot Or Forfeit Pace" : "Attacking Movement");
        match.AdvanceDecisionTiming(.25f);
        AssertPaceTimingDisplay(match.DecisionTiming, "Snapshot Or Forfeit Pace");
        match.AdvanceDecisionTiming(choice.remaining);
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.True);
        Assert.That(phase.remaining, Is.EqualTo(phaseBefore - choice.budget));
        input.TryExecuteSyntheticHexClick(Hex(inBox ? 14 * side : side, inBox ? 2 : 1), "Pace timing next step after expiry", out _);
        yield return Until(() => movement.isPlayerMoving, "expired choice still permits movement");
        match.AdvanceDecisionTiming(1);
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.False, "animation keeps match time live");
        yield return Until(() => !movement.isPlayerMoving && movement.isAwaitingHexDestination, "next pace choice");
        match.RefreshDecisionTiming(); var next = match.DecisionTiming.Current;
        Assert.That(next.kind, Is.EqualTo(DecisionKind.SnapshotOrForfeitPace));
        Assert.That(next.id, Is.Not.EqualTo(choice.id), "a completed pace starts a new logical choice");
        Assert.That(next.remaining, Is.EqualTo(next.budget));
        match.AdvanceDecisionTiming(next.remaining); LegacyKey("X");
        yield return Until(() => !movement.isDribblerRunning && movement.movedTokens.Contains(attacker), "forfeit remains legal after own expiry");
        match.RefreshDecisionTiming();
        Assert.That(match.DecisionTiming.Capture().decisions.Any(d => d.id == next.id && !d.inactive), Is.False);
    }

    private static void AssertPaceTimingDisplay(DecisionTimingService timing, string expected)
    {
        var root = new GameObject("Pace countdown assertion", typeof(RectTransform)); root.SetActive(false);
        try
        {
            var banner = root.AddComponent<MatchEventBannerController>();
            banner.timingVisibility = root.AddComponent<CanvasGroup>();
            banner.timingCaption = new GameObject("Caption", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TMP_Text>();
            banner.timingDetail = new GameObject("Detail", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI)).GetComponent<TMPro.TMP_Text>();
            banner.timingIcon = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            banner.timingCaption.transform.SetParent(root.transform); banner.timingDetail.transform.SetParent(root.transform); banner.timingIcon.transform.SetParent(root.transform);
            banner.SetTimingStatus(timing);
            Assert.That(banner.IsTimingVisible, Is.True);
            Assert.That(banner.timingDetail.text, Does.Contain(expected));
        }
        finally { Object.DestroyImmediate(root); }
    }

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

    public IEnumerator RunLiveTimingSnapshot(int side, bool declare = false)
    {
        yield return PrepareLegacySoaresStealFoul(side); match.isClockRunning=true;
        match.RefreshDecisionTiming(); var parent=match.DecisionTiming.Capture().decisions.Single(d=>d.kind==DecisionKind.AttackingMovement);
        movement.PerformLeniencyTest(1); yield return Until(()=>movement.isWaitingForInjuryRoll,"injury follows card");
        movement.PerformInjuryTest(1); yield return DismissLegacyDuel(false);
        yield return Until(()=>movement.isWaitingForFoulDecision,"advantage choice"); LegacyKey("A");
        yield return Until(()=>movement.isWaitingForSnapshotDecision,"snapshot-or-forfeit pace");
        match.RefreshDecisionTiming(); Assert.That(match.DecisionTiming.Current.kind,Is.EqualTo(DecisionKind.SnapshotOrForfeitPace));
        Assert.That(match.DecisionTiming.Current.id,Is.Not.EqualTo(parent.id));
        float parentBefore=parent.remaining;
        float before=match.DecisionTiming.Current.remaining; match.AdvanceDecisionTiming(3);
        Assert.That(match.DecisionTiming.Current.remaining,Is.EqualTo(before-3));
        Assert.That(match.DecisionTiming.Capture().decisions.Single(d=>d.id==parent.id).remaining,Is.EqualTo(parentBefore-3));
        Assert.That(match.DecisionTiming.HoldsMatchClock,Is.False);
        if (declare)
        {
            var root = new GameObject("Snapshot movement panel", typeof(RectTransform), typeof(CanvasGroup));
            root.SetActive(false);
            try
            {
                var panel = root.AddComponent<MovementPhaseMovedTokensPanel>();
                var slots = Enumerable.Range(0, 6).Select(i => new GameObject("Slot " + i).AddComponent<MovementPhaseMovedTokenSlotView>()).ToArray();
                foreach (var slot in slots) slot.transform.SetParent(root.transform);
                typeof(MovementPhaseMovedTokensPanel).GetField("movementPhaseManager", Private).SetValue(panel, movement);
                typeof(MovementPhaseMovedTokensPanel).GetField("canvasGroup", Private).SetValue(panel, root.GetComponent<CanvasGroup>());
                typeof(MovementPhaseMovedTokensPanel).GetField("attackingMovementSlots", Private).SetValue(panel, slots);
                EnableForAuthoredPresentation(match.shotManager);
                Invoke(grid,"LoadPrecomputedShootingPaths");
                var dribbler = movement.selectedToken;
                LegacyKey("S");
                yield return Until(() => match.shotManager.isWaitingforBlockerSelection, "declared snapshot owns defender input");
                Invoke(panel,"Refresh",true);
                Assert.That(movement.MovedTokenEntries.Any(e => e.token == dribbler),Is.True,"snapshot completes the dribbler");
                Assert.That(slots.Any(s => s.State == MovementPhaseMovedTokenSlotView.PresentationState.Completed),Is.True);
                Assert.That(slots.Any(s => s.State == MovementPhaseMovedTokenSlotView.PresentationState.Active),Is.False,"next mover must not pulse during snapshot resolution");
                // Resolve the actual defender selection and movement; next mover remains unavailable.
                var blocker = match.playerTokenManager.allTokens.First(t => !t.isAttacker && !t.IsGoalKeeper && match.shotManager.GetValidSnapshotBlockerDestinationsForDecisionPreview(t).Count > 0);
                input.TryExecuteSyntheticHexClick(blocker.currentHex,"Snapshot blocker",out _);
                var target = match.shotManager.GetValidSnapshotBlockerDestinationsForDecisionPreview(blocker).First();
                input.TryExecuteSyntheticHexClick(target,"Snapshot blocker move",out _);
                yield return Until(() => match.shotManager.isWaitingForTargetSelection,"snapshot goal-line target");
                Invoke(panel,"Refresh",false);
                Assert.That(slots.Any(s => s.State == MovementPhaseMovedTokenSlotView.PresentationState.Active),Is.False);
            }
            finally { Object.DestroyImmediate(root); }
            yield break;
        }
        LegacyKey("X");
        yield return Until(()=>!movement.isWaitingForSnapshotDecision,"forfeit pace continues the phase");
    }
}
