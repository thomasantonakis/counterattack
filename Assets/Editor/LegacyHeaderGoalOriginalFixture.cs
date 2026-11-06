using System.Collections;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOriginalHeaderGoalLegacy(string outcome, bool rush)
    {
        yield return PrepareNativeHeaderAtGoal(rush, true);
        var header = match.shotManager.headerManager; var shot = match.shotManager;
        var p = match.playerTokenManager.allTokens.ToDictionary(t => t.playerName);
        if (outcome == "prep") yield break;
        int att = outcome is "goal" ? 6 : outcome is "off_target" or "unfinished_own_goal" ? 1 : outcome == "loose" ? 5 : 2;
        int def = outcome == "headed_away" ? 6 : outcome == "loose" ? 5 : 1;
        if (outcome == "off_target") p["Yaneva"].heading = 6;
        int gk = outcome is "goal" or "off_target" or "unfinished_own_goal" ? 1 : outcome is "headed_away" or "loose" ? 2 : 6;
        yield return RollNativeHeaderGoalChallenge(att, def, rush ? gk : null);
        if (outcome == "unfinished_own_goal")
        {
            // This unfinished legacy method had no own-goal assertion. With a
            // rushing keeper its three natural ones actually give the GK the win;
            // without one, the two legal header rolls tie and the third GK roll
            // is inapplicable. Preserve the valid inputs and observe their states.
            if (rush)
                yield return Until(() => shot.isWaitingForSaveandHoldScenario && !header.isActivated,
                    "three natural-one aerial rolls give the rushing goalkeeper possession");
            else
                yield return Until(() => loose.isActivated && loose.isWaitingForDirectionRoll,
                    "without a rushing keeper the original natural-one header rolls tie into scatter");
            Assert.That(match.goalFlowManager.isActivated, Is.False); yield break;
        }
        if (outcome == "goal")
        {
            if (!rush)
            {
                Assert.That(header.isActivated, Is.False); Assert.That(shot.isActivated && shot.isWaitingForGKDiceRoll, Is.True);
                yield return ResolveNativeHeaderSave(2);
            }
            yield return Until(() => match.goalFlowManager.isActivated, "original headed goal starts actual celebration");
            yield return WaitForLegacyGoalReset();
            Assert.That(match.gameData.stats.GetPlayerStats(p["Cafferata"]).assists, Is.EqualTo(1));
            Assert.That(match.gameData.stats.homeTeamStats.totalAssists, Is.EqualTo(1)); yield break;
        }
        if (outcome == "off_target")
        {
            yield return Until(() => match.currentState == MatchManager.GameState.WaitingForGoalKickFinalThirds, "natural-one header goes off target into the original goal-kick Final Third state"); yield break;
        }
        if (outcome == "headed_away")
        { yield return Until(() => header.isWaitingForHeaderTargetSelection, "defending winner heads away after winning the original aerial challenge"); yield break; }
        if (outcome == "loose")
        {
            yield return Until(() => loose.isActivated && loose.isWaitingForDirectionRoll, "original tied aerial challenge opens scatter");
            yield return RollNativeHeaderScatter(1, 4);
            yield return Until(() => !loose.isActivated, "original tied header scatter completes its actual movement"); yield break;
        }
        if (!rush)
        {
            Assert.That(!header.isActivated && shot.isWaitingForGKDiceRoll, Is.True);
            yield return ResolveNativeHeaderSave(6);
            yield return Until(() => !shot.isWaitingForGKDiceRoll && shot.isWaitingforHandlingTest, "saved original headed shot waits for handling");
            yield return ResolveNativeHeaderHandling(outcome is "corner" or "spill" ? 6 : 1);
        }
        if (outcome is "corner" or "spill")
        {
            Assert.That(loose.isActivated && loose.isWaitingForDirectionRoll, Is.True);
            yield return RollNativeHeaderScatter(outcome == "corner" ? 4 : 3, null);
            if (outcome == "spill")
            {
                yield return Until(() => loose.isWaitingForDistanceRoll, "the original goalkeeper spill advances from consumed direction into its distance roll");
                yield break;
            }
            Assert.That(loose.isWaitingForDistanceRoll, Is.False);
            yield return Until(() => match.finalThirdManager.isActivated, "original corner first offers Final Thirds", 1200);
            yield return ForfeitLegacyFinalThirds();
            yield return Until(() => !loose.isActivated && match.freeKickManager.isActivated && match.freeKickManager.isWaitingForKickerSelection,
                "original headed-shot corner reaches kicker selection");
            var kicker = p.Values.Single(t => t.playerName.Contains("Noruega"));
            input.TryExecuteSyntheticHexClick(kicker.currentHex, "Legacy migration", out _);
            yield return Until(() => !match.freeKickManager.isWaitingForKickerSelection && match.freeKickManager.isWaitingForSetupPhase,
                "original corner kicker selection begins inspectable setup");
            // The legacy AssertTrue(false, "Break") was a manual stop marker.
            yield break;
        }
        yield return Until(() => shot.isWaitingForSaveandHoldScenario && !header.isActivated, "original headed save hands distribution to ShotManager");
        if (outcome == "quick_throw")
        {
            LegacyKey("Q"); yield return Until(() => !shot.isActivated && ground.isActivated, "held headed shot starts the real Quick Throw action");
            Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.QuickThrow)); Assert.That(ground.isQuickThrow, Is.True);
        }
        else if (outcome == "goal_kick")
        {
            LegacyKey("K"); yield return Until(() => !shot.isWaitingForSaveandHoldScenario && match.finalThirdManager.isActivated,
                "held headed shot can choose goalkeeper kick and start Final Thirds"); Assert.That(match.finalThirdManager.bothSides, Is.True);
        }
    }
}
