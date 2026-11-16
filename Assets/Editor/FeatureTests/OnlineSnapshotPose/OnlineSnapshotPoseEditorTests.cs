using System;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
[Category("OnlineSnapshotPose")]
public sealed class OnlineSnapshotPoseEditorTests
{
    [TestCase("missing-pose")][TestCase("wrong-match")][TestCase("missing-player")][TestCase("duplicate-player")][TestCase("unknown-player")]
    [TestCase("nan-ball")][TestCase("infinite-player")][TestCase("world-bound")][TestCase("static-pose")]
    public void RejectsInvalidPoseWithoutConsumingPublication(string branch)
    {
        var state=new OnlineGroundPassState{matchId="snapshot-pose",catalogId=OnlineFullRoomBootstrap.Catalog,publicationSequence=1,ballMoving=true,
            homeDirection="LeftToRight",awayDirection="RightToLeft",homeStats=new MatchManager.TeamStats(),awayStats=new MatchManager.TeamStats(),legalActions=Array.Empty<string>(),
            tokens=Enumerable.Range(0,32).Select(i=>{string side=i<16?"Home":"Away";int jersey=i%16+1;
                return new OnlineGroundPassTokenState{id=side.ToLowerInvariant()+":"+jersey,playerStats=new MatchManager.PlayerStats(),runtime=new RoomTokenSnapshot{tokenKey=side+":"+jersey,teamSide=side,jerseyNumber=jersey,playerName="Pose "+i}};}).ToArray()};
        state.roomPose=new OnlineGroundPassPresentation{matchId=state.matchId,ballX=.35f,ballY=1.2f,players=state.tokens.Select((t,i)=>new OnlinePlayerPresentation{id=t.id,x=i,y=.2f,z=3}).ToArray()};
        var replica=new OnlineGroundPassReplica(state.matchId,state.tokens.Select(t=>t.id),OnlineFullRoomBootstrap.Catalog);Assert.That(replica.Apply(state),Is.True);
        var invalid=OnlineGroundPassState.Copy(state);invalid.publicationSequence=2;
        switch(branch)
        {
            case "missing-pose":invalid.roomPose=null;break;
            case "wrong-match":invalid.roomPose.matchId="other";break;
            case "missing-player":invalid.roomPose.players=invalid.roomPose.players.Skip(1).ToArray();break;
            case "duplicate-player":invalid.roomPose.players[1].id=invalid.roomPose.players[0].id;break;
            case "unknown-player":invalid.roomPose.players[0].id="other:1";break;
            case "nan-ball":invalid.roomPose.ballX=float.NaN;break;
            case "infinite-player":invalid.roomPose.players[0].z=float.PositiveInfinity;break;
            case "world-bound":invalid.roomPose.ballY=1001;break;
            case "static-pose":invalid.ballMoving=false;break;
        }
        Assert.That(replica.Apply(invalid),Is.False);Assert.That(replica.State.publicationSequence,Is.EqualTo(1));
        state.publicationSequence=2;Assert.That(replica.Apply(state),Is.True);Assert.That(replica.State.roomPose.ballX,Is.EqualTo(.35f));
    }
}
