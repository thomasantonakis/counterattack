using System.Linq;
using TMPro;
using UnityEngine;

namespace CounterAttack.Online
{
    public sealed partial class OnlineRoomMenu
    {
        [SerializeField] private SubstitutionMenuView roomSubstitutions;
        private SubstitutionDropdownRowView localSubstitutionRow;
        private long roomSubstitutionPublication = -1;

        private void WireAuthoredSubstitutions()
        {
            if (roomSubstitutions == null) return;
            roomSubstitutions.backButton.onClick.AddListener(() => substitutionPanel.SetActive(false));
            roomSubstitutions.confirmButton.onClick.AddListener(CommitSubstitution);
        }

        private void RefreshAuthoredSubstitutions()
        {
            if (roomSubstitutions == null || rosterState == null) return;
            bool open = substitutionPanel.activeSelf;
            roomSubstitutions.gameObject.SetActive(open);
            if (!open)
            {
                roomSubstitutionPublication = -1;
                return;
            }

            bool localHome = board.localTeam == OnlineTeam.Home;
            if (localSubstitutionRow == null)
            {
                foreach (bool home in new[] { true, false })
                {
                    var rows = roomSubstitutions.GetRows(home);
                    for (int i = 0; i < rows.Length; i++)
                        rows[i].gameObject.SetActive(home == localHome && i == 0);
                }
                localSubstitutionRow = roomSubstitutions.GetRows(localHome)[0];
                localSubstitutionRow.outgoingDropdown.onValueChanged.AddListener(value => outgoingPlayer.value = value);
                localSubstitutionRow.incomingDropdown.onValueChanged.AddListener(value => incomingPlayer.value = value);
            }

            if (roomSubstitutionPublication != rosterState.publicationSequence)
            {
                roomSubstitutionPublication = rosterState.publicationSequence;
                var settings = board.match.gameData.gameSettings;
                roomSubstitutions.homeTeamTitleText.text = settings.homeTeamName;
                roomSubstitutions.awayTeamTitleText.text = settings.awayTeamName;
                roomSubstitutions.homeSubsRemainingText.text = $"{board.match.GetSubstitutionsRemaining(true)} substitutions left";
                roomSubstitutions.awaySubsRemainingText.text = $"{board.match.GetSubstitutionsRemaining(false)} substitutions left";
                foreach (bool home in new[] { true, false })
                {
                    var tokens = board.tokens.Where(token => token.isHomeTeam == home).OrderBy(token => token.jerseyNumber);
                    PopulateRoomRoster(roomSubstitutions.GetPlayingRows(home), tokens.Where(token => token.isPlaying).ToArray());
                    PopulateRoomRoster(roomSubstitutions.GetBenchRows(home), tokens.Where(token => !token.isPlaying).ToArray());
                }
            }

            MirrorOptions(localSubstitutionRow.outgoingDropdown, outgoingPlayer);
            MirrorOptions(localSubstitutionRow.incomingDropdown, incomingPlayer);
            roomSubstitutions.confirmButton.interactable = substitutionCommitButton.interactable;
        }

        private void PopulateRoomRoster(TextMeshProUGUI[] rows, PlayerToken[] tokens)
        {
            for (int index = 0; index < rows.Length; index++)
            {
                var row = rows[index];
                if (row == null) continue;
                var token = index < tokens.Length ? tokens[index] : null;
                row.gameObject.SetActive(token != null);
                row.text = token == null ? string.Empty : $"{token.jerseyNumber}. {token.playerName}" + (token.IsGoalKeeper ? " (GK)" : string.Empty);
                roomSubstitutions.SetRosterStatus(row, SubstitutionMenuManager.RosterStatusColor(token));
            }
        }

        private static void MirrorOptions(TMP_Dropdown authored, UnityEngine.UI.Dropdown source)
        {
            var labels = source.options.Select(option => option.text);
            if (!authored.options.Select(option => option.text).SequenceEqual(labels))
            {
                authored.ClearOptions();
                authored.AddOptions(labels.ToList());
            }
            authored.SetValueWithoutNotify(source.value);
            authored.RefreshShownValue();
            authored.interactable = source.interactable;
        }
    }
}
