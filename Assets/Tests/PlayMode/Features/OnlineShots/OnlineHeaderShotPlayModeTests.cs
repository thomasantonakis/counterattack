using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineShots")]
public sealed class OnlineHeaderShotPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator HeadedShotContinuesThroughKeeper([Values(1,-1)]int side,[Values("hold","spill","tie","cancel","pause","automatic","cancel-handling","pause-handling")]string branch)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineHeaderShot").Invoke(fixture,new object[]{side,branch});
}
