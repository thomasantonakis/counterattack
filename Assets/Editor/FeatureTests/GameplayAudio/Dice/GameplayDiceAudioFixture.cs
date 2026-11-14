using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunDiceWiggleWait()
    {
        Board(1);
        var library = ScriptableObject.CreateInstance<GameplayAudioLibrary>();
        try
        {
            using (var audio = new GoalAudioRig(root.transform))
            {
                var source = new GameObject("Owned muted dice wait voice").AddComponent<AudioSource>();
                source.transform.SetParent(root.transform); source.playOnAwake = false; source.mute = true;
                audio.Director.gameEffects = source;
                var wiggleSource = new GameObject("Owned muted independent wiggle voice").AddComponent<AudioSource>();
                wiggleSource.transform.SetParent(root.transform); wiggleSource.playOnAwake = false; wiggleSource.mute = true;
                audio.Director.diceWiggleEffects = wiggleSource;
                var wiggle = AssetDatabase.LoadAssetAtPath<AudioClip>(DiceAudioAuthoring.WiggleClipPath);
                Assert.That(wiggle, Is.Not.Null);
                var roll = AssetDatabase.LoadAssetAtPath<AudioClip>(DiceAudioAuthoring.ClipPaths[0]);
                library.pools = new[] {
                    new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.DiceWiggle, clips = new[] { wiggle } },
                    new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.Dice, clips = new[] { roll } } };
                audio.Director.library = library;
                yield return null;

                Assert.That(audio.Director.PlayGameCue(GameplayAudioPool.Dice), Is.True);
                AudioClip activeRoll = source.clip;
                match.DecisionTiming.BeginRoll("dice_wiggle_wait", "Both teams", () => true, () => { });
                audio.Director.SyncDiceWiggleWaitAudio();
                Assert.That(source.isPlaying, Is.True, "The existing dice roll must keep playing under the next wiggle.");
                Assert.That(source.clip, Is.SameAs(activeRoll));
                Assert.That(wiggleSource.isPlaying, Is.True);
                Assert.That(wiggleSource.clip, Is.SameAs(wiggle));
                Assert.That(wiggleSource.volume, Is.EqualTo(GameplayAudioConfig.PoolGain(GameplayAudioPool.DiceWiggle)));
                int sampleAtStart = wiggleSource.timeSamples;
                audio.Director.SyncDiceWiggleWaitAudio();
                Assert.That(wiggleSource.timeSamples, Is.EqualTo(sampleAtStart), "The same active wait must not restart the wiggle.");

                match.DecisionTiming.EndRoll();
                audio.Director.SyncDiceWiggleWaitAudio();
                Assert.That(wiggleSource.isPlaying, Is.False, "The wiggle must stop as soon as its roll resolves.");
                Assert.That(source.isPlaying, Is.True, "Stopping the wiggle must leave the independent roll voice alone.");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(library); }
    }

    public IEnumerator RunDiceAudio(bool rigged, string boundary)
    {
        Board(1);
        bool had = PlayerPrefs.HasKey("CounterAttack.Audio.Game.Enabled");
        bool previous = GameplayAudioPreferences.Local.Enabled(GameplayAudioCategory.Game);
        var rng = UnityEngine.Random.state;
        var library = ScriptableObject.CreateInstance<GameplayAudioLibrary>();
        try
        {
            GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Game, boundary != "muted");
            using (var audio = new GoalAudioRig(root.transform))
            {
                var source = new GameObject("Owned muted dice voice").AddComponent<AudioSource>();
                source.transform.SetParent(root.transform); source.playOnAwake = false; source.mute = true;
                audio.Director.gameEffects = source;
                var clips = DiceAudioAuthoring.ClipPaths;
                foreach (string clipPath in clips) Assert.That(AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath), Is.Not.Null);
                library.pools = new[] { new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.Dice,
                    clips = boundary == "missing" ? Array.Empty<AudioClip>() :
                        clips.Select(path => AssetDatabase.LoadAssetAtPath<AudioClip>(path)).ToArray() } };
                audio.Director.library = library;
                yield return null;
                RollInputOverride? input = rigged ? new RollInputOverride { hasOverride = true, roll = 4 } : (RollInputOverride?)null;
                audio.Director.enabled = false;
                var before = UnityEngine.Random.state;
                var expected = match.ResolveGameplayDiceRoll("audio_test", input, jackpotEnabled: false);
                var after = UnityEngine.Random.state;
                UnityEngine.Random.state = before;
                audio.Director.enabled = true;
                if (boundary == "paused") match.SetPauseMenuOpen(true);
                int sequence = match.TimingRollSequence;
                var state = match.currentState; var possession = match.teamInAttack;
                int notifications = 0;
                GameplayAudioEvents.IncidentCue captured = default;
                Action<GameplayAudioEvents.IncidentCue> handler = cue =>
                { if(cue.Kind == GameplayAudioEvents.IncidentKind.DiceRequested) { notifications++; captured = cue; } };
                GameplayAudioEvents.IncidentConfirmed += handler;
                GameplayDiceRollResult actual;
                try { actual = match.ResolveGameplayDiceRoll("audio_test", input, jackpotEnabled: false); }
                finally { GameplayAudioEvents.IncidentConfirmed -= handler; }
                Assert.That(actual.roll, Is.EqualTo(expected.roll));
                Assert.That(actual.overrideUsed, Is.EqualTo(rigged));
                Assert.That(UnityEngine.Random.state, Is.EqualTo(after), "Audio must not consume gameplay RNG.");
                Assert.That(match.TimingRollSequence, Is.EqualTo(sequence + 1));
                Assert.That(match.currentState, Is.EqualTo(state)); Assert.That(match.teamInAttack, Is.EqualTo(possession));
                Assert.That(notifications, Is.EqualTo(1));
                Assert.That(source.isPlaying, Is.EqualTo(boundary == "normal"));
                if (boundary == "normal")
                {
                    Assert.That(library.Get(GameplayAudioPool.Dice), Does.Contain(source.clip));
                    Assert.That(source.volume, Is.EqualTo(GameplayAudioConfig.PoolGain(GameplayAudioPool.Dice)));
                    source.Stop(); GameplayAudioEvents.DeliverIncident(captured);
                    Assert.That(source.isPlaying, Is.False, "Redelivery must not replay a roll.");
                    match.ResolveGameplayDiceRoll("audio_test_rapid", input, jackpotEnabled: false);
                    Assert.That(source.isPlaying, Is.False, "Rapid rolls must respect the shared cooldown.");
                }
                match.SetPauseMenuOpen(false);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(library); UnityEngine.Random.state = rng;
            GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Game, previous);
            if (!had) PlayerPrefs.DeleteKey("CounterAttack.Audio.Game.Enabled"); PlayerPrefs.Save();
        }
    }
}
