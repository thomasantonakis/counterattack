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
    public MatchManager.TeamInAttack awardedTeam;
    public int maxThrowDistance = 6;
    public int maxHeaderLooseOffsetFromAttacker = 1;

    private const int DefaultGroundBallDistance = 11;

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

        if (isWaitingForGroundTarget && hex != null)
        {
            HandleThrowToFeet();
            return;
        }

        if (isWaitingForHeaderTarget && hex != null)
        {
            if (!IsValidHeaderThrowTarget(hex))
            {
                Debug.Log($"Invalid throw-to-head target {hex.coordinates}. Choose a hex within 6 of thrower and close to an attacker.");
                return;
            }

            StartCoroutine(HandleThrowToHead(hex));
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

            if (keyData.key == KeyCode.X)
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
                isWaitingForThrowTypeSelection = false;
                isWaitingForGroundTarget = true;
                Debug.Log("Throw-in option selected: [P] to feet. Select target hex (up to 6).");
                return;
            }

            if (keyData.key == KeyCode.C)
            {
                keyData.isConsumed = true;
                isWaitingForThrowTypeSelection = false;
                isWaitingForHeaderTarget = true;
                Debug.Log("Throw-in option selected: [C] to head. Select target hex (up to 6 and near attacker).");
                return;
            }
        }
    }

    private IEnumerator HandleThrowerSelection(PlayerToken token)
    {
        selectedThrower = token;
        yield return StartCoroutine(MoveTokenToHex(selectedThrower, throwInHex));
        ball.PlaceAtCell(throwInHex);
        MatchManager.Instance.SetLastToken(selectedThrower);
        isWaitingForTakerSelection = false;
        Debug.Log($"{selectedThrower.name} moved to throw-in hex at {throwInHex.coordinates}.");
        StartCoroutine(RunMandatoryMovementPhaseThenPromptOptional());
    }

    private IEnumerator RunMandatoryMovementPhaseThenPromptOptional()
    {
        isRunningMandatoryMovement = true;
        movementPhaseManager.ResetMovementPhase();
        movementPhaseManager.ActivateMovementPhase();
        movementPhaseManager.CommitToAction();
        while (movementPhaseManager.isActivated)
        {
            yield return null;
        }
        isRunningMandatoryMovement = false;
        isWaitingForOptionalMovementDecision = true;
        Debug.Log("Throw-in mandatory movement completed. Press [M] for one extra movement phase or [X] to throw now.");
    }

    private IEnumerator RunOptionalMovementPhaseThenThrow()
    {
        isRunningOptionalMovement = true;
        movementPhaseManager.ResetMovementPhase();
        movementPhaseManager.ActivateMovementPhase();
        movementPhaseManager.CommitToAction();
        while (movementPhaseManager.isActivated)
        {
            yield return null;
        }
        isRunningOptionalMovement = false;
        EnterThrowExecutionSelection();
    }

    private void EnterThrowExecutionSelection()
    {
        isWaitingForThrowTypeSelection = true;
        MatchManager.Instance.currentState = MatchManager.GameState.WaitingForThrowInTaker;
        Debug.Log("Throw-in ready. Press [P] to throw to feet or [C] to throw to head.");
    }

    private void HandleThrowToFeet()
    {
        isWaitingForGroundTarget = false;
        groundBallManager.imposedDistance = maxThrowDistance;
        groundBallManager.ActivateGroundBall();
        MatchManager.Instance.CommitToAction();
        StartCoroutine(RestoreGroundBallDefaultDistanceWhenDone());
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

    private IEnumerator HandleThrowToHead(HexCell targetHex)
    {
        isWaitingForHeaderTarget = false;
        MatchManager.Instance.hangingPassType = "aerial";
        yield return StartCoroutine(ball.MoveToCell(targetHex));
        finalThirdManager.TriggerFinalThirdPhase();
        StartCoroutine(headerManager.FindEligibleHeaderTokens(targetHex));
        ResetThrowInState();
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

        List<HexCell> nearbyHexes = HexGrid.GetHexesInRange(hexGrid, targetHex, maxHeaderLooseOffsetFromAttacker);
        bool hasNearbyAwardedAttacker = nearbyHexes.Any(hex =>
        {
            PlayerToken token = hex.GetOccupyingToken();
            return token != null && IsTokenEligibleThrower(token);
        });

        return hasNearbyAwardedAttacker;
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

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2;
        return sb.ToString();
    }

    public string GetInstructions()
    {
        if (!isActivated) return string.Empty;
        StringBuilder sb = new();
        sb.Append("TI: ");

        if (isWaitingForTakerSelection) sb.Append("Select a player from awarded team to take throw-in, ");
        if (isWaitingForOptionalMovementDecision) sb.Append("Press [M] for optional movement phase or [X] to throw now, ");
        if (isWaitingForThrowTypeSelection) sb.Append("Press [P] for throw to feet or [C] for throw to head, ");
        if (isWaitingForGroundTarget) sb.Append("Select throw-to-feet target (up to 6 hexes), ");
        if (isWaitingForHeaderTarget) sb.Append("Select throw-to-head target (up to 6, near attacker), ");

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
