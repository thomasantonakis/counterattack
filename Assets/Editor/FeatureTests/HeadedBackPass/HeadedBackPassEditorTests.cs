using System.Linq;
using NUnit.Framework;

public sealed class HeadedBackPassEditorTests
{
    [Test]
    public void LandingRecoveryPreservesExclusions([Values(1,-1)] int side,
        [Values("eligible","jumped","stunned","suppressed","path_only")] string status)
    {
        using var board = new GoalkeeperBoxEntryFixture();
        board.Board(side);
        var header = board.match.shotManager.headerManager;
        var landing = board.Hex(-17 * side);
        var adjacent = landing.GetNeighbors(board.grid).First(h => h != null && !h.isOutOfBounds && h.GetOccupyingToken() == null);
        board.Place(board.defender, status == "path_only" ? board.Hex(-13 * side, 1) : adjacent);
        if (status == "jumped") header.defenderWillJump.Add(board.defender);
        if (status == "stunned") board.movement.stunnedTokens.Add(board.defender);
        if (status == "suppressed") header.SuppressHeaderInterceptionForToken(board.defender, "already attempted");
        Assert.That(header.GetHeaderPassInterceptorsForPreview(landing).Contains(board.defender),
            Is.EqualTo(status == "eligible"));
    }

    [Test]
    public void BundledRulesMatchApprovedHeadersDraftAndStoppage()
    {
        var blocks = RulesViewerEditorTools.LoadDocument();
        var viewer = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(RulesViewerEditorTools.PrefabPath).GetComponent<RulesViewer>();
        Assert.That(viewer.entries.Length, Is.EqualTo(blocks.Count));
        for (int i = 0; i < blocks.Count; i++) Assert.That(viewer.entries[i].text.text, Is.EqualTo(blocks[i].text));
        string text = string.Join(" ", blocks.Select(b => b.text));
        Assert.That(text, Does.Contain("16 or 18"));
        Assert.That(text, Does.Contain("3 actions per extra-time half"));
        Assert.That(text, Does.Contain("Hands requires a normal D6"));
    }
}
