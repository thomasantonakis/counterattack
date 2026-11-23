using System;
using System.Collections.Generic;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineDraft")]
public sealed class OnlineDraftAuthorityEditorTests
{
    private OnlineMatchProtocol protocol;
    private OnlineDraftAuthority draft;
    private readonly Queue<int> dice=new();
    private OnlineDraftRequest Request(OnlineTeam actor,OnlineDraftIntent intent,string card=null,int slot=0,int other=0)
    {var s=draft.Capture();return new(){matchId=s.matchId,sequence=(actor==OnlineTeam.Home?s.homeSequence:s.awaySequence)+1,revision=s.revision,intent=intent,cardId=card,slot=slot,otherSlot=other};}
    private string Connection(OnlineTeam team)=>team==OnlineTeam.Home?"h":"a";
    private OnlineCommandStatus Submit(OnlineTeam actor,OnlineDraftIntent intent,string card=null,int slot=0,int other=0)=>draft.Submit(Connection(actor),Request(actor,intent,card,slot,other)).Status;
    private void Create(bool hostAway,int size,Func<double> clock=null)
    {
        var version=new OnlineCompatibility("draft-test","rules","catalog");
        protocol=new("draft-test","home","away",hostAway?"away":"home",version);
        protocol.BindAuthenticatedAccount("home","h",version);protocol.BindAuthenticatedAccount("away","a",version);
        var settings=OnlineSetupAgreementEditorTests.Valid();settings.squadSize=size.ToString();
        dice.Clear();draft=new(protocol,settings,OnlineDraftCatalog.Load(),_=>0,()=>dice.Dequeue(),clock);
    }
    [TestCase(true)]
    [TestCase(false)]
    public void CompletedRollHoldsBeforePicksOrReroll(bool tie)
    {
        double now=4;
        Create(false,16,()=>now);
        dice.Enqueue(6);dice.Enqueue(tie?6:1);
        Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Roll),Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(Submit(OnlineTeam.Away,OnlineDraftIntent.Roll),Is.EqualTo(OnlineCommandStatus.Accepted));
        var result=draft.Capture();
        Assert.That(result.phase,Is.EqualTo("roll-result"));
        Assert.That(result.homeRoll,Is.EqualTo(6));
        Assert.That(result.awayRoll,Is.EqualTo(tie?6:1));
        Assert.That(result.cards,Is.Empty);
        Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Roll),Is.EqualTo(OnlineCommandStatus.NotReady));
        now=8.999;
        Assert.That(draft.AdvanceRollResult(),Is.False);
        now=9;
        Assert.That(draft.AdvanceRollResult(),Is.True);
        Assert.That(draft.Capture().phase,Is.EqualTo(tie?"roll":"pick"));
        Assert.That(draft.AdvanceRollResult(),Is.False);
    }
    private void StartPicks(bool homeWins=true)
    {dice.Enqueue(homeWins?6:1);dice.Enqueue(homeWins?1:6);Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Roll),Is.EqualTo(OnlineCommandStatus.Accepted));Assert.That(Submit(OnlineTeam.Away,OnlineDraftIntent.Roll),Is.EqualTo(OnlineCommandStatus.Accepted));}
    private void Pick()
    {var s=draft.Capture();var roster=s.turn==OnlineTeam.Home?s.home:s.away;int slot=Array.FindIndex(roster,c=>c==null)+1;Assert.That(Submit(s.turn,OnlineDraftIntent.Pick,s.cards.First(card=>card!=null).id,slot),Is.EqualTo(OnlineCommandStatus.Accepted));}
    [Test]
    public void PickLeavesMiddleOfferEmptyUntilTheNextBatch()
    {
        Create(false,16);
        StartPicks();
        var before=draft.Capture();
        Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Pick,before.cards[1].id,2),Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(draft.Capture().cards.Select(card=>card?.id),
            Is.EqualTo(new[]{before.cards[0].id,null,before.cards[2].id,before.cards[3].id}));
        Pick();
        Pick();
        Pick();
        var next=draft.Capture();
        Assert.That(next.batch,Is.EqualTo(before.batch+1));
        Assert.That(next.cards,Has.All.Not.Null);
    }
    [Test] public void RealCatalogDraft([Values(false,true)]bool hostAway,[Values(16,18)]int size,[Values("roll","ties","batch","boundaries","identity","duplicate","snapshot","swap","recovery","complete")]string branch)
    {
        Create(hostAway,size);var initial=draft.Capture();
        Assert.That(initial.home.Length,Is.EqualTo(size));Assert.That(initial.away.Length,Is.EqualTo(size));
        Assert.That(new[]{initial.home[0],initial.home[11],initial.away[0],initial.away[11]}.Select(c=>c.id).Distinct().Count(),Is.EqualTo(4));
        Assert.That(initial.home[0].saving,Is.GreaterThanOrEqualTo(initial.home[11].saving));
        if(branch=="identity")
        {
            var request=Request(OnlineTeam.Home,OnlineDraftIntent.Roll);
            Assert.That(draft.Submit("stranger",request).Status,Is.EqualTo(OnlineCommandStatus.UnknownConnection));
            request.matchId="another-match";Assert.That(draft.Submit("h",request).Status,Is.EqualTo(OnlineCommandStatus.WrongMatch));return;
        }
        if(branch=="ties")
        {
            dice.Enqueue(4);dice.Enqueue(4);Submit(OnlineTeam.Home,OnlineDraftIntent.Roll);Submit(OnlineTeam.Away,OnlineDraftIntent.Roll);
            var tied=draft.Capture();Assert.That(tied.rollRound,Is.EqualTo(2));Assert.That(tied.phase,Is.EqualTo("roll"));Assert.That(tied.cards,Is.Empty);
            StartPicks(false);Assert.That(draft.Capture().turn,Is.EqualTo(OnlineTeam.Away));return;
        }
        Assert.That(Submit(OnlineTeam.Away,OnlineDraftIntent.Roll),Is.EqualTo(OnlineCommandStatus.WrongTeam));StartPicks();
        if(branch=="roll"){var s=draft.Capture();Assert.That(s.homeRoll,Is.EqualTo(6));Assert.That(s.awayRoll,Is.EqualTo(1));Assert.That(s.phase,Is.EqualTo("pick"));Assert.That(s.cards.Length,Is.EqualTo(4));return;}
        if(branch=="boundaries")
        {
            var s=draft.Capture();long revision=s.revision;
            Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Pick,s.cards.First(card=>card!=null).id,1),Is.EqualTo(OnlineCommandStatus.IllegalAction));
            Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Pick,"unknown",2),Is.EqualTo(OnlineCommandStatus.IllegalAction));
            Assert.That(Submit(OnlineTeam.Away,OnlineDraftIntent.Pick,s.cards.First(card=>card!=null).id,2),Is.EqualTo(OnlineCommandStatus.WrongTeam));
            Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Pick,s.cards.First(card=>card!=null).id,size+1),Is.EqualTo(OnlineCommandStatus.IllegalAction));
            Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Ready),Is.EqualTo(OnlineCommandStatus.NotReady));Assert.That(draft.Capture().revision,Is.EqualTo(revision));return;
        }
        if(branch=="duplicate")
        {
            var s=draft.Capture();var r=Request(OnlineTeam.Home,OnlineDraftIntent.Pick,s.cards.First(card=>card!=null).id,2);
            Assert.That(draft.Submit("h",r).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(draft.Submit("h",r).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
            r.slot=3;Assert.That(draft.Submit("h",r).Status,Is.EqualTo(OnlineCommandStatus.SequenceConflict));Assert.That(draft.Capture().cards.Count(card=>card!=null),Is.EqualTo(3));return;
        }
        if(branch=="snapshot")
        {
            var s=draft.Capture();s.cards[0].name="Injected";s.home[0].saving=999;s.settings.homeTeamName="Injected";
            var real=draft.Capture();Assert.That(real.cards[0].name,Is.Not.EqualTo("Injected"));Assert.That(real.home[0].saving,Is.LessThan(999));Assert.That(real.settings.homeTeamName,Is.Not.EqualTo("Injected"));return;
        }
        if(branch=="batch")
        {
            var ids=draft.Capture().cards.Select(c=>c.id).ToArray();for(int i=0;i<4;i++)Pick();
            var next=draft.Capture();Assert.That(next.batch,Is.EqualTo(2));Assert.That(next.turn,Is.EqualTo(OnlineTeam.Away));Assert.That(next.cards.Select(c=>c.id).Intersect(ids),Is.Empty);return;
        }
        if(branch=="swap")
        {
            Pick();var s=draft.Capture();string id=s.home[1].id;
            Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Swap,null,2,1),Is.EqualTo(OnlineCommandStatus.IllegalAction));
            Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Swap,null,2,3),Is.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(draft.Capture().home[2].id,Is.EqualTo(id));Assert.That(draft.Capture().home[1],Is.Null);return;
        }
        while(draft.Capture().phase=="pick")Pick();var finished=draft.Capture();
        Assert.That(finished.home.Concat(finished.away).Select(c=>c.id).Distinct().Count(),Is.EqualTo(size*2));
        Assert.That(finished.batch,Is.EqualTo((size-2)/2));
        Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Ready),Is.EqualTo(OnlineCommandStatus.Accepted));Assert.That(Submit(OnlineTeam.Away,OnlineDraftIntent.Ready),Is.EqualTo(OnlineCommandStatus.Accepted));
        if(branch=="recovery")
        {
            protocol.Disconnect("a");Assert.That(draft.Capture().homeReady || draft.Capture().awayReady,Is.False);
            Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Start),Is.EqualTo(OnlineCommandStatus.Disconnected));
            protocol.BindAuthenticatedAccount("away","a2",protocol.Compatibility);Assert.That(draft.Capture().away.Select(c=>c.id),Is.EqualTo(finished.away.Select(c=>c.id)));return;
        }
        var host=protocol.HostTeam;var guest=host==OnlineTeam.Home?OnlineTeam.Away:OnlineTeam.Home;
        Assert.That(Submit(guest,OnlineDraftIntent.Start),Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(Submit(host,OnlineDraftIntent.Start),Is.EqualTo(OnlineCommandStatus.Accepted));Assert.That(draft.Capture().phase,Is.EqualTo("opening-roll"));
    }
}
