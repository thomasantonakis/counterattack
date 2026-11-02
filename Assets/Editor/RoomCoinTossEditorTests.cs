using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class RoomCoinTossEditorTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject root;
    private MatchManager match, previousMatch;

    [SetUp]
    public void SetUp()
    {
        previousMatch = MatchManager.Instance;
        root = new GameObject("Room coin toss fixture"); root.SetActive(false);
        match = root.AddComponent<MatchManager>();
        MatchManager.Instance = match;
        match.gameData = new MatchManager.GameData();
        match.gameData.gameSettings.gameMode = "Hot Seat";
        match.gameData.gameSettings.halfDuration = 45;
        match.gameData.gameSettings.numberOfHalfs = 2;
        match.gameData.gameSettings.homeTeamName = "Aurora";
        match.gameData.gameSettings.awayTeamName = "Inverness";
    }

    [TearDown]
    public void TearDown()
    {
        MatchManager.Instance = previousMatch;
        Object.DestroyImmediate(root);
    }

    [TestCase(true, CoinTossChoice.LeftSide, false, false)]
    [TestCase(true, CoinTossChoice.RightSide, false, true)]
    [TestCase(false, CoinTossChoice.LeftSide, true, false)]
    [TestCase(false, CoinTossChoice.RightSide, true, true)]
    public void WinnerChoosingShootoutEndGivesOpponentFirstPenalty(bool winnerHome, CoinTossChoice side, bool firstHome, bool rightGoal)
    {
        var decision = new CoinTossDecision(winnerHome, CoinTossPurpose.PenaltyShootout);
        Assert.That(decision.Choose(CoinTossChoice.LeftToRight), Is.False, "Shootout ends are absolute, rather than team attack directions.");
        Assert.That(decision.Choose(side), Is.True);
        Assert.That(decision.Result, Is.Null);
        Assert.That(decision.ShootoutResult.homeTakesFirstPenalty, Is.EqualTo(firstHome));
        Assert.That(decision.ShootoutResult.useRightGoal, Is.EqualTo(rightGoal));
        Assert.That(decision.Choose(CoinTossChoice.Kickoff), Is.False);
    }

    [TestCase(true, CoinTossChoice.LeftSide, false)]
    [TestCase(true, CoinTossChoice.RightSide, true)]
    [TestCase(false, CoinTossChoice.LeftSide, false)]
    [TestCase(false, CoinTossChoice.RightSide, true)]
    public void WinnerChoosingFirstPenaltyLeavesOnlyEndChoiceForOpponent(bool winnerHome, CoinTossChoice side, bool rightGoal)
    {
        var decision = new CoinTossDecision(winnerHome, CoinTossPurpose.PenaltyShootout);
        Assert.That(decision.CpuChoice, Is.EqualTo(CoinTossChoice.Kickoff));
        decision.Choose(CoinTossChoice.Kickoff);
        Assert.That(decision.CanChooseKickoff, Is.False);
        Assert.That(decision.ChoosingHome, Is.EqualTo(!winnerHome));
        Assert.That(decision.Choose(CoinTossChoice.Kickoff), Is.False);
        Assert.That(decision.CpuChoice, Is.EqualTo(CoinTossChoice.LeftSide));
        decision.Choose(side);
        Assert.That(decision.ShootoutResult.homeTakesFirstPenalty, Is.EqualTo(winnerHome));
        Assert.That(decision.ShootoutResult.useRightGoal, Is.EqualTo(rightGoal));
    }

    [TestCase(true, true)] [TestCase(true, false)]
    [TestCase(false, true)] [TestCase(false, false)]
    public void ExtraTimeStartsWithChosenSidesThenSwitchesAndAlternatesItsOwnKickoff(bool homeLeftToRight, bool homeKickoff)
    {
        match.currentHalf = 2;
        // Choose the same directions as the second half: the old automatic switch would be wrong.
        match.homeTeamDirection = homeLeftToRight ? MatchManager.TeamAttackingDirection.LeftToRight : MatchManager.TeamAttackingDirection.RightToLeft;
        match.awayTeamDirection = homeLeftToRight ? MatchManager.TeamAttackingDirection.RightToLeft : MatchManager.TeamAttackingDirection.LeftToRight;
        match.gameData.extraTimeOpeningSetup = new MatchOpeningSetup
        {
            homeAttacksLeftToRight = homeLeftToRight, homeHasFirstHalfKickoff = homeKickoff
        };
        typeof(MatchManager).GetField("firstHalfKickoffTeam", Private).SetValue(match,
            homeKickoff ? MatchManager.TeamInAttack.Away : MatchManager.TeamInAttack.Home);
        Invoke(match, "SetUpNextHalf", true);
        Assert.That(match.currentHalf, Is.EqualTo(3));
        Assert.That(match.homeTeamDirection, Is.EqualTo(match.gameData.extraTimeOpeningSetup.DirectionFor(true)));
        Assert.That(match.awayTeamDirection, Is.EqualTo(match.gameData.extraTimeOpeningSetup.DirectionFor(false)));
        Assert.That(match.teamInAttack, Is.EqualTo(match.gameData.extraTimeOpeningSetup.KickoffTeam));
        Invoke(match, "SetUpNextHalf", false);
        Assert.That(match.currentHalf, Is.EqualTo(4));
        Assert.That(match.homeTeamDirection, Is.EqualTo(match.gameData.extraTimeOpeningSetup.DirectionFor(false)));
        Assert.That(match.teamInAttack, Is.EqualTo(homeKickoff ? MatchManager.TeamInAttack.Away : MatchManager.TeamInAttack.Home));
        Assert.That(match.gameData.gameSettings.homeTeamName, Is.EqualTo("Aurora"));
        Assert.That(match.gameData.gameSettings.awayTeamName, Is.EqualTo("Inverness"));
    }

    [Test]
    public void RegulationHalfTimeStillSwitchesSidesAndAlternatesKickoff()
    {
        match.currentHalf = 1;
        match.homeTeamDirection = MatchManager.TeamAttackingDirection.LeftToRight;
        match.awayTeamDirection = MatchManager.TeamAttackingDirection.RightToLeft;
        Invoke(match, "SetUpNextHalf", false);
        Assert.That(match.currentHalf, Is.EqualTo(2));
        Assert.That(match.homeTeamDirection, Is.EqualTo(MatchManager.TeamAttackingDirection.RightToLeft));
        Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
    }

    [TestCase("None", 0, false, false)]
    [TestCase("Penalties", 0, false, true)]
    [TestCase("Extra Time", 0, true, false)]
    [TestCase("Extra Time & Penalties", 0, true, true)]
    [TestCase("Extra Time & Penalties", 1, false, true)]
    public void TieBreakerRulesStillGateExtraTimeAndPenalties(string rule, int homeGoals, bool extraTime, bool penalties)
    {
        match.gameData.gameSettings.tiebreaker = rule;
        match.gameData.stats.homeTeamStats.totalGoals = homeGoals;
        Assert.That(Invoke(match, "ShouldStartExtraTime"), Is.EqualTo(extraTime));
        Assert.That(Invoke(match, "TieBreakerIncludesPenalties"), Is.EqualTo(penalties));
        match.gameData.gameSettings.halfDuration = 30;
        Assert.That(Invoke(match, "ShouldStartExtraTime"), Is.False);
    }

    [TestCase(false, false)] [TestCase(false, true)]
    [TestCase(true, false)] [TestCase(true, true)]
    public void ShootoutCoordinatesFirstTakerAndBothTeamsAttackTheChosenGoal(bool rightGoal, bool firstHome)
    {
        match.gameData.penaltyShootoutSetup = new PenaltyShootoutSetup { useRightGoal = rightGoal, homeTakesFirstPenalty = firstHome };
        var shootout = root.AddComponent<PenaltyShootoutManager>(); shootout.matchManager = match;
        match.playerTokenManager = root.AddComponent<PlayerTokenManager>();
        shootout.playerTokenManager = match.playerTokenManager;
        var home = root.AddComponent<PlayerToken>(); home.isHomeTeam = true;
        var away = root.AddComponent<PlayerToken>(); away.isHomeTeam = false;
        shootout.playerTokenManager.allTokens.Add(home); shootout.playerTokenManager.allTokens.Add(away);
        int sign = rightGoal ? 1 : -1;
        Assert.That(Property(shootout, "FirstKickIsHome"), Is.EqualTo(firstHome));
        Assert.That(Property(shootout, "PenaltySpotCoordinates"), Is.EqualTo(new Vector3Int(14 * sign, 0, 0)));
        Assert.That(Property(shootout, "DefendingGoalkeeperCoordinates"), Is.EqualTo(new Vector3Int(18 * sign, 0, 0)));
        Assert.That(Property(shootout, "HomeGoalkeeperRestCoordinates"), Is.EqualTo(new Vector3Int(18 * sign, 0, -9)));
        Assert.That(Property(shootout, "AwayGoalkeeperRestCoordinates"), Is.EqualTo(new Vector3Int(18 * sign, 0, 9)));
        foreach (bool shooterHome in new[] { firstHome, !firstHome })
        {
            Invoke(shootout, "SetShootoutAttackContext", shooterHome);
            Assert.That(match.teamInAttack, Is.EqualTo(shooterHome ? MatchManager.TeamInAttack.Home : MatchManager.TeamInAttack.Away));
            var direction = shooterHome ? match.homeTeamDirection : match.awayTeamDirection;
            Assert.That(direction, Is.EqualTo(rightGoal ? MatchManager.TeamAttackingDirection.LeftToRight : MatchManager.TeamAttackingDirection.RightToLeft));
            Assert.That(home.isAttacker, Is.EqualTo(shooterHome));
            Assert.That(away.isAttacker, Is.EqualTo(!shooterHome));
            Assert.That(home.isHomeTeam, Is.True); Assert.That(away.isHomeTeam, Is.False);
        }
    }

    [Test]
    public void ExtraTimeAndShootoutChoicesSurviveSerializationIndependentlyOfLiveDirections()
    {
        match.gameData.extraTimeOpeningSetup = new MatchOpeningSetup { homeHasFirstHalfKickoff = false, homeAttacksLeftToRight = false };
        match.gameData.penaltyShootoutSetup = new PenaltyShootoutSetup { homeTakesFirstPenalty = true, useRightGoal = true };
        match.gameData.runtimeSnapshot = new RoomRuntimeSnapshot { homeTeamDirection = "LeftToRight", awayTeamDirection = "RightToLeft" };
        var loaded = JsonConvert.DeserializeObject<MatchManager.GameData>(JsonConvert.SerializeObject(match.gameData));
        Assert.That(loaded.extraTimeOpeningSetup.homeHasFirstHalfKickoff, Is.False);
        Assert.That(loaded.extraTimeOpeningSetup.homeAttacksLeftToRight, Is.False);
        Assert.That(loaded.penaltyShootoutSetup.homeTakesFirstPenalty, Is.True);
        Assert.That(loaded.penaltyShootoutSetup.useRightGoal, Is.True);
        Assert.That(loaded.runtimeSnapshot.homeTeamDirection, Is.EqualTo("LeftToRight"));
    }

    [TestCase(CoinTossPurpose.ExtraTime)] [TestCase(CoinTossPurpose.PenaltyShootout)]
    public void RoomPanelRoutesManagerRollsHoldsClockAndBlocksOtherGameplayUntilBothChoicesFinish(CoinTossPurpose purpose)
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        try
        {
            var live = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<MatchManager>(true)).Single();
            MatchManager.Instance = live;
            live.gameData = match.gameData;
            live.BeginGameplayEventLoggingSuppression();
            var coin = live.coinTossPanel;
            Assert.That(coin.HasSceneBindings, Is.True);
            Assert.That(coin.dicePanel.externalInput, Is.True);
            Assert.That(coin.dicePanel.roomMatch, Is.SameAs(live));
            Assert.That(coin.dicePanel.visuals.activeSelf, Is.False);
            Invoke(coin, "Awake");
            Assert.That(coin.OpenForRoom(live, purpose), Is.True);
            live.currentState = MatchManager.GameState.MovementPhase; live.isClockRunning = true;
            Assert.That(Invoke(live, "CanAdvanceMatchClock"), Is.False);
            var input = root.AddComponent<GameInputManager>();
            Assert.That(Invoke(input, "DispatchSyntheticClick", null, null, "test"), Is.False);
            var homeRoll = new KeyPressData(KeyCode.Alpha2, true, true, false);
            Assert.That(CoinTossPanelController.ConsumeRoomKey(homeRoll), Is.True);
            Assert.That(homeRoll.consumedByHomeTeam, Is.True);
            var awayRoll = new KeyPressData(KeyCode.Alpha6, true, true, false);
            CoinTossPanelController.ConsumeRoomKey(awayRoll);
            Assert.That(awayRoll.consumedByHomeTeam, Is.False);
            Assert.That(coin.choices.activeSelf, Is.False);
            coin.dicePanel.AdvanceResult(Deadline(coin.dicePanel)); coin.AdvanceChoices();
            Assert.That(coin.choices.activeSelf, Is.True);
            Assert.That(coin.dicePanel.homeDie.ShowsJackpot || coin.dicePanel.awayDie.ShowsJackpot, Is.False);
            Assert.That(coin.dicePanel.homeDie.DisplayedRoll, Is.EqualTo(2));
            Assert.That(coin.dicePanel.awayDie.DisplayedRoll, Is.EqualTo(6));
            string kickoff = coin.kickoffButton.GetComponentInChildren<TMPro.TMP_Text>().text;
            Assert.That(kickoff, Does.Contain(purpose == CoinTossPurpose.ExtraTime ? "Extra Time First Half Kick Off" : "Take the First Penalty"));
            coin.kickoffButton.onClick.Invoke();
            Assert.That(coin.kickoffButton.interactable, Is.False);
            Assert.That(coin.dicePanel.footer.text, Does.Contain("Aurora manager"));
            coin.rightToLeftButton.onClick.Invoke();
            Assert.That(CoinTossPanelController.IsRoomVisible, Is.False);
            if (purpose == CoinTossPurpose.ExtraTime) Assert.That(coin.Result.homeHasFirstHalfKickoff, Is.False);
            else
            {
                Assert.That(coin.ShootoutResult.homeTakesFirstPenalty, Is.False);
                Assert.That(coin.ShootoutResult.useRightGoal, Is.True);
            }
            live.EndGameplayEventLoggingSuppression();
        }
        finally { MatchManager.Instance = match; EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static object Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    private static object Property(object target, string name) => target.GetType().GetProperty(name, Private).GetValue(target);
    private static float Deadline(DraftOrderPanelController panel) => (float)typeof(DraftOrderPanelController).GetField("resultUntil", Private).GetValue(panel);
}
