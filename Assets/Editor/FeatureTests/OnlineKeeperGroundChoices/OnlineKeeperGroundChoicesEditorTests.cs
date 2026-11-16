using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
[Category("OnlineKeeperGroundChoices")]
public sealed class OnlineKeeperGroundChoicesEditorTests
{
    [TestCase(KeyCode.D,"duel.dive")][TestCase(KeyCode.T,"duel.tackle")][TestCase(KeyCode.N,"duel.stand")]
    public void KeysUsePublishedKeeperChoices(KeyCode key,string action)
    {
        var state=new OnlineGroundPassState{legalActions=new[]{action}};
        Assert.That(OnlineBoardActionPicker.Key(state,key),Is.EqualTo(action));state.legalActions=new string[0];
        Assert.That(OnlineBoardActionPicker.Key(state,key),Is.Null);
    }
    [Test] public void AmbiguousDiveFamiliesAreNotGuessed()
    {
        var state=new OnlineGroundPassState{legalActions=new[]{"shot.keeper.hands","duel.dive"}};
        Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.D),Is.Null);
    }
}
