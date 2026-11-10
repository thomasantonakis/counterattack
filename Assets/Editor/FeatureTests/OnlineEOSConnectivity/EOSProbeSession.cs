using System;
using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Platform;
using NUnit.Framework;
using PlayEveryWare.EpicOnlineServices;
using PlayEveryWare.EpicOnlineServices.Utility;
using UnityEditor;

// One EOS SDK lifetime per Editor process. Only focused, one-use probes use this helper.
internal sealed class EOSProbeSession : IDisposable
{
    private bool ownsInitialization;

    public PlatformInterface Platform { get; private set; }
    public ProductUserId UserId { get; private set; }

    public IEnumerator OpenAndLogin()
    {
        MacOSPlatformSpecifics.Register();
        EOSManager.EOSSingleton.LoadEOSLibraries();
        var initializeOptions = ConfigurationUtility.GetEOSInitializeOptions();
        var initializeResult = PlatformInterface.Initialize(ref initializeOptions.options);
        if (initializeResult == Result.AlreadyConfigured)
            Assert.Inconclusive("EOS is already configured; reopen Unity before this isolated probe.");
        Assert.That(initializeResult, Is.EqualTo(Result.Success), "EOS native initialization");
        ownsInitialization = true;

        var createOptions = ConfigurationUtility.GetEOSCreateOptions();
        Platform = PlatformInterface.Create(ref createOptions.options);
        Assert.That(Platform, Is.Not.Null, "EOS platform creation");

        var connect = Platform.GetConnectInterface();
        Assert.That(connect, Is.Not.Null, "EOS Connect interface");
        Result? deviceResult = null;
        var deviceOptions = new CreateDeviceIdOptions { DeviceModel = "Counterattack M0 Mac" };
        connect.CreateDeviceId(ref deviceOptions, null,
            (ref CreateDeviceIdCallbackInfo info) => deviceResult = info.ResultCode);
        var deadline = EditorApplication.timeSinceStartup + 25;
        while (!deviceResult.HasValue && EditorApplication.timeSinceStartup < deadline)
        {
            Platform.Tick();
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
            Platform.Tick();
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
                Platform.Tick();
                yield return null;
            }
            Assert.That(createUserResult.HasValue, Is.True, "Connect.CreateUser callback timed out");
            Assert.That(createUserResult.Value, Is.EqualTo(Result.Success), "Creating the test product user");

            login = null;
            connect.Login(ref loginOptions, null, (ref LoginCallbackInfo info) => login = info);
            deadline = EditorApplication.timeSinceStartup + 25;
            while (!login.HasValue && EditorApplication.timeSinceStartup < deadline)
            {
                Platform.Tick();
                yield return null;
            }
            Assert.That(login.HasValue, Is.True, "Connect.Login retry timed out");
        }

        Assert.That(login.Value.ResultCode, Is.EqualTo(Result.Success), "EOS Connect login");
        Assert.That(login.Value.LocalUserId, Is.Not.Null, "Authenticated product user ID");
        Assert.That(login.Value.LocalUserId.IsValid(), Is.True, "Authenticated product user ID validity");
        UserId = login.Value.LocalUserId;
    }

    public void Dispose()
    {
        Platform?.Release();
        Platform = null;
        if (ownsInitialization)
        {
            ownsInitialization = false;
            Assert.That(PlatformInterface.Shutdown(), Is.EqualTo(Result.Success), "EOS native shutdown");
        }
    }
}
