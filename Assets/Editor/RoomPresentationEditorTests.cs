using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class RoomPresentationEditorTests
{
    private Scene scene;
    private GameObject root;
    private MatchManager previousMatch;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp] public void SetUp()
    {
        previousMatch = MatchManager.Instance;
        scene = EditorSceneManager.NewPreviewScene();
        root = new GameObject("Room presentation isolated fixture"); root.SetActive(false);
        SceneManager.MoveGameObjectToScene(root, scene);
    }
    [TearDown] public void TearDown()
    {
        MatchManager.Instance = previousMatch;
        Object.DestroyImmediate(root);
        EditorSceneManager.ClosePreviewScene(scene);
    }

    [TestCase(true)] [TestCase(false)]
    public void MostRecentOverlayWinsAndReleaseRestoresTheOther(bool shotFirst)
    {
        var held = new RoomEligibilityChordState();
        Assert.That(held.Update(true, shotFirst, !shotFirst, false), Is.EqualTo(shotFirst ? RoomEligibilityKind.Shooting : RoomEligibilityKind.Heading));
        Assert.That(held.Update(true, true, true, false), Is.EqualTo(shotFirst ? RoomEligibilityKind.Heading : RoomEligibilityKind.Shooting));
        Assert.That(held.Update(true, shotFirst, !shotFirst, false), Is.EqualTo(shotFirst ? RoomEligibilityKind.Shooting : RoomEligibilityKind.Heading));
        Assert.That(held.Update(false, true, true, false), Is.EqualTo(RoomEligibilityKind.None));
        Assert.That(held.Update(true, false, false, false), Is.EqualTo(RoomEligibilityKind.None));
    }

    [TestCase(true)] [TestCase(false)]
    public void ModalAndFocusLossRequireAReleaseBeforeRearming(bool modal)
    {
        var held = new RoomEligibilityChordState();
        held.Update(true, true, false, false);
        if (modal) held.Update(true, true, false, true); else held.Clear();
        Assert.That(held.Visible, Is.EqualTo(RoomEligibilityKind.None));
        Assert.That(held.Update(true, true, false, false), Is.EqualTo(RoomEligibilityKind.None));
        held.Update(true, false, false, false);
        Assert.That(held.Update(true, false, true, false), Is.EqualTo(RoomEligibilityKind.Heading));
    }

    [Test]
    public void IntroRequestIsOneUseAndExplicitSaveLoadsClearIt()
    {
        var app = root.AddComponent<ApplicationManager>();
        Assert.That(app.ConsumeDraftRoomIntro(), Is.False, "Direct Room testing bypasses intro.");
        app.PrepareDraftRoomIntro();
        Assert.That(app.ConsumeDraftRoomIntro(), Is.True);
        Assert.That(app.ConsumeDraftRoomIntro(), Is.False);
        app.PrepareDraftRoomIntro(); app.SetActiveSaveFilePath("/disposable-test/no-file-read.json");
        Assert.That(app.ConsumeDraftRoomIntro(), Is.False, "A saved-match launch bypasses intro.");
    }

    [TestCase(true)] [TestCase(false)]
    public void IntroSkipReservesHeldInputAndItsReleaseFrame(bool escape)
    {
        var latch = new RoomIntroSkipLatch();
        latch.Begin(escape, !escape, 100);
        latch.Observe(escape, !escape, 101);
        Assert.That(latch.IsConsuming(101), Is.True);
        latch.Observe(false, false, 102);
        Assert.That(latch.IsConsuming(102), Is.True, "Release must not open Pause or reach kickoff.");
        Assert.That(latch.IsConsuming(103), Is.False);
        latch.Clear(); Assert.That(latch.IsConsuming(103), Is.False);
    }

    [TestCase(1.333333f)] [TestCase(1.777778f)] [TestCase(2.333333f)]
    public void BoundarySweepProjectsEveryPoseInsidePositionAndFrustumBounds(float aspect)
    {
        var camera = root.AddComponent<Camera>(); camera.fieldOfView = 50f; camera.aspect = aspect;
        var envelope = root.AddComponent<RoomCameraEnvelope>();
        for (int yaw = 0; yaw < 360; yaw += 30)
        for (int pitch = 20; pitch <= 90; pitch += 10)
        foreach (float distance in new[] { 12f, 40f, 90f, 200f })
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 position = -(rotation * Vector3.forward) * distance;
            envelope.Constrain(camera, Vector3.zero, ref position, ref rotation);
            Assert.That(envelope.PositionEnvelope.Contains(position), Is.True, $"yaw {yaw}, pitch {pitch}, distance {distance}");
            Assert.That(envelope.ContainsFrustum(camera, position, rotation), Is.True, $"yaw {yaw}, pitch {pitch}, distance {distance}");
        }
    }

    [Test]
    public void TipsCycleWithoutRepeatsAndPauseFreezesTheTimer()
    {
        var match = root.AddComponent<MatchManager>(); MatchManager.Instance = match;
        var tips = root.AddComponent<RoomTips>();
        typeof(RoomTips).GetMethod("Start", Private).Invoke(tips, null);
        var values = (string[])typeof(RoomTips).GetField("tips", Private).GetValue(tips);
        var seen = new HashSet<int>();
        int first = tips.CurrentTipIndex;
        for (int i = 0; i < values.Length; i++)
        {
            Assert.That(seen.Add(tips.CurrentTipIndex), Is.True);
            tips.Advance(30f);
        }
        Assert.That(tips.CurrentTipIndex, Is.EqualTo(first));
        tips.Advance(5f); float elapsed = tips.Elapsed;
        match.SetPauseMenuOpen(true); tips.Advance(60f);
        Assert.That(tips.Elapsed, Is.EqualTo(elapsed));
        Assert.That(tips.CurrentTipIndex, Is.EqualTo(first));
        match.SetPauseMenuOpen(false);
        tips.Advance(25f); Assert.That(tips.CurrentTipIndex, Is.Not.EqualTo(first));
    }

    [TestCase("Clear")] [TestCase("Rain")]
    public void SnowChangesOnlyAuthoredMarkingsAndRestoresOriginalWeatherColors(string restoreWeather)
    {
        var match = root.AddComponent<MatchManager>(); MatchManager.Instance = match;
        match.gameData = new MatchManager.GameData();
        var room = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        try
        {
            var lines = room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PitchLines>(true)).Single();
            Transform generated = lines.transform.Find("GeneratedVisuals");
            var marking = generated.Find("PitchMarkings").GetComponentInChildren<MeshRenderer>();
            var net = generated.Find("GoalNets").GetComponentInChildren<MeshRenderer>();
            var spot = generated.Find("PitchDots").GetComponentInChildren<SpriteRenderer>();
            var actionLabel = generated.Find("PitchActionLabels").GetComponentInChildren<MeshRenderer>();
            var originalLabel = new MaterialPropertyBlock(); actionLabel.GetPropertyBlock(originalLabel);
            var originalMark = new MaterialPropertyBlock(); marking.GetPropertyBlock(originalMark);
            var originalNet = new MaterialPropertyBlock(); net.GetPropertyBlock(originalNet);
            Color spotColor = spot.color;
            match.gameData.gameSettings.weatherConditions = "Snow"; lines.RefreshWeatherMarkings();
            var block = new MaterialPropertyBlock(); marking.GetPropertyBlock(block);
            Color orange = block.GetColor("_BaseColor");
            Assert.That(orange.r, Is.GreaterThan(.9f)); Assert.That(orange.g, Is.InRange(.2f, .5f));
            net.GetPropertyBlock(block); Assert.That(block.GetColor("_BaseColor"), Is.EqualTo(originalNet.GetColor("_BaseColor")), "Nets stay white in Snow.");
            Assert.That(Vector4.Distance(spot.color, orange), Is.LessThan(1f / 255f), "SpriteRenderer colors use packed color channels.");
            actionLabel.GetPropertyBlock(block); Assert.That(block.GetColor("_BaseColor"), Is.EqualTo(originalLabel.GetColor("_BaseColor")));
            match.gameData.gameSettings.weatherConditions = restoreWeather; lines.RefreshWeatherMarkings();
            marking.GetPropertyBlock(block); Assert.That(block.GetColor("_BaseColor"), Is.EqualTo(originalMark.GetColor("_BaseColor")));
            Assert.That(spot.color, Is.EqualTo(spotColor));
        }
        finally { EditorSceneManager.ClosePreviewScene(room); }
    }
}
