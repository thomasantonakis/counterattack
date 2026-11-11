using System;
using System.Collections;
using System.IO;
using System.Text;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.P2P;
using UnityEngine;

internal sealed class EOSStandalonePacketProbe
{
    private const byte ProbeChannel = 7;

    private static IEnumerator Login(EOSStandaloneSession session)
    {
        var login = session.OpenAndLogin();
        while (login.MoveNext())
            yield return login.Current;
    }

    private static SocketId ProbeSocket(string lobbyId) =>
        new SocketId { SocketName = "ca-m0-" + lobbyId.Substring(0, Math.Min(24, lobbyId.Length)) };

    private static byte[] ProbeBytes(string prefix, string lobbyId) =>
        Encoding.ASCII.GetBytes(prefix + lobbyId);

    private static void RequireForcedRelays(P2PInterface p2p)
    {
        var set = new SetRelayControlOptions { RelayControl = RelayControl.ForceRelays };
        EOSProofCheck.Require(p2p.SetRelayControl(ref set) == Result.Success, "Forcing EOS relay routing");
        var get = new GetRelayControlOptions();
        EOSProofCheck.Require(p2p.GetRelayControl(ref get, out var configured) == Result.Success, "Reading EOS relay control");
        EOSProofCheck.Require(configured == RelayControl.ForceRelays, "EOS relay control setting");
    }

