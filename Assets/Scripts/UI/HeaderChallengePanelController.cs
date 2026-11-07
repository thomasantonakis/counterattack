using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class HeaderContestantFields
{
    public GameObject root;
    public TMP_Text player, attribute, result, status;
    public Image highlight;
    public DuelDieView die;
    [NonSerialized] public PlayerToken token;
}

/// <summary>Only presents manager-owned nominations, scores and continuations. No gameplay dice or rules.</summary>
public sealed class HeaderChallengePanelController : MonoBehaviour
{
    public GameObject visuals, scatter, recoveryContent;
    public DuelPlayerPanelFields recoveryPlayer = new();
    public RectTransform panel, leftColumn, rightColumn;
    public TMP_Text title, attackTeam, defenseTeam, attackBest, defenseBest, guidance, outcome, footer;
    public HeaderContestantFields[] attackers = new HeaderContestantFields[2];
    public HeaderContestantFields[] defenders = new HeaderContestantFields[3];
    public DuelCheckPanelFields direction = new(), distance = new();
    public float resultSeconds = 5f;
    public float compactHeight = 1020, expandedHeight = 1280, recoveryHeight = 1610, preferredWidth = 1540;
    public static HeaderChallengePanelController Instance { get; private set; }
    private MonoBehaviour owner;
    private HeaderManager header;
    private LooseBallManager loose;
    private LooseBallManager scatterHistoryOwner;
    private bool complete, remembered, suspended;
    private float closeAt, remaining;
    private string matchKey;
    private static int escapeFrame = -1;
    private static bool escapeHeld;
    public bool IsOpen { get; private set; }
    public bool IsResultHeld { get; private set; }
    public static bool IsVisible => Instance != null && Instance.IsOpen;
    public static bool IsHoldingContinuation => IsVisible && !Instance.IsAwaitingRoll;
    public static bool IsMatchClockHeld => IsVisible && Instance.IsResultHeld;
    private bool IsAwaitingRoll => !complete && (header != null && header.isWaitingForHeaderRoll
        || loose != null && (loose.isWaitingForDirectionRoll || loose.isWaitingForDistanceRoll
            || recoveryContent != null && recoveryContent.activeSelf && loose.isWaitingForInterceptionRoll));
    public bool HasSceneBindings => visuals != null && panel != null && scatter != null
        && title != null && attackTeam != null && defenseTeam != null && attackBest != null && defenseBest != null
        && guidance != null && outcome != null && footer != null && leftColumn != null && rightColumn != null
        && RecoveryBound && Bound(attackers, 2) && Bound(defenders, 3) && CheckBound(direction) && CheckBound(distance);
    private bool RecoveryBound => recoveryContent != null && recoveryPlayer?.playerName != null
        && recoveryPlayer.teamName != null && recoveryPlayer.attribute != null && recoveryPlayer.modifier != null
        && recoveryPlayer.guidance != null && recoveryPlayer.die != null && recoveryPlayer.total != null;
    private static bool Bound(HeaderContestantFields[] rows, int count)
    {
        if (rows == null || rows.Length != count) return false;
        foreach (var row in rows)
            if (row == null || row.root == null || row.player == null || row.attribute == null
                || row.result == null || row.status == null || row.highlight == null || row.die == null) return false;
        return true;
    }
    private static bool CheckBound(DuelCheckPanelFields fields) => fields?.column != null
        && fields.rule != null && fields.result != null && fields.die != null;
    private void Awake() { Instance = this; Close(); }
    private void OnDisable() { Close(); scatterHistoryOwner = null; }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public bool Begin(HeaderManager manager)
    {
        if (!HasSceneBindings || manager == null) return false;
        Close();
        scatterHistoryOwner = null;
        recoveryContent.SetActive(false);
        header = manager;
        owner = manager;
        title.text = ChallengeTitle(manager.headerAtGoalDeclared);
        var match = MatchManager.Instance;
        string key = match.GetEntityId() + ":" + match.gameData?.playSessionId;
        if (matchKey != key) { remembered = false; matchKey = key; }
        PlayerToken first = manager.attackerWillJump[0];
        bool attackLeft = (first.isHomeTeam ? match.homeTeamDirection : match.awayTeamDirection)
            == MatchManager.TeamAttackingDirection.LeftToRight;
        // Columns remain authored; switch their role assignment with the duel's direction convention.
        PlaceColumn((RectTransform)attackTeam.transform.parent, attackLeft);
        PlaceColumn((RectTransform)defenseTeam.transform.parent, !attackLeft);
        Configure(attackers, manager.attackerWillJump);
        Configure(defenders, manager.defenderWillJump);
        var settings = match.gameData.gameSettings;
        attackTeam.text = "ATTACKING TEAM\n" + (first.isHomeTeam ? settings.homeTeamName : settings.awayTeamName);
        defenseTeam.text = "DEFENDING TEAM\n" + (first.isHomeTeam ? settings.awayTeamName : settings.homeTeamName);
        outcome.text = "";
        guidance.text = "";
        scatter.SetActive(false);
        direction.die.Clear(); distance.die.Clear();
        direction.result.text = distance.result.text = "Pending";
        visuals.SetActive(true);
        IsOpen = true;
        visuals.transform.SetAsLastSibling();
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        if (!remembered && panel.parent is RectTransform bounds)
            panel.anchoredPosition = new Vector2(0, bounds.rect.height * .3f);
        Refresh(); FitAndClamp();
        return true;
    }

