using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineFouls")]
public sealed class OnlineFoulPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator FoulContinues([Values(1,-1)]int side,[Values("booking","injury","automatic","hold","cancel","advantage","take","sent-off")]string branch)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineFoul").Invoke(fixture,new object[]{side,branch});
}
