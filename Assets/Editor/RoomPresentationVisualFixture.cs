using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private GameObject LoadRoomVisualStage(bool requestIntro)
    {
        var cache = (Dictionary<string, Material>)typeof(PlayerTokenVisuals).GetField("SharedDecalMaterials", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var previousMaterials = cache.Where(p => p.Value != null).ToArray(); cache.Clear();
        restoreAuthoredPanelState += () => { cache.Clear(); foreach (var pair in previousMaterials) cache[pair.Key] = pair.Value; };
        // Initialize the production grid/path entry point using its authored cell prefab.
        // The older fixture's invisible logical cells remain owned by its root and cleanup.
        Set(grid, "hexCellPrefab", AssetDatabase.LoadAssetAtPath<HexCell>("Assets/Prefabs/HexagonalCell.prefab"));
        grid.cells = null; Invoke(grid, "Start");
        LoadLegacyRoster();
        match.gameData.gameSettings.weatherConditions = "Clear";
        Assert.That(grid.IsGridInitialized(), Is.True);
        var previousApp = ApplicationManager.Instance;
        var app = Child("Isolated Room launch").AddComponent<ApplicationManager>();
        typeof(ApplicationManager).GetProperty(nameof(ApplicationManager.Instance)).SetValue(null, app);
        restoreAuthoredPanelState += () => typeof(ApplicationManager).GetProperty(nameof(ApplicationManager.Instance)).SetValue(null, previousApp);
        if (requestIntro) app.PrepareDraftRoomIntro();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPresentationEditorTools.VisualStagePath);
        Assert.That(prefab, Is.Not.Null);
        var stage = Object.Instantiate(prefab, root.transform);
        foreach (PitchLines lines in stage.GetComponentsInChildren<PitchLines>(true))
        { lines.hexGrid = grid; Set(lines, "finalThirdManager", match.finalThirdManager); }
        foreach (ScoreboardManager scoreboard in stage.GetComponentsInChildren<ScoreboardManager>(true)) scoreboard.matchManager = match;
        foreach (RoomHexEligibilityOverlay overlay in stage.GetComponentsInChildren<RoomHexEligibilityOverlay>(true)) Set(overlay, "grid", grid);
        var panel = stage.GetComponentInChildren<MovementPhaseMovedTokensPanel>(true); Set(panel, "movementPhaseManager", movement);
        var camera = stage.GetComponentInChildren<CameraController>(true); camera.gameInputManager = input; input.cameraController = camera;
        var template = stage.transform.Find("AuthoredTokenVisualTemplate");
        foreach (PlayerToken token in match.playerTokenManager.allTokens.Where(t => t.isPlaying))
        {
            var visual = Object.Instantiate(template.gameObject, token.transform);
            visual.name = "Authored inspection token visual"; visual.transform.localPosition = Vector3.up * .2f;
            string kit = token.isHomeTeam ? match.gameData.gameSettings.homeKit : match.gameData.gameSettings.awayKit;
            if (token.IsGoalKeeper) kit = token.isHomeTeam ? match.gameData.gameSettings.homeGKKit : match.gameData.gameSettings.awayGKKit;
            visual.SetActive(true);
            var styling = visual.GetComponent<PlayerTokenVisuals>();
            var style = TokenKitCatalog.ResolveStyle(kit); styling.ApplyStyle(style);
            var number = visual.GetComponentInChildren<TextMeshPro>(); number.text = token.jerseyNumber.ToString(); styling.ApplyNumberStyle(number, style);
        }
        var ballTemplate = stage.transform.Find("AuthoredBallVisualTemplate");
        var ballVisual = Object.Instantiate(ballTemplate.gameObject, ball.transform);
        ballVisual.transform.localPosition = Vector3.zero; ballVisual.SetActive(true);
        stage.SetActive(true);
        return stage;
    }

    public IEnumerator RunIntroReadinessWithBench()
    {
        var stage = LoadRoomVisualStage(true);
        var intro = stage.GetComponentInChildren<RoomMatchIntro>();
        yield return Until(() => stage.GetComponentInChildren<StadiumCrowdController>().HasAppliedColours
            && stage.GetComponentInChildren<PitchLines>().HasAppliedWeather
            && stage.GetComponentInChildren<CameraController>().IsReady, "authored presentation readiness");
        Assert.That(match.playerTokenManager.allTokens.Count(t => !t.isPlaying && t.GetCurrentHex() == null), Is.GreaterThan(0));
        Assert.That((bool)Invoke(intro, "Ready"), Is.True, "Bench players do not need pitch hexes for the intro to begin.");
    }

    public IEnumerator RunIntroLifecycle(string mode)
    {
        var stage = LoadRoomVisualStage(mode != "load");
        var intro = stage.GetComponentInChildren<RoomMatchIntro>();
        var camera = stage.GetComponentInChildren<CameraController>();
        var hud = stage.GetComponentInChildren<RoomInspectionCard>().GetComponent<CanvasGroup>();
        var kickoff = Add<KickoffManager>(); match.kickoffManager = kickoff; input.kickoffManager = kickoff;
        kickoff.ball = ball; kickoff.hexGrid = grid; kickoff.playerTokenManager = match.playerTokenManager;
        kickoff.freeKickManager = match.freeKickManager; kickoff.groundBallManager = ground;
        kickoff.movementPhaseManager = movement; kickoff.headerManager = match.shotManager.headerManager;
        Subscribe(kickoff); Subscribe(ground); match.currentState = MatchManager.GameState.KickOffSetup; kickoff.StartPreKickoffPhase();
        if (mode == "load")
        {
            yield return null;
            Assert.That(intro.IsHoldingMatch, Is.False); Assert.That(hud.alpha, Is.EqualTo(1f));
            yield break;
        }
        Assert.That(intro.IsHoldingMatch, Is.True); Assert.That(hud.alpha, Is.EqualTo(0f));
        if (mode == "missing")
        {
            Object.DestroyImmediate(stage.GetComponentInChildren<StadiumCrowdController>().gameObject);
            Set(intro, "readinessTimeout", .1f);
        }
        else
        {
            var ui = (GameObject)typeof(RoomMatchIntro).GetField("introUI", Private).GetValue(intro);
            yield return Until(() => ui.activeInHierarchy, "intro begins after readiness");
            var key = new KeyPressData(KeyCode.Return, false, false, false);
            Assert.That(RoomInputOwnership.ConsumeKey(key, true, attacker), Is.True);
            input.SimulateKeyDataPress(KeyCode.Return);
            Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.KickOffSetup));
            input.hoveredToken = attacker; Invoke(input, "ProcessInputs"); Assert.That(input.hoveredToken, Is.Null);
            Assert.That(camera.TryHandlePresetKey(new KeyPressData(KeyCode.Alpha2, false, false, false)), Is.False);
            Assert.That(stage.GetComponentInChildren<RoomTips>().Elapsed, Is.EqualTo(0f));
            if (mode == "button") ui.GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
            else if (mode == "escape") RoomInputOwnership.ConsumeKey(new KeyPressData(KeyCode.Escape, false, false, false), true, attacker);
            if (mode is "button" or "escape") Assert.That(RoomInputOwnership.ReservePauseEscape, Is.True);
        }
        yield return Until(() => !intro.IsHoldingMatch, "intro completion or safe fallback", int.MaxValue);
        Assert.That(hud.alpha, Is.EqualTo(1f)); Assert.That(match.IsGameplayInputBlocked, Is.False);
        var position = camera.transform.position; var rotation = camera.transform.rotation;
        camera.SetCameraToPreset(1);
        Assert.That(Vector3.Distance(camera.transform.position, position), Is.LessThan(.001f));
        Assert.That(Quaternion.Angle(camera.transform.rotation, rotation), Is.LessThan(.01f));
        yield return null;
        Assert.That(RoomInputOwnership.ReservePauseEscape, Is.False);
        input.SimulateKeyDataPress(KeyCode.Return); input.SimulateKeyDataPress(KeyCode.Return);
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.KickoffBlown), "Kickoff input is restored.");
    }

    public IEnumerator RunRoomVisualFrames()
    {
        var stage = LoadRoomVisualStage(false);
        yield return Until(() => stage.GetComponentInChildren<CameraController>().IsReady
            && stage.GetComponentInChildren<StadiumCrowdController>().HasAppliedColours, "visual stage startup");
        var controller = stage.GetComponentInChildren<CameraController>(); controller.enabled = false;
        var camera = controller.GetComponent<Camera>();
        var envelope = (RoomCameraEnvelope)typeof(CameraController).GetField("safeEnvelope", Private).GetValue(controller);
        var overlay = stage.GetComponentInChildren<RoomHexEligibilityOverlay>(); overlay.enabled = false;
        var intro = stage.GetComponentInChildren<RoomMatchIntro>(); intro.enabled = false;
        controller.SetCameraToPreset(1);
        Quaternion beforeZoom = camera.transform.rotation;
        controller.ZoomBy(100f); Assert.That(Quaternion.Angle(beforeZoom, camera.transform.rotation), Is.LessThan(.001f));
        controller.ZoomBy(-100f); Assert.That(Quaternion.Angle(beforeZoom, camera.transform.rotation), Is.LessThan(.001f));
        intro.ApplyPath(3.999f); var beforeTransition = camera.transform.position; var beforeRotation = camera.transform.rotation;
        intro.ApplyPath(4.001f);
        Assert.That(Vector3.Distance(beforeTransition, camera.transform.position), Is.LessThan(.02f));
        Assert.That(Quaternion.Angle(beforeRotation, camera.transform.rotation), Is.LessThan(.05f), "Intro phase transition stays continuous.");
        foreach (float aspect in new[] { 4f / 3f, 16f / 9f, 21f / 9f })
        {
            camera.aspect = aspect;
            for (int preset = 1; preset <= 4; preset++)
            {
                int slot = preset;
                CaptureRoomFrame(camera, $"preset-{preset}-aspect-{aspect:F2}", (int)(720 * aspect), 720, () =>
                {
                    controller.SetCameraToPreset(slot);
                    Assert.That(envelope.ContainsFrustum(camera, camera.transform.position, camera.transform.rotation), Is.True, $"preset {slot}, aspect {aspect}");
                    if (slot <= 2) AssertPitchFits(controller, camera);
                });
            }
        }
        camera.aspect = 16f / 9f;
        var introUI = (GameObject)typeof(RoomMatchIntro).GetField("introUI", Private).GetValue(intro);
        var names = (TMP_Text)typeof(RoomMatchIntro).GetField("teamNames", Private).GetValue(intro);
        names.text = match.gameData.gameSettings.homeTeamName + "  v  " + match.gameData.gameSettings.awayTeamName;
        Invoke(intro, "HideHud"); introUI.SetActive(true);
        foreach (float seconds in new[] { 0f, 4f, 7f, 10f, 15f })
        {
            CaptureRoomFrame(camera, $"intro-{seconds:00}s", 1280, 720, () =>
            {
                intro.ApplyPath(seconds);
                Assert.That(envelope.ContainsFrustum(camera, camera.transform.position, camera.transform.rotation), Is.True, $"intro {seconds}s");
            });
        }
        introUI.SetActive(false); Invoke(intro, "RestoreHud");
        controller.SetCameraToPreset(1);
        foreach (string weather in new[] { "Clear", "Rain", "Snow" })
        {
            match.gameData.gameSettings.weatherConditions = weather;
            Invoke(grid, "ApplyWeatherPaletteFromLoadedSettings"); stage.GetComponentInChildren<PitchLines>().RefreshWeatherMarkings();
            var target = grid.cells.Cast<HexCell>().First(c => c != null && c.CanShootFrom && c.CanHeadFrom && !c.isOutOfBounds);
            Place(attacker, target); target.HighlightHex("CanShootFrom", 1);
            var block = new MaterialPropertyBlock(); target.hexRenderer.GetPropertyBlock(block); Color original = block.GetColor("_MainColor");
            foreach (RoomEligibilityKind kind in new[] { RoomEligibilityKind.Shooting, RoomEligibilityKind.Heading })
            {
                overlay.Render(kind);
                var icons = (RoomHexEligibilityOverlay.Icon[])typeof(RoomHexEligibilityOverlay).GetField("icons", Private).GetValue(overlay);
                var occupied = icons.Single(i => i.coordinates == target.coordinates);
                Assert.That((kind == RoomEligibilityKind.Shooting ? occupied.shooting : occupied.heading).enabled, Is.False, "Occupied hexes have no eligibility icon.");
                bool Enabled(RoomHexEligibilityOverlay.Icon icon)
                {
                    var renderer = kind == RoomEligibilityKind.Shooting ? icon.shooting : icon.heading;
                    return renderer != null && renderer.enabled;
                }
                Assert.That(icons.Any(i => i.coordinates.x < 0 && Enabled(i)), Is.True);
                Assert.That(icons.Any(i => i.coordinates.x > 0 && Enabled(i)), Is.True);
                target.hexRenderer.GetPropertyBlock(block); Assert.That(block.GetColor("_MainColor"), Is.EqualTo(original));
                Assert.That(icons.Where(Enabled)
                    .All(i => (kind == RoomEligibilityKind.Shooting ? i.shooting : i.heading).gameObject.layer == 2), Is.True);
                CaptureRoomFrame(camera, $"overlay-{weather}-{kind}", 1280, 720, () => { controller.SetCameraToPreset(1); overlay.Render(kind); });
            }
            overlay.Render(RoomEligibilityKind.None);
        }
        match.gameData.gameSettings.weatherConditions = "Clear";
        Invoke(grid, "ApplyWeatherPaletteFromLoadedSettings"); stage.GetComponentInChildren<PitchLines>().RefreshWeatherMarkings();
        foreach (var token in new[] { attacker, homeGK })
        {
            var inspection = stage.GetComponentInChildren<RoomInspectionCard>();
            inspection.Toggle(token); Canvas.ForceUpdateCanvases();
            CaptureRoomFrame(camera, "card-" + (token.IsGoalKeeper ? "goalkeeper" : "outfield"), 1280, 720, () => Invoke(inspection, "Resize"));
            inspection.Close();
        }
    }

    private static void AssertPitchFits(CameraController controller, Camera camera)
    {
        Bounds bounds = (Bounds)typeof(CameraController).GetField("pitchBounds", Private).GetValue(controller);
        Rect available = (Rect)Invoke(controller, "GetAvailableViewport", camera);
        for (int i = 0; i < 8; i++)
        {
            Vector3 point = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            Vector3 viewport = camera.WorldToViewportPoint(point);
            Assert.That(viewport.x, Is.InRange(available.xMin - .001f, available.xMax + .001f), "Pitch width must fit beside the panel.");
            Assert.That(viewport.y, Is.InRange(available.yMin - .001f, available.yMax + .001f), $"Pitch height must fit with its margin. position={camera.transform.position}, rotation={camera.transform.eulerAngles}, aspect={camera.aspect}, shift={camera.projectionMatrix[0, 2]}, bounds={bounds}, viewport={available}");
        }
    }

    private static void CaptureRoomFrame(Camera camera, string name, int width, int height, Action configure = null)
    {
        string directory = Path.Combine(SessionState.GetString("CounterAttack.FrameworkResultDirectory", "TestResults/GameplayRegression/Runs/RoomPresentation"), "frames");
        Directory.CreateDirectory(directory);
        var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
        var uiCamera = camera.transform.parent.GetComponentsInChildren<Camera>(true).FirstOrDefault(c => c.name == "PresentationUICamera");
        var previousUiTarget = uiCamera != null ? uiCamera.targetTexture : null;
        var target = new RenderTexture(width, height, 24); Texture2D image = null;
        try
        {
            camera.targetTexture = target;
            if (uiCamera != null) uiCamera.targetTexture = target;
            Canvas.ForceUpdateCanvases(); configure?.Invoke(); Canvas.ForceUpdateCanvases(); camera.Render();
            if (uiCamera != null) uiCamera.Render();
            RenderTexture.active = target;
            image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
            if (uiCamera != null) uiCamera.targetTexture = previousUiTarget;
            if (image != null) Object.DestroyImmediate(image);
            target.Release(); Object.DestroyImmediate(target);
        }
    }
}
