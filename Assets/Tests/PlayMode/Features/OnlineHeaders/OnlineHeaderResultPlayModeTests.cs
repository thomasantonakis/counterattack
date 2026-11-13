using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineHeaders")]
public sealed class OnlineHeaderResultPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator HeaderResultPreservesAuthorityHolds([Values(1,-1)]int side,[Values(false,true)]bool enabledOwner)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineHeaderResultHold").Invoke(fixture,new object[]{side,enabledOwner,false});
    [UnityTest] public IEnumerator CancelledHeldHeaderCannotPublishLateOutcome([Values(1,-1)]int side)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineHeaderResultHold").Invoke(fixture,new object[]{side,false,true});
    [UnityTest] public IEnumerator ContestedHeaderCompletesHeadedPass([Values(1,-1)]int side,[Values(false,true)]bool defenseWins,[Values(false,true)]bool automatic)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineHeaderCompletion").Invoke(fixture,new object[]{side,defenseWins,automatic});
    [UnityTest] public IEnumerator HeaderNominationsAndFreeHeader([Values(1,-1)]int side,[Values(false,true)]bool unchallenged)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineHeaderNominations").Invoke(fixture,new object[]{side,unchallenged});
}
