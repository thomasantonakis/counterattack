using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Consumes persistent Room lighting assets and restores globals when Room leaves.</summary>
[ExecuteAlways]
public sealed class RoomLightingController : MonoBehaviour
{
    public RoomLightingPreset midAfternoon;
    public RoomLightingPreset sunset;
    public RoomLightingPreset lateNight;
    public Light sun;
    public Light[] floodlights = new Light[4];
    public StadiumRibbonController ribbon;
    [SerializeField] private FixtureLighting preview;
    private EnvironmentState previous;
    private Scene environmentScene;
    private MatchManager subscribed;
    private string appliedValue;
    public FixtureLighting Applied { get; private set; }

    private void OnEnable()
    {
        previous = new EnvironmentState(sun);
        environmentScene = SceneManager.GetActiveScene();
        appliedValue = null;
        if (!Application.isPlaying) Apply(preview);
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            if (Applied != preview) Apply(preview);
            return;
        }
        var match = MatchManager.Instance;
        if (subscribed != match)
        {
            if (subscribed != null) subscribed.OnGameSettingsLoaded -= ApplyLoadedSettings;
            subscribed = match;
            if (subscribed != null) subscribed.OnGameSettingsLoaded += ApplyLoadedSettings;
        }
        string value = match?.gameData?.gameSettings?.fixtureLighting;
        if (appliedValue == null || appliedValue != FixtureLightingOptions.Normalize(value)) ApplyLoadedSettings();
    }

    public void ApplyLoadedSettings() => Apply(FixtureLightingOptions.Parse(
        MatchManager.Instance?.gameData?.gameSettings?.fixtureLighting));

    public void Apply(FixtureLighting selection)
    {
        RoomLightingPreset preset = selection switch
        {
            FixtureLighting.Sunset => sunset,
            FixtureLighting.LateNight => lateNight,
            _ => midAfternoon
        };
        if (preset == null) return;
        RenderSettings.skybox = preset.sky;
        RenderSettings.ambientMode = preset.ambientMode;
        RenderSettings.ambientSkyColor = preset.ambientSky;
        RenderSettings.ambientEquatorColor = preset.ambientEquator;
        RenderSettings.ambientGroundColor = preset.ambientGround;
        RenderSettings.ambientIntensity = preset.ambientIntensity;
        RenderSettings.defaultReflectionMode = preset.reflectionMode;
        RenderSettings.customReflectionTexture = preset.reflection;
        RenderSettings.reflectionIntensity = preset.reflectionIntensity;
        RenderSettings.sun = sun;
        if (sun != null)
        {
            sun.enabled = preset.sunlight;
            sun.color = preset.sunColor;
            sun.intensity = preset.sunIntensity;
            sun.shadows = preset.sunShadows;
            sun.transform.localEulerAngles = preset.sunEuler;
        }
        foreach (Light floodlight in floodlights)
        {
            if (floodlight == null) continue;
            floodlight.enabled = preset.floodlights;
            floodlight.color = preset.floodlightColor;
            floodlight.intensity = preset.floodlightIntensity;
        }
        ribbon?.Apply(preset.floodlights, preset.ribbonColor, preset.ribbonBrightness);
        Applied = selection;
        if (!Application.isPlaying) preview = selection;
        appliedValue = selection.ToString();
    }

    [ContextMenu("Preview/Mid afternoon")]
    private void PreviewDay() { preview = FixtureLighting.MidAfternoon; Apply(preview); }
    [ContextMenu("Preview/Sunset")]
    private void PreviewSunset() { preview = FixtureLighting.Sunset; Apply(preview); }
    [ContextMenu("Preview/Late night")]
    private void PreviewNight() { preview = FixtureLighting.LateNight; Apply(preview); }

    private void OnDisable()
    {
        if (subscribed != null) subscribed.OnGameSettingsLoaded -= ApplyLoadedSettings;
        subscribed = null;
        RestoreEnvironment();
    }

    private void OnDestroy() => RestoreEnvironment();

    private void RestoreEnvironment()
    {
        // RenderSettings belongs to the active scene. Unloading Room must not
        // overwrite an incoming scene that already installed its own environment.
        if (SceneManager.GetActiveScene() == environmentScene) previous?.Restore(sun);
    }

    private sealed class EnvironmentState
    {
        private readonly Material sky = RenderSettings.skybox;
        private readonly AmbientMode ambientMode = RenderSettings.ambientMode;
        private readonly Color skyColor = RenderSettings.ambientSkyColor;
        private readonly Color equator = RenderSettings.ambientEquatorColor;
        private readonly Color ground = RenderSettings.ambientGroundColor;
        private readonly float ambient = RenderSettings.ambientIntensity;
        private readonly DefaultReflectionMode reflectionMode = RenderSettings.defaultReflectionMode;
        private readonly Texture reflection = RenderSettings.customReflectionTexture;
        private readonly float reflectionIntensity = RenderSettings.reflectionIntensity;
        private readonly Light originalSun = RenderSettings.sun;
        private readonly bool sunEnabled;
        private readonly Color sunColor;
        private readonly float sunIntensity;
        private readonly LightShadows sunShadows;
        private readonly Quaternion sunRotation;
        private readonly Light capturedLight;

        public EnvironmentState(Light light)
        {
            light = light != null ? light : originalSun;
            capturedLight = light;
            if (light == null) return;
            sunEnabled = light.enabled; sunColor = light.color; sunIntensity = light.intensity;
            sunShadows = light.shadows;
            sunRotation = light.transform.localRotation;
        }

        public void Restore(Light light)
        {
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientSkyColor = skyColor;
            RenderSettings.ambientEquatorColor = equator;
            RenderSettings.ambientGroundColor = ground;
            RenderSettings.ambientIntensity = ambient;
            RenderSettings.defaultReflectionMode = reflectionMode;
            RenderSettings.customReflectionTexture = reflection;
            RenderSettings.reflectionIntensity = reflectionIntensity;
            RenderSettings.sun = originalSun;
            light = capturedLight;
            if (light == null) return;
            light.enabled = sunEnabled; light.color = sunColor; light.intensity = sunIntensity;
            light.shadows = sunShadows;
            light.transform.localRotation = sunRotation;
        }
    }
}
