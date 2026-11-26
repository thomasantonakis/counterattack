using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class OnlineRoomActionSceneFixture
{
    public IEnumerator RunRoomFinalThird(int side)
    {
        yield return RunRoomLongPass(side);
        var version=new OnlineCompatibility("room-f3","rules","catalog");
        var protocol=new OnlineMatchProtocol("room-f3","home","away","away",version);
        protocol.BindAuthenticatedAccount("home","home",version);protocol.BindAuthenticatedAccount("away","away",version);
        var authority=new OnlineGroundPassAuthority(board.match,protocol);
        using var pause=new OnlineSharedPause(protocol,board.match);
        long sequence=0;
        void Submit(string action)
        {
            var state=pause.Capture(authority);Assert.That(state.owner,Is.EqualTo("Home"));Assert.That(state.legalActions,Does.Contain(action),state.stage);
            Assert.That(authority.Submit("home",new OnlineMatchCommand(state.matchId,++sequence,state.revision,state.decisionId,action)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        Assert.That(authority.Capture().stage,Is.EqualTo("final-third.select"));
        var keeper=board.tokens.Single(t=>t.isHomeTeam && t.jerseyNumber==1);
        Submit(OnlineGroundPassAuthority.FinalThirdPlayerAction(OnlineRollPublication.ActorId(keeper)));
        double selectionDeadline=Time.realtimeSinceStartupAsDouble+5;
        while(authority.Capture().stage!="final-third.target" && Time.realtimeSinceStartupAsDouble<selectionDeadline)yield return null;
        Assert.That(authority.Capture().stage,Is.EqualTo("final-third.target"));
        var target=keeper.currentHex.GetNeighbors(board.grid).First(h=>h!=null && h.coordinates.x*side>keeper.currentHex.coordinates.x*side
            && authority.Capture().legalActions.Contains(OnlineGroundPassAuthority.FinalThirdTargetAction(h.coordinates.x,h.coordinates.z)));
        Submit(OnlineGroundPassAuthority.FinalThirdTargetAction(target.coordinates.x,target.coordinates.z));
        double deadline=Time.realtimeSinceStartupAsDouble+10;
        while(board.match.finalThirdManager.isActivated && Time.realtimeSinceStartupAsDouble<deadline)yield return null;
        Assert.That(board.match.finalThirdManager.isActivated,Is.False);Assert.That(keeper.currentHex,Is.SameAs(target));
        Assert.That(authority.Capture().stage,Does.StartWith("movement."));Assert.That(board.match.TimingRollSequence,Is.EqualTo(1));
    }
}
