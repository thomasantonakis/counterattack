using System;
using System.IO;
using System.Linq;
using CounterAttack.Online;
using Epic.OnlineServices;
using NUnit.Framework;
using UnityEngine;
[Category("OnlineCompactPresentation")]
public sealed class OnlineCompactPresentationEditorTests
{
    private static OnlineGroundPassMessage Message(int count=2)=>new(){kind="presentation",matchId="compact-room",peerNonce="abcdef1234567890abcdef1234567890",connectionGeneration=2,
        presentation=new OnlineGroundPassPresentation{matchId="compact-room",publicationSequence=3,frameSequence=10,ballX=.1234567f,ballY=.2f,ballZ=-4.56789f,
            players=Enumerable.Range(0,count).Select(i=>new OnlinePlayerPresentation{id=(i<18?"home:":"away:")+(i%18+1),x=i+.1234567f,y=.2f,z=-i/3f}).ToArray()}};
    [TestCase(0,false)][TestCase(32,false)][TestCase(36,false)][TestCase(36,true)]
    public void LosslessRoundTripFitsOneEosPacket(int count,bool upper)
    {
        var source=Message(count);if(upper)source.peerNonce=source.peerNonce.ToUpperInvariant();
        var packets=OnlineGroundPassWire.Encode(source,true).ToArray();Assert.That(packets.Length,Is.EqualTo(1));Assert.That(packets[0].Length,Is.LessThanOrEqualTo(1170));
        var decoded=new OnlineGroundPassWire().Receive(packets[0],packets[0].Length);
        Assert.That(JsonUtility.ToJson(decoded.presentation),Is.EqualTo(JsonUtility.ToJson(source.presentation)));
        Assert.That(decoded.matchId,Is.EqualTo(source.matchId));Assert.That(decoded.peerNonce,Is.EqualTo(source.peerNonce));Assert.That(decoded.connectionGeneration,Is.EqualTo(2));
    }
    [TestCase("presentation",true)][TestCase("state",false)][TestCase("command",false)][TestCase("reply",false)][TestCase("control",false)][TestCase("hello",false)]
    public void OnlyFullRoomPresentationUsesDroppableDelivery(string kind,bool compact)
    {Assert.That(OnlineCompactPresentation.Use(true,kind),Is.EqualTo(compact));Assert.That(OnlineCompactPresentation.Use(false,kind),Is.False);}
    [TestCase(-1000f)][TestCase(1000f)]
    public void FullRoomPoseBoundaryRoundTripsExactly(float coordinate)
    {
        var source=Message();source.presentation.players[0].x=coordinate;var packet=OnlineCompactPresentation.Encode(source);
        Assert.That(OnlineCompactPresentation.Decode(packet,packet.Length).presentation.players[0].x,Is.EqualTo(coordinate));
    }
    [TestCase(Result.NoConnection)][TestCase(Result.LimitExceeded)][TestCase(Result.TimedOut)]
    public void CongestedPoseDropsWithoutTriggeringControlRecovery(Result result)
    {Assert.That(OnlineCompactPresentation.DropTransient(true,result),Is.True);Assert.That(OnlineCompactPresentation.DropTransient(false,result),Is.False);}
    [TestCase("kind")][TestCase("nonce")][TestCase("generation")][TestCase("match")][TestCase("frame")][TestCase("publication")]
    [TestCase("actor")][TestCase("duplicate")][TestCase("too-many")][TestCase("nan")][TestCase("range")]
    public void InvalidPresentationCannotEnterCompactLane(string branch)
    {
        var source=Message();switch(branch)
        {
            case "kind":source.kind="command";break;case "nonce":source.peerNonce="wrong";break;case "generation":source.connectionGeneration=0;break;
            case "match":source.presentation.matchId="wrong";break;case "frame":source.presentation.frameSequence=0;break;case "publication":source.presentation.publicationSequence=0;break;
            case "actor":source.presentation.players[0].id="home:19";break;case "duplicate":source.presentation.players[1].id=source.presentation.players[0].id;break;
            case "too-many":source.presentation.players=Message(37).presentation.players;break;case "nan":source.presentation.ballX=float.NaN;break;case "range":source.presentation.players[0].x=1001;break;
        }
        Assert.Throws<InvalidDataException>(()=>OnlineCompactPresentation.Encode(source));
    }
    [TestCase("truncated")][TestCase("trailing")][TestCase("version")][TestCase("magic")]
    public void MalformedPacketIsRejected(string branch)
    {
        var packet=OnlineCompactPresentation.Encode(Message());if(branch=="truncated")Array.Resize(ref packet,packet.Length-1);
        if(branch=="trailing")Array.Resize(ref packet,packet.Length+1);if(branch=="version")packet[2]=9;if(branch=="magic")packet[0]=0;
        Assert.That(OnlineCompactPresentation.Decode(packet,packet.Length),Is.Null);
    }
    [Test] public void ReorderedAndConflictingFramesCannotOverwriteReplica()
    {
        var message=Message();var replica=new OnlineGroundPassReplica(message.matchId,new[]{"home:1","home:2"});
        Assert.That(replica.Apply(new OnlineGroundPassState{matchId=message.matchId,publicationSequence=3,legalActions=Array.Empty<string>(),tokens=new[]{new OnlineGroundPassTokenState{id="home:1"},new OnlineGroundPassTokenState{id="home:2"}}}),Is.True);
        OnlineGroundPassPresentation Decode()=>OnlineCompactPresentation.Decode(OnlineCompactPresentation.Encode(message),OnlineCompactPresentation.Encode(message).Length).presentation;
        Assert.That(replica.ApplyPresentation(Decode()),Is.True);message.presentation.frameSequence=9;Assert.That(replica.ApplyPresentation(Decode()),Is.False);
        message.presentation.frameSequence=10;message.presentation.ballX=3;Assert.That(replica.ApplyPresentation(Decode()),Is.False);
        message.presentation.frameSequence=11;message.presentation.players[1].id="away:18";Assert.That(replica.ApplyPresentation(Decode()),Is.False);
    }
    [Test] public void CompactFrameDoesNotDisturbReliableFragmentAssembly()
    {
        var frame=Message();var state=new OnlineGroundPassMessage{kind="state",matchId=frame.matchId,state=new OnlineGroundPassState{stage=new string('x',6000)}};
        var fragments=OnlineGroundPassWire.Encode(state).ToArray();Assert.That(fragments.Length,Is.GreaterThan(1));var reader=new OnlineGroundPassWire();
        Assert.That(reader.Receive(fragments[0],fragments[0].Length),Is.Null);var compact=OnlineCompactPresentation.Encode(frame);Assert.That(reader.Receive(compact,compact.Length).kind,Is.EqualTo("presentation"));
        OnlineGroundPassMessage decoded=null;foreach(var fragment in fragments.Skip(1))decoded=reader.Receive(fragment,fragment.Length);
        Assert.That(decoded.kind,Is.EqualTo("state"));Assert.That(decoded.state.stage,Is.EqualTo(state.state.stage));
    }
}
