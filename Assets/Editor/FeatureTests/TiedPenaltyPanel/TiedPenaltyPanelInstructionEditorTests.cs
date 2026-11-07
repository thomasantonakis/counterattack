using NUnit.Framework;
using UnityEngine;
public sealed class TiedPenaltyPanelInstructionEditorTests
{
    [TestCase(true)] [TestCase(false)]
    public void DecisiveKickStakesRemainInWalkupInstructions(bool scores)
    {
        using var board = new GoalkeeperBoxEntryFixture();
        var host = new GameObject("Shootout instruction test");
        try
        {
            var shootout = host.AddComponent<PenaltyShootoutManager>(); shootout.matchManager = board.match;
            var taker = scores ? board.attacker : board.defender;
            shootout.homeShootoutScore = scores ? 4 : 5; shootout.awayShootoutScore = 4;
            shootout.homeAttempts = scores ? 4 : 5; shootout.awayAttempts = scores ? 5 : 4;
            GoalkeeperBoxEntryFixture.Set(shootout, "transitionInstructionActive", true);
            GoalkeeperBoxEntryFixture.Set(shootout, "transitionInstructionShooter", taker);
            Assert.That(shootout.GetInstructions(), Does.Contain(scores
                ? "Fixture Home win if 9.Home carrier scores"
                : "Fixture Away lose if 8.Away recovering player misses"));
            Assert.That(shootout.GetInstructions(), Does.Not.Contain("[ ]"));
        }
        finally { Object.DestroyImmediate(host); }
    }
}
