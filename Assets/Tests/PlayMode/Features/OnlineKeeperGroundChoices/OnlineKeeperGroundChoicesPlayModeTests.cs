using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineKeeperGroundChoices")]
public sealed class OnlineKeeperGroundChoicesPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator OutsideKeeperChoices([Values(1,-1)]int side,[Values("moving","stationary","away-moving","away-stationary")]string context,[Values("dive","foot","stand")]string choice)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineKeeperGroundChoice").Invoke(fixture,new object[]{side,context,choice});
    [UnityTest] public IEnumerator WallKeeperChoices([Values(1,-1)]int side,[Values("dive","stand")]string choice)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineKeeperGroundChoice").Invoke(fixture,new object[]{side,"wall",choice});
    [UnityTest] public IEnumerator AwayAttackWallChoices([Values(1,-1)]int side,[Values("dive","stand")]string choice)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineKeeperGroundChoice").Invoke(fixture,new object[]{side,"away-wall",choice});
    [UnityTest] public IEnumerator KeeperDiveOutcomes([Values(1,-1)]int side,[Values("moving","stationary","wall","away-moving","away-stationary","away-wall")]string context,[Values("win","lose","tie")]string outcome)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineKeeperGroundChoice").Invoke(fixture,new object[]{side,context,"dive-"+outcome});
}
