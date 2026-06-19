using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SubstitutionMenuManager : MonoBehaviour
{
    private static readonly Color ButtonColor = new(0.88f, 0.88f, 0.88f, 1f);
    private static readonly Color DisabledButtonColor = new(0.42f, 0.42f, 0.42f, 1f);
    private static readonly Color TextColor = new(0.94f, 0.94f, 0.94f, 1f);
    private static readonly Color MutedTextColor = new(0.66f, 0.68f, 0.7f, 1f);
    private static readonly Color InjuredColor = new(1f, 0.55f, 0.12f, 1f);
    private static readonly Color CautionedColor = new(1f, 0.86f, 0.18f, 1f);
    private static readonly Color SentOffColor = new(1f, 0.2f, 0.18f, 1f);
    private readonly List<SelectionRow> selectionRows = new();
    private PauseMenuManager pauseMenuManager;
    private GameObject pausePanel;
    private GameObject substitutionPanel;
    private SubstitutionMenuView substitutionView;
    private Button openButton;
    private Button backButton;
    private Button confirmButton;
    private MatchManager subscribedMatchManager;
    private bool suppressDropdownEvents;

    private sealed class SelectionRow
    {
        public bool isHomeTeam;
        public TMP_Dropdown outgoingDropdown;
        public TMP_Dropdown incomingDropdown;
        public readonly List<PlayerToken> outgoingOptions = new();
        public readonly List<PlayerToken> incomingOptions = new();
        public PlayerToken selectedOutgoing;
        public PlayerToken selectedIncoming;
    }

    private sealed class RosterEntry
    {
        public int jersey;
        public string playerName;
        public PlayerToken token;
    }

    public void Configure(PauseMenuManager owner, GameObject configuredPausePanel, Button configuredOpenButton, Button fallbackAnchorButton)
    {
        pauseMenuManager = owner;
        pausePanel = configuredPausePanel;
        EnsureOpenButton(configuredOpenButton, fallbackAnchorButton);
        EnsurePanel();
        SubscribeToMatchManager();
        RefreshOpenButtonState();
    }

    private void OnDestroy()
    {
        if (subscribedMatchManager != null)
        {
            subscribedMatchManager.OnSubstitutionStateChanged -= RefreshOpenButtonState;
        }
    }

    private void SubscribeToMatchManager()
    {
        if (subscribedMatchManager == MatchManager.Instance)
        {
            return;
        }

        if (subscribedMatchManager != null)
        {
            subscribedMatchManager.OnSubstitutionStateChanged -= RefreshOpenButtonState;
        }

        subscribedMatchManager = MatchManager.Instance;
        if (subscribedMatchManager != null)
        {
            subscribedMatchManager.OnSubstitutionStateChanged += RefreshOpenButtonState;
        }
    }

    private void EnsureOpenButton(Button configuredOpenButton, Button fallbackAnchorButton)
    {
        if (openButton != null || pausePanel == null)
        {
            return;
        }

        if (configuredOpenButton != null)
        {
            openButton = configuredOpenButton;
            openButton.onClick.RemoveAllListeners();
        }
        else if (fallbackAnchorButton != null)
        {
            openButton = Instantiate(fallbackAnchorButton.gameObject, fallbackAnchorButton.transform.parent).GetComponent<Button>();
            openButton.name = "SubstitutionsButton";
            openButton.transform.SetSiblingIndex(fallbackAnchorButton.transform.GetSiblingIndex() + 1);
            openButton.onClick.RemoveAllListeners();
            SetButtonText(openButton, "Substitutions");
        }
        else
        {
            openButton = CreateButton(pausePanel.transform, "SubstitutionsButton", "Substitutions");
        }

        openButton.onClick.AddListener(OnOpenButtonClicked);
    }

    public void RefreshOpenButtonState()
    {
        SubscribeToMatchManager();
        if (openButton == null)
        {
            return;
        }

        MatchManager matchManager = MatchManager.Instance;
        if (IsOpen && matchManager != null && !matchManager.AreSubstitutionsAvailable)
        {
            CloseToPauseMenu();
            return;
        }

        openButton.interactable = CanOpenSubstitutionMenu();
        ColorBlock colors = openButton.colors;
        colors.normalColor = openButton.interactable ? ButtonColor : DisabledButtonColor;
        openButton.colors = colors;
    }

    private bool CanOpenSubstitutionMenu()
    {
        MatchManager matchManager = MatchManager.Instance;
        return matchManager != null
            && matchManager.AreSubstitutionsAvailable
            && (matchManager.GetSubstitutionsRemaining(true) > 0 || matchManager.GetSubstitutionsRemaining(false) > 0);
    }

    public bool IsOpen => substitutionPanel != null && substitutionPanel.activeSelf;
    public bool IsGoalkeeperExitBlocked => IsGoalkeeperSubstitutionBlockingExit();

    private void OnOpenButtonClicked()
    {
        MatchManager matchManager = MatchManager.Instance;
        bool canOpen = CanOpenSubstitutionMenu();
        LogSubstitutionUiClick(
            "open_button",
            "open_substitutions",
            ("available", canOpen),
            ("homeRemaining", matchManager != null ? matchManager.GetSubstitutionsRemaining(true) : 0),
            ("awayRemaining", matchManager != null ? matchManager.GetSubstitutionsRemaining(false) : 0),
            ("goalkeeperReplacementRequired", matchManager != null && matchManager.IsAnyGoalkeeperReplacementRequired));
        OpenSubstitutionMenu();
    }

    private void OnBackButtonClicked()
    {
        if (IsGoalkeeperSubstitutionBlockingExit())
        {
            string reason = MatchManager.Instance?.GetGoalkeeperActionBlockReason() ?? "Confirm the goalkeeper substitution before leaving this panel.";
            LogSubstitutionUiClick(
                "back_button",
                "blocked_goalkeeper_substitution_required",
                ("reason", reason),
                ("selectedRowCount", CountSelectedRows()),
                ("selectedSubstitutions", BuildSelectedSubstitutionsSummary()));
            Debug.LogWarning(reason);
            return;
        }

        LogSubstitutionUiClick(
            "back_button",
            "back_to_pause_menu",
            ("selectedRowCount", CountSelectedRows()),
            ("selectedSubstitutions", BuildSelectedSubstitutionsSummary()));
        CloseToPauseMenu();
    }

    private void OnConfirmButtonClicked()
    {
        List<SelectionRow> validRows = GetValidRows();
        List<SelectionRow> proposedRows = GetProposedRows();
        LogSubstitutionUiClick(
            "confirm_button",
            "confirm_substitutions",
            ("validSelectionCount", validRows.Count),
            ("proposedSelectionCount", proposedRows.Count),
            ("allProposedSubstitutionsValid", AreAllProposedSubstitutionsValid()),
            ("requiredSubstitutionsSelected", AreRequiredSubstitutionsSelected()),
            ("selectedSubstitutions", BuildSelectedSubstitutionsSummary()));
        ConfirmSubstitutions();
    }

    public void OpenSubstitutionMenu()
    {
        if (!CanOpenSubstitutionMenu())
        {
            Debug.LogWarning("Substitutions are not available right now.");
            RefreshOpenButtonState();
            return;
        }

        EnsurePanel();
        pausePanel.SetActive(false);
        substitutionPanel.SetActive(true);
        RebuildMenu();
    }

    public void CloseToPauseMenu()
    {
        if (substitutionPanel != null)
        {
            substitutionPanel.SetActive(false);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        RefreshOpenButtonState();
    }

    private void EnsurePanel()
    {
        if (substitutionPanel != null || pausePanel == null)
        {
            return;
        }

        Transform parent = pausePanel.transform.parent != null
            ? pausePanel.transform.parent
            : pausePanel.transform;

        substitutionView = parent.GetComponentsInChildren<SubstitutionMenuView>(true)
            .FirstOrDefault(view => view != null && view.name == "SubstitutionPanel");
        if (substitutionView == null)
        {
            substitutionView = FindObjectsByType<SubstitutionMenuView>(FindObjectsInactive.Include)
                .FirstOrDefault(view => view != null && view.name == "SubstitutionPanel");
        }

        if (substitutionView == null)
        {
            Debug.LogError("[SubstitutionPanel] Missing designer-owned SubstitutionPanel under the scene Canvas.");
            return;
        }

        substitutionPanel = substitutionView.gameObject;
        if (!substitutionView.HasRequiredReferences())
        {
            Debug.LogError("[SubstitutionPanel] Scene panel references are incomplete. Use Tools/Counter Attack/Ensure Scene-Owned Match Panels In Room.");
            substitutionPanel = null;
            substitutionView = null;
            return;
        }

        if (substitutionPanel.transform.parent != parent)
        {
            substitutionPanel.transform.SetParent(parent, false);
        }

        GameplayInputConsumer.Ensure(substitutionPanel, blocksGameplayWhileActive: true);
        WirePanelButtons();
        substitutionPanel.SetActive(false);
    }

    private void WirePanelButtons()
    {
        backButton = substitutionView.backButton;
        confirmButton = substitutionView.confirmButton;
        backButton.onClick.RemoveAllListeners();
        confirmButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(OnBackButtonClicked);
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
    }

    private void RebuildMenu()
    {
        selectionRows.Clear();
        if (substitutionView == null || !substitutionView.HasRequiredReferences())
        {
            return;
        }

        RebuildTeamColumn(true);
        RebuildTeamColumn(false);
        PrefillRequiredSubstitutions();
        RebuildDropdownOptions();
    }

    private void RebuildTeamColumn(bool isHomeTeam)
    {
        MatchManager matchManager = MatchManager.Instance;
        if (matchManager == null)
        {
            return;
        }

        string teamName = isHomeTeam
            ? matchManager.gameData.gameSettings.homeTeamName
            : matchManager.gameData.gameSettings.awayTeamName;
        TextMeshProUGUI teamTitle = isHomeTeam ? substitutionView.homeTeamTitleText : substitutionView.awayTeamTitleText;
        TextMeshProUGUI remainingText = isHomeTeam ? substitutionView.homeSubsRemainingText : substitutionView.awaySubsRemainingText;
        TextMeshProUGUI[] playingRows = substitutionView.GetPlayingRows(isHomeTeam);
        TextMeshProUGUI[] benchRows = substitutionView.GetBenchRows(isHomeTeam);

        teamTitle.text = teamName;
        BuildLineupRows(playingRows, benchRows, isHomeTeam);

        int remaining = matchManager.GetSubstitutionsRemaining(isHomeTeam);
        remainingText.text = $"{remaining} subs remaining";

        SubstitutionDropdownRowView[] rowViews = substitutionView.GetRows(isHomeTeam);
        for (int index = 0; index < rowViews.Length; index++)
        {
            bool shouldShow = index < remaining;
            rowViews[index].gameObject.SetActive(shouldShow);
            if (shouldShow)
            {
                SelectionRow row = BindSelectionRow(rowViews[index], isHomeTeam);
                selectionRows.Add(row);
            }
        }
    }

    private void BuildLineupRows(TextMeshProUGUI[] playingRows, TextMeshProUGUI[] benchRows, bool isHomeTeam)
    {
        Dictionary<string, MatchManager.RosterPlayer> roster = isHomeTeam
            ? MatchManager.Instance.gameData.rosters.home
            : MatchManager.Instance.gameData.rosters.away;
        if (roster == null)
        {
            FillRosterList(playingRows, Enumerable.Empty<RosterEntry>());
            FillRosterList(benchRows, Enumerable.Empty<RosterEntry>());
            return;
        }

        int squadSize = GetConfiguredSquadSize(roster);
        Dictionary<int, PlayerToken> tokensByJersey = FindObjectsByType<PlayerToken>(FindObjectsInactive.Include)
            .Where(token => token != null && token.isHomeTeam == isHomeTeam)
            .GroupBy(token => token.jerseyNumber)
            .ToDictionary(group => group.Key, group => group.First());

        List<RosterEntry> entries = roster
            .Select(pair => new
            {
                Player = pair.Value,
                Jersey = int.TryParse(pair.Key, out int parsedJersey) ? parsedJersey : int.MaxValue
            })
            .Where(entry => entry.Jersey < int.MaxValue)
            .Where(entry => entry.Jersey <= squadSize)
            .OrderBy(entry => entry.Jersey)
            .Select(entry =>
            {
                tokensByJersey.TryGetValue(entry.Jersey, out PlayerToken token);
                return new RosterEntry
                {
                    jersey = entry.Jersey,
                    playerName = entry.Player.name,
                    token = token
                };
            })
            .ToList();

        FillRosterList(playingRows, entries.Where(IsPlayingRosterEntry).OrderBy(entry => entry.jersey));
        FillRosterList(benchRows, entries.Where(entry => !IsPlayingRosterEntry(entry)).OrderBy(entry => entry.jersey));
    }

    private int GetConfiguredSquadSize(Dictionary<string, MatchManager.RosterPlayer> roster)
    {
        int configuredSquadSize = MatchManager.Instance?.gameData?.gameSettings?.squadSize ?? 0;
        if (configuredSquadSize > 0)
        {
            return configuredSquadSize;
        }

        return roster
            .Select(pair => int.TryParse(pair.Key, out int parsedJersey) ? parsedJersey : 0)
            .DefaultIfEmpty(11)
            .Max();
    }

    private bool IsPlayingRosterEntry(RosterEntry entry)
    {
        if (entry.token != null)
        {
            return entry.token.isPlaying;
        }

        return entry.jersey <= 11;
    }

    private void FillRosterList(TextMeshProUGUI[] rows, IEnumerable<RosterEntry> entries)
    {
        if (rows == null)
        {
            return;
        }

        List<RosterEntry> entryList = entries?.ToList() ?? new List<RosterEntry>();
        for (int i = 0; i < rows.Length; i++)
        {
            TextMeshProUGUI rowText = rows[i];
            if (rowText == null)
            {
                continue;
            }

            bool hasEntry = i < entryList.Count;
            rowText.gameObject.SetActive(hasEntry);
            if (!hasEntry)
            {
                rowText.text = string.Empty;
                continue;
            }

            RosterEntry entry = entryList[i];
            rowText.text = BuildPlayerDisplay(entry.jersey, entry.playerName, entry.token);
            rowText.richText = true;
        }
    }

    private string BuildPlayerDisplay(int jersey, string playerName, PlayerToken token)
    {
        string color = ColorUtility.ToHtmlStringRGB(ResolvePlayerTextColor(token));
        string goalkeeperSuffix = IsGoalkeeperRosterEntry(jersey, token) ? " (GK)" : string.Empty;
        string status = BuildStatusTags(token);
        return $"<color=#{color}>{jersey}. {playerName}{goalkeeperSuffix}</color>{status}";
    }

    private static bool IsGoalkeeperRosterEntry(int jersey, PlayerToken token)
    {
        if (token != null)
        {
            return token.IsGoalKeeper;
        }

        return jersey == 1 || jersey == 12;
    }

    private Color ResolvePlayerTextColor(PlayerToken token)
    {
        if (token == null)
        {
            return MutedTextColor;
        }

        if (token.isSentOff)
        {
            return SentOffColor;
        }

        if (token.isBooked)
        {
            return CautionedColor;
        }

        if (token.isInjured || token.requiresSubstitution)
        {
            return InjuredColor;
        }

        return token.isPlaying ? TextColor : MutedTextColor;
    }

    private string BuildStatusTags(PlayerToken token)
    {
        if (token == null)
        {
            return string.Empty;
        }

        List<string> tags = new();
        if (token.isInjured || token.requiresSubstitution)
        {
            tags.Add("<color=#FF8C1A> INJ</color>");
        }
        if (token.isBooked)
        {
            tags.Add("<color=#FFD92E> YC</color>");
        }
        if (token.isSentOff)
        {
            tags.Add("<color=#FF332D> RC</color>");
        }
        if (token.wasSubbedOn)
        {
            tags.Add("<color=#48C774> IN</color>");
        }
        if (token.wasSubbedOff)
        {
            tags.Add("<color=#FF5A5F> OUT</color>");
        }

        return tags.Count == 0 ? string.Empty : string.Join("", tags);
    }

    private SelectionRow BindSelectionRow(SubstitutionDropdownRowView rowView, bool isHomeTeam)
    {
        TMP_Dropdown outgoing = rowView.outgoingDropdown;
        TMP_Dropdown incoming = rowView.incomingDropdown;
        SelectionRow row = new()
        {
            isHomeTeam = isHomeTeam,
            outgoingDropdown = outgoing,
            incomingDropdown = incoming,
        };

        outgoing.onValueChanged.RemoveAllListeners();
        incoming.onValueChanged.RemoveAllListeners();
        outgoing.onValueChanged.AddListener(value => OnOutgoingChanged(row, value));
        incoming.onValueChanged.AddListener(value => OnIncomingChanged(row, value));
        return row;
    }

    private void PrefillRequiredSubstitutions()
    {
        MatchManager matchManager = MatchManager.Instance;
        foreach (bool isHomeTeam in new[] { true, false })
        {
            List<SelectionRow> teamRows = selectionRows.Where(row => row.isHomeTeam == isHomeTeam).ToList();
            if (teamRows.Count == 0)
            {
                continue;
            }

            if (matchManager != null && matchManager.IsGoalkeeperReplacementRequired(isHomeTeam))
            {
                teamRows[0].selectedOutgoing = null;
                teamRows[0].selectedIncoming = matchManager.GetAvailableBenchGoalkeeper(isHomeTeam);
                continue;
            }

            PlayerToken forcedGoalkeeperOut = matchManager != null
                ? matchManager.GetForcedGoalkeeperSubstitutionOutgoing(isHomeTeam)
                : null;
            if (forcedGoalkeeperOut != null)
            {
                teamRows[0].selectedOutgoing = forcedGoalkeeperOut;
                teamRows[0].selectedIncoming = matchManager.GetAvailableBenchGoalkeeper(isHomeTeam);
            }

            List<PlayerToken> requiredTokens = GetOutgoingCandidates(isHomeTeam)
                .Where(token => token != null && token.requiresSubstitution && token != forcedGoalkeeperOut)
                .ToList();
            for (int index = 0; index < requiredTokens.Count && index < teamRows.Count; index++)
            {
                int rowIndex = forcedGoalkeeperOut != null ? index + 1 : index;
                if (rowIndex >= teamRows.Count)
                {
                    break;
                }

                teamRows[rowIndex].selectedOutgoing = requiredTokens[index];
            }
        }
    }

    private void OnOutgoingChanged(SelectionRow row, int value)
    {
        if (suppressDropdownEvents)
        {
            return;
        }

        row.selectedOutgoing = value >= 0 && value < row.outgoingOptions.Count
            ? row.outgoingOptions[value]
            : null;
        row.selectedIncoming = null;
        LogSubstitutionUiClick(
            "outgoing_dropdown",
            "select_outgoing_player",
            ("team", FormatTeamSide(row.isHomeTeam)),
            ("row", GetTeamRowNumber(row)),
            ("selectedToken", FormatTokenForLog(row.selectedOutgoing)));
        RebuildDropdownOptions();
        if (row.selectedOutgoing != null && !ShouldKeepIncomingDropdownClosed(row))
        {
            row.incomingDropdown.Show();
        }
    }

    private void OnIncomingChanged(SelectionRow row, int value)
    {
        if (suppressDropdownEvents)
        {
            return;
        }

        row.selectedIncoming = value >= 0 && value < row.incomingOptions.Count
            ? row.incomingOptions[value]
            : null;
        LogSubstitutionUiClick(
            "incoming_dropdown",
            "select_incoming_player",
            ("team", FormatTeamSide(row.isHomeTeam)),
            ("row", GetTeamRowNumber(row)),
            ("selectedToken", FormatTokenForLog(row.selectedIncoming)),
            ("outgoingToken", FormatTokenForLog(row.selectedOutgoing)));
        RebuildDropdownOptions();
    }

    private void RebuildDropdownOptions()
    {
        suppressDropdownEvents = true;
        foreach (SelectionRow row in selectionRows)
        {
            row.outgoingOptions.Clear();
            row.outgoingOptions.Add(null);
            row.outgoingOptions.AddRange(GetOutgoingCandidates(row.isHomeTeam)
                .Where(token => token == row.selectedOutgoing || !IsSelectedAsOutgoingElsewhere(row, token)));
            if (!row.outgoingOptions.Contains(row.selectedOutgoing))
            {
                row.selectedOutgoing = null;
            }
            ApplyOptions(row.outgoingDropdown, row.outgoingOptions, row.selectedOutgoing);
        }

        foreach (SelectionRow row in selectionRows)
        {
            row.incomingOptions.Clear();
            row.incomingOptions.Add(null);
            row.incomingOptions.AddRange(GetIncomingCandidates(row)
                .Where(token => token == row.selectedIncoming || !IsSelectedAsIncomingElsewhere(row, token)));
            if (!row.incomingOptions.Contains(row.selectedIncoming))
            {
                row.selectedIncoming = null;
            }
            if (row.selectedIncoming == null && ShouldAutoSelectGoalkeeperIncoming(row))
            {
                row.selectedIncoming = row.incomingOptions.FirstOrDefault(token => token != null);
            }
            ApplyOptions(row.incomingDropdown, row.incomingOptions, row.selectedIncoming);
            row.incomingDropdown.interactable = row.selectedOutgoing != null;
        }

        suppressDropdownEvents = false;
        RefreshConfirmButton();
    }

    private List<PlayerToken> GetOutgoingCandidates(bool isHomeTeam)
    {
        PlayerTokenManager tokenManager = MatchManager.Instance?.playerTokenManager;
        if (tokenManager == null)
        {
            return new List<PlayerToken>();
        }

        MatchManager matchManager = MatchManager.Instance;
        List<PlayerToken> candidates = new();
        candidates.AddRange(tokenManager.GetPlayingTokens(isHomeTeam)
            .Where(token => token != null && !token.isSentOff && token.GetCurrentHex() != null)
            .Where(token => matchManager == null
                || !matchManager.IsGoalkeeperReplacementRequired(isHomeTeam)
                || !token.IsGoalKeeper)
            .OrderBy(token => token.jerseyNumber));

        return candidates
            .Where(token => token != null)
            .Distinct()
            .ToList();
    }

    private List<PlayerToken> GetIncomingCandidates(SelectionRow row)
    {
        PlayerTokenManager tokenManager = MatchManager.Instance?.playerTokenManager;
        if (tokenManager == null)
        {
            return new List<PlayerToken>();
        }

        bool isGoalkeeperReplacement = MatchManager.Instance != null
            && MatchManager.Instance.IsGoalkeeperReplacementRequired(row.isHomeTeam);
        if (isGoalkeeperReplacement)
        {
            return tokenManager.GetAvailableBenchTokens(row.isHomeTeam)
                .Where(token => token != null && !token.wasSubbedOff && !token.isSentOff)
                .Where(token => token.IsGoalKeeper)
                .OrderBy(token => token.jerseyNumber)
                .ToList();
        }

        if (row.selectedOutgoing == null)
        {
            return new List<PlayerToken>();
        }

        bool needsGoalkeeper = row.selectedOutgoing.IsGoalKeeper;
        
        if (needsGoalkeeper)
        {
            return tokenManager.GetAvailableBenchTokens(row.isHomeTeam)
                .Where(token => token != null && !token.wasSubbedOff && !token.isSentOff)
                .Where(token => token.IsGoalKeeper)
                .OrderBy(token => token.jerseyNumber)
                .ToList();
        }
        else
        {
            return tokenManager.GetAvailableBenchTokens(row.isHomeTeam)
                .Where(token => token != null && !token.wasSubbedOff && !token.isSentOff)
                .Where(token => !token.IsGoalKeeper)
                .OrderBy(token => token.jerseyNumber)
                .ToList();
        }
    }

    private bool IsSelectedAsOutgoingElsewhere(SelectionRow currentRow, PlayerToken token)
    {
        return token != null && selectionRows.Any(row => row != currentRow && row.selectedOutgoing == token);
    }

    private bool IsSelectedAsIncomingElsewhere(SelectionRow currentRow, PlayerToken token)
    {
        return token != null && selectionRows.Any(row => row != currentRow && row.selectedIncoming == token);
    }

    private bool ShouldAutoSelectGoalkeeperIncoming(SelectionRow row)
    {
        MatchManager matchManager = MatchManager.Instance;
        if (matchManager == null || row == null)
        {
            return false;
        }

        return matchManager.IsGoalkeeperReplacementRequired(row.isHomeTeam)
            || (row.selectedOutgoing != null && row.selectedOutgoing.IsGoalKeeper);
    }

    private bool ShouldKeepIncomingDropdownClosed(SelectionRow row)
    {
        MatchManager matchManager = MatchManager.Instance;
        return matchManager != null
            && row != null
            && row.selectedOutgoing != null
            && row.selectedIncoming != null
            && matchManager.IsGoalkeeperReplacementRequired(row.isHomeTeam)
            && !row.selectedOutgoing.IsGoalKeeper
            && row.selectedIncoming.IsGoalKeeper;
    }

    private bool IsGoalkeeperSubstitutionBlockingExit()
    {
        MatchManager matchManager = MatchManager.Instance;
        return matchManager != null
            && matchManager.ShouldOpenSubstitutionPanelForPendingGoalkeeperAction();
    }

    private void ApplyOptions(TMP_Dropdown dropdown, List<PlayerToken> optionTokens, PlayerToken selectedToken)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(optionTokens
            .Select(token => new TMP_Dropdown.OptionData(token == null ? "-" : $"{token.jerseyNumber}. {token.playerName}"))
            .ToList());
        int selectedIndex = Mathf.Max(0, optionTokens.IndexOf(selectedToken));
        dropdown.SetValueWithoutNotify(selectedIndex);
        dropdown.RefreshShownValue();
    }

    private void RefreshConfirmButton()
    {
        if (confirmButton == null)
        {
            return;
        }

        confirmButton.interactable = AreAllProposedSubstitutionsValid() && AreRequiredSubstitutionsSelected();
        ColorBlock colors = confirmButton.colors;
        colors.normalColor = confirmButton.interactable ? ButtonColor : DisabledButtonColor;
        confirmButton.colors = colors;

        if (backButton != null)
        {
            bool backInteractable = !IsGoalkeeperSubstitutionBlockingExit();
            backButton.interactable = backInteractable;
            ColorBlock backColors = backButton.colors;
            backColors.normalColor = backInteractable ? ButtonColor : DisabledButtonColor;
            backButton.colors = backColors;
        }
    }

    private bool AreRequiredSubstitutionsSelected()
    {
        List<PlayerToken> requiredTokens = GetRequiredPlayingTokens();
        MatchManager matchManager = MatchManager.Instance;
        if (matchManager != null && matchManager.IsAnyGoalkeeperReplacementRequired)
        {
            bool hasGoalkeeperReplacement = selectionRows.Any(row =>
                matchManager.IsGoalkeeperReplacementRequired(row.isHomeTeam)
                && row.selectedOutgoing != null
                && row.selectedIncoming != null
                && matchManager.CanRegisterSubstitution(row.selectedOutgoing, row.selectedIncoming, out _));
            if (!hasGoalkeeperReplacement)
            {
                return false;
            }
        }

        if (requiredTokens.Count == 0)
        {
            return true;
        }

        return requiredTokens.All(requiredToken => selectionRows.Any(row =>
            row.selectedOutgoing == requiredToken
            && row.selectedIncoming != null
            && MatchManager.Instance != null
            && MatchManager.Instance.CanRegisterSubstitution(row.selectedOutgoing, row.selectedIncoming, out _)));
    }

    private List<PlayerToken> GetRequiredPlayingTokens()
    {
        PlayerTokenManager tokenManager = MatchManager.Instance?.playerTokenManager;
        if (tokenManager == null)
        {
            return new List<PlayerToken>();
        }

        return tokenManager.GetPlayingTokens(true)
            .Concat(tokenManager.GetPlayingTokens(false))
            .Where(token => token != null && token.requiresSubstitution)
            .ToList();
    }

    private List<SelectionRow> GetValidRows()
    {
        MatchManager matchManager = MatchManager.Instance;
        if (matchManager == null || !matchManager.AreSubstitutionsAvailable)
        {
            return new List<SelectionRow>();
        }

        return selectionRows
            .Where(row => row.selectedOutgoing != null
                && row.selectedIncoming != null
                && matchManager.CanRegisterSubstitution(row.selectedOutgoing, row.selectedIncoming, out _))
            .ToList();
    }

    private List<SelectionRow> GetProposedRows()
    {
        return selectionRows
            .Where(row => row.selectedOutgoing != null || row.selectedIncoming != null)
            .ToList();
    }

    private bool AreAllProposedSubstitutionsValid()
    {
        MatchManager matchManager = MatchManager.Instance;
        if (matchManager == null || !matchManager.AreSubstitutionsAvailable)
        {
            return false;
        }

        List<SelectionRow> proposedRows = GetProposedRows();
        return proposedRows.Count > 0
            && proposedRows.All(row => row.selectedOutgoing != null
                && row.selectedIncoming != null
                && matchManager.CanRegisterSubstitution(row.selectedOutgoing, row.selectedIncoming, out _));
    }

    private void LogSubstitutionUiClick(
        string control,
        string action,
        params (string Key, object Value)[] details)
    {
        MatchManager.Instance?.RecordUiClick("substitutions_panel", control, action, "clicked", details);
    }

    private int CountSelectedRows()
    {
        return selectionRows.Count(row => row.selectedOutgoing != null || row.selectedIncoming != null);
    }

    private int GetTeamRowNumber(SelectionRow targetRow)
    {
        if (targetRow == null)
        {
            return 0;
        }

        List<SelectionRow> teamRows = selectionRows
            .Where(row => row.isHomeTeam == targetRow.isHomeTeam)
            .ToList();
        int index = teamRows.IndexOf(targetRow);
        return index >= 0 ? index + 1 : 0;
    }

    private string BuildSelectedSubstitutionsSummary()
    {
        IEnumerable<string> selectedRows = selectionRows
            .Where(row => row.selectedOutgoing != null || row.selectedIncoming != null)
            .Select(row => $"{FormatTeamSide(row.isHomeTeam)}:{FormatTokenForLog(row.selectedOutgoing)}->{FormatTokenForLog(row.selectedIncoming)}");
        return string.Join("; ", selectedRows);
    }

    private static string FormatTeamSide(bool isHomeTeam)
    {
        return isHomeTeam ? "Home" : "Away";
    }

    private static string FormatTokenForLog(PlayerToken token)
    {
        if (token == null)
        {
            return "-";
        }

        string playerName = string.IsNullOrWhiteSpace(token.playerName)
            ? token.name
            : token.playerName;
        return $"{MatchManager.GetStableTokenKey(token)}:{playerName}";
    }

    private void ConfirmSubstitutions()
    {
        MatchManager matchManager = MatchManager.Instance;
        if (matchManager == null || !matchManager.AreSubstitutionsAvailable)
        {
            RefreshConfirmButton();
            return;
        }

        List<SelectionRow> proposedRows = GetProposedRows();
        if (proposedRows.Count == 0 || !AreAllProposedSubstitutionsValid() || !AreRequiredSubstitutionsSelected())
        {
            RefreshConfirmButton();
            return;
        }

        PlayerTokenManager tokenManager = matchManager.playerTokenManager;
        List<bool> completedGoalkeeperReplacementTeams = new();
        foreach (SelectionRow row in proposedRows)
        {
            PlayerToken outgoing = row.selectedOutgoing;
            PlayerToken incoming = row.selectedIncoming;
            bool completedGoalkeeperReplacement = matchManager.IsGoalkeeperReplacementRequired(outgoing.isHomeTeam)
                && !outgoing.IsGoalKeeper
                && incoming.IsGoalKeeper;
            HexCell outgoingHex = outgoing.GetCurrentHex();
            HexCell destinationHex = completedGoalkeeperReplacement
                ? matchManager.GetGoalkeeperReplacementDestinationHex(outgoing.isHomeTeam) ?? outgoingHex
                : outgoingHex;
            Vector3 incomingBenchPosition = incoming.transform.position;
            if (!matchManager.RegisterSubstitution(outgoing, incoming, out string error))
            {
                Debug.LogWarning(error);
                continue;
            }

            Vector3 destinationPosition = destinationHex != null
                ? GetTokenPositionForHex(destinationHex, outgoing.transform.position.y)
                : outgoing.transform.position;
            bool wasAttacker = outgoing.isAttacker;
            bool ballWasOnDestinationHex = matchManager.ball != null && matchManager.ball.GetCurrentHex() == destinationHex;

            tokenManager.MoveActiveTokenToBenchSlot(outgoing, incomingBenchPosition);
            tokenManager.MoveBenchTokenToActive(incoming);
            incoming.isAttacker = wasAttacker;
            incoming.transform.position = destinationPosition;
            if (destinationHex != null)
            {
                destinationHex.occupyingToken = null;
                destinationHex.isAttackOccupied = wasAttacker;
                destinationHex.isDefenseOccupied = !wasAttacker;
                incoming.SetCurrentHex(destinationHex);
                destinationHex.ResetHighlight();
                destinationHex.HighlightHex(wasAttacker ? "isAttackOccupied" : "isDefenseOccupied");
                if (ballWasOnDestinationHex)
                {
                    matchManager.ball.PlaceAtCell(destinationHex);
                }
            }

            // Check if outfield player is being brought on as GK
            if (outgoing.IsGoalKeeper && !incoming.IsGoalKeeper)
            {
                Debug.Log($"Converting {incoming.playerName} to goalkeeper role.");
                matchManager.ConvertOutfieldToGK(incoming);
            }

            if (completedGoalkeeperReplacement)
            {
                completedGoalkeeperReplacementTeams.Add(outgoing.isHomeTeam);
            }

            Debug.Log($"Substitution confirmed: {outgoing.name} off, {incoming.name} on.");
        }

        foreach (bool isHomeTeam in completedGoalkeeperReplacementTeams.Distinct())
        {
            matchManager.CompleteGoalkeeperReplacement(isHomeTeam);
        }

        CloseToPauseMenu();
    }

    private static Vector3 GetTokenPositionForHex(HexCell hex, float y)
    {
        Vector3 center = hex.GetHexCenter();
        return new Vector3(center.x, y, center.z);
    }

    private Button CreateButton(Transform parent, string name, string label, float width = 210f, float height = 44f)
    {
        GameObject buttonObject = CreateRect(name, parent, typeof(Image), typeof(Button));
        Image image = buttonObject.GetComponent<Image>();
        image.color = ButtonColor;
        Button button = buttonObject.GetComponent<Button>();
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
        SetButtonText(button, label);
        return button;
    }

    private void SetButtonText(Button button, string label)
    {
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text == null)
        {
            text = CreateText("Text", button.transform, label, 18f, Color.black, TextAlignmentOptions.Center);
            Stretch(text.GetComponent<RectTransform>());
        }

        text.text = label;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject gameObject = CreateRect(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI label = gameObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Truncate;
        return label;
    }

    private static GameObject CreateRect(string name, Transform parent, params System.Type[] components)
    {
        List<System.Type> componentList = new() { typeof(RectTransform), typeof(CanvasRenderer) };
        componentList.AddRange(components);
        GameObject gameObject = new(name, componentList.ToArray());
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

}
