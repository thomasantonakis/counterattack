using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class OnlineFullRoomSceneFixture
{
    public IEnumerator RunGuestGoalAudio(bool homeScored)
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        const string matchId = "online-goal-audio";
        var data = Data(16, false, useFrontendSeats: true);
        yield return LoadNativeRoom(data, false, matchId);
        var host = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var compatibility = new OnlineCompatibility("ground-wire-v7", "ground-rules-v1", OnlineFullRoomBootstrap.Catalog);
        var authority = new OnlineGroundPassAuthority(host.match,
            OnlineFrontendSessionPolicy.CreateMatchProtocol(matchId, "home", "away", compatibility));
        var scorer = host.tokens.First(token => token.isHomeTeam == homeScored && !token.IsGoalKeeper && token.currentHex != null);
        void Score() => host.match.gameData.gameLog.LogEvent(scorer, MatchManager.ActionType.GoalScored);
        Score();
        var baseline = authority.Capture();
        Score();
        var goal = authority.Capture();
        Score();
        var mutedGoal = authority.Capture();
        Score();
        var laterGoal = authority.Capture();

        yield return LoadPresentationGuest(data, matchId, nativeRoom: true);
        var guest = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        Assert.That(guest.match.IsGameplayInputBlocked, Is.True);
        var replica = new OnlineGroundPassReplica(matchId, guest.tokens.Select(OnlineRollPublication.ActorId), OnlineFullRoomBootstrap.Catalog);
        var audio = Object.FindAnyObjectByType<GameplayAudioDirector>();
        var cheers = homeScored ? audio.homeReaction : audio.awayReaction;
        var groans = homeScored ? audio.awayReaction : audio.homeReaction;
        const GameplayAudioCategory stadium = GameplayAudioCategory.Stadium;
        const string preferenceKey = "CounterAttack.Audio.Stadium.Enabled";
        var preferences = GameplayAudioPreferences.Local;
        bool hadPreference = PlayerPrefs.HasKey(preferenceKey);
        bool wasEnabled = preferences.Enabled(stadium);
        void Present(OnlineGroundPassState publication)
        {
            Assert.That(replica.Apply(publication), Is.True);
            guest.Display(replica.State, "Guest goal audio fixture");
        }
        void AssertReaction()
        {
            Assert.That(cheers.clip, Is.Not.Null);
            Assert.That(audio.library.Get(GameplayAudioPool.Cheer), Does.Contain(cheers.clip));
            Assert.That(groans.clip, Is.Not.Null);
            Assert.That(audio.library.Get(GameplayAudioPool.Groan), Does.Contain(groans.clip));
        }
        void ClearReaction()
        {
            cheers.Stop();
            groans.Stop();
            cheers.clip = groans.clip = null;
        }
        try
        {
            preferences.SetEnabled(stadium, true);
            ClearReaction();
            Present(baseline);
            Assert.That(cheers.clip, Is.Null, "Existing scores must be silent on the initial snapshot.");
            Present(goal);
            AssertReaction();
            ClearReaction();
            Present(goal);
            Assert.That(cheers.clip, Is.Null, "A duplicate snapshot must be silent.");

            preferences.SetEnabled(stadium, false);
            Present(mutedGoal);
            Assert.That(cheers.clip, Is.Null);
            Assert.That(groans.clip, Is.Null);
            preferences.SetEnabled(stadium, true);
            Present(mutedGoal);
            Assert.That(cheers.clip, Is.Null, "Unmuting must not replay a received goal.");
            Present(laterGoal);
            AssertReaction();
            var stats = homeScored ? guest.match.gameData.stats.homeTeamStats : guest.match.gameData.stats.awayTeamStats;
            Assert.That(stats.totalGoals, Is.EqualTo(4));
        }
        finally
        {
            preferences.SetEnabled(stadium, wasEnabled);
            if (!hadPreference) PlayerPrefs.DeleteKey(preferenceKey);
            PlayerPrefs.Save();
        }
    }
}
