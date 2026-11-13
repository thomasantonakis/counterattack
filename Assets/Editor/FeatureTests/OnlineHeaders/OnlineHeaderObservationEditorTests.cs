using System;
using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineHeaders")]
public sealed class OnlineHeaderObservationEditorTests
{
    private static OnlineGroundPassState State()=>new()
    {
        matchId="header",publicationSequence=1,revision=1,legalActions=Array.Empty<string>(),
        tokens=new[]{new OnlineGroundPassTokenState{id="home:9"},new OnlineGroundPassTokenState{id="away:4"}},
        header=new OnlineHeaderObservation{attackers=new[]{"home:9"},defenders=new[]{"away:4"},roller="away:4",
            scores=new[]{new OnlineHeaderScore{actorId="home:9",roll=5,total=9}}}
    };

    [Test] public void HostObservationsAreDetachedAndSurviveVersionedWire()
    {
        var source=State();var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;
        foreach(var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="state",matchId="header",state=source}))
        {
            var message=wire.Receive(packet,packet.Length);if(message!=null)decoded=message;
        }
        Assert.That(decoded,Is.Not.Null);Assert.That(decoded.version,Is.EqualTo("ground-wire-v7"));
        var replica=new OnlineGroundPassReplica("header");Assert.That(replica.Apply(decoded.state),Is.True);
        source.header.scores[0].total=0;decoded.state.header.attackers[0]="away:4";
        var read=replica.State;Assert.That(read.header.scores[0].total,Is.EqualTo(9));
        Assert.That(read.header.attackers,Is.EqualTo(new[]{"home:9"}));
        read.header.scores[0].total=1;Assert.That(replica.State.header.scores[0].total,Is.EqualTo(9));
    }

    [TestCase("foreign")][TestCase("duplicate")][TestCase("cross-side")][TestCase("score-actor")]
    [TestCase("score-roll")][TestCase("score-total")][TestCase("duplicate-score")][TestCase("roller")][TestCase("winner")]
    public void InvalidObservationCannotReplaceAcceptedState(string corruption)
    {
        var replica=new OnlineGroundPassReplica("header");Assert.That(replica.Apply(State()),Is.True);
        var state=State();state.publicationSequence=2;
        switch(corruption)
        {
            case "foreign":state.header.attackers[0]="home:99";break;
            case "duplicate":state.header.attackers=new[]{"home:9","home:9"};break;
            case "cross-side":state.header.defenders[0]="home:9";break;
            case "score-actor":state.header.scores[0].actorId="away:99";break;
            case "score-roll":state.header.scores[0].roll=7;break;
            case "score-total":state.header.scores[0].total=int.MaxValue;break;
            case "duplicate-score":state.header.scores=new[]{state.header.scores[0],state.header.scores[0]};break;
            case "roller":state.header.roller="away:99";break;
            case "winner":state.header.winner="away:99";break;
        }
        Assert.That(replica.Apply(state),Is.False);Assert.That(replica.State.publicationSequence,Is.EqualTo(1));
    }
}
