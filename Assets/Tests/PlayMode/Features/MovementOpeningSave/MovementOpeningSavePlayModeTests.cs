using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class MovementOpeningSavePlayModeTests
{
    private object board;
    private static Type Fixture => AppDomain.CurrentDomain.GetAssemblies()
        .Single(assembly => assembly.GetName().Name == "Assembly-CSharp-Editor")
        .GetType("GoalkeeperBoxEntryFixture", throwOnError: true);

    [SetUp]
    public void SetUp() => board = Activator.CreateInstance(Fixture);

    [UnityTearDown]
    public IEnumerator Cleanup() => (IEnumerator)Fixture.GetMethod("Cleanup").Invoke(board, null);

    [UnityTest]
    public IEnumerator OpeningSavesRestoresAndContinues([Values(1, -1)] int side, [Values(false, true)] bool preview)
        => (IEnumerator)Fixture.GetMethod("RunMovementOpeningSave").Invoke(board, new object[] { side, preview, false });

    [UnityTest]
    public IEnumerator ExpiredDecisionBudgetStaysExpiredAfterReload()
        => (IEnumerator)Fixture.GetMethod("RunMovementOpeningSave").Invoke(board, new object[] { 1, true, true });

    [UnityTest]
    public IEnumerator ForfeitClosesOpeningWithoutAnyMoves()
        => (IEnumerator)Fixture.GetMethod("RunForfeitedMovementOpeningCannotSave").Invoke(board, null);

    [UnityTest]
    public IEnumerator AcceptedNutmegClosesOpeningWithoutAnyMoves()
        => (IEnumerator)Fixture.GetMethod("RunAcceptedNutmegClosesMovementOpening").Invoke(board, null);
}
