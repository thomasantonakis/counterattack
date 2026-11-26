using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineShootout")]
public sealed class OnlineShootoutPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineFullRoomSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator OrderingTracksPausedSubstitution([Values(false,true)]bool home,[Values(false,true)]bool mirror)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,16,mirror,$"shootout:True:False:roster-{(home?"home":"away")}"});
    [UnityTest] public IEnumerator CompleteShootout([Values(false,true)]bool winnerHome,[Values(false,true)]bool sudden,[Values(false,true)]bool mirror)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,16,mirror,$"shootout:{!winnerHome}:False:{(sudden?"sudden":"finish")}-{(winnerHome?"home":"away")}"});
    [UnityTest] public IEnumerator FirstKickOutcomeContinues([Values(false,true)]bool homeFirst,[Values("goal","offtarget","save","tie")]string outcome,[Values(false,true)]bool mirror)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,16,mirror,$"shootout:{homeFirst}:False:{outcome}"});
    [UnityTest] public IEnumerator OwnOrdersRequireBothManagers([Values(false,true)]bool homeFirst,[Values(false,true)]bool uneven,[Values(false,true)]bool mirror)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{false,16,mirror,$"shootout:{homeFirst}:{uneven}"});
}
