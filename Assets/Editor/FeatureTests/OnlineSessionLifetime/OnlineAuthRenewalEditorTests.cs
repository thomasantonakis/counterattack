using System;
using System.Collections.Generic;
using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineAuthRenewal")]
public sealed class OnlineAuthRenewalEditorTests
{
    private double now;
    private OnlineAuthRenewal renewal;
    private readonly List<Action<bool,string,string>> replies=new();
    [SetUp] public void Setup(){now=0;replies.Clear();renewal=new("local-user",()=>now,reply=>replies.Add(reply));}
    [TearDown] public void Cleanup()=>renewal.Dispose();
    [Test] public void ExpiryStartsRefreshOnlyForBoundIdentity()
    {
        Assert.That(renewal.NotifyExpiring("other-user"),Is.False);renewal.Tick();Assert.That(replies,Is.Empty);
        Assert.That(renewal.NotifyExpiring("local-user"),Is.True);Assert.That(replies,Is.Empty);renewal.Tick();
        Assert.That(replies.Count,Is.EqualTo(1));replies[0](true,"local-user","Success");
        Assert.That(renewal.SuccessfulRefreshes,Is.EqualTo(1));Assert.That(renewal.Pending,Is.False);Assert.That(renewal.Fault,Is.Null);
    }
    [Test] public void DuplicateNoticesCannotIssueConcurrentLogins()
    {
        renewal.NotifyExpiring("local-user");Assert.That(renewal.NotifyExpiring("local-user"),Is.False);
        renewal.Tick();renewal.Tick();Assert.That(replies.Count,Is.EqualTo(1));
    }
    [Test] public void FailedLoginRetriesAndPreservesIdentity()
    {
        renewal.NotifyExpiring("local-user");renewal.Tick();replies[0](false,null,"ServiceFailure");
        now=4.99;renewal.Tick();Assert.That(replies.Count,Is.EqualTo(1));now=5;renewal.Tick();Assert.That(replies.Count,Is.EqualTo(2));
        replies[1](true,"local-user","Success");Assert.That(renewal.SuccessfulRefreshes,Is.EqualTo(1));Assert.That(renewal.Fault,Is.Null);
    }
    [Test] public void MissingCallbackRetriesAndLateOldReplyCannotCompleteNewAttempt()
    {
        renewal.NotifyExpiring("local-user");renewal.Tick();now=20;renewal.Tick();now=25;renewal.Tick();
        Assert.That(replies.Count,Is.EqualTo(2));replies[0](true,"local-user","Success");
        Assert.That(renewal.SuccessfulRefreshes,Is.Zero);Assert.That(renewal.Pending,Is.True);
        replies[1](true,"local-user","Success");Assert.That(renewal.SuccessfulRefreshes,Is.EqualTo(1));
    }
    [Test] public void RepeatedFailuresStopBeforeAuthenticationExpiryWindow()
    {
        renewal.NotifyExpiring("local-user");
        for(int i=0;i<10;i++){now=i*5;renewal.Tick();Assert.That(replies.Count,Is.EqualTo(i+1));replies[i](false,null,"ServiceFailure");}
        now=50;renewal.Tick();Assert.That(renewal.Fault,Does.Contain("ServiceFailure"));Assert.That(renewal.Pending,Is.False);
        Assert.That(renewal.NotifyExpiring("local-user"),Is.False);now=100;renewal.Tick();Assert.That(replies.Count,Is.EqualTo(10));
    }
    [Test] public void SuccessfulLoginWithDifferentIdentityIsRejected()
    {
        renewal.NotifyExpiring("local-user");renewal.Tick();replies[0](true,"different-user","Success");
        Assert.That(renewal.Fault,Does.Contain("identity"));Assert.That(renewal.SuccessfulRefreshes,Is.Zero);
    }
    [Test] public void DisposedOwnerIgnoresPendingNativeCallback()
    {
        renewal.NotifyExpiring("local-user");renewal.Tick();renewal.Dispose();replies[0](true,"local-user","Success");
        Assert.That(renewal.SuccessfulRefreshes,Is.Zero);Assert.That(renewal.Pending,Is.False);renewal.Tick();
        Assert.That(renewal.NotifyExpiring("local-user"),Is.False);
    }
    [Test] public void DuplicateReplyCannotOverwriteAcceptedSuccess()
    {
        renewal.NotifyExpiring("local-user");renewal.Tick();replies[0](true,"local-user","Success");replies[0](false,null,"InvalidAuth");
        Assert.That(renewal.SuccessfulRefreshes,Is.EqualTo(1));Assert.That(renewal.Fault,Is.Null);
    }
    [Test] public void LaterExpirationCanRenewSameSessionAgain()
    {
        renewal.NotifyExpiring("local-user");renewal.Tick();replies[0](true,"local-user","Success");
        now=3590;Assert.That(renewal.NotifyExpiring("local-user"),Is.True);renewal.Tick();replies[1](true,"local-user","Success");
        Assert.That(renewal.SuccessfulRefreshes,Is.EqualTo(2));Assert.That(renewal.Fault,Is.Null);
    }
    [Test] public void SynchronousCompletionCannotLeaveRenewalPending()
    {
        using var direct=new OnlineAuthRenewal("local-user",()=>now,reply=>reply(true,"local-user","Success"));
        direct.NotifyExpiring("local-user");direct.Tick();Assert.That(direct.Pending,Is.False);Assert.That(direct.SuccessfulRefreshes,Is.EqualTo(1));
    }
    [Test] public void ThrowingLoginRetriesWithoutPublishingExceptionSecrets()
    {
        int calls=0;using var throwing=new OnlineAuthRenewal("local-user",()=>now,reply=>{calls++;throw new InvalidOperationException("private-token-value");});
        throwing.NotifyExpiring("local-user");Assert.DoesNotThrow(()=>throwing.Tick());now=5;throwing.Tick();Assert.That(calls,Is.EqualTo(2));
        now=50;throwing.Tick();Assert.That(throwing.Fault,Does.Contain("InvalidOperationException"));Assert.That(throwing.Fault,Does.Not.Contain("private-token-value"));
    }
    [Test] public void FailedCompletionDoesNotThrowThroughNativeCallback()
    {
        renewal.NotifyExpiring("local-user");renewal.Tick();Assert.DoesNotThrow(()=>replies[0](false,null,"InvalidAuth"));
        Assert.That(renewal.Fault,Is.Null);now=50;renewal.Tick();Assert.That(renewal.Fault,Does.Contain("InvalidAuth"));
    }
    [Test] public void SuccessBeforeDeadlineSuppressesTimeout()
    {
        renewal.NotifyExpiring("local-user");renewal.Tick();now=49;replies[0](true,"local-user","Success");now=51;renewal.Tick();
        Assert.That(renewal.Fault,Is.Null);Assert.That(renewal.SuccessfulRefreshes,Is.EqualTo(1));
    }
    [TestCase("identity")][TestCase("clock")][TestCase("login")]
    public void InvalidConstructionIsRejected(string kind)
    {
        TestDelegate construct=()=>new OnlineAuthRenewal(kind=="identity"?" ":"local-user",kind=="clock"?null:()=>0,kind=="login"?null:reply=>{});
        if(kind=="identity")Assert.Throws<ArgumentException>(construct);else Assert.Throws<ArgumentNullException>(construct);
    }
}
