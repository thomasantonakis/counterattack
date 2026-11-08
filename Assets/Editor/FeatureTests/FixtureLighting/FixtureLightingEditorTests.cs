using System.Linq;
using CounterAttack.Editor;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[Category("FixtureLighting")]
public sealed class FixtureLightingEditorTests
{
    private Scene scene;
    private GameObject root;
    private RoomLightingController lighting;
    private RoomLightingPreset day, sunset, night;

    [SetUp]
    public void SetUp()
    {
        scene = EditorSceneManager.NewPreviewScene();
        root = new GameObject("Isolated fixture lighting"); root.SetActive(false);
        SceneManager.MoveGameObjectToScene(root,scene);
        lighting = root.AddComponent<RoomLightingController>();
        lighting.sun = Child("Sun").AddComponent<Light>();
        lighting.sun.type = LightType.Directional;
        lighting.sun.color = Color.cyan; lighting.sun.intensity = .7f;
        lighting.sun.transform.localEulerAngles = new Vector3(50,-30,0);
        lighting.floodlights = Enumerable.Range(0,4).Select(i => Child("Flood " + i).AddComponent<Light>()).ToArray();
        foreach (var light in lighting.floodlights) light.type = LightType.Spot;
        var renderer = Child("Tile").AddComponent<MeshRenderer>();
        lighting.ribbon = Child("Ribbon").AddComponent<StadiumRibbonController>();
        lighting.ribbon.Configure(new Renderer[] { renderer });
        day = ScriptableObject.CreateInstance<RoomLightingPreset>();
        sunset = ScriptableObject.CreateInstance<RoomLightingPreset>();
        night = ScriptableObject.CreateInstance<RoomLightingPreset>();
        day.sunEuler = new Vector3(50,-30,0);
        day.sunColor = new Color(1,.96f,.84f);
        sunset.floodlights = true; sunset.sunIntensity = .4f; sunset.sunEuler = new Vector3(12,-30,0);
        sunset.ribbonColor = new Color(1,.96f,.89f); sunset.floodlightIntensity = 2.9f;
        sunset.sunShadows = LightShadows.None;
        night.sunlight = false; night.floodlights = true; night.floodlightIntensity = 3.4f;
        night.ribbonColor = new Color(.94f,.97f,1);
        lighting.midAfternoon = day; lighting.sunset = sunset; lighting.lateNight = night;
        root.SetActive(true);
    }

    private GameObject Child(string name)
    {
        var go = new GameObject(name); go.transform.SetParent(root.transform,false); return go;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(day); Object.DestroyImmediate(sunset); Object.DestroyImmediate(night);
        EditorSceneManager.ClosePreviewScene(scene);
    }

    [TestCase(null)] [TestCase("")] [TestCase("Unknown")] [TestCase("Late night")] [TestCase("999")]
    public void MissingAndUnknownValuesResolveToDaylight(string value)
    {
        Assert.That(FixtureLightingOptions.Parse(value),Is.EqualTo(FixtureLighting.MidAfternoon));
        Assert.That(FixtureLightingOptions.Normalize(value),Is.EqualTo("MidAfternoon"));
        Assert.That(JsonConvert.DeserializeObject<GameSettings>("{}").fixtureLighting,Is.EqualTo("MidAfternoon"));
        Assert.That(JsonConvert.DeserializeObject<MatchManager.GameSettings>("{}").fixtureLighting,Is.EqualTo("MidAfternoon"));
    }

    [TestCase("MidAfternoon")] [TestCase("Sunset")] [TestCase("LateNight")]
    public void LightingRoundTripsAcrossBothSavedRepresentationsWithoutChangingWeather(string value)
    {
        var setup = new GameSettings { fixtureLighting = value, weatherConditions = "Snow", ballColor = "Orange", squadSize = "16" };
        var room = JsonConvert.DeserializeObject<MatchManager.GameSettings>(JsonConvert.SerializeObject(setup));
        Assert.That(room.fixtureLighting,Is.EqualTo(value));
        var loaded = JsonConvert.DeserializeObject<GameSettings>(JsonConvert.SerializeObject(room));
        Assert.That(loaded.fixtureLighting,Is.EqualTo(value));
        Assert.That(loaded.weatherConditions,Is.EqualTo("Snow"));
        Assert.That(loaded.ballColor,Is.EqualTo("Orange"));
    }

