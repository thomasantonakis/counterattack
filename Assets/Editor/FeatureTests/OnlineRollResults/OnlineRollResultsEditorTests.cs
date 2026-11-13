using System;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

[Category("OnlineRollResults")]
public sealed class OnlineRollResultsEditorTests
{
    private GameObject root;
    private MatchManager match;
    private PlayerToken actor;
    private Random.State random;
    [SetUp] public void Setup()
    {
        Assert.That(OnlineRoomSession.Current, Is.Null);
        random=Random.state;
        root=new GameObject("Owned online roll publications"); root.SetActive(false);
        match=root.AddComponent<MatchManager>(); match.gameData=new MatchManager.GameData();
        actor=root.AddComponent<PlayerToken>(); actor.playerName="Owned carrier"; actor.isHomeTeam=true; actor.jerseyNumber=9;
    }
    [TearDown] public void Cleanup()
    { if(root!=null) Object.DestroyImmediate(root); Random.state=random; }
    private GameplayDiceRollResult Roll(int value) => match.ResolveGameplayDiceRoll("online_result_test",
        new RollInputOverride{hasOverride=true,roll=value,isJackpot=false},actor:actor);

    [Test] public void AuthoritativeResultIsDetachedAndReadingItCannotDrawAgain()
    {
        var result=Roll(4); var after=Random.state;
        var publication=match.CaptureOnlineRolls()[0];
        Assert.That(publication.roll,Is.EqualTo(result.roll)); Assert.That(publication.sequence,Is.EqualTo(1));
        Assert.That(publication.actorId,Is.EqualTo("home:9")); Assert.That(publication.team,Is.EqualTo("Home"));
        publication.roll=1; publication.actorId="away:1";
        Assert.That(match.CaptureOnlineRolls()[0].roll,Is.EqualTo(4));
        Assert.That(match.CaptureOnlineRolls()[0].actorId,Is.EqualTo("home:9"));
        Assert.That(match.TimingRollSequence,Is.EqualTo(1)); Assert.That(Random.state,Is.EqualTo(after));
    }
    [Test] public void BoundedHistoryPreservesActualAuthorityDrawSequences()
    {
        for(var index=0;index<70;index++) Roll(index%6+1);
        var history=match.CaptureOnlineRolls();
        Assert.That(history.Length,Is.EqualTo(64)); Assert.That(history[0].sequence,Is.EqualTo(7));
        Assert.That(history[63].sequence,Is.EqualTo(70)); Assert.That(match.TimingRollSequence,Is.EqualTo(70));
    }
    [Test] public void GuestCannotConsumeRandomnessOrProduceAnAuthoritativeRoll()
    {
        using(new OnlineRoomSession("guest-roll-test",true))
        {
            var before=Random.state;
            Assert.Throws<InvalidOperationException>(()=>Roll(6));
            Assert.That(match.TimingRollSequence,Is.Zero); Assert.That(match.CaptureOnlineRolls(),Is.Empty);
            Assert.That(Random.state,Is.EqualTo(before));
        }
    }
    [Test] public void ReplacingMatchDataCannotPublishAnotherRunsResults()
    {
        Roll(3); match.gameData=new MatchManager.GameData();
        Assert.That(match.CaptureOnlineRolls(),Is.Empty);
        Roll(5); Assert.That(match.CaptureOnlineRolls().Length,Is.EqualTo(1));
        Assert.That(match.CaptureOnlineRolls()[0].roll,Is.EqualTo(5));
    }

    [TestCase("foreign")]
    [TestCase("reordered")]
    [TestCase("future")]
    [TestCase("jackpot")]
    [TestCase("face")]
    [TestCase("rewrite")]
    public void ReplicaRejectsInvalidRollHistoryAtomically(string corruption)
    {
        Roll(3); Roll(5);
        var state = new OnlineGroundPassState { matchId="roll-test", revision=1, publicationSequence=1,
            rollSequence=2, legalActions=Array.Empty<string>(),
            tokens=new[]{new OnlineGroundPassTokenState{id="home:9"}}, rolls=match.CaptureOnlineRolls() };
        var replica=new OnlineGroundPassReplica("roll-test"); Assert.That(replica.Apply(state),Is.True);
        state.publicationSequence=2;
        switch(corruption)
        {
            case "foreign":state.rolls[0].actorId="away:99";break;
            case "reordered":Array.Reverse(state.rolls);break;
            case "future":state.rolls[1].sequence=3;break;
            case "jackpot":state.rolls[0].isJackpot=true;break;
            case "face":state.rolls[0].roll=7;break;
            case "rewrite":state.rolls[0].roll=4;break;
        }
        Assert.That(replica.Apply(state),Is.False); Assert.That(replica.State.publicationSequence,Is.EqualTo(1));
        Assert.That(replica.State.rolls[0].roll,Is.EqualTo(3)); Assert.That(match.TimingRollSequence,Is.EqualTo(2));
    }

    [Test] public void ReplicaCannotRewindAuthorityDrawSequenceWithANewerPublication()
    {
        var state=new OnlineGroundPassState{matchId="roll-test",revision=1,publicationSequence=1,rollSequence=2,
            tokens=Array.Empty<OnlineGroundPassTokenState>(),legalActions=Array.Empty<string>()};
        var replica=new OnlineGroundPassReplica("roll-test");Assert.That(replica.Apply(state),Is.True);
        state.publicationSequence=2;state.rollSequence=1;
        Assert.That(replica.Apply(state),Is.False);Assert.That(replica.State.rollSequence,Is.EqualTo(2));
    }
}
