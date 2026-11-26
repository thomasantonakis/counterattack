using System;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
[Category("OnlineStoppageActions")]
public sealed class OnlineStoppageActionsEditorTests
{
    private static OnlineGroundPassState Valid()=>new OnlineGroundPassState{matchId="stoppage-validation",catalogId=OnlineFullRoomBootstrap.Catalog,publicationSequence=1,half=1,
        homeStats=new MatchManager.TeamStats(),awayStats=new MatchManager.TeamStats(),homeDirection="LeftToRight",awayDirection="RightToLeft",legalActions=Array.Empty<string>(),
        tokens=Enumerable.Range(0,32).Select(i=>{bool home=i<16;int jersey=i%16+1;string side=home?"Home":"Away";return new OnlineGroundPassTokenState{id=(home?"home:":"away:")+jersey,runtime=new RoomTokenSnapshot{tokenKey=side+":"+jersey,teamSide=side,jerseyNumber=jersey,playerName="Fixture"},playerStats=new MatchManager.PlayerStats()};}).ToArray()};
    [TestCase("negative-total")][TestCase("regulation-cap")][TestCase("extra-cap")][TestCase("negative-remaining")][TestCase("excess-remaining")]
    [TestCase("undetermined-counts")][TestCase("awaiting-before-expiry")][TestCase("awaiting-after-result")]
    public void MalformedAllowanceDoesNotAdvanceReplica(string branch)
    {
        var state=Valid();var replica=new OnlineGroundPassReplica(state.matchId,state.tokens.Select(t=>t.id),state.catalogId);Assert.That(replica.Apply(state),Is.True);
        var bad=OnlineGroundPassState.Copy(state);bad.publicationSequence=2;bad.halfExpired=true;bad.stoppageDetermined=true;bad.stoppageTotal=3;bad.stoppageRemaining=2;
        switch(branch){case "negative-total":bad.stoppageTotal=-1;break;case "regulation-cap":bad.stoppageTotal=8;break;case "extra-cap":bad.half=3;bad.stoppageTotal=4;break;case "negative-remaining":bad.stoppageRemaining=-1;break;case "excess-remaining":bad.stoppageRemaining=4;break;case "undetermined-counts":bad.stoppageDetermined=false;break;case "awaiting-before-expiry":bad.halfExpired=false;bad.stoppageDetermined=false;bad.stoppageTotal=bad.stoppageRemaining=0;bad.stoppageAwaitingRoll=true;break;case "awaiting-after-result":bad.stoppageAwaitingRoll=true;break;}
        Assert.That(replica.Apply(bad),Is.False);Assert.That(replica.State.publicationSequence,Is.EqualTo(1));state.publicationSequence=2;Assert.That(replica.Apply(state),Is.True);
    }
    [TestCase(1,7)][TestCase(3,3)]
    public void CappedCompletedAllowanceIsRetained(int half,int total)
    {var state=Valid();state.half=half;state.halfExpired=state.stoppageDetermined=true;state.stoppageTotal=total;state.stoppageRemaining=0;var replica=new OnlineGroundPassReplica(state.matchId,state.tokens.Select(t=>t.id),state.catalogId);Assert.That(replica.Apply(state),Is.True);Assert.That(replica.State.stoppageTotal,Is.EqualTo(total));}
    [Test] public void RUsesPublishedStoppageCommand()=>Assert.That(OnlineBoardActionPicker.Key(new OnlineGroundPassState{legalActions=new[]{"stoppage.roll"}},KeyCode.R),Is.EqualTo("stoppage.roll"));
}
