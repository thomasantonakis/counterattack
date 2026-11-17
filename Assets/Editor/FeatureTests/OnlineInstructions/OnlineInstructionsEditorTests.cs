using System;
using CounterAttack.Online;
using NUnit.Framework;
[Category("OnlineInstructions")]
public sealed class OnlineInstructionsEditorTests
{
    private static OnlineGroundPassState State()=>new(){matchId="instructions",publicationSequence=1,revision=1,decisionId="choice",owner="Home",
        tokens=Array.Empty<OnlineGroundPassTokenState>(),legalActions=new[]{"ground.start"}};
    [TestCase("missing",true)][TestCase("home",true)][TestCase("away",true)][TestCase("neutral",true)]
    [TestCase("blank",false)][TestCase("null-text",false)][TestCase("huge",false)][TestCase("owner",false)][TestCase("missing-owner",false)]
    [TestCase("boundary",true)][TestCase("unicode",true)]
    public void BoundedInstructionContract(string kind,bool accepted)
    {
        var s=State();s.instruction=new OnlineInstructionObservation{text="Select a player",owner="Home"};
        switch(kind)
        {
            case "missing":s.instruction=null;break;case "away":s.instruction.owner="Away";break;case "neutral":s.instruction.owner="Neutral";break;
            case "blank":s.instruction.text=" ";break;case "null-text":s.instruction.text=null;break;case "huge":s.instruction.text=new string('a',OnlineInstructionObservation.TextLimit+1);break;
            case "owner":s.instruction.owner="foreign";break;case "missing-owner":s.instruction.owner=null;break;
            case "boundary":s.instruction.text=new string('a',OnlineInstructionObservation.TextLimit);break;case "unicode":s.instruction.text="\u039a\u03b5\u03c6\u03b1\u03bb\u03b9\u03ac ? [H] ? Inverness";break;
        }
        Assert.That(new OnlineGroundPassReplica(s.matchId).Apply(s),Is.EqualTo(accepted));
    }
    [Test] public void InstructionRefreshCannotMutateOrRevokeAnUnchangedDecision()
    {
        var s=State();s.instruction=new OnlineInstructionObservation{text="Choose a player",owner="Home"};var replica=new OnlineGroundPassReplica(s.matchId);
        Assert.That(replica.Apply(s),Is.True);s.instruction.text="Choose a destination";Assert.That(replica.State.instruction.text,Is.EqualTo("Choose a player"));
        s.publicationSequence++;Assert.That(replica.Apply(s),Is.True);Assert.That(replica.State.decisionId,Is.EqualTo("choice"));Assert.That(replica.State.revision,Is.EqualTo(1));Assert.That(replica.State.instruction.text,Is.EqualTo("Choose a destination"));
    }
    [Test] public void InvalidInstructionRefreshKeepsTheLastAcceptedState()
    {
        var s=State();s.instruction=new OnlineInstructionObservation{text="Choose a player",owner="Home"};var replica=new OnlineGroundPassReplica(s.matchId);
        Assert.That(replica.Apply(s),Is.True);s.publicationSequence++;s.instruction.owner="foreign";Assert.That(replica.Apply(s),Is.False);Assert.That(replica.State.publicationSequence,Is.EqualTo(1));Assert.That(replica.State.instruction.owner,Is.EqualTo("Home"));
    }
}
