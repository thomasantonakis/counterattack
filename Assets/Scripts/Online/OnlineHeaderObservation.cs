using System;

namespace CounterAttack.Online
{
    [Serializable]
    public sealed class OnlineHeaderScore
    {
        public string actorId;
        public int roll, total;
    }

    // Values observed from the host's real challenge; guests never resolve a score.
    [Serializable]
    public sealed class OnlineHeaderObservation
    {
        public string[] attackers, defenders;
        public string roller, winner;
        public OnlineHeaderScore[] scores;
    }
}
