using System.Collections;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunNutmegSelectionLegacy(string branch)
    {
        var players = LoadLegacyRoster();
        var dribbler = players["Yaneva"]; var gilbert = players["Gilbert"];
        var paterson = players["Paterson"]; var stewart = players["Stewart"];
        // Independent board at the end of the old shared preparation phase.
        // Moves, forfeits and phase transitions themselves have native coverage.
        Place(players["Nazef"], Hex(8, 6)); Place(paterson, Hex(3, 4));
        bool stationary = branch.StartsWith("stationary/");
        Place(dribbler, stationary ? Hex(4, 4) : Hex(6, 3));
        ball.PlaceAtCell(dribbler.currentHex); match.SetLastToken(dribbler);
        AttachAuthoredPanels(); EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        Subscribe(match.finalThirdManager);
        match.BroadcastSafeEndofMovementPhase(false, false);
        if (stationary) match.difficulty_level = int.Parse(branch.Split('/')[1]);
        LegacyKey("M");
        input.TryExecuteSyntheticHexClick(dribbler.currentHex, "Legacy migration", out _);
        Assert.That(movement.selectedToken, Is.SameAs(dribbler));
        if (stationary)
        {
            Assert.That(movement.isWaitingForNutmegDecisionWithoutMoving, Is.True);
            var origin = dribbler.currentHex;
            var victim = movement.nutmeggableDefenders.First();
            Invoke(movement, "OnHoverReceived", victim, victim.currentHex);
            Invoke(movement, "OnHoverReceived", null, null);
            Assert.That(movement.isWaitingForNutmegDecisionWithoutMoving, Is.True);
            Assert.That(movement.nutmeggableDefenders, Does.Contain(victim));
            if (branch.EndsWith("/key")) LegacyKey("N");
            if (branch.EndsWith("/click") || movement.lookingForNutmegVictim)
                input.TryExecuteSyntheticHexClick(victim.currentHex, "Legacy migration", out _);
            Assert.That(movement.isCommitted, Is.True);
            Assert.That(movement.nutmegVictim, Is.SameAs(victim));
            Assert.That(movement.isWaitingForNutmegDecisionWithoutMoving, Is.False);
            Assert.That(movement.isWaitingForInterceptionDiceRoll || movement.isWaitingForTackleRoll, Is.True);
            Assert.That(dribbler.currentHex, Is.SameAs(origin));
            yield break;
        }
        yield return LegacyDribbleStep(dribbler, Hex(5, 3));
        yield return LegacyDribbleStep(dribbler, Hex(4, 4));
        Assert.That(movement.isWaitingForNutmegDecision, Is.True);
        Assert.That(movement.nutmeggableDefenders, Is.EquivalentTo(new[] { gilbert, paterson, stewart }));
        if (branch == "reject")
        {
            LegacyKey("X");
            Assert.That(movement.isWaitingForInterceptionDiceRoll, Is.True);
            Assert.That(movement.eligibleDefenders, Is.EqualTo(new[] { gilbert, paterson, stewart }));
            yield break;
        }
        LegacyKey("N"); Assert.That(movement.lookingForNutmegVictim, Is.True);
        input.TryExecuteSyntheticHexClick(paterson.currentHex, "Legacy migration", out _);
        Assert.That(movement.nutmegVictim, Is.SameAs(paterson));
        Assert.That(movement.isWaitingForInterceptionDiceRoll, Is.True);
        Assert.That(movement.isWaitingForTackleRoll, Is.False);
        Assert.That(movement.eligibleDefenders, Is.EqualTo(new[] { gilbert, stewart }));
        Assert.That(movement.selectedDefender, Is.SameAs(gilbert));
        movement.StartCoroutine(movement.PerformBallInterceptionDiceRoll(2));
        yield return Until(() => movement.isWaitingForInterceptionDiceRoll && movement.selectedDefender == stewart,
            "failed Gilbert steal advances to Stewart while preserving the nutmeg victim");
        movement.StartCoroutine(movement.PerformBallInterceptionDiceRoll(2));
        yield return Until(() => movement.isWaitingForTackleRoll, "all other steals fail before the reserved nutmeg starts");
        Assert.That(movement.isWaitingForInterceptionDiceRoll, Is.False);
        movement.PerformTackleDiceRoll(true, 2);
        yield return AssertLegacyOutfieldDefenderPresentation();
        movement.PerformTackleDiceRoll(false, new RollInputOverride { hasOverride = true, roll = 6, isJackpot = false });
        yield return DismissLegacyDuel(false);
        yield return Until(() => movement.isWaitingForReposition, "won nutmeg grants its mandatory reposition");
        Assert.That(movement.stunnedTokens, Does.Contain(paterson));
        Assert.That(movement.stunnedTokens.Contains(gilbert) || movement.stunnedTokens.Contains(stewart), Is.False);
    }

    private IEnumerator AssertLegacyOutfieldDefenderPresentation()
    {
        bool left = duelPanel.DribblerOnLeft;
        var dribbler = left ? duelPanel.leftPlayer : duelPanel.rightPlayer;
        var tackler = left ? duelPanel.rightPlayer : duelPanel.leftPlayer;
        Assert.That(left, Is.EqualTo(match.homeTeamDirection == MatchManager.TeamAttackingDirection.LeftToRight));
        Assert.That(dribbler.attribute.text, Does.Contain("xDr"));
        Assert.That(tackler.attribute.text, Does.Contain("xTk"));
        Assert.That(duelPanel.leniency.column.anchorMin.x, Is.EqualTo(left ? .5f : 0f));
        Assert.That(duelPanel.injury.column.anchorMin.x, Is.EqualTo(left ? 0f : .5f));
        yield return Until(() => !tackler.die.IsAnimating && dribbler.die.IsAnimating, "defender roll freezes before dribbler roll");
        Assert.That(movement.GetInstructions(), Does.Contain(dribbler.guidance.text));
        bool paused = match.IsPauseMenuOpen;
        LegacyKey("Escape"); Assert.That(duelPanel.IsOpen && !duelPanel.IsComplete, Is.True);
        Assert.That(match.IsPauseMenuOpen, Is.EqualTo(paused));
        LegacyKey("Return"); Assert.That(duelPanel.IsResultHeld || DuelResolutionPanelController.IsMatchClockHeld, Is.False);
    }
}
