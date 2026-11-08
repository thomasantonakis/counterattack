using System.Collections;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

/// <summary>Owned native framework rig. Never opens Room or loads/writes a user's save.</summary>
public sealed class FixtureLightingRuntimeFixture
{
    private readonly MatchManager previousMatch = MatchManager.Instance;
    private readonly Scene previousScene = SceneManager.GetActiveScene();
    private readonly Scene scene;
    private readonly GameObject root;
    private readonly MatchManager match;
    private readonly RoomLightingController lighting;
    private Scene incomingScene;

    public FixtureLightingRuntimeFixture()
    {
        scene = SceneManager.CreateScene("Fixture lighting isolated runtime");
        SceneManager.SetActiveScene(scene);
        root = new GameObject("Fixture lighting test rig");
        var matchHost = new GameObject("Dormant match settings"); matchHost.SetActive(false);
        matchHost.transform.SetParent(root.transform,false);
        match = matchHost.AddComponent<MatchManager>();
        match.gameData = new MatchManager.GameData(); MatchManager.Instance = match;
        var host = new GameObject("Authored lighting consumer"); host.SetActive(false); host.transform.SetParent(root.transform,false);
        lighting = host.AddComponent<RoomLightingController>();
        lighting.midAfternoon = Preset("MidAfternoon"); lighting.sunset = Preset("Sunset"); lighting.lateNight = Preset("LateNight");
        var sun = new GameObject("Sun"); sun.transform.SetParent(host.transform,false);
        lighting.sun = sun.AddComponent<Light>(); lighting.sun.type = LightType.Directional;
        for (int i = 0; i < 4; i++)
        {
            var spot = new GameObject("Spot " + i); spot.transform.SetParent(host.transform,false);
            lighting.floodlights[i] = spot.AddComponent<Light>(); lighting.floodlights[i].type = LightType.Spot;
        }
        var ribbon = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CounterAttack.Editor.RoomFixtureLightingAuthoring.RibbonPath),host.transform);
        lighting.ribbon = ribbon.GetComponent<StadiumRibbonController>();
        host.SetActive(true);
    }

    private static RoomLightingPreset Preset(string name) => AssetDatabase.LoadAssetAtPath<RoomLightingPreset>(
        CounterAttack.Editor.RoomFixtureLightingAuthoring.Folder + "/" + name + ".asset");

    public IEnumerator Run(string value)
    {
        // First Update binds the real match-settings event, independently of Start ordering.
        yield return null;
        foreach (string selected in new[] { value, "MidAfternoon", value, "Unknown", value })
        {
            match.gameData.gameSettings.fixtureLighting = selected;
            match.gameData.gameSettings.weatherConditions = "Snow";
            float timeScale = Time.timeScale;
            try { Time.timeScale = 0; match.ApplyLiveGameSettingsChanges(); }
            finally { Time.timeScale = timeScale; }
            var expected = FixtureLightingOptions.Parse(selected);
            Assert.That(lighting.Applied,Is.EqualTo(expected),"Existing settings event applies synchronously, including while paused.");
            Assert.That(match.gameData.gameSettings.fixtureLighting,Is.EqualTo(expected.ToString()));
            Assert.That(match.gameData.gameSettings.weatherConditions,Is.EqualTo("Snow"));
            Assert.That(lighting.sun.enabled,Is.EqualTo(expected != FixtureLighting.LateNight));
            foreach (var light in lighting.floodlights) Assert.That(light.enabled,Is.EqualTo(expected != FixtureLighting.MidAfternoon));
            foreach (var tile in lighting.ribbon.Tiles) Assert.That(tile.enabled,Is.EqualTo(expected != FixtureLighting.MidAfternoon));
            var restored = JsonConvert.DeserializeObject<MatchManager.GameData>(JsonConvert.SerializeObject(match.gameData));
            Assert.That(restored.gameSettings.fixtureLighting,Is.EqualTo(expected.ToString()));
        }
        lighting.enabled = false;
        Material restoredSky = RenderSettings.skybox;
        match.gameData.gameSettings.fixtureLighting = "Sunset";
        match.ApplyLiveGameSettingsChanges();
        Assert.That(RenderSettings.skybox,Is.SameAs(restoredSky),"Leaving Room removes the settings listener.");
    }

    public IEnumerator RunSceneTransition()
    {
        yield return null;
        lighting.Apply(FixtureLighting.LateNight);
        Scene next = incomingScene = SceneManager.CreateScene("Fixture lighting incoming scene");
        try
        {
            SceneManager.SetActiveScene(next);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.magenta;
            lighting.enabled = false;
            Assert.That(RenderSettings.ambientMode,Is.EqualTo(AmbientMode.Flat));
            Assert.That(RenderSettings.ambientLight,Is.EqualTo(Color.magenta));
        }
        finally { SceneManager.SetActiveScene(scene); }
        yield return SceneManager.UnloadSceneAsync(next);
        incomingScene = default;
    }

    public IEnumerator Cleanup()
    {
        // Disable while the owned scene is active, then restore singleton before OnDestroy.
        lighting.enabled = false;
        MatchManager.Instance = previousMatch;
        Object.DestroyImmediate(root);
        SceneManager.SetActiveScene(previousScene);
        if (incomingScene.IsValid() && incomingScene.isLoaded) yield return SceneManager.UnloadSceneAsync(incomingScene);
        yield return SceneManager.UnloadSceneAsync(scene);
    }
}
