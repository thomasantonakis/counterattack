using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineResultHold")]
public sealed class OnlineResultHoldPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator ResultContinuationSurvivesConnectionAndSharedHolds([Values(1,-1)]int side,[Values(false,true)]bool manuallyHeld)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineResultHold").Invoke(fixture,new object[]{side,manuallyHeld});
    [UnityTest] public IEnumerator OfflineCountdownStillUsesUnscaledTime()
        =>(IEnumerator)Bridge.GetMethod("RunOfflineResultCountdown").Invoke(fixture,null);
    [UnityTest] public IEnumerator ControlledDisabledOwnerRetainsItsRollPrompt()
        =>(IEnumerator)Bridge.GetMethod("RunOnlineDisabledOwner").Invoke(fixture,null);
}
