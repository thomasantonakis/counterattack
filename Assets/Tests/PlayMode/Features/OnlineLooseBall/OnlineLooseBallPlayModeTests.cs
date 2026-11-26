using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineLooseBall")]
public sealed class OnlineLooseBallPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator TiedHeaderContinuesThroughLooseBall([Values(1,-1)]int side,[Values("space","recover","miss","automatic","hold","cancel","replace")]string branch)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineHeaderLooseBall").Invoke(fixture,new object[]{side,branch});
    [UnityTest] public IEnumerator KeeperCatchSpillContinuesThroughLooseBall([Values(1,-1)]int side,[Values(false,true)]bool automatic)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineKeeperLooseBall").Invoke(fixture,new object[]{side,automatic});
}
