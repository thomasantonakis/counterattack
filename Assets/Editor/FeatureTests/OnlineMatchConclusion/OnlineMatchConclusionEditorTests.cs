using CounterAttack.Online;
using NUnit.Framework;
[Category("OnlineMatchConclusion")]
public sealed class OnlineMatchConclusionEditorTests
{
    [TestCase("complete",true)][TestCase("pending",false)][TestCase("live",false)][TestCase("coin",false)]
    [TestCase("shootout",false)][TestCase("catalog",false)][TestCase("null",false)][TestCase("held-complete",true)]
    public void OnlyConfirmedRegularResultIsPresented(string branch,bool expected)
    {
        var state=new OnlineGroundPassState{catalogId=OnlineFullRoomBootstrap.Catalog,matchComplete=true,gameState="MatchEnded"};
        switch(branch)
        {
            case "pending":state.matchComplete=false;break;case "live":state.gameState="KickOffSetup";break;
            case "coin":state.coin=new OnlineCoinTossSnapshot();break;case "shootout":state.shootout=new OnlineShootoutSnapshot();break;
            case "catalog":state.catalogId="prepared-board-v1";break;case "null":state=null;break;case "held-complete":state.connectionHeld=true;break;
        }
        Assert.That(OnlineMatchConclusionView.CanDisplay(state),Is.EqualTo(expected));
    }
}
