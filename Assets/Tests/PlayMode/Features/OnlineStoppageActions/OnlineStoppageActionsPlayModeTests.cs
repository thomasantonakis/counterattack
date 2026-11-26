using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineStoppageActions")]
public sealed class OnlineStoppageActionsPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineFullRoomSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator ExpiredHalfRollsAndResumes([Values(false,true)]bool home,[Values(false,true)]bool extra,[Values(1,6)]int face)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,16,!home,$"stoppage:{home}:{extra}:{face}"});
    [UnityTest] public IEnumerator ExpiredHalfAutomaticRoll([Values(false,true)]bool home,[Values(false,true)]bool extra)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,16,!home,$"stoppage:{home}:{extra}:6:auto"});
}
