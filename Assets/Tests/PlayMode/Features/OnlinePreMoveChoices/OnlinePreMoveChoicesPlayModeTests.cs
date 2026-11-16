using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlinePreMoveChoices")]
public sealed class OnlinePreMoveChoicesPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator ExistingPreMoveAlternatives([Values(1,-1)]int side,[Values(false,true)]bool home,[Values("keeper-switch","nutmeg-move","blocked-forfeit","offline-blocked-forfeit","nutmeg-direct")]string choice)
        =>(IEnumerator)Bridge.GetMethod("RunOnlinePreMove").Invoke(fixture,new object[]{side,home,choice});
    [UnityTest] public IEnumerator ThrowInMustMoveRequiredDefenders([Values(1,-1)]int side,[Values(false,true)]bool home)
        =>(IEnumerator)Bridge.GetMethod("RunOnlinePreMove").Invoke(fixture,new object[]{side,home,"throw-in"});
    [UnityTest] public IEnumerator HoldBlocksUncommittedActionSwitch([Values(1,-1)]int side,[Values(false,true)]bool home)
        =>(IEnumerator)Bridge.GetMethod("RunOnlinePreMove").Invoke(fixture,new object[]{side,home,"uncommitted-hold"});
    [UnityTest] public IEnumerator StationaryOutfieldForfeitOfferRemainsUnchanged([Values(1,-1)]int side,[Values(false,true)]bool home)
        =>(IEnumerator)Bridge.GetMethod("RunOnlinePreMove").Invoke(fixture,new object[]{side,home,"existing-outfield-offer"});
}
