using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>Independent scene-authored stoppage-actions presentation. MatchManager owns the rule.</summary>
public sealed class StoppageActionsPanelController : MonoBehaviour
{
    public MatchManager match;
    public GameObject visuals;
    public RectTransform panel;
    public TMP_Text title;
    public TMP_Text situation;
    public TMP_Text rollingTeam;
    public TMP_Text rule;
    public TMP_Text result;
    public TMP_Text footer;
    public DuelDieView die;
    [Min(1f)] public float resultSeconds = 5f;
    [Min(1f)] public float preferredWidth = 1040f;
    [Min(1f)] public float preferredHeight = 740f;
    [Range(0f, 1f)] public float initialTopInsetFraction = .2f;
    public static StoppageActionsPanelController Instance { get; private set; }
    private enum Stage { Closed, AwaitingRoll, Complete }
    private Stage stage;
    private float closeAt;
    private float heldSecondsRemaining;
    private bool rememberedPosition;
    private Vector2 draggedPosition;
    private string matchKey;
    private static bool waitForEscapeRelease;
    private static int reservedEscapeFrame = -1;
    public bool IsOpen => stage != Stage.Closed;
    public bool IsAwaitingRoll => stage == Stage.AwaitingRoll;
    public bool IsResultHeld { get; private set; }
    public bool HasSceneBindings => visuals != null && panel != null && title != null && situation != null
        && rollingTeam != null && rule != null && result != null && footer != null && die != null;
    public RectTransform Panel => panel;
    public static bool IsVisible => Instance != null && Instance.IsOpen;
    public static bool IsHoldingContinuation => IsVisible && !Instance.IsAwaitingRoll;
    public static bool IsMatchClockHeld => IsVisible && Instance.IsResultHeld;

    private void Awake()
    {
        Instance = this;
        stage = Stage.Closed;
        if (visuals != null) visuals.SetActive(false);
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }
    private void OnDisable()
    {
        stage = Stage.Closed;
        IsResultHeld = false;
        if (visuals != null) visuals.SetActive(false);
    }

    public static string SituationCopy(string periodLabel) => $"{periodLabel} — scheduled time is over";

    public bool Begin(MatchManager manager)
    {
        // Wait for any originating duel/scatter presentation to finish; never replace its contents.
        if (!HasSceneBindings || IsOpen || manager == null || !manager.isActiveAndEnabled
            || manager.gameData?.gameSettings == null || !manager.IsWaitingForExtraActionsRoll
            || DuelResolutionPanelController.IsVisible) return false;
        match = manager;
        string currentMatch = manager.GetEntityId().ToString() + ":" + manager.gameData?.playSessionId;
        if (matchKey != currentMatch) { matchKey = currentMatch; rememberedPosition = false; }
        stage = Stage.AwaitingRoll;
        IsResultHeld = false;
        title.text = "STOPPAGE ACTIONS";
        situation.text = SituationCopy(manager.GetCurrentPeriodLabel());
        bool home = manager.teamInAttack == MatchManager.TeamInAttack.Home;
        var settings = manager.gameData.gameSettings;
        rollingTeam.text = $"Attacking team: {(home ? settings.homeTeamName : settings.awayTeamName)}";
        var colors = TokenKitCatalog.GetInstructionPalette(TokenKitCatalog.ResolveStyle(home ? settings.homeKit : settings.awayKit));
        rollingTeam.color = Color.Lerp(colors.Primary, Color.white, .4f);
        die.SetTeamColors(colors.Primary, colors.Secondary);
        die.Begin(false);
        rule.text = $"Referee leniency {manager.refereeLeniency} + D6\nMaximum: 7 stoppage actions";
        result.text = "";
        footer.text = "Press [R] to roll";
        visuals.SetActive(true);
        visuals.transform.SetAsLastSibling();
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        if (rememberedPosition) panel.anchoredPosition = draggedPosition;
        else if (panel.parent is RectTransform bounds)
            panel.anchoredPosition = new Vector2(0f, bounds.rect.height * (.5f - initialTopInsetFraction));
        FitAndClamp();
        return true;
    }

    public IEnumerator ShowResultAndWait(MatchManager manager, GameplayDiceRollResult roll, int leniency, int total)
    {
        if (match != manager || !IsAwaitingRoll) return Empty();
        die.Resolve(roll.roll, false);
        int sum = leniency + roll.roll;
        string count = total == 1 ? "stoppage action" : "stoppage actions";
        result.text = $"Referee leniency {leniency} + roll {roll.roll} = {sum}\n{total} {count}"
            + (sum > total ? " (maximum 7)" : "");
        stage = Stage.Complete;
        IsResultHeld = false;
        closeAt = Time.unscaledTime + resultSeconds;
        UpdateFooter();
        return WaitUntilClosed();
    }

