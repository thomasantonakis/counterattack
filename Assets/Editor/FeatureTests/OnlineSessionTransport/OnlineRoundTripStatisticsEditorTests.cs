using CounterAttack.Online;
using NUnit.Framework;
[Category("OnlineSessionTransport")]
public sealed class OnlineRoundTripStatisticsEditorTests
{
    [Test] public void MeasuresBoundedMonotonicRoundTrips([Values("one-clock","duplicate","unknown","negative","pending-bound","samples-bound","percentiles")]string branch)
    {
        double now=0;var stats=new OnlineRoundTripStatistics(()=>now);
        if(branch=="unknown"){Assert.That(stats.Replied(1),Is.False);Assert.That(stats.Capture().samples,Is.Zero);return;}
        if(branch=="pending-bound")
        {for(int i=1;i<=65;i++)stats.Sent(i);now=.2;Assert.That(stats.Replied(1),Is.False);Assert.That(stats.Replied(65),Is.True);Assert.That(stats.Capture().samples,Is.EqualTo(1));return;}
        if(branch=="samples-bound")
        {for(int i=1;i<=200;i++){now=i;stats.Sent(i);now+=.1;stats.Replied(i);}Assert.That(stats.Capture().samples,Is.EqualTo(128));Assert.That(stats.Capture().p95Milliseconds,Is.EqualTo(100).Within(.001));return;}
        stats.Sent(1);
        if(branch=="negative"){now=-1;Assert.That(stats.Replied(1),Is.False);Assert.That(stats.Capture().samples,Is.Zero);return;}
        now=.125;Assert.That(stats.Replied(1),Is.True);
        if(branch=="duplicate"){Assert.That(stats.Replied(1),Is.False);Assert.That(stats.Capture().samples,Is.EqualTo(1));return;}
        if(branch=="percentiles")
        {now=1;stats.Sent(2);now=1.2;stats.Replied(2);Assert.That(stats.Capture().p50Milliseconds,Is.EqualTo(162.5).Within(.001));Assert.That(stats.Capture().p95Milliseconds,Is.EqualTo(196.25).Within(.001));return;}
        Assert.That(stats.Capture().lastMilliseconds,Is.EqualTo(125).Within(.001));
    }
}
