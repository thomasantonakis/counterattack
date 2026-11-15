using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineSnapshotPose")]
public sealed class OnlineSnapshotPosePlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineFullRoomSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    private IEnumerator Run(int size,bool mirror)=>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,size,mirror,"snapshot-pose"});
    [UnityTest] public IEnumerator ReconnectDuringFlight()=>Run(16,false);
    [UnityTest] public IEnumerator EncodedSnapshotKeepsMovingPoses([Values(16,18)]int size,[Values(false,true)]bool mirror)=>Run(size,mirror);
}
