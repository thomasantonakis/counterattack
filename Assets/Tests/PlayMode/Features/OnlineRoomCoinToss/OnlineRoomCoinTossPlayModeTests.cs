using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineRoomCoinToss")]
public sealed class OnlineRoomCoinTossPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineFullRoomSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator LegacyChoiceAndRoomHoldCannotBypassAuthority([Values(false,true)]bool penalties)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,16,false,$"room-coin:{penalties}:True:False:True:False:True"});
    [UnityTest] public IEnumerator OrdinaryHalfTransitions([Values(1,3)]int half,[Values(false,true)]bool mirror)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,16,mirror,$"ordinary-half:{half}"});
    [UnityTest] public IEnumerator RealRoomTossTransitions([Values(false,true)]bool penalties,[Values(false,true)]bool winnerHome,[Values(false,true)]bool takeFirst,[Values(false,true)]bool chooseRight)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,16,false,$"room-coin:{penalties}:{winnerHome}:{takeFirst}:{chooseRight}:False"});
    [UnityTest] public IEnumerator RetieWaitsAcrossOverlappingHolds([Values(false,true)]bool penalties)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,16,true,$"room-coin:{penalties}:True:True:False:True"});
}