    private static void PlaceColumn(RectTransform column, bool left)
    {
        column.anchorMin = new Vector2(left ? 0 : .5f, 1);
        column.anchorMax = new Vector2(left ? .5f : 1, 1);
    }

    private void Configure(HeaderContestantFields[] rows, List<PlayerToken> tokens)
    {
        if (rows == defenders)
        {
            // The authored third row is reserved for an eligible nominated goalkeeper.
            var nominated = tokens.FindAll(token => !token.IsGoalKeeper);
            while (nominated.Count < 2) nominated.Add(null);
            nominated.Add(tokens.Find(token => token.IsGoalKeeper));
            tokens = nominated;
        }
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            row.token = i < tokens.Count ? tokens[i] : null;
            row.root.SetActive(row.token != null);
            row.die.Clear();
            row.result.text = "Roll — - Total: —";
            row.status.text = "Pending";
            row.highlight.color = new Color32(236, 242, 244, 255);
            if (row.token == null) continue;
            row.player.text = PlayerName(row.token);
            int penalty = header.GetHeaderPenalty(row.token);
            row.attribute.text = AttributeText(HeaderManager.UsesAerialAbility(row.token), header.GetHeaderAttribute(row.token), penalty);
            if (header.headerAtGoalDeclared && rows == attackers)
            {
                float xGoals = header.CalculateHeaderExpectedGoals(row.token,
                    includeGoalkeeperSave: !header.defenderWillJump.Contains(header.hexGrid.GetDefendingGK()));
                row.attribute.text += $" · xGoals {xGoals:0.00}";
            }
            var settings = MatchManager.Instance.gameData.gameSettings;
            var palette = TokenKitCatalog.GetInstructionPalette(TokenKitCatalog.ResolveStyle(
                row.token.isHomeTeam ? settings.homeKit : settings.awayKit));
            row.die.SetTeamColors(palette.Primary, palette.Secondary);
        }
    }

    public static string PlayerName(PlayerToken token) => token == null ? "—"
        : $"#{token.jerseyNumber}  {(string.IsNullOrWhiteSpace(token.playerName) ? token.name : token.playerName)}";
    public static string ChallengeTitle(bool atGoal) => atGoal ? "HEADER AT GOAL CHALLENGE" : "HEADER PASS CHALLENGE";
    public static string AttributeText(bool goalkeeper, int value, int modifier)
    {
        string label = goalkeeper ? "Aerial" : "Heading";
        return modifier == 0 ? $"{label}: {value}"
            : $"{label}: {value}{modifier:+0;-0} = {value + modifier} (distance penalty)";
    }
    public static string Score(int value) => value == 50 ? "JACKPOT" : value.ToString();

    public void Refresh()
    {
        if (!IsOpen || header == null) return;
        var bestAttack = header.GetBestHeaderCandidate(header.attackerWillJump);
        var bestDefense = header.GetBestHeaderCandidate(header.defenderWillJump, true);
        Summary(attackBest, "Best Attack", bestAttack);
        Summary(defenseBest, "Best Defense", bestDefense);
        UpdateRows(attackers, bestAttack);
        UpdateRows(defenders, bestDefense);
        guidance.text = PlayerName(header.tokenRolling) + " · " + header.GetHeaderRollGuidance(header.tokenRolling);
        footer.text = "[R] roll — " + PlayerName(header.tokenRolling);
    }
    private void Summary(TMP_Text text, string label, PlayerToken best) => text.text = label + ": "
        + (best == null ? "—" : Score(header.tokenScores[best].totalScore)) + "\n" + PlayerName(best);
    private void UpdateRows(HeaderContestantFields[] rows, PlayerToken best)
    {
        foreach (var row in rows)
        {
            if (row.token == null) continue;
            bool rolled = header.tokenScores.TryGetValue(row.token, out var score);
            bool active = !rolled && header.tokenRolling == row.token;
            row.status.text = rolled ? (best == row.token ? "Rolled · TEAM BEST" : "Rolled") : active ? "ROLLING" : "Pending";
            row.highlight.color = active ? new Color32(255, 239, 180, 255)
                : rolled && best == row.token ? new Color32(224, 242, 237, 255) : new Color32(236, 242, 244, 255);
            row.die.RefreshBorderContrast();
            if (rolled)
            {
                bool jackpot = score.totalScore == 50;
                row.die.Resolve(score.roll, jackpot);
                row.result.text = jackpot ? "Roll JACKPOT - Total: JACKPOT" : $"Roll {score.roll} - Total: {score.totalScore}";
            }
            else if (active && !row.die.IsAnimating) row.die.Begin(true);
        }
    }

    public IEnumerator Finish(HeaderManager manager, PlayerToken winner, string result, bool scatterFollows)
    {
        if (!IsOpen || owner != manager) yield break;
        Refresh();
        outcome.text = result;
        guidance.text = "";
        foreach (var rows in new[] { attackers, defenders }) foreach (var row in rows)
            if (winner != null && row.token == winner)
            {
                row.status.text = "WINNER · TEAM BEST";
                row.highlight.color = new Color32(177, 224, 213, 255);
                row.die.RefreshBorderContrast();
            }
        if (scatterFollows) { footer.text = "Loose ball continuation"; yield break; }
        yield return WaitForDismissal();
    }
    public void TransferScatter(HeaderManager from, LooseBallManager to)
    {
        if (!IsOpen || owner != from) return;
        var deflector = from.GetBestHeaderCandidate(from.defenderWillJump, true);
        if (deflector != null)
        {
            var settings = MatchManager.Instance.gameData.gameSettings;
            var palette = TokenKitCatalog.GetInstructionPalette(TokenKitCatalog.ResolveStyle(
                deflector.isHomeTeam ? settings.homeKit : settings.awayKit));
            direction.die.SetTeamColors(palette.Primary, palette.Secondary);
            distance.die.SetTeamColors(palette.Primary, palette.Secondary);
        }
        owner = to; loose = to; header = null;
        scatterHistoryOwner = to;
        scatter.SetActive(true);
        footer.text = "Loose ball continuation";
        FitAndClamp();
    }
    public bool Owns(MonoBehaviour manager) => IsOpen && owner == manager;
    // Temporarily release input during the pre-scatter GK move without losing authored results.
    public void SuspendForExternalInput(MonoBehaviour manager)
    {
        if (owner != manager) return;
        suspended = true; IsOpen = false; visuals.SetActive(false);
    }
    public void ResumeAfterExternalInput(MonoBehaviour manager)
    {
        if (!suspended || owner != manager) return;
        suspended = false; IsOpen = true; visuals.SetActive(true);
    }
    public void BeginScatter(LooseBallManager manager, bool isDirection)
    {
        if (!Owns(manager)) return;
        (isDirection ? direction : distance).die.Begin(false);
        (isDirection ? direction : distance).result.text = "Rolling";
        footer.text = isDirection ? "[R] direction roll" : "[R] distance roll";
    }
    public IEnumerator ResolveScatter(LooseBallManager manager, GameplayDiceRollResult roll, string result, bool isDirection)
    {
        if (!Owns(manager)) yield break;
        var fields = isDirection ? direction : distance;
        fields.die.Resolve(roll.roll, false);
        fields.result.text = $"Roll {roll.roll} · {result}";
        if (!isDirection) yield return WaitForDismissal();
    }
    public bool BeginRecovery(LooseBallManager manager, PlayerToken token, bool useSaving = false, int modifier = 0)
    {
        if (!RecoveryBound || token == null || scatterHistoryOwner != manager
            || !isActiveAndEnabled) return false;
        useSaving |= token.IsGoalKeeper;
        owner = manager; loose = manager; header = null;
        complete = IsResultHeld = suspended = false;
        IsOpen = true;
        recoveryContent.SetActive(true);
        recoveryPlayer.playerName.text = PlayerName(token);
        var settings = MatchManager.Instance.gameData.gameSettings;
        recoveryPlayer.teamName.text = token.isHomeTeam ? settings.homeTeamName : settings.awayTeamName;
        int skill = useSaving ? token.saving : token.tackling;
        recoveryPlayer.attribute.text = SingleRollResolutionPresentation.LooseRecoveryAttribute(token, useSaving, modifier);
        recoveryPlayer.modifier.text = $"xRecovery {ExpectedStatsCalculator.CalculateRecoveryProbability(skill, modifier):0.00}";
        recoveryPlayer.guidance.text = SingleRollResolutionPresentation.RecoveryGuidance(skill + modifier, 6, 0);
        recoveryPlayer.total.text = "Awaiting roll";
        var palette = TokenKitCatalog.GetInstructionPalette(TokenKitCatalog.ResolveStyle(token.isHomeTeam ? settings.homeKit : settings.awayKit));
        recoveryPlayer.die.SetTeamColors(palette.Primary, palette.Secondary);
        recoveryPlayer.die.Begin(true);
        footer.text = "[R] loose-ball recovery roll";
        visuals.SetActive(true);
        visuals.transform.SetAsLastSibling();
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        FitAndClamp();
        return true;
    }
    public IEnumerator ResolveRecovery(LooseBallManager manager, GameplayDiceRollResult roll, bool recovered)
    {
        if (!Owns(manager) || !recoveryContent.activeSelf) yield break;
        recoveryPlayer.die.Resolve(roll.roll, roll.jackpotEnabled && roll.isJackpot);
        recoveryPlayer.total.text = (roll.jackpotEnabled && roll.isJackpot ? "JACKPOT" : $"Roll {roll.roll}")
            + (recovered ? " · BALL RECOVERED" : " · RECOVERY FAILED");
        yield return WaitForDismissal();
    }
    private IEnumerator WaitForDismissal()
    {
        complete = true; closeAt = Time.unscaledTime + resultSeconds;
        UpdateFooter();
        while (IsOpen) yield return null;
    }
    public void Cancel(MonoBehaviour manager)
    {
        if (owner != manager && scatterHistoryOwner != manager) return;
        Close(); scatterHistoryOwner = null;
    }
    public void Close()
    {
        if (IsOpen) { escapeFrame = Time.frameCount; escapeHeld |= Input.GetKey(KeyCode.Escape); }
        IsOpen = complete = IsResultHeld = suspended = false;
        owner = null; header = null; loose = null;
        if (visuals != null) visuals.SetActive(false);
        foreach (var die in GetComponentsInChildren<DuelDieView>(true)) if (die.IsAnimating) die.Clear();
    }
    public static bool ReservePauseEscape()
    {
        if (IsVisible) return true;
        if (escapeHeld)
        {
            if (!Input.GetKey(KeyCode.Escape)) { escapeHeld = false; escapeFrame = Time.frameCount; }
            return true;
        }
        return escapeFrame == Time.frameCount;
    }
    public static bool ConsumeKey(KeyPressData key)
    {
        if (key.key == KeyCode.Escape && ReservePauseEscape())
        {
            escapeHeld |= Input.GetKey(KeyCode.Escape);
            if (IsVisible && Instance.complete) Instance.Close();
            key.Consume(nameof(HeaderChallengePanelController)); return true;
        }
        if (!IsVisible) return false;
        var current = Instance;
        if ((key.key is KeyCode.Return or KeyCode.KeypadEnter) && current.complete)
        {
            if (current.IsResultHeld) current.closeAt = Time.unscaledTime + current.remaining;
            else current.remaining = Mathf.Max(0, current.closeAt - Time.unscaledTime);
            current.IsResultHeld = !current.IsResultHeld;
            current.UpdateFooter();
        }
        else if ((key.key == KeyCode.R || RollInputOverride.TryParse(key, out _)) && current.IsAwaitingRoll) return false;
        key.Consume(nameof(HeaderChallengePanelController)); return true;
    }
    private void Update()
    {
        if (!IsOpen) return;
        if (owner == null || !owner.isActiveAndEnabled) { Close(); return; }
        if (Input.GetKeyDown(KeyCode.Escape)) { escapeHeld = true; if (complete) { Close(); return; } }
        if (complete) { UpdateFooter(); if (!IsResultHeld && Time.unscaledTime >= closeAt) Close(); }
        FitAndClamp();
    }
    private void UpdateFooter() => footer.text = IsResultHeld
        ? "Result held · Match clock stopped · [Enter] resume · [Esc] close"
        : $"Closes in {Mathf.Max(0, Mathf.CeilToInt(closeAt - Time.unscaledTime))}s · [Enter] hold · [Esc] close";
    public void MovePanel(Vector2 position) { panel.anchoredPosition = position; remembered = true; FitAndClamp(); }
    public void FitAndClamp()
    {
        if (panel == null || panel.parent is not RectTransform bounds) return;
        float height = recoveryContent != null && recoveryContent.activeSelf ? recoveryHeight
            : scatter != null && scatter.activeSelf ? expandedHeight : compactHeight;
        panel.sizeDelta = new Vector2(preferredWidth, height);
        float scale = Mathf.Max(.001f, Mathf.Min(1, bounds.rect.width * .5f / preferredWidth, bounds.rect.height * .94f / Mathf.Max(expandedHeight, recoveryHeight)));
        panel.localScale = Vector3.one * scale;
        float x = Mathf.Max(0, (bounds.rect.width - preferredWidth * scale) * .5f);
        float top = bounds.rect.height * .5f;
        panel.anchoredPosition = new Vector2(Mathf.Clamp(panel.anchoredPosition.x, -x, x),
            Mathf.Clamp(panel.anchoredPosition.y, -top + height * scale, top));
    }
}
