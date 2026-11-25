using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

/// <summary>Silent owned audio fixture: no boards, live saves, or gameplay input.</summary>
public static class GameplayAudioPlaybackFixture
{
    public static IEnumerator Run(bool homeScored)
    {
        var previousMatch = MatchManager.Instance;
        var root = new GameObject("Isolated silent audio playback");
        root.SetActive(false);
        var library = ScriptableObject.CreateInstance<GameplayAudioLibrary>();
        var clip = AudioClip.Create("Silent audio test", 48000 * 5, 1, 48000, false);
        var preferences = GameplayAudioPreferences.Local;
        const string stadiumEnabledKey = "CounterAttack.Audio.Stadium.Enabled";
        bool hadEnabled = PlayerPrefs.HasKey(stadiumEnabledKey);
        bool previousEnabled = preferences.Enabled(GameplayAudioCategory.Stadium);
        try
        {
            MatchManager.Instance = null;
            preferences.SetEnabled(GameplayAudioCategory.Stadium, true);
            library.pools = new[]
            {
                new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.Chatter, clips = new[] { clip } },
                new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.Chant, clips = new[] { clip } },
                new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.Cheer, clips = new[] { clip } },
                new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.Groan, clips = new[] { clip } },
                new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.Whistle, clips = new[] { clip } },
                new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.PauseMusic, clips = new[] { clip } }
            };
            var director = root.AddComponent<GameplayAudioDirector>();
            director.library = library;
            director.homeChatter = Source(root, "Home chatter");
            director.awayChatter = Source(root, "Away chatter");
            director.homeChatterNext = Source(root, "Home chatter incoming");
            director.awayChatterNext = Source(root, "Away chatter incoming");
            director.homeChant = Source(root, "Home chant");
            director.awayChant = Source(root, "Away chant");
            director.homeReaction = Source(root, "Home reaction");
            director.awayReaction = Source(root, "Away reaction");
            director.whistle = Source(root, "Whistle");
            director.gameEffects = Source(root, "Game effects");
            director.music = Source(root, "Music");
            root.SetActive(true);
            yield return null;
            Assert.That(director.homeChatter.isPlaying, Is.True);
            Assert.That(director.awayChatter.isPlaying, Is.True);
            director.AdvanceAudio(GameplayAudioConfig.ChatterFadeSeconds);
            float homeBed = director.homeChatter.volume + director.homeChatterNext.volume;
            float awayBed = director.awayChatter.volume + director.awayChatterNext.volume;
            Assert.That(homeBed, Is.InRange(0.7f * 0.4f * GameplayAudioConfig.CrowdOutputGain, 0.7f * 0.5f * GameplayAudioConfig.CrowdOutputGain));
            Assert.That(awayBed, Is.InRange(0.3f * 0.4f * GameplayAudioConfig.CrowdOutputGain, 0.3f * 0.5f * GameplayAudioConfig.CrowdOutputGain));
            Assert.That(director.TryPlayChant(true), Is.True);
            Assert.That(director.TryPlayChant(false), Is.True);
            Assert.That(director.TryPlayChant(true), Is.False, "No same-stand chant stacking.");
            Assert.That(director.homeChant.loop, Is.False);
            Assert.That(director.homeChant.volume, Is.EqualTo(0.7f * GameplayAudioConfig.ChantIntensity * GameplayAudioConfig.CrowdOutputGain).Within(0.0001f));
            Assert.That(director.awayChant.volume, Is.EqualTo(0.3f * GameplayAudioConfig.ChantIntensity * GameplayAudioConfig.CrowdOutputGain).Within(0.0001f));
            Assert.That(director.PlayGoal("owned-run:1", homeScored), Is.True);
            Assert.That(director.PlayGoal("owned-run:1", homeScored), Is.False);
            Assert.That(director.PlayWhistle("owned-run:1"), Is.True, "Whistles and crowd have independent delivery lanes.");
            Assert.That(director.homeReaction.volume, Is.EqualTo(0.7f * GameplayAudioConfig.CrowdOutputGain - homeBed - director.homeChant.volume).Within(0.0001f));
            Assert.That(director.awayReaction.volume, Is.EqualTo(0.3f * GameplayAudioConfig.CrowdOutputGain - awayBed - director.awayChant.volume).Within(0.0001f));
            Assert.That(director.homeChant.isPlaying, Is.True, "A chant must fade, not stop abruptly.");
            director.AdvanceAudio(GameplayAudioConfig.ChantFadeSeconds * 0.5f);
            Assert.That(director.homeChant.volume, Is.EqualTo(0.7f * GameplayAudioConfig.ChantIntensity * GameplayAudioConfig.CrowdOutputGain * 0.5f).Within(0.0001f));
            Assert.That(director.homeChatter.volume + director.homeChatterNext.volume, Is.EqualTo(homeBed).Within(0.0001f));
            Assert.That(director.awayChatter.volume + director.awayChatterNext.volume, Is.EqualTo(awayBed).Within(0.0001f));
            director.AdvanceAudio(GameplayAudioConfig.ChantFadeSeconds);
            Assert.That(director.homeChant.isPlaying, Is.False);
            Assert.That(director.awayChant.isPlaying, Is.False);
            Assert.That(director.SecondsUntilNextChant(true), Is.InRange(GameplayAudioConfig.ChantMinIntervalSeconds - 0.1f, GameplayAudioConfig.ChantMaxIntervalSeconds));
            Assert.That(director.SecondsUntilNextChant(false), Is.InRange(GameplayAudioConfig.ChantMinIntervalSeconds - 0.1f, GameplayAudioConfig.ChantMaxIntervalSeconds));
            Assert.That(director.TryPlayChant(true), Is.False, "An event reaction owns the foreground until it finishes.");
            director.SetPaused(true);
            foreach (var source in root.GetComponentsInChildren<AudioSource>())
                if (source != director.music) Assert.That(source.isPlaying, Is.False);
            Assert.That(director.music.isPlaying, Is.EqualTo(preferences.Gain(GameplayAudioCategory.Music) > 0f));
            Assert.That(director.PlayGoal("owned-run:2", !homeScored), Is.False);
            director.SetPaused(false);
            yield return null;
            Assert.That(director.homeChatter.isPlaying, Is.True);
            Assert.That(director.awayChatter.isPlaying, Is.True);
            Assert.That(director.homeReaction.isPlaying, Is.False);
            Assert.That(director.awayReaction.isPlaying, Is.False);
            Assert.That(director.homeChant.isPlaying, Is.False, "Interrupted chants must not resume.");
            Assert.That(director.awayChant.isPlaying, Is.False);
            Assert.That(director.whistle.isPlaying, Is.False);
            Assert.That(director.music.isPlaying, Is.False);
            Assert.That(director.PlayGoal("owned-run:2", !homeScored), Is.False);
            Assert.That(director.PlayGoal("owned-run:3", !homeScored), Is.True);
            preferences.SetEnabled(GameplayAudioCategory.Stadium, false);
            Assert.That(director.homeReaction.volume, Is.Zero);
            Assert.That(director.awayReaction.volume, Is.Zero);
            Assert.That(director.homeChatter.volume, Is.Zero);
            Assert.That(director.homeChatterNext.volume, Is.Zero);
            Assert.That(director.awayChatterNext.volume, Is.Zero);
            Assert.That(director.whistle.volume, Is.Zero);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(clip);
            UnityEngine.Object.DestroyImmediate(library);
            MatchManager.Instance = previousMatch;
            preferences.SetEnabled(GameplayAudioCategory.Stadium, previousEnabled);
            if (!hadEnabled) PlayerPrefs.DeleteKey(stadiumEnabledKey);
            PlayerPrefs.Save();
        }
    }

    public static IEnumerator RunChantDeadlineRetry()
    {
        var previousMatch = MatchManager.Instance;
        var preferences = GameplayAudioPreferences.Local;
        const string stadiumEnabledKey = "CounterAttack.Audio.Stadium.Enabled";
        bool hadEnabled = PlayerPrefs.HasKey(stadiumEnabledKey);
        bool previousEnabled = preferences.Enabled(GameplayAudioCategory.Stadium);
        var root = new GameObject("Isolated silent chant retry"); root.SetActive(false);
        var clip = AudioClip.Create("Silent chant retry", 48000 * 8, 1, 48000, false);
        var library = ScriptableObject.CreateInstance<GameplayAudioLibrary>();
        library.pools = new[] {
            new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.Chatter, clips = new[] { clip } },
            new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.Chant, clips = new[] { clip } },
            new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.Cheer, clips = new[] { clip } },
            new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.Groan, clips = new[] { clip } }
        };
        try
        {
            MatchManager.Instance = null;
            preferences.SetEnabled(GameplayAudioCategory.Stadium, true);
            var director = root.AddComponent<GameplayAudioDirector>(); director.library = library;
            director.homeChatter = Source(root, "Home chatter");
            director.awayChatter = Source(root, "Away chatter");
            director.homeChatterNext = Source(root, "Home chatter incoming");
            director.awayChatterNext = Source(root, "Away chatter incoming");
            director.homeChant = Source(root, "Home chant");
            director.awayChant = Source(root, "Away chant");
            director.homeReaction = Source(root, "Home reaction");
            director.awayReaction = Source(root, "Away reaction");
            root.SetActive(true);
            yield return null;

            var stateField = typeof(GameplayAudioDirector).GetField("homeChantState",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            object chantState = stateField.GetValue(director);
            chantState.GetType().GetField("NextAt").SetValue(chantState, Time.unscaledTime - 1f);
            Assert.That(director.PlayGoal("temporary chant blocker", true), Is.True);
            director.AdvanceAudio(0f);
            Assert.That(director.SecondsUntilNextChant(true), Is.Zero,
                "A temporary crowd reaction must not reset a chant that is already due.");

            director.homeReaction.Stop(); director.awayReaction.Stop();
            director.AdvanceAudio(.01f);
            Assert.That(director.homeChant.isPlaying, Is.True,
                "The due home chant should play as soon as the temporary reaction clears.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(clip);
            UnityEngine.Object.DestroyImmediate(library);
            MatchManager.Instance = previousMatch;
            preferences.SetEnabled(GameplayAudioCategory.Stadium, previousEnabled);
            if (!hadEnabled) PlayerPrefs.DeleteKey(stadiumEnabledKey);
            PlayerPrefs.Save();
        }
    }

    public static IEnumerator RunCrossfade(int clipCount)
    {
        var root = new GameObject("Isolated silent Walla crossfade");
        var clips = new AudioClip[clipCount];
        try
        {
            for (int i = 0; i < clipCount; i++) clips[i] = AudioClip.Create("Silent Walla " + i, 48000 * 6, 1, 48000, false);
            var first = Source(root, "Outgoing");
            var second = Source(root, "Incoming");
            var policy = new GameplayAudioPolicy(381);
            var loop = new GameplayAudioChatterLoop(first, second, true,
                previous => policy.Pick(GameplayAudioPool.Chatter, clips, previous), () => 0.45f);
            if (clipCount == 0)
            {
                Assert.That(loop.Start(), Is.False);
                loop.Tick(3f, 1f);
                Assert.That(first.isPlaying || second.isPlaying, Is.False);
                yield break;
            }
            Assert.That(loop.Start(), Is.True);
            yield return null;
            loop.Tick(GameplayAudioConfig.ChatterFadeSeconds, 1f);
            float expected = 0.7f * 0.45f * GameplayAudioConfig.CrowdOutputGain;
            Assert.That(first.volume + second.volume, Is.EqualTo(expected).Within(0.0001f));
            AudioClip previousClip = loop.ActiveSource.clip;
            loop.ActiveSource.time = 4f;
            loop.Tick(0f, 1f);
            Assert.That(loop.IsCrossfading, Is.True);
            Assert.That(first.isPlaying && second.isPlaying, Is.True);
            Assert.That(loop.IncomingSource.clip, clipCount > 1 ? Is.Not.EqualTo(previousClip) : Is.EqualTo(previousClip));
            loop.Tick(1f, 1f);
            Assert.That(loop.Blend, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(first.volume, Is.EqualTo(expected * 0.5f).Within(0.001f));
            Assert.That(second.volume, Is.EqualTo(expected * 0.5f).Within(0.001f));
            Assert.That(first.volume + second.volume, Is.EqualTo(expected).Within(0.001f), "Crossfading must not double the bed gain.");
            loop.ApplyGain(0f);
            Assert.That(first.volume + second.volume, Is.Zero, "Mute applies to both sides of an active crossfade.");
            loop.ApplyGain(1f);
            loop.Tick(1f, 1f);
            Assert.That(loop.IsCrossfading, Is.False);
            Assert.That(loop.ActiveSource, Is.EqualTo(second));
            Assert.That(first.isPlaying, Is.False);
            Assert.That(second.loop, Is.False);
            loop.ActiveSource.time = 4f;
            loop.Tick(0f, 1f);
            Assert.That(loop.IsCrossfading, Is.True);
            loop.Stop();
            loop.Tick(3f, 1f);
            Assert.That(first.isPlaying || second.isPlaying, Is.False, "Stopped/paused crossfades cannot restart themselves.");
            Assert.That(loop.Start(), Is.True);
            Assert.That(loop.IsCrossfading, Is.False);
            Assert.That(root.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(2));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            foreach (AudioClip clip in clips) if (clip != null) UnityEngine.Object.DestroyImmediate(clip);
        }
    }

    private static AudioSource Source(GameObject root, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.mute = true;
        return source;
    }
}
