using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PenaltyShootoutOrderPanelController : MonoBehaviour
{
    private static readonly Color PanelColor = new(0.05f, 0.06f, 0.07f, 0.94f);
    private static readonly Color ColumnColor = new(0.12f, 0.13f, 0.15f, 0.96f);
    private static readonly Color RowColor = new(0.18f, 0.2f, 0.23f, 0.98f);
    private static readonly Color ButtonColor = new(0.9f, 0.9f, 0.9f, 1f);
    private static readonly Color TextColor = new(0.94f, 0.94f, 0.94f, 1f);

    private PenaltyShootoutManager manager;
    private PenaltyShootoutOrderPanelView view;
    private static int activePanelCount;

    public static bool IsAnyPanelActive => activePanelCount > 0;

    private void OnEnable()
    {
        activePanelCount++;
    }

    private void OnDisable()
    {
        activePanelCount = Mathf.Max(0, activePanelCount - 1);
    }

    public void Configure(PenaltyShootoutManager configuredManager, List<PlayerToken> homeOrder, List<PlayerToken> awayOrder)
    {
        manager = configuredManager;
        view = GetComponent<PenaltyShootoutOrderPanelView>();
        if (view == null || view.homeRowsContainer == null || view.awayRowsContainer == null || view.startButton == null)
        {
            BuildGeneratedView();
        }

        gameObject.SetActive(true);
        view.titleText.text = "Penalty Shootout Order";
        EnsureRootBlocksRaycasts();
        view.homeHeaderText.text = "Home";
        view.awayHeaderText.text = "Away";
        view.startButton.onClick.RemoveAllListeners();
        view.startButton.onClick.AddListener(OnStartClicked);
        RebuildRows(view.homeRowsContainer, homeOrder);
        RebuildRows(view.awayRowsContainer, awayOrder);
    }

    public void PlaceDraggedRow(PenaltyShootoutOrderRowView row, PointerEventData eventData, Transform originalParent, int originalSiblingIndex)
    {
        if (row == null)
        {
            return;
        }

        Transform container = row.transform.parent;
        if (originalParent != null && container != originalParent)
        {
            row.transform.SetParent(originalParent, false);
        }

        row.transform.SetSiblingIndex(originalSiblingIndex);
        PenaltyShootoutOrderRowView targetRow = FindRowUnderPointer(eventData, row);
        if (targetRow == null)
        {
            RefreshRowLabels(row.transform.parent);
            return;
        }

        if (targetRow.transform.parent != row.transform.parent)
        {
            Debug.LogWarning("Penalty shootout order rows can only be swapped within their own team column.");
            RefreshRowLabels(row.transform.parent);
            RefreshRowLabels(targetRow.transform.parent);
            return;
        }

        SwapRowTokens(row, targetRow);
        manager?.UpdateOrdersFromPanel(GetOrder(true), GetOrder(false));
    }

    public List<PlayerToken> GetOrder(bool home)
    {
        Transform container = home ? view.homeRowsContainer : view.awayRowsContainer;
        return container.GetComponentsInChildren<PenaltyShootoutOrderRowView>(false)
            .Select(row => row.Token)
            .Where(token => token != null)
            .ToList();
    }

    private void OnStartClicked()
    {
        manager?.BeginShootoutFromOrderPanel(GetOrder(true), GetOrder(false));
    }

    private void RebuildRows(Transform container, List<PlayerToken> tokens)
    {
        foreach (Transform child in container.Cast<Transform>().ToList())
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < tokens.Count; i++)
        {
            PenaltyShootoutOrderRowView row = CreateRow(container);
            row.Configure(this, tokens[i], i + 1);
        }
    }

    private PenaltyShootoutOrderRowView CreateRow(Transform parent)
    {
        if (view != null && view.rowTemplate != null)
        {
            PenaltyShootoutOrderRowView row = Instantiate(view.rowTemplate, parent);
            row.name = "PenaltyOrderRow";
            row.gameObject.SetActive(true);
            return row;
        }

        return CreateGeneratedRow(parent, "PenaltyOrderRow", true);
    }

    private PenaltyShootoutOrderRowView CreateGeneratedRow(Transform parent, string rowName, bool active)
    {
        GameObject rowObject = CreateRect(rowName, parent, typeof(Image), typeof(CanvasGroup), typeof(PenaltyShootoutOrderRowView));
        rowObject.SetActive(active);
        Image image = rowObject.GetComponent<Image>();
        image.color = RowColor;
        LayoutElement layout = rowObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 34f;
        TMP_Text name = CreateText("Name", rowObject.transform, string.Empty, 15f, TextAlignmentOptions.Left);
        RectTransform nameRect = name.rectTransform;
        nameRect.anchorMin = Vector2.zero;
        nameRect.anchorMax = new Vector2(0.68f, 1f);
        nameRect.offsetMin = new Vector2(10f, 0f);
        nameRect.offsetMax = new Vector2(-6f, 0f);

        TMP_Text shooting = CreateText("Shooting", rowObject.transform, string.Empty, 15f, TextAlignmentOptions.Right);
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

    private void BuildGeneratedView()
    {
        foreach (Transform child in transform.Cast<Transform>().ToList())
        {
            Destroy(child.gameObject);
        }

        view = gameObject.GetComponent<PenaltyShootoutOrderPanelView>() ?? gameObject.AddComponent<PenaltyShootoutOrderPanelView>();
        RectTransform rect = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image panelImage = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        panelImage.color = PanelColor;
        panelImage.raycastTarget = true;
        EnsureRootBlocksRaycasts();

        VerticalLayoutGroup rootLayout = gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(18, 18, 14, 14);
        rootLayout.spacing = 10f;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;

        view.titleText = CreateText("Title", transform, "Penalty Shootout Order", 24f, TextAlignmentOptions.Center);
        view.titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

        GameObject columns = CreateRect("Columns", transform);
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
        view.rowTemplate = CreateGeneratedRow(transform, "PenaltyOrderRowTemplate", false);
        view.startButton = CreateButton(transform, "Start Penalty Shootout");
    }

    private Transform CreateColumn(Transform parent, string name, string title, out TMP_Text header)
    {
        GameObject column = CreateRect(name, parent, typeof(Image));
        column.GetComponent<Image>().color = ColumnColor;
        VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        column.AddComponent<LayoutElement>().flexibleWidth = 1f;
        header = CreateText("Header", column.transform, title, 18f, TextAlignmentOptions.Center);
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
        GameObject rows = CreateRect("Rows", column.transform);
        VerticalLayoutGroup rowLayout = rows.AddComponent<VerticalLayoutGroup>();
        rowLayout.spacing = 4f;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rows.AddComponent<LayoutElement>().flexibleHeight = 1f;
        return rows.transform;
    }

    private void EnsureRootBlocksRaycasts()
    {
        CanvasGroup canvasGroup = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        Image panelImage = gameObject.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.raycastTarget = true;
        }
    }

    private static Button CreateButton(Transform parent, string label)
    {
        GameObject buttonObject = CreateRect("StartPenaltyShootoutButton", parent, typeof(Image), typeof(Button));
        buttonObject.GetComponent<Image>().color = ButtonColor;
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 44f;
        TMP_Text text = CreateText("Text", buttonObject.transform, label, 17f, TextAlignmentOptions.Center);
        text.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return buttonObject.GetComponent<Button>();
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateRect(name, parent);
        TMP_Text tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = TextColor;
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

    private PenaltyShootoutOrderRowView FindRowUnderPointer(PointerEventData eventData, PenaltyShootoutOrderRowView draggedRow)
    {
        if (eventData == null || EventSystem.current == null)
        {
            return null;
        }

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (RaycastResult result in results)
        {
            PenaltyShootoutOrderRowView row = result.gameObject.GetComponentInParent<PenaltyShootoutOrderRowView>();
            if (row != null && row != draggedRow)
            {
                return row;
            }
        }

        return null;
    }

    private void SwapRowTokens(PenaltyShootoutOrderRowView row, PenaltyShootoutOrderRowView targetRow)
    {
        if (row == null || targetRow == null || row == targetRow)
        {
            RefreshRowLabels(row != null ? row.transform.parent : null);
            return;
        }

        PlayerToken rowToken = row.Token;
        PlayerToken targetToken = targetRow.Token;
        int rowIndex = row.transform.GetSiblingIndex();
        int targetIndex = targetRow.transform.GetSiblingIndex();
        row.Configure(this, targetToken, rowIndex + 1);
        targetRow.Configure(this, rowToken, targetIndex + 1);
        RefreshRowLabels(row.transform.parent);
    }

    private void RefreshRowLabels(Transform container)
    {
        if (container == null)
        {
            return;
        }

        PenaltyShootoutOrderRowView[] rows = container.GetComponentsInChildren<PenaltyShootoutOrderRowView>(true);
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i].Configure(this, rows[i].Token, i + 1);
        }
    }

    private static GameObject CreateRect(string name, Transform parent, params System.Type[] components)
    {
        List<System.Type> componentTypes = new() { typeof(RectTransform), typeof(CanvasRenderer) };
        componentTypes.AddRange(components);
        GameObject gameObject = new(name, componentTypes.ToArray());
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }
}
