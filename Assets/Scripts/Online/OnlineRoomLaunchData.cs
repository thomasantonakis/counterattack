using System;
using System.Globalization;
using Newtonsoft.Json;

namespace CounterAttack.Online
{
    // Builds the real MatchManager data graph from the authenticated draft.
    // Never discovers a save, generates new dice, or reads the local active slot.
    public static class OnlineRoomLaunchData
    {
        public static MatchManager.GameData Create(OnlineDraftState ready)
        {
            if(ready==null || !ready.connected || !ready.homeReady || !ready.awayReady || ready.phase!="room-ready"
                || !Guid.TryParseExact(ready.roomSessionId,"N",out var sessionId) || sessionId==Guid.Empty
                || !DateTime.TryParseExact(ready.roomStartedUtc,"O",CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var started) || started.Kind!=DateTimeKind.Utc)
                throw new ArgumentException("A completed connected online opening is required",nameof(ready));
            var replica=new OnlineDraftClient(ready.matchId,ready.hostTeam);
            if(!replica.Apply(ready) || ready.settings.referee=="Random")throw new ArgumentException("The host must finalize a valid roster and referee",nameof(ready));
            var data=new MatchManager.GameData
            {
                gameSettings=JsonConvert.DeserializeObject<MatchManager.GameSettings>(JsonConvert.SerializeObject(ready.settings)),
                openingSetup=OnlineDraftAuthority.Copy(ready.openingSetup),
                createdUtc=ready.roomStartedUtc,playSessionId=ready.roomSessionId,playSessionStartedUtc=ready.roomStartedUtc,
                inheritedEventSequence=0,sourceSavePath=null,sourceSaveSavedAtUtc=null
            };
            foreach(var pair in new[]{(ready.home,data.rosters.home),(ready.away,data.rosters.away)})
                for(int i=0;i<pair.Item1.Length;i++)
                {
                    var c=pair.Item1[i]??throw new ArgumentException("Every roster slot must be filled",nameof(ready));
                    pair.Item2.Add((i+1).ToString(CultureInfo.InvariantCulture),new MatchManager.RosterPlayer
                    {name=c.name,pace=c.pace,dribbling=c.dribbling,heading=c.heading,highPass=c.highPass,resilience=c.resilience,shooting=c.shooting,tackling=c.tackling,aerial=c.aerial,saving=c.saving,handling=c.handling});
                }
            TeamNameAbbreviator.EnsureAbbreviations(data.gameSettings);return data;
        }
        public static MatchManager.GameData Copy(MatchManager.GameData source)
        {
            if(source==null)return null;
            var clone=JsonConvert.DeserializeObject<MatchManager.GameData>(JsonConvert.SerializeObject(source));clone.gameLog?.RebindStats(clone.stats);return clone;
        }
    }
}
