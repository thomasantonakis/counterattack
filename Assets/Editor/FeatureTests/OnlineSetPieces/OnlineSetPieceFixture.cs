using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineSetPiece(int side,string branch)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        var fk=match.freeKickManager;Subscribe(fk);
        if(branch=="shot")
        {
            Invoke(grid,"LoadPrecomputedShootingPaths");Place(attacker,Hex(13*side));
            ball.PlaceAtCell(attacker.currentHex);match.SetLastToken(attacker);AttachAuthoredPanels();
        }
        bool corner=branch.StartsWith("corner");
        if(branch=="clearance")Place(defender,Hex(side));
        fk.StartFreeKickPreparation(corner?Hex(18*side,12):null,indirectFreeKick:branch=="indirect");
        using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string action)
        {
            var turn=authority.Capture();var connection=turn.owner=="Home"?"home":"away";
            OnlineAccepted(authority,connection,protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        }
        Assert.That(authority.Capture().stage,Is.EqualTo("set-piece.initial"));
        using(var guest=new OnlineRoomSession("set-piece-guest",true))
            Assert.That(fk.TryExecuteOnlineSetPiece(OnlineSetPieceIntent.Skip),Is.False);
        Assert.That(fk.TryExecuteOnlineSetPiece(OnlineSetPieceIntent.Player,defender),Is.False);
        var initial=authority.Capture();
        Assert.That(authority.Submit("away",OnlineGroundCommand(initial,1,"set-piece.player:home:9")).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        if(branch=="hold")
        {
            Assert.That(pause.Submit("home",new OnlineMatchCommand(protocol.MatchId,1,0,"shared-pause",OnlineSharedPause.RequestAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(fk.TryExecuteOnlineSetPiece(OnlineSetPieceIntent.Skip),Is.False);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(match.currentState,Is.EqualTo(MatchManager.GameState.FreeKickKickerSelect));
            Assert.That(pause.Submit("home",new OnlineMatchCommand(protocol.MatchId,2,1,"shared-pause",OnlineSharedPause.ResumeAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        if(corner)
        {
            Assert.That(authority.Capture().legalActions,Does.Not.Contain("set-piece.skip"));
            Submit("set-piece.player:home:9");
        }
        else Submit("set-piece.skip");
        yield return Until(()=>fk.isWaitingForSetupPhase && !fk.IsResolvingSetup,"initial taker selection completes");
        bool moved=false,cleared=false;
        for(int i=0;i<24 && !fk.isWaitingForExecution;i++)
        {
            var turn=authority.Capture();var phase=match.currentState;
            if(fk.isWaitingForFinalKickerSelection)
            {
                Submit(turn.legalActions.First(a=>a.StartsWith("set-piece.player:")));
                yield return null;continue;
            }
            if((branch=="move" && !moved && phase==MatchManager.GameState.FreeKickAtt1)
                || (branch=="clearance" && !cleared && phase==MatchManager.GameState.FreeKickDef1))
            {
                bool defending=branch=="clearance";
                Submit(defending?"set-piece.player:away:8":"set-piece.player:home:10");
                turn=authority.Capture();Assert.That(turn.stage,Is.EqualTo("set-piece.target"));
                Assert.That(turn.legalActions,Does.Not.Contain("set-piece.target:0:0"));
                if(defending)Assert.That(turn.legalActions,Does.Not.Contain("set-piece.target:"+(2*side)+":0"));
                var token=defending?defender:match.playerTokenManager.allTokens.Single(t=>t.isHomeTeam && t.jerseyNumber==10);
                var origin=token.currentHex;
                Submit(turn.legalActions.First(a=>a.StartsWith("set-piece.target:")));
                yield return Until(()=>!fk.IsResolvingSetup,"real setup movement finishes",120);
                Assert.That(token.currentHex,Is.Not.SameAs(origin));Assert.That(fk.SetupMovedTokens,Does.Contain(token));
                if(defending)cleared=true;else moved=true;
                if(match.currentState==phase)Submit("set-piece.skip");
            }
            else Submit("set-piece.skip");
            yield return Until(()=>match.currentState!=phase || fk.isWaitingForExecution,"next real setup phase",120);
        }
        yield return Until(()=>fk.isWaitingForExecution,"set-piece execution ready");
        Assert.That(authority.Capture().stage,Is.EqualTo("set-piece.execution"));
        Assert.That(fk.selectedKicker,Is.Not.Null);
        if(branch=="move")Assert.That(moved,Is.True);
        if(branch=="clearance")Assert.That(cleared,Is.True);
        if(branch=="indirect" || corner)Assert.That(authority.Capture().legalActions,Does.Not.Contain("set-piece.shot"));
        string action=branch=="shot"?"set-piece.shot":branch.Contains("high")?"set-piece.high":branch=="long"?"set-piece.long":"set-piece.ground";
        Submit(action);
        if(branch=="shot" && match.shotManager.isWaitingForShotCommitConfirmation)
        {
            Assert.That(authority.Capture().stage,Is.EqualTo("set-piece.execution"));Submit(action);
        }
        yield return Until(()=>match.shotManager.isActivated || ground.isActivated || high.isActivated || longBall.isActivated,"real execution child activated",120);
        Assert.That(authority.Capture().legalActions,Does.Not.Contain(action),"child owns the next decision");
        if(action=="set-piece.shot")Assert.That(match.shotManager.shotType,Is.EqualTo("freeKick"));
        else if(action=="set-piece.high")Assert.That(high.isActivated,Is.True);
        else if(action=="set-piece.long")Assert.That(longBall.isActivated,Is.True);
        else Assert.That(ground.isActivated,Is.True);
    }
}
