using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class HeaderChallengePresentationEditorTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject root;
    private MatchManager oldMatch;
    private HeaderChallengePanelController oldHeader;
    private DuelResolutionPanelController oldDuel;
    private StoppageActionsPanelController oldStoppage;

    [SetUp]
    public void SetUp()
    {
        oldMatch = MatchManager.Instance;
        oldHeader = HeaderChallengePanelController.Instance;
        oldDuel = DuelResolutionPanelController.Instance;
        oldStoppage = StoppageActionsPanelController.Instance;
        MatchManager.Instance = null;
        SetInstance<HeaderChallengePanelController>(null);
        SetInstance<DuelResolutionPanelController>(null);
        SetInstance<StoppageActionsPanelController>(null);
        root = new GameObject("Header presentation test fixture");
    }

    [TearDown]
    public void TearDown()
    {
        // Restore globals before destroying the fake match so its exit hook cannot flush a live save.
        MatchManager.Instance = oldMatch;
        SetInstance(oldHeader);
        SetInstance(oldDuel);
        SetInstance(oldStoppage);
        Object.DestroyImmediate(root);
    }

    public static IEnumerable<TestCaseData> CameraCases()
    {
        foreach (string modal in new[] { "Header", "Duel", "Stoppage" })
        for (int index = 1; index <= 4; index++)
        foreach (bool numpad in new[] { false, true })
        foreach (bool shift in new[] { false, true })
            yield return new TestCaseData(modal,
                (KeyCode)((numpad ? (int)KeyCode.Keypad0 : (int)KeyCode.Alpha0) + index), index, shift);
    }

    [TestCaseSource(nameof(CameraCases))]
    public void HeldModalAllowsCameraPresetWithoutReleasingHold(string modal, KeyCode key, int index, bool shift)
    {
        object panel;
        if (modal == "Header")
        {
            var header = Component<HeaderChallengePanelController>("Header");
            SetProperty(header, "IsOpen", true); SetField(header, "complete", true);
            SetProperty(header, "IsResultHeld", true); SetInstance(header); panel = header;
        }
        else if (modal == "Duel")
        {
            var duel = Component<DuelResolutionPanelController>("Duel");
            SetStage(duel, "Complete"); SetProperty(duel, "IsResultHeld", true);
            SetInstance(duel); panel = duel;
        }
        else
        {
            var stoppage = Component<StoppageActionsPanelController>("Stoppage");
            SetStage(stoppage, "Complete"); SetProperty(stoppage, "IsResultHeld", true);
            SetInstance(stoppage); panel = stoppage;
        }
        var camera = Component<CameraController>("Camera");
        var input = Component<GameInputManager>("Input"); input.cameraController = camera;
        camera.transform.position = new Vector3(3, 25, -18);
        Vector3 original = camera.transform.position;
        var positions = (Vector3[])GetField(camera, "presetPositions");
        positions[index - 1] = new Vector3(index * 5, 35, -30);
        var chord = new KeyPressData(key, shift, false, false);
        bool handled = (bool)typeof(GameInputManager).GetMethod("TryHandleModalCameraPreset", Private)
            .Invoke(input, new object[] { chord });
        Assert.That(handled, Is.True);
        Assert.That(chord.consumedBy, Is.EqualTo(nameof(CameraController)));
        Assert.That(GetProperty(panel, "IsResultHeld"), Is.True);
        if (shift)
        {
            Assert.That(positions[index - 1], Is.EqualTo(original));
            Assert.That(camera.transform.position, Is.EqualTo(original));
        }
        else Assert.That(camera.transform.position, Is.Not.EqualTo(original));
    }

    [TestCase(KeyCode.Alpha1)] [TestCase(KeyCode.Alpha2)] [TestCase(KeyCode.Alpha3)] [TestCase(KeyCode.Alpha4)]
    [TestCase(KeyCode.Keypad1)] [TestCase(KeyCode.Keypad2)] [TestCase(KeyCode.Keypad3)] [TestCase(KeyCode.Keypad4)]
    public void CameraDoesNotConsumeDeterministicRollOverrides(KeyCode key)
    {
        var camera = Component<CameraController>("Camera");
        var chord = new KeyPressData(key, true, true, false);
        Assert.That(camera.TryHandlePresetKey(chord), Is.False);
        Assert.That(chord.isConsumed, Is.False);
        Assert.That(RollInputOverride.TryParse(chord, out var roll), Is.True);
        Assert.That(roll.hasOverride, Is.True);
    }

    [TestCase(LooseBallPresentationContext.Standalone)]
    [TestCase(LooseBallPresentationContext.ShotResolution)]
    public void ScatterHandoffSurvivesOriginCleanupAndPreservesExplicitContext(LooseBallPresentationContext context)
    {
        var ui = Panel();
        var header = Component<HeaderManager>("Header manager", false);
        var loose = Component<LooseBallManager>("Loose manager", false);
        SetProperty(loose, "PresentationContext", context);
        SetField(ui, "owner", header); SetProperty(ui, "IsOpen", true);
        ui.TransferScatter(header, loose);
        ui.Cancel(header);
        Assert.That(ui.Owns(loose), Is.True, "Origin cleanup must not dismiss the transferred expansion.");
        Assert.That(ui.scatter.activeSelf, Is.True);
        Assert.That(loose.PresentationContext, Is.EqualTo(context));
        ui.Cancel(loose);
        Assert.That(ui.IsOpen, Is.False);
    }

    [Test]
    public void RecoveryReopensRetainedContestAndScatterThenLooseCancellationReleasesHistory()
    {
        var ui = Panel();
        var header = Component<HeaderManager>("Header manager", false);
        var loose = Component<LooseBallManager>("Loose manager", false);
        SetField(ui, "owner", header); SetProperty(ui, "IsOpen", true);
        ui.TransferScatter(header, loose);
        ui.direction.result.text = "North"; ui.distance.result.text = "3 hexes";
        ui.Close();
        var match = Component<MatchManager>("Match", false);
        match.gameData = new MatchManager.GameData();
        match.gameData.gameSettings.homeKit = "blue";
        match.gameData.gameSettings.awayKit = "red_white";
        MatchManager.Instance = match;
        var token = Component<PlayerToken>("Recovering outfielder", false);
        token.tackling = 5; token.playerName = "Defender";
        Assert.That(ui.BeginRecovery(loose, token), Is.True);
        Assert.That(ui.recoveryContent.activeSelf, Is.True);
        Assert.That(ui.direction.result.text, Is.EqualTo("North"));
        Assert.That(ui.distance.result.text, Is.EqualTo("3 hexes"));
        Assert.That(ui.recoveryPlayer.modifier.text,
            Is.EqualTo($"xRecovery {ExpectedStatsCalculator.CalculateRecoveryProbability(token):0.00}"));
        ui.Cancel(loose);
        Assert.That(ui.BeginRecovery(loose, token), Is.False, "Cancelled history must not reopen a stale continuation.");
    }

    [Test]
    public void ScatterHasNoJackpotsAndCosmeticPresentationDoesNotAdvanceGameplayRandomness()
    {
        var ui = Panel();
        var header = Component<HeaderManager>("Header manager", false);
        var loose = Component<LooseBallManager>("Loose manager", false);
        SetField(ui, "owner", header); SetProperty(ui, "IsOpen", true);
        string randomBefore = JsonUtility.ToJson(UnityEngine.Random.state);
        ui.TransferScatter(header, loose);
        ui.BeginScatter(loose, true);
        var display = ui.ResolveScatter(loose,
            new GameplayDiceRollResult { roll = 4, isJackpot = true, jackpotEnabled = false }, "North", true);
        Assert.That(display.MoveNext(), Is.False);
        Assert.That(ui.direction.die.ShowsJackpot, Is.False);
        Assert.That(ui.direction.die.DisplayedRoll, Is.EqualTo(4));
        ui.BeginScatter(loose, false);
        Assert.That(ui.direction.result.text, Does.Contain("North"));
        Assert.That(JsonUtility.ToJson(UnityEngine.Random.state), Is.EqualTo(randomBefore));
    }

    [TestCase(false)] [TestCase(true)]
    public void RecoveryDisplaysManagerOutcomeAndWaitsForDismissal(bool recovered)
    {
        var ui = Panel();
        var loose = Component<LooseBallManager>("Loose manager", false);
        SetField(ui, "owner", loose); SetProperty(ui, "IsOpen", true); ui.recoveryContent.SetActive(true);
        IEnumerator resolve = ui.ResolveRecovery(loose, new GameplayDiceRollResult { roll = 6 }, recovered);
        Assert.That(resolve.MoveNext(), Is.True);
        Assert.That(ui.recoveryPlayer.total.text, Does.Contain(recovered ? "BALL RECOVERED" : "RECOVERY FAILED"));
        var dismissal = (IEnumerator)resolve.Current;
        Assert.That(dismissal.MoveNext(), Is.True);
        Assert.That(GetField(ui, "complete"), Is.True);
        ui.Cancel(loose);
        Assert.That(dismissal.MoveNext(), Is.False);
    }

    [Test]
    public void EqualDefendersRetainEarlierOutfielderButPreferGoalkeeperWithoutRaisingScore()
    {
        var header = Component<HeaderManager>("Header manager", false);
        var earlier = Component<PlayerToken>("Earlier", false);
        var later = Component<PlayerToken>("Later", false);
        var keeper = Component<PlayerToken>("Keeper", false); keeper.ConvertToGK();
        header.defenderWillJump.AddRange(new[] { earlier, later, keeper });
        header.tokenScores[earlier] = (4, 9); header.tokenScores[later] = (5, 9);
        Assert.That(header.GetBestHeaderCandidate(header.defenderWillJump, true), Is.SameAs(earlier));
        header.tokenScores[keeper] = (3, 9);
        Assert.That(header.GetBestHeaderCandidate(header.defenderWillJump, true), Is.SameAs(keeper));
        Assert.That(header.tokenScores[keeper].totalScore, Is.EqualTo(header.tokenScores[earlier].totalScore));
    }

    [Test]
    public void SavedRoomBindsAllDiceAndExpandedFooterClearsRecovery()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        try
        {
            var ui = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<HeaderChallengePanelController>(true)).Single();
            Assert.That(ui.HasSceneBindings, Is.True);
            Assert.That(ui.GetComponentsInChildren<DuelDieView>(true).Length, Is.EqualTo(8));
            Assert.That(ui.attackers.Concat(ui.defenders).All(row => row.root.transform.Find("Border") != null), Is.True);
            ui.scatter.SetActive(true); ui.recoveryContent.SetActive(true); ui.FitAndClamp();
            var recovery = (RectTransform)ui.recoveryContent.transform;
            float recoveryBottom = -recovery.anchoredPosition.y + recovery.rect.height;
            float footerTop = ui.panel.rect.height - ui.footer.rectTransform.anchoredPosition.y - ui.footer.rectTransform.rect.height;
            Assert.That(footerTop, Is.GreaterThan(recoveryBottom));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private HeaderChallengePanelController Panel()
    {
        var ui = Component<HeaderChallengePanelController>("Header panel");
        ui.visuals = Rect("Visuals", ui.transform).gameObject;
        ((RectTransform)ui.visuals.transform).sizeDelta = new Vector2(3840, 2160);
        ui.panel = Rect("Panel", ui.visuals.transform);
        ui.scatter = Rect("Scatter", ui.panel).gameObject;
        ui.recoveryContent = Rect("Recovery", ui.panel).gameObject;
        ui.footer = Label("Footer", ui.panel);
        ui.direction = new DuelCheckPanelFields { die = Component<DuelDieView>("Direction"), result = Label("Direction result", ui.panel) };
        ui.distance = new DuelCheckPanelFields { die = Component<DuelDieView>("Distance"), result = Label("Distance result", ui.panel) };
        ui.recoveryPlayer = new DuelPlayerPanelFields
        {
            playerName = Label("Player", ui.panel), teamName = Label("Team", ui.panel),
            attribute = Label("Attribute", ui.panel), modifier = Label("Expected recovery", ui.panel),
            guidance = Label("Guidance", ui.panel), total = Label("Result", ui.panel),
            die = Component<DuelDieView>("Recovery die")
        };
        return ui;
    }
    private T Component<T>(string name, bool active = true) where T : Component
    {
        var go = new GameObject(name); go.SetActive(active); go.transform.SetParent(root.transform, false);
        return go.AddComponent<T>();
    }
    private static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
    private static TMP_Text Label(string name, Transform parent) => Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
    private static void SetInstance<T>(T value) where T : Component
        => typeof(T).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public).SetValue(null, value);
    private static void SetField(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    private static object GetField(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    private static void SetProperty(object target, string name, object value) => target.GetType().GetProperty(name).SetValue(target, value);
    private static object GetProperty(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
    private static void SetStage(object target, string value)
    {
        var field = target.GetType().GetField("stage", Private);
        field.SetValue(target, Enum.Parse(field.FieldType, value));
    }
}
