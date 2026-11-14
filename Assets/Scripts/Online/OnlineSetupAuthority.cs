using System;
using System.Collections.Generic;
using Newtonsoft.Json;
namespace CounterAttack.Online
{
    public enum OnlineSetupIntent { Team, Settings, Ready, Unready, Start }
    [Serializable] public sealed class OnlineSetupTeam { public string name,kit,goalkeeperKit; }
    [Serializable] public sealed class OnlineSetupRequest
    {
        public string matchId;public long sequence,revision;public OnlineSetupIntent intent;
        public OnlineSetupTeam team;public GameSettings settings;
    }
    [Serializable] public sealed class OnlineSetupState
    {
        public string matchId,phase;public long revision,configurationRevision;public bool connected,homeReady,awayReady;
        public long homeSequence,awaySequence;public GameSettings settings;
    }
    // Separate setup lane: reuses authenticated identity and connection lifetime,
    // without changing a gameplay decision or trusting client team/account claims.
    public sealed class OnlineSetupAuthority
    {
        private readonly OnlineMatchProtocol protocol;
        private GameSettings settings;
        private long revision,configurationRevision,connectionRevision;
        private bool homeReady,awayReady,started;
        private readonly Dictionary<OnlineTeam,long> sequence=new(){{OnlineTeam.Home,0},{OnlineTeam.Away,0}};
        private readonly Dictionary<(OnlineTeam,long),(string request,long revision)> receipts=new();
        private readonly Queue<(OnlineTeam,long)> receiptOrder=new();
        public OnlineSetupAuthority(OnlineMatchProtocol protocol,GameSettings initial)
        {
            this.protocol=protocol??throw new ArgumentNullException(nameof(protocol));
            settings=OnlineSetupRules.Normalize(initial);var error=OnlineSetupRules.Validate(settings);
            if(!string.IsNullOrEmpty(error))throw new ArgumentException(error,nameof(initial));
            connectionRevision=protocol.ConnectionRevision;
        }
        private void ObserveConnections()
        {
            if(connectionRevision==protocol.ConnectionRevision)return;
            connectionRevision=protocol.ConnectionRevision;
            if(started)return;
            homeReady=awayReady=false;revision=checked(revision+1);
        }
        public OnlineSetupState Capture()
        {
            ObserveConnections();return new OnlineSetupState{matchId=protocol.MatchId,revision=revision,configurationRevision=configurationRevision,
                phase=started?"draft-ready":"setup",connected=protocol.BothConnected,homeReady=homeReady,awayReady=awayReady,
                homeSequence=sequence[OnlineTeam.Home],awaySequence=sequence[OnlineTeam.Away],settings=OnlineSetupRules.Copy(settings)};
        }
        public OnlineCommandReply Submit(string connection,OnlineSetupRequest request)
        {
            ObserveConnections();OnlineCommandReply Reply(OnlineCommandStatus status)=>new(status,revision);
            if(request==null || request.sequence<=0 || request.revision<0 || !Enum.IsDefined(typeof(OnlineSetupIntent),request.intent))return Reply(OnlineCommandStatus.InvalidCommand);
            if(request.matchId!=protocol.MatchId)return Reply(OnlineCommandStatus.WrongMatch);
            if(!protocol.TryGetTeam(connection,out var actor))return Reply(OnlineCommandStatus.UnknownConnection);
            string body=JsonConvert.SerializeObject(request);
            var key=(actor,request.sequence);
            if(receipts.TryGetValue(key,out var prior))return prior.request==body?new OnlineCommandReply(OnlineCommandStatus.Duplicate,prior.revision):Reply(OnlineCommandStatus.SequenceConflict);
            if(request.sequence<=sequence[actor])return Reply(OnlineCommandStatus.StaleSequence);
            if(!protocol.BothConnected)return Reply(OnlineCommandStatus.Disconnected);
            if(request.revision!=revision)return Reply(OnlineCommandStatus.StaleRevision);
            if(started)return Reply(OnlineCommandStatus.NotReady);
            if((request.intent==OnlineSetupIntent.Team)!=(request.team!=null) || (request.intent==OnlineSetupIntent.Settings)!=(request.settings!=null))return Reply(OnlineCommandStatus.InvalidCommand);
            if(request.intent is OnlineSetupIntent.Settings or OnlineSetupIntent.Start && actor!=protocol.HostTeam)return Reply(OnlineCommandStatus.WrongTeam);
            GameSettings next=null;
            if(request.intent==OnlineSetupIntent.Team)
            {
                next=OnlineSetupRules.Copy(settings);
                if(actor==OnlineTeam.Home){next.homeTeamName=request.team.name;next.homeKit=request.team.kit;next.homeGKKit=request.team.goalkeeperKit;next.homeTeamAbbreviation=null;}
                else{next.awayTeamName=request.team.name;next.awayKit=request.team.kit;next.awayGKKit=request.team.goalkeeperKit;next.awayTeamAbbreviation=null;}
            }
            if(request.intent==OnlineSetupIntent.Settings)
            {
                next=OnlineSetupRules.Copy(request.settings);
                next.homeTeamName=settings.homeTeamName;next.awayTeamName=settings.awayTeamName;
                next.homeKit=settings.homeKit;next.awayKit=settings.awayKit;next.homeGKKit=settings.homeGKKit;next.awayGKKit=settings.awayGKKit;
                next.homeTeamAbbreviation=settings.homeTeamAbbreviation;next.awayTeamAbbreviation=settings.awayTeamAbbreviation;
            }
            if(next!=null)
            {
                next=OnlineSetupRules.Normalize(next);if(!string.IsNullOrEmpty(OnlineSetupRules.Validate(next)))return Reply(OnlineCommandStatus.IllegalAction);
                settings=next;homeReady=awayReady=false;configurationRevision=checked(configurationRevision+1);
            }
            else switch(request.intent)
            {
                case OnlineSetupIntent.Ready:if(actor==OnlineTeam.Home)homeReady=true;else awayReady=true;break;
                case OnlineSetupIntent.Unready:if(actor==OnlineTeam.Home)homeReady=false;else awayReady=false;break;
                case OnlineSetupIntent.Start:if(!homeReady || !awayReady)return Reply(OnlineCommandStatus.NotReady);started=true;break;
            }
            revision=checked(revision+1);sequence[actor]=request.sequence;receipts.Add(key,(body,revision));receiptOrder.Enqueue(key);
            while(receiptOrder.Count>128)receipts.Remove(receiptOrder.Dequeue());return Reply(OnlineCommandStatus.Accepted);
        }
    }
}
