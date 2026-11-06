using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunKeeperFoulLegacy(string branch)
    {
        bool dangerous = branch is "danger_feet" or "danger_hands_red";
        bool mirrored = branch is "danger_hands_red" or "normal_feet_yellow";
        bool hands = branch is "hand_win" or "hand_tie" or "normal_hands" or "danger_hands_red";
        bool forcedSecond = branch == "second_injury", stale = branch == "stale_touch";
        int side = mirrored ? -1 : 1;
        var players = LoadLegacyRoster(side); var dribbler = players["Yaneva"]; var gk = players["Kuzmic"];
        var source = Hex(11 * side);
        var challenge = dangerous ? source.GetNeighbors(grid).First(h => h != null
            && source.IsDangerousTacklingPosition(match.homeTeamDirection, h)) : Hex(12 * side);
        foreach (var token in match.playerTokenManager.allTokens.Where(t => t.isPlaying && t != dribbler && t != gk))
            if (token.currentHex.isInFinalThird == side)
                Place(token, grid.cells.Cast<HexCell>().First(h => h != null && !h.isOutOfBounds
                    && h.isInFinalThird == 0 && !h.isAttackOccupied && !h.isDefenseOccupied && h.GetOccupyingToken() == null));
        Place(dribbler, source); Place(gk, Hex((dangerous ? 8 : 13) * side));
        ball.PlaceAtCell(source); match.SetLastToken(dribbler); match.UpdatePossessionAfterPass(source);
        Assert.That(source.IsDangerousTacklingPosition(match.homeTeamDirection, challenge), Is.EqualTo(dangerous));
        Assert.That(keeper.IsGoalkeeperOwnPenaltyHex(gk, source), Is.False);
        match.refereeLeniency = 4;
        if (forcedSecond) dribbler.ReceiveInjury();
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        Subscribe(match.finalThirdManager); Subscribe(match.freeKickManager);
        match.BroadcastAnyOtherScenario(); LegacyKey("M"); LegacyKey("X");
        yield return Until(() => movement.isMovementPhaseDef, "real movement entry and attack forfeit hand the goalkeeper a defender move");
        input.TryExecuteSyntheticHexClick(gk.currentHex, "Legacy migration", out _);
        Assert.That(movement.selectedToken, Is.SameAs(gk));
        input.TryExecuteSyntheticHexClick(challenge, "Legacy migration", out _);
        yield return Until(() => movement.isWaitingForTackleDecision && !movement.isPlayerMoving,
            "goalkeeper moves to the normal or cached dangerous challenge hex");
        Assert.That(movement.selectedDefender, Is.SameAs(gk));
        LegacyKey(hands ? "D" : "T");
        yield return Until(() => movement.isWaitingForTackleRoll && duelPanel.IsOpen, "goalkeeper challenge opens its authored duel");
        if (branch is "hand_win" or "hand_tie")
        {
            movement.PerformTackleDiceRoll(true, new RollInputOverride { hasOverride = true, roll = 6, isJackpot = true });
            yield return Until(() => (duelPanel.DribblerOnLeft ? duelPanel.leftPlayer : duelPanel.rightPlayer).die.IsAnimating,
                "outside handball challenge shows the actual defender roll before dribbler rolls");
            movement.PerformTackleDiceRoll(false, new RollInputOverride { hasOverride = true, roll = 6, isJackpot = branch == "hand_tie" });
            yield return DismissLegacyDuel(false);
            yield return Until(() => match.finalThirdManager.isActivated || match.freeKickManager.isWaitingForKickerSelection,
                "winning or tying outside goalkeeper hand contact forces the free kick");
            Assert.That(gk.isSentOff, Is.True);
            Assert.That(ball.GetCurrentHex(), Is.SameAs(source));
            Assert.That(movement.isWaitingForInjuryRoll || movement.isWaitingForFoulDecision || loose.isActivated
                || match.shotManager.isWaitingForSaveandHoldScenario, Is.False);
            yield break;
        }
        string prompt = movement.GetInstructions();
        Assert.That(prompt, Does.Contain(dangerous ? "harsh leniency" : "normal leniency"));
        var tacklerView = duelPanel.DribblerOnLeft ? duelPanel.rightPlayer : duelPanel.leftPlayer;
        Assert.That(tacklerView.guidance.text, Does.Contain(dangerous ? "Roll 1–2" : "Roll 1 ="));
        movement.PerformTackleDiceRoll(true, dangerous ? 2 : 1);
        yield return Until(() => (duelPanel.DribblerOnLeft ? duelPanel.leftPlayer : duelPanel.rightPlayer).die.IsAnimating,
            "positional foul waits for the dribbler roll before leniency");
        movement.PerformTackleDiceRoll(false, 6);
        yield return Until(() => movement.isWaitingForYellowCardRoll, "actual positional foul enters leniency rather than automatic handball red");
        Assert.That(gk.isSentOff, Is.False);
        Assert.That(movement.GetInstructions(), Does.Contain(dangerous ? "Fail: straight red card" : "Fail: yellow card"));
        Assert.That(movement.GetInstructions(), Does.Contain(dangerous ? "Pass: yellow card" : "Pass: no card"));
        Assert.That(duelPanel.leniency.rule.text, Does.Contain(dangerous ? "straight red" : "no card"));
        var previous = players["Nazef"];
        if (stale)
        {
            previous.ReceiveInjury(); Assert.That(dribbler.isInjured, Is.False);
            match.LastTokenToTouchTheBallOnPurpose = previous;
        }
        bool red = branch == "danger_hands_red";
        bool yellow = branch is "danger_feet" or "normal_feet_yellow";
        movement.PerformLeniencyTest(red || branch == "normal_feet_yellow" ? 6 : 1);
        yield return Until(() => movement.isWaitingForInjuryRoll, "both leniency outcomes continue to an injury check");
        Assert.That(gk.isSentOff, Is.EqualTo(red)); Assert.That(gk.isBooked, Is.EqualTo(yellow));
        movement.PerformInjuryTest(stale || forcedSecond ? 6 : 1);
        if (stale)
        {
            Assert.That(dribbler.isInjured && !dribbler.requiresSubstitution, Is.True);
            Assert.That(previous.requiresSubstitution, Is.False);
            Assert.That(duelPanel.injury.result.text, Does.Not.Contain("SECOND INJURY"));
            match.LastTokenToTouchTheBallOnPurpose = dribbler;
        }
        if (red)
        {
            // Physical input routes dice-modal keys before the pause guard.
            // The synthetic general-purpose API rejects all paused input, so
            // exercise that same actual modal consumer for the forced GK pause.
            yield return Until(() => duelPanel.IsComplete, "red-card foul finishes injury before its result is dismissed");
            Assert.That(match.IsPauseMenuOpen && match.IsGoalkeeperReplacementRequired(gk.isHomeTeam), Is.True);
            Assert.That(DuelResolutionPanelController.ConsumeKey(new KeyPressData(UnityEngine.KeyCode.Escape, false, false, false)), Is.True);
            yield return Until(() => !duelPanel.IsOpen, "physical dice-modal dismissal releases the red-card foul continuation");
        }
        else yield return DismissLegacyDuel(false);
        if (forcedSecond)
        {
            yield return Until(() => dribbler.requiresSubstitution && match.IsPauseMenuOpen,
                "second injury opens mandatory substitution after the result gate");
            Assert.That(movement.isWaitingForFoulDecision, Is.False);
            var incoming = match.playerTokenManager.benchTokens.First(t => t.isHomeTeam && !t.IsGoalKeeper);
            var menu = Add<SubstitutionMenuManager>();
            var rowType = typeof(SubstitutionMenuManager).GetNestedType("SelectionRow", System.Reflection.BindingFlags.NonPublic);
            var row = Activator.CreateInstance(rowType, true);
            rowType.GetField("isHomeTeam").SetValue(row, true);
            rowType.GetField("selectedOutgoing").SetValue(row, dribbler);
            rowType.GetField("selectedIncoming").SetValue(row, incoming);
            ((IList)typeof(SubstitutionMenuManager).GetField("selectionRows", Private).GetValue(menu)).Add(row);
            var destination = dribbler.currentHex; Invoke(menu, "ConfirmSubstitutions");
            Assert.That(dribbler.wasSubbedOff && !dribbler.requiresSubstitution, Is.True);
            Assert.That(incoming.isPlaying && incoming.currentHex == destination, Is.True);
            Assert.That(movement.isWaitingForFoulDecision || movement.isWaitingForReposition, Is.False);
            match.SetPauseMenuOpen(false);
            yield return Until(() => match.finalThirdManager.isActivated || match.freeKickManager.isWaitingForKickerSelection,
                "unpausing mandatory substitution reaches the foul's Final Third gate before forfeit");
            yield return ForfeitLegacyFinalThirds();
            yield return Until(() => match.freeKickManager.isWaitingForKickerSelection && !movement.isActivated,
                "forced substitution continues to the free kick and cannot restore the old dribbler listeners");
            yield break;
        }
        if (!red)
        {
            yield return Until(() => movement.isWaitingForFoulDecision, "ordinary goalkeeper foul retains Play On / Take Foul");
            Assert.That(movement.GetInstructions(), Does.Contain("[A]"));
            Assert.That(movement.GetInstructions(), Does.Contain("[F]"));
            LegacyKey("F");
            yield return Until(() => match.finalThirdManager.isActivated || match.freeKickManager.isWaitingForKickerSelection,
                "taking the goalkeeper foul starts free-kick preparation");
        }
        else
        {
            Assert.That(movement.isWaitingForFoulDecision, Is.False);
            yield return Until(() => match.finalThirdManager.isActivated || match.freeKickManager.isWaitingForKickerSelection,
                "straight-red positional foul forces the restart while goalkeeper replacement is pending");
        }
    }
}
