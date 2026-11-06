using System.Linq;
using NUnit.Framework;

public sealed class LegacyGoalkeeperWallEditorTests
{
    private GoalkeeperBoxEntryFixture board;
    [SetUp] public void SetUp() { board = new GoalkeeperBoxEntryFixture(); board.Board(1); board.match.difficulty_level = 3; }
    [TearDown] public void TearDown() => board?.Dispose();
    [Test]
    public void WallSavePrecedesOutfieldInterceptionAndDirectPickupSkipsWallRoll()
    {
        board.Place(board.attacker, board.Hex(10, -2)); board.Place(board.defender, board.Hex(18, -3));
        board.ball.PlaceAtCell(board.attacker.currentHex);
        var interactions = GroundPassCommon.BuildOrderedBallPathInteractions(board.grid, board.ball,
            board.keeper, board.Hex(18, -2), includeGoalkeeperBoxMove: false);
        int wall = interactions.FindIndex(i => i.Type == GroundBallPathInteractionType.GoalkeeperWallSave);
        int outfield = interactions.FindIndex(i => i.Type == GroundBallPathInteractionType.OutfieldInterception && i.DefenderToken == board.defender);
        Assert.That(wall, Is.GreaterThanOrEqualTo(0)); Assert.That(outfield, Is.GreaterThanOrEqualTo(0));
        Assert.That(wall, Is.LessThan(outfield));
        bool safeBoxTargetExists = board.grid.cells.Cast<HexCell>()
            .Where(h => h != null && !h.isOutOfBounds && h.isInPenaltyBox == 1
                && HexGridUtils.GetHexStepDistance(board.attacker.currentHex, h) <= 11)
            .Any(h =>
            {
                var path = GroundPassCommon.BuildOrderedBallPathInteractions(board.grid, board.ball, board.keeper,
                    h, candidateDefenders: Enumerable.Empty<HexCell>());
                return path.Count == 1 && path[0].Type == GroundBallPathInteractionType.GoalkeeperBoxMove
                    && GroundPassCommon.CountDangerousInteractions(path) == 0;
            });
        Assert.That(safeBoxTargetExists, Is.True, "A procedural box move alone must not make every box target dangerous.");
        board.Place(board.attacker, board.Hex(10)); board.ball.PlaceAtCell(board.attacker.currentHex);
        var direct = GroundPassCommon.BuildOrderedBallPathInteractions(board.grid, board.ball,
            board.keeper, board.awayGK.currentHex, includeGoalkeeperBoxMove: false);
        Assert.That(direct.Any(i => i.Type == GroundBallPathInteractionType.GoalkeeperDirectPickup), Is.True);
        Assert.That(direct.Any(i => i.Type == GroundBallPathInteractionType.GoalkeeperWallSave), Is.False);
    }
}
