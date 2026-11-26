using System;
using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineMovement")]
public sealed class OnlineMovementPresentationEditorTests
{
    private static OnlineGroundPassState State()=>new(){matchId="movement",publicationSequence=1,revision=1,
        tokens=new[]{new OnlineGroundPassTokenState{id="home:9",x=0,z=0}},legalActions=Array.Empty<string>()};
    [Test] public void MovingPlayerFramesAreIdempotentAndCannotChangeRulesOrCommittedPositions()
    {
        var replica=new OnlineGroundPassReplica("movement");Assert.That(replica.Apply(State()),Is.True);
        var frame=new OnlineGroundPassPresentation{matchId="movement",publicationSequence=1,frameSequence=1,
            players=new[]{new OnlinePlayerPresentation{id="home:9",x=2,y=.5f,z=3}}};
        Assert.That(replica.ApplyPresentation(frame),Is.True);Assert.That(replica.ApplyPresentation(frame),Is.True);
        Assert.That(replica.State.tokens[0].x,Is.Zero);Assert.That(replica.State.rollSequence,Is.Zero);
        frame.players[0].x=3;Assert.That(replica.ApplyPresentation(frame),Is.False);
    }
    [TestCase("foreign")][TestCase("duplicate")][TestCase("nonfinite")][TestCase("stale")]
    public void InvalidPlayerFrameCannotReplaceAcceptedPresentation(string corruption)
    {
        var replica=new OnlineGroundPassReplica("movement");Assert.That(replica.Apply(State()),Is.True);
        var player=new OnlinePlayerPresentation{id="home:9",x=2,y=.5f,z=3};
        var frame=new OnlineGroundPassPresentation{matchId="movement",publicationSequence=1,frameSequence=1,players=new[]{player}};
        Assert.That(replica.ApplyPresentation(frame),Is.True);
        frame.frameSequence=2;
        switch(corruption)
        {
            case "foreign":player.id="away:99";break;
            case "duplicate":frame.players=new[]{player,player};break;
            case "nonfinite":player.x=float.NaN;break;
            case "stale":frame.publicationSequence=0;break;
        }
        Assert.That(replica.ApplyPresentation(frame),Is.False);Assert.That(replica.State.publicationSequence,Is.EqualTo(1));
    }
    [Test] public void ForeignSelectedPlayerCannotReplaceCommittedDecision()
    {
        var replica=new OnlineGroundPassReplica("movement");var state=State();Assert.That(replica.Apply(state),Is.True);
        state.publicationSequence=2;state.selectedPlayer="away:99";
        Assert.That(replica.Apply(state),Is.False);Assert.That(replica.State.selectedPlayer,Is.Null.Or.Empty);
    }
}
