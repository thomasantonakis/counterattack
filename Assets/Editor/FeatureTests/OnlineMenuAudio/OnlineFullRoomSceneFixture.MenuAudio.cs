using System.Collections;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class OnlineFullRoomSceneFixture
{
    public IEnumerator RunPauseMenuAudio()
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var compatibility = new OnlineCompatibility("ground-wire-v7", "ground-rules-v1", OnlineFullRoomBootstrap.Catalog);
        var protocol = OnlineFrontendSessionPolicy.CreateMatchProtocol("online-menu-audio", "home", "away", compatibility);
        Assert.That(protocol.BindAuthenticatedAccount("home", "host", compatibility), Is.True);
        Assert.That(protocol.BindAuthenticatedAccount("away", "guest", compatibility), Is.True);
        yield return LoadNativeRoom(Data(16, false, useFrontendSeats: true), false, protocol.MatchId);
        var startup = Object.FindAnyObjectByType<OnlineRoomStartup>();
        var authority = new OnlineGroundPassAuthority(startup.Board.match, protocol);
        pause = new OnlineSharedPause(protocol, startup.Board.match);
        Assert.That(pause.Submit("host", new OnlineMatchCommand(protocol.MatchId, 1, pause.Revision,
            "shared-pause", OnlineSharedPause.RequestAction)).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        startup.Board.Display(pause.Capture(authority), "Menu audio fixture");
        yield return OnlineMenuAudioFixture.WithGameAudio(ExercisePauseMenus);
    }

    private static IEnumerator ExercisePauseMenus()
    {
        var view = Object.FindAnyObjectByType<PauseMenuManager>(FindObjectsInactive.Include);
        var rules = Object.FindAnyObjectByType<RulesViewer>(FindObjectsInactive.Include);
        var player = MenuAudioPlayer.Instance;
        player.voice.Stop();
        Assert.That(view.editSettings.IsInteractable(), Is.True);
        OnlineMenuAudioFixture.Hover(view.editSettings);
        OnlineMenuAudioFixture.AssertCue(GameplayAudioPool.MenuHover);
        view.editSettings.onClick.Invoke();
        Assert.That(view.editSettingsPanel.activeInHierarchy, Is.True);
        OnlineMenuAudioFixture.AssertCue(GameplayAudioPool.MenuAdvance);

        yield return OnlineMenuAudioFixture.WaitForCueCooldown();
        Assert.That(view.editSettingsBackButton.GetComponents<MenuAudioFeedback>(), Has.Length.EqualTo(1));
        view.editSettingsBackButton.onClick.Invoke();
        Assert.That(view.pausePanel.activeInHierarchy, Is.True);
        OnlineMenuAudioFixture.AssertCue(GameplayAudioPool.MenuBack);

        yield return OnlineMenuAudioFixture.WaitForCueCooldown();
        var open = view.pausePanel.GetComponentInChildren<RulesViewerOpener>();
        open.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        Assert.That(RulesViewer.IsOpen, Is.True);
        OnlineMenuAudioFixture.AssertCue(GameplayAudioPool.MenuAdvance);

        yield return OnlineMenuAudioFixture.WaitForCueCooldown();
        player.voice.Stop();
        OnlineMenuAudioFixture.Hover(rules.closeButton);
        OnlineMenuAudioFixture.AssertCue(GameplayAudioPool.MenuHover);
        rules.closeButton.onClick.Invoke();
        Assert.That(RulesViewer.IsOpen, Is.False);
        Assert.That(view.pausePanel.activeInHierarchy, Is.True);
        OnlineMenuAudioFixture.AssertCue(GameplayAudioPool.MenuBack);
    }
}
