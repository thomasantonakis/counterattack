using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Only controls the authored post-game view. Does not touch the live MatchStatsUI.</summary>
public sealed class PostMatchReportController : MonoBehaviour
{
    [Serializable]
    public sealed class PlayerRow
    {
        public GameObject root;
        public TMP_Text rank;
        public TMP_Text player;
        public TMP_Text team;
        public TMP_Text value;
    }

    [SerializeField] private GameObject statsPage;
    [SerializeField] private GameObject playersPage;
    [SerializeField] private GameObject tabBar;
    [SerializeField] private UnityEngine.UI.Button statsTab;
    [SerializeField] private UnityEngine.UI.Button playersTab;
    [SerializeField] private UnityEngine.UI.Button pitchTab;
    [SerializeField] private PostMatchPitchView pitchPage;
    [SerializeField] private UnityEngine.UI.Button luckTab;
    [SerializeField] private PostMatchLuckView luckPage;
    [SerializeField] private UnityEngine.UI.Button ratingsTab;
    [SerializeField] private PostMatchRatingView ratingsPage;
    [SerializeField] private UnityEngine.UI.Button[] metricButtons;
    [SerializeField] private PostMatchLeaderboard.Metric[] metricKinds;
    [SerializeField] private UnityEngine.UI.Button[] teamButtons;
    [SerializeField] private TMP_Text heading;
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text metricColumnTitle;
    [SerializeField] private CanvasGroup homeStatsColumn;
    [SerializeField] private CanvasGroup awayStatsColumn;
    [SerializeField] private TMP_Text emptyMessage;
    [SerializeField] private TMP_Text pageLabel;
    [SerializeField] private UnityEngine.UI.Button previousPage;
    [SerializeField] private UnityEngine.UI.Button nextPage;
    [SerializeField] private PlayerRow[] rows;

    private readonly List<(UnityEngine.UI.Button button, UnityAction action)> listeners = new();
    private MatchManager.GameData matchData;
    private PostMatchLeaderboard.Metric metric;
    private PostMatchLeaderboard.Team team;
    private int page;
    private bool reportAvailable = true;
    private bool historyAvailable = true;
    public bool ShowingPlayers { get; private set; }
    public bool ShowingPitch { get; private set; }
    public bool ShowingLuck { get; private set; }
    public bool ShowingRatings { get; private set; }
    public void ConfigureLuck(UnityEngine.UI.Button button, PostMatchLuckView view) { luckTab = button; luckPage = view; }
    public void ConfigureRatings(UnityEngine.UI.Button button, PostMatchRatingView view) { ratingsTab = button; ratingsPage = view; }

    public void ConfigurePitch(UnityEngine.UI.Button button, PostMatchPitchView view)
    { pitchTab = button; pitchPage = view; }

    public void Configure(GameObject stats, GameObject players, GameObject tabs,
        UnityEngine.UI.Button statsButton, UnityEngine.UI.Button playersButton,
        UnityEngine.UI.Button[] metrics, UnityEngine.UI.Button[] teams,
        TMP_Text title, TMP_Text help, TMP_Text empty, TMP_Text paging,
        UnityEngine.UI.Button previous, UnityEngine.UI.Button next, PlayerRow[] playerRows,
        PostMatchLeaderboard.Metric[] kinds, TMP_Text valueHeading, CanvasGroup homeColumn, CanvasGroup awayColumn)
    {
        Unbind();
        statsPage = stats; playersPage = players; tabBar = tabs;
        statsTab = statsButton; playersTab = playersButton;
        metricButtons = metrics; teamButtons = teams;
        metricKinds = kinds; metricColumnTitle = valueHeading;
        homeStatsColumn = homeColumn; awayStatsColumn = awayColumn;
        heading = title; description = help; emptyMessage = empty; pageLabel = paging;
        previousPage = previous; nextPage = next; rows = playerRows;
        if (isActiveAndEnabled) Bind();
    }

