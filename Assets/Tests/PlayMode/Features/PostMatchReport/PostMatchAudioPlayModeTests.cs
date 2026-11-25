using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("PostMatchReport")]
public sealed class PostMatchAudioPlayModeTests
{
    [UnityTest]
    public IEnumerator FullTimeStopsStadiumAndMatchSoundsButRespectsMusicPreference([Values(true, false)] bool musicEnabled)
    {
        var fixture = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("PostMatchAudioFixture", true);
        return (IEnumerator)fixture.GetMethod("Run").Invoke(null, new object[] { musicEnabled });
    }
}
