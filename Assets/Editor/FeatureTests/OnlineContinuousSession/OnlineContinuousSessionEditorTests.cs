using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineContinuousSession")]
public sealed class OnlineContinuousSessionEditorTests
{
    private static OnlineGroundPassState FinishedPass()=>new(){stage="action",gameState="EndofFirstTimePass",
        tokens=new[]{new OnlineGroundPassTokenState{id="home:9",passesAttempted=1,passesCompleted=1}}};
    [Test] public void CompletedPassEndsProofButKeepsContinuousSessionOpen()
    {var state=FinishedPass();Assert.That(OnlineManualSessionPolicy.CanFinish(state,false),Is.True);Assert.That(OnlineManualSessionPolicy.CanFinish(state,true),Is.False);}
    [Test] public void MatchEndCanFinishContinuousWithoutAPass()
    {var state=FinishedPass();state.gameState="MatchEnded";state.tokens=System.Array.Empty<OnlineGroundPassTokenState>();Assert.That(OnlineManualSessionPolicy.CanFinish(state,true),Is.True);}
    [TestCase("pass")][TestCase("ball")][TestCase("player")][TestCase("pause")][TestCase("connection")]
    public void MatchEndWaitsForMotionAndHolds(string pending)
    {
        var state=FinishedPass();state.gameState="MatchEnded";
        state.passActive=pending=="pass";state.ballMoving=pending=="ball";state.playerMoving=pending=="player";
        state.isSharedPaused=pending=="pause";state.connectionHeld=pending=="connection";
        Assert.That(OnlineManualSessionPolicy.CanFinish(state,true),Is.False);
    }
    [Test] public void ProfilesCannotAcceptEachOthersSnapshotCatalog()
    {
        var state=FinishedPass();state.matchId="session";state.catalogId=OnlineManualSessionPolicy.Catalog(false);
        state.legalActions=System.Array.Empty<string>();state.rolls=System.Array.Empty<OnlineRollPublication>();state.publicationSequence=1;
        var replica=new OnlineGroundPassReplica("session",new[]{"home:9"},OnlineManualSessionPolicy.Catalog(true));
        Assert.That(replica.Apply(state),Is.False);
        state.catalogId=OnlineManualSessionPolicy.Catalog(true);Assert.That(replica.Apply(state),Is.True);
    }
}
