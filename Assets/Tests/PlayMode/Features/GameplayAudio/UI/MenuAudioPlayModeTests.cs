using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("MenuAudio")]
public sealed class MenuAudioPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor")
        .GetType("MenuAudioPlaybackFixture", true);
    [SetUp] public void SetUp() => fixture = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator Cleanup()
    { if (fixture != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null); fixture = null; }
    private IEnumerator Run(string name, params object[] args) => (IEnumerator)Bridge.GetMethod(name).Invoke(fixture, args);
    [UnityTest] public IEnumerator EachCueWorksDuringPause([Values(0, 1, 2, 3)] int cue) => Run("RunPlayback", cue);
    [UnityTest] public IEnumerator PriorityDuplicatesAndMute() => Run("RunPriorityMute");
    [UnityTest] public IEnumerator CheckboxInputSelectsButRefreshAndDisabledInputStaySilent() => Run("RunCheckbox");
    [UnityTest] public IEnumerator PreMatchRollOffUsesDiceCue([Values(false, true)] bool rigged) => Run("RunPreMatchDice", rigged);
    [UnityTest] public IEnumerator DropdownInputIsDistinctFromProgrammaticRefresh() => Run("RunDropdown");
    [UnityTest] public IEnumerator DisabledButtonsAndRejectedNavigationStaySilent() => Run("RunButtonAndRejectedAction");
    [UnityTest] public IEnumerator NavigationVoiceSurvivesSceneChangeWithoutDuplicates() => Run("RunSceneLifetime");
}
