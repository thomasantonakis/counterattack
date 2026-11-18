using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

[Category("PostMatchLuck")]
public sealed class PostMatchLuckEditorTests
{
    private static GameplayEvent Roll(int id, GameplayLuckModel model, int roll = 6, string actor = "Home:9", bool forced = false, bool jackpot = false)
        => new() { sequenceNumber = id, kind = "dice.roll", actorTokenKey = actor,
            dice = new GameplayDiceEvent { context = "test", roll = roll, isJackpot = jackpot, overrideUsed = forced, jackpotEnabled = true, luck = model } };
    private static MatchManager.GameData Data(params GameplayEvent[] events) => new() { events = events.ToList() };
    private static GameplayLuckModel Capture(string context, string side = "Home", params (string key, string value)[] parameters)
        => GameplayLuckModel.Capture(context, side, null, parameters.ToDictionary(p => p.key, p => p.value));

    [TestCase("Home", 83.333333333)]
    [TestCase("Away", -83.333333333)]
    public void RareRecoveryIsMeasuredAgainstItsChanceAndBenefitsOppositeManagersConversely(string side, double luck)
    {
        var report = PostMatchLuckData.Build(Data(Roll(1, Capture("pass_interception", side, ("defenderTackling", "4")), actor: side + ":9")));
        Assert.That(report.Checks.Count, Is.EqualTo(1));
        Assert.That(report.HomeLuckPercentagePoints, Is.EqualTo(luck).Within(.00001));
        Assert.That(report.AssessedRolls, Is.EqualTo(1));
    }

    [TestCase(1, .5)]
    [TestCase(6, -.5)]
    public void LowInjuryRollIsFavourableRatherThanMistakingHighDiceForLuck(int roll, double surplus)
    {
        var report = PostMatchLuckData.Build(Data(Roll(1, Capture("movement_injury", "Home", ("attackerResilience", "4")), roll)));
        Assert.That(report.ExpectedHome, Is.EqualTo(.5).Within(1e-9));
        Assert.That(report.HomeSurplus, Is.EqualTo(surplus).Within(1e-9));
    }

    [TestCase("ftp_interception", "defenderTackling", "0", "isBlockingPath", "True", 1d / 3)]
    [TestCase("pass_interception", "defenderTackling", "0", "unused", "0", 1d / 6)]
    [TestCase("goalkeeper_handling", "goalkeeperHandling", "5", "unused", "0", 2d / 3)]
    [TestCase("movement_foul_card_leniency", "refereeLeniency", "4", "unused", "0", .5)]
    [TestCase("header_control", "dribbling", "4", "controlPenalty", "-1", 1d / 6)]
    [TestCase("shot_block", "defenderTackling", "0", "requiredNaturalRoll", "5", 1d / 3)]
    [TestCase("movement_ball_interception", "defenderTackling", "9", "isDribblerRunning", "True", 5d / 6)]
    public void CapturedRulesRespectThresholdsPenaltiesAndFoulBranches(string context, string key1, string value1, string key2, string value2, double expected)
    {
        var report = PostMatchLuckData.Build(Data(Roll(1, Capture(context, "Home", (key1, value1), (key2, value2)), 3)));
        Assert.That(report.ExpectedHome, Is.EqualTo(expected).Within(1e-9));
    }

    [TestCase(false, 0d)]
    [TestCase(true, 1d / 12)]
    public void AccuracyUsesActualJackpotDistribution(bool jackpots, double expected)
    {
        var entry = Roll(1, Capture("high_pass_accuracy", "Home", ("highPassAttribute", "1"), ("threshold", "8")));
        entry.dice.luckJackpotEnabled = jackpots;
        Assert.That(PostMatchLuckData.Build(Data(entry)).ExpectedHome, Is.EqualTo(expected).Within(1e-9));
    }

