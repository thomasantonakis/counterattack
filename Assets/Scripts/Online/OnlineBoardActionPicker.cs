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
                case KeyCode.P:return available("ground.start");
                case KeyCode.L:return available("long.start");
                case KeyCode.C:return available("high.start");
                case KeyCode.F:return available("ftp.start");
                case KeyCode.M:return available("movement.start");
                case KeyCode.D:return Unique(state,action=>action=="shot.keeper.hands" || action=="duel.dive");
                case KeyCode.B:return available("shot.keeper.block");
                case KeyCode.N:return available("duel.stand");
                case KeyCode.T:return available("duel.tackle");
                case KeyCode.S:return available("shot.confirm")??available("shot.start");
                case KeyCode.R:return Unique(state,action=>action.EndsWith(".roll",StringComparison.Ordinal)
                    || action=="shot.save" || action=="shot.handling" || action=="shot.block"
                    || action=="foul.leniency" || action=="foul.injury" || action=="header.interception");
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if(state?.legalActions?.Any(action=>action?.EndsWith(".confirm",StringComparison.Ordinal)==true)==true)
                        return Unique(state,action=>action.EndsWith(".confirm",StringComparison.Ordinal));
                    return state?.hasSelectedTarget==true?Hex(state,state.selectedX,state.selectedZ):null;
                default:return null;
            }
        }
    }
}
