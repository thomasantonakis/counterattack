using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunGoalDuringCrossfade(bool homeScored)
    {
        Board(1);
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            audio.Director.AdvanceAudio(GameplayAudioConfig.ChatterFadeSeconds);
            audio.Director.homeChatter.time = 6f;
            audio.Director.awayChatter.time = 6f;
            audio.Director.AdvanceAudio(0f);
            Assert.That(audio.Director.homeChatterNext.isPlaying && audio.Director.awayChatterNext.isPlaying, Is.True);
            Assert.That(audio.Director.PlayGoal("Owned crossfade goal", homeScored), Is.True);
            for (int step = 0; step < 4; step++)
            {
                audio.Director.AdvanceAudio(0.5f);
                Assert.That(audio.Director.homeReaction.volume + audio.Director.homeChatter.volume + audio.Director.homeChatterNext.volume,
                    Is.EqualTo(0.7f * GameplayAudioConfig.CrowdOutputGain).Within(0.0001f));
                Assert.That(audio.Director.awayReaction.volume + audio.Director.awayChatter.volume + audio.Director.awayChatterNext.volume,
                    Is.EqualTo(0.3f * GameplayAudioConfig.CrowdOutputGain).Within(0.0001f));
            }
            GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Stadium, false);
            Assert.That(audio.Director.homeReaction.volume + audio.Director.homeChatter.volume + audio.Director.homeChatterNext.volume, Is.Zero);
            GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Stadium, true);
            Assert.That(audio.Director.homeReaction.volume + audio.Director.homeChatter.volume + audio.Director.homeChatterNext.volume,
                Is.EqualTo(0.7f * GameplayAudioConfig.CrowdOutputGain).Within(0.0001f));
        }
    }

    public IEnumerator RunGoalAudioBoundary(string boundary)
    {
        Board(1);
        match.matchStatsUI = Child("Owned score display").AddComponent<MatchStatsUI>();
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            GameplayAudioEvents.GoalCue captured = default;
            int notifications = 0;
            Action<GameplayAudioEvents.GoalCue> observe = cue => { captured = cue; notifications++; };
            GameplayAudioEvents.GoalConfirmed += observe;
            try
            {
                if (boundary == "restore")
                {
                    var snapshot = new RoomRuntimeSnapshot
                    {
                        stats = match.gameData.stats,
                        homeScorers = new System.Collections.Generic.List<MatchManager.GoalEvent>
                        { new MatchManager.GoalEvent { scorer = "Historical scorer", minute = 12 } }
                    };
                    Invoke(match, "RestoreSnapshotStateFields", snapshot);
                    Assert.That(match.homeScorers.Count, Is.EqualTo(1));
                    Assert.That(notifications, Is.Zero);
                    Assert.That(audio.Director.homeReaction.isPlaying, Is.False);
                }
                if (boundary == "paused") Set(match, "isPauseMenuOpen", true);
                if (boundary == "muted") GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Stadium, false);
                if (boundary == "disabled") audio.Director.enabled = false;
                if (boundary == "missing") audio.Director.library.pools = Array.Empty<GameplayAudioLibrary.Pool>();

                match.gameData.gameLog.LogEvent(attacker, MatchManager.ActionType.GoalScored);
                Assert.That(match.gameData.stats.homeTeamStats.totalGoals, Is.EqualTo(1));
                Assert.That(notifications, Is.EqualTo(1));
                Assert.That(captured.HomeScored, Is.True);
                Assert.That(captured.Source, Is.SameAs(match));
                Assert.That(captured.IncidentId, Is.Not.Null.And.Not.Empty);

                if (boundary == "duplicate" || boundary == "restore")
                {
                    Assert.That(audio.Director.homeReaction.isPlaying, Is.True);
                    audio.Director.homeReaction.Stop();
                    audio.Director.awayReaction.Stop();
                }
                else Assert.That(audio.Director.homeReaction.isPlaying || audio.Director.awayReaction.isPlaying, Is.False);

                if (boundary == "disabled")
                {
                    audio.Director.enabled = true;
                    Assert.That(audio.Director.homeReaction.isPlaying, Is.False, "Enabling audio must not replay history.");
                }
                else
                {
                    Set(match, "isPauseMenuOpen", false);
                    audio.Director.SetPaused(false);
                    GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Stadium, true);
                    GameplayAudioEvents.DeliverGoalConfirmed(captured);
                    Assert.That(audio.Director.homeReaction.isPlaying || audio.Director.awayReaction.isPlaying, Is.False,
                        "Consumed notifications must not replay after duplication, pause or mute.");
                }
                Assert.That(match.gameData.stats.homeTeamStats.totalGoals, Is.EqualTo(1));
            }
            finally { GameplayAudioEvents.GoalConfirmed -= observe; }
        }
    }

    public IEnumerator RunConfirmedGoalAudio(bool homeScored, int direction, bool ownGoal)
    {
        Board(direction);
        match.matchStatsUI = Child("Owned inactive goal-score display").AddComponent<MatchStatsUI>();
        match.matchStatsUI.enabled = false;
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            audio.Director.AdvanceAudio(GameplayAudioConfig.ChatterFadeSeconds);
            float homeBed = audio.Director.homeChatter.volume + audio.Director.homeChatterNext.volume;
            float awayBed = audio.Director.awayChatter.volume + audio.Director.awayChatterNext.volume;
            Assert.That(audio.Director.TryPlayChant(true), Is.True);
            Assert.That(audio.Director.TryPlayChant(false), Is.True);
            PlayerToken credited = homeScored ? attacker : defender;
            PlayerToken actor = ownGoal ? (homeScored ? defender : attacker) : credited;
            match.ClearLastTokenChain();
            match.SetLastToken(actor);
            match.teamInAttack = actor.isHomeTeam ? MatchManager.TeamInAttack.Home : MatchManager.TeamInAttack.Away;
            int oldHome = match.gameData.stats.homeTeamStats.totalGoals;
            int oldAway = match.gameData.stats.awayTeamStats.totalGoals;
            int playerGoals = match.gameData.stats.GetPlayerStats(actor).goals;
            var state = match.currentState;
            var possession = match.teamInAttack;
            int rolls = match.TimingRollSequence;
            var rng = UnityEngine.Random.state;

            if (ownGoal) match.gameData.gameLog.LogOwnGoal(actor, homeScored);
            else match.gameData.gameLog.LogEvent(actor, MatchManager.ActionType.GoalScored);

            Assert.That(match.gameData.stats.homeTeamStats.totalGoals, Is.EqualTo(oldHome + (homeScored ? 1 : 0)));
            Assert.That(match.gameData.stats.awayTeamStats.totalGoals, Is.EqualTo(oldAway + (homeScored ? 0 : 1)));
            Assert.That(match.gameData.stats.GetPlayerStats(actor).goals, Is.EqualTo(playerGoals + (ownGoal ? 0 : 1)));
            Assert.That((homeScored ? match.homeScorers : match.awayScorers).Count, Is.EqualTo(1));
            Assert.That(audio.Director.homeReaction.clip, Is.SameAs(homeScored ? audio.Cheer : audio.Groan));
            Assert.That(audio.Director.awayReaction.clip, Is.SameAs(homeScored ? audio.Groan : audio.Cheer));
            Assert.That(audio.Director.homeReaction.isPlaying && audio.Director.awayReaction.isPlaying, Is.True);
            Assert.That(audio.Director.homeReaction.volume, Is.EqualTo(0.7f * GameplayAudioConfig.CrowdOutputGain - homeBed - audio.Director.homeChant.volume).Within(0.0001f));
            Assert.That(audio.Director.awayReaction.volume, Is.EqualTo(0.3f * GameplayAudioConfig.CrowdOutputGain - awayBed - audio.Director.awayChant.volume).Within(0.0001f));
            audio.Director.AdvanceAudio(GameplayAudioConfig.ChantFadeSeconds);
            Assert.That(audio.Director.homeChant.isPlaying || audio.Director.awayChant.isPlaying, Is.False);
            Assert.That(audio.Director.homeReaction.volume + homeBed, Is.EqualTo(0.7f * GameplayAudioConfig.CrowdOutputGain).Within(0.0001f));
            Assert.That(audio.Director.awayReaction.volume + awayBed, Is.EqualTo(0.3f * GameplayAudioConfig.CrowdOutputGain).Within(0.0001f));
            Assert.That(audio.Director.homeChatter.volume + audio.Director.homeChatterNext.volume, Is.EqualTo(homeBed).Within(0.0001f));
            Assert.That(audio.Director.awayChatter.volume + audio.Director.awayChatterNext.volume, Is.EqualTo(awayBed).Within(0.0001f));
            Assert.That(match.currentState, Is.EqualTo(state), "Audio cannot advance the goal flow.");
            Assert.That(match.teamInAttack, Is.EqualTo(possession));
            Assert.That(match.TimingRollSequence, Is.EqualTo(rolls));
            Assert.That(UnityEngine.Random.state, Is.EqualTo(rng));
        }
    }

    private sealed class GoalAudioRig : IDisposable
    {
        public readonly GameplayAudioDirector Director;
        public readonly AudioClip Cheer;
        public readonly AudioClip Groan;
        private readonly AudioClip bed;
        private readonly GameplayAudioLibrary library;
        private readonly GameObject audioRoot;
        private readonly System.Collections.Generic.List<AudioClip> incidentClips = new System.Collections.Generic.List<AudioClip>();
        private readonly bool hadEnabled;
        private readonly bool originalEnabled;
        private const string Key = "CounterAttack.Audio.Stadium.Enabled";

        public GoalAudioRig(Transform parent)
        {
            hadEnabled = PlayerPrefs.HasKey(Key);
            originalEnabled = GameplayAudioPreferences.Local.Enabled(GameplayAudioCategory.Stadium);
            GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Stadium, true);
            audioRoot = new GameObject("Owned silent goal audio");
            audioRoot.SetActive(false);
            audioRoot.transform.SetParent(parent, false);
            library = ScriptableObject.CreateInstance<GameplayAudioLibrary>();
            bed = AudioClip.Create("Owned silent bed", 48000 * 8, 1, 48000, false);
            Cheer = AudioClip.Create("Owned silent goal cheer", 48000 * 8, 1, 48000, false);
            Groan = AudioClip.Create("Owned silent missed-chance groan", 48000 * 8, 1, 48000, false);
            library.pools = new[]
            {
                new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.Chatter, clips = new[] { bed } },
                new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.Chant, clips = new[] { bed } },
                new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.Cheer, clips = new[] { Cheer } },
                new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.Groan, clips = new[] { Groan } }
            };
            var pools = new System.Collections.Generic.List<GameplayAudioLibrary.Pool>(library.pools);
            foreach (GameplayAudioPool pool in Enum.GetValues(typeof(GameplayAudioPool)))
            {
                if ((int)pool < (int)GameplayAudioPool.DuelWon) continue;
                var clip = AudioClip.Create("Owned silent " + pool, 48000 * 8, 1, 48000, false);
                incidentClips.Add(clip);
                pools.Add(new GameplayAudioLibrary.Pool { kind = pool, clips = new[] { clip } });
            }
            library.pools = pools.ToArray();
            Director = audioRoot.AddComponent<GameplayAudioDirector>();
            Director.library = library;
            Director.homeChatter = Source("Home Walla A");
            Director.homeChatterNext = Source("Home Walla B");
            Director.awayChatter = Source("Away Walla A");
            Director.awayChatterNext = Source("Away Walla B");
            Director.homeChant = Source("Home chant");
            Director.awayChant = Source("Away chant");
            Director.homeReaction = Source("Home reaction");
            Director.awayReaction = Source("Away reaction");
            Director.whistle = Source("Whistle");
            Director.announcements = Source("Announcements");
            Director.homePreparation = Source("Home preparation A");
            Director.homePreparationNext = Source("Home preparation B");
            Director.awayPreparation = Source("Away preparation A");
            Director.awayPreparationNext = Source("Away preparation B");
            audioRoot.SetActive(true);
        }

        private AudioSource Source(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(audioRoot.transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.mute = true;
            return source;
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(audioRoot);
            UnityEngine.Object.DestroyImmediate(library);
            UnityEngine.Object.DestroyImmediate(bed);
            UnityEngine.Object.DestroyImmediate(Cheer);
            UnityEngine.Object.DestroyImmediate(Groan);
            foreach (var clip in incidentClips) UnityEngine.Object.DestroyImmediate(clip);
            GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Stadium, originalEnabled);
            if (!hadEnabled) PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
