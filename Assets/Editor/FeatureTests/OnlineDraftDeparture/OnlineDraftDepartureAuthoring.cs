using System;
using System.Linq;
using CounterAttack.Online;
using TMPro;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class OnlineDraftDepartureAuthoring
{
    [CliCommand("author_online_draft_departure", "Wire Arcade pool drops and selectable online lobby codes")]
    public static string Prepare()
    {
        var original = SceneManager.GetActiveScene();
        string originalPath = original.path;
        if (EditorApplication.isPlayingOrWillChangePlaymode || original.isDirty || SceneManager.sceneCount != 1)
            throw new InvalidOperationException("Expected one clean stopped scene");

        try
        {
            var draft = EditorSceneManager.OpenScene("Assets/Scenes/Draft.unity", OpenSceneMode.Single);
            var view = Object.FindAnyObjectByType<OnlineDraftView>();
            var dropTarget = view.poolTable.GetComponent<OnlineDraftPoolDropTarget>()
                ?? view.poolTable.AddComponent<OnlineDraftPoolDropTarget>();
            dropTarget.view = view;
            var surface = view.poolTable.GetComponent<Image>() ?? view.poolTable.AddComponent<Image>();
            surface.raycastTarget = true;
            if (surface.sprite == null) surface.color = Color.clear;
            Save(draft);

            var setup = EditorSceneManager.OpenScene("Assets/Scenes/CreateNewGameScene.unity", OpenSceneMode.Single);
            var network = Object.FindAnyObjectByType<OnlineSetupNetworkPanel>();
            var header = network.panel.transform.parent;
            network.sessionLobbyCode = CreateCode(network.sessionLobbyCode, network.lobbyCode, header);
            var layout = network.sessionLobbyCode.GetComponent<LayoutElement>();
            layout.ignoreLayout = false;
            layout.minHeight = layout.preferredHeight = 34;
            layout.preferredWidth = 230;
            network.sessionLobbyCode.transform.SetSiblingIndex(network.panel.transform.GetSiblingIndex());
            Save(setup);

            var room = EditorSceneManager.OpenScene("Assets/Scenes/Room.unity", OpenSceneMode.Single);
            var pause = Object.FindAnyObjectByType<PauseMenuManager>(FindObjectsInactive.Include);
            var pauseCard = pause.pausePanel.transform.GetChild(0);
            var template = pause.saveNameInput;
            if (template == null)
                template = room.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TMP_InputField>(true))
                    .Single(field => field.name == "SaveNameInput");
            pause.onlineLobbyCode = CreateCode(pause.onlineLobbyCode, template, pauseCard);
            var codeRect = (RectTransform)pause.onlineLobbyCode.transform;
            codeRect.anchorMin = codeRect.anchorMax = new Vector2(.5f, 1);
            codeRect.pivot = new Vector2(.5f, 1);
            codeRect.anchoredPosition = new Vector2(0, -10);
            codeRect.sizeDelta = new Vector2(230, 40);
            Save(room);
            return "Authored the pool drop target and both lobby code fields";
        }
        finally
        {
            EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
        }
    }

    private static TMP_InputField CreateCode(TMP_InputField field, TMP_InputField template, Transform parent)
    {
        field ??= Object.Instantiate(template, parent);
        field.name = "OnlineLobbyCode";
        field.onValueChanged = new TMP_InputField.OnChangeEvent();
        field.onEndEdit = new TMP_InputField.SubmitEvent();
        field.onSubmit = new TMP_InputField.SubmitEvent();
        field.readOnly = true;
        field.interactable = true;
        field.characterLimit = OnlineSpectatorAdmission.CodeLength;
        field.onFocusSelectAll = true;
        field.transition = Selectable.Transition.None;
        field.textComponent.color = Color.white;
        field.textComponent.alignment = TextAlignmentOptions.Center;
        field.textComponent.enableAutoSizing = false;
        field.textComponent.fontSize = 24;
        if (field.placeholder != null) field.placeholder.enabled = false;
        if (field.targetGraphic != null) field.targetGraphic.color = new Color32(12, 38, 60, 255);
        field.SetTextWithoutNotify(string.Empty);
        field.gameObject.SetActive(false);
        return field;
    }

    private static void Save(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + scene.path);
    }
}
