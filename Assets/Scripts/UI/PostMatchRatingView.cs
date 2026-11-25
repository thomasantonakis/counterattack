using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class PostMatchRatingView : MonoBehaviour
{
    [Serializable] public sealed class PlayerRow { public Button button; public TMP_Text rank, player, team, rating; }
    [SerializeField] private PlayerRow[] rows;
    [SerializeField] private TMP_Text pageLabel, detail;
    [SerializeField] private Button previous, next;
    private readonly List<(Button button, UnityAction callback)> listeners = new();
    private IReadOnlyList<PostMatchRatingData.Row> players = Array.Empty<PostMatchRatingData.Row>();
    private MatchManager.GameData data;
    private int page, selected;
    public void Configure(PlayerRow[] authoredRows, TMP_Text paging, TMP_Text breakdown, Button before, Button after)
    { rows = authoredRows; pageLabel = paging; detail = breakdown; previous = before; next = after; }
    public void Show(MatchManager.GameData match)
    {
        data = match; players = PostMatchRatingData.Build(match?.stats); page = selected = 0;
        Bind(); Render();
    }
    private void OnEnable() { Bind(); Render(); }
    private void OnDisable() => Unbind();
    private void Bind()
    {
        Unbind();
        if (rows == null) return;
        for (int i = 0; i < rows.Length; i++)
        {
            int index = i;
            Listen(rows[i].button, () => { selected = page * rows.Length + index; Render(); });
        }
        Listen(previous, () => { page = Math.Max(0, page - 1); selected = page * rows.Length; Render(); });
        Listen(next, () => { page++; selected = page * rows.Length; Render(); });
    }
    private void Listen(Button button, UnityAction callback)
    { if (button == null) return; button.onClick.AddListener(callback); listeners.Add((button, callback)); }
    private void Unbind()
    { foreach (var listener in listeners) if (listener.button != null) listener.button.onClick.RemoveListener(listener.callback); listeners.Clear(); }
    private void Render()
    {
        if (rows == null || rows.Length == 0 || pageLabel == null) return;
        int pages = Math.Max(1, (int)Math.Ceiling(players.Count / (double)rows.Length)); page = Math.Clamp(page, 0, pages - 1);
        selected = Math.Clamp(selected, 0, Math.Max(0, players.Count - 1));
        for (int i = 0; i < rows.Length; i++)
        {
            int index = page * rows.Length + i; var row = rows[i]; row.button.gameObject.SetActive(index < players.Count);
            if (index >= players.Count) continue;
            var player = players[index]; row.rank.text = (index + 1).ToString();
            row.player.text = player.Jersey > 0 ? player.Jersey + "  " + player.Name : player.Name;
            row.team.text = player.Side == "Home" ? data?.gameSettings?.homeTeamName ?? "Home"
                : player.Side == "Away" ? data?.gameSettings?.awayTeamName ?? "Away" : "Unassigned";
            row.rating.text = player.DisplayRating;
            row.button.targetGraphic.color = index == selected ? new Color(.83f,.91f,.95f) : i % 2 == 0 ? new Color(.92f,.95f,.97f) : new Color(.97f,.98f,.99f);
        }
        pageLabel.text = players.Count == 0 ? "No recorded players" : $"{players.Count} players · Page {page + 1}/{pages}";
        previous.interactable = page > 0; next.interactable = page + 1 < pages;
        detail.text = players.Count == 0 ? "No player statistics recorded." : players[selected].Name + " · " + players[selected].DisplayRating + "\n" + players[selected].Breakdown;
    }
}
