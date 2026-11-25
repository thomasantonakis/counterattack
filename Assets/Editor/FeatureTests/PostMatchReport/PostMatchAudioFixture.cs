using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public static class PostMatchAudioFixture
{
    public static IEnumerator Run(bool musicEnabled)
    {
        var previousMatch = MatchManager.Instance;
        var prefs = GameplayAudioPreferences.Local;
        var categories = new[] { GameplayAudioCategory.Stadium, GameplayAudioCategory.Game, GameplayAudioCategory.Music };
        var enabled = categories.Select(prefs.Enabled).ToArray();
        var keys = new[] { "CounterAttack.Audio.Stadium.Enabled", "CounterAttack.Audio.Game.Enabled", "CounterAttack.Audio.Music.Enabled" };
        var had = keys.Select(PlayerPrefs.HasKey).ToArray();
        var matchRoot = new GameObject("Owned inactive post-match state"); matchRoot.SetActive(false);
        var audioRoot = new GameObject("Owned post-match audio"); audioRoot.SetActive(false);
        var clip = AudioClip.Create("Silent post-match test", 48000 * 5, 1, 48000, false);
        var library = ScriptableObject.CreateInstance<GameplayAudioLibrary>();
        GameObject panel = null;
        try
        {
            var match = matchRoot.AddComponent<MatchManager>();
            match.enabled = false; // Never initialize a board or load/write a live save.
            match.gameData = new MatchManager.GameData();
            MatchManager.Instance = match;
            prefs.SetEnabled(GameplayAudioCategory.Stadium, true);
            prefs.SetEnabled(GameplayAudioCategory.Game, true);
            prefs.SetEnabled(GameplayAudioCategory.Music, musicEnabled);
            library.pools = new[] { GameplayAudioPool.Chatter, GameplayAudioPool.Chant, GameplayAudioPool.Cheer,
                GameplayAudioPool.Groan, GameplayAudioPool.Whistle, GameplayAudioPool.Dice,
                GameplayAudioPool.DiceWiggle, GameplayAudioPool.PauseMusic }
                .Select(p => new GameplayAudioLibrary.Pool { kind = p, clips = new[] { clip } }).ToArray();
            var director = audioRoot.AddComponent<GameplayAudioDirector>(); director.library = library;
            AudioSource Source() { var s = audioRoot.AddComponent<AudioSource>(); s.playOnAwake = false; s.mute = true; return s; }
            director.homeChatter = Source(); director.awayChatter = Source();
            director.homeChatterNext = Source(); director.awayChatterNext = Source();
            director.homeChant = Source(); director.awayChant = Source();
            director.homeReaction = Source(); director.awayReaction = Source();
            director.whistle = Source(); director.gameEffects = Source(); director.diceWiggleEffects = Source(); director.music = Source();
            audioRoot.SetActive(true);
            yield return null;
            Assert.That(director.TryPlayChant(true), Is.True);
            Assert.That(director.PlayWhistle("before-full-time"), Is.True);
            Assert.That(director.PlayGameCue(GameplayAudioPool.Dice), Is.True);
            panel = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PostMatchReportEditorTools.PrefabPath));
            panel.name = "Owned full-time panel";
            var show = typeof(EndGamePanelManager).GetMethod("ShowPanel", BindingFlags.Instance | BindingFlags.NonPublic);
            show.Invoke(panel.GetComponent<EndGamePanelManager>(), null);
            Assert.That(match.IsPauseMenuOpen, Is.True);
            foreach (var source in audioRoot.GetComponents<AudioSource>())
                if (source != director.music) Assert.That(source.isPlaying, Is.False, "Stadium and match sounds must stop immediately.");
            Assert.That(director.music.isPlaying, Is.EqualTo(musicEnabled));
            Assert.That(director.PlayGameCue(GameplayAudioPool.Dice), Is.False);
            Assert.That(director.PlayWhistle("late-whistle"), Is.False);
            Assert.That(director.TryPlayChant(false), Is.False);
            yield return null;
            Assert.That(director.homeChatter.isPlaying, Is.False, "The regular Update must preserve the report pause.");
            Assert.That(director.music.isPlaying, Is.EqualTo(musicEnabled));
            show.Invoke(panel.GetComponent<EndGamePanelManager>(), null);
            Assert.That(director.music.isPlaying, Is.EqualTo(musicEnabled));
        }
        finally
        {
            if (panel != null) Object.DestroyImmediate(panel);
            Object.DestroyImmediate(audioRoot);
            Object.DestroyImmediate(matchRoot);
            Object.DestroyImmediate(clip); Object.DestroyImmediate(library);
            MatchManager.Instance = previousMatch;
            for (int i = 0; i < categories.Length; i++) { prefs.SetEnabled(categories[i], enabled[i]); if (!had[i]) PlayerPrefs.DeleteKey(keys[i]); }
            PlayerPrefs.Save();
        }
    }
}
