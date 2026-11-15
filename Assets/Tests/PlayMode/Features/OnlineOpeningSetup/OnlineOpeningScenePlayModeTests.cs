using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineOpeningScene")]
public sealed class OnlineOpeningScenePlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineDraftSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator AuthoredOpeningChoices([Values(false,true)]bool localHome,[Values(false,true)]bool kickoff)
        =>(IEnumerator)Bridge.GetMethod("RunOpening").Invoke(fixture,new object[]{localHome,kickoff});
}
