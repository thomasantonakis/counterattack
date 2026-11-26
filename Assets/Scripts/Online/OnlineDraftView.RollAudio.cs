using UnityEngine;

namespace CounterAttack.Online
{
    public sealed partial class OnlineDraftView
    {
        private OnlineDraftState lastAudibleRollState;
        private bool waitingOnRoll, wigglePlayed;
        private float wiggleDeadline;

        private void PresentRollAudio(OnlineDraftState state, bool connected)
        {
            var previous = lastAudibleRollState;
            lastAudibleRollState = connected ? state : null;
            bool waiting = connected && state?.phase is "roll" or "opening-roll";
            bool sameRound = previous != null && state != null && previous.connectionRevision == state.connectionRevision
                && previous.selection == state.selection && previous.rollRound == state.rollRound;
            bool newResult = connected && sameRound && (previous.phase switch
            {
                "roll" => state.phase is "roll" or "roll-result",
                "opening-roll" => state.phase is "opening-roll" or "opening-result",
                _ => false
            });
            if (newResult && (previous.homeRoll == 0 && state.homeRoll > 0 || previous.awayRoll == 0 && state.awayRoll > 0))
            {
                StopRollWiggle();
                MenuAudioPlayer.Play(GameplayAudioPool.Dice);
            }

            if (!waiting || !sameRound || previous.phase != state.phase || previous.turn != state.turn)
            {
                StopRollWiggle();
                waitingOnRoll = waiting;
                if (waiting) wiggleDeadline = Time.unscaledTime + DraftOrderPanelController.RollWiggleDelaySeconds;
            }
        }

        private void TickRollWiggle()
        {
            if (waitingOnRoll && !wigglePlayed && Time.unscaledTime >= wiggleDeadline)
                wigglePlayed = MenuAudioPlayer.Play(GameplayAudioPool.DiceWiggle);
        }

        private void StopRollWiggle()
        {
            if (wigglePlayed) MenuAudioPlayer.StopDiceWiggle();
            waitingOnRoll = wigglePlayed = false;
        }

        private void OnDisable()
        {
            StopRollWiggle();
            lastAudibleRollState = null;
        }
    }
}
