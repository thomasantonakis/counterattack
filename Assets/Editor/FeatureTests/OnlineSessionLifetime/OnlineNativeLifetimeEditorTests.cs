using System;
using System.Collections.Generic;
using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineAuthRenewal")]
public sealed class OnlineNativeLifetimeEditorTests
{
    [Test] public void SubscriptionsRetireBeforeNativeReleaseAndLateCleanupIsSafe()
    {
        bool alive=true;int removed=0,released=0;
        var lifetime=new OnlineNativeSessionLifetime(()=>{alive=false;released++;});
        var subscription=lifetime.Own(()=>{Assert.That(alive,Is.True,"A native RemoveNotify must never run after platform release");removed++;});
        lifetime.Dispose();subscription.Dispose();lifetime.Dispose();
        Assert.That(removed,Is.EqualTo(1));Assert.That(released,Is.EqualTo(1));Assert.That(alive,Is.False);
    }
    [Test] public void EarlyCallerDisposalRemovesLeaseWithoutDoubleNativeCall()
    {
        int removed=0;using var lifetime=new OnlineNativeSessionLifetime(()=>{});var subscription=lifetime.Own(()=>removed++);
        Assert.That(lifetime.OwnedCount,Is.EqualTo(1));subscription.Dispose();Assert.That(lifetime.OwnedCount,Is.Zero);
        lifetime.Dispose();Assert.That(removed,Is.EqualTo(1));
    }
    [Test] public void SubscriptionsRetireInReverseRegistrationOrder()
    {
        var events=new List<string>();var lifetime=new OnlineNativeSessionLifetime(()=>events.Add("platform"));
        lifetime.Own(()=>events.Add("auth"));lifetime.Own(()=>events.Add("p2p"));lifetime.Dispose();
        Assert.That(events,Is.EqualTo(new[]{"p2p","auth","platform"}));
    }
    [Test] public void ThrowingCleanupStillReleasesOtherHandlesBeforePlatform()
    {
        var events=new List<string>();var lifetime=new OnlineNativeSessionLifetime(()=>events.Add("platform"));
        lifetime.Own(()=>events.Add("first"));lifetime.Own(()=>{events.Add("second");throw new InvalidOperationException("remove failed");});
        Assert.Throws<InvalidOperationException>(()=>lifetime.Dispose());lifetime.Dispose();
        Assert.That(events,Is.EqualTo(new[]{"second","first","platform"}));
    }
    [Test] public void ReentrantDisposalCannotReleasePlatformBeforeCallbackCleanup()
    {
        var events=new List<string>();OnlineNativeSessionLifetime lifetime=null;lifetime=new(()=>events.Add("platform"));
        lifetime.Own(()=>{lifetime.Dispose();events.Add("removed");});lifetime.Dispose();
        Assert.That(events,Is.EqualTo(new[]{"removed","platform"}));
    }
    [Test] public void DisposedLifetimeRejectsNewHandleWithoutCallingIt()
    {
        int removed=0;var lifetime=new OnlineNativeSessionLifetime(()=>{});lifetime.Dispose();
        Assert.Throws<ObjectDisposedException>(()=>lifetime.Own(()=>removed++));Assert.That(removed,Is.Zero);
    }
    [Test] public void NullSubscriptionsCannotBecomeOwned()
    {
        using var lifetime=new OnlineNativeSessionLifetime(()=>{});
        Assert.Throws<ArgumentNullException>(()=>lifetime.Own((IDisposable)null));Assert.Throws<ArgumentNullException>(()=>lifetime.Own((Action)null));
        Assert.That(lifetime.OwnedCount,Is.Zero);
    }
    [Test] public void PlatformReleaseFailureCannotRepeatNativeCleanup()
    {
        int removed=0,released=0;var lifetime=new OnlineNativeSessionLifetime(()=>{released++;throw new InvalidOperationException("release failed");});
        lifetime.Own(()=>removed++);Assert.Throws<InvalidOperationException>(()=>lifetime.Dispose());lifetime.Dispose();
        Assert.That(removed,Is.EqualTo(1));Assert.That(released,Is.EqualTo(1));
    }
}
