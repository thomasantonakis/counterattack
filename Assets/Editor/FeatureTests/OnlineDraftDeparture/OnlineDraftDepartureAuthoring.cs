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

        string temporary = AssetDatabase.GenerateUniqueAssetPath("Assets/TemporaryOnlineLobbyCode.prefab");
        try
        {
            var room = EditorSceneManager.OpenScene("Assets/Scenes/Room.unity", OpenSceneMode.Single);
            var pause = Object.FindAnyObjectByType<PauseMenuManager>(FindObjectsInactive.Include);
            if (pause.onlineLobbyCode == null)
            {
                var pauseCard = pause.pausePanel.transform.GetChild(0);
                var template = pause.saveNameInput ?? room.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<TMP_InputField>(true))
                    .Single(field => field.name == "SaveNameInput");
                pause.onlineLobbyCode = CreateCode(null, template, pauseCard);
                var codeRect = (RectTransform)pause.onlineLobbyCode.transform;
                codeRect.anchorMin = codeRect.anchorMax = new Vector2(.5f, 1);
                codeRect.pivot = new Vector2(.5f, 1);
                codeRect.anchoredPosition = new Vector2(0, -10);
                codeRect.sizeDelta = new Vector2(230, 40);
                Save(room);
            }
            var roomCode = PrefabUtility.SaveAsPrefabAsset(pause.onlineLobbyCode.gameObject, temporary);
            if (roomCode == null) throw new InvalidOperationException("Could not copy the Room lobby code");

            var draft = EditorSceneManager.OpenScene("Assets/Scenes/Draft.unity", OpenSceneMode.Single);
            var view = Object.FindAnyObjectByType<OnlineDraftView>();
            var dropTarget = view.poolTable.GetComponent<OnlineDraftPoolDropTarget>()
                ?? view.poolTable.AddComponent<OnlineDraftPoolDropTarget>();
            dropTarget.view = view;
            var surface = view.poolTable.GetComponent<Image>() ?? view.poolTable.AddComponent<Image>();
            surface.raycastTarget = true;
            if (surface.sprite == null) surface.color = Color.clear;
            view.sessionLobbyCode = PlaceTitleCode(view.sessionLobbyCode, roomCode, view.sourceUi.draftTurnText);
            Save(draft);

            var setup = EditorSceneManager.OpenScene("Assets/Scenes/CreateNewGameScene.unity", OpenSceneMode.Single);
            var network = Object.FindAnyObjectByType<OnlineSetupNetworkPanel>();
            var title = network.panel.transform.parent.Find("Title").GetComponent<TMP_Text>();
            network.sessionLobbyCode = PlaceTitleCode(network.sessionLobbyCode, roomCode, title);
            network.backButton = setup.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Button>(true))
                .Single(button => Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                    .Any(index => button.onClick.GetPersistentTarget(index) == network.setup
                        && button.onClick.GetPersistentMethodName(index) == nameof(CreateNewGameManager.BackToCreateLoadRoomMenu)));
            Save(setup);
            return "Placed matching Room-style lobby code fields beside both titles and wired the setup Back button";
        }
        finally
        {
            AssetDatabase.DeleteAsset(temporary);
            EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
        }
    }

    private static TMP_InputField PlaceTitleCode(TMP_InputField oldField, GameObject roomCode, TMP_Text title)
    {
        if (oldField != null) Object.DestroyImmediate(oldField.gameObject);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(roomCode, title.transform);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        var field = instance.GetComponent<TMP_InputField>();
        var codeRect = (RectTransform)instance.transform;
        codeRect.anchorMin = codeRect.anchorMax = Vector2.one * .5f;
        codeRect.pivot = new Vector2(0, .5f);
        var sampleSize = field.textComponent.GetPreferredValues(new string('0', OnlineSpectatorAdmission.CodeLength));
        var padding = OnlineLobbyCodeTitle.Padding;
        codeRect.sizeDelta = new Vector2(Mathf.Ceil(sampleSize.x), Mathf.Ceil(sampleSize.y)) + 2 * padding;
        codeRect.localPosition = new Vector3(title.GetPreferredValues().x * .5f + 16, 0);
        var viewport = field.textViewport;
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = padding;
        viewport.offsetMax = -padding;
        field.textComponent.margin = Vector4.zero;
        instance.AddComponent<OnlineLobbyCodeTitle>().title = title;
        return field;
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
