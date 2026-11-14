using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineGroundDuels")]
public sealed class OnlineGroundDuelPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    {if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator GroundDuelContinues([Values(1,-1)]int side,[Values("nutmeg","tackle")]string kind,[Values("attacker","defender","tie","automatic","hold","cancel","stay","defender-stay")]string branch)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineGroundDuel").Invoke(fixture,new object[]{side,kind,branch});
    [UnityTest] public IEnumerator MovedDefenderMayStand([Values(1,-1)]int side)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineGroundDuel").Invoke(fixture,new object[]{side,"tackle","stand"});
    [UnityTest] public IEnumerator NutmegVictimSelectionKeepsOtherSteals([Values(1,-1)]int side)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineGroundDuel").Invoke(fixture,new object[]{side,"nutmeg","select"});
}
