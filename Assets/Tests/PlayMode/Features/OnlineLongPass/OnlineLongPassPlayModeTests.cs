using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineLongPass")]
public sealed class OnlineLongPassPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator LongPassAuthorityAndReplica([Values(1,-1)]int side,[Values(false,true)]bool accurate)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineLongPass").Invoke(fixture,new object[]{side,accurate});
    [UnityTest] public IEnumerator TargetOccupancyInvalidatesOldCommandsAndGuestCannotExecute()
        =>(IEnumerator)Bridge.GetMethod("RunOnlineLongTargetInvalidation").Invoke(fixture,null);
    [UnityTest] public IEnumerator KeeperResponseAndRecoveryCompleteOnce([Values(1,-1)]int side,[Values("move","claim","intercept","failed")]string response)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineLongKeeperResponse").Invoke(fixture,new object[]{side,response});
}

[Category("OnlineLongPass")]
public sealed class OnlineActionControlsPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineActionControlsSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator AuthoredControlsSubmitOnlyOwnedSemanticActions()
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,null);
}

[Category("OnlineLongPass")]
public sealed class OnlineRoomLongPassPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineRoomActionSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator RealRoomManagersExecuteLongPass([Values(1,-1)]int side)
        =>(IEnumerator)Bridge.GetMethod("RunRoomLongPass").Invoke(fixture,new object[]{side});
}
