using System;
using System.Collections;
using TMPro;
using UnityEngine;

[Serializable]
public sealed class DuelPlayerPanelFields
{
    public TMP_Text playerName;
    public TMP_Text teamName;
    public TMP_Text role;
    public TMP_Text attribute;
    public TMP_Text modifier;
    public TMP_Text total;
    public TMP_Text guidance;
    public DuelDieView die;
}

[Serializable]
public sealed class DuelCheckPanelFields
{
    public RectTransform column;
    public TMP_Text rule;
    public TMP_Text result;
    public DuelDieView die;
}

/// <summary>Scene-authored tackle/nutmeg UI. Gameplay owns results and continuation.</summary>
public sealed class DuelResolutionPanelController : MonoBehaviour
{
    public GameObject visuals;
    public RectTransform panel;
    public TMP_Text title;
    public TMP_Text outcome;
    public TMP_Text footer;
    public GameObject foulChecks;
    public GameObject singleRollContent;
    public GameObject playerColumns;
    public DuelPlayerPanelFields singlePlayer = new();
    private MonoBehaviour singleOwner;
    private Func<bool> singleWaiting;
    private bool singleMode;
    public DuelPlayerPanelFields leftPlayer = new();
    public DuelPlayerPanelFields rightPlayer = new();
    public DuelCheckPanelFields leniency = new();
    public DuelCheckPanelFields injury = new();
    [Min(1f)] public float resultSeconds = 5f;
    [Min(1f)] public float preferredWidth = 1680f;
    [Min(1f)] public float singleRollWidth = 1040f;
    [Min(1f)] public float normalHeight = 820f;
    [Min(1f)] public float expandedHeight = 1220f;
    [Tooltip("Initial top edge, measured down from the screen top as a fraction of its height.")]
    [Range(0f, 1f)] public float initialTopInsetFraction = .2f;
    public static DuelResolutionPanelController Instance { get; private set; }
    private enum Stage { Closed, Defender, Dribbler, Transition, Leniency, Injury, Complete, Single }
    private Stage stage;
    private MovementPhaseManager owner;
    private DuelPlayerPanelFields dribblerView;
    private DuelPlayerPanelFields defenderView;
    private int dribbling;
    private int tackling;
    private int defenderBonus;
    private int foulThreshold;
    private float closeAt;
    private bool expanded;
    private bool rememberedPosition;
    private Vector2 lastDraggedPosition;
    private string matchKey;
    private Color attackerColor;
    private Color attackerSecondaryColor;
    private bool foulIsPenalty;
    private float heldSecondsRemaining;
    public bool IsResultHeld { get; private set; }
    private static bool waitForEscapeRelease;
    private static int reservedEscapeFrame = -1;
    public bool IsOpen => stage != Stage.Closed;
    public bool IsComplete => stage == Stage.Complete;
    public bool DribblerOnLeft => dribblerView == leftPlayer;
    public bool HasSceneBindings => visuals != null && panel != null && title != null && outcome != null
        && footer != null && foulChecks != null && PlayerBound(leftPlayer) && PlayerBound(rightPlayer)
        && leniency?.column != null && leniency.rule != null && leniency.result != null && leniency.die != null
        && injury?.column != null && injury.rule != null && injury.result != null && injury.die != null;
    public RectTransform Panel => panel;
    public static bool IsVisible => Instance != null && Instance.IsOpen;
    public static bool IsHoldingContinuation => IsVisible && !Instance.IsAwaitingRoll;
    public static bool IsMatchClockHeld => IsVisible && Instance.IsResultHeld;
    public bool IsAwaitingRoll => stage is Stage.Defender or Stage.Dribbler or Stage.Leniency or Stage.Injury or Stage.Single;

