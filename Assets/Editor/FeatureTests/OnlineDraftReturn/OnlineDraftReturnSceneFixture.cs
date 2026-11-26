using System;
using System.Collections;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed class OnlineDraftReturnSceneFixture
{
    private ApplicationManager app;
    private OnlineFrontendSession session;
    private bool created, hadSave;
    private string previousMode, previousSave;

    public IEnumerator Run(bool asHost)
    {
        Assert.That(OnlineFrontendSession.Current, Is.Null);
        created = ApplicationManager.Instance == null;
        ApplicationManager.EnsureInstanceExists();
        app = ApplicationManager.Instance;
        previousMode = app.SelectedRoomGameMode;
        previousSave = app.LastSavedFileName;
        hadSave = app.HasExplicitSaveContext;
        app.SetSelectedRoomGameMode(ApplicationManager.OnlineGameMode);

        var link = new OnlineDraftReturnTransport();
        link.Roll();
        link.Pick();
        OnlineSessionChannel channel = asHost ? link.Host : link.Guest;
        session = new GameObject("Draft return test session").AddComponent<OnlineFrontendSession>();
        const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic;
        var type = typeof(OnlineFrontendSession);
        type.GetField("channel", members).SetValue(session, channel);
        var onAgreed = (Action<GameSettings>)type.GetMethod("OnAgreed", members).CreateDelegate(typeof(Action<GameSettings>), session);
        channel.Client.DraftAgreed += onAgreed;

        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Draft.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        var view = Object.FindAnyObjectByType<OnlineDraftView>();
        var button = view.backToMatchSetupButton;
        Assert.That(view.Client, Is.SameAs(channel.Draft));
        Assert.That(button.isActiveAndEnabled && button.interactable, Is.True);
        Assert.That(button.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Back to Match Setup"));
        var backRect = (RectTransform)button.transform;
        var leave = (RectTransform)view.leaveButton.transform;
        Assert.That(backRect.anchoredPosition.y, Is.EqualTo(leave.anchoredPosition.y));
        Assert.That(backRect.anchoredPosition.x, Is.GreaterThanOrEqualTo(leave.anchoredPosition.x + leave.rect.width));
        var priorDraft = session.Draft;
        var settings = channel.Client.ViewSettings();

        button.onClick.Invoke();
        link.Flush();
        yield return WaitForScene("CreateNewGameScene");
        yield return null;
        var setup = Object.FindAnyObjectByType<OnlineSetupNetworkPanel>();
        Assert.That(OnlineFrontendSession.Current, Is.SameAs(session));
        Assert.That(setup.setup.homeTeamInputField.text, Is.EqualTo(settings.homeTeamName));
        Assert.That(setup.setup.awayTeamInputField.text, Is.EqualTo(settings.awayTeamName));
        Assert.That(setup.setup.createGameButton.interactable, Is.True);
        Assert.That(channel.Client.BothReady, Is.False);
        Assert.That(app.LastSavedFileName, Is.EqualTo(previousSave));

        link.Agree();
        yield return WaitForScene("Draft");
        yield return null;
        var fresh = Object.FindAnyObjectByType<OnlineDraftView>();
        Assert.That(fresh.Client, Is.Not.SameAs(priorDraft));
        Assert.That(fresh.Client.Capture().home[1], Is.Null);
        Assert.That(fresh.backToMatchSetupButton.interactable, Is.True);
        channel.Client.DraftAgreed -= onAgreed;
    }

    private static IEnumerator WaitForScene(string name)
    {
        float deadline = Time.realtimeSinceStartup + 10;
        while (SceneManager.GetActiveScene().name != name && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(name));
    }

    public IEnumerator Cleanup()
    {
        if (session != null) session.Close();
        if (app == null) yield break;
        app.SetSelectedRoomGameMode(previousMode);
        if (hadSave) app.SetActiveSaveFilePath(previousSave);
        else { app.ClearActiveSaveContext(); app.LastSavedFileName = previousSave; }
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity", new LoadSceneParameters(LoadSceneMode.Single));
        if (created) Object.Destroy(app.gameObject);
        yield return null;
    }
}
