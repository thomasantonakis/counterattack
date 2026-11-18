using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("ShootingPathInspection")]
public sealed class ShootingPathInspectorPlayModeTests
{
    [UnityTest]
    public IEnumerator DiagnosticSceneRendersAuthoredOverlaysAndCleansUpOnDisable()
    {
        Type bridge = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
            .GetType("ShootingPathInspectorRuntimeFixture", true);
        return (IEnumerator)bridge.GetMethod("Run").Invoke(Activator.CreateInstance(bridge), null);
    }
}
