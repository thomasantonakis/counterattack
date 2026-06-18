using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PenaltyShootoutOrderPanelController : MonoBehaviour
{
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
        if (view == null || !view.HasRequiredReferences())
        {
            Debug.LogError("[Shootout] PenaltyShootoutOrderPanel scene references are incomplete. Use Tools/Counter Attack/Ensure Scene-Owned Match Panels In Room.");
            return;
        }

        gameObject.SetActive(true);
        view.titleText.text = "Penalty Shootout Order";
        EnsureRootBlocksRaycasts();
        view.homeHeaderText.text = "Home";
        view.awayHeaderText.text = "Away";
        view.startButton.onClick.RemoveAllListeners();
        view.startButton.onClick.AddListener(OnStartClicked);
        RebuildRows(view.GetRows(true), homeOrder);
        RebuildRows(view.GetRows(false), awayOrder);
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
        return view.GetRows(home)
            .Where(row => row != null && row.gameObject.activeSelf)
            .Select(row => row.Token)
            .Where(token => token != null)
            .ToList();
    }

    private void OnStartClicked()
    {
        manager?.BeginShootoutFromOrderPanel(GetOrder(true), GetOrder(false));
    }

    private void RebuildRows(PenaltyShootoutOrderRowView[] rows, List<PlayerToken> tokens)
    {
        if (rows == null)
        {
            return;
        }

        int tokenCount = tokens?.Count ?? 0;
        for (int i = 0; i < rows.Length; i++)
        {
            PenaltyShootoutOrderRowView row = rows[i];
            if (row == null)
            {
                continue;
            }

            bool active = i < tokenCount;
            row.gameObject.SetActive(active);
            row.Configure(this, active ? tokens[i] : null, i + 1);
        }
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
}
