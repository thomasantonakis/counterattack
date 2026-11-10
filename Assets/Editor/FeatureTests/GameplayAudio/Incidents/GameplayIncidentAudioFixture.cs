using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOutsideHandballAudio(bool tie)
    {
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            int count = 0;
            Action<GameplayAudioEvents.IncidentCue> observe = cue =>
            {
                if (cue.Kind == GameplayAudioEvents.IncidentKind.Foul)
                { count++; Assert.That(cue.HomeTeam, Is.False); }
            };
            GameplayAudioEvents.IncidentConfirmed += observe;
            try
            {
                yield return RunKeeperFoulLegacy(tie ? "hand_tie" : "hand_win");
                Assert.That(count, Is.EqualTo(1));
            }
            finally { GameplayAudioEvents.IncidentConfirmed -= observe; }
        }
    }

    public IEnumerator RunRecoveryFoulAudio(bool penalisedHome)
    {
        Board(1);
        if (penalisedHome) match.ChangePossession();
        var offender = penalisedHome ? attacker : defender;
        var carrier = penalisedHome ? defender : attacker;
        ball.PlaceAtCell(carrier.currentHex);
        match.SetLastToken(carrier);
        movement.selectedDefender = offender;
        movement.selectedToken = carrier;
        movement.isDribblerRunning = true;
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            movement.StartCoroutine(movement.PerformBallInterceptionDiceRoll(1));
            yield return Until(() => movement.isWaitingForYellowCardRoll, "Recovery foul reaches its existing booking decision");
            AssertIncidentStand(audio.Director, penalisedHome ? audio.Director.homeReaction : audio.Director.awayReaction,
                GameplayAudioPool.FoulReaction, penalisedHome);
            Assert.That((penalisedHome ? audio.Director.awayReaction : audio.Director.homeReaction).isPlaying, Is.False);
            Assert.That(match.gameData.stats.GetPlayerStats(offender).interceptionsMade, Is.Zero);
            Assert.That(match.gameData.stats.GetPlayerStats(offender).yellowCards, Is.Zero, "Audio cannot resolve the pending booking.");
        }
    }

    public IEnumerator RunOutOfBoundsAudio(string restart, int direction)
    {
        Board(direction);
        var manager = Add<OutOfBoundsManager>();
        manager.hexGrid = grid; manager.ball = ball;
        manager.throwInManager = CreateLegacyThrowInManager();
        manager.outOfBoundsPushManager = Add<OutOfBoundsPushManager>();
        manager.outOfBoundsPushManager.Configure(grid, ball);
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            IEnumerator operation = restart == "throw"
                ? (IEnumerator)Invoke(manager, "HandleThrowIn", Hex(0, 12), "inaccuracy", attacker)
                : manager.HandleGoalKickOrCorner(Hex(17 * direction, 10),
                    (restart == "goal_kick" ? direction : -direction) > 0 ? "RightGoalLine" : "LeftGoalLine", "inaccuracy", attacker, defender);
            try
            {
                Assert.That(operation.MoveNext(), Is.True);
                AssertIncidentStand(audio.Director, audio.Director.awayReaction, GameplayAudioPool.OpponentError, false);
                Assert.That(audio.Director.homeReaction.isPlaying, Is.False);
            }
            finally { (operation as IDisposable)?.Dispose(); }
        }
    }

    public IEnumerator RunIncidentAudio(string action, bool home, int direction)
    {
        Board(direction);
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            var actor = home ? attacker : defender;
            var other = home ? defender : attacker;
            var state = match.currentState;
            var possession = match.teamInAttack;
            var rng = UnityEngine.Random.state;
            int rolls = match.TimingRollSequence;
            GameplayAudioPool team = GameplayAudioPool.None, opponent = GameplayAudioPool.None;
            switch (action)
            {
                case "tackle": case "nutmeg": case "keep":
                    match.gameData.gameLog.LogEvent(actor, MatchManager.ActionType.GroundDuelWon,
                        connectedToken: other, tackleType: action == "tackle" ? "successful" : action);
                    team = GameplayAudioPool.DuelWon; opponent = GameplayAudioPool.DuelLost; break;
                case "steal":
                    match.gameData.gameLog.LogEvent(actor, MatchManager.ActionType.InterceptionSuccess,
                        connectedToken: other, recoveryType: "steal");
                    team = GameplayAudioPool.DuelWon; opponent = GameplayAudioPool.DuelLost; break;
                case "foul":
                    match.gameData.gameLog.LogEvent(other, MatchManager.ActionType.GroundDuelWon,
                        connectedToken: actor, tackleType: "foul");
                    // The statistic follows the tackler's earlier audible roll; it stays silent here.
                    break;
                case "off_target":
                    match.gameData.gameLog.LogEvent(actor, MatchManager.ActionType.ShotOffTarget);
                    team = GameplayAudioPool.Groan; opponent = GameplayAudioPool.OpponentError; break;
                case "block":
                    match.gameData.gameLog.LogEvent(actor, MatchManager.ActionType.ShotBlocked, connectedToken: other);
                    match.gameData.gameLog.LogEvent(other, MatchManager.ActionType.ShotBlockMade, connectedToken: actor);
                    team = GameplayAudioPool.Groan; opponent = GameplayAudioPool.DefensiveShotRelief; break;
                case "save":
                    var goalkeeper = home ? homeGK : awayGK;
                    match.gameData.gameLog.LogEvent(other, MatchManager.ActionType.ShotBlocked, connectedToken: goalkeeper);
                    Assert.That(audio.Director.homeReaction.isPlaying || audio.Director.awayReaction.isPlaying, Is.False);
                    match.gameData.gameLog.LogEvent(goalkeeper, MatchManager.ActionType.SaveMade, saveType: "held");
                    team = GameplayAudioPool.DefensiveShotRelief; opponent = GameplayAudioPool.Groan; break;
                case "kickoff":
                    match.gameData.gameLog.LogEvent(actor, MatchManager.ActionType.KickOffPassAttempt); break;
                case "half_time": match.RecordMatchTransition("half_time", "started"); break;
                case "full_time": match.RecordMatchTransition("full_time", "match_ended"); break;
                case "pending":
                    match.gameData.gameLog.LogEvent(actor, MatchManager.ActionType.PassCompleted);
                    match.gameData.gameLog.LogEvent(actor, MatchManager.ActionType.Injured, connectedToken: other);
                    match.gameData.gameLog.LogEvent(actor, MatchManager.ActionType.InterceptionAttempt);
                    break;
            }
            if (action == "kickoff" || action == "half_time" || action == "full_time")
            {
                var pool = action == "kickoff" ? GameplayAudioPool.KickOff
                    : action == "half_time" ? GameplayAudioPool.HalfTime : GameplayAudioPool.FullTime;
                Assert.That(audio.Director.whistle.clip, Is.SameAs(audio.Director.library.Get(pool)[0]));
                Assert.That(audio.Director.whistle.isPlaying, Is.True);
            }
            else
            {
                AssertIncidentStand(audio.Director, home ? audio.Director.homeReaction : audio.Director.awayReaction, team, home);
                AssertIncidentStand(audio.Director, home ? audio.Director.awayReaction : audio.Director.homeReaction, opponent, !home);
            }
            Assert.That(match.currentState, Is.EqualTo(state));
            Assert.That(match.teamInAttack, Is.EqualTo(possession));
            Assert.That(match.TimingRollSequence, Is.EqualTo(rolls));
            Assert.That(UnityEngine.Random.state, Is.EqualTo(rng));
            Assert.That(match.gameData.stats.homeTeamStats.totalGoals + match.gameData.stats.awayTeamStats.totalGoals, Is.Zero);
        }
    }

    private static void AssertIncidentStand(GameplayAudioDirector director, AudioSource source, GameplayAudioPool pool, bool home)
    {
        if (pool == GameplayAudioPool.None) { Assert.That(source.isPlaying, Is.False); return; }
        Assert.That(source.clip, Is.SameAs(director.library.Get(pool)[0]));
        Assert.That(source.isPlaying, Is.True);
        float layer = pool == GameplayAudioPool.SnapshotAvailable ? 0.8f * Mathf.Pow(10f, 6f / 20f) : 0.5f;
        Assert.That(source.volume, Is.EqualTo((home ? 0.7f : 0.3f) * layer * GameplayAudioConfig.CrowdOutputGain).Within(0.0001f));
    }

    public IEnumerator RunIncidentSubstitution(bool home, bool injury, bool multiple)
    {
        Board(1);
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            var outgoing = home ? attacker : defender;
            var incoming = Token("Owned substitute", home, false, 19);
            if (injury) outgoing.ReceiveInjury();
            Set(match, "isPauseMenuOpen", true);
            match.RecordSubstitutionEvent(outgoing, incoming);
            if (multiple) match.RecordSubstitutionEvent(outgoing, incoming);
            Assert.That(audio.Director.announcements.isPlaying, Is.False);
            audio.Director.AdvanceAudio(1f);
            Assert.That(audio.Director.announcements.isPlaying, Is.False);
            Set(match, "isPauseMenuOpen", false);
            audio.Director.SetPaused(false);
            audio.Director.AdvanceAudio(0f);
            var pool = home ? GameplayAudioPool.HomeSubstitution
                : multiple ? GameplayAudioPool.DoubleAwaySubstitution : GameplayAudioPool.AwaySubstitution;
            Assert.That(audio.Director.announcements.clip, Is.SameAs(audio.Director.library.Get(pool)[0]));
            Assert.That(audio.Director.announcements.isPlaying, Is.True);
            Assert.That(match.GetPlayerSubOffCount(outgoing), Is.EqualTo(multiple ? 2 : 1));
            if (injury) AssertIncidentStand(audio.Director, home ? audio.Director.homeReaction : audio.Director.awayReaction,
                GameplayAudioPool.InjurySubstitution, home);
            audio.Director.announcements.Stop();
            audio.Director.AdvanceAudio(0f);
            Assert.That(audio.Director.announcements.isPlaying, Is.False, "The pending batch plays once.");
        }
    }

    public IEnumerator RunPreparationAudio(bool home, int direction)
    {
        Board(direction);
        match.teamInAttack = home ? MatchManager.TeamInAttack.Home : MatchManager.TeamInAttack.Away;
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            match.currentState = MatchManager.GameState.FreeKickKickerSelect;
            audio.Director.ObserveMatchAudioState();
            audio.Director.AdvanceAudio(GameplayAudioConfig.ChatterFadeSeconds);
            Assert.That(audio.Director.homePreparation.isPlaying && audio.Director.awayPreparation.isPlaying, Is.True);
            Assert.That(audio.Director.homePreparation.volume, Is.EqualTo(0.7f * 0.5f * GameplayAudioConfig.CrowdOutputGain).Within(0.0001f));
            Assert.That(audio.Director.awayPreparation.volume, Is.EqualTo(0.3f * 0.5f * GameplayAudioConfig.CrowdOutputGain).Within(0.0001f));
            audio.Director.homePreparation.time = 6f;
            audio.Director.AdvanceAudio(1f);
            Assert.That(audio.Director.homePreparation.isPlaying && audio.Director.homePreparationNext.isPlaying, Is.True);
            Assert.That(audio.Director.homePreparation.volume + audio.Director.homePreparationNext.volume,
                Is.EqualTo(0.7f * 0.5f * GameplayAudioConfig.CrowdOutputGain).Within(0.0001f));
            match.currentState = MatchManager.GameState.FreeKickExecution;
            audio.Director.ObserveMatchAudioState();
            Assert.That(audio.Director.homePreparation.isPlaying || audio.Director.homePreparationNext.isPlaying || audio.Director.awayPreparation.isPlaying, Is.False);
            Assert.That(audio.Director.homeChatter.isPlaying && audio.Director.awayChatter.isPlaying, Is.True);
        }
    }

    public IEnumerator RunSnapshotAudio(bool home)
    {
        Board(1);
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            match.currentState = MatchManager.GameState.MovementPhase;
            var actor = home ? attacker : defender;
            ball.PlaceAtCell(actor.currentHex);
            match.shotManager.isAvailable = true;
            audio.Director.ObserveMatchAudioState();
            AssertIncidentStand(audio.Director, home ? audio.Director.homeReaction : audio.Director.awayReaction,
                GameplayAudioPool.SnapshotAvailable, home);
            audio.Director.homeReaction.Stop(); audio.Director.awayReaction.Stop();
            audio.Director.ObserveMatchAudioState();
            Assert.That(audio.Director.homeReaction.isPlaying || audio.Director.awayReaction.isPlaying, Is.False,
                "Availability must not announce every frame.");
            foreach (var state in new[] { MatchManager.GameState.HalfTime, MatchManager.GameState.MatchEnded,
                MatchManager.GameState.KickOffSetup, MatchManager.GameState.PostGoalKickOffSetup })
            {
                match.shotManager.isAvailable = false;
                audio.Director.ObserveMatchAudioState();
                match.shotManager.isAvailable = true;
                match.currentState = state;
                audio.Director.ObserveMatchAudioState();
                Assert.That(audio.Director.homeReaction.isPlaying || audio.Director.awayReaction.isPlaying, Is.False,
                    "A stale availability flag cannot prompt shooting during " + state);
            }
        }
    }

    public IEnumerator RunStealAudio(bool succeeds)
    {
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            int count = 0;
            GameplayAudioEvents.IncidentKind actual = default;
            Action<GameplayAudioEvents.IncidentCue> observe = cue =>
            {
                if (cue.Kind == GameplayAudioEvents.IncidentKind.DuelWon || cue.Kind == GameplayAudioEvents.IncidentKind.DuelLost)
                { count++; actual = cue.Kind; Assert.That(cue.HomeTeam, Is.False); }
            };
            GameplayAudioEvents.IncidentConfirmed += observe;
            try
            {
                yield return RunMovementStealLegacy(succeeds);
                Assert.That(count, Is.EqualTo(1));
                Assert.That(actual, Is.EqualTo(succeeds ? GameplayAudioEvents.IncidentKind.DuelWon : GameplayAudioEvents.IncidentKind.DuelLost));
            }
            finally { GameplayAudioEvents.IncidentConfirmed -= observe; }
        }
    }
}
