using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category("OfflineDraftPresentation")]
public sealed class OfflineDraftPresentationPlayModeTests
{
    private object fixture;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies()
        .Single(assembly => assembly.GetName().Name == "Assembly-CSharp-Editor")
        .GetType("OfflineDraftPresentationFixture", true);

    [SetUp] public void SetUp() => fixture = Activator.CreateInstance(Bridge);

    [UnityTest]
    public IEnumerator DealtCardsShowRatingsAboveAuthoredCircles([Values("Hot Seat", "Single Player")] string mode)
        => (IEnumerator)Bridge.GetMethod("RunCards").Invoke(fixture, new object[] { mode });

    [UnityTest]
    public IEnumerator DragHighlightsAndAssignsOnlyAnEligibleRoster([Values(false, true)] bool homeWins)
        => (IEnumerator)Bridge.GetMethod("RunDrop").Invoke(fixture, new object[] { "Hot Seat", homeWins });

    [UnityTest]
    public IEnumerator SinglePlayerDragHighlightsAndAssignsTheHumanRoster()
        => (IEnumerator)Bridge.GetMethod("RunDrop").Invoke(fixture, new object[] { "Single Player", true });

    [UnityTest]
    public IEnumerator FirstPickRollsAutomaticallyForBothTeams([Values("Hot Seat", "Single Player")] string mode)
        => (IEnumerator)Bridge.GetMethod("RunAutomaticRoll").Invoke(fixture, new object[] { mode, false });

    [UnityTest]
    public IEnumerator AuthoredFirstPickFooterFitsOnOneLine([Values("Hot Seat", "Single Player")] string mode)
        => (IEnumerator)Bridge.GetMethod("RunFooter").Invoke(fixture, new object[] { mode });

    [UnityTest]
    public IEnumerator KickoffTossAutoRollsBothTeams([Values("Hot Seat", "Single Player")] string mode)
        => (IEnumerator)Bridge.GetMethod("RunAutomaticRoll").Invoke(fixture, new object[] { mode, true });

    [UnityTearDown]
    public IEnumerator Cleanup() => (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(fixture, null);
}
