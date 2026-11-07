using NUnit.Framework;
using System.Linq;
public sealed class PenaltyDotUpcomingRoundEditorTests
{
    private static bool?[] Row(bool[] results, int round)
        => Enumerable.Range(0, 5).Select(i => ScoreboardManager.GetPenaltyDotResult(results, i, round)).ToArray();

    [TestCase(true)] [TestCase(false)]
    public void SixthKickShiftsBothRowsAndReservesGreySlots(bool homeFirst)
    {
        int round = ScoreboardManager.GetPenaltyDotDisplayRound(5, 5, homeFirst);
        Assert.That(round, Is.EqualTo(6));
        Assert.That(Row(new[] { true, false, true, false, true }, round), Is.EqualTo(new bool?[] { false, true, false, true, null }));
        Assert.That(Row(new[] { false, true, false, true, false }, round), Is.EqualTo(new bool?[] { true, false, true, false, null }));
    }

    [Test]
    public void FirstSixthResultFillsItsSlotWhileOtherTeamRemainsPending()
    {
        int round = ScoreboardManager.GetPenaltyDotDisplayRound(6, 5, false);
        Assert.That(round, Is.EqualTo(6));
        Assert.That(Row(new[] { true, false, true, false, true, false }, round), Is.EqualTo(new bool?[] { false, true, false, true, false }));
        Assert.That(Row(new[] { false, true, false, true, false }, round), Is.EqualTo(new bool?[] { true, false, true, false, null }));
    }

    [Test]
    public void CompletedRoundStaysVisibleUntilNextWalk()
    {
        bool[] results = { true, false, true, false, true, false };
        int completed = ScoreboardManager.GetPenaltyDotDisplayRound(6, 6, null);
        Assert.That(Row(results, completed), Is.EqualTo(new bool?[] { false, true, false, true, false }));
        int upcoming = ScoreboardManager.GetPenaltyDotDisplayRound(6, 6, true);
        Assert.That(Row(results, upcoming), Is.EqualTo(new bool?[] { true, false, true, false, null }));
    }

    [Test]
    public void FirstFiveRoundsRetainTheirOriginalSlots()
    {
        int round = ScoreboardManager.GetPenaltyDotDisplayRound(4, 4, false);
        Assert.That(Row(new[] { true, false, true, true }, round), Is.EqualTo(new bool?[] { true, false, true, true, null }));
    }
}
