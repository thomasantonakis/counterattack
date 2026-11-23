using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineDraftScene")]
public sealed class OnlineDraftScenePlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineDraftSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator AuthoredDraft([Values(false,true)]bool hostAway,[Values(false,true)]bool localHost,[Values("pick","ownership","swap","recovery","complete")]string branch)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{hostAway,localHost,branch});
    [UnityTest] public IEnumerator RemainingCardsStayInTheirAuthoredPositions()
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,true,"fixed-offer-positions"});
}
