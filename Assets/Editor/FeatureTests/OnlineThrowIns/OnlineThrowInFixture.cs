using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineThrowIn(int side,string branch)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out var receiver);
        var header=movement.headerManager;var finalThird=match.finalThirdManager;
        Place(attacker,Hex(0,11));Place(receiver,Hex(0,8));ball.PlaceAtCell(Hex(0,12));
        if(branch=="head-target")Place(Token("Second throw target",true,false,7),Hex(2*side,9));
        var throwIn=Add<ThrowInManager>();throwIn.ball=ball;throwIn.hexGrid=grid;
        throwIn.movementPhaseManager=movement;throwIn.groundBallManager=ground;throwIn.headerManager=header;throwIn.finalThirdManager=finalThird;
        Subscribe(movement);Subscribe(finalThird);AttachAuthoredPanels();
        throwIn.StartThrowInPreparation(Hex(0,12),MatchManager.TeamInAttack.Home,false);
        using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string action)
        {
            var turn=authority.Capture();string connection=turn.owner=="Home"?"home":"away";
            OnlineAccepted(authority,connection,protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        }
        Assert.That(authority.Capture().stage,Is.EqualTo("throw.taker"));
        using(var guest=new OnlineRoomSession("throw-guest",true))Assert.That(throwIn.TryExecuteOnlineThrowIn(OnlineThrowInIntent.Taker,attacker),Is.False);
        Assert.That(throwIn.TryExecuteOnlineThrowIn(OnlineThrowInIntent.Taker,defender),Is.False);
        var stale=OnlineGroundCommand(authority.Capture(),1,"throw.player:home:9");
        Assert.That(authority.Submit("away",stale).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Submit("throw.player:home:9");
        Assert.That(throwIn.TryExecuteOnlineThrowIn(OnlineThrowInIntent.Taker,attacker),Is.False,"selection in flight cannot be repeated");
        Assert.That(authority.Submit("home",stale).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        IEnumerator CompleteMoves(System.Func<bool> finished)
        {
            for(int i=0;i<16 && !finished();i++)
            {
                yield return Until(()=>finished() || authority.Capture().legalActions.Any(a=>a=="movement.forfeit" || a=="final-third.forfeit"),"throw-in owned movement phase");
                if(finished())break;
                var turn=authority.Capture();Assert.That(turn.legalActions,Does.Not.Contain("movement.player:home:9"),"thrower cannot move during protected movement");
                Submit(turn.legalActions.Contains("final-third.forfeit")?"final-third.forfeit":"movement.forfeit");yield return null;
            }
            Assert.That(finished(),Is.True);
        }
        yield return CompleteMoves(()=>throwIn.isWaitingForOptionalMovementDecision);
        Assert.That(authority.Capture().stage,Is.EqualTo("throw.optional"));
        if(branch=="hold")yield return HoldOnlineHeaderContinuation(pause,protocol,()=>throwIn.isWaitingForOptionalMovementDecision && match.TimingRollSequence==0);
        Submit(branch=="optional"?"throw.movement":"throw.now");
        if(branch=="optional")yield return CompleteMoves(()=>throwIn.isWaitingForThrowTypeSelection);
        yield return Until(()=>throwIn.isWaitingForThrowTypeSelection,"actual throw execution selection");
        bool head=branch.StartsWith("head");Submit(head?"throw.head":"throw.feet");
        if(head)
        {
            if(branch=="head-target")
            {
                Assert.That(authority.Capture().stage,Is.EqualTo("throw.target"));
                Submit("throw.target:0:8");Assert.That(throwIn.currentHeaderThrowTarget,Is.SameAs(receiver.currentHex));
                Assert.That(throwIn.isWaitingForHeaderTarget,Is.True);Submit("throw.target:0:8");
            }
            yield return Until(()=>header.isActivated,"throw to head enters real header manager",120);
            Assert.That(ball.GetCurrentHex(),Is.SameAs(receiver.currentHex));
            Assert.That(throwIn.isActivated,Is.False);Assert.That(authority.Capture().stage,Does.StartWith("header."));
        }
        else
        {
            yield return Until(()=>ground.isAwaitingTargetSelection,"throw to feet uses real six-hex ground targeting");
            Assert.That(ground.imposedDistance,Is.EqualTo(6));
            Submit("ground.target:0:8");Submit("ground.target:0:8");
            yield return Until(()=>!ground.isActivated && match.LastTokenToTouchTheBallOnPurpose==receiver,"throw to feet completes to receiver",120);
            yield return Until(()=>ground.imposedDistance==11,"throw distance restores after completion");
            Assert.That(match.gameData.stats.GetPlayerStats(attacker).passesCompleted,Is.EqualTo(1));
            Assert.That(throwIn.isActivated,Is.False);Assert.That(ball.GetCurrentHex(),Is.SameAs(receiver.currentHex));
        }
    }
}
