using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RoomSaveBrowserEditorTools
{
    private const string ScenePath = "Assets/Scenes/CreateLoadRoom.unity";

    [MenuItem("Tools/Counter Attack/Update Save Browser Delete Column")]
    public static void UpdateDeleteColumn() => AuthorDeleteColumn();

    public static object AuthorDeleteColumn()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring the browser.");
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            var manager = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CreateLoadRoomManager>(true)).Single();
            var serialized = new SerializedObject(manager);
            var header = (Transform)serialized.FindProperty("headerRow").objectReferenceValue;
            var preview = (GameObject)serialized.FindProperty("previewRow").objectReferenceValue;
            var overlay = (GameObject)serialized.FindProperty("loadBrowserOverlay").objectReferenceValue;
            if (header == null || preview == null || overlay == null)
                throw new InvalidOperationException("The existing load browser is not wired.");

            SetWidth(header.Find("TeamsColumn"), 520);
            SetWidth(preview.transform.Find("TeamsColumn"), 520);
            SetWidth(header.Find("TieColumn"), 128);
            SetWidth(preview.transform.Find("TieColumn"), 128);
            var headerLayout = header.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            headerLayout.padding.left = 24;

            Transform deleteHeader = header.Find("DeleteColumn");
            if (deleteHeader == null)
            {
                deleteHeader = UnityEngine.Object.Instantiate(header.Find("TieColumn"), header, false);
                deleteHeader.name = "DeleteColumn";
            }
            SetWidth(deleteHeader, 160);
            deleteHeader.GetComponent<TextMeshProUGUI>().text = "Delete";
            deleteHeader.SetSiblingIndex(header.Find("TieColumn").GetSiblingIndex() + 1);

            // Every instantiated row needs its own raycast surface. The old
            // preview had a Button but no Image; author that surface here.
            var rowImage = preview.GetComponent<UnityEngine.UI.Image>() ?? preview.AddComponent<UnityEngine.UI.Image>();
            rowImage.color = new Color(1, 1, 1, 0);
            rowImage.raycastTarget = true;
            var rowButton = preview.GetComponent<UnityEngine.UI.Button>();
            rowButton.targetGraphic = rowImage;
            rowButton.transition = UnityEngine.UI.Selectable.Transition.None;
            if (preview.TryGetComponent(out Animator animator)) animator.enabled = false;

            Transform deleteColumn = preview.transform.Find("DeleteColumn");
            if (deleteColumn == null)
            {
                var control = new GameObject("DeleteColumn", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(UnityEngine.UI.LayoutElement));
                control.layer = preview.layer;
                deleteColumn = control.transform;
                deleteColumn.SetParent(preview.transform, false);
                var label = UnityEngine.Object.Instantiate(preview.transform.Find("TieColumn").GetComponent<TextMeshProUGUI>(), deleteColumn, false);
                label.name = "Label";
                if (label.TryGetComponent(out UnityEngine.UI.LayoutElement oldLayout)) UnityEngine.Object.DestroyImmediate(oldLayout);
                RectTransform textRect = label.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(8, 4);
                textRect.offsetMax = new Vector2(-8, -4);
                label.text = "Delete";
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                label.raycastTarget = false;
            }
            SetWidth(deleteColumn, 160);
            var element = deleteColumn.GetComponent<UnityEngine.UI.LayoutElement>();
            element.minHeight = element.preferredHeight = 56;
            element.flexibleHeight = 0;
            var image = deleteColumn.GetComponent<UnityEngine.UI.Image>();
            image.color = Color.white;
            image.raycastTarget = true;
            var delete = deleteColumn.GetComponent<UnityEngine.UI.Button>();
            delete.targetGraphic = image;
            delete.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            var colors = delete.colors;
            colors.normalColor = new Color(.56f, .11f, .12f);
            colors.highlightedColor = colors.selectedColor = new Color(.72f, .15f, .16f);
            colors.pressedColor = new Color(.4f, .08f, .08f);
            delete.colors = colors;
            delete.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            deleteColumn.SetSiblingIndex(preview.transform.Find("TieColumn").GetSiblingIndex() + 1);

            Transform status = overlay.transform.Find("LoadStatus");
            if (status == null)
            {
                var label = UnityEngine.Object.Instantiate(preview.transform.Find("TeamsColumn").GetComponent<TextMeshProUGUI>(), overlay.transform, false);
                label.name = "LoadStatus";
                if (label.TryGetComponent(out UnityEngine.UI.LayoutElement oldLayout)) UnityEngine.Object.DestroyImmediate(oldLayout);
                status = label.transform;
                RectTransform rect = label.rectTransform;
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.pivot = new Vector2(0, .5f);
                rect.anchoredPosition = new Vector2(64, 48);
                rect.sizeDelta = new Vector2(1400, 56);
                label.text = string.Empty;
                label.raycastTarget = false;
            }
            serialized.FindProperty("loadBrowserStatusText").objectReferenceValue = status.GetComponent<TextMeshProUGUI>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)header);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)preview.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save the load-browser scene.");
            return new { scene = ScenePath, after = "Tie", button = deleteColumn.name, template = preview.name };
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    private static void SetWidth(Transform column, float width)
    {
        if (column == null) throw new InvalidOperationException("An existing browser column is missing.");
        column.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = width;
    }
}
