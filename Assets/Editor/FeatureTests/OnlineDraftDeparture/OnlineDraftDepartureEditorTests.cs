using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineDraftDeparture")]
public sealed class OnlineDraftDepartureEditorTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void ArcadePoolStaysOpenUntilBothTeamsConfirm(bool keepers)
    {
        var settings = OnlineSetupAgreementEditorTests.Valid();
        settings.gkDraft = keepers ? "Arcade" : "Deal";
        settings.draft = "Arcade";
        var link = new OnlineDraftReturnTransport(settings);
        var home = link.Host.Draft;
        var away = link.Guest.Draft;
        string id = home.Capture().available[0];
        int[] slots = keepers ? new[] { 1, 12 } : Enumerable.Range(1, home.Capture().home.Length)
            .Where(slot => slot != 1 && slot != 12).ToArray();

        link.Request(away, OnlineDraftIntent.Pick, id, slots[0]);
        Assert.That(home.Request(OnlineDraftIntent.ReturnToPool, id, slots[0]), Is.True);
        link.Flush();
        Assert.That(home.LastReply, Is.EqualTo(OnlineCommandStatus.IllegalAction));
        Assert.That(home.Capture().away[slots[0] - 1].id, Is.EqualTo(id));
        foreach (int slot in slots)
        {
            link.Request(home, OnlineDraftIntent.Pick, id, slot);
            if (slot != slots[0]) link.Request(away, OnlineDraftIntent.Pick, id, slot);
        }

        Assert.That(home.Phase, Is.EqualTo("review"));
        link.Request(away, OnlineDraftIntent.Ready);
        Assert.That(home.Capture().available, Does.Contain(id));
        link.Request(home, OnlineDraftIntent.ReturnToPool, id, slots[0]);
        var reopened = away.Capture();
        Assert.That(reopened.phase, Is.EqualTo("pick"));
        Assert.That(reopened.home[slots[0] - 1], Is.Null);
        Assert.That(reopened.away[slots[0] - 1].id, Is.EqualTo(id));
        Assert.That(reopened.homeReady || reopened.awayReady, Is.False);
        link.Request(home, OnlineDraftIntent.Pick, id, slots[0]);
        link.Request(home, OnlineDraftIntent.Ready);
        link.Request(away, OnlineDraftIntent.Ready);
        Assert.That(away.Capture().available, Does.Not.Contain(id));
        Assert.That(home.Request(OnlineDraftIntent.ReturnToPool, id, slots[0]), Is.True);
        link.Flush();
        Assert.That(home.LastReply, Is.EqualTo(keepers ? OnlineCommandStatus.IllegalAction : OnlineCommandStatus.NotReady));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TransportDepartureRemainsTerminalAcrossLateMessages(bool hostRemains)
    {
        var link = new OnlineDraftReturnTransport(startDraft: false);
        OnlineSessionChannel local = hostRemains ? link.Host : link.Guest;
        typeof(OnlineSessionChannel).GetMethod("MarkPeerLeft",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(local, new object[] { false });
        Assert.That(local.PeerLeft, Is.True);
        Assert.That(local.Client.Connected, Is.False);
        Assert.That(local.Receive(hostRemains ? OnlineDraftReturnTransport.GuestAccount : OnlineDraftReturnTransport.HostAccount,
            System.Array.Empty<byte>(), 0), Is.False);
        local.Tick();
        Assert.That(local.PeerLeft, Is.True);
    }
}