    [TestCase(FixtureLighting.MidAfternoon)] [TestCase(FixtureLighting.Sunset)] [TestCase(FixtureLighting.LateNight)]
    public void PresetSwitchingUpdatesSunFloodlightsRibbonAndEnvironment(FixtureLighting value)
    {
        var preset = value == FixtureLighting.Sunset ? sunset : value == FixtureLighting.LateNight ? night : day;
        for (int repeat = 0; repeat < 3; repeat++)
        {
            lighting.Apply(FixtureLighting.LateNight); lighting.Apply(FixtureLighting.Sunset); lighting.Apply(value);
            Assert.That(lighting.sun.enabled,Is.EqualTo(preset.sunlight));
            Assert.That(lighting.sun.color,Is.EqualTo(preset.sunColor));
            Assert.That(lighting.sun.intensity,Is.EqualTo(preset.sunIntensity));
            Assert.That(lighting.sun.shadows,Is.EqualTo(preset.sunShadows));
            Assert.That(Quaternion.Angle(lighting.sun.transform.localRotation,Quaternion.Euler(preset.sunEuler)),Is.LessThan(.01f));
            Assert.That(RenderSettings.ambientMode,Is.EqualTo(preset.ambientMode));
            Assert.That(RenderSettings.ambientSkyColor,Is.EqualTo(preset.ambientSky));
            Assert.That(RenderSettings.skybox,Is.SameAs(preset.sky));
            Assert.That(RenderSettings.customReflectionTexture,Is.SameAs(preset.reflection));
            foreach (var light in lighting.floodlights)
            {
                Assert.That(light.enabled,Is.EqualTo(preset.floodlights));
                Assert.That(light.intensity,Is.EqualTo(preset.floodlightIntensity));
            }
            var tile = lighting.ribbon.Tiles[0];
            Assert.That(tile.enabled,Is.EqualTo(preset.floodlights));
            var block = new MaterialPropertyBlock(); tile.GetPropertyBlock(block);
            Assert.That(Vector4.Distance(block.GetColor("_Color"),preset.ribbonColor * preset.ribbonBrightness),Is.LessThan(.00001f));
        }
    }

    [Test]
    public void DisablingRoomRestoresTheEnvironmentCapturedBeforeActivation()
    {
        root.SetActive(false);
        var originalSky = RenderSettings.skybox;
        var ambientMode = RenderSettings.ambientMode;
        var skyColor = RenderSettings.ambientSkyColor;
        var equator = RenderSettings.ambientEquatorColor;
        var ground = RenderSettings.ambientGroundColor;
        float ambient = RenderSettings.ambientIntensity, reflectionIntensity = RenderSettings.reflectionIntensity;
        var reflection = RenderSettings.customReflectionTexture;
        var reflectionMode = RenderSettings.defaultReflectionMode;
        var sun = RenderSettings.sun;
        Color originalSunColor = lighting.sun.color;
        LightShadows originalSunShadows = lighting.sun.shadows;
        root.SetActive(true); lighting.Apply(FixtureLighting.LateNight); root.SetActive(false);
        Assert.That(RenderSettings.skybox,Is.SameAs(originalSky));
        Assert.That(RenderSettings.ambientMode,Is.EqualTo(ambientMode));
        Assert.That(RenderSettings.ambientSkyColor,Is.EqualTo(skyColor));
        Assert.That(RenderSettings.ambientEquatorColor,Is.EqualTo(equator));
        Assert.That(RenderSettings.ambientGroundColor,Is.EqualTo(ground));
        Assert.That(RenderSettings.ambientIntensity,Is.EqualTo(ambient));
        Assert.That(RenderSettings.reflectionIntensity,Is.EqualTo(reflectionIntensity));
        Assert.That(RenderSettings.customReflectionTexture,Is.SameAs(reflection));
        Assert.That(RenderSettings.defaultReflectionMode,Is.EqualTo(reflectionMode));
        Assert.That(RenderSettings.sun,Is.SameAs(sun));
        Assert.That(lighting.sun.color,Is.EqualTo(originalSunColor));
        Assert.That(lighting.sun.shadows,Is.EqualTo(originalSunShadows));
        Assert.That(lighting.sun.enabled,Is.True);
    }

