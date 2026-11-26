using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlinePenalty(int side,string branch)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out var receiver);
        if(branch=="goal")yield return AttachLegacyGoalFlow();
        Invoke(grid,"LoadPrecomputedShootingPaths");AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        attacker.shooting=branch=="goal"?6:3;awayGK.saving=branch=="goal"?1:8;awayGK.handling=4;
        if(branch=="defence-clearance")Place(defender,Hex(14*side,2));
        if(branch=="attack-clearance")Place(receiver,Hex(14*side,2));
        var penalty=Add<PenaltyKickManager>();match.penaltyKickManager=penalty;penalty.Configure(match,grid,ball,movement,match.shotManager);
        penalty.StartPenaltyPreparation();
        using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        yield return Until(()=>awayGK.currentHex==Hex(18*side),"required keeper placement before taker decision");
        yield return null;Assert.That(authority.Capture().stage,Is.EqualTo("penalty.kicker"));
        Assert.That(authority.Capture().owner,Is.EqualTo("Home"));
        using(var guest=new OnlineRoomSession("penalty-guest",true))Assert.That(penalty.TryExecuteOnlinePenalty(OnlinePenaltyIntent.Player,attacker),Is.False);
        Assert.That(penalty.TryExecuteOnlinePenalty(OnlinePenaltyIntent.Player,awayGK),Is.False);
        var old=OnlineGroundCommand(authority.Capture(),1,"penalty.player:home:9");
        Assert.That(authority.Submit("away",old).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Submit("home","penalty.player:home:9");
        yield return Until(()=>authority.Capture().stage=="penalty.select","penalty taker and ball reach required spot");
        Assert.That(authority.Submit("home",old).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(ball.GetCurrentHex(),Is.SameAs(Hex(14*side)));Assert.That(awayGK.currentHex,Is.SameAs(Hex(18*side)));
        if(branch=="hold")yield return HoldOnlineHeaderContinuation(pause,protocol,()=>penalty.isWaitingForSetupPhase && match.TimingRollSequence==0);
        for(int i=0;i<3;i++)
        {
            var state=authority.Capture();string connection=state.owner=="Home"?"home":"away";
            bool clear=(branch=="defence-clearance" && i==0) || (branch=="attack-clearance" && i==1);
            if(clear)
            {
                Assert.That(state.legalActions,Does.Not.Contain("penalty.confirm"));
                Submit(connection,connection=="home"?"penalty.player:home:10":"penalty.player:away:8");
                state=authority.Capture();Assert.That(state.stage,Is.EqualTo("penalty.target"));
                Assert.That(state.legalActions,Does.Not.Contain("penalty.target:"+(14*side)+":0"));
                Assert.That(state.legalActions,Does.Not.Contain("penalty.target:"+(11*side)+":0"));
                Submit(connection,state.legalActions.First(a=>a.StartsWith("penalty.target:")));
                yield return Until(()=>authority.Capture().stage=="penalty.select","penalty setup movement ends");
            }
            Submit(connection,"penalty.confirm");
            if(i<2)yield return Until(()=>authority.Capture().stage=="penalty.select","next penalty setup team");
        }
        Assert.That(penalty.isActivated,Is.False);
        Assert.That(match.shotManager.shotType,Is.EqualTo("penalty"));
        Submit("home","shot.target:"+(19*side)+":0");
        yield return Until(()=>authority.Capture().stage=="shot.roll","penalty shooter roll");
        void Roll(string connection,string action,int face)
        {
            RigOnlineLongRoll(face);
            if(branch=="automatic")match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
            else Submit(connection,action);
        }
        Roll("home","shot.roll",branch=="goal"?5:2);
        yield return Until(()=>authority.Capture().stage=="shot.save","penalty keeper roll");
        Assert.That(authority.Capture().owner,Is.EqualTo("Away"));Roll("away","shot.save",branch=="goal"?1:3);
        if(branch=="goal")
        {
            yield return Until(()=>match.goalFlowManager.isActivated,"penalty goal enters real celebration");
            Assert.That(authority.Capture().homeStats.totalGoals,Is.EqualTo(1));
            Assert.That(match.gameData.stats.GetPlayerStats(attacker).shotsAttempted,Is.EqualTo(1));yield break;
        }
        yield return Until(()=>authority.Capture().stage=="shot.handling","saved penalty handling");Roll("away","shot.handling",1);
        yield return Until(()=>authority.Capture().stage=="keeper.distribution","held penalty gives keeper distribution");
        Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(awayGK));
        Assert.That(match.gameData.stats.GetPlayerStats(awayGK).attemptsSaved,Is.EqualTo(1));
        Assert.That(match.TimingRollSequence,Is.EqualTo(3));Assert.That(authority.Capture().homeStats.totalGoals,Is.Zero);
    }
}
