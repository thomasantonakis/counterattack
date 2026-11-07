using System.Collections;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunApprovedAccuracy(string action, bool jackpot, int side)
    {
        Board(side);
        Place(attacker, Hex(0));
        attacker.highPass = 0;
        attacker.dribbling = 0;
        ball.PlaceAtCell(attacker.currentHex);
        match.SetLastToken(attacker);
        AttachAuthoredPanels();
        EnableForAuthoredPresentation(high);
        EnableForAuthoredPresentation(longBall);
        EnableForAuthoredPresentation(match.shotManager.headerManager);
        EnableForAuthoredPresentation(loose);
        Subscribe(match.finalThirdManager);
        var roll = new RollInputOverride { hasOverride = true, roll = 6, isJackpot = jackpot };
        if (action == "control")
        {
            ball.PlaceAtCell(Hex(2 * side));
            var header = match.shotManager.headerManager;
            yield return header.FindEligibleHeaderTokens(ball.GetCurrentHex());
            Assert.That(header.isWaitingForControlOrHeaderDecision, Is.True);
            LegacyKey("B");
            yield return Until(() => header.isWaitingForControlRoll, "real unchallenged landing offers ball control");
            Assert.That(header.GetControlPenaltyForPreview(attacker), Is.EqualTo(-1));
            _ = header.PerformControlRoll(roll);
            if (jackpot) yield return DismissLegacyDuel(false);
            if (jackpot)
            {
                yield return Until(() => ball.GetCurrentHex() == attacker.currentHex && !header.isActivated,
                    "jackpot controls a distance-two ball despite zero Dribbling");
                Assert.That(loose.isActivated, Is.False);
                Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(attacker));
            }
            else yield return Until(() => loose.isWaitingForDirectionRoll, "numeric six cannot control at zero Dribbling with distance penalty");
            yield break;
        }
        var target = Hex((action == "high" ? 5 : 8) * side);
        if (action == "high") Place(Token("High pass receiver", true, false, 8), target);
        match.BroadcastSafeEndofMovementPhase(countMovementAction: false, triggerFinalThird: false);
        LegacyKey(action == "high" ? "C" : "L");
        input.TryExecuteSyntheticHexClick(target, "Approved rule decisions", out _);
        input.TryExecuteSyntheticHexClick(target, "Approved rule decisions", out _);
        if (action == "high")
        {
            yield return Until(() => high.isWaitingForAttackerSelection, "high pass target confirms through real input");
            LegacyKey("X");
            yield return Until(() => high.isWaitingForDefenderSelection, "high pass attacker forfeits free move");
            LegacyKey("X");
            yield return Until(() => high.isWaitingForAccuracyRoll, "high pass defense forfeits free move");
            high.PerformAccuracyRoll(roll);
        }
        else
        {
            yield return Until(() => longBall.isWaitingForAccuracyRoll, "long ball target confirms through real input");
            typeof(LongBallManager).GetMethod("PerformAccuracyRoll", Private, null,
                new[] { typeof(RollInputOverride?) }, null).Invoke(longBall, new object[] { (RollInputOverride?)roll });
        }
        if (jackpot)
        {
            yield return Until(() => duelPanel.IsComplete || high.isWaitingForDirectionRoll || longBall.isWaitingForDirectionRoll,
                "accuracy roll resolves to a result or inaccuracy");
            Assert.That(high.isWaitingForDirectionRoll || longBall.isWaitingForDirectionRoll, Is.False,
                "jackpot is automatic accuracy success even when a numeric six would fail");
            yield return DismissLegacyDuel(false);
        }
        if (jackpot)
        {
            yield return Until(() => ball.GetCurrentHex() == target, "jackpot reaches the intended target with zero High Pass");
            Assert.That(high.isWaitingForDirectionRoll || longBall.isWaitingForDirectionRoll, Is.False);
        }
        else yield return Until(() => action == "high" ? high.isWaitingForDirectionRoll : longBall.isWaitingForDirectionRoll,
            "numeric six fails accuracy with zero High Pass");
    }

    public IEnumerator RunApprovedBookedNoCard()
    {
        yield return PrepareLegacySoaresStealFoul();
        var defender = movement.selectedDefender;
        defender.ReceiveYellowCard();
        movement.PerformLeniencyTest(1);
        yield return Until(() => movement.isWaitingForInjuryRoll, "no-card foul still checks injury");
        movement.PerformInjuryTest(1);
        yield return DismissLegacyDuel(false);
        yield return Until(() => movement.isWaitingForFoulDecision, "existing booking alone must not force the restart");
        Assert.That(defender.isBooked && !defender.isSentOff, Is.True);
        LegacyKey("A");
        yield return Until(() => movement.isWaitingForSnapshotDecision, "advantage continues the real dribble");
        Assert.That(match.penaltyKickManager?.isActivated == true, Is.False);
    }

    public IEnumerator RunApprovedKeeperCards(int cardRoll, int side)
    {
        Board(side);
        Invoke(grid, "CalculateGoalDistanceAndDangerousTackleCaches");
        Place(attacker, Hex(14 * side));
        var behind = attacker.currentHex.GetNeighbors(grid).First(h => h != null
            && attacker.currentHex.IsDangerousTacklingPosition(match.homeTeamDirection, h));
        Place(awayGK, behind);
        attacker.resilience = 6;
        ball.PlaceAtCell(attacker.currentHex);
        match.SetLastToken(attacker);
        match.refereeLeniency = 4;
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement);
        Subscribe(match.finalThirdManager);
        match.BroadcastAnyOtherScenario();
        LegacyKey("M"); LegacyKey("X");
        yield return Until(() => movement.isMovementPhaseDef, "attack forfeit reaches defensive movement");
        input.TryExecuteSyntheticHexClick(awayGK.currentHex, "Approved rule decisions", out _);
        LegacyKey("T");
        yield return Until(() => movement.isWaitingForTackleRoll, "adjacent keeper starts a real tackle");
        Assert.That(movement.GetInstructions(), Does.Contain("normal leniency"));
        movement.PerformTackleDiceRoll(true, 2);
        yield return Until(() => (duelPanel.DribblerOnLeft ? duelPanel.leftPlayer : duelPanel.rightPlayer).die.IsAnimating,
            "keeper foul roll is shown before the dribbler rolls");
        movement.PerformTackleDiceRoll(false, 6);
        yield return Until(() => movement.isWaitingForYellowCardRoll, "from-behind keeper foul reaches card check");
        Assert.That(movement.PendingFoulIsPenalty, Is.True);
        Assert.That(duelPanel.leniency.rule.text, Does.Contain("no card"));
        movement.PerformLeniencyTest(cardRoll);
        Assert.That(awayGK.isSentOff, Is.False, "own-box goalkeeper foul cannot be a straight red");
        Assert.That(awayGK.isBooked, Is.EqualTo(cardRoll == 6));
        yield return Until(() => movement.isWaitingForInjuryRoll, "ordinary keeper leniency continues to injury");
        movement.PerformInjuryTest(1);
        yield return DismissLegacyDuel(false);
        yield return Until(() => movement.isWaitingForFoulDecision, "first keeper card retains penalty/play-on choice");
    }

    public IEnumerator RunApprovedOutsideHeader(int side, bool jackpotTie)
    {
        Board(side);
        Place(attacker, Hex(7 * side)); attacker.heading = 0;
        Place(awayGK, Hex(9 * side)); awayGK.aerial = 6;
        var landing = Hex(8 * side);
        ball.PlaceAtCell(landing);
        var header = match.shotManager.headerManager;
        AttachAuthoredPanels(); EnableForAuthoredPresentation(header);
        yield return header.FindEligibleHeaderTokens(landing);
        yield return Until(() => header.isWaitingForDefenderSelection, "real landing nominates sole attacker then defense");
        header.HandleDefenderHeaderSelection(awayGK);
        header.ConfirmDefenderHeaderSelection();
        yield return Until(() => header.isWaitingForHeaderRoll && header.tokenRolling == attacker, "attacker rolls first");
        header.PerformHeaderRoll(new RollInputOverride { hasOverride = true, roll = jackpotTie ? 6 : 1, isJackpot = jackpotTie });
        yield return Until(() => header.isWaitingForHeaderRoll && header.tokenRolling == awayGK, "outside keeper rolls a normal header");
        Assert.That(header.headerChallengePanel.defenders.First(r => r.token == awayGK).attribute.text, Does.Contain("Heading: 0"));
        header.PerformHeaderRoll(new RollInputOverride { hasOverride = true, roll = 6, isJackpot = jackpotTie });
        Assert.That(header.tokenScores[awayGK].totalScore, Is.EqualTo(jackpotTie ? 50 : 6));
        if (jackpotTie)
        {
            Assert.That(header.tokenScores[attacker].totalScore, Is.EqualTo(50));
            yield return Until(() => loose.isWaitingForDirectionRoll, "two jackpots tie without adding either attribute");
            Assert.That(match.shotManager.isWaitingForSaveandHoldScenario, Is.False);
            yield break;
        }
        yield return Until(() => !header.isWaitingForHeaderRoll && header.headerChallengePanel.outcome.text.Length > 0,
            "keeper wins the contested header");
        LegacyKey("Escape");
        yield return Until(() => header.isWaitingForHeaderTargetSelection, "outside keeper must head away instead of holding");
        Assert.That(match.shotManager.isWaitingForSaveandHoldScenario, Is.False);
        Assert.That(header.challengeWinner, Is.SameAs(awayGK));
        Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
    }

    public IEnumerator RunApprovedKeeperRush(string route, int side)
        => RunApprovedKeeperRushCore(route, side, false);

    public IEnumerator RunOutsideKeeperAndTwoDefenders(int side)
        => RunApprovedKeeperRushCore("outside_stay", side, true);

    private IEnumerator RunApprovedKeeperRushCore(string route, int side, bool twoDefenders)
    {
        Board(side);
        Place(attacker, Hex(0)); attacker.highPass = 6;
        var start = Hex((route == "inside_out" ? 13 : 10) * side);
        var destination = Hex((route == "outside_in" ? 12 : route == "inside_out" ? 11 : route == "outside_stay" ? 10 : 9) * side);
        var landing = Hex((route == "outside_in" ? 11 : route == "inside_out" ? 10 : 8) * side);
        Place(awayGK, start); awayGK.aerial = 6;
        Place(Token("Rush high-pass receiver", true, false, 8), landing);
        if (twoDefenders)
        {
            var neighbors = landing.GetNeighbors(grid).Where(h => h != null && h.GetOccupyingToken() == null
                && h != destination && h != start).Take(2).ToArray();
            Place(defender, neighbors[0]); defender.heading = 0;
            Place(Token("Second outfield defender", false, false, 11), neighbors[1]);
        }
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker);
        AttachAuthoredPanels(); EnableForAuthoredPresentation(high);
        EnableForAuthoredPresentation(match.shotManager.headerManager);
        Subscribe(match.finalThirdManager); Subscribe(movement);
        match.BroadcastSafeEndofMovementPhase(false, false);
        LegacyKey("C");
        input.TryExecuteSyntheticHexClick(landing, "Approved rule decisions", out _);
        input.TryExecuteSyntheticHexClick(landing, "Approved rule decisions", out _);
        yield return Until(() => high.isWaitingForAttackerSelection, "pass commits before free moves");
        LegacyKey("X");
        yield return Until(() => high.isWaitingForDefenderSelection, "attack forfeits its three-hex move");
        LegacyKey("X");
        yield return Until(() => high.isWaitingForAccuracyRoll, "defense forfeits its three-hex move");
        high.PerformAccuracyRoll(6);
        yield return DismissLegacyDuel(false);
        yield return Until(() => high.isWaitingForDefGKChallengeDecision, "outside landing still permits a reachable keeper challenge");
        Assert.That(grid.highlightedHexes, Does.Contain(destination));
        if (destination == start) LegacyKey("G");
        else input.TryExecuteSyntheticHexClick(destination, "Approved rule decisions", out _);
        yield return Until(() => awayGK.currentHex == destination && !high.isActivated, "rush completes and commits the keeper to the challenge");
        yield return ForfeitLegacyFinalThirds();
        var header = match.shotManager.headerManager;
        yield return Until(() => header.isActivated && header.defenderWillJump.Contains(awayGK), "rushing keeper remains nominated");
        Assert.That(header.GetHeaderAttribute(awayGK), Is.EqualTo(route == "outside_in" ? 6 : 0));
        Assert.That(ball.GetCurrentHex(), Is.SameAs(landing));
        if (!twoDefenders) yield break;
        yield return Until(() => header.isWaitingForDefenderSelection, "rushing GK is locked before selecting outfield challengers");
        Assert.That(header.defenderWillJump, Does.Contain(awayGK));
        LegacyKey("A");
        Assert.That(header.defenderWillJump.Count, Is.EqualTo(3), "Select All must include two outfield players plus the outside-box goalkeeper");
        Assert.That(header.defenderWillJump.Count(t => !t.IsGoalKeeper), Is.EqualTo(2));
        yield return Until(() => header.isWaitingForHeaderRoll, "all three nominations enter the real header challenge");
        Assert.That(header.headerChallengePanel.defenders[2].token, Is.SameAs(awayGK), "GK retains the separate authored third row");
        Assert.That(header.headerChallengePanel.defenders[2].attribute.text, Is.EqualTo("Heading: 0"));
        while (header.isWaitingForHeaderRoll)
        {
            var rolling = header.tokenRolling;
            header.PerformHeaderRoll(rolling == awayGK ? 6 : 1);
            if (rolling == awayGK)
            {
                Assert.That(header.tokenScores[awayGK].totalScore, Is.EqualTo(6), "zero Heading cannot receive a further distance penalty");
                break;
            }
            yield return Until(() => header.isWaitingForHeaderRoll, "next nominated player gets their header roll");
        }
        yield return Until(() => header.headerChallengePanel.outcome.text.Length > 0, "outside keeper wins the challenge");
        LegacyKey("Escape");
        yield return Until(() => header.isWaitingForHeaderTargetSelection, "outside keeper heads away after winning");
        Assert.That(match.shotManager.isWaitingForSaveandHoldScenario, Is.False);
    }
}