    private void Awake()
    {
        Instance = this;
        stage = Stage.Closed;
        if (visuals != null) visuals.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnDisable()
    {
        IsResultHeld = false;
        stage = Stage.Closed;
        owner = null;
        if (visuals != null) visuals.SetActive(false);
    }

    public bool Begin(MovementPhaseManager manager, PlayerToken attacker, PlayerToken defender,
        bool nutmeg, int effectiveTackling, int bonus, int threshold,
        ExpectedStatsCalculator.GroundDuelExpectation expectation, bool penaltyArea)
    {
        if (!HasSceneBindings || attacker == null || defender == null) return false;
        singleMode = false;
        singleOwner = null;
        singleWaiting = null;
        if (singleRollContent != null) singleRollContent.SetActive(false);
        if (playerColumns != null) playerColumns.SetActive(true);
        owner = manager;
        var match = MatchManager.Instance;
        // A different match starts at the authored top inset; nothing is written to player prefs.
        string currentMatch = match != null ? match.GetEntityId().ToString() + ":" + match.gameData?.playSessionId : "";
        if (matchKey != currentMatch) { matchKey = currentMatch; rememberedPosition = false; }
        bool leftToRight = (attacker.isHomeTeam ? match.homeTeamDirection : match.awayTeamDirection)
            == MatchManager.TeamAttackingDirection.LeftToRight;
        dribblerView = leftToRight ? leftPlayer : rightPlayer;
        defenderView = leftToRight ? rightPlayer : leftPlayer;
        dribbling = attacker.dribbling;
        tackling = effectiveTackling;
        defenderBonus = bonus;
        foulThreshold = threshold;
        foulIsPenalty = penaltyArea;
        IsResultHeld = false;
        expanded = false;
        foulChecks.SetActive(false);
        attackerColor = TeamColor(attacker);
        attackerSecondaryColor = TeamPalette(attacker).Secondary;
        ConfigurePlayer(dribblerView, attacker, "Dribbling", dribbling, 0, "xDr", expectation.xDribbles);
        ConfigurePlayer(defenderView, defender, "Tackling", tackling, bonus, "xTk", expectation.xTackles);
        ArrangeFoulChecks(leftToRight);
        title.text = nutmeg ? "NUTMEG DUEL" : "TACKLE DUEL";
        outcome.text = "";
        string foulResult = foulIsPenalty ? "penalty" : "foul";
        defenderView.guidance.text = threshold == 1 ? $"Roll 1 = {foulResult}" : $"Roll 1–{threshold} = {foulResult}";
        dribblerView.guidance.text = "Waiting for tackler's result";
        leniency.die.Clear();
        injury.die.Clear();
        leniency.result.text = injury.result.text = "Pending";
        visuals.SetActive(true);
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        visuals.transform.SetAsLastSibling();
        if (rememberedPosition) panel.anchoredPosition = lastDraggedPosition;
        else ResetPanelPosition();
        stage = Stage.Defender;
        defenderView.die.Begin(true);
        footer.text = "Press [R] — tackler rolls";
        FitAndClamp();
        return true;
    }

    // Single rolls share the authored overlay and its exclusive input/countdown/drag lifecycle.
    // Managers supply the rule and continuation; this view never rolls dice or resolves gameplay.
    public bool BeginSingle(MonoBehaviour manager, Func<bool> waiting, string situation,
        PlayerToken token, string attributeText, string probabilityText, bool jackpotAllowed, string successGuidance = null)
    {
        if (!HasSceneBindings || singleRollContent == null || playerColumns == null || !PlayerBound(singlePlayer)) return false;
        if (IsOpen) return false;
        singleMode = true;
        singleOwner = manager;
        singleWaiting = waiting;
        owner = null;
        stage = Stage.Single;
        IsResultHeld = false;
        expanded = false;
        playerColumns.SetActive(false);
        foulChecks.SetActive(false);
        singleRollContent.SetActive(true);
        title.text = situation;
        outcome.text = "";
        singlePlayer.playerName.text = token != null ? $"#{token.jerseyNumber}  {token.playerName}" : "";
        singlePlayer.teamName.text = token == null ? "" : token.isHomeTeam
            ? MatchManager.Instance.gameData.gameSettings.homeTeamName : MatchManager.Instance.gameData.gameSettings.awayTeamName;
        singlePlayer.attribute.text = attributeText ?? "";
        singlePlayer.modifier.text = probabilityText ?? "";
        singlePlayer.total.text = "";
        singlePlayer.guidance.text = successGuidance ?? "";
        Color color = token != null ? TeamColor(token) : new Color(.13f, .35f, .63f);
        singlePlayer.playerName.color = Color.Lerp(color, Color.white, .4f);
        singlePlayer.die.SetTeamColors(color, token != null ? TeamPalette(token).Secondary : Color.white);
        singlePlayer.die.Begin(jackpotAllowed);
        footer.text = "Press [R] to roll";
        visuals.SetActive(true);
        visuals.transform.SetAsLastSibling();
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        var match = MatchManager.Instance;
        string currentMatch = match != null ? match.GetEntityId().ToString() + ":" + match.gameData?.playSessionId : "";
        if (matchKey != currentMatch) { matchKey = currentMatch; rememberedPosition = false; }
        if (rememberedPosition) panel.anchoredPosition = lastDraggedPosition;
        else ResetPanelPosition();
        FitAndClamp();
        return true;
    }

    public IEnumerator ResolveSingle(MonoBehaviour manager, GameplayDiceRollResult roll, string result, bool continueWithFoulChecks = false)
    {
        if (!singleMode || singleOwner != manager || stage != Stage.Single) return EmptyPresentation();
        stage = Stage.Transition;
        singlePlayer.die.Resolve(roll.roll, roll.jackpotEnabled && roll.isJackpot);
        singlePlayer.total.text = roll.isJackpot && roll.jackpotEnabled ? "JACKPOT ★ (6)" : $"Roll {roll.roll}";
        outcome.text = result;
        // A running dribbler's recovery foul continues directly into the existing checks.
        // Keep the resolved recovery die visible; only the final injury result starts dismissal.
        return continueWithFoulChecks ? EmptyPresentation() : WaitForDismissal();
    }

    public bool ContinueRecoveryFoul(MovementPhaseManager manager, PlayerToken attacker,
        PlayerToken defender, bool penalty)
    {
        if (!singleMode || singleOwner != manager || stage != Stage.Transition
            || attacker == null || defender == null) return false;
        // Hand input and the existing foul-result callbacks to the movement manager.
        owner = manager;
        singleWaiting = null;
        attackerColor = TeamColor(attacker);
        attackerSecondaryColor = TeamPalette(attacker).Secondary;
        foulIsPenalty = penalty;
        bool dribblerOnLeft = (attacker.isHomeTeam ? MatchManager.Instance.homeTeamDirection
            : MatchManager.Instance.awayTeamDirection) == MatchManager.TeamAttackingDirection.LeftToRight;
        ArrangeFoulChecks(dribblerOnLeft);
        leniency.die.Clear();
        injury.die.Clear();
        leniency.result.text = injury.result.text = "Pending";
        singlePlayer.guidance.text = (penalty ? "Penalty awarded to: " : "Fouled: ")
            + $"#{attacker.jerseyNumber}  {attacker.playerName}";
        outcome.text = penalty ? "PENALTY!" : "FOUL";
        return true;
    }

    private static IEnumerator EmptyPresentation() { yield break; }

    private static Color TeamColor(PlayerToken token) => TeamPalette(token).Primary;

    private static TokenKitInstructionPalette TeamPalette(PlayerToken token)
    {
        var settings = MatchManager.Instance.gameData.gameSettings;
        string kit = token.isHomeTeam ? settings.homeKit : settings.awayKit;
        return TokenKitCatalog.GetInstructionPalette(TokenKitCatalog.ResolveStyle(kit));
    }

    private static void ConfigurePlayer(DuelPlayerPanelFields view, PlayerToken token,
        string attribute, int skill, int bonus, string expectedLabel, float expectedValue)
    {
        var settings = MatchManager.Instance.gameData.gameSettings;
        view.playerName.text = $"#{token.jerseyNumber}  {token.playerName}";
        view.teamName.text = token.isHomeTeam ? settings.homeTeamName : settings.awayTeamName;
        if (view.role != null) view.role.text = "";
        view.attribute.text = $"{attribute} {skill} ({expectedLabel} {expectedValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)})";
        view.modifier.text = bonus != 0 ? $"Nutmeg Bonus  +{bonus}" : "";
        view.total.text = "Total  —";
        Color color = TeamColor(token);
        view.playerName.color = Color.Lerp(color, Color.white, .4f);
        view.die.SetTeamColors(color, TeamPalette(token).Secondary);
        view.die.Clear();
    }

    private static bool PlayerBound(DuelPlayerPanelFields view)
        => view?.playerName != null && view.teamName != null && view.attribute != null
            && view.modifier != null && view.total != null && view.guidance != null && view.die != null;

    public void ShowDuelRoll(bool defender, GameplayDiceRollResult roll)
    {
        if (!IsOpen) return;
        var view = defender ? defenderView : dribblerView;
        view.die.Resolve(roll.roll, roll.isJackpot);
        int skill = defender ? tackling : dribbling;
        int bonus = defender ? defenderBonus : 0;
        view.total.text = DuelRollGuidance.RollTotal(roll.roll, skill, bonus, roll.isJackpot);
        if (defender)
        {
            stage = Stage.Dribbler;
            bool foul = roll.roll <= foulThreshold && !roll.isJackpot;
            defenderView.guidance.text = foul ? foulIsPenalty ? "Penalty roll" : "Foul roll" : "Rolled";
            int defenderTotal = roll.isJackpot ? 50 : tackling + defenderBonus + roll.roll;
            outcome.text = foul ? foulIsPenalty ? "PENALTY!" : "FOUL — tackler committed a foul"
                : DuelRollGuidance.LiveOutcomes(ExpectedStatsCalculator.CalculateGroundDuelAfterDefenderRoll(dribbling, defenderTotal, false));
            dribblerView.guidance.text = DuelRollGuidance.ForDribbler(dribbling,
                defenderTotal, roll.isJackpot, foul, foulIsPenalty);
            dribblerView.die.Begin(true);
            footer.text = "Press [R] — dribbler rolls";
        }
        else
        {
            stage = Stage.Transition;
            dribblerView.guidance.text = "Rolled";
            footer.text = "Resolving duel…";
        }
    }

    public void ShowOutcome(string text)
    {
        if (IsOpen) outcome.text = text;
    }

    public void BeginLeniency(int threshold, bool harsh, bool alreadyBooked, int resilience, bool alreadyInjured)
    {
        if (!IsOpen) return;
        SetExpandedLayout(true);
        string yellow = alreadyBooked ? "second yellow — red card" : "yellow card";
        leniency.rule.text = DuelRollGuidance.ThresholdCheck("Referee leniency", threshold,
            harsh ? "straight red" : yellow, harsh ? yellow : "no card");
        leniency.result.text = "Awaiting roll";
        injury.rule.text = InjuryRule(resilience, alreadyInjured);
        var defenderDie = singleMode ? singlePlayer.die : defenderView.die;
        leniency.die.SetTeamColors(defenderDie.background.color,
            defenderDie.border != null ? defenderDie.border.effectColor : Color.white);
        injury.die.SetTeamColors(attackerColor, attackerSecondaryColor);
        leniency.die.Begin(false);
        stage = Stage.Leniency;
        footer.text = "Press [R] — leniency check";
        FitAndClamp();
    }

    public void ShowLeniency(GameplayDiceRollResult roll, string result)
    {
        if (!IsOpen) return;
        leniency.die.Resolve(roll.roll, false);
        leniency.result.text = result;
        stage = Stage.Transition;
    }

    private static string InjuryRule(int resilience, bool alreadyInjured)
        => DuelRollGuidance.ThresholdCheck("Resilience", resilience,
            alreadyInjured ? "second injury — cannot continue" : "injury", "avoids injury");

    public void ArrangeFoulChecks(bool dribblerOnLeft)
    {
        PlaceCheckColumn(leniency.column, !dribblerOnLeft);
        PlaceCheckColumn(injury.column, dribblerOnLeft);
    }

    private static void PlaceCheckColumn(RectTransform column, bool left)
    {
        if (column == null) return;
        column.anchorMin = new Vector2(left ? 0f : .5f, column.anchorMin.y);
        column.anchorMax = new Vector2(left ? .5f : 1f, column.anchorMax.y);
    }

    public void BeginInjury(int resilience, bool alreadyInjured)
    {
        if (!IsOpen) return;
        injury.rule.text = InjuryRule(resilience, alreadyInjured);
        injury.result.text = "Awaiting roll";
        injury.die.Begin(false);
        stage = Stage.Injury;
        footer.text = "Press [R] — injury check";
    }

    public void ShowInjury(GameplayDiceRollResult roll, string result)
    {
        if (!IsOpen) return;
        injury.die.Resolve(roll.roll, false);
        injury.result.text = result;
        stage = Stage.Transition;
    }

    public IEnumerator WaitForDismissal()
    {
        if (!IsOpen) yield break;
        stage = Stage.Complete;
        IsResultHeld = false;
        closeAt = Time.unscaledTime + resultSeconds;
        UpdateFooter();
        while (IsOpen) yield return null;
    }

    public bool Dismiss()
    {
        if (!IsComplete) return false;
        ReserveEscape();
        IsResultHeld = false;
        stage = Stage.Closed;
        owner = null;
        singleOwner = null;
        singleWaiting = null;
        visuals.SetActive(false);
        return true;
    }

    public void Cancel(MovementPhaseManager manager)
    {
        if (owner != manager && singleOwner != manager) return;
        ReserveEscape();
        IsResultHeld = false;
        stage = Stage.Closed;
        owner = null;
        if (visuals != null) visuals.SetActive(false);
    }

    private static void ReserveEscape()
    {
        waitForEscapeRelease |= Input.GetKey(KeyCode.Escape);
        reservedEscapeFrame = Time.frameCount;
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
        // Update may already have dismissed the panel on this physical key-down.
        // Keep that same press exclusive to the panel regardless of script order.
        if (key.key == KeyCode.Escape && ReservePauseEscape())
        {
            if (IsVisible && Instance.IsComplete) Instance.Dismiss();
            key.Consume(nameof(DuelResolutionPanelController));
            return true;
        }
        if (!IsVisible) return false;
        var current = Instance;
        if (key.key is KeyCode.Return or KeyCode.KeypadEnter)
        {
            current.ToggleResultHold();
            key.Consume(nameof(DuelResolutionPanelController));
            return true;
        }
        bool roll = key.key == KeyCode.R || RollInputOverride.TryParse(key, out _);
        bool expected = current.owner != null && (current.stage switch
        {
            Stage.Defender or Stage.Dribbler => current.owner.isWaitingForTackleRoll,
            Stage.Leniency => current.owner.isWaitingForYellowCardRoll,
            Stage.Injury => current.owner.isWaitingForInjuryRoll,
            _ => false
        });
        if (current.singleMode && current.stage == Stage.Single) expected = current.singleOwner != null
            && current.singleWaiting != null && current.singleWaiting();
        if (roll && expected) return false;
        key.Consume(nameof(DuelResolutionPanelController));
        return true;
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (singleMode && stage == Stage.Single && (singleOwner == null || !singleOwner.isActiveAndEnabled
            || singleWaiting == null || !singleWaiting()))
        {
            stage = Stage.Complete;
            Dismiss();
            return;
        }
        FitAndClamp();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            waitForEscapeRelease = true;
            if (IsComplete) { Dismiss(); return; }
        }
        if (IsComplete)
        {
            UpdateFooter();
            if (!IsResultHeld && Time.unscaledTime >= closeAt) Dismiss();
        }
    }

