using System;
using UnityEngine;

/// <summary>Device-local preferences only; never serialized into a match save.</summary>
public sealed class GameplayAudioPreferences
{
    public static GameplayAudioPreferences Local { get; } = new GameplayAudioPreferences();
    public event Action Changed;
    private readonly string prefix;

    public GameplayAudioPreferences(string keyPrefix = "CounterAttack.Audio.") => prefix = keyPrefix;
    private string Key(GameplayAudioCategory category, string suffix) => prefix + category + suffix;
    public bool Enabled(GameplayAudioCategory category) => PlayerPrefs.GetInt(Key(category, ".Enabled"), 1) != 0;
    public float Gain(GameplayAudioCategory category) => Enabled(category) ? GameplayAudioConfig.CategoryVolume(category) : 0f;

    public void SetEnabled(GameplayAudioCategory category, bool enabled)
    {
        PlayerPrefs.SetInt(Key(category, ".Enabled"), enabled ? 1 : 0);
        Save();
    }

    private void Save()
    {
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
