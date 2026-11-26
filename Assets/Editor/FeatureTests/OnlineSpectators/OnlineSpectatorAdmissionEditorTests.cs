using CounterAttack.Online;
using NUnit.Framework;
using System.Reflection;

public sealed class OnlineSpectatorAdmissionEditorTests
{
    [Test]
    public void ViewerHelloUsesOnlyTheBaselineAndPreservesMatchCodeValidation()
    {
        const string lobby = "spectator-lobby";
        var match = new OnlineSpectatorAdmission();
        const BindingFlags policy = BindingFlags.Static | BindingFlags.NonPublic;
        var suffix = (string)typeof(OnlineSpectatorAdmission).GetProperty("ViewerCompatibilitySuffix", policy).GetValue(null);
        var read = typeof(OnlineSpectatorAdmission).GetMethod("ViewerHelloCode", policy);
        string prefix = "ca-m0-hello:" + lobby;
        string Parse(string hello, string onLobby = lobby) =>
            (string)read.Invoke(null, new object[] { hello, onLobby });

        Assert.That(suffix, Is.EqualTo(":2"));
        Assert.That(match.TryAdmitViewer("viewer", Parse(prefix + match.MatchCode + suffix)), Is.True);
        Assert.That(Parse(prefix + match.MatchCode + suffix, "another-lobby"), Is.Null);
        Assert.That(Parse(prefix + match.MatchCode + ":1"), Is.Null);
        Assert.That(Parse(prefix + match.MatchCode + ":3"), Is.Null, "New clients must use the shared v2 baseline");
        var incorrectCode = match.MatchCode == "00000000" ? "00000001" : "00000000";
        Assert.That(match.TryAdmitViewer("wrong-code", Parse(prefix + incorrectCode + suffix)), Is.False);
    }

    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    [TestCase(false, false, true)]
    public void HostSharesTheCodeAfterViewerDiscoveryOrWithViewingDisabled(bool viewing, bool advertised, bool expected)
    {
        Assert.That(OnlineFrontendSessionPolicy.IsMatchCodeReady(viewing, advertised), Is.EqualTo(expected));
    }

    [Test]
    public void SharedCodeRoundTripsThroughTheOnlineMenuAsEightHexCharacters()
    {
        var match = new OnlineSpectatorAdmission();
        Assert.That(match.MatchCode, Does.Match("^[0-9A-F]{8}$"));
        Assert.That(OnlineModeMenu.TryReadLobbyCode(" " + match.MatchCode + " ", out var code), Is.True);
        Assert.That(code, Is.EqualTo(match.MatchCode));
        Assert.That(match.TryAdmitViewer("viewer", code), Is.True);
        Assert.That(OnlineModeMenu.TryReadLobbyCode(code + "=", out _), Is.False);
        Assert.That(OnlineModeMenu.TryReadLobbyCode(code.Substring(0, 7) + "!", out _), Is.False);
    }

    [Test]
    public void MatchCodePreservesLeadingZerosAndNormalizesLowercaseHex()
    {
        Assert.That(OnlineModeMenu.TryReadLobbyCode(" 00a7f29c ", out var code), Is.True);
        Assert.That(code, Is.EqualTo("00A7F29C"));
        Assert.That(OnlineModeMenu.TryReadLobbyCode("0012345G", out code), Is.False);
        Assert.That(code, Is.Null);
        Assert.That(OnlineModeMenu.TryReadLobbyCode("0012345", out _), Is.False);
        Assert.That(OnlineModeMenu.TryReadLobbyCode("001234567", out _), Is.False);
    }

    [Test]
    public void SameCodeAdmitsEarlyViewerAndOneAwayManager()
    {
        var match = new OnlineSpectatorAdmission();
        Assert.That(match.TryAdmitViewer("first-viewer", match.MatchCode), Is.True);
        Assert.That(match.TryAdmitAway("first-viewer", match.MatchCode), Is.False);
        Assert.That(match.TryAdmitAway("away", match.MatchCode), Is.True);
        Assert.That(match.TryAdmitAway("other", match.MatchCode), Is.False);
        Assert.That(match.TryAdmitAway("away", match.MatchCode), Is.True, "Away can reconnect");
        Assert.That(match.TryAdmitViewer("away", match.MatchCode), Is.False);
        Assert.That(match.TryAdmitViewer("other", match.MatchCode), Is.True, "Late spectator uses the same code");
    }

