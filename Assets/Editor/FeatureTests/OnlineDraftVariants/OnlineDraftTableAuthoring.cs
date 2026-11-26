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
            var referencePanel = (RectTransform)reference.parent;
            Vector2 panelSize = referencePanel.sizeDelta;
            Vector2 panelPosition = referencePanel.anchoredPosition;
            if (PrefabUtility.SaveAsPrefabAsset(reference.gameObject, temporary) == null)
                throw new InvalidOperationException("Could not copy the authored FreeDraft table");

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Draft.unity", OpenSceneMode.Single);
            var view = Object.FindAnyObjectByType<OnlineDraftView>();
            var cardPanel = view.cards[0].transform.parent;
            if (view.poolTable != null)
            {
                ApplyLayout(view, cardPanel.parent, panelSize, panelPosition);
                AuthorLocalPreview(view, cardPanel.parent);
                Save(scene, view);
                return "Applied the offline table geometry and spaced pool rows";
            }

            var table = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(temporary), cardPanel.parent);
            PrefabUtility.UnpackPrefabInstance(table, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            table.name = "OnlineFreeDraftTable";

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

            view.poolRows = new OnlineDraftClickTarget[8];
            for (int i = view.poolRows.Length - 1; i >= 0; i--)
            {
                var row = i == 0 ? template : Object.Instantiate(template, content);
                row.name = $"PoolRow{i + 1}";
                row.transform.SetSiblingIndex(0);
                view.poolRows[i] = ConfigureRow(row);
                row.SetActive(false);
            }

            view.poolTable = table;
            ApplyLayout(view, cardPanel.parent, panelSize, panelPosition);
            AuthorLocalPreview(view, cardPanel.parent);
            table.SetActive(false);
            Save(scene, view);
            return "Authored the online pool table, eight rows and matching drag cards";
        }
        finally
        {
            AssetDatabase.DeleteAsset(temporary);
            if (!string.IsNullOrEmpty(originalPath)) EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
        }
    }

    private static void ApplyLayout(OnlineDraftView view, Transform middlePanel, Vector2 panelSize, Vector2 panelPosition)
    {
        var table = (RectTransform)view.poolTable.transform;
        // The dealt-card grid sizes every direct child as a single card. The table owns its full panel.
        table.SetParent(middlePanel, false);
        table.anchorMin = table.anchorMax = table.pivot = new Vector2(.5f, .5f);
        table.anchoredPosition = panelPosition;
        table.sizeDelta = panelSize;

        var content = view.poolScroll.content;
        if (!content.TryGetComponent<VerticalLayoutGroup>(out var rows)) rows = content.gameObject.AddComponent<VerticalLayoutGroup>();
        rows.childAlignment = TextAnchor.UpperCenter;
        rows.spacing = 8;
        rows.childControlWidth = rows.childForceExpandWidth = true;
        rows.childControlHeight = rows.childForceExpandHeight = false;
        if (!content.TryGetComponent<ContentSizeFitter>(out var fitter)) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = (RectTransform)view.poolScroll.transform;
        scroll.anchorMin = Vector2.zero;
        scroll.anchorMax = Vector2.one;
        scroll.pivot = new Vector2(.5f, .5f);
        scroll.offsetMin = new Vector2(0, 74);
        scroll.offsetMax = new Vector2(0, -144);

        var header = view.poolHeaders[0].GetComponentInChildren<TMP_Text>(true);
        foreach (var button in new[] { view.previousPageButton, view.nextPageButton })
        {
            var rect = (RectTransform)button.transform;
            rect.SetParent(table, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = new Vector2(button == view.previousPageButton ? -128 : 128, 0);
            rect.sizeDelta = new Vector2(220, 56);

            var label = button.GetComponentInChildren<TMP_Text>(true);
            label.font = header.font;
            label.fontSharedMaterial = header.fontSharedMaterial;
            label.enableAutoSizing = false;
            label.fontSize = header.fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.margin = Vector4.zero;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        }
    }

    private static void AuthorLocalPreview(OnlineDraftView view, Transform middlePanel)
    {
        if (view.localPoolDragPreview != null) return;
        var oldRow = middlePanel.Find("OnlinePoolDragGhost");
        if (oldRow != null) Object.DestroyImmediate(oldRow.gameObject);

        var remote = view.freeDraftDragPreview;
        view.localPoolDragPreview = Object.Instantiate(remote, remote.transform.parent);
        view.localPoolDragPreview.name = "OnlineLocalPoolDragCard";
        view.localPoolDragPreview.gameObject.SetActive(false);
    }

    private static void Save(Scene scene, OnlineDraftView view)
    {
        EditorUtility.SetDirty(view);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Draft");
    }
}
