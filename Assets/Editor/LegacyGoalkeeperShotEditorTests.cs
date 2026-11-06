using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public sealed class LegacyGoalkeeperShotEditorTests
{
    private GoalkeeperBoxEntryFixture board;
    [SetUp] public void SetUp() { board = new GoalkeeperBoxEntryFixture(); board.Board(1); }
    [TearDown] public void TearDown() => board?.Dispose();

    [Test]
    public void MixedPossibleSaveHexesPreferOwnBoxOverCloserOutsideHex()
    {
        board.Place(board.attacker, board.Hex(8, 8)); board.Place(board.awayGK, board.Hex(12, 8));
        board.ball.PlaceAtCell(board.attacker.currentHex);
        var shot = board.match.shotManager;
        shot.shooter = board.attacker; shot.shotType = "fullPower"; shot.targetHex = board.Hex(19);
        shot.alreadyInterceptedDefs = new List<PlayerToken>();
        var path = new List<HexCell> { board.Hex(12, 8), board.Hex(12, 7) };
        object interaction = GoalkeeperBoxEntryFixture.Invoke(shot, "BuildGKSaveInteraction", path);
        Assert.That(interaction, Is.Not.Null);
        Assert.That(interaction.GetType().GetField("interactionHex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .GetValue(interaction), Is.SameAs(board.Hex(12, 7)));
        Assert.That(shot.isWaitingForOutsideBoxGKShotDecision, Is.False);
    }
}
