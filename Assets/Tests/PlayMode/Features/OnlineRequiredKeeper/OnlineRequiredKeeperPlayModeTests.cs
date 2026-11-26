using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineRequiredKeeper")]
public sealed class OnlineRequiredKeeperPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineFullRoomSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    private IEnumerator Run(bool home,string scenario,bool opponent,bool mirror)=>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,16,mirror,$"required-keeper:{home}:{scenario}:{opponent}"});
    [UnityTest] public IEnumerator SentOffKeeperPausesAffectedTeam()=>Run(false,"bench",false,false);
    [UnityTest] public IEnumerator ForcedBenchKeeperReplacement([Values(false,true)]bool home,[Values("bench","double")]string scenario,[Values(false,true)]bool opponent,[Values(false,true)]bool mirror)=>Run(home,scenario,opponent,mirror);
    [UnityTest] public IEnumerator EmergencyKeeperNomination([Values(false,true)]bool home,[Values("no-bench","no-subs")]string scenario,[Values(false,true)]bool opponent,[Values(false,true)]bool mirror)=>Run(home,scenario,opponent,mirror);
}
