using UnityEngine;

/// <summary>One scene-authored UI voice survives scene transitions. Never creates objects or drives navigation.</summary>
public sealed class MenuAudioPlayer : MonoBehaviour
{
    public static MenuAudioPlayer Instance { get; private set; }
    public GameplayAudioLibrary library;
    public AudioSource voice;
    public int LastPlayedFrame { get; private set; } = -1;
    private readonly MenuAudioPolicy policy = new MenuAudioPolicy();
    private GameplayAudioPool current;
    private bool registered;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Unity does not serialize this AudioSource property into the prefab.
        if (voice != null) voice.ignoreListenerPause = true;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (Instance != this) return;
        GameplayAudioPreferences.Local.Changed += RefreshVolume;
        registered = true;
    }

    private void OnDisable()
    {
        if (registered) GameplayAudioPreferences.Local.Changed -= RefreshVolume;
        registered = false;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public static bool Play(GameplayAudioPool cue) => Instance != null && Instance.TryPlay(cue);

    public bool TryPlay(GameplayAudioPool cue)
    {
        if (!MenuAudioPolicy.IsMenuCue(cue) || voice == null || library == null) return false;
        float gain = GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Game);
        if (gain <= 0f) return false;
        var clips = library.Get(cue);
        AudioClip clip = System.Array.Find(clips, c => c != null);
        if (clip == null || !policy.Accept(cue, Time.unscaledTimeAsDouble, voice.isPlaying, current)) return false;
        current = cue;
        voice.Stop();
        voice.clip = clip;
        voice.loop = false;
        RefreshVolume();
        voice.Play();
        LastPlayedFrame = Time.frameCount;
        return true;
    }

    private void RefreshVolume()
    {
        if (voice == null) return;
        float gain = GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Game);
        voice.volume = GameplayAudioConfig.PoolGain(current) * gain;
        if (gain <= 0f) voice.Stop();
    }
}
