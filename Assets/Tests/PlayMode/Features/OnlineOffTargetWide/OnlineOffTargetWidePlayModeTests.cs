using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineOffTargetWide")]
public sealed class OnlineOffTargetWidePlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineFullRoomSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator RealWideMiss([Values(false,true)]bool mirror,
        [Values("flight","reset-flight","replace-flight")]string branch)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,18,mirror,"action-preview:shot-wide-"+branch});
    [UnityTest] public IEnumerator RealSnapshotWideMiss([Values(false,true)]bool mirror,[Values("flight","reset-flight","replace-flight")]string branch)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,18,mirror,"action-preview:snapshot-wide-"+branch});
}
