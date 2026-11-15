using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineFullRoom")]
public sealed class OnlineFullRoomPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineFullRoomSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator ActualFullRoom([Values(false,true)]bool guest,[Values(16,18)]int size,[Values(false,true)]bool mirror)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{guest,size,mirror,"initial"});
    [UnityTest] public IEnumerator AuthoredKickoff([Values(16,18)]int size,[Values(false,true)]bool mirror)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,size,mirror,"kickoff"});
    [UnityTest] public IEnumerator FullGuestProjection([Values(16,18)]int size,[Values(false,true)]bool mirror)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,size,mirror,"replica"});
}
