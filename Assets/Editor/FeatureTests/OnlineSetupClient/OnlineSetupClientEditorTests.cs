using System;
using System.Collections.Generic;
using CounterAttack.Online;
using NUnit.Framework;
[Category("OnlineSetupClient")]
public sealed class OnlineSetupClientEditorTests
{
    private OnlineSetupAuthority authority;private OnlineMatchProtocol protocol;
    private OnlineSetupClient host,guest;
    private string hostConnection,guestConnection,guestAccount;
    private readonly Queue<(OnlineSetupClient client,OnlineSetupRequest request)> queue=new();
    private static readonly OnlineCompatibility Version=new("setup-client-1","rules-1","catalog-1");
    private int hostAgreed,guestAgreed;
    private void Create(bool hostAway)
    {
        hostAgreed=guestAgreed=0;queue.Clear();
        protocol=new OnlineMatchProtocol("setup-client","home","away",hostAway?"away":"home",Version);
        protocol.BindAuthenticatedAccount("home","h",Version);protocol.BindAuthenticatedAccount("away","a",Version);
        authority=new OnlineSetupAuthority(protocol,OnlineSetupAgreementEditorTests.Valid());
        host=new OnlineSetupClient(protocol.MatchId,hostAway?OnlineTeam.Away:OnlineTeam.Home);
        guest=new OnlineSetupClient(protocol.MatchId,hostAway?OnlineTeam.Home:OnlineTeam.Away);
        hostConnection=hostAway?"a":"h";guestConnection=hostAway?"h":"a";guestAccount=hostAway?"home":"away";
        host.Requested+=r=>queue.Enqueue((host,r));guest.Requested+=r=>queue.Enqueue((guest,r));
        host.DraftAgreed+=_=>hostAgreed++;guest.DraftAgreed+=_=>guestAgreed++;
        host.Apply(authority.Capture());guest.Apply(authority.Capture());
    }
    private void Flush()
    {
        for(int i=0;queue.Count>0 && i<32;i++)
        {
            var item=queue.Dequeue();var reply=authority.Submit(item.client==host?hostConnection:guestConnection,item.request);
            var state=authority.Capture();item.client.Receive(item.request.sequence,reply.Status,state);
            (item.client==host?guest:host).Apply(state);
        }
        Assert.That(queue,Is.Empty,"setup requests must settle without an echo loop");
    }
    private void Confirm(OnlineSetupClient client){Assert.That(client.Confirm(client.ViewSettings()),Is.True);Flush();}
    private void Ready(){Confirm(host);Confirm(guest);Assert.That(host.BothReady && guest.BothReady,Is.True);Assert.That(authority.Capture().configurationRevision,Is.Zero,"unchanged confirmation preserves the other player's agreement");}
    private static void Name(GameSettings s,OnlineTeam team,string value){if(team==OnlineTeam.Home)s.homeTeamName=value;else s.awayTeamName=value;}
    private static string Name(GameSettings s,OnlineTeam team)=>team==OnlineTeam.Home?s.homeTeamName:s.awayTeamName;
    [TestCase(false)]
    [TestCase(true)]
    public void HostSwitchToInternationalPublishesBothNationalTeams(bool hostAway)
    {
        Create(hostAway);
        var setup=host.ViewSettings();
        setup.matchType="International";
        setup.squadSize="18";
        setup.draft=setup.gkDraft="International";
        setup.homeTeamName="England";setup.awayTeamName="Germany";
        host.Edit(setup);Flush();
        var observed=guest.ViewSettings();
        Assert.That(observed.matchType,Is.EqualTo("International"));
        Assert.That(observed.homeTeamName,Is.EqualTo("England"));
        Assert.That(observed.awayTeamName,Is.EqualTo("Germany"));
        Assert.That(observed.squadSize,Is.EqualTo("18"));
    }
    [TestCase(false)]
    [TestCase(true)]
    public void UnconfirmedEditsReachBothManagers(bool hostAway)
    {
        Create(hostAway);
        var hostSelection=host.ViewSettings();
        Name(hostSelection,host.Team,"Host live");
        hostSelection.halfDuration=30;
        host.Edit(hostSelection);
        var guestSelection=guest.ViewSettings();
        Name(guestSelection,guest.Team,"Guest live");
        guest.Edit(guestSelection);
        Flush();
        Assert.That(Name(guest.ViewSettings(),host.Team),Is.EqualTo("Host live"));
        Assert.That(Name(host.ViewSettings(),guest.Team),Is.EqualTo("Guest live"));
        Assert.That(guest.ViewSettings().halfDuration,Is.EqualTo(30));
        Assert.That(host.LocalReady || guest.LocalReady,Is.False);
    }
    [Test] public void ClientFlow([Values(false,true)]bool hostAway,[Values("agree-start","local-edit","remote-edit","pending-edit","reconnect","rejected","role-filter","detached","duplicate-ack","stale-snapshot","wrong-match","revision-conflict")]string branch)
    {
        Create(hostAway);
        if(branch=="agree-start")
        {
            Ready();Assert.That(guest.CanConfirm,Is.False);Assert.That(host.CanConfirm,Is.True);Confirm(host);
            Assert.That(hostAgreed,Is.EqualTo(1));Assert.That(guestAgreed,Is.EqualTo(1));
            host.Apply(authority.Capture());guest.Apply(authority.Capture());Assert.That(hostAgreed+guestAgreed,Is.EqualTo(2));return;
        }
        if(branch=="local-edit")
        {
            Ready();var draft=guest.ViewSettings();Name(draft,guest.Team,"Edited after ready");guest.Edit(draft);Flush();
            Assert.That(guest.LocalReady,Is.False);Assert.That(host.BothReady,Is.False);Assert.That(Name(guest.ViewSettings(),guest.Team),Is.EqualTo("Edited after ready"));return;
        }
        if(branch=="remote-edit" || branch=="role-filter")
        {
            var own=host.ViewSettings();Name(own,host.Team,"Host unsent edit");host.Edit(own);
            var theirs=guest.ViewSettings();Name(theirs,guest.Team,"Guest confirmed edit");Name(theirs,host.Team,"Forged opponent");theirs.halfDuration=30;guest.Edit(theirs);
            Assert.That(guest.ViewSettings().halfDuration,Is.EqualTo(45));Assert.That(Name(guest.ViewSettings(),host.Team),Is.Not.EqualTo("Forged opponent"));
            Flush();Confirm(guest);Assert.That(Name(host.ViewSettings(),host.Team),Is.EqualTo("Host unsent edit"));
            Assert.That(Name(host.ViewSettings(),guest.Team),Is.EqualTo("Guest confirmed edit"));Assert.That(authority.Capture().settings.halfDuration,Is.EqualTo(45));return;
        }
        if(branch=="pending-edit")
        {
            var first=guest.ViewSettings();Name(first,guest.Team,"First selection");guest.Edit(first);Assert.That(guest.Pending,Is.True);
            var second=guest.ViewSettings();Name(second,guest.Team,"Second selection");guest.Edit(second);Flush();
            Assert.That(guest.Pending,Is.False);Assert.That(guest.LocalReady,Is.False);Assert.That(Name(guest.ViewSettings(),guest.Team),Is.EqualTo("Second selection"));
            Confirm(guest);Assert.That(Name(authority.Capture().settings,guest.Team),Is.EqualTo("Second selection"));return;
        }
        if(branch=="reconnect")
        {
            Ready();protocol.BindAuthenticatedAccount(guestAccount,"returning",Version);guestConnection="returning";
            host.Apply(authority.Capture());guest.Apply(authority.Capture());Assert.That(host.BothReady || guest.LocalReady,Is.False);
            Confirm(guest);Assert.That(guest.LocalReady,Is.True);Assert.That(host.LocalReady,Is.False);return;
        }
        if(branch=="rejected")
        {
            var draft=guest.ViewSettings();Name(draft,guest.Team,"Keep this draft");guest.Edit(draft);guest.Confirm(guest.ViewSettings());
            var state=authority.Capture();var direct=new OnlineSetupRequest{matchId=state.matchId,sequence=1,revision=state.revision,intent=OnlineSetupIntent.Settings,settings=state.settings};direct.settings.halfDuration=30;
            Assert.That(authority.Submit(hostConnection,direct).Status,Is.EqualTo(OnlineCommandStatus.Accepted));Flush();
            Assert.That(guest.LastReply,Is.EqualTo(OnlineCommandStatus.Accepted));Assert.That(guest.Pending,Is.False);
            Assert.That(Name(guest.ViewSettings(),guest.Team),Is.EqualTo("Keep this draft"));Assert.That(guest.ViewSettings().halfDuration,Is.EqualTo(30));Confirm(guest);return;
        }
        if(branch=="detached")
        {
            var state=authority.Capture();host.Apply(state);state.settings.homeTeamName="Injected";
            var copy=host.Capture();copy.settings.homeTeamName="Injected twice";
            Assert.That(host.ViewSettings().homeTeamName,Is.EqualTo("Home club"));return;
        }
        if(branch=="duplicate-ack")
        {
            guest.Confirm(guest.ViewSettings());var item=queue.Dequeue();var reply=authority.Submit(guestConnection,item.request);var state=authority.Capture();
            Assert.That(guest.Receive(item.request.sequence,reply.Status,state),Is.True);
            var duplicate=authority.Submit(guestConnection,item.request);Assert.That(duplicate.Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
            Assert.That(guest.Receive(item.request.sequence,duplicate.Status,state),Is.True);Assert.That(queue,Is.Empty);Assert.That(guest.LocalReady,Is.True);return;
        }
        if(branch=="stale-snapshot")
        {var old=authority.Capture();Confirm(host);Assert.That(host.Apply(old),Is.False);Assert.That(host.LocalReady,Is.True);return;}
        if(branch=="wrong-match")
        {
            guest.Confirm(guest.ViewSettings());var bad=authority.Capture();bad.matchId="another-match";
            Assert.That(guest.Receive(1,OnlineCommandStatus.Accepted,bad),Is.False);Assert.That(guest.Pending,Is.True);Flush();return;
        }
        if(branch=="revision-conflict")
        {
            var bad=authority.Capture();bad.settings.homeTeamName="Different same revision";
            Assert.That(host.Apply(bad),Is.False);Assert.That(host.ViewSettings().homeTeamName,Is.EqualTo("Home club"));return;
        }
    }
}
