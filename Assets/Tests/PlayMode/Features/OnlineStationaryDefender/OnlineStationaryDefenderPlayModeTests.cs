using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineStationaryDefender")]
public sealed class OnlineStationaryDefenderPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator OutfielderCanTackleOrMove([Values(1,-1)]int side,[Values(false,true)]bool home,[Values(false,true)]bool move)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineStationaryDefender").Invoke(fixture,new object[]{side,home,false,move});
    [UnityTest] public IEnumerator KeeperCanMoveInsteadOfChallenge([Values(1,-1)]int side,[Values(false,true)]bool home)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineStationaryDefender").Invoke(fixture,new object[]{side,home,true,true});
    [UnityTest] public IEnumerator DistantDefenderCannotTackleInPlace([Values(1,-1)]int side,[Values(false,true)]bool home)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineDistantDefender").Invoke(fixture,new object[]{side,home});
}
