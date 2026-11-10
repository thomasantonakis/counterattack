using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("GameplayAudio")]
public sealed class GameplayAudioPlaybackPlayModeTests
{
    [UnityTest]
    public IEnumerator IndependentStandsFadePauseResumeAndMute([Values(true, false)] bool homeScored)
    {
        var fixture = AppDomain.CurrentDomain.GetAssemblies()
            .Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
            .GetType("GameplayAudioPlaybackFixture", true);
        return (IEnumerator)fixture.GetMethod("Run").Invoke(null, new object[] { homeScored });
    }

    [UnityTest]
    public IEnumerator ContinuousWallaCrossfadesSmallPoolsWithoutGainDoubling([Values(0, 1, 2, 3)] int clipCount)
    {
        var fixture = AppDomain.CurrentDomain.GetAssemblies()
            .Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
            .GetType("GameplayAudioPlaybackFixture", true);
        return (IEnumerator)fixture.GetMethod("RunCrossfade").Invoke(null, new object[] { clipCount });
    }
}
