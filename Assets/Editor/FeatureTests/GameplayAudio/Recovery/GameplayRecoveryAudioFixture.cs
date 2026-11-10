using System.Collections;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunPhysicalRecoveryAudio()
    {
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            yield return RunGroundPassLegacy("second_interceptor");
            Assert.That(audio.Director.awayReaction.clip, Is.SameAs(audio.Director.library.Get(GameplayAudioPool.DuelWon)[0]));
            Assert.That(audio.Director.homeReaction.clip, Is.SameAs(audio.Director.library.Get(GameplayAudioPool.DuelLost)[0]));
        }
    }

    public IEnumerator RunRecoveryAudio(string kind, bool home, int direction)
    {
        Board(direction);
        var actor = home ? attacker : defender;
        var opponent = home ? defender : attacker;
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            var state = match.currentState;
            var possession = match.teamInAttack;
            int rolls = match.TimingRollSequence;
            var rng = UnityEngine.Random.state;
            bool success = kind != "attempt" && kind != "teammate" && kind != "tackle_duplicate" && kind != "gk_duplicate";
            if (kind == "attempt") match.gameData.gameLog.LogEvent(actor, MatchManager.ActionType.InterceptionAttempt);
            else if (kind == "tackle_duplicate")
            {
                match.gameData.gameLog.LogEvent(actor, MatchManager.ActionType.GroundDuelWon, connectedToken: opponent, tackleType: "successful");
                audio.Director.homeReaction.Stop(); audio.Director.awayReaction.Stop();
                match.gameData.gameLog.LogEvent(actor, MatchManager.ActionType.BallRecovery, connectedToken: opponent, recoveryType: "tackle");
            }
            else if (kind == "gk_duplicate")
                match.gameData.gameLog.LogEvent(home ? homeGK : awayGK, MatchManager.ActionType.BallRecovery, connectedToken: opponent, recoveryType: "shot");
            else if (kind == "loose" || kind == "header" || kind == "teammate")
                match.gameData.gameLog.LogEvent(actor, MatchManager.ActionType.BallRecovery,
                    connectedToken: kind == "teammate" ? actor : opponent, recoveryType: kind == "teammate" ? "loose" : kind);
            else match.gameData.gameLog.LogEvent(actor, MatchManager.ActionType.InterceptionSuccess, connectedToken: opponent, recoveryType: kind);
            Assert.That(audio.Director.homeReaction.isPlaying || audio.Director.awayReaction.isPlaying, Is.EqualTo(success));
            if (success)
            {
                AssertIncidentStand(audio.Director, home ? audio.Director.homeReaction : audio.Director.awayReaction, GameplayAudioPool.DuelWon, home);
                AssertIncidentStand(audio.Director, home ? audio.Director.awayReaction : audio.Director.homeReaction, GameplayAudioPool.DuelLost, !home);
            }
            Assert.That(match.currentState, Is.EqualTo(state));
            Assert.That(match.teamInAttack, Is.EqualTo(possession));
            Assert.That(match.TimingRollSequence, Is.EqualTo(rolls));
            Assert.That(UnityEngine.Random.state, Is.EqualTo(rng));
        }
    }
}
