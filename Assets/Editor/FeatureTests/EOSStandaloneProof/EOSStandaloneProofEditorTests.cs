using System;
using System.IO;
using NUnit.Framework;

[Category("EOSStandaloneProof")]
public sealed class EOSStandaloneProofEditorTests
{
    private string output;

    [SetUp]
    public void SetUp() { output = Path.Combine(Path.GetTempPath(), "CA-EOS-proof-" + Guid.NewGuid().ToString("N")); }

    [TestCase("host")]
    [TestCase("login")]
    public void ExplicitRoleAndFreshAbsoluteOutputAreAccepted(string role)
    {
        var args = EOSProofArguments.Parse(new[] { "player", "-ca-eos-role", role, "-ca-eos-output", output });
        Assert.That(args.Role, Is.EqualTo(role));
        Assert.That(args.OutputDirectory, Is.EqualTo(output));
    }

    [Test]
    public void GuestRequiresLobbyFile()
    {
        Assert.Throws<EOSProofFailure>(() => EOSProofArguments.Parse(new[] { "-ca-eos-role", "guest", "-ca-eos-output", output }));
        var lobby = Path.Combine(output, "lobby-id.txt");
        Assert.That(EOSProofArguments.Parse(new[] { "-ca-eos-role", "guest", "-ca-eos-output", output, "-ca-eos-lobby-file", lobby }).LobbyFile, Is.EqualTo(lobby));
    }

    [TestCase("-ca-eos-role", "invalid")]
    [TestCase("-ca-eos-role", "host", "-ca-eos-output", "relative")]
    [TestCase("-ca-eos-role", "host", "-ca-eos-role", "guest")]
    [TestCase("-ca-eos-typo", "value")]
    [TestCase("-ca-eos-role")]
    public void InvalidArgumentsFailBeforeNetworkInitialization(params string[] args)
    {
        Assert.Throws<EOSProofFailure>(() => EOSProofArguments.Parse(args));
    }

    [Test]
    public void ExistingResultCannotBeReusedAsFreshEvidence()
    {
        Directory.CreateDirectory(output);
        try
        {
            File.WriteAllText(Path.Combine(output, "result.json"), "{}");
            Assert.Throws<EOSProofFailure>(() => EOSProofArguments.Parse(new[] { "-ca-eos-role", "login", "-ca-eos-output", output }));
        }
        finally { Directory.Delete(output, true); }
    }
}
