using System;
using Newtonsoft.Json;
namespace CounterAttack.Online
{
    public sealed class OnlineSetupClient
    {
        public event Action<OnlineSetupRequest> Requested;
        public event Action Changed;
        public event Action<GameSettings> DraftAgreed;
        public OnlineTeam Team {get;}
        public bool IsHost=>state!=null && Team==state.hostTeam;
        public bool Pending=>pending!=null;
        public bool Dirty=>draft!=null;
        public bool LocalReady=>state!=null && (Team==OnlineTeam.Home?state.homeReady:state.awayReady);
        public bool BothReady=>state?.homeReady==true && state.awayReady;
        public bool Connected=>state?.connected==true && transportAvailable;
        public string Phase=>state?.phase;
        public bool CanReturnToSetup=>Phase=="draft-ready" && Connected && !Pending;
        public OnlineCommandStatus? LastReply {get;private set;}
        public bool CanConfirm=>state?.phase=="setup" && Connected && !Pending && (!LocalReady || Dirty || IsHost && BothReady);
        private readonly string matchId;
        private OnlineSetupState state;
        private GameSettings draft;
        private OnlineSetupRequest pending;
        private long nextSequence,editVersion,pendingEditVersion,rejectedEditVersion=-1;
        private bool agreed;
        private bool transportAvailable=true;
        public void SetTransportAvailable(bool available)
        {
            if(transportAvailable==available)return;transportAvailable=available;Changed?.Invoke();
            if(available)FlushUnready();
        }
        public OnlineSetupClient(string matchId,OnlineTeam team)
        {OnlineProtocolIds.Require(matchId,nameof(matchId));if(!Enum.IsDefined(typeof(OnlineTeam),team))throw new ArgumentOutOfRangeException(nameof(team));this.matchId=matchId;Team=team;}
        public OnlineSetupState Capture()=>state==null?null:JsonConvert.DeserializeObject<OnlineSetupState>(JsonConvert.SerializeObject(state));
        public GameSettings ViewSettings()=>state==null?null:draft==null?OnlineSetupRules.Copy(state.settings):OnlineSetupRules.MergeOwnedSelection(state.settings,draft,Team,IsHost);
        private bool CanApply(OnlineSetupState snapshot)
        {
            if(snapshot==null || snapshot.matchId!=matchId || snapshot.revision<0 || snapshot.configurationRevision<0
                || snapshot.configurationRevision>snapshot.revision || snapshot.connectionRevision<0 || snapshot.homeSequence<0 || snapshot.awaySequence<0
                || !Enum.IsDefined(typeof(OnlineTeam),snapshot.hostTeam) || snapshot.phase!="setup" && snapshot.phase!="draft-ready"
                || !string.IsNullOrEmpty(OnlineSetupRules.Validate(snapshot.settings,allowUnnamedTeams:snapshot.phase=="setup")))return false;
            return state==null || snapshot.revision>state.revision || snapshot.revision==state.revision && JsonConvert.SerializeObject(snapshot)==JsonConvert.SerializeObject(state);
        }
        public bool Apply(OnlineSetupState snapshot)
        {
            if(!CanApply(snapshot))return false;
            long confirmedSequence = Team == OnlineTeam.Home ? snapshot.homeSequence : snapshot.awaySequence;
            if (state != null && snapshot.connectionRevision != state.connectionRevision)
            {
                // The reconnected snapshot can carry the acknowledgement whose reply was lost.
                if (pending?.sequence == confirmedSequence && pending.intent is OnlineSetupIntent.Confirm or OnlineSetupIntent.Edit
                    && pendingEditVersion == editVersion)
                    draft = null;
                pending = null;
            }
            if(Phase=="draft-ready" && snapshot.phase=="setup")
            {agreed=false;pending=null;draft=null;}
            if(state!=null && state.settings.matchType!=snapshot.settings.matchType && draft?.matchType!=snapshot.settings.matchType)
            {
                draft=null;
                pending=null;
            }
            state=JsonConvert.DeserializeObject<OnlineSetupState>(JsonConvert.SerializeObject(snapshot));
            nextSequence=Math.Max(nextSequence,confirmedSequence);
            Changed?.Invoke();
            if(!agreed && state.phase=="draft-ready" && state.connected)
            {agreed=true;DraftAgreed?.Invoke(OnlineSetupRules.Copy(state.settings));}
            FlushUnready();return true;
        }
        public void Edit(GameSettings proposed)
        {
            if(state?.phase!="setup" || proposed==null)return;
            draft=OnlineSetupRules.Copy(proposed);editVersion=checked(editVersion+1);Changed?.Invoke();FlushUnready();
        }
        private void FlushUnready()
        {
            if(state?.phase!="setup" || !Connected || Pending || !Dirty || rejectedEditVersion==editVersion)return;
            var candidate=OnlineSetupRules.Normalize(OnlineSetupRules.MergeOwnedSelection(state.settings,draft,Team,IsHost));
            if(string.IsNullOrEmpty(OnlineSetupRules.Validate(candidate,allowUnnamedTeams:true)))Send(OnlineSetupIntent.Edit);
            else if(LocalReady)Send(OnlineSetupIntent.Unready);
        }
        public bool Confirm(GameSettings proposed)
        {
            if(!CanConfirm || proposed==null || Requested==null)return false;
            var candidate=OnlineSetupRules.Normalize(OnlineSetupRules.MergeOwnedSelection(state.settings,proposed,Team,IsHost));
            if(!string.IsNullOrEmpty(OnlineSetupRules.Validate(candidate)))return false;
            if(IsHost && BothReady && !Dirty && JsonConvert.SerializeObject(candidate)==JsonConvert.SerializeObject(state.settings))
            {Send(OnlineSetupIntent.Start);return true;}
            draft=OnlineSetupRules.Copy(proposed);editVersion=checked(editVersion+1);Send(OnlineSetupIntent.Confirm);return true;
        }
        public bool ReturnToSetup()
        {
            if(!CanReturnToSetup || Requested==null)return false;
            Send(OnlineSetupIntent.BackToSetup);
            return true;
        }
        private void Send(OnlineSetupIntent intent)
        {
            if(Requested==null)return;
            var request=new OnlineSetupRequest{matchId=matchId,sequence=checked(++nextSequence),revision=state.revision,intent=intent};
            if(intent is OnlineSetupIntent.Confirm or OnlineSetupIntent.Edit)
            {
                request.team=new OnlineSetupTeam{name=Team==OnlineTeam.Home?draft.homeTeamName:draft.awayTeamName,
                    kit=Team==OnlineTeam.Home?draft.homeKit:draft.awayKit,goalkeeperKit=Team==OnlineTeam.Home?draft.homeGKKit:draft.awayGKKit};
                if(IsHost)request.settings=OnlineSetupRules.Copy(draft);
            }
            pending=request;pendingEditVersion=editVersion;Changed?.Invoke();
            Requested(JsonConvert.DeserializeObject<OnlineSetupRequest>(JsonConvert.SerializeObject(request)));
        }
        public bool Receive(long sequence,OnlineCommandStatus reply,OnlineSetupState snapshot)
        {
            if(!CanApply(snapshot))return false;
            if(pending?.sequence==sequence)
            {
                bool committed=(pending.intent is OnlineSetupIntent.Confirm or OnlineSetupIntent.Edit)
                    && (reply is OnlineCommandStatus.Accepted or OnlineCommandStatus.Duplicate);
                if(committed && pendingEditVersion==editVersion)draft=null;
                if(pending.intent==OnlineSetupIntent.Edit && !committed && reply!=OnlineCommandStatus.StaleRevision)
                    rejectedEditVersion=pendingEditVersion;
                pending=null;LastReply=reply;
            }
            return Apply(snapshot);
        }
    }
}
