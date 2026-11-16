using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
[Category("OnlineMovementSnapshot")]
public sealed class OnlineMovementSnapshotEditorTests
{
    [TestCase(KeyCode.S,"movement.snapshot")][TestCase(KeyCode.X,"movement.snapshot.decline")]
    [TestCase(KeyCode.X,"dribble.finish")][TestCase(KeyCode.X,"shot.cancel")]
    public void KeysFollowPublishedContext(KeyCode key,string action)
    {
        var state=new OnlineGroundPassState{legalActions=new[]{action}};
        Assert.That(OnlineBoardActionPicker.Key(state,key),Is.EqualTo(action));state.legalActions=new string[0];
        Assert.That(OnlineBoardActionPicker.Key(state,key),Is.Null);
    }
    [Test] public void AmbiguousDeclinesAreNotGuessed()
    {
        var state=new OnlineGroundPassState{legalActions=new[]{"movement.snapshot.decline","shot.cancel"}};
        Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.X),Is.Null);
    }
}
