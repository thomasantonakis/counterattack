using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineGoals")]
public sealed class OnlineGoalPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator GoalPublishesResetAndConcedingRestart([Values(1,-1)]int side)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineGoalReset").Invoke(fixture,new object[]{side});
    [UnityTest] public IEnumerator SetupMovementOwnsTeamAndLegalHalf([Values(1,-1)]int side,[Values(false,true)]bool postGoal)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineKickoffSetup").Invoke(fixture,new object[]{side,postGoal});
}
