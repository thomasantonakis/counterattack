using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineGroundDuel(int side,string kind,string branch)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        Place(defender,Hex(3*side));Subscribe(movement);AttachAuthoredPanels();duelPanel.resultSeconds=.1f;
        if(branch=="select")
        {
            var extra=Token("Second nutmeg candidate",false,false,7);
            var position=Hex(2*side).GetNeighbors(grid).First(h=>h!=null && h.GetOccupyingToken()==null && !h.isOutOfBounds
                && HexGridUtils.GetHexStepDistance(Hex(side),h)>1);
            Place(extra,position);
        }
        using var pause=new OnlineSharedPause(protocol,match);pause.Capture(authority);
        void Submit(string connection,string action)=>OnlineAccepted(authority,connection,
            protocol.LastAcceptedSequence(connection=="home"?OnlineTeam.Home:OnlineTeam.Away)+1,action);
        Submit("home","movement.start");
        if(kind=="nutmeg")
        {
            Submit("home","movement.player:home:9");Submit("home","movement.target:"+side+":0");
            yield return Until(()=>authority.Capture().stage=="dribble.target","first dribble pace before nutmeg");
            Submit("home","movement.target:"+(2*side)+":0");
            yield return Until(()=>movement.isWaitingForNutmegDecision,"real mid-dribble nutmeg offer");
            Submit("home","duel.nutmeg");
            if(branch=="select")
            {
                Assert.That(authority.Capture().stage,Is.EqualTo("duel.victim"));
                Assert.That(authority.Capture().legalActions.Count(a=>a.StartsWith("duel.victim:")),Is.EqualTo(2));
                Assert.That(movement.TryExecuteOnlineGroundDuel(OnlineGroundDuelIntent.Victim,attacker),Is.False);
                Submit("home","duel.victim:away:8");
                yield return Until(()=>authority.Capture().stage=="dribble.steal","unselected candidate owns the pre-nutmeg steal");
                Assert.That(authority.Capture().selectedPlayer,Is.EqualTo("away:7"));
                RigOnlineLongRoll(2);Submit("away","dribble.roll");
            }
        }
        else
        {
            Submit("home","movement.forfeit");Submit("away","movement.player:away:8");
            Submit("away","movement.target:"+side+":0");
            yield return Until(()=>movement.isWaitingForTackleDecision,"moved defender reaches tackle-or-stand choice");
            Assert.That(authority.Capture().stage,Is.EqualTo("duel.choice"));
            Submit("away",branch=="stand"?"duel.stand":"duel.tackle");
            if(branch=="stand")
            {
                Assert.That(movement.defendersMoved,Is.EqualTo(1));Assert.That(match.TimingRollSequence,Is.Zero);
                Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(attacker));yield break;
            }
        }
        yield return Until(()=>authority.Capture().stage=="duel.roll","defender owns first ground-duel roll");
        var first=authority.Capture();Assert.That(first.owner,Is.EqualTo("Away"));Assert.That(first.selectedPlayer,Is.EqualTo("away:8"));
        using(var guest=new OnlineRoomSession("ground-duel-guest",true))
            Assert.That(movement.TryExecuteOnlineGroundDuel(OnlineGroundDuelIntent.Roll),Is.False);
        var late=OnlineGroundCommand(first,protocol.LastAcceptedSequence(OnlineTeam.Away)+1,"duel.roll");
        void Roll(string connection,int face)
        {
            RigOnlineLongRoll(face);
            if(branch=="automatic")match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
            else Submit(connection,"duel.roll");
        }
        bool wonDefence=branch.StartsWith("defender");
        Roll("away",wonDefence?6:branch=="tie"?3:2);
        Assert.That(authority.Capture().owner,Is.EqualTo("Home"));
        Assert.That(authority.Submit("away",late).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        Roll("home",wonDefence?2:branch=="tie"?(kind=="nutmeg"?4:3):6);
        Assert.That(match.TimingRollSequence,Is.EqualTo(branch=="select"?3:2));
        Assert.That(match.CaptureOnlineRolls().Select(r=>r.actorId),Is.EqualTo(branch=="select"?new[]{"away:7","away:8","home:9"}:new[]{"away:8","home:9"}));
        if(branch=="cancel")
        {
            movement.ResetMovementPhase();yield return new WaitForSecondsRealtime(.3f);
            Assert.That(match.gameData.stats.GetPlayerStats(attacker).groundDuelsWon,Is.Zero);
            Assert.That(match.gameData.stats.GetPlayerStats(defender).groundDuelsWon,Is.Zero);yield break;
        }
        if(branch=="hold")yield return HoldOnlineHeaderContinuation(pause,protocol,()=>!movement.isWaitingForReposition && movement.isActivated && match.TimingRollSequence==2);
        if(branch=="tie")
        {
            yield return Until(()=>authority.Capture().stage=="loose.direction","tied ground duel enters real scatter");
            RigOnlineLongRoll(4);var turn=authority.Capture();Submit(turn.owner=="Home"?"home":"away","loose.roll");
            yield return Until(()=>authority.Capture().stage=="loose.distance","ground-duel scatter distance");
            RigOnlineLongRoll(2);turn=authority.Capture();Submit(turn.owner=="Home"?"home":"away","loose.roll");
            for(int i=0;i<6 && loose.isActivated;i++)
            {
                yield return Until(()=>!loose.isActivated || authority.Capture().legalActions.Contains("loose.roll"),"scatter collection or owned recovery roll",120);
                if(!loose.isActivated)break;
                RigOnlineLongRoll(6);turn=authority.Capture();Submit(turn.owner=="Home"?"home":"away","loose.roll");
            }
            yield return Until(()=>!loose.isActivated,"tied ground-duel scatter completes",120);
            Assert.That(match.gameData.stats.GetPlayerStats(attacker).groundDuelsWon,Is.Zero);
            Assert.That(match.gameData.stats.GetPlayerStats(defender).groundDuelsWon,Is.Zero);yield break;
        }
        yield return Until(()=>authority.Capture().stage=="duel.reposition","winner owns real reposition");
        Assert.That(authority.Capture().owner,Is.EqualTo(wonDefence?"Away":"Home"));
        if(kind=="nutmeg" && !wonDefence)
        {
            Assert.That(authority.Capture().legalActions,Does.Not.Contain("duel.stay"),"successful nutmeg must move through the victim");
            Assert.That(movement.TryExecuteOnlineGroundDuel(OnlineGroundDuelIntent.Stay),Is.False);
        }
        Assert.That(movement.TryExecuteOnlineGroundDuel(OnlineGroundDuelIntent.Reposition,target:attacker.currentHex),Is.False,"occupied reposition is not admitted");
        var winner=wonDefence?defender:attacker;
        if((branch=="stay" && kind=="tackle") || branch=="defender-stay")
        {
            var origin=winner.currentHex;
            Submit(wonDefence?"away":"home","duel.stay");
            yield return Until(()=>!movement.isWaitingForReposition && !movement.IsResolvingReposition,"optional reposition forfeit");
            Assert.That(winner.currentHex,Is.SameAs(origin));
            Assert.That(match.gameData.stats.GetPlayerStats(winner).groundDuelsWon,Is.EqualTo(1));
            if(wonDefence)yield return Until(()=>match.teamInAttack==MatchManager.TeamInAttack.Away && match.LastTokenToTouchTheBallOnPurpose==defender,"forfeited defender reposition continues recovery");
            yield break;
        }
        var destination=movement.repositionHexes.First();
        Submit(wonDefence?"away":"home","duel.target:"+destination.coordinates.x+":"+destination.coordinates.z);
        yield return Until(()=>!movement.IsResolvingReposition && !movement.isWaitingForReposition,"owned reposition completes");
        Assert.That(winner.currentHex,Is.SameAs(destination));
        Assert.That(match.gameData.stats.GetPlayerStats(winner).groundDuelsWon,Is.EqualTo(1));
        if(wonDefence)yield return Until(()=>match.teamInAttack==MatchManager.TeamInAttack.Away && match.LastTokenToTouchTheBallOnPurpose==defender,"defender win continues into actual possession");
        else Assert.That(match.LastTokenToTouchTheBallOnPurpose,Is.SameAs(attacker));
    }
}
