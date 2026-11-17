using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineOffTargetCamera")]
public sealed class OnlineOffTargetCameraPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineFullRoomSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator RealCameraMiss([Values(false,true)]bool mirror,
        [Values("flight","pause-flight","pause-hold","hold-flight","hold-hold","reset-flight","reset-hold")]string branch)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,18,mirror,"action-preview:shot-camera-"+branch});
    [UnityTest] public IEnumerator RealSnapshotCameraMiss([Values(false,true)]bool mirror,[Values("flight","reset-flight","hold-hold")]string branch)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,18,mirror,"action-preview:snapshot-camera-"+branch});
}
