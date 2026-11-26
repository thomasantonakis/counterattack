using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CounterAttack.Online;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.P2P;
using UnityEngine;

internal sealed class EOSStandalonePacketProbe
{
    internal const byte ManagerProbeChannel = 7;
    internal const byte WatchProbeChannel = 41;
    internal const string WatchLobbyFileName = "watch-lobby-id.txt";
    internal const string LobbyBucket = "counterattack-m0-p2p";
    private const string WatchLobbyBucket = "counterattack-watch-proof";
    internal const string LiveWatchLobbyBucket = "counterattack-watch-v1";

    private static IEnumerator Login(EOSStandaloneSession session)
    {
        var login = session.OpenAndLogin();
        while (login.MoveNext())
            yield return login.Current;
    }

    internal static SocketId ProbeSocket(string lobbyId, bool watchLobby) =>
        new SocketId { SocketName = (watchLobby ? "ca-watch-" : "ca-m0-") + lobbyId.Substring(0, Math.Min(watchLobby ? 23 : 24, lobbyId.Length)) };

    private static byte[] ProbeBytes(string prefix, string lobbyId) =>
        Encoding.ASCII.GetBytes(prefix + lobbyId);

    private static void RequireRelayControl(P2PInterface p2p,RelayControl policy)
    {
        var set = new SetRelayControlOptions { RelayControl = policy };
        EOSProofCheck.Require(p2p.SetRelayControl(ref set) == Result.Success, "Setting EOS relay routing");
        var get = new GetRelayControlOptions();
        EOSProofCheck.Require(p2p.GetRelayControl(ref get, out var configured) == Result.Success, "Reading EOS relay control");
        EOSProofCheck.Require(configured == policy, "EOS relay control setting");
    }

    private static AttributeData JoinCodeAttribute(string code) =>
        new AttributeData { Key = OnlineFrontendSessionPolicy.LobbyCodeAttribute, Value = code };

    internal static IEnumerator<object> PublishJoinCode(EOSStandaloneSession session, LobbyInterface lobby,
        Utf8String lobbyId, string code)
    {
        var options = new UpdateLobbyModificationOptions { LocalUserId = session.UserId, LobbyId = lobbyId };
        EOSProofCheck.Require(lobby.UpdateLobbyModification(ref options, out var modification) == Result.Success,
            "Preparing the EOS lobby code");
        try
        {
            var attribute = new LobbyModificationAddAttributeOptions
            {
                Attribute = JoinCodeAttribute(code),
                Visibility = LobbyAttributeVisibility.Public
            };
            EOSProofCheck.Require(modification.AddAttribute(ref attribute) == Result.Success, "Adding the EOS lobby code");
            var permission = new LobbyModificationSetPermissionLevelOptions
            {
                PermissionLevel = LobbyPermissionLevel.Publicadvertised
            };
            EOSProofCheck.Require(modification.SetPermissionLevel(ref permission) == Result.Success,
                "Making the lobby discoverable by code");

            Result? result = null;
            var update = new UpdateLobbyOptions { LobbyModificationHandle = modification };
            lobby.UpdateLobby(ref update, null, (ref UpdateLobbyCallbackInfo info) => result = info.ResultCode);
            var deadline = Time.realtimeSinceStartupAsDouble + 25;
            while (!result.HasValue && Time.realtimeSinceStartupAsDouble < deadline)
            {
                session.Tick();
                yield return null;
            }
            EOSProofCheck.Require(result.HasValue, "Publishing the EOS lobby code timed out");
            EOSProofCheck.Require(result == Result.Success, "Publishing the EOS lobby code");
        }
        finally
        {
            modification.Release();
        }
    }

