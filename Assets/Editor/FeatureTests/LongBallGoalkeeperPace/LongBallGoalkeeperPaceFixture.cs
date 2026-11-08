using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private void PrepareLongPace(int side)
    {
        Board(side);
        Place(attacker, Hex(-8 * side, -4)); ball.PlaceAtCell(attacker.currentHex);
        Subscribe(longBall); Subscribe(movement); Subscribe(match.finalThirdManager);
        longBall.ActivateLongBall();
        longBall.isAwaitingTargetSelection = false;
        longBall.CommitToThisAction();
    }

    private void AssertLandingHeld(HexCell landing)
    {
        Assert.That(ball.GetCurrentHex(), Is.SameAs(landing));
        Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(attacker));
        Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Home));
        Assert.That(awayGK.isAttacker, Is.False);
        Assert.That(homeGK.isAttacker, Is.True);
        // Movement clears occupied-hex flags until arrival; GetDefendingGK is
        // an occupancy query, so identity is checked there only when stationary.
        if (!movement.isPlayerMoving) Assert.That(grid.GetDefendingGK(), Is.SameAs(awayGK));
        Assert.That(match.attackHasPossession, Is.True, "No possession update before Pace completes.");
        if (longBall.isWaitingForDefLBMove)
            Assert.That(longBall.IsInstructionExpectingHomeTeam(), Is.False);
        Assert.That(LongInterceptionPending, Is.False, "No landing recovery prompt may overtake Pace.");
        Assert.That(match.totalCompletedActions, Is.Zero);
        Assert.That(match.finalThirdManager.isActivated || movement.isActivated || ground.isAvailable
            || ftp.isAvailable || high.isAvailable || longBall.isAvailable, Is.False);
        Assert.That(match.gameData.stats.GetPlayerStats(defender).possessionWon, Is.Zero);
        Assert.That(match.gameData.stats.GetPlayerStats(defender).interceptionsAttempted, Is.Zero);
    }

    public IEnumerator RunLongPaceLanding(int side, string recipient, bool move)
    {
        PrepareLongPace(side);
        var landing = Hex(2 * side, -4);
        if (recipient == "direct")
        {
            Place(defender, landing);
            Place(Token("Adjacent defender", false, false, 7), landing.GetNeighbors(grid).First(h => h != null));
        }
        if (recipient == "zoi") Place(defender, landing.GetNeighbors(grid).First(h => h != null));
        if (recipient == "teammate") Place(Token("Long receiver", true, false, 10), landing);
        longBall.StartCoroutine(longBall.HandleLongBallMovement(landing));
        yield return Until(() => longBall.isWaitingForDefLBMove || !longBall.isActivated || LongInterceptionPending,
            "landing reaches defensive response");
        Assert.That(longBall.isWaitingForDefLBMove, Is.True, "Pace must precede direct recovery and ZOI recovery.");
        AssertLandingHeld(landing);
        var origin = awayGK.currentHex;
        if (move)
        {
            var destination = grid.highlightedHexes.First(h => h != origin);
            Assert.That(input.TryExecuteSyntheticHexClick(destination, "Long Ball Pace", out string error), Is.True, error);
            // A second click/decline during the asynchronous move must not release
            // landing resolution before the goalkeeper reaches the destination.
            Invoke(longBall, "OnKeyReceived", new KeyPressData(KeyCode.X, false, false, false));
            Assert.That(longBall.isWaitingForDefLBMove, Is.True);
            AssertLandingHeld(landing);
            yield return Until(() => !longBall.isWaitingForDefLBMove, "Pace movement finishes");
            Assert.That(awayGK.currentHex, Is.SameAs(destination));
            Assert.That(grid.GetDefendingGK(), Is.SameAs(awayGK));
            Assert.That(homeGK.currentHex, Is.SameAs(Hex(-16 * side)));
        }
        else LegacyKey("X");
        if (recipient == "zoi")
        {
            yield return Until(() => LongInterceptionPending, "recovery offered only after Pace");
            var flow = (IEnumerator)typeof(LongBallManager).GetMethod("PerformInterceptionCheck", Private, null,
                new[] { typeof(HexCell), typeof(int?) }, null).Invoke(longBall, new object[] { landing, (int?)6 });
            longBall.StartCoroutine(flow);
        }
        yield return Until(() => !longBall.isActivated, "landing completes once");
        Assert.That(longBall.isWaitingForDefLBMove, Is.False);
        Assert.That(match.totalCompletedActions, Is.EqualTo(1));
        if (recipient is "direct" or "zoi")
        {
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(defender));
            Assert.That(ball.GetCurrentHex(), Is.SameAs(defender.currentHex));
            Assert.That(ground.isAvailable && movement.isAvailable && longBall.isAvailable, Is.True);
            var stats = match.gameData.stats.GetPlayerStats(defender);
            Assert.That(stats.possessionWon, Is.EqualTo(1));
            Assert.That(stats.interceptionsAttempted, Is.EqualTo(recipient == "zoi" ? 1 : 0));
            Assert.That(stats.interceptionsMade, Is.EqualTo(recipient == "zoi" ? 1 : 0));
        }
        else
        {
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Home));
            Assert.That(ball.GetCurrentHex(), Is.SameAs(landing));
            Assert.That(match.attackHasPossession, Is.EqualTo(recipient == "teammate"));
            Assert.That(match.gameData.stats.GetPlayerStats(attacker).aerialPassesCompleted,
                Is.EqualTo(recipient == "teammate" ? 1 : 0));
        }
        yield return null;
        Assert.That(match.totalCompletedActions, Is.EqualTo(1));
    }
    public IEnumerator RunLongPaceBox(int side, bool entryMove, bool paceMove)
    {
        PrepareLongPace(side);
        var landing = Hex(12 * side);
        Place(defender, landing);
        longBall.StartCoroutine(longBall.HandleLongBallMovement(landing));
        yield return Until(() => keeper.isActivated, "box-entry offer precedes Pace");
        Assert.That(longBall.isWaitingForDefLBMove, Is.False);
        AssertLandingHeld(landing);
        yield return Choose(awayGK, entryMove);
        yield return Until(() => longBall.isWaitingForDefLBMove, "separate Pace offer survives entry move or decline");
        AssertLandingHeld(landing);
        if (paceMove)
        {
            var destination = grid.highlightedHexes.First(h => h != landing);
            input.TryExecuteSyntheticHexClick(destination, "Long Ball Pace", out _);
        }
        else LegacyKey("X");
        yield return Until(() => !longBall.isActivated, "direct recovery after both responses");
        Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(defender));
        Assert.That(ball.GetCurrentHex(), Is.SameAs(landing));
        Assert.That(match.totalCompletedActions, Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(defender).possessionWon, Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(defender).interceptionsAttempted, Is.Zero);
    }

    public IEnumerator RunLongPaceRecheck(int side, string branch)
    {
        PrepareLongPace(side);
        var landing = Hex((branch == "claim_inside" ? 14 : 10) * side);
        Place(awayGK, Hex((branch == "leave_zoi" ? 11 : 16) * side));
        awayGK.pace = 8;
        longBall.StartCoroutine(longBall.HandleLongBallMovement(landing));
        if (branch == "claim_inside")
        {
            yield return Until(() => keeper.isActivated, "claim box entry");
            LegacyKey("X");
        }
        yield return Until(() => longBall.isWaitingForDefLBMove, "Pace before eligibility recheck");
        AssertLandingHeld(landing);
        var context = new RoomDecisionContext(); longBall.PopulateRoomDecisionContext(context);
        if (branch.StartsWith("claim")) LegacyKey("V");
        else
        {
            var destination = Hex((branch == "leave_zoi" ? 15 : 11) * side);
            Assert.That(grid.highlightedHexes, Does.Contain(destination));
            input.TryExecuteSyntheticHexClick(destination, "Long Ball Pace", out _);
        }
        if (branch == "enter_zoi")
        {
            yield return Until(() => LongInterceptionPending, "repositioned keeper now has landing ZOI");
            var candidates = (System.Collections.Generic.List<HexCell>)typeof(LongBallManager)
                .GetField("interceptingDefenders", Private).GetValue(longBall);
            Assert.That(candidates, Is.EquivalentTo(new[] {awayGK.currentHex}));
            var flow = (IEnumerator)typeof(LongBallManager).GetMethod("PerformInterceptionCheck", Private, null,
                new[] {typeof(HexCell), typeof(int?)}, null).Invoke(longBall, new object[] {landing,(int?)1});
            longBall.StartCoroutine(flow);
        }
        yield return Until(() => !longBall.isActivated, "updated landing resolves");
        Assert.That(match.totalCompletedActions, Is.EqualTo(1));
        Assert.That(longBall.isWaitingForDefLBMove, Is.False, "a missed recovery must not offer Pace twice");
        if (branch.StartsWith("claim"))
        {
            Assert.That(ball.GetCurrentHex(), Is.SameAs(awayGK.currentHex));
            Assert.That(awayGK.currentHex, Is.SameAs(landing));
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(awayGK));
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
            Assert.That(match.shotManager.isWaitingForSaveandHoldScenario, Is.EqualTo(branch == "claim_inside"));
            Assert.That(match.gameData.stats.GetPlayerStats(awayGK).possessionWon, Is.EqualTo(1));
            Assert.That(match.gameData.stats.GetPlayerStats(awayGK).xRecoveries, Is.EqualTo(1f));
        }
        else
        {
            Assert.That(ball.GetCurrentHex(), Is.SameAs(landing));
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Home));
            Assert.That(match.attackHasPossession, Is.False);
            Assert.That(match.gameData.stats.GetPlayerStats(awayGK).interceptionsAttempted,
                Is.EqualTo(branch == "enter_zoi" ? 1 : 0));
        }
    }

    public IEnumerator RunLongPaceDelivery(int side, bool inaccurate, bool teammate)
    {
        PrepareLongPace(side);
        attacker.highPass = 6;
        Place(defender, Hex(11 * side, 10)); // Keep intended receivers onside.
        var intended = Hex(6 * side,-7);
        var landing = inaccurate ? Hex(5 * side,-8) : intended;
        if (teammate) Place(Token("Long receiver",true,false,10),landing);
        // Confirm through the real commit/statistics/offside entry point. Deliberately
        // inaccurate passes can subsequently land on recipients forbidden as targets.
        Invoke(longBall,"ConfirmLongBallTargetSelection",intended);
        LongRoll("PerformAccuracyRoll",inaccurate ? 1 : 6);
        if (inaccurate)
        {
            yield return Until(() => longBall.isWaitingForDirectionRoll,"scatter direction");
            LongRoll("PerformDirectionRoll",side == 1 ? 2 : 6);
            yield return Until(() => longBall.isWaitingForDistanceRoll,"scatter distance");
            LongRoll("PerformDistanceRoll",1);
        }
        yield return Until(() => longBall.isWaitingForDefLBMove,"delivery waits for Pace");
        AssertLandingHeld(landing);
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).aerialPassesAttempted,Is.EqualTo(1));
        LegacyKey("X");
        yield return Until(() => !longBall.isActivated,"delivery continuation");
        Assert.That(ball.GetCurrentHex(),Is.SameAs(landing));
        Assert.That(match.attackHasPossession,Is.EqualTo(teammate));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).aerialPassesCompleted,Is.EqualTo(teammate ? 1 : 0));
        Assert.That(match.totalCompletedActions,Is.EqualTo(1));
        yield return ForfeitLegacyFinalThirds();
        Assert.That(movement.isActivated || movement.isAvailable,Is.True);
        Assert.That(ground.isAvailable || ftp.isAvailable || high.isAvailable || longBall.isAvailable,Is.False);
    }

    public IEnumerator RunLongPaceBoundary(int side, string branch)
    {
        PrepareLongPace(side);
        var landing = Hex(2 * side,-4);
        Place(defender,landing);
        if (branch == "zero") awayGK.pace = 0;
        if (branch == "blocked")
            foreach (var h in awayGK.currentHex.GetNeighbors(grid).Where(h => h != null && !h.isOutOfBounds))
                Place(Token("Blocked defender " + h.coordinates,false,false,20 + h.coordinates.z),h);
        bool done = false;
        longBall.StartCoroutine(Observe(longBall.HandleLongBallMovement(landing),() => done = true));
        if (branch is "zero" or "blocked")
        {
            yield return Until(() => done,"no legal Pace destination exits cleanly");
            Assert.That(longBall.isWaitingForDefLBMove,Is.False);
            Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(defender));
            Assert.That(match.totalCompletedActions,Is.EqualTo(1));
            yield break;
        }
        if (branch == "flight") yield return null;
        else
        {
            yield return Until(() => longBall.isWaitingForDefLBMove,"response before reset");
            AssertLandingHeld(landing);
        }
        longBall.CleanUpLongBall();
        if (branch == "new_action")
        {
            longBall.ActivateLongBall();
            longBall.isAwaitingTargetSelection = false;
        }
        for (int i=0;i<150;i++) yield return null;
        Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(attacker));
        Assert.That(match.teamInAttack,Is.EqualTo(MatchManager.TeamInAttack.Home));
        Assert.That(match.totalCompletedActions,Is.Zero);
        Assert.That(LongInterceptionPending || longBall.isWaitingForDefLBMove || match.finalThirdManager.isActivated,Is.False);
        Assert.That(match.gameData.stats.GetPlayerStats(defender).possessionWon,Is.Zero);
    }

    public IEnumerator RunLongPaceStoppage(int side, string branch)
    {
        PrepareLongPace(side);
        var landing = Hex(2 * side,-4);
        if (branch == "offside")
        {
            var receiver = Token("Offside receiver",true,false,10);
            landing = Hex(10 * side); Place(receiver,landing);
            Place(defender,Hex(5 * side,5));
            var offside = match.EnsureOffsideManager();
            offside.EvaluateAndStore("Long Ball Pace fixture",true);
            Assert.That(offside.IsTokenOffside(receiver),Is.True);
        }
        else if (branch == "oob")
        {
            var oob = Add<OutOfBoundsManager>();
            var throwIn = CreateLegacyThrowInManager();
            oob.ball=ball; oob.hexGrid=grid; oob.freeKickManager=match.freeKickManager;
            oob.throwInManager=throwIn; oob.playerTokenManager=match.playerTokenManager;
            oob.finalThirdManager=match.finalThirdManager;
            longBall.outOfBoundsManager=oob;
            longBall.currentTargetHex=Hex(2 * side,-12);
            Set(longBall,"directionIndex",0);
            landing=Hex(2 * side,-14);
            Assert.That(landing.isOutOfBounds,Is.True);
        }
        else Place(defender,landing);
        bool done=false;
        longBall.StartCoroutine(Observe(longBall.HandleLongBallMovement(landing),() => done=true));
        if (branch is "oob" or "offside")
        {
            yield return Until(() => done,"stoppage without a Pace response");
            if (branch == "oob")
                yield return Until(() => match.currentState == MatchManager.GameState.WaitingForThrowInTaker,"OOB throw-in awarded");
            else Assert.That(match.freeKickManager.isWaitingForKickerSelection,Is.True);
            Assert.That(longBall.isWaitingForDefLBMove || LongInterceptionPending,Is.False);
            Assert.That(match.teamInAttack,Is.EqualTo(MatchManager.TeamInAttack.Away));
            Assert.That(match.totalCompletedActions,Is.Zero);
            yield break;
        }
        yield return Until(() => longBall.isWaitingForDefLBMove,"Pace precedes the action clock gate");
        AssertLandingHeld(landing);
        if (branch == "half_gate")
        {
            match.isHalfExpired=true; match.extraActionsDetermined=false;
            LegacyKey("X");
            yield return Until(() => !longBall.isActivated,"action resolves into the stoppage roll gate");
            Assert.That(match.totalCompletedActions,Is.EqualTo(1));
            Assert.That(match.IsWaitingForExtraActionsRoll,Is.True);
            Assert.That(match.finalThirdManager.isActivated || ground.isAvailable || movement.isAvailable,Is.False);
        }
        else
        {
            match.currentState = branch == "ended" ? MatchManager.GameState.MatchEnded : MatchManager.GameState.HalfTime;
            LegacyKey("X");
            yield return Until(() => done,"ended action cannot recover after Pace");
            Assert.That(match.totalCompletedActions,Is.Zero);
            Assert.That(match.teamInAttack,Is.EqualTo(MatchManager.TeamInAttack.Home));
            Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(attacker));
            Assert.That(match.finalThirdManager.isActivated,Is.False);
        }
    }

    public IEnumerator RunLongPaceRollCancel(int side)
    {
        PrepareLongPace(side); AttachAuthoredPanels();
        var landing=Hex(2 * side,-4);
        Place(defender,landing.GetNeighbors(grid).First(h => h != null));
        longBall.StartCoroutine(longBall.HandleLongBallMovement(landing));
        yield return Until(() => longBall.isWaitingForDefLBMove,"Pace before recovery presentation");
        AssertLandingHeld(landing); LegacyKey("X");
        yield return Until(() => LongInterceptionPending,"post-Pace recovery prompt");
        var flow=(IEnumerator)typeof(LongBallManager).GetMethod("PerformInterceptionCheck",Private,null,
            new[] {typeof(HexCell),typeof(int?)},null).Invoke(longBall,new object[] {landing,(int?)6});
        longBall.StartCoroutine(flow);
        yield return null;
        Assert.That(match.teamInAttack,Is.EqualTo(MatchManager.TeamInAttack.Home));
        longBall.CleanUpLongBall();
        yield return null;
        Assert.That(duelPanel.IsOpen,Is.False);
        Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(attacker));
        Assert.That(match.totalCompletedActions,Is.Zero);
        Assert.That(match.gameData.stats.GetPlayerStats(defender).possessionWon,Is.Zero);
    }

}
