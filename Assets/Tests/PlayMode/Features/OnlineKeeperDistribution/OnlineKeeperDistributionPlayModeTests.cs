using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineKeeperDistribution")]
public sealed class OnlineKeeperDistributionPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator CatchContinuesToDistribution([Values(1,-1)]int side,[Values("quick","drop","kick")]string branch,[Values(false,true)]bool held)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineKeeperDistribution").Invoke(fixture,new object[]{side,branch,held});
    [UnityTest] public IEnumerator AwayKeeperOwnsDistribution([Values(1,-1)]int side)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineAwayKeeperDistribution").Invoke(fixture,new object[]{side});
}
