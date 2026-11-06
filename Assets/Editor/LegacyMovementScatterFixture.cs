using System.Collections;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunInjuredTackleScatterLegacy(bool intercept)
    {
        yield return PrepareLegacyInjuredDribbler();
        var players = match.playerTokenManager.allTokens.ToDictionary(t => t.playerName);
        var paterson = players["Paterson"];
        Assert.That(movement.movedTokens.Count, Is.EqualTo(1)); Assert.That(movement.attackersMoved, Is.EqualTo(1));
        LegacyKey("X");
        input.TryExecuteSyntheticHexClick(paterson.currentHex, "Legacy migration", out _);
        Assert.That(movement.isWaitingForTackleDecisionWithoutMoving, Is.True);
        Assert.That(movement.isWaitingForTackleDecision, Is.False);
        input.TryExecuteSyntheticHexClick(Hex(5, 3), "Legacy migration", out _);
        yield return Until(() => movement.isWaitingForTackleDecision && !movement.isPlayerMoving, "booked Paterson moves before attempting the second duel");
        Assert.That(movement.isWaitingForTackleDecisionWithoutMoving, Is.False);
        yield return RollLegacyTackleToScatter(5, 4);
        yield return RollLegacyNorthScatter(6);
        yield return Until(() => loose.isWaitingForInterceptionRoll, "tackle deflection offers Stewart's zone interception before the occupied destination");
        Assert.That(loose.potentialInterceptor, Is.SameAs(players["Stewart"]));
        Assert.That(loose.potentialInterceptor, Is.Not.SameAs(paterson));
        loose.PerformInterceptionRoll(intercept ? 6 : 1);
        yield return DismissLegacyDuel(false);
        if (!intercept) Assert.That(!loose.isWaitingForInterceptionRoll || loose.potentialInterceptor != players["McNulty"], Is.True,
            "The token on the ball's actual path recovers directly instead of rolling to intercept itself.");
        var expected = players[intercept ? "Stewart" : "McNulty"];
        yield return Until(() => !loose.isActivated && ball.GetCurrentHex() == expected.currentHex,
            "loose tackle ball finishes on its actual recovering player");
        AssertLegacyMovementAnyOtherRecovery(expected);
    }

    public IEnumerator RunTackleScatterThrowLegacy()
    {
        var players = LoadLegacyRoster(); var yaneva = players["Yaneva"]; var paterson = players["Paterson"];
        var throwIn = CreateLegacyThrowInManager(); var oob = Add<OutOfBoundsManager>();
        oob.ball = ball; oob.hexGrid = grid; oob.freeKickManager = match.freeKickManager;
        oob.throwInManager = throwIn; oob.outOfBoundsPushManager = throwIn.outOfBoundsPushManager;
        oob.playerTokenManager = match.playerTokenManager; oob.finalThirdManager = match.finalThirdManager;
        loose.outOfBoundsManager = oob;
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        LegacyKey("P"); ClickPass(yaneva.currentHex); ClickPass(yaneva.currentHex);
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == yaneva.currentHex, "scatter-to-throw audit receives its kickoff pass");
        yield return ForfeitLegacyFinalThirds(); LegacyKey("M");
        input.TryExecuteSyntheticHexClick(yaneva.currentHex, "Legacy migration", out _);
        foreach (var destination in new[] { Hex(9), Hex(8, 1), Hex(7, 1), Hex(6, 2) })
        {
            yield return LegacyDribbleStep(yaneva, destination);
            Assert.That(movement.isAwaitingTokenSelection, Is.False);
        }
        LegacyKey("X");
        Assert.That(movement.isAwaitingTokenSelection, Is.True);
        Assert.That(movement.movedTokens, Does.Contain(yaneva)); LegacyKey("X");
        input.TryExecuteSyntheticHexClick(paterson.currentHex, "Legacy migration", out _);
        Assert.That(movement.isWaitingForTackleDecisionWithoutMoving || movement.isWaitingForTackleDecision, Is.False);
        input.TryExecuteSyntheticHexClick(Hex(6, 3), "Legacy migration", out _);
        yield return Until(() => movement.isWaitingForTackleDecision && !movement.isPlayerMoving, "Paterson reaches the first scatter duel");
        Assert.That(movement.isWaitingForTackleDecisionWithoutMoving, Is.False);
        Assert.That(movement.movedTokens, Does.Contain(paterson));
        Assert.That(movement.defendersMoved, Is.EqualTo(0)); Assert.That(movement.movedTokens.Count, Is.EqualTo(2));
        yield return RollLegacyTackleToScatter(6, 4); yield return RollLegacyNorthScatter(6);
        yield return Until(() => loose.isWaitingForInterceptionRoll, "McNulty receives the zone roll before the ball hits Kalla");
        Assert.That(loose.potentialInterceptor, Is.SameAs(players["McNulty"]));
        loose.PerformInterceptionRoll(1); yield return DismissLegacyDuel(false);
        yield return Until(() => !loose.isActivated && ball.GetCurrentHex() == players["Kalla"].currentHex,
            "failed McNulty roll lets the path hit Kalla and resumes defensive movement");
        Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(players["Kalla"]));
        Assert.That(match.PreviousTokenToTouchTheBallOnPurpose, Is.Null);
        Assert.That(movement.defendersMoved, Is.EqualTo(1)); Assert.That(movement.movedTokens.Count, Is.EqualTo(2));
        Assert.That(movement.movedTokens, Does.Contain(yaneva)); Assert.That(movement.movedTokens, Does.Contain(paterson));
        Assert.That(movement.isMovementPhaseDef && movement.isAwaitingTokenSelection && !movement.isAwaitingHexDestination, Is.True);
        var stewart = players["Stewart"];
        input.TryExecuteSyntheticHexClick(stewart.currentHex, "Legacy migration", out _);
        Assert.That(movement.isMovementPhaseDef && movement.isAwaitingTokenSelection && movement.isAwaitingHexDestination, Is.True);
        input.TryExecuteSyntheticHexClick(Hex(6, 7), "Legacy migration", out _);
        yield return Until(() => movement.isWaitingForTackleDecision && !movement.isPlayerMoving, "Stewart moves to challenge the new Kalla dribbler");
        Assert.That(movement.isAwaitingHexDestination || movement.isAwaitingTokenSelection, Is.False);
        yield return RollLegacyTackleToScatter(4, 4); yield return RollLegacyNorthScatter(6);
        yield return Until(() => match.currentState == MatchManager.GameState.WaitingForThrowInTaker,
            "second tied tackle scatters north out of bounds and enters throw-in selection");
    }

    public IEnumerator RunMovementScatterBoxLegacy()
    {
        var players = LoadLegacyRoster(); var yaneva = players["Yaneva"]; var soares = players["Soares"];
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        Subscribe(ground); Subscribe(match.finalThirdManager);
        LegacyKey("P"); ClickPass(yaneva.currentHex); ClickPass(yaneva.currentHex);
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == yaneva.currentHex, "box-scatter audit receives the kickoff pass");
        yield return ForfeitLegacyFinalThirds(); LegacyKey("M"); LegacyKey("X");
        input.TryExecuteSyntheticHexClick(soares.currentHex, "Legacy migration", out _);
        input.TryExecuteSyntheticHexClick(Hex(11), "Legacy migration", out _);
        yield return Until(() => movement.isWaitingForTackleDecision && !movement.isPlayerMoving, "Soares moves outside the box next to Yaneva");
        yield return RollLegacyTackleToScatter(6, 4);
        Assert.That(keeper.isActivated, Is.False);
        loose.PerformDirectionRoll(5);
        yield return Until(() => loose.isWaitingForDistanceRoll, "northeast scatter waits for distance before assessing the box entry");
        Assert.That(loose.isWaitingForDirectionRoll, Is.False); loose.PerformDistanceRoll(6);
        yield return Until(() => keeper.isActivated, "resolved loose-ball destination offers the defending goalkeeper hex before flight");
        Assert.That(loose.isActivated, Is.True);
        Assert.That(loose.isWaitingForDirectionRoll || loose.isWaitingForDistanceRoll, Is.False);
        Assert.That(ball.GetCurrentHex(), Is.SameAs(soares.currentHex), "The initial attraction stays outside the box until the keeper decision.");
        input.TryExecuteSyntheticHexClick(Hex(16, -1), "Legacy migration", out _);
        yield return Until(() => !keeper.isActivated && !keeper.IsResolvingMove, "the actual goalkeeper move releases loose-ball flight");
        Assert.That(players["Kuzmic"].currentHex, Is.SameAs(Hex(16, -1)));
        yield return Until(() => !loose.isActivated, "post-roll loose ball completes after the free goalkeeper hex");
        Assert.That(match.clearPreviousOnNextBallCollection, Is.True);
        Assert.That(movement.isMovementPhaseDef, Is.True);
        Assert.That(movement.movedTokens.Count, Is.EqualTo(2)); Assert.That(movement.defendersMoved, Is.EqualTo(1));
        Assert.That(movement.movedTokens, Does.Contain(soares));
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.MovementPhase));
    }

    private IEnumerator RollLegacyTackleToScatter(int defenderRoll, int attackerRoll)
    {
        LegacyKey("T");
        yield return Until(() => movement.isWaitingForTackleRoll && duelPanel.IsOpen, "actual tackle opens its defender-first authored duel");
        movement.PerformTackleDiceRoll(true, defenderRoll); yield return AssertLegacyOutfieldDefenderPresentation();
        movement.PerformTackleDiceRoll(false, attackerRoll);
        yield return Until(() => loose.isActivated && loose.isWaitingForDirectionRoll, "tied tackle hands input directly to loose-ball direction");
    }

    public IEnumerator RunPickupThenMissedScatterLegacy()
    {
        var players = LoadLegacyRoster(); var yaneva = players["Yaneva"]; var paterson = players["Paterson"];
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        Subscribe(ground); Subscribe(match.finalThirdManager);
        LegacyKey("P"); ClickPass(Hex(6)); ClickPass(Hex(6));
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == Hex(6) && movement.isActivated,
            "ground pass to space automatically starts committed attacking movement");
        AssertLegacyHeaderSpaceContinuation();
        input.TryExecuteSyntheticHexClick(yaneva.currentHex, "Legacy migration", out _);
        var ownStats = match.gameData.stats.GetPlayerStats(yaneva);
        float ownX = ownStats.xRecoveries; int ownMade = ownStats.possessionWon;
        LegacyKey("V");
        yield return Until(() => movement.isDribblerRunning && movement.remainingDribblerPace == 2
            && yaneva.currentHex == Hex(6) && !movement.isPlayerMoving, "Yaneva picks up her team's pass with two actual paces remaining");
        Assert.That(ownStats.xRecoveries, Is.EqualTo(ownX).Within(.0001f)); Assert.That(ownStats.possessionWon, Is.EqualTo(ownMade));
        Assert.That(movement.isAwaitingHexDestination && !movement.isAwaitingTokenSelection, Is.True);
        LegacyKey("X"); Assert.That(movement.isAwaitingTokenSelection, Is.True); Assert.That(movement.attackersMoved, Is.EqualTo(1));
        LegacyKey("X"); Assert.That(movement.attackersMoved, Is.EqualTo(4));
        input.TryExecuteSyntheticHexClick(paterson.currentHex, "Legacy migration", out _);
        input.TryExecuteSyntheticHexClick(Hex(6, 1), "Legacy migration", out _);
        yield return Until(() => movement.isWaitingForTackleDecision && !movement.isPlayerMoving, "Paterson approaches the collected pass to tackle Yaneva");
        yield return RollLegacyTackleToScatter(6, 4);
        loose.PerformDirectionRoll(3);
        yield return Until(() => loose.isWaitingForDistanceRoll, "northwest scatter consumes its direction before distance");
        Assert.That(loose.isWaitingForDirectionRoll, Is.False); loose.PerformDistanceRoll(6);
        foreach (var expected in new[] { players["Gilbert"], players["Vladoiu"] })
        {
            yield return Until(() => loose.isWaitingForInterceptionRoll, "northwest loose-ball route offers the next real zone recovery");
            Assert.That(loose.potentialInterceptor, Is.SameAs(expected));
            Assert.That(loose.isWaitingForDirectionRoll || loose.isWaitingForDistanceRoll, Is.False);
            loose.PerformInterceptionRoll(1); yield return DismissLegacyDuel(false);
        }
        yield return Until(() => !loose.isActivated && movement.isMovementPhaseDef && movement.isAwaitingTokenSelection,
            "two failed loose-ball interceptions leave defensive movement ready for another token");
        Assert.That(ball.GetCurrentHex(), Is.SameAs(Hex(0, 4)));
        Assert.That(movement.isAwaitingHexDestination, Is.False);
        Assert.That(movement.defendersMoved, Is.EqualTo(1)); Assert.That(movement.movedTokens.Count, Is.EqualTo(2));
        Assert.That(movement.movedTokens, Does.Contain(yaneva)); Assert.That(movement.movedTokens, Does.Contain(paterson));
        var collector = players["McNulty"];
        input.TryExecuteSyntheticHexClick(collector.currentHex, "Legacy migration", out _); Assert.That(movement.isBallPickable, Is.True);
        input.TryExecuteSyntheticHexClick(players["Marell"].currentHex, "Legacy migration", out _); Assert.That(movement.isBallPickable, Is.False);
        input.TryExecuteSyntheticHexClick(collector.currentHex, "Legacy migration", out _); Assert.That(movement.isBallPickable, Is.True);
        var stats = match.gameData.stats.GetPlayerStats(collector); var teamStats = match.gameData.stats.GetTeamStats(collector.isHomeTeam);
        float x = stats.xRecoveries, teamX = teamStats.totalXRecoveries;
        int made = stats.possessionWon, teamMade = teamStats.totalPossessionWon, attempts = stats.interceptionsAttempted;
        LegacyKey("V");
        yield return Until(() => !movement.isActivated && ball.GetCurrentHex() == collector.currentHex
            && match.LastTokenToTouchTheBallOnPurpose == collector, "defending McNulty's automatic pickup establishes new possession");
        Assert.That(stats.xRecoveries, Is.EqualTo(x + 1).Within(.0001f)); Assert.That(stats.possessionWon, Is.EqualTo(made + 1));
        Assert.That(teamStats.totalXRecoveries, Is.EqualTo(teamX + 1).Within(.0001f)); Assert.That(teamStats.totalPossessionWon, Is.EqualTo(teamMade + 1));
        Assert.That(stats.interceptionsAttempted, Is.EqualTo(attempts), "Automatic pickup cannot invent an interception roll.");
        Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away)); AssertLegacyMovementAnyOtherRecovery(collector);
        Assert.That(match.PreviousTokenToTouchTheBallOnPurpose, Is.Null); Assert.That(match.clearPreviousOnNextBallCollection, Is.False);
    }
    private IEnumerator RollLegacyNorthScatter(int distance)
    {
        loose.PerformDirectionRoll(4);
        yield return Until(() => loose.isWaitingForDistanceRoll, "northward loose-ball direction resolves before distance");
        Assert.That(loose.isWaitingForDirectionRoll, Is.False); loose.PerformDistanceRoll(distance);
    }
    private void AssertLegacyMovementAnyOtherRecovery(PlayerToken recovering)
    {
        Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(recovering));
        Assert.That(match.attackHasPossession, Is.True);
        Assert.That(movement.isAvailable && ground.isAvailable && longBall.isAvailable, Is.True);
        Assert.That(ground.imposedDistance, Is.EqualTo(6));
        Assert.That(ftp.isAvailable || high.isAvailable || movement.isActivated || ground.isActivated
            || ftp.isActivated || high.isActivated || longBall.isActivated, Is.False);
    }
}