    internal static IEnumerator HostProbe(string outputDirectory,
        Func<EOSStandaloneSession, ProductUserId, SocketId, string, IEnumerator> exchange = null, RelayControl relayControl=RelayControl.ForceRelays,
        Action<string> onCodePublished = null, EOSStandaloneSession watchOwner = null,
        EOSWatchLobbyProof.Admission admission = null, Action<string> onLobbyCreated = null,
        OnlineSpectatorAdmission liveAdmission = null, Action<EOSStandaloneSession, string> openViewing = null,
        Action closeViewing = null, string requiredHost = null, string requiredGuest = null)
    {
        bool watchLobby = watchOwner != null;
        byte probeChannel = watchLobby ? WatchProbeChannel : ManagerProbeChannel;
        var lobbyIdFile = Path.Combine(outputDirectory, watchLobby ? WatchLobbyFileName : "lobby-id.txt");

        var session = watchOwner ?? new EOSStandaloneSession();
        LobbyInterface lobby = null;
        P2PInterface p2p = null;
        Utf8String lobbyId = null;
        ulong lobbyNotification = 0;
        ulong connectionNotification = 0;
        ProductUserId acceptedGuest = null;
        bool lobbyDestroyed = false;
        try
        {
            if (!watchLobby)
            {
                var login = Login(session);
                while (login.MoveNext())
                    yield return login.Current;
            }

            if (requiredHost != null && session.UserId.ToString() != requiredHost)
                throw new InvalidOperationException("Use the original host's EOS profile to load this match.");

            lobby = session.Platform.GetLobbyInterface();
            p2p = session.Platform.GetP2PInterface();
            EOSProofCheck.Require(lobby != null, "EOS Lobby interface");
            EOSProofCheck.Require(p2p != null, "EOS P2P interface");
            RequireRelayControl(p2p,relayControl);

            Result? createResult = null;
            var create = new CreateLobbyOptions
            {
                LocalUserId = session.UserId,
                MaxLobbyMembers = 2,
                PermissionLevel = LobbyPermissionLevel.Joinviapresence,
                PresenceEnabled = false,
                AllowInvites = false,
                BucketId = watchLobby ? WatchLobbyBucket : LobbyBucket,
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
                session.Tick();
                yield return null;
            }
            EOSProofCheck.Require(createResult.HasValue, "CreateLobby callback timed out");
            EOSProofCheck.Require(createResult.Value == Result.Success, "Creating the P2P coordination lobby");
            EOSProofCheck.Require(!(string.IsNullOrEmpty(lobbyId)), "P2P coordination lobby ID");

            var lobbyIdText = lobbyId.ToString();
            onLobbyCreated?.Invoke(lobbyIdText);
            var socket = ProbeSocket(lobbyIdText, watchLobby);
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
                        !string.Equals(target, session.UserId.ToString(), StringComparison.Ordinal) &&
                        (liveAdmission == null || acceptedGuest == null))
                    {
                        if (requiredGuest != null && target != requiredGuest)
                        {
                            var kick = new KickMemberOptions { LobbyId = lobbyId, LocalUserId = session.UserId,
                                TargetUserId = info.TargetUserId };
                            lobby.KickMember(ref kick, null, (ref KickMemberCallbackInfo result) =>
                            {
                                if (result.ResultCode != Result.Success)
                                    Debug.LogWarning("Could not remove a manager from the reserved resume lobby: " + result.ResultCode);
                            });
                        }
                        else
                        {
                            joinedGuestId = target;
                            guestLeft = false;
                        }
                    }
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
                        (requiredGuest == null || info.RemoteUserId.ToString() == requiredGuest) &&
                        info.SocketId.HasValue &&
                        string.Equals(info.SocketId.Value.SocketName, socket.SocketName, StringComparison.Ordinal))
                        requestedGuestId = info.RemoteUserId.ToString();
                });
            EOSProofCheck.Require(connectionNotification > 0UL, "P2P connection request notification");

            if (onCodePublished != null)
            {
                var code = liveAdmission?.MatchCode ?? OnlineFrontendSessionPolicy.GenerateLobbyCode();
                var lookup = liveAdmission != null ? OnlineSpectatorAdmission.Lookup(code) : code;
                using var publish = PublishJoinCode(session, lobby, lobbyId, lookup);
                while (publish.MoveNext())
                    yield return publish.Current;
                if (liveAdmission != null && requiredGuest != null)
                    PublishAwayReservation(session, lobby, lobbyId, code, requiredGuest);
                onCodePublished(code);
            }

            openViewing?.Invoke(session, lobbyIdText);
            File.WriteAllText(lobbyIdFile, admission?.Handoff(lobbyIdText) ?? lobbyIdText);
            if (liveAdmission != null)
            {
                var admit = AdmitLiveManager(session, lobby, p2p, lobbyId, socket, liveAdmission,
                    identity => acceptedGuest = identity);
                while (admit.MoveNext()) yield return admit.Current;
                joinedGuestId = acceptedGuest.ToString();
                guestLeft = false;
            }
            else
            {
                deadline = Time.realtimeSinceStartupAsDouble + 900;
                while ((joinedGuestId == null || requestedGuestId == null) &&
                       Time.realtimeSinceStartupAsDouble < deadline)
                {
                    session.Tick();
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
                    RequestedChannel = probeChannel
                };
                bool helloReceived = false;
                deadline = Time.realtimeSinceStartupAsDouble + 180;
                while (!helloReceived && Time.realtimeSinceStartupAsDouble < deadline)
                {
                    session.Tick();
                    ProductUserId sender = null;
                    var receivedSocket = new SocketId();
                    var result = p2p.ReceivePacket(ref receiveOptions, ref sender, ref receivedSocket,
                        out var channel, new ArraySegment<byte>(buffer), out var bytesWritten);
                    EOSProofCheck.Require(result == Result.Success || result == Result.NotFound, "Receiving P2P hello");
                    if (result == Result.Success)
                    {
                        if (requiredGuest != null && sender?.ToString() != requiredGuest) continue;
                        EOSProofCheck.Require(sender?.ToString() == joinedGuestId, "P2P hello sender");
                        EOSProofCheck.Require(receivedSocket.SocketName == socket.SocketName, "P2P hello socket");
                        EOSProofCheck.Require(channel == probeChannel, "P2P hello channel");
                        int admissionLength = admission == null ? 0 : OnlineSpectatorAdmission.CodeLength;
                        EOSProofCheck.Require(bytesWritten == (uint)(expectedHello.Length + admissionLength), "P2P hello byte count");
                        for (var i = 0; i < expectedHello.Length; i++)
                            EOSProofCheck.Require(buffer[i] == expectedHello[i], "P2P hello payload byte " + i);
                        if (admission != null)
                        {
                            var code = Encoding.ASCII.GetString(buffer, expectedHello.Length, admissionLength);
                            EOSProofCheck.Require(admission.TryAdmit(joinedGuestId, code), "Match Code or role admission rejected");
                        }
                        helloReceived = true;
                    }
                    yield return null;
                }
                EOSProofCheck.Require(helloReceived, "No P2P hello arrived from the joined guest");

            }

            var ack = ProbeBytes("ca-m0-ack:", lobbyIdText);
            var send = new SendPacketOptions
            {
                LocalUserId = session.UserId,
                RemoteUserId = acceptedGuest,
                SocketId = socket,
                Channel = probeChannel,
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
                session.Tick();
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
                    session.Tick();
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
            closeViewing?.Invoke();
            if (p2p != null && acceptedGuest != null && session.Platform != null)
            {
                admission?.Release(acceptedGuest.ToString());
                var close = new CloseConnectionOptions
                {
                    LocalUserId = session.UserId,
                    RemoteUserId = acceptedGuest,
                    SocketId = ProbeSocket(lobbyId.ToString(), watchLobby)
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
                // Teardown pumps outstanding native callbacks without starting
                // another renewal or throwing its health error before Dispose.
                for (var i = 0; i < 20; i++)
                    session.Platform.Tick();
            }
            if (File.Exists(lobbyIdFile))
                File.Delete(lobbyIdFile);
            if (!watchLobby)
                session.Dispose();
        }
    }

    private static IEnumerator AdmitLiveManager(EOSStandaloneSession session, LobbyInterface lobby,
        P2PInterface p2p, Utf8String lobbyId, SocketId socket,
        OnlineSpectatorAdmission admission, Action<ProductUserId> admitted)
    {
        var buffer = new byte[256];
        var receive = new ReceivePacketOptions
        {
            LocalUserId = session.UserId, MaxDataSizeBytes = (uint)buffer.Length, RequestedChannel = ManagerProbeChannel
        };
        string prefix = "ca-m0-hello:" + lobbyId;
        string candidate = null;
        string pendingCode = null;
        double candidateDeadline = 0, nextInspection = 0;
        double deadline = Time.realtimeSinceStartupAsDouble + 900;
        while (Time.realtimeSinceStartupAsDouble < deadline)
        {
            session.Tick();
            double now = Time.realtimeSinceStartupAsDouble;
            if (now >= nextInspection)
            {
                nextInspection = now + .25;
                var members = EosLiveSpectators.ReadMembers(session, lobby, lobbyId);
                if (members != null)
                {
                    string member = System.Linq.Enumerable.FirstOrDefault(members);
                    if (member != candidate)
                    {
                        candidate = member;
                        pendingCode = null;
                        candidateDeadline = now + 12;
                    }
                    if (candidate != null && now >= candidateDeadline)
                    {
                        RejectManager(session, lobby, p2p, lobbyId, socket, ProductUserId.FromString(candidate));
                        candidate = null;
                        pendingCode = null;
                    }
                    if (candidate != null)
                    {
                        var accept = new AcceptConnectionOptions
                        {
                            LocalUserId = session.UserId, RemoteUserId = ProductUserId.FromString(candidate), SocketId = socket
                        };
                        p2p.AcceptConnection(ref accept);
                    }
                }
            }
            if (pendingCode != null && admission.TryAdmitAway(candidate, pendingCode))
            {
                PublishAwayReservation(session, lobby, lobbyId, admission.MatchCode, candidate);
                admitted(ProductUserId.FromString(candidate));
                yield break;
            }
            for (int i = 0; i < 8; i++)
            {
                ProductUserId sender = null;
                var actualSocket = new SocketId();
                var result = p2p.ReceivePacket(ref receive, ref sender, ref actualSocket, out var channel,
                    new ArraySegment<byte>(buffer), out var size);
                if (result == Result.NotFound) break;
                if (result != Result.Success || candidate == null || candidate != sender?.ToString()
                    || channel != ManagerProbeChannel || actualSocket.SocketName != socket.SocketName) continue;
                string hello = Encoding.ASCII.GetString(buffer, 0, (int)size);
                string code = hello.Length == prefix.Length + OnlineSpectatorAdmission.CodeLength
                    && hello.StartsWith(prefix, StringComparison.Ordinal) ? hello.Substring(prefix.Length) : null;
                if (admission.TryAdmitAway(candidate, code, out bool waitingForViewerLeave))
                {
                    PublishAwayReservation(session, lobby, lobbyId, admission.MatchCode, candidate);
                    admitted(sender);
                    yield break;
                }
                if (waitingForViewerLeave)
                {
                    pendingCode = code;
                    continue;
                }
                RejectManager(session, lobby, p2p, lobbyId, socket, sender);
                candidate = null;
                pendingCode = null;
            }
            yield return null;
        }
        throw new EOSProofFailure("No Away manager presented a valid Match Code");
    }

    private static void PublishAwayReservation(EOSStandaloneSession session, LobbyInterface lobby, Utf8String lobbyId,
        string matchCode, string identity)
    {
        var options = new UpdateLobbyModificationOptions { LocalUserId = session.UserId, LobbyId = lobbyId };
        if (lobby.UpdateLobbyModification(ref options, out var modification) != Result.Success) return;
        try
        {
            var attribute = new LobbyModificationAddAttributeOptions
            {
                Visibility = LobbyAttributeVisibility.Public,
                Attribute = new AttributeData
                {
                    Key = OnlineSpectatorAdmission.AwayReservationAttribute,
                    Value = OnlineSpectatorAdmission.AwayReservation(matchCode, identity)
                }
            };
            if (modification.AddAttribute(ref attribute) != Result.Success) return;
            var update = new UpdateLobbyOptions { LobbyModificationHandle = modification };
            lobby.UpdateLobby(ref update, null, (ref UpdateLobbyCallbackInfo _) => { });
        }
        finally { modification.Release(); }
    }

    private static void RejectManager(EOSStandaloneSession session, LobbyInterface lobby, P2PInterface p2p,
        Utf8String lobbyId, SocketId socket, ProductUserId peer)
    {
        var close = new CloseConnectionOptions { LocalUserId = session.UserId, RemoteUserId = peer, SocketId = socket };
        p2p.CloseConnection(ref close);
        var kick = new KickMemberOptions { LocalUserId = session.UserId, LobbyId = lobbyId, TargetUserId = peer };
        lobby.KickMember(ref kick, null, (ref KickMemberCallbackInfo _) => { });
    }

    internal static IEnumerator GuestProbe(string inputLobby,
        Func<EOSStandaloneSession, ProductUserId, SocketId, string, IEnumerator> exchange = null,
        RelayControl relayControl = RelayControl.ForceRelays, bool searchByCode = false, bool watchLobby = false,
        string matchCode = null)
    {
        byte probeChannel = watchLobby ? WatchProbeChannel : ManagerProbeChannel;
        string lobbyId = searchByCode ? null : inputLobby;
        string lookupCode = null;
        EOSProofCheck.Require(searchByCode ? OnlineFrontendSessionPolicy.TryNormalizeLobbyCode(inputLobby, out lookupCode) :
            inputLobby != null && inputLobby.Length == 32,
            searchByCode ? OnlineFrontendSessionPolicy.LobbyCodePrompt : "Expected a 32-character lobby ID");
        if (searchByCode)
        {
            matchCode = lookupCode;
            if (watchLobby)
                matchCode += OnlineSpectatorAdmission.ViewerCompatibilitySuffix;
        }

        var session = new EOSStandaloneSession();
        LobbySearch search = null;
        LobbyDetails details = null;
        P2PInterface p2p = null;
        ProductUserId hostId = null;
        bool joined = false;
        ulong membershipNotification = 0;
        bool removed = false;
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
            RequireRelayControl(p2p,relayControl);
            var createSearch = new CreateLobbySearchOptions { MaxResults = searchByCode ? 2U : 1U };
            EOSProofCheck.Require(lobby.CreateLobbySearch(ref createSearch, out search) == Result.Success,
                "Creating an EOS lobby search");
            if (searchByCode)
            {
                var parameter = new LobbySearchSetParameterOptions
                {
                    Parameter = JoinCodeAttribute(OnlineSpectatorAdmission.Lookup(lookupCode)),
                    ComparisonOp = ComparisonOp.Equal
                };
                EOSProofCheck.Require(search.SetParameter(ref parameter) == Result.Success, "Searching by the lobby code");
                var bucket = new LobbySearchSetParameterOptions
                {
                    Parameter = new AttributeData { Key = LobbyInterface.SEARCH_BUCKET_ID, Value = watchLobby ? LiveWatchLobbyBucket : LobbyBucket },
                    ComparisonOp = ComparisonOp.Equal
                };
                EOSProofCheck.Require(search.SetParameter(ref bucket) == Result.Success, "Scoping the lobby-code search");
            }
            else
            {
                var setId = new LobbySearchSetLobbyIdOptions { LobbyId = lobbyId };
                EOSProofCheck.Require(search.SetLobbyId(ref setId) == Result.Success, "Setting the P2P lobby ID");
            }

            var deadline = Time.realtimeSinceStartupAsDouble + 25;
            var retryUntil = Time.realtimeSinceStartupAsDouble + (watchLobby ? 12 : 5);
            uint matches;
            do
            {
                Result? findResult = null;
                var find = new LobbySearchFindOptions { LocalUserId = session.UserId };
                search.Find(ref find, null, (ref LobbySearchFindCallbackInfo info) => findResult = info.ResultCode);
                while (!findResult.HasValue && Time.realtimeSinceStartupAsDouble < deadline)
                {
                    session.Tick();
                    yield return null;
                }
                EOSProofCheck.Require(findResult.HasValue, "EOS lobby search timed out");
                EOSProofCheck.Require(findResult.Value == Result.Success, "Finding the EOS host lobby");
                var count = new LobbySearchGetSearchResultCountOptions();
                matches = search.GetSearchResultCount(ref count);
                if (matches != 0 || !searchByCode || Time.realtimeSinceStartupAsDouble >= retryUntil)
                    break;

                var retryAt = Time.realtimeSinceStartupAsDouble + 1;
                while (Time.realtimeSinceStartupAsDouble < retryAt)
                {
                    session.Tick();
                    yield return null;
                }
            } while (Time.realtimeSinceStartupAsDouble < deadline);

            if (searchByCode)
                OnlineFrontendSessionPolicy.RequireUniqueLobbyCodeMatch(matches);
            else
                EOSProofCheck.Require(matches == 1U, "Exact P2P lobby-ID result");
            var index = new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = 0 };
            EOSProofCheck.Require(search.CopySearchResultByIndex(ref index, out details) == Result.Success, "Copying P2P host lobby details");
            EOSProofCheck.Require(details != null, "P2P host lobby details");
            if (searchByCode)
            {
                var copyInfo = new LobbyDetailsCopyInfoOptions();
                EOSProofCheck.Require(details.CopyInfo(ref copyInfo, out var info) == Result.Success,
                    "Reading the resolved EOS lobby ID");
                lobbyId = info?.LobbyId?.ToString();
                EOSProofCheck.Require(!string.IsNullOrEmpty(lobbyId), "Resolved EOS lobby ID is missing");
            }
            var owner = new LobbyDetailsGetLobbyOwnerOptions();
            hostId = details.GetLobbyOwner(ref owner);
            EOSProofCheck.Require(hostId != null, "P2P host product user ID");
            EOSProofCheck.Require(hostId.IsValid(), "P2P host product user ID validity");
            EOSProofCheck.Require(hostId.ToString() != session.UserId.ToString(), "Distinct P2P host user");

            var memberOptions = new AddNotifyLobbyMemberStatusReceivedOptions();
            membershipNotification = lobby.AddNotifyLobbyMemberStatusReceived(ref memberOptions, null,
                (ref LobbyMemberStatusReceivedCallbackInfo info) =>
                {
                    if (string.Equals(info.LobbyId, lobbyId, StringComparison.Ordinal)
                        && info.TargetUserId?.ToString() == session.UserId.ToString()
                        && info.CurrentStatus == LobbyMemberStatus.Kicked)
                        removed = true;
                });
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
                session.Tick();
                yield return null;
            }
            EOSProofCheck.Require(joinResult.HasValue, "Joining P2P host lobby timed out");
            EOSProofCheck.Require(joinResult.Value == Result.Success, "Joining P2P host lobby: " + joinResult.Value);
            joined = true;

            var socket = ProbeSocket(lobbyId, watchLobby);
            var hello = ProbeBytes("ca-m0-hello:", lobbyId + matchCode);
            var send = new SendPacketOptions
            {
                LocalUserId = session.UserId,
                RemoteUserId = hostId,
                SocketId = socket,
                Channel = probeChannel,
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
                RequestedChannel = probeChannel
            };
            bool ackReceived = false;
            deadline = Time.realtimeSinceStartupAsDouble + (searchByCode ? 18 : 180);
            while (!ackReceived && Time.realtimeSinceStartupAsDouble < deadline)
            {
                session.Tick();
                if (removed)
                    throw new InvalidOperationException(watchLobby ? "The host declined the spectator join."
                        : searchByCode && OnlineMatchRoleLookup.IsAwayReservedForSomeoneElse(details, lookupCode, session.UserId)
                            ? "Away is reserved for the original manager."
                            : "The host declined the Away join. Try again.");
                ProductUserId sender = null;
                var receivedSocket = new SocketId();
                var result = p2p.ReceivePacket(ref receive, ref sender, ref receivedSocket,
                    out var channel, new ArraySegment<byte>(buffer), out var bytesWritten);
                EOSProofCheck.Require(result == Result.Success || result == Result.NotFound, "Receiving P2P acknowledgement");
                if (result == Result.Success)
                {
                    EOSProofCheck.Require(sender?.ToString() == hostId.ToString(), "P2P acknowledgement sender");
                    EOSProofCheck.Require(receivedSocket.SocketName == socket.SocketName, "P2P acknowledgement socket");
                    EOSProofCheck.Require(channel == probeChannel, "P2P acknowledgement channel");
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
                session.Tick();
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
                    SocketId = ProbeSocket(lobbyId, watchLobby)
                };
                p2p.CloseConnection(ref close);
            }
            if (membershipNotification != 0 && session.Platform != null)
                session.Platform.GetLobbyInterface().RemoveNotifyLobbyMemberStatusReceived(membershipNotification);
            if (joined && !removed && !leaveAttempted && session.Platform != null)
            {
                var lobby = session.Platform.GetLobbyInterface();
                var leave = new LeaveLobbyOptions { LocalUserId = session.UserId, LobbyId = lobbyId };
                lobby.LeaveLobby(ref leave, null, (ref LeaveLobbyCallbackInfo _) => { });
                // Teardown pumps outstanding native callbacks without starting
                // another renewal or throwing its health error before Dispose.
                for (var i = 0; i < 20; i++)
                    session.Platform.Tick();
            }
            details?.Release();
            search?.Release();
            session.Dispose();
        }
    }
}
