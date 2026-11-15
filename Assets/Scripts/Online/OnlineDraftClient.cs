using System;
using System.Linq;
using Newtonsoft.Json;

namespace CounterAttack.Online
{
    public sealed class OnlineDraftClient
    {
        public event Action Changed;
        public event Action<OnlineDraftRequest> Requested;
        public OnlineTeam Team {get;}
        public string Phase=>state?.phase;
        public bool Connected=>available && state?.connected==true;
        public bool Pending {get;private set;}
        public bool IsHost=>state?.hostTeam==Team;
        public OnlineCommandStatus? LastReply {get;private set;}
        private readonly string matchId;
        private OnlineDraftState state;
        private long sequence,pendingSequence;
        private bool available=true;
        public OnlineDraftClient(string matchId,OnlineTeam team)
        {OnlineProtocolIds.Require(matchId,nameof(matchId));if(!Enum.IsDefined(typeof(OnlineTeam),team))throw new ArgumentOutOfRangeException(nameof(team));this.matchId=matchId;Team=team;}
        public OnlineDraftState Capture()=>OnlineDraftAuthority.Copy(state);
        public void SetTransportAvailable(bool value){if(available==value)return;available=value;Changed?.Invoke();}
        private bool Valid(OnlineDraftState s)
        {
            if(s==null || s.matchId!=matchId || s.revision<0 || s.connectionRevision<0 || s.homeSequence<0 || s.awaySequence<0
                || !Enum.IsDefined(typeof(OnlineTeam),s.hostTeam) || !Enum.IsDefined(typeof(OnlineTeam),s.turn)
                || !new[]{"roll","pick","review","opening-roll","opening-choice","room-ready"}.Contains(s.phase) || !string.IsNullOrEmpty(OnlineSetupRules.Validate(s.settings)))return false;
            if(s.phase=="room-ready" && s.openingSetup==null)return false;
            if(!int.TryParse(s.settings.squadSize,out int size) || s.home?.Length!=size || s.away?.Length!=size || s.cards==null)return false;
            var all=s.home.Concat(s.away).Concat(s.cards).Where(c=>c!=null).ToArray();
            if(all.Any(c=>!OnlineProtocolIds.IsValid(c.id) || string.IsNullOrWhiteSpace(c.name)))return false;
            if(all.GroupBy(c=>c.id).Any(g=>g.Count()>1 && (g.First().goalkeeper?s.settings.gkDraft!="Arcade":s.settings.draft!="Arcade")))return false;
            if(s.available==null || s.available.Any(id=>!OnlineProtocolIds.IsValid(id)) || s.available.Distinct().Count()!=s.available.Length)return false;
            foreach(var roster in new[]{s.home,s.away})for(int i=0;i<roster.Length;i++)if(roster[i]!=null && roster[i].goalkeeper!=(i is 0 or 11))return false;
            if(s.homeRoll is <0 or >6 || s.awayRoll is <0 or >6 || s.rollRound<1 || s.batch<0 || s.cards.Length>4)return false;
            return state==null || s.revision>state.revision || s.revision==state.revision && JsonConvert.SerializeObject(s)==JsonConvert.SerializeObject(state);
        }
        public bool Apply(OnlineDraftState snapshot)
        {
            if(!Valid(snapshot))return false;
            if(state!=null && state.connectionRevision!=snapshot.connectionRevision)Pending=false;
            state=OnlineDraftAuthority.Copy(snapshot);sequence=Math.Max(sequence,Team==OnlineTeam.Home?state.homeSequence:state.awaySequence);Changed?.Invoke();return true;
        }
        public bool Request(OnlineDraftIntent intent,string cardId=null,int slot=0,int otherSlot=0,CoinTossChoice choice=CoinTossChoice.Kickoff)
        {
            if(!Connected || Pending || state.phase=="room-ready" || Requested==null || !Enum.IsDefined(typeof(OnlineDraftIntent),intent))return false;
            var request=new OnlineDraftRequest{matchId=matchId,revision=state.revision,sequence=checked(++sequence),intent=intent,cardId=cardId,slot=slot,otherSlot=otherSlot,choice=choice};
            pendingSequence=request.sequence;Pending=true;Changed?.Invoke();Requested(request);return true;
        }
        public bool Receive(long replySequence,OnlineCommandStatus status,OnlineDraftState snapshot)
        {
            if(!Valid(snapshot))return false;
            if(Pending && pendingSequence==replySequence){Pending=false;LastReply=status;}return Apply(snapshot);
        }
    }
}
