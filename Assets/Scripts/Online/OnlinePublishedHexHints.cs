using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
namespace CounterAttack.Online
{
    /// <summary>Presentation of admitted choices; never calculates gameplay legality.</summary>
    public static class OnlinePublishedHexHints
    {
        public static IReadOnlyDictionary<Vector2Int,string> Build(OnlineGroundPassState state)
        {
            var hints=new Dictionary<Vector2Int,string>();
            if(state==null || state.connectionHeld || state.isSharedPaused)return hints;
            var coordinates=new Dictionary<Vector2Int,string>();var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var action in state.legalActions??Array.Empty<string>())
            {
                if(action==null || !seen.Add(action))continue;var parts=action.Split(':');if(parts.Length<3)continue;
                if(!int.TryParse(parts[^2],NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out int x)
                    || !int.TryParse(parts[^1],NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out int z))continue;
                string suffix=":"+x.ToString(CultureInfo.InvariantCulture)+":"+z.ToString(CultureInfo.InvariantCulture);
                if(!action.EndsWith(suffix,StringComparison.Ordinal))continue;
                var key=new Vector2Int(x,z);coordinates[key]=coordinates.ContainsKey(key)?null:action;
            }
            foreach(var coordinate in coordinates)if(coordinate.Value!=null)hints[coordinate.Key]="ReachOverlayAttacker";
            foreach(var token in state.tokens??Array.Empty<OnlineGroundPassTokenState>())
            {
                if(token==null || !token.onBoard || !token.visible)continue;
                if(OnlineBoardActionPicker.Actor(state,token.id)!=null)hints[new Vector2Int(token.x,token.z)]="PaceAvailable";
                if(token.id==state.selectedPlayer)hints[new Vector2Int(token.x,token.z)]="MovementDestinationHover";
            }
            if(state.hasSelectedTarget)hints[new Vector2Int(state.selectedX,state.selectedZ)]="passTargetCommitted";
            return hints;
        }
    }
}
