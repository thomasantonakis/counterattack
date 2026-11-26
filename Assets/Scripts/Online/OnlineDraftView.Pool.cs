using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CounterAttack.Online
{
    public sealed partial class OnlineDraftView
    {
        public GameObject poolTable;
        public ScrollRect poolScroll;
        public Button[] poolHeaders;
        public TMP_InputField[] poolFilters;
        public OnlineDraftClickTarget[] poolRows;

        private readonly string[] filters = new string[10];
        private int sortColumn = -1;
        private bool ascending;
        private int poolCount;
        private bool poolBound;

        private void BindPool()
        {
            if (poolBound || poolTable == null || watching) return;
            poolBound = true;
            for (int index = 0; index < filters.Length; index++)
            {
                int column = index;
                poolHeaders[column].onClick.AddListener(() =>
                {
                    if (!CanBrowsePool()) return;
                    ascending = sortColumn != column ? IsTextColumn(column) : !ascending;
                    sortColumn = column;
                    ResetPoolPage();
                    Render();
                });
                poolFilters[column].onValueChanged.AddListener(value =>
                {
                    if (!CanBrowsePool())
                    {
                        poolFilters[column].SetTextWithoutNotify(filters[column] ?? string.Empty);
                        return;
                    }
                    filters[column] = value;
                    ResetPoolPage();
                    Render();
                });
            }
        }

        private static bool IsPoolOpen(OnlineDraftState state) => state?.freeSelection == true
            && (state.phase == "pick" || state.repeatable && state.phase == "review" && !(state.homeReady && state.awayReady));

        private bool CanBrowsePool() => activeDrag == null && IsPoolOpen(rendered)
            && !watching && client?.Connected == true && !client.Pending && (rendered.repeatable || CanPick(rendered));

        private bool CanPick(OnlineDraftState state)
        {
            return !watching && client?.Connected == true && !client.Pending && state?.phase == "pick"
                && (state.repeatable || state.turn == client.Team) && OwnRoster(state).Where((_, i) =>
                    GoalkeeperSlot(i + 1) == (state.selection == "goalkeepers")).Any(card => card == null);
        }

        private OnlineDraftCard[] OwnRoster(OnlineDraftState state) => client.Team == OnlineTeam.Home ? state.home : state.away;

        private bool CanPlacePoolCard(OnlineDraftState state, OnlineTeam team, int slot, string id)
        {
            if (!CanPick(state) || !state.freeSelection || team != client.Team || !state.available.Contains(id)) return false;
            var roster = OwnRoster(state);
            return slot >= 1 && slot <= roster.Length && roster[slot - 1] == null
                && GoalkeeperSlot(slot) == (state.selection == "goalkeepers");
        }

        internal void SelectPoolCard(string id, bool quickPick)
        {
            var state = rendered;
            if (!CanBrowsePool() || !CanPick(state) || !state.available.Contains(id)) return;
            selected = id;
            selectedSlot = 0;
            if (quickPick)
            {
                for (int index = 1; index <= OwnRoster(state).Length; index++)
                {
                    if (!CanPlacePoolCard(state, client.Team, index, id)) continue;
                    selected = null;
                    if (client.Request(OnlineDraftIntent.Pick, id, index)) return;
                    break;
                }
            }
            Render();
        }

        private void ResetPoolPage()
        {
            page = 0;
            selected = null;
            if (poolScroll != null) poolScroll.verticalNormalizedPosition = 1;
        }

        private void ResetPool()
        {
            ResetPoolPage();
            sortColumn = -1;
            Array.Clear(filters, 0, filters.Length);
            if (poolFilters != null)
                foreach (var input in poolFilters) input.SetTextWithoutNotify(string.Empty);
        }

        private void RenderPool(OnlineDraftState state)
        {
            if (poolTable == null) return;
            BindPool();
            bool visible = IsPoolOpen(state) && !watching;
            poolTable.SetActive(visible);
            if (!visible) return;

            bool canBrowse = CanBrowsePool();
            bool keeper = state.selection == "goalkeepers";
            for (int i = 0; i < filters.Length; i++)
            {
                bool enabled = canBrowse;
                poolHeaders[i].interactable = enabled;
                var input = poolFilters[i];
                if (!enabled && input.isFocused) input.DeactivateInputField();
                input.interactable = enabled;
                string column = ColumnLabel(i, keeper);
                poolHeaders[i].GetComponentInChildren<TMP_Text>().text = i == sortColumn
                    ? column + (ascending ? " ^" : " v") : column;
            }
            var focus = EventSystem.current?.currentSelectedGameObject;
            if (!canBrowse && focus != null && (focus.transform.IsChildOf(poolTable.transform)
                 || focus == previousPageButton?.gameObject || focus == nextPageButton?.gameObject))
                EventSystem.current.SetSelectedGameObject(null);

            localCatalog ??= OnlineDraftCatalog.Load().ToDictionary(card => card.id);
            var candidates = state.available.Where(localCatalog.ContainsKey).Select(id => localCatalog[id]).Where(MatchesFilters);
            var matching = SortCandidates(candidates).ToArray();
            poolCount = matching.Length;
            page = Mathf.Clamp(page, 0, PoolPageCount - 1);
            for (int i = 0; i < poolRows.Length; i++)
            {
                int index = page * poolRows.Length + i;
                var candidate = index < poolCount ? matching[index] : null;
                var row = poolRows[i];
                row.BindPoolCard(candidate);
                row.gameObject.SetActive(candidate != null);
            }
        }

        private int PoolPageCount => Mathf.Max(1, (poolCount + poolRows.Length - 1) / poolRows.Length);

        private bool MatchesFilters(OnlineDraftCard card)
        {
            for (int i = 0; i < filters.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(filters[i])) continue;
                if (IsTextColumn(i))
                {
                    if (TextValue(card, i).IndexOf(filters[i].Trim(), StringComparison.OrdinalIgnoreCase) < 0) return false;
                }
                else if (!DraftManager.MatchesNumericFilter(StatValue(card, i), filters[i])) return false;
            }
            return true;
        }

        private IEnumerable<OnlineDraftCard> SortCandidates(IEnumerable<OnlineDraftCard> candidates)
        {
            if (sortColumn < 0) return candidates;
            if (IsTextColumn(sortColumn))
                return ascending ? candidates.OrderBy(c => TextValue(c, sortColumn), StringComparer.OrdinalIgnoreCase)
                    : candidates.OrderByDescending(c => TextValue(c, sortColumn), StringComparer.OrdinalIgnoreCase);
            return ascending ? candidates.OrderBy(c => StatValue(c, sortColumn)) : candidates.OrderByDescending(c => StatValue(c, sortColumn));
        }

        internal static string CellValue(OnlineDraftCard card, int column) => IsTextColumn(column) ? TextValue(card, column)
            : StatValue(card, column).ToString();

        private static bool IsTextColumn(int column) => column is 0 or 1 or 9;

        private static string TextValue(OnlineDraftCard card, int column) => (column switch
        {
            0 => card.name, 1 => DraftManager.AbbreviateNationality(card.country), _ => card.type
        }) ?? string.Empty;

        internal static int StatValue(OnlineDraftCard card, int column) => column switch
        {
            2 => card.pace, 3 => card.dribbling, 4 => card.goalkeeper ? card.aerial : card.heading,
            5 => card.highPass, 6 => card.resilience, 7 => card.goalkeeper ? card.saving : card.shooting,
            8 => card.goalkeeper ? card.handling : card.tackling, _ => 0
        };

        private static string ColumnLabel(int column, bool keeper) => column switch
        {
            0 => "Name", 1 => "Nat", 2 => "Pace", 3 => "Dr", 4 => keeper ? "Aer" : "Hd",
            5 => "HP", 6 => "Res", 7 => keeper ? "Sv" : "Sh", 8 => keeper ? "Han" : "Tac", _ => "Type"
        };
    }
}
