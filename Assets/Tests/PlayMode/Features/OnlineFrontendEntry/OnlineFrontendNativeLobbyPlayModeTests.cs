using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineFrontendNativeLobby")]
public sealed class OnlineFrontendNativeLobbyPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineFrontendEntrySceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest,Timeout(120000)] public IEnumerator AuthoredHostCreatesAndCancelsPrivateNativeLobby()
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{"host-cancel"});
}