    internal static IEnumerator HostProbe(string outputDirectory,
        Func<EOSStandaloneSession, ProductUserId, SocketId, string, IEnumerator> exchange = null)
    {
        var lobbyIdFile = Path.Combine(outputDirectory, "lobby-id.txt");

        var session = new EOSStandaloneSession();
        LobbyInterface lobby = null;
        P2PInterface p2p = null;
        Utf8String lobbyId = null;
        ulong lobbyNotification = 0;
        ulong connectionNotification = 0;
        ProductUserId acceptedGuest = null;
        bool lobbyDestroyed = false;
        try
        {
            var login = Login(session);
            while (login.MoveNext())
                yield return login.Current;

            lobby = session.Platform.GetLobbyInterface();
            p2p = session.Platform.GetP2PInterface();
            EOSProofCheck.Require(lobby != null, "EOS Lobby interface");
            EOSProofCheck.Require(p2p != null, "EOS P2P interface");
            RequireForcedRelays(p2p);

            Result? createResult = null;
            var create = new CreateLobbyOptions
            {
                LocalUserId = session.UserId,
                MaxLobbyMembers = 2,
                PermissionLevel = LobbyPermissionLevel.Joinviapresence,
                PresenceEnabled = false,
                AllowInvites = false,
                BucketId = "counterattack-m0-p2p",
                DisableHostMigration = true,
                EnableRTCRoom = false
            };
            lobby.CreateLobby(ref create, null, (ref CreateLobbyCallbackInfo info) =>
            {
                createResult = info.ResultCode;
                lobbyId = info.LobbyId;
            });
            var deadline = Time.realtimeSinceStartupAsDouble + 25;
            while (!createResult.HasValue && Time.realtimeSinceStartupAsDouble < deadline)
            {
                session.Platform.Tick();
                yield return null;
            }
            EOSProofCheck.Require(createResult.HasValue, "CreateLobby callback timed out");
            EOSProofCheck.Require(createResult.Value == Result.Success, "Creating the P2P coordination lobby");
            EOSProofCheck.Require(!(string.IsNullOrEmpty(lobbyId)), "P2P coordination lobby ID");

            var lobbyIdText = lobbyId.ToString();
            var socket = ProbeSocket(lobbyIdText);
            string joinedGuestId = null;
            string requestedGuestId = null;
            bool guestLeft = false;
            var memberOptions = new AddNotifyLobbyMemberStatusReceivedOptions();
            lobbyNotification = lobby.AddNotifyLobbyMemberStatusReceived(ref memberOptions, null,
                (ref LobbyMemberStatusReceivedCallbackInfo info) =>
                {
                    if (!string.Equals(info.LobbyId, lobbyId, StringComparison.Ordinal) ||
                        info.TargetUserId == null || !info.TargetUserId.IsValid())
                        return;
                    var target = info.TargetUserId.ToString();
                    if (info.CurrentStatus == LobbyMemberStatus.Joined &&
                        !string.Equals(target, session.UserId.ToString(), StringComparison.Ordinal))
                        joinedGuestId = target;
                    else if ((info.CurrentStatus == LobbyMemberStatus.Left ||
                              info.CurrentStatus == LobbyMemberStatus.Disconnected) &&
                             string.Equals(target, joinedGuestId, StringComparison.Ordinal))
                        guestLeft = true;
                });
            EOSProofCheck.Require(lobbyNotification > 0UL, "Lobby member notification");

            var requestOptions = new AddNotifyPeerConnectionRequestOptions
            {
                LocalUserId = session.UserId,
                SocketId = socket
            };
            connectionNotification = p2p.AddNotifyPeerConnectionRequest(ref requestOptions, null,
                (ref OnIncomingConnectionRequestInfo info) =>
                {
                    if (info.RemoteUserId != null && info.RemoteUserId.IsValid() &&
                        info.SocketId.HasValue &&
                        string.Equals(info.SocketId.Value.SocketName, socket.SocketName, StringComparison.Ordinal))
                        requestedGuestId = info.RemoteUserId.ToString();
                });
            EOSProofCheck.Require(connectionNotification > 0UL, "P2P connection request notification");

            File.WriteAllText(lobbyIdFile, lobbyIdText);
            deadline = Time.realtimeSinceStartupAsDouble + 900;
            while ((joinedGuestId == null || requestedGuestId == null) &&
                   Time.realtimeSinceStartupAsDouble < deadline)
            {
                session.Platform.Tick();
                yield return null;
            }
            EOSProofCheck.Require(joinedGuestId != null, "A distinct guest did not join the lobby");
            EOSProofCheck.Require(requestedGuestId == joinedGuestId, "The P2P requester was not the joined lobby member");

            acceptedGuest = ProductUserId.FromString(joinedGuestId);
            EOSProofCheck.Require(acceptedGuest != null, "Joined guest product user ID");
            EOSProofCheck.Require(acceptedGuest.IsValid(), "Joined guest product user ID validity");
            var accept = new AcceptConnectionOptions
            {
                LocalUserId = session.UserId,
                RemoteUserId = acceptedGuest,
                SocketId = socket
            };
            EOSProofCheck.Require(p2p.AcceptConnection(ref accept) == Result.Success, "Accepting the joined guest's P2P connection");

            var expectedHello = ProbeBytes("ca-m0-hello:", lobbyIdText);
            var buffer = new byte[256];
            var receiveOptions = new ReceivePacketOptions
            {
                LocalUserId = session.UserId,
                MaxDataSizeBytes = (uint)buffer.Length,
                RequestedChannel = ProbeChannel
            };
            bool helloReceived = false;
            deadline = Time.realtimeSinceStartupAsDouble + 180;
            while (!helloReceived && Time.realtimeSinceStartupAsDouble < deadline)
            {
                session.Platform.Tick();
                ProductUserId sender = null;
                var receivedSocket = new SocketId();
                var result = p2p.ReceivePacket(ref receiveOptions, ref sender, ref receivedSocket,
                    out var channel, new ArraySegment<byte>(buffer), out var bytesWritten);
                EOSProofCheck.Require(result == Result.Success || result == Result.NotFound, "Receiving P2P hello");
                if (result == Result.Success)
                {
                    EOSProofCheck.Require(sender?.ToString() == joinedGuestId, "P2P hello sender");
                    EOSProofCheck.Require(receivedSocket.SocketName == socket.SocketName, "P2P hello socket");
                    EOSProofCheck.Require(channel == ProbeChannel, "P2P hello channel");
                    EOSProofCheck.Require(bytesWritten == (uint)expectedHello.Length, "P2P hello byte count");
                    for (var i = 0; i < expectedHello.Length; i++)
                        EOSProofCheck.Require(buffer[i] == expectedHello[i], "P2P hello payload byte " + i);
                    helloReceived = true;
                }
                yield return null;
            }
            EOSProofCheck.Require(helloReceived, "No P2P hello arrived from the joined guest");

            var ack = ProbeBytes("ca-m0-ack:", lobbyIdText);
            var send = new SendPacketOptions
            {
                LocalUserId = session.UserId,
                RemoteUserId = acceptedGuest,
                SocketId = socket,
                Channel = ProbeChannel,
                Data = new ArraySegment<byte>(ack),
                AllowDelayedDelivery = true,
                Reliability = PacketReliability.ReliableOrdered,
                DisableAutoAcceptConnection = false
            };
            EOSProofCheck.Require(p2p.SendPacket(ref send) == Result.Success, "Queueing the P2P acknowledgement");

            if (exchange != null)
            {
                var gameplay = exchange(session, acceptedGuest, socket, lobbyIdText);
                try { while (gameplay.MoveNext()) yield return gameplay.Current; }
                finally { (gameplay as IDisposable)?.Dispose(); }
            }

            deadline = Time.realtimeSinceStartupAsDouble + 60;
            while (!guestLeft && Time.realtimeSinceStartupAsDouble < deadline)
            {
                session.Platform.Tick();
                yield return null;
            }
            EOSProofCheck.Require(guestLeft, "The acknowledged guest did not leave the lobby");

            var closeAfterExchange = new CloseConnectionOptions
            {
                LocalUserId = session.UserId,
                RemoteUserId = acceptedGuest,
                SocketId = socket
            };
            EOSProofCheck.Require(p2p.CloseConnection(ref closeAfterExchange) == Result.Success, "Closing the P2P probe connection");

            Result? destroyResult = null;
            bool destroyTimedOut = false;
            var destroy = new DestroyLobbyOptions { LocalUserId = session.UserId, LobbyId = lobbyId };
            for (var attempt = 0; attempt < 3; attempt++)
            {
                destroyResult = null;
                lobby.DestroyLobby(ref destroy, null,
                    (ref DestroyLobbyCallbackInfo info) => destroyResult = info.ResultCode);
                deadline = Time.realtimeSinceStartupAsDouble + 30;
                while (!destroyResult.HasValue && Time.realtimeSinceStartupAsDouble < deadline)
                {
                    session.Platform.Tick();
                    yield return null;
                }
                if (destroyResult == Result.Success ||
                    (destroyTimedOut && destroyResult == Result.NotFound))
                {
                    lobbyDestroyed = true;
                    break;
                }
                if (destroyResult != Result.TimedOut)
                    break;
                destroyTimedOut = true;
            }
            EOSProofCheck.Require(destroyResult.HasValue, "DestroyLobby callback timed out");
            EOSProofCheck.Require(lobbyDestroyed, "Destroying the P2P coordination lobby (last EOS result: " + destroyResult.Value + ")");
        }
        finally
        {
            if (p2p != null && acceptedGuest != null && session.Platform != null)
            {
                var close = new CloseConnectionOptions
                {
                    LocalUserId = session.UserId,
                    RemoteUserId = acceptedGuest,
                    SocketId = ProbeSocket(lobbyId.ToString())
                };
                p2p.CloseConnection(ref close);
            }
            if (connectionNotification != 0)
                p2p.RemoveNotifyPeerConnectionRequest(connectionNotification);
            if (lobbyNotification != 0)
                lobby.RemoveNotifyLobbyMemberStatusReceived(lobbyNotification);
            if (lobby != null && !string.IsNullOrEmpty(lobbyId) && !lobbyDestroyed && session.Platform != null)
            {
                var cleanup = new DestroyLobbyOptions { LocalUserId = session.UserId, LobbyId = lobbyId };
                lobby.DestroyLobby(ref cleanup, null, (ref DestroyLobbyCallbackInfo _) => { });
                for (var i = 0; i < 20; i++)
                    session.Platform.Tick();
            }
            if (File.Exists(lobbyIdFile))
                File.Delete(lobbyIdFile);
            session.Dispose();
        }
    }

