using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class CreateLoadRoomManager : MonoBehaviour
{
    private static readonly Color RowColor = new(0.9f, 0.92f, 0.96f, 1f);
    private static readonly Color RowTextColor = new(0.08f, 0.09f, 0.12f, 1f);
    private const string CreateNewGameSceneName = "CreateNewHSGameScene";
    private const string RoomSceneName = "Room";
    private const string MainMenuSceneName = "MainMenu";

    [Header("Create / Load")]
    [SerializeField] private TextMeshProUGUI createNewButtonText;
    [SerializeField] private TextMeshProUGUI loadButtonText;

    [Header("Load Browser")]
    [SerializeField] private GameObject loadBrowserOverlay;
    [SerializeField] private TextMeshProUGUI loadBrowserTitleText;
    [SerializeField] private Transform headerRow;
    [SerializeField] private Transform rowsContent;
    [SerializeField] private GameObject previewRow;
    [SerializeField] private TextMeshProUGUI emptyStateText;
    [SerializeField] private Button loadBrowserCancelButton;
    [SerializeField] private ScrollRect loadBrowserScrollRect;

    private static readonly SaveColumnDefinition[] DefaultColumns =
    {
        new("Teams", "Teams", 230f),
        new("Score", "Score", 62f),
        new("Clock", "Clock", 80f),
        new("CreatedAt", "Created at", 172f),
        new("LastActivityAt", "Last activity", 172f),
        new("MatchDuration", "Match Duration", 120f),
        new("Tie", "Tie", 52f),
        new("Status", "Status", 126f)
    };

    private string selectedGameMode = ApplicationManager.HotSeatGameMode;

    private void Awake()
    {
        ResolveSelectedGameMode();
        BindCreateLoadReferences();
        BindLoadBrowserReferences();
        ConfigureLoadBrowser();
        ApplyModeLabels();
        CloseLoadBrowser();
    }

    public void CreateNewRoom()
    {
        ApplicationManager.EnsureInstanceExists();
        ApplicationManager.Instance.SetSelectedRoomGameMode(selectedGameMode);
        SceneManager.LoadScene(CreateNewGameSceneName);
    }

    public void LoadRoom()
    {
        if (!EnsureLoadBrowserReady())
        {
            return;
        }

        loadBrowserOverlay.SetActive(true);
        RebuildSaveRows();
        Debug.Log($"Opening {selectedGameMode} save browser");
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(MainMenuSceneName);
    }
    
    public void ExitGameFromCreateLoadRoom()
    {
        Debug.Log("Game Quit!");
        Application.Quit(); // This will close the application
    }

    private void ResolveSelectedGameMode()
    {
        ApplicationManager.EnsureInstanceExists();
        string requestedMode = ApplicationManager.Instance.SelectedRoomGameMode;
        selectedGameMode = string.IsNullOrWhiteSpace(requestedMode)
            ? ApplicationManager.HotSeatGameMode
            : requestedMode;
        ApplicationManager.Instance.SetSelectedRoomGameMode(selectedGameMode);
    }

    private void BindCreateLoadReferences()
    {
        createNewButtonText ??= FindButtonLabel("Create New Room Game");
        loadButtonText ??= FindButtonLabel("Load Room Game");
    }

    private void ApplyModeLabels()
    {
        string modeLabel = GetModeDisplayName();
        if (createNewButtonText != null)
        {
            createNewButtonText.text = $"Create New {modeLabel} Game";
        }

        if (loadButtonText != null)
        {
            loadButtonText.text = $"Load {modeLabel} Game";
        }

        if (loadBrowserTitleText != null)
        {
            loadBrowserTitleText.text = $"Load {modeLabel} Game";
        }
    }

    private string GetModeDisplayName()
    {
        return string.IsNullOrWhiteSpace(selectedGameMode)
            ? ApplicationManager.HotSeatGameMode
            : selectedGameMode.Trim();
    }

    private bool EnsureLoadBrowserReady()
    {
        ResolveSelectedGameMode();
        BindCreateLoadReferences();
        BindLoadBrowserReferences();
        ConfigureLoadBrowser();
        ApplyModeLabels();

        bool ready = loadBrowserOverlay != null
            && headerRow != null
            && rowsContent != null
            && previewRow != null
            && emptyStateText != null
            && loadBrowserCancelButton != null
            && loadBrowserScrollRect != null;

        if (!ready)
        {
            Debug.LogError("Room save browser is not wired. Add RoomLoadBrowser to the CreateLoadRoom scene and assign CreateLoadRoomManager load browser references.");
        }

        return ready;
    }

    private void BindLoadBrowserReferences()
    {
        if (loadBrowserOverlay == null)
        {
            loadBrowserOverlay = FindSceneObject("RoomLoadBrowser");
        }

        if (loadBrowserOverlay == null)
        {
            return;
        }

        loadBrowserTitleText ??= FindChildComponent<TextMeshProUGUI>(loadBrowserOverlay.transform, "Title");
        headerRow ??= FindChildComponent<RectTransform>(loadBrowserOverlay.transform, "HeaderRow");
        rowsContent ??= FindChildComponent<RectTransform>(loadBrowserOverlay.transform, "Content");
        previewRow ??= FindChildTransform(loadBrowserOverlay.transform, "PreviewRow")?.gameObject;
        emptyStateText ??= FindChildComponent<TextMeshProUGUI>(loadBrowserOverlay.transform, "EmptyState");
        loadBrowserCancelButton ??= FindChildComponent<Button>(loadBrowserOverlay.transform, "CancelButton");
        loadBrowserScrollRect ??= FindChildComponent<ScrollRect>(loadBrowserOverlay.transform, "SaveScroll");
    }

    private void ConfigureLoadBrowser()
    {
        if (loadBrowserCancelButton != null)
        {
            loadBrowserCancelButton.onClick.RemoveListener(CloseLoadBrowser);
            loadBrowserCancelButton.onClick.AddListener(CloseLoadBrowser);
        }
    }

    private void CloseLoadBrowser()
    {
        if (loadBrowserOverlay != null)
        {
            loadBrowserOverlay.SetActive(false);
        }
    }

    private void RebuildSaveRows()
    {
        if (!EnsureLoadBrowserReady())
        {
            return;
        }

        List<SaveColumnLayout> columnLayouts = GetColumnLayouts();
        SynchronizeHeaderRow(columnLayouts);
        ClearGeneratedRows();

        if (previewRow != null)
        {
            previewRow.SetActive(false);
        }

        List<RoomSaveSummary> summaries = RoomSaveService.GetEligibleSaveSummaries(selectedGameMode);
        Debug.Log($"{selectedGameMode} load browser found {summaries.Count} eligible save file(s).");
        emptyStateText.text = summaries.Count == 0
            ? $"No eligible {GetModeDisplayName()} save files found."
            : string.Empty;

        foreach (RoomSaveSummary summary in summaries)
        {
            CreateSaveRow(summary, columnLayouts);
        }

        RebuildRowsLayout();
    }

    private void CreateSaveRow(RoomSaveSummary summary, IReadOnlyList<SaveColumnLayout> columnLayouts)
    {
        GameObject row = CreateRect($"SaveRow_{summary.FileName}", rowsContent, typeof(Image), typeof(Button));
        row.GetComponent<Image>().color = RowColor;
        row.AddComponent<LayoutElement>().preferredHeight = 40f;
        Button rowButton = row.GetComponent<Button>();
        rowButton.onClick.AddListener(() => SelectSave(summary));

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.padding = new RectOffset(8, 8, 4, 4);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;

        foreach (SaveColumnLayout columnLayout in columnLayouts)
        {
            TextAlignmentOptions alignment = columnLayout.Definition.Key == "Teams"
                ? TextAlignmentOptions.MidlineLeft
                : TextAlignmentOptions.Center;
            AddColumn(
                row.transform,
                GetColumnValue(summary, columnLayout.Definition.Key),
                columnLayout.Width,
                RowTextColor,
                alignment,
                $"{columnLayout.Definition.Key}Column");
        }
    }

    private void SelectSave(RoomSaveSummary summary)
    {
        if (summary == null || string.IsNullOrWhiteSpace(summary.FilePath))
        {
            return;
        }

        ApplicationManager.EnsureInstanceExists();
        ApplicationManager.Instance.SetSelectedRoomGameMode(selectedGameMode);
        ApplicationManager.Instance.SetActiveSaveFilePath(summary.FilePath);
        PlayerPrefs.SetString("currentGameSettings", summary.FilePath);
        PlayerPrefs.Save();
        CloseLoadBrowser();
        SceneManager.LoadScene(RoomSceneName);
    }

    private static string FormatUtc(string value)
    {
        if (DateTime.TryParse(value, out DateTime parsed))
        {
            return parsed.ToUniversalTime().ToString("yyyy-MM-dd HH:mm");
        }

        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private void RebuildRowsLayout()
    {
        Canvas.ForceUpdateCanvases();

        if (headerRow is RectTransform headerRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(headerRect);
        }

        if (rowsContent is RectTransform contentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        if (loadBrowserScrollRect != null)
        {
            loadBrowserScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    [ContextMenu("Sync Load Browser Table Layout")]
    private void SyncLoadBrowserTableLayout()
    {
        BindLoadBrowserReferences();

        if (headerRow == null || previewRow == null)
        {
            return;
        }

        SynchronizeHeaderRow(GetColumnLayouts());
        RebuildRowsLayout();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(headerRow.gameObject);
        UnityEditor.EditorUtility.SetDirty(previewRow);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private List<SaveColumnLayout> GetColumnLayouts()
    {
        List<SaveColumnLayout> columnLayouts = new();
        HashSet<string> includedColumnKeys = new(StringComparer.Ordinal);

        if (previewRow != null)
        {
            foreach (Transform child in previewRow.transform)
            {
                if (!TryGetColumnDefinition(child.name, out SaveColumnDefinition definition)
                    || includedColumnKeys.Contains(definition.Key))
                {
                    continue;
                }

                columnLayouts.Add(new SaveColumnLayout(definition, GetColumnWidth(child, definition.DefaultWidth)));
                includedColumnKeys.Add(definition.Key);
            }
        }

        foreach (SaveColumnDefinition definition in DefaultColumns)
        {
            if (includedColumnKeys.Add(definition.Key))
            {
                columnLayouts.Add(new SaveColumnLayout(definition, definition.DefaultWidth));
            }
        }

        return columnLayouts;
    }

    private void SynchronizeHeaderRow(IReadOnlyList<SaveColumnLayout> columnLayouts)
    {
        if (headerRow == null)
        {
            return;
        }

        for (int index = 0; index < columnLayouts.Count; index++)
        {
            SaveColumnLayout columnLayout = columnLayouts[index];
            Transform column = FindColumnChild(headerRow, columnLayout.Definition.Key);
            if (column == null)
            {
                TextMeshProUGUI createdColumn = AddColumn(
                    headerRow,
                    columnLayout.Definition.Header,
                    columnLayout.Width,
                    Color.white,
                    columnLayout.Definition.Key == "Teams" ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center,
                    $"{columnLayout.Definition.Key}Column");
                column = createdColumn.transform;
            }

            column.SetSiblingIndex(index);
            SetColumnWidth(column, columnLayout.Width);

            if (column.TryGetComponent(out TextMeshProUGUI label))
            {
                label.text = columnLayout.Definition.Header;
            }
        }
    }

    private static Transform FindColumnChild(Transform parent, string columnKey)
    {
        foreach (Transform child in parent)
        {
            if (TryGetColumnDefinition(child.name, out SaveColumnDefinition definition)
                && definition.Key == columnKey)
            {
                return child;
            }
        }

        return null;
    }

    private static float GetColumnWidth(Transform column, float fallbackWidth)
    {
        if (column.TryGetComponent(out LayoutElement layoutElement) && layoutElement.preferredWidth > 0f)
        {
            return layoutElement.preferredWidth;
        }

        if (column is RectTransform rectTransform && rectTransform.sizeDelta.x > 0f)
        {
            return rectTransform.sizeDelta.x;
        }

        return fallbackWidth;
    }

    private static void SetColumnWidth(Transform column, float width)
    {
        LayoutElement layoutElement = column.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = column.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = width;
    }

    private static bool TryGetColumnDefinition(string columnName, out SaveColumnDefinition definition)
    {
        string normalizedColumnName = NormalizeColumnKey(columnName);
        foreach (SaveColumnDefinition columnDefinition in DefaultColumns)
        {
            if (normalizedColumnName == NormalizeColumnKey(columnDefinition.Key)
                || normalizedColumnName == NormalizeColumnKey(columnDefinition.Header)
                || normalizedColumnName == NormalizeColumnKey($"{columnDefinition.Key}Column")
                || normalizedColumnName == NormalizeColumnKey($"{columnDefinition.Header}Column"))
            {
                definition = columnDefinition;
                return true;
            }
        }

        if (normalizedColumnName == "lastsaved"
            || normalizedColumnName == "lastsavedat"
            || normalizedColumnName == "lastsavedutc"
            || normalizedColumnName == "lastsavedcolumn"
            || normalizedColumnName == "lastsavedatcolumn"
            || normalizedColumnName == "lastsavedutccolumn")
        {
            foreach (SaveColumnDefinition columnDefinition in DefaultColumns)
            {
                if (columnDefinition.Key == "LastActivityAt")
                {
                    definition = columnDefinition;
                    return true;
                }
            }
        }

        definition = default;
        return false;
    }

    private static string NormalizeColumnKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        char[] buffer = new char[value.Length];
        int writeIndex = 0;
        foreach (char character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                buffer[writeIndex] = char.ToLowerInvariant(character);
                writeIndex++;
            }
        }

        return new string(buffer, 0, writeIndex);
    }

    private static string GetColumnValue(RoomSaveSummary summary, string columnKey)
    {
        return columnKey switch
        {
            "Teams" => summary.Teams,
            "Clock" => summary.Clock,
            "Score" => summary.Score,
            "HalfLength" => summary.HalfLength,
            "Halves" => summary.Halves,
            "MatchDuration" => summary.MatchDuration,
            "Tie" => summary.Tie,
            "CreatedAt" => FormatUtc(summary.CreatedUtc),
            "CreatedUtc" => FormatUtc(summary.CreatedUtc),
            "LastActivityAt" => FormatUtc(summary.LastActivityUtc),
            "LastActivityUtc" => FormatUtc(summary.LastActivityUtc),
            "LastSavedAt" => FormatUtc(summary.LastSavedUtc),
            "LastSavedUtc" => FormatUtc(summary.LastSavedUtc),
            "Status" => summary.Status,
            _ => string.Empty
        };
    }

    private static TextMeshProUGUI AddColumn(Transform parent, string text, float width, Color color, TextAlignmentOptions alignment, string name = "Column")
    {
        TextMeshProUGUI label = CreateText(name, parent, text, 12f, color, alignment);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.gameObject.AddComponent<LayoutElement>().preferredWidth = width;
        return label;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateRect(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.characterSpacing = 0f;
        return label;
    }

    private static GameObject CreateRect(string name, Transform parent, params Type[] components)
    {
        Type[] allComponents = new Type[components.Length + 2];
        allComponents[0] = typeof(RectTransform);
        allComponents[1] = typeof(CanvasRenderer);
        Array.Copy(components, 0, allComponents, 2, components.Length);
        GameObject gameObject = new(name, allComponents);
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (Canvas canvas in canvases)
        {
            Transform found = FindChildTransform(canvas.transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        Transform child = FindChildTransform(root, childName);
        return child == null ? null : child.GetComponent<T>();
    }

    private static TextMeshProUGUI FindButtonLabel(params string[] buttonNames)
    {
        foreach (string buttonName in buttonNames)
        {
            GameObject buttonObject = FindSceneObject(buttonName);
            if (buttonObject == null)
            {
                continue;
            }

            TextMeshProUGUI label = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                return label;
            }
        }

        return null;
    }

    private static Transform FindChildTransform(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private void ClearGeneratedRows()
    {
        if (rowsContent == null)
        {
            return;
        }

        for (int index = rowsContent.childCount - 1; index >= 0; index--)
        {
            Transform child = rowsContent.GetChild(index);
            if (previewRow != null && child == previewRow.transform)
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }

    private readonly struct SaveColumnDefinition
    {
        public SaveColumnDefinition(string key, string header, float defaultWidth)
        {
            Key = key;
            Header = header;
            DefaultWidth = defaultWidth;
        }

        public string Key { get; }
        public string Header { get; }
        public float DefaultWidth { get; }
    }

    private readonly struct SaveColumnLayout
    {
        public SaveColumnLayout(SaveColumnDefinition definition, float width)
        {
            Definition = definition;
            Width = width;
        }

        public SaveColumnDefinition Definition { get; }
        public float Width { get; }
    }
}
