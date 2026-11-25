using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Local audio policy. Its random generator never consumes gameplay RNG.</summary>
public sealed class GameplayAudioPolicy
{
    private readonly System.Random random;
    private readonly HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
    private readonly Queue<string> order = new Queue<string>();
    private readonly Dictionary<GameplayAudioPool, AudioClip> lastClips = new Dictionary<GameplayAudioPool, AudioClip>();
    private readonly Dictionary<GameplayAudioPool, double> lastTimes = new Dictionary<GameplayAudioPool, double>();
    public bool Paused { get; private set; }

    public GameplayAudioPolicy(int seed) => random = new System.Random(seed);
    public void SetPaused(bool paused) => Paused = paused;

    // IDs are scoped to a match run, not just an action name. Old IDs are evicted at a fixed bound.
    // Suppressed events are consumed too: they must not reappear after pause/priority rejection.
    public bool AcceptEvent(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !seen.Add(id)) return false;
        order.Enqueue(id);
        if (order.Count > GameplayAudioConfig.RememberedEventCount) seen.Remove(order.Dequeue());
        return !Paused;
    }

    public bool AcceptCue(GameplayAudioPool pool, double now)
    {
        if (double.IsNaN(now) || double.IsInfinity(now)) return false;
        if (lastTimes.TryGetValue(pool, out double previous) && now - previous < GameplayAudioConfig.Cooldown(pool)) return false;
        lastTimes[pool] = now;
        return true;
    }

    public AudioClip Pick(GameplayAudioPool pool, AudioClip[] clips, AudioClip exclude = null,
        params AudioClip[] avoidWhenPossible)
    {
        if (clips == null || clips.Length == 0) return null;
        var candidates = new List<AudioClip>();
        foreach (AudioClip clip in clips)
            if (clip != null && !candidates.Contains(clip)) candidates.Add(clip);
        if (candidates.Count == 0) return null;
        // Never repeat this stand's outgoing clip when another recording exists.
        if (exclude != null && candidates.Count > 1) candidates.Remove(exclude);
        var preferred = candidates.FindAll(c => Array.IndexOf(avoidWhenPossible, c) < 0);
        if (preferred.Count > 0) candidates = preferred;
        lastClips.TryGetValue(pool, out AudioClip previous);
        if (candidates.Count > 1) candidates.Remove(previous);
        AudioClip selected = candidates[random.Next(candidates.Count)];
        lastClips[pool] = selected;
        return selected;
    }

    public float NextWallaIntensity() => Mathf.Lerp(GameplayAudioConfig.ChatterMinIntensity,
        GameplayAudioConfig.ChatterMaxIntensity, (float)random.NextDouble());

    public float NextChantDelay() => Mathf.Lerp(GameplayAudioConfig.ChantMinIntervalSeconds,
        GameplayAudioConfig.ChantMaxIntervalSeconds, (float)random.NextDouble());

    public static bool CanReplace(int incomingPriority, bool active, int activePriority) =>
        !active || incomingPriority > activePriority;
}
