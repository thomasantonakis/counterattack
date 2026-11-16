using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineLooseSnapshotChoice")]
public sealed class OnlineLooseSnapshotChoicePlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator CollectedLooseBallSnapshot([Values(1,-1)]int side,[Values("shoot","decline","hold","cancel")]string choice)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineLooseSnapshotChoice").Invoke(fixture,new object[]{side,choice});
}
