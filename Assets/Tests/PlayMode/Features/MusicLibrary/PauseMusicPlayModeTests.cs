using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("MusicLibrary")]
public sealed class PauseMusicPlayModeTests
{
    [UnityTest] public IEnumerator PauseMusicStartsMutesAndStops()
    {
        var bridge = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
            .GetType("PauseMusicFixture", true);
        yield return (IEnumerator)bridge.GetMethod("Run").Invoke(null, null);
    }
}
