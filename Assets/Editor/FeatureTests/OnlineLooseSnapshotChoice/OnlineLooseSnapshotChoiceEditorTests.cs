using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
[Category("OnlineLooseSnapshotChoice")]
public sealed class OnlineLooseSnapshotChoiceEditorTests
{
    [TestCase(KeyCode.S,"shot.start")][TestCase(KeyCode.X,"shot.decline")]
    public void CollectedSnapshotKeysRequirePublishedChoice(KeyCode key,string action)
    {
        var state=new OnlineGroundPassState{legalActions=new[]{action}};
        Assert.That(OnlineBoardActionPicker.Key(state,key),Is.EqualTo(action));state.legalActions=new string[0];
        Assert.That(OnlineBoardActionPicker.Key(state,key),Is.Null);
    }
}
