using System;
using System.Collections;
using System.IO;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

[Category("OnlineEOSConnectivity")]
public sealed class EOSTwoMacLobbyEditorTests
{
    private static IEnumerator Login(EOSProbeSession session)
    {
        var login = session.OpenAndLogin();
        while (login.MoveNext())
            yield return login.Current;
    }

    [UnityTest]
    [Timeout(1080000)]
    public IEnumerator HostWaitsForDistinctGuestAndDestroysLobby()
    {
        const string marker = "Temp/OnlineEOSTwoMacHost.run";
        const string lobbyIdFile = "Temp/OnlineEOSHostLobbyId.txt";
        if (!File.Exists(marker))
            Assert.Ignore("The one-use two-Mac host marker is absent.");
        File.Delete(marker);

        var session = new EOSProbeSession();
        LobbyInterface lobby = null;
        Utf8String lobbyId = null;
        ulong notificationId = 0;
        bool destroyAttempted = false;
        try
        {
            var login = Login(session);
            while (login.MoveNext())
                yield return login.Current;

            lobby = session.Platform.GetLobbyInterface();
            Assert.That(lobby, Is.Not.Null, "EOS Lobby interface");
            Result? createResult = null;
            var options = new CreateLobbyOptions
            {
                LocalUserId = session.UserId,
                MaxLobbyMembers = 2,
                PermissionLevel = LobbyPermissionLevel.Joinviapresence,
                PresenceEnabled = false,
                AllowInvites = false,
                BucketId = "counterattack-m0-two-mac",
                DisableHostMigration = true,
                EnableRTCRoom = false
            };
            lobby.CreateLobby(ref options, null, (ref CreateLobbyCallbackInfo info) =>
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
            Assert.That(createResult.Value, Is.EqualTo(Result.Success), "Creating the unadvertised lobby");
            Assert.That(string.IsNullOrEmpty(lobbyId), Is.False, "Lobby ID for the guest");

            bool distinctGuestJoined = false;
            bool distinctGuestLeft = false;
            string guestId = null;
            var notificationOptions = new AddNotifyLobbyMemberStatusReceivedOptions();
            notificationId = lobby.AddNotifyLobbyMemberStatusReceived(ref notificationOptions, null,
                (ref LobbyMemberStatusReceivedCallbackInfo info) =>
                {
                    if (!string.Equals(info.LobbyId, lobbyId, StringComparison.Ordinal) ||
                        info.TargetUserId == null || !info.TargetUserId.IsValid())
                        return;
                    var targetId = info.TargetUserId.ToString();
                    if (info.CurrentStatus == LobbyMemberStatus.Joined &&
                        !string.Equals(targetId, session.UserId.ToString(), StringComparison.Ordinal))
                    {
                        guestId = targetId;
                        distinctGuestJoined = true;
                    }
                    else if ((info.CurrentStatus == LobbyMemberStatus.Left ||
                              info.CurrentStatus == LobbyMemberStatus.Disconnected) &&
                             string.Equals(targetId, guestId, StringComparison.Ordinal))
                    {
                        distinctGuestLeft = true;
                    }
                });
            Assert.That(notificationId, Is.GreaterThan(0UL), "Lobby member notification registration");

            File.WriteAllText(lobbyIdFile, lobbyId);
            deadline = EditorApplication.timeSinceStartup + 900;
            while (!distinctGuestJoined && EditorApplication.timeSinceStartup < deadline)
            {
                session.Platform.Tick();
                yield return null;
            }
            if (distinctGuestJoined)
            {
                deadline = EditorApplication.timeSinceStartup + 30;
                while (!distinctGuestLeft && EditorApplication.timeSinceStartup < deadline)
                {
                    session.Platform.Tick();
                    yield return null;
                }
            }

            Result? destroyResult = null;
            var destroyOptions = new DestroyLobbyOptions { LocalUserId = session.UserId, LobbyId = lobbyId };
            destroyAttempted = true;
            lobby.DestroyLobby(ref destroyOptions, null,
                (ref DestroyLobbyCallbackInfo info) => destroyResult = info.ResultCode);
            deadline = EditorApplication.timeSinceStartup + 25;
            while (!destroyResult.HasValue && EditorApplication.timeSinceStartup < deadline)
            {
                session.Platform.Tick();
                yield return null;
            }

            Assert.That(distinctGuestJoined, Is.True, "A distinct EOS product user did not join within fifteen minutes");
            Assert.That(distinctGuestLeft, Is.True, "The guest did not leave before lobby cleanup");
            Assert.That(destroyResult.HasValue, Is.True, "DestroyLobby callback timed out");
            Assert.That(destroyResult.Value, Is.EqualTo(Result.Success), "Destroying the host lobby");
        }
        finally
        {
            if (lobby != null && !string.IsNullOrEmpty(lobbyId) && !destroyAttempted && session.Platform != null)
            {
                var cleanup = new DestroyLobbyOptions { LocalUserId = session.UserId, LobbyId = lobbyId };
                lobby.DestroyLobby(ref cleanup, null, (ref DestroyLobbyCallbackInfo _) => { });
                for (var i = 0; i < 20; i++)
                    session.Platform.Tick();
            }
            if (notificationId != 0)
                lobby.RemoveNotifyLobbyMemberStatusReceived(notificationId);
            if (File.Exists(lobbyIdFile))
                File.Delete(lobbyIdFile);
            session.Dispose();
        }
    }

    [UnityTest]
    [Timeout(120000)]
    public IEnumerator GuestSearchesAndJoinsHostByLobbyId()
    {
        const string marker = "Temp/OnlineEOSTwoMacGuest.run";
        const string lobbyIdFile = "Temp/OnlineEOSJoinId.txt";
        if (!File.Exists(marker))
            Assert.Ignore("The one-use two-Mac guest marker is absent.");
        File.Delete(marker);
        Assert.That(File.Exists(lobbyIdFile), Is.True, "The local guest lobby-ID file is absent");
        var lobbyId = File.ReadAllText(lobbyIdFile).Trim();
        File.Delete(lobbyIdFile);
        Assert.That(lobbyId.Length, Is.GreaterThan(0).And.LessThanOrEqualTo(256), "Guest lobby-ID length");

        var session = new EOSProbeSession();
        LobbySearch search = null;
        LobbyDetails details = null;
        try
        {
            var login = Login(session);
            while (login.MoveNext())
                yield return login.Current;

            var lobby = session.Platform.GetLobbyInterface();
            Assert.That(lobby, Is.Not.Null, "EOS Lobby interface");
            var createSearchOptions = new CreateLobbySearchOptions { MaxResults = 1 };
            Assert.That(lobby.CreateLobbySearch(ref createSearchOptions, out search),
                Is.EqualTo(Result.Success), "Creating a lobby-ID search");
            Assert.That(search, Is.Not.Null, "Lobby search handle");
            var setIdOptions = new LobbySearchSetLobbyIdOptions { LobbyId = lobbyId };
            Assert.That(search.SetLobbyId(ref setIdOptions), Is.EqualTo(Result.Success), "Setting the lobby ID");

            Result? findResult = null;
            var findOptions = new LobbySearchFindOptions { LocalUserId = session.UserId };
            search.Find(ref findOptions, null, (ref LobbySearchFindCallbackInfo info) => findResult = info.ResultCode);
            var deadline = EditorApplication.timeSinceStartup + 25;
            while (!findResult.HasValue && EditorApplication.timeSinceStartup < deadline)
            {
                session.Platform.Tick();
                yield return null;
            }
            Assert.That(findResult.HasValue, Is.True, "LobbySearch.Find callback timed out");
            Assert.That(findResult.Value, Is.EqualTo(Result.Success), "Finding the host lobby");
            var countOptions = new LobbySearchGetSearchResultCountOptions();
            Assert.That(search.GetSearchResultCount(ref countOptions), Is.EqualTo(1U), "Exact lobby-ID search result");
            var indexOptions = new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = 0 };
            Assert.That(search.CopySearchResultByIndex(ref indexOptions, out details),
                Is.EqualTo(Result.Success), "Copying the host lobby details");
            Assert.That(details, Is.Not.Null, "Host lobby details handle");

            Result? joinResult = null;
            var joinOptions = new JoinLobbyOptions
            {
                LobbyDetailsHandle = details,
                LocalUserId = session.UserId,
                PresenceEnabled = false
            };
            lobby.JoinLobby(ref joinOptions, null, (ref JoinLobbyCallbackInfo info) => joinResult = info.ResultCode);
            deadline = EditorApplication.timeSinceStartup + 25;
            while (!joinResult.HasValue && EditorApplication.timeSinceStartup < deadline)
            {
                session.Platform.Tick();
                yield return null;
            }

            Result? leaveResult = null;
            if (joinResult == Result.Success)
            {
                deadline = EditorApplication.timeSinceStartup + 2;
                while (EditorApplication.timeSinceStartup < deadline)
                {
                    session.Platform.Tick();
                    yield return null;
                }
                var leaveOptions = new LeaveLobbyOptions { LocalUserId = session.UserId, LobbyId = lobbyId };
                lobby.LeaveLobby(ref leaveOptions, null,
                    (ref LeaveLobbyCallbackInfo info) => leaveResult = info.ResultCode);
                deadline = EditorApplication.timeSinceStartup + 25;
                while (!leaveResult.HasValue && EditorApplication.timeSinceStartup < deadline)
                {
                    session.Platform.Tick();
                    yield return null;
                }
            }

            Assert.That(joinResult.HasValue, Is.True, "JoinLobby callback timed out");
            Assert.That(joinResult.Value, Is.EqualTo(Result.Success), "Joining the host lobby");
            Assert.That(leaveResult.HasValue, Is.True, "LeaveLobby callback timed out");
            Assert.That(leaveResult.Value, Is.EqualTo(Result.Success), "Leaving the host lobby");
        }
        finally
        {
            details?.Release();
            search?.Release();
            session.Dispose();
        }
    }
}
