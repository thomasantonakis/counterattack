using UnityEngine;

/// <summary>One scene-authored UI voice survives scene transitions. Never creates objects or drives navigation.</summary>
public sealed class MenuAudioPlayer : MonoBehaviour
{
    public static MenuAudioPlayer Instance { get; private set; }
    public GameplayAudioLibrary library;
    public AudioSource voice;
    public AudioSource wiggleVoice;
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
    public static void StopDiceWiggle()
    {
        if (Instance == null || Instance.wiggleVoice == null || !Instance.wiggleVoice.isPlaying) return;
        Instance.wiggleVoice.Stop();
        Instance.wiggleVoice.clip = null;
    }

    public bool TryPlay(GameplayAudioPool cue)
    {
        if (cue == GameplayAudioPool.DiceWiggle) return TryPlayDiceWiggle();
        if ((!MenuAudioPolicy.IsMenuCue(cue) && cue != GameplayAudioPool.Dice) || voice == null || library == null) return false;
        float gain = GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Game);
        if (gain <= 0f) return false;
        var clips = library.Get(cue);
        var validClips = System.Array.FindAll(clips, c => c != null);
        AudioClip clip = null;
        if (validClips.Length > 0)
            clip = cue == GameplayAudioPool.Dice
                ? validClips[policy.PickDiceIndex(validClips.Length)]
                : validClips[0];
        if (clip == null) return false;
        bool accepted = cue == GameplayAudioPool.Dice
            ? policy.AcceptDice(Time.unscaledTimeAsDouble, voice.isPlaying, current)
            : policy.Accept(cue, Time.unscaledTimeAsDouble, voice.isPlaying, current);
        if (!accepted) return false;
        current = cue;
        voice.Stop();
        voice.clip = clip;
        voice.loop = false;
        RefreshVolume();
        voice.Play();
        LastPlayedFrame = Time.frameCount;
        return true;
    }

    private bool TryPlayDiceWiggle()
    {
        if (wiggleVoice == null || library == null || GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Game) <= 0f) return false;
        var clips = library.Get(GameplayAudioPool.DiceWiggle);
        AudioClip clip = System.Array.Find(clips, c => c != null);
        if (clip == null || !policy.AcceptDiceWiggle(Time.unscaledTimeAsDouble)) return false;
        wiggleVoice.Stop();
        wiggleVoice.clip = clip;
        wiggleVoice.loop = false;
        wiggleVoice.ignoreListenerPause = true;
        RefreshWiggleVolume();
        wiggleVoice.Play();
        return true;
    }

    private void RefreshVolume()
    {
        if (voice == null) return;
        float gain = GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Game);
        voice.volume = GameplayAudioConfig.PoolGain(current) * gain;
        RefreshWiggleVolume();
        if (gain <= 0f)
        {
            voice.Stop();
            wiggleVoice?.Stop();
        }
    }

    private void RefreshWiggleVolume()
    {
        if (wiggleVoice != null)
            wiggleVoice.volume = GameplayAudioConfig.PoolGain(GameplayAudioPool.DiceWiggle)
                * GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Game);
    }
}
