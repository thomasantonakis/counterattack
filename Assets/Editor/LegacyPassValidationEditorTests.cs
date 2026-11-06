using NUnit.Framework;

public sealed class LegacyPassValidationEditorTests
{
    private GoalkeeperBoxEntryFixture board;
    [SetUp] public void SetUp() { board = new GoalkeeperBoxEntryFixture(); board.Board(1); }
    [TearDown] public void TearDown() => board?.Dispose();

    [TestCase("kickoff")]
    [TestCase("ground")]
    [TestCase("ftp")]
    public void DefendingGoalkeeperOnPathBlocksPass(string action)
    {
        board.match.difficulty_level = 1;
        board.Place(board.attacker, board.Hex(0));
        board.Place(board.awayGK, board.Hex(-1));
        board.ball.PlaceAtCell(board.attacker.currentHex);
        var target = board.Hex(-4);
        Assert.That(GroundPassCommon.CalculateThickPath(board.grid, board.attacker.currentHex,
            target, board.ball.ballRadius), Does.Contain(board.awayGK.currentHex));
        board.ground.isKickoffPass = action == "kickoff";
        GroundPassValidationResult result = action == "ftp"
            ? (GroundPassValidationResult)GoalkeeperBoxEntryFixture.Invoke(board.ftp, "ValidateFTPTargetPath", target)
            : board.ground.ValidateGroundPassPath(target, action == "kickoff" ? 1 : 99);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(PassValidationFailureReason.BlockedByDefender));
    }

    [Test]
    public void EasyKickoffHalfwayTargetRetainsInterceptions()
    {
        board.match.difficulty_level = 1;
        board.Place(board.attacker, board.Hex(0)); board.Place(board.defender, board.Hex(1, 2));
        board.ball.PlaceAtCell(board.attacker.currentHex);
        board.ground.isKickoffPass = true;
        var result = board.ground.ValidateGroundPassPath(board.Hex(0, 4), 99);
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.IsDangerous, Is.True);
    }
}
