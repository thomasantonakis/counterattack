using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class OnlineFullRoomSceneFixture
{
    private const BindingFlags ConfirmationFields = BindingFlags.Instance | BindingFlags.NonPublic;

    public IEnumerator RunOnlineMenuExitConfirmation()
    {
        InitializeFixture();
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/OnlineModeMenu.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        var menu = Object.FindAnyObjectByType<OnlineModeMenu>();
        var roots = menu.gameObject.scene.GetRootGameObjects();
        var exit = roots.SelectMany(root => root.GetComponentsInChildren<Button>(true)).Single(button =>
            Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Any(index =>
                button.onClick.GetPersistentTarget(index) == menu
                && button.onClick.GetPersistentMethodName(index) == nameof(OnlineModeMenu.Exit)));
        var overlay = roots.SelectMany(root => root.GetComponentsInChildren<MenuConfirmation>(true)).Single();
        Assert.That(AssetDatabase.GetDependencies(menu.gameObject.scene.path, recursive: false),
            Does.Contain("Assets/Resources/UI/Common/QuitGameConfirmation.prefab"));
        Assert.That(overlay.GetComponentsInChildren<TMPro.TMP_Text>(true).Select(label => label.text),
            Does.Contain("Are you sure you want to quit?"));
        Assert.That(overlay.IsOpen, Is.False);

        exit.onClick.Invoke();
        Assert.That(overlay.IsOpen, Is.True);
        GetConfirmationButton(overlay, "backButton").onClick.Invoke();
        Assert.That(overlay.IsOpen, Is.False);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(OnlineModeMenu.SceneName));
    }

    public IEnumerator RunOnlinePauseExitConfirmation(string boundary) =>
        RunCheckpointRoundTrip(homeAttacks: true, afterMovement: false, finishReport: false,
            expectHistory: false, boundary: boundary);

    private IEnumerator VerifyOnlinePauseLeave(OnlineGroundPassBoard board, PauseMenuManager view,
        OnlineCheckpointStore store, string slotFolder, string boundary)
    {
        var overlay = GetLeaveConfirmation(view);
        view.backToMainMenuButton.onClick.Invoke();
        Assert.That(overlay.IsOpen, Is.True);
        Assert.That(overlay.transform.GetSiblingIndex(), Is.EqualTo(overlay.transform.parent.childCount - 1));
        board.controls.Escape();
        Assert.That(overlay.IsOpen, Is.False);
        Assert.That(pause.IsPaused, Is.True, "Cancelling must leave the match paused.");
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(OnlineRoomStartup.SceneName));

        if (boundary == "leave-without-saving")
        {
            view.backToMainMenuButton.onClick.Invoke();
            GetConfirmationButton(overlay, "alternativeButton").onClick.Invoke();
            yield return WaitForMainMenuAfterLeaving();
            Assert.That(Directory.Exists(slotFolder), Is.False);
            yield break;
        }

        void NameAndSave(string name)
        {
            view.saveNameInput.text = name;
            view.saveAsSaveButton.onClick.Invoke();
        }

        var confirm = GetConfirmationButton(overlay, "confirmButton");
        string slotName = "Exit confirmation checkpoint";
        System.Guid? priorCheckpointId = null;
        if (boundary == "leave-existing")
        {
            view.saveMatchAsButton.onClick.Invoke();
            NameAndSave(slotName);
            priorCheckpointId = ReadCheckpoint().checkpointId;
        }

        view.backToMainMenuButton.onClick.Invoke();
        confirm.onClick.Invoke();
        if (boundary == "leave-new")
        {
            Assert.That(overlay.IsOpen, Is.False);
            Assert.That(view.saveAsOverlay.activeInHierarchy, Is.True);
            view.saveAsCancelButton.onClick.Invoke();
            Assert.That(view.pausePanel.activeInHierarchy, Is.True);
            Assert.That(pause.IsPaused, Is.True);

            view.backToMainMenuButton.onClick.Invoke();
            confirm.onClick.Invoke();
            NameAndSave(string.Empty);
            Assert.That(view.saveAsOverlay.activeInHierarchy, Is.True, "Invalid names must keep the save open.");
            Assert.That(Directory.Exists(slotFolder), Is.False);
            NameAndSave(slotName);
        }

        yield return WaitForMainMenuAfterLeaving();
        var saved = ReadCheckpoint();
        Assert.That(saved.displayName, Is.EqualTo(slotName));
        Assert.That(saved.match.runtimeSnapshot.stats.homeTeamStats.totalGoals, Is.EqualTo(1));
        if (priorCheckpointId.HasValue) Assert.That(saved.checkpointId, Is.Not.EqualTo(priorCheckpointId.Value));

        OnlineCheckpoint ReadCheckpoint()
        {
            var file = Directory.GetFiles(slotFolder, "*.json").Single();
            return store.Load(System.Guid.ParseExact(Path.GetFileNameWithoutExtension(file), "N"));
        }
    }

    private static MenuConfirmation GetLeaveConfirmation(PauseMenuManager view) =>
        (MenuConfirmation)typeof(PauseMenuManager).GetField("leaveMatchConfirmation", ConfirmationFields).GetValue(view);

    private static Button GetConfirmationButton(MenuConfirmation overlay, string field) =>
        (Button)typeof(MenuConfirmation).GetField(field, ConfirmationFields).GetValue(overlay);

    private static IEnumerator WaitForMainMenuAfterLeaving()
    {
        float deadline = Time.realtimeSinceStartup + 10f;
        while (SceneManager.GetActiveScene().name != "MainMenu" && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        Assert.That(OnlineRoomSession.Current, Is.Null);
    }
}
