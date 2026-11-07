using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
public sealed class ShootoutUiFeedbackPlayModeTests
{
    private object board;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    [SetUp] public void SetUp() => board = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown()
    { if (board != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(board, null); board = null; }
    [UnityTest] public IEnumerator StakesAndRollPrompt([Values(true,false)] bool scores)
        => (IEnumerator)Bridge.GetMethod("RunShootoutUiStakes").Invoke(board, new object[] { scores });
    [UnityTest] public IEnumerator OffTargetHasNoTotal([Values(1,-1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunShootoutUiOffTarget").Invoke(board, new object[] { side });
    [UnityTest] public IEnumerator PreviewsFollowNewContestants([Values(true,false)] bool homeShooter)
        => (IEnumerator)Bridge.GetMethod("RunShootoutUiPreviews").Invoke(board, new object[] { homeShooter });
}
