using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineManualControls")]
public sealed class OnlineManualControlsPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
        .GetType("OnlineManualControlsSceneFixture", true);
    [SetUp] public void Setup() => fixture = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    { if (fixture != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null); fixture = null; }
    [UnityTest] public IEnumerator RealButtonsUseAuthorizedGateway([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("Run").Invoke(fixture, new object[] { side });
    [UnityTest] public IEnumerator PendingAndLocalLossStopSubmission()
        => (IEnumerator)Bridge.GetMethod("RunPending").Invoke(fixture, null);
    [UnityTest] public IEnumerator RealExchangeRecoversInterruptedTransport([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunTransport").Invoke(fixture, new object[] { side });
}
