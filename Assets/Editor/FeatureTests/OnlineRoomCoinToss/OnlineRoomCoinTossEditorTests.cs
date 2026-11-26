using System;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
[Category("OnlineRoomCoinToss")]
public sealed class OnlineRoomCoinTossEditorTests
{
    private static OnlineGroundPassState State()=>new(){matchId="room-coin",catalogId=OnlineFullRoomBootstrap.Catalog,publicationSequence=1,
        homeDirection="LeftToRight",awayDirection="RightToLeft",homeStats=new MatchManager.TeamStats(),awayStats=new MatchManager.TeamStats(),legalActions=Array.Empty<string>(),
        coin=new OnlineCoinTossSnapshot{purpose="PenaltyShootout",stage="choice",homeRoll=6,awayRoll=1,hasWinner=true,winnerHome=true,choosingHome=true,canChooseKickoff=true},
        tokens=Enumerable.Range(0,32).Select(i=>{string side=i<16?"Home":"Away";int jersey=i%16+1;
            return new OnlineGroundPassTokenState{id=side.ToLowerInvariant()+":"+jersey,playerStats=new MatchManager.PlayerStats(),runtime=new RoomTokenSnapshot{tokenKey=side+":"+jersey,teamSide=side,jerseyNumber=jersey,playerName="Coin "+i}};}).ToArray()};
    [Test] public void ReplicaClearsOptionalCoinAndRetainsAbsentWorldPoses()
    {
        var state=State();var replica=new OnlineGroundPassReplica(state.matchId,state.tokens.Select(t=>t.id),OnlineFullRoomBootstrap.Catalog);
        Assert.That(replica.Apply(state),Is.True);Assert.That(replica.State.coin.homeRoll,Is.EqualTo(6));
        state.publicationSequence=2;state.coin=null;Assert.That(replica.Apply(state),Is.True);
        Assert.That(replica.State.coin,Is.Null,"An absent coin cannot become an empty live coin on the guest");Assert.That(replica.State.roomPose,Is.Null);Assert.That(replica.State.shootout,Is.Null);
        Assert.That(replica.State.tokens[0].runtime.currentHex,Is.Null);Assert.That(replica.Apply(replica.State),Is.True,"A projected snapshot must remain valid when read again");
        var copy=replica.State;copy.tokens[0].runtime.playerName="Local mutation";Assert.That(replica.State.tokens[0].runtime.playerName,Is.Not.EqualTo(copy.tokens[0].runtime.playerName));
    }
    [TestCase("purpose")][TestCase("stage")][TestCase("negative")][TestCase("oversize")][TestCase("winner")][TestCase("chooser")][TestCase("tied-winner")]
    [TestCase("missing-winner")][TestCase("home-stage")][TestCase("away-stage")][TestCase("result-stage")][TestCase("closed")]
    public void RejectsMalformedCoinWithoutAdvancingGuest(string branch)
    {
        var state=State();var replica=new OnlineGroundPassReplica(state.matchId,state.tokens.Select(t=>t.id),OnlineFullRoomBootstrap.Catalog);Assert.That(replica.Apply(state),Is.True);
        var invalid=Newtonsoft.Json.JsonConvert.DeserializeObject<OnlineGroundPassState>(Newtonsoft.Json.JsonConvert.SerializeObject(state));invalid.publicationSequence=2;var c=invalid.coin;
        switch(branch)
        {
            case "purpose":c.purpose="Other";break;case "stage":c.stage="unknown";break;case "negative":c.homeRoll=-1;break;case "oversize":c.awayRoll=7;break;
            case "winner":c.winnerHome=false;break;case "chooser":c.choosingHome=false;break;case "tied-winner":c.awayRoll=6;break;case "missing-winner":c.hasWinner=false;break;
            case "home-stage":c.stage="home";break;case "away-stage":c.stage="away";break;case "result-stage":c.stage="result";break;case "closed":c.stage="closed";break;
        }
        Assert.That(replica.Apply(invalid),Is.False);Assert.That(replica.State.publicationSequence,Is.EqualTo(1));state.publicationSequence=2;Assert.That(replica.Apply(state),Is.True);
    }
}
