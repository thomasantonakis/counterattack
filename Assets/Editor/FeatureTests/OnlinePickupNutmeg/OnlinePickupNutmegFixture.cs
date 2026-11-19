using System.Collections;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlinePickupNutmeg(int side,bool home,bool attempt)
    {
        var authority=OnlineGroundBoard(home?side:-side,true,false,out var protocol,out _);
        var collector=Token("Online pickup nutmeg collector",home,false,11);
        if(!home)match.ChangePossession();
        var previous=home?attacker:defender;var opponent=home?defender:attacker;
        Place(previous,Hex(-9*side,-6));Place(collector,Hex(-6*side));Place(opponent,Hex(-3*side));
        var pickup=Hex(-5*side);var next=Hex(-4*side);
        ball.PlaceAtCell(pickup);match.SetLastToken(previous);match.UpdatePossessionAfterPass(pickup);
        match.BroadcastSafeEndofMovementPhase(countMovementAction:false,triggerFinalThird:false);
        Subscribe(movement);AttachAuthoredPanels();
        using var pause=new OnlineSharedPause(protocol,match);
        string own=home?"home":"away",other=home?"away":"home";var team=home?OnlineTeam.Home:OnlineTeam.Away;
        void Submit(string action)=>OnlineAccepted(authority,own,protocol.LastAcceptedSequence(team)+1,action);
        if(!movement.isActivated)Submit("movement.start");Submit("movement.player:"+OnlineRollPublication.ActorId(collector));
        var state=pause.Capture(authority);var action=OnlineBoardActionPicker.Key(state,KeyCode.V);
        Assert.That(action,Is.EqualTo("movement.target:"+pickup.coordinates.x+":0"));Submit(action);
        yield return Until(()=>!movement.isPlayerMoving && collector.currentHex==pickup && authority.Capture().stage=="dribble.target","real V pickup enters running dribble");
        Submit("movement.target:"+next.coordinates.x+":0");
        yield return Until(()=>!movement.isPlayerMoving && movement.isWaitingForNutmegDecision,"post-pickup move offers nutmeg decision");
        state=pause.Capture(authority);Assert.That(state.stage,Is.EqualTo("dribble.nutmeg"));Assert.That(state.owner,Is.EqualTo(home?"Home":"Away"));
        var key=attempt?KeyCode.N:KeyCode.X;action=OnlineBoardActionPicker.Key(state,key);
        Assert.That(action,Is.EqualTo(attempt?"duel.nutmeg":"dribble.decline-nutmeg"));
        pause.SetConnectionHold(true);Assert.That(OnlineBoardActionPicker.Key(pause.Capture(authority),key),Is.Null);pause.SetConnectionHold(false);
        state=pause.Capture(authority);var message=OnlineGroundPassMessage.Command(state,protocol.LastAcceptedSequence(team)+1,action);
        var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;
        foreach(var packet in OnlineGroundPassWire.Encode(message))decoded=wire.Receive(packet,packet.Length);
        Assert.That(authority.Submit(other,decoded.ToCommand()).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(authority.Submit(own,decoded.ToCommand()).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(movement.isWaitingForNutmegDecision,Is.False);Assert.That(ball.GetCurrentHex(),Is.SameAs(next));
        Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(collector));Assert.That(match.TimingRollSequence,Is.Zero);
        if(attempt)
        {
            yield return Until(()=>movement.isNutmegInProgress || movement.lookingForNutmegVictim,"N proceeds into authentic nutmeg resolution");
            Assert.That(movement.isWaitingForInterceptionDiceRoll,Is.False);
        }
        else
        {
            yield return Until(()=>movement.isWaitingForInterceptionDiceRoll,"X declines nutmeg and offers defender steal");
            Assert.That(authority.Capture().stage,Is.EqualTo("dribble.steal"));Assert.That(authority.Capture().owner,Is.EqualTo(home?"Away":"Home"));
        }
    }
}