    public void ShowReport(MatchManager.GameData data, bool available)
    {
        // Explicit initialization also supports inspecting the authored report in Edit Mode.
        // Rebinding owns only our listeners and is safe after a runtime OnEnable callback.
        Bind();
        matchData = data;
        reportAvailable = available;
        SetHistoryAvailable(true);
        metric = PostMatchLeaderboard.Metric.Distance;
        team = PostMatchLeaderboard.Team.Both;
        page = 0;
        if (tabBar != null) tabBar.SetActive(available);
        SetTeamLabel(1, data?.gameSettings?.homeTeamName, "Home");
        SetTeamLabel(2, data?.gameSettings?.awayTeamName, "Away");
        RefreshTeamColumns();
        ShowStats();
        RefreshPlayers();
        if (pitchPage != null) pitchPage.Show(data, team, reset: true);
        if (luckPage != null) luckPage.Show(data);
        if (ratingsPage != null) ratingsPage.Show(data);
    }

    internal void SetHistoryAvailable(bool available)
    {
        historyAvailable = available;
        if (pitchTab != null) pitchTab.interactable = available;
        if (luckTab != null) luckTab.interactable = available;
    }

    internal void ApplyOnlineHistory(IReadOnlyList<GameplayEvent> events, int half)
    {
        if (matchData == null) return;
        matchData.events = new List<GameplayEvent>(events);
        matchData.runtimeSnapshot = new RoomRuntimeSnapshot { clock = new RoomClockSnapshot { currentHalf = half } };
        SetHistoryAvailable(true);
        if (pitchPage != null) pitchPage.Show(matchData, team);
        if (luckPage != null) luckPage.Show(matchData);
    }

    private void OnEnable() => Bind();
    private void OnDisable() => Unbind();

    private void Bind()
    {
        Unbind();
        Listen(statsTab, ShowStats);
        Listen(playersTab, ShowPlayers);
        Listen(pitchTab, ShowPitch);
        Listen(luckTab, ShowLuck);
        Listen(ratingsTab, ShowRatings);
        for (int i = 0; i < (metricButtons?.Length ?? 0); i++)
        {
            if (metricKinds == null || i >= metricKinds.Length) continue;
            var selected = metricKinds[i];
            Listen(metricButtons[i], () => SelectMetric(selected));
        }
        for (int i = 0; i < (teamButtons?.Length ?? 0); i++)
        {
            var selected = (PostMatchLeaderboard.Team)i;
            Listen(teamButtons[i], () => SelectTeam(selected));
        }
        Listen(previousPage, () => { page = Mathf.Max(0, page - 1); RefreshPlayers(); });
        Listen(nextPage, () => { page++; RefreshPlayers(); });
    }

    private void Listen(UnityEngine.UI.Button button, UnityAction action)
    {
        if (button == null) return;
        button.onClick.AddListener(action);
        listeners.Add((button, action));
    }

    private void Unbind()
    {
        foreach (var listener in listeners)
            if (listener.button != null) listener.button.onClick.RemoveListener(listener.action);
        listeners.Clear();
    }

    public void ShowStats() => SelectTab(0);
    public void ShowPlayers()
    {
        if (reportAvailable) SelectTab(1);
    }

    public void ShowPitch() { if (reportAvailable && historyAvailable) SelectTab(2); }
    public void ShowLuck() { if (reportAvailable && historyAvailable) SelectTab(3); }
    public void ShowRatings() { if (reportAvailable) SelectTab(4); }

    private void SelectTab(int tab)
    {
        bool players = tab == 1;
        ShowingPlayers = players;
        ShowingPitch = tab == 2;
        ShowingLuck = tab == 3;
        ShowingRatings = tab == 4;
        if (statsPage != null) statsPage.SetActive(tab == 0);
        if (playersPage != null) playersPage.SetActive(players);
        if (pitchPage != null) pitchPage.gameObject.SetActive(ShowingPitch);
        if (luckPage != null) luckPage.gameObject.SetActive(ShowingLuck);
        if (ratingsPage != null) ratingsPage.gameObject.SetActive(ShowingRatings);
        if (teamButtons?.Length > 0) teamButtons[0].transform.parent.gameObject.SetActive(reportAvailable && (tab == 1 || tab == 2));
        if (previousPage != null) previousPage.transform.parent.gameObject.SetActive(players);
        MarkSelected(statsTab, tab == 0);
        MarkSelected(playersTab, players);
        MarkSelected(pitchTab, tab == 2);
        MarkSelected(luckTab, tab == 3);
        MarkSelected(ratingsTab, tab == 4);
    }

