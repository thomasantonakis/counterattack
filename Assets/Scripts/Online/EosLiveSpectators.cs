using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.P2P;

namespace CounterAttack.Online
{
    internal sealed class EosLiveSpectators : IDisposable
    {
        internal delegate bool Capture(long sequence, out byte[] snapshot);

        private readonly EOSStandaloneSession session;
        private readonly LobbyInterface lobby;
        private readonly P2PInterface p2p;
        private readonly OnlineSpectatorAdmission admission;
        private readonly Capture capture;
        private readonly string matchId;
        private readonly string compatibilitySuffix;
        private readonly Dictionary<string, EosSpectatorRoomLink> links = new(StringComparer.Ordinal);
        private readonly Dictionary<string, double> pendingMembers = new(StringComparer.Ordinal);
        private readonly byte[] receiveBuffer = new byte[256];
        private IEnumerator<object> publish;
        private Utf8String lobbyId;
        private SocketId socket;
        private Result? created;
        private ulong incoming;
        private long sequence;
        private double nextCapture, nextInspection, startupDeadline;
        private bool closed;
        internal bool Ready => !closed && incoming != 0 && publish == null;

        internal EosLiveSpectators(EOSStandaloneSession session, OnlineSpectatorAdmission admission,
            Capture capture, double now)
        {
            this.session = session;
            this.admission = admission;
            this.capture = capture;
            matchId = OnlineSpectatorAdmission.MatchIdentity(admission.MatchCode);
            compatibilitySuffix = OnlineSpectatorAdmission.ViewerCompatibilitySuffix(OnlineFrontendSessionPolicy.Compatibility());
            lobby = session.Platform.GetLobbyInterface();
            p2p = session.Platform.GetP2PInterface();
            startupDeadline = now + 30;
            var options = new CreateLobbyOptions
            {
                LocalUserId = session.UserId,
                MaxLobbyMembers = OnlineSpectatorAdmission.ViewerLimit + 1,
                PermissionLevel = LobbyPermissionLevel.Joinviapresence,
                PresenceEnabled = false, AllowInvites = false, DisableHostMigration = true,
                BucketId = EOSStandalonePacketProbe.LiveWatchLobbyBucket
            };
            lobby.CreateLobby(ref options, null, (ref CreateLobbyCallbackInfo info) =>
            {
                created = info.ResultCode;
                lobbyId = info.LobbyId;
                if (closed) DestroyLobby();
            });
        }

        internal void Tick(double now)
        {
            if (closed) return;
            if (!created.HasValue)
            {
                EOSProofCheck.Require(now < startupDeadline, "Creating the watch lobby timed out");
                return;
            }
            EOSProofCheck.Require(created == Result.Success && !string.IsNullOrEmpty(lobbyId), "Creating the watch lobby: " + created);
            if (incoming == 0)
            {
                socket = EOSStandalonePacketProbe.ProbeSocket(lobbyId.ToString(), watchLobby: true);
                var request = new AddNotifyPeerConnectionRequestOptions { LocalUserId = session.UserId, SocketId = socket };
                incoming = p2p.AddNotifyPeerConnectionRequest(ref request, null, AcceptCandidate);
                EOSProofCheck.Require(incoming != 0, "Registering watch connection requests");
                publish = EOSStandalonePacketProbe.PublishJoinCode(session, lobby, lobbyId,
                    OnlineSpectatorAdmission.Lookup(admission.MatchCode));
            }
            if (publish != null)
            {
                if (publish.MoveNext()) return;
                publish.Dispose();
                publish = null;
            }

            if (now >= nextInspection)
            {
                InspectMembers(now);
                nextInspection = now + .5;
            }
            PollAdmission();
            if (links.Count != 0 && now >= nextCapture)
            {
                nextCapture = now + .25;
                if (capture(sequence + 1, out var snapshot))
                {
                    sequence++;
                    foreach (var link in links.Values) link.QueueLatest(snapshot);
                }
            }
            foreach (var pair in links.ToArray())
            {
                try { pair.Value.Flush(); }
                catch (System.IO.IOException) { RemoveViewer(pair.Key, kick: true); }
            }
        }

        private void AcceptCandidate(ref OnIncomingConnectionRequestInfo info)
        {
            if (closed || info.RemoteUserId == null || !info.RemoteUserId.IsValid()
                || info.SocketId?.SocketName != socket.SocketName || !IsMember(info.RemoteUserId.ToString()))
                return;
            AcceptCandidate(info.RemoteUserId);
        }

        private void AcceptCandidate(ProductUserId peer)
        {
            var options = new AcceptConnectionOptions { LocalUserId = session.UserId, RemoteUserId = peer, SocketId = socket };
            p2p.AcceptConnection(ref options);
        }

