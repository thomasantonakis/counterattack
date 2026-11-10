using System;
using System.Collections;
using System.IO;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.Platform;
using NUnit.Framework;
using PlayEveryWare.EpicOnlineServices;
using PlayEveryWare.EpicOnlineServices.Utility;
using UnityEditor;
using UnityEngine.TestTools;

[Category("OnlineEOSConnectivity")]
public sealed class EOSPrivateLobbyEditorTests
{
    [UnityTest]
    [Timeout(120000)]
    public IEnumerator DeviceIdentityCanCreateAndDestroyPrivateLobby()
    {
        var runMarker = Path.Combine("Temp", "OnlineEOSLobby.run");
        if (!File.Exists(runMarker))
            Assert.Ignore("The one-use local EOS lobby marker is absent.");
        File.Delete(runMarker);

        MacOSPlatformSpecifics.Register();
        EOSManager.EOSSingleton.LoadEOSLibraries();
        var initializeOptions = ConfigurationUtility.GetEOSInitializeOptions();
        var initializeResult = PlatformInterface.Initialize(ref initializeOptions.options);
        if (initializeResult == Result.AlreadyConfigured)
            Assert.Inconclusive("EOS is already configured in this Editor process; reopen Unity before this isolated probe.");
        Assert.That(initializeResult, Is.EqualTo(Result.Success), "EOS native initialization");

        PlatformInterface platform = null;
        try
        {
            var createOptions = ConfigurationUtility.GetEOSCreateOptions();
            platform = PlatformInterface.Create(ref createOptions.options);
            Assert.That(platform, Is.Not.Null, "EOS platform creation");

            var connect = platform.GetConnectInterface();
            Assert.That(connect, Is.Not.Null, "EOS Connect interface");
            Result? deviceResult = null;
            var deviceOptions = new CreateDeviceIdOptions { DeviceModel = "Counterattack M0 Mac" };
            connect.CreateDeviceId(ref deviceOptions, null,
                (ref CreateDeviceIdCallbackInfo info) => deviceResult = info.ResultCode);
            var deadline = EditorApplication.timeSinceStartup + 25;
            while (!deviceResult.HasValue && EditorApplication.timeSinceStartup < deadline)
            {
                platform.Tick();
                yield return null;
            }
            Assert.That(deviceResult.HasValue, Is.True, "CreateDeviceId callback timed out");
            Assert.That(deviceResult.Value, Is.EqualTo(Result.Success).Or.EqualTo(Result.DuplicateNotAllowed),
                "Creating or reusing the local device identity");

            var loginOptions = new LoginOptions
            {
                Credentials = new Credentials { Type = ExternalCredentialType.DeviceidAccessToken },
                UserLoginInfo = new UserLoginInfo { DisplayName = "Counterattack M0" }
            };
            LoginCallbackInfo? login = null;
            connect.Login(ref loginOptions, null, (ref LoginCallbackInfo info) => login = info);
            deadline = EditorApplication.timeSinceStartup + 25;
            while (!login.HasValue && EditorApplication.timeSinceStartup < deadline)
            {
                platform.Tick();
                yield return null;
            }
            Assert.That(login.HasValue, Is.True, "Connect.Login callback timed out");
            if (login.Value.ResultCode == Result.InvalidUser)
            {
                Result? createUserResult = null;
                var userOptions = new CreateUserOptions { ContinuanceToken = login.Value.ContinuanceToken };
                connect.CreateUser(ref userOptions, null,
                    (ref CreateUserCallbackInfo info) => createUserResult = info.ResultCode);
                deadline = EditorApplication.timeSinceStartup + 25;
                while (!createUserResult.HasValue && EditorApplication.timeSinceStartup < deadline)
                {
                    platform.Tick();
                    yield return null;
                }
                Assert.That(createUserResult.HasValue, Is.True, "Connect.CreateUser callback timed out");
                Assert.That(createUserResult.Value, Is.EqualTo(Result.Success), "Creating the test product user");

                login = null;
                connect.Login(ref loginOptions, null, (ref LoginCallbackInfo info) => login = info);
                deadline = EditorApplication.timeSinceStartup + 25;
                while (!login.HasValue && EditorApplication.timeSinceStartup < deadline)
                {
                    platform.Tick();
                    yield return null;
                }
                Assert.That(login.HasValue, Is.True, "Connect.Login retry timed out");
            }
            Assert.That(login.Value.ResultCode, Is.EqualTo(Result.Success), "EOS Connect login");
            Assert.That(login.Value.LocalUserId, Is.Not.Null, "Authenticated product user ID");
            Assert.That(login.Value.LocalUserId.IsValid(), Is.True, "Authenticated product user ID validity");

            var lobby = platform.GetLobbyInterface();
            Assert.That(lobby, Is.Not.Null, "EOS Lobby interface");
            Result? createResult = null;
            Utf8String lobbyId = null;
            var lobbyOptions = new CreateLobbyOptions
            {
                LocalUserId = login.Value.LocalUserId,
                MaxLobbyMembers = 2,
                PermissionLevel = LobbyPermissionLevel.Inviteonly,
                PresenceEnabled = false,
                AllowInvites = false,
                BucketId = "counterattack-m0",
                DisableHostMigration = true,
                EnableRTCRoom = false,
                EnableJoinById = true
            };
            lobby.CreateLobby(ref lobbyOptions, null, (ref CreateLobbyCallbackInfo info) =>
            {
                createResult = info.ResultCode;
                lobbyId = info.LobbyId;
            });
            deadline = EditorApplication.timeSinceStartup + 25;
            while (!createResult.HasValue && EditorApplication.timeSinceStartup < deadline)
            {
                platform.Tick();
                yield return null;
            }

            Result? destroyResult = null;
            if (createResult == Result.Success && !string.IsNullOrEmpty(lobbyId))
            {
                var destroyOptions = new DestroyLobbyOptions
                {
                    LocalUserId = login.Value.LocalUserId,
                    LobbyId = lobbyId
                };
                lobby.DestroyLobby(ref destroyOptions, null,
                    (ref DestroyLobbyCallbackInfo info) => destroyResult = info.ResultCode);
                deadline = EditorApplication.timeSinceStartup + 25;
                while (!destroyResult.HasValue && EditorApplication.timeSinceStartup < deadline)
                {
                    platform.Tick();
                    yield return null;
                }
            }

            Assert.That(createResult.HasValue, Is.True, "CreateLobby callback timed out");
            Assert.That(createResult.Value, Is.EqualTo(Result.Success), "Creating a private lobby");
            Assert.That(string.IsNullOrEmpty(lobbyId), Is.False, "Private lobby ID");
            Assert.That(destroyResult.HasValue, Is.True, "DestroyLobby callback timed out");
            Assert.That(destroyResult.Value, Is.EqualTo(Result.Success), "Destroying the private lobby");
        }
        finally
        {
            platform?.Release();
            Assert.That(PlatformInterface.Shutdown(), Is.EqualTo(Result.Success), "EOS native shutdown");
        }
    }
}
