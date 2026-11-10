using TMPro;
using UnityEngine;

/// <summary>Scene-authored first-pick and coin-toss roll-offs. Animation never consumes gameplay randomness.</summary>
public sealed class DraftOrderPanelController : MonoBehaviour
{
    public GameObject visuals;
    public RectTransform panel;
    public TMP_Text title, homeTeam, awayTeam, homeResult, awayResult, outcome, footer;
    public DuelDieView homeDie, awayDie;
    private enum Stage { Closed, Home, Away, Result, Choice }
    private Stage stage;
    private int homeScore, awayScore;
    private float resultUntil;
    private bool coinToss;
    private string coinTossChoices;
    public bool externalInput;
    public MatchManager roomMatch;
    public bool IsOpen => stage != Stage.Closed;
    public bool? HomeWon { get; private set; }
    public bool IsAwaitingHomeRoll => stage == Stage.Home;
    public bool IsAwaitingAwayRoll => stage == Stage.Away;
    public bool HasSceneBindings => visuals != null && panel != null && title != null && homeTeam != null && awayTeam != null
        && homeResult != null && awayResult != null && outcome != null && footer != null && homeDie != null && awayDie != null;

    private void Awake() { if (visuals != null) visuals.SetActive(false); }
    private void OnDisable()
    {
        stage = Stage.Closed;
        HomeWon = null;
        if (visuals != null) visuals.SetActive(false);
        if (homeDie != null) homeDie.Clear();
        if (awayDie != null) awayDie.Clear();
    }

    public static string TeamName(GameSettings settings, bool home)
    {
        string name = home ? settings?.homeTeamName : settings?.awayTeamName;
        return string.IsNullOrWhiteSpace(name) ? (home ? "Home" : "Away") : name;
    }

    public bool Open(GameSettings settings, bool forCoinToss = false, string tossTitle = "COIN TOSS FLIP", string choicesCopy = "kickoff or direction")
    {
        if (!HasSceneBindings || settings == null || IsOpen) return false;
        HomeWon = null;
        coinToss = forCoinToss;
        coinTossChoices = choicesCopy;
        title.text = coinToss ? tossTitle : "ROLL FOR FIRST DRAFT PICK";
        homeTeam.text = TeamName(settings, true);
        awayTeam.text = TeamName(settings, false);
        var home = TokenKitCatalog.GetInstructionPalette(TokenKitCatalog.ResolveStyle(settings.homeKit));
        var away = TokenKitCatalog.GetInstructionPalette(TokenKitCatalog.ResolveStyle(settings.awayKit));
        homeDie.SetTeamColors(home.Primary, home.Secondary);
        awayDie.SetTeamColors(away.Primary, away.Secondary);
        visuals.SetActive(true);
        visuals.transform.SetAsLastSibling();
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        BeginRound();
        Fit();
        return true;
    }

    private void BeginRound()
    {
        homeScore = awayScore = 0;
        homeResult.text = "";
        awayResult.text = "Waiting for roll";
        outcome.text = coinToss ? $"Highest roll chooses {coinTossChoices}" : "Highest roll drafts first";
        homeDie.Begin(false);
        awayDie.Clear();
        stage = Stage.Home;
        footer.text = $"{homeTeam.text} manager · Press [R] to roll";
    }

    public bool HandleKey(KeyPressData key)
    {
        if (!IsOpen) return false;
        bool hasOverride = RollInputOverride.TryParse(key, out var rollOverride) && !rollOverride.isJackpot;
        if (key.key != KeyCode.R && !hasOverride) return false;
        key.Consume(nameof(DraftOrderPanelController));
        if (stage is Stage.Home or Stage.Away)
            Roll(hasOverride ? rollOverride : null);
        return true;
    }

    public void Roll(RollInputOverride? rollOverride = null)
    {
        if (stage is not (Stage.Home or Stage.Away)) return;
        if (roomMatch == null) MenuAudioPlayer.Play(GameplayAudioPool.Dice);
        int roll = roomMatch != null
            ? roomMatch.ResolveGameplayDiceRoll("coin_toss", rollOverride, jackpotEnabled: false,
                details: new System.Collections.Generic.Dictionary<string, string>
                {
                    ["team"] = stage == Stage.Home ? "Home" : "Away", ["purpose"] = title.text
                }).roll
            : rollOverride.HasValue && rollOverride.Value.hasOverride
                ? rollOverride.Value.isJackpot ? 6 : Mathf.Clamp(rollOverride.Value.roll, 1, 6)
                : Random.Range(1, 7);
        string result = $"Roll {roll}";
        if (stage == Stage.Home)
        {
            homeScore = roll;
            homeDie.Resolve(roll, false);
            homeResult.text = result;
            awayResult.text = "";
            awayDie.Begin(false);
            stage = Stage.Away;
            footer.text = $"{awayTeam.text} manager · Press [R] to roll";
            return;
        }
        awayScore = roll;
        awayDie.Resolve(roll, false);
        awayResult.text = result;
        outcome.text = homeScore == awayScore ? "TIE · Both managers roll again"
            : $"{(homeScore > awayScore ? homeTeam.text : awayTeam.text)} {(coinToss ? "wins the coin toss" : "drafts first")}";
        resultUntil = Time.unscaledTime + 2f;
        stage = Stage.Result;
        footer.text = homeScore == awayScore ? "New rolls in 2 seconds"
            : coinToss ? "Choices open in 2 seconds" : "Draft begins in 2 seconds";
    }

    public void AdvanceResult(float unscaledTime)
    {
        if (stage != Stage.Result || unscaledTime < resultUntil) return;
        if (homeScore == awayScore) { BeginRound(); return; }
        HomeWon = homeScore > awayScore;
        if (coinToss) { stage = Stage.Choice; return; }
        Close();
    }

    public void Close()
    {
        stage = Stage.Closed;
        visuals.SetActive(false);
    }

    private void Update()
    {
        AdvanceResult(Time.unscaledTime);
        if (!IsOpen) return;
        Fit();
        if (externalInput) return;
        if (Input.GetKeyDown(KeyCode.R)) HandleKey(new KeyPressData(KeyCode.R, false, false, false));
        if (!(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            || !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) return;
        for (int i = 1; i <= 6; i++)
        {
            var number = (KeyCode)((int)KeyCode.Alpha0 + i);
            var keypad = (KeyCode)((int)KeyCode.Keypad0 + i);
            if (Input.GetKeyDown(number)) HandleKey(new KeyPressData(number, true, true, false));
            if (Input.GetKeyDown(keypad)) HandleKey(new KeyPressData(keypad, true, true, false));
        }
    }

    private void Fit()
    {
        if (panel == null || panel.parent is not RectTransform bounds) return;
        panel.localScale = Vector3.one * Mathf.Max(.001f,
            Mathf.Min(1f, bounds.rect.width * .65f / panel.sizeDelta.x, bounds.rect.height * .9f / panel.sizeDelta.y));
    }
}