    internal static IEnumerator GuestProbe(string inputLobbyId,
        Func<EOSStandaloneSession, ProductUserId, SocketId, string, IEnumerator> exchange = null)
    {
        var lobbyId = inputLobbyId;
        EOSProofCheck.Require(lobbyId != null && lobbyId.Length == 32, "Expected a 32-character lobby ID");

        var session = new EOSStandaloneSession();
        LobbySearch search = null;
        LobbyDetails details = null;
        P2PInterface p2p = null;
        ProductUserId hostId = null;
        bool joined = false;
        bool leaveAttempted = false;
        try
        {
            var login = Login(session);
            while (login.MoveNext())
                yield return login.Current;

            var lobby = session.Platform.GetLobbyInterface();
            p2p = session.Platform.GetP2PInterface();
            EOSProofCheck.Require(lobby != null, "EOS Lobby interface");
            EOSProofCheck.Require(p2p != null, "EOS P2P interface");
            RequireForcedRelays(p2p);
            var createSearch = new CreateLobbySearchOptions { MaxResults = 1 };
            EOSProofCheck.Require(lobby.CreateLobbySearch(ref createSearch, out search) == Result.Success, "Creating a P2P lobby-ID search");
            var setId = new LobbySearchSetLobbyIdOptions { LobbyId = lobbyId };
            EOSProofCheck.Require(search.SetLobbyId(ref setId) == Result.Success, "Setting the P2P lobby ID");
            Result? findResult = null;
            var find = new LobbySearchFindOptions { LocalUserId = session.UserId };
            search.Find(ref find, null, (ref LobbySearchFindCallbackInfo info) => findResult = info.ResultCode);
            var deadline = Time.realtimeSinceStartupAsDouble + 25;
            while (!findResult.HasValue && Time.realtimeSinceStartupAsDouble < deadline)
            {
                session.Platform.Tick();
                yield return null;
            }
            EOSProofCheck.Require(findResult.HasValue, "P2P lobby search timed out");
            EOSProofCheck.Require(findResult.Value == Result.Success, "Finding the P2P host lobby");
            var count = new LobbySearchGetSearchResultCountOptions();
            EOSProofCheck.Require(search.GetSearchResultCount(ref count) == 1U, "Exact P2P lobby-ID result");
            var index = new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = 0 };
            EOSProofCheck.Require(search.CopySearchResultByIndex(ref index, out details) == Result.Success, "Copying P2P host lobby details");
            EOSProofCheck.Require(details != null, "P2P host lobby details");
            var owner = new LobbyDetailsGetLobbyOwnerOptions();
            hostId = details.GetLobbyOwner(ref owner);
            EOSProofCheck.Require(hostId != null, "P2P host product user ID");
            EOSProofCheck.Require(hostId.IsValid(), "P2P host product user ID validity");
            EOSProofCheck.Require(hostId.ToString() != session.UserId.ToString(), "Distinct P2P host user");

            Result? joinResult = null;
            var join = new JoinLobbyOptions
            {
                LobbyDetailsHandle = details,
                LocalUserId = session.UserId,
                PresenceEnabled = false
            };
            lobby.JoinLobby(ref join, null, (ref JoinLobbyCallbackInfo info) => joinResult = info.ResultCode);
            deadline = Time.realtimeSinceStartupAsDouble + 25;
            while (!joinResult.HasValue && Time.realtimeSinceStartupAsDouble < deadline)
            {
                session.Platform.Tick();
                yield return null;
            }
            EOSProofCheck.Require(joinResult.HasValue, "Joining P2P host lobby timed out");
            EOSProofCheck.Require(joinResult.Value == Result.Success, "Joining P2P host lobby");
            joined = true;

            var socket = ProbeSocket(lobbyId);
            var hello = ProbeBytes("ca-m0-hello:", lobbyId);
            var send = new SendPacketOptions
            {
                LocalUserId = session.UserId,
                RemoteUserId = hostId,
                SocketId = socket,
                Channel = ProbeChannel,
                Data = new ArraySegment<byte>(hello),
                AllowDelayedDelivery = true,
                Reliability = PacketReliability.ReliableOrdered,
                DisableAutoAcceptConnection = false
            };
            EOSProofCheck.Require(p2p.SendPacket(ref send) == Result.Success, "Queueing P2P hello");

            var expectedAck = ProbeBytes("ca-m0-ack:", lobbyId);
            var buffer = new byte[256];
            var receive = new ReceivePacketOptions
            {
                LocalUserId = session.UserId,
                MaxDataSizeBytes = (uint)buffer.Length,
                RequestedChannel = ProbeChannel
            };
            bool ackReceived = false;
            deadline = Time.realtimeSinceStartupAsDouble + 180;
            while (!ackReceived && Time.realtimeSinceStartupAsDouble < deadline)
            {
                session.Platform.Tick();
                ProductUserId sender = null;
                var receivedSocket = new SocketId();
                var result = p2p.ReceivePacket(ref receive, ref sender, ref receivedSocket,
                    out var channel, new ArraySegment<byte>(buffer), out var bytesWritten);
                EOSProofCheck.Require(result == Result.Success || result == Result.NotFound, "Receiving P2P acknowledgement");
                if (result == Result.Success)
                {
                    EOSProofCheck.Require(sender?.ToString() == hostId.ToString(), "P2P acknowledgement sender");
                    EOSProofCheck.Require(receivedSocket.SocketName == socket.SocketName, "P2P acknowledgement socket");
                    EOSProofCheck.Require(channel == ProbeChannel, "P2P acknowledgement channel");
                    EOSProofCheck.Require(bytesWritten == (uint)expectedAck.Length, "P2P acknowledgement byte count");
                    for (var i = 0; i < expectedAck.Length; i++)
                        EOSProofCheck.Require(buffer[i] == expectedAck[i], "P2P acknowledgement payload byte " + i);
                    ackReceived = true;
                }
                yield return null;
            }
            EOSProofCheck.Require(ackReceived, "No P2P acknowledgement arrived from the host");

            if (exchange != null)
            {
                var gameplay = exchange(session, hostId, socket, lobbyId);
                try { while (gameplay.MoveNext()) yield return gameplay.Current; }
                finally { (gameplay as IDisposable)?.Dispose(); }
            }

            Result? leaveResult = null;
            var leave = new LeaveLobbyOptions { LocalUserId = session.UserId, LobbyId = lobbyId };
            leaveAttempted = true;
            lobby.LeaveLobby(ref leave, null, (ref LeaveLobbyCallbackInfo info) => leaveResult = info.ResultCode);
            deadline = Time.realtimeSinceStartupAsDouble + 25;
            while (!leaveResult.HasValue && Time.realtimeSinceStartupAsDouble < deadline)
            {
                session.Platform.Tick();
                yield return null;
            }
            EOSProofCheck.Require(leaveResult.HasValue, "Leaving P2P host lobby timed out");
            EOSProofCheck.Require(leaveResult.Value == Result.Success, "Leaving P2P host lobby");
        }
        finally
        {
            if (p2p != null && hostId != null && session.Platform != null)
            {
                var close = new CloseConnectionOptions
                {
                    LocalUserId = session.UserId,
                    RemoteUserId = hostId,
                    SocketId = ProbeSocket(lobbyId)
                };
                p2p.CloseConnection(ref close);
            }
            if (joined && !leaveAttempted && session.Platform != null)
            {
                var lobby = session.Platform.GetLobbyInterface();
                var leave = new LeaveLobbyOptions { LocalUserId = session.UserId, LobbyId = lobbyId };
                lobby.LeaveLobby(ref leave, null, (ref LeaveLobbyCallbackInfo _) => { });
                for (var i = 0; i < 20; i++)
                    session.Platform.Tick();
            }
            details?.Release();
            search?.Release();
            session.Dispose();
        }
    }
}
