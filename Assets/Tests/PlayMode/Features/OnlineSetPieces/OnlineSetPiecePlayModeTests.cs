using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineSetPieces")]
public sealed class OnlineSetPiecePlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator SetPieceContinues([Values(1,-1)]int side,[Values("ground","high","long","indirect","corner-ground","corner-high","move","clearance","shot","hold")]string branch)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineSetPiece").Invoke(fixture,new object[]{side,branch});
}
