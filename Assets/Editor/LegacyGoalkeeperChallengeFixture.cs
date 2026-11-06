using System.Collections;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunGoalkeeperChallengeLegacy(string scenario)
    {
        int side = scenario.StartsWith("Left/") ? -1 : 1;
        string branch = scenario.Contains("/") ? scenario.Split('/')[1] : scenario;
        bool hands = branch == "lost_dive";
        Board(side); AttachAuthoredPanels();
        EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        Subscribe(match.finalThirdManager);
        match.BroadcastAnyOtherScenario();
        LegacyKey("M"); LegacyKey("X");
        yield return Until(() => movement.isMovementPhaseDef, "real movement entry and attack forfeit establish the defender turn");
        input.TryExecuteSyntheticHexClick(awayGK.currentHex, "Legacy migration", out _);
        Assert.That(movement.selectedToken, Is.SameAs(awayGK));
        input.TryExecuteSyntheticHexClick(Hex(12 * side), "Legacy migration", out _);
        yield return Until(() => movement.isWaitingForTackleDecision && !movement.isPlayerMoving, "outside-box goalkeeper challenge choice");
        Assert.That(movement.selectedDefender, Is.SameAs(awayGK));
        string prompt = movement.GetInstructions();
        foreach (string choice in new[] { "[D]", "[T]", "[N]", "Tackling 0" })
            Assert.That(prompt, Does.Contain(choice));
        if (branch == "prompt") yield break;
        LegacyKey(hands ? "D" : "T");
        yield return Until(() => movement.isWaitingForTackleRoll && duelPanel.IsOpen, "authored goalkeeper duel opens its first roll");
        string rollPrompt = movement.GetInstructions();
        Assert.That(rollPrompt, Does.Contain("1 is a foul"));
        Assert.That(rollPrompt, Does.Not.Contain("1 is a penalty"));
        if (!hands) Assert.That(rollPrompt, Does.Contain("Tackling: 0"));
        bool left = side == 1;
        var dribblerView = left ? duelPanel.leftPlayer : duelPanel.rightPlayer;
        var tacklerView = left ? duelPanel.rightPlayer : duelPanel.leftPlayer;
        Assert.That(duelPanel.DribblerOnLeft, Is.EqualTo(left));
        if (hands)
        {
            // 42982cc intentionally distinguishes GK dive probabilities from
            // outfield xDr/xTk tie weighting; preserve that current copy contract.
            Assert.That(dribblerView.attribute.text, Does.Contain("Duel win"));
            Assert.That(tacklerView.attribute.text, Does.Contain("Duel win"));
        }
        else
        {
            Assert.That(dribblerView.attribute.text, Does.Contain("xDr"));
            Assert.That(tacklerView.attribute.text, Does.Contain("xTk"));
        }
        Assert.That(duelPanel.leniency.column.anchorMin.x, Is.EqualTo(left ? .5f : 0f));
        Assert.That(duelPanel.injury.column.anchorMin.x, Is.EqualTo(left ? 0f : .5f));
        movement.PerformTackleDiceRoll(true, branch is "foot_tie" or "foot_win" ? 6 : 2);
        yield return Until(() => !tacklerView.die.IsAnimating && dribblerView.die.IsAnimating,
            "goalkeeper roll freezes before the dribbler animation starts");
        Assert.That(movement.GetInstructions(), Does.Contain(dribblerView.guidance.text));
        bool paused = match.IsPauseMenuOpen;
        LegacyKey("Escape");
        Assert.That(duelPanel.IsOpen && !duelPanel.IsComplete, Is.True);
        Assert.That(match.IsPauseMenuOpen, Is.EqualTo(paused));
        LegacyKey("Return");
        Assert.That(duelPanel.IsResultHeld || DuelResolutionPanelController.IsMatchClockHeld, Is.False);
        if (branch is "foot_tie" or "foot_win") movement.PerformTackleDiceRoll(false, branch == "foot_tie" ? 2 : 1);
        else movement.PerformTackleDiceRoll(false, new RollInputOverride { hasOverride = true, roll = 6, isJackpot = true });
        if (branch == "foot_tie")
        {
            // Ties transfer the authored duel into its inline scatter controls;
            // they do not stop on a completed-result countdown first.
            yield return Until(() => loose.isActivated && loose.isWaitingForDirectionRoll,
                "zero-tackling foot duel tie transfers directly into loose-ball direction selection");
            Assert.That(duelPanel.IsOpen, Is.True);
            Assert.That(awayGK.isSentOff || match.freeKickManager.isActivated, Is.False);
            yield break;
        }
        yield return DismissLegacyDuel(scenario == "Right/lost_foot");
        if (branch == "foot_win")
        {
            yield return Until(() => !movement.isWaitingForTackleRoll && ball.GetCurrentHex() == awayGK.currentHex,
                "outside goalkeeper foot win follows normal tackle recovery");
            Assert.That(awayGK.isSentOff || match.freeKickManager.isActivated || match.freeKickManager.isWaitingForKickerSelection
                || movement.isWaitingForInjuryRoll || movement.isWaitingForFoulDecision, Is.False);
            if (movement.isWaitingForReposition)
            {
                LegacyKey("X");
                yield return Until(() => !movement.isWaitingForReposition, "normal foot-win reposition can be skipped");
            }
        }
        else
        {
            yield return Until(() => movement.isWaitingForReposition, "attacker wins and receives the real reposition choice");
            Assert.That(keeper.isActivated, Is.False);
            var destination = Hex(12 * side, 1);
            Assert.That(movement.repositionHexes, Does.Contain(destination));
            input.TryExecuteSyntheticHexClick(destination, "Legacy migration", out _);
            yield return Until(() => keeper.isActivated, "beaten goalkeeper retains its box-entry move after reposition");
            Assert.That(attacker.currentHex, Is.SameAs(destination));
            Assert.That(ball.GetCurrentHex(), Is.SameAs(destination));
            Assert.That(keeper.GetCurrentDefendingGoalkeeper(), Is.SameAs(awayGK));
            LegacyKey("X");
            yield return Until(() => !keeper.isActivated && !movement.isWaitingForReposition,
                "skipping the box move resumes the duel reposition continuation");
        }
    }

    private IEnumerator DismissLegacyDuel(bool automatically)
    {
        yield return Until(() => duelPanel.IsComplete, "all required duel rolls complete before the result gate");
        Assert.That(movement.isWaitingForReposition || movement.isWaitingForFoulDecision, Is.False);
        LegacyKey("A"); LegacyKey("F");
        Assert.That(duelPanel.IsOpen && duelPanel.IsComplete, Is.True, "Unrelated keys must not bypass the result gate.");
        if (automatically)
        {
            LegacyKey("Return");
            Assert.That(duelPanel.IsResultHeld && DuelResolutionPanelController.IsMatchClockHeld, Is.True);
            string footer = duelPanel.footer.text;
            var clock = typeof(MatchManager).GetField("currentHalfRegulationSeconds", Private);
            float before = (float)clock.GetValue(match);
            Assert.That(match.isClockRunning && !match.isHalfExpired, Is.True);
            float horizon = Time.unscaledTime + duelPanel.resultSeconds;
            var waitClock = System.Diagnostics.Stopwatch.StartNew();
            while (Time.unscaledTime < horizon)
            {
                Assert.That(waitClock.Elapsed.TotalSeconds, Is.LessThan(8));
                Invoke(match, "UpdateMatchClock");
                Assert.That((float)clock.GetValue(match), Is.EqualTo(before));
                Assert.That(duelPanel.IsOpen && duelPanel.IsResultHeld, Is.True);
                Assert.That(duelPanel.footer.text, Is.EqualTo(footer));
                yield return null;
            }
            LegacyKey("Return");
            Assert.That(duelPanel.IsResultHeld, Is.False);
            yield return Until(() => !duelPanel.IsOpen, "resumed result countdown dismisses automatically");
        }
        else
        {
            LegacyKey("Escape");
            yield return Until(() => !duelPanel.IsOpen, "completed duel can be dismissed with Escape");
        }
    }
}
