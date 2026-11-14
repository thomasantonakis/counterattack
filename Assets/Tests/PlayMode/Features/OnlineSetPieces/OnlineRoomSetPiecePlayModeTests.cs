using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineSetPieces")]
public sealed class OnlineRoomSetPiecePlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineRoomActionSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator RoomFreeKickCompletesPass([Values(1,-1)]int side)
        =>(IEnumerator)Bridge.GetMethod("RunRoomSetPiece").Invoke(fixture,new object[]{side});
    [UnityTest] public IEnumerator RoomFreeKickRetiresCommittedRestart([Values(1,-1)]int side)
        =>(IEnumerator)Bridge.GetMethod("RunRoomSetPiece").Invoke(fixture,new object[]{side});
}
