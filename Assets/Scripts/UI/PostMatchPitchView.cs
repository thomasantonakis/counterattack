using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public sealed class PostMatchPitchView : MonoBehaviour
{
    [SerializeField] private PostMatchPitchGraphic pitch;
    [SerializeField] private PostMatchPitchMarker[] markers;
    [SerializeField] private TMP_Text status;
    [SerializeField] private TMP_Text playerDetail;
    [SerializeField] private PostMatchDropdown periodDropdown;
    [SerializeField] private PostMatchDropdown possessionDropdown;
    [SerializeField] private PostMatchDropdown heatmapDropdown;
    private IReadOnlyList<PostMatchPitchData.Half> periodChoices;
    private MatchManager.GameData data;
    private PostMatchLeaderboard.Team team;
    private PostMatchPitchData.Half half;
    private PostMatchPitchData.Possession possession;
    private PostMatchPitchData.Player pinned;
    private PostMatchPitchData.Player hovered;
    private bool movement;

    public void Configure(PostMatchPitchGraphic graphic, PostMatchPitchMarker[] playerMarkers, TMP_Text help, TMP_Text detail,
        PostMatchDropdown periods, PostMatchDropdown possessions, PostMatchDropdown heatmaps)
    { pitch = graphic; markers = playerMarkers; status = help; playerDetail = detail; periodDropdown = periods; possessionDropdown = possessions; heatmapDropdown = heatmaps; }

    public void Show(MatchManager.GameData match, PostMatchLeaderboard.Team selectedTeam, bool reset = false)
    {
        data = match; team = selectedTeam;
        if (reset) { half = PostMatchPitchData.Half.All; possession = PostMatchPitchData.Possession.All; movement = false; }
        pinned = hovered = null;
        periodChoices = PostMatchPitchData.AvailablePeriods(data);
        if (!periodChoices.Contains(half)) half = PostMatchPitchData.Half.All;
        Options(periodDropdown, periodChoices.Select(PostMatchPitchData.PeriodLabel), periodChoices.ToList().IndexOf(half));
        Options(possessionDropdown, new[] { "All game", "Attacking", "Defending" }, (int)possession);
        Options(heatmapDropdown, new[] { "Position", "Movement" }, movement ? 1 : 0);
        Bind(); Refresh();
    }

    private static void Options(PostMatchDropdown dropdown, IEnumerable<string> choices, int value)
    {
        if (dropdown == null) return;
        dropdown.ClearOptions(); dropdown.AddOptions(choices.ToList()); dropdown.SetValueWithoutNotify(value); dropdown.FitOptions();
    }

    private void OnEnable() => Bind();
    private void OnDisable() => Unbind();
    private void Bind()
    {
        Unbind();
        if (periodDropdown != null) periodDropdown.onValueChanged.AddListener(PeriodChanged);
        if (possessionDropdown != null) possessionDropdown.onValueChanged.AddListener(PossessionChanged);
        if (heatmapDropdown != null) heatmapDropdown.onValueChanged.AddListener(HeatmapChanged);
    }
    private void Unbind()
    {
        if (periodDropdown != null) periodDropdown.onValueChanged.RemoveListener(PeriodChanged);
        if (possessionDropdown != null) possessionDropdown.onValueChanged.RemoveListener(PossessionChanged);
        if (heatmapDropdown != null) heatmapDropdown.onValueChanged.RemoveListener(HeatmapChanged);
    }
    private void PeriodChanged(int index)
    {
        if (periodChoices == null || index < 0 || index >= periodChoices.Count) return;
        half = periodChoices[index]; pinned = hovered = null; Refresh();
    }
    private void PossessionChanged(int index) { possession = (PostMatchPitchData.Possession)index; pinned = hovered = null; Refresh(); }
    private void HeatmapChanged(int index) { movement = index == 1; RefreshHeatmap(); }

    private void Refresh()
    {
        if (markers == null || pitch == null) return;
        var players = PostMatchPitchData.Build(data, team, half, possession);
        for (int i = 0; i < markers.Length; i++)
        {
            var marker = markers[i]; marker.gameObject.SetActive(i < players.Count);
            if (i >= players.Count) continue;
            var player = players[i]; marker.Bind(this, player); marker.number.text = player.Number.ToString();
            marker.disc.color = player.Side == "Home" ? new Color(.12f, .45f, .65f) : new Color(.78f, .36f, .12f);
            var rect = marker.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = player.Average; rect.anchoredPosition = Vector2.zero;
        }
        status.text = players.Count == 0 ? "No recorded action-boundary positions for this selection."
            : $"{players.Count} players · Directions normalized: Home → / Away ←";
        RefreshHeatmap();
    }

    public void Preview(PostMatchPitchData.Player player) { hovered = player; RefreshHeatmap(); }
    public void EndPreview() { hovered = null; RefreshHeatmap(); }
    public void Pin(PostMatchPitchData.Player player) { pinned = pinned?.Key == player?.Key ? null : player; hovered = null; RefreshHeatmap(); }
    private void RefreshHeatmap()
    {
        if (pitch == null || playerDetail == null) return;
        var player = hovered ?? pinned;
        pitch.ShowHeatmap(player == null ? null : movement ? player.Movement : player.Positions);
        playerDetail.text = player == null ? "Hover a number to see its heatmap. Click to pin; click again to release."
            : $"{player.Name} · #{player.Number} · {(movement ? player.Movement.Count + " recorded movement steps" : player.Positions.Count + " position samples")}{(pinned?.Key == player.Key ? " · Pinned" : "")}";
    }
}
