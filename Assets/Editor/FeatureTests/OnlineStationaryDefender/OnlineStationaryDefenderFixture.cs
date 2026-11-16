using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineStationaryDefender(int side,bool home,bool keeper,bool move)
        =>RunOnlineStationaryDefenderCore(side,home,keeper,move,true);
    public IEnumerator RunOnlineDistantDefender(int side,bool home)
        =>RunOnlineStationaryDefenderCore(side,home,false,true,false);
    private IEnumerator RunOnlineStationaryDefenderCore(int side,bool home,bool keeper,bool move,bool adjacent)
    {
        var authority=OnlineGroundBoard(home?side:-side,true,false,out var protocol,out _);if(!home)match.ChangePossession();
        var attackSide=home?OnlineTeam.Home:OnlineTeam.Away;var defenceSide=home?OnlineTeam.Away:OnlineTeam.Home;
        string attack=home?"home":"away",defence=home?"away":"home";
        var carrier=home?attacker:defender;var other=home?defender:attacker;var challenger=keeper?(home?awayGK:homeGK):other;
        Place(carrier,Hex(5*side));if(keeper)Place(other,Hex(2*side,-10));Place(challenger,Hex((adjacent?6:8)*side));
        ball.PlaceAtCell(carrier.currentHex);match.SetLastToken(carrier);match.UpdatePossessionAfterPass(carrier.currentHex);match.BroadcastSafeEndofMovementPhase(countMovementAction:false,triggerFinalThird:false);
        Subscribe(movement);AttachAuthoredPanels();duelPanel.resultSeconds=.1f;using var pause=new OnlineSharedPause(protocol,match);
        void Submit(string connection,string action)
        {
            var state=pause.Capture(authority);var team=connection=="home"?OnlineTeam.Home:OnlineTeam.Away;var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;
            foreach(var packet in OnlineGroundPassWire.Encode(OnlineGroundPassMessage.Command(state,protocol.LastAcceptedSequence(team)+1,action)))decoded=wire.Receive(packet,packet.Length);
            Assert.That(state.legalActions,Does.Contain(action));Assert.That(authority.Submit(connection,decoded.ToCommand()).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        Submit(attack,"movement.start");Submit(attack,"movement.forfeit");yield return Until(()=>movement.isMovementPhaseDef,"defensive movement turn");Submit(defence,"movement.player:"+OnlineRollPublication.ActorId(challenger));
        yield return Until(()=>movement.isAwaitingHexDestination,"real defender selection offers destinations");
        Assert.That(movement.isWaitingForTackleDecisionWithoutMoving,Is.EqualTo(adjacent));
        var offered=pause.Capture(authority);Assert.That(offered.owner,Is.EqualTo(defenceSide.ToString()));Assert.That(offered.selectedPlayer,Is.EqualTo(OnlineRollPublication.ActorId(challenger)));
        Assert.That(offered.legalActions.Contains("duel.tackle"),Is.EqualTo(adjacent));Assert.That(offered.legalActions.Any(a=>a.StartsWith("movement.target:")),Is.True,"The defender can move away instead of tackling");
        if(!adjacent)Assert.That(movement.TryExecuteOnlineGroundDuel(OnlineGroundDuelIntent.Tackle),Is.False,"A distant defender cannot start a stationary tackle");
        Assert.That(offered.legalActions.Contains("duel.stand"),Is.EqualTo(keeper),"Stationary outfield selection has no invented stand command");
        var destination=grid.highlightedHexes.First(h=>h!=null && h.GetOccupyingToken()==null && movement.IsHexValidForMovement(h) && HexGridUtils.GetHexStepDistance(carrier.currentHex,h)>1);
        var guestRng=Random.state;using(var guest=new OnlineRoomSession("stationary-guest",true)){Assert.That(movement.TryExecuteOnlineGroundDuel(OnlineGroundDuelIntent.Tackle),Is.False);Assert.That(movement.TryExecuteOnlineMovement(OnlineMovementIntent.Move,target:destination),Is.False);}Assert.That(Random.state,Is.EqualTo(guestRng));
        if(adjacent){var wrong=OnlineGroundCommand(offered,protocol.LastAcceptedSequence(attackSide)+1,"duel.tackle");Assert.That(authority.Submit(attack,wrong).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));}
        pause.SetConnectionHold(true);Assert.That(pause.Capture(authority).legalActions,Is.Empty);Assert.That(movement.TryExecuteOnlineGroundDuel(OnlineGroundDuelIntent.Tackle),Is.False);Assert.That(movement.TryExecuteOnlineMovement(OnlineMovementIntent.Move,target:destination),Is.False);pause.SetConnectionHold(false);
        if(move)
        {
            Submit(defence,"movement.target:"+destination.coordinates.x+":"+destination.coordinates.z);yield return Until(()=>!movement.isPlayerMoving && !movement.isWaitingForTackleDecisionWithoutMoving,"normal move away clears stationary choice");
            Assert.That(challenger.currentHex,Is.SameAs(destination));Assert.That(movement.movedTokens,Does.Contain(challenger));Assert.That(match.TimingRollSequence,Is.Zero);Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(carrier));yield break;
        }
        Assert.That(OnlineBoardActionPicker.Key(pause.Capture(authority),KeyCode.T),Is.EqualTo("duel.tackle"));Submit(defence,"duel.tackle");
        Assert.That(movement.isWaitingForTackleDecisionWithoutMoving,Is.False);Assert.That(movement.movedTokens,Does.Contain(challenger));yield return Until(()=>authority.Capture().stage=="duel.roll","stationary foot tackle opens existing duel");
        RigOnlineLongRoll(2);Submit(defence,"duel.roll");RigOnlineLongRoll(6);Submit(attack,"duel.roll");yield return Until(()=>authority.Capture().stage=="duel.reposition","carrier wins existing duel and may reposition");
        Assert.That(authority.Capture().owner,Is.EqualTo(attackSide.ToString()));Submit(attack,"duel.stay");yield return Until(()=>!movement.isWaitingForReposition && !movement.IsResolvingReposition,"real duel continuation resumes");
        Assert.That(match.TimingRollSequence,Is.EqualTo(2));Assert.That(match.gameData.stats.GetPlayerStats(carrier).groundDuelsWon,Is.EqualTo(1));Assert.That(match.gameData.stats.GetPlayerStats(challenger).groundDuelsInvolved,Is.EqualTo(1));Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(carrier));
    }
}
