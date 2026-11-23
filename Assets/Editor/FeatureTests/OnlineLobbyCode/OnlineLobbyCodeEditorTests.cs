using System;
using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineLobbyCode")]
public sealed class OnlineLobbyCodeEditorTests
{
    [Test]
    public void HostCodeRoundTripsThroughGuestMenu()
    {
        string generated = OnlineFrontendSessionPolicy.GenerateLobbyCode();
        Assert.That(generated, Does.Match("^[0-9A-F]{8}$"));
        Assert.That(OnlineModeMenu.TryReadLobbyCode(" " + generated.ToLowerInvariant() + "\t", out var code), Is.True);
        Assert.That(code, Is.EqualTo(generated));
    }

    [TestCase("", false)]
    [TestCase("7A3F9C2", false)]
    [TestCase("7A3F9C2BA", false)]
    [TestCase("7A3F9C2G", false)]
    [TestCase("7A3F 9C2", false)]
    [TestCase("0123456789ABCDEF0123456789ABCDEF", false)]
    [TestCase("  00000000 ", true)]
    public void GuestMenuValidatesTheCompleteCode(string input, bool valid)
    {
        Assert.That(OnlineModeMenu.TryReadLobbyCode(input, out var code), Is.EqualTo(valid));
        if (!valid)
            Assert.That(code, Is.Null);
    }

    [TestCase(0U, "No active lobby")]
    [TestCase(2U, "matches multiple lobbies")]
    public void NonUniqueResultsCannotProceed(uint matches, string message)
    {
        Assert.That(() => OnlineFrontendSessionPolicy.RequireUniqueLobbyCodeMatch(matches),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains(message));
    }

    [Test]
    public void OneResultCanProceed()
    {
        Assert.DoesNotThrow(() => OnlineFrontendSessionPolicy.RequireUniqueLobbyCodeMatch(1));
    }
}
