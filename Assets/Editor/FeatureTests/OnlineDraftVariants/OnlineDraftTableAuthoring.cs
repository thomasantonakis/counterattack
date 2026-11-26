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

public static class OnlineDraftTableAuthoring
{
    [CliCommand("author_online_draft_table", "Copy the FreeDraft table into the online Draft scene and wire its controls")]
    public static string Prepare()
    {
        var original = SceneManager.GetActiveScene();
        string originalPath = original.path;
        if (EditorApplication.isPlayingOrWillChangePlaymode || original.isDirty || SceneManager.sceneCount != 1)
            throw new InvalidOperationException("Expected one clean stopped scene");

        string temporary = AssetDatabase.GenerateUniqueAssetPath("Assets/TemporaryOnlineDraftTable.prefab");
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/FreeDraft.unity", OpenSceneMode.Single);
            var reference = Object.FindAnyObjectByType<DraftManager>().draftPanel.transform.Find("FreeDraftTable");
            if (PrefabUtility.SaveAsPrefabAsset(reference.gameObject, temporary) == null)
                throw new InvalidOperationException("Could not copy the authored FreeDraft table");

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Draft.unity", OpenSceneMode.Single);
            var view = Object.FindAnyObjectByType<OnlineDraftView>();
            if (view.poolTable != null) throw new InvalidOperationException("The online Draft table is already authored");
            var cardPanel = view.cards[0].transform.parent;
            var table = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(temporary), cardPanel);
            PrefabUtility.UnpackPrefabInstance(table, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            table.name = "OnlineFreeDraftTable";
            var tableRect = (RectTransform)table.transform;
            tableRect.offsetMin = new Vector2(0, 60);
            tableRect.offsetMax = Vector2.zero;

            foreach (var filter in table.GetComponentsInChildren<FreeDraftTableFilterField>(true)) Object.DestroyImmediate(filter);
            var headerRow = table.transform.Find("HeaderRow");
            var filterRow = table.transform.Find("FilterRow");
            view.poolHeaders = headerRow.Cast<Transform>().Select(child => child.GetComponent<Button>()).ToArray();
            view.poolFilters = filterRow.Cast<Transform>().Select(child => child.GetComponentInChildren<TMP_InputField>(true)).ToArray();
            if (view.poolHeaders.Length != 10 || view.poolFilters.Length != 10
                || view.poolHeaders.Any(button => button == null) || view.poolFilters.Any(filter => filter == null))
                throw new InvalidOperationException("The table should have ten sort buttons and ten input fields");
            foreach (var button in view.poolHeaders) button.onClick = new Button.ButtonClickedEvent();
            foreach (var filter in view.poolFilters)
            {
                filter.onValueChanged = new TMP_InputField.OnChangeEvent();
                filter.SetTextWithoutNotify(string.Empty);
            }

            view.poolScroll = table.GetComponentInChildren<ScrollRect>(true);
            var content = view.poolScroll.content;
            var template = content.Find("PreviewRow").gameObject;
            OnlineDraftClickTarget ConfigureRow(GameObject row)
            {
                var target = row.AddComponent<OnlineDraftClickTarget>();
                target.view = view;
                target.poolCells = row.transform.Cast<Transform>().Select(cell => cell.GetComponentInChildren<TMP_Text>(true)).ToArray();
                if (target.poolCells.Length != 10 || target.poolCells.Any(cell => cell == null))
                    throw new InvalidOperationException("The row should have ten text cells");
                if (!row.TryGetComponent<CanvasGroup>(out _)) row.AddComponent<CanvasGroup>();
                return target;
            }

            var ghost = Object.Instantiate(template, view.GetComponentInParent<Canvas>()?.transform ?? cardPanel.parent);
            ghost.name = "OnlinePoolDragGhost";
            view.poolDragGhost = ConfigureRow(ghost);
            view.poolDragGhost.enabled = false;
            var ghostGroup = ghost.GetComponent<CanvasGroup>();
            ghostGroup.blocksRaycasts = false;
            ghostGroup.interactable = false;
            var ghostRect = (RectTransform)ghost.transform;
            ghostRect.anchorMin = ghostRect.anchorMax = new Vector2(.5f, .5f);
            ghost.SetActive(false);

            view.poolRows = new OnlineDraftClickTarget[8];
            for (int i = view.poolRows.Length - 1; i >= 0; i--)
            {
                var row = i == 0 ? template : Object.Instantiate(template, content);
                row.name = $"PoolRow{i + 1}";
                row.transform.SetSiblingIndex(0);
                view.poolRows[i] = ConfigureRow(row);
                row.SetActive(false);
            }

            foreach (var button in new[] { view.previousPageButton, view.nextPageButton })
            {
                var rect = (RectTransform)button.transform;
                rect.SetParent(tableRect, false);
                bool previous = button == view.previousPageButton;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
                rect.anchoredPosition = new Vector2(previous ? -110 : 110, -30);
                rect.sizeDelta = new Vector2(200, 48);
            }
            view.poolTable = table;
            table.SetActive(false);
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Draft");
            return "Authored the online pool table, eight rows and drag ghost";
        }
        finally
        {
            AssetDatabase.DeleteAsset(temporary);
            if (!string.IsNullOrEmpty(originalPath)) EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
        }
    }
}
