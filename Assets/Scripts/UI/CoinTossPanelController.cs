using UnityEngine;
using UnityEngine.UI;

/// <summary>Both draft scenes use this authored panel before loading Room.</summary>
public sealed class CoinTossPanelController : MonoBehaviour
{
    public DraftOrderPanelController dicePanel;
    public GameObject choices;
    public Button kickoffButton, leftToRightButton, rightToLeftButton;
    private GameSettings settings;
    public CoinTossDecision Decision { get; private set; }
    public MatchOpeningSetup Result => Decision?.Result;
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
        if (kickoffButton != null) kickoffButton.onClick.RemoveListener(ChooseKickoff);
        if (leftToRightButton != null) leftToRightButton.onClick.RemoveListener(ChooseLeftToRight);
        if (rightToLeftButton != null) rightToLeftButton.onClick.RemoveListener(ChooseRightToLeft);
    }

    public bool Open(GameSettings gameSettings)
    {
        if (!HasSceneBindings || IsOpen || gameSettings == null) return false;
        settings = gameSettings;
        Decision = null;
        choices.SetActive(false);
        dicePanel.panel.sizeDelta = new Vector2(1440, 720);
        SetTop(dicePanel.footer.rectTransform, 625);
        return dicePanel.Open(settings, true);
    }

    private void ChooseKickoff() => Choose(CoinTossChoice.Kickoff);
    private void ChooseLeftToRight() => Choose(CoinTossChoice.LeftToRight);
    private void ChooseRightToLeft() => Choose(CoinTossChoice.RightToLeft);

    public void Choose(CoinTossChoice choice)
    {
        if (Decision == null || !IsOpen || !Decision.Choose(choice)) return;
        if (Result != null)
        {
            Debug.Log($"Coin toss: {DraftOrderPanelController.TeamName(settings, Result.homeHasFirstHalfKickoff)} kicks off. "
                + $"{DraftOrderPanelController.TeamName(settings, true)} attacks {Result.DirectionFor(true)}; "
                + $"{DraftOrderPanelController.TeamName(settings, false)} attacks {Result.DirectionFor(false)}.");
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
            Decision = new CoinTossDecision(dicePanel.HomeWon.Value);
            dicePanel.panel.sizeDelta = new Vector2(1440, 1080);
            SetTop(dicePanel.footer.rectTransform, 995);
            choices.SetActive(true);
            RefreshChoices();
        }
        if (Result == null && CoinTossDecision.IsCpuManager(settings, Decision.ChoosingHome))
            Choose(Decision.CpuChoice);
    }

    private void RefreshChoices()
    {
        string chooser = DraftOrderPanelController.TeamName(settings, Decision.ChoosingHome);
        string winner = DraftOrderPanelController.TeamName(settings, Decision.WinnerIsHome);
        dicePanel.outcome.text = Decision.CanChooseKickoff ? $"{winner} wins · Choose kickoff or direction"
            : $"{winner} takes first-half kickoff · {chooser} chooses direction";
        bool human = !CoinTossDecision.IsCpuManager(settings, Decision.ChoosingHome);
        kickoffButton.interactable = human && Decision.CanChooseKickoff;
        leftToRightButton.interactable = rightToLeftButton.interactable = human;
        dicePanel.footer.text = $"{chooser} manager · {(human ? "Click your choice" : "CPU choosing")} · Directions describe your team's attack";
    }

    private static void SetTop(RectTransform rect, float top) => rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -top);
}
