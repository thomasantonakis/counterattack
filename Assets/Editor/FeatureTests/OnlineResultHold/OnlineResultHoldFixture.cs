using System.Collections;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOfflineResultCountdown()
    {
        Board(1);AttachAuthoredPanels();EnableForAuthoredPresentation(longBall);
        duelPanel.resultSeconds=.2f;
        Assert.That(duelPanel.BeginSingle(longBall,()=>true,"Offline accuracy",attacker,"High Pass","",true),Is.True);
        bool continued=false;
        IEnumerator Continue()
        { yield return duelPanel.ResolveSingle(longBall,new GameplayDiceRollResult{roll=4},"Accurate");continued=true; }
        Time.timeScale=0;duelPanel.StartCoroutine(Continue());
        yield return Until(()=>continued,"offline unscaled countdown remains available");
        Assert.That(duelPanel.IsOpen,Is.False);
    }

    public IEnumerator RunOnlineDisabledOwner()
    {
        Board(1);AttachAuthoredPanels();
        var version=new OnlineCompatibility("result-test","rules","catalog");
        var protocol=new OnlineMatchProtocol("result-test","home","away","away",version);
        using var pause=new OnlineSharedPause(protocol,match);
        Assert.That(longBall.enabled,Is.False);
        Assert.That(duelPanel.BeginSingle(longBall,()=>true,"Online accuracy",attacker,"High Pass","",true),Is.True);
        yield return null;yield return null;
        Assert.That(duelPanel.IsOpen && duelPanel.IsAwaitingRoll,Is.True,"controlled authority managers suppress local input loops");
        duelPanel.CancelSingle(longBall);
        Assert.That(duelPanel.IsOpen,Is.False);
    }

    public IEnumerator RunOnlineResultHold(int side, bool manuallyHeld)
    {
        Board(side); AttachAuthoredPanels(); EnableForAuthoredPresentation(longBall);
        duelPanel.resultSeconds=1f;
        var version=new OnlineCompatibility("result-test","rules","catalog");
        var protocol=new OnlineMatchProtocol("result-test","home","away","away",version);
        protocol.BindAuthenticatedAccount("home","home",version);protocol.BindAuthenticatedAccount("away","away",version);
        using var pause=new OnlineSharedPause(protocol,match);
        bool waiting=true,continued=false;
        Assert.That(duelPanel.BeginSingle(longBall,()=>waiting,"Long pass accuracy",attacker,"High Pass","",true),Is.True);
        var result=match.ResolveGameplayDiceRoll("long_ball_accuracy",new RollInputOverride{hasOverride=true,roll=4},actor:attacker);
        waiting=false;
        IEnumerator Continue()
        { yield return duelPanel.ResolveSingle(longBall,result,"Accurate");continued=true; }
        duelPanel.StartCoroutine(Continue());
        yield return Until(()=>duelPanel.IsComplete,"real result countdown starts");
        if(manuallyHeld) { duelPanel.ToggleResultHold(); Assert.That(duelPanel.IsResultHeld,Is.True); }
        pause.SetConnectionHold(true);
        Assert.That(pause.Submit("home",new OnlineMatchCommand("result-test",1,0,"shared-pause",OnlineSharedPause.RequestAction)).Status,
            Is.EqualTo(OnlineCommandStatus.Accepted));
        yield return new WaitForSecondsRealtime(1.2f);
        Assert.That(duelPanel.IsOpen,Is.True,"unscaled result timer must not release a connection hold");
        Assert.That(continued,Is.False);Assert.That(duelPanel.Dismiss(),Is.False,"local close cannot bypass authority hold");
        var before=duelPanel.IsResultHeld;
        DuelResolutionPanelController.ConsumeKey(new KeyPressData(KeyCode.Return,false,false,false));
        Assert.That(duelPanel.IsResultHeld,Is.EqualTo(before),"personal keys cannot change an online result");
        pause.SetConnectionHold(false);
        yield return new WaitForSecondsRealtime(.25f);
        Assert.That(duelPanel.IsOpen,Is.True,"shared pause remains after connection recovery");
        Assert.That(pause.Submit("home",new OnlineMatchCommand("result-test",2,1,"shared-pause",OnlineSharedPause.ResumeAction)).Status,
            Is.EqualTo(OnlineCommandStatus.Accepted));
        if(manuallyHeld)
        {
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(duelPanel.IsOpen && duelPanel.IsResultHeld,Is.True,"existing result hold survives transport hold");
            duelPanel.ToggleResultHold();
        }
        Assert.That(duelPanel.IsOpen,Is.True,"countdown resumes with remaining time");
        yield return Until(()=>continued,"authority releases exactly one continuation");
        Assert.That(match.TimingRollSequence,Is.EqualTo(1));Assert.That(match.CaptureOnlineRolls().Length,Is.EqualTo(1));
    }
}
