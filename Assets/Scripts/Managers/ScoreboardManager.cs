using TMPro;
using UnityEngine;

public class ScoreboardManager : MonoBehaviour
{
    public MatchManager matchManager;  // Drag and drop the MatchManager object here
    public TMP_Text homeTeamText;  // Drag and drop the TextMeshPro element here
    public TMP_Text awayTeamText;  // Drag and drop the TextMeshPro element here

    private TMP_Text separatorText;
    private TMP_Text homeScoreText;
    private TMP_Text awayScoreText;
    private TMP_Text timeText;

    private void OnEnable()
    {
        EnsureLayout();

        if (matchManager == null)
        {
            matchManager = MatchManager.Instance ?? FindFirstObjectByType<MatchManager>();
        }

        if (matchManager == null)
        {
            Debug.LogError("ScoreboardManager: MatchManager reference is missing.");
            return;
        }

        matchManager.OnGameSettingsLoaded += LoadTeamNames;

        // Room direct-play can load settings before this component's Start/OnEnable ordering settles.
        // Apply the already-loaded names immediately when data is present.
        if (matchManager.gameData?.gameSettings != null)
        {
            LoadTeamNames();
        }

        UpdateScoreboardData();
        CancelInvoke(nameof(UpdateScoreboardData));
        InvokeRepeating(nameof(UpdateScoreboardData), 1f, 1f);
    }

    private void OnDisable()
    {
        if (matchManager != null)
        {
            matchManager.OnGameSettingsLoaded -= LoadTeamNames;
        }

        CancelInvoke(nameof(UpdateScoreboardData));
    }

    void LoadTeamNames()
    {
        Debug.Log("ScoreboardManager: Running LoadTeamNames");
        if (matchManager == null)
        {
            matchManager = MatchManager.Instance ?? FindFirstObjectByType<MatchManager>();
        }

        if (homeTeamText == null || awayTeamText == null)
        {
            Debug.LogError("ScoreboardManager: Team text references are missing.");
            return;
        }

        // Use the resolved MatchManager reference instead of reaching back through the singleton
        // during early scene initialization, where Instance may not be ready yet.
        if (matchManager != null && matchManager.gameData?.gameSettings != null)
        {
            MatchManager.GameSettings settings = matchManager.gameData.gameSettings;

            string homeTeamName = settings.homeTeamName;
            string awayTeamName = settings.awayTeamName;
            homeTeamText.text = homeTeamName;
            awayTeamText.text = awayTeamName;
        }
        else
        {
            Debug.LogError("ScoreboardManager: Game settings are not loaded!");
        }
    }

    private void UpdateScoreboardData()
    {
        if (matchManager == null)
        {
            matchManager = MatchManager.Instance ?? FindFirstObjectByType<MatchManager>();
        }

        if (homeScoreText == null || awayScoreText == null || timeText == null)
        {
            EnsureLayout();
        }

        int homeGoals = matchManager?.gameData?.stats?.homeTeamStats.totalGoals ?? 0;
        int awayGoals = matchManager?.gameData?.stats?.awayTeamStats.totalGoals ?? 0;

        if (separatorText != null)
        {
            separatorText.text = "-";
        }

        if (homeScoreText != null)
        {
            homeScoreText.text = homeGoals.ToString();
        }

        if (awayScoreText != null)
        {
            awayScoreText.text = awayGoals.ToString();
        }

        if (timeText != null)
        {
            timeText.text = "00:00";
        }
    }

    private void EnsureLayout()
    {
        RectTransform root = transform as RectTransform;
        if (root == null || homeTeamText == null || awayTeamText == null)
        {
            return;
        }

        ConfigureNameText(homeTeamText, new Vector2(0f, 1f), new Vector2(0.46f, 1f), new Vector2(0f, 1f), new Vector2(0f, -6f), TextAlignmentOptions.TopRight);
        ConfigureNameText(awayTeamText, new Vector2(0.54f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -6f), TextAlignmentOptions.TopLeft);

        separatorText = EnsureText(separatorText, "ScoreboardSeparator", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(30f, 34f));
        homeScoreText = EnsureText(homeScoreText, "HomeScoreText", root, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(185f, 34f));
        awayScoreText = EnsureText(awayScoreText, "AwayScoreText", root, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(185f, 34f));
        timeText = EnsureText(timeText, "TimeText", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(92f, 28f));

        ConfigureSecondaryText(separatorText, 24f, TextAlignmentOptions.Center);
        ConfigureSecondaryText(homeScoreText, 28f, TextAlignmentOptions.Center);
        ConfigureSecondaryText(awayScoreText, 28f, TextAlignmentOptions.Center);
        ConfigureSecondaryText(timeText, 22f, TextAlignmentOptions.Center);
    }

    private void ConfigureNameText(TMP_Text text, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, TextAlignmentOptions alignment)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(0f, 34f);

        text.alignment = alignment;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 26f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.characterSpacing = 0f;
    }

    private static TMP_Text EnsureText(TMP_Text existing, string objectName, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        TMP_Text text = existing;
        if (text == null)
        {
            Transform child = parent.Find(objectName);
            if (child != null)
            {
                text = child.GetComponent<TMP_Text>();
            }
        }

        if (text == null)
        {
            GameObject gameObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            text = gameObject.GetComponent<TextMeshProUGUI>();
        }

        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        text.raycastTarget = false;
        text.color = Color.white;
        return text;
    }

    private static void ConfigureSecondaryText(TMP_Text text, float fontSizeMax, TextAlignmentOptions alignment)
    {
        if (text == null)
        {
            return;
        }

        text.alignment = alignment;
        text.enableAutoSizing = true;
        text.fontSizeMin = fontSizeMax - 6f;
        text.fontSizeMax = fontSizeMax;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.characterSpacing = 0f;
    }
}
