using System;
using System.Collections.Generic;
using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineSeatOwnership")]
public sealed class OnlineSeatOwnershipEditorTests
{
    [Test]
    public void HostEditsHomeAndGuestEditsAwayThroughTheSetupChannel()
    {
        var version = OnlineFrontendSessionPolicy.Compatibility();
        var protocol = OnlineFrontendSessionPolicy.CreateMatchProtocol("seat-setup", "host", "guest", version);
        Assert.That(protocol.BindAuthenticatedAccount("host", "host-connection", version), Is.True);
        var host = new OnlineSetupHostChannel(protocol, "host-connection", "guest", OnlineSetupAgreementEditorTests.Valid(), () => 0);
        var guest = new OnlineSetupGuestChannel(protocol.MatchId, "host", version, OnlineFrontendSessionPolicy.HostTeam, () => 0);
        var delivery = new Queue<Action>();
        host.Outgoing += packet => delivery.Enqueue(() => Assert.That(guest.Receive("host", packet, packet.Length), Is.True));
        guest.Outgoing += packet => delivery.Enqueue(() => Assert.That(host.Receive("guest", packet, packet.Length), Is.True));
        host.Begin();
        Deliver();
        Assert.That(host.Client.Team, Is.EqualTo(OnlineTeam.Home));
        Assert.That(host.Client.IsHost, Is.True);
        Assert.That(guest.Client.Team, Is.EqualTo(OnlineTeam.Away));
        Assert.That(guest.Client.IsHost, Is.False);

        var hostEdit = host.Client.ViewSettings();
        hostEdit.homeTeamName = "Host FC";
        Assert.That(host.Client.Confirm(hostEdit), Is.True);
        Deliver();
        var guestEdit = guest.Client.ViewSettings();
        guestEdit.awayTeamName = "Guest FC";
        guestEdit.homeTeamName = "Wrong team";
        Assert.That(guest.Client.Confirm(guestEdit), Is.True);
        Deliver();
        Assert.That(host.Client.ViewSettings().homeTeamName, Is.EqualTo("Host FC"));
        Assert.That(host.Client.ViewSettings().awayTeamName, Is.EqualTo("Guest FC"));
        Assert.That(guest.Client.ViewSettings().homeTeamName, Is.EqualTo("Host FC"));

        void Deliver()
        {
            for (int i = 0; delivery.Count > 0 && i < 128; i++) delivery.Dequeue()();
            Assert.That(delivery, Is.Empty);
        }
    }

    [Test]
    public void PreviousAwayHostBuildIsRejected()
    {
        var current = OnlineFrontendSessionPolicy.Compatibility();
        var old = new OnlineCompatibility(current.Protocol, "frontend-rules-v7", current.Catalog);
        var protocol = OnlineFrontendSessionPolicy.CreateMatchProtocol("seat-version", "host", "guest", current);
        Assert.That(protocol.BindAuthenticatedAccount("guest", "returning-guest", old), Is.False);
        Assert.That(protocol.BindAuthenticatedAccount("guest", "new-guest", current), Is.True);
        Assert.That(protocol.TryGetTeam("new-guest", out var team), Is.True);
        Assert.That(team, Is.EqualTo(OnlineTeam.Away));
    }
}
