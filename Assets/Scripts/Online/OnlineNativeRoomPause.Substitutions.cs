using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace CounterAttack.Online
{
    internal sealed partial class OnlineNativeRoomPause
    {
        private sealed class ReviewRow
        {
            internal bool home;
            internal int index;
            internal SubstitutionDropdownRowView view;
            internal PlayerToken[] outgoing = Array.Empty<PlayerToken>();
            internal PlayerToken[] incoming = Array.Empty<PlayerToken>();
        }

        private readonly List<ReviewRow> reviewRows = new();

        private void BindReviewRows()
        {
            foreach (bool home in new[] { true, false })
            {
                var rows = substitutions.GetRows(home);
                for (int i = 0; i < rows.Length && i < OnlineSubstitutionReview.Capacity; i++)
                {
                    var row = new ReviewRow { home = home, index = i, view = rows[i] };
                    reviewRows.Add(row);
                    if (home != IsHome) continue;
                    row.view.outgoingDropdown.onValueChanged.AddListener(index => ChangeReviewOrder(row, index, true));
                    row.view.incomingDropdown.onValueChanged.AddListener(index => ChangeReviewOrder(row, index, false));
                }
            }
        }

        private void ChangeReviewOrder(ReviewRow row, int index, bool outgoing)
        {
            var review = state?.substitutionReview;
            if (review == null || !board.controls.CanEditSubstitutionReview) return;
            var current = review.Orders(row.home)[row.index];
            var options = outgoing ? row.outgoing : row.incoming;
            if (index < 0 || index >= options.Length) return;
            int jersey = options[index]?.jerseyNumber ?? 0;
            if (outgoing && current.outgoing != jersey) board.controls.SetSubstitutionOrder(row.index, jersey, 0);
            else if (!outgoing) board.controls.SetSubstitutionOrder(row.index, current.outgoing, jersey);
        }

        private void DisplaySubstitutionReview(OnlineSubstitutionReview review)
        {
            if (substitutions == null || terminalMessage != null) return;
            substitutions.titleText.text = state.connectionHeld ? "Substitutions · Reconnecting" : "Substitutions";
            var settings = board.match.gameData.gameSettings;
            substitutions.homeTeamTitleText.text = settings.homeTeamName;
            substitutions.awayTeamTitleText.text = settings.awayTeamName;
            foreach (bool home in new[] { true, false })
            {
                var roster = board.tokens.Where(token => token.isHomeTeam == home && !token.isSentOff)
                    .OrderBy(token => token.jerseyNumber).ToArray();
                var playing = roster.Where(token => token.isPlaying && token.currentHex != null).ToArray();
                var bench = roster.Where(token => !token.isPlaying && !token.wasSubbedOff).ToArray();
                DisplayRoster(substitutions.GetPlayingRows(home), playing);
                DisplayRoster(substitutions.GetBenchRows(home), bench);
                var orders = review.Orders(home);
                int remaining = board.match.GetSubstitutionsRemaining(home);
                var summary = home ? substitutions.homeSubsRemainingText : substitutions.awaySubsRemainingText;
                string status = review.Confirmed(home) ? " · Confirmed" : review.HasOrders(home) ? " · Awaiting confirmation" : "";
                summary.text = $"{remaining} substitutions left{status}";
                foreach (var row in reviewRows.Where(item => item.home == home))
                {
                    row.view.gameObject.SetActive(row.index < remaining);
                    if (row.index >= remaining) continue;
                    var order = orders[row.index];
                    var selected = playing.FirstOrDefault(token => token.jerseyNumber == order.outgoing);
                    row.outgoing = new PlayerToken[] { null }.Concat(playing.Where(token => token == selected
                        || !orders.Any(other => other.outgoing == token.jerseyNumber))).ToArray();
                    row.incoming = new PlayerToken[] { null }.Concat(bench.Where(token => selected != null
                        && token.IsGoalKeeper == selected.IsGoalKeeper
                        && (order.incoming == token.jerseyNumber || !orders.Any(other => other.incoming == token.jerseyNumber)))).ToArray();
                    SetReviewOptions(row.view.outgoingDropdown, row.outgoing, order.outgoing, "Select outgoing player");
                    SetReviewOptions(row.view.incomingDropdown, row.incoming, order.incoming, "Select incoming player");
                    bool editable = home == IsHome && board.controls.CanEditSubstitutionReview;
                    row.view.outgoingDropdown.interactable = editable;
                    row.view.incomingDropdown.interactable = editable && selected != null;
                }
            }
            bool localHasOrders = review.HasOrders(IsHome);
            substitutions.confirmButton.interactable = board.controls.CanEditSubstitutionReview && localHasOrders
                && review.Orders(IsHome).All(order => !order.HasSelection || order.IsComplete);
            substitutions.backButton.interactable = board.controls.CanEditSubstitutionReview
                && (localHasOrders || !review.HasOrders(!IsHome) || review.Confirmed(!IsHome));
        }

        private static void SetReviewOptions(TMP_Dropdown dropdown, PlayerToken[] options, int jersey, string placeholder)
        {
            var labels = options.Select(token => token == null ? placeholder : $"{token.jerseyNumber}. {token.playerName}").ToList();
            if (!dropdown.options.Select(option => option.text).SequenceEqual(labels))
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(labels);
            }
            dropdown.SetValueWithoutNotify(Math.Max(0, Array.FindIndex(options, token => (token?.jerseyNumber ?? 0) == jersey)));
            dropdown.RefreshShownValue();
        }
    }
}
