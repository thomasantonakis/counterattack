using NUnit.Framework;

public sealed class GoalkeeperBoxEntryEditorTests
{
    private GoalkeeperBoxEntryFixture board;
    [SetUp] public void SetUp() => board = new GoalkeeperBoxEntryFixture();
    [TearDown] public void TearDown() => board?.Dispose();

    [TestCase(1, true)]
    [TestCase(-1, true)]
    [TestCase(1, false)]
    [TestCase(-1, false)]
    public void EntryEligibilityUsesPurposefulAttackingTeamAndDirection(int side, bool attackingTouch)
    {
        board.Board(side);
        board.match.LastTokenToTouchTheBallOnPurpose = attackingTouch ? board.attacker : board.defender;
        Assert.That(board.keeper.CanOfferGKMoveForPenaltyBox(side, board.Hex(12 * side), out var gk), Is.EqualTo(attackingTouch));
        Assert.That(gk, attackingTouch ? Is.SameAs(board.awayGK) : Is.Null);
        Assert.That(board.keeper.CanOfferGKMoveForPenaltyBox(-side, board.Hex(-12 * side), out _), Is.False);
        Assert.That(board.keeper.CanOfferGKMoveForPenaltyBox(0, board.Hex(0), out _), Is.False);
        Assert.That(board.keeper.isActivated, Is.False, "Eligibility previews must not start an interruption.");
    }

    [TestCase(1)] [TestCase(-1)]
    public void OfferConsumesOnlyThisEntryAndLeavingTheBoxRearmsIt(int side)
    {
        board.Board(side);
        Assert.That(board.keeper.TryStartGKMoveForPenaltyBox(side, board.Hex(12 * side), out var gk), Is.True);
        Assert.That(gk, Is.SameAs(board.awayGK));
        Assert.That(board.keeper.ShouldGKMove(board.Hex(13 * side)), Is.False);
        board.keeper.isActivated = false;
        board.ball.PlaceAtCell(board.Hex(13 * side));
        Assert.That(board.keeper.ShouldGKMove(board.Hex(14 * side)), Is.False);
        board.ball.PlaceAtCell(board.Hex(11 * side));
        Assert.That(board.keeper.ShouldGKMove(board.Hex(12 * side)), Is.True);
    }

    [TestCase(1)] [TestCase(-1)]
    public void PreviouslyMovedGoalkeeperRemainsEligibleForBoxEntry(int side)
    {
        board.Board(side);
        board.movement.movedTokens.Add(board.awayGK);
        board.Place(board.awayGK, board.Hex(12 * side, 2));
        Assert.That(board.keeper.ShouldGKMove(board.Hex(12 * side)), Is.True);
        Assert.That(board.keeper.GetCurrentDefendingGoalkeeper(), Is.SameAs(board.awayGK));
    }
}
