using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OnlineMenuAudio")]
public sealed class OnlineMenuAudioPlayModeTests
{
    private object fixture;
    private Type bridge;

    private IEnumerator Run(string fixtureName, string method)
    {
        bridge = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
            .GetType(fixtureName, throwOnError: true);
        fixture = Activator.CreateInstance(bridge);
        return (IEnumerator)bridge.GetMethod(method).Invoke(fixture, null);
    }

    [UnityTest]
    public IEnumerator LobbyAndSaveBrowserUseStandardCues() =>
        Run("OnlineMenuAudioFixture", "LobbyAndSaveBrowser");

    [UnityTest]
    public IEnumerator DraftPagingUsesDirectionalCues() =>
        Run("OnlineDraftSceneFixture", "RunDraftMenuAudio");

    [UnityTest]
    public IEnumerator OnlinePauseAndRulesUseStandardCues() =>
        Run("OnlineFullRoomSceneFixture", "RunPauseMenuAudio");

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (fixture != null) yield return (IEnumerator)bridge.GetMethod("Cleanup").Invoke(fixture, null);
        fixture = null;
    }
}