    [Test]
    public void NegativeSkillsAreClampedForAttributeChecksButLooseRecoveryKeepsItsActualRawModifierRule()
    {
        var control = Roll(1, Capture("header_control", "Home", ("dribbling", "0"), ("controlPenalty", "-3")), 6);
        var loose = Roll(2, Capture("loose_ball_interception", "Home", ("recoveryAttribute", "0"), ("recoveryModifier", "-3")), 6);
        var report = PostMatchLuckData.Build(Data(control, loose));
        Assert.That(report.Checks[0].ExpectedHome, Is.EqualTo(1d / 12).Within(1e-9)); // Only the jackpot reaches control's 9.
        Assert.That(report.Checks[1].ExpectedHome, Is.EqualTo(1d / 6).Within(1e-9)); // Natural six still recovers.
        Assert.That(report.Checks[1].ActualHome, Is.EqualTo(1));
    }

    [TestCase(1, 37d / 72)]
    [TestCase(2, 5d / 9)]
    public void PairedGroundDuelCountsOnceWithFoulsAndNeutralTies(int foulThreshold, double expected)
    {
        var model = new GameplayLuckModel { rule = "ground_duel", category = "Duels & control", side = "Home", contestId = "duel", attackerSkill = 4, defenderSkill = 4, foulThreshold = foulThreshold };
        var defense = Roll(1, model, 3, "Away:8"); defense.dice.context = "movement_tackle_defender";
        var attack = Roll(2, model, 3); attack.dice.context = "movement_tackle_attacker";
        var report = PostMatchLuckData.Build(Data(defense, attack));
        Assert.That(report.Checks.Count, Is.EqualTo(1));
        Assert.That(report.AssessedRolls, Is.EqualTo(2));
        Assert.That(report.ExpectedHome, Is.EqualTo(expected).Within(1e-9));
        Assert.That(report.ActualHome, Is.EqualTo(.5));
    }

    [Test]
    public void ForcingEitherDuelRollExcludesTheWholeContestAndIncompleteOrDuplicateParticipantsDoNotCreateResults()
    {
        var model = new GameplayLuckModel { rule = "ground_duel", side = "Home", contestId = "duel" };
        var defense = Roll(1, model, actor: "Away:8", forced: true); defense.dice.context = "movement_tackle_defender";
        var attack = Roll(2, model); attack.dice.context = "movement_tackle_attacker";
        var report = PostMatchLuckData.Build(Data(defense, attack));
        Assert.That(report.Checks, Is.Empty); Assert.That(report.ForcedRolls, Is.EqualTo(1)); Assert.That(report.UnassessedRolls, Is.EqualTo(1));
        Assert.That(PostMatchLuckData.Build(Data(attack)).Checks, Is.Empty);
        defense.dice.overrideUsed = false; attack.dice.context = "movement_tackle_defender";
        Assert.That(PostMatchLuckData.Build(Data(defense, attack)).Checks, Is.Empty);
    }

    [Test]
    public void GoalkeeperSaveTieHasHalfWeightAndOutsideBoxHandsDoNotReceiveSaveCredit()
    {
        var save = new GameplayLuckModel { rule = "comparison", category = "Goalkeeping", side = "Home", skill = 5, threshold = 10 };
        var report = PostMatchLuckData.Build(Data(Roll(1, save, 5)));
        Assert.That(report.ExpectedHome, Is.EqualTo(.25).Within(1e-9)); Assert.That(report.ActualHome, Is.EqualTo(.5));
        save.outsideHands = true;
        report = PostMatchLuckData.Build(Data(Roll(1, save, 6)));
        Assert.That(report.ActualHome, Is.Zero); Assert.That(report.ExpectedHome, Is.Zero);
    }

    [Test]
    public void FullAerialContestUsesEachTeamsBestCandidateAndCountsThreeRollsOnce()
    {
        var contenders = new[] { new GameplayLuckModel.Contender { tokenKey = "Home:9", attacking = true, skill = 4 },
            new GameplayLuckModel.Contender { tokenKey = "Home:10", attacking = true, skill = 4 }, new GameplayLuckModel.Contender { tokenKey = "Away:8", skill = 4 } };
        var model = GameplayLuckModel.AerialContest("aerial", "Home", false, contenders);
        var rolls = new[] { Roll(1, model, 2), Roll(2, model, 5, "Home:10"), Roll(3, model, 4, "Away:8") };
        foreach (var roll in rolls) roll.dice.luckJackpotEnabled = false;
        var report = PostMatchLuckData.Build(Data(rolls));
        Assert.That(report.ExpectedHome, Is.EqualTo(143d / 216).Within(1e-9));
        Assert.That(report.ActualHome, Is.EqualTo(1)); Assert.That(report.Checks.Count, Is.EqualTo(1)); Assert.That(report.AssessedRolls, Is.EqualTo(3));
        Assert.That(PostMatchLuckData.Build(Data(rolls.Take(2).ToArray())).Checks, Is.Empty);
    }

