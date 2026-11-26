using System;
using System.Collections.Generic;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineDraftReturn")]
public sealed class OnlineDraftReturnEditorTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void EitherManagerCanReturnAndStartAFreshDraft(bool hostRequests)
    {
        var link = new OnlineDraftReturnTransport();
        var home = link.DraftFor(OnlineTeam.Home);
        var away = link.DraftFor(OnlineTeam.Away);
        link.Roll();
        link.Pick();
        Assert.That(home.Capture().home[1], Is.Not.Null);

        var setup = hostRequests ? link.Host.Client : link.Guest.Client;
        var agreed = setup.ViewSettings();
        string oldNonce = link.Host.Nonce;
        Assert.That(link.Guest.Draft.Request(OnlineDraftIntent.Pick, away.Capture().cards[1].id, 3), Is.True);
        byte[] delayed = link.ToHost.Dequeue();
        Assert.That(link.ToHost, Is.Empty);

        Assert.That(setup.ReturnToSetup(), Is.True);
        link.Flush();
        Assert.That(setup.LastReply, Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(link.Host.Nonce, Is.Not.EqualTo(oldNonce));
        Assert.That(link.Host.Draft, Is.Null);
        Assert.That(link.Guest.Draft, Is.Null);
        Assert.That(link.Host.Client.BothReady || link.Guest.Client.BothReady, Is.False);
        Assert.That(link.Host.Client.LocalReady || link.Guest.Client.LocalReady, Is.False);
        Assert.That(link.Guest.Client.ViewSettings().homeTeamName, Is.EqualTo(agreed.homeTeamName));
        Assert.That(link.Host.Client.ViewSettings().awayKit, Is.EqualTo(agreed.awayKit));
        Assert.That(home.Connected || away.Connected, Is.False);

        var edited = link.Host.Client.ViewSettings();
        edited.draft = "Free Regular";
        link.Host.Client.Edit(edited);
        link.Flush();
        link.Agree();
        Assert.That(link.Host.Draft.Capture().settings.draft, Is.EqualTo("Free Regular"));
        Assert.That(link.Guest.Draft.Capture().home[1], Is.Null);
        Assert.That(link.Host.Receive(OnlineDraftReturnTransport.GuestAccount, delayed, delayed.Length), Is.False);
        Assert.That(link.Guest.Draft.Phase, Is.EqualTo("roll"));
        Assert.That(link.GuestAgreements, Is.EqualTo(2));
    }

    [Test]
    public void ReturnIsRejectedAfterTheOpeningChoicesCommit()
    {
        var link = new OnlineDraftReturnTransport();
        link.Roll();
        while (link.Host.Draft.Phase == "pick") link.Pick();
        link.Request(link.Host.Draft, OnlineDraftIntent.Ready);
        link.Request(link.Guest.Draft, OnlineDraftIntent.Ready);
        link.Request(link.Host.Draft, OnlineDraftIntent.Start);
        link.Roll();
        link.Request(link.DraftFor(OnlineTeam.Home), OnlineDraftIntent.Choose, choice: CoinTossChoice.Kickoff);
        link.Request(link.DraftFor(OnlineTeam.Away), OnlineDraftIntent.Choose, choice: CoinTossChoice.LeftToRight);
        Assert.That(link.Guest.Draft.Phase, Is.EqualTo("room-ready"));
        Assert.That(link.Guest.Client.ReturnToSetup(), Is.True);
        link.Flush();
        Assert.That(link.Guest.Client.LastReply, Is.EqualTo(OnlineCommandStatus.NotReady));
        Assert.That(link.Guest.Draft.Phase, Is.EqualTo("room-ready"));
    }
}

internal sealed class OnlineDraftReturnTransport
{
    internal const string HostAccount = "home", GuestAccount = "away";
    internal OnlineSetupHostChannel Host { get; }
    internal OnlineSetupGuestChannel Guest { get; }
    internal Queue<byte[]> ToHost { get; } = new();
    internal int GuestAgreements { get; private set; }
    internal double Now { get; set; }
    private readonly Queue<byte[]> toGuest = new();
    private readonly Queue<int> rolls = new();

    internal OnlineDraftReturnTransport(GameSettings settings = null, bool startDraft = true, bool timed = false)
    {
        var compatibility = new OnlineCompatibility(OnlineSessionWire.Version, "draft-return-rules", "catalog");
        var protocol = new OnlineMatchProtocol("draft-return", HostAccount, GuestAccount, HostAccount, compatibility);
        protocol.BindAuthenticatedAccount(HostAccount, "host", compatibility);
        Host = new OnlineSetupHostChannel(protocol, "host", GuestAccount, settings ?? OnlineSetupAgreementEditorTests.Valid(), () => Now);
        Guest = new OnlineSetupGuestChannel(protocol.MatchId, HostAccount, compatibility, protocol.HostTeam, () => Now);
        Host.EnableDraft(agreed => new OnlineDraftAuthority(protocol, agreed, OnlineDraftCatalog.Load(), _ => 0, rolls.Dequeue,
            timed ? () => Now : null));
        Host.Outgoing += toGuest.Enqueue;
        Guest.Outgoing += ToHost.Enqueue;
        Guest.Client.DraftAgreed += _ => GuestAgreements++;
        Host.Begin();
        Flush();
        if (startDraft) Agree();
    }

    internal void Agree()
    {
        foreach (var client in new[] { Host.Client, Guest.Client, Host.Client })
        {
            Assert.That(client.Confirm(client.ViewSettings()), Is.True);
            Flush();
        }
    }

    internal OnlineDraftClient DraftFor(OnlineTeam team) => team == OnlineTeam.Home ? Host.Draft : Guest.Draft;

    internal void Roll()
    {
        QueueRolls(6, 1);
        Request(Host.Draft, OnlineDraftIntent.Roll);
        Request(Guest.Draft, OnlineDraftIntent.Roll);
    }

    internal void QueueRolls(int home, int away)
    {
        rolls.Enqueue(home);
        rolls.Enqueue(away);
    }

    internal void Pick()
    {
        var state = Host.Draft.Capture();
        var roster = state.turn == OnlineTeam.Home ? state.home : state.away;
        Request(DraftFor(state.turn), OnlineDraftIntent.Pick, state.cards.First(card => card != null).id,
            Array.FindIndex(roster, card => card == null) + 1);
    }

    internal void Request(OnlineDraftClient client, OnlineDraftIntent intent, string id = null, int slot = 0,
        CoinTossChoice choice = CoinTossChoice.Kickoff)
    {
        Assert.That(client.Request(intent, id, slot, choice: choice), Is.True);
        Flush();
        Assert.That(client.LastReply, Is.EqualTo(OnlineCommandStatus.Accepted));
    }

    internal void Flush()
    {
        for (int i = 0; i < 4096 && (ToHost.Count > 0 || toGuest.Count > 0); i++)
        {
            if (toGuest.TryDequeue(out var peerPacket))
                Assert.That(Guest.Receive(HostAccount, peerPacket, peerPacket.Length), Is.True);
            if (ToHost.TryDequeue(out var hostPacket))
                Assert.That(Host.Receive(GuestAccount, hostPacket, hostPacket.Length), Is.True);
        }
        Assert.That(ToHost, Is.Empty);
        Assert.That(toGuest, Is.Empty);
    }
}
