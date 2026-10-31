using System;
using System.Linq;
using UnityEngine;

/// <summary>Fits the existing authored lineup rows to the current roster size.</summary>
[RequireComponent(typeof(RectTransform))]
public sealed class MatchStatsLineupLayout : MonoBehaviour
{
    [SerializeField] private RectTransform rowsRoot;
    [SerializeField] private RectTransform[] playerRows = Array.Empty<RectTransform>();
    [SerializeField] private RectTransform startingXIHeader;
    [SerializeField] private RectTransform substitutionsHeader;
    [SerializeField] private RectTransform scorerPanel;
    [Header("Authored spacing in canvas units")]
    [SerializeField] private float lineupBottomFromTop = 1116f;
    [SerializeField] private float scorerTopFromTop = 76f;
    [SerializeField] private float scorerLineupGap = 8f;
    [SerializeField] private float headerHeight = 54f;
    [SerializeField] private float regularRowHeight = 48f;
    [SerializeField] private float largeRosterRowHeight = 46f;
    [SerializeField] private int regularRosterSize = 16;

    private int lastRosterSize = -1;

    public void ApplyRosterSize(int rowCount, bool force = false)
    {
        if (rowsRoot == null || startingXIHeader == null || substitutionsHeader == null || scorerPanel == null
            || playerRows == null || playerRows.Length == 0 || playerRows.Any(row => row == null))
        {
            Debug.LogError("MatchStatsLineupLayout requires its authored rows, headers and scorer panel.", this);
            return;
        }

        rowCount = Mathf.Clamp(rowCount, 0, playerRows.Length);
        if (!force && lastRosterSize == rowCount) return;
        float rowHeight = rowCount > regularRosterSize ? largeRosterRowHeight : regularRowHeight;
        float lineupHeight = headerHeight * 2f + rowCount * rowHeight;
        float lineupTop = lineupBottomFromTop - lineupHeight;
        float scorerHeight = lineupTop - scorerLineupGap - scorerTopFromTop;
        if (rowCount > 0 && scorerHeight < 0f)
        {
            Debug.LogError("The authored lineup dimensions leave no room for the scorer panel.", this);
            return;
        }

        lastRosterSize = rowCount;
        for (int i = 0; i < playerRows.Length; i++)
        {
            playerRows[i].gameObject.SetActive(i < rowCount);
            if (i < rowCount) playerRows[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowHeight);
        }

        gameObject.SetActive(rowCount > 0);
        if (rowCount == 0) return;

        startingXIHeader.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, headerHeight);
        substitutionsHeader.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, headerHeight);
        var lineup = (RectTransform)transform;
        lineup.anchoredPosition = new Vector2(lineup.anchoredPosition.x, -lineupTop);
        lineup.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lineupHeight);
        scorerPanel.anchoredPosition = new Vector2(scorerPanel.anchoredPosition.x, -scorerTopFromTop);
        scorerPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, scorerHeight);
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rowsRoot);
    }

    [ContextMenu("Preview 16-player roster")]
    private void Preview16Players() => ApplyRosterSize(16, true);

    [ContextMenu("Preview 18-player roster")]
    private void Preview18Players() => ApplyRosterSize(18, true);
}
