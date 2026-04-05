using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Text;
using System.IO;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MatchStatsUI : MonoBehaviour
{
    private enum TemplateTableKind
    {
        Scoreboard,
        Stats,
        Lineups,
    }

    private enum TemplateRowKind
    {
        Header,
        Section,
        Metric,
    }

    private enum ColumnTextAlignment
    {
        Left,
        Center,
        Right,
    }

    private sealed class TemplateRow
    {
        public TemplateRowKind kind;
        public string centerLabel;
    }

    private sealed class ScoreboardScorerSummary
    {
        public string scorer;
        public List<MatchManager.GoalEvent> goals = new();
        public int earliestMinute;
    }

    private readonly struct ColumnLayout
    {
        public readonly float sidePaddingPx;
        public readonly float monospaceStepPx;
        public readonly int leftChars;
        public readonly int centerChars;
        public readonly int rightChars;
        public readonly int dividerChars;

        public ColumnLayout(float sidePaddingPx, float monospaceStepPx, int leftChars, int centerChars, int rightChars, int dividerChars)
        {
            this.sidePaddingPx = sidePaddingPx;
            this.monospaceStepPx = monospaceStepPx;
            this.leftChars = leftChars;
            this.centerChars = centerChars;
            this.rightChars = rightChars;
            this.dividerChars = dividerChars;
        }
    }

    private sealed class LineupPlayerRow
    {
        public string displayName;
        public PlayerToken liveToken;
        public int jerseyNumber;
        public bool isBooked;
        public bool isSentOff;
        public int goals;
        public int assists;
        public int yellowCards;
        public int redCards;
        public int injuries;
        public int subOns;
        public int subOffs;
    }

    private sealed class LineupHoverEntry
    {
        public PlayerToken homeToken;
        public PlayerToken awayToken;
    }

    private sealed class LineupHoverRowBounds
    {
        public float topY;
        public float bottomY;
        public LineupHoverEntry entry;
    }

    public TMP_Text statsText;  // Drag the TextMeshPro UI element here
    public TMP_Text homeScorersText;
    public TMP_Text awayScorersText;
    public RectTransform panel;     // Assign the MatchStatsUI panel here
    public Button toggleButton;     // Assign a small edge button (like "◀"/"▶")
    public float collapsedX = 370f; // Distance off-screen to slide
    public float animationSpeed = 5f;
    public PlayerCard playerCardPrefab;
    public GoalkeeperCard goalkeeperCardPrefab;
    private float statsFontSizeMin = 13f;
    private float statsFontSizeMax = 19f;
    private float statsLineSpacing = 10f;
    private float statsParagraphSpacing = 0f;
    [SerializeField] private GameObject externalScoreboardRoot;

    private bool isExpanded = true;
    private Vector2 onScreenPos;
    private Vector2 offScreenPos;

    private static readonly Color PanelBackgroundColor = new(0.05f, 0.08f, 0.14f, 0.9f);
    private static readonly Color ToggleBackgroundColor = new(0.86f, 0.71f, 0.34f, 0.95f);
    private const string HomeColor = "#8FD3FF";
    private const string AwayColor = "#FFBC7A";
    private const string AccentColor = "#F4E7B2";
    private const string MutedColor = "#B8C3D1";
    private const string DividerColor = "#5E6B7F";
    private const string SoftAccentColor = "#D5DFEA";
    private const string NeutralPlayerColor = "#FFFFFF";
    private const string CardYellowColor = "#F4D35E";
    private const string CardRedColor = "#FF6B6B";
    private const string EmptyLabel = "none";
    private static readonly Color PreviewOutfieldFrameColor = new(0.22f, 0.30f, 0.40f, 1f);
    private static readonly Color PreviewGoalkeeperFrameColor = new(0.45f, 0.40f, 0.51f, 1f);
    private static readonly Color PreviewNameColor = new(0.20f, 0.27f, 0.37f, 1f);
    private static readonly Color PreviewSecondaryColor = new(0.18f, 0.63f, 0.24f, 1f);
    private static readonly Color PreviewLabelColor = new(0.11f, 0.12f, 0.16f, 1f);
    private static readonly Color PreviewHighValueColor = new(0.46f, 0.82f, 0.77f, 1f);
    private static readonly Color PreviewMidValueColor = new(0.90f, 0.67f, 0.16f, 1f);
    private static readonly Color PreviewLowValueColor = new(0.88f, 0.37f, 0.34f, 1f);
    private const float SidePaddingRatio = 0.03f;
    private const float ScoreboardSideColumnRatio = 0.41f;
    private const float ScoreboardCenterColumnRatio = 0.18f;
    private const float ScoreboardMonospaceStepPx = 7.25f;
    private const float StatsCenterColumnRatio = 0.50f;
    private const float StatsSideColumnRatio = 0.25f;
    private const float LineupCenterColumnRatio = 0.04f;
    private const float LineupSideColumnRatio = 0.48f;
    private const float MonospaceStepPx = 7.25f;
    private const string LineupColumnGap = "   ";
    private static readonly Regex RichTagRegex = new("<.*?>", RegexOptions.Compiled);
    private readonly List<TemplateRow> templateRows = new();
    private string lineupHeaderLabel = "Lineups";
    private bool showLineups;
    private string statsTemplatePath;
    private DateTime statsTemplateWriteUtc;
    private string currentHomeColor = HomeColor;
    private string currentAwayColor = AwayColor;
    private ColumnTextAlignment scoreboardLeftColumnAlignment = ColumnTextAlignment.Right;
    private ColumnTextAlignment scoreboardCenterColumnAlignment = ColumnTextAlignment.Center;
    private ColumnTextAlignment scoreboardRightColumnAlignment = ColumnTextAlignment.Left;
    private ColumnTextAlignment lineupLeftColumnAlignment = ColumnTextAlignment.Right;
    private ColumnTextAlignment lineupCenterColumnAlignment = ColumnTextAlignment.Center;
    private ColumnTextAlignment lineupRightColumnAlignment = ColumnTextAlignment.Left;
    private RectTransform hoverCardsRoot;
    private RectTransform homeHoverCardAnchor;
    private RectTransform awayHoverCardAnchor;
    private PlayerCard homeHoverCard;
    private PlayerCard awayHoverCard;
    private GoalkeeperCard homeGoalkeeperHoverCard;
    private GoalkeeperCard awayGoalkeeperHoverCard;
    private readonly Dictionary<int, LineupHoverEntry> lineupHoverEntries = new();
    private readonly List<LineupHoverRowBounds> lineupHoverRows = new();
    private ColumnLayout currentLineupLayout;
    private PlayerToken lineupHoveredHomeToken;
    private PlayerToken lineupHoveredAwayToken;
    private PlayerToken lastHoveredHomeToken;
    private PlayerToken lastHoveredAwayToken;
    private Sprite previewValueBadgeSprite;

    private const float TablesTopPadding = 0f;
    private const float TablesBottomAnchor = 0.43f;
    private const float CardsBottomPadding = 0.03f;
    private const float CardsTopAnchor = 0.39f;
    private const float CardColumnGap = 0.035f;
    private const float CardPrefabWidth = 330f;
    private const float CardPrefabHeight = 500f;
    private const float PreviewCardBottomInset = 6f;
    private const float PreviewCardWidthFill = 0.98f;
    private const float PreviewCardHeightFill = 0.99f;
    private const float PreviewFaceWidth = 286f;
    private const float PreviewFaceHeight = 392f;
    private const float PreviewFaceYOffset = 30f;
    private const float PreviewNameWidth = 238f;
    private const float PreviewNameHeight = 42f;
    private const float PreviewNameY = 158f;
    private const float PreviewCountryWidth = 208f;
    private const float PreviewCountryHeight = 28f;
    private const float PreviewCountryY = 116f;
    private const float PreviewAttributesWidth = 266f;
    private const float PreviewAttributesHeight = 272f;
    private const float PreviewAttributesY = -38f;
    private const float PreviewLogoWidth = 252f;
    private const float PreviewLogoHeight = 36f;
    private const float PreviewLogoY = -220f;
    private const float PreviewRowHeight = 34f;
    private const float PreviewRowLabelWidth = 154f;
    private const float PreviewRowValueWidth = 60f;
    private const float PreviewRowSpacing = 8f;
    private const float PreviewRowValueRightPadding = 6f;
    private const float PreviewValueBadgeSize = 56f;
    private const int BaselineLineupRowCount = 16;
    private const float LineSpacingAdjustmentPerLineupRow = 0.75f;

    private void Awake()
    {
        statsTemplatePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", "Wiki", "stats.md"));
        LoadStatsTemplate();
        ApplyRuntimeVisualStyle();
        ConfigurePanelLayout();
        EnsureExternalScoreboardRoot();
        EnsureHoverCards();

        if (statsText != null)
        {
            statsText.text = string.Empty;
        }

        if (homeScorersText != null)
        {
            homeScorersText.text = string.Empty;
        }

        if (awayScorersText != null)
        {
            awayScorersText.text = string.Empty;
        }
    }

    private void Start()
    {
        onScreenPos = panel.anchoredPosition;
        offScreenPos = new Vector2(collapsedX, onScreenPos.y);
        toggleButton.onClick.AddListener(TogglePanel);
        UpdateToggleGlyph();
        UpdateExternalScoreboardVisibility();
        UpdateStatsUI();
        UpdateScorersDisplay();
        InvokeRepeating(nameof(UpdateStatsUI), 1f, 1f);  // Update UI every second
    }

    private void OnEnable()
    {
        GameInputManager.OnHover += HandleHoverTokenChanged;
    }

    private void OnDisable()
    {
        GameInputManager.OnHover -= HandleHoverTokenChanged;
    }

    private void Update()
    {
        UpdateLineupHoverFromStatsText();
    }

    private void TogglePanel()
    {
        isExpanded = !isExpanded;
        StopAllCoroutines();
        StartCoroutine(SlidePanel(isExpanded ? onScreenPos : offScreenPos));
        UpdateToggleGlyph();
        UpdateExternalScoreboardVisibility();
    }

    private IEnumerator SlidePanel(Vector2 targetPos)
    {
        while (Vector2.Distance(panel.anchoredPosition, targetPos) > 0.1f)
        {
            panel.anchoredPosition = Vector2.Lerp(panel.anchoredPosition, targetPos, Time.deltaTime * animationSpeed);
            yield return null;
        }
        panel.anchoredPosition = targetPos;
    }

    void UpdateStatsUI()
    {
        if (MatchManager.Instance == null || MatchManager.Instance.gameData == null)
        {
            statsText.text = "Match data not available";
            return;
        }

        MatchManager.TeamStats homeTeam = MatchManager.Instance.gameData.stats.homeTeamStats;
        MatchManager.TeamStats awayTeam = MatchManager.Instance.gameData.stats.awayTeamStats;

        RefreshTemplateIfNeeded();
        RefreshTeamColors();

        string homeTeamName = MatchManager.Instance.gameData.gameSettings.homeTeamName;
        string awayTeamName = MatchManager.Instance.gameData.gameSettings.awayTeamName;
        int lineupRowCount = GetMaxLineupRowCount();
        ColumnLayout scoreboardLayout = GetColumnLayout(ScoreboardSideColumnRatio, ScoreboardCenterColumnRatio, 5, false, ScoreboardMonospaceStepPx);
        ColumnLayout statsLayout = GetColumnLayout(StatsSideColumnRatio, StatsCenterColumnRatio, 10);
        ColumnLayout lineupLayout = GetColumnLayout(LineupSideColumnRatio, LineupCenterColumnRatio, 3);
        currentLineupLayout = lineupLayout;
        lineupHoverEntries.Clear();
        ApplyDynamicStatsSpacing(lineupRowCount);

        StringBuilder builder = new();
        int currentLineIndex = 0;
        AppendScoreboard(builder, scoreboardLayout, homeTeamName, awayTeamName, homeTeam.totalGoals, awayTeam.totalGoals, ref currentLineIndex);

        foreach (TemplateRow row in templateRows)
        {
            switch (row.kind)
            {
                case TemplateRowKind.Header:
                    AppendLine(builder, BuildHeaderRow(homeTeamName, awayTeamName, homeTeam.totalGoals, awayTeam.totalGoals, statsLayout), ref currentLineIndex);
                    break;
                case TemplateRowKind.Section:
                    AppendLine(builder, BuildSectionRow(row.centerLabel, statsLayout), ref currentLineIndex);
                    break;
                case TemplateRowKind.Metric:
                    (string homeValue, string awayValue, bool emphasize) = ResolveMetricRow(row.centerLabel, homeTeam, awayTeam);
                    AppendLine(builder, BuildMetricRow(homeValue, row.centerLabel, awayValue, statsLayout, emphasize), ref currentLineIndex);
                    break;
            }
        }

        AppendLineupsTable(builder, lineupLayout, homeTeamName, awayTeamName, ref currentLineIndex);

        statsText.text = builder.ToString().TrimEnd();
        statsText.ForceMeshUpdate();
        RebuildLineupHoverRows();
        UpdateScorersDisplay();
        RefreshHoverCards();
    }

    public void UpdateScorersDisplay()
    {
        if (MatchManager.Instance == null)
        {
            return;
        }

        if (homeScorersText != null)
        {
            homeScorersText.text = string.Empty;
            homeScorersText.gameObject.SetActive(false);
        }

        if (awayScorersText != null)
        {
            awayScorersText.text = string.Empty;
            awayScorersText.gameObject.SetActive(false);
        }
    }

    private string FormatScorerList(List<MatchManager.GoalEvent> scorers)
    {
        return scorers == null || scorers.Count == 0
            ? EmptyLabel
            : string.Join(" | ", scorers.Select(g => g.ToString()));
    }

    private void ApplyRuntimeVisualStyle()
    {
        Image panelImage = panel != null ? panel.GetComponent<Image>() : null;
        if (panelImage != null)
        {
            panelImage.color = PanelBackgroundColor;
        }

        Image toggleImage = toggleButton != null ? toggleButton.GetComponent<Image>() : null;
        if (toggleImage != null)
        {
            toggleImage.color = ToggleBackgroundColor;
        }

        if (statsText != null)
        {
            ConfigureText(statsText, statsFontSizeMin, Mathf.Max(statsFontSizeMin, statsFontSizeMax), TextAlignmentOptions.TopLeft, false);
            statsText.richText = true;
            statsText.lineSpacing = statsLineSpacing;
            statsText.paragraphSpacing = statsParagraphSpacing;
        }
        ConfigureText(homeScorersText, 10f, 13f, TextAlignmentOptions.TopLeft, true);
        ConfigureText(awayScorersText, 10f, 13f, TextAlignmentOptions.TopLeft, true);

        TextMeshProUGUI toggleLabel = toggleButton != null ? toggleButton.GetComponentInChildren<TextMeshProUGUI>() : null;
        if (toggleLabel != null)
        {
            ConfigureText(toggleLabel, 18f, 22f, TextAlignmentOptions.Center, false);
            toggleLabel.color = new Color(0.09f, 0.12f, 0.17f, 1f);
            toggleLabel.fontStyle = FontStyles.Bold;
        }
    }

    private void ConfigurePanelLayout()
    {
        if (panel != null)
        {
            float panelWidth = panel.sizeDelta.x > 0f ? panel.sizeDelta.x : 370f;
            panel.anchorMin = new Vector2(1f, 0f);
            panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(1f, 0.5f);
            panel.anchoredPosition = new Vector2(0f, 0f);
            panel.sizeDelta = new Vector2(panelWidth, 0f);
        }

        if (statsText != null)
        {
            RectTransform statsRect = statsText.rectTransform;
            statsRect.anchorMin = new Vector2(SidePaddingRatio, TablesBottomAnchor);
            statsRect.anchorMax = new Vector2(1f - SidePaddingRatio, 1f - TablesTopPadding);
            statsRect.pivot = new Vector2(0.5f, 1f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;
        }
    }

    private void EnsureHoverCards()
    {
        if (panel == null)
        {
            return;
        }

        EnsurePlayerCardPrefab();
        EnsureGoalkeeperCardPrefab();
        if (playerCardPrefab == null && goalkeeperCardPrefab == null)
        {
            return;
        }

        if (hoverCardsRoot == null)
        {
            hoverCardsRoot = CreateChildRect("HoverCardsRoot", panel);
            hoverCardsRoot.anchorMin = new Vector2(SidePaddingRatio, CardsBottomPadding);
            hoverCardsRoot.anchorMax = new Vector2(1f - SidePaddingRatio, CardsTopAnchor);
            hoverCardsRoot.offsetMin = Vector2.zero;
            hoverCardsRoot.offsetMax = Vector2.zero;
        }

        if (homeHoverCardAnchor == null)
        {
            homeHoverCardAnchor = CreateChildRect("HomeHoverCardAnchor", hoverCardsRoot);
            homeHoverCardAnchor.anchorMin = new Vector2(0f, 0f);
            homeHoverCardAnchor.anchorMax = new Vector2(0.5f - (CardColumnGap * 0.5f), 1f);
            homeHoverCardAnchor.offsetMin = Vector2.zero;
            homeHoverCardAnchor.offsetMax = Vector2.zero;
            homeHoverCardAnchor.gameObject.AddComponent<RectMask2D>();
        }

        if (awayHoverCardAnchor == null)
        {
            awayHoverCardAnchor = CreateChildRect("AwayHoverCardAnchor", hoverCardsRoot);
            awayHoverCardAnchor.anchorMin = new Vector2(0.5f + (CardColumnGap * 0.5f), 0f);
            awayHoverCardAnchor.anchorMax = new Vector2(1f, 1f);
            awayHoverCardAnchor.offsetMin = Vector2.zero;
            awayHoverCardAnchor.offsetMax = Vector2.zero;
            awayHoverCardAnchor.gameObject.AddComponent<RectMask2D>();
        }

        if (homeHoverCard == null && playerCardPrefab != null)
        {
            homeHoverCard = Instantiate(playerCardPrefab, homeHoverCardAnchor);
            PrepareHoverCard(homeHoverCard, false);
        }

        if (awayHoverCard == null && playerCardPrefab != null)
        {
            awayHoverCard = Instantiate(playerCardPrefab, awayHoverCardAnchor);
            PrepareHoverCard(awayHoverCard, false);
        }

        if (homeGoalkeeperHoverCard == null && goalkeeperCardPrefab != null)
        {
            homeGoalkeeperHoverCard = Instantiate(goalkeeperCardPrefab, homeHoverCardAnchor);
            PrepareHoverCard(homeGoalkeeperHoverCard, true);
        }

        if (awayGoalkeeperHoverCard == null && goalkeeperCardPrefab != null)
        {
            awayGoalkeeperHoverCard = Instantiate(goalkeeperCardPrefab, awayHoverCardAnchor);
            PrepareHoverCard(awayGoalkeeperHoverCard, true);
        }
    }

    private void EnsureExternalScoreboardRoot()
    {
        if (externalScoreboardRoot != null)
        {
            return;
        }

        ScoreboardManager scoreboardManager = FindObjectsByType<ScoreboardManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault();

        if (scoreboardManager != null)
        {
            externalScoreboardRoot = scoreboardManager.gameObject;
        }
    }

    private void UpdateExternalScoreboardVisibility()
    {
        EnsureExternalScoreboardRoot();
        if (externalScoreboardRoot != null)
        {
            externalScoreboardRoot.SetActive(!isExpanded);
        }
    }

    private static RectTransform CreateChildRect(string name, RectTransform parent)
    {
        GameObject child = new(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private void PrepareHoverCard(Component card, bool isGoalkeeper)
    {
        if (card == null)
        {
            return;
        }

        RectTransform anchor = card.transform.parent as RectTransform;
        RectTransform cardRect = card.GetComponent<RectTransform>();
        if (anchor == null || cardRect == null)
        {
            return;
        }

        cardRect.anchorMin = new Vector2(0.5f, 0f);
        cardRect.anchorMax = new Vector2(0.5f, 0f);
        cardRect.pivot = new Vector2(0.5f, 0f);
        cardRect.anchoredPosition = new Vector2(0f, PreviewCardBottomInset);
        cardRect.localScale = Vector3.one * CalculateCardScale(anchor, cardRect);
        ApplyPreviewCardTheme(card.gameObject, isGoalkeeper);
        card.gameObject.SetActive(false);
    }

    private static float CalculateCardScale(RectTransform anchor, RectTransform cardRect)
    {
        float availableWidth = anchor.rect.width;
        float availableHeight = anchor.rect.height;
        if (availableWidth <= 0f || availableHeight <= 0f)
        {
            return 0.4f;
        }

        float sourceWidth = Mathf.Max(cardRect.rect.width, CardPrefabWidth);
        float sourceHeight = Mathf.Max(cardRect.rect.height, CardPrefabHeight);
        return Mathf.Min(
            (availableWidth * PreviewCardWidthFill) / sourceWidth,
            (availableHeight * PreviewCardHeightFill) / sourceHeight);
    }

    private void HandleHoverTokenChanged(PlayerToken token, HexCell hex)
    {
        if (token == null)
        {
            return;
        }

        if (token.isHomeTeam)
        {
            lastHoveredHomeToken = token;
        }
        else
        {
            lastHoveredAwayToken = token;
        }

        RefreshHoverCards();
    }

    private void RefreshHoverCards()
    {
        EnsureHoverCards();
        RefreshHoverCard(homeHoverCard, homeGoalkeeperHoverCard, homeHoverCardAnchor, lineupHoveredHomeToken ?? lastHoveredHomeToken, true);
        RefreshHoverCard(awayHoverCard, awayGoalkeeperHoverCard, awayHoverCardAnchor, lineupHoveredAwayToken ?? lastHoveredAwayToken, false);
    }

    private void RefreshHoverCard(PlayerCard outfieldCard, GoalkeeperCard goalkeeperCard, RectTransform anchor, PlayerToken token, bool isHomeTeam)
    {
        if (anchor == null)
        {
            return;
        }

        UpdateHoverCardScale(outfieldCard, anchor);
        UpdateHoverCardScale(goalkeeperCard, anchor);

        if (token == null || MatchManager.Instance?.gameData?.gameSettings == null)
        {
            SetHoverCardActive(outfieldCard, false);
            SetHoverCardActive(goalkeeperCard, false);
            return;
        }

        string teamLabel = isHomeTeam
            ? MatchManager.Instance.gameData.gameSettings.homeTeamName
            : MatchManager.Instance.gameData.gameSettings.awayTeamName;

        if (token.IsGoalKeeper && goalkeeperCard != null)
        {
            SetHoverCardActive(outfieldCard, false);
            goalkeeperCard.UpdateFromToken(token, teamLabel);
            ApplyPreviewCardTheme(goalkeeperCard.gameObject, true);
            SetHoverCardActive(goalkeeperCard, true);
            return;
        }

        if (outfieldCard != null)
        {
            SetHoverCardActive(goalkeeperCard, false);
            outfieldCard.UpdateFromToken(token, teamLabel);
            ApplyPreviewCardTheme(outfieldCard.gameObject, false);
            SetHoverCardActive(outfieldCard, true);
            return;
        }

        SetHoverCardActive(goalkeeperCard, false);
    }

    private void EnsurePlayerCardPrefab()
    {
        if (playerCardPrefab != null)
        {
            return;
        }

#if UNITY_EDITOR
        playerCardPrefab = AssetDatabase.LoadAssetAtPath<PlayerCard>("Assets/Prefabs/PlayerCardPrefab.prefab");
#endif
    }

    private void EnsureGoalkeeperCardPrefab()
    {
        if (goalkeeperCardPrefab != null)
        {
            return;
        }

#if UNITY_EDITOR
        goalkeeperCardPrefab = AssetDatabase.LoadAssetAtPath<GoalkeeperCard>("Assets/Prefabs/GoalKeeperCardPrefab.prefab");
#endif
    }

    private static void SetHoverCardActive(Component card, bool isActive)
    {
        if (card != null)
        {
            card.gameObject.SetActive(isActive);
        }
    }

    private static void UpdateHoverCardScale(Component card, RectTransform anchor)
    {
        if (card == null || anchor == null)
        {
            return;
        }

        RectTransform cardRect = card.GetComponent<RectTransform>();
        if (cardRect == null)
        {
            return;
        }

        cardRect.localScale = Vector3.one * CalculateCardScale(anchor, cardRect);
    }

    private void ApplyPreviewCardTheme(GameObject cardObject, bool isGoalkeeper)
    {
        if (cardObject == null)
        {
            return;
        }

        Image frameImage = cardObject.GetComponent<Image>();
        if (frameImage != null)
        {
            frameImage.color = isGoalkeeper ? PreviewGoalkeeperFrameColor : PreviewOutfieldFrameColor;
        }

        RectTransform whiteBackground = FindDescendantComponent<RectTransform>(cardObject.transform, "WhiteBackground");
        if (whiteBackground != null)
        {
            whiteBackground.localScale = Vector3.one;
            whiteBackground.sizeDelta = new Vector2(PreviewFaceWidth, PreviewFaceHeight);
            whiteBackground.anchoredPosition = new Vector2(0f, PreviewFaceYOffset);
        }

        RectTransform flag = FindDescendantComponent<RectTransform>(cardObject.transform, "Flag");
        if (flag != null)
        {
            flag.gameObject.SetActive(false);
        }

        TMP_Text playerName = FindDescendantComponent<TMP_Text>(cardObject.transform, "PlayerName");
        if (playerName != null)
        {
            playerName.color = PreviewNameColor;
            playerName.fontStyle = FontStyles.Bold | FontStyles.Italic;
            playerName.enableAutoSizing = true;
            playerName.fontSizeMin = 26f;
            playerName.fontSizeMax = 40f;
            playerName.characterSpacing = -1.2f;
            playerName.alignment = TextAlignmentOptions.Left;
            playerName.textWrappingMode = TextWrappingModes.NoWrap;
            playerName.overflowMode = TextOverflowModes.Ellipsis;
            if (playerName.rectTransform != null)
            {
                playerName.rectTransform.anchoredPosition = new Vector2(0f, PreviewNameY);
                playerName.rectTransform.sizeDelta = new Vector2(PreviewNameWidth, PreviewNameHeight);
            }
        }

        TMP_Text country = FindDescendantComponent<TMP_Text>(cardObject.transform, "Country");
        if (country != null)
        {
            country.color = PreviewSecondaryColor;
            country.fontStyle = FontStyles.Bold;
            country.enableAutoSizing = true;
            country.fontSizeMin = 17f;
            country.fontSizeMax = 24f;
            country.characterSpacing = 0.35f;
            country.alignment = TextAlignmentOptions.Left;
            country.textWrappingMode = TextWrappingModes.NoWrap;
            country.overflowMode = TextOverflowModes.Ellipsis;
            if (country.rectTransform != null)
            {
                country.rectTransform.anchoredPosition = new Vector2(18f, PreviewCountryY);
                country.rectTransform.sizeDelta = new Vector2(PreviewCountryWidth, PreviewCountryHeight);
            }
        }

        foreach (TMP_Text text in cardObject.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null)
            {
                continue;
            }

            if (text.name.EndsWith("Label", StringComparison.Ordinal))
            {
                text.color = PreviewLabelColor;
                text.enableAutoSizing = true;
                text.fontSizeMin = 20f;
                text.fontSizeMax = 28f;
                text.alignment = TextAlignmentOptions.Left;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Ellipsis;
            }
            else if (text.name.EndsWith("Value", StringComparison.Ordinal))
            {
                text.color = Color.white;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.enableAutoSizing = false;
                text.fontSize = 34f;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Overflow;
                UpdateValueBadge(text);
            }
            else if (text.text != null && text.text.IndexOf("counter attack", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                text.color = isGoalkeeper ? new Color(0.92f, 0.89f, 0.96f, 1f) : new Color(0.64f, 0.86f, 1f, 1f);
                text.enableAutoSizing = true;
                text.fontSizeMin = 22f;
                text.fontSizeMax = 30f;
                text.characterSpacing = 2.4f;
                text.alignment = TextAlignmentOptions.Center;
                if (text.rectTransform != null)
                {
                    text.rectTransform.anchoredPosition = new Vector2(0f, PreviewLogoY);
                    text.rectTransform.sizeDelta = new Vector2(PreviewLogoWidth, PreviewLogoHeight);
                }
            }
        }

        ConfigurePreviewAttributeRows(cardObject.transform, isGoalkeeper);
    }

    private void ConfigurePreviewAttributeRows(Transform cardRoot, bool isGoalkeeper)
    {
        RectTransform attributes = FindDescendantComponent<RectTransform>(cardRoot, "Attributes");
        if (attributes == null)
        {
            return;
        }

        attributes.anchoredPosition = new Vector2(0f, PreviewAttributesY);
        attributes.sizeDelta = new Vector2(PreviewAttributesWidth, PreviewAttributesHeight);

        if (attributes.TryGetComponent(out VerticalLayoutGroup verticalLayout))
        {
            verticalLayout.enabled = false;
        }

        string[] rowOrder = isGoalkeeper
            ? new[] { "Aerial", "Dribbling", "Pace", "Resilience", "Saving", "Handling", "High Pass" }
            : new[] { "Pace", "Dribbling", "Heading", "High Pass", "Resilience", "Shooting", "Tackling" };

        float rowWidth = PreviewAttributesWidth - 20f;
        float rowTopInset = 2f;
        float rowStep = rowOrder.Length > 1
            ? (PreviewAttributesHeight - PreviewRowHeight - rowTopInset) / (rowOrder.Length - 1)
            : 0f;

        for (int index = 0; index < rowOrder.Length; index++)
        {
            Transform row = FindNamedChild(attributes, rowOrder[index]);
            if (row == null)
            {
                continue;
            }

            row.SetSiblingIndex(index);

            RectTransform rowRect = row as RectTransform;
            if (rowRect != null)
            {
                rowRect.anchorMin = new Vector2(0.5f, 1f);
                rowRect.anchorMax = new Vector2(0.5f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.anchoredPosition = new Vector2(0f, -(rowTopInset + (index * rowStep)));
                rowRect.sizeDelta = new Vector2(rowWidth, PreviewRowHeight);
            }

            if (row.TryGetComponent(out HorizontalLayoutGroup rowLayout))
            {
                rowLayout.enabled = false;
            }

            if (row.TryGetComponent(out LayoutElement rowLayoutElement))
            {
                rowLayoutElement.preferredHeight = PreviewRowHeight;
                rowLayoutElement.preferredWidth = rowWidth;
                rowLayoutElement.flexibleHeight = 0f;
                rowLayoutElement.flexibleWidth = 0f;
            }

            TMP_Text label = row.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(component => component != null && component.name.EndsWith("Label", StringComparison.Ordinal));
            TMP_Text value = row.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(component => component != null && component.name.EndsWith("Value", StringComparison.Ordinal));

            if (label != null)
            {
                label.text = isGoalkeeper && row.name == "Aerial" ? "Aerial Ability" : row.name;
                if (label.TryGetComponent(out LayoutElement labelLayoutElement))
                {
                    labelLayoutElement.preferredWidth = PreviewRowLabelWidth;
                    labelLayoutElement.flexibleWidth = 0f;
                    labelLayoutElement.minWidth = PreviewRowLabelWidth;
                }

                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = new Vector2(0f, 0.5f);
                labelRect.anchorMax = new Vector2(0f, 0.5f);
                labelRect.pivot = new Vector2(0f, 0.5f);
                labelRect.anchoredPosition = new Vector2(0f, 0f);
                labelRect.sizeDelta = new Vector2(PreviewRowLabelWidth, PreviewRowHeight);
            }

            if (value != null)
            {
                if (value.TryGetComponent(out LayoutElement valueLayoutElement))
                {
                    valueLayoutElement.preferredWidth = PreviewRowValueWidth;
                    valueLayoutElement.flexibleWidth = 0f;
                    valueLayoutElement.minWidth = PreviewRowValueWidth;
                }

                RectTransform valueRect = value.rectTransform;
                valueRect.anchorMin = new Vector2(1f, 0.5f);
                valueRect.anchorMax = new Vector2(1f, 0.5f);
                valueRect.pivot = new Vector2(1f, 0.5f);
                valueRect.anchoredPosition = new Vector2(-PreviewRowValueRightPadding, 0f);
                valueRect.sizeDelta = new Vector2(PreviewRowValueWidth, PreviewRowHeight);
                UpdateValueBadge(value);
            }
        }
    }

    private void UpdateValueBadge(TMP_Text valueText)
    {
        if (valueText == null)
        {
            return;
        }

        if (!int.TryParse(valueText.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedValue))
        {
            return;
        }

        previewValueBadgeSprite ??= CreatePreviewBadgeSprite();
        if (previewValueBadgeSprite == null)
        {
            return;
        }

        Transform parent = valueText.transform.parent;
        if (parent == null)
        {
            return;
        }

        string badgeName = $"{valueText.name}Badge";
        Image badge = FindDescendantComponent<Image>(parent, badgeName);
        if (badge == null)
        {
            GameObject badgeObject = new(badgeName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeObject.transform.SetParent(parent, false);
            badge = badgeObject.GetComponent<Image>();
            badge.transform.SetSiblingIndex(valueText.transform.GetSiblingIndex());
        }

        RectTransform valueRect = valueText.rectTransform;
        RectTransform badgeRect = badge.rectTransform;
        badgeRect.anchorMin = valueRect.anchorMin;
        badgeRect.anchorMax = valueRect.anchorMax;
        badgeRect.pivot = valueRect.pivot;
        badgeRect.anchoredPosition = valueRect.anchoredPosition;
        badgeRect.localRotation = Quaternion.identity;
        badgeRect.localScale = Vector3.one;
        badgeRect.sizeDelta = new Vector2(PreviewValueBadgeSize, PreviewValueBadgeSize);

        badge.sprite = previewValueBadgeSprite;
        badge.preserveAspect = true;
        badge.color = GetPreviewValueBadgeColor(parsedValue);
        badge.raycastTarget = false;
        valueText.raycastTarget = false;
    }

    private static Color GetPreviewValueBadgeColor(int value)
    {
        if (value >= 5)
        {
            return PreviewHighValueColor;
        }

        if (value >= 3)
        {
            return PreviewMidValueColor;
        }

        return PreviewLowValueColor;
    }

    private static Sprite CreatePreviewBadgeSprite()
    {
        const int textureSize = 128;
        const float edgeSoftness = 2f;
        Texture2D texture = new(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "PreviewValueBadgeRuntime",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        Color32[] pixels = new Color32[textureSize * textureSize];
        Vector2 center = new((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        float radius = ((textureSize * 0.5f) - 2f) * 0.7f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01((radius - distance) / edgeSoftness);
                pixels[(y * textureSize) + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize);
    }

    private static T FindDescendantComponent<T>(Transform root, string name) where T : Component
    {
        if (root == null || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (!string.Equals(child.name, name, StringComparison.Ordinal))
            {
                continue;
            }

            if (child.TryGetComponent(out T component))
            {
                return component;
            }
        }

        return null;
    }

    private static Transform FindNamedChild(Transform parent, string name)
    {
        if (parent == null || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        foreach (Transform child in parent)
        {
            if (string.Equals(child.name, name, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }

    private static void ConfigureText(TMP_Text target, float minSize, float maxSize, TextAlignmentOptions alignment, bool wrap)
    {
        if (target == null)
        {
            return;
        }

        target.richText = true;
        target.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        target.overflowMode = TextOverflowModes.Overflow;
        target.enableAutoSizing = true;
        target.fontSizeMin = minSize;
        target.fontSizeMax = maxSize;
        target.alignment = alignment;
        target.lineSpacing = -9f;
        target.paragraphSpacing = 0f;
    }

    private void UpdateToggleGlyph()
    {
        TextMeshProUGUI toggleLabel = toggleButton != null ? toggleButton.GetComponentInChildren<TextMeshProUGUI>() : null;
        if (toggleLabel != null)
        {
            toggleLabel.text = isExpanded ? ">" : "<";
        }
    }

    private void LoadStatsTemplate()
    {
        templateRows.Clear();
        lineupHeaderLabel = "Lineups";
        showLineups = false;
        scoreboardLeftColumnAlignment = ColumnTextAlignment.Right;
        scoreboardCenterColumnAlignment = ColumnTextAlignment.Center;
        scoreboardRightColumnAlignment = ColumnTextAlignment.Left;
        lineupLeftColumnAlignment = ColumnTextAlignment.Right;
        lineupCenterColumnAlignment = ColumnTextAlignment.Center;
        lineupRightColumnAlignment = ColumnTextAlignment.Left;

        string templatePath = string.IsNullOrWhiteSpace(statsTemplatePath)
            ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", "Wiki", "stats.md"))
            : statsTemplatePath;
        if (File.Exists(templatePath))
        {
            statsTemplateWriteUtc = File.GetLastWriteTimeUtc(templatePath);
            int tableIndex = 0;
            bool insideTable = false;
            foreach (string rawLine in File.ReadAllLines(templatePath))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (insideTable)
                    {
                        tableIndex++;
                        insideTable = false;
                    }
                    continue;
                }

                if (!line.StartsWith("|"))
                {
                    continue;
                }

                insideTable = true;

                string[] segments = line.Split('|');
                if (segments.Length < 5)
                {
                    continue;
                }

                string left = segments[1].Trim();
                string center = segments[2].Trim();
                string right = segments[3].Trim();
                TemplateTableKind tableKind = tableIndex switch
                {
                    0 => TemplateTableKind.Scoreboard,
                    1 => TemplateTableKind.Stats,
                    _ => TemplateTableKind.Lineups,
                };

                if (IsAlignmentRow(left, center, right))
                {
                    if (tableKind == TemplateTableKind.Scoreboard)
                    {
                        scoreboardLeftColumnAlignment = ParseAlignment(left, ColumnTextAlignment.Right);
                        scoreboardCenterColumnAlignment = ParseAlignment(center, ColumnTextAlignment.Center);
                        scoreboardRightColumnAlignment = ParseAlignment(right, ColumnTextAlignment.Left);
                    }
                    else if (tableKind == TemplateTableKind.Lineups)
                    {
                        lineupLeftColumnAlignment = ParseAlignment(left, ColumnTextAlignment.Right);
                        lineupCenterColumnAlignment = ParseAlignment(center, ColumnTextAlignment.Center);
                        lineupRightColumnAlignment = ParseAlignment(right, ColumnTextAlignment.Left);
                    }
                    continue;
                }

                if (string.IsNullOrWhiteSpace(center))
                {
                    continue;
                }

                if (tableKind == TemplateTableKind.Stats)
                {
                    if (center.StartsWith("score", System.StringComparison.OrdinalIgnoreCase))
                    {
                        templateRows.Add(new TemplateRow { kind = TemplateRowKind.Header, centerLabel = center });
                    }
                    else if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right))
                    {
                        templateRows.Add(new TemplateRow { kind = TemplateRowKind.Section, centerLabel = center });
                    }
                    else
                    {
                        templateRows.Add(new TemplateRow { kind = TemplateRowKind.Metric, centerLabel = center });
                    }
                }
                else
                {
                    if (!showLineups)
                    {
                        lineupHeaderLabel = center;
                        showLineups = true;
                    }
                }
            }
        }

        if (templateRows.Count == 0)
        {
            templateRows.Add(new TemplateRow { kind = TemplateRowKind.Section, centerLabel = "ATT" });
            templateRows.Add(new TemplateRow { kind = TemplateRowKind.Metric, centerLabel = "Sh / On" });
            templateRows.Add(new TemplateRow { kind = TemplateRowKind.Metric, centerLabel = "Blk / Off" });
            templateRows.Add(new TemplateRow { kind = TemplateRowKind.Metric, centerLabel = "Ast / Cor" });
        }
    }

    private void AppendScoreboard(StringBuilder builder, ColumnLayout layout, string homeTeamName, string awayTeamName, int homeGoals, int awayGoals, ref int currentLineIndex)
    {
        AppendLine(builder, BuildScoreboardHeaderRow(homeTeamName, awayTeamName, homeGoals, awayGoals, layout), ref currentLineIndex);

        List<ScoreboardScorerSummary> homeScorers = BuildScoreboardScorerSummaries(MatchManager.Instance?.homeScorers);
        List<ScoreboardScorerSummary> awayScorers = BuildScoreboardScorerSummaries(MatchManager.Instance?.awayScorers);
        int rowCount = Math.Max(4, Math.Max(homeScorers.Count, awayScorers.Count));

        for (int index = 0; index < rowCount; index++)
        {
            ScoreboardScorerSummary homeRow = index < homeScorers.Count ? homeScorers[index] : null;
            ScoreboardScorerSummary awayRow = index < awayScorers.Count ? awayScorers[index] : null;
            AppendLine(builder, BuildScoreboardScorerRow(homeRow, awayRow, layout), ref currentLineIndex);
        }

        AppendLine(builder, string.Empty, ref currentLineIndex);
    }

    private string BuildHeaderRow(string homeTeamName, string awayTeamName, int homeGoals, int awayGoals, ColumnLayout layout)
    {
        return BuildThreeColumnRow(
            TruncateLineupName(homeTeamName, layout.leftChars),
            $"{homeGoals}-{awayGoals}",
            TruncateLineupName(awayTeamName, layout.rightChars),
            layout,
            currentHomeColor,
            AccentColor,
            currentAwayColor,
            ColumnTextAlignment.Center,
            ColumnTextAlignment.Center,
            true);
    }

    private string BuildScoreboardHeaderRow(string homeTeamName, string awayTeamName, int homeGoals, int awayGoals, ColumnLayout layout)
    {
        string homeText = AlignRich(
            $"<size=108%><b><color={currentHomeColor}>{TrimToWidth(SanitizeName(homeTeamName), layout.leftChars)}</color></b></size>",
            layout.leftChars,
            scoreboardLeftColumnAlignment);

        string centerText = AlignRich(
            $"<size=138%><b><color={AccentColor}>{homeGoals} - {awayGoals}</color></b></size>",
            layout.centerChars,
            scoreboardCenterColumnAlignment);

        string awayText = AlignRich(
            $"<size=108%><b><color={currentAwayColor}>{TrimToWidth(SanitizeName(awayTeamName), layout.rightChars)}</color></b></size>",
            layout.rightChars,
            scoreboardRightColumnAlignment);

        return BuildRichThreeColumnRow(homeText, centerText, awayText, layout);
    }

    private string BuildScoreboardScorerRow(ScoreboardScorerSummary homeSummary, ScoreboardScorerSummary awaySummary, ColumnLayout layout)
    {
        string homeText = BuildScoreboardSideText(homeSummary, layout.leftChars, currentHomeColor, scoreboardLeftColumnAlignment);
        string centerText = AlignRich(string.Empty, layout.centerChars, scoreboardCenterColumnAlignment);
        string awayText = BuildScoreboardSideText(awaySummary, layout.rightChars, currentAwayColor, scoreboardRightColumnAlignment);
        return BuildRichThreeColumnRow(homeText, centerText, awayText, layout);
    }

    private string BuildScoreboardSideText(ScoreboardScorerSummary summary, int width, string color, ColumnTextAlignment alignment)
    {
        if (summary == null)
        {
            return AlignRich(string.Empty, width, alignment);
        }

        string plainText = FormatScoreboardScorerSummary(summary);
        string truncated = TrimToWidth(plainText, width);
        string richText = $"<size=94%><b><color={color}>{truncated}</color></b></size>";
        return AlignRich(richText, width, alignment);
    }

    private static string BuildRichThreeColumnRow(string leftText, string centerText, string rightText, ColumnLayout layout)
    {
        return
            $"<space={layout.sidePaddingPx:0.##}px><mspace={layout.monospaceStepPx:0.##}px>{leftText}{centerText}{rightText}</mspace>";
    }

    private static string BuildDivider(ColumnLayout layout)
    {
        return $"<space={layout.sidePaddingPx:0.##}px><color={DividerColor}>{new string('-', layout.dividerChars)}</color>";
    }

    private string BuildSectionRow(string title, ColumnLayout layout)
    {
        return BuildThreeColumnRow(
            string.Empty,
            title,
            string.Empty,
            layout,
            MutedColor,
            SoftAccentColor,
            MutedColor,
            ColumnTextAlignment.Center,
            ColumnTextAlignment.Center,
            true);
    }

    private string BuildMetricRow(string homeValue, string label, string awayValue, ColumnLayout layout, bool emphasize = false)
    {
        return BuildThreeColumnRow(
            homeValue,
            label,
            awayValue,
            layout,
            currentHomeColor,
            MutedColor,
            currentAwayColor,
            ColumnTextAlignment.Center,
            ColumnTextAlignment.Center,
            emphasize);
    }

    private static string BuildThreeColumnRow(
        string left,
        string center,
        string right,
        ColumnLayout layout,
        string leftColor,
        string centerColor,
        string rightColor,
        ColumnTextAlignment leftAlignment,
        ColumnTextAlignment rightAlignment,
        bool emphasize = false)
    {
        string leftText = AlignTo(left, layout.leftChars, leftAlignment);
        string centerText = AlignCenter(center, layout.centerChars);
        string rightText = AlignTo(right, layout.rightChars, rightAlignment);

        if (emphasize)
        {
            leftText = $"<b>{leftText}</b>";
            centerText = $"<b>{centerText}</b>";
            rightText = $"<b>{rightText}</b>";
        }

        return
            $"<space={layout.sidePaddingPx:0.##}px><mspace={layout.monospaceStepPx:0.##}px><color={leftColor}>{leftText}</color>" +
            $"<color={centerColor}>{centerText}</color>" +
            $"<color={rightColor}>{rightText}</color></mspace>";
    }

    private string BuildLineupRow(LineupPlayerRow homePlayer, string centerLabel, LineupPlayerRow awayPlayer, ColumnLayout layout)
    {
        string homeName = BuildLineupSideText(homePlayer, layout.leftChars, true, lineupLeftColumnAlignment);
        string centerText = BuildLineupCenterCell(centerLabel, layout.centerChars, MutedColor, lineupCenterColumnAlignment);
        string awayName = BuildLineupSideText(awayPlayer, layout.rightChars, false, lineupRightColumnAlignment);
        return BuildLineupTableRow(homeName, centerText, awayName, layout);
    }

    private string BuildLineupHeaderRow(string homeTeamName, string awayTeamName, ColumnLayout layout)
    {
        string homeText = AlignRich(
            $"<color={currentHomeColor}>{TruncateLineupName(homeTeamName, layout.leftChars)}</color>",
            layout.leftChars,
            lineupLeftColumnAlignment);
        string centerText = BuildLineupCenterCell(lineupHeaderLabel, layout.centerChars, AccentColor, lineupCenterColumnAlignment);
        string awayText = AlignRich(
            $"<color={currentAwayColor}>{TruncateLineupName(awayTeamName, layout.rightChars)}</color>",
            layout.rightChars,
            lineupRightColumnAlignment);
        return BuildLineupTableRow(homeText, centerText, awayText, layout);
    }

    private static string BuildLineupTableRow(string leftText, string centerText, string rightText, ColumnLayout layout)
    {
        return
            $"<space={layout.sidePaddingPx:0.##}px><mspace={layout.monospaceStepPx:0.##}px>{leftText}" +
            $"{LineupColumnGap}{centerText}{LineupColumnGap}" +
            $"{rightText}</mspace>";
    }

    private static string BuildLineupCenterCell(string value, int width, string color, ColumnTextAlignment alignment)
    {
        return $"<color={color}>{AlignTo(value, width, alignment)}</color>";
    }

    private static (string homeValue, string awayValue, bool emphasize) ResolveMetricRow(
        string label,
        MatchManager.TeamStats homeTeam,
        MatchManager.TeamStats awayTeam)
    {
        switch (NormalizeMetricKey(label))
        {
            case "shotson":
            case "shon":
            case "totalshots":
                return ($"{homeTeam.totalShots}", $"{awayTeam.totalShots}", false);
            case "ontargetcorners":
                return ($"{homeTeam.totalShotsOnTarget}/{homeTeam.totalCorners}", $"{awayTeam.totalShotsOnTarget}/{awayTeam.totalCorners}", false);
            case "blockedoff":
            case "blkoff":
            case "blockedofftarget":
                return ($"{homeTeam.totalShotsBlocked}/{homeTeam.totalShotsOffTarget}", $"{awayTeam.totalShotsBlocked}/{awayTeam.totalShotsOffTarget}", false);
            case "assistscorners":
            case "astcor":
            case "asissts":
            case "assists":
                return ($"{homeTeam.totalAssists}", $"{awayTeam.totalAssists}", false);
            case "groundpass":
            case "gndpass":
            case "groundattmade":
            case "groundattemptedmade":
                return (FormatAttemptedMade(homeTeam.totalPassesAttempted, homeTeam.totalPassesCompleted), FormatAttemptedMade(awayTeam.totalPassesAttempted, awayTeam.totalPassesCompleted), false);
            case "aerialattargcomp":
            case "aerialatttargcomp":
            case "aeratc":
            case "aerialatttrgmade":
            case "aerialattemptedtargetmade":
                return (FormatAerialTriplet(homeTeam), FormatAerialTriplet(awayTeam), false);
            case "posswonlost":
            case "wonlost":
                return ($"{homeTeam.totalPossessionWon}/{homeTeam.totalPossessionLost}", $"{awayTeam.totalPossessionWon}/{awayTeam.totalPossessionLost}", false);
            case "distancecovered":
                return ($"{homeTeam.totalPacesRan}", $"{awayTeam.totalPacesRan}", false);
            case "possession":
                return (FormatPossession(homeTeam, awayTeam), FormatPossession(awayTeam, homeTeam), false);
            case "interceptions":
            case "int":
                return (FormatCompactCompletion(homeTeam.totalInterceptionsMade, homeTeam.totalInterceptionsAttempted), FormatCompactCompletion(awayTeam.totalInterceptionsMade, awayTeam.totalInterceptionsAttempted), false);
            case "savespaces":
            case "svpace":
                return ($"{homeTeam.totalAttemptsSaved}/{homeTeam.totalPacesRan}", $"{awayTeam.totalAttemptsSaved}/{awayTeam.totalPacesRan}", false);
            case "groundduels":
            case "gndduel":
            case "groundattwon":
            case "groundattemptedwon":
                return (FormatAttemptedMade(homeTeam.totalGroundDuelsInvolved, homeTeam.totalGroundDuelsWon), FormatAttemptedMade(awayTeam.totalGroundDuelsInvolved, awayTeam.totalGroundDuelsWon), false);
            case "aerialduels":
            case "airduel":
            case "airattwon":
            case "airattemptedwon":
                return (FormatAttemptedMade(homeTeam.totalAerialChallengesInvolved, homeTeam.totalAerialChallengesWon), FormatAttemptedMade(awayTeam.totalAerialChallengesInvolved, awayTeam.totalAerialChallengesWon), false);
            case "yellowred":
            case "yelred":
            case "yellowredcards":
                return ($"{homeTeam.totalYellowCards}/{homeTeam.totalRedCards}", $"{awayTeam.totalYellowCards}/{awayTeam.totalRedCards}", false);
            case "injuriessubs":
            case "injsub":
            case "injuriessubsused":
                return ($"{homeTeam.totalInjuries}/{homeTeam.totalSubstiutions}", $"{awayTeam.totalInjuries}/{awayTeam.totalSubstiutions}", false);
            case "xrecoveries":
            case "xrec":
            case "xrecoveriesmade":
                return (FormatExpectedAndMade(homeTeam.totalXRecoveries, homeTeam.totalInterceptionsMade), FormatExpectedAndMade(awayTeam.totalXRecoveries, awayTeam.totalInterceptionsMade), true);
            case "xdribbles":
            case "xdri":
            case "xdribblesmade":
                return (FormatExpectedAndMade(homeTeam.totalXDribbles, homeTeam.totalGroundDuelsWon), FormatExpectedAndMade(awayTeam.totalXDribbles, awayTeam.totalGroundDuelsWon), true);
            case "xtackles":
            case "xtac":
            case "xtacklesmade":
                return (FormatExpectedAndMade(homeTeam.totalXTackles, homeTeam.totalGroundDuelsWon), FormatExpectedAndMade(awayTeam.totalXTackles, awayTeam.totalGroundDuelsWon), true);
            default:
                return ("-", "-", false);
        }
    }

    private static string FormatCompactCompletion(int completed, int attempted)
    {
        return $"{completed}/{attempted}";
    }

    private static string FormatAttemptedMade(int attempted, int made)
    {
        return $"{attempted}/{made}";
    }

    private static string FormatAerialTriplet(MatchManager.TeamStats stats)
    {
        return $"{stats.totalAerialPassesAttempted}/{stats.totalAerialPassesTargeted}/{stats.totalAerialPassesCompleted}";
    }

    private static string FormatExpected(float value)
    {
        return value.ToString("0.00");
    }

    private static string FormatExpectedAndMade(float expected, int made)
    {
        return $"{expected:0.00}/{made}";
    }

    private static string FormatPossession(MatchManager.TeamStats team, MatchManager.TeamStats opponent)
    {
        int totalRecoveries = team.totalPossessionWon + opponent.totalPossessionWon;
        if (totalRecoveries <= 0)
        {
            return "50%";
        }

        float share = (float)team.totalPossessionWon / totalRecoveries;
        return $"{Mathf.RoundToInt(share * 100f)}%";
    }

    private int GetMaxLineupRowCount()
    {
        if (MatchManager.Instance == null || MatchManager.Instance.gameData?.rosters == null)
        {
            return BaselineLineupRowCount;
        }

        int homeCount = MatchManager.Instance.gameData.rosters.home?.Count ?? 0;
        int awayCount = MatchManager.Instance.gameData.rosters.away?.Count ?? 0;
        return Mathf.Max(homeCount, awayCount, 1);
    }

    private void RebuildLineupHoverRows()
    {
        lineupHoverRows.Clear();
        if (statsText == null)
        {
            return;
        }

        List<(float centerY, float ascender, float descender, LineupHoverEntry entry)> rows = new();
        for (int renderedLineIndex = 0; renderedLineIndex < statsText.textInfo.lineCount; renderedLineIndex++)
        {
            int rawLineIndex = GetRawLineIndexFromRenderedLine(renderedLineIndex);
            if (rawLineIndex < 0 || !lineupHoverEntries.TryGetValue(rawLineIndex, out LineupHoverEntry entry))
            {
                continue;
            }

            TMP_LineInfo lineInfo = statsText.textInfo.lineInfo[renderedLineIndex];
            if (lineInfo.characterCount <= 0)
            {
                continue;
            }

            rows.Add((((lineInfo.ascender + lineInfo.descender) * 0.5f), lineInfo.ascender, lineInfo.descender, entry));
        }

        for (int index = 0; index < rows.Count; index++)
        {
            float topY = index == 0
                ? rows[index].ascender
                : ((rows[index - 1].centerY + rows[index].centerY) * 0.5f);
            float bottomY = index == rows.Count - 1
                ? rows[index].descender
                : ((rows[index].centerY + rows[index + 1].centerY) * 0.5f);

            lineupHoverRows.Add(new LineupHoverRowBounds
            {
                topY = topY,
                bottomY = bottomY,
                entry = rows[index].entry,
            });
        }
    }

    private void ApplyDynamicStatsSpacing(int lineupRowCount)
    {
        if (statsText == null)
        {
            return;
        }

        int rowDelta = BaselineLineupRowCount - lineupRowCount;
        float adjustedLineSpacing = statsLineSpacing + (rowDelta * LineSpacingAdjustmentPerLineupRow);
        statsText.lineSpacing = adjustedLineSpacing;
        statsText.paragraphSpacing = statsParagraphSpacing;
    }

    private void AppendLineupsTable(StringBuilder builder, ColumnLayout layout, string homeTeamName, string awayTeamName, ref int currentLineIndex)
    {
        if (!showLineups || MatchManager.Instance == null || MatchManager.Instance.gameData?.rosters == null)
        {
            return;
        }

        List<LineupPlayerRow> homeLineup = BuildLineupRows(true);
        List<LineupPlayerRow> awayLineup = BuildLineupRows(false);
        int rowCount = Math.Max(homeLineup.Count, awayLineup.Count);
        if (rowCount == 0)
        {
            return;
        }

        AppendLine(builder, string.Empty, ref currentLineIndex);
        AppendLine(builder, BuildLineupHeaderRow(homeTeamName, awayTeamName, layout), ref currentLineIndex);

        for (int index = 0; index < rowCount; index++)
        {
            if (ShouldInsertBenchSeparator(homeLineup, awayLineup, index))
            {
                AppendLine(builder, BuildBenchSeparator(layout), ref currentLineIndex);
            }

            LineupPlayerRow homePlayer = index < homeLineup.Count ? homeLineup[index] : null;
            LineupPlayerRow awayPlayer = index < awayLineup.Count ? awayLineup[index] : null;
            string centerLabel = BuildLineupCenterLabel(homePlayer, awayPlayer);
            lineupHoverEntries[currentLineIndex] = new LineupHoverEntry
            {
                homeToken = homePlayer?.liveToken,
                awayToken = awayPlayer?.liveToken,
            };
            AppendLine(builder, BuildLineupRow(homePlayer, centerLabel, awayPlayer, layout), ref currentLineIndex);
        }
    }

    private List<LineupPlayerRow> BuildLineupRows(bool isHomeTeam)
    {
        Dictionary<string, MatchManager.RosterPlayer> roster = isHomeTeam
            ? MatchManager.Instance.gameData.rosters.home
            : MatchManager.Instance.gameData.rosters.away;

        if (roster == null)
        {
            return new List<LineupPlayerRow>();
        }

        Dictionary<int, PlayerToken> liveTokens = FindObjectsByType<PlayerToken>(FindObjectsSortMode.None)
            .Where(token => token != null && token.isHomeTeam == isHomeTeam)
            .GroupBy(token => token.jerseyNumber)
            .ToDictionary(group => group.Key, group => group.First());

        return roster
            .Select(entry =>
            {
                int jerseyNumber = int.TryParse(entry.Key, out int parsedJersey) ? parsedJersey : int.MaxValue;
                liveTokens.TryGetValue(jerseyNumber, out PlayerToken liveToken);
                MatchManager.PlayerStats playerStats = MatchManager.Instance.gameData.stats.GetPlayerStats(entry.Value.name);

                return new LineupPlayerRow
                {
                    jerseyNumber = jerseyNumber,
                    displayName = liveToken != null ? liveToken.playerName : entry.Value.name,
                    liveToken = liveToken,
                    isBooked = liveToken != null ? liveToken.isBooked : playerStats.yellowCards > 0,
                    isSentOff = liveToken != null ? liveToken.isSentOff : playerStats.redCards > 0,
                    goals = playerStats.goals,
                    assists = playerStats.assists,
                    yellowCards = playerStats.yellowCards,
                    redCards = playerStats.redCards,
                    injuries = playerStats.injuries,
                    subOns = MatchManager.Instance.GetPlayerSubOnCount(entry.Value.name),
                    subOffs = MatchManager.Instance.GetPlayerSubOffCount(entry.Value.name),
                };
            })
            .OrderBy(row => row.jerseyNumber)
            .ToList();
    }

    private static string BuildLineupCenterLabel(LineupPlayerRow homePlayer, LineupPlayerRow awayPlayer)
    {
        int jerseyNumber = homePlayer?.jerseyNumber ?? awayPlayer?.jerseyNumber ?? 0;
        return jerseyNumber > 0 ? jerseyNumber.ToString() : "-";
    }

    private static string GetLineupPlayerColor(LineupPlayerRow player)
    {
        if (player == null)
        {
            return MutedColor;
        }

        if (player.isSentOff)
        {
            return CardRedColor;
        }

        if (player.isBooked)
        {
            return CardYellowColor;
        }

        return NeutralPlayerColor;
    }

    private string BuildLineupSideText(LineupPlayerRow player, int width, bool isHomeSide, ColumnTextAlignment alignment)
    {
        if (player == null)
        {
            return AlignTo("-", width, alignment);
        }

        string decorations = BuildLineupDecorations(player);
        int decorationWidth = VisibleLength(decorations);
        int spacingWidth = string.IsNullOrEmpty(decorations) ? 0 : 1;
        int nameWidth = Mathf.Max(1, width - decorationWidth - spacingWidth);
        string coloredName = $"<color={GetLineupPlayerColor(player)}>{TrimToWidth(player.displayName, nameWidth)}</color>";

        string content = isHomeSide
            ? string.IsNullOrEmpty(decorations) ? coloredName : $"{decorations} {coloredName}"
            : string.IsNullOrEmpty(decorations) ? coloredName : $"{coloredName} {decorations}";

        return AlignRich(content, width, alignment);
    }

    private static string BuildLineupDecorations(LineupPlayerRow player)
    {
        StringBuilder builder = new();
        AppendRepeatedIcon(builder, "⬆️", player.subOns);
        AppendRepeatedIcon(builder, "⚽", player.goals);
        AppendRepeatedIcon(builder, "👟", player.assists);
        AppendRepeatedIcon(builder, "🟨", player.yellowCards);
        AppendRepeatedIcon(builder, "🟥", player.redCards);
        AppendRepeatedIcon(builder, "🚑", player.injuries);
        AppendRepeatedIcon(builder, "⬇️", player.subOffs);
        return builder.ToString();
    }

    private static void AppendRepeatedIcon(StringBuilder builder, string icon, int count)
    {
        for (int index = 0; index < count; index++)
        {
            builder.Append(icon);
        }
    }

    private static void AppendLine(StringBuilder builder, string line, ref int currentLineIndex)
    {
        builder.AppendLine(line);
        currentLineIndex++;
    }

    private static List<ScoreboardScorerSummary> BuildScoreboardScorerSummaries(List<MatchManager.GoalEvent> goalEvents)
    {
        if (goalEvents == null || goalEvents.Count == 0)
        {
            return new List<ScoreboardScorerSummary>();
        }

        return goalEvents
            .GroupBy(goal => goal.scorer)
            .Select(group => new ScoreboardScorerSummary
            {
                scorer = group.Key,
                goals = group.OrderBy(goal => goal.minute).ToList(),
                earliestMinute = group.Min(goal => goal.minute),
            })
            .OrderBy(summary => summary.earliestMinute)
            .ThenBy(summary => summary.scorer)
            .ToList();
    }

    private static string FormatScoreboardScorerSummary(ScoreboardScorerSummary summary)
    {
        if (summary == null || string.IsNullOrWhiteSpace(summary.scorer))
        {
            return string.Empty;
        }

        IEnumerable<string> minutes = summary.goals
            .OrderBy(goal => goal.minute)
            .Select(goal => $"{FormatScoreMinute(goal.minute)}{(goal.isPenalty ? "(p)" : string.Empty)}");

        return $"{summary.scorer} {string.Join(", ", minutes)}";
    }

    private static string FormatScoreMinute(int minute)
    {
        if (minute > 90)
        {
            return $"90'+{minute - 90}'";
        }

        if (minute > 45 && minute < 60)
        {
            return $"45'+{minute - 45}'";
        }

        return $"{minute}'";
    }

    private static string NormalizeMetricKey(string label)
    {
        StringBuilder builder = new(label.Length);
        foreach (char character in label)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }

    private void UpdateLineupHoverFromStatsText()
    {
        if (statsText == null || !showLineups || lineupHoverRows.Count == 0)
        {
            ClearLineupHoverOverrides();
            return;
        }

        RectTransform statsRect = statsText.rectTransform;
        if (statsRect == null)
        {
            ClearLineupHoverOverrides();
            return;
        }

        RectTransform panelRect = panel;
        if (panelRect != null && !RectTransformUtility.RectangleContainsScreenPoint(panelRect, Input.mousePosition, null))
        {
            ClearLineupHoverOverrides();
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(statsRect, Input.mousePosition, null, out Vector2 localPoint))
        {
            ClearLineupHoverOverrides();
            return;
        }

        LineupHoverRowBounds hoverRow = lineupHoverRows.FirstOrDefault(row => localPoint.y <= row.topY && localPoint.y >= row.bottomY);
        if (hoverRow == null)
        {
            ClearLineupHoverOverrides();
            return;
        }

        LineupHoverEntry hoverEntry = hoverRow.entry;

        float xFromLeft = localPoint.x + (statsRect.rect.width * statsRect.pivot.x);
        float gapWidth = VisibleLength(LineupColumnGap) * currentLineupLayout.monospaceStepPx;
        float homeStart = currentLineupLayout.sidePaddingPx;
        float homeEnd = homeStart + (currentLineupLayout.leftChars * currentLineupLayout.monospaceStepPx);
        float centerStart = homeEnd + gapWidth;
        float centerEnd = centerStart + (currentLineupLayout.centerChars * currentLineupLayout.monospaceStepPx);
        float awayStart = centerEnd + gapWidth;
        float awayEnd = awayStart + (currentLineupLayout.rightChars * currentLineupLayout.monospaceStepPx);

        if (xFromLeft >= homeStart && xFromLeft <= homeEnd)
        {
            if (hoverEntry.homeToken != null)
            {
                SetLineupHoverOverrides(hoverEntry.homeToken, null);
            }
            return;
        }

        if (xFromLeft >= awayStart && xFromLeft <= awayEnd)
        {
            if (hoverEntry.awayToken != null)
            {
                SetLineupHoverOverrides(null, hoverEntry.awayToken);
            }
            return;
        }

        return;
    }

    private int GetRawLineIndexFromRenderedLine(int renderedLineIndex)
    {
        if (statsText == null || renderedLineIndex < 0 || renderedLineIndex >= statsText.textInfo.lineCount)
        {
            return -1;
        }

        TMP_LineInfo lineInfo = statsText.textInfo.lineInfo[renderedLineIndex];
        if (lineInfo.characterCount <= 0 || lineInfo.firstCharacterIndex < 0 || lineInfo.firstCharacterIndex >= statsText.textInfo.characterCount)
        {
            return -1;
        }

        int sourceIndex = statsText.textInfo.characterInfo[lineInfo.firstCharacterIndex].index;
        string sourceText = statsText.text ?? string.Empty;
        int rawLineIndex = 0;

        for (int index = 0; index < sourceIndex && index < sourceText.Length; index++)
        {
            if (sourceText[index] == '\n')
            {
                rawLineIndex++;
            }
        }

        return rawLineIndex;
    }

    private void SetLineupHoverOverrides(PlayerToken homeToken, PlayerToken awayToken)
    {
        if (lineupHoveredHomeToken == homeToken && lineupHoveredAwayToken == awayToken)
        {
            return;
        }

        if (homeToken != null)
        {
            lastHoveredHomeToken = homeToken;
        }

        if (awayToken != null)
        {
            lastHoveredAwayToken = awayToken;
        }

        lineupHoveredHomeToken = homeToken;
        lineupHoveredAwayToken = awayToken;
        RefreshHoverCards();
    }

    private void ClearLineupHoverOverrides()
    {
        if (lineupHoveredHomeToken == null && lineupHoveredAwayToken == null)
        {
            return;
        }

        lineupHoveredHomeToken = null;
        lineupHoveredAwayToken = null;
        RefreshHoverCards();
    }

    private static string AlignLeft(string value, int width)
    {
        string trimmed = TrimToWidth(value, width);
        return trimmed.PadRight(width);
    }

    private static string AlignTo(string value, int width, ColumnTextAlignment alignment)
    {
        switch (alignment)
        {
            case ColumnTextAlignment.Left:
                return AlignLeft(value, width);
            case ColumnTextAlignment.Right:
                return AlignRight(value, width);
            default:
                return AlignCenter(value, width);
        }
    }

    private static string AlignRight(string value, int width)
    {
        string trimmed = TrimToWidth(value, width);
        return trimmed.PadLeft(width);
    }

    private static string AlignCenter(string value, int width)
    {
        string trimmed = TrimToWidth(value, width);
        int totalPadding = Math.Max(0, width - trimmed.Length);
        int padLeft = totalPadding / 2;
        int padRight = totalPadding - padLeft;
        return new string(' ', padLeft) + trimmed + new string(' ', padRight);
    }

    private static string TrimToWidth(string value, int width)
    {
        string safeValue = value ?? string.Empty;
        return safeValue.Length <= width ? safeValue : safeValue.Substring(0, width);
    }

    private static string AlignLeftRich(string value, int width)
    {
        int visibleLength = VisibleLength(value);
        return value + new string(' ', Math.Max(0, width - visibleLength));
    }

    private static string AlignRightRich(string value, int width)
    {
        int visibleLength = VisibleLength(value);
        return new string(' ', Math.Max(0, width - visibleLength)) + value;
    }

    private static string AlignCenterRich(string value, int width)
    {
        int visibleLength = VisibleLength(value);
        int totalPadding = Math.Max(0, width - visibleLength);
        int padLeft = totalPadding / 2;
        int padRight = totalPadding - padLeft;
        return new string(' ', padLeft) + value + new string(' ', padRight);
    }

    private static string AlignRich(string value, int width, ColumnTextAlignment alignment)
    {
        switch (alignment)
        {
            case ColumnTextAlignment.Left:
                return AlignLeftRich(value, width);
            case ColumnTextAlignment.Right:
                return AlignRightRich(value, width);
            default:
                return AlignCenterRich(value, width);
        }
    }

    private static int VisibleLength(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        string plain = RichTagRegex.Replace(value, string.Empty);
        return new StringInfo(plain).LengthInTextElements;
    }

    private static string TruncateLineupName(string value, int width)
    {
        return TrimToWidth(SanitizeName(value), width);
    }

    private static string SanitizeName(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Team" : value.Trim();
    }

    private ColumnLayout GetColumnLayout(float sideColumnRatio, float centerColumnRatio, int minCenterChars, bool preferOddCenter = false, float monospaceStepPxOverride = MonospaceStepPx)
    {
        float textWidth = statsText != null ? Mathf.Max(220f, statsText.rectTransform.rect.width) : 320f;
        float sidePaddingPx = textWidth * SidePaddingRatio;
        float contentWidth = Mathf.Max(180f, textWidth - (sidePaddingPx * 2f));
        float monospaceStep = Mathf.Max(3f, monospaceStepPxOverride);
        int leftChars = Mathf.Max(6, Mathf.FloorToInt((contentWidth * sideColumnRatio) / monospaceStep));
        int centerChars = Mathf.Max(minCenterChars, Mathf.FloorToInt((contentWidth * centerColumnRatio) / monospaceStep));
        if (preferOddCenter && centerChars % 2 == 0)
        {
            centerChars += 1;
        }
        int rightChars = Mathf.Max(6, Mathf.FloorToInt((contentWidth * sideColumnRatio) / monospaceStep));
        int dividerChars = Mathf.Max(18, leftChars + centerChars + rightChars);

        return new ColumnLayout(sidePaddingPx, monospaceStep, leftChars, centerChars, rightChars, dividerChars);
    }

    private void RefreshTemplateIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(statsTemplatePath) || !File.Exists(statsTemplatePath))
        {
            return;
        }

        DateTime currentWriteUtc = File.GetLastWriteTimeUtc(statsTemplatePath);
        if (currentWriteUtc > statsTemplateWriteUtc)
        {
            LoadStatsTemplate();
        }
    }

    private void RefreshTeamColors()
    {
        currentHomeColor = ResolveKitBodyColor(MatchManager.Instance.gameData.gameSettings.homeKit, HomeColor);
        currentAwayColor = ResolveKitBodyColor(MatchManager.Instance.gameData.gameSettings.awayKit, AwayColor);
    }

    private static string ResolveKitBodyColor(string kitIdOrAlias, string fallbackColor)
    {
        if (string.IsNullOrWhiteSpace(kitIdOrAlias))
        {
            return fallbackColor;
        }

        TokenKitPreset preset = TokenKitCatalog.GetPresetByIdOrAlias(kitIdOrAlias);
        if (preset == null || preset.Style == null)
        {
            return fallbackColor;
        }

        return "#" + ColorUtility.ToHtmlStringRGB(preset.Style.bodyColor);
    }

    private static bool ShouldInsertBenchSeparator(List<LineupPlayerRow> homeLineup, List<LineupPlayerRow> awayLineup, int index)
    {
        if (index <= 0)
        {
            return false;
        }

        int previousJersey = GetJerseyAtIndex(homeLineup, awayLineup, index - 1);
        int currentJersey = GetJerseyAtIndex(homeLineup, awayLineup, index);
        return previousJersey == 11 && currentJersey == 12;
    }

    private static int GetJerseyAtIndex(List<LineupPlayerRow> homeLineup, List<LineupPlayerRow> awayLineup, int index)
    {
        if (index < homeLineup.Count && homeLineup[index] != null)
        {
            return homeLineup[index].jerseyNumber;
        }

        if (index < awayLineup.Count && awayLineup[index] != null)
        {
            return awayLineup[index].jerseyNumber;
        }

        return 0;
    }

    private static string BuildBenchSeparator(ColumnLayout layout)
    {
        string left = new string(' ', layout.leftChars);
        string center = AlignCenter("-", layout.centerChars);
        string right = new string(' ', layout.rightChars);
        return
            $"<space={layout.sidePaddingPx:0.##}px><mspace={layout.monospaceStepPx:0.##}px>{left}" +
            $"  <color={DividerColor}>{center}</color>  " +
            $"{right}</mspace>";
    }

    private static bool IsAlignmentRow(string left, string center, string right)
    {
        return IsAlignmentCell(left) && IsAlignmentCell(center) && IsAlignmentCell(right);
    }

    private static bool IsAlignmentCell(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (char character in value)
        {
            if (character != '-' && character != ':')
            {
                return false;
            }
        }

        return value.Contains('-');
    }

    private static ColumnTextAlignment ParseAlignment(string cell, ColumnTextAlignment fallback)
    {
        if (string.IsNullOrWhiteSpace(cell))
        {
            return fallback;
        }

        bool alignLeft = cell.StartsWith(":");
        bool alignRight = cell.EndsWith(":");

        if (alignLeft && alignRight)
        {
            return ColumnTextAlignment.Center;
        }

        if (alignRight)
        {
            return ColumnTextAlignment.Right;
        }

        if (alignLeft)
        {
            return ColumnTextAlignment.Left;
        }

        return fallback;
    }

}
