using System.Collections;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlinePickupKey(int side,bool home,bool defending)
        =>RunOnlinePickupKeyCore(side,home,defending,null);
    public IEnumerator RunOnlineRestrictedPickup(int side,bool home,bool setPiece)
        =>RunOnlinePickupKeyCore(side,home,false,setPiece?"set-piece":"hanging");
    private IEnumerator RunOnlinePickupKeyCore(int side,bool home,bool defending,string restriction)
    {
        var authority=OnlineGroundBoard(home?side:-side,true,false,out var protocol,out _);
        bool collectorHome=home!=defending;var collector=Token("Online pickup collector",collectorHome,false,11);if(!home)match.ChangePossession();
        var previous=home?attacker:defender;Place(previous,Hex(-9*side,-6));Place(collector,Hex(-6*side));var pickup=Hex(-5*side);
        ball.PlaceAtCell(pickup);match.SetLastToken(previous);match.UpdatePossessionAfterPass(pickup);Assert.That(match.attackHasPossession,Is.False);
        if(restriction=="hanging")match.SetHangingPass("ground",collector);else if(restriction=="set-piece")match.MarkSetPieceTakerForNextTouchExclusion(collector);
        match.BroadcastSafeEndofMovementPhase(countMovementAction:false,triggerFinalThird:false);Subscribe(movement);AttachAuthoredPanels();using var pause=new OnlineSharedPause(protocol,match);
        string attack=home?"home":"away",own=collectorHome?"home":"away";var ownTeam=collectorHome?OnlineTeam.Home:OnlineTeam.Away;
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        if(!movement.isActivated)Submit(attack,"movement.start");
        if(defending)Submit(attack,"movement.forfeit");Submit(own,"movement.player:"+OnlineRollPublication.ActorId(collector));
        var state=pause.Capture(authority);
        if(restriction!=null)
        {
            Assert.That(movement.isBallPickable,Is.False);Assert.That(state.legalActions,Does.Not.Contain("movement.target:"+pickup.coordinates.x+":"+pickup.coordinates.z));Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.V),Is.Null);
            Assert.That(movement.TryExecuteOnlineMovement(OnlineMovementIntent.Move,target:pickup),Is.False);Assert.That(collector.currentHex,Is.SameAs(Hex(-6*side)));Assert.That(pickup.GetOccupyingToken(),Is.Null);Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(previous));Assert.That(match.TimingRollSequence,Is.Zero);yield break;
        }
        Assert.That(movement.isBallPickable,Is.True);string action=OnlineBoardActionPicker.Key(state,KeyCode.V);Assert.That(action,Is.EqualTo("movement.target:"+pickup.coordinates.x+":"+pickup.coordinates.z));
        var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;foreach(var packet in OnlineGroundPassWire.Encode(OnlineGroundPassMessage.Command(state,protocol.LastAcceptedSequence(ownTeam)+1,action)))decoded=wire.Receive(packet,packet.Length);
        var request=decoded.ToCommand();var rng=Random.state;string other=own=="home"?"away":"home";Assert.That(authority.Submit(other,request).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));Assert.That(Random.state,Is.EqualTo(rng));
        pause.SetConnectionHold(true);Assert.That(pause.Capture(authority).legalActions,Is.Empty);Assert.That(movement.TryExecuteOnlineMovement(OnlineMovementIntent.Move,target:pickup),Is.False);pause.SetConnectionHold(false);
        using(var guest=new OnlineRoomSession("pickup-key-guest",true))Assert.That(movement.TryExecuteOnlineMovement(OnlineMovementIntent.Move,target:pickup),Is.False);
        state=pause.Capture(authority);request=OnlineGroundCommand(state,protocol.LastAcceptedSequence(ownTeam)+1,OnlineBoardActionPicker.Key(state,KeyCode.V));Assert.That(authority.Submit(own,request).Status,Is.EqualTo(OnlineCommandStatus.Accepted));Assert.That(authority.Submit(own,request).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        yield return Until(()=>!movement.isPlayerMoving && collector.currentHex==pickup && match.LastTokenToTouchTheBallOnPurpose==collector,"actual movement collects free ball and binds ownership");
        Assert.That(ball.GetCurrentHex(),Is.SameAs(pickup));Assert.That(pickup.GetOccupyingToken(),Is.SameAs(collector));Assert.That(match.attackHasPossession,Is.True);Assert.That(match.teamInAttack,Is.EqualTo(collectorHome?MatchManager.TeamInAttack.Home:MatchManager.TeamInAttack.Away));Assert.That(match.TimingRollSequence,Is.Zero);
        Assert.That(OnlineBoardActionPicker.Key(pause.Capture(authority),KeyCode.V),Is.Null,"Collection retires pickup shortcut");
        if(defending){Assert.That(match.gameData.stats.GetPlayerStats(collector).possessionWon,Is.EqualTo(1));Assert.That(match.gameData.stats.GetPlayerStats(collector).xRecoveries,Is.EqualTo(1));}
        else{yield return Until(()=>authority.Capture().stage=="dribble.target","attacking collection retains real dribble continuation");Submit(own,"dribble.finish");Assert.That(movement.movedTokens,Does.Contain(collector));}
    }
}
