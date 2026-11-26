using System.Collections.Generic;
using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineOpeningSetup")]
public sealed class OnlineOpeningEditorTests
{
    private OnlineMatchProtocol protocol;private OnlineDraftAuthority authority;
    private readonly Queue<int> dice=new();private string awayConnection="a";
    private OnlineDraftRequest last;
    private void Create(bool hostAway)
    {
        dice.Clear();awayConnection="a";
        var v=new OnlineCompatibility(OnlineSessionWire.Version,"online-opening","catalog");protocol=new("online-opening","home","away",hostAway?"away":"home",v);
        protocol.BindAuthenticatedAccount("home","h",v);protocol.BindAuthenticatedAccount("away","a",v);
        var s=OnlineSetupAgreementEditorTests.Valid();s.matchType="International";s.squadSize="18";s.draft=s.gkDraft="International";s.homeTeamName="England";s.awayTeamName="Germany";
        authority=new(protocol,s,OnlineDraftCatalog.Load(),_=>0,()=>dice.Dequeue());
        Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Ready),Is.EqualTo(OnlineCommandStatus.Accepted));Assert.That(Submit(OnlineTeam.Away,OnlineDraftIntent.Ready),Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(Submit(protocol.HostTeam,OnlineDraftIntent.Start),Is.EqualTo(OnlineCommandStatus.Accepted));Assert.That(authority.Capture().phase,Is.EqualTo("opening-roll"));
    }
    private OnlineCommandStatus Submit(OnlineTeam team,OnlineDraftIntent intent,CoinTossChoice choice=CoinTossChoice.Kickoff)
    {
        var s=authority.Capture();last=new(){matchId=s.matchId,revision=s.revision,sequence=(team==OnlineTeam.Home?s.homeSequence:s.awaySequence)+1,intent=intent,choice=choice,slot=1,otherSlot=12};
        return authority.Submit(team==OnlineTeam.Home?"h":awayConnection,last).Status;
    }
    private void RollPair(int home,int away)
    {dice.Enqueue(home);dice.Enqueue(away);Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Roll),Is.EqualTo(OnlineCommandStatus.Accepted));Assert.That(Submit(OnlineTeam.Away,OnlineDraftIntent.Roll),Is.EqualTo(OnlineCommandStatus.Accepted));}
    [Test] public void OpeningUsesExistingKickoffAndDirectionRules([Values(false,true)]bool hostAway,[Values(false,true)]bool homeWins,[Values(CoinTossChoice.Kickoff,CoinTossChoice.LeftToRight,CoinTossChoice.RightToLeft)]CoinTossChoice first)
    {
        Create(hostAway);RollPair(homeWins?6:1,homeWins?1:6);var winner=homeWins?OnlineTeam.Home:OnlineTeam.Away;var loser=homeWins?OnlineTeam.Away:OnlineTeam.Home;
        Assert.That(authority.Capture().turn,Is.EqualTo(winner));Assert.That(Submit(winner,OnlineDraftIntent.Choose,first),Is.EqualTo(OnlineCommandStatus.Accepted));
        if(first==CoinTossChoice.Kickoff){Assert.That(authority.Capture().turn,Is.EqualTo(loser));Assert.That(authority.Capture().openingCanChooseKickoff,Is.False);Assert.That(Submit(loser,OnlineDraftIntent.Choose,CoinTossChoice.LeftToRight),Is.EqualTo(OnlineCommandStatus.Accepted));}
        var s=authority.Capture();Assert.That(s.phase,Is.EqualTo("room-ready"));Assert.That(s.openingSetup.coinTossWinnerIsHome,Is.EqualTo(homeWins));
        Assert.That(s.openingSetup.homeHasFirstHalfKickoff,Is.EqualTo(first==CoinTossChoice.Kickoff?homeWins:!homeWins));
        Assert.That(s.openingSetup.homeAttacksLeftToRight,Is.EqualTo(first==CoinTossChoice.Kickoff?!homeWins:homeWins==(first==CoinTossChoice.LeftToRight)));
        var existing=new CoinTossDecision(homeWins);existing.Choose(first);if(!existing.IsComplete)existing.Choose(CoinTossChoice.LeftToRight);
        Assert.That(s.openingSetup.DirectionFor(true),Is.EqualTo(existing.Result.DirectionFor(true)));Assert.That(s.openingSetup.KickoffTeam,Is.EqualTo(existing.Result.KickoffTeam));
        s.openingSetup.homeHasFirstHalfKickoff=!s.openingSetup.homeHasFirstHalfKickoff;Assert.That(authority.Capture().openingSetup.homeHasFirstHalfKickoff,Is.EqualTo(existing.Result.homeHasFirstHalfKickoff));
    }
    [Test] public void OpeningIsOwnedAndRecoverable([Values(false,true)]bool hostAway,[Values("invalid","rejoin","duplicate","ties")]string branch)
    {
        Create(hostAway);
        if(branch=="ties"){RollPair(3,3);Assert.That(authority.Capture().phase,Is.EqualTo("opening-roll"));Assert.That(authority.Capture().rollRound,Is.EqualTo(2));}
        if(branch=="duplicate")
        {
            dice.Enqueue(6);dice.Enqueue(1);Submit(OnlineTeam.Home,OnlineDraftIntent.Roll);var accepted=last;
            Assert.That(authority.Submit("h",accepted).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));Assert.That(dice.Count,Is.EqualTo(1));Submit(OnlineTeam.Away,OnlineDraftIntent.Roll);
        }
        else RollPair(6,1);
        if(branch=="invalid")
        {
            long revision=authority.Capture().revision;
            Assert.That(Submit(OnlineTeam.Away,OnlineDraftIntent.Choose,CoinTossChoice.Kickoff),Is.EqualTo(OnlineCommandStatus.WrongTeam));
            Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Choose,CoinTossChoice.LeftSide),Is.EqualTo(OnlineCommandStatus.IllegalAction));
            Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Swap),Is.EqualTo(OnlineCommandStatus.IllegalAction));
            Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Unready),Is.EqualTo(OnlineCommandStatus.NotReady));Assert.That(authority.Capture().revision,Is.EqualTo(revision));
        }
        Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Choose),Is.EqualTo(OnlineCommandStatus.Accepted));
        if(branch=="duplicate"){Assert.That(authority.Submit("h",last).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));Assert.That(authority.Capture().turn,Is.EqualTo(OnlineTeam.Away));}
        if(branch=="rejoin")
        {
            protocol.Disconnect("a");var held=authority.Capture();Assert.That(held.phase,Is.EqualTo("opening-choice"));Assert.That(held.homeReady && held.awayReady,Is.True);
            Assert.That(Submit(OnlineTeam.Home,OnlineDraftIntent.Choose),Is.EqualTo(OnlineCommandStatus.Disconnected));protocol.BindAuthenticatedAccount("away","a2",protocol.Compatibility);awayConnection="a2";
            Submit(OnlineTeam.Away,OnlineDraftIntent.Choose,CoinTossChoice.LeftToRight);Assert.That(authority.Submit("a",last).Status,Is.EqualTo(OnlineCommandStatus.UnknownConnection));
        }
        else Assert.That(Submit(OnlineTeam.Away,OnlineDraftIntent.Choose,CoinTossChoice.LeftToRight),Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(authority.Capture().phase,Is.EqualTo("room-ready"));
    }
}
