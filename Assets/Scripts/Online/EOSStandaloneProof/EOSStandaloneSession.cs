using System;
using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Platform;
using PlayEveryWare.EpicOnlineServices;
using PlayEveryWare.EpicOnlineServices.Utility;
using UnityEngine;

// One application run owns one session; Windows borrows the native plugin platform.
internal sealed class EOSStandaloneSession : IDisposable
{
    public PlatformInterface Platform { get; private set; }
    public ProductUserId UserId { get; private set; }
    private bool ownsPlatform;

    private static bool initialized;
    internal static void EnsureInitialized()
    {
#if (UNITY_STANDALONE_WIN && !UNITY_EDITOR) || UNITY_EDITOR_WIN
        if (EOSManagerPlatformSpecificsSingleton.Instance == null) WindowsPlatformSpecifics.Register();
        EOSProofCheck.Require(EOSManagerPlatformSpecificsSingleton.Instance is WindowsPlatformSpecifics, "Windows adapter");
#elif (UNITY_STANDALONE_OSX && !UNITY_EDITOR) || UNITY_EDITOR_OSX
        if (EOSManagerPlatformSpecificsSingleton.Instance == null) MacOSPlatformSpecifics.Register();
        EOSProofCheck.Require(EOSManagerPlatformSpecificsSingleton.Instance is MacOSPlatformSpecifics, "macOS adapter");
#else
        throw new InvalidOperationException("Standalone EOS proof supports Windows and macOS only");
#endif
        if (initialized) return;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // The packaged Windows Gfx plugin initializes EOS before managed scene
        // startup. Borrow its platform; never initialize, release or shut it down
        // from this probe. The native plugin owns that lifetime.
        initialized = true;
        return;
#else
        EOSManager.EOSSingleton.LoadEOSLibraries();
        var options = ConfigurationUtility.GetEOSInitializeOptions();
        var result = PlatformInterface.Initialize(ref options.options);
        EOSProofCheck.Require(result == Result.Success, "EOS native initialization: " + result);
        initialized = true;
#endif
    }

    internal static void Shutdown()
    {
        if (!initialized) return;
#if !UNITY_STANDALONE_WIN || UNITY_EDITOR
        PlatformInterface.Shutdown();
#endif
        initialized = false;
    }

    public IEnumerator OpenAndLogin()
    {
        EnsureInitialized();

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        Platform = EOSManager.Instance.GetEOSPlatformInterface();
        ownsPlatform = false;
#else
        var createOptions = ConfigurationUtility.GetEOSCreateOptions();
        Platform = PlatformInterface.Create(ref createOptions.options);
        ownsPlatform = true;
#endif
        EOSProofCheck.Require(Platform != null, "EOS platform creation");

        var connect = Platform.GetConnectInterface();
        EOSProofCheck.Require(connect != null, "EOS Connect interface");
        Result? deviceResult = null;
        var deviceOptions = new CreateDeviceIdOptions { DeviceModel = "Counterattack standalone proof" };
        connect.CreateDeviceId(ref deviceOptions, null,
            (ref CreateDeviceIdCallbackInfo info) => deviceResult = info.ResultCode);
        var deadline = Time.realtimeSinceStartupAsDouble + 25;
        while (!deviceResult.HasValue && Time.realtimeSinceStartupAsDouble < deadline)
        {
            Platform.Tick();
            yield return null;
        }
        EOSProofCheck.Require(deviceResult.HasValue, "CreateDeviceId callback timed out");
        EOSProofCheck.Require(deviceResult.Value == Result.Success || deviceResult.Value == Result.DuplicateNotAllowed, "Creating or reusing the local device identity");

        var loginOptions = new LoginOptions
        {
            Credentials = new Credentials { Type = ExternalCredentialType.DeviceidAccessToken },
            UserLoginInfo = new UserLoginInfo { DisplayName = "Counterattack M0" }
        };
        LoginCallbackInfo? login = null;
        connect.Login(ref loginOptions, null, (ref LoginCallbackInfo info) => login = info);
        deadline = Time.realtimeSinceStartupAsDouble + 25;
        while (!login.HasValue && Time.realtimeSinceStartupAsDouble < deadline)
        {
            Platform.Tick();
            yield return null;
        }
        EOSProofCheck.Require(login.HasValue, "Connect.Login callback timed out");
        if (login.Value.ResultCode == Result.InvalidUser)
        {
            Result? createUserResult = null;
            var userOptions = new CreateUserOptions { ContinuanceToken = login.Value.ContinuanceToken };
            connect.CreateUser(ref userOptions, null,
                (ref CreateUserCallbackInfo info) => createUserResult = info.ResultCode);
            deadline = Time.realtimeSinceStartupAsDouble + 25;
            while (!createUserResult.HasValue && Time.realtimeSinceStartupAsDouble < deadline)
            {
                Platform.Tick();
                yield return null;
            }
            EOSProofCheck.Require(createUserResult.HasValue, "Connect.CreateUser callback timed out");
            EOSProofCheck.Require(createUserResult.Value == Result.Success, "Creating the test product user");

            login = null;
            connect.Login(ref loginOptions, null, (ref LoginCallbackInfo info) => login = info);
            deadline = Time.realtimeSinceStartupAsDouble + 25;
            while (!login.HasValue && Time.realtimeSinceStartupAsDouble < deadline)
            {
                Platform.Tick();
                yield return null;
            }
            EOSProofCheck.Require(login.HasValue, "Connect.Login retry timed out");
        }

        EOSProofCheck.Require(login.Value.ResultCode == Result.Success, "EOS Connect login");
        EOSProofCheck.Require(login.Value.LocalUserId != null, "Authenticated product user ID");
        EOSProofCheck.Require(login.Value.LocalUserId.IsValid(), "Authenticated product user ID validity");
        UserId = login.Value.LocalUserId;
    }

    public void Dispose()
    {
        if (ownsPlatform) Platform?.Release();
        Platform = null;
        UserId = null;
    }
}