    [Test]
    public void ViewerCapacityReturnsOnLeaveAndRejectsDuplicateJoins()
    {
        var match = new OnlineSpectatorAdmission();
        Assert.That(match.ViewerCount, Is.Zero);
        Assert.That(match.TryAdmitViewer("first", match.MatchCode), Is.True);
        Assert.That(match.TryAdmitViewer("first", match.MatchCode), Is.False);
        Assert.That(match.TryAdmitViewer("second", match.MatchCode), Is.True);
        Assert.That(match.TryAdmitViewer("third", match.MatchCode), Is.False);
        Assert.That(match.ViewerCount, Is.EqualTo(2));
        match.ViewerLeft("first");
        Assert.That(match.TryAdmitViewer("third", match.MatchCode), Is.True);
        Assert.That(match.ViewerCount, Is.EqualTo(2));
    }

    [Test]
    public void AwayJoinWaitsForTheSameIdentityToLeaveTheViewerLobby()
    {
        var match = new OnlineSpectatorAdmission();
        Assert.That(match.TryAdmitViewer("guest", match.MatchCode), Is.True);
        Assert.That(match.TryAdmitAway("guest", "wrong", out var waiting), Is.False);
        Assert.That(waiting, Is.False);
        Assert.That(match.TryAdmitAway("guest", match.MatchCode, out waiting), Is.False);
        Assert.That(waiting, Is.True);

        match.ViewerLeft("guest");
        Assert.That(match.TryAdmitAway("guest", match.MatchCode, out waiting), Is.True);
        Assert.That(waiting, Is.False);
        Assert.That(match.TryAdmitViewer("guest", match.MatchCode), Is.False);
    }

    [Test]
    public void SpectatorCannotClaimAReservedManagerSeatByLeaving()
    {
        var match = new OnlineSpectatorAdmission(savedAwayIdentity: "original-away");
        Assert.That(match.TryAdmitViewer("guest", match.MatchCode), Is.True);
        Assert.That(match.TryAdmitAway("guest", match.MatchCode, out var waiting), Is.False);
        Assert.That(waiting, Is.False);
        match.ViewerLeft("guest");
        Assert.That(match.TryAdmitAway("guest", match.MatchCode), Is.False);
        Assert.That(match.TryAdmitAway("original-away", match.MatchCode), Is.True);
    }

    [Test]
    public void ViewingCanCloseWhileTheOriginalManagerRetainsAccess()
    {
        var match = new OnlineSpectatorAdmission(savedAwayIdentity: "saved-away");
        Assert.That(match.TryAdmitAway("first-guest", match.MatchCode), Is.False);
        Assert.That(match.TryAdmitViewer("first-guest", match.MatchCode), Is.True);
        match.CloseViewing();
        Assert.That(match.ViewerCount, Is.Zero);
        Assert.That(match.TryAdmitViewer("first-guest", match.MatchCode), Is.False);
        Assert.That(match.TryAdmitAway("saved-away", match.MatchCode), Is.True);
    }

    [Test]
    public void StaleCodeAndMissingTransportIdentityCannotClaimRoles()
    {
        var closedMatch = new OnlineSpectatorAdmission();
        var match = new OnlineSpectatorAdmission();
        Assert.That(match.TryAdmitViewer("guest", closedMatch.MatchCode), Is.False);
        Assert.That(match.TryAdmitAway("guest", closedMatch.MatchCode), Is.False);
        Assert.That(match.TryAdmitViewer(null, match.MatchCode), Is.False);
        Assert.That(match.TryAdmitAway("", match.MatchCode), Is.False);
        Assert.That(match.TryAdmitAway("actual-away", match.MatchCode), Is.True);
        Assert.That(match.ViewerCount, Is.Zero);
    }
}
