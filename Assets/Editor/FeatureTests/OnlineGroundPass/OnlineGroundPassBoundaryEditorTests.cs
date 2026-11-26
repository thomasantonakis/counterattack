using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineGroundPass")]
public sealed class OnlineGroundPassBoundaryEditorTests
{
    [Test]
    public void WithdrawnBoundaryRejectsAQueuedCommandAndRetainsAcceptedReceipt()
    {
        var version = new OnlineCompatibility("v1", "rules", "fixture");
        var protocol = new OnlineMatchProtocol("match", "a", "b", "b", version);
        protocol.BindAuthenticatedAccount("a", "home", version);
        protocol.BindAuthenticatedAccount("b", "away", version);
        protocol.PublishDecision("start", OnlineTeam.Home, new[] { "ground.start" });
        var start = new OnlineMatchCommand("match", 1, protocol.Revision, "start", "ground.start");
        Assert.That(protocol.Admit("home", start).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        protocol.PublishDecision("target", OnlineTeam.Home, new[] { "ground.target:0:6" });
        var queued = new OnlineMatchCommand("match", 2, protocol.Revision, "target", "ground.target:0:6");
        var before = protocol.Revision;
        protocol.WithdrawDecision();
        Assert.That(protocol.Revision, Is.EqualTo(before + 1));
        Assert.That(protocol.HasPendingDecision, Is.False);
        Assert.That(protocol.Admit("home", queued).Status, Is.EqualTo(OnlineCommandStatus.StaleRevision));
        Assert.That(protocol.Admit("home", start).Status, Is.EqualTo(OnlineCommandStatus.Duplicate));
        protocol.WithdrawDecision();
        Assert.That(protocol.Revision, Is.EqualTo(before + 1), "withdrawing an empty boundary is idempotent");
    }
}
