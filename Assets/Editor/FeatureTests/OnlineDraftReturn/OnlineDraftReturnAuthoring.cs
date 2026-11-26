using System;
using CounterAttack.Online;
using TMPro;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OnlineDraftReturnAuthoring
{
    [CliCommand("author_online_draft_return", "Author Back to Match Setup beside Leave session")]
    public static string Prepare()
    {
        var previous = SceneManager.GetActiveScene();
        string previousPath = previous.path;
        const string draftPath = "Assets/Scenes/Draft.unity";
        if (EditorApplication.isPlayingOrWillChangePlaymode || previous.isDirty || SceneManager.sceneCount != 1)
            throw new InvalidOperationException("Expected one clean stopped scene");
        try
        {
            var scene = previousPath == draftPath ? previous : EditorSceneManager.OpenScene(draftPath, OpenSceneMode.Single);
            var view = UnityEngine.Object.FindAnyObjectByType<OnlineDraftView>();
            if (view.backToMatchSetupButton != null) return "Back to Match Setup is already authored";

            var leave = view.leaveButton;
            var button = UnityEngine.Object.Instantiate(leave, leave.transform.parent);
            button.name = "OnlineDraftBackToMatchSetup";
            button.onClick = new Button.ButtonClickedEvent();
            var label = button.GetComponentInChildren<TMP_Text>(true);
            label.text = "Back to Match Setup";
            var rect = (RectTransform)button.transform;
            var leaveRect = (RectTransform)leave.transform;
            rect.anchoredPosition = leaveRect.anchoredPosition + new Vector2(leaveRect.sizeDelta.x + 40, 0);
            rect.sizeDelta = new Vector2(Mathf.Ceil(label.GetPreferredValues(label.text).x) + 32, leaveRect.sizeDelta.y);
            button.interactable = false;
            button.gameObject.SetActive(false);
            view.backToMatchSetupButton = button;
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Draft");
            return "Authored Back to Match Setup beside Leave session";
        }
        finally
        {
            if (!string.IsNullOrEmpty(previousPath) && previousPath != draftPath)
                EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
        }
    }
}
