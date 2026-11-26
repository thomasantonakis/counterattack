using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineFtpPreviewSwitch")]
public sealed class OnlineFtpPreviewSwitchPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator MovementSwitchRespectsPreviewCommitmentAndHold([Values(1,-1)]int side,[Values(false,true)]bool home,[Values("unselected","selected","committed","hold","experienced")]string branch)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineFtpPreviewSwitch").Invoke(fixture,new object[]{side,home,branch});
}
