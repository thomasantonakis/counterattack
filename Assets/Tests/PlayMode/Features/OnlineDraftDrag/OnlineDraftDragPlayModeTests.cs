using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
[Category("OnlineDraftDrag")]
public sealed class OnlineDraftDragPlayModeTests
{
    private object fixture;
    private static Type Bridge=>AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor").GetType("OnlineDraftSceneFixture",true);
    [SetUp] public void Setup()=>fixture=Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup(){if(fixture!=null)yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture,null);fixture=null;}
    [UnityTest] public IEnumerator AuthoredDragRoutesOneSemanticRequest([Values(false,true)]bool hostAway,[Values(false,true)]bool localHost,[Values("card","roster","keeper","foreign","kind","disconnect","reconnect","same","empty","ready","cancel")]string branch)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{hostAway,localHost,"drag-"+branch});
    [UnityTest] public IEnumerator DraftModeAndPhaseGuards([Values(false,true)]bool hostAway,[Values(false,true)]bool localHost,[Values("roll-blocked","gk-card","arcade-card","free-card","large")]string branch)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{hostAway,localHost,"drag-"+branch});
    [UnityTest] public IEnumerator LastPoolCardRestoresVisualWhenHidden([Values(false,true)]bool hostAway)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{hostAway,hostAway,"drag-last-card"});
    [UnityTest] public IEnumerator PendingGuestDropCannotSubmitAgain([Values(false,true)]bool hostAway)
        =>(IEnumerator)Bridge.GetMethod("Run").Invoke(fixture,new object[]{hostAway,false,"drag-pending"});
}