    [Test]
    public void AuthoredRibbonHas256OrderedIndependentInwardTilesWithSharedUnlitAssets()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomFixtureLightingAuthoring.RibbonPath);
        Assert.That(prefab,Is.Not.Null,"Run the dedicated fixture lighting authoring operation first.");
        var ribbon = prefab.GetComponent<StadiumRibbonController>();
        Assert.That(ribbon.Tiles.Count,Is.EqualTo(256));
        Assert.That(ribbon.Tiles.Distinct().Count(),Is.EqualTo(256));
        Assert.That(prefab.GetComponentsInChildren<Light>(true),Is.Empty);
        Assert.That(prefab.GetComponentsInChildren<Collider>(true),Is.Empty);
        var mesh = ribbon.Tiles[0].GetComponent<MeshFilter>().sharedMesh;
        var material = ribbon.Tiles[0].sharedMaterial;
        Assert.That(material.shader.name,Is.EqualTo("Unlit/Color"));
        Assert.That(material.globalIlluminationFlags,Is.EqualTo(MaterialGlobalIlluminationFlags.None));
        for (int i = 0; i < 256; i++)
        {
            var tile = ribbon.Tiles[i];
            Assert.That(tile.name,Is.EqualTo($"Section_{i / 8 + 1:00}_Tile_{i % 8 + 1:00}"));
            Assert.That(tile.sharedMaterial,Is.SameAs(material));
            Assert.That(tile.GetComponent<MeshFilter>().sharedMesh,Is.SameAs(mesh));
            Assert.That(tile.shadowCastingMode,Is.EqualTo(ShadowCastingMode.Off));
            Assert.That(tile.receiveShadows,Is.False);
            Assert.That(tile.lightProbeUsage,Is.EqualTo(LightProbeUsage.Off));
            Assert.That((int)(GameObjectUtility.GetStaticEditorFlags(tile.gameObject) & StaticEditorFlags.ContributeGI),Is.Zero);
            Assert.That(tile.transform.localScale.y,Is.EqualTo(.12f).Within(.0001f));
            Assert.That(tile.transform.localPosition.y + tile.transform.localScale.y / 2,Is.EqualTo(2.07f).Within(.0001f));
            var a = RoomFixtureLightingAuthoring.RibbonPoint(i / 256f);
            var b = RoomFixtureLightingAuthoring.RibbonPoint((i + 1f) / 256f);
            Assert.That(tile.transform.localScale.x,Is.EqualTo(Vector3.Distance(a,b) * .995f).Within(.0001f));
            Vector3 inward = Vector3.Cross(Vector3.up,(b-a).normalized);
            Assert.That(Vector3.Dot(-tile.transform.forward,inward),Is.GreaterThan(.999f));
            Assert.That(Vector3.Distance(tile.transform.localPosition,(a+b)*.5f + inward*.012f),Is.LessThan(.0001f));
        }
    }

    [Test]
    public void AuthoredDropdownLayoutSupportsDisabledLayoutGroupsAndRepeatedAuthoring()
    {
        var container = new GameObject("Settings rows", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup));
        container.transform.SetParent(root.transform,false);
        container.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().enabled = false;
        TMPro.TMP_Dropdown MakeRow(string name, float y)
        {
            var row = new GameObject(name + "Row",typeof(RectTransform)); row.transform.SetParent(container.transform,false);
            var rect = (RectTransform)row.transform; rect.sizeDelta = new Vector2(936,62); rect.anchoredPosition = new Vector2(0,y);
            var field = new GameObject(name,typeof(RectTransform)); field.transform.SetParent(row.transform,false);
            return field.AddComponent<TMPro.TMP_Dropdown>();
        }
        var weather = MakeRow("Weather",-150);
        var lightingField = MakeRow("FixtureLighting",-150);
        var ball = MakeRow("Ball",-225);
        RoomFixtureLightingAuthoring.LayoutDropdownRow(weather,lightingField);
        var weatherRect = (RectTransform)weather.transform.parent;
        var lightingRect = (RectTransform)lightingField.transform.parent;
        var ballRect = (RectTransform)ball.transform.parent;
        Assert.That(weatherRect.anchoredPosition.y,Is.EqualTo(-150));
        Assert.That(weatherRect.anchoredPosition.y-lightingRect.anchoredPosition.y,Is.GreaterThan(weatherRect.rect.height));
        Assert.That(lightingRect.anchoredPosition.y-ballRect.anchoredPosition.y,Is.GreaterThan(lightingRect.rect.height));
        Vector2 position = ballRect.anchoredPosition, size = ((RectTransform)container.transform).sizeDelta;
        RoomFixtureLightingAuthoring.LayoutDropdownRow(weather,lightingField);
        Assert.That(ballRect.anchoredPosition,Is.EqualTo(position));
        Assert.That(((RectTransform)container.transform).sizeDelta,Is.EqualTo(size));
    }

    [Test]
    public void SetupListKeepsTheAddedRowInsideItsFixedPanel()
    {
        var container = new GameObject("Setup list",typeof(RectTransform),typeof(UnityEngine.UI.VerticalLayoutGroup));
        container.transform.SetParent(root.transform,false);
        var rect = (RectTransform)container.transform; rect.sizeDelta = new Vector2(1704,896);
        var group = container.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        group.childControlHeight = false; group.childForceExpandHeight = false; group.spacing = 32;
        var fields = new TMPro.TMP_Dropdown[9];
        for (int i = 0; i < fields.Length; i++)
        {
            var row = new GameObject("Row " + i,typeof(RectTransform)); row.transform.SetParent(container.transform,false);
            ((RectTransform)row.transform).sizeDelta = new Vector2(1704,84);
            var field = new GameObject("Field",typeof(RectTransform)); field.transform.SetParent(row.transform,false);
            fields[i] = field.AddComponent<TMPro.TMP_Dropdown>();
        }
        RoomFixtureLightingAuthoring.LayoutDropdownRow(fields[6],fields[7]);
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        var corners = new Vector3[4]; fields[8].transform.parent.GetComponent<RectTransform>().GetWorldCorners(corners);
        float lastBottom = rect.InverseTransformPoint(corners[0]).y;
        Assert.That(lastBottom,Is.GreaterThanOrEqualTo(rect.rect.yMin - .01f));
        Assert.That(group.spacing,Is.GreaterThan(8));
    }

    [Test]
    public void IndividualTileOverrideDoesNotChangeItsNeighbour()
    {
        var other = Child("Neighbour").AddComponent<MeshRenderer>();
        var first = lighting.ribbon.Tiles[0];
        lighting.ribbon.Configure(new Renderer[] { first,other });
        lighting.ribbon.Apply(true,Color.white,1);
        lighting.ribbon.SetTile(0,true,Color.red,.5f);
        var block = new MaterialPropertyBlock(); first.GetPropertyBlock(block);
        Assert.That(block.GetColor("_Color"),Is.EqualTo(Color.red * .5f));
        other.GetPropertyBlock(block); Assert.That(block.GetColor("_Color"),Is.EqualTo(Color.white));
    }
}
