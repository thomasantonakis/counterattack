using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineRoomMenu")]
public sealed class OnlineRoomMenuPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineFullRoomSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator AuthoredMenuRespectsPauseOwnership([Values(false,true)]bool guest,[Values(false,true)]bool own)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{guest,16,own,"menu"});
    [UnityTest] public IEnumerator LeaveReturnsToMenuWithoutWritingMatch([Values(false,true)]bool guest)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{guest,16,false,"menu-leave"});
}
