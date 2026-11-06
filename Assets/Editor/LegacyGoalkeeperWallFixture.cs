using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunGoalkeeperWallPushLegacy()
    {
        Board(1); match.difficulty_level = 3;
        Place(attacker, Hex(10, -2)); ball.PlaceAtCell(attacker.currentHex);
        var displaced = Token("Yaneva", true, false, 10); Place(displaced, Hex(16, -2));
        keeper.gkPushManager = Add<GKPushManager>(); keeper.gkPushManager.Configure(grid, ball);
        yield return keeper.ResolveGoalkeeperSaveAndHold(awayGK, displaced.currentHex, "gkWall");
        Assert.That(awayGK.currentHex, Is.SameAs(Hex(16, -2)));
        Assert.That(ball.GetCurrentHex(), Is.SameAs(Hex(16, -2)));
        Assert.That(displaced.currentHex, Is.Not.SameAs(Hex(16, -2)));
        Assert.That(!match.shotManager.isActivated && match.shotManager.isWaitingForSaveandHoldScenario, Is.True);
        Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(awayGK));
    }
}
