using System;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineDraftVariants")]
public sealed class OnlineDraftVariantEditorTests
{
    private OnlineDraftAuthority authority;private OnlineMatchProtocol protocol;
    private OnlineDraftClient replica;
    private void Create(bool hostAway,GameSettings settings)
    {
        var v=new OnlineCompatibility(OnlineSessionWire.Version,"draft-variants","catalog");protocol=new("draft-variants","home","away",hostAway?"away":"home",v);
        protocol.BindAuthenticatedAccount("home","h",v);protocol.BindAuthenticatedAccount("away","a",v);
        int rolls=0;authority=new(protocol,settings,OnlineDraftCatalog.Load(),_=>0,()=>++rolls%2==1?6:1);replica=new(protocol.MatchId,OnlineTeam.Home);
    }
    private void Submit(OnlineTeam actor,OnlineDraftIntent intent,string card=null,int slot=0)
    {
        var s=authority.Capture();var reply=authority.Submit(actor==OnlineTeam.Home?"h":"a",new OnlineDraftRequest{matchId=s.matchId,revision=s.revision,sequence=(actor==OnlineTeam.Home?s.homeSequence:s.awaySequence)+1,intent=intent,cardId=card,slot=slot});
        Assert.That(reply.Status,Is.EqualTo(OnlineCommandStatus.Accepted));Assert.That(replica.Apply(authority.Capture()),Is.True);
    }
    [Test] public void AllRegularModeCombinations([Values(false,true)]bool hostAway,[Values(16,18)]int size,[Values("Regular","Free Regular","Arcade")]string mode,[Values("Deal","Free","Arcade")]string keepers)
    {
        var settings=OnlineSetupAgreementEditorTests.Valid();settings.squadSize=size.ToString();settings.draft=mode;settings.gkDraft=keepers;
        settings.includeNonTabletopia=settings.includeInternationals=settings.includeNonTabletopiaGK=settings.includeInternationalsGK=true;
        Create(hostAway,settings);Assert.That(replica.Apply(authority.Capture()),Is.True);
        int selections=0;
        for(int step=0;step<100 && authority.Capture().phase!="review";step++)
        {
            var s=authority.Capture();
            if(s.phase=="roll"){Submit(s.turn,OnlineDraftIntent.Roll);continue;}
            bool keeper=s.selection=="goalkeepers";
            OnlineTeam actor=s.turn;
            if(s.repeatable)actor=s.home.Where((_,i)=>(i is 0 or 11)==keeper).Any(c=>c==null)?OnlineTeam.Home:OnlineTeam.Away;
            var roster=actor==OnlineTeam.Home?s.home:s.away;
            int slot=Enumerable.Range(0,roster.Length).First(i=>(i is 0 or 11)==keeper && roster[i]==null)+1;
            string id=s.freeSelection?s.available.First():s.cards.First().id;
            Submit(actor,OnlineDraftIntent.Pick,id,slot);selections++;
        }
        var final=authority.Capture();Assert.That(final.phase,Is.EqualTo("review"));Assert.That(final.home.Concat(final.away).All(c=>c!=null),Is.True);
        Assert.That(selections,Is.EqualTo((size-2)*2+(keepers=="Deal"?0:4)));
        var gks=final.home.Concat(final.away).Where(c=>c.goalkeeper).ToArray();Assert.That(gks.Length,Is.EqualTo(4));
        Assert.That(gks.Select(c=>c.id).Distinct().Count(),Is.EqualTo(keepers=="Arcade"?1:4));
        var players=final.home.Concat(final.away).Where(c=>!c.goalkeeper).ToArray();Assert.That(players.Select(c=>c.id).Distinct().Count(),Is.EqualTo(mode=="Arcade"?1:players.Length));
        Submit(OnlineTeam.Home,OnlineDraftIntent.Ready);Submit(OnlineTeam.Away,OnlineDraftIntent.Ready);Submit(protocol.HostTeam,OnlineDraftIntent.Start);
        Assert.That(authority.Capture().phase,Is.EqualTo("opening-roll"));
        var wire=new OnlineSessionWire();OnlineSessionMessage decoded=null;
        foreach(var packet in OnlineSessionWire.Encode(new OnlineSessionMessage{matchId=protocol.MatchId,kind="draft-state",draftState=authority.Capture()}))decoded=wire.Receive(packet,packet.Length)??decoded;
        Assert.That(decoded.draftState.home.Length,Is.EqualTo(size));
    }
    [Test] public void InternationalRostersUseRealNationalCards([Values(false,true)]bool hostAway)
    {
        var settings=OnlineSetupAgreementEditorTests.Valid();settings.matchType="International";settings.squadSize="18";settings.draft=settings.gkDraft="International";
        settings.homeTeamName="England";settings.awayTeamName="Germany";Create(hostAway,settings);
        var state=authority.Capture();Assert.That(state.phase,Is.EqualTo("review"));Assert.That(replica.Apply(state),Is.True);
        Assert.That(state.home.All(c=>c.country=="England"),Is.True);Assert.That(state.away.All(c=>c.country=="Germany"),Is.True);
        Assert.That(state.home.Concat(state.away).Select(c=>c.id).Distinct().Count(),Is.EqualTo(36));
        Assert.That(state.home[0].goalkeeper && state.home[11].goalkeeper,Is.True);
        Submit(OnlineTeam.Home,OnlineDraftIntent.Ready);Submit(OnlineTeam.Away,OnlineDraftIntent.Ready);Submit(protocol.HostTeam,OnlineDraftIntent.Start);
    }
}
