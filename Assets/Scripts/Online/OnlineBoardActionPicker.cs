using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace CounterAttack.Online
{
    /// <summary>Maps personal input to an unambiguous, already published semantic choice.</summary>
    public static class OnlineBoardActionPicker
    {
        private static string Unique(OnlineGroundPassState state, Func<string,bool> predicate)
        {
            var matches=state?.legalActions?.Where(action=>action!=null && predicate(action)).Distinct().Take(2).ToArray();
            return matches?.Length==1?matches[0]:null;
        }
        private static string OneOf(OnlineGroundPassState state,params string[] choices)=>Unique(state,action=>Array.IndexOf(choices,action)>=0);
        public static string Actor(OnlineGroundPassState state,string id)
        {
            if(string.IsNullOrEmpty(id))return null;
            var parts=id.Split(':');
            if(parts.Length!=2 || (parts[0]!="home" && parts[0]!="away")
                || !int.TryParse(parts[1],NumberStyles.None,CultureInfo.InvariantCulture,out int jersey)
                || jersey<1 || jersey>18 || jersey.ToString(CultureInfo.InvariantCulture)!=parts[1])return null;
            return Unique(state,action=>action.EndsWith(":"+id,StringComparison.Ordinal));
        }
        public static string Hex(OnlineGroundPassState state,int x,int z)
        {
            string suffix=":"+x.ToString(CultureInfo.InvariantCulture)+":"+z.ToString(CultureInfo.InvariantCulture);
            return Unique(state,action=>action.EndsWith(suffix,StringComparison.Ordinal));
        }
        public static string Key(OnlineGroundPassState state,KeyCode key)
        {
            string available(string action)=>state?.legalActions?.Contains(action)==true?action:null;
            switch(key)
            {
                case KeyCode.P:return OneOf(state,"ground.start","set-piece.ground","throw.feet","final-third.pass");
                case KeyCode.L:return OneOf(state,"long.start","set-piece.long");
                case KeyCode.C:return OneOf(state,"high.start","set-piece.high","throw.head");
                case KeyCode.F:return OneOf(state,"ftp.start","header.backpass.feet","keeper.pickup.feet","foul.take");
                case KeyCode.M:return OneOf(state,"movement.start","throw.movement");
                case KeyCode.D:return OneOf(state,"shot.keeper.hands","duel.dive","final-third.drop");
                case KeyCode.B:return OneOf(state,"shot.keeper.block","header.control");
                case KeyCode.N:return OneOf(state,"duel.stand","duel.nutmeg");
                case KeyCode.T:return OneOf(state,"duel.tackle","throw.now");
                case KeyCode.S:return OneOf(state,"movement.snapshot","set-piece.shot",available("shot.confirm")!=null?"shot.confirm":"shot.start");
                case KeyCode.Q:return OneOf(state,"keeper.quick","keeper.quick.start");
                case KeyCode.K:return OneOf(state,"keeper.final-third","final-third.kick");
                case KeyCode.H:return OneOf(state,"header.free","header.pass","header.backpass.hands","keeper.pickup.hands");
                case KeyCode.G:return available("high.keeper.stay");
                case KeyCode.V:return available("long.keeper.claim");
                case KeyCode.A:return available("foul.advantage");
                case KeyCode.X:return OneOf(state,"movement.snapshot.decline","dribble.finish","shot.cancel","shot.decline","movement.forfeit",
                    "high.skip","high.keeper.decline","ftp.skip","long.keeper.decline","keeper.stay","set-piece.skip","duel.stay","dribble.decline-nutmeg");
                case KeyCode.R:return Unique(state,action=>action.EndsWith(".roll",StringComparison.Ordinal)
                    || action=="shot.save" || action=="shot.handling" || action=="shot.block"
                    || action=="foul.leniency" || action=="foul.injury" || action=="header.interception");
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if(state?.legalActions?.Any(action=>action?.EndsWith(".confirm",StringComparison.Ordinal)==true || action=="final-third.forfeit")==true)
                        return Unique(state,action=>action.EndsWith(".confirm",StringComparison.Ordinal) || action=="final-third.forfeit");
                    return state?.hasSelectedTarget==true?Hex(state,state.selectedX,state.selectedZ):null;
                default:return null;
            }
        }
    }
}
