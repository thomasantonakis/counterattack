using System;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
[Category("OnlineShootout")]
public sealed class OnlineShootoutEditorTests
{
    [TestCase("MatchEnded",false,false)][TestCase("MatchEnded",true,true)]
    [TestCase("PenaltyShootoutComplete",false,false)][TestCase("PenaltyShootoutComplete",true,true)]
    [TestCase("PenaltyShootoutOrderSelection",false,false)][TestCase("PenaltyShootoutOrderSelection",true,false)]
    public void FullRoomFinishesOnlyAfterRealCompletion(string phase,bool complete,bool expected)
    {
        var state=new OnlineGroundPassState{catalogId=OnlineFullRoomBootstrap.Catalog,gameState=phase,matchComplete=complete,shootout=new OnlineShootoutSnapshot{complete=complete}};
        Assert.That(OnlineManualSessionPolicy.CanFinish(state,true),Is.EqualTo(expected));
        state.playerMoving=true;Assert.That(OnlineManualSessionPolicy.CanFinish(state,true),Is.False);
        state.playerMoving=false;state.connectionHeld=true;Assert.That(OnlineManualSessionPolicy.CanFinish(state,true),Is.False);
    }
    [TestCase("missing-order")][TestCase("duplicate")][TestCase("cross-team")][TestCase("ineligible")][TestCase("unequal")][TestCase("sent-off")]
    [TestCase("score")][TestCase("results")][TestCase("attempts")][TestCase("phase")][TestCase("unknown-shooter")][TestCase("unknown-keeper")][TestCase("missing-eligible")]
    public void InvalidShootoutCannotAdvanceGuest(string branch)
    {
        var state=new OnlineGroundPassState{matchId="shootout-validation",catalogId=OnlineFullRoomBootstrap.Catalog,publicationSequence=1,
            homeDirection="LeftToRight",awayDirection="RightToLeft",homeStats=new MatchManager.TeamStats(),awayStats=new MatchManager.TeamStats(),legalActions=Array.Empty<string>(),
            tokens=Enumerable.Range(0,32).Select(i=>{string side=i<16?"Home":"Away";int jersey=i%16+1;
                return new OnlineGroundPassTokenState{id=side.ToLowerInvariant()+":"+jersey,playerStats=new MatchManager.PlayerStats(),runtime=new RoomTokenSnapshot{tokenKey=side+":"+jersey,teamSide=side,jerseyNumber=jersey,playerName="Shootout "+i,isPlaying=jersey<=11}};}).ToArray()};
        var home=state.tokens.Where(t=>t.runtime.teamSide=="Home" && t.runtime.isPlaying).Select(t=>t.id).ToArray();var away=state.tokens.Where(t=>t.runtime.teamSide=="Away" && t.runtime.isPlaying).Select(t=>t.id).ToArray();
        state.shootout=new OnlineShootoutSnapshot{selecting=true,homeOrder=home,awayOrder=away,homeEligible=home,awayEligible=away,homeResults=Array.Empty<bool>(),awayResults=Array.Empty<bool>()};
        var replica=new OnlineGroundPassReplica(state.matchId,state.tokens.Select(t=>t.id),OnlineFullRoomBootstrap.Catalog);Assert.That(replica.Apply(state),Is.True);
        var invalid=JsonUtility.FromJson<OnlineGroundPassState>(JsonUtility.ToJson(state));invalid.publicationSequence=2;var s=invalid.shootout;
        switch(branch)
        {
            case "missing-order":s.homeOrder=null;break;
            case "duplicate":s.homeOrder[1]=s.homeOrder[0];break;
            case "cross-team":s.homeOrder[0]="away:1";break;
            case "ineligible":s.homeOrder[0]="home:12";break;
            case "unequal":s.homeOrder=s.homeOrder.Skip(1).ToArray();break;
            case "sent-off":invalid.tokens[0].runtime.isSentOff=true;invalid.tokens[0].runtime.isPlaying=false;break;
            case "score":s.homeScore=1;break;
            case "results":s.homeResults=new[]{true};break;
            case "attempts":s.homeAttempts=-1;break;
            case "phase":s.active=true;break;
            case "unknown-shooter":s.shooter="home:19";break;
            case "unknown-keeper":s.keeper="away:19";break;
            case "missing-eligible":s.awayEligible=null;break;
        }
        Assert.That(replica.Apply(invalid),Is.False);Assert.That(replica.State.publicationSequence,Is.EqualTo(1));state.publicationSequence=2;Assert.That(replica.Apply(state),Is.True);
    }
}
