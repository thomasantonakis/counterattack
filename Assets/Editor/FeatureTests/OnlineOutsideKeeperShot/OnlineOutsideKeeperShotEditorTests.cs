using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
[Category("OnlineOutsideKeeperShot")]
public sealed class OnlineOutsideKeeperShotEditorTests
{
    [TestCase(KeyCode.D,"shot.keeper.hands")][TestCase(KeyCode.B,"shot.keeper.block")]
    public void PhysicalChoiceUsesOnlyPublishedAction(KeyCode key,string action)
    {
        var state=new OnlineGroundPassState{legalActions=new[]{action}};
        Assert.That(OnlineBoardActionPicker.Key(state,key),Is.EqualTo(action));
        state.legalActions=new string[0];Assert.That(OnlineBoardActionPicker.Key(state,key),Is.Null);
    }
}
