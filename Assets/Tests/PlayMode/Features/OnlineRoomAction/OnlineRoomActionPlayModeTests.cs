using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineRoomAction")]
public sealed class OnlineRoomActionPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineRoomActionSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    { if(fixture!=null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null); fixture=null; }
    [UnityTest] public IEnumerator RoomPassOutcomeAndReplica([Values(1,-1)] int side,[Values("safe","intercept","failed")] string branch)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{side,branch});
}
