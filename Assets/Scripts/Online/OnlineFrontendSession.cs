using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using Epic.OnlineServices.Lobby;
using UnityEngine;
namespace CounterAttack.Online
{
    // Nonvisual authored prefab instance owns transport across scene transitions.
    public sealed class OnlineFrontendSession:MonoBehaviour
    {
        public static OnlineFrontendSession Current {get;private set;}
        public event Action<OnlineSetupClient> ClientAvailable;
        public event Action<GameSettings> DraftAgreed;
        public event Action<OnlineDraftClient> DraftAvailable;
        public OnlineDraftClient Draft=>channel?.Draft;
        public event Action<MatchManager.GameData> RoomPrepared;
        private MatchManager.GameData roomLaunch;
        private OnlineRoomLaunchLifetime roomLifetime;
        private IEnumerator gameplay;
        private EOSGroundPassExchange spectatorRoomSource;
        internal bool TryEncodeSpectatorRoom(long sequence, out byte[] payload)
        {
            payload = null;
            return spectatorRoomSource != null && spectatorRoomSource.TryEncodeSpectatorRoom(sequence, out payload);
        }
        private OnlineGroundPassBoard roomBoard;
        public MatchManager.GameData CaptureRoomLaunch()=>OnlineRoomLaunchData.Copy(roomLaunch);
        private OnlineDraftClient announcedDraft;
        public OnlineSetupClient Client=>channel?.Client;
        public string LobbyCode {get;private set;}
        private string lobbyId;
        public string Status {get;private set;}="Not connected";
        public string RunDirectory {get;private set;}
        public string Error {get;private set;}
        public bool Busy {get;private set;}
        public bool Hosting {get;private set;}
        public GameSettings AgreedSettings=>OnlineSetupRules.Copy(agreedSettings);
        public OnlineLatencySummary Latency=>channel?.Latency.Capture()??new OnlineLatencySummary();
        public int QueuedPackets=>outgoing.Count;
        private OnlineSessionChannel channel;
        private OnlineFrontendRecovery recovery;
        private ulong requestNotification,closedNotification;
        private IDisposable connectionNotifications;
        private bool connectionLost;
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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterShutdown()
        {
#if !UNITY_EDITOR
            Application.quitting -= ShutdownOnQuit;
            Application.quitting += ShutdownOnQuit;
#endif
        }

#if !UNITY_EDITOR
        private static void ShutdownOnQuit()
        {
            try { RetireActiveSession(); }
            finally { EOSStandaloneSession.Shutdown(); }
        }
#endif

