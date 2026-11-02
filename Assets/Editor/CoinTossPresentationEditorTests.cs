using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class CoinTossPresentationEditorTests
{
    [TestCase(true, CoinTossChoice.LeftToRight, false, true)]
    [TestCase(true, CoinTossChoice.RightToLeft, false, false)]
    [TestCase(false, CoinTossChoice.LeftToRight, true, false)]
    [TestCase(false, CoinTossChoice.RightToLeft, true, true)]
    public void WinnerChoosingDirectionGivesOpponentKickoff(bool winnerHome, CoinTossChoice choice, bool homeKickoff, bool homeLeftToRight)
    {
        var decision = new CoinTossDecision(winnerHome);
        Assert.That(decision.ChoosingHome, Is.EqualTo(winnerHome));
        Assert.That(decision.Choose(choice), Is.True);
        AssertOpening(decision.Result, winnerHome, homeKickoff, homeLeftToRight);
        Assert.That(decision.Choose(CoinTossChoice.Kickoff), Is.False, "A completed toss cannot be overwritten.");
    }

    [TestCase(true, CoinTossChoice.LeftToRight, false)]
    [TestCase(true, CoinTossChoice.RightToLeft, true)]
    [TestCase(false, CoinTossChoice.LeftToRight, true)]
    [TestCase(false, CoinTossChoice.RightToLeft, false)]
    public void WinnerChoosingKickoffLeavesOnlyDirectionsForLoser(bool winnerHome, CoinTossChoice loserChoice, bool homeLeftToRight)
    {
        var decision = new CoinTossDecision(winnerHome);
        Assert.That(decision.Choose(CoinTossChoice.Kickoff), Is.True);
        Assert.That(decision.Result, Is.Null);
        Assert.That(decision.ChoosingHome, Is.EqualTo(!winnerHome));
        Assert.That(decision.CanChooseKickoff, Is.False);
        Assert.That(decision.Choose(CoinTossChoice.Kickoff), Is.False);
        Assert.That(decision.Choose(loserChoice), Is.True);
        AssertOpening(decision.Result, winnerHome, winnerHome, homeLeftToRight);
    }

    [TestCase(true)] [TestCase(false)]
    public void CpuAlwaysChoosesBallIfAvailableAndCompletesForcedDirectionChoice(bool cpuHome)
    {
        var winner = new CoinTossDecision(cpuHome);
        Assert.That(winner.CpuChoice, Is.EqualTo(CoinTossChoice.Kickoff));
        winner.Choose(winner.CpuChoice);
        winner.Choose(CoinTossChoice.RightToLeft);
        Assert.That(winner.Result.homeHasFirstHalfKickoff, Is.EqualTo(cpuHome));

        var loser = new CoinTossDecision(!cpuHome);
        loser.Choose(CoinTossChoice.Kickoff);
        Assert.That(loser.ChoosingHome, Is.EqualTo(cpuHome));
        Assert.That(loser.CpuChoice, Is.EqualTo(cpuHome ? CoinTossChoice.LeftToRight : CoinTossChoice.RightToLeft));
        loser.Choose(loser.CpuChoice);
        AssertOpening(loser.Result, !cpuHome, !cpuHome, true);
    }

    [Test]
    public void CpuChoiceRespectsActualManagerControlsAndLegacyPersonas()
    {
        var settings = new GameSettings { gameMode = "Single Player", homeTeamControl = "CPU", awayTeamControl = "Human" };
        Assert.That(CoinTossDecision.IsCpuManager(settings, true), Is.True);
        Assert.That(CoinTossDecision.IsCpuManager(settings, false), Is.False);
        settings.homeTeamControl = null; settings.homeRoomPersona = "Codex Tactician (Hard)";
        Assert.That(CoinTossDecision.IsCpuManager(settings, true), Is.True);
        settings.homeTeamControl = "Human";
        Assert.That(CoinTossDecision.IsCpuManager(settings, true), Is.False);
        settings.gameMode = "Hot Seat"; settings.homeTeamControl = "CPU";
        Assert.That(CoinTossDecision.IsCpuManager(settings, true), Is.False);
    }

    [TestCase(true)] [TestCase(false)]
    public void SavedOpeningChoicesRetainTeamIdentityAndUseOppositeFormationHalves(bool homeLeftToRight)
    {
        var data = new MatchManager.GameData();
        data.gameSettings.homeTeamName = "Aurora F.C.";
        data.gameSettings.awayTeamName = "Inverness CT";
        data.openingSetup = new MatchOpeningSetup { homeHasFirstHalfKickoff = false, homeAttacksLeftToRight = homeLeftToRight };
        var loaded = JsonConvert.DeserializeObject<MatchManager.GameData>(JsonConvert.SerializeObject(data));
        Assert.That(loaded.gameSettings.homeTeamName, Is.EqualTo("Aurora F.C."));
        Assert.That(loaded.gameSettings.awayTeamName, Is.EqualTo("Inverness CT"));
        Assert.That(loaded.openingSetup.KickoffTeam, Is.EqualTo(MatchManager.TeamInAttack.Away));
        var home = GoalFlowManager.GetResetFormationCoordinatesForDirection(loaded.openingSetup.DirectionFor(true));
        var away = GoalFlowManager.GetResetFormationCoordinatesForDirection(loaded.openingSetup.DirectionFor(false));
        Assert.That(home.Length, Is.EqualTo(11)); Assert.That(away.Length, Is.EqualTo(11));
        Assert.That(home.All(hex => homeLeftToRight ? hex.x < 0 : hex.x > 0), Is.True);
        Assert.That(away.All(hex => homeLeftToRight ? hex.x > 0 : hex.x < 0), Is.True);
        Assert.That(home.Intersect(away), Is.Empty);
        Assert.That(home[0].x, Is.EqualTo(homeLeftToRight ? -16 : 16), "Home GK stays with the Home roster on its chosen half.");
    }

    [Test]
    public void LoadingMatchKeepsOpeningChoicesAlongsideLaterLiveDirections()
    {
        string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CounterAttackCoinToss_" + System.Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(folder);
        try
        {
            var data = new MatchManager.GameData();
            data.openingSetup = new MatchOpeningSetup { homeHasFirstHalfKickoff = false, homeAttacksLeftToRight = false };
            data.runtimeSnapshot = new RoomRuntimeSnapshot
            {
                currentState = "StandardPass", teamInAttack = "Home", attackHasPossession = true,
                homeTeamDirection = "LeftToRight", awayTeamDirection = "RightToLeft"
            };
            string source = System.IO.Path.Combine(folder, "match.json");
            System.IO.File.WriteAllText(source, JsonConvert.SerializeObject(data));
            Assert.That(RoomSaveService.CreateLoadedRunCopy(source, out string run, out string message), Is.True, message);
            var loaded = JsonConvert.DeserializeObject<MatchManager.GameData>(System.IO.File.ReadAllText(run));
            Assert.That(loaded.openingSetup.homeHasFirstHalfKickoff, Is.False);
            Assert.That(loaded.openingSetup.homeAttacksLeftToRight, Is.False);
            Assert.That(loaded.runtimeSnapshot.homeTeamDirection, Is.EqualTo("LeftToRight"), "Live directions survive independently of first-half choices.");
            Assert.That(loaded.runtimeSnapshot.teamInAttack, Is.EqualTo("Home"));
        }
        finally { System.IO.Directory.Delete(folder, true); }
    }

    [TestCase("Draft")] [TestCase("FreeDraft")]
    public void SavedPanelsRollRerollThenExpandWithDisabledKickoffAndClickableDirections(string sceneName)
    {
        var scene = EditorSceneManager.OpenPreviewScene($"Assets/Scenes/{sceneName}.unity");
        try
        {
            var ui = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DraftUIManager>(true)).Single();
            var coin = ui.coinTossPanel;
            Assert.That(coin, Is.Not.Null); Assert.That(coin.HasSceneBindings, Is.True);
            var dice = coin.dicePanel;
            Assert.That(dice.visuals.activeSelf, Is.False);
            Assert.That(coin.choices.activeSelf, Is.False);
            Assert.That(dice.homeDie.numericFaces.All(sprite => sprite != null), Is.True);
            Assert.That(dice.awayDie.numericFaces.All(sprite => sprite != null), Is.True);
            Assert.That(scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)).Count(), Is.EqualTo(1));
            Assert.That(coin.GetComponentInParent<Canvas>().GetComponent<UnityEngine.UI.GraphicRaycaster>(), Is.Not.Null);
            typeof(CoinTossPanelController).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(coin, null);
            Assert.That(coin.Open(new GameSettings { gameMode = "Hot Seat", homeTeamName = "Aurora", awayTeamName = "Inverness", homeKit = "blue", awayKit = "red_white" }), Is.True);
            Assert.That(coin.Open(new GameSettings()), Is.False, "Double clicks cannot restart a live toss.");
            Assert.That(dice.title.text, Is.EqualTo("COIN TOSS FLIP"));
            dice.Roll(Rig(3)); dice.Roll(Rig(3)); dice.AdvanceResult(Deadline(dice));
            Assert.That(dice.IsAwaitingHomeRoll, Is.True);
            Assert.That(coin.choices.activeSelf, Is.False);
            dice.Roll(Rig(6)); dice.Roll(Rig(2));
            dice.AdvanceResult(Deadline(dice) - .01f); coin.AdvanceChoices();
            Assert.That(coin.choices.activeSelf, Is.False);
            dice.AdvanceResult(Deadline(dice)); coin.AdvanceChoices();
            Assert.That(dice.homeDie.DisplayedRoll, Is.EqualTo(6));
            Assert.That(dice.awayDie.DisplayedRoll, Is.EqualTo(2));
            Assert.That(dice.homeDie.ShowsJackpot || dice.awayDie.ShowsJackpot, Is.False);
            Assert.That(coin.choices.activeSelf, Is.True);
            Assert.That(dice.panel.sizeDelta.y, Is.EqualTo(1080));
            coin.kickoffButton.onClick.Invoke();
            Assert.That(coin.kickoffButton.interactable, Is.False);
            Assert.That(coin.leftToRightButton.interactable, Is.True);
            Assert.That(dice.footer.text, Does.Contain("Inverness manager"));
            Assert.That(coin.Result, Is.Null);
            coin.rightToLeftButton.onClick.Invoke();
            AssertOpening(coin.Result, true, true, true);
            Assert.That(coin.IsOpen, Is.False);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static void AssertOpening(MatchOpeningSetup opening, bool winnerHome, bool homeKickoff, bool homeLeftToRight)
    {
        Assert.That(opening, Is.Not.Null);
        Assert.That(opening.coinTossWinnerIsHome, Is.EqualTo(winnerHome));
        Assert.That(opening.homeHasFirstHalfKickoff, Is.EqualTo(homeKickoff));
        Assert.That(opening.homeAttacksLeftToRight, Is.EqualTo(homeLeftToRight));
        Assert.That(opening.DirectionFor(true), Is.Not.EqualTo(opening.DirectionFor(false)));
    }

    private static RollInputOverride Rig(int roll) => new() { hasOverride = true, roll = roll, isJackpot = false };
    private static float Deadline(DraftOrderPanelController panel) => (float)typeof(DraftOrderPanelController)
        .GetField("resultUntil", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panel);
}
