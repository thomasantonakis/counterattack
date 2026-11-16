using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineMatchConclusion")]
public sealed class OnlineMatchConclusionPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineFullRoomSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator RegularFullTime([Values(-1,0,1)]int margin,[Values(1,2,4)]int halves,[Values(false,true)]bool leaveAsGuest)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,16,false,$"conclusion:{margin}:{halves}:{leaveAsGuest}"});
}
