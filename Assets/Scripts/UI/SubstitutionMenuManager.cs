using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SubstitutionMenuManager : MonoBehaviour
{
    private const string SubstitutionPanelResourcePath = "UI/SubstitutionPanel";
    private static readonly Color PanelColor = new(0.05f, 0.06f, 0.07f, 0.94f);
    private static readonly Color ColumnColor = new(0.12f, 0.13f, 0.15f, 0.96f);
    private static readonly Color ButtonColor = new(0.88f, 0.88f, 0.88f, 1f);
    private static readonly Color DisabledButtonColor = new(0.42f, 0.42f, 0.42f, 1f);
    private static readonly Color TextColor = new(0.94f, 0.94f, 0.94f, 1f);
    private static readonly Color MutedTextColor = new(0.66f, 0.68f, 0.7f, 1f);
    private static readonly Color InjuredColor = new(1f, 0.55f, 0.12f, 1f);
    private static readonly Color CautionedColor = new(1f, 0.86f, 0.18f, 1f);
    private static readonly Color SentOffColor = new(1f, 0.2f, 0.18f, 1f);
    private static readonly Color OutDropdownColor = new(0.36f, 0.08f, 0.08f, 1f);
    private static readonly Color InDropdownColor = new(0.08f, 0.28f, 0.13f, 1f);

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
        LogSubstitutionUiClick(
            "confirm_button",
            "confirm_substitutions",
            ("validSelectionCount", validRows.Count),
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

        SubstitutionMenuView prefab = Resources.Load<SubstitutionMenuView>(SubstitutionPanelResourcePath);
        if (prefab != null)
        {
            substitutionView = Instantiate(prefab, parent);
            substitutionPanel = substitutionView.gameObject;
            if (!substitutionView.HasRequiredReferences())
            {
                Debug.LogWarning("SubstitutionPanel prefab is missing one or more required references. Falling back to generated layout.");
                Destroy(substitutionPanel);
                substitutionPanel = null;
                substitutionView = null;
            }
            else
            {
                WirePanelButtons();
                substitutionPanel.SetActive(false);
                return;
            }
        }

        CreateGeneratedPanel(parent);
    }

    private void CreateGeneratedPanel(Transform parent)
    {
        substitutionPanel = CreateRect("SubstitutionPanel", parent, typeof(Image));
        substitutionView = substitutionPanel.AddComponent<SubstitutionMenuView>();
        Image panelImage = substitutionPanel.GetComponent<Image>();
        panelImage.color = PanelColor;
        RectTransform panelRect = substitutionPanel.GetComponent<RectTransform>();
        Stretch(panelRect);

        VerticalLayoutGroup rootLayout = substitutionPanel.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(16, 16, 12, 12);
        rootLayout.spacing = 8;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        TextMeshProUGUI title = CreateText("Title", substitutionPanel.transform, "Substitutions", 28f, TextColor, TextAlignmentOptions.Center);
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
        substitutionView.titleText = title;

        GameObject body = CreateRect("Body", substitutionPanel.transform);
        LayoutElement bodyLayout = body.AddComponent<LayoutElement>();
        bodyLayout.flexibleHeight = 1f;
        bodyLayout.minHeight = 0f;
        HorizontalLayoutGroup bodyGroup = body.AddComponent<HorizontalLayoutGroup>();
        bodyGroup.spacing = 12;
        bodyGroup.childControlWidth = true;
        bodyGroup.childControlHeight = true;
        bodyGroup.childForceExpandWidth = true;
        bodyGroup.childForceExpandHeight = true;

        CreateTeamColumn(body.transform, true);
        CreateTeamColumn(body.transform, false);

        GameObject footer = CreateRect("Footer", substitutionPanel.transform);
        footer.AddComponent<LayoutElement>().preferredHeight = 38f;
        HorizontalLayoutGroup footerGroup = footer.AddComponent<HorizontalLayoutGroup>();
        footerGroup.childAlignment = TextAnchor.MiddleRight;
        footerGroup.spacing = 10;
        footerGroup.childControlWidth = false;
        footerGroup.childControlHeight = false;
        footerGroup.childForceExpandWidth = false;
        footerGroup.childForceExpandHeight = false;

        GameObject spacer = CreateRect("FooterSpacer", footer.transform);
        spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;
        backButton = CreateButton(footer.transform, "BackButton", "Back", 130f, 34f);
        confirmButton = CreateButton(footer.transform, "ConfirmSubsButton", "Confirm Subs", 170f, 34f);
        substitutionView.backButton = backButton;
        substitutionView.confirmButton = confirmButton;
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

    private void CreateTeamColumn(Transform parent, bool isHomeTeam)
    {
        GameObject column = CreateRect(isHomeTeam ? "HomeSubstitutionsColumn" : "AwaySubstitutionsColumn", parent, typeof(Image));
        column.GetComponent<Image>().color = ColumnColor;
        LayoutElement layoutElement = column.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;
        layoutElement.minWidth = 360f;

        VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 6;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI teamTitle = CreateText("TeamTitle", column.transform, isHomeTeam ? "Home" : "Away", 20f, TextColor, TextAlignmentOptions.Center);
        teamTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

        GameObject rosterArea = CreateRect("RosterArea", column.transform);
        LayoutElement rosterLayout = rosterArea.AddComponent<LayoutElement>();
        rosterLayout.preferredHeight = 220f;
        rosterLayout.flexibleHeight = 1f;

        HorizontalLayoutGroup rosterGroup = rosterArea.AddComponent<HorizontalLayoutGroup>();
        rosterGroup.spacing = 12;
        rosterGroup.childControlWidth = true;
        rosterGroup.childControlHeight = true;
        rosterGroup.childForceExpandWidth = true;
        rosterGroup.childForceExpandHeight = true;

        Transform playingList = CreateRosterListShell(rosterArea.transform, "Playing");
        Transform benchList = CreateRosterListShell(rosterArea.transform, "Bench");

        TextMeshProUGUI remainingText = CreateText("SubsRemaining", column.transform, "3 subs remaining", 15f, MutedTextColor, TextAlignmentOptions.Center);
        remainingText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        GameObject rowsContainer = CreateRect("SubstitutionRows", column.transform);
        rowsContainer.AddComponent<LayoutElement>().preferredHeight = 140f;
        VerticalLayoutGroup rowsLayout = rowsContainer.AddComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 4;
        rowsLayout.childControlWidth = true;
        rowsLayout.childControlHeight = false;
        rowsLayout.childForceExpandWidth = true;
        rowsLayout.childForceExpandHeight = false;

        SubstitutionDropdownRowView[] rows = new SubstitutionDropdownRowView[MatchManager.ExtraTimeMaxSubstitutionsPerTeam];
        for (int index = 0; index < rows.Length; index++)
        {
            rows[index] = CreateSelectionRowView(rowsContainer.transform, isHomeTeam, index);
        }

        if (isHomeTeam)
        {
            substitutionView.homeTeamTitleText = teamTitle;
            substitutionView.homePlayingListContent = playingList;
            substitutionView.homeBenchListContent = benchList;
            substitutionView.homeSubsRemainingText = remainingText;
            substitutionView.homeSubstitutionRowsContainer = rowsContainer.transform;
            substitutionView.homeRows = rows;
        }
        else
        {
            substitutionView.awayTeamTitleText = teamTitle;
            substitutionView.awayPlayingListContent = playingList;
            substitutionView.awayBenchListContent = benchList;
            substitutionView.awaySubsRemainingText = remainingText;
            substitutionView.awaySubstitutionRowsContainer = rowsContainer.transform;
            substitutionView.awayRows = rows;
        }
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
        Transform playingList = isHomeTeam ? substitutionView.homePlayingListContent : substitutionView.awayPlayingListContent;
        Transform benchList = isHomeTeam ? substitutionView.homeBenchListContent : substitutionView.awayBenchListContent;

        teamTitle.text = teamName;
        BuildLineupRows(playingList, benchList, isHomeTeam);

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

    private void BuildLineupRows(Transform playingList, Transform benchList, bool isHomeTeam)
    {
        ClearChildren(playingList);
        ClearChildren(benchList);
        Dictionary<string, MatchManager.RosterPlayer> roster = isHomeTeam
            ? MatchManager.Instance.gameData.rosters.home
            : MatchManager.Instance.gameData.rosters.away;
        if (roster == null)
        {
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

        FillRosterList(playingList, entries.Where(IsPlayingRosterEntry).OrderBy(entry => entry.jersey));
        FillRosterList(benchList, entries.Where(entry => !IsPlayingRosterEntry(entry)).OrderBy(entry => entry.jersey));
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

    private Transform CreateRosterListShell(Transform parent, string title)
    {
        GameObject listObject = CreateRect($"{title}List", parent);
        LayoutElement listLayout = listObject.AddComponent<LayoutElement>();
        listLayout.flexibleWidth = 1f;
        listLayout.minWidth = 0f;

        VerticalLayoutGroup listGroup = listObject.AddComponent<VerticalLayoutGroup>();
        listGroup.spacing = 2;
        listGroup.childControlWidth = true;
        listGroup.childControlHeight = false;
        listGroup.childForceExpandWidth = true;
        listGroup.childForceExpandHeight = false;

        TextMeshProUGUI header = CreateText($"{title}Header", listObject.transform, title, 14f, MutedTextColor, TextAlignmentOptions.Left);
        header.fontStyle = FontStyles.Bold;
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        GameObject content = CreateRect("Content", listObject.transform);
        VerticalLayoutGroup contentGroup = content.AddComponent<VerticalLayoutGroup>();
        contentGroup.spacing = 2;
        contentGroup.childControlWidth = true;
        contentGroup.childControlHeight = false;
        contentGroup.childForceExpandWidth = true;
        contentGroup.childForceExpandHeight = false;
        return content.transform;
    }

    private void FillRosterList(Transform listContent, IEnumerable<RosterEntry> entries)
    {
        foreach (RosterEntry entry in entries)
        {
            string label = BuildPlayerDisplay(entry.jersey, entry.playerName, entry.token);
            TextMeshProUGUI rowText = CreateText($"Player_{entry.jersey}", listContent, label, 13f, TextColor, TextAlignmentOptions.Left);
            rowText.richText = true;
            rowText.gameObject.AddComponent<LayoutElement>().preferredHeight = 17f;
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

    private SubstitutionDropdownRowView CreateSelectionRowView(Transform parent, bool isHomeTeam, int index)
    {
        GameObject rowObject = CreateRect($"{(isHomeTeam ? "Home" : "Away")}SubRow_{index + 1}", parent);
        rowObject.AddComponent<LayoutElement>().preferredHeight = 32f;
        HorizontalLayoutGroup rowLayout = rowObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 8;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;

        TMP_Dropdown outgoing = CreateDropdown(rowObject.transform, "OutgoingDropdown", "v", OutDropdownColor);
        TMP_Dropdown incoming = CreateDropdown(rowObject.transform, "IncomingDropdown", "^", InDropdownColor);
        SubstitutionDropdownRowView rowView = rowObject.AddComponent<SubstitutionDropdownRowView>();
        rowView.outgoingDropdown = outgoing;
        rowView.incomingDropdown = incoming;
        return rowView;
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
        foreach (bool isHomeTeam in new[] { true, false })
        {
            List<PlayerToken> requiredTokens = GetOutgoingCandidates(isHomeTeam)
                .Where(token => token.requiresSubstitution)
                .ToList();
            List<SelectionRow> teamRows = selectionRows.Where(row => row.isHomeTeam == isHomeTeam).ToList();
            for (int index = 0; index < requiredTokens.Count && index < teamRows.Count; index++)
            {
                teamRows[index].selectedOutgoing = requiredTokens[index];
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
        if (row.selectedOutgoing != null)
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

        return tokenManager.GetPlayingTokens(isHomeTeam)
            .Where(token => token != null && !token.isSentOff && token.GetCurrentHex() != null)
            .Where(token => MatchManager.Instance == null
                || !MatchManager.Instance.IsGoalkeeperReplacementRequired(isHomeTeam)
                || !token.IsGoalKeeper)
            .OrderBy(token => token.jerseyNumber)
            .ToList();
    }

    private List<PlayerToken> GetIncomingCandidates(SelectionRow row)
    {
        if (row.selectedOutgoing == null)
        {
            return new List<PlayerToken>();
        }

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

        confirmButton.interactable = GetValidRows().Count > 0 && AreRequiredSubstitutionsSelected();
        ColorBlock colors = confirmButton.colors;
        colors.normalColor = confirmButton.interactable ? ButtonColor : DisabledButtonColor;
        confirmButton.colors = colors;
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

        List<SelectionRow> validRows = GetValidRows();
        if (validRows.Count == 0 || !AreRequiredSubstitutionsSelected())
        {
            RefreshConfirmButton();
            return;
        }

        PlayerTokenManager tokenManager = matchManager.playerTokenManager;
        foreach (SelectionRow row in validRows)
        {
            PlayerToken outgoing = row.selectedOutgoing;
            PlayerToken incoming = row.selectedIncoming;
            HexCell destinationHex = outgoing.GetCurrentHex();
            Vector3 incomingBenchPosition = incoming.transform.position;
            if (!matchManager.RegisterSubstitution(outgoing, incoming, out string error))
            {
                Debug.LogWarning(error);
                continue;
            }

            Vector3 destinationPosition = outgoing.transform.position;
            bool wasAttacker = outgoing.isAttacker;
            bool ballWasOnOutgoingHex = matchManager.ball != null && matchManager.ball.GetCurrentHex() == destinationHex;

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
                if (ballWasOnOutgoingHex)
                {
                    matchManager.ball.PlaceAtCell(destinationHex);
                }
            }

            bool completedGoalkeeperReplacement = matchManager.IsGoalkeeperReplacementRequired(outgoing.isHomeTeam)
                && !outgoing.IsGoalKeeper
                && incoming.IsGoalKeeper;

            // Check if outfield player is being brought on as GK
            if (outgoing.IsGoalKeeper && !incoming.IsGoalKeeper)
            {
                Debug.Log($"Converting {incoming.playerName} to goalkeeper role.");
                matchManager.ConvertOutfieldToGK(incoming);
            }

            if (completedGoalkeeperReplacement)
            {
                matchManager.CompleteGoalkeeperReplacement(outgoing.isHomeTeam);
            }

            Debug.Log($"Substitution confirmed: {outgoing.name} off, {incoming.name} on.");
        }

        CloseToPauseMenu();
    }

    private TMP_Dropdown CreateDropdown(Transform parent, string name, string arrowText, Color backgroundColor)
    {
        GameObject root = CreateRect(name, parent, typeof(Image), typeof(TMP_Dropdown));
        root.GetComponent<Image>().color = backgroundColor;
        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.minHeight = 30f;
        layout.preferredHeight = 30f;

        TMP_Dropdown dropdown = root.GetComponent<TMP_Dropdown>();
        TextMeshProUGUI label = CreateText("Label", root.transform, "-", 13f, TextColor, TextAlignmentOptions.MidlineLeft);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(10f, 0f);
        labelRect.offsetMax = new Vector2(-34f, 0f);

        TextMeshProUGUI arrow = CreateText("Arrow", root.transform, arrowText, 16f, TextColor, TextAlignmentOptions.Center);
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0f);
        arrowRect.anchorMax = new Vector2(1f, 1f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.sizeDelta = new Vector2(28f, 0f);
        arrowRect.anchoredPosition = Vector2.zero;

        RectTransform template = CreateDropdownTemplate(root.transform, out TextMeshProUGUI itemText);
        dropdown.template = template;
        dropdown.captionText = label;
        dropdown.itemText = itemText;
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<TMP_Dropdown.OptionData> { new("-") });
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private RectTransform CreateDropdownTemplate(Transform parent, out TextMeshProUGUI itemText)
    {
        GameObject template = CreateRect("Template", parent, typeof(Image), typeof(ScrollRect));
        template.SetActive(false);
        Image templateImage = template.GetComponent<Image>();
        templateImage.color = new Color(0.08f, 0.09f, 0.1f, 0.98f);
        RectTransform templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 1f);
        templateRect.anchorMax = new Vector2(1f, 1f);
        templateRect.pivot = new Vector2(0.5f, 0f);
        templateRect.anchoredPosition = new Vector2(0f, 0f);
        templateRect.sizeDelta = new Vector2(0f, 132f);

        GameObject viewport = CreateRect("Viewport", template.transform, typeof(Image), typeof(Mask));
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;
        Stretch(viewport.GetComponent<RectTransform>());

        GameObject content = CreateRect("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 28f);
        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject item = CreateRect("Item", content.transform, typeof(Toggle), typeof(Image));
        item.AddComponent<LayoutElement>().preferredHeight = 26f;
        Toggle toggle = item.GetComponent<Toggle>();
        Image itemImage = item.GetComponent<Image>();
        itemImage.color = new Color(0.16f, 0.17f, 0.19f, 1f);
        toggle.targetGraphic = itemImage;

        itemText = CreateText("Item Label", item.transform, "Option", 13f, TextColor, TextAlignmentOptions.MidlineLeft);
        RectTransform itemTextRect = itemText.GetComponent<RectTransform>();
        itemTextRect.anchorMin = Vector2.zero;
        itemTextRect.anchorMax = Vector2.one;
        itemTextRect.offsetMin = new Vector2(10f, 0f);
        itemTextRect.offsetMax = new Vector2(-10f, 0f);

        ScrollRect scrollRect = template.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        return templateRect;
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

    private static void ClearChildren(Transform parent)
    {
        for (int index = parent.childCount - 1; index >= 0; index--)
        {
            Destroy(parent.GetChild(index).gameObject);
        }
    }

    private static void AddSpacer(Transform parent, float height)
    {
        GameObject spacer = CreateRect("Spacer", parent);
        spacer.AddComponent<LayoutElement>().preferredHeight = height;
    }
}
