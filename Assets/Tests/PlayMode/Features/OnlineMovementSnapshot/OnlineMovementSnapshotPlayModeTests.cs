using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineMovementSnapshot")]
public sealed class OnlineMovementSnapshotPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator OfferedSnapshot([Values(1,-1)]int side,[Values("running","exhausted","away-running","away-exhausted")]string context,[Values("shoot","decline","hold","cancel")]string choice)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineMovementSnapshot").Invoke(fixture,new object[]{side,context,choice});
    [UnityTest] public IEnumerator RunningPlayerMayContinue([Values(1,-1)]int side,[Values("running","away-running")]string context)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineMovementSnapshot").Invoke(fixture,new object[]{side,context,"continue"});
    [UnityTest] public IEnumerator SnapshotResolvesThroughKeeperHold([Values(1,-1)]int side,[Values("running","exhausted","away-running","away-exhausted")]string context)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineMovementSnapshot").Invoke(fixture,new object[]{side,context,"resolve"});
}
