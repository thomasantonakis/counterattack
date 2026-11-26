using System;
using System.Collections.Generic;
using System.Linq;

namespace CounterAttack.Online
{
    public sealed partial class OnlineDraftAuthority
    {
        private bool goalkeeperPhase;
        private List<OnlineDraftCard> pool=new(),outfieldPool=new();
        private bool FreeSelection=>goalkeeperPhase || settings.draft!="Regular";
        private bool Repeatable=>goalkeeperPhase?settings.gkDraft=="Arcade":settings.draft=="Arcade";
        private bool ArcadePoolOpen=>Repeatable && (phase=="pick" || phase=="review" && !(homeReady && awayReady));
        private static IOrderedEnumerable<OnlineDraftCard> RankKeepers(IEnumerable<OnlineDraftCard> cards)=>cards.OrderByDescending(c=>c.saving).ThenByDescending(c=>c.handling).ThenByDescending(c=>c.pace).ThenByDescending(c=>c.aerial).ThenByDescending(c=>c.name);
        private static IOrderedEnumerable<OnlineDraftCard> RankOutfield(IEnumerable<OnlineDraftCard> cards)=>cards.OrderByDescending(c=>c.shooting+c.dribbling+c.pace+c.tackling+c.heading).ThenByDescending(c=>c.shooting).ThenByDescending(c=>c.pace).ThenByDescending(c=>c.dribbling).ThenByDescending(c=>c.tackling).ThenByDescending(c=>c.name);
        private bool StageFull(OnlineDraftCard[] roster)=>roster.Where((_,i)=>(i is 0 or 11)==goalkeeperPhase).All(c=>c!=null);
        private void BeginOutfield()
        {
            goalkeeperPhase=false;homeReady=awayReady=false;homeRoll=awayRoll=0;round=1;turn=OnlineTeam.Home;
            if(settings.draft=="Regular")phase="roll";
            else{pool=outfieldPool;phase=settings.draft=="Arcade"?"pick":"roll";}
        }
        private void InitializeInternational(OnlineDraftCard[] all)
        {
            for(int team=0;team<2;team++)
            {
                var roster=team==0?home:away;string name=team==0?settings.homeTeamName:settings.awayTeamName;
                var selected=all.Where(c=>string.Equals(c.type?.Trim(),"World Cup",StringComparison.OrdinalIgnoreCase) && string.Equals(c.country?.Trim(),name?.Trim(),StringComparison.OrdinalIgnoreCase)).ToArray();
                var keepers=RankKeepers(selected.Where(c=>c.goalkeeper)).Take(2).ToArray();
                var players=selected.Where(c=>!c.goalkeeper).OrderBy(c=>c.squadNumber<=0).ThenBy(c=>c.squadNumber<=0?int.MaxValue:c.squadNumber).ThenBy(c=>c.name).ToArray();
                if(keepers.Length!=2 || players.Length<roster.Length-2)throw new ArgumentException("International roster is incomplete: "+name);
                roster[0]=keepers[0];roster[11]=keepers[1];var assigned=new HashSet<string>();
                foreach(var c in players)
                {int index=c.squadNumber-1;if(index>=0 && index<roster.Length && index is not (0 or 11) && roster[index]==null){roster[index]=c;assigned.Add(c.id);}}
                foreach(var c in players.Where(c=>!assigned.Contains(c.id)))
                {int index=Array.FindIndex(roster,c=>c==null);if(index<0)break;roster[index]=c;}
            }
            phase="review";
        }
    }
}
