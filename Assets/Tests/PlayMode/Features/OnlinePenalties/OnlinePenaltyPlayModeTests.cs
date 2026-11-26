using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlinePenalties")]
public sealed class OnlinePenaltyPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator PenaltyContinues([Values(1,-1)]int side,[Values("goal","save","automatic","hold","defence-clearance","attack-clearance")]string branch)
        =>(IEnumerator)Bridge.GetMethod("RunOnlinePenalty").Invoke(fixture,new object[]{side,branch});
}
