using System.Collections;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunContinuousPassSequence(int side)
    {
        yield return RunOnlineFirstTimePass(side,"safe");
        var version=OnlineGroundVersion();
        var protocol=new OnlineMatchProtocol("continuous","home","away","away",version);
        protocol.BindAuthenticatedAccount("home","home",version);protocol.BindAuthenticatedAccount("away","away",version);
        var state=new OnlineGroundPassAuthority(match,protocol).Capture();
        Assert.That(match.gameData.stats.playerStats.Sum(p=>p.stats.passesCompleted),Is.EqualTo(2));
        var board=Add<OnlineGroundPassBoard>();board.continuousManualSession=true;
        var type=typeof(OnlineGroundPassBoard).Assembly.GetType("CounterAttack.Online.EOSGroundPassExchange",true);
        var constructor=type.GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic).Single(c=>c.GetParameters().Length==2);
        var exchange=constructor.Invoke(new object[]{board,"unused"});
        var complete=type.GetMethod("CompletedManualAction",BindingFlags.Instance|BindingFlags.NonPublic);
        Assert.That((bool)complete.Invoke(exchange,new object[]{state}),Is.False,"actual exchange keeps the completed ground pass and FTP session open");
        Assert.That((bool)complete.Invoke(exchange,new object[]{onlineFtpPrecedingPass}),Is.False,"continuous session also stays open at the original first-pass boundary");
        board.continuousManualSession=false;
        Assert.That((bool)complete.Invoke(exchange,new object[]{onlineFtpPrecedingPass}),Is.True,"default proof ends at the original first-pass boundary, before FTP");
    }
}