    private IEnumerator WaitUntilClosed() { while (IsOpen) yield return null; }
    private static IEnumerator Empty() { yield break; }

    public void Cancel(MatchManager manager)
    {
        if (match == manager && IsOpen) Close();
    }

    public bool Dismiss()
    {
        if (stage != Stage.Complete) return false;
        Close();
        return true;
    }

    private void Close()
    {
        waitForEscapeRelease |= Input.GetKey(KeyCode.Escape);
        reservedEscapeFrame = Time.frameCount;
        stage = Stage.Closed;
        IsResultHeld = false;
        if (visuals != null) visuals.SetActive(false);
    }

    public static bool ReservePauseEscape()
    {
        if (IsVisible) return true;
        if (waitForEscapeRelease)
        {
            if (!Input.GetKey(KeyCode.Escape))
            {
                waitForEscapeRelease = false;
                reservedEscapeFrame = Time.frameCount;
            }
            return true;
        }
        return Time.frameCount == reservedEscapeFrame;
    }

    public static bool ConsumeKey(KeyPressData key)
    {
        if (key.key == KeyCode.Escape && ReservePauseEscape())
        {
            if (IsVisible) Instance.Dismiss();
            key.Consume(nameof(StoppageActionsPanelController));
            return true;
        }
        if (!IsVisible) return false;
        if (key.key is KeyCode.Return or KeyCode.KeypadEnter)
        {
            Instance.ToggleResultHold();
            key.Consume(nameof(StoppageActionsPanelController));
            return true;
        }
        bool roll = key.key == KeyCode.R || RollInputOverride.TryParse(key, out _);
        if (roll && Instance.IsAwaitingRoll && Instance.match != null && Instance.match.IsWaitingForExtraActionsRoll)
            return false;
        key.Consume(nameof(StoppageActionsPanelController));
        return true;
    }

    private void Update()
    {
        if (!IsOpen)
        {
            // Also restores a pending roll loaded from a saved clock snapshot.
            if (match != null && match.IsWaitingForExtraActionsRoll) Begin(match);
            return;
        }
        if (match == null || !match.isActiveAndEnabled || (IsAwaitingRoll && !match.IsWaitingForExtraActionsRoll))
        {
            Close();
            return;
        }
        FitAndClamp();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            waitForEscapeRelease = true;
            if (Dismiss()) return;
        }
        if (stage != Stage.Complete) return;
        UpdateFooter();
        if (!IsResultHeld && Time.unscaledTime >= closeAt) Dismiss();
    }

    public void ToggleResultHold()
    {
        if (stage != Stage.Complete) return;
        if (IsResultHeld) { closeAt = Time.unscaledTime + heldSecondsRemaining; IsResultHeld = false; }
        else { heldSecondsRemaining = Mathf.Max(0f, closeAt - Time.unscaledTime); IsResultHeld = true; }
        UpdateFooter();
    }

    private void UpdateFooter()
        => footer.text = IsResultHeld ? "Result held — match clock stopped. [Enter] resume · [Esc] close"
            : $"Closes automatically in {Mathf.Max(0, Mathf.CeilToInt(closeAt - Time.unscaledTime))} seconds — [Enter] hold · [Esc] close now";

    public void MovePanel(Vector2 position)
    {
        panel.anchoredPosition = position;
        rememberedPosition = true;
        FitAndClamp();
        draggedPosition = panel.anchoredPosition;
    }

    public void FitAndClamp()
    {
        if (panel == null || panel.parent is not RectTransform bounds) return;
        float width = Mathf.Max(1f, preferredWidth), height = Mathf.Max(1f, preferredHeight);
        panel.sizeDelta = new Vector2(width, height);
        float scale = Mathf.Max(.001f, Mathf.Min(1f, bounds.rect.width * .5f / width, bounds.rect.height * .96f / height));
        panel.localScale = Vector3.one * scale;
        float xLimit = Mathf.Max(0f, (bounds.rect.width - width * scale) * .5f);
        float screenTop = bounds.rect.height * .5f;
        Vector2 position = panel.anchoredPosition;
        panel.anchoredPosition = new Vector2(Mathf.Clamp(position.x, -xLimit, xLimit),
            Mathf.Clamp(position.y, -screenTop + height * scale, screenTop));
    }
}
