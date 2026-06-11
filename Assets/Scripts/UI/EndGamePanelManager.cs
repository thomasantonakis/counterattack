using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndGamePanelManager : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const int StatsRowCount = 21;
    private static readonly string[] StatRowTitles =
    {
        "ATTACKING",
        "Total Shots / xG",
        "On Target / Corners",
        "Blocked / Off Target",
        "PASSING",
        "Assists",
        "Ground (Att./Made)",
        "Aerial(Att./Trg./Made)",
        "Game Play",
        "Distance Covered",
        "Possession",
        "DUELS",
        "Ground Att. / Won",
        "Air Att. / Won",
        "DISCIPLINE",
        "Yellow / Red Cards",
        "Injuries / Subs Used",
        "OPTA STATS",
        "xRecoveries / Made",
        "xDribbles / Made",
        "xTackles / Made",
    };

    [Header("Designer-owned view")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text recapText;
    [SerializeField] private TMP_Text homeTeamScoreText;
    [SerializeField] private TMP_Text centerScoreText;
    [SerializeField] private TMP_Text awayTeamScoreText;
    [SerializeField] private TMP_Text homeScorersText;
    [SerializeField] private TMP_Text awayScorersText;
    [SerializeField] private ScrollRect recapScroll;
    [SerializeField] private TMP_Text[] homeStatsTexts = new TMP_Text[StatsRowCount];
    [SerializeField] private TMP_Text[] statTitleTexts = new TMP_Text[StatsRowCount];
    [SerializeField] private TMP_Text[] awayStatsTexts = new TMP_Text[StatsRowCount];
    [SerializeField] private Button mainMenuButton;

    private string titleOverride;
    private string recapOverride;
    private string scoreLineOverride;

    public static void ShowMatchEndedPanel()
    {
        EndGamePanelManager manager = ResolveExisting(logIfMissing: true);
        if (manager == null)
        {
            return;
        }

        manager.titleOverride = null;
        manager.recapOverride = null;
        manager.scoreLineOverride = manager.BuildMatchEndedScoreLineOverride();
        manager.ShowPanel();
    }

    public static void ShowPenaltyShootoutPendingPanel()
    {
        EndGamePanelManager manager = ResolveExisting(logIfMissing: true);
        if (manager == null)
        {
            return;
        }

        manager.titleOverride = "Penalties";
        manager.recapOverride = "The match is tied after extra time. Penalty shootout flow is pending.";
        manager.scoreLineOverride = null;
        manager.ShowPanel();
    }

    public static void ShowPenaltyShootoutCompletePanel(string finalScoreLine)
    {
        EndGamePanelManager manager = ResolveExisting(logIfMissing: true);
        if (manager == null)
        {
            return;
        }

        manager.titleOverride = "Full Time";
        manager.recapOverride = null;
        manager.scoreLineOverride = finalScoreLine;
        manager.ShowPanel();
    }

    public static void EnsureScenePanel()
    {
        ResolveExisting(logIfMissing: false)?.EnsurePanelReferences();
    }

    public void ConfigureReferences(
        GameObject configuredRoot,
        TMP_Text configuredTitleText,
        TMP_Text configuredRecapText,
        TMP_Text configuredHomeTeamScoreText,
        TMP_Text configuredCenterScoreText,
        TMP_Text configuredAwayTeamScoreText,
        TMP_Text configuredHomeScorersText,
        TMP_Text configuredAwayScorersText,
        ScrollRect configuredRecapScroll,
        TMP_Text[] configuredHomeStatsTexts,
        TMP_Text[] configuredStatTitleTexts,
        TMP_Text[] configuredAwayStatsTexts,
        Button configuredMainMenuButton)
    {
        root = configuredRoot;
        titleText = configuredTitleText;
        recapText = configuredRecapText;
        homeTeamScoreText = configuredHomeTeamScoreText;
        centerScoreText = configuredCenterScoreText;
        awayTeamScoreText = configuredAwayTeamScoreText;
        homeScorersText = configuredHomeScorersText;
        awayScorersText = configuredAwayScorersText;
        recapScroll = configuredRecapScroll;
        homeStatsTexts = configuredHomeStatsTexts;
        statTitleTexts = configuredStatTitleTexts;
        awayStatsTexts = configuredAwayStatsTexts;
        mainMenuButton = configuredMainMenuButton;
        EnsureButtonListener();
    }

    private static EndGamePanelManager ResolveExisting(bool logIfMissing)
    {
        EndGamePanelManager manager = FindObjectsByType<EndGamePanelManager>(FindObjectsInactive.Include)
            .FirstOrDefault();
        if (manager == null)
        {
            if (logIfMissing)
            {
                Debug.LogError("[EndGamePanel] Missing designer-owned EndGamePanelManager under the scene Canvas. Use Tools/Counter Attack/Ensure End Game Panel In Scene.");
            }

            return null;
        }

        manager.EnsurePanelReferences();
        return manager;
    }

    private void Awake()
    {
        EnsurePanelReferences();
    }

    private void ShowPanel()
    {
        if (!EnsurePanelReferences())
        {
            return;
        }

        titleText.text = string.IsNullOrWhiteSpace(titleOverride) ? "Full Time" : titleOverride;
        UpdateScoreRecapFields();
        UpdateStatsFields(showStats: string.IsNullOrWhiteSpace(recapOverride));
        HideLiveMatchUi();
        root.SetActive(true);
        Canvas.ForceUpdateCanvases();

        if (recapScroll != null)
        {
            recapScroll.verticalNormalizedPosition = 1f;
        }

        Debug.Log("[EndGamePanel] Match recap panel shown.");
    }

    private bool EnsurePanelReferences()
    {
        if (root == null)
        {
            root = gameObject;
        }

        AutoBindMissingReferences();
        EnsureButtonListener();

        if (root != null && titleText != null && recapText != null && HasStatsFieldReferences())
        {
            return true;
        }

        Debug.LogError("[EndGamePanel] Scene panel references are incomplete. The designer-owned panel was not shown.");
        return false;
    }

    private void AutoBindMissingReferences()
    {
        if (titleText == null)
        {
            titleText = transform.Find("RecapPanel/Title")?.GetComponent<TMP_Text>()
                ?? GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(text => text != null && text.name == "Title");
        }

        if (recapText == null)
        {
            recapText = transform.Find("RecapPanel/RecapScroll/Viewport/Content/ScoreRecapText")?.GetComponent<TMP_Text>()
                ?? transform.Find("RecapPanel/RecapScroll/Viewport/Content")?.GetComponent<TMP_Text>()
                ?? GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(text => text != null && (text.name == "ScoreRecapText" || text.name == "Content"));
        }

        BindScoreRecapTextFields();

        if (recapScroll == null)
        {
            recapScroll = GetComponentInChildren<ScrollRect>(true);
        }

        BindStatsTextArray("RecapPanel/RecapScroll/Viewport/Content/StatsGrid/HomeStats", "HomeStat_", ref homeStatsTexts);
        BindStatsTextArray("RecapPanel/RecapScroll/Viewport/Content/StatsGrid/StatTitles", "StatTitle_", ref statTitleTexts);
        BindStatsTextArray("RecapPanel/RecapScroll/Viewport/Content/StatsGrid/AwayStats", "AwayStat_", ref awayStatsTexts);

        if (mainMenuButton == null)
        {
            mainMenuButton = GetComponentsInChildren<Button>(true).FirstOrDefault(button => button != null && button.name == "BackToMainMenuButton");
        }
    }

    private void BindScoreRecapTextFields()
    {
        homeTeamScoreText ??= transform.Find("RecapPanel/RecapScroll/Viewport/Content/ScoreRecap/ScoreRow/HomeTeamText")?.GetComponent<TMP_Text>()
            ?? GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(text => text != null && text.name == "HomeTeamText");
        centerScoreText ??= transform.Find("RecapPanel/RecapScroll/Viewport/Content/ScoreRecap/ScoreRow/CenterScoreText")?.GetComponent<TMP_Text>()
            ?? GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(text => text != null && text.name == "CenterScoreText");
        awayTeamScoreText ??= transform.Find("RecapPanel/RecapScroll/Viewport/Content/ScoreRecap/ScoreRow/AwayTeamText")?.GetComponent<TMP_Text>()
            ?? GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(text => text != null && text.name == "AwayTeamText");
        homeScorersText ??= transform.Find("RecapPanel/RecapScroll/Viewport/Content/ScoreRecap/ScorersRow/HomeScorersText")?.GetComponent<TMP_Text>()
            ?? GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(text => text != null && text.name == "HomeScorersText");
        awayScorersText ??= transform.Find("RecapPanel/RecapScroll/Viewport/Content/ScoreRecap/ScorersRow/AwayScorersText")?.GetComponent<TMP_Text>()
            ?? GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(text => text != null && text.name == "AwayScorersText");
    }

    private void BindStatsTextArray(string parentPath, string itemPrefix, ref TMP_Text[] target)
    {
        if (target == null || target.Length != StatsRowCount)
        {
            target = new TMP_Text[StatsRowCount];
        }

        Transform parent = transform.Find(parentPath);
        for (int i = 0; i < StatsRowCount; i++)
        {
            if (target[i] != null)
            {
                continue;
            }

            string childName = $"{itemPrefix}{i + 1:00}";
            target[i] = parent?.Find(childName)?.GetComponent<TMP_Text>()
                ?? GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(text => text != null && text.name == childName);
        }
    }

    private bool HasStatsFieldReferences()
    {
        return HasCompleteTextArray(homeStatsTexts)
            && HasCompleteTextArray(statTitleTexts)
            && HasCompleteTextArray(awayStatsTexts);
    }

    private static bool HasCompleteTextArray(TMP_Text[] texts)
    {
        return texts != null && texts.Length >= StatsRowCount && texts.Take(StatsRowCount).All(text => text != null);
    }

    private bool HasScoreRecapReferences()
    {
        return recapText != null
            && homeTeamScoreText != null
            && centerScoreText != null
            && awayTeamScoreText != null
            && homeScorersText != null
            && awayScorersText != null;
    }

    private void EnsureButtonListener()
    {
        if (mainMenuButton == null)
        {
            return;
        }

        mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void UpdateScoreRecapFields()
    {
        MatchManager matchManager = MatchManager.Instance;
        if (matchManager == null || matchManager.gameData == null)
        {
            recapText.text = "Match data not available";
            ClearOptionalScoreRecapFields();
            return;
        }

        string homeTeamName = matchManager.gameData.gameSettings.homeTeamName;
        string awayTeamName = matchManager.gameData.gameSettings.awayTeamName;
        int homeGoals = matchManager.gameData.stats.homeTeamStats.totalGoals;
        int awayGoals = matchManager.gameData.stats.awayTeamStats.totalGoals;
        string centerScore = string.IsNullOrWhiteSpace(scoreLineOverride)
            ? $"{homeGoals} - {awayGoals}"
            : ExtractCenterScore(scoreLineOverride, homeTeamName, awayTeamName);

        if (!string.IsNullOrWhiteSpace(recapOverride))
        {
            recapText.text = recapOverride;
            ClearOptionalScoreRecapFields();
            return;
        }

        string homeScorers = FormatScorerRows(matchManager.homeScorers);
        string awayScorers = FormatScorerRows(matchManager.awayScorers);
        if (HasScoreRecapReferences())
        {
            recapText.text = string.Empty;
            homeTeamScoreText.text = homeTeamName;
            centerScoreText.text = centerScore;
            awayTeamScoreText.text = awayTeamName;
            homeScorersText.text = homeScorers;
            awayScorersText.text = awayScorers;
            return;
        }

        string fallbackScoreLine = $"{homeTeamName} {centerScore} {awayTeamName}";
        string fallbackScorers = string.Join("\n", new[] { homeScorers, awayScorers }.Where(value => !string.IsNullOrWhiteSpace(value)));
        recapText.text = string.IsNullOrWhiteSpace(fallbackScorers)
            ? fallbackScoreLine
            : $"{fallbackScoreLine}\n{fallbackScorers}";
    }

    private void ClearOptionalScoreRecapFields()
    {
        if (homeTeamScoreText != null) homeTeamScoreText.text = string.Empty;
        if (centerScoreText != null) centerScoreText.text = string.Empty;
        if (awayTeamScoreText != null) awayTeamScoreText.text = string.Empty;
        if (homeScorersText != null) homeScorersText.text = string.Empty;
        if (awayScorersText != null) awayScorersText.text = string.Empty;
    }

    private static string ExtractCenterScore(string scoreLine, string homeTeamName, string awayTeamName)
    {
        string center = scoreLine.Trim();
        if (!string.IsNullOrWhiteSpace(homeTeamName) && center.StartsWith(homeTeamName, System.StringComparison.Ordinal))
        {
            center = center[homeTeamName.Length..].TrimStart();
        }

        if (!string.IsNullOrWhiteSpace(awayTeamName) && center.EndsWith(awayTeamName, System.StringComparison.Ordinal))
        {
            center = center[..^awayTeamName.Length].TrimEnd();
        }

        return center;
    }

    private static string FormatScorerRows(System.Collections.Generic.List<MatchManager.GoalEvent> scorers)
    {
        if (scorers == null || scorers.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            "\n",
            scorers
                .Where(goal => goal != null && !string.IsNullOrWhiteSpace(goal.scorer))
                .GroupBy(goal => goal.scorer)
                .Select(group => new
                {
                    Scorer = group.Key,
                    Goals = group.OrderBy(goal => goal.minute).ToList(),
                    FirstMinute = group.Min(goal => goal.minute),
                })
                .OrderBy(summary => summary.FirstMinute)
                .ThenBy(summary => summary.Scorer)
                .Select(summary => $"{summary.Scorer} {string.Join(", ", summary.Goals.Select(FormatGoalMinute))}"));
    }

    private static string FormatGoalMinute(MatchManager.GoalEvent goal)
    {
        string minute = string.IsNullOrWhiteSpace(goal.minuteLabel) ? $"{goal.minute}'" : goal.minuteLabel;
        return goal.isPenalty ? $"{minute} (p)" : minute;
    }

    private static void HideLiveMatchUi()
    {
        MatchStatsUI statsUI = FindObjectsByType<MatchStatsUI>(FindObjectsInactive.Include)
            .FirstOrDefault();
        if (statsUI != null)
        {
            statsUI.MoveOffScreenForEndGame();
        }

        ScoreboardManager scoreboardManager = FindObjectsByType<ScoreboardManager>(FindObjectsInactive.Include)
            .FirstOrDefault();
        if (scoreboardManager != null)
        {
            scoreboardManager.gameObject.SetActive(false);
        }
    }

    private void UpdateStatsFields(bool showStats)
    {
        if (!HasStatsFieldReferences())
        {
            return;
        }

        MatchManager matchManager = MatchManager.Instance;
        MatchManager.TeamStats homeTeam = matchManager?.gameData?.stats?.homeTeamStats;
        MatchManager.TeamStats awayTeam = matchManager?.gameData?.stats?.awayTeamStats;

        for (int i = 0; i < StatsRowCount; i++)
        {
            string homeValue = string.Empty;
            string awayValue = string.Empty;
            if (showStats && homeTeam != null && awayTeam != null)
            {
                (homeValue, awayValue) = ResolveEndGameStatValues(StatRowTitles[i], homeTeam, awayTeam);
            }

            homeStatsTexts[i].text = homeValue;
            awayStatsTexts[i].text = awayValue;
        }
    }

    private static (string homeValue, string awayValue) ResolveEndGameStatValues(string label, MatchManager.TeamStats homeTeam, MatchManager.TeamStats awayTeam)
    {
        switch (NormalizeMetricKey(label))
        {
            case "attacking":
            case "passing":
            case "gameplay":
            case "duels":
            case "discipline":
            case "optastats":
                return (string.Empty, string.Empty);
            case "totalshotsxg":
                return (FormatShotsAndExpectedGoals(homeTeam), FormatShotsAndExpectedGoals(awayTeam));
            case "ontargetcorners":
                return ($"{homeTeam.totalShotsOnTarget}/{homeTeam.totalCorners}", $"{awayTeam.totalShotsOnTarget}/{awayTeam.totalCorners}");
            case "blockedofftarget":
                return ($"{homeTeam.totalShotsBlocked}/{homeTeam.totalShotsOffTarget}", $"{awayTeam.totalShotsBlocked}/{awayTeam.totalShotsOffTarget}");
            case "assists":
                return ($"{homeTeam.totalAssists}", $"{awayTeam.totalAssists}");
            case "groundattmade":
                return (FormatAttemptedMade(homeTeam.totalPassesAttempted, homeTeam.totalPassesCompleted), FormatAttemptedMade(awayTeam.totalPassesAttempted, awayTeam.totalPassesCompleted));
            case "aerialatttrgmade":
                return (FormatAerialTriplet(homeTeam), FormatAerialTriplet(awayTeam));
            case "distancecovered":
                return ($"{homeTeam.totalPacesRan}", $"{awayTeam.totalPacesRan}");
            case "possession":
                return (FormatPossession(homeTeam, awayTeam), FormatPossession(awayTeam, homeTeam));
            case "groundattwon":
                return (FormatAttemptedMade(homeTeam.totalGroundDuelsInvolved, homeTeam.totalGroundDuelsWon), FormatAttemptedMade(awayTeam.totalGroundDuelsInvolved, awayTeam.totalGroundDuelsWon));
            case "airattwon":
                return (FormatAttemptedMade(homeTeam.totalAerialChallengesInvolved, homeTeam.totalAerialChallengesWon), FormatAttemptedMade(awayTeam.totalAerialChallengesInvolved, awayTeam.totalAerialChallengesWon));
            case "yellowredcards":
                return ($"{homeTeam.totalYellowCards}/{homeTeam.totalRedCards}", $"{awayTeam.totalYellowCards}/{awayTeam.totalRedCards}");
            case "injuriessubsused":
                return ($"{homeTeam.totalInjuries}/{homeTeam.totalSubstiutions}", $"{awayTeam.totalInjuries}/{awayTeam.totalSubstiutions}");
            case "xrecoveriesmade":
                return (FormatExpectedAndMade(homeTeam.totalXRecoveries, homeTeam.totalPossessionWon), FormatExpectedAndMade(awayTeam.totalXRecoveries, awayTeam.totalPossessionWon));
            case "xdribblesmade":
                return (FormatExpectedAndMade(homeTeam.totalXDribbles, homeTeam.totalDribblesMade), FormatExpectedAndMade(awayTeam.totalXDribbles, awayTeam.totalDribblesMade));
            case "xtacklesmade":
                return (FormatExpectedAndMade(homeTeam.totalXTackles, homeTeam.totalTacklesMade), FormatExpectedAndMade(awayTeam.totalXTackles, awayTeam.totalTacklesMade));
            default:
                return ("-", "-");
        }
    }

    private static string NormalizeMetricKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        return normalized;
    }

    private static string FormatAttemptedMade(int attempted, int made)
    {
        return $"{attempted}/{made}";
    }

    private static string FormatAerialTriplet(MatchManager.TeamStats stats)
    {
        return $"{stats.totalAerialPassesAttempted}/{stats.totalAerialPassesTargeted}/{stats.totalAerialPassesCompleted}";
    }

    private static string FormatShotsAndExpectedGoals(MatchManager.TeamStats stats)
    {
        return $"{stats.totalShots}/{stats.totalXGoals:0.00}";
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

    private string BuildMatchEndedScoreLineOverride()
    {
        MatchManager matchManager = MatchManager.Instance;
        if (matchManager == null || matchManager.gameData == null || !MatchUsedExtraTime(matchManager))
        {
            return null;
        }

        string homeTeamName = matchManager.gameData.gameSettings.homeTeamName;
        string awayTeamName = matchManager.gameData.gameSettings.awayTeamName;
        int homeGoals = matchManager.gameData.stats.homeTeamStats.totalGoals;
        int awayGoals = matchManager.gameData.stats.awayTeamStats.totalGoals;
        (int regulationHomeGoals, int regulationAwayGoals) = CountRegulationGoals(matchManager);
        return $"{homeTeamName} a.e.t {homeGoals} ({regulationHomeGoals}-{regulationAwayGoals}) {awayGoals} {awayTeamName}";
    }

    private static bool MatchUsedExtraTime(MatchManager matchManager)
    {
        string tiebreaker = matchManager.gameData?.gameSettings?.tiebreaker ?? string.Empty;
        return matchManager.currentHalf > 2
            && tiebreaker.IndexOf("Extra Time", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static (int homeGoals, int awayGoals) CountRegulationGoals(MatchManager matchManager)
    {
        int homeGoals = matchManager?.homeScorers?.Count(goal => goal != null && goal.minute <= 90) ?? 0;
        int awayGoals = matchManager?.awayScorers?.Count(goal => goal != null && goal.minute <= 90) ?? 0;
        return (homeGoals, awayGoals);
    }

    private void ReturnToMainMenu()
    {
        MatchManager.Instance?.SetPauseMenuOpen(false);
        SceneManager.LoadScene(MainMenuSceneName);
    }
}
