using CounterAttack.Online;
using NUnit.Framework;

[Category("OnlineDraftPolish")]
public sealed class OnlineDraftPolishEditorTests
{
    [Test]
    public void NamelessManagersConnectAndEditButCannotStartDraft()
    {
        var defaults = OnlineModeMenu.HostDefaults();
        var link = new OnlineDraftReturnTransport(defaults, startDraft: false);
        var host = link.Host.Client;
        var guest = link.Guest.Client;
        Assert.That(host.Connected && guest.Connected, Is.True);
        Assert.That(host.ViewSettings().homeTeamName, Is.Empty);
        Assert.That(guest.ViewSettings().awayTeamName, Is.Empty);
        Assert.That(host.Confirm(host.ViewSettings()), Is.False);
        Assert.That(guest.Confirm(guest.ViewSettings()), Is.False);

        var home = host.ViewSettings();
        home.homeTeamName = " Home Club ";
        host.Edit(home);
        link.Flush();
        Assert.That(guest.ViewSettings().homeTeamName, Is.EqualTo("Home Club"));
        Assert.That(guest.ViewSettings().awayTeamName, Is.Empty);
        Assert.That(host.Confirm(host.ViewSettings()), Is.False);
        Assert.That(link.Host.Draft, Is.Null);

        var away = guest.ViewSettings();
        away.awayTeamName = "Away Club";
        guest.Edit(away);
        link.Flush();
        link.Agree();
        Assert.That(link.Host.Draft.Capture().settings.homeTeamName, Is.EqualTo("Home Club"));
        Assert.That(link.Guest.Draft.Capture().settings.awayTeamName, Is.EqualTo("Away Club"));
    }
}
