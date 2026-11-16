using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineLooseSnapshotChoice(int side,string choice)
    {
        yield return PrepareLegacyLooseSnapshot(side);movement.enabled=loose.enabled=match.shotManager.enabled=false;
        var shot=match.shotManager;var carrier=match.LastTokenToTouchTheBallOnPurpose;long draws=match.TimingRollSequence;
        var compatibility=OnlineGroundVersion();var protocol=new OnlineMatchProtocol("loose-snapshot-choice","home-account","away-account","away-account",compatibility);
        protocol.BindAuthenticatedAccount("home-account","home",compatibility);protocol.BindAuthenticatedAccount("away-account","away",compatibility);
        var authority=new OnlineGroundPassAuthority(match,protocol);using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        var state=authority.Capture();Assert.That(state.stage,Is.EqualTo("shot.loose-choice"));Assert.That(state.owner,Is.EqualTo("Home"));
        Assert.That(state.selectedPlayer,Is.EqualTo(OnlineRollPublication.ActorId(carrier)));Assert.That(state.legalActions,Is.EquivalentTo(new[]{"shot.start","shot.decline"}));
        string action=choice is "shoot" or "hold"?"shot.start":"shot.decline";
        var request=OnlineGroundCommand(state,1,action);Assert.That(authority.Submit("away",request).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        using(var guest=new OnlineRoomSession("loose-snapshot-guest",true))Assert.That(shot.TryExecuteOnlineShot(OnlineShotIntent.Select),Is.False);
        if(choice=="cancel")
        {
            Invoke(shot,"ResetShotProcess");Assert.That(authority.Submit("home",request).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(match.gameData.stats.GetPlayerStats(carrier).shotsAttempted,Is.Zero);Assert.That(match.TimingRollSequence,Is.EqualTo(draws));yield break;
        }
        if(choice=="hold")
        {
            Assert.That(pause.Submit("away",new OnlineMatchCommand(protocol.MatchId,1,pause.Revision,"shared-pause",OnlineSharedPause.RequestAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            pause.SetConnectionHold(true);Assert.That(shot.TryExecuteOnlineShot(OnlineShotIntent.Select),Is.False);yield return new WaitForSecondsRealtime(.1f);
            pause.SetConnectionHold(false);Assert.That(shot.TryExecuteOnlineShot(OnlineShotIntent.Select),Is.False);
            Assert.That(pause.Submit("away",new OnlineMatchCommand(protocol.MatchId,2,pause.Revision,"shared-pause",OnlineSharedPause.ResumeAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            request=OnlineGroundCommand(authority.Capture(),1,action);
        }
        var message=OnlineGroundPassMessage.Command(authority.Capture(),1,action);var reader=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;
        foreach(var packet in OnlineGroundPassWire.Encode(message))decoded=reader.Receive(packet,packet.Length);
        Assert.That(authority.Submit("home",decoded.ToCommand()).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(authority.Submit("home",request).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        if(choice is "shoot" or "hold")
        {
            Assert.That(shot.isActivated,Is.True);Assert.That(shot.shotType,Is.EqualTo("snapshot"));Assert.That(shot.shooter,Is.SameAs(carrier));
            Assert.That(authority.Capture().stage,Is.EqualTo("shot.snapshot.select"));Assert.That(match.gameData.stats.GetPlayerStats(carrier).shotsAttempted,Is.EqualTo(1));
            Assert.That(movement.movedTokens.Contains(carrier),Is.False,"New collector must not replace the original pending mover");
        }
        else
        {
            yield return Until(()=>!shot.isWaitingForSnapshotDecisionFromLoose && movement.isAwaitingTokenSelection,"declining collected snapshot releases the original movement parent");
            Assert.That(movement.attackersMoved,Is.EqualTo(1));Assert.That(match.gameData.stats.GetPlayerStats(carrier).shotsAttempted,Is.Zero);
        }
        Assert.That(movement.movedTokens.Count,Is.EqualTo(1));Assert.That(match.TimingRollSequence,Is.EqualTo(draws));
        Assert.That(match.PreviousTokenToTouchTheBallOnPurpose,Is.Null);Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(carrier));
    }
}
