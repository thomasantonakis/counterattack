using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene-authored first-half, extra-time and shootout coin-toss presentation.</summary>
public sealed class CoinTossPanelController : MonoBehaviour
{
    public DraftOrderPanelController dicePanel;
    public GameObject choices;
    public Button kickoffButton, leftToRightButton, rightToLeftButton;
    private GameSettings settings;
    public CoinTossPurpose Purpose { get; private set; }
    public static CoinTossPanelController ActiveRoomPanel { get; private set; }
    public static bool IsRoomVisible => ActiveRoomPanel != null && ActiveRoomPanel.IsOpen;
    public static bool? RoomInputIsHome => !IsRoomVisible ? null
        : ActiveRoomPanel.dicePanel.IsAwaitingHomeRoll ? true
        : ActiveRoomPanel.dicePanel.IsAwaitingAwayRoll ? false : ActiveRoomPanel.Decision?.ChoosingHome;
    public CoinTossDecision Decision { get; private set; }
    public MatchOpeningSetup Result => Decision?.Result;
    public PenaltyShootoutSetup ShootoutResult => Decision?.ShootoutResult;
    public bool IsOpen => dicePanel != null && dicePanel.IsOpen;
    public bool HasSceneBindings => dicePanel != null && dicePanel.HasSceneBindings && choices != null
        && kickoffButton != null && leftToRightButton != null && rightToLeftButton != null;

    private void Awake()
    {
        if (choices != null) choices.SetActive(false);
        if (kickoffButton != null) kickoffButton.onClick.AddListener(ChooseKickoff);
        if (leftToRightButton != null) leftToRightButton.onClick.AddListener(ChooseLeftToRight);
        if (rightToLeftButton != null) rightToLeftButton.onClick.AddListener(ChooseRightToLeft);
    }

    private void OnDestroy()
    {
        if (ActiveRoomPanel == this) ActiveRoomPanel = null;
        if (kickoffButton != null) kickoffButton.onClick.RemoveListener(ChooseKickoff);
        if (leftToRightButton != null) leftToRightButton.onClick.RemoveListener(ChooseLeftToRight);
        if (rightToLeftButton != null) rightToLeftButton.onClick.RemoveListener(ChooseRightToLeft);
    }

    public bool Open(GameSettings gameSettings, CoinTossPurpose purpose = CoinTossPurpose.FirstHalf)
    {
        if (!HasSceneBindings || IsOpen || gameSettings == null) return false;
        settings = gameSettings;
        Purpose = purpose;
        Decision = null;
        choices.SetActive(false);
        dicePanel.panel.sizeDelta = new Vector2(1440, 720);
        SetTop(dicePanel.footer.rectTransform, 625);
        bool shootout = Purpose == CoinTossPurpose.PenaltyShootout;
        string half = Purpose == CoinTossPurpose.ExtraTime ? "Extra Time First Half" : "First Half";
        SetLabel(kickoffButton, shootout ? "1. Take the First Penalty" : $"1. {half} Kick Off");
        SetLabel(leftToRightButton, shootout ? "2. Left Side" : $"2. {half} Left to Right");
        SetLabel(rightToLeftButton, shootout ? "3. Right Side" : $"3. {half} Right to Left");
        string title = Purpose == CoinTossPurpose.FirstHalf ? "COIN TOSS FLIP"
            : $"COIN TOSS FLIP · {(shootout ? "PENALTY SHOOTOUT" : "EXTRA TIME")}";
        bool opened = dicePanel.Open(settings, true, title, shootout ? "first penalty or shootout end" : "kickoff or direction");
        if (opened && dicePanel.roomMatch != null) ActiveRoomPanel = this;
        return opened;
    }

    private void ChooseKickoff() => Choose(CoinTossChoice.Kickoff);

