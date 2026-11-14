using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineGoalkeeper")]
public sealed class OnlineGoalkeeperPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator BoxEntryOwnsFreeMove([Values(1,-1)]int side,[Values(false,true)]bool move)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineKeeperFreeMove").Invoke(fixture,new object[]{side,move});
    [UnityTest] public IEnumerator HeadedFeetContinuationOwnsPickup([Values(1,-1)]int side,[Values("hands","feet","interrupt")]string branch)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineKeeperPickup").Invoke(fixture,new object[]{side,branch});
}
