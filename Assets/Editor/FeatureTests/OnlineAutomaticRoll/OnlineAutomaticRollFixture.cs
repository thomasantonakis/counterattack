using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOnlineAutomaticLongRoll(int side,bool interrupted)
    {
        var authority=OnlineGroundBoard(side,true,false,out var protocol,out _);
        AttachAuthoredPanels();duelPanel.resultSeconds=.1f;match.isClockRunning=true;attacker.highPass=4;
        using var pause=new OnlineSharedPause(protocol,match);
        OnlineAccepted(authority,"home",1,OnlineGroundPassAuthority.LongStartAction);
        OnlineAccepted(authority,"home",2,OnlineGroundPassAuthority.LongTargetAction(6*side,-6));
        OnlineAccepted(authority,"home",3,OnlineGroundPassAuthority.LongTargetAction(6*side,-6));
        RigOnlineLongRoll(1);
        if(interrupted)
        {
            pause.SetConnectionHold(true);var random=Random.state;
            match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds*2);
            Assert.That(match.TimingRollSequence,Is.Zero);Assert.That(Random.state,Is.EqualTo(random));
            pause.SetConnectionHold(false);
        }
        var lateAccuracy=OnlineGroundCommand(authority.Capture(),4,OnlineGroundPassAuthority.LongRollAction);
        match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
        Assert.That(authority.Submit("home",lateAccuracy).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        yield return Until(()=>authority.Capture().stage=="long.direction","automatic inaccurate accuracy continues to direction");
        Assert.That(match.TimingRollSequence,Is.EqualTo(1));
        RigOnlineLongRoll(1);
        var lateDirection=OnlineGroundCommand(authority.Capture(),4,OnlineGroundPassAuthority.LongRollAction);
        match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);
        Assert.That(authority.Capture().legalActions,Does.Not.Contain(OnlineGroundPassAuthority.LongRollAction),
            "automatic resolution must claim the same busy boundary as a manual command");
        Assert.That(authority.Submit("home",lateDirection).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));
        yield return Until(()=>authority.Capture().stage=="long.distance","one automatic direction reaches its next decision");
        Assert.That(match.TimingRollSequence,Is.EqualTo(2));
        Assert.That(match.CaptureOnlineRolls().Select(r=>r.context),Is.EqualTo(new[]{"long_ball_accuracy","long_ball_direction"}));
        Assert.That(match.gameData.stats.GetPlayerStats(attacker).aerialPassesAttempted,Is.EqualTo(1));
    }
}
