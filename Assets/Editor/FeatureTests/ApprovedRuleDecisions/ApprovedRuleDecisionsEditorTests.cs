using NUnit.Framework;
using System.Reflection;

public sealed class ApprovedRuleDecisionsEditorTests
{
    [TestCase(0, 1f / 12f)]
    [TestCase(1, 1f / 12f)]
    [TestCase(2, 1f / 6f)]
    [TestCase(3, 1f / 3f)]
    [TestCase(7, 1f)]
    public void HighPassAccuracyIncludesAutomaticJackpot(int attribute, float expected)
        => Assert.That(HighPassManager.AccuracyProbabilityForDecisionPreview(attribute), Is.EqualTo(expected).Within(.00001f));

    [TestCase(1, false)]
    [TestCase(-1, false)]
    [TestCase(1, true)]
    [TestCase(-1, true)]
    public void KeeperOutsideOwnBoxUsesHeadingZero(int side, bool attacking)
    {
        using var board = new GoalkeeperBoxEntryFixture();
        board.Board(side);
        var keeper = attacking ? board.homeGK : board.awayGK;
        board.Place(keeper, board.Hex(10 * side));
        keeper.aerial = 6;
        Assert.That(board.match.shotManager.headerManager.GetHeaderAttribute(keeper), Is.Zero);
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void DefendingKeeperInsideOwnBoxKeepsAerial(int side)
    {
        using var board = new GoalkeeperBoxEntryFixture();
        board.Board(side);
        board.awayGK.aerial = 6;
        Assert.That(board.match.shotManager.headerManager.GetHeaderAttribute(board.awayGK), Is.EqualTo(6));
    }

    [TestCase(1)]
    [TestCase(-1)]
    public void PreviewUsesProjectedKeeperPositionAndDistancePenalty(int side)
    {
        using var board = new GoalkeeperBoxEntryFixture();
        board.Board(side); board.awayGK.aerial = 6;
        Assert.That(HeaderManager.HeaderAttributeAt(board.awayGK, board.Hex(10 * side)), Is.Zero);
        Assert.That(HeaderManager.HeaderAttributeAt(board.awayGK, board.Hex(13 * side)), Is.EqualTo(6));
        board.Place(board.awayGK, board.Hex(10 * side));
        var preview = typeof(RoomAIDecisionSnapshotBuilder).GetMethod("EffectiveHeaderAttribute", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(preview.Invoke(null, new object[] { board.awayGK, board.Hex(8 * side) }), Is.EqualTo(0));
    }

    [TestCase(0, 0)]
    [TestCase(1, -1)]
    [TestCase(6, -1)]
    public void DistancePenaltyCannotReduceHeaderAttributeBelowZero(int heading, int expectedPenalty)
    {
        using var board = new GoalkeeperBoxEntryFixture();
        board.Board(1); board.defender.heading = heading;
        board.Place(board.defender, board.Hex(8)); board.ball.PlaceAtCell(board.Hex(10));
        Assert.That(board.match.shotManager.headerManager.GetHeaderPenalty(board.defender), Is.EqualTo(expectedPenalty));
        Assert.That(HeaderManager.EffectiveHeaderAttributeAt(board.defender, board.Hex(8), board.Hex(10)),
            Is.EqualTo(heading + expectedPenalty));
    }

    [Test]
    public void NonJackpotRollConvertsForcedStarToSix()
    {
        using var board = new GoalkeeperBoxEntryFixture();
        var result = board.match.ResolveGameplayDiceRoll("movement_foul_card_leniency",
            new RollInputOverride { hasOverride = true, roll = 1, isJackpot = true }, jackpotEnabled: false);
        Assert.That(result.roll, Is.EqualTo(6));
        Assert.That(result.isJackpot || result.jackpotEnabled, Is.False);
    }

    [Test]
    public void AuthoredRulebookMatchesApprovedEdition()
    {
        var blocks = RulesViewerEditorTools.LoadDocument();
        var viewer = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(RulesViewerEditorTools.PrefabPath)
            .GetComponent<RulesViewer>();
        Assert.That(viewer.entries.Length, Is.EqualTo(blocks.Count));
        for (int i = 0; i < blocks.Count; i++)
            Assert.That(viewer.entries[i].text.text, Is.EqualTo(blocks[i].text), "Rulebook block " + i);
    }
}
