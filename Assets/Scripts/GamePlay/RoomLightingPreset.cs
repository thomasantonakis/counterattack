using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "CounterAttack/Room lighting preset")]
public sealed class RoomLightingPreset : ScriptableObject
{
    public Material sky;
    public AmbientMode ambientMode = AmbientMode.Trilight;
    public Color ambientSky = Color.gray;
    public Color ambientEquator = Color.gray;
    public Color ambientGround = Color.gray;
    public float ambientIntensity = 1f;
    public DefaultReflectionMode reflectionMode = DefaultReflectionMode.Custom;
    public Cubemap reflection;
    public float reflectionIntensity = 1f;
    public bool sunlight = true;
    public Color sunColor = Color.white;
    public float sunIntensity = 1f;
    public LightShadows sunShadows = LightShadows.Soft;
    public Vector3 sunEuler;
    public bool floodlights;
    public Color floodlightColor = Color.white;
    public float floodlightIntensity = 2.5f;
    public Color ribbonColor = Color.white;
    [Min(0)] public float ribbonBrightness = 1f;
}
