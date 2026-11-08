using NUnit.Framework;

public sealed class GoalkeeperPickupEditorTests
{
    [Test]
    public void TeammateKicksNeverPermitHands([Values(1,-1)] int side,
        [Values(MatchManager.MatchActionKind.StandardPass, MatchManager.MatchActionKind.FirstTimePass,
            MatchManager.MatchActionKind.HighPass, MatchManager.MatchActionKind.LongBall)] MatchManager.MatchActionKind action)
    {
        using var b = new GoalkeeperBoxEntryFixture(); b.Board(side);
        b.match.CommitToAction(action);
        b.match.SetLastToken(b.homeGK); b.ball.PlaceAtCell(b.homeGK.currentHex);
        b.match.UpdatePossessionAfterPass(b.homeGK.currentHex);
        Assert.That(b.match.CanGoalkeeperHandle(b.homeGK), Is.False);
        Assert.That(b.match.CanOfferGoalkeeperPickup(b.homeGK), Is.False);
        b.match.BroadcastDefensiveRecoveryOutcome(b.homeGK, b.homeGK.currentHex, false);
        Assert.That(b.match.shotManager.isWaitingForSaveandHoldScenario, Is.False,
            "generic recovery and own-box location cannot grant hands");
    }

    [Test]
    public void OpponentRecoveryAndBoundaries([Values(1,-1)] int side)
    {
        using var b = new GoalkeeperBoxEntryFixture(); b.Board(side);
        b.Place(b.homeGK, b.Hex(-10 * side));
        b.match.SetLastToken(b.defender);
        b.match.SetLastTokenFromLooseBall(b.homeGK);
        b.ball.PlaceAtCell(b.homeGK.currentHex); b.match.UpdatePossessionAfterPass(b.homeGK.currentHex);
        b.match.BroadcastDefensiveRecoveryOutcome(b.homeGK, b.homeGK.currentHex, false);
        Assert.That(b.match.currentState, Is.EqualTo(MatchManager.GameState.AnyOtherScenario));
        Assert.That(b.match.CanGoalkeeperHandle(b.homeGK), Is.True);
        Assert.That(b.match.CanOfferGoalkeeperPickup(b.homeGK), Is.False);
        b.movement.ResetMovementPhase();
        b.Place(b.homeGK, b.Hex(-13 * side)); b.ball.PlaceAtCell(b.homeGK.currentHex);
        Assert.That(b.match.CanOfferGoalkeeperPickup(b.homeGK), Is.True);
        b.Place(b.homeGK, b.Hex(13 * side)); b.ball.PlaceAtCell(b.homeGK.currentHex);
        Assert.That(b.match.CanOfferGoalkeeperPickup(b.homeGK), Is.False, "opponent box is not own box");
        b.match.SetLastToken(b.attacker);
        Assert.That(b.match.CanGoalkeeperHandle(b.homeGK), Is.False, "passing/loss consumes eligibility");
    }

    [Test]
    public void OldSaveNeverInfersEligibility()
    {
        using var b = new GoalkeeperBoxEntryFixture(); b.Board(1);
        b.match.SetHangingPass("aerial");
        GoalkeeperBoxEntryFixture.Invoke(b.match, "RestoreTouchReferences", new RoomTouchReferences());
        b.match.SetLastToken(b.homeGK); b.ball.PlaceAtCell(b.homeGK.currentHex);
        Assert.That(b.match.CanGoalkeeperHandle(b.homeGK), Is.False);
    }

    [Test]
    public void CanceledMovementCannotCompleteOrPrompt([Values(1,-1)] int side)
    {
        using var b = new GoalkeeperBoxEntryFixture(); b.Board(side);
        b.match.BeginGoalkeeperBallDelivery(GoalkeeperDeliveryKind.Header, b.attacker);
        b.match.SetLastToken(b.homeGK); b.ball.PlaceAtCell(b.homeGK.currentHex);
        b.movement.ActivateMovementPhase(); b.movement.CommitToAction();
        int actions = b.match.totalCompletedActions;
        b.movement.EndMovementPhase(false);
        b.match.BroadcastSafeEndofMovementPhase();
        Assert.That(b.keeper.IsWaitingForPickupChoice, Is.False);
        Assert.That(b.match.totalCompletedActions, Is.EqualTo(actions));
    }

    [Test]
    public void RulebookTextMatchesSource()
    {
        var blocks = RulesViewerEditorTools.LoadDocument();
        var viewer = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(RulesViewerEditorTools.PrefabPath).GetComponent<RulesViewer>();
        Assert.That(viewer.entries.Length, Is.EqualTo(blocks.Count));
        for (int i = 0; i < blocks.Count; i++) Assert.That(viewer.entries[i].text.text, Is.EqualTo(blocks[i].text));
    }
}
