using System;
using System.IO;
using NUnit.Framework;

public sealed class OnlineSpectatorProofEditorTests
{
    private string output;

    [SetUp]
    public void SetUp() => output = Path.Combine(Path.GetTempPath(), "CA-watch-proof-" + Guid.NewGuid().ToString("N"));

    [TestCase("watch-host")]
    [TestCase("watch-lobbies")]
    public void HostCanStartBeforeEitherLobbyExists(string role)
    {
        var arguments = EOSProofArguments.Parse(new[] { "-ca-eos-role", role, "-ca-eos-output", output });
        Assert.That(arguments.Role, Is.EqualTo(role));
        Assert.That(arguments.LobbyFile, Is.Null);
    }

    [TestCase("watch-manager")]
    [TestCase("watch-viewer")]
    public void ClientsRequireAnAbsoluteLobbyHandoffPath(string role)
    {
        var lobbyFile = Path.Combine(output, "lobby-id.txt");
        var arguments = EOSProofArguments.Parse(new[]
        {
            "-ca-eos-role", role, "-ca-eos-output", output, "-ca-eos-lobby-file", lobbyFile
        });
        Assert.That(arguments.Role, Is.EqualTo(role));
        Assert.That(arguments.LobbyFile, Is.EqualTo(lobbyFile));
        Assert.Throws<EOSProofFailure>(() => EOSProofArguments.Parse(new[]
        {
            "-ca-eos-role", role, "-ca-eos-output", output
        }));
        Assert.Throws<EOSProofFailure>(() => EOSProofArguments.Parse(new[]
        {
            "-ca-eos-role", role, "-ca-eos-output", output, "-ca-eos-lobby-file", "relative.txt"
        }));
    }
}
