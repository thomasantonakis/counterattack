using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
[Category("OnlinePickupKey")]
public sealed class OnlinePickupKeyEditorTests
{
    [TestCase("attack",1)][TestCase("attack",-1)][TestCase("defence",1)][TestCase("defence",-1)][TestCase("two-for-two",1)][TestCase("two-for-two",-1)]
    public void VUsesPublishedBallDestination(string phase,int side)
    {string action="movement.target:"+(5*side)+":2";var state=new OnlineGroundPassState{stage="movement."+phase+".target",ballOnBoard=true,ballX=5*side,ballZ=2,legalActions=new[]{action}};Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.V),Is.EqualTo(action));}
    [TestCase("off-board")][TestCase("unpublished")][TestCase("pass")][TestCase("dribble")][TestCase("selection")][TestCase("ambiguous")]
    public void VCannotGuessAnUnpublishedPickup(string branch)
    {
        var state=new OnlineGroundPassState{stage="movement.attack.target",ballOnBoard=true,ballX=5,ballZ=2,legalActions=new[]{"movement.target:5:2"}};
        switch(branch){case "off-board":state.ballOnBoard=false;break;case "unpublished":state.legalActions=new[]{"movement.target:4:2"};break;case "pass":state.stage="target";break;case "dribble":state.stage="dribble.target";break;case "selection":state.stage="movement.attack.select";break;case "ambiguous":state.legalActions=new[]{"movement.target:5:2","long.keeper.claim"};break;}
        Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.V),Is.Null);
    }
    [Test] public void VRetainsPublishedKeeperClaim()=>Assert.That(OnlineBoardActionPicker.Key(new OnlineGroundPassState{stage="long.keeper",legalActions=new[]{"long.keeper.claim"}},KeyCode.V),Is.EqualTo("long.keeper.claim"));
}
