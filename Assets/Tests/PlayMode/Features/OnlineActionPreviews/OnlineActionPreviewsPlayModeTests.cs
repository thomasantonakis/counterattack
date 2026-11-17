using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineActionPreviews")]
public sealed class OnlineActionPreviewsPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineFullRoomSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator GroundPassPreview([Values(16,18)]int size,[Values(false,true)]bool mirror,[Values(false,true)]bool risk)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,size,mirror,"action-preview:"+(risk?"risk":"safe")});
    [UnityTest] public IEnumerator ShotPreview([Values(false,true)]bool mirror)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,18,mirror,"action-preview:shot"});
    [UnityTest] public IEnumerator MediumTargetDoesNotInventRisk([Values(false,true)]bool mirror)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,18,mirror,"action-preview:medium"});
    [UnityTest] public IEnumerator RaisedGoalPreviewClearsOnHolds([Values(false,true)]bool mirror)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,18,mirror,"action-preview:shot-hold"});
    [UnityTest] public IEnumerator DisabledAuthorityBallClearsGoalMarkerOnMotion([Values(false,true)]bool mirror)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,18,mirror,"action-preview:shot-expiry"});
}
