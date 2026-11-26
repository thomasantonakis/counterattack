using CounterAttack.Online;
using NUnit.Framework;
using System.Linq;

[Category("OnlineRoomAction")]
public sealed class OnlineRoomPublicationEditorTests
{
    [Test] public void LateFragmentsCannotContaminateAReturnedConnection()
    {
        var old=OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="hello",matchId="room",peerNonce="old",digest=new string('x',2000)}).ToArray();
        var fresh=OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="hello",matchId="room",peerNonce="new",digest=new string('y',2000)}).ToArray();
        var wire=new OnlineGroundPassWire(); wire.Receive(old[0],old[0].Length,true); wire.Receive(fresh[0],fresh[0].Length,true);
        Assert.That(wire.Receive(old[1],old[1].Length,true),Is.Null);
        OnlineGroundPassMessage decoded=null;
        foreach(var fragment in fresh.Skip(1)) decoded=wire.Receive(fragment,fragment.Length,true);
        Assert.That(decoded.peerNonce,Is.EqualTo("new")); Assert.That(decoded.digest,Is.EqualTo(new string('y',2000)));
    }
    [Test] public void PreviousBinaryHeaderIsRejectedBeforePayloadDispatch()
    {
        var packet=OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="hello",matchId="room"}).Single();
        packet[0]=1; packet[1]=0xCA;
        Assert.Throws<System.IO.InvalidDataException>(()=>new OnlineGroundPassWire().Receive(packet,packet.Length));
    }
    [Test] public void RecoveryFramingCanRetireAnInterruptedPartialMessage()
    {
        var partial=OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="hello",matchId="room",digest=new string('x',2000)}).First();
        var fresh=OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="hello",matchId="room",peerNonce="fresh"}).Single();
        var wire=new OnlineGroundPassWire(); Assert.That(wire.Receive(partial,partial.Length),Is.Null);
        Assert.That(wire.Receive(fresh,fresh.Length,true).peerNonce,Is.EqualTo("fresh"));
    }
    [Test] public void AnimationCannotReplaceCommittedStateOrAcceptOldConflictingFrames()
    {
        var replica=new OnlineGroundPassReplica("room",new[]{"home:9"},"catalog");
        Assert.That(replica.Apply(State("home:9","catalog",1)),Is.True);
        var frame=new OnlineGroundPassPresentation{matchId="room",publicationSequence=1,frameSequence=1,ballX=2,ballY=1.2f,ballZ=3};
        Assert.That(replica.ApplyPresentation(frame),Is.True);
        Assert.That(replica.State.tokens[0].x,Is.Zero);
        Assert.That(replica.State.rollSequence,Is.Zero);
        frame.ballX=4; Assert.That(replica.ApplyPresentation(frame),Is.False);
        frame.frameSequence=2; frame.publicationSequence=0; Assert.That(replica.ApplyPresentation(frame),Is.False);
        frame.publicationSequence=1; frame.ballX=float.NaN; Assert.That(replica.ApplyPresentation(frame),Is.False);
    }
    private static OnlineGroundPassState State(string id,string catalog,long sequence) => new()
    { matchId="room",catalogId=catalog,publicationSequence=sequence,revision=1,legalActions=new[]{"ground.start"},
        tokens=new[]{new OnlineGroundPassTokenState{id=id,onBoard=true,x=0,z=0}} };
    [TestCase("away:9","catalog")] [TestCase("home:9","wrong")]
    public void IncompatibleRosterOrCatalogCannotPartiallyReplaceReplica(string id,string catalog)
    {
        var replica=new OnlineGroundPassReplica("room",new[]{"home:9"},"catalog");
        Assert.That(replica.Apply(State("home:9","catalog",1)),Is.True);
        Assert.That(replica.Apply(State(id,catalog,2)),Is.False);
        Assert.That(replica.State.publicationSequence,Is.EqualTo(1));
        Assert.That(replica.State.tokens[0].id,Is.EqualTo("home:9"));
    }
}
