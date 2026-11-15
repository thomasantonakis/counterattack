using System;
using System.Collections.Generic;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineDraftTransport")]
public sealed class OnlineDraftChannelEditorTests
{
    private OnlineSetupHostChannel host;
    private OnlineSetupGuestChannel guest;
    private readonly Queue<byte[]> toHost=new(),toGuest=new();
    private readonly Queue<int> rolls=new();
    private string hostAccount,guestAccount;
    private void Flush()
    {
        for(int i=0;i<4096 && (toHost.Count>0 || toGuest.Count>0);i++)
        {
            if(toGuest.Count>0){var p=toGuest.Dequeue();Assert.That(guest.Receive(hostAccount,p,p.Length),Is.True);}
            if(toHost.Count>0){var p=toHost.Dequeue();Assert.That(host.Receive(guestAccount,p,p.Length),Is.True);}
        }
        Assert.That(toHost,Is.Empty);Assert.That(toGuest,Is.Empty);
    }
    private void Create(bool hostAway)
    {
        toHost.Clear();toGuest.Clear();rolls.Clear();hostAccount=hostAway?"away":"home";guestAccount=hostAway?"home":"away";
        var v=new OnlineCompatibility(OnlineSessionWire.Version,"draft-channel-rules","catalog");
        var protocol=new OnlineMatchProtocol("draft-channel","home","away",hostAccount,v);protocol.BindAuthenticatedAccount(hostAccount,"host",v);
        host=new(protocol,"host",guestAccount,OnlineSetupAgreementEditorTests.Valid(),()=>0);
        guest=new(protocol.MatchId,hostAccount,v,protocol.HostTeam,()=>0);
        host.EnableDraft(s=>new OnlineDraftAuthority(protocol,s,OnlineDraftCatalog.Load(),_=>0,()=>rolls.Dequeue()));
        host.Outgoing+=toGuest.Enqueue;guest.Outgoing+=toHost.Enqueue;
        host.Begin();Flush();host.Client.Confirm(host.Client.ViewSettings());Flush();guest.Client.Confirm(guest.Client.ViewSettings());Flush();host.Client.Confirm(host.Client.ViewSettings());Flush();
        Assert.That(host.Draft,Is.Not.Null);Assert.That(guest.Draft,Is.Not.Null);Assert.That(host.Draft.Capture().phase,Is.EqualTo("roll"));
    }
    private OnlineDraftClient Client(OnlineTeam team)=>host.Draft.Team==team?host.Draft:guest.Draft;
    private void Roll()
    {rolls.Enqueue(6);rolls.Enqueue(1);Assert.That(Client(OnlineTeam.Home).Request(OnlineDraftIntent.Roll),Is.True);Flush();Assert.That(Client(OnlineTeam.Away).Request(OnlineDraftIntent.Roll),Is.True);Flush();}
    private void Pick()
    {var s=host.Draft.Capture();var roster=s.turn==OnlineTeam.Home?s.home:s.away;Assert.That(Client(s.turn).Request(OnlineDraftIntent.Pick,s.cards[0].id,Array.FindIndex(roster,c=>c==null)+1),Is.True);Flush();}
    [Test] public void SerializedDraft([Values(false,true)]bool hostAway,[Values("arrival","roll","pick","complete","rejoin","lost-reply","malformed","local-loss")]string branch)
    {
        Create(hostAway);
        if(branch=="arrival"){Assert.That(guest.Draft.Team,Is.Not.EqualTo(host.Draft.Team));Assert.That(guest.Draft.Connected,Is.True);return;}
        if(branch=="malformed")
        {
            var s=guest.Draft.Capture();long revision=s.revision;s.revision++;s.home[1]=s.home[0];
            Assert.That(guest.Draft.Apply(s),Is.False);Assert.That(guest.Draft.Capture().revision,Is.EqualTo(revision));return;
        }
        if(branch=="local-loss")
        {guest.TransportLost();Assert.That(guest.Draft.Connected,Is.False);Assert.That(guest.Draft.Request(OnlineDraftIntent.Roll),Is.False);host.Begin();Flush();Assert.That(guest.Draft.Connected,Is.True);return;}
        Roll();if(branch=="roll"){Assert.That(guest.Draft.Capture().homeRoll,Is.EqualTo(6));Assert.That(guest.Draft.Capture().awayRoll,Is.EqualTo(1));return;}
        if(branch=="lost-reply")
        {
            if(host.Draft.Team==OnlineTeam.Home)Pick();
            var s=guest.Draft.Capture();Assert.That(s.turn,Is.EqualTo(guest.Draft.Team));var roster=s.turn==OnlineTeam.Home?s.home:s.away;
            Assert.That(guest.Draft.Request(OnlineDraftIntent.Pick,s.cards[0].id,Array.FindIndex(roster,c=>c==null)+1),Is.True);
            while(toHost.Count>0){var p=toHost.Dequeue();Assert.That(host.Receive(guestAccount,p,p.Length),Is.True);}
            Assert.That(guest.Draft.Pending,Is.True);var accepted=host.DraftAuthority.Capture();
            toGuest.Clear();host.Begin();Flush();Assert.That(guest.Draft.Pending,Is.False);
            Assert.That(guest.Draft.Capture().homeSequence,Is.EqualTo(accepted.homeSequence));Assert.That(guest.Draft.Capture().awaySequence,Is.EqualTo(accepted.awaySequence));return;
        }
        Pick();var before=guest.Draft.Capture();
        if(branch=="rejoin")
        {
            guest.TransportLost();host.Begin();Flush();var after=guest.Draft.Capture();
            Assert.That(after.home.Select(c=>c?.id),Is.EqualTo(before.home.Select(c=>c?.id)));Assert.That(after.turn,Is.EqualTo(before.turn));Assert.That(guest.Draft.Connected,Is.True);return;
        }
        if(branch=="pick"){Assert.That(before.home[1],Is.Not.Null);Assert.That(before.cards.Length,Is.EqualTo(3));Assert.That(host.Draft.Capture().revision,Is.EqualTo(before.revision));return;}
        while(host.Draft.Capture().phase=="pick")Pick();
        Assert.That(host.Draft.Request(OnlineDraftIntent.Ready),Is.True);Flush();Assert.That(guest.Draft.Request(OnlineDraftIntent.Ready),Is.True);Flush();
        Assert.That(host.Draft.Request(OnlineDraftIntent.Start),Is.True);Flush();Assert.That(guest.Draft.Capture().phase,Is.EqualTo("room-ready"));
        Assert.That(guest.Draft.Capture().home.Concat(guest.Draft.Capture().away).Select(c=>c.id).Distinct().Count(),Is.EqualTo(32));
    }
}