        private void PollAdmission()
        {
            string prefix = "ca-m0-hello:" + lobbyId;
            string suffix = compatibilitySuffix;
            var options = new ReceivePacketOptions
            {
                LocalUserId = session.UserId, RequestedChannel = EOSStandalonePacketProbe.WatchProbeChannel,
                MaxDataSizeBytes = (uint)receiveBuffer.Length
            };
            for (int i = 0; i < 8; i++)
            {
                ProductUserId sender = null;
                var actual = new SocketId();
                var result = p2p.ReceivePacket(ref options, ref sender, ref actual, out var channel,
                    new ArraySegment<byte>(receiveBuffer), out var count);
                if (result == Result.NotFound) return;
                if (result != Result.Success || sender == null || channel != EOSStandalonePacketProbe.WatchProbeChannel
                    || actual.SocketName != socket.SocketName) continue;
                string identity = sender.ToString();
                if (!IsMember(identity)) continue;
                string hello = Encoding.ASCII.GetString(receiveBuffer, 0, (int)count);
                string code = hello.Length == prefix.Length + OnlineSpectatorAdmission.CodeLength + suffix.Length
                    && hello.StartsWith(prefix, StringComparison.Ordinal) && hello.EndsWith(suffix, StringComparison.Ordinal)
                    ? hello.Substring(prefix.Length, OnlineSpectatorAdmission.CodeLength) : null;
                if (!admission.CanResumeViewer(identity, code) && !admission.TryAdmitViewer(identity, code))
                {
                    RemoveViewer(identity, kick: true);
                    continue;
                }
                var ack = new SendPacketOptions
                {
                    LocalUserId = session.UserId, RemoteUserId = sender, SocketId = socket,
                    Channel = EOSStandalonePacketProbe.WatchProbeChannel, Reliability = PacketReliability.ReliableOrdered,
                    Data = new ArraySegment<byte>(Encoding.ASCII.GetBytes("ca-m0-ack:" + lobbyId))
                };
                if (p2p.SendPacket(ref ack) != Result.Success)
                {
                    RemoveViewer(identity, kick: true);
                    continue;
                }
                pendingMembers.Remove(identity);
                if (links.Remove(identity, out var prior)) prior.Dispose();
                links.Add(identity, new EosSpectatorRoomLink(session, sender, socket, matchId));
            }
        }

        private bool IsMember(string identity) => ReadMembers(session, lobby, lobbyId)?.Contains(identity) == true;

        internal static HashSet<string> ReadMembers(EOSStandaloneSession session, LobbyInterface lobby, Utf8String lobbyId)
        {
            var options = new CopyLobbyDetailsHandleOptions { LocalUserId = session.UserId, LobbyId = lobbyId };
            if (lobby.CopyLobbyDetailsHandle(ref options, out var details) != Result.Success || details == null)
                return null;
            try
            {
                var members = new HashSet<string>(StringComparer.Ordinal);
                var countOptions = new LobbyDetailsGetMemberCountOptions();
                uint count = details.GetMemberCount(ref countOptions);
                for (uint i = 0; i < count; i++)
                {
                    var index = new LobbyDetailsGetMemberByIndexOptions { MemberIndex = i };
                    var member = details.GetMemberByIndex(ref index);
                    if (member != null) members.Add(member.ToString());
                }
                members.Remove(session.UserId.ToString());
                return members;
            }
            finally { details.Release(); }
        }

        private void InspectMembers(double now)
        {
            var members = ReadMembers(session, lobby, lobbyId);
            if (members == null) return;
            foreach (string identity in links.Keys.Where(identity => !members.Contains(identity)).ToArray())
                RemoveViewer(identity, kick: false);
            foreach (string identity in pendingMembers.Keys.Where(identity => !members.Contains(identity)).ToArray())
                pendingMembers.Remove(identity);
            foreach (string identity in members)
            {
                if (links.ContainsKey(identity)) continue;
                if (!pendingMembers.TryGetValue(identity, out double deadline))
                {
                    pendingMembers.Add(identity, now + 12);
                    AcceptCandidate(ProductUserId.FromString(identity));
                }
                else if (now >= deadline)
                    RemoveViewer(identity, kick: true);
            }
        }

        private void RemoveViewer(string identity, bool kick)
        {
            if (links.Remove(identity, out var link)) link.Dispose();
            admission.ViewerLeft(identity);
            pendingMembers.Remove(identity);
            var peer = ProductUserId.FromString(identity);
            var close = new CloseConnectionOptions { LocalUserId = session.UserId, RemoteUserId = peer, SocketId = socket };
            p2p.CloseConnection(ref close);
            if (!kick) return;
            var options = new KickMemberOptions { LocalUserId = session.UserId, LobbyId = lobbyId, TargetUserId = peer };
            lobby.KickMember(ref options, null, (ref KickMemberCallbackInfo _) => { });
        }

        public void Dispose()
        {
            if (closed) return;
            closed = true;
            publish?.Dispose();
            if (session.Platform == null) return;
            if (incoming != 0) p2p.RemoveNotifyPeerConnectionRequest(incoming);
            foreach (string identity in links.Keys.ToArray()) RemoveViewer(identity, kick: false);
            DestroyLobby();
            session.Platform.Tick();
        }

        private void DestroyLobby()
        {
            if (created != Result.Success || string.IsNullOrEmpty(lobbyId) || session.Platform == null) return;
            var options = new DestroyLobbyOptions { LocalUserId = session.UserId, LobbyId = lobbyId };
            lobbyId = null;
            lobby.DestroyLobby(ref options, null, (ref DestroyLobbyCallbackInfo _) => { });
        }
    }
}
