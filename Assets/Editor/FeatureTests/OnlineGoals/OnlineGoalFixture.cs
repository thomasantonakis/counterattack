using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineGoalReset(int side)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        yield return AttachLegacyGoalFlow();
        Invoke(grid,"LoadPrecomputedShootingPaths");
        Place(attacker,Hex(13*side));Place(awayGK,Hex(16*side,10));
        ball.PlaceAtCell(attacker.currentHex);match.SetLastToken(attacker);match.UpdatePossessionAfterPass(attacker.currentHex);
        attacker.shooting=3;match.RefreshAvailableActionsForCurrentState();AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string action)=>OnlineAccepted(authority,"home",protocol.LastAcceptedSequence(OnlineTeam.Home)+1,action);
        Submit("shot.start");Submit("shot.confirm");Submit("shot.target:"+(19*side)+":0");
        yield return Until(()=>authority.Capture().stage=="shot.roll","unopposed on-target shot reaches shooter roll");
        RigOnlineLongRoll(2);Submit("shot.roll");
        yield return Until(()=>match.goalFlowManager.isActivated,"real shot scores and starts celebration");
        var state=authority.Capture();
        Assert.That(state.stage,Is.EqualTo("goal.resolving"));
        Assert.That(state.owner,Is.EqualTo("None"));Assert.That(state.legalActions,Is.Empty);
        Assert.That(state.playerMoving,Is.True,"goal reset must stream player and ball presentation frames");
        Assert.That(state.homeStats.totalGoals,Is.EqualTo(1));Assert.That(state.awayStats.totalGoals,Is.Zero);
        Assert.That(pause.Submit("home",new OnlineMatchCommand(protocol.MatchId,1,0,"shared-pause",OnlineSharedPause.RequestAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        pause.SetConnectionHold(true);
        // Time.deltaTime already belongs to the current frame. Let the newly
        // started celebration coroutines reach the next frame before measuring.
        yield return null;
        var positions=match.playerTokenManager.allTokens.Select(t=>t.transform.position).ToArray();
        var ballPosition=ball.transform.position;
        yield return new WaitForSecondsRealtime(.25f);
        void AssertFrozen()
        {
            Assert.That(match.goalFlowManager.isActivated,Is.True);
            Assert.That(match.playerTokenManager.allTokens.Select((t,i)=>Vector3.Distance(t.transform.position,positions[i])<.001f).All(v=>v),Is.True);
            Assert.That(Vector3.Distance(ball.transform.position,ballPosition),Is.LessThan(.001f));
            Assert.That(authority.Capture().legalActions,Is.Empty);
        }
        AssertFrozen();pause.SetConnectionHold(false);yield return new WaitForSecondsRealtime(.15f);AssertFrozen();
        Assert.That(pause.Submit("home",new OnlineMatchCommand(protocol.MatchId,2,1,"shared-pause",OnlineSharedPause.ResumeAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        yield return WaitForLegacyGoalReset();
        state=authority.Capture();
        Assert.That(state.stage,Is.EqualTo("kickoff.setup"));
        Assert.That(state.legalActions,Does.Not.Contain("shot.start"));
        Assert.That(match.teamInAttack,Is.EqualTo(MatchManager.TeamInAttack.Away));
        Assert.That(ball.GetCurrentHex(),Is.SameAs(Hex(0)));
        Assert.That(state.homeStats.totalGoals,Is.EqualTo(1));Assert.That(match.TimingRollSequence,Is.EqualTo(1));
        Assert.That(state.owner,Is.EqualTo("Home"));
        var stale=OnlineGroundCommand(state,protocol.LastAcceptedSequence(OnlineTeam.Home)+1,"kickoff.confirm");
        Assert.That(authority.Submit("away",stale).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        Submit("kickoff.confirm");
        Assert.That(authority.Capture().owner,Is.EqualTo("Away"));
        Assert.That(authority.Submit("home",stale).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        void Away(string action)=>OnlineAccepted(authority,"away",protocol.LastAcceptedSequence(OnlineTeam.Away)+1,action);
        Away("kickoff.confirm");
        yield return Until(()=>authority.Capture().stage=="kickoff.taker","both setups confirmed before conceding taker selection");
        Assert.That(authority.Capture().owner,Is.EqualTo("Away"));
        Away(OnlineGroundPassAuthority.KickoffTakerAction(OnlineRollPublication.ActorId(defender)));
        yield return Until(()=>ground.isKickoffPass && authority.Capture().stage=="target","taker moves to center and activates real kickoff pass");
        var receiver=match.playerTokenManager.allTokens.First(t=>!t.isHomeTeam && t!=defender && !t.IsGoalKeeper);
        var receiverHex=receiver.currentHex;
        var targetAction=OnlineGroundPassAuthority.TargetAction(receiverHex.coordinates.x,receiverHex.coordinates.z);
        Assert.That(authority.Capture().legalActions,Does.Contain(targetAction));
        Assert.That(authority.Capture().legalActions,Does.Not.Contain("shot.start"));
        Away(targetAction);Away(targetAction);
        yield return Until(()=>!ground.isActivated && match.LastTokenToTouchTheBallOnPurpose==receiver,"actual conceding kickoff pass completes");
        Assert.That(ball.GetCurrentHex(),Is.SameAs(receiverHex));
        Assert.That(match.gameData.stats.homeTeamStats.totalGoals,Is.EqualTo(1));
        Assert.That(match.TimingRollSequence,Is.EqualTo(1),"safe kickoff requires no interception dice");
    }

    public IEnumerator RunOnlineKickoffSetup(int side,bool postGoal)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        yield return AttachLegacyGoalFlow();
        match.currentState=postGoal?MatchManager.GameState.PostGoalKickOffSetup:MatchManager.GameState.KickOffSetup;
        if(postGoal)match.kickoffManager.StartPostGoalKickoffSetupPhase();else match.kickoffManager.StartPreKickoffPhase();
        using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Assert.That(authority.Capture().stage,Is.EqualTo("kickoff.setup"));
        Submit("home",OnlineGroundPassAuthority.KickoffPlayerAction(OnlineRollPublication.ActorId(attacker)));
        var state=authority.Capture();
        Assert.That(state.selectedPlayer,Is.EqualTo(OnlineRollPublication.ActorId(attacker)));
        Assert.That(state.legalActions,Does.Not.Contain(OnlineGroundPassAuthority.KickoffPlayerAction(OnlineRollPublication.ActorId(defender))));
        Assert.That(state.legalActions,Does.Not.Contain(OnlineGroundPassAuthority.KickoffMoveAction(4*side,6)));
        if(postGoal)Assert.That(state.legalActions,Does.Not.Contain(OnlineGroundPassAuthority.KickoffMoveAction(0,0)));
        Assert.That(match.kickoffManager.TryExecuteOnlineKickoff(OnlineKickoffIntent.Move,target:defender.currentHex),Is.False);
        using(var guest=new OnlineRoomSession("kickoff-guest",true))
            Assert.That(match.kickoffManager.TryExecuteOnlineKickoff(OnlineKickoffIntent.Confirm),Is.False);
        var destination=Hex(-4*side,6);
        var command=OnlineGroundCommand(state,protocol.LastAcceptedSequence(OnlineTeam.Home)+1,OnlineGroundPassAuthority.KickoffMoveAction(-4*side,6));
        Assert.That(authority.Submit("home",command).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(authority.Submit("home",command).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        yield return Until(()=>match.kickoffManager.IsWaitingForMovementToComplete(),"real kickoff setup move starts");
        Assert.That(authority.Capture().playerMoving,Is.True);Assert.That(authority.Capture().legalActions,Is.Empty);
        pause.SetConnectionHold(true);var position=attacker.transform.position;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(Vector3.Distance(position,attacker.transform.position),Is.LessThan(.001f));
        pause.SetConnectionHold(false);
        yield return Until(()=>!match.kickoffManager.IsWaitingForMovementToComplete(),"setup movement finishes");
        Assert.That(attacker.currentHex,Is.SameAs(destination));
        Submit("home","kickoff.confirm");
        Assert.That(authority.Capture().owner,Is.EqualTo("Away"));
        Submit("away",OnlineGroundPassAuthority.KickoffPlayerAction(OnlineRollPublication.ActorId(defender)));
        Submit("away",OnlineGroundPassAuthority.KickoffMoveAction(4*side,6));
        yield return Until(()=>!match.kickoffManager.IsWaitingForMovementToComplete(),"away setup movement finishes");
        Assert.That(defender.currentHex,Is.SameAs(Hex(4*side,6)));
        var replica=new OnlineGroundPassReplica(protocol.MatchId);
        Assert.That(replica.Apply(authority.Capture()),Is.True);
        Assert.That(match.TimingRollSequence,Is.Zero);
    }
}
