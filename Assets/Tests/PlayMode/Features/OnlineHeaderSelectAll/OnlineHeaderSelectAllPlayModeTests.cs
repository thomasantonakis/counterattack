using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineHeaderSelectAll")]
public sealed class OnlineHeaderSelectAllPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator BulkNominationsResolveRealHeader([Values(1,-1)]int side,[Values(false,true)]bool home,[Values(false,true)]bool keeper)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineHeaderAll").Invoke(fixture,new object[]{side,home,keeper?"keeper":"normal"});
    [UnityTest] public IEnumerator OverLimitRequiresIndividualNominations([Values(1,-1)]int side,[Values(false,true)]bool home,[Values("attack","defence")]string crowd)
        =>(IEnumerator)Bridge.GetMethod("RunOnlineHeaderAll").Invoke(fixture,new object[]{side,home,"crowded-"+crowd});
}
