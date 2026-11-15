using System;
using System.Collections.Generic;
using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineFrontendRecovery")]
public sealed class OnlineFrontendRecoveryEditorTests
{
    private double now;
    private OnlineSetupHostChannel host;
    private OnlineSetupGuestChannel guest;
    private OnlineFrontendRecovery hostRecovery,guestRecovery;
    private readonly Queue<byte[]> toHost=new(),toGuest=new();
    private string hostAccount,guestAccount;
    private void Create(bool hostAway)
    {
        now=0;toHost.Clear();toGuest.Clear();hostAccount=hostAway?"away":"home";guestAccount=hostAway?"home":"away";
        var version=new OnlineCompatibility(OnlineSessionWire.Version,"recovery-rules","recovery-catalog");
        var protocol=new OnlineMatchProtocol("frontend-recovery","home","away",hostAccount,version);
        protocol.BindAuthenticatedAccount(hostAccount,"host-connection",version);
        host=new OnlineSetupHostChannel(protocol,"host-connection",guestAccount,OnlineSetupAgreementEditorTests.Valid(),()=>now);
        guest=new OnlineSetupGuestChannel(protocol.MatchId,hostAccount,version,protocol.HostTeam,()=>now);
        host.Outgoing+=toGuest.Enqueue;guest.Outgoing+=toHost.Enqueue;
        hostRecovery=new(host,()=>now,toGuest.Clear,()=>toGuest.Count==0);
        guestRecovery=new(guest,()=>now,toHost.Clear,()=>toHost.Count==0);
        host.Begin();
    }
    private void Flush()
    {
        for(int i=0;i<512 && (toHost.Count>0 || toGuest.Count>0);i++)
        {
            if(toGuest.Count>0){var p=toGuest.Dequeue();Assert.That(guest.Receive(hostAccount,p,p.Length),Is.True);}
            if(toHost.Count>0){var p=toHost.Dequeue();Assert.That(host.Receive(guestAccount,p,p.Length),Is.True);}
        }
        Assert.That(toHost,Is.Empty);Assert.That(toGuest,Is.Empty);
    }
    private void Agree()
    {
        Assert.That(host.Client.Confirm(host.Client.ViewSettings()),Is.True);Flush();
        Assert.That(guest.Client.Confirm(guest.Client.ViewSettings()),Is.True);Flush();
        Assert.That(host.Client.Confirm(host.Client.ViewSettings()),Is.True);Flush();
        Assert.That(host.Authority.Capture().phase,Is.EqualTo("draft-ready"));
    }
    [Test] public void SerializedRecovery([Values(false,true)]bool hostAway,[Values("initial-retry","silence","pending","agreed","bounded-retry","expiry","untrusted","closed")]string branch)
    {
        Create(hostAway);
        if(branch=="initial-retry")
        {
            string nonce=host.Nonce;toGuest.Clear();now=2;hostRecovery.Tick();Flush();
            Assert.That(host.Ready && guest.Ready,Is.True);Assert.That(host.Nonce,Is.EqualTo(nonce));return;
        }
        Flush();int notifications=0;guest.Client.DraftAgreed+=_=>notifications++;
        if(branch=="agreed")Agree();
        if(branch=="pending")
        {
            var proposed=guest.Client.ViewSettings();if(guest.Client.Team==OnlineTeam.Home)proposed.homeTeamName="Accepted selection";else proposed.awayTeamName="Accepted selection";
            Assert.That(guest.Client.Confirm(proposed),Is.True);
            while(toHost.Count>0){var p=toHost.Dequeue();Assert.That(host.Receive(guestAccount,p,p.Length),Is.True);}
            Assert.That(guest.Client.Pending,Is.True);toGuest.Clear();
        }
        var before=host.Authority.Capture();string oldNonce=host.Nonce;
        if(branch=="untrusted")
        {
            now=14;
            foreach(var p in OnlineSessionWire.Encode(new OnlineSessionMessage{kind="heartbeat",matchId=before.matchId,nonce=oldNonce,status="ping",sequence=1}))
                Assert.That(host.Receive("stranger",p,p.Length),Is.False);
        }
        if(branch=="closed"){hostRecovery.ConnectionLost();guestRecovery.ConnectionLost();}
        else{now=OnlineFrontendRecovery.SilenceSeconds;hostRecovery.Tick();guestRecovery.Tick();}
        Assert.That(hostRecovery.Recovering && guestRecovery.Recovering,Is.True);
        Assert.That(guest.Client.Connected,Is.False);Assert.That(guest.Client.CanConfirm,Is.False);
        Assert.That(host.Nonce,Is.Not.EqualTo(oldNonce));string recoveryNonce=host.Nonce;
        if(branch=="expiry")
        {
            now+=OnlineFrontendRecovery.RecoverySeconds-1;hostRecovery.ConnectionLost();guestRecovery.ConnectionLost();
            now+=1;hostRecovery.Tick();guestRecovery.Tick();
            Assert.That(hostRecovery.Expired && guestRecovery.Expired,Is.True);Assert.That(toHost,Is.Empty);Assert.That(toGuest,Is.Empty);
            Assert.That(host.Authority.Capture().settings.homeTeamName,Is.EqualTo(before.settings.homeTeamName));return;
        }
        if(branch=="bounded-retry")
        {
            int queued=toGuest.Count;Assert.That(queued,Is.GreaterThan(0));
            for(int i=0;i<20;i++){now+=2;hostRecovery.Tick();}
            Assert.That(toGuest.Count,Is.EqualTo(queued));Assert.That(host.Nonce,Is.EqualTo(recoveryNonce));
            toGuest.Clear();now+=2;hostRecovery.Tick();Assert.That(toGuest.Count,Is.EqualTo(queued));
        }
        Flush();hostRecovery.Tick();guestRecovery.Tick();
        Assert.That(hostRecovery.Recovering || guestRecovery.Recovering,Is.False);Assert.That(host.Ready && guest.Ready,Is.True);
        var after=guest.Client.Capture();Assert.That(after.settings.homeTeamName,Is.EqualTo(before.settings.homeTeamName));Assert.That(after.settings.awayTeamName,Is.EqualTo(before.settings.awayTeamName));
        Assert.That(after.homeSequence,Is.EqualTo(before.homeSequence));Assert.That(after.awaySequence,Is.EqualTo(before.awaySequence));
        Assert.That(guest.Client.Pending,Is.False);
        if(branch=="agreed"){Assert.That(after.phase,Is.EqualTo("draft-ready"));Assert.That(after.homeReady && after.awayReady,Is.True);Assert.That(notifications,Is.EqualTo(1));}
        else Assert.That(after.homeReady || after.awayReady,Is.False,"Reconnect invalidates setup consent");
    }
}
