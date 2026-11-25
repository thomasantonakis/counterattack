using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Single persistent music service; match scenes without an assignment are silent.</summary>
public sealed class BackgroundMusicPlayer : MonoBehaviour
{
    private const string ResourcePath = "CounterAttackMusicLibrary";
    private static BackgroundMusicPlayer instance;
    private MusicLibrary library;
    private AudioSource[] voices;
    private int current;
    private int trackIndex;
    private MusicLibrary.Playlist playlist;
    private Coroutine transition;
    private float[] levels = new float[2];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var config = Resources.Load<MusicLibrary>(ResourcePath);
        if (config == null) return;
        var root = new GameObject("BackgroundMusicPlayer");
        instance = root.AddComponent<BackgroundMusicPlayer>();
        instance.Initialize(config);
    }

    private void Initialize(MusicLibrary config)
    {
        library = config;
        DontDestroyOnLoad(gameObject);
        voices = new[] { gameObject.AddComponent<AudioSource>(), gameObject.AddComponent<AudioSource>() };
        foreach (var voice in voices) { voice.playOnAwake = false; voice.spatialBlend = 0f; voice.ignoreListenerPause = true; }
        SceneManager.activeSceneChanged += SceneChanged;
        GameplayAudioPreferences.Local.Changed += ApplyVolume;
        SelectScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.activeSceneChanged -= SceneChanged;
        GameplayAudioPreferences.Local.Changed -= ApplyVolume;
        instance = null;
    }

    private void SceneChanged(Scene previous, Scene next) => SelectScene(next.name);
    private void SelectScene(string scene)
    {
        MusicLibrary.Playlist next = null;
        foreach (var candidate in library.playlists)
            if (candidate != null && candidate.scenes != null && ArrayContains(candidate.scenes, scene)) { next = candidate; break; }
        if (next == playlist) return;
        playlist = next;
        trackIndex = 0;
        // The match director can start its own pause music as soon as Room loads.
        if (scene == "Room" && next == null)
        {
            if (transition != null) StopCoroutine(transition);
            transition = null;
            foreach (var voice in voices) { voice.Stop(); voice.clip = null; }
            levels[0] = levels[1] = 0f;
            return;
        }
        StartTrack(FindTrack(0));
    }

    private static bool ArrayContains(string[] scenes, string scene)
    {
        foreach (var item in scenes) if (item == scene) return true;
        return false;
    }

    private AudioClip FindTrack(int start)
    {
        if (playlist?.tracks == null) return null;
        for (int i = start; i < playlist.tracks.Length; i++)
            if (playlist.tracks[i] != null) { trackIndex = i; return playlist.tracks[i]; }
        return null;
    }

    private void StartTrack(AudioClip clip)
    {
        if (transition != null) StopCoroutine(transition);
        int outgoing = current;
        int incoming = 1 - current;
        voices[incoming].Stop();
        voices[incoming].clip = clip;
        levels[incoming] = 0f;
        current = incoming;
        if (clip != null)
        {
            voices[incoming].loop = playlist.repeat == MusicLibrary.RepeatMode.RepeatTrack;
            voices[incoming].Play();
        }
        if (library.fadeSeconds <= 0f)
        {
            levels[outgoing] = 0f; levels[incoming] = clip != null ? 1f : 0f;
            voices[outgoing].Stop(); voices[outgoing].clip = null;
            ApplyVolume(); transition = null;
        }
        else transition = StartCoroutine(Fade(outgoing, incoming, clip != null));
    }

    private IEnumerator Fade(int outgoing, int incoming, bool hasClip)
    {
        float start = levels[outgoing], elapsed = 0f, duration = Mathf.Max(0f, library.fadeSeconds);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            levels[outgoing] = Mathf.Lerp(start, 0f, t);
            levels[incoming] = hasClip ? t : 0f;
            ApplyVolume();
            yield return null;
        }
        levels[outgoing] = 0f; levels[incoming] = hasClip ? 1f : 0f;
        voices[outgoing].Stop(); voices[outgoing].clip = null;
        ApplyVolume(); transition = null;
    }

    private void ApplyVolume()
    {
        if (voices == null) return;
        float gain = GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Music)
            * Mathf.Clamp01(library.musicVolume) * Mathf.Clamp01(library.masterVolume);
        for (int i = 0; i < voices.Length; i++) voices[i].volume = levels[i] * gain;
    }

    private void Update()
    {
        if (transition != null || playlist == null || voices[current].clip == null || voices[current].isPlaying) return;
        var next = FindTrack(trackIndex + 1);
        if (next == null && playlist.repeat == MusicLibrary.RepeatMode.RepeatPlaylist) next = FindTrack(0);
        if (next != null) StartTrack(next);
        else { voices[current].clip = null; levels[current] = 0f; }
    }
}
