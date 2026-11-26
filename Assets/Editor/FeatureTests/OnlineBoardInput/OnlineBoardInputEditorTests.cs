using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
[Category("OnlineBoardInput")]
public sealed class OnlineBoardInputEditorTests
{
    [TestCase("kickoff.player")][TestCase("kickoff.taker")][TestCase("movement.player")]
    [TestCase("high.player")][TestCase("ftp.player")][TestCase("header.nominate")][TestCase("header.remove")]
    [TestCase("penalty.player")][TestCase("set-piece.player")][TestCase("throw.player")]
    public void ActorUsesOnlyPublishedChoice(string prefix)
    {
        var state=new OnlineGroundPassState{legalActions=new[]{prefix+":away:11",prefix+":home:2"}};
        Assert.That(OnlineBoardActionPicker.Actor(state,"away:11"),Is.EqualTo(prefix+":away:11"));
        Assert.That(OnlineBoardActionPicker.Actor(state,"home:11"),Is.Null);
    }
    [TestCase("ground.target")][TestCase("long.target")][TestCase("high.target")]
    [TestCase("ftp.target")][TestCase("movement.target")][TestCase("kickoff.move")]
    [TestCase("header.target")][TestCase("header.goal")][TestCase("shot.target")][TestCase("penalty.target")]
    [TestCase("set-piece.target")][TestCase("throw.target")][TestCase("final-third.target")]
    public void HexUsesExactSignedCoordinates(string prefix)
    {
        var state=new OnlineGroundPassState{legalActions=new[]{prefix+":-12:3",prefix+":12:3","movement.player:home:3"}};
        Assert.That(OnlineBoardActionPicker.Hex(state,-12,3),Is.EqualTo(prefix+":-12:3"));
        Assert.That(OnlineBoardActionPicker.Hex(state,1,3),Is.Null);
    }
    [TestCase(null)][TestCase("")][TestCase("home:0")][TestCase("away:19")][TestCase("home:01")][TestCase("other:2")][TestCase("home:2:3")]
    public void RejectsMalformedActor(string id)=>Assert.That(OnlineBoardActionPicker.Actor(new OnlineGroundPassState{legalActions=new[]{"movement.player:"+id}},id),Is.Null);
    [Test] public void AmbiguousChoicesAreNotGuessed()
    {
        var state=new OnlineGroundPassState{legalActions=new[]{"movement.player:home:2","other.player:home:2","ground.target:1:0","other.target:1:0","high.roll","coin.roll"}};
        Assert.That(OnlineBoardActionPicker.Actor(state,"home:2"),Is.Null);Assert.That(OnlineBoardActionPicker.Hex(state,1,0),Is.Null);Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.R),Is.Null);
    }
    [TestCase(KeyCode.P,"ground.start")][TestCase(KeyCode.L,"long.start")][TestCase(KeyCode.C,"high.start")]
    [TestCase(KeyCode.F,"ftp.start")][TestCase(KeyCode.M,"movement.start")][TestCase(KeyCode.S,"shot.start")]
    [TestCase(KeyCode.R,"shot.save")][TestCase(KeyCode.R,"coin.roll")][TestCase(KeyCode.R,"header.interception")]
    [TestCase(KeyCode.Return,"kickoff.confirm")][TestCase(KeyCode.KeypadEnter,"penalty.confirm")]
    public void KeysRequirePublishedAction(KeyCode key,string action)
    {
        Assert.That(OnlineBoardActionPicker.Key(new OnlineGroundPassState{legalActions=new[]{action}},key),Is.EqualTo(action));
        Assert.That(OnlineBoardActionPicker.Key(new OnlineGroundPassState{legalActions=new string[0]},key),Is.Null);
    }
    [Test] public void ConfirmUsesPublishedPreviewAndShotCommit()
    {
        var state=new OnlineGroundPassState{hasSelectedTarget=true,selectedX=-1,selectedZ=2,legalActions=new[]{"long.target:-1:2","shot.confirm","shot.start"}};
        Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.S),Is.EqualTo("shot.confirm"));state.legalActions=new[]{"long.target:-1:2"};
        Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.Return),Is.EqualTo("long.target:-1:2"));state.hasSelectedTarget=false;
        Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.Return),Is.Null);Assert.That(OnlineBoardActionPicker.Key(null,KeyCode.P),Is.Null);
    }
    [Test] public void AmbiguousConfirmCannotFallBackToTarget()
    {
        var state=new OnlineGroundPassState{hasSelectedTarget=true,selectedX=1,selectedZ=0,legalActions=new[]{"shot.confirm","penalty.confirm","ground.target:1:0"}};
        Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.Return),Is.Null);
    }
}
