using System;
using System.Collections.Generic;
using System.Linq;
namespace CounterAttack.Online
{
    internal static class OnlineRosterPublication
    {
        private static bool Finite(float value)=>!float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value)<=1000;
        private static bool FiniteStatistic(float value)=>!float.IsNaN(value) && !float.IsInfinity(value);
        internal static bool Valid(OnlineGroundPassState state)
        {
            if(state.catalogId!=OnlineFullRoomBootstrap.Catalog)return true;
            if(state.tokens.Length is not (32 or 36) || state.homeStats==null || state.awayStats==null
                || !FiniteStatistic(state.regulationSeconds) || state.regulationSeconds<0
                || state.ballOnBoard && (state.ballX < -25 || state.ballX>24 || state.ballZ < -19 || state.ballZ>18)
                || state.homeDirection is not ("LeftToRight" or "RightToLeft")
                || state.awayDirection is not ("LeftToRight" or "RightToLeft") || state.homeDirection==state.awayDirection)return false;
            var pose=state.roomPose;
            if((state.ballMoving || state.playerMoving)!=(pose!=null))return false;
            if(pose!=null && (pose.matchId!=state.matchId || !Finite(pose.ballX) || !Finite(pose.ballY) || !Finite(pose.ballZ)
                || pose.players==null || pose.players.Length!=state.tokens.Length
                || pose.players.Any(p=>p==null || !Finite(p.x) || !Finite(p.y) || !Finite(p.z))
                || !pose.players.Select(p=>p.id).OrderBy(id=>id,StringComparer.Ordinal).SequenceEqual(state.tokens.Select(t=>t.id).OrderBy(id=>id,StringComparer.Ordinal))))return false;
            var occupied=new HashSet<(int,int)>();
            foreach(var token in state.tokens)
            {
                var player=token.runtime;var stats=token.playerStats;
                if(player==null || stats==null || player.teamSide is not ("Home" or "Away") || player.jerseyNumber<1 || player.jerseyNumber>state.tokens.Length/2
                    || token.id!=(player.teamSide=="Home"?"home:":"away:")+player.jerseyNumber
                    || player.tokenKey!=player.teamSide+":"+player.jerseyNumber || player.playerName==null || player.playerName.Length>256
                    || !Finite(token.inactiveX) || !Finite(token.inactiveY) || !Finite(token.inactiveZ)
                    || !FiniteStatistic(stats.xRecoveries) || !FiniteStatistic(stats.xDribbles) || !FiniteStatistic(stats.xTackles) || !FiniteStatistic(stats.xGoals)
                    || token.onBoard!=(player.currentHex!=null) || player.isSentOff && (token.onBoard || token.visible || player.isPlaying)
                    || player.wasSubbedOff && player.isPlaying)return false;
                if(new[]{player.pace,player.dribbling,player.highPass,player.resilience,player.heading,player.shooting,player.tackling,player.aerial,player.saving,player.handling}.Any(value=>value < -1 || value>128))return false;
                if(token.onBoard && (!player.isPlaying || player.currentHex.x!=token.x || player.currentHex.z!=token.z
                    || token.x < -25 || token.x>24 || token.z < -19 || token.z>18 || !occupied.Add((token.x,token.z))))return false;
            }
            return true;
        }
    }
}
