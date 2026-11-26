using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineFrontendEntry")]
public sealed class OnlineFrontendEntryPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineFrontendEntrySceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator NormalMenuEntry([Values("entry","offline","invalid-join","save-guard","persistent-lifetime","back")]string branch)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{branch});
    [UnityTest] public IEnumerator HostSetupNavigation([Values("host-early-setup","host-auto-setup")]string branch)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{branch});
}
