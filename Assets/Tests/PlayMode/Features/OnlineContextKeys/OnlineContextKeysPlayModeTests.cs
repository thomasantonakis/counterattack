using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineContextKeys")]
public sealed class OnlineContextKeysPlayModeTests
{
    private object fixture;
    private static Type Type(string name)=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType(name,true);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)fixture.GetType().GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator RealSetPieceKeys([Values(1,-1)]int side,[Values("ground","high","long","shot","corner-high")]string branch)
    {fixture=Activator.CreateInstance(Type("GoalkeeperBoxEntryFixture"));return (IEnumerator)fixture.GetType().GetMethod("RunOnlineSetPiece").Invoke(fixture,new object[]{side,"keys-"+branch});}
    [UnityTest] public IEnumerator RealThrowInKeys([Values(1,-1)]int side,[Values("feet","head","optional")]string branch)
    {fixture=Activator.CreateInstance(Type("GoalkeeperBoxEntryFixture"));return (IEnumerator)fixture.GetType().GetMethod("RunOnlineThrowIn").Invoke(fixture,new object[]{side,"keys-"+branch});}
    [UnityTest] public IEnumerator AuthoredUiConsumesTyping([Values(false,true)]bool guest)
    {fixture=Activator.CreateInstance(Type("OnlineFullRoomSceneFixture"));return (IEnumerator)fixture.GetType().GetMethod("Run").Invoke(fixture,new object[]{guest,16,false,"context-key-focus"});}
}
