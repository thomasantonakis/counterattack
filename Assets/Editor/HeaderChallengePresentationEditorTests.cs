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

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void RecoveryReopensRetainedContestAndScatterThenLooseCancellationReleasesHistory(bool goalkeeper, bool wallSave)
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
        if (goalkeeper) token.ConvertToGK();
        token.tackling = goalkeeper ? 0 : 5; token.saving = 6; token.playerName = "Defender";
        int modifier = wallSave ? -1 : 0;
        Assert.That(ui.BeginRecovery(loose, token, wallSave, modifier), Is.True);
        Assert.That(ui.recoveryContent.activeSelf, Is.True);
        Assert.That(ui.direction.result.text, Is.EqualTo("North"));
        Assert.That(ui.distance.result.text, Is.EqualTo("3 hexes"));
        Assert.That(ui.recoveryPlayer.modifier.text,
            Is.EqualTo($"xRecovery {ExpectedStatsCalculator.CalculateRecoveryProbability(goalkeeper ? token.saving : token.tackling, modifier):0.00}"));
        Assert.That(ui.recoveryPlayer.attribute.text, Is.EqualTo(wallSave
            ? "Saving 6 -1 = 5 (Distance Penalty)" : goalkeeper ? "Saving 6" : $"Tackling {token.tackling}"));
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
            var shared = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DuelResolutionPanelController>(true)).Single();
            Assert.That(shared.gameObject.activeInHierarchy && shared.enabled, Is.True,
                "Shared dice controller must initialize in Play Mode; hide its Visuals child, not its root.");
            var recoveryDetails = (RectTransform)shared.looseRecoveryPlayer.playerName.transform.parent;
            var recoveryRoll = (RectTransform)shared.looseRecoveryPlayer.die.transform.parent;
            Assert.That(recoveryDetails.anchorMax.x, Is.EqualTo(.5f));
            Assert.That(recoveryRoll.anchorMin.x, Is.EqualTo(.5f));
            Assert.That(((RectTransform)shared.looseRecoveryPlayer.die.transform).anchoredPosition.y, Is.Zero);
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

    [TestCase(5, 0, 9, "Success: roll 5–6 to save · Roll 4: loose ball · JACKPOT: save")]
    [TestCase(5, -1, 10, "No successful numeric rolls to save · Roll 6: loose ball · JACKPOT: save")]
    [TestCase(5, 0, 50, "JACKPOT: tied save / loose ball · Numeric rolls: goal")]
    public void HeaderSaveGuidanceDistinguishesStrictSaveFromTie(int saving, int modifier, int power, string expected)
        => Assert.That(SingleRollResolutionPresentation.HeaderSaveGuidance(saving, modifier, power), Is.EqualTo(expected));

    [TestCase(1, "No numeric rolls hold the ball")]
    [TestCase(4, "Rolls 1–3: hold · Rolls 4–6: spill")]
    [TestCase(6, "Rolls 1–5: hold · Rolls 6–6: spill")]
    public void HeaderHandlingGuidanceUsesStrictNumericComparison(int handling, string expected)
        => Assert.That(SingleRollResolutionPresentation.HandlingGuidance(handling), Is.EqualTo(expected));

    [TestCase(2, false, "GOAL", false, true, true, 0)]
    [TestCase(4, false, "TIED SAVE", false, true, false, 0)]
    [TestCase(6, false, "SAVED", false, false, true, 0)]
    [TestCase(6, true, "SAVED", false, false, false, 0)]
    [TestCase(6, true, "TIED SAVE", true, true, false, 0)]
    [TestCase(6, false, "SAVED", false, true, true, -1)]
    [TestCase(6, false, "SAVED", false, false, false, -1)]
    public void HeaderSavingUsesAuthoredModalAndReleasesOnManagerDisable(int roll, bool jackpot, string outcome,
        bool headerJackpot, bool attackerHome, bool attackLeft, int saveModifier)
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        try
        {
            var panel = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DuelResolutionPanelController>(true)).Single();
            SetInstance(panel);
            var match = Component<MatchManager>("Match", false);
            match.gameData = new MatchManager.GameData();
            match.gameData.gameSettings.homeKit = "blue";
            match.gameData.gameSettings.awayKit = "red_white";
            match.homeTeamDirection = attackerHome == attackLeft
                ? MatchManager.TeamAttackingDirection.LeftToRight : MatchManager.TeamAttackingDirection.RightToLeft;
            match.awayTeamDirection = attackerHome == attackLeft
                ? MatchManager.TeamAttackingDirection.RightToLeft : MatchManager.TeamAttackingDirection.LeftToRight;
            MatchManager.Instance = match;
            var shot = Component<ShotManager>("Shot", false);
            var keeper = Component<PlayerToken>("Keeper", false);
            keeper.playerName = "Goalkeeper"; keeper.saving = 5;
            keeper.isHomeTeam = !attackerHome;
            var attacker = Component<PlayerToken>("Header winner", false);
            attacker.playerName = "Header winner"; attacker.heading = 5;
            attacker.isHomeTeam = attackerHome;
            int headerPower = headerJackpot ? 50 : 9;
            Assert.That(panel.BeginHeaderSave(shot, () => true, attacker,
                headerJackpot ? 6 : 4, headerPower, 0, keeper, saveModifier, .27f), Is.True);
            Assert.That(panel.playerColumns.activeSelf, Is.True);
            Assert.That(panel.singleRollContent.activeSelf, Is.False);
            var attackerView = attackLeft ? panel.leftPlayer : panel.rightPlayer;
            var keeperView = attackLeft ? panel.rightPlayer : panel.leftPlayer;
            Assert.That(panel.DribblerOnLeft, Is.EqualTo(attackLeft));
            Assert.That(attackerView.playerName.text, Does.Contain("Header winner"));
            Assert.That(keeperView.playerName.text, Does.Contain("Goalkeeper"));
            Assert.That(attackerView.total.text, Does.Contain(headerJackpot ? "JACKPOT" : "Total: 9"));
            Assert.That(attackerView.modifier.text, Does.Contain($"xGoals {.27f:0.00}"));
            Assert.That(attackerView.die.IsAnimating, Is.False, "The winning header must never roll again.");
            Assert.That(keeperView.die.IsAnimating, Is.True);
            Assert.That(keeperView.attribute.text, Is.EqualTo(saveModifier == 0
                ? "Saving 5" : "Saving 5 -1 = 4 (Distance Penalty)"));
            Assert.That(keeperView.modifier.text, Is.EqualTo("GOALKEEPER SAVE"));
            Assert.That(keeperView.total.text, Is.Empty, "Pending Saving shows the animated die without a placeholder label.");
            string frozenHeader = attackerView.total.text;
            var key = new KeyPressData(KeyCode.Alpha4, true, true, false);
            Assert.That(DuelResolutionPanelController.ConsumeKey(key), Is.False,
                "Deterministic overrides must reach ShotManager while awaiting a roll.");
            string randomBefore = JsonUtility.ToJson(UnityEngine.Random.state);
            IEnumerator result = panel.ResolveSingle(shot,
                new GameplayDiceRollResult { roll = roll, jackpotEnabled = true, isJackpot = jackpot }, outcome,
                resolvedTotal: jackpot ? 50 : roll + keeper.saving + saveModifier);
            Assert.That(keeperView.die.ShowsJackpot, Is.EqualTo(jackpot));
            Assert.That(attackerView.total.text, Is.EqualTo(frozenHeader));
            Assert.That(keeperView.total.text, Does.Contain(jackpot ? "JACKPOT" : $"Total: {roll + keeper.saving + saveModifier}"));
            Assert.That(panel.outcome.text, Is.EqualTo(outcome));
            Assert.That(result.MoveNext(), Is.True);
            Assert.That(panel.IsComplete, Is.True);
            Assert.That(JsonUtility.ToJson(UnityEngine.Random.state), Is.EqualTo(randomBefore));
            DuelResolutionPanelController.ConsumeKey(new KeyPressData(KeyCode.Return, false, false, false));
            Assert.That(panel.IsResultHeld, Is.True);
            typeof(ShotManager).GetMethod("OnDisable", Private).Invoke(shot, null);
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(panel.IsResultHeld, Is.False);
            Assert.That(result.MoveNext(), Is.False);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [TestCase(true, true, false, false)]
    [TestCase(true, false, false, false)]
    [TestCase(false, true, false, false)]
    [TestCase(false, false, false, false)]
    [TestCase(true, true, true, false)]
    [TestCase(false, false, false, true)]
    public void ShotComparisonRollsShooterThenKeeperWithoutDismissal(bool attackerHome, bool attackLeft, bool offTarget, bool jackpot)
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        try
        {
            var panel = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DuelResolutionPanelController>(true)).Single();
            SetInstance(panel);
            var match = Component<MatchManager>("Match", false);
            match.gameData = new MatchManager.GameData();
            match.gameData.gameSettings.homeKit = "blue";
            match.gameData.gameSettings.awayKit = "red_white";
            match.homeTeamDirection = attackerHome == attackLeft
                ? MatchManager.TeamAttackingDirection.LeftToRight : MatchManager.TeamAttackingDirection.RightToLeft;
            match.awayTeamDirection = attackerHome == attackLeft
                ? MatchManager.TeamAttackingDirection.RightToLeft : MatchManager.TeamAttackingDirection.LeftToRight;
            MatchManager.Instance = match;
            var manager = Component<ShotManager>("Shot", false);
            var attacker = Component<PlayerToken>("Shooter", false);
            attacker.playerName = "Shooter"; attacker.isHomeTeam = attackerHome;
            var keeper = Component<PlayerToken>("Keeper", false);
            keeper.playerName = "Keeper"; keeper.isHomeTeam = !attackerHome; keeper.saving = 5;
            bool shotPending = true, savePending = false;
            Assert.That(panel.BeginShotComparison(manager, () => shotPending || savePending, "Snapshot", attacker, keeper,
                "Shooting 6 -1 = 5 (snapshot)", "Saving 5 -1 = 4 (Distance Penalty)", .37f), Is.True);
            var attack = attackLeft ? panel.leftPlayer : panel.rightPlayer;
            var defense = attackLeft ? panel.rightPlayer : panel.leftPlayer;
            Assert.That(panel.DribblerOnLeft, Is.EqualTo(attackLeft));
            Assert.That(attack.modifier.text, Does.Contain($"xGoals {.37f:0.00}"));
            Assert.That(attack.die.IsAnimating, Is.True);
            Assert.That(defense.die.IsAnimating, Is.False);
            Assert.That(defense.die.DisplayedRoll, Is.Zero);
            Assert.That(panel.singleRollContent.activeSelf, Is.False);
            Assert.That(panel.playerColumns.activeSelf, Is.True);
            string randomBefore = JsonUtility.ToJson(UnityEngine.Random.state);
            int power = jackpot ? 50 : offTarget ? 6 : 9;
            var shotResult = panel.ResolveSingle(manager,
                new GameplayDiceRollResult { roll = offTarget ? 1 : 4, isJackpot = jackpot, jackpotEnabled = true },
                offTarget ? "OFF TARGET" : "ON TARGET", resolvedTotal: power, continueWithNextRoll: !offTarget);
            shotPending = false;
            if (offTarget)
            {
                Assert.That(shotResult.MoveNext(), Is.True);
                Assert.That(panel.IsComplete, Is.True);
                Assert.That(defense.die.IsAnimating, Is.False);
                Assert.That(panel.ContinueShotSaving(manager, () => savePending, keeper, power, -1, false), Is.False);
            }
            else
            {
                Assert.That(shotResult.MoveNext(), Is.False);
                Assert.That(panel.IsComplete, Is.False, "There must be no countdown between shot and save.");
                string frozen = attack.total.text;
                savePending = true;
                Assert.That(panel.ContinueShotSaving(manager, () => savePending, keeper, power, -1, false), Is.True);
                Assert.That(attack.die.IsAnimating, Is.False);
                Assert.That(attack.die.ShowsJackpot, Is.EqualTo(jackpot));
                Assert.That(defense.die.IsAnimating, Is.True);
                Assert.That(DuelResolutionPanelController.ConsumeKey(new KeyPressData(KeyCode.R, false, false, false)), Is.False);
                var saveResult = panel.ResolveSingle(manager, new GameplayDiceRollResult { roll = 6 }, "SAVED", resolvedTotal: 10);
                Assert.That(saveResult.MoveNext(), Is.True);
                Assert.That(panel.IsComplete, Is.True);
                Assert.That(attack.total.text, Is.EqualTo(frozen));
                Assert.That(defense.total.text, Does.Contain("Total: 10"));
                Assert.That(attack.modifier.text, Does.Contain($"xGoals {.37f:0.00}"),
                    "Attempt xG must remain visible after both dice resolve.");
            }
            Assert.That(JsonUtility.ToJson(UnityEngine.Random.state), Is.EqualTo(randomBefore));
            panel.CancelSingle(manager);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [TestCase(true, true, 1, false)]
    [TestCase(false, true, 4, false)]
    [TestCase(true, false, 5, false)]
    [TestCase(false, false, 6, true)]
    public void FreeKickRestoresShooterBesideWallAndUpdatedSaving(bool home, bool left, int roll, bool jackpot)
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        try
        {
            var panel = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DuelResolutionPanelController>(true)).Single();
            SetInstance(panel);
            var match = AccuracyMatch();
            match.homeTeamDirection = home == left ? MatchManager.TeamAttackingDirection.LeftToRight : MatchManager.TeamAttackingDirection.RightToLeft;
            match.awayTeamDirection = home == left ? MatchManager.TeamAttackingDirection.RightToLeft : MatchManager.TeamAttackingDirection.LeftToRight;
            var attacker = Component<PlayerToken>("FK shooter", false);
            attacker.playerName = "FK shooter"; attacker.shooting = 5; attacker.isHomeTeam = home;
            var keeper = Component<PlayerToken>("FK keeper", false);
            keeper.playerName = "FK keeper"; keeper.isHomeTeam = !home; keeper.ConvertToGK(); keeper.saving = 5;
            var origin = Component<HexCell>("FK origin", false);
            origin.isDifficultShotPosition = true; // Free kicks apply only the outside-box penalty.
            var keeperHex = Component<HexCell>("Keeper hex", false);
            keeperHex.isDefenseOccupied = true; keeperHex.occupyingToken = keeper;
            var shot = Component<ShotManager>("FK shot", false);
            shot.hexGrid = Component<HexGrid>("Grid", false); shot.hexGrid.cells = new[,] { { keeperHex } };
            shot.shotType = "freeKick"; shot.shooter = attacker; shot.isWaitingForShotRoll = true;
            SetField(shot, "freeKickShotOriginHex", origin);
            var begin = typeof(ShotManager).GetMethod("BeginFreeKickShotPresentation", Private);
            string randomBefore = JsonUtility.ToJson(UnityEngine.Random.state);
            Assert.That(begin.Invoke(shot, null), Is.True);
            var attack = left ? panel.leftPlayer : panel.rightPlayer;
            var defense = left ? panel.rightPlayer : panel.leftPlayer;
            Assert.That(panel.DribblerOnLeft, Is.EqualTo(left));
            Assert.That(attack.attribute.text, Is.EqualTo("Shooting 5 -1 = 4 (outside the box)"));
            Assert.That(attack.modifier.text, Does.Contain("xGoals"));
            Assert.That(attack.guidance.text, Does.Contain("Power 9+"));
            Assert.That(attack.die.IsAnimating, Is.True);
            Assert.That(defense.die.IsAnimating, Is.False);
            shot.shooterRoll = roll; shot.totalShotPower = jackpot ? 50 : 4 + roll;
            SetField(shot, "shooterRollWasJackpot", jackpot);
            shot.isWaitingForShotRoll = false;
            string guidance = ShotManager.FreeKickShotGuidance(roll, shot.totalShotPower);
            var result = panel.ResolveSingle(shot,
                new GameplayDiceRollResult { roll = roll, isJackpot = jackpot, jackpotEnabled = true },
                guidance, resolvedTotal: shot.totalShotPower);
            Assert.That(result.MoveNext(), Is.True);
            Assert.That(panel.IsComplete, Is.True);
            string frozen = attack.total.text;
            Assert.That(panel.outcome.text, Is.EqualTo(guidance));
            panel.ToggleResultHold(); Assert.That(panel.IsResultHeld, Is.True);
            Assert.That(panel.Dismiss(), Is.True);
            Assert.That(result.MoveNext(), Is.False);
            Assert.That(DuelResolutionPanelController.IsVisible, Is.False, "The GK box move must receive pitch input.");

            if (roll == 1 || shot.totalShotPower < 9)
            {
                var blocker = Component<PlayerToken>("Wall defender", false);
                blocker.playerName = "Wall defender"; blocker.isHomeTeam = !home;
                shot.isWaitingForBlockDiceRoll = true;
                Assert.That(begin.Invoke(shot, null), Is.True);
                Assert.That(panel.ContinueShotBlock(shot, () => shot.isWaitingForBlockDiceRoll, blocker,
                    "Tackling: 4 · on-path", "Roll 5–6: block", guidance), Is.True);
                Assert.That(attack.total.text, Is.EqualTo(frozen));
                Assert.That(attack.die.IsAnimating, Is.False);
                Assert.That(defense.playerName.text, Does.Contain("Wall defender"));
                Assert.That(defense.die.IsAnimating, Is.True);
                Assert.That(DuelResolutionPanelController.ConsumeKey(new KeyPressData(KeyCode.R, false, false, false)), Is.False);
                var failed = panel.ResolveSingle(shot, new GameplayDiceRollResult { roll = 1 }, "BLOCK FAILED");
                Assert.That(failed.MoveNext(), Is.True); panel.Dismiss();
                shot.isWaitingForBlockDiceRoll = false;
                if (roll == 1) return; // A natural one never reaches Saving.
            }

            // Restore after the box move with a freshly calculated distance penalty.
            var interactionType = typeof(ShotManager).GetNestedType("ShotInteraction", BindingFlags.NonPublic);
            var interaction = Activator.CreateInstance(interactionType, true);
            interactionType.GetField("defender").SetValue(interaction, keeper);
            interactionType.GetField("gkPenalty").SetValue(interaction, (int?)-1);
            var typeField = interactionType.GetField("type");
            typeField.SetValue(interaction, Enum.Parse(typeField.FieldType, "GKSave"));
            shot.isWaitingForGKDiceRoll = true;
            typeof(ShotManager).GetMethod("BeginGroundSavePresentation", Private).Invoke(shot, new[] { interaction });
            Assert.That(panel.IsAwaitingRoll, Is.True);
            Assert.That(attack.total.text, Is.EqualTo(frozen));
            Assert.That(attack.die.IsAnimating, Is.False);
            Assert.That(attack.guidance.text, Is.EqualTo(guidance));
            Assert.That(defense.playerName.text, Does.Contain("FK keeper"));
            Assert.That(defense.attribute.text, Is.EqualTo("Saving 5 -1 = 4 (Distance Penalty)"));
            Assert.That(defense.die.IsAnimating, Is.True);
            Assert.That(JsonUtility.ToJson(UnityEngine.Random.state), Is.EqualTo(randomBefore), "Restoring panels must not roll again.");

            var loose = Component<LooseBallManager>("FK loose ball", false);
            if (!jackpot)
            {
                var save = panel.ResolveSingle(shot, new GameplayDiceRollResult { roll = 6 }, "SAVED", resolvedTotal: 10);
                Assert.That(save.MoveNext(), Is.True); panel.Dismiss();
                shot.isWaitingForGKDiceRoll = false; shot.isWaitingforHandlingTest = true;
                keeper.handling = 4;
                typeof(ShotManager).GetMethod("BeginHeaderHandlingPresentation", Private).Invoke(shot, new object[] { keeper });
                Assert.That(panel.title.text, Is.EqualTo("Free Kick Shot · Handling"));
                Assert.That(panel.singlePlayer.die.IsAnimating, Is.True);
                panel.ResolveSingle(shot, new GameplayDiceRollResult { roll = 6 }, "SPILLED",
                    continueWithScatter: true, scatterOwner: loose);
            }
            else panel.ResolveSingle(shot, new GameplayDiceRollResult { roll = 6, isJackpot = true, jackpotEnabled = true },
                "TIED SAVE", continueWithScatter: true, scatterOwner: loose, resolvedTotal: 50);
            Assert.That(panel.BeginLooseBall(loose, () => true, keeper, true), Is.True);
            Assert.That(panel.scatterContinuationContent.activeSelf, Is.True);
            Assert.That(panel.scatterCaption.text, Does.Contain("FK keeper"));
            Assert.That(panel.continuationDirection.die.IsAnimating, Is.True);
            Assert.That(panel.continuationDistance.die.IsAnimating, Is.False);
            panel.CancelSingle(loose);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [TestCase(1, 10, "Natural 1 · Off target unless blocked")]
    [TestCase(4, 8, "Shot power below 9 · Outfield blocks may attempt")]
    [TestCase(5, 9, "Perfect height · Outfield blocks skipped")]
    [TestCase(6, 50, "Perfect height · Outfield blocks skipped")]
    public void FreeKickPresentationExplainsNaturalOneAndPerfectHeight(int roll, int power, string expected)
        => Assert.That(ShotManager.FreeKickShotGuidance(roll, power), Is.EqualTo(expected));

    [Test]
    public void ShotPanelXGoalsUsesCapturedDefendersWithoutConditioningOnRealizedRolls()
    {
        var manager = Component<ShotManager>("Shot", false);
        var attacker = Component<PlayerToken>("Shooter", false); attacker.shooting = 6;
        var blocker = Component<PlayerToken>("Blocker", false); blocker.tackling = 5;
        var keeper = Component<PlayerToken>("Keeper", false); keeper.saving = 5;
        manager.shooter = attacker;
        manager.shotType = "snapshot";
        var interactionType = typeof(ShotManager).GetNestedType("ShotInteraction", BindingFlags.NonPublic);
        var blockInteraction = Activator.CreateInstance(interactionType, true);
        interactionType.GetField("defender").SetValue(blockInteraction, blocker);
        interactionType.GetField("requiredNaturalRoll").SetValue(blockInteraction, 6);
        ((IList)GetField(manager, "expectedGoalOutfieldBlockInteractions")).Add(blockInteraction);
        var saveInteraction = Activator.CreateInstance(interactionType, true);
        interactionType.GetField("defender").SetValue(saveInteraction, keeper);
        interactionType.GetField("gkPenalty").SetValue(saveInteraction, (int?)-1);
        SetField(manager, "expectedGoalGkSaveInteraction", saveInteraction);
        float expected = ExpectedStatsCalculator.CalculateShotGoalProbability(attacker, 1,
            new[] { new ExpectedStatsCalculator.ShotBlockerExpectation(blocker, 6) }, keeper, -1);
        string randomBefore = JsonUtility.ToJson(UnityEngine.Random.state);
        Assert.That(manager.CalculateCurrentShotExpectedGoals(), Is.EqualTo(expected).Within(.000001f));
        manager.alreadyInterceptedDefs = new List<PlayerToken> { blocker };
        manager.shooterRoll = 1;
        Assert.That(manager.CalculateCurrentShotExpectedGoals(), Is.EqualTo(expected).Within(.000001f));
        manager.shooterRoll = 6; manager.totalShotPower = 50;
        Assert.That(manager.CalculateCurrentShotExpectedGoals(), Is.EqualTo(expected).Within(.000001f));
        Assert.That(GetField(manager, "expectedGoalLogged"), Is.False, "Displaying xG must not log another statistic.");
        Assert.That(JsonUtility.ToJson(UnityEngine.Random.state), Is.EqualTo(randomBefore));
    }

    [TestCase(false, false, false)]
    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    public void ShotScatterRetainsItsOriginAndUsesDefenderTeam(bool handling, bool corner, bool defendingHome)
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        try
        {
            var panel = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DuelResolutionPanelController>(true)).Single();
            SetInstance(panel);
            var match = Component<MatchManager>("Match", false);
            match.gameData = new MatchManager.GameData();
            match.gameData.gameSettings.homeKit = "blue";
            match.gameData.gameSettings.awayKit = "red_white";
            MatchManager.Instance = match;
            var shot = Component<ShotManager>("Shot", false);
            var loose = Component<LooseBallManager>("Loose", false);
            var keeper = Component<PlayerToken>("Deflecting goalkeeper", false);
            keeper.playerName = "Keeper"; keeper.isHomeTeam = defendingHome; keeper.saving = 5;
            var attacker = Component<PlayerToken>("Header winner", false);
            attacker.playerName = "Attacker"; attacker.isHomeTeam = !defendingHome; attacker.heading = 3;
            if (handling)
                Assert.That(panel.BeginSingle(shot, () => true, "Header at Goal · Handling", keeper,
                    "Handling: 4", null, false), Is.True);
            else
                Assert.That(panel.BeginHeaderSave(shot, () => true, attacker, 5, 8, 0, keeper, 0), Is.True);
            string originTitle = panel.title.text;
            string attackerTotal = (attacker.isHomeTeam ? panel.leftPlayer : panel.rightPlayer).total.text;
            var resolve = panel.ResolveSingle(shot, new GameplayDiceRollResult { roll = handling ? 6 : 3 },
                handling ? "SPILLED" : "TIED SAVE", continueWithScatter: true, scatterOwner: loose);
            Assert.That(resolve.MoveNext(), Is.False, "Scatter handoff must not dismiss the origin result.");
            loose.isActivated = true; loose.isWaitingForDirectionRoll = true; loose.causingDeflection = keeper;
            Assert.That(loose.IsInstructionExpectingHomeTeam(), Is.EqualTo(defendingHome));
            Assert.That(panel.BeginLooseBall(loose, () => loose.isWaitingForDirectionRoll, keeper, true), Is.True);
            panel.CancelSingle(shot);
            Assert.That(panel.IsOpen, Is.True, "Origin cleanup must not cancel the transferred scatter.");
            Assert.That(panel.title.text, Is.EqualTo(originTitle));
            Assert.That(panel.scatterContinuationContent.activeSelf, Is.True);
            Assert.That(panel.scatterCaption.text, Does.Contain("Keeper"));
            var settings = match.gameData.gameSettings;
            var palette = TokenKitCatalog.GetInstructionPalette(TokenKitCatalog.ResolveStyle(
                defendingHome ? settings.homeKit : settings.awayKit));
            Assert.That(panel.continuationDirection.die.background.color, Is.EqualTo(palette.Primary));
            Assert.That(panel.continuationDistance.die.background.color, Is.EqualTo(palette.Primary));
            Assert.That(panel.singleRollContent.activeSelf, Is.EqualTo(handling));
            if (!handling)
                Assert.That((attacker.isHomeTeam ? panel.leftPlayer : panel.rightPlayer).total.text, Is.EqualTo(attackerTotal));
            Assert.That(panel.ResolveLooseBallRoll(loose, new GameplayDiceRollResult { roll = 4 }, "North", true).MoveNext(), Is.False);
            if (corner)
            {
                var dismissal = panel.ResolveLooseBallCorner(loose);
                Assert.That(panel.continuationDistance.column.gameObject.activeSelf, Is.False);
                Assert.That(panel.outcome.text, Is.EqualTo(handling ? "SPILLED" : "TIED SAVE"),
                    "The original roll outcome must remain intact.");
                Assert.That(panel.scatterOutcome.text, Is.EqualTo("Corner kick awarded"));
                Assert.That(panel.scatterOutcome.gameObject.activeSelf, Is.True);
                panel.FitAndClamp();
                var resultLine = panel.scatterOutcome.rectTransform;
                var controls = panel.footer.rectTransform;
                Assert.That(resultLine.anchoredPosition.y,
                    Is.GreaterThan(controls.anchoredPosition.y + controls.rect.height));
                var scatter = (RectTransform)panel.scatterContinuationContent.transform;
                Assert.That(panel.panel.rect.height - resultLine.anchoredPosition.y - resultLine.rect.height,
                    Is.GreaterThan(-scatter.anchoredPosition.y + scatter.rect.height));
                Assert.That(dismissal.MoveNext(), Is.True);
                Assert.That(panel.IsComplete, Is.True);
                panel.CancelSingle(loose);
                Assert.That(dismissal.MoveNext(), Is.False);
            }
            else
            {
                loose.isWaitingForDirectionRoll = false; loose.isWaitingForDistanceRoll = true;
                Assert.That(panel.BeginLooseBall(loose, () => loose.isWaitingForDistanceRoll, keeper, false), Is.True);
                Assert.That(panel.continuationDirection.result.text, Does.Contain("North"));
                var movement = panel.ResolveLooseBallRoll(loose, new GameplayDiceRollResult { roll = 2 }, "2 hexes", false,
                    continueWithMovement: true);
                Assert.That(movement.MoveNext(), Is.False, "Shot loose balls continue without the five-second scatter countdown.");
                Assert.That(panel.IsComplete, Is.False);
                loose.isWaitingForDistanceRoll = false; loose.isWaitingForInterceptionRoll = true;
                panel.SuspendLooseBallForExternalInput(loose);
                Assert.That(panel.IsOpen, Is.False, "GK movement must be able to receive external input.");
                Assert.That(panel.BeginLooseBallRecovery(loose, () => loose.isWaitingForInterceptionRoll, keeper), Is.True);
                Assert.That(panel.looseRecoveryPlayer.attribute.text, Is.EqualTo("Saving 5"));
                Assert.That(panel.title.text, Is.EqualTo(originTitle));
                Assert.That(panel.continuationDirection.result.text, Does.Contain("North"));
                panel.CancelSingle(loose);
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [TestCase(false, false, 3, false)]
    [TestCase(false, false, 5, true)]
    [TestCase(false, false, 6, true)]
    [TestCase(false, true, 5, true)]
    [TestCase(true, false, 5, true)]
    [TestCase(true, true, 5, true)]
    public void GoalkeeperRecoveryPresentationMatchesActualInterceptionBranch(bool headerOwned, bool wallSave, int roll, bool recovered)
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        try
        {
            var shared = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DuelResolutionPanelController>(true)).Single();
            SetInstance(shared);
            var header = Panel();
            var match = Component<MatchManager>("Match", false);
            match.gameData = new MatchManager.GameData();
            match.gameData.gameSettings.homeKit = "blue";
            match.gameData.gameSettings.awayKit = "red_white";
            MatchManager.Instance = match;
            var loose = Component<LooseBallManager>("Loose", false);
            var keeper = Component<PlayerToken>("Goalkeeper", false); keeper.ConvertToGK();
            keeper.saving = 6; keeper.tackling = 0; keeper.playerName = "Keeper";
            loose.potentialInterceptor = keeper; loose.interceptionRoll = roll;
            loose.isActivated = true; loose.isWaitingForInterceptionRoll = true;
            if (headerOwned)
            {
                SetField(loose, "headerPresentationOwner", header);
                SetField(header, "scatterHistoryOwner", loose);
            }
            else SetField(shared, "scatterHistoryOwner", loose);
            typeof(LooseBallManager).GetMethod("BeginRecoveryPresentation", Private).Invoke(loose, new object[] { wallSave, wallSave ? -1 : 0 });
            var view = headerOwned ? header.recoveryPlayer : shared.looseRecoveryPlayer;
            Assert.That(view.playerName.text, Does.Contain("Keeper"));
            Assert.That(view.attribute.text, Is.EqualTo(wallSave ? "Saving 6 -1 = 5 (Distance Penalty)" : "Saving 6"));
            Assert.That(view.guidance.text, Does.Contain(wallSave ? "5–6" : "4–6"));
            Assert.That(view.modifier.text, Is.EqualTo($"xRecovery {(wallSave ? 2f / 6f : 3f / 6f):0.00}"));
            Assert.That(view.die.IsAnimating, Is.True);
            Assert.That(loose.IsInstructionExpectingHomeTeam(), Is.EqualTo(keeper.isHomeTeam));
            Assert.That(typeof(LooseBallManager).GetMethod("CurrentRecoverySucceeds", Private).Invoke(loose, null), Is.EqualTo(recovered));
            if (!headerOwned)
            {
                var resolve = shared.ResolveLooseBallRecovery(loose, new GameplayDiceRollResult { roll = roll }, recovered);
                Assert.That(resolve.MoveNext(), Is.True);
                Assert.That(view.total.text, Does.Contain(recovered ? "BALL RECOVERED" : "RECOVERY FAILED"));
                shared.CancelSingle(loose);
            }
            else header.Cancel(loose);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [TestCase(MatchManager.GameState.GoalKick, false, false, 0, true)] // Offer before K.
    [TestCase(MatchManager.GameState.GoalKick, true, true, 0, true)]
    [TestCase(MatchManager.GameState.HighPass, true, true, 5, true)] // Committed GK kick, 21 hexes.
    [TestCase(MatchManager.GameState.HighPass, true, true, 9, false)] // Opposite final third.
    [TestCase(MatchManager.GameState.HighPass, true, true, -14, false)] // Below the minimum.
    [TestCase(MatchManager.GameState.HighPass, true, false, 0, false)] // Ordinary HP still capped.
    [TestCase(MatchManager.GameState.HighPass, true, false, -4, true)]
    public void GoalkeeperTargetMapUsesExtendedRangeBeforeAndAfterCommit(MatchManager.GameState state,
        bool activated, bool goalkeeperKick, int targetX, bool allowed)
    {
        var match = AccuracyMatch(); match.currentState = state;
        var hp = Component<HighPassManager>("Goal-kick target map", false);
        hp.isActivated = activated; hp.isGoalkeeperKick = goalkeeperKick; hp.minPassDistance = 3;
        var source = Component<HexCell>("Goal-kick spot", false);
        source.coordinates = new Vector3Int(-16, 0, 0); source.isInFinalThird = -1;
        var target = Component<HexCell>("Receiving target", false);
        target.coordinates = new Vector3Int(targetX, 0, 0); target.isInFinalThird = targetX >= 8 ? 1 : targetX <= -8 ? -1 : 0;
        var receiver = Component<PlayerToken>("Reachable receiver", false);
        var reachable = new Dictionary<HexCell, List<PlayerToken>> { [target] = new() { receiver } };
        var result = typeof(HighPassManager).GetMethod("IsHighPassTargetAvailableForPrecompute", Private)
            .Invoke(hp, new object[] { target, source, reachable, new HashSet<HexCell>() });
        Assert.That(result, Is.EqualTo(allowed));
        var preview = (GameplayActionPreview)typeof(HighPassManager).GetMethod("BuildHighPassTargetPreview", Private)
            .Invoke(hp, new object[] { target, false, "", false });
        bool goalkeeperRules = goalkeeperKick || (!activated && state == MatchManager.GameState.GoalKick);
        Assert.That(preview.imposedMaxDistance, Is.EqualTo(goalkeeperRules ? 0 : 15),
            "Telemetry must not reintroduce a 15-hex goalkeeper-kick limit.");
    }

    [TestCase(false, false, false)] // Free kick / regular high pass.
    [TestCase(true, false, false)] // Corner skips the movement phases.
    [TestCase(false, true, false)] // Goalkeeper distribution.
    [TestCase(false, true, true)] // Goal kick.
    public void HighPassAccuracyShowsTheRestartTakerAndAnimatedTeamDie(bool corner, bool goalkeeper, bool goalKick)
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        Material targetMaterial = null;
        try
        {
            var panel = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DuelResolutionPanelController>(true)).Single();
            SetInstance(panel);
            AccuracyMatch();
            var passer = Component<PlayerToken>("Restart taker", false);
            passer.playerName = "Restart taker"; passer.isHomeTeam = false; passer.highPass = 4;
            if (goalkeeper) passer.ConvertToGK();
            // Keep LastToken unset: the explicitly selected restart taker remains authoritative.
            var hp = Component<HighPassManager>("HP", false);
            hp.isActivated = true; hp.isCornerKick = corner; hp.isGoalkeeperKick = goalkeeper;
            SetProperty(hp, "IsDirectGoalKick", goalKick);
            hp.SetPendingSetPieceTakerForCommit(passer);
            hp.hexGrid = Component<HexGrid>("Grid", false);
            if (corner)
            {
                var target = Component<HexCell>("Corner target", false);
                target.hexRenderer = target.gameObject.AddComponent<MeshRenderer>();
                targetMaterial = new Material(Shader.Find("Sprites/Default"));
                target.hexRenderer.material = targetMaterial;
                typeof(HighPassManager).GetMethod("ConfirmHighPassTargetSelection", Private)
                    .Invoke(hp, new object[] { target, false });
            }
            else typeof(HighPassManager).GetMethod("ForfeitDefenderHighPassMove", Private).Invoke(hp, null);
            Assert.That(hp.isWaitingForAccuracyRoll, Is.True);
            AssertAccuracyPanel(panel, passer, "High Pass Accuracy", 8, "Success: roll 4–6");
            Assert.That(panel.singlePlayer.modifier.text, Is.EqualTo($"Accuracy {0.5f:P1}"));

            // Pending checks can recover after an overlapping originating result closes.
            panel.CancelSingle(hp);
            typeof(HighPassManager).GetMethod("LateUpdate", Private).Invoke(hp, null);
            Assert.That(panel.IsOpen, Is.True);
            panel.CancelSingle(hp);
        }
        finally
        {
            if (targetMaterial != null) Object.DestroyImmediate(targetMaterial);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [TestCase(-1, 1, 4, 10, "Success: roll 6", 1f / 6f)]
    [TestCase(1, -1, 4, 10, "Success: roll 6", 1f / 6f)]
    [TestCase(-1, -1, 4, 9, "Success: roll 5–6", 2f / 6f)]
    [TestCase(1, 1, 4, 9, "Success: roll 5–6", 2f / 6f)]
    [TestCase(0, 1, 4, 9, "Success: roll 5–6", 2f / 6f)]
    [TestCase(-1, 0, 4, 9, "Success: roll 5–6", 2f / 6f)]
    [TestCase(0, 0, 4, 9, "Success: roll 5–6", 2f / 6f)]
    [TestCase(-1, 1, 3, 10, "Success: jackpot only", 1f / 12f)]
    [TestCase(-1, -1, 3, 9, "Success: roll 6", 1f / 6f)]
    public void LongBallFreeKickUsesAdjacentTakerAndActualBallThirds(int sourceThird, int targetThird,
        int highPass, int threshold, string guidance, float probability)
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        try
        {
            var panel = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<DuelResolutionPanelController>(true)).Single();
            SetInstance(panel);
            var match = AccuracyMatch();
            match.helperFunctions = Component<HelperFunctions>("Dice helpers", false);
            var source = Component<HexCell>("Unoccupied restart spot", false); source.isInFinalThird = sourceThird;
            var target = Component<HexCell>("LB target", false); target.isInFinalThird = targetThird;
            var passer = Component<PlayerToken>("Adjacent free kick taker", false);
            passer.playerName = "Adjacent taker"; passer.isHomeTeam = true; passer.highPass = highPass;
            match.LastTokenToTouchTheBallOnPurpose = passer;
            var lb = Component<LongBallManager>("LB", false);
            lb.ball = Component<Ball>("Ball", false); SetField(lb.ball, "currentCell", source);
            lb.currentTargetHex = target;
            var priorOwner = Component<ShotManager>("Previous result", false);
            Assert.That(panel.BeginSingle(priorOwner, () => true, "Previous result", passer, "", "", true), Is.True);
            // A stale cached danger flag must not determine the displayed range or probability.
            SetField(lb, "isDangerous", threshold != 10);
            typeof(LongBallManager).GetMethod("BeginAccuracyRoll", Private).Invoke(lb, null);
            Assert.That(panel.title.text, Is.EqualTo("Previous result"), "Accuracy must not replace an originating result.");
            panel.CancelSingle(priorOwner);
            typeof(LongBallManager).GetMethod("LateUpdate", Private).Invoke(lb, null);
            Assert.That(source.GetOccupyingToken(), Is.Null);
            AssertAccuracyPanel(panel, passer, "Long Ball Accuracy", threshold, guidance);
            Assert.That(panel.singlePlayer.attribute.text.Contains("Opposite final thirds"), Is.EqualTo(threshold == 10));
            Assert.That(lb.AccuracyProbability(highPass), Is.EqualTo(probability).Within(.00001f));
            Assert.That(panel.singlePlayer.modifier.text, Is.EqualTo($"Accuracy {probability:P1}"));
            panel.CancelSingle(lb);
            typeof(LongBallManager).GetMethod("LateUpdate", Private).Invoke(lb, null);
            Assert.That(panel.IsOpen, Is.True);
            panel.CancelSingle(lb);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private MatchManager AccuracyMatch()
    {
        var match = Component<MatchManager>("Accuracy match", false);
        match.gameData = new MatchManager.GameData(); match.difficulty_level = 3;
        match.gameData.gameSettings.homeTeamName = "Home"; match.gameData.gameSettings.awayTeamName = "Away";
        match.gameData.gameSettings.homeKit = "blue"; match.gameData.gameSettings.awayKit = "red_white";
        MatchManager.Instance = match;
        return match;
    }

    private static void AssertAccuracyPanel(DuelResolutionPanelController panel, PlayerToken passer,
        string title, int threshold, string guidance)
    {
        Assert.That(panel.IsOpen && panel.IsAwaitingRoll && panel.visuals.activeSelf, Is.True);
        Assert.That(panel.title.text, Is.EqualTo(title));
        Assert.That(panel.singlePlayer.playerName.text, Does.Contain(passer.playerName));
        Assert.That(panel.singlePlayer.teamName.text, Is.EqualTo(passer.isHomeTeam ? "Home" : "Away"));
        Assert.That(panel.singlePlayer.attribute.text, Does.Contain($"High Pass {passer.highPass} · Target {threshold}"));
        Assert.That(panel.singlePlayer.guidance.text, Is.EqualTo(guidance));
        Assert.That(panel.singlePlayer.die.IsAnimating, Is.True);
        var settings = MatchManager.Instance.gameData.gameSettings;
        var colors = TokenKitCatalog.GetInstructionPalette(TokenKitCatalog.ResolveStyle(passer.isHomeTeam ? settings.homeKit : settings.awayKit));
        Assert.That(panel.singlePlayer.die.background.color, Is.EqualTo(colors.Primary));
        Assert.That(panel.singlePlayer.die.face.color, Is.EqualTo(colors.Secondary));
        Assert.That(DuelResolutionPanelController.ConsumeKey(new KeyPressData(KeyCode.R, false, false, false)), Is.False);
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
