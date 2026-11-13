using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineMovement")]
public sealed class OnlineMovementPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator OwnedMovementAndLongPassContinuation([Values(1,-1)]int side,[Values(false,true)]bool afterLongPass)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineMovement").Invoke(fixture,new object[]{side,afterLongPass});
    [UnityTest] public IEnumerator InvalidTargetsAndGuestCannotMoveAuthority()
        =>(IEnumerator)Bridge.GetMethod("RunOnlineMovementInvalidTargets").Invoke(fixture,null);
}

[Category("OnlineMovement")]
public sealed class OnlineRoomMovementPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineRoomActionSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator RealRoomControlsMoveAPlayer([Values(1,-1)]int side)
        =>(IEnumerator)Bridge.GetMethod("RunRoomMovement").Invoke(fixture,new object[]{side});
}
