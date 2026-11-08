using System.Collections;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunPickupReproduction(int side)
    {
        yield return PrepareHeadedBackPass(side, false);
        var header = match.shotManager.headerManager;
        yield return Until(() => header.isWaitingForHeadedBackPassChoice, "direct header choice");
        LegacyKey("F");
        yield return Until(() => !header.isActivated, "header completes at feet");
        yield return FinishPickupFinalThirds();
        movement.ActivateMovementPhase(); movement.CommitToAction();
        movement.ForfeitTeamMovementPhase();
        movement.ForfeitTeamMovementPhase();
        movement.ForfeitTeamMovementPhase();
        Assert.That(match.finalThirdManager.isActivated, Is.False,
            "eligible keeper must choose pickup/feet before Final Thirds");
        Assert.That(keeper.IsWaitingForPickupChoice, Is.True);
    }

    private void FinishPickupMovement()
    {
        for (int i = 0; i < 4 && movement.isActivated; i++) movement.ForfeitTeamMovementPhase();
        Assert.That(movement.isActivated, Is.False);
    }

    private IEnumerator FinishPickupFinalThirds()
    {
        for (int frame = 0; frame < 600 && match.finalThirdManager.isActivated
            && !match.finalThirdManager.isWaitingForWhatToDo; frame++)
        {
            input.TryExecuteSyntheticKeyPress("Return", "goalkeeper pickup Final Thirds", out _);
            yield return null;
        }
        Assert.That(!match.finalThirdManager.isActivated || match.finalThirdManager.isWaitingForWhatToDo,
            Is.True, "Final Thirds reaches its actual continuation");
    }

    public IEnumerator RunDeferredPickup(int side, string arrival, string choice)
    {
        bool outside = arrival == "outside";
        bool nearby = arrival == "nearby";
        yield return PrepareHeadedBackPass(side, arrival == "corner", outside, nearby);
        var header = match.shotManager.headerManager;
        if (!outside && !nearby)
        {
            yield return Until(() => header.isWaitingForHeadedBackPassChoice, "direct header choice");
            LegacyKey("F");
        }
        yield return Until(() => !header.isActivated, "headed delivery completes");
        yield return FinishPickupFinalThirds();
        EnableForAuthoredPresentation(keeper);
        int before = match.totalCompletedActions;
        movement.ActivateMovementPhase(); movement.CommitToAction();
        if (outside || nearby)
        {
            movement.selectedToken = homeGK;
            movement.remainingDribblerPace = homeGK.pace;
            movement.HighlightValidMovementHexes(homeGK, homeGK.pace);
            var destination = Hex((nearby ? -15 : -13) * side, nearby ? 0 : 1);
            var move = (System.Threading.Tasks.Task)Invoke(movement, "AsyncMoveTokenToHexRegularly", destination);
            yield return Until(() => move.IsCompleted, "keeper moves through real collection/dribble path");
            Assert.That(move.IsFaulted, Is.False, move.Exception?.ToString());
            Assert.That(keeper.IsWaitingForPickupChoice, Is.False, "no mid-phase pickup after collection or box entry");
        }
        FinishPickupMovement();
        Assert.That(match.totalCompletedActions, Is.EqualTo(before + 1));
        Assert.That(keeper.IsWaitingForPickupChoice, Is.True);
        Assert.That(match.finalThirdManager.isActivated, Is.False);
        Assert.That(ball.GetCurrentHex().GetOccupyingToken(), Is.SameAs(homeGK));
        Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(homeGK));
        Assert.That(keeper.IsInstructionExpectingHomeTeam(), Is.True);
        Assert.That(duelPanel.title.text, Does.Contain("Pick Up"));
        Assert.That(duelPanel.singlePlayer.die.gameObject.activeSelf, Is.False);
        Assert.That(duelPanel.singlePlayer.die.transform.parent.gameObject.activeSelf, Is.False);
        Assert.That(keeper.GetInstructions(), Does.Contain("[Q]"));
        Assert.That(keeper.GetInstructions(), Does.Contain("\n[F]"));
        var context = new RoomDecisionContext(); keeper.PopulateRoomDecisionContext(context);
        Assert.That(context.validKeys, Does.Contain("H")); Assert.That(context.validKeys, Does.Contain("F"));
        Assert.That(DuelResolutionPanelController.ConsumeKey(new KeyPressData(KeyCode.R, false, false, false)), Is.True);
        match.BroadcastSafeEndofMovementPhase();
        Assert.That(match.totalCompletedActions, Is.EqualTo(before + 1), "pending choice cannot double-complete");
        if (choice == "reset")
        {
            match.ClearLiveActionAvailabilityForStopPlay(); keeper.ChooseDeferredPickup(true);
            Assert.That(keeper.IsWaitingForPickupChoice || match.shotManager.isWaitingForSaveandHoldScenario, Is.False);
            Assert.That(match.CanGoalkeeperHandle(homeGK), Is.False);
            yield break;
        }
        if (choice == "feet")
        {
            LegacyKey("F");
            Assert.That(match.CanGoalkeeperHandle(homeGK), Is.True);
            Assert.That(movement.isAvailable && ground.isAvailable && high.isAvailable && longBall.isAvailable, Is.True);
            yield return FinishPickupFinalThirds();
            // Save/load the actual touch snapshot using an in-memory JSON round trip.
            var snapshot = Newtonsoft.Json.JsonConvert.DeserializeObject<RoomRuntimeSnapshot>(
                Newtonsoft.Json.JsonConvert.SerializeObject(match.CreateRuntimeSnapshot()));
            match.ResetGoalkeeperHandling();
            Invoke(match, "RestoreTouchReferences", snapshot.touchReferences);
            Assert.That(match.CanGoalkeeperHandle(homeGK), Is.True);
            movement.ActivateMovementPhase(); movement.CommitToAction(); FinishPickupMovement();
            Assert.That(keeper.IsWaitingForPickupChoice, Is.True, "feet permission survives later movement and save/load");
        }
        LegacyKey("H");
        Assert.That(match.shotManager.isWaitingForSaveandHoldScenario, Is.True);
        Assert.That(match.finalThirdManager.isActivated, Is.False);
        Assert.That(match.gameData.stats.GetPlayerStats(homeGK).attemptsSaved, Is.Zero, "pickup is not a save");
        Assert.That(match.totalCompletedActions, Is.EqualTo(before + (choice == "feet" ? 2 : 1)));
        LegacyKey(choice == "Q" ? "Q" : "K");
        if (choice == "Q")
        {
            Assert.That(match.finalThirdManager.isActivated, Is.False);
            Assert.That(ground.isQuickThrow && ground.isActivated, Is.True);
        }
        else
        {
            yield return Until(() => match.finalThirdManager.isActivated, "K begins Save and Hold Final Thirds");
            yield return FinishPickupFinalThirds();
            Assert.That(match.finalThirdManager.isWaitingForWhatToDo, Is.True);
            LegacyKey("D");
            Assert.That(match.CanGoalkeeperHandle(homeGK), Is.False);
            movement.ActivateMovementPhase(); movement.CommitToAction(); FinishPickupMovement();
            Assert.That(keeper.IsWaitingForPickupChoice, Is.False, "deliberate Drop Ball locks out re-pickup");
        }
    }

    public IEnumerator RunLostHeaderTargetCleanup()
    {
        yield return PrepareNativeHeaderAtGoal(true, true, afterKickoff: true);
        var header = match.shotManager.headerManager;
        var target = header.headerAtGoalTarget;
        Assert.That(target.transform.position.y, Is.GreaterThan(0.02f));
        string[] names = { "Yaneva", "Soares", "Kuzmic" };
        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i];
            yield return Until(() => header.isWaitingForHeaderRoll && header.tokenRolling?.playerName == name,
                "headed goal challenge roll " + name);
            header.PerformHeaderRoll(new RollInputOverride { hasOverride = true, roll = i == 0 ? 1 : 6 });
        }
        yield return Until(() => header.headerChallengePanel.outcome.text.Contains("wins"), "defender winner resolves before continuation");
        Assert.That(header.headerAtGoalTarget, Is.Null);
        Assert.That(target.transform.position.y, Is.LessThan(0.02f), "release committed goal target immediately at winner resolution");
        Assert.That(typeof(Ball).GetField("committedGoalTarget", Private).GetValue(ball), Is.Null);
    }

    public IEnumerator RunDirectHeaderCatchPercentage(int side)
    {
        yield return PrepareHeadedBackPass(side, false);
        var header = match.shotManager.headerManager;
        yield return Until(() => header.isWaitingForHeadedBackPassChoice, "direct header choice");
        LegacyKey("H");
        yield return Until(() => header.isWaitingForHeadedBackPassRoll, "ordinary catch roll");
        Assert.That(duelPanel.singlePlayer.modifier.text, Is.EqualTo("Catch 83.3%"));
        Assert.That(duelPanel.singlePlayer.die.transform.parent.gameObject.activeSelf, Is.True,
            "a real catch roll restores the authored dice tile after choices");
    }

    public IEnumerator RunOutsidePhaseDeferral(int side, bool opponent)
    {
        if (opponent)
        {
            Board(side); AttachAuthoredPanels(); EnableForAuthoredPresentation(keeper);
            Subscribe(match.finalThirdManager); Subscribe(match.shotManager);
            Place(homeGK, Hex(-10 * side)); Place(defender, Hex(-7 * side));
            match.ChangePossession(); ball.PlaceAtCell(defender.currentHex); match.SetLastToken(defender);
            match.currentState = MatchManager.GameState.StandardPass;
            match.CommitToAction(MatchManager.MatchActionKind.StandardPass);
            Set(ground, "currentDefenderHex", homeGK.currentHex);
            Invoke(ground, "ResolveGroundInterceptionSuccess", homeGK);
            yield return Until(() => match.currentState == MatchManager.GameState.AnyOtherScenario,
                "opponent pass interception outside box reaches Any Other Scenario");
            Assert.That(match.gameData.stats.GetPlayerStats(homeGK).interceptionsMade, Is.EqualTo(1));
        }
        else
        {
            yield return PrepareHeadedBackPass(side, false, true);
            yield return Until(() => !match.shotManager.headerManager.isActivated, "outside header received at feet");
        }
        yield return FinishPickupFinalThirds();
        EnableForAuthoredPresentation(keeper);
        movement.ActivateMovementPhase(); movement.CommitToAction(); FinishPickupMovement();
        Assert.That(keeper.IsWaitingForPickupChoice, Is.False, "outside-box phase end defers choice");
        Assert.That(match.CanGoalkeeperHandle(homeGK), Is.True);
        yield return FinishPickupFinalThirds();
        movement.ActivateMovementPhase(); movement.CommitToAction();
        movement.selectedToken = homeGK; movement.remainingDribblerPace = homeGK.pace;
        movement.HighlightValidMovementHexes(homeGK, homeGK.pace);
        var move = (System.Threading.Tasks.Task)Invoke(movement, "AsyncMoveTokenToHexRegularly", Hex(-13 * side, 1));
        yield return Until(() => move.IsCompleted, "later movement carries keeper into own box");
        Assert.That(move.IsFaulted, Is.False, move.Exception?.ToString());
        FinishPickupMovement();
        Assert.That(keeper.IsWaitingForPickupChoice, Is.True);
    }
}
