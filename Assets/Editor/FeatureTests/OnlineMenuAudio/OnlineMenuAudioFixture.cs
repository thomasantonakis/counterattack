using System;
using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class OnlineMenuAudioFixture
{
    private const string GamePreference = "CounterAttack.Audio.Game.Enabled";

    public static IEnumerator WithGameAudio(Func<IEnumerator> run)
    {
        var preferences = GameplayAudioPreferences.Local;
        bool previous = preferences.Enabled(GameplayAudioCategory.Game);
        bool hadPreference = PlayerPrefs.HasKey(GamePreference);
        var player = MenuAudioPlayer.Instance;
        bool previousMute = player != null && player.voice.mute;
        try
        {
            preferences.SetEnabled(GameplayAudioCategory.Game, true);
            Assert.That(player, Is.Not.Null, "The authored scene must provide menu audio.");
            player.voice.mute = true;
            yield return run();
        }
        finally
        {
            if (player != null) player.voice.mute = previousMute;
            preferences.SetEnabled(GameplayAudioCategory.Game, previous);
            if (!hadPreference) PlayerPrefs.DeleteKey(GamePreference);
            PlayerPrefs.Save();
        }
    }

    public IEnumerator LobbyAndSaveBrowser()
    {
        Assert.That(OnlineFrontendSession.Current, Is.Null);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/OnlineModeMenu.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return WithGameAudio(ExerciseLobby);
    }

    private static IEnumerator ExerciseLobby()
    {
        yield return WaitForCueCooldown();
        var menu = Object.FindAnyObjectByType<OnlineModeMenu>();
        var copy = menu.copyButton;
        // This control is normally shown only after hosting. The copy listener
        // leaves the clipboard alone while there is no lobby code.
        copy.gameObject.SetActive(true);
        copy.interactable = true;
        var player = MenuAudioPlayer.Instance;
        player.voice.Stop();
        Hover(copy);
        AssertCue(GameplayAudioPool.MenuHover);
        yield return null;
        copy.gameObject.SetActive(true);
        copy.interactable = true;
        copy.onClick.Invoke();
        copy.GetComponent<MenuAudioFeedback>().FlushPendingSelection();
        AssertCue(GameplayAudioPool.MenuSelect);

        yield return null;
        menu.lobbyCode.SetTextWithoutNotify("bad");
        var join = menu.joinButton;
        Assert.That(join.GetComponent<MenuAudioFeedback>().buttonSelection, Is.False);
        int priorCue = player.LastPlayedFrame;
        join.onClick.Invoke();
        Assert.That(player.LastPlayedFrame, Is.EqualTo(priorCue));
        Assert.That(OnlineFrontendSession.Current, Is.Null);

        menu.loadMatchButton.onClick.Invoke();
        AssertCue(GameplayAudioPool.MenuAdvance);
        yield return WaitForScene("CreateLoadRoom");

        var cancel = Buttons().Single(button => button.name == "CancelButton" && button.gameObject.activeInHierarchy);
        Assert.That(cancel.GetComponents<MenuAudioFeedback>(), Has.Length.EqualTo(1));
        yield return WaitForCueCooldown();
        cancel.onClick.Invoke();
        AssertCue(GameplayAudioPool.MenuBack);
        yield return WaitForScene(OnlineModeMenu.SceneName);
        Assert.That(MenuAudioPlayer.Instance, Is.SameAs(player));

        yield return WaitForCueCooldown();
        Object.FindAnyObjectByType<OnlineModeMenu>().Back();
        AssertCue(GameplayAudioPool.MenuBack);
        yield return WaitForScene("MainMenu");
        yield return WaitForCueCooldown();
        var multiplayer = Buttons().Single(button => button.gameObject.activeInHierarchy
            && Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Any(index =>
                button.onClick.GetPersistentMethodName(index) == nameof(MainMenuManager.PlayMultiPlayerGame)));
        multiplayer.onClick.Invoke();
        AssertCue(GameplayAudioPool.MenuAdvance);
        yield return WaitForScene(OnlineModeMenu.SceneName);
    }

    public IEnumerator Cleanup()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
    }

    public static void Hover(Button button)
    {
        Assert.That(button.GetComponents<MenuAudioFeedback>(), Has.Length.EqualTo(1), button.name);
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
    }

    public static void AssertCue(GameplayAudioPool cue)
    {
        var player = MenuAudioPlayer.Instance;
        Assert.That(player.voice.clip, Is.SameAs(player.library.Get(cue).Single()), cue.ToString());
        Assert.That(player.LastPlayedFrame, Is.EqualTo(Time.frameCount), cue.ToString());
    }

    public static IEnumerator WaitForCueCooldown()
    {
        double deadline = Time.unscaledTimeAsDouble + .45;
        while (Time.unscaledTimeAsDouble < deadline) yield return null;
    }

    private static Button[] Buttons() => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<Button>(true)).ToArray();

    private static IEnumerator WaitForScene(string name)
    {
        float deadline = Time.realtimeSinceStartup + 10f;
        while (SceneManager.GetActiveScene().name != name && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(name));
    }
}