    [Test]
    public void UnchallengedHeaderAtGoalUsesTheWinningCandidatesNaturalRoll()
    {
        var model = GameplayLuckModel.AerialContest("header", "Home", true, new[] {
            new GameplayLuckModel.Contender { tokenKey = "Home:9", attacking = true, skill = 10 },
            new GameplayLuckModel.Contender { tokenKey = "Home:10", attacking = true, skill = 0 } });
        var first = Roll(1, model, 1); var second = Roll(2, model, 6, "Home:10");
        first.dice.luckJackpotEnabled = second.dice.luckJackpotEnabled = false;
        var report = PostMatchLuckData.Build(Data(first, second));
        Assert.That(report.ActualHome, Is.Zero); Assert.That(report.ExpectedHome, Is.EqualTo(5d / 6).Within(1e-9));
    }

    [Test]
    public void LegacyCoverageDeduplicationUnknownVersionsAndJsonRoundTripAreReadOnly()
    {
        var legacy = Roll(1, null); legacy.dice.context = "pass_interception"; legacy.dice.modifiers["defenderTackling"] = "4";
        var neutral = Roll(2, null); neutral.dice.context = "loose_ball_direction";
        var future = Roll(3, Capture("headed_back_pass_catch")); future.dice.luck.version = 99;
        var data = Data(legacy, legacy, neutral, future);
        string before = JsonConvert.SerializeObject(data);
        var report = PostMatchLuckData.Build(JsonConvert.DeserializeObject<MatchManager.GameData>(before));
        Assert.That(report.TotalRolls, Is.EqualTo(3)); Assert.That(report.AssessedRolls, Is.EqualTo(1)); Assert.That(report.UnassessedRolls, Is.EqualTo(2));
        Assert.That(JsonConvert.SerializeObject(data), Is.EqualTo(before));
        Assert.That(PostMatchLuckData.Build(null).Checks, Is.Empty);
    }

    [Test]
    public void AuthoredLuckTabUsesNamedTeamsKeepsFiltersHiddenAndDoesNotCreateUiOrChangeStats()
    {
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PostMatchReportEditorTools.PrefabPath));
        try
        {
            var data = Data(Roll(1, Capture("pass_interception", "Home", ("defenderTackling", "4"))));
            data.gameSettings.homeTeamName = "Inverness"; data.gameSettings.awayTeamName = "Aurora";
            int count = root.GetComponentsInChildren<Transform>(true).Length;
            var report = root.GetComponent<PostMatchReportController>(); report.ShowReport(data, true);
            var content = root.transform.Find(PostMatchReportEditorTools.ContentPath);
            string[] stats = content.Find("StatsGrid").GetComponentsInChildren<TMP_Text>(true).Select(t => t.text).ToArray();
            content.Find("ReportTabs/LuckTab").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(report.ShowingLuck, Is.True); Assert.That(report.ShowingPitch || report.ShowingPlayers, Is.False);
            Assert.That(content.Find("TeamFilters").gameObject.activeSelf, Is.False);
            Assert.That(root.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "LuckHomeName").text, Is.EqualTo("Inverness"));
            Assert.That(root.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "HomeLuck").text, Is.EqualTo("+83.3 pp"));
            Assert.That(root.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(count));
            Assert.That(content.Find("StatsGrid").GetComponentsInChildren<TMP_Text>(true).Select(t => t.text), Is.EqualTo(stats));
            report.ShowStats(); Assert.That(report.ShowingLuck, Is.False);
            report.ShowReport(null, false); report.ShowLuck(); Assert.That(report.ShowingLuck, Is.False);
        }
        finally { Object.DestroyImmediate(root); }
    }
}
