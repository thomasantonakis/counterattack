using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private IEnumerator PrepareHeadedBackPass(int side, bool corner, bool outside = false)
    {
        Board(side);
        Place(attacker, Hex(-13 * side));
        if (outside) Place(homeGK, Hex(-10 * side));
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker);
        AttachAuthoredPanels();
        var header = match.shotManager.headerManager;
        EnableForAuthoredPresentation(header); EnableForAuthoredPresentation(loose);
        Subscribe(match.shotManager); Subscribe(match.finalThirdManager);
        high.isCornerKick = corner;
        yield return header.FindEligibleHeaderTokens(ball.GetCurrentHex());
        Assert.That(header.isWaitingForControlOrHeaderDecision, Is.True);
        LegacyKey("H");
        yield return Until(() => header.isWaitingForHeaderTargetSelection, "free header opens real target selection");
        input.TryExecuteSyntheticHexClick(homeGK.currentHex, "Headed back-pass", out _);
    }

    public IEnumerator RunHeadedBackPass(int side, bool corner, string branch)
    {
        yield return PrepareHeadedBackPass(side, corner);
        var header = match.shotManager.headerManager;
        yield return Until(() => header.isWaitingForHeadedBackPassChoice, "keeper offers hands or feet after direct teammate header");
        Assert.That(duelPanel.title.text, Does.Contain("Hands or Feet"));
        Assert.That(duelPanel.singlePlayer.die.gameObject.activeSelf, Is.False);
        Assert.That(match.finalThirdManager.isActivated, Is.False);
        Assert.That(header.IsInstructionExpectingHomeTeam(), Is.True);
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesAttempted, Is.EqualTo(1));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesCompleted, Is.EqualTo(1));
        var context = new RoomDecisionContext();
        header.PopulateRoomDecisionContext(context);
        Assert.That(context.validKeys, Does.Contain("H"));
        Assert.That(context.validKeys, Does.Contain("F"));
        // The authored overlay must forward only the two choice keys.
        Assert.That(DuelResolutionPanelController.ConsumeKey(new KeyPressData(KeyCode.R, false, false, false)), Is.True);
        Assert.That(DuelResolutionPanelController.ConsumeKey(new KeyPressData(KeyCode.H, false, false, false)), Is.False);
        if (branch == "cancel")
        {
            header.CleanUpHeader();
            header.PerformHeadedBackPassRoll(new RollInputOverride { hasOverride = true, roll = 2 });
            yield return null;
            Assert.That(header.isWaitingForHeadedBackPassRoll || loose.isActivated, Is.False);
            Assert.That(duelPanel.IsOpen, Is.False);
            movement.isWaitingForTackleRoll = true;
            duelPanel.Begin(movement, attacker, defender, false, defender.tackling, 0, 1,
                new ExpectedStatsCalculator.GroundDuelExpectation(), false);
            Assert.That(DuelResolutionPanelController.ConsumeKey(new KeyPressData(KeyCode.R, false, false, false)), Is.False,
                "a canceled choice must not block the next normal duel roll");
            yield break;
        }
        LegacyKey(branch == "feet" ? "F" : "H");
        if (branch == "feet")
        {
            yield return Until(() => !header.isActivated, "feet completes the headed pass without a catch roll");
            Assert.That(match.shotManager.isWaitingForSaveandHoldScenario, Is.False);
            Assert.That(ball.GetCurrentHex(), Is.SameAs(homeGK.currentHex));
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(homeGK));
            yield break;
        }
        yield return Until(() => header.isWaitingForHeadedBackPassRoll, "hands opens a separate catch roll");
        if (branch == "cancel_roll")
        {
            header.CleanUpHeader();
            header.PerformHeadedBackPassRoll(new RollInputOverride { hasOverride = true, roll = 1 });
            yield return null;
            Assert.That(loose.isActivated || match.shotManager.isWaitingForSaveandHoldScenario, Is.False);
            Assert.That(duelPanel.IsOpen, Is.False);
            yield break;
        }
        Assert.That(duelPanel.title.text, Does.Contain("Goalkeeper Catch"));
        Assert.That(duelPanel.singlePlayer.guidance.text, Does.Contain("Roll 1"));
        Assert.That(duelPanel.singlePlayer.die.gameObject.activeSelf, Is.True);
        var roll = new RollInputOverride { hasOverride = true, roll = branch == "spill" ? 1 : 2,
            isJackpot = branch == "star" };
        header.PerformHeadedBackPassRoll(roll);
        if (branch == "spill")
        {
            yield return Until(() => loose.isWaitingForDirectionRoll, "one starts loose ball from the keeper");
            Assert.That(ball.GetCurrentHex(), Is.SameAs(homeGK.currentHex));
            Assert.That(match.shotManager.isWaitingForSaveandHoldScenario, Is.False);
            Assert.That(match.finalThirdManager.isActivated, Is.False);
        }
        else
        {
            yield return DismissLegacyDuel(false);
            yield return Until(() => match.shotManager.isWaitingForSaveandHoldScenario, "two through six enter keeper distribution before final thirds");
            Assert.That(match.finalThirdManager.isActivated, Is.False);
            Assert.That(duelPanel.IsOpen, Is.False);
            Assert.That(match.gameData.stats.GetPlayerStats(homeGK).attemptsSaved, Is.Zero, "catching a teammate header is not a save");
        }
    }

    public IEnumerator RunOutsideHeadedBackPass(int side)
    {
        yield return PrepareHeadedBackPass(side, false, true);
        var header = match.shotManager.headerManager;
        yield return Until(() => !header.isActivated, "outside-box keeper receives only at feet");
        Assert.That(header.isWaitingForHeadedBackPassChoice || header.isWaitingForHeadedBackPassRoll, Is.False);
        Assert.That(match.shotManager.isWaitingForSaveandHoldScenario, Is.False);
        Assert.That(ball.GetCurrentHex(), Is.SameAs(homeGK.currentHex));
    }
}
