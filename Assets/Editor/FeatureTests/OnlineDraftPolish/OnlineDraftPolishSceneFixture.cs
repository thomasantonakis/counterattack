using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class OnlineDraftPolishSceneFixture
{
    private ApplicationManager app;
    private OnlineFrontendSession session;
    private MenuAudioPlaybackFixture audio;
    private bool createdApp, hadSave;
    private string previousMode, previousSave;

    public IEnumerator RunRollAudio(bool asHost)
    {
        audio = new MenuAudioPlaybackFixture();
        var link = new OnlineDraftReturnTransport(timed: true);
        yield return Load("Draft", asHost ? link.Host : link.Guest);
        var view = Object.FindAnyObjectByType<OnlineDraftView>();
        yield return VerifyRolls(link, view.source.draftOrderPanel, opening: false);

        while (link.Host.Draft.Phase == "pick") link.Pick();
        link.Request(link.Host.Draft, OnlineDraftIntent.Ready);
        link.Request(link.Guest.Draft, OnlineDraftIntent.Ready);
        link.Request(link.Host.Draft, OnlineDraftIntent.Start);
        yield return VerifyRolls(link, view.sourceUi.coinTossPanel.dicePanel, opening: true);
        var opening = link.Host.Draft.Capture();
        Assert.That(opening.openingWinnerIsHome && opening.openingCanChooseKickoff, Is.True);
        Assert.That(view.sourceUi.coinTossPanel.kickoffButton.interactable, Is.EqualTo(asHost));
    }

    private static IEnumerator VerifyRolls(OnlineDraftReturnTransport link, DraftOrderPanelController dice, bool opening)
    {
        var sound = MenuAudioPlayer.Instance;
        yield return WaitForWiggle(sound);
        Assert.That(dice.homeDie.IsAnimating, Is.True);
        link.QueueRolls(6, 1);
        link.Request(link.Host.Draft, OnlineDraftIntent.Roll);
        Assert.That(dice.homeDie.DisplayedRoll, Is.EqualTo(6));
        Assert.That(dice.awayDie.IsAnimating, Is.True);
        Assert.That(sound.library.Get(GameplayAudioPool.Dice), Does.Contain(sound.voice.clip));
        Assert.That(sound.wiggleVoice.isPlaying, Is.False);

        yield return WaitForWiggle(sound);
        link.Request(link.Guest.Draft, OnlineDraftIntent.Roll);
        Assert.That(dice.awayDie.DisplayedRoll, Is.EqualTo(1));
        Assert.That(sound.wiggleVoice.isPlaying, Is.False);
        Assert.That(sound.LastPlayedFrame, Is.EqualTo(Time.frameCount));
        Assert.That(dice.footer.text, Is.EqualTo(opening ? "Choices open in 2 seconds" : "Draft begins in 2 seconds"));
        link.Now += 1.99;
        link.Host.Tick();
        link.Flush();
        Assert.That(link.Guest.Draft.Phase, Is.EqualTo(opening ? "opening-result" : "roll-result"));
        link.Now += .02;
        link.Host.Tick();
        link.Flush();
        Assert.That(link.Guest.Draft.Phase, Is.EqualTo(opening ? "opening-choice" : "pick"));
    }

    private static IEnumerator WaitForWiggle(MenuAudioPlayer sound)
    {
        float deadline = Time.realtimeSinceStartup + 2;
        while (!sound.wiggleVoice.isPlaying && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(sound.wiggleVoice.isPlaying, Is.True);
        Assert.That(sound.wiggleVoice.clip, Is.SameAs(sound.library.Get(GameplayAudioPool.DiceWiggle)[0]));
    }

    public IEnumerator RunKeeperRatings()
    {
        var settings = OnlineSetupAgreementEditorTests.Valid();
        settings.gkDraft = "Arcade";
        var link = new OnlineDraftReturnTransport(settings);
        yield return Load("Draft", link.Host);
        var view = Object.FindAnyObjectByType<OnlineDraftView>();
        Assert.That(view.poolHeaders[3].interactable && view.poolFilters[3].interactable, Is.True);
        var initial = view.poolRows.Where(row => row.PoolCard != null).ToArray();
        Assert.That(initial.Select(row => row.poolCells[3].text),
            Is.EqualTo(initial.Select(row => row.PoolCard.dribbling.ToString())));
        Assert.That(initial.Select(row => row.PoolCard.dribbling), Does.Contain(1));
        view.poolHeaders[3].onClick.Invoke();
        view.poolFilters[3].text = ">=2";
        var filtered = view.poolRows.Where(row => row.PoolCard != null).Select(row => row.PoolCard.dribbling).ToArray();
        Assert.That(filtered, Is.Not.Empty);
        Assert.That(filtered, Is.EqualTo(filtered.OrderByDescending(value => value)));
        Assert.That(filtered.All(value => value >= 2), Is.True);
    }

    public IEnumerator RunDeparture(bool inDraft)
    {
        var link = new OnlineDraftReturnTransport(inDraft ? OnlineSetupAgreementEditorTests.Valid() : OnlineModeMenu.HostDefaults(),
            startDraft: inDraft);
        yield return Load(inDraft ? "Draft" : "CreateNewGameScene", link.Host);
        if (!inDraft)
        {
            var setup = Object.FindAnyObjectByType<OnlineSetupNetworkPanel>();
            foreach (var field in new[] { setup.setup.homeTeamInputField, setup.setup.awayTeamInputField })
            {
                Assert.That(field.text, Is.Empty);
                Assert.That(field.placeholder.GetComponent<TMP_Text>().text, Is.Not.Empty);
            }
            Assert.That(setup.setup.createGameButton.interactable, Is.False);
        }
        var panel = Object.FindAnyObjectByType<OnlineConnectionPanel>();
        var rect = (RectTransform)panel.transform;
        var originalScale = rect.localScale;
        var originalSize = rect.rect.size;
        var label = panel.GetComponentInChildren<TMP_Text>();
        float originalFontSize = label.fontSize;
        Assert.That(rect.anchoredPosition, Is.Not.EqualTo(Vector2.zero));

        link.Guest.Leave();
        link.Flush();
        yield return null;
        Canvas.ForceUpdateCanvases();
        label.ForceMeshUpdate();
        Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(.5f, .5f)));
        Assert.That(rect.anchorMax, Is.EqualTo(rect.anchorMin));
        Assert.That(rect.anchoredPosition, Is.EqualTo(Vector2.zero));
        Assert.That(rect.rect.size, Is.EqualTo(originalSize));
        Assert.That(rect.localScale, Is.EqualTo(originalScale * 3));
        Assert.That(panel.GetComponent<Image>().color, Is.EqualTo((Color)new Color32(135, 28, 37, 255)));
        Assert.That(label.textInfo.lineCount, Is.EqualTo(3));
        Assert.That(label.fontSize * 3, Is.GreaterThan(originalFontSize));
        Assert.That(label.text.Split('\n')[2], Is.EqualTo("Press Leave session to exit."));
        var leave = inDraft ? Object.FindAnyObjectByType<OnlineDraftView>().leaveButton
            : Object.FindAnyObjectByType<OnlineSetupNetworkPanel>().leaveButton;
        Assert.That(leave.interactable, Is.True);
    }

    private IEnumerator Load(string scene, OnlineSessionChannel channel)
    {
        Assert.That(OnlineFrontendSession.Current, Is.Null);
        createdApp = ApplicationManager.Instance == null;
        ApplicationManager.EnsureInstanceExists();
        app = ApplicationManager.Instance;
        previousMode = app.SelectedRoomGameMode;
        previousSave = app.LastSavedFileName;
        hadSave = app.HasExplicitSaveContext;
        app.SetSelectedRoomGameMode(ApplicationManager.OnlineGameMode);
        session = new GameObject("Draft presentation test session").AddComponent<OnlineFrontendSession>();
        typeof(OnlineFrontendSession).GetField("channel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(session, channel);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode($"Assets/Scenes/{scene}.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
    }

    public IEnumerator Cleanup()
    {
        if (session != null) session.Close();
        if (app != null)
        {
            app.SetSelectedRoomGameMode(previousMode);
            if (hadSave) app.SetActiveSaveFilePath(previousSave);
            else { app.ClearActiveSaveContext(); app.LastSavedFileName = previousSave; }
        }
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity", new LoadSceneParameters(LoadSceneMode.Single));
        if (audio != null) yield return audio.Cleanup();
        if (createdApp && app != null) Object.Destroy(app.gameObject);
        yield return null;
    }
}
