using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineFinalThird")]
public sealed class OnlineFinalThirdPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator LongPassFinalThirdOwnershipAndContinuation([Values(1,-1)]int side,[Values(false,true)]bool move)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineFinalThird").Invoke(fixture,new object[]{side,move});
}

[Category("OnlineFinalThird")]
public sealed class OnlineRoomFinalThirdPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineRoomActionSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator RealRoomFinalThirdReturnsToMovement([Values(1,-1)]int side)
        =>(IEnumerator)Bridge.GetMethod("RunRoomFinalThird").Invoke(fixture,new object[]{side});
}
