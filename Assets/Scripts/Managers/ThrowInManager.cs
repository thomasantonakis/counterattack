using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class ThrowInManager : MonoBehaviour
{
    [Header("Dependencies")]
    public Ball ball;
    public HexGrid hexGrid;
    public MovementPhaseManager movementPhaseManager;
    public GroundBallManager groundBallManager;
    public HeaderManager headerManager;
    public FinalThirdManager finalThirdManager;
    public OutOfBoundsPushManager outOfBoundsPushManager;

    [Header("Runtime Flags")]
    public bool isActivated = false;
    public bool isWaitingForTakerSelection = false;
    public bool isRunningMandatoryMovement = false;
    public bool isWaitingForOptionalMovementDecision = false;
    public bool isRunningOptionalMovement = false;
    public bool isWaitingForThrowTypeSelection = false;
    public bool isWaitingForGroundTarget = false;
    public bool isWaitingForHeaderTarget = false;

    [Header("Runtime Data")]
    public HexCell throwInHex;
    public PlayerToken selectedThrower;
    public HexCell currentHeaderThrowTarget;
    public MatchManager.TeamInAttack awardedTeam;
    public int maxThrowDistance = 6;

    private const int DefaultGroundBallDistance = 11;
    private const int ThrowInProtectedRadius = 2;

    private void OnEnable()
    {
        GameInputManager.OnClick += OnClickReceived;
        GameInputManager.OnKeyPress += OnKeyReceived;
    }

    private void OnDisable()
    {
        GameInputManager.OnClick -= OnClickReceived;
        GameInputManager.OnKeyPress -= OnKeyReceived;
    }

    public void StartThrowInPreparation(HexCell inboundsHex, MatchManager.TeamInAttack throwInAwardedTeam)
    {
        if (inboundsHex == null)
        {
            Debug.LogError("ThrowInManager cannot start because inbounds throw-in hex is null.");
            return;
        }

        if (ball == null)
        {
            Debug.LogError("ThrowInManager cannot start because Ball is not linked.");
            return;
        }

        ResetThrowInState();
        isActivated = true;
        throwInHex = inboundsHex;
        awardedTeam = throwInAwardedTeam;
        EnsureAwardedTeamInAttack();
        StartCoroutine(ball.MoveToCell(throwInHex));
        MatchManager.Instance.currentState = MatchManager.GameState.WaitingForThrowInTaker;
        isWaitingForTakerSelection = true;
        Debug.Log($"Throw-in preparation started at {throwInHex.coordinates} for {awardedTeam}. Select the taker.");
    }

    private void OnClickReceived(PlayerToken token, HexCell hex)
    {
        if (!isActivated)
        {
            return;
        }

        if (isWaitingForTakerSelection)
        {
            if (token == null)
            {
                Debug.Log($"No token at {hex?.coordinates}. Select a player to take the throw-in.");
                return;
            }

            if (!IsTokenEligibleThrower(token))
            {
                Debug.Log($"{token.name} is not eligible to take this throw-in.");
                return;
            }

            StartCoroutine(HandleThrowerSelection(token));
            return;
        }

        if (isWaitingForHeaderTarget && hex != null)
        {
            if (!IsValidHeaderThrowTarget(hex))
            {
                Debug.Log($"Invalid throw-to-head target {hex.coordinates}. Choose an attacker within 6 hexes of the throw-in taker.");
                return;
            }

            if (MatchManager.Instance.difficulty_level == 3 || currentHeaderThrowTarget == hex)
            {
                StartCoroutine(CommitThrowToHead(hex));
                return;
            }

            currentHeaderThrowTarget = hex;
            hexGrid.ClearHighlightedHexes();
            currentHeaderThrowTarget.HighlightHex("passTargetCommitted");
            if (!hexGrid.highlightedHexes.Contains(currentHeaderThrowTarget))
            {
                hexGrid.highlightedHexes.Add(currentHeaderThrowTarget);
            }
            Debug.Log($"Throw-to-head target selected: {hex.GetOccupyingToken()?.name ?? hex.name}. Click again to confirm, or choose another eligible attacker.");
            return;
        }
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (!isActivated || keyData.isConsumed)
        {
            return;
        }

        if (isWaitingForOptionalMovementDecision)
        {
            if (keyData.key == KeyCode.M)
            {
                keyData.isConsumed = true;
                isWaitingForOptionalMovementDecision = false;
                StartCoroutine(RunOptionalMovementPhaseThenThrow());
                return;
            }

            if (keyData.key == KeyCode.T)
            {
                keyData.isConsumed = true;
                isWaitingForOptionalMovementDecision = false;
                EnterThrowExecutionSelection();
                return;
            }
        }

        if (isWaitingForThrowTypeSelection)
        {
            if (keyData.key == KeyCode.P)
            {
                keyData.isConsumed = true;
                HandleThrowToFeet();
                return;
            }

            if (keyData.key == KeyCode.C)
            {
                List<HexCell> availableHeaderTargets = GetAvailableHeaderThrowTargets();
                if (availableHeaderTargets.Count == 0)
                {
                    keyData.isConsumed = true;
                    Debug.LogWarning("Throw-to-head is not available: no attacker is within 6 hexes of the throw-in taker.");
                    return;
                }

                keyData.isConsumed = true;
                isWaitingForThrowTypeSelection = false;
                if (availableHeaderTargets.Count == 1)
                {
                    Debug.Log($"Throw-to-head auto-targeted: {availableHeaderTargets[0].GetOccupyingToken()?.name ?? availableHeaderTargets[0].name}.");
                    StartCoroutine(CommitThrowToHead(availableHeaderTargets[0]));
                    return;
                }

                isWaitingForHeaderTarget = true;
                currentHeaderThrowTarget = null;
                HighlightHeaderThrowTargets(availableHeaderTargets);
                Debug.Log(MatchManager.Instance.difficulty_level == 3
                    ? "Throw-in option selected: [C] to head. Click an eligible attacker to commit."
                    : "Throw-in option selected: [C] to head. Click an eligible attacker, then click again to confirm.");
                return;
            }
        }
    }

    private IEnumerator HandleThrowerSelection(PlayerToken token)
    {
        selectedThrower = token;
        yield return StartCoroutine(ResolveThrowInSpotOccupancy(selectedThrower));
        yield return StartCoroutine(MoveTokenToHex(selectedThrower, throwInHex));
        ball.PlaceAtCell(throwInHex);
        MatchManager.Instance.ClearLastTokenChain();
        MatchManager.Instance.SetLastToken(selectedThrower);
        MatchManager.Instance.MarkSetPieceTakerForNextTouchExclusion(selectedThrower);
        isWaitingForTakerSelection = false;
        Debug.Log($"{selectedThrower.name} moved to throw-in hex at {throwInHex.coordinates}.");
        finalThirdManager.TriggerFinalThirdPhase();
        while (finalThirdManager.isActivated)
        {
            yield return null;
        }
        StartCoroutine(RunMandatoryMovementPhaseThenPromptOptional());
    }

    private IEnumerator RunMandatoryMovementPhaseThenPromptOptional()
    {
        isRunningMandatoryMovement = true;
        movementPhaseManager.ResetMovementPhase();
        movementPhaseManager.ApplyThrowInRestrictions(selectedThrower, throwInHex, ThrowInProtectedRadius, blockTackleWithoutMoving: true);
        movementPhaseManager.ActivateMovementPhase();
        movementPhaseManager.CommitToAction();
        while (movementPhaseManager.isActivated)
        {
            yield return null;
        }
        movementPhaseManager.ClearThrowInRestrictions();
        isRunningMandatoryMovement = false;
        isWaitingForOptionalMovementDecision = true;
        Debug.Log("Throw-in mandatory movement completed. Press [M] for one extra movement phase or [T] to throw now.");
    }

    private IEnumerator RunOptionalMovementPhaseThenThrow()
    {
        isRunningOptionalMovement = true;
        movementPhaseManager.ResetMovementPhase();
        movementPhaseManager.ApplyThrowInRestrictions(selectedThrower, throwInHex, ThrowInProtectedRadius);
        movementPhaseManager.ActivateMovementPhase();
        movementPhaseManager.CommitToAction();
        while (movementPhaseManager.isActivated)
        {
            yield return null;
        }
        movementPhaseManager.ClearThrowInRestrictions();
        isRunningOptionalMovement = false;
        EnterThrowExecutionSelection();
    }

    private void EnterThrowExecutionSelection()
    {
        isWaitingForThrowTypeSelection = true;
        MatchManager.Instance.currentState = MatchManager.GameState.WaitingForThrowInTaker;
        Debug.Log(GetAvailableHeaderThrowTargets().Count > 0
            ? "Throw-in ready. Press [P] to throw to feet or [C] to throw to head."
            : "Throw-in ready. Press [P] to throw to feet.");
    }

    private void HandleThrowToFeet()
    {
        isWaitingForThrowTypeSelection = false;
        isWaitingForGroundTarget = false;
        groundBallManager.imposedDistance = maxThrowDistance;
        groundBallManager.ActivateGroundBall();
        MatchManager.Instance.CommitToAction();
        StartCoroutine(RestoreGroundBallDefaultDistanceWhenDone());
        Debug.Log("Throw-in option selected: [P] to feet. Select target hex (up to 6).");
        ResetThrowInState();
    }

    private IEnumerator RestoreGroundBallDefaultDistanceWhenDone()
    {
        while (groundBallManager.isActivated || groundBallManager.isAwaitingTargetSelection || groundBallManager.isWaitingForDiceRoll)
        {
            yield return null;
        }
        groundBallManager.imposedDistance = DefaultGroundBallDistance;
    }

    private IEnumerator CommitThrowToHead(HexCell targetHex)
    {
        isWaitingForHeaderTarget = false;
        currentHeaderThrowTarget = targetHex;
        MatchManager.Instance.CommitToAction();
        MatchManager.Instance.hangingPassType = "aerial";
        yield return StartCoroutine(ball.MoveToCell(targetHex));
        finalThirdManager.TriggerFinalThirdPhase();
        StartCoroutine(headerManager.FindEligibleHeaderTokens(targetHex));
        ResetThrowInState();
    }

    private IEnumerator ResolveThrowInSpotOccupancy(PlayerToken incomingThrower)
    {
        PlayerToken occupyingToken = throwInHex != null ? throwInHex.GetOccupyingToken() : null;
        if (occupyingToken == null || occupyingToken == incomingThrower)
        {
            yield break;
        }

        EnsureOutOfBoundsPushManager();
        if (outOfBoundsPushManager == null)
        {
            Debug.LogError("ThrowInManager cannot resolve occupied throw-in spot because OutOfBoundsPushManager is not linked.");
            yield break;
        }

        if (occupyingToken.isAttacker)
        {
            yield return StartCoroutine(outOfBoundsPushManager.ResolveAttackerOnRestartSpotPush(throwInHex));
        }
        else
        {
            yield return StartCoroutine(outOfBoundsPushManager.ResolveOutOfBoundsPush(throwInHex));
        }
    }

    private bool IsTokenEligibleThrower(PlayerToken token)
    {
        if (token == null)
        {
            return false;
        }

        bool isAwardedHome = awardedTeam == MatchManager.TeamInAttack.Home;
        return token.isHomeTeam == isAwardedHome;
    }

    private bool IsValidHeaderThrowTarget(HexCell targetHex)
    {
        if (targetHex == null || selectedThrower == null)
        {
            return false;
        }

        HexCell throwerHex = selectedThrower.GetCurrentHex();
        if (throwerHex == null)
        {
            return false;
        }

        int distanceFromThrower = HexGridUtils.GetHexStepDistance(throwerHex, targetHex);
        if (distanceFromThrower > maxThrowDistance)
        {
            return false;
        }

        PlayerToken targetToken = targetHex.GetOccupyingToken();
        return targetToken != null
            && targetToken != selectedThrower
            && targetToken.isAttacker
            && IsTokenEligibleThrower(targetToken);
    }

    private List<HexCell> GetAvailableHeaderThrowTargets()
    {
        if (selectedThrower == null || hexGrid == null || throwInHex == null)
        {
            return new List<HexCell>();
        }

        return HexGrid.GetHexesInRange(hexGrid, throwInHex, maxThrowDistance)
            .Where(IsValidHeaderThrowTarget)
            .OrderBy(hex => HexGridUtils.GetHexStepDistance(throwInHex, hex))
            .ThenBy(hex => hex.coordinates.x)
            .ThenBy(hex => hex.coordinates.z)
            .ToList();
    }

    private void HighlightHeaderThrowTargets(List<HexCell> targets)
    {
        hexGrid.ClearHighlightedHexes();
        foreach (HexCell target in targets)
        {
            target.HighlightHex(target == currentHeaderThrowTarget ? "passTargetCommitted" : "passTarget");
            if (!hexGrid.highlightedHexes.Contains(target))
            {
                hexGrid.highlightedHexes.Add(target);
            }
        }
    }

    private IEnumerator MoveTokenToHex(PlayerToken token, HexCell targetHex)
    {
        if (token == null || targetHex == null)
        {
            yield break;
        }

        HexCell tokenHex = token.GetCurrentHex();
        if (tokenHex != null)
        {
            tokenHex.isAttackOccupied = false;
            tokenHex.isDefenseOccupied = false;
            tokenHex.ResetHighlight();
        }

        if (token.isAttacker)
        {
            targetHex.isAttackOccupied = true;
        }
        else
        {
            targetHex.isDefenseOccupied = true;
        }

        yield return StartCoroutine(token.JumpToHex(targetHex));
        ball.AdjustBallHeightBasedOnOccupancy();
    }

    private void EnsureAwardedTeamInAttack()
    {
        if (MatchManager.Instance.teamInAttack != awardedTeam)
        {
            MatchManager.Instance.ChangePossession();
        }
    }

    private void EnsureOutOfBoundsPushManager()
    {
        if (outOfBoundsPushManager == null)
        {
            outOfBoundsPushManager = FindAnyObjectByType<OutOfBoundsPushManager>();
        }

        outOfBoundsPushManager?.Configure(hexGrid, ball);
    }

    private void ResetThrowInState()
    {
        isActivated = false;
        isWaitingForTakerSelection = false;
        isRunningMandatoryMovement = false;
        isWaitingForOptionalMovementDecision = false;
        isRunningOptionalMovement = false;
        isWaitingForThrowTypeSelection = false;
        isWaitingForGroundTarget = false;
        isWaitingForHeaderTarget = false;
        currentHeaderThrowTarget = null;
    }

    public string GetDebugStatus()
    {
        StringBuilder sb = new();
        sb.Append("TI: ");

        if (isActivated) sb.Append("isActivated, ");
        if (isWaitingForTakerSelection) sb.Append("isWaitingForTakerSelection, ");
        if (isRunningMandatoryMovement) sb.Append("isRunningMandatoryMovement, ");
        if (isWaitingForOptionalMovementDecision) sb.Append("isWaitingForOptionalMovementDecision, ");
        if (isRunningOptionalMovement) sb.Append("isRunningOptionalMovement, ");
        if (isWaitingForThrowTypeSelection) sb.Append("isWaitingForThrowTypeSelection, ");
        if (isWaitingForGroundTarget) sb.Append("isWaitingForGroundTarget, ");
        if (isWaitingForHeaderTarget) sb.Append("isWaitingForHeaderTarget, ");
        if (throwInHex != null) sb.Append($"throwInHex: {throwInHex.name}, ");
        if (selectedThrower != null) sb.Append($"selectedThrower: {selectedThrower.name}, ");
        if (currentHeaderThrowTarget != null) sb.Append($"currentHeaderThrowTarget: {currentHeaderThrowTarget.name}, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2;
        return sb.ToString();
    }

    public string GetInstructions()
    {
        if (!isActivated) return string.Empty;
        StringBuilder sb = new();
        sb.Append("TI: ");

        if (isWaitingForTakerSelection) sb.Append("Select a player from awarded team to take throw-in, ");
        if (isWaitingForOptionalMovementDecision) sb.Append("Press [M] for optional movement phase or [T] to throw now, ");
        if (isWaitingForThrowTypeSelection)
        {
            int headerTargetCount = GetAvailableHeaderThrowTargets().Count;
            sb.Append(headerTargetCount > 0
                ? "Press [P] for throw to feet or [C] for throw to head, "
                : "Press [P] for throw to feet, ");
        }
        if (isWaitingForGroundTarget) sb.Append("Select throw-to-feet target (up to 6 hexes), ");
        if (isWaitingForHeaderTarget)
        {
            sb.Append(MatchManager.Instance != null && MatchManager.Instance.difficulty_level == 3
                ? "Select an eligible attacker within 6 for throw-to-head, "
                : "Select an eligible attacker within 6, then click again to confirm throw-to-head, ");
        }

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2;
        return sb.ToString();
    }

    public bool? IsInstructionExpectingHomeTeam()
    {
        if (!isActivated)
        {
            return null;
        }

        return awardedTeam == MatchManager.TeamInAttack.Home;
    }
}
