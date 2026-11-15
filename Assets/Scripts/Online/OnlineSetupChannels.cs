using System;
using System.Collections.Generic;
using System.IO;
namespace CounterAttack.Online
{
    // Identity here is supplied by the authenticated transport, never by packet data.
    public abstract class OnlineSessionChannel
    {
        public event Action<byte[]> Outgoing;
        public string Fault {get;protected set;}
        public string Nonce {get;protected set;}
        public bool PeerLeft {get;private set;}
        public abstract OnlineSetupClient Client {get;}
        public OnlineRoundTripStatistics Latency {get;}
        protected readonly string matchId,remoteAccount;
        protected readonly OnlineCompatibility compatibility;
        protected readonly Func<double> clock;
        protected OnlineSessionWire wire=new();
        private long ping;
        private double nextHeartbeat;
        public double LastReceived {get;private set;}
        protected OnlineSessionChannel(string matchId,string remoteAccount,OnlineCompatibility compatibility,Func<double> monotonicSeconds)
        {
            OnlineProtocolIds.Require(matchId,nameof(matchId));OnlineProtocolIds.Require(remoteAccount,nameof(remoteAccount));
            this.matchId=matchId;this.remoteAccount=remoteAccount;this.compatibility=compatibility??throw new ArgumentNullException(nameof(compatibility));
            clock=monotonicSeconds??throw new ArgumentNullException(nameof(monotonicSeconds));Latency=new(clock);LastReceived=clock();
        }
        protected void Send(OnlineSessionMessage message)
        {
            message.matchId=matchId;message.nonce=Nonce;
            foreach(var packet in OnlineSessionWire.Encode(message))Outgoing?.Invoke(packet);
        }
        protected OnlineSessionMessage Hello(OnlineSetupState state=null)=>new(){kind="hello",protocol=compatibility.Protocol,rules=compatibility.Rules,catalog=compatibility.Catalog,state=state};
        protected bool Compatible(OnlineSessionMessage message)=>message.protocol==compatibility.Protocol && message.rules==compatibility.Rules && message.catalog==compatibility.Catalog;
        protected static bool ValidNonce(string nonce)=>Guid.TryParseExact(nonce,"N",out var guid) && guid!=Guid.Empty;
        public bool Receive(string authenticatedAccount,byte[] packet,int length)
        {
            if(authenticatedAccount!=remoteAccount)return false;
            OnlineSessionMessage message;
            try{message=wire.Receive(packet,length,true);}catch(InvalidDataException){return false;}
            if(message==null)return true;
            if(message.matchId!=matchId)return false;
            bool accepted;
            if(message.kind=="hello")accepted=ReceiveHello(message);
            else
            {
                if(message.nonce!=Nonce || !ValidNonce(Nonce))return false;
                if(message.kind=="leave"){PeerLeft=true;TransportLost();accepted=true;}
                else if(message.kind=="heartbeat")
                {
                    accepted=message.sequence>0 && (message.status=="ping" || message.status=="pong");
                    if(accepted && message.status=="ping")Send(new OnlineSessionMessage{kind="heartbeat",status="pong",sequence=message.sequence});
                    else if(accepted)Latency.Replied(message.sequence);
                }
                else accepted=Dispatch(message);
            }
            if(accepted)LastReceived=clock();return accepted;
        }
        public void Tick()
        {
            if(!Ready || clock()<nextHeartbeat)return;
            nextHeartbeat=clock()+1;long sequence=checked(++ping);Latency.Sent(sequence);
            Send(new OnlineSessionMessage{kind="heartbeat",status="ping",sequence=sequence});
        }
        public void Leave(){if(ValidNonce(Nonce))Send(new OnlineSessionMessage{kind="leave"});TransportLost();}
        public void TransportLost(){Client.SetTransportAvailable(false);Lost();}
        protected virtual void Lost(){}
        protected void RestartLiveness(){PeerLeft=false;LastReceived=clock();Client.SetTransportAvailable(true);}
        public abstract bool Ready {get;}
        protected abstract bool ReceiveHello(OnlineSessionMessage message);
        protected abstract bool Dispatch(OnlineSessionMessage message);
    }
    public sealed class OnlineSetupHostChannel:OnlineSessionChannel
    {
        public override OnlineSetupClient Client {get;}
        public OnlineSetupAuthority Authority {get;}
        private readonly OnlineMatchProtocol protocol;
        private readonly string localConnection;
        private string remoteConnection;
        public override bool Ready=>protocol.BothConnected && Fault==null;
        public OnlineSetupHostChannel(OnlineMatchProtocol protocol,string localConnection,string peerAccount,GameSettings settings,Func<double> clock)
            :base(protocol.MatchId,peerAccount,protocol.Compatibility,clock)
        {
            this.protocol=protocol;this.localConnection=localConnection;
            if(!protocol.TryGetTeam(localConnection,out var team) || team!=protocol.HostTeam)throw new ArgumentException("Host connection is not authenticated");
            Authority=new(protocol,settings);Client=new(protocol.MatchId,protocol.HostTeam);Client.Apply(Authority.Capture());
            Client.Requested+=Local;
        }
        public void Begin()
        {
            if(remoteConnection!=null)protocol.Disconnect(remoteConnection);
            remoteConnection=null;Nonce=Guid.NewGuid().ToString("N");wire=new();Fault=null;RestartLiveness();
            Client.Apply(Authority.Capture());Send(Hello(Authority.Capture()));
        }
        public void ResendHello()
        {if(!Ready && ValidNonce(Nonce) && Fault==null && !PeerLeft)Send(Hello(Authority.Capture()));}
        protected override bool ReceiveHello(OnlineSessionMessage message)
        {
            if(message.nonce!=Nonce || !ValidNonce(Nonce))return false;
            if(!Compatible(message)){Fault="Both games must use the same version and player catalog.";return false;}
            string connection="frontend-peer-"+Nonce;
            if(!protocol.BindAuthenticatedAccount(remoteAccount,connection,compatibility))return false;
            remoteConnection=connection;var state=Authority.Capture();Send(new OnlineSessionMessage{kind="setup-state",state=state});Client.Apply(state);return true;
        }
        protected override void Lost()
        {
            if(remoteConnection!=null)protocol.Disconnect(remoteConnection);
            Client.Apply(Authority.Capture());
        }
        private void Local(OnlineSetupRequest request)
        {
            var reply=Authority.Submit(localConnection,request);var state=Authority.Capture();
            Client.Receive(request.sequence,reply.Status,state);
            if(Ready)Send(new OnlineSessionMessage{kind="setup-state",state=Authority.Capture()});
        }
        protected override bool Dispatch(OnlineSessionMessage message)
        {
            if(!Ready || message.kind!="setup-request" || message.request==null || message.request.matchId!=matchId)return false;
            var reply=Authority.Submit(remoteConnection,message.request);var state=Authority.Capture();
            // Queue acknowledgement before a local view can react with another edit.
            Send(new OnlineSessionMessage{kind="setup-reply",sequence=message.request.sequence,status=reply.Status.ToString(),state=state});
            Client.Apply(state);return true;
        }
    }
    public sealed class OnlineSetupGuestChannel:OnlineSessionChannel
    {
        public override OnlineSetupClient Client {get;}
        public override bool Ready=>Client.Connected && Fault==null;
        private readonly OnlineTeam expectedHost;
        private readonly HashSet<string> retired=new(StringComparer.Ordinal);
        private readonly Queue<string> order=new();
        public OnlineSetupGuestChannel(string matchId,string hostAccount,OnlineCompatibility compatibility,OnlineTeam expectedHost,Func<double> clock)
            :base(matchId,hostAccount,compatibility,clock)
        {
            this.expectedHost=expectedHost;Client=new(matchId,expectedHost==OnlineTeam.Home?OnlineTeam.Away:OnlineTeam.Home);
            Client.Requested+=r=>Send(new OnlineSessionMessage{kind="setup-request",request=r});
        }
        protected override bool ReceiveHello(OnlineSessionMessage message)
        {
            if(!ValidNonce(message.nonce) || retired.Contains(message.nonce) || message.state==null || message.state.hostTeam!=expectedHost)return false;
            if(!Compatible(message)){Fault="Both games must use the same version and player catalog.";return false;}
            if(!Client.Apply(message.state))return false;
            if(Nonce!=null && Nonce!=message.nonce)
            {retired.Add(Nonce);order.Enqueue(Nonce);while(order.Count>64)retired.Remove(order.Dequeue());wire=new();}
            Nonce=message.nonce;Fault=null;RestartLiveness();Send(Hello());return true;
        }
        protected override bool Dispatch(OnlineSessionMessage message)
        {
            if(message.kind=="setup-state")return Client.Apply(message.state);
            if(message.kind=="setup-reply" && Enum.TryParse<OnlineCommandStatus>(message.status,out var status))return Client.Receive(message.sequence,status,message.state);
            return false;
        }
    }
}