    public void ToggleResultHold()
    {
        if (!IsComplete) return;
        if (IsResultHeld)
        {
            closeAt = Time.unscaledTime + heldSecondsRemaining;
            IsResultHeld = false;
        }
        else
        {
            heldSecondsRemaining = Mathf.Max(0f, closeAt - Time.unscaledTime);
            IsResultHeld = true;
        }
        UpdateFooter();
    }

    private void UpdateFooter()
    {
        footer.text = IsResultHeld ? "Result held — match clock stopped. [Enter] resume · [Esc] close"
            : $"Closes automatically in {Mathf.Max(0, Mathf.CeilToInt(closeAt - Time.unscaledTime))} seconds — [Enter] hold · [Esc] close now";
    }

    public void MovePanel(Vector2 position)
    {
        panel.anchoredPosition = position;
        rememberedPosition = true;
        FitAndClamp();
        lastDraggedPosition = panel.anchoredPosition;
    }

    /// <summary>Shared by gameplay and edit-mode previews; the top edge stays fixed unless clamping is needed.</summary>
    public void SetExpandedLayout(bool showChecks)
    {
        expanded = showChecks;
        if (foulChecks != null) foulChecks.SetActive(showChecks);
        FitAndClamp();
    }

    public void ResetPanelPosition()
    {
        if (panel == null || panel.parent is not RectTransform bounds) return;
        panel.anchoredPosition = new Vector2(0f, bounds.rect.height * (.5f - initialTopInsetFraction));
        FitAndClamp();
    }

