using System;
using System.Collections;
using System.IO;
using System.Text;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.P2P;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

[Category("OnlineEOSConnectivity")]
public sealed class EOSTwoMacP2PEditorTests
{
    private const byte ProbeChannel = 7;

    private static IEnumerator Login(EOSProbeSession session)
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
        Assert.That(p2p.SetRelayControl(ref set), Is.EqualTo(Result.Success), "Forcing EOS relay routing");
        var get = new GetRelayControlOptions();
        Assert.That(p2p.GetRelayControl(ref get, out var configured), Is.EqualTo(Result.Success),
            "Reading EOS relay control");
        Assert.That(configured, Is.EqualTo(RelayControl.ForceRelays), "EOS relay control setting");
    }

    [UnityTest]
    [Timeout(1320000)]
    public IEnumerator HostReceivesPacketAndAcknowledgesDistinctGuest() => HostProbe(false);

    [UnityTest]
    [Timeout(1320000)]
    public IEnumerator HostExchangesPacketsWithForcedRelays() => HostProbe(true);

    private static IEnumerator HostProbe(bool forceRelays)
    {
        string marker = forceRelays ? "Temp/OnlineEOSRelayHost.run" : "Temp/OnlineEOSP2PHost.run";
        const string lobbyIdFile = "Temp/OnlineEOSP2PHostLobbyId.txt";
        if (!File.Exists(marker))
            Assert.Ignore("The one-use two-Mac P2P host marker is absent.");
        File.Delete(marker);

        var session = new EOSProbeSession();
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
            Assert.That(lobby, Is.Not.Null, "EOS Lobby interface");
            Assert.That(p2p, Is.Not.Null, "EOS P2P interface");
            if (forceRelays) RequireForcedRelays(p2p);

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
            var deadline = EditorApplication.timeSinceStartup + 25;
            while (!createResult.HasValue && EditorApplication.timeSinceStartup < deadline)
            {
                session.Platform.Tick();
                yield return null;
            }
            Assert.That(createResult.HasValue, Is.True, "CreateLobby callback timed out");
            Assert.That(createResult.Value, Is.EqualTo(Result.Success), "Creating the P2P coordination lobby");
            Assert.That(string.IsNullOrEmpty(lobbyId), Is.False, "P2P coordination lobby ID");

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
            Assert.That(lobbyNotification, Is.GreaterThan(0UL), "Lobby member notification");

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
            Assert.That(connectionNotification, Is.GreaterThan(0UL), "P2P connection request notification");

            File.WriteAllText(lobbyIdFile, lobbyIdText);
            deadline = EditorApplication.timeSinceStartup + 900;
            while ((joinedGuestId == null || requestedGuestId == null) &&
                   EditorApplication.timeSinceStartup < deadline)
            {
                session.Platform.Tick();
                yield return null;
            }
            Assert.That(joinedGuestId, Is.Not.Null, "A distinct guest did not join the lobby");
            Assert.That(requestedGuestId, Is.EqualTo(joinedGuestId),
                "The P2P requester was not the joined lobby member");

            acceptedGuest = ProductUserId.FromString(joinedGuestId);
            Assert.That(acceptedGuest, Is.Not.Null, "Joined guest product user ID");
            Assert.That(acceptedGuest.IsValid(), Is.True, "Joined guest product user ID validity");
            var accept = new AcceptConnectionOptions
            {
                LocalUserId = session.UserId,
                RemoteUserId = acceptedGuest,
                SocketId = socket
            };
            Assert.That(p2p.AcceptConnection(ref accept), Is.EqualTo(Result.Success),
                "Accepting the joined guest's P2P connection");

            var expectedHello = ProbeBytes("ca-m0-hello:", lobbyIdText);
            var buffer = new byte[256];
            var receiveOptions = new ReceivePacketOptions
            {
                LocalUserId = session.UserId,
                MaxDataSizeBytes = (uint)buffer.Length,
                RequestedChannel = ProbeChannel
            };
            bool helloReceived = false;
            deadline = EditorApplication.timeSinceStartup + 180;
            while (!helloReceived && EditorApplication.timeSinceStartup < deadline)
            {
                session.Platform.Tick();
                ProductUserId sender = null;
                var receivedSocket = new SocketId();
                var result = p2p.ReceivePacket(ref receiveOptions, ref sender, ref receivedSocket,
                    out var channel, new ArraySegment<byte>(buffer), out var bytesWritten);
                Assert.That(result, Is.EqualTo(Result.Success).Or.EqualTo(Result.NotFound), "Receiving P2P hello");
                if (result == Result.Success)
                {
                    Assert.That(sender?.ToString(), Is.EqualTo(joinedGuestId), "P2P hello sender");
                    Assert.That(receivedSocket.SocketName, Is.EqualTo(socket.SocketName), "P2P hello socket");
                    Assert.That(channel, Is.EqualTo(ProbeChannel), "P2P hello channel");
                    Assert.That(bytesWritten, Is.EqualTo((uint)expectedHello.Length), "P2P hello byte count");
                    for (var i = 0; i < expectedHello.Length; i++)
                        Assert.That(buffer[i], Is.EqualTo(expectedHello[i]), "P2P hello payload byte " + i);
                    helloReceived = true;
                }
                yield return null;
            }
            Assert.That(helloReceived, Is.True, "No P2P hello arrived from the joined guest");

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
            Assert.That(p2p.SendPacket(ref send), Is.EqualTo(Result.Success), "Queueing the P2P acknowledgement");

            deadline = EditorApplication.timeSinceStartup + 60;
            while (!guestLeft && EditorApplication.timeSinceStartup < deadline)
            {
                session.Platform.Tick();
                yield return null;
            }
            Assert.That(guestLeft, Is.True, "The acknowledged guest did not leave the lobby");

            var closeAfterExchange = new CloseConnectionOptions
            {
                LocalUserId = session.UserId,
                RemoteUserId = acceptedGuest,
                SocketId = socket
            };
            Assert.That(p2p.CloseConnection(ref closeAfterExchange), Is.EqualTo(Result.Success),
                "Closing the P2P probe connection");

            Result? destroyResult = null;
            bool destroyTimedOut = false;
            var destroy = new DestroyLobbyOptions { LocalUserId = session.UserId, LobbyId = lobbyId };
            for (var attempt = 0; attempt < 3; attempt++)
            {
                destroyResult = null;
                lobby.DestroyLobby(ref destroy, null,
                    (ref DestroyLobbyCallbackInfo info) => destroyResult = info.ResultCode);
                deadline = EditorApplication.timeSinceStartup + 30;
                while (!destroyResult.HasValue && EditorApplication.timeSinceStartup < deadline)
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
            Assert.That(destroyResult.HasValue, Is.True, "DestroyLobby callback timed out");
            Assert.That(lobbyDestroyed, Is.True,
                "Destroying the P2P coordination lobby (last EOS result: " + destroyResult.Value + ")");
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

    [UnityTest]
    [Timeout(300000)]
    public IEnumerator GuestSendsPacketAndReceivesAcknowledgement() => GuestProbe(false);

    [UnityTest]
    [Timeout(300000)]
    public IEnumerator GuestExchangesPacketsWithForcedRelays() => GuestProbe(true);

    private static IEnumerator GuestProbe(bool forceRelays)
    {
        string marker = forceRelays ? "Temp/OnlineEOSRelayGuest.run" : "Temp/OnlineEOSP2PGuest.run";
        const string lobbyIdFile = "Temp/OnlineEOSP2PJoinId.txt";
        if (!File.Exists(marker))
            Assert.Ignore("The one-use two-Mac P2P guest marker is absent.");
        File.Delete(marker);
        Assert.That(File.Exists(lobbyIdFile), Is.True, "The local P2P lobby-ID file is absent");
        var lobbyId = File.ReadAllText(lobbyIdFile).Trim();
        File.Delete(lobbyIdFile);
        Assert.That(lobbyId.Length, Is.GreaterThanOrEqualTo(24).And.LessThanOrEqualTo(256),
            "P2P coordination lobby-ID length");

        var session = new EOSProbeSession();
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
            Assert.That(lobby, Is.Not.Null, "EOS Lobby interface");
            Assert.That(p2p, Is.Not.Null, "EOS P2P interface");
            if (forceRelays) RequireForcedRelays(p2p);
            var createSearch = new CreateLobbySearchOptions { MaxResults = 1 };
            Assert.That(lobby.CreateLobbySearch(ref createSearch, out search), Is.EqualTo(Result.Success),
                "Creating a P2P lobby-ID search");
            var setId = new LobbySearchSetLobbyIdOptions { LobbyId = lobbyId };
            Assert.That(search.SetLobbyId(ref setId), Is.EqualTo(Result.Success), "Setting the P2P lobby ID");
            Result? findResult = null;
            var find = new LobbySearchFindOptions { LocalUserId = session.UserId };
            search.Find(ref find, null, (ref LobbySearchFindCallbackInfo info) => findResult = info.ResultCode);
            var deadline = EditorApplication.timeSinceStartup + 25;
            while (!findResult.HasValue && EditorApplication.timeSinceStartup < deadline)
            {
                session.Platform.Tick();
                yield return null;
            }
            Assert.That(findResult.HasValue, Is.True, "P2P lobby search timed out");
            Assert.That(findResult.Value, Is.EqualTo(Result.Success), "Finding the P2P host lobby");
            var count = new LobbySearchGetSearchResultCountOptions();
            Assert.That(search.GetSearchResultCount(ref count), Is.EqualTo(1U), "Exact P2P lobby-ID result");
            var index = new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = 0 };
            Assert.That(search.CopySearchResultByIndex(ref index, out details), Is.EqualTo(Result.Success),
                "Copying P2P host lobby details");
            Assert.That(details, Is.Not.Null, "P2P host lobby details");
            var owner = new LobbyDetailsGetLobbyOwnerOptions();
            hostId = details.GetLobbyOwner(ref owner);
            Assert.That(hostId, Is.Not.Null, "P2P host product user ID");
            Assert.That(hostId.IsValid(), Is.True, "P2P host product user ID validity");
            Assert.That(hostId.ToString(), Is.Not.EqualTo(session.UserId.ToString()), "Distinct P2P host user");

            Result? joinResult = null;
            var join = new JoinLobbyOptions
            {
                LobbyDetailsHandle = details,
                LocalUserId = session.UserId,
                PresenceEnabled = false
            };
            lobby.JoinLobby(ref join, null, (ref JoinLobbyCallbackInfo info) => joinResult = info.ResultCode);
            deadline = EditorApplication.timeSinceStartup + 25;
            while (!joinResult.HasValue && EditorApplication.timeSinceStartup < deadline)
            {
                session.Platform.Tick();
                yield return null;
            }
            Assert.That(joinResult.HasValue, Is.True, "Joining P2P host lobby timed out");
            Assert.That(joinResult.Value, Is.EqualTo(Result.Success), "Joining P2P host lobby");
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
            Assert.That(p2p.SendPacket(ref send), Is.EqualTo(Result.Success), "Queueing P2P hello");

            var expectedAck = ProbeBytes("ca-m0-ack:", lobbyId);
            var buffer = new byte[256];
            var receive = new ReceivePacketOptions
            {
                LocalUserId = session.UserId,
                MaxDataSizeBytes = (uint)buffer.Length,
                RequestedChannel = ProbeChannel
            };
            bool ackReceived = false;
            deadline = EditorApplication.timeSinceStartup + 180;
            while (!ackReceived && EditorApplication.timeSinceStartup < deadline)
            {
                session.Platform.Tick();
                ProductUserId sender = null;
                var receivedSocket = new SocketId();
                var result = p2p.ReceivePacket(ref receive, ref sender, ref receivedSocket,
                    out var channel, new ArraySegment<byte>(buffer), out var bytesWritten);
                Assert.That(result, Is.EqualTo(Result.Success).Or.EqualTo(Result.NotFound), "Receiving P2P acknowledgement");
                if (result == Result.Success)
                {
                    Assert.That(sender?.ToString(), Is.EqualTo(hostId.ToString()), "P2P acknowledgement sender");
                    Assert.That(receivedSocket.SocketName, Is.EqualTo(socket.SocketName), "P2P acknowledgement socket");
                    Assert.That(channel, Is.EqualTo(ProbeChannel), "P2P acknowledgement channel");
                    Assert.That(bytesWritten, Is.EqualTo((uint)expectedAck.Length), "P2P acknowledgement byte count");
                    for (var i = 0; i < expectedAck.Length; i++)
                        Assert.That(buffer[i], Is.EqualTo(expectedAck[i]), "P2P acknowledgement payload byte " + i);
                    ackReceived = true;
                }
                yield return null;
            }
            Assert.That(ackReceived, Is.True, "No P2P acknowledgement arrived from the host");

            Result? leaveResult = null;
            var leave = new LeaveLobbyOptions { LocalUserId = session.UserId, LobbyId = lobbyId };
            leaveAttempted = true;
            lobby.LeaveLobby(ref leave, null, (ref LeaveLobbyCallbackInfo info) => leaveResult = info.ResultCode);
            deadline = EditorApplication.timeSinceStartup + 25;
            while (!leaveResult.HasValue && EditorApplication.timeSinceStartup < deadline)
            {
                session.Platform.Tick();
                yield return null;
            }
            Assert.That(leaveResult.HasValue, Is.True, "Leaving P2P host lobby timed out");
            Assert.That(leaveResult.Value, Is.EqualTo(Result.Success), "Leaving P2P host lobby");
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
