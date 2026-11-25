using CounterAttack.Online;
using NUnit.Framework;

public sealed class OnlineSpectatorAdmissionEditorTests
{
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