    public void FitAndClamp()
    {
        if (panel == null || panel.parent is not RectTransform bounds) return;
        bool singleLayout = singleRollContent != null && singleRollContent.activeSelf;
        float width = Mathf.Max(1f, singleLayout && !expanded ? singleRollWidth : preferredWidth);
        float maximumHeight = Mathf.Max(1f, normalHeight, expandedHeight);
        float height = expanded ? maximumHeight : Mathf.Max(1f, normalHeight);
        panel.sizeDelta = new Vector2(width, height);
        // Reserve the same scale for both states so expansion cannot move or resize existing content.
        float scale = Mathf.Max(.001f, Mathf.Min(1f, bounds.rect.width * .5f / Mathf.Max(1f, preferredWidth),
            bounds.rect.height * .96f / maximumHeight));
        panel.localScale = Vector3.one * scale;
        float horizontalLimit = Mathf.Max(0f, (bounds.rect.width - width * scale) * .5f);
        float screenTop = bounds.rect.height * .5f;
        float lowestTop = -screenTop + height * scale;
        Vector2 position = panel.anchoredPosition;
        // The scene-authored pivot is top-centre. Raising this lower limit corrects only bottom overflow.
        panel.anchoredPosition = new Vector2(Mathf.Clamp(position.x, -horizontalLimit, horizontalLimit),
            Mathf.Clamp(position.y, lowestTop, screenTop));
    }
}
