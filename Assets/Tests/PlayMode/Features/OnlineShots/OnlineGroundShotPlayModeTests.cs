using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineShots")]
public sealed class OnlineGroundShotPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator ShotAndSnapshotContinueThroughSave([Values(1,-1)]int side,[Values(false,true)]bool snapshot,[Values(false,true)]bool automatic)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineGroundShot").Invoke(fixture,new object[]{side,snapshot,automatic});
    [UnityTest] public IEnumerator CompletedPassOffersSnapshotAndMovement([Values(1,-1)]int side)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineShotAfterPass").Invoke(fixture,new object[]{side});
    [UnityTest] public IEnumerator ChangedContextInvalidatesShotPreview([Values(1,-1)]int side,[Values(false,true)]bool finalThird)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineShotPreviewInterruption").Invoke(fixture,new object[]{side,finalThird});
}