    public void SelectMetric(PostMatchLeaderboard.Metric selected)
    {
        metric = selected; page = 0; RefreshPlayers();
    }

    public void SelectTeam(PostMatchLeaderboard.Team selected)
    {
        team = selected; page = 0; RefreshTeamColumns(); RefreshPlayers();
        if (pitchPage != null) pitchPage.Show(matchData, team);
    }

    private void SetTeamLabel(int index, string name, string fallback)
    {
        if (teamButtons == null || index >= teamButtons.Length || teamButtons[index] == null) return;
        var text = teamButtons[index].GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = string.IsNullOrWhiteSpace(name) ? fallback : name;
    }

    private void RefreshTeamColumns()
    {
        // Team selection belongs to the player/pitch views; Match Stats always compares both teams.
        if (homeStatsColumn != null) homeStatsColumn.alpha = 1f;
        if (awayStatsColumn != null) awayStatsColumn.alpha = 1f;
    }

    private void RefreshPlayers()
    {
        if (heading == null || rows == null || rows.Length == 0) return;
        var leaders = PostMatchLeaderboard.Build(matchData?.stats, metric, team);
        heading.text = PostMatchLeaderboard.Title(metric);
        if (metricColumnTitle != null) metricColumnTitle.text = PostMatchLeaderboard.ColumnTitle(metric);
        description.text = PostMatchLeaderboard.Description(metric);
        int pages = Mathf.Max(1, Mathf.CeilToInt(leaders.Count / (float)rows.Length));
        page = Mathf.Clamp(page, 0, pages - 1);
        for (int i = 0; i < rows.Length; i++)
        {
            var view = rows[i];
            int index = page * rows.Length + i;
            view.root.SetActive(index < leaders.Count);
            if (index >= leaders.Count) continue;
            var row = leaders[index];
            view.rank.text = (index + 1).ToString();
            view.player.text = row.JerseyNumber > 0 ? $"{row.JerseyNumber}  {row.PlayerName}" : row.PlayerName;
            view.team.text = row.TeamSide == "Home" ? matchData?.gameSettings?.homeTeamName ?? "Home"
                : row.TeamSide == "Away" ? matchData?.gameSettings?.awayTeamName ?? "Away" : "Unassigned";
            view.team.color = row.TeamSide == "Home" ? new Color(.12f, .39f, .55f)
                : row.TeamSide == "Away" ? new Color(.65f, .29f, .10f) : new Color(.12f, .26f, .33f);
            view.value.text = row.DisplayValue;
        }
        emptyMessage.gameObject.SetActive(leaders.Count == 0);
        emptyMessage.text = "No recorded performances for this selection.";
        pageLabel.text = leaders.Count == 0 ? "0 players" : $"{leaders.Count} players  •  Page {page + 1}/{pages}";
        previousPage.interactable = page > 0;
        nextPage.interactable = page < pages - 1;
        for (int i = 0; i < (metricButtons?.Length ?? 0); i++) MarkSelected(metricButtons[i], metricKinds != null && i < metricKinds.Length && metricKinds[i] == metric);
        for (int i = 0; i < (teamButtons?.Length ?? 0); i++) MarkSelected(teamButtons[i], i == (int)team);
    }

    private static void MarkSelected(UnityEngine.UI.Button button, bool selected)
    {
        if (button?.targetGraphic == null) return;
        button.targetGraphic.color = selected ? new Color(.19f, .39f, .48f, 1f) : new Color(.10f, .15f, .22f, 1f);
    }
}
