using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineOutsideKeeperShot")]
public sealed class OnlineOutsideKeeperShotPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator ActualOutsideKeeperChoice([Values(1,-1)]int side,[Values("hands","block","eligible","handball","hold","cancel")]string choice)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineOutsideKeeperShot").Invoke(fixture,new object[]{side,choice});
}
