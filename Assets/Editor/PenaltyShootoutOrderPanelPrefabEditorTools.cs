#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PenaltyShootoutOrderPanelPrefabEditorTools
{
    private const string PrefabPath = "Assets/Resources/UI/PenaltyShootoutOrderPanel.prefab";
    private const string SceneInstanceName = "PenaltyShootoutOrderPanel";

    [InitializeOnLoadMethod]
    private static void EnsurePrefabExistsOnEditorLoad()
    {
        if (!File.Exists(PrefabPath) || PrefabNeedsRebuild())
        {
            CreatePenaltyShootoutOrderPanelPrefab();
        }

        EnsureSceneInstanceInEditMode();
    }

    [MenuItem("Tools/Counter Attack/Rebuild Penalty Shootout Order Panel Prefab")]
    public static void CreatePenaltyShootoutOrderPanelPrefab()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

        GameObject root = CreateRect("PenaltyShootoutOrderPanel", null, typeof(Image), typeof(CanvasGroup), typeof(PenaltyShootoutOrderPanelView), typeof(PenaltyShootoutOrderPanelController));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        Image rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(0.05f, 0.06f, 0.07f, 0.94f);
        rootImage.raycastTarget = true;
        CanvasGroup rootCanvasGroup = root.GetComponent<CanvasGroup>();
        rootCanvasGroup.blocksRaycasts = true;
        rootCanvasGroup.interactable = true;

        VerticalLayoutGroup rootLayout = root.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(18, 18, 14, 14);
        rootLayout.spacing = 10f;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;

        PenaltyShootoutOrderPanelView view = root.GetComponent<PenaltyShootoutOrderPanelView>();
        view.titleText = CreateText("Title", root.transform, "Penalty Shootout Order", 24f, TextAlignmentOptions.Center, Color.white);
        view.titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

        GameObject columns = CreateRect("Columns", root.transform);
        HorizontalLayoutGroup columnsLayout = columns.AddComponent<HorizontalLayoutGroup>();
        columnsLayout.spacing = 12f;
        columnsLayout.childControlWidth = true;
        columnsLayout.childControlHeight = true;
        columnsLayout.childForceExpandWidth = true;
        columnsLayout.childForceExpandHeight = true;
        columns.AddComponent<LayoutElement>().flexibleHeight = 1f;

        view.homeRowsContainer = CreateColumn(columns.transform, "HomeColumn", "Home", out TMP_Text homeHeader);
        view.awayRowsContainer = CreateColumn(columns.transform, "AwayColumn", "Away", out TMP_Text awayHeader);
        view.homeHeaderText = homeHeader;
        view.awayHeaderText = awayHeader;
        view.rowTemplate = CreateRowTemplate(root.transform);
        CreatePreviewRow(view.homeRowsContainer, "1. #10 Home Taker", "Shooting: 5");
        CreatePreviewRow(view.awayRowsContainer, "1. #9 Away Taker", "Shooting: 4");
        view.startButton = CreateButton(root.transform, "Start Penalty Shootout");

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PenaltyShootoutOrderPanelPrefab] Created {PrefabPath}");
    }

    [MenuItem("Tools/Counter Attack/Ensure Penalty Shootout Order Panel In Scene")]
    public static void EnsureSceneInstanceInEditMode()
    {
        if (Application.isPlaying)
        {
            return;
        }

        Canvas canvas = ResolveMainCanvas();
        if (canvas == null)
        {
            return;
        }

        PenaltyShootoutOrderPanelController existing = canvas.GetComponentsInChildren<PenaltyShootoutOrderPanelController>(true)
            .FirstOrDefault(controller => controller != null && controller.name == SceneInstanceName);
        if (existing != null)
        {
            PenaltyShootoutOrderPanelView existingView = existing.GetComponent<PenaltyShootoutOrderPanelView>();
            if (existingView == null || existingView.rowTemplate == null || existingView.rowTemplate.nameText == null || existingView.rowTemplate.shootingText == null)
            {
                Object.DestroyImmediate(existing.gameObject);
                existing = null;
            }
        }

        if (existing != null)
        {
            existing.transform.SetParent(canvas.transform, false);
            existing.gameObject.SetActive(false);
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, canvas.transform) as GameObject;
        if (instance == null)
        {
            return;
        }

        instance.name = SceneInstanceName;
        instance.SetActive(false);
        RectTransform rect = instance.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[PenaltyShootoutOrderPanelPrefab] Added inactive scene instance under {canvas.name}.");
    }

    private static Transform CreateColumn(Transform parent, string name, string title, out TMP_Text header)
    {
        GameObject column = CreateRect(name, parent, typeof(Image));
        column.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.15f, 0.96f);
        VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        column.AddComponent<LayoutElement>().flexibleWidth = 1f;

        header = CreateText("Header", column.transform, title, 18f, TextAlignmentOptions.Center, Color.white);
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

        GameObject rows = CreateRect("Rows", column.transform);
        VerticalLayoutGroup rowLayout = rows.AddComponent<VerticalLayoutGroup>();
        rowLayout.spacing = 4f;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rows.AddComponent<LayoutElement>().flexibleHeight = 1f;
        return rows.transform;
    }

    private static Button CreateButton(Transform parent, string label)
    {
        GameObject buttonObject = CreateRect("StartPenaltyShootoutButton", parent, typeof(Image), typeof(Button));
        buttonObject.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f, 1f);
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 44f;

        TMP_Text text = CreateText("Text", buttonObject.transform, label, 17f, TextAlignmentOptions.Center, new Color(0.12f, 0.12f, 0.12f, 1f));
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return buttonObject.GetComponent<Button>();
    }

    private static PenaltyShootoutOrderRowView CreateRowTemplate(Transform parent)
    {
        GameObject rowObject = CreateRect("PenaltyOrderRowTemplate", parent, typeof(Image), typeof(CanvasGroup), typeof(PenaltyShootoutOrderRowView));
        rowObject.SetActive(false);
        rowObject.GetComponent<Image>().color = new Color(0.18f, 0.2f, 0.23f, 0.98f);
        LayoutElement layout = rowObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 34f;

        TMP_Text name = CreateText("Name", rowObject.transform, "1. #10 Example Player", 15f, TextAlignmentOptions.Left, Color.white);
        RectTransform nameRect = name.rectTransform;
        nameRect.anchorMin = Vector2.zero;
        nameRect.anchorMax = new Vector2(0.68f, 1f);
        nameRect.offsetMin = new Vector2(10f, 0f);
        nameRect.offsetMax = new Vector2(-6f, 0f);

        TMP_Text shooting = CreateText("Shooting", rowObject.transform, "Shooting: 5", 15f, TextAlignmentOptions.Right, Color.white);
        RectTransform shootingRect = shooting.rectTransform;
        shootingRect.anchorMin = new Vector2(0.68f, 0f);
        shootingRect.anchorMax = Vector2.one;
        shootingRect.offsetMin = new Vector2(6f, 0f);
        shootingRect.offsetMax = new Vector2(-10f, 0f);

        PenaltyShootoutOrderRowView row = rowObject.GetComponent<PenaltyShootoutOrderRowView>();
        row.nameText = name;
        row.shootingText = shooting;
        return row;
    }

    private static PenaltyShootoutOrderRowView CreatePreviewRow(Transform parent, string nameTextValue, string shootingTextValue)
    {
        PenaltyShootoutOrderRowView row = CreateRowTemplate(parent);
        row.name = "PenaltyOrderPreviewRow";
        row.gameObject.SetActive(true);
        row.nameText.text = nameTextValue;
        row.shootingText.text = shootingTextValue;
        return row;
    }


    private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = CreateRect(name, parent);
        TMP_Text tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        if (TMP_Settings.defaultFontAsset != null)
        {
            tmp.font = TMP_Settings.defaultFontAsset;
        }
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 10f;
        tmp.fontSizeMax = fontSize;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    private static bool PrefabNeedsRebuild()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            return true;
        }

        PenaltyShootoutOrderPanelView view = prefab.GetComponent<PenaltyShootoutOrderPanelView>();
        if (view == null || view.titleText == null || view.homeHeaderText == null || view.awayHeaderText == null || view.rowTemplate == null || view.startButton == null)
        {
            return true;
        }

        if (view.rowTemplate.nameText == null || view.rowTemplate.shootingText == null)
        {
            return true;
        }

        Image rootImage = prefab.GetComponent<Image>();
        CanvasGroup rootCanvasGroup = prefab.GetComponent<CanvasGroup>();
        if (rootImage == null || !rootImage.raycastTarget || rootCanvasGroup == null || !rootCanvasGroup.blocksRaycasts)
        {
            return true;
        }

        return prefab.GetComponentsInChildren<TMP_Text>(true).Any(text => text != null && text.font == null);
    }

    private static Canvas ResolveMainCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        Canvas namedCanvas = canvases.FirstOrDefault(candidate => candidate != null && candidate.name == "Canvas");
        if (namedCanvas != null)
        {
            return namedCanvas;
        }

        return canvases.FirstOrDefault(candidate =>
            candidate != null
            && candidate.isRootCanvas
            && candidate.name != "HoveredTokenNameCanvas");
    }

    private static GameObject CreateRect(string name, Transform parent, params System.Type[] components)
    {
        System.Type[] componentTypes = new System.Type[components.Length + 2];
        componentTypes[0] = typeof(RectTransform);
        componentTypes[1] = typeof(CanvasRenderer);
        for (int i = 0; i < components.Length; i++)
        {
            componentTypes[i + 2] = components[i];
        }

        GameObject gameObject = new(name, componentTypes);
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, false);
        }

        return gameObject;
    }
}
#endif
