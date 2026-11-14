using System;
using System.Collections.Generic;
using CounterAttack.Online;
using NUnit.Framework;
[Category("OnlineSessionTransport")]
public sealed class OnlineSetupChannelEditorTests
{
    private OnlineSetupHostChannel host;private OnlineSetupGuestChannel guest;private OnlineMatchProtocol protocol;
    private readonly Queue<(OnlineSessionChannel receiver,string sender,byte[] packet)> queue=new();
    private string hostAccount,guestAccount;private double now;
    private static readonly OnlineCompatibility Version=new("session-wire-v1","frontend-rules-1","catalog-1");
    private void Create(bool hostAway,bool connect=true)
    {
        queue.Clear();now=0;hostAccount=hostAway?"away":"home";guestAccount=hostAway?"home":"away";
        protocol=new OnlineMatchProtocol("frontend-channel","home","away",hostAccount,Version);
        protocol.BindAuthenticatedAccount(hostAccount,"host-connection",Version);
        host=new(protocol,"host-connection",guestAccount,OnlineSetupAgreementEditorTests.Valid(),()=>now);
        guest=new(protocol.MatchId,hostAccount,Version,protocol.HostTeam,()=>now);
        host.Outgoing+=p=>queue.Enqueue((guest,hostAccount,p));guest.Outgoing+=p=>queue.Enqueue((host,guestAccount,p));
        host.Begin();if(connect)Flush();
    }
    private void Deliver()
    {var next=queue.Dequeue();Assert.That(next.receiver.Receive(next.sender,next.packet,next.packet.Length),Is.True);}
    private void Flush(){for(int i=0;queue.Count>0 && i<512;i++)Deliver();Assert.That(queue,Is.Empty);}
    private bool Inject(OnlineSessionChannel receiver,string sender,OnlineSessionMessage message)
    {bool accepted=true;foreach(var packet in OnlineSessionWire.Encode(message))accepted=receiver.Receive(sender,packet,packet.Length) && accepted;return accepted;}
    private OnlineSessionMessage Request(string nonce)=>new(){kind="setup-request",matchId=protocol.MatchId,nonce=nonce,request=new OnlineSetupRequest{matchId=protocol.MatchId,sequence=1,revision=host.Authority.Capture().revision,intent=OnlineSetupIntent.Ready}};
    private void Ready()
    {Assert.That(host.Client.Confirm(host.Client.ViewSettings()),Is.True);Flush();Assert.That(guest.Client.Confirm(guest.Client.ViewSettings()),Is.True);Flush();Assert.That(host.Client.BothReady,Is.True);}
    [Test] public void AuthenticatedChannel([Values(false,true)]bool hostAway,[Values("agreement","unauthenticated","before-hello","wrong-version","wrong-catalog","nonce","reconnect","heartbeat","leave","guest-injection","stale-frame","local-loss","reconnect-after-start")]string branch)
    {
        bool before=branch is "before-hello" or "wrong-version" or "wrong-catalog";Create(hostAway,!before);
        if(branch=="before-hello")
        {Assert.That(Inject(host,guestAccount,Request(host.Nonce)),Is.False);Assert.That(host.Authority.Capture().connected,Is.False);return;}
        if(branch is "wrong-version" or "wrong-catalog")
        {
            var hello=new OnlineSessionMessage{kind="hello",matchId=protocol.MatchId,nonce=host.Nonce,protocol=branch=="wrong-version"?"older":Version.Protocol,rules=Version.Rules,catalog=branch=="wrong-catalog"?"other":Version.Catalog};
            Assert.That(Inject(host,guestAccount,hello),Is.False);Assert.That(host.Ready,Is.False);Assert.That(host.Fault,Is.Not.Empty);return;
        }
        Assert.That(host.Ready && guest.Ready,Is.True);
        if(branch=="agreement")
        {Ready();Assert.That(host.Client.Confirm(host.Client.ViewSettings()),Is.True);Flush();Assert.That(guest.Client.Capture().phase,Is.EqualTo("draft-ready"));return;}
        if(branch=="unauthenticated")
        {Assert.That(host.Receive("stranger",new byte[]{1,2,3},3),Is.False);Ready();return;}
        if(branch=="nonce")
        {Assert.That(Inject(host,guestAccount,Request(Guid.NewGuid().ToString("N"))),Is.False);Assert.That(host.Authority.Capture().homeSequence+host.Authority.Capture().awaySequence,Is.Zero);return;}
        if(branch=="reconnect" || branch=="stale-frame" || branch=="reconnect-after-start")
        {
            int agreed=0;guest.Client.DraftAgreed+=_=>agreed++;
            Ready();if(branch=="reconnect-after-start"){host.Client.Confirm(host.Client.ViewSettings());Flush();Assert.That(agreed,Is.EqualTo(1));}
            string old=host.Nonce;var stale=Request(old);host.TransportLost();guest.TransportLost();
            Assert.That(host.Client.Connected || guest.Client.Connected,Is.False);host.Begin();Flush();
            Assert.That(host.Nonce,Is.Not.EqualTo(old));Assert.That(Inject(host,guestAccount,stale),Is.False);
            if(branch=="reconnect-after-start")
            {
                Assert.That(host.Authority.Capture().phase,Is.EqualTo("draft-ready"));Assert.That(host.Ready && guest.Ready,Is.True);
                Assert.That(guest.Client.CanConfirm,Is.False);Assert.That(agreed,Is.EqualTo(1));return;
            }
            Assert.That(host.Client.BothReady || guest.Client.LocalReady,Is.False);
            Assert.That(host.Authority.Capture().phase,Is.EqualTo("setup"));
            Assert.That(guest.Client.Confirm(guest.Client.ViewSettings()),Is.True);Flush();Assert.That(guest.Client.LocalReady,Is.True);return;
        }
        if(branch=="heartbeat")
        {
            now=1;host.Tick();Assert.That(queue.Count,Is.EqualTo(1));now=1.04;Deliver();now=1.09;Flush();
            var sample=host.Latency.Capture();Assert.That(sample.samples,Is.EqualTo(1));Assert.That(sample.lastMilliseconds,Is.EqualTo(90).Within(.001));return;
        }
        if(branch=="leave")
        {guest.Leave();Flush();Assert.That(host.PeerLeft,Is.True);Assert.That(host.Client.Connected,Is.False);return;}
        if(branch=="guest-injection")
        {
            var state=host.Authority.Capture();state.settings.homeTeamName="Forged host state";
            Assert.That(Inject(host,guestAccount,new OnlineSessionMessage{kind="setup-state",matchId=protocol.MatchId,nonce=host.Nonce,state=state}),Is.False);
            Assert.That(host.Authority.Capture().settings.homeTeamName,Is.EqualTo("Home club"));return;
        }
        if(branch=="local-loss")
        {
            guest.TransportLost();Assert.That(guest.Client.Confirm(guest.Client.ViewSettings()),Is.False);
            guest.Client.Apply(host.Authority.Capture());Assert.That(guest.Client.Connected,Is.False,"a cached snapshot cannot lift local transport loss");return;
        }
    }
}
