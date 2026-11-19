using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlinePickupNutmeg")]
public sealed class OnlinePickupNutmegPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator PickupDribbleUsesAuthenticatedNutmegKeys([Values(1,-1)]int side,[Values(false,true)]bool home,[Values(false,true)]bool attempt)
        =>(IEnumerator)Bridge.GetMethod("RunOnlinePickupNutmeg").Invoke(fixture,new object[]{side,home,attempt});
}
