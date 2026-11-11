using System;
using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Platform;
using NUnit.Framework;
using PlayEveryWare.EpicOnlineServices;
using PlayEveryWare.EpicOnlineServices.Utility;
using UnityEditor;
using UnityEngine;

// Keep the native SDK alive across focused tests. EOS cannot initialize again after Shutdown.
internal sealed class EOSProbeSession : IDisposable
{
    private const string InitializedKey = "Counterattack.OnlineEOSConnectivity.Initialized";
    private static PlatformInterface activePlatform;
    private static bool sessionActive;
    private static bool platformSpecificsRegistered;

    static EOSProbeSession()
    {
        AssemblyReloadEvents.beforeAssemblyReload += ReleaseForDomainReload;
        EditorApplication.quitting += ShutdownOnEditorQuit;
    }

    private static void ReleaseForDomainReload()
    {
        activePlatform?.Release();
        activePlatform = null;
        sessionActive = false;
    }

    private static void ShutdownOnEditorQuit()
    {
        ReleaseForDomainReload();
        if (!SessionState.GetBool(InitializedKey, false)) return;
        SessionState.SetBool(InitializedKey, false);
        var result = PlatformInterface.Shutdown();
        if (result != Result.Success)
            Debug.LogError("EOS probe shutdown on Editor quit failed: " + result);
    }

    public PlatformInterface Platform { get; private set; }
    public ProductUserId UserId { get; private set; }

    internal static void EnsureInitialized()
    {
        if (!platformSpecificsRegistered)
        {
#if UNITY_EDITOR_WIN
            if (EOSManagerPlatformSpecificsSingleton.Instance == null)
                WindowsPlatformSpecifics.Register();
            Assert.That(EOSManagerPlatformSpecificsSingleton.Instance, Is.TypeOf<WindowsPlatformSpecifics>(),
                "EOS probes must use the Windows platform adapter");
#elif UNITY_EDITOR_OSX
            if (EOSManagerPlatformSpecificsSingleton.Instance == null)
                MacOSPlatformSpecifics.Register();
            Assert.That(EOSManagerPlatformSpecificsSingleton.Instance, Is.TypeOf<MacOSPlatformSpecifics>(),
                "EOS probes must use the macOS platform adapter");
#else
            Assert.Inconclusive("EOS connectivity probes support the Windows and macOS Editors only.");
#endif
            EOSManager.EOSSingleton.LoadEOSLibraries();
            platformSpecificsRegistered = true;
        }
        if (!SessionState.GetBool(InitializedKey, false))
        {
            var initializeOptions = ConfigurationUtility.GetEOSInitializeOptions();
            var initializeResult = PlatformInterface.Initialize(ref initializeOptions.options);
            if (initializeResult == Result.AlreadyConfigured)
                Assert.Inconclusive("EOS was initialized outside this probe helper; reopen Unity before isolated probes.");
            Assert.That(initializeResult, Is.EqualTo(Result.Success), "EOS native initialization");
            SessionState.SetBool(InitializedKey, true);
        }
    }

    public IEnumerator OpenAndLogin()
    {
        Assert.That(sessionActive, Is.False, "Another EOS probe session is still active");
        EnsureInitialized();

        var createOptions = ConfigurationUtility.GetEOSCreateOptions();
        Platform = PlatformInterface.Create(ref createOptions.options);
        Assert.That(Platform, Is.Not.Null, "EOS platform creation");
        activePlatform = Platform;
        sessionActive = true;

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
        UserId = null;
        activePlatform = null;
        sessionActive = false;
    }
}
