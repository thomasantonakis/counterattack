using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
[Category("OnlineContextKeys")]
public sealed class OnlineContextKeysEditorTests
{
    [TestCase(KeyCode.P,"set-piece.ground")][TestCase(KeyCode.P,"throw.feet")][TestCase(KeyCode.P,"final-third.pass")]
    [TestCase(KeyCode.L,"set-piece.long")][TestCase(KeyCode.C,"set-piece.high")][TestCase(KeyCode.C,"throw.head")]
    [TestCase(KeyCode.F,"header.backpass.feet")][TestCase(KeyCode.F,"keeper.pickup.feet")][TestCase(KeyCode.F,"foul.take")]
    [TestCase(KeyCode.M,"throw.movement")][TestCase(KeyCode.D,"final-third.drop")][TestCase(KeyCode.B,"header.control")]
    [TestCase(KeyCode.N,"duel.nutmeg")][TestCase(KeyCode.T,"throw.now")][TestCase(KeyCode.S,"set-piece.shot")]
    [TestCase(KeyCode.Q,"keeper.quick")][TestCase(KeyCode.Q,"keeper.quick.start")]
    [TestCase(KeyCode.K,"keeper.final-third")][TestCase(KeyCode.K,"final-third.kick")]
    [TestCase(KeyCode.H,"header.free")][TestCase(KeyCode.H,"header.pass")][TestCase(KeyCode.H,"header.backpass.hands")][TestCase(KeyCode.H,"keeper.pickup.hands")]
    [TestCase(KeyCode.G,"high.keeper.stay")][TestCase(KeyCode.V,"long.keeper.claim")][TestCase(KeyCode.A,"foul.advantage")]
    [TestCase(KeyCode.X,"movement.forfeit")][TestCase(KeyCode.X,"high.skip")][TestCase(KeyCode.X,"high.keeper.decline")][TestCase(KeyCode.X,"ftp.skip")]
    [TestCase(KeyCode.X,"long.keeper.decline")][TestCase(KeyCode.X,"keeper.stay")][TestCase(KeyCode.X,"set-piece.skip")][TestCase(KeyCode.X,"duel.stay")][TestCase(KeyCode.X,"dribble.decline-nutmeg")]
    [TestCase(KeyCode.Return,"final-third.forfeit")][TestCase(KeyCode.KeypadEnter,"final-third.forfeit")]
    public void ExistingPhysicalKeyUsesItsPublishedContext(KeyCode key,string action)
    {
        var state=new OnlineGroundPassState{legalActions=new[]{action}};
        Assert.That(OnlineBoardActionPicker.Key(state,key),Is.EqualTo(action));state.legalActions=new string[0];Assert.That(OnlineBoardActionPicker.Key(state,key),Is.Null);
    }
    [TestCase(KeyCode.P,"ground.start","set-piece.ground")][TestCase(KeyCode.F,"ftp.start","keeper.pickup.feet")]
    [TestCase(KeyCode.H,"header.free","header.backpass.hands")][TestCase(KeyCode.Q,"keeper.quick","keeper.quick.start")]
    [TestCase(KeyCode.K,"keeper.final-third","final-third.kick")][TestCase(KeyCode.X,"dribble.finish","high.skip")]
    [TestCase(KeyCode.Return,"kickoff.confirm","final-third.forfeit")]
    [TestCase(KeyCode.S,"set-piece.shot","shot.start")]
    public void AmbiguousFamiliesAreNotGuessed(KeyCode key,string first,string second)
        =>Assert.That(OnlineBoardActionPicker.Key(new OnlineGroundPassState{legalActions=new[]{first,second}},key),Is.Null);
}