    public bool OpenForRoom(MatchManager match, CoinTossPurpose purpose)
    {
        var source = match?.gameData?.gameSettings;
        if (source == null || dicePanel == null || dicePanel.roomMatch != match) return false;
        return Open(new GameSettings
        {
            gameMode = source.gameMode, homeTeamName = source.homeTeamName, awayTeamName = source.awayTeamName,
            homeKit = source.homeKit, awayKit = source.awayKit,
            homeTeamControl = source.homeTeamControl, awayTeamControl = source.awayTeamControl,
            homeRoomPersona = source.homeRoomPersona, awayRoomPersona = source.awayRoomPersona
        }, purpose);
    }
    private void ChooseLeftToRight() => Choose(Purpose == CoinTossPurpose.PenaltyShootout ? CoinTossChoice.LeftSide : CoinTossChoice.LeftToRight);
    private void ChooseRightToLeft() => Choose(Purpose == CoinTossPurpose.PenaltyShootout ? CoinTossChoice.RightSide : CoinTossChoice.RightToLeft);

    public void Choose(CoinTossChoice choice)
    {
        if (Decision == null || !IsOpen || !Decision.Choose(choice)) return;
        dicePanel.roomMatch?.RecordGameplayOutcome("coin_toss.choice", "coin_toss", "selected",
            details: new System.Collections.Generic.Dictionary<string, string>
            {
                ["purpose"] = Purpose.ToString(), ["choice"] = choice.ToString(),
                ["team"] = (choice == CoinTossChoice.Kickoff ? Decision.WinnerIsHome : Decision.ChoosingHome) ? "Home" : "Away"
            });
        if (Decision.IsComplete)
        {
            if (Result != null) Debug.Log($"Coin toss: {DraftOrderPanelController.TeamName(settings, Result.homeHasFirstHalfKickoff)} kicks off. "
                + $"{DraftOrderPanelController.TeamName(settings, true)} attacks {Result.DirectionFor(true)}; "
                + $"{DraftOrderPanelController.TeamName(settings, false)} attacks {Result.DirectionFor(false)}.");
            else Debug.Log($"Coin toss: {DraftOrderPanelController.TeamName(settings, ShootoutResult.homeTakesFirstPenalty)} takes the first penalty at the {(ShootoutResult.useRightGoal ? "right" : "left")} goal.");
            dicePanel.Close();
            return;
        }
        RefreshChoices();
    }

    private void Update() => AdvanceChoices();

    public void AdvanceChoices()
    {
        if (!IsOpen || !dicePanel.HomeWon.HasValue) return;
        if (Decision == null)
        {
            Decision = new CoinTossDecision(dicePanel.HomeWon.Value, Purpose);
            dicePanel.panel.sizeDelta = new Vector2(1440, 1080);
            SetTop(dicePanel.footer.rectTransform, 995);
            choices.SetActive(true);
            RefreshChoices();
        }
        if (!Decision.IsComplete && CoinTossDecision.IsCpuManager(settings, Decision.ChoosingHome))
            Choose(Decision.CpuChoice);
    }

    private void RefreshChoices()
    {
        string chooser = DraftOrderPanelController.TeamName(settings, Decision.ChoosingHome);
        string winner = DraftOrderPanelController.TeamName(settings, Decision.WinnerIsHome);
        bool shootout = Purpose == CoinTossPurpose.PenaltyShootout;
        string opening = shootout ? "the first penalty" : Purpose == CoinTossPurpose.ExtraTime ? "extra-time kickoff" : "first-half kickoff";
        dicePanel.outcome.text = Decision.CanChooseKickoff ? $"{winner} wins · Choose {(shootout ? "first penalty or shootout end" : "kickoff or direction")}"
            : $"{winner} takes {opening} · {chooser} chooses {(shootout ? "shootout end" : "direction")}";
        bool human = !CoinTossDecision.IsCpuManager(settings, Decision.ChoosingHome);
        kickoffButton.interactable = human && Decision.CanChooseKickoff;
        leftToRightButton.interactable = rightToLeftButton.interactable = human;
        dicePanel.footer.text = $"{chooser} manager · {(human ? "Click your choice" : "CPU choosing")} · {(shootout ? "Side selects the goal used by both teams" : "Directions describe your team's attack")}";
    }

    public static bool ConsumeRoomKey(KeyPressData key)
    {
        if (!IsRoomVisible) return false;
        key.expectedByHomeTeam = RoomInputIsHome;
        ActiveRoomPanel.dicePanel.HandleKey(key);
        key.Consume(nameof(CoinTossPanelController), RoomInputIsHome);
        return true;
    }

    private static void SetLabel(Button button, string text)
    {
        var label = button.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (label != null) label.text = text;
    }

    private static void SetTop(RectTransform rect, float top) => rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -top);
}