        internal static void RetireActiveSession()
        {
            if (Current != null) Current.OnDestroy();
        }

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
            if (!OnlineFrontendSessionPolicy.TryNormalizeLobbyCode(lobbyCode, out var code))
                throw new ArgumentException(OnlineFrontendSessionPolicy.LobbyCodePrompt, nameof(lobbyCode));
            if(Busy || running!=null)throw new InvalidOperationException("An online session is already running");
            Begin(false,code,forceRelay);
        }
        private void Begin(bool hosting,string code,bool forceRelay)
        {
            previousBackground=Application.runInBackground;ownsBackground=true;Application.runInBackground=true;
            Hosting=hosting;LobbyCode=code;relayOnly=forceRelay;stop=false;Busy=true;Error=null;
            startedUtc=DateTime.UtcNow.ToString("O");
            RunDirectory=Path.Combine(Application.persistentDataPath,"OnlineSessions",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(RunDirectory);
            Status=hosting?"Creating an online session...":"Joining the online session...";
            var route=OnlineFrontendSessionPolicy.Route(forceRelay);
            probe = hosting
                ? EOSStandalonePacketProbe.HostProbe(RunDirectory, Exchange, route, OnLobbyCodePublished)
                : EOSStandalonePacketProbe.GuestProbe(code, Exchange, route, searchByCode: true);
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
            try{StopRoom();}catch(Exception error){failure??=error;}
            try{(probe as IDisposable)?.Dispose();}catch(Exception error){failure??=error;}
            probe=null;native=null;channel?.TransportLost();
            Busy=false;running=null;
            if(failure!=null && !stop)
            {
                Error=failure.Message;
                Status="Online connection failed. Leave the session to try again.";
                FindAnyObjectByType<OnlineRoomStartup>()?.Board?.nativePause?.PresentClosedMatch(Status);
                Debug.LogWarning("Online session failed: "+failure.Message);
            }
            else Status="Online session closed";
            Retain(failure);
            if(stop && !disposed)Destroy(gameObject);
        }
        private IEnumerator Exchange(EOSStandaloneSession session,ProductUserId remote,SocketId socketId,string lobby)
        {
            native=session;peer=remote;socket=socketId;lobbyId=lobby;
            var compatibility=OnlineFrontendSessionPolicy.Compatibility();string match="online-"+lobby;
            if(Hosting)
            {
                var protocol=OnlineFrontendSessionPolicy.CreateMatchProtocol(match,session.UserId.ToString(),remote.ToString(),compatibility);
                protocol.BindAuthenticatedAccount(session.UserId.ToString(),"frontend-host",compatibility);
                var hostChannel=new OnlineSetupHostChannel(protocol,"frontend-host",remote.ToString(),initial,()=>Time.realtimeSinceStartupAsDouble);
                hostChannel.EnableDraft(s=>new OnlineDraftAuthority(protocol,s,OnlineDraftCatalog.Load(),rollClock:()=>Time.realtimeSinceStartupAsDouble));
                channel=hostChannel;
            }
            else channel=new OnlineSetupGuestChannel(match,remote.ToString(),compatibility,OnlineFrontendSessionPolicy.HostTeam,()=>Time.realtimeSinceStartupAsDouble);
            channel.Outgoing+=Queue;
            channel.Client.DraftAgreed+=OnAgreed;
            ClientAvailable?.Invoke(channel.Client);
            if(channel is OnlineSetupHostChannel host)host.Begin();
            recovery=new OnlineFrontendRecovery(channel,()=>Time.realtimeSinceStartupAsDouble,()=>outgoing.Clear(),()=>outgoing.Count==0);
            RegisterConnectionNotifications();
            Status="Checking game versions and settings...";
            try
            {
                while(!disposed)
                {
                    session.Tick();
                    if(connectionLost){connectionLost=false;recovery.ConnectionLost();}
                    Flush();Poll();PollReturningGuest();recovery.Tick();channel.Tick();Flush();
                    if(channel.Fault!=null)throw new InvalidOperationException(channel.Fault);
                    if(channel.PeerLeft)
                    {
                        Status="The other manager left the session.";
                        roomBoard?.menu?.PresentPeerDeparture();
                        roomBoard?.nativePause?.PresentClosedMatch("The other manager left the match. Return to the main menu.");
                        break;
                    }
                    if(stop)break;
                    if(recovery.Expired)throw new TimeoutException("The other manager did not reconnect within five minutes.");
                    if(roomLifetime!=null)
                    {
                        if(gameplay==null)
                        {
                            var startup=FindAnyObjectByType<OnlineRoomStartup>();
                            if(startup?.Board!=null)
                            {
                                roomBoard=startup.Board;
                                var exchange=new EOSGroundPassExchange(roomBoard,RunDirectory);
                                if (Hosting) spectatorRoomSource = exchange;
                                gameplay=Hosting?exchange.Host(native,peer,socket,lobbyId):exchange.Guest(native,peer,socket,lobbyId);
                            }
                        }
                        if(gameplay!=null && !gameplay.MoveNext()){Status="Online match finished.";break;}
                        Status=recovery.Recovering?"Connection interrupted. Waiting for the other manager...":"Online match connected";
                    }
                    else if(recovery.Recovering)Status="Connection interrupted. Waiting for the other manager to reconnect...";
                    else if(channel.Ready)Status=agreedSettings==null?"Connected. Review your selections.":"Both managers agreed. Preparing Draft...";
                    yield return null;
                }
            }
            finally
            {
                try{StopRoom();}
                finally{RemoveConnectionNotifications();}
            }
            channel.TransportLost();
        }
        private bool SamePeer(ProductUserId remote,SocketId? remoteSocket)=>remote?.ToString()==peer?.ToString() && remoteSocket?.SocketName==socket.SocketName;
        private bool ReservedPeerIsMember()
        {
            var lobby=native.Platform.GetLobbyInterface();var copy=new CopyLobbyDetailsHandleOptions{LocalUserId=native.UserId,LobbyId=lobbyId};
            if(lobby.CopyLobbyDetailsHandle(ref copy,out var details)!=Result.Success || details==null)return false;
            try
            {
                var countOptions=new LobbyDetailsGetMemberCountOptions();uint count=details.GetMemberCount(ref countOptions);
                for(uint i=0;i<count;i++){var index=new LobbyDetailsGetMemberByIndexOptions{MemberIndex=i};if(details.GetMemberByIndex(ref index)?.ToString()==peer.ToString())return true;}
                return false;
            }
            finally{details.Release();}
        }
        private void RegisterConnectionNotifications()
        {
            var p2p=native.Platform.GetP2PInterface();
            var request=new AddNotifyPeerConnectionRequestOptions{LocalUserId=native.UserId,SocketId=socket};
            requestNotification=p2p.AddNotifyPeerConnectionRequest(ref request,null,(ref OnIncomingConnectionRequestInfo info)=>
            {
                if(!SamePeer(info.RemoteUserId,info.SocketId) || !ReservedPeerIsMember())return;
                var accept=new AcceptConnectionOptions{LocalUserId=native.UserId,RemoteUserId=peer,SocketId=socket};p2p.AcceptConnection(ref accept);
            });
            var closed=new AddNotifyPeerConnectionClosedOptions{LocalUserId=native.UserId,SocketId=socket};
            closedNotification=p2p.AddNotifyPeerConnectionClosed(ref closed,null,(ref OnRemoteConnectionClosedInfo info)=>
            {if(SamePeer(info.RemoteUserId,info.SocketId))connectionLost=true;});
            ulong requestId=requestNotification,closedId=closedNotification;
            connectionNotifications=native.OwnNativeSubscription(()=>
            {
                if(requestId!=0)p2p.RemoveNotifyPeerConnectionRequest(requestId);
                if(closedId!=0)p2p.RemoveNotifyPeerConnectionClosed(closedId);
            });
            if(requestId==0 || closedId==0){RemoveConnectionNotifications();throw new InvalidOperationException("Could not observe online connection recovery");}
        }
        private void RemoveConnectionNotifications()
        {
            var owned=connectionNotifications;connectionNotifications=null;
            requestNotification=closedNotification=0;owned?.Dispose();
        }
        private void PollReturningGuest()
        {
            if(!Hosting)return;
            var p2p=native.Platform.GetP2PInterface();var receive=new ReceivePacketOptions{LocalUserId=native.UserId,MaxDataSizeBytes=(uint)receiveBuffer.Length,RequestedChannel=7};
            for(int i=0;i<8;i++)
            {
                ProductUserId sender=null;var receivedSocket=new SocketId();
                var result=p2p.ReceivePacket(ref receive,ref sender,ref receivedSocket,out var channelId,new ArraySegment<byte>(receiveBuffer),out var size);
                if(result==Result.NotFound)return;
                if(result!=Result.Success)throw new IOException("Receiving reconnect handshake failed: "+result);
                if(!SamePeer(sender,receivedSocket) || channelId!=7 || System.Text.Encoding.ASCII.GetString(receiveBuffer,0,(int)size)!="ca-m0-hello:"+lobbyId || !ReservedPeerIsMember())continue;
                recovery.ConnectionLost();var ack=System.Text.Encoding.ASCII.GetBytes("ca-m0-ack:"+lobbyId);
                var send=new SendPacketOptions{LocalUserId=native.UserId,RemoteUserId=peer,SocketId=socket,Channel=7,Data=new ArraySegment<byte>(ack),AllowDelayedDelivery=false,Reliability=PacketReliability.ReliableOrdered};
                p2p.SendPacket(ref send);
            }
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
                    Channel=OnlineFrontendSessionPolicy.Channel,Data=new ArraySegment<byte>(packet),AllowDelayedDelivery=false,Reliability=PacketReliability.ReliableOrdered};
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
            if(Draft!=null && !ReferenceEquals(announcedDraft,Draft))
            {
                announcedDraft=Draft;DraftAvailable?.Invoke(Draft);
                if(agreedSettings!=null && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="CreateNewGameScene")UnityEngine.SceneManagement.SceneManager.LoadScene("Draft");
            }
            if(roomLaunch==null && Draft?.Connected==true && Draft.Phase=="room-ready")
            {
                roomLaunch=OnlineRoomLaunchData.Create(Draft.Capture());
                File.WriteAllText(Path.Combine(RunDirectory,"room-bootstrap.json"),Newtonsoft.Json.JsonConvert.SerializeObject(roomLaunch,Newtonsoft.Json.Formatting.Indented));
                RoomPrepared?.Invoke(OnlineRoomLaunchData.Copy(roomLaunch));
                roomLifetime=new OnlineRoomLaunchLifetime("ground-"+lobbyId,!Hosting,roomLaunch,RunDirectory);
                UnityEngine.SceneManagement.SceneManager.LoadScene(OnlineRoomStartup.SceneName);
            }
        }

