using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineSubstitutions")]
public sealed class OnlineSubstitutionsPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineFullRoomSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator OwnPausedTeamSubstitutesOnceAndReplicates([Values(false,true)]bool home,[Values(16,18)]int size,[Values(false,true)]bool mirror)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,size,mirror,"substitution-"+(home?"home":"away")});
    [UnityTest] public IEnumerator AuthoredSubstitutionControlsRouteOwnTeam([Values(false,true)]bool home,[Values(false,true)]bool mirror)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,16,mirror,"substitution-ui-"+(home?"home":"away")});
    [UnityTest] public IEnumerator AuthoredGoalkeeperSwapPreservesRole([Values(false,true)]bool home,[Values(false,true)]bool mirror)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,16,mirror,"substitution-ui-keeper-"+(home?"home":"away")});
    [UnityTest] public IEnumerator ExtraTimeAllowanceProjectsWithoutGuessing([Values(false,true)]bool home)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,18,false,"substitution-limit-"+(home?"home":"away")});
}
