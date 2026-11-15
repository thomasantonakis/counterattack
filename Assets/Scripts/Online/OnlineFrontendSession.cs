using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using UnityEngine;
namespace CounterAttack.Online
{
    // Nonvisual authored prefab instance owns transport across scene transitions.
    public sealed class OnlineFrontendSession:MonoBehaviour
    {
        public static OnlineFrontendSession Current {get;private set;}
        public event Action<OnlineSetupClient> ClientAvailable;
        public event Action<GameSettings> DraftAgreed;
        public OnlineSetupClient Client=>channel?.Client;
        public string LobbyCode {get;private set;}
        public string Status {get;private set;}="Not connected";
        public string RunDirectory {get;private set;}
        public string Error {get;private set;}
        public bool Busy {get;private set;}
        public bool Hosting {get;private set;}
        public GameSettings AgreedSettings=>OnlineSetupRules.Copy(agreedSettings);
        public OnlineLatencySummary Latency=>channel?.Latency.Capture()??new OnlineLatencySummary();
        public int QueuedPackets=>outgoing.Count;
        private OnlineSessionChannel channel;
        private readonly Queue<byte[]> outgoing=new();
        private readonly byte[] receiveBuffer=new byte[1170];
        private IEnumerator probe;
        private Coroutine running;
        private EOSStandaloneSession native;
        private ProductUserId peer;
        private SocketId socket;
        private bool stop,disposed,relayOnly;
        private GameSettings initial,agreedSettings;
        private string startedUtc;
        private bool ownsBackground,previousBackground;
        private ulong bytesSent,bytesReceived;
        private void Awake()
        {
            if(Current!=null && Current!=this){Destroy(gameObject);return;}
            Current=this;DontDestroyOnLoad(gameObject);
        }
        public void Host(GameSettings settings,bool forceRelay=false)
        {
            if(Busy || running!=null)throw new InvalidOperationException("An online session is already running");
            initial=OnlineSetupRules.Normalize(settings);var invalid=OnlineSetupRules.Validate(initial);
            if(!string.IsNullOrEmpty(invalid))throw new ArgumentException(invalid,nameof(settings));
            Begin(true,null,forceRelay);
        }
        public void Join(string lobbyCode,bool forceRelay=false)
        {
            if(!OnlineFrontendSessionPolicy.ValidLobbyCode(lobbyCode))throw new ArgumentException("Enter the 32-character lobby code",nameof(lobbyCode));
            if(Busy || running!=null)throw new InvalidOperationException("An online session is already running");
            Begin(false,lobbyCode,forceRelay);
        }
        private void Begin(bool hosting,string code,bool forceRelay)
        {
            previousBackground=Application.runInBackground;ownsBackground=true;Application.runInBackground=true;
            Hosting=hosting;LobbyCode=code;relayOnly=forceRelay;stop=false;Busy=true;Error=null;
            startedUtc=DateTime.UtcNow.ToString("O");
            RunDirectory=Path.Combine(Application.persistentDataPath,"OnlineSessions",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(RunDirectory);
            Status=hosting?"Creating a private online session...":"Joining the online session...";
            var route=OnlineFrontendSessionPolicy.Route(forceRelay);
            probe=hosting?EOSStandalonePacketProbe.HostProbe(RunDirectory,Exchange,route):EOSStandalonePacketProbe.GuestProbe(code,Exchange,route);
            running=StartCoroutine(Run());
        }
        private IEnumerator Run()
        {
            Exception failure=null;
            while(!disposed && probe!=null)
            {
                bool next=false;
                try
                {
                    if(stop && channel==null)break;
                    next=probe.MoveNext();
                }
                catch(Exception error){failure=error;}
                if(!next || failure!=null)break;
                yield return probe.Current;
            }
            try{(probe as IDisposable)?.Dispose();}catch(Exception error){failure??=error;}
            probe=null;native=null;channel?.TransportLost();
            try{EOSStandaloneSession.Shutdown();}catch(Exception error){failure??=error;}
            Busy=false;running=null;
            if(failure!=null && !stop){Error=failure.Message;Status="Online connection failed. Leave the session to try again.";Debug.LogWarning("Online session failed: "+failure.Message);}
            else Status="Online session closed";
            Retain(failure);
            if(stop && !disposed)Destroy(gameObject);
        }
        private IEnumerator Exchange(EOSStandaloneSession session,ProductUserId remote,SocketId socketId,string lobby)
        {
            native=session;peer=remote;socket=socketId;LobbyCode=lobby;
            var compatibility=OnlineFrontendSessionPolicy.Compatibility();string match="online-"+lobby;
            if(Hosting)
            {
                var protocol=new OnlineMatchProtocol(match,remote.ToString(),session.UserId.ToString(),session.UserId.ToString(),compatibility);
                protocol.BindAuthenticatedAccount(session.UserId.ToString(),"frontend-host",compatibility);
                channel=new OnlineSetupHostChannel(protocol,"frontend-host",remote.ToString(),initial,()=>Time.realtimeSinceStartupAsDouble);
            }
            else channel=new OnlineSetupGuestChannel(match,remote.ToString(),compatibility,OnlineTeam.Away,()=>Time.realtimeSinceStartupAsDouble);
            channel.Outgoing+=Queue;
            channel.Client.DraftAgreed+=OnAgreed;
            ClientAvailable?.Invoke(channel.Client);
            if(channel is OnlineSetupHostChannel host)host.Begin();
            Status="Checking game versions and settings...";
            while(!disposed)
            {
                session.Platform.Tick();Flush();Poll();channel.Tick();Flush();
                if(channel.Fault!=null)throw new InvalidOperationException(channel.Fault);
                if(channel.PeerLeft){Status="The other manager left the session.";break;}
                if(stop)break;
                if(Time.realtimeSinceStartupAsDouble-channel.LastReceived>OnlineFrontendSessionPolicy.PeerTimeoutSeconds)
                {channel.TransportLost();throw new TimeoutException("The other manager stopped responding.");}
                if(channel.Ready && agreedSettings==null)Status="Connected. Review your selections.";
                yield return null;
            }
            channel.TransportLost();
        }
        private void Queue(byte[] packet)
        {
            if(outgoing.Count>=OnlineFrontendSessionPolicy.MaxQueuedPackets)throw new InvalidOperationException("Online control queue exceeded its limit");
            outgoing.Enqueue(packet);
        }
        private void Flush()
        {
            var p2p=native.Platform.GetP2PInterface();
            for(int i=0;i<128 && outgoing.Count>0;i++)
            {
                var packet=outgoing.Peek();var options=new SendPacketOptions{LocalUserId=native.UserId,RemoteUserId=peer,SocketId=socket,
                    Channel=OnlineFrontendSessionPolicy.Channel,Data=new ArraySegment<byte>(packet),AllowDelayedDelivery=true,Reliability=PacketReliability.ReliableOrdered};
                var result=p2p.SendPacket(ref options);
                if(result==Result.LimitExceeded || result==Result.NoConnection || result==Result.TimedOut)return;
                if(result!=Result.Success)throw new IOException("Sending online session data failed: "+result);
                outgoing.Dequeue();bytesSent+=(ulong)packet.Length;
            }
        }
        private void Poll()
        {
            var p2p=native.Platform.GetP2PInterface();var options=new ReceivePacketOptions{LocalUserId=native.UserId,MaxDataSizeBytes=(uint)receiveBuffer.Length,RequestedChannel=OnlineFrontendSessionPolicy.Channel};
            for(int i=0;i<128;i++)
            {
                ProductUserId sender=null;var receivedSocket=new SocketId();
                var result=p2p.ReceivePacket(ref options,ref sender,ref receivedSocket,out var channelId,new ArraySegment<byte>(receiveBuffer),out var size);
                if(result==Result.NotFound)return;
                if(result!=Result.Success)throw new IOException("Receiving online session data failed: "+result);
                if(sender?.ToString()!=peer.ToString() || channelId!=OnlineFrontendSessionPolicy.Channel || receivedSocket.SocketName!=socket.SocketName)continue;
                bytesReceived+=size;channel.Receive(sender.ToString(),receiveBuffer,(int)size);
            }
        }
        private void Update()
        {
            if(!Busy || !Hosting || LobbyCode!=null || RunDirectory==null)return;
            string path=Path.Combine(RunDirectory,"lobby-id.txt");
            if(!File.Exists(path))return;
            try{var code=File.ReadAllText(path).Trim();if(OnlineFrontendSessionPolicy.ValidLobbyCode(code)){LobbyCode=code;Status="Share the lobby code with the other manager.";}}
            catch(IOException){}
        }
        private void OnAgreed(GameSettings settings)
        {
            if(agreedSettings!=null)return;agreedSettings=OnlineSetupRules.Copy(settings);Status="Both managers agreed. Preparing Draft...";
            DraftAgreed?.Invoke(OnlineSetupRules.Copy(settings));
        }
        public void Close()
        {
            if(stop)return;stop=true;Status="Closing the online session...";
            try{channel?.Leave();if(native?.Platform!=null && outgoing.Count>0)Flush();}catch(Exception error){Debug.LogWarning("Online leave: "+error.Message);}
            if(!Busy)Destroy(gameObject);
        }
        [Serializable] private sealed class Report
        {
            public string role,status,message,platform,startedUtc,completedUtc,routingPolicy;
            public ulong bytesSent,bytesReceived;public OnlineLatencySummary latency;public bool setupAgreed;
        }
        private void Retain(Exception failure)
        {
            if(RunDirectory==null)return;
            try{File.WriteAllText(Path.Combine(RunDirectory,"frontend-session-report.json"),JsonUtility.ToJson(new Report{role=Hosting?"host":"guest",status=failure==null?"closed":"failed",
                message=failure?.Message,platform=Application.platform.ToString(),startedUtc=startedUtc,completedUtc=DateTime.UtcNow.ToString("O"),
                routingPolicy=OnlineFrontendSessionPolicy.Route(relayOnly).ToString(),bytesSent=bytesSent,bytesReceived=bytesReceived,latency=Latency,setupAgreed=agreedSettings!=null},true));}
            catch(IOException error){Debug.LogWarning("Could not retain the online session report: "+error.Message);}
        }
        private void OnDestroy()
        {
            if(disposed)return;disposed=true;
            bool ownedProbe=probe!=null;
            try{(probe as IDisposable)?.Dispose();if(ownedProbe)EOSStandaloneSession.Shutdown();}catch(Exception error){Debug.LogWarning("Online session cleanup: "+error.Message);}
            probe=null;if(ownsBackground)Application.runInBackground=previousBackground;
            if(Current==this)Current=null;
        }
    }
}
