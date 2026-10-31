using TMPro;
using UnityEngine;

public class ScoreboardManager : MonoBehaviour
{
    private const float TimerPanelHorizontalPadding = 16f;
    private const float TimerPanelVerticalPadding = 12f;

    public MatchManager matchManager;  // Drag and drop the MatchManager object here
    public TMP_Text homeTeamText;  // Drag and drop the TextMeshPro element here
    public TMP_Text awayTeamText;  // Drag and drop the TextMeshPro element here
    public TMP_Text timeText;  // Drag and drop the TextMeshPro element here
    public TMP_Text homeScoreText;  // Drag and drop the TextMeshPro element here
    public TMP_Text awayScoreText;  // Drag and drop the TextMeshPro element here

    private RectTransform timerPanelRect;

    private void OnEnable()
    {
        EnsureLayout();

        if (matchManager == null)
        {
            matchManager = MatchManager.Instance ?? FindAnyObjectByType<MatchManager>();
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
            matchManager = MatchManager.Instance ?? FindAnyObjectByType<MatchManager>();
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

            TeamNameAbbreviator.EnsureAbbreviations(settings);
            homeTeamText.text = settings.homeTeamAbbreviation;
            awayTeamText.text = settings.awayTeamAbbreviation;
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
            matchManager = MatchManager.Instance ?? FindAnyObjectByType<MatchManager>();
        }

        if (homeScoreText == null || awayScoreText == null || timeText == null)
        {
            EnsureLayout();
        }

        if (PenaltyShootoutPresentation.TryGetDisplayState(matchManager, out PenaltyShootoutDisplayState shootoutState))
        {
            if (homeScoreText != null)
            {
                homeScoreText.text = $"({shootoutState.homeBaseGoals}) {shootoutState.homePenaltyGoals}";
            }

            if (awayScoreText != null)
            {
                awayScoreText.text = $"{shootoutState.awayPenaltyGoals} ({shootoutState.awayBaseGoals})";
            }

            if (timeText != null)
            {
                timeText.text = shootoutState.clockText;
                FitTimerPanelToClockText();
            }

            return;
        }

        int homeGoals = matchManager?.gameData?.stats?.homeTeamStats.totalGoals ?? 0;
        int awayGoals = matchManager?.gameData?.stats?.awayTeamStats.totalGoals ?? 0;

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
            timeText.text = matchManager != null ? matchManager.GetClockDisplayText() : "00:00";
            FitTimerPanelToClockText();
        }
    }

    private void FitTimerPanelToClockText()
    {
        if (timeText == null)
        {
            return;
        }

        RectTransform textRect = timeText.rectTransform;
        if (timerPanelRect == null)
        {
            timerPanelRect = textRect.parent as RectTransform;
        }

        if (timerPanelRect == null)
        {
            return;
        }

        timeText.ForceMeshUpdate();
        Vector2 preferredTextSize = timeText.GetPreferredValues(timeText.text);
        preferredTextSize.x = Mathf.Ceil(preferredTextSize.x);
        preferredTextSize.y = Mathf.Ceil(preferredTextSize.y);

        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredTextSize.x);
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredTextSize.y);
        timerPanelRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            preferredTextSize.x + TimerPanelHorizontalPadding);
        timerPanelRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            preferredTextSize.y + TimerPanelVerticalPadding);
    }

    private void EnsureLayout()
    {
        RectTransform root = transform as RectTransform;
        if (root == null || homeTeamText == null || awayTeamText == null)
        {
            Debug.LogError("ScoreboardManager: homeTeamText or awayTeamText reference are missing.");
            return;
        }
    }
}
