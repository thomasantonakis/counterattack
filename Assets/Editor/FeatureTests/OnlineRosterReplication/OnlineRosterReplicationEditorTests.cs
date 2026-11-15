using System;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
[Category("OnlineRosterReplication")]
public sealed class OnlineRosterReplicationEditorTests
{
    [TestCase("missing-runtime")][TestCase("identity")][TestCase("duplicate-hex")][TestCase("inconsistent-hex")][TestCase("sent-off-visible")]
    [TestCase("direction")][TestCase("non-finite-world")][TestCase("non-finite-stat")][TestCase("attribute")][TestCase("roster")]
    [TestCase("team-stats")][TestCase("ball-bounds")][TestCase("clock")]
    public void RejectsMalformedRosterWithoutAdvancingReplica(string branch)
    {
        var valid=new OnlineGroundPassState{matchId="roster-validation",catalogId=OnlineFullRoomBootstrap.Catalog,publicationSequence=1,
            homeStats=new MatchManager.TeamStats(),awayStats=new MatchManager.TeamStats(),
            homeDirection="LeftToRight",awayDirection="RightToLeft",legalActions=Array.Empty<string>(),tokens=Enumerable.Range(0,32).Select(i=>
            {
                bool home=i<16;int number=i%16+1;string side=home?"Home":"Away";
                return new OnlineGroundPassTokenState{id=(home?"home:":"away:")+number,visible=true,
                    runtime=new RoomTokenSnapshot{tokenKey=side+":"+number,teamSide=side,jerseyNumber=number,playerName="Owned "+i},playerStats=new MatchManager.PlayerStats()};
            }).ToArray()};
        var replica=new OnlineGroundPassReplica(valid.matchId,valid.tokens.Select(t=>t.id),OnlineFullRoomBootstrap.Catalog);
        Assert.That(replica.Apply(valid),Is.True);var invalid=JsonUtility.FromJson<OnlineGroundPassState>(JsonUtility.ToJson(valid));invalid.publicationSequence=2;
        var token=invalid.tokens[0];
        switch(branch)
        {
            case "missing-runtime":token.runtime=null;break;
            case "identity":token.runtime.teamSide="Away";break;
            case "duplicate-hex":foreach(var item in invalid.tokens.Take(2)){item.onBoard=true;item.runtime.isPlaying=true;item.runtime.currentHex=new RoomHexCoordinates{x=0,z=0};}break;
            case "inconsistent-hex":token.runtime.currentHex=new RoomHexCoordinates{x=0,z=0};break;
            case "sent-off-visible":token.runtime.isSentOff=true;break;
            case "direction":invalid.awayDirection=invalid.homeDirection;break;
            case "non-finite-world":token.inactiveX=float.PositiveInfinity;break;
            case "non-finite-stat":token.playerStats.xGoals=float.NaN;break;
            case "attribute":token.runtime.pace=999;break;
            case "roster":token.runtime.jerseyNumber=17;token.runtime.tokenKey="Home:17";token.id="home:17";break;
            case "team-stats":invalid.homeStats=null;break;
            case "ball-bounds":invalid.ballOnBoard=true;invalid.ballX=100;break;
            case "clock":invalid.regulationSeconds=float.NaN;break;
        }
        Assert.That(replica.Apply(invalid),Is.False);Assert.That(replica.State.publicationSequence,Is.EqualTo(1));
        valid.publicationSequence=2;Assert.That(replica.Apply(valid),Is.True,"Rejected data must not consume a publication sequence");
    }
}
