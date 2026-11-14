using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineFirstTimePass")]
public sealed class OnlineRoomFirstTimePassPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineRoomActionSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator RoomPassContinuesThroughFtp([Values(1,-1)]int side)
        =>(IEnumerator)Bridge.GetMethod("RunRoomFirstTimePass").Invoke(fixture,new object[]{side});
}
