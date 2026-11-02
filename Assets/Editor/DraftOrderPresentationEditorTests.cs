using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class DraftOrderPresentationEditorTests
{
    private GameObject root;
    private DraftOrderPanelController panel;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Draft order fixture", typeof(RectTransform));
        root.SetActive(false);
        ((RectTransform)root.transform).sizeDelta = new Vector2(3840, 2160);
        panel = root.AddComponent<DraftOrderPanelController>();
        panel.visuals = Rect("Visuals", root.transform).gameObject;
        panel.panel = Rect("Panel", root.transform);
        panel.title = Label("Title"); panel.homeTeam = Label("Home"); panel.awayTeam = Label("Away");
        panel.homeResult = Label("Home roll"); panel.awayResult = Label("Away roll");
        panel.outcome = Label("Outcome"); panel.footer = Label("Footer");
        panel.homeDie = Die("Home die"); panel.awayDie = Die("Away die");
        Assert.That(panel.Open(new GameSettings
        {
            homeTeamName = "Aurora F.C.", awayTeamName = "Inverness CT", homeKit = "blue", awayKit = "red_white"
        }), Is.True);
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(root);

    [TestCase(6, false, 2, false, true)]
    [TestCase(2, false, 6, false, false)]
    [TestCase(6, true, 5, false, true)]
    public void BothManagersRollThenWinnerWaitsExactlyTwoSeconds(int home, bool homeJackpot, int away, bool awayJackpot, bool homeWins)
    {
        Assert.That(panel.IsAwaitingHomeRoll, Is.True);
        Assert.That(panel.homeDie.IsAnimating, Is.True);
        Assert.That(panel.awayDie.IsAnimating, Is.False);
        Assert.That(panel.footer.text, Does.Contain("Aurora F.C."));
        panel.Roll(Rig(home, homeJackpot));
        Assert.That(panel.IsAwaitingAwayRoll, Is.True);
        Assert.That(panel.homeDie.IsAnimating, Is.False);
        Assert.That(panel.awayDie.IsAnimating, Is.True);
        Assert.That(panel.footer.text, Does.Contain("Inverness CT"));
        panel.Roll(Rig(away, awayJackpot));
        Assert.That(panel.HomeWon, Is.Null, "First pick must not be released during the result pause.");
        float deadline = Deadline();
        panel.AdvanceResult(deadline - .01f);
        Assert.That(panel.IsOpen, Is.True);
        string random = JsonUtility.ToJson(Random.state);
        Assert.That(panel.HandleKey(new KeyPressData(KeyCode.R, false, false, false)), Is.True);
        Assert.That(JsonUtility.ToJson(Random.state), Is.EqualTo(random), "R during the result pause must not roll again.");
        panel.AdvanceResult(deadline);
        Assert.That(panel.IsOpen, Is.False);
        Assert.That(panel.HomeWon, Is.EqualTo(homeWins));
        Assert.That(panel.visuals.activeSelf, Is.False);
    }

    [TestCase(false)] [TestCase(true)]
    public void TiesRequireTwoFreshRollsAfterTwoSeconds(bool legacyJackpotOverride)
    {
        panel.Roll(Rig(4, legacyJackpotOverride));
        panel.Roll(Rig(4, legacyJackpotOverride));
        Assert.That(panel.outcome.text, Does.Contain("TIE"));
        float deadline = Deadline();
        panel.AdvanceResult(deadline - .01f);
        Assert.That(panel.IsAwaitingHomeRoll, Is.False);
        panel.AdvanceResult(deadline);
        Assert.That(panel.HomeWon, Is.Null);
        Assert.That(panel.IsAwaitingHomeRoll, Is.True);
        Assert.That(panel.homeDie.IsAnimating, Is.True);
        Assert.That(panel.awayDie.DisplayedRoll, Is.Zero);
        Assert.That(panel.homeResult.text, Is.Empty);
        panel.Roll(Rig(2));
        panel.AdvanceResult(deadline + 10);
        Assert.That(panel.IsAwaitingAwayRoll, Is.True, "A prior away roll must not survive a tied round.");
        panel.Roll(Rig(5));
        panel.AdvanceResult(Deadline());
        Assert.That(panel.HomeWon, Is.False);
    }

    [Test]
    public void DiceUseJerseyColorsAndSyntheticOverridesFollowTheSameRollState()
    {
        var home = TokenKitCatalog.GetInstructionPalette(TokenKitCatalog.ResolveStyle("blue"));
        var away = TokenKitCatalog.GetInstructionPalette(TokenKitCatalog.ResolveStyle("red_white"));
        Assert.That(panel.homeDie.background.color, Is.EqualTo(home.Primary));
        Assert.That(panel.awayDie.background.color, Is.EqualTo(away.Primary));
        var key = new KeyPressData(KeyCode.Alpha3, true, true, false);
        Assert.That(panel.HandleKey(key), Is.True);
        Assert.That(key.consumedBy, Is.EqualTo(nameof(DraftOrderPanelController)));
        Assert.That(panel.homeDie.DisplayedRoll, Is.EqualTo(3));
        Assert.That(panel.IsAwaitingAwayRoll, Is.True);
    }

    [Test]
    public void DraftDiceHaveNoJackpotAnimationsOrOutcomes()
    {
        Assert.That(panel.HandleKey(new KeyPressData(KeyCode.Alpha8, true, true, false)), Is.False);
        Assert.That(panel.IsAwaitingHomeRoll, Is.True);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(DuelDieView).GetField("faceIndex", flags).SetValue(panel.homeDie, 10);
        typeof(DuelDieView).GetField("nextFaceTime", flags).SetValue(panel.homeDie, -1f);
        typeof(DuelDieView).GetMethod("Update", flags).Invoke(panel.homeDie, null);
        Assert.That(panel.homeDie.ShowsJackpot, Is.False);
        Assert.That(panel.homeDie.DisplayedRoll, Is.EqualTo(6));
        panel.Roll(Rig(6, true));
        panel.Roll(Rig(6));
        Assert.That(panel.homeResult.text, Is.EqualTo("Roll 6"));
        Assert.That(panel.awayResult.text, Is.EqualTo("Roll 6"));
        Assert.That(panel.homeDie.ShowsJackpot || panel.awayDie.ShowsJackpot, Is.False);
        Assert.That(panel.outcome.text, Does.Contain("TIE"));
    }

    [Test]
    public void RegularDraftInstructionsUseNamesAndBlockPicksDuringRollOff()
    {
        var manager = root.AddComponent<DraftManager>();
        manager.currentSettings = new GameSettings { homeTeamName = "Aurora F.C.", awayTeamName = "Inverness CT" };
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(DraftManager).GetField("currentTeamTurn", flags).SetValue(manager, "Home");
        typeof(DraftManager).GetField("firstBatchStarter", flags).SetValue(manager, "Away");
        typeof(DraftManager).GetField("currentBatchNumber", flags).SetValue(manager, 1);
        var ui = root.AddComponent<DraftUIManager>();
        typeof(DraftUIManager).GetField("draftManager", flags).SetValue(ui, manager);
        ui.draftTurnText = Label("Turn"); ui.draftBatchText = Label("Batch"); ui.draftMetaText = Label("Meta");
        ui.RefreshDraftStateUI();
        Assert.That(ui.draftTurnText.text, Is.EqualTo("Aurora F.C. PICKS NOW"));
        Assert.That(ui.draftMetaText.text, Does.Contain("Inverness CT starts batch 1"));
        typeof(DraftManager).GetProperty("IsDraftOrderPending").SetValue(manager, true);
        ui.RefreshDraftStateUI();
        Assert.That(ui.draftTurnText.text, Is.EqualTo("ROLL FOR FIRST PICK"));
        Assert.That(manager.IsValidTeamPanel("HomeRoster"), Is.False);
        Assert.That(manager.IsValidTeamPanel("AwayRoster"), Is.False);
    }

    [Test]
    public void SavedRegularDraftBindsPanelAndFreeDraftDoesNotContainIt()
    {
        var regular = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Draft.unity");
        try
        {
            var manager = regular.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DraftManager>(true)).Single();
            Assert.That(manager.draftOrderPanel, Is.Not.Null);
            Assert.That(manager.draftOrderPanel.HasSceneBindings, Is.True);
            Assert.That(manager.draftOrderPanel.gameObject.activeInHierarchy, Is.True);
            Assert.That(manager.draftOrderPanel.visuals.activeSelf, Is.False);
            Assert.That(manager.draftOrderPanel.homeDie.numericFaces.All(sprite => sprite != null), Is.True);
            Assert.That(manager.draftOrderPanel.awayDie.numericFaces.All(sprite => sprite != null), Is.True);
        }
        finally { EditorSceneManager.ClosePreviewScene(regular); }
        var free = EditorSceneManager.OpenPreviewScene("Assets/Scenes/FreeDraft.unity");
        try
        {
            Assert.That(free.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DraftOrderPanelController>(true)), Is.Empty);
        }
        finally { EditorSceneManager.ClosePreviewScene(free); }
    }

    private float Deadline() => (float)typeof(DraftOrderPanelController)
        .GetField("resultUntil", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panel);
    private static RollInputOverride Rig(int roll, bool jackpot = false)
        => new() { hasOverride = true, roll = roll, isJackpot = jackpot };
    private TMP_Text Label(string name) => Rect(name, root.transform).gameObject.AddComponent<TextMeshProUGUI>();
    private DuelDieView Die(string name)
    {
        var rect = Rect(name, root.transform);
        var die = rect.gameObject.AddComponent<DuelDieView>();
        die.background = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        return die;
    }
    private static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
}
