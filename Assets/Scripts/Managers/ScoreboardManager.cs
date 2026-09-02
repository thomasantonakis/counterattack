using TMPro;
using UnityEngine;

public class ScoreboardManager : MonoBehaviour
{
    public MatchManager matchManager;  // Drag and drop the MatchManager object here
    public TMP_Text homeTeamText;  // Drag and drop the TextMeshPro element here
    public TMP_Text awayTeamText;  // Drag and drop the TextMeshPro element here
    public TMP_Text timeText;  // Drag and drop the TextMeshPro element here
    public TMP_Text homeScoreText;  // Drag and drop the TextMeshPro element here
    public TMP_Text awayScoreText;  // Drag and drop the TextMeshPro element here

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
        }
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
