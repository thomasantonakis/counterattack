using System;
using System.Linq;
namespace CounterAttack.Online
{
    [Serializable] public sealed class OnlineShootoutSnapshot
    {
        public bool selecting,active,complete,homeReady,awayReady,suddenDeath,winnerIsHome,homeFirst,rightGoal,tossWinnerHome;
        public string[] homeOrder,awayOrder,homeEligible,awayEligible;
        public int homeScore,awayScore,homeAttempts,awayAttempts,homePreGoals,awayPreGoals;
        public bool[] homeResults,awayResults;
        public string shooter,keeper;
        internal bool Valid(OnlineGroundPassState state)
        {
            bool Order(string[] order,string[] eligible,string side)=>order!=null && eligible!=null && order.Length is >=1 and <=11
                && eligible.Length>=order.Length && eligible.Length<=11 && order.Distinct().Count()==order.Length && eligible.Distinct().Count()==eligible.Length
                && order.All(eligible.Contains) && eligible.All(id=>state.tokens.Any(t=>t.id==id && t.runtime?.teamSide==side && t.runtime.isPlaying && !t.runtime.isSentOff));
            return Order(homeOrder,homeEligible,"Home") && Order(awayOrder,awayEligible,"Away") && homeOrder.Length==awayOrder.Length
                && homeOrder.Length==Math.Min(homeEligible.Length,awayEligible.Length)
                && !(selecting && (active || complete)) && !(complete && active)
                && homeAttempts>=0 && awayAttempts>=0 && homeAttempts<=10000 && awayAttempts<=10000
                && Math.Abs(homeAttempts-awayAttempts)<=1 && homeScore>=0 && awayScore>=0 && homeScore<=homeAttempts && awayScore<=awayAttempts
                && homeResults!=null && awayResults!=null && homeResults.Length==homeAttempts && awayResults.Length==awayAttempts
                && homeResults.Count(r=>r)==homeScore && awayResults.Count(r=>r)==awayScore
                && (shooter==null || state.tokens.Any(t=>t.id==shooter)) && (keeper==null || state.tokens.Any(t=>t.id==keeper));
        }
    }
}
