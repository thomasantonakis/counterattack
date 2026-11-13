using System;
using System.IO;
using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineRoomStartup")]
public sealed class OnlineRoomStartupEditorTests
{
    [Test] public void GuestPersistenceFailsBeforeResolvingOrCreatingPaths()
    {
        var folder = Path.Combine(Path.GetTempPath(), "CounterattackReplica-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(folder, "never-created.json");
        using (new OnlineRoomSession("replica", true))
        {
            Assert.That(RoomSaveService.CreateLoadedRunCopy(path, out var run, out var reason), Is.False);
            Assert.That(run, Is.Empty); Assert.That(reason, Does.Contain("replicas"));
            Assert.That(RoomSaveService.SaveToPath(null, path, true, out _, out reason), Is.False);
            Assert.That(reason, Does.Contain("replicas"));
            Assert.That(RoomSaveService.SaveInPlace(null, out _, out reason), Is.False);
            Assert.That(reason, Does.Contain("replicas"));
            Assert.That(RoomSaveService.SaveAs(null, "never-created", false, out _, out reason), Is.False);
            Assert.That(reason, Does.Contain("replicas"));
            Assert.That(Directory.Exists(folder), Is.False);
        }
        Assert.That(OnlineRoomSession.Current, Is.Null);
    }
    [Test] public void OfflineAndHostDoNotAcquireGuestRole()
    {
        Assert.That(OnlineRoomSession.IsGuest, Is.False);
        using (new OnlineRoomSession("host", false))
        {
            Assert.That(OnlineRoomSession.IsGuest, Is.False);
            Assert.Throws<InvalidOperationException>(() => new OnlineRoomSession("second", true));
        }
        Assert.That(OnlineRoomSession.Current, Is.Null);
    }
}
