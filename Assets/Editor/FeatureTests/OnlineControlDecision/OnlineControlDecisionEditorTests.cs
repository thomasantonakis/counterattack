using System;
using CounterAttack.Online;
using NUnit.Framework;
[Category("OnlineControlDecision")]
public sealed class OnlineControlDecisionEditorTests
{
    [TestCase(false)][TestCase(true)]
    public void CommandRetainsDisplayedBoundaryAfterPublisherChanges(bool home)
    {
        var source=new OnlineGroundPassState{matchId="original",decisionId="selected",revision=17,owner=home?"Home":"Away",legalActions=new[]{"ground.target:2:-1"},homeAcceptedSequence=5,awayAcceptedSequence=8,homePauseSequence=10,awayPauseSequence=12,sharedPauseRevision=4};
        var decision=OnlineControlDecision.Capture(source);source.matchId="changed";source.decisionId="changed";source.revision++;source.legalActions[0]="wrong";
        var command=decision.Command((home?decision.homeAcceptedSequence:decision.awayAcceptedSequence)+1,decision.legalActions[0]);
        Assert.That(command.matchId,Is.EqualTo("original"));Assert.That(command.decisionId,Is.EqualTo("selected"));Assert.That(command.revision,Is.EqualTo(17));
        Assert.That(command.sequence,Is.EqualTo(home?6:9));Assert.That(command.actionId,Is.EqualTo("ground.target:2:-1"));
        Assert.That(decision.sharedPauseRevision,Is.EqualTo(4));Assert.That(home?decision.homePauseSequence:decision.awayPauseSequence,Is.EqualTo(home?10:12));
    }
    [TestCase("connection")][TestCase("pause")][TestCase("required")][TestCase("forced")][TestCase("emergency")][TestCase("shootout")]
    public void PublishedGatesRemainDetached(string branch)
    {
        var source=new OnlineGroundPassState{connectionHeld=branch=="connection",isSharedPaused=branch=="pause",pauseRequester="Away",keeperActionBlocked=branch is "required" or "forced" or "emergency",homeKeeperReplacement=branch=="required",awayForcedKeeperSubstitution=branch=="forced",homeEmergencyKeeper=branch=="emergency",substitutionsAvailable=true,shootout=branch=="shootout"?new OnlineShootoutSnapshot{selecting=true}:null};
        var decision=OnlineControlDecision.Capture(source);source.connectionHeld=source.isSharedPaused=source.keeperActionBlocked=source.homeKeeperReplacement=source.awayForcedKeeperSubstitution=source.homeEmergencyKeeper=false;
        source.pauseRequester="Home";source.substitutionsAvailable=false;if(source.shootout!=null)source.shootout.selecting=false;
        Assert.That(decision.connectionHeld,Is.EqualTo(branch=="connection"));Assert.That(decision.isSharedPaused,Is.EqualTo(branch=="pause"));
        Assert.That(decision.keeperActionBlocked,Is.EqualTo(branch is "required" or "forced" or "emergency"));
        Assert.That(decision.homeKeeperReplacement,Is.EqualTo(branch=="required"));Assert.That(decision.awayForcedKeeperSubstitution,Is.EqualTo(branch=="forced"));Assert.That(decision.homeEmergencyKeeper,Is.EqualTo(branch=="emergency"));
        Assert.That(decision.shootoutSelecting,Is.EqualTo(branch=="shootout"));Assert.That(decision.pauseRequester,Is.EqualTo("Away"));Assert.That(decision.substitutionsAvailable,Is.True);
    }
    [Test] public void AbsentPublicationAndChoicesAreSafe()
    {
        Assert.That(OnlineControlDecision.Capture(null),Is.Null);
        var decision=OnlineControlDecision.Capture(new OnlineGroundPassState());Assert.That(decision.legalActions,Is.Empty);Assert.That(decision.shootoutSelecting,Is.False);
    }
    [Test] public void CaptureAllocationDoesNotScaleWithRosterPayload()
    {
        var empty=new OnlineGroundPassState{legalActions=new[]{"ground.start","long.start"}};
        var large=new OnlineGroundPassState{legalActions=empty.legalActions,tokens=new OnlineGroundPassTokenState[10000],homeStats=new MatchManager.TeamStats(),awayStats=new MatchManager.TeamStats()};
        for(int i=0;i<large.tokens.Length;i++)large.tokens[i]=new OnlineGroundPassTokenState{id="roster-payload-"+i,passesAttempted=i};
        // Keep both groups rooted in the same scope: collecting a previous group's
        // objects during the second sample would produce a false negative delta.
        var first=new OnlineControlDecision[4096];var second=new OnlineControlDecision[4096];
        for(int i=0;i<8;i++)GC.KeepAlive(OnlineControlDecision.Capture(empty));
        GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
        long before=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
        for(int i=0;i<first.Length;i++)first[i]=OnlineControlDecision.Capture(empty);
        long middle=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
        for(int i=0;i<second.Length;i++)second[i]=OnlineControlDecision.Capture(large);
        long after=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
        GC.KeepAlive(first);GC.KeepAlive(second);GC.KeepAlive(large);
        long baseline=middle-before,full=after-middle;
        TestContext.WriteLine($"4,096 simultaneously retained captures: empty publication {baseline} bytes; 10,000-actor publication {full} bytes");
        Assert.That(baseline,Is.GreaterThan(0),"The heap measurement must observe retained allocations");
        Assert.That(full,Is.GreaterThan(0),"A zero counter is not allocation evidence");
        // Mono reports heap changes in allocator blocks (512 KiB on this Editor),
        // rather than exact object bytes. Permit one block, retaining the absolute budget.
        Assert.That(full,Is.LessThanOrEqualTo(baseline+524288),"Input capture cost must depend on choices, not the size of the world publication");
        Assert.That(full,Is.LessThan(4194304),"Four thousand retained captures must not serialize the full match");
    }
}
