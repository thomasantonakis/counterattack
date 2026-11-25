using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public static class PauseMusicFixture
{
    public static IEnumerator Run()
    {
        var root = new GameObject("Pause music test");
        root.SetActive(false);
        var director = root.AddComponent<GameplayAudioDirector>();
        director.stadiumActive = false;
        director.library = AssetDatabase.LoadAssetAtPath<GameplayAudioLibrary>("Assets/Audio/GameplayAudioLibrary.asset");
        director.music = root.AddComponent<AudioSource>();
        director.music.playOnAwake = false;
        director.music.mute = true;
        const string key = "CounterAttack.Audio.Music.Enabled";
        bool hadKey = PlayerPrefs.HasKey(key);
        int oldValue = PlayerPrefs.GetInt(key, 1);
        try
        {
            GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Music, true);
            root.SetActive(true);
            director.SetPaused(true);
            yield return null;
            Assert.That(director.music.isPlaying, Is.True);
            CollectionAssert.Contains(director.library.Get(GameplayAudioPool.PauseMusic), director.music.clip);
            Assert.That(director.library.Get(GameplayAudioPool.PauseMusic).Length, Is.GreaterThan(1));
            Assert.That(director.music.loop, Is.False);
            Assert.That(director.music.volume, Is.EqualTo(0.25f).Within(0.001f));
            var first = director.music.clip;
            director.music.Stop(); // Simulate the current track ending.
            yield return null;
            Assert.That(director.music.isPlaying, Is.True);
            Assert.That(director.music.clip, Is.Not.EqualTo(first));
            GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Music, false);
            Assert.That(director.music.volume, Is.Zero);
            director.SetPaused(false);
            Assert.That(director.music.isPlaying, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
            if (hadKey) PlayerPrefs.SetInt(key, oldValue); else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Music, oldValue != 0);
            if (!hadKey) { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
        }
    }
}