        private void OnLobbyCodePublished(string code)
        {
            LobbyCode = code;
            Status = "Share the lobby code with the other manager.";
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
            // A menu may unload Room in this same call stack. Release subscriptions,
            // holds and coroutines before any scene-owned controls are destroyed.
            try{StopRoom();}catch(Exception error){Debug.LogWarning("Online Room cleanup: "+error.Message);}
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
            try{StopRoom();}catch(Exception error){Debug.LogWarning("Online Room cleanup: "+error.Message);}
            try{(probe as IDisposable)?.Dispose();}catch(Exception error){Debug.LogWarning("Online session cleanup: "+error.Message);}
            probe=null;if(ownsBackground)Application.runInBackground=previousBackground;
            if(Current==this)Current=null;
        }
        private void StopRoom()
        {
            try{(gameplay as IDisposable)?.Dispose();}
            finally
            {
                gameplay=null;
                spectatorRoomSource=null;
                try
                {
                    if(roomBoard!=null)
                    {
                        if(roomBoard.controls!=null)roomBoard.controls.SetConnected(false);
                        // Rules coroutines cannot outlive their authenticated transport.
                        if (roomBoard.usesNativeRoom)
                            roomBoard.GetComponent<OnlineRoomStartup>().StopRules();
                        else
                        {
                            foreach(var component in roomBoard.authorityRoot.GetComponentsInChildren<MonoBehaviour>(true))
                                if(component!=null)component.StopAllCoroutines();
                            roomBoard.authorityRoot.SetActive(false);
                        }
                    }
                }
                finally{roomBoard=null;roomLifetime?.Dispose();roomLifetime=null;}
            }
        }
    }
}
