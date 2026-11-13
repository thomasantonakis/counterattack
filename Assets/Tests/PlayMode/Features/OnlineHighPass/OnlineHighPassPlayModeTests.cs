using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineHighPass")]
public sealed class OnlineHighPassPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator HighPassLandingAndBallControl([Values(1,-1)]int side,[Values(false,true)]bool scatter)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineHighPass").Invoke(fixture,new object[]{side,scatter});
    [UnityTest] public IEnumerator MandatoryAttackerAndOptionalDefenderPreparation([Values(1,-1)]int side,[Values(false,true)]bool singleEligible)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineHighPreparation").Invoke(fixture,new object[]{side,singleEligible});
    [UnityTest] public IEnumerator CancelledFlightCannotPublishALaterLanding([Values(1,-1)]int side)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineHighCancelledFlight").Invoke(fixture,new object[]{side});
    [UnityTest] public IEnumerator KeeperChallengeUsesOwnedMoveStayOrDecline([Values(1,-1)]int side,[Values("move","stay","decline")]string response)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineHighKeeper").Invoke(fixture,new object[]{side,response});
    [UnityTest] public IEnumerator AutomaticHighPassDeadlineRejectsLateManualRoll([Values(1,-1)]int side)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineHighAutomatic").Invoke(fixture,new object[]{side});
    [UnityTest] public IEnumerator ChangedOccupancyRevokesTargetAndGuestCannotExecute()
        =>(IEnumerator)Bridge.GetMethod("RunOnlineHighInvalidatedTarget").Invoke(fixture,null);
}

[Category("OnlineHighPass")]
public sealed class OnlineRoomHighPassPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineRoomActionSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator RealRoomHighPassAndBallControl([Values(1,-1)]int side)
        =>(IEnumerator)Bridge.GetMethod("RunRoomHighPass").Invoke(fixture,new object[]{side});
}
