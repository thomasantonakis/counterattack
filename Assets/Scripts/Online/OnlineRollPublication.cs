using System;
using System.Globalization;

namespace CounterAttack.Online
{
    [Serializable]
    public sealed class OnlineRollPublication
    {
        public long sequence;
        public string context, actorId, actorName, team;
        public int roll;
        public bool isJackpot, jackpotEnabled;

        public static string ActorId(PlayerToken token) => token == null ? null
            : (token.isHomeTeam ? "home:" : "away:") + token.jerseyNumber.ToString(CultureInfo.InvariantCulture);

        public OnlineRollPublication Copy() => new()
        { sequence=sequence, context=context, actorId=actorId, actorName=actorName, team=team,
            roll=roll, isJackpot=isJackpot, jackpotEnabled=jackpotEnabled };
    }
}
