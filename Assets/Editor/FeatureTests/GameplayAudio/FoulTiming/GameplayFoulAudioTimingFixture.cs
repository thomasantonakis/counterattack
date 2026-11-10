using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunCardAudioAfterFoul(string card, bool penalisedHome, int direction)
    {
        Board(direction);
        Invoke(grid, "CalculateGoalDistanceAndDangerousTackleCaches");
        if (penalisedHome) match.ChangePossession();
        var offender = penalisedHome ? attacker : defender;
        var carrier = penalisedHome ? defender : attacker;
        Place(carrier, Hex(0));
        var attackingDirection = carrier.isHomeTeam ? match.homeTeamDirection : match.awayTeamDirection;
        bool dangerous = card == "straight_red";
        Place(offender, carrier.currentHex.GetNeighbors(grid).First(h => h != null && h.GetOccupyingToken() == null
            && carrier.currentHex.IsDangerousTacklingPosition(attackingDirection, h) == dangerous));
        ball.PlaceAtCell(carrier.currentHex);
        match.SetLastToken(carrier);
        match.refereeLeniency = 4;
        movement.selectedDefender = offender;
        movement.selectedToken = carrier;
        if (card == "second_yellow") offender.ReceiveYellowCard();
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            Invoke(movement, "StartTackleDiceRollSequence");
            movement.PerformTackleDiceRoll(true, 1);
            movement.PerformTackleDiceRoll(false, 6);
            yield return Until(() => movement.isWaitingForYellowCardRoll, "Actual foul enters its existing leniency roll");
            int count = 0;
            Action<GameplayAudioEvents.IncidentCue> observe = cue =>
            { if (cue.Kind == GameplayAudioEvents.IncidentKind.CardShown) { count++; Assert.That(cue.HomeTeam, Is.EqualTo(!penalisedHome)); } };
            GameplayAudioEvents.IncidentConfirmed += observe;
            try
            {
                movement.PerformLeniencyTest(card == "no_card" ? 1 : 6);
                bool shown = card != "no_card";
                Assert.That(count, Is.EqualTo(shown ? 1 : 0));
                var reaction = penalisedHome ? audio.Director.awayReaction : audio.Director.homeReaction;
                Assert.That(reaction.isPlaying, Is.EqualTo(shown));
                if (shown)
                {
                    Assert.That(reaction.clip, Is.SameAs(audio.Director.library.Get(GameplayAudioPool.OpponentError)[0]));
                    Assert.That(reaction.volume, Is.EqualTo((penalisedHome ? 0.3f : 0.7f) * 0.5f * GameplayAudioConfig.CrowdOutputGain).Within(0.0001f));
                }
                Assert.That(offender.isBooked || offender.isSentOff, Is.EqualTo(shown));
                Assert.That(offender.isSentOff, Is.EqualTo(card == "second_yellow" || card == "straight_red"));
                Assert.That(match.gameData.stats.GetPlayerStats(offender).redCards,
                    Is.EqualTo(card == "second_yellow" || card == "straight_red" ? 1 : 0));
                Assert.That(carrier.isInjured, Is.False, "Audio cannot advance the pending injury check.");
            }
            finally { GameplayAudioEvents.IncidentConfirmed -= observe; }
        }
    }

    public IEnumerator RunEarlyTackleFoulAudio(string scenario, bool penalisedHome, int direction)
    {
        Board(direction);
        Invoke(grid, "CalculateGoalDistanceAndDangerousTackleCaches");
        if (penalisedHome) match.ChangePossession();
        var offender = penalisedHome ? attacker : defender;
        var carrier = penalisedHome ? defender : attacker;
        Place(carrier, Hex(0));
        var attackingDirection = carrier.isHomeTeam ? match.homeTeamDirection : match.awayTeamDirection;
        bool dangerous = scenario.StartsWith("danger", StringComparison.Ordinal);
        var destination = carrier.currentHex.GetNeighbors(grid).First(h => h != null && !h.isOutOfBounds
            && h.GetOccupyingToken() == null && carrier.currentHex.IsDangerousTacklingPosition(attackingDirection, h) == dangerous);
        Place(offender, destination);
        ball.PlaceAtCell(carrier.currentHex);
        match.SetLastToken(carrier);
        movement.selectedDefender = offender;
        movement.selectedToken = carrier;
        movement.isNutmegInProgress = scenario.StartsWith("nutmeg", StringComparison.Ordinal);
        movement.nutmegVictim = movement.isNutmegInProgress ? offender : null;
        bool jackpot = scenario == "jackpot";
        int roll = jackpot ? 6 : scenario[scenario.Length - 1] - '0';
        bool foul = !jackpot && roll <= (dangerous ? 2 : 1);
        using (var audio = new GoalAudioRig(root.transform))
        {
            yield return null;
            Invoke(movement, "StartTackleDiceRollSequence");
            int notifications = 0;
            Action<GameplayAudioEvents.IncidentCue> observe = cue =>
            { if (cue.Kind == GameplayAudioEvents.IncidentKind.Foul) notifications++; };
            GameplayAudioEvents.IncidentConfirmed += observe;
            try
            {
                var state = match.currentState;
                var possession = match.teamInAttack;
                int priorDribbles = match.gameData.stats.GetPlayerStats(carrier).dribblesMade;
                movement.PerformTackleDiceRoll(true, new RollInputOverride { hasOverride = true, roll = roll, isJackpot = jackpot });
                Assert.That(notifications, Is.EqualTo(foul ? 1 : 0), "The tackler's roll establishes the audible foul immediately.");
                Assert.That((bool)typeof(MovementPhaseManager).GetField("tackleAttackerRolled",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(movement), Is.False);
                Assert.That(match.currentState, Is.EqualTo(state));
                Assert.That(match.teamInAttack, Is.EqualTo(possession));
                Assert.That(match.gameData.stats.GetPlayerStats(carrier).dribblesMade, Is.EqualTo(priorDribbles),
                    "The sound must not perform the later foul resolution.");
                var reaction = penalisedHome ? audio.Director.homeReaction : audio.Director.awayReaction;
                Assert.That(reaction.isPlaying, Is.EqualTo(foul));
                if (foul)
                {
                    Assert.That(reaction.clip, Is.SameAs(audio.Director.library.Get(GameplayAudioPool.FoulReaction)[0]));
                    reaction.Stop();
                    movement.PerformTackleDiceRoll(false, 6);
                    yield return Until(() => movement.isWaitingForYellowCardRoll, "Dribbler roll retains the existing booking continuation");
                    Assert.That(notifications, Is.EqualTo(1), "The later foul statistic must not repeat the cue.");
                    Assert.That(reaction.isPlaying, Is.False);
                    Assert.That(match.gameData.stats.GetPlayerStats(carrier).dribblesMade, Is.EqualTo(priorDribbles + 1));
                }
            }
            finally { GameplayAudioEvents.IncidentConfirmed -= observe; }
        }
    }
}
