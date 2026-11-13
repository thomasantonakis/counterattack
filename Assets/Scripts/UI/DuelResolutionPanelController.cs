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
    public UnityEngine.UI.Image modifierBackground;
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
    public GameObject looseBallContent;
    public TMP_Text looseBallPlayer;
    public DuelCheckPanelFields looseDirection = new();
    public DuelCheckPanelFields looseDistance = new();
    public GameObject scatterContinuationContent;
    public TMP_Text scatterCaption;
    public TMP_Text scatterOutcome;
    public DuelCheckPanelFields continuationDirection = new();
    public DuelCheckPanelFields continuationDistance = new();
    private DuelCheckPanelFields activeDirection;
    private DuelCheckPanelFields activeDistance;
    private MonoBehaviour pendingScatterOwner;
    private bool scatterAttached;
    public GameObject looseRecoveryContent;
    public DuelPlayerPanelFields looseRecoveryPlayer = new();
    public float recoveryStandaloneTop = 720f;
    public float recoveryAttachedTop = 1168f;
    public float recoveryContentHeight = 400f;
    private MonoBehaviour scatterHistoryOwner;
    private bool looseRecoveryMode;
    private bool looseBallMode;
    private bool looseBallSuspended;
    private bool looseDirectionPending;
    public DuelPlayerPanelFields singlePlayer = new();
    public GameObject singleAttributeTile;
    public GameObject singleProbabilityTile;
    private MonoBehaviour singleOwner;
    private Func<bool> singleWaiting;
    private bool singleMode;
    private KeyCode[] singleChoiceKeys;
    private DuelPlayerPanelFields singleRollView;
    private bool shotComparison;
    private PlayerToken shotComparisonKeeper;
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
    private GroundDuelPresentationKind duelKind;
    private int foulThreshold;
    private float closeAt;
    private bool expanded;
    private bool rememberedPosition;
    private Vector2 lastDraggedPosition;
    private string matchKey;
    private bool resultAllowsContinuation;
    private Color attackerColor;
    private Color attackerSecondaryColor;
    private bool foulIsPenalty;
    private float heldSecondsRemaining;
    private bool onlineAuthorityControlled, onlineAuthorityHeld;
    private float onlineSecondsRemaining;

    // Called by the match's authority pause lane, never by a remote UI message.
    public void SetOnlineAuthorityControl(bool controlled, bool held)
    {
        bool nextHeld = controlled && held;
        if (nextHeld && !onlineAuthorityHeld && IsComplete)
            onlineSecondsRemaining = IsResultHeld ? heldSecondsRemaining : Mathf.Max(0f, closeAt - Time.unscaledTime);
        if (!nextHeld && onlineAuthorityHeld && IsComplete)
        {
            if (IsResultHeld) heldSecondsRemaining = onlineSecondsRemaining;
            else closeAt = Time.unscaledTime + onlineSecondsRemaining;
        }
        onlineAuthorityControlled = controlled;
        onlineAuthorityHeld = nextHeld;
    }
    public bool IsResultHeld { get; private set; }
    private static bool waitForEscapeRelease;
    private static int reservedEscapeFrame = -1;
    public bool IsOpen => stage != Stage.Closed && !looseBallSuspended;
    public bool IsComplete => stage == Stage.Complete;
    public bool DribblerOnLeft => dribblerView == leftPlayer;
    public bool HasSceneBindings => visuals != null && panel != null && title != null && outcome != null
        && footer != null && foulChecks != null && PlayerBound(leftPlayer) && PlayerBound(rightPlayer)
        && leniency?.column != null && leniency.rule != null && leniency.result != null && leniency.die != null
        && injury?.column != null && injury.rule != null && injury.result != null && injury.die != null;
    public RectTransform Panel => panel;
    public static bool IsVisible => Instance != null && Instance.IsOpen;
    public static bool IsHoldingContinuation => IsVisible && !Instance.IsAwaitingRoll && !Instance.resultAllowsContinuation;
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
        ExpectedStatsCalculator.GroundDuelExpectation expectation, bool penaltyArea,
        string defenderAttribute = "Tackling", GroundDuelPresentationKind kind = GroundDuelPresentationKind.Tackle)
    {
        if (!HasSceneBindings || attacker == null || defender == null) return false;
        singleMode = false;
        singleChoiceKeys = null;
        looseBallSuspended = false;
        if (scatterOutcome != null) scatterOutcome.gameObject.SetActive(false);
        looseBallMode = false;
        scatterHistoryOwner = null;
        looseRecoveryMode = false;
        if (looseRecoveryContent != null) looseRecoveryContent.SetActive(false);
        pendingScatterOwner = null;
        scatterAttached = false;
        if (scatterContinuationContent != null) scatterContinuationContent.SetActive(false);
        if (looseBallContent != null) looseBallContent.SetActive(false);
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
        duelKind = kind;
        foulThreshold = threshold;
        foulIsPenalty = penaltyArea;
        IsResultHeld = false;
        expanded = false;
        foulChecks.SetActive(false);
        attackerColor = TeamColor(attacker);
        attackerSecondaryColor = TeamPalette(attacker).Secondary;
        ConfigurePlayer(dribblerView, attacker, "Dribbling", dribbling, 0, "xDr", expectation.xDribbles);
        bool dive = kind is GroundDuelPresentationKind.GoalkeeperDive or GroundDuelPresentationKind.OutsideBoxHands;
        ConfigurePlayer(defenderView, defender, defenderAttribute, tackling, bonus,
            dive ? "Duel win" : "xTk", dive ? expectation.defenderWinProbability : expectation.xTackles);
        if (dive)
        {
            // A dive tie has its own gameplay branch; do not apply the outfield xDr/xTk tie weighting.
            dribblerView.attribute.text = $"Dribbling {dribbling} (Duel win {expectation.attackerWinProbability:0.00})";
            defenderView.modifier.text = bonus == 0 ? "" : $"Saving modifier  {bonus:+0;-0}";
        }
        if (defenderView.role != null)
        {
            defenderView.role.text = kind switch
            {
                GroundDuelPresentationKind.OutsideBoxHands => "Hands outside box — win/tie: straight red",
                GroundDuelPresentationKind.OutsideBoxFeet => "Feet outside box — Tackling 0",
                GroundDuelPresentationKind.GoalkeeperDive => "Dive — Saving vs Dribbling",
                _ => defenderAttribute == "Saving" ? "Goalkeeper — own penalty area" : ""
            };
            defenderView.role.gameObject.SetActive(defenderView.role.text.Length > 0);
        }
        ArrangeFoulChecks(leftToRight);
        title.text = kind switch
        {
            GroundDuelPresentationKind.OutsideBoxHands => "GOALKEEPER CHALLENGE — HANDS",
            GroundDuelPresentationKind.OutsideBoxFeet => "GOALKEEPER CHALLENGE — FEET",
            GroundDuelPresentationKind.GoalkeeperDive => "GOALKEEPER DIVE",
            _ => nutmeg ? "NUTMEG DUEL" : "TACKLE DUEL"
        };
        outcome.text = "";
        string foulResult = foulIsPenalty ? "penalty" : "foul";
        defenderView.guidance.text = threshold == 1 ? $"Roll 1 = {foulResult}" : $"Roll 1–{threshold} = {foulResult}";
        // Manager-supplied geometry drives the foul range and card policy, for any tackler.
        defenderView.guidance.text += threshold > 1 ? " — from behind; harsh leniency" : " — normal leniency";
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
        if (IsComplete && resultAllowsContinuation && singleOwner == manager) Dismiss();
        if (IsOpen) return false;
        resultAllowsContinuation = false;
        singleMode = true;
        singleChoiceKeys = null;
        singleRollView = singlePlayer;
        looseBallSuspended = false;
        shotComparison = false;
        shotComparisonKeeper = null;
        if (scatterOutcome != null) scatterOutcome.gameObject.SetActive(false);
        looseBallMode = false;
        scatterHistoryOwner = null;
        looseRecoveryMode = false;
        if (looseRecoveryContent != null) looseRecoveryContent.SetActive(false);
        pendingScatterOwner = null;
        scatterAttached = false;
        if (scatterContinuationContent != null) scatterContinuationContent.SetActive(false);
        if (looseBallContent != null) looseBallContent.SetActive(false);
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
        if (singleAttributeTile != null) singleAttributeTile.SetActive(!string.IsNullOrWhiteSpace(attributeText));
        if (singleProbabilityTile != null) singleProbabilityTile.SetActive(!string.IsNullOrWhiteSpace(probabilityText));
        singlePlayer.total.text = "";
        singlePlayer.guidance.text = successGuidance ?? "";
        Color color = token != null ? TeamColor(token) : new Color(.13f, .35f, .63f);
        singlePlayer.playerName.color = PlayerNameColor;
        singlePlayer.die.SetTeamColors(color, token != null ? TeamPalette(token).Secondary : Color.white);
        singlePlayer.die.gameObject.SetActive(true);
        if (singlePlayer.die.transform.parent.name == "RollTile")
            singlePlayer.die.transform.parent.gameObject.SetActive(true);
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

    // Reuses the scene-authored single-player panel for a choice before its roll.
    public bool BeginSingleChoice(MonoBehaviour manager, Func<bool> waiting, string situation,
        PlayerToken token, string guidance, string controls, params KeyCode[] keys)
    {
        if (!BeginSingle(manager, waiting, situation, token, null, null, false, guidance)) return false;
        singleChoiceKeys = keys;
        singlePlayer.die.gameObject.SetActive(false);
        if (singlePlayer.die.transform.parent.name == "RollTile")
            singlePlayer.die.transform.parent.gameObject.SetActive(false);
        footer.text = controls;
        return true;
    }

    // Repeat the authoritative winning header beside the pending Saving check.
    // Both columns already exist in the scene; the attacker never rolls again here.
    public bool BeginHeaderSave(ShotManager manager, Func<bool> waiting, PlayerToken attacker,
        int headerRoll, int headerPower, int headerModifier, PlayerToken goalkeeper, int saveModifier,
        float? expectedGoals = null)
    {
        if (attacker == null || goalkeeper == null) return false;
        if (!BeginSingle(manager, waiting, "Header at Goal · Goalkeeper Save", goalkeeper,
            null, null, true)) return false;
        singleRollContent.SetActive(false);
        playerColumns.SetActive(true);
        singlePlayer.die.Clear();
        var match = MatchManager.Instance;
        bool attackLeft = (attacker.isHomeTeam ? match.homeTeamDirection : match.awayTeamDirection)
            == MatchManager.TeamAttackingDirection.LeftToRight;
        dribblerView = attackLeft ? leftPlayer : rightPlayer;
        defenderView = attackLeft ? rightPlayer : leftPlayer;
        ConfigureHeaderSavePlayer(dribblerView, attacker);
        ConfigureHeaderSavePlayer(defenderView, goalkeeper);
        dribblerView.attribute.text = $"Heading: {attacker.heading}";
        dribblerView.modifier.text = headerModifier == 0 ? "WINNING ATTACKING HEADER"
            : $"WINNING ATTACKING HEADER · Modifier: {headerModifier:+0;-0}";
        if (expectedGoals.HasValue) dribblerView.modifier.text += $" · xGoals {expectedGoals.Value:0.00}";
        bool jackpot = headerPower == 50;
        if (headerRoll > 0) dribblerView.die.Resolve(headerRoll, jackpot);
        else dribblerView.die.Clear();
        dribblerView.total.text = jackpot ? "Roll JACKPOT · Total: JACKPOT"
            : headerRoll > 0 ? $"Roll {headerRoll} · Total: {headerPower}" : $"Header total: {headerPower}";
        dribblerView.guidance.text = "Aerial challenge won · On target";
        defenderView.attribute.text = saveModifier == 0 ? $"Saving {goalkeeper.saving}"
            : $"Saving {goalkeeper.saving} {saveModifier:+0;-0} = {GameplayScore.EffectiveAttribute(goalkeeper.saving, saveModifier)} (Distance Penalty)";
        defenderView.modifier.text = "GOALKEEPER SAVE";
        defenderView.total.text = "";
        defenderView.guidance.text = SingleRollResolutionPresentation.HeaderSaveGuidance(goalkeeper.saving, saveModifier, headerPower);
        defenderView.die.Begin(true);
        singleRollView = defenderView;
        footer.text = "Press [R] — goalkeeper Saving roll";
        FitAndClamp();
        return true;
    }

    public bool IsShotComparisonFor(ShotManager manager)
        => IsOpen && singleOwner == manager && shotComparison;

    public bool BeginShotComparison(ShotManager manager, Func<bool> waiting, string situation,
        PlayerToken shooter, PlayerToken goalkeeper, string shootingAttribute, string savingAttribute,
        float? expectedGoals = null, string shooterGuidance = null, string goalkeeperGuidance = null)
    {
        if (shooter == null || goalkeeper == null) return false;
        if (!BeginSingle(manager, waiting, situation + " · Shot vs Saving", shooter, null, null, true)) return false;
        singleRollContent.SetActive(false);
        playerColumns.SetActive(true);
        singlePlayer.die.Clear();
        var match = MatchManager.Instance;
        bool attackLeft = (shooter.isHomeTeam ? match.homeTeamDirection : match.awayTeamDirection)
            == MatchManager.TeamAttackingDirection.LeftToRight;
        dribblerView = attackLeft ? leftPlayer : rightPlayer;
        defenderView = attackLeft ? rightPlayer : leftPlayer;
        ConfigureHeaderSavePlayer(dribblerView, shooter);
        ConfigureHeaderSavePlayer(defenderView, goalkeeper);
        dribblerView.attribute.text = shootingAttribute;
        dribblerView.modifier.text = "SHOOTER" + (expectedGoals.HasValue ? $" · xGoals {expectedGoals.Value:0.00}" : "");
        dribblerView.total.text = "";
        dribblerView.guidance.text = shooterGuidance ?? "Natural 1: off target · Other rolls: on target · JACKPOT total: JACKPOT";
        defenderView.attribute.text = savingAttribute;
        defenderView.modifier.text = "GOALKEEPER SAVE";
        defenderView.total.text = "";
        defenderView.guidance.text = goalkeeperGuidance ?? "Shooter rolls first";
        defenderView.die.Clear();
        dribblerView.die.Begin(true);
        singleRollView = dribblerView;
        shotComparison = true;
        shotComparisonKeeper = goalkeeper;
        footer.text = "Press [R] — shooter roll";
        // BeginSingle temporarily uses the narrower single-roll layout. Restore
        // the dragged location after switching back to the comparison layout.
        if (PenaltyShootoutManager.ActiveShootout != null && rememberedPosition)
            panel.anchoredPosition = lastDraggedPosition;
        FitAndClamp();
        return true;
    }

    public bool ContinueShotSaving(ShotManager manager, Func<bool> waiting, PlayerToken goalkeeper,
        int shotPower, int savingModifier, bool outsideBoxHandSave,
        string savingAttribute = null, string shooterGuidance = null)
    {
        if (!IsShotComparisonFor(manager) || stage != Stage.Transition || shotComparisonKeeper != goalkeeper) return false;
        singleWaiting = waiting;
        stage = Stage.Single;
        singleRollView = defenderView;
        ConfigureHeaderSavePlayer(defenderView, goalkeeper);
        if (savingAttribute != null) defenderView.attribute.text = savingAttribute;
        defenderView.modifier.text = "GOALKEEPER SAVE";
        defenderView.total.text = "";
        dribblerView.guidance.text = shooterGuidance ?? "Shot resolved · On target";
        defenderView.guidance.text = outsideBoxHandSave ? "Tie or win: handball and red card · Lose: shot continues"
            : SingleRollResolutionPresentation.HeaderSaveGuidance(goalkeeper.saving, savingModifier, shotPower);
        defenderView.die.Begin(true);
        outcome.text = "";
        footer.text = "Press [R] — goalkeeper Saving roll";
        return true;
    }

    // Free kicks roll the shooter before the wall. Keep that resolved roll in the
    // attacking column while the existing defending column presents each blocker.
    public bool ContinueShotBlock(ShotManager manager, Func<bool> waiting, PlayerToken blocker,
        string attribute, string guidance, string shooterGuidance)
    {
        if (!IsShotComparisonFor(manager) || stage != Stage.Transition || blocker == null) return false;
        singleWaiting = waiting;
        stage = Stage.Single;
        ConfigureHeaderSavePlayer(defenderView, blocker);
        defenderView.attribute.text = attribute;
        defenderView.modifier.text = "DEFENDER BLOCK";
        defenderView.total.text = "";
        defenderView.guidance.text = guidance;
        dribblerView.guidance.text = shooterGuidance;
        defenderView.die.Begin(true);
        singleRollView = defenderView;
        title.text = "Free Kick Shot · Defender Block";
        outcome.text = "";
        footer.text = "Press [R] — defender block roll";
        FitAndClamp();
        return true;
    }

    private static void ConfigureHeaderSavePlayer(DuelPlayerPanelFields view, PlayerToken token)
    {
        var settings = MatchManager.Instance.gameData.gameSettings;
        view.playerName.text = $"#{token.jerseyNumber}  {token.playerName}";
        view.teamName.text = token.isHomeTeam ? settings.homeTeamName : settings.awayTeamName;
        if (view.role != null) view.role.gameObject.SetActive(false);
        var palette = TeamPalette(token);
        view.playerName.color = PlayerNameColor;
        view.die.SetTeamColors(palette.Primary, palette.Secondary);
    }

    // Explicit manager handoff: a failed accuracy check or tied duel owns this continuation.
    public bool PrepareScatterContinuation(MonoBehaviour manager, MonoBehaviour nextOwner)
    {
        if (!IsOpen || stage != Stage.Transition || (owner != manager && singleOwner != manager)
            || nextOwner == null || scatterContinuationContent == null
            || !LooseRollBound(continuationDirection) || !LooseRollBound(continuationDistance)) return false;
        pendingScatterOwner = nextOwner;
        footer.text = "Preparing direction roll…";
        return true;
    }

    public bool BeginLooseBall(MonoBehaviour manager, Func<bool> waiting, PlayerToken token, bool direction,
        bool inaccuracy = false)
    {
        if (looseBallContent == null || looseBallPlayer == null || !LooseRollBound(looseDirection)
            || !LooseRollBound(looseDistance)) return false;
        if (direction)
        {
            bool attached = IsOpen && stage == Stage.Transition && pendingScatterOwner == manager;
            if (attached)
            {
                singleMode = true;
                singleOwner = manager;
                singleWaiting = waiting;
                owner = null;
                stage = Stage.Single;
                IsResultHeld = false;
                looseBallContent.SetActive(false);
                scatterCaption.text = (inaccuracy ? "Inaccuracy" : "Loose Ball")
                    + (token != null ? $" · #{token.jerseyNumber} {token.playerName}" : "");
                SetScatterExpandedLayout();
                activeDirection = continuationDirection;
                activeDistance = continuationDistance;
            }
            else
            {
                if (!BeginSingle(manager, waiting, "Loose Ball Direction", token, null, null, false)) return false;
                singleRollContent.SetActive(false);
                looseBallContent.SetActive(true);
                looseBallPlayer.text = token != null ? $"#{token.jerseyNumber}  {token.playerName}" : "";
                activeDirection = looseDirection;
                activeDistance = looseDistance;
            }
            scatterAttached = attached;
            scatterHistoryOwner = manager;
            pendingScatterOwner = null;
            looseBallMode = true;
            looseDirectionPending = true;
            Color primary = token != null ? TeamColor(token) : new Color(.13f, .35f, .63f);
            Color secondary = token != null ? TeamPalette(token).Secondary : Color.white;
            looseBallPlayer.color = PlayerNameColor;
            activeDirection.die.SetTeamColors(primary, secondary);
            activeDistance.die.SetTeamColors(primary, secondary);
            activeDirection.column.gameObject.SetActive(true);
            activeDistance.column.gameObject.SetActive(true);
            activeDirection.die.Clear();
            activeDistance.die.Clear();
            activeDirection.result.text = "Awaiting roll";
            activeDistance.result.text = "Pending";
            activeDirection.die.Begin(false);
            footer.text = "Press [R] — direction roll";
            FitAndClamp();
            return true;
        }
        if (!IsOpen || !looseBallMode || singleOwner != manager || looseDirectionPending
            || stage != Stage.Transition) return false;
        singleWaiting = waiting;
        stage = Stage.Single;
        if (!scatterAttached) title.text = "Loose Ball Distance";
        activeDistance.result.text = "Awaiting roll";
        activeDistance.die.Begin(false);
        footer.text = "Press [R] — distance roll";
        return true;
    }

    public void CompleteShootoutDeflection(MonoBehaviour manager, ShotManager shot, int distance, bool goal)
    {
        if (!looseBallMode || singleOwner != manager || stage != Stage.Transition) return;
        activeDistance.die.Clear();
        activeDistance.result.text = $"Fixed distance: {distance} {(distance == 1 ? "hex" : "hexes")}";
        outcome.text = goal ? "Shootout deflection · GOAL" : "Shootout deflection · Penalty missed";
        // The shootout now owns the continuation; no distance die is rolled.
        singleOwner = shot;
        singleWaiting = null;
        resultAllowsContinuation = true;
        BeginResultCountdown();
    }

    public void SetScatterExpandedLayout()
    {
        expanded = true;
        foulChecks.SetActive(false);
        scatterContinuationContent.SetActive(true);
        FitAndClamp();
    }

    private static bool LooseRollBound(DuelCheckPanelFields fields)
        => fields?.column != null && fields.rule != null && fields.result != null && fields.die != null;

    public IEnumerator ResolveLooseBallRoll(MonoBehaviour manager, GameplayDiceRollResult roll,
        string result, bool direction, bool continueWithMovement = false)
    {
        if (!looseBallMode || singleOwner != manager || stage != Stage.Single
            || direction != looseDirectionPending) return EmptyPresentation();
        resultAllowsContinuation = !direction && continueWithMovement;
        stage = Stage.Transition;
        var view = direction ? activeDirection : activeDistance;
        // Scatter has no jackpot outcome. Legacy/scripted jackpot inputs normalize to numeric six.
        view.die.Resolve(roll.roll, false);
        string face = $"Roll {roll.roll}";
        view.result.text = $"{face}\n{result}";
        if (direction)
        {
            looseDirectionPending = false;
            footer.text = "Preparing distance roll…";
            // The manager clears its direction wait and enters the distance wait next.
            // The panel stays open with the direction die and text intact throughout.
            return EmptyPresentation();
        }
        if (continueWithMovement)
        {
            footer.text = "Loose ball resolved · Continuing play…";
            return EmptyPresentation();
        }
        return WaitForDismissal();
    }

    public void SuspendLooseBallForExternalInput(MonoBehaviour manager)
    {
        if (singleOwner != manager || !looseBallMode || stage != Stage.Transition) return;
        looseBallSuspended = true;
        visuals.SetActive(false);
    }

    public IEnumerator ResolveLooseBallCorner(MonoBehaviour manager)
    {
        if (!looseBallMode || singleOwner != manager || stage != Stage.Transition) return EmptyPresentation();
        activeDistance.column.gameObject.SetActive(false);
        activeDistance.die.Clear();
        if (scatterOutcome != null)
        {
            scatterOutcome.text = "Corner kick awarded";
            scatterOutcome.gameObject.SetActive(true);
        }
        FitAndClamp();
        return WaitForDismissal();
    }

    public bool BeginLooseBallRecovery(MonoBehaviour manager, Func<bool> waiting, PlayerToken token,
        bool useSaving = false, int modifier = 0)
    {
        bool continuingScatter = singleOwner == manager && looseBallMode && stage == Stage.Transition;
        if (token == null || (IsOpen && !continuingScatter) || looseRecoveryContent == null
            || !PlayerBound(looseRecoveryPlayer)) return false;
        if (scatterHistoryOwner != manager) return false;
        resultAllowsContinuation = false;
        useSaving |= token.IsGoalKeeper;
        singleMode = true;
        looseBallSuspended = false;
        singleOwner = manager;
        singleWaiting = waiting;
        owner = null;
        stage = Stage.Single;
        IsResultHeld = false;
        looseBallMode = false;
        looseRecoveryMode = true;
        int skill = useSaving ? token.saving : token.tackling;
        float expected = ExpectedStatsCalculator.CalculateRecoveryProbability(skill, modifier);
        ConfigurePlayer(looseRecoveryPlayer, token, useSaving ? "Saving" : "Tackling", skill, 0, "xRecovery", expected);
        looseRecoveryPlayer.attribute.text = SingleRollResolutionPresentation.LooseRecoveryAttribute(token, useSaving, modifier);
        looseRecoveryPlayer.modifier.text = $"xRecovery {expected:0.00}";
        looseRecoveryPlayer.total.text = "";
        looseRecoveryPlayer.guidance.text = SingleRollResolutionPresentation.RecoveryGuidance(GameplayScore.EffectiveAttribute(skill, modifier), 6, 0);
        looseRecoveryPlayer.die.Begin(true);
        SetLooseRecoveryLayout(scatterAttached);
        footer.text = "Press [R] — recovery roll";
        visuals.SetActive(true);
        visuals.transform.SetAsLastSibling();
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        return true;
    }

    public void SetLooseRecoveryLayout(bool attached)
    {
        looseRecoveryMode = true;
        expanded = true;
        foulChecks.SetActive(false);
        var recovery = (RectTransform)looseRecoveryContent.transform;
        recovery.anchoredPosition = new Vector2(recovery.anchoredPosition.x,
            -(attached ? recoveryAttachedTop : recoveryStandaloneTop));
        looseRecoveryContent.SetActive(true);
        FitAndClamp();
    }

    public IEnumerator ResolveLooseBallRecovery(MonoBehaviour manager, GameplayDiceRollResult roll, bool recovered)
    {
        if (!looseRecoveryMode || singleOwner != manager || stage != Stage.Single) return EmptyPresentation();
        stage = Stage.Transition;
        looseRecoveryPlayer.die.Resolve(roll.roll, roll.jackpotEnabled && roll.isJackpot);
        string face = roll.jackpotEnabled && roll.isJackpot ? "JACKPOT ★" : $"Roll {roll.roll}";
        looseRecoveryPlayer.total.text = face + (recovered ? "\nBALL RECOVERED" : "\nRECOVERY FAILED");
        return WaitForDismissal();
    }

    public void ForgetLooseBallPresentation(MonoBehaviour manager)
    {
        CancelSingle(manager);
        if (scatterHistoryOwner != manager) return;
        scatterHistoryOwner = null;
        looseRecoveryMode = false;
        if (looseRecoveryContent != null) looseRecoveryContent.SetActive(false);
    }

    public void CancelSingle(MonoBehaviour manager, bool preserveResultCountdown = false)
    {
        if (stage == Stage.Closed || (singleOwner != manager && pendingScatterOwner != manager)) return;
        // Resetting a resolved shootout shot must not erase its visible countdown.
        if (preserveResultCountdown && IsComplete && resultAllowsContinuation) return;
        stage = Stage.Complete;
        Dismiss();
    }

    public IEnumerator ResolveSingle(MonoBehaviour manager, GameplayDiceRollResult roll, string result,
        bool continueWithFoulChecks = false, bool continueWithScatter = false, MonoBehaviour scatterOwner = null,
        int? resolvedTotal = null, bool continueWithNextRoll = false, bool continueWhileResultVisible = false)
    {
        if (!singleMode || singleOwner != manager || stage != Stage.Single) return EmptyPresentation();
        resultAllowsContinuation = false;
        stage = Stage.Transition;
        var rollingView = singleRollView ?? singlePlayer;
        rollingView.die.Resolve(roll.roll, roll.jackpotEnabled && roll.isJackpot);
        rollingView.total.text = roll.isJackpot && roll.jackpotEnabled
            ? (resolvedTotal.HasValue ? "Roll JACKPOT · Total: JACKPOT" : "JACKPOT ★")
            : $"Roll {roll.roll}" + (resolvedTotal.HasValue ? $" · Total: {resolvedTotal.Value}" : "");
        outcome.text = result;
        // A running dribbler's recovery foul continues directly into the existing checks.
        // Keep the resolved recovery die visible; only the final injury result starts dismissal.
        if (continueWithScatter && PrepareScatterContinuation(manager, scatterOwner != null ? scatterOwner : manager)) return EmptyPresentation();
        if (continueWhileResultVisible)
        {
            resultAllowsContinuation = true;
            BeginResultCountdown();
            return EmptyPresentation();
        }
        return continueWithFoulChecks || continueWithNextRoll ? EmptyPresentation() : WaitForDismissal();
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
        if (view.role != null) { view.role.text = ""; view.role.gameObject.SetActive(false); }
        view.attribute.text = $"{attribute} {skill} ({expectedLabel} {expectedValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)})";
        view.modifier.text = bonus != 0 ? $"Nutmeg Bonus  +{bonus}" : "";
        view.total.text = "Total  —";
        Color color = TeamColor(token);
        view.playerName.color = PlayerNameColor;
        view.die.SetTeamColors(color, TeamPalette(token).Secondary);
        view.die.Clear();
    }

    private static bool PlayerBound(DuelPlayerPanelFields view)
        => view?.playerName != null && view.teamName != null && view.attribute != null
            && view.modifier != null && view.total != null && view.guidance != null && view.die != null;

    // Names sit on the authored white surface, including while its parent is hidden.
    private static readonly Color PlayerNameColor = new Color32(30, 69, 89, 255);

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
            int defenderTotal = GameplayScore.Total(tackling, roll.roll, defenderBonus, roll.isJackpot);
            outcome.text = foul ? foulIsPenalty ? "PENALTY!" : "FOUL — tackler committed a foul"
                : DuelRollGuidance.LiveOutcomes(ExpectedStatsCalculator.CalculateGroundDuelAfterDefenderRoll(dribbling, defenderTotal, false), duelKind);
            dribblerView.guidance.text = DuelRollGuidance.ForDribbler(dribbling,
                defenderTotal, roll.isJackpot, foul, foulIsPenalty, duelKind);
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
        BeginResultCountdown();
        while (IsOpen) yield return null;
    }

    private void BeginResultCountdown()
    {
        stage = Stage.Complete;
        IsResultHeld = false;
        closeAt = Time.unscaledTime + resultSeconds;
        if (onlineAuthorityHeld) onlineSecondsRemaining = resultSeconds;
        UpdateFooter();
    }

    public bool Dismiss()
    {
        if (!IsComplete || onlineAuthorityHeld) return false;
        ReserveEscape();
        looseBallSuspended = false;
        IsResultHeld = false;
        stage = Stage.Closed;
        resultAllowsContinuation = false;
        owner = null;
        singleOwner = null;
        singleWaiting = null;
        pendingScatterOwner = null;
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
        if (IsVisible && Instance.onlineAuthorityControlled)
        {
            key.Consume(nameof(DuelResolutionPanelController));
            return true;
        }
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
        if (expected && (current.singleChoiceKeys != null
            ? System.Array.IndexOf(current.singleChoiceKeys, key.key) >= 0 : roll)) return false;
        key.Consume(nameof(DuelResolutionPanelController));
        return true;
    }

    private void Update()
    {
        if (!IsOpen) return;
        RefreshModifierBackground(leftPlayer);
        RefreshModifierBackground(rightPlayer);
        if (singleMode && stage == Stage.Single && (singleOwner == null
            || !(onlineAuthorityControlled ? singleOwner.gameObject.activeInHierarchy : singleOwner.isActiveAndEnabled)
            || singleWaiting == null || !singleWaiting()))
        {
            stage = Stage.Complete;
            Dismiss();
            return;
        }
        FitAndClamp();
        if (!onlineAuthorityControlled && Input.GetKeyDown(KeyCode.Escape))
        {
            waitForEscapeRelease = true;
            if (IsComplete) { Dismiss(); return; }
        }
        if (IsComplete)
        {
            UpdateFooter();
            if (!onlineAuthorityHeld && !IsResultHeld && Time.unscaledTime >= closeAt) Dismiss();
        }
    }

    public void ToggleResultHold()
    {
        if (!IsComplete || onlineAuthorityHeld) return;
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
        if (onlineAuthorityControlled)
        {
            footer.text = onlineAuthorityHeld ? "Waiting for the match to resume"
                : IsResultHeld ? "Result held"
                : $"Closes automatically in {Mathf.Max(0, Mathf.CeilToInt(closeAt - Time.unscaledTime))} seconds";
            return;
        }
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
        looseRecoveryMode = false;
        if (looseRecoveryContent != null) looseRecoveryContent.SetActive(false);
        if (scatterContinuationContent != null) scatterContinuationContent.SetActive(false);
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
        bool singleLayout = (singleRollContent != null && singleRollContent.activeSelf)
            || (looseBallContent != null && looseBallContent.activeSelf);
        float width = Mathf.Max(1f, singleLayout ? singleRollWidth : preferredWidth);
        float maximumHeight = Mathf.Max(1f, normalHeight, expandedHeight);
        if (scatterOutcome != null && scatterContinuationContent != null)
        {
            var scatter = (RectTransform)scatterContinuationContent.transform;
            var result = scatterOutcome.rectTransform;
            maximumHeight = Mathf.Max(maximumHeight,
                -scatter.anchoredPosition.y + scatter.rect.height + result.anchoredPosition.y + result.rect.height + 24f);
        }
        if (looseRecoveryContent != null && looseRecoveryContent.activeSelf)
            maximumHeight = Mathf.Max(maximumHeight,
                -((RectTransform)looseRecoveryContent.transform).anchoredPosition.y + recoveryContentHeight
                    + footer.rectTransform.rect.height + 48f);
        float height = expanded ? maximumHeight : Mathf.Max(1f, normalHeight);
        if (singleLayout && expanded)
        {
            float contentBottom = -outcome.rectTransform.anchoredPosition.y + outcome.rectTransform.rect.height;
            contentBottom = Mathf.Max(contentBottom, VisibleSectionBottom(foulChecks));
            contentBottom = Mathf.Max(contentBottom, VisibleSectionBottom(scatterContinuationContent));
            contentBottom = Mathf.Max(contentBottom, VisibleSectionBottom(looseRecoveryContent));
            float resultSpace = scatterOutcome != null && scatterOutcome.gameObject.activeSelf
                ? scatterOutcome.rectTransform.rect.height + Mathf.Max(12f,
                    scatterOutcome.rectTransform.anchoredPosition.y - footer.rectTransform.anchoredPosition.y
                    - footer.rectTransform.rect.height) : 0f;
            height = Mathf.Max(normalHeight, contentBottom + 24f + resultSpace
                + footer.rectTransform.rect.height + footer.rectTransform.anchoredPosition.y);
        }
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

    private static float VisibleSectionBottom(GameObject section)
    {
        if (section == null || !section.activeSelf) return 0;
        var rect = (RectTransform)section.transform;
        return -rect.anchoredPosition.y + rect.rect.height;
    }

    private static void RefreshModifierBackground(DuelPlayerPanelFields view)
    {
        if (view?.modifierBackground == null || view.modifier == null) return;
        bool visible = !string.IsNullOrWhiteSpace(view.modifier.text);
        if (view.modifierBackground.enabled != visible) view.modifierBackground.enabled = visible;
    }
}
