using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunRestartPreparationLegacy(string branch)
    {
        Board(1); Subscribe(match.freeKickManager); Subscribe(match.finalThirdManager);
        var free = match.freeKickManager;
        if (branch == "penalty_no_final_third")
        {
            movement.ResetMovementPhase(); movement.ActivateMovementPhase(); movement.CommitToAction();
            typeof(MovementPhaseManager).GetMethod("EndMovementPhaseForStopPlay", Private, null,
                new[] { typeof(bool) }, null).Invoke(movement, new object[] { false });
            Assert.That(movement.isActivated || match.finalThirdManager.isActivated, Is.False);
            match.penaltyKickManager = Add<PenaltyKickManager>();
            var penalty = match.EnsurePenaltyKickManager(); penalty.StartPenaltyPreparation();
            Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.PenaltyKickerSelect));
            Assert.That(match.finalThirdManager.isActivated, Is.False);
            yield break;
        }
        if (branch == "goal_kick_clearance")
        {
            // Right goal kicks belong to Away. Keep team identity and occupancy
            // consistent instead of only changing the old token's isAttacker flag.
            match.ChangePossession();
            Place(awayGK, Hex(15)); Place(defender, Hex(0)); Place(homeGK, Hex(-15)); Place(attacker, Hex(16, -2));
            ball.PlaceAtCell(Hex(16));
            match.currentState = MatchManager.GameState.WaitingForGoalKickFinalThirds;
            match.finalThirdManager.TriggerOobGoalKickFinalThirds(Hex(16), awayGK);
            yield return Until(() => match.finalThirdManager.isActivated
                && (string)typeof(FinalThirdManager).GetField("currentTeamMoving", Private).GetValue(match.finalThirdManager) == "defense",
                "goal-kick Final Third skips the empty attacking outfield step and waits for defensive box clearance");
            Assert.That(match.finalThirdManager.GetInstructions(), Does.Contain(attacker.playerName));
            Assert.That(match.finalThirdManager.GetInstructions(), Does.Contain("must leave the penalty box"));
            yield break;
        }
        if (branch == "offside_spot")
        {
            var offside = match.EnsureOffsideManager();
            var judged = Hex(12); var interference = Hex(14); var spotDefender = Token("Original spot defender", true, false, 7);
            Place(attacker, judged); Place(spotDefender, Hex(6, 6)); Place(defender, Hex(10, -2));
            var secondDefender = Token("Second-last defender", false, false, 6); Place(secondDefender, Hex(9, 2));
            Place(awayGK, Hex(-16));
            ball.PlaceAtCell(Hex(4)); match.currentState = MatchManager.GameState.EndOfMovementPhase; match.attackHasPossession = false;
            offside.EvaluateAndStore("restart_test", forceReassessment: true);
            Assert.That(offside.IsTokenOffside(attacker), Is.True);
            Place(attacker, interference); Place(spotDefender, judged);
            Assert.That(offside.TryHandleOffsideCollection(attacker, "restart_test_interference", interference), Is.True);
            Assert.That(ball.GetCurrentHex(), Is.SameAs(judged));
            Assert.That(free.isWaitingForKickerSelection, Is.True);
            Assert.That((List<PlayerToken>)typeof(FreeKickManager).GetField("shouldDefMoveTokens", Private).GetValue(free), Does.Contain(spotDefender));
            yield break;
        }
        bool corner = branch != "free_kick_keeper";
        var spot = corner ? Hex(18, 12) : Hex(8);
        var start = corner ? Hex(16, 11) : Hex(0, -8);
        var target = corner ? Hex(15, 11) : Hex(2, -8);
        bool keeperTaker = branch is "corner_keeper" or "free_kick_keeper";
        var selected = keeperTaker ? homeGK : Token("Selected corner taker", true, false, 7);
        Place(selected, start);
        var occupant = branch == "corner_defender" ? defender : attacker;
        if (corner) Place(occupant, spot); else Place(attacker, Hex(8, 1));
        ball.PlaceAtCell(spot);
        if (corner) free.StartFreeKickPreparation(spot);
        else free.StartFreeKickPreparation(restartSpot: spot);
        input.TryExecuteSyntheticHexClick(selected.currentHex, "Legacy migration", out _);
        yield return Until(() => !free.isWaitingForKickerSelection && match.currentState == MatchManager.GameState.FreeKickAttGK,
            "actual restart taker selection and displacement finish before attacking goalkeeper setup");
        if (branch == "corner_defender")
        {
            Assert.That(selected.currentHex, Is.SameAs(spot));
            Assert.That(defender.currentHex, Is.Not.SameAs(spot));
            Assert.That(spot.GetNeighbors(grid), Does.Contain(defender.currentHex));
            Assert.That((List<PlayerToken>)typeof(FreeKickManager).GetField("shouldDefMoveTokens", Private).GetValue(free), Does.Contain(defender));
        }
        else if (corner)
        {
            Assert.That(occupant.currentHex, Is.SameAs(spot));
            Assert.That(selected.currentHex, Is.Not.SameAs(spot));
            Assert.That(spot.GetNeighbors(grid), Does.Contain(selected.currentHex));
        }
        else Assert.That(selected.currentHex, Is.Not.SameAs(start));
        if (!keeperTaker) yield break;
        input.TryExecuteSyntheticHexClick(selected.currentHex, "Legacy migration", out _);
        input.TryExecuteSyntheticHexClick(target, "Legacy migration", out _);
        yield return Until(() => selected.currentHex == target, "selected goalkeeper taker may move while an alternate kicker remains at the restart");
        Assert.That(attacker.currentHex, Is.SameAs(corner ? spot : Hex(8, 1)));
    }
}
