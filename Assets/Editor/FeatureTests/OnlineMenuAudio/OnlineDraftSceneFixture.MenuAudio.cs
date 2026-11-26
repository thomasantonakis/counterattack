using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;

public sealed partial class OnlineDraftSceneFixture
{
    public IEnumerator RunDraftMenuAudio() => Run(hostAway: false, localHost: true, branch: "menu-audio");

    private static IEnumerator CheckPageSounds(OnlineDraftView view) =>
        OnlineMenuAudioFixture.WithGameAudio(() => ExercisePaging(view));

    private static IEnumerator ExercisePaging(OnlineDraftView view)
    {
        Assert.That(view.Client.Capture().freeSelection, Is.True);
        Assert.That(view.nextPageButton.IsInteractable(), Is.True);
        Assert.That(view.previousPageButton.IsInteractable(), Is.False);
        var player = MenuAudioPlayer.Instance;
        player.voice.Stop();
        int previousCue = player.LastPlayedFrame;
        view.previousPageButton.onClick.Invoke();
        Assert.That(player.LastPlayedFrame, Is.EqualTo(previousCue), "An unchanged page plays no back cue.");

        OnlineMenuAudioFixture.Hover(view.nextPageButton);
        OnlineMenuAudioFixture.AssertCue(GameplayAudioPool.MenuHover);
        view.nextPageButton.onClick.Invoke();
        OnlineMenuAudioFixture.AssertCue(GameplayAudioPool.MenuAdvance);
        Assert.That(view.previousPageButton.IsInteractable(), Is.True);

        yield return OnlineMenuAudioFixture.WaitForCueCooldown();
        view.previousPageButton.onClick.Invoke();
        OnlineMenuAudioFixture.AssertCue(GameplayAudioPool.MenuBack);
        Assert.That(view.previousPageButton.IsInteractable(), Is.False);
        foreach (var button in new[] { view.kickoffChoiceButton, view.leftChoiceButton, view.rightChoiceButton, view.leaveButton })
            Assert.That(button.GetComponents<MenuAudioFeedback>().Single().buttonSelection, Is.False, button.name);
    }
}
