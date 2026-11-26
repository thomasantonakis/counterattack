using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlinePreMove(int side,bool home,string choice)
    {
        var authority=OnlineGroundBoard(home?side:-side,true,false,out var protocol,out _);if(!home)match.ChangePossession();
        var carrier=home?attacker:defender;var opponent=home?defender:attacker;var keeper=home?awayGK:homeGK;
        string attack=home?"home":"away",defence=home?"away":"home";
        Place(opponent,Hex(side));Place(carrier,Hex(0));ball.PlaceAtCell(carrier.currentHex);match.SetLastToken(carrier);match.UpdatePossessionAfterPass(carrier.currentHex);
        match.BroadcastSafeEndofMovementPhase(countMovementAction:false,triggerFinalThird:false);Subscribe(movement);AttachAuthoredPanels();using var pause=new OnlineSharedPause(protocol,match);
        OnlineTeam Team(string connection)=>connection=="home"?OnlineTeam.Home:OnlineTeam.Away;
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,protocol.LastAcceptedSequence(Team(connection))+1,action);
        void GuardMove(HexCell target)
        {
            pause.SetConnectionHold(true);Assert.That(pause.Capture(authority).legalActions,Is.Empty);Assert.That(movement.TryExecuteOnlineMovement(OnlineMovementIntent.Move,target:target),Is.False);pause.SetConnectionHold(false);
            using(var guest=new OnlineRoomSession("pre-move-guest",true))Assert.That(movement.TryExecuteOnlineMovement(OnlineMovementIntent.Move,target:target),Is.False);
        }
        if(choice=="uncommitted-hold")
        {
            Submit(attack,"movement.start");Submit(attack,"movement.player:"+OnlineRollPublication.ActorId(home?homeGK:awayGK));var state=pause.Capture(authority);Assert.That(state.legalActions,Does.Contain("ground.start"));
            var old=OnlineGroundCommand(state,protocol.LastAcceptedSequence(Team(attack))+1,"ground.start");pause.SetConnectionHold(true);var held=pause.Capture(authority);Assert.That(held.legalActions,Is.Empty);Assert.That(OnlineBoardActionPicker.Key(held,KeyCode.P),Is.Null);Assert.That(authority.Submit(attack,old).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));Assert.That(movement.isActivated,Is.True);Assert.That(ground.isActivated,Is.False);
            pause.SetConnectionHold(false);Assert.That(pause.Capture(authority).legalActions,Does.Contain("ground.start"));Submit(attack,"ground.start");Assert.That(movement.isActivated,Is.False);Assert.That(ground.isActivated && ground.isAwaitingTargetSelection,Is.True);Assert.That(match.TimingRollSequence,Is.Zero);yield break;
        }
        if(choice=="existing-outfield-offer")
        {
            Submit(attack,"movement.start");Submit(attack,"movement.forfeit");Submit(defence,"movement.player:"+OnlineRollPublication.ActorId(opponent));Assert.That(movement.isWaitingForTackleDecisionWithoutMoving,Is.True);
            Assert.That(pause.Capture(authority).legalActions,Does.Contain("movement.forfeit"),"Preserve the existing outfield offer until its physical-X behavior is reviewed");Assert.That(match.TimingRollSequence,Is.Zero);yield break;
        }
        if(choice=="throw-in")
        {
            Place(carrier,Hex(0,11));Place(opponent,Hex(side,11));ball.PlaceAtCell(Hex(0,12));match.SetLastToken(carrier);
            var throws=Add<ThrowInManager>();throws.ball=ball;throws.hexGrid=grid;throws.movementPhaseManager=movement;throws.groundBallManager=ground;throws.headerManager=movement.headerManager;throws.finalThirdManager=match.finalThirdManager;
            Subscribe(match.finalThirdManager);throws.StartThrowInPreparation(Hex(0,12),home?MatchManager.TeamInAttack.Home:MatchManager.TeamInAttack.Away,false);
            yield return Until(()=>authority.Capture().stage=="throw.taker","actual throw-in taker selection");Submit(attack,"throw.player:"+OnlineRollPublication.ActorId(carrier));
            yield return Until(()=>authority.Capture().legalActions.Contains("movement.forfeit"),"actual throw-in attacking movement",180);Submit(attack,"movement.forfeit");
            yield return Until(()=>movement.isMovementPhaseDef,"actual required defensive clearance");var state=pause.Capture(authority);Assert.That(state.legalActions,Does.Not.Contain("movement.forfeit"));Assert.That(movement.TryExecuteOnlineMovement(OnlineMovementIntent.Forfeit),Is.False);Assert.That(movement.isMovementPhaseDef,Is.True);
            Submit(defence,"movement.player:"+OnlineRollPublication.ActorId(opponent));var target=grid.highlightedHexes.First(h=>h!=null && h.GetOccupyingToken()==null && movement.IsHexValidForMovement(h) && HexGridUtils.GetHexStepDistance(carrier.currentHex,h)>2);
            GuardMove(target);Submit(defence,"movement.target:"+target.coordinates.x+":"+target.coordinates.z);yield return Until(()=>!movement.isPlayerMoving && opponent.currentHex==target,"required defender leaves protected zone");
            Assert.That(pause.Capture(authority).legalActions,Does.Contain("movement.forfeit"));Submit(defence,"movement.forfeit");Assert.That(movement.isMovementPhaseDef,Is.False);Assert.That(match.TimingRollSequence,Is.Zero);yield break;
        }
        if(choice=="keeper-switch")
        {
            Place(carrier,Hex(5*side));Place(keeper,Hex(6*side));Place(opponent,Hex(2*side,-10));ball.PlaceAtCell(carrier.currentHex);match.UpdatePossessionAfterPass(carrier.currentHex);
            Submit(attack,"movement.start");Submit(attack,"movement.forfeit");Submit(defence,"movement.player:"+OnlineRollPublication.ActorId(keeper));Assert.That(movement.isWaitingForTackleDecisionWithoutMoving,Is.True);
            var state=pause.Capture(authority);string select="movement.player:"+OnlineRollPublication.ActorId(opponent);Assert.That(state.legalActions,Does.Contain(select));Assert.That(state.legalActions,Does.Not.Contain("movement.forfeit"),"A waiting stationary tackle cannot silently forfeit its phase");
            pause.SetConnectionHold(true);Assert.That(movement.TryExecuteOnlineMovement(OnlineMovementIntent.SelectPlayer,opponent),Is.False);pause.SetConnectionHold(false);using(var guest=new OnlineRoomSession("keeper-switch-guest",true))Assert.That(movement.TryExecuteOnlineMovement(OnlineMovementIntent.SelectPlayer,opponent),Is.False);
            Submit(defence,select);Assert.That(movement.selectedToken,Is.SameAs(opponent));Assert.That(movement.isWaitingForTackleDecisionWithoutMoving,Is.False);Assert.That(movement.movedTokens,Has.No.Member(keeper));
            var target=grid.highlightedHexes.First(h=>h!=null && h.GetOccupyingToken()==null && movement.IsHexValidForMovement(h));Submit(defence,"movement.target:"+target.coordinates.x+":"+target.coordinates.z);
            yield return Until(()=>!movement.isPlayerMoving && opponent.currentHex==target,"alternative defender completes its move");Assert.That(movement.movedTokens,Does.Contain(opponent));Assert.That(movement.movedTokens,Has.No.Member(keeper));Assert.That(match.TimingRollSequence,Is.Zero);yield break;
        }
        Submit(attack,"movement.start");Submit(attack,"movement.player:"+OnlineRollPublication.ActorId(carrier));Assert.That(movement.isWaitingForNutmegDecisionWithoutMoving,Is.True);Assert.That(movement.isDribblerRunning,Is.False);
        if(choice=="nutmeg-direct")
        {
            string action="duel.victim:"+OnlineRollPublication.ActorId(opponent);Assert.That(OnlineBoardActionPicker.Actor(pause.Capture(authority),OnlineRollPublication.ActorId(opponent)),Is.EqualTo(action));
            Assert.That(movement.TryExecuteOnlineGroundDuel(OnlineGroundDuelIntent.Victim,carrier),Is.False);pause.SetConnectionHold(true);Assert.That(movement.TryExecuteOnlineGroundDuel(OnlineGroundDuelIntent.Victim,opponent),Is.False);pause.SetConnectionHold(false);using(var guest=new OnlineRoomSession("initial-victim-guest",true))Assert.That(movement.TryExecuteOnlineGroundDuel(OnlineGroundDuelIntent.Victim,opponent),Is.False);
            Submit(attack,action);yield return Until(()=>authority.Capture().stage=="duel.roll","direct victim click starts the real nutmeg");RigOnlineLongRoll(2);Submit(defence,"duel.roll");RigOnlineLongRoll(6);Submit(attack,"duel.roll");
            yield return Until(()=>authority.Capture().stage=="duel.reposition","nutmeg winner reaches forced reposition");Assert.That(authority.Capture().legalActions,Does.Not.Contain("duel.stay"));var target=movement.repositionHexes.First();Submit(attack,"duel.target:"+target.coordinates.x+":"+target.coordinates.z);
            yield return Until(()=>!movement.IsResolvingReposition && !movement.isWaitingForReposition && carrier.currentHex==target,"actual nutmeg moves through the victim");Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(carrier));Assert.That(match.gameData.stats.GetPlayerStats(carrier).groundDuelsWon,Is.EqualTo(1));Assert.That(match.TimingRollSequence,Is.EqualTo(2));yield break;
        }
        if(choice=="nutmeg-move")
        {
            var target=grid.highlightedHexes.First(h=>h!=null && h.GetOccupyingToken()==null && movement.IsHexValidForMovement(h) && HexGridUtils.GetHexStepDistance(opponent.currentHex,h)>1);
            string action="movement.target:"+target.coordinates.x+":"+target.coordinates.z;Assert.That(pause.Capture(authority).legalActions,Does.Contain(action));GuardMove(target);Submit(attack,action);
            yield return Until(()=>!movement.isPlayerMoving && carrier.currentHex==target && authority.Capture().stage=="dribble.target","moving away declines initial nutmeg and continues actual dribble");Assert.That(movement.isWaitingForNutmegDecisionWithoutMoving,Is.False);Submit(attack,"dribble.finish");Assert.That(movement.movedTokens,Does.Contain(carrier));Assert.That(match.TimingRollSequence,Is.Zero);yield break;
        }
        if(choice=="offline-blocked-forfeit")Invoke(movement,"OnKeyReceived",new KeyPressData(KeyCode.X,false,false,false));
        else{var state=pause.Capture(authority);Assert.That(OnlineBoardActionPicker.Key(state,KeyCode.X),Is.Null);Assert.That(movement.TryExecuteOnlineMovement(OnlineMovementIntent.Forfeit),Is.False);}
        Assert.That(movement.isMovementPhaseAttack,Is.True,"The source X guard requires answering or moving out of an initial nutmeg choice");Assert.That(movement.isWaitingForNutmegDecisionWithoutMoving,Is.True);Assert.That(movement.movedTokens,Has.No.Member(carrier));Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(carrier));Assert.That(match.TimingRollSequence,Is.Zero);
    }
}
