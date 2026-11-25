using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[Category("StadiumGoalEffects")]
public sealed class StadiumGoalEffectsEditorTests
{
    private Scene scene;
    private GameObject root;
    private RoomLightingController lighting;
    private RoomLightingPreset preset;
    private Renderer[] tiles;
    private readonly TokenKitInstructionPalette palette = new(Color.red, Color.blue);

    [SetUp]
    public void SetUp()
    {
        scene = EditorSceneManager.NewPreviewScene();
        root = new GameObject("Owned goal effects fixture");
        root.SetActive(false);
        SceneManager.MoveGameObjectToScene(root, scene);
        lighting = root.AddComponent<RoomLightingController>();
        lighting.floodlights = Enumerable.Range(0, 4).Select(i => Child("Flood " + i).AddComponent<Light>()).ToArray();
        lighting.ribbon = Child("Ribbon").AddComponent<StadiumRibbonController>();
        tiles = Enumerable.Range(0, 256).Select(i => (Renderer)Child("LED " + i).AddComponent<MeshRenderer>()).ToArray();
        lighting.ribbon.Configure(tiles);
        preset = ScriptableObject.CreateInstance<RoomLightingPreset>();
        preset.floodlights = true;
        preset.floodlightIntensity = 3;
        preset.ribbonColor = Color.green;
        preset.ribbonBrightness = 1;
        lighting.sunset = lighting.lateNight = preset;
        root.SetActive(true);
        lighting.Apply(FixtureLighting.LateNight);
    }

    private GameObject Child(string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(root.transform, false);
        return child;
    }

    private Color TileColor(int i)
    {
        var block = new MaterialPropertyBlock();
        tiles[i].GetPropertyBlock(block);
        return block.GetColor("_Color");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(preset);
        EditorSceneManager.ClosePreviewScene(scene);
    }

    [TestCase(FixtureLighting.MidAfternoon, "Club", true, false)]
    [TestCase(FixtureLighting.MidAfternoon, "International", false, false)]
    [TestCase(FixtureLighting.Sunset, "Club", true, true)]
    [TestCase(FixtureLighting.Sunset, "Club", false, false)]
    [TestCase(FixtureLighting.LateNight, "Club", true, true)]
    [TestCase(FixtureLighting.LateNight, "Club", false, false)]
    [TestCase(FixtureLighting.Sunset, "International", true, true)]
    [TestCase(FixtureLighting.Sunset, "International", false, true)]
    [TestCase(FixtureLighting.LateNight, "International", true, true)]
    [TestCase(FixtureLighting.LateNight, "International", false, true)]
    public void OnlyEligibleGoalsAnimate(FixtureLighting time, string match, bool home, bool expected)
    {
        lighting.midAfternoon = preset;
        lighting.Apply(time);
        lighting.BeginGoalEffects(home, match, palette);
        Assert.That(lighting.GoalEffectsActive, Is.EqualTo(expected));
        if (!expected) Assert.That(TileColor(0), Is.EqualTo(Color.green));
    }

    [Test]
    public void CelebrationSwapsFiveLedBlocksAndPauseDoesNotAdvance()
    {
        lighting.BeginGoalEffects(true, "Club", palette);
        for (int i = 0; i < 20; i++)
            Assert.That(TileColor(i), Is.EqualTo(i / 5 % 2 == 0 ? Color.red : Color.blue));
        lighting.AdvanceGoalEffects(0);
        Assert.That(TileColor(0), Is.EqualTo(Color.red));
        lighting.AdvanceGoalEffects(.401f);
        Assert.That(TileColor(0), Is.EqualTo(Color.blue));
        Assert.That(TileColor(5), Is.EqualTo(Color.red));
    }

    [Test]
    public void ChaseMovesFiveTriplesClockwiseAcrossSeamAndCompletesOneLap()
    {
        lighting.BeginGoalEffects(false, "International", palette);
        lighting.BeginGoalReturn(256);
        Assert.That(tiles.Count(t => TileColor(System.Array.IndexOf(tiles, t)) == Color.blue), Is.EqualTo(15));
        lighting.AdvanceGoalEffects(255);
        Assert.That(TileColor(255), Is.EqualTo(Color.blue));
        Assert.That(TileColor(0), Is.EqualTo(Color.blue));
        Assert.That(TileColor(1), Is.EqualTo(Color.blue));
        Assert.That(TileColor(2), Is.EqualTo(Color.red));
        lighting.AdvanceGoalEffects(1);
        Assert.That(TileColor(0), Is.EqualTo(Color.blue));
        Assert.That(TileColor(1), Is.EqualTo(Color.blue));
        Assert.That(TileColor(2), Is.EqualTo(Color.blue));
        Assert.That(TileColor(3), Is.EqualTo(Color.red));
        lighting.AdvanceGoalEffects(50);
        Assert.That(TileColor(3), Is.EqualTo(Color.red), "Chase must not start another lap");
    }

    [Test]
    public void FourFloodlightDipsRestoreEachOriginalIntensityAndCleanupRestoresRibbon()
    {
        for (int i = 0; i < 4; i++) lighting.floodlights[i].intensity = i + 2;
        lighting.BeginGoalEffects(true, "Club", palette);
        for (int step = 0; step < 8; step++)
        {
            if (step > 0) lighting.AdvanceGoalEffects(.15001f);
            for (int i = 0; i < 4; i++)
                Assert.That(lighting.floodlights[i].intensity, Is.EqualTo((i + 2) * (step % 2 == 0 ? .2f : 1)).Within(.001f));
        }
        lighting.AdvanceGoalEffects(.151f);
        Assert.That(lighting.floodlights[0].intensity, Is.EqualTo(2));
        lighting.EndGoalEffects();
        Assert.That(TileColor(0), Is.EqualTo(Color.green));
        Assert.That(lighting.GoalEffectsActive, Is.False);
    }

    [Test]
    public void DisableOrLightingSwitchDuringEffectRestoresAndCancels()
    {
        lighting.BeginGoalEffects(true, "Club", palette);
        lighting.enabled = false;
        Assert.That(lighting.GoalEffectsActive, Is.False);
        Assert.That(lighting.floodlights[0].intensity, Is.EqualTo(3));
        Assert.That(TileColor(0), Is.EqualTo(Color.green));
        lighting.enabled = true;
        lighting.Apply(FixtureLighting.LateNight);
        lighting.BeginGoalEffects(true, "Club", palette);
        lighting.midAfternoon = preset;
        lighting.Apply(FixtureLighting.MidAfternoon);
        Assert.That(lighting.GoalEffectsActive, Is.False);
        Assert.That(TileColor(0), Is.EqualTo(Color.green));
    }
}
