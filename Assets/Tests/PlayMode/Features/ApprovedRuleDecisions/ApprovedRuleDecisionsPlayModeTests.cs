using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public sealed class ApprovedRuleDecisionsPlayModeTests
{
    private object board;
    private static Type Bridge => AppDomain.CurrentDomain.GetAssemblies()
        .Single(a => a.GetName().Name == "Assembly-CSharp-Editor").GetType("GoalkeeperBoxEntryFixture", true);
    [SetUp] public void SetUp() => board = Activator.CreateInstance(Bridge);
    [UnityTearDown] public IEnumerator TearDown()
    {
        if (board != null) yield return (IEnumerator)Bridge.GetMethod("Cleanup").Invoke(board, null);
        board = null;
    }

    [UnityTest]
    public IEnumerator AccuracyAndControl([Values("high", "long", "control")] string action,
        [Values(false, true)] bool jackpot, [Values(1, -1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunApprovedAccuracy").Invoke(board, new object[] { action, jackpot, side });

    [UnityTest]
    public IEnumerator BookedDefenderWithoutNewCardCanPlayOn()
        => (IEnumerator)Bridge.GetMethod("RunApprovedBookedNoCard").Invoke(board, null);

    [UnityTest]
    public IEnumerator OwnBoxKeeperUsesNormalCards([Values(1, 6)] int cardRoll, [Values(1, -1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunApprovedKeeperCards").Invoke(board, new object[] { cardRoll, side });

    [UnityTest]
    [Category("OutsideKeeperHeaders")]
    public IEnumerator OutsideKeeperWinsHeaderWithoutHolding([Values(1, -1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunApprovedOutsideHeader").Invoke(board, new object[] { side, false });

    [UnityTest]
    [Category("OutsideKeeperHeaders")]
    public IEnumerator TwoHeaderJackpotsTieAtFifty([Values(1, -1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunApprovedOutsideHeader").Invoke(board, new object[] { side, true });

    [UnityTest]
    [Category("OutsideKeeperHeaders")]
    public IEnumerator KeeperRushUsesFinalKeeperHex([Values("inside_out", "outside_in", "outside_out")] string route,
        [Values(1, -1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunApprovedKeeperRush").Invoke(board, new object[] { route, side });

    [UnityTest]
    public IEnumerator OutsideKeeperDisciplineRemainsRegular([Values("danger_feet", "danger_hands_red", "hand_win", "hand_tie")] string branch)
        => (IEnumerator)Bridge.GetMethod("RunKeeperFoulLegacy").Invoke(board, new object[] { branch });

    [UnityTest]
    [Category("OutsideKeeperHeaders")]
    public IEnumerator OutsideKeeperKeepsSeparateNominationAndZeroHeading([Values(1, -1)] int side)
        => (IEnumerator)Bridge.GetMethod("RunOutsideKeeperAndTwoDefenders").Invoke(board, new object[] { side });

    [UnityTest]
    public IEnumerator MandatoryFoulStoppagesRemain([Values(false, true)] bool secondYellow, [Values(false, true)] bool secondInjury)
        => (IEnumerator)Bridge.GetMethod("RunFoulTakenLegacy").Invoke(board, new object[] { secondYellow, secondInjury });
}
