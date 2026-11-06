using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunTackleWinnerStealLegacy(bool stealSucceeds)
    {
        var players = LoadLegacyRoster(); var yaneva = players["Yaneva"]; var nazef = players["Nazef"]; var paterson = players["Paterson"];
        Subscribe(ground); Subscribe(movement); Subscribe(loose); Subscribe(match.finalThirdManager);
        LegacyKey("P"); ClickPass(Hex(7, 3)); ClickPass(Hex(7, 3));
        yield return Until(() => ground.isWaitingForDiceRoll, "ground pass to space offers its actual interception");
        ground.PerformGroundInterceptionDiceRoll(1);
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == Hex(7, 3), "failed interception leaves the hanging ground pass at its target");
        Assert.That(match.hangingPassType, Is.EqualTo("ground"));
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        input.TryExecuteSyntheticHexClick(yaneva.currentHex, "Legacy migration", out _);
        Assert.That(movement.isBallPickable, Is.True); LegacyKey("V");
        yield return Until(() => yaneva.currentHex == Hex(7, 3) && !movement.isPlayerMoving
            && match.LastTokenToTouchTheBallOnPurpose == yaneva && movement.isDribblerRunning,
            "Yaneva collects the hanging pass through the real pickup command");
        Assert.That(match.PreviousTokenToTouchTheBallOnPurpose, Is.SameAs(players["Cafferata"]));
        Assert.That(match.gameData.stats.GetPlayerStats(players["Cafferata"]).passesCompleted, Is.EqualTo(1));
        Assert.That(movement.isWaitingForInterceptionDiceRoll, Is.False);
        LegacyKey("X");
        input.TryExecuteSyntheticHexClick(nazef.currentHex, "Legacy migration", out _);
        input.TryExecuteSyntheticHexClick(Hex(7, 5), "Legacy migration", out _);
        yield return Until(() => nazef.currentHex == Hex(7, 5) && !movement.isPlayerMoving, "Nazef moves into the future tackle-winner recovery zone");
        LegacyKey("X");
        yield return Until(() => movement.isMovementPhaseDef, "attack forfeit hands movement to defense");
        input.TryExecuteSyntheticHexClick(paterson.currentHex, "Legacy migration", out _);
        input.TryExecuteSyntheticHexClick(Hex(7, 2), "Legacy migration", out _);
        yield return Until(() => movement.isWaitingForTackleDecision && !movement.isPlayerMoving, "Paterson moves next to Yaneva and can tackle");
        LegacyKey("T");
        movement.PerformTackleDiceRoll(true, 6);
        yield return Until(() => (duelPanel.DribblerOnLeft ? duelPanel.leftPlayer : duelPanel.rightPlayer).die.IsAnimating,
            "defender result freezes before the attacking tackle roll");
        movement.PerformTackleDiceRoll(false, 2);
        yield return DismissLegacyDuel(false);
        yield return Until(() => movement.isWaitingForReposition, "won tackle exposes the real defender reposition choice");
        Assert.That(movement.repositionHexes, Does.Contain(Hex(7, 4)));
        input.TryExecuteSyntheticHexClick(Hex(7, 4), "Legacy migration", out _);
        yield return Until(() => movement.isWaitingForInterceptionDiceRoll && movement.selectedDefender == nazef,
            "tackle-winner reposition exposes the other team's Nazef steal");
        string prompt = movement.GetInstructions();
        Assert.That(prompt, Does.Contain("Press [R] to roll for interception with Nazef"));
        foreach (string forbidden in new[] { "Click on a Free Hex", "Press [X] to forfeit", "Click on a Reposition Hex" })
            Assert.That(prompt, Does.Not.Contain(forbidden));
        movement.StartCoroutine(movement.PerformBallInterceptionDiceRoll(stealSucceeds ? 6 : 1));
        yield return DismissLegacyDuel(false);
        var expected = stealSucceeds ? nazef : paterson;
        yield return Until(() => !movement.isActivated && match.LastTokenToTouchTheBallOnPurpose == expected
            && ball.GetCurrentHex() == expected.currentHex, "post-tackle recovery attempt reaches its actual possession and phase continuation");
        Assert.That(match.currentState, Is.EqualTo(stealSucceeds ? MatchManager.GameState.AnyOtherScenario : MatchManager.GameState.SuccessfulTackle));
        Assert.That(match.attackHasPossession, Is.True);
        Assert.That(movement.isAvailable && ground.isAvailable && longBall.isAvailable, Is.True);
        Assert.That(high.isAvailable, Is.EqualTo(!stealSucceeds));
        Assert.That(ftp.isAvailable || movement.isActivated || ground.isActivated || high.isActivated || ftp.isActivated || longBall.isActivated, Is.False);
        if (stealSucceeds) Assert.That(ground.imposedDistance, Is.EqualTo(6));
    }

    public IEnumerator RunDifferentDribblerStealLegacy()
    {
        var players = LoadLegacyRoster(); var yaneva = players["Yaneva"]; var paterson = players["Paterson"]; var toothnail = players["Toothnail"];
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        Subscribe(ground); Subscribe(match.finalThirdManager);
        LegacyKey("P"); ClickPass(yaneva.currentHex); ClickPass(yaneva.currentHex);
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == yaneva.currentHex, "standard pass supplies Yaneva before the tied-tackle audit");
        yield return ForfeitLegacyFinalThirds(); LegacyKey("M");
        input.TryExecuteSyntheticHexClick(yaneva.currentHex, "Legacy migration", out _);
        foreach (var target in new[] { Hex(9), Hex(8, 1), Hex(8, 2), Hex(8, 3), Hex(8, 4) })
            yield return LegacyDribbleStep(yaneva, target);
        LegacyKey("X"); LegacyKey("X");
        yield return Until(() => movement.isMovementPhaseDef, "attacking dribble forfeit reaches the defensive section");
        input.TryExecuteSyntheticHexClick(paterson.currentHex, "Legacy migration", out _);
        input.TryExecuteSyntheticHexClick(Hex(8, 5), "Legacy migration", out _);
        yield return Until(() => movement.isWaitingForTackleDecision && !movement.isPlayerMoving, "Paterson moves next to the original dribbler");
        LegacyKey("T"); movement.PerformTackleDiceRoll(true, 5);
        yield return Until(() => (duelPanel.DribblerOnLeft ? duelPanel.leftPlayer : duelPanel.rightPlayer).die.IsAnimating,
            "Paterson result freezes before the original dribbler rolls");
        movement.PerformTackleDiceRoll(false, 3);
        yield return Until(() => loose.isActivated && loose.isWaitingForDirectionRoll, "tied tackle transfers directly to inline scatter controls");
        loose.PerformDirectionRoll(4);
        yield return Until(() => loose.isWaitingForDistanceRoll, "northward tackle scatter resolves its direction");
        loose.PerformDistanceRoll(6);
        yield return Until(() => !loose.isActivated && !duelPanel.IsOpen && ball.GetCurrentHex() == toothnail.currentHex,
            "actual northward scatter stops on Toothnail before its nominal destination");
        LegacyKey("X");
        yield return Until(() => movement.isMovementPhase2f2, "defensive forfeit reaches the new dribbler's two-for-two section");
        input.TryExecuteSyntheticHexClick(toothnail.currentHex, "Legacy migration", out _);
        yield return LegacyDribbleStep(toothnail, Hex(8, 7));
        input.TryExecuteSyntheticHexClick(Hex(8, 6), "Legacy migration", out _);
        yield return Until(() => movement.isWaitingForInterceptionDiceRoll, "different dribbler entering Paterson's zone offers a fresh steal");
        Assert.That(movement.selectedDefender, Is.SameAs(paterson));
    }
}
