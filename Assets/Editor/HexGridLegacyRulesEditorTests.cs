using NUnit.Framework;

/// <summary>Replacement assertions from legacy Scenario_008d, without match startup or fixed delays.</summary>
public sealed class HexGridLegacyRulesEditorTests
{
    private GoalkeeperBoxEntryFixture board;
    [SetUp]
    public void SetUp()
    {
        board = new GoalkeeperBoxEntryFixture();
        GoalkeeperBoxEntryFixture.Invoke(board.grid, "CalculateGoalDistanceAndDangerousTackleCaches");
    }
    [TearDown] public void TearDown() => board?.Dispose();

    [Test]
    public void Scenario008dRightShotHexHasTheAuthoredDistancesToBothGoals()
    {
        Assert.That(board.Hex(14).DistanceToRightGoal, Is.EqualTo(5));
        Assert.That(board.Hex(14).DistanceToLeftGoal, Is.EqualTo(33));
    }

    [TestCase(-13, -1, true)]
    [TestCase(-13, 0, true)]
    [TestCase(-14, -1, false)]
    [TestCase(-14, 1, false)]
    [TestCase(-15, -1, false)]
    [TestCase(-15, 0, false)]
    public void Scenario008dDangerousTackleCacheMatchesTheLegacyBoard(int x, int z, bool dangerous)
    {
        Assert.That(board.Hex(-14).IsDangerousTacklingPosition(
            MatchManager.TeamAttackingDirection.RightToLeft, board.Hex(x, z)), Is.EqualTo(dangerous));
    }
}
