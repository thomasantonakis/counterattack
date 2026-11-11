using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Attached only to the authored proof scene. No startup hooks run in game scenes.
public sealed class EOSStandaloneProof : MonoBehaviour
{
    [Serializable]
    private sealed class Report
    {
        public string role;
        public string platform;
        public string unityVersion;
        public string startedUtc;
        public string completedUtc;
        public string status;
        public string message;
        public string relayControl = "ForceRelays";
    }

    private IEnumerator Start()
    {
        Application.runInBackground = true;
        var started = DateTime.UtcNow.ToString("O");
        EOSProofArguments arguments = null;
        Exception failure = null;
        IEnumerator probe = null;
        try
        {
            arguments = EOSProofArguments.Parse(Environment.GetCommandLineArgs());
            Directory.CreateDirectory(arguments.OutputDirectory);
            probe = arguments.Role == "host"
                ? EOSStandalonePacketProbe.HostProbe(arguments.OutputDirectory)
                : arguments.Role == "guest"
                    ? EOSStandalonePacketProbe.GuestProbe(File.ReadAllText(arguments.LobbyFile).Trim())
                    : LoginOnly();
        }
        catch (Exception exception) { failure = exception; }

        // Drive the iterator ourselves so exceptions become a retained failure report.
        // The probes flatten their login iterators, so every yielded value is one frame.
        while (failure == null && probe != null)
        {
            bool next = false;
            try { next = probe.MoveNext(); }
            catch (Exception exception) { failure = exception; }
            if (!next || failure != null) break;
            yield return null;
        }
        try { (probe as IDisposable)?.Dispose(); }
        catch (Exception exception) { failure = failure ?? exception; }
        try { EOSStandaloneSession.Shutdown(); }
        catch (Exception exception) { failure = failure ?? exception; }

        var report = new Report
        {
            role = arguments?.Role ?? "invalid",
            platform = Application.platform.ToString(),
            unityVersion = Application.unityVersion,
            startedUtc = started,
            completedUtc = DateTime.UtcNow.ToString("O"),
            status = failure == null ? "passed" : "failed",
            // Assertion messages contain stage names, never credentials or user IDs.
            message = failure == null ? "Probe completed" : failure is EOSProofFailure
                ? failure.Message : failure.GetType().Name + ": inspect the local Player.log",
            relayControl = arguments?.Role == "login" ? "not exercised" : "ForceRelays"
        };
        if (arguments != null)
        {
            var path = Path.Combine(arguments.OutputDirectory, "result.json");
            File.WriteAllText(path + ".tmp", JsonUtility.ToJson(report, true));
            File.Move(path + ".tmp", path);
        }
        Debug.Log("EOS standalone proof: " + report.status + " (" + report.message + ")");
#if !UNITY_EDITOR
        Application.Quit(failure == null ? 0 : 1);
#endif
    }

    private static IEnumerator LoginOnly()
    {
        using (var session = new EOSStandaloneSession())
        {
            var login = session.OpenAndLogin();
            while (login.MoveNext()) yield return login.Current;
        }
    }
}

public sealed class EOSProofArguments
{
    public string Role { get; private set; }
    public string OutputDirectory { get; private set; }
    public string LobbyFile { get; private set; }

    public static EOSProofArguments Parse(string[] args)
    {
        var result = new EOSProofArguments();
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("-ca-eos-", StringComparison.Ordinal)) continue;
            var key = args[i];
            if (++i >= args.Length || args[i].StartsWith("-", StringComparison.Ordinal))
                throw new EOSProofFailure("Missing probe argument value");
            switch (key)
            {
                case "-ca-eos-role":
                    EOSProofCheck.Require(result.Role == null, "Duplicate role argument");
                    result.Role = args[i]; break;
                case "-ca-eos-output":
                    EOSProofCheck.Require(result.OutputDirectory == null, "Duplicate output argument");
                    result.OutputDirectory = args[i]; break;
                case "-ca-eos-lobby-file":
                    EOSProofCheck.Require(result.LobbyFile == null, "Duplicate lobby-file argument");
                    result.LobbyFile = args[i]; break;
                default: throw new EOSProofFailure("Unknown probe argument");
            }
        }
        EOSProofCheck.Require(result.Role == "host" || result.Role == "guest" || result.Role == "login", "Expected host, guest or login role");
        EOSProofCheck.Require(!string.IsNullOrWhiteSpace(result.OutputDirectory) && Path.IsPathRooted(result.OutputDirectory), "Expected absolute output directory");
        EOSProofCheck.Require(!File.Exists(Path.Combine(result.OutputDirectory, "result.json")), "Use a fresh run directory");
        if (result.Role == "guest")
            EOSProofCheck.Require(!string.IsNullOrEmpty(result.LobbyFile) && Path.IsPathRooted(result.LobbyFile), "Guest needs an absolute lobby-file path");
        return result;
    }
}

public sealed class EOSProofFailure : Exception
{
    public EOSProofFailure(string message) : base(message) { }
}

internal static class EOSProofCheck
{
    internal static void Require(bool condition, string stage)
    {
        if (!condition) throw new EOSProofFailure(stage);
    }
}
