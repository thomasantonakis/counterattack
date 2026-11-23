using System;
namespace CounterAttack.Online
{
    [Serializable] public sealed class OnlineCoinTossSnapshot
    {
        public string purpose,stage;
        public int homeRoll,awayRoll;
        public int? rollSeconds;
        public bool hasWinner,winnerHome,choosingHome,canChooseKickoff;
        internal bool Valid()=>Enum.TryParse(purpose,out CoinTossPurpose parsed) && Enum.IsDefined(typeof(CoinTossPurpose),parsed)
            && (rollSeconds == null || (parsed is CoinTossPurpose.FirstHalf or CoinTossPurpose.PenaltyShootout) && stage is "home" or "away" && rollSeconds is >= 0 and <= 5)
            && homeRoll is >=0 and <=6 && awayRoll is >=0 and <=6
            && (stage=="home" && homeRoll==0 && awayRoll==0 && !hasWinner
                || stage=="away" && homeRoll>=1 && awayRoll==0 && !hasWinner
                || stage=="result" && homeRoll>=1 && awayRoll>=1 && !hasWinner
                || stage=="choice" && homeRoll>=1 && awayRoll>=1 && homeRoll!=awayRoll && hasWinner && winnerHome==(homeRoll>awayRoll)
                    && choosingHome==(canChooseKickoff?winnerHome:!winnerHome));
    }
}
