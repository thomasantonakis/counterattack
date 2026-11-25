using System;
using UnityEngine;

/// <summary>Crossfades two pre-authored sources. Owns no gameplay state or objects.</summary>
public sealed class GameplayAudioChatterLoop
{
    private AudioSource active;
    private AudioSource incoming;
    private readonly Func<AudioClip, AudioClip> select;
    private readonly Func<float> intensity;
    private readonly bool home;
    private readonly Func<string> matchType;
    private bool running;
    private float elapsed;
    private float duration;
    private float envelope;
    private float currentIntensity;
    private float nextIntensity;

    public AudioSource ActiveSource => active;
    public AudioSource IncomingSource => incoming;
    public bool IsCrossfading { get; private set; }
    public float Blend => IsCrossfading ? Mathf.Clamp01(elapsed / duration) : 0f;

    public GameplayAudioChatterLoop(AudioSource first, AudioSource second, bool home,
        Func<AudioClip, AudioClip> select, Func<float> intensity, Func<string> matchType = null)
    {
        active = first;
        incoming = second;
        this.home = home;
        this.select = select;
        this.intensity = intensity;
        this.matchType = matchType;
    }

    public bool Start()
    {
        Stop();
        if (active == null || incoming == null || active == incoming) return false;
        AudioClip clip = select(null);
        if (clip == null || clip.length <= 0f) return false;
        currentIntensity = intensity();
        StartClip(active, clip);
        running = true;
        return true;
    }

    public void Tick(float deltaTime, float categoryGain)
    {
        if (!running) return;
        deltaTime = Mathf.Max(0f, deltaTime);
        envelope = Mathf.MoveTowards(envelope, 1f, deltaTime / GameplayAudioConfig.ChatterFadeSeconds);
        if (!IsCrossfading)
        {
            float lead = Mathf.Min(GameplayAudioConfig.ChatterCrossfadeSeconds, active.clip.length * 0.5f);
            if (!active.isPlaying || active.time >= active.clip.length - lead)
            {
                AudioClip next = select(active.clip);
                if (next == null || next.length <= 0f) { Stop(); return; }
                nextIntensity = intensity();
                duration = Mathf.Min(lead, next.length * 0.5f);
                if (active.isPlaying) duration = Mathf.Min(duration, Mathf.Max(0.001f, active.clip.length - active.time));
                elapsed = active.isPlaying ? 0f : duration;
                StartClip(incoming, next);
                IsCrossfading = true;
            }
        }
        if (IsCrossfading)
        {
            elapsed += deltaTime;
            if (elapsed >= duration)
            {
                active.Stop();
                AudioSource previous = active;
                active = incoming;
                incoming = previous;
                currentIntensity = nextIntensity;
                IsCrossfading = false;
            }
        }
        ApplyGain(categoryGain);
    }

    public void ApplyGain(float categoryGain)
    {
        float output = envelope * GameplayAudioConfig.CrowdOutputGain;
        float blend = Blend;
        if (active != null) active.volume = running
            ? GameplayAudioConfig.StandGain(home, currentIntensity, categoryGain, matchType?.Invoke()) * output * (1f - blend) : 0f;
        if (incoming != null) incoming.volume = running && IsCrossfading
            ? GameplayAudioConfig.StandGain(home, nextIntensity, categoryGain, matchType?.Invoke()) * output * blend : 0f;
    }

    public void Stop()
    {
        if (active != null) { active.Stop(); active.volume = 0f; }
        if (incoming != null) { incoming.Stop(); incoming.volume = 0f; }
        running = false;
        IsCrossfading = false;
        envelope = 0f;
        elapsed = 0f;
    }

    private static void StartClip(AudioSource source, AudioClip clip)
    {
        source.Stop();
        source.clip = clip;
        source.loop = false;
        source.volume = 0f;
        source.Play();
    }
}
