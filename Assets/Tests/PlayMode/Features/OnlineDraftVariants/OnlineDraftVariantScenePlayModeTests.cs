using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineDraftVariantScene")]
public sealed class OnlineDraftVariantScenePlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineDraftSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator AuthoredPoolSelection([Values("Free Regular","Arcade","FreeGK","International","FullMixed")]string mode,[Values(false,true)]bool localHome)
        =>(IEnumerator)Bridge.GetMethod("RunVariant").Invoke(fixture,new object[]{mode,localHome});
}
