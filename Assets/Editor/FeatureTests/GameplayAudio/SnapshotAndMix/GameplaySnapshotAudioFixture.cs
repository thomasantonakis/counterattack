using System.Collections;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunMovementSnapshotAudio(bool home, int direction)
    {
        Board(direction);
        if (!home) match.ChangePossession();
        var carrier = home ? attacker : defender;
        Place(carrier, Hex(13 * (home ? direction : -direction)));
        ball.PlaceAtCell(carrier.currentHex);
        match.SetLastToken(carrier);
        match.currentState = MatchManager.GameState.MovementPhase;
        movement.selectedToken = carrier;
        movement.isActivated = true;
        movement.isMovementPhaseAttack = true;
        movement.isDribblerRunning = true;
        movement.remainingDribblerPace = 1;
        match.shotManager.isAvailable = false;
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            yield return (IEnumerator)Invoke(movement, "ContinueDribblerMovement");
            Assert.That(movement.isWaitingForSnapshotDecision, Is.True);
            Assert.That(match.shotManager.isAvailable, Is.False, "Movement owns this real snapshot offer.");
            audio.Director.ObserveMatchAudioState();
            var source = home ? audio.Director.homeReaction : audio.Director.awayReaction;
            Assert.That(source.isPlaying, Is.True, "An actual movement snapshot offer must trigger the urging clip.");
            Assert.That(source.clip, Is.SameAs(audio.Director.library.Get(GameplayAudioPool.SnapshotAvailable)[0]));
            Assert.That(source.volume, Is.EqualTo((home ? 0.7f : 0.3f) * 0.8f * Mathf.Pow(10f, 6f / 20f) * 0.75f).Within(0.0001f));
            source.time = 1f;
            yield return null;
            audio.Director.ObserveMatchAudioState();
            Assert.That(source.isPlaying && source.time >= 1f, Is.True, "The next frame must not stop or restart the cue.");
            keeper.isActivated = true;
            movement.isWaitingForSnapshotDecision = false;
            audio.Director.ObserveMatchAudioState();
            Assert.That(source.isPlaying && source.time >= 1f, Is.True, "A defending GK move must not stop the one-shot.");
            keeper.isActivated = false;
            movement.isWaitingForSnapshotDecision = true;
            source.Stop();
            audio.Director.ObserveMatchAudioState();
            Assert.That(source.isPlaying, Is.False, "A brief availability interruption must not repeat the cue each pace.");
        }
    }

    public IEnumerator RunDeferredSnapshotAudio(string branch)
    {
        Board(1);
        match.currentState = MatchManager.GameState.MovementPhase;
        movement.isActivated = true;
        match.shotManager.isAvailable = false;
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            if (branch == "blocked" || branch == "stale")
                audio.Director.PlayReaction("Owned prior duel", GameplayAudioConfig.Incident(GameplayAudioEvents.IncidentKind.DuelWon, true));
            if (branch == "gk") keeper.isActivated = true;
            if (branch == "muted") GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Stadium, false);
            if (branch == "paused") { Set(match, "isPauseMenuOpen", true); audio.Director.SetPaused(true); }
            movement.isWaitingForSnapshotDecision = true;
            audio.Director.ObserveMatchAudioState();
            Assert.That(audio.Director.homeReaction.clip, Is.Not.SameAs(audio.Director.library.Get(GameplayAudioPool.SnapshotAvailable)[0]));
            audio.Director.homeReaction.Stop(); audio.Director.awayReaction.Stop();
            keeper.isActivated = false;
            if (branch == "stale") movement.isWaitingForSnapshotDecision = false;
            if (branch == "paused") { Set(match, "isPauseMenuOpen", false); audio.Director.SetPaused(false); }
            if (branch == "muted") GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Stadium, true);
            audio.Director.ObserveMatchAudioState();
            Assert.That(audio.Director.homeReaction.isPlaying, Is.EqualTo(branch == "blocked" || branch == "gk"));
            if (audio.Director.homeReaction.isPlaying)
                Assert.That(audio.Director.homeReaction.clip, Is.SameAs(audio.Director.library.Get(GameplayAudioPool.SnapshotAvailable)[0]));
        }
    }

    public IEnumerator RunInternationalCrowdAudio(string matchType, bool homeScored)
    {
        Board(1);
        match.gameData.gameSettings.matchType = matchType;
        match.gameData.gameSettings.includeInternationals = matchType == "Club";
        float homeShare = matchType == "International" ? 0.5f : 0.7f;
        float awayShare = matchType == "International" ? 0.5f : 0.3f;
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            audio.Director.AdvanceAudio(GameplayAudioConfig.ChatterFadeSeconds);
            Assert.That(audio.Director.homeChatter.volume + audio.Director.homeChatterNext.volume,
                Is.InRange(homeShare * 0.4f * 0.75f, homeShare * 0.5f * 0.75f));
            Assert.That(audio.Director.awayChatter.volume + audio.Director.awayChatterNext.volume,
                Is.InRange(awayShare * 0.4f * 0.75f, awayShare * 0.5f * 0.75f));
            Assert.That(audio.Director.TryPlayChant(true) && audio.Director.TryPlayChant(false), Is.True);
            Assert.That(audio.Director.homeChant.volume, Is.EqualTo(homeShare * 0.3f * 0.75f).Within(0.0001f));
            Assert.That(audio.Director.awayChant.volume, Is.EqualTo(awayShare * 0.3f * 0.75f).Within(0.0001f));
            Assert.That(audio.Director.PlayGoal("Owned mixed-share goal", homeScored), Is.True);
            audio.Director.AdvanceAudio(GameplayAudioConfig.ChantFadeSeconds);
            Assert.That(audio.Director.homeReaction.volume + audio.Director.homeChatter.volume + audio.Director.homeChatterNext.volume,
                Is.EqualTo(homeShare * 0.75f).Within(0.0001f));
            Assert.That(audio.Director.awayReaction.volume + audio.Director.awayChatter.volume + audio.Director.awayChatterNext.volume,
                Is.EqualTo(awayShare * 0.75f).Within(0.0001f));
            audio.Director.SetPaused(true); audio.Director.SetPaused(false);
            match.currentState = MatchManager.GameState.FreeKickKickerSelect;
            audio.Director.ObserveMatchAudioState();
            audio.Director.AdvanceAudio(GameplayAudioConfig.ChatterFadeSeconds);
            Assert.That(audio.Director.homePreparation.volume, Is.EqualTo(homeShare * 0.5f * 0.75f).Within(0.0001f));
            Assert.That(audio.Director.awayPreparation.volume, Is.EqualTo(awayShare * 0.5f * 0.75f).Within(0.0001f));
            GameplayAudioPreferences.Local.SetEnabled(GameplayAudioCategory.Stadium, false);
            Assert.That(audio.Director.homePreparation.volume + audio.Director.awayPreparation.volume, Is.Zero);
        }
    }
}
