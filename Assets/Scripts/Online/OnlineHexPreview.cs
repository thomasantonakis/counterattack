using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
namespace CounterAttack.Online
{
    [Serializable]
    public sealed class OnlineHexPreview
    {
        public const int Limit=1900;
        public int x,z;
        [JsonProperty("r")] public string reason;
        [JsonProperty("d")] public float darkness;
        [JsonProperty("y")] public float height;
        [JsonProperty("g")] public bool committedGoal;
        private static readonly HashSet<string> Reasons=new(StringComparer.Ordinal){"passTarget","passTargetCommitted","ballPath","dangerousPass","impossiblePass",
            "ShotSaveHex","highPass","longPass","longPassDifficult","PaceAvailable","PaceRisk","DangerousTackle","MovementGoal","HeaderTargetFree","HeaderTargetRisk",
            "DefenderZOI","reposition","nutmeggableDef","CanShootFrom"};
        public static bool Shareable(string reason)=>reason!=null && Reasons.Contains(reason);
        public static bool Valid(OnlineHexPreview[] values)
        {
            if(values==null)return true;if(values.Length>Limit)return false;var seen=new HashSet<Vector2Int>();
            foreach(var p in values)
                if(p==null || p.x< -25 || p.x>24 || p.z< -19 || p.z>18 || !Shareable(p.reason)
                    || float.IsNaN(p.darkness) || float.IsInfinity(p.darkness) || p.darkness<0 || p.darkness>10
                    || float.IsNaN(p.height) || float.IsInfinity(p.height) || p.height< -1 || p.height>1 || !seen.Add(new Vector2Int(p.x,p.z)))return false;
            return true;
        }
        public static OnlineHexPreview[] Capture(HexGrid grid, HexCell selected, IEnumerable<HexCell> headerAtGoalTargets = null)
        {
            var headerTargets = new HashSet<HexCell>(headerAtGoalTargets ?? Enumerable.Empty<HexCell>());
            var result = (grid.highlightedHexes ?? new List<HexCell>()).Append(selected).Concat(headerTargets)
                .Where(hex => hex != null && hex.hexRenderer != null && grid.GetHexCellAt(hex.coordinates) == hex)
                .Distinct().OrderBy(hex => hex.coordinates.x).ThenBy(hex => hex.coordinates.z)
                .Select(hex =>
                {
                    bool headerTarget = headerTargets.Contains(hex);
                    string reason = hex.HighlightReason;
                    // Header aim publishes the raised base highlight even during local hover.
                    if (headerTarget) reason = "CanShootFrom";
                    else if (!Shareable(reason)) reason = hex.IsCommittedGoalTarget ? "passTargetCommitted" : null;
                    return reason == null ? null : new OnlineHexPreview
                    {
                        x = hex.coordinates.x,
                        z = hex.coordinates.z,
                        reason = reason,
                        darkness = headerTarget ? 1f : hex.HighlightDarkness,
                        height = hex.transform.position.y,
                        committedGoal = hex.IsCommittedGoalTarget
                    };
                })
                .Where(preview => preview != null).ToArray();
            if (!Valid(result)) throw new InvalidOperationException("Online hex preview exceeds its bounded contract");
            return result;
        }
    }
}
