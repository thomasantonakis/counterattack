using System.Collections;
using System.Linq;
using UnityEngine;

public class KickoffManager : MonoBehaviour
{
    private enum KickoffFlowPhase
    {
        None,
        InitialSetup,
        PostGoalSetup,
        TakerSelection
    }

    public PlayerTokenManager playerTokenManager;
    public HexGrid hexGrid;
    public Ball ball;
    public FreeKickManager freeKickManager;
    public GroundBallManager groundBallManager;
    public MovementPhaseManager movementPhaseManager;
    public HeaderManager headerManager;
    public PlayerToken selectedToken;
    [Header("Runtime Items")]
    private bool isActivated = false;
    private int setupConfirmCount = 0;
    private KickoffFlowPhase flowPhase = KickoffFlowPhase.None;
    private bool isMovingSetupToken = false;
    private bool isMovingKickoffTaker = false;

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

    private void OnClickReceived(PlayerToken token, HexCell hex)
    {
        if (!isActivated) return;

        MatchManager.GameState currentState = MatchManager.Instance.currentState;
        if (currentState == MatchManager.GameState.KickOffSetup)
        {
            HandleInitialSetupClick(token, hex);
            return;
        }

        if (currentState == MatchManager.GameState.PostGoalKickOffSetup)
        {
            if (isMovingSetupToken)
            {
                return;
            }

            HandlePostGoalSetupClick(token, hex);
            return;
        }

        if (currentState == MatchManager.GameState.KickOffTakerSelection)
        {
            HandleKickoffTakerSelectionClick(token);
            return;
        }

        DeactivateSetup();
    }

    private void HandleInitialSetupClick(PlayerToken token, HexCell hex)
    {
        if (token != null && token != selectedToken)
        {
            SelectToken(token);
        }
        else if (hex != null)
        {
            StartCoroutine(TryMoveInitialSetupToken(hex));
        }
    }

    private void HandlePostGoalSetupClick(PlayerToken token, HexCell hex)
    {
        if (token != null && token != selectedToken)
        {
            SelectToken(token);
            return;
        }

        if (hex != null)
        {
            StartCoroutine(TryMovePostGoalSetupToken(hex));
        }
    }

    private void HandleKickoffTakerSelectionClick(PlayerToken token)
    {
        if (isMovingKickoffTaker)
        {
            return;
        }

        if (token == null)
        {
            Debug.LogWarning("Select an attacking token to take the kick-off.");
            return;
        }

        if (!token.isAttacker)
        {
            Debug.LogWarning($"{token.name} is defending and cannot take the kick-off.");
            return;
        }

        StartCoroutine(SelectKickoffTakerAndStartPass(token));
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (keyData.isConsumed) return;
        if (!isActivated) return;
        MatchManager.GameState currentState = MatchManager.Instance.currentState;
        if (currentState != MatchManager.GameState.KickOffSetup
            && currentState != MatchManager.GameState.PostGoalKickOffSetup
            && currentState != MatchManager.GameState.KickOffTakerSelection)
        {
            DeactivateSetup();
            return;
        }
        if (currentState == MatchManager.GameState.KickOffSetup && keyData.key == KeyCode.Space)
        {
            ConfirmInitialSetup();
            keyData.isConsumed = true;
            return;
        }

        if (currentState == MatchManager.GameState.PostGoalKickOffSetup
            && (keyData.key == KeyCode.Return || keyData.key == KeyCode.KeypadEnter))
        {
            ConfirmPostGoalSetup();
            keyData.isConsumed = true;
        }
    }

    public void StartPreKickoffPhase()
    {
        isActivated = true;
        flowPhase = KickoffFlowPhase.InitialSetup;
        Debug.Log("Pre-Kickoff Formation: Click tokens to reposition them. Press Space twice to start!");
        setupConfirmCount = 0;
    }

    public void StartPostGoalKickoffSetupPhase()
    {
        isActivated = true;
        flowPhase = KickoffFlowPhase.PostGoalSetup;
        setupConfirmCount = 0;
        selectedToken = null;
        Debug.Log("Post-goal kick-off setup: both teams may reposition tokens in their own half, including the halfway line, except the center spot. The kicker will be selected later. Defenders cannot stand in the center circle. Press Enter twice when both teams are ready.");
    }

    private void SelectToken(PlayerToken token)
    {
        if (selectedToken != null)
        {
            Debug.Log($"Deselecting {selectedToken.name}");
        }
        if (token != selectedToken && selectedToken != null) Debug.Log($"Switching Selected token to {token.name}");
        else Debug.Log($"Selecting token {token.name}");
        selectedToken = token;
        
    }

    private IEnumerator TryMoveInitialSetupToken(HexCell targetHex)
    {
        if (selectedToken == null)
        {
            Debug.LogWarning($"Please select a Token first");
            yield break;
        }
        else
        {
            HexCell currentHex = selectedToken.GetCurrentHex();
            bool isSameHalf = (currentHex.coordinates.x * targetHex.coordinates.x) >= 0;

            if (!isSameHalf)
            {
                Debug.LogWarning($"{selectedToken.name} cannot move outside their half!");
                yield break;
            }
            if (!selectedToken.isAttacker && targetHex.isInCircle == 5)
            {
                Debug.LogWarning($"Defenders should not be placed on the KickOff Circle!");
                yield break;
            }
            yield return StartCoroutine(freeKickManager.MoveTokenToHex(selectedToken, targetHex));
            Debug.Log($"{selectedToken.name} moved to {targetHex.coordinates}");
            selectedToken = null; // Deselect after moving
        }
    }

    private IEnumerator TryMovePostGoalSetupToken(HexCell targetHex)
    {
        if (selectedToken == null)
        {
            Debug.LogWarning("Please select a token first.");
            yield break;
        }

        if (!IsValidPostGoalSetupDestination(selectedToken, targetHex))
        {
            yield break;
        }

        HexCell currentHex = selectedToken.GetCurrentHex();
        if (currentHex == targetHex)
        {
            selectedToken = null;
            yield break;
        }

        isMovingSetupToken = true;
        yield return StartCoroutine(freeKickManager.MoveTokenToHex(selectedToken, targetHex));
        isMovingSetupToken = false;
        Debug.Log($"{selectedToken.name} moved to {targetHex.coordinates}");
        selectedToken = null;
    }

    private bool IsValidPostGoalSetupDestination(PlayerToken token, HexCell targetHex)
    {
        if (token == null || targetHex == null)
        {
            Debug.LogWarning("Please select a token and a valid destination hex.");
            return false;
        }

        if (targetHex.isOutOfBounds || targetHex.isInGoal != 0)
        {
            Debug.LogWarning($"{targetHex.coordinates} is not a valid kick-off setup destination.");
            return false;
        }

        if (targetHex.coordinates.x == 0 && targetHex.coordinates.z == 0)
        {
            Debug.LogWarning("The kick-off hex (0, 0) must stay empty until the taker is selected.");
            return false;
        }

        if (!token.isAttacker && targetHex.isInCircle == 5)
        {
            Debug.LogWarning($"{token.name} is defending and cannot stand in the center circle for kick-off setup.");
            return false;
        }

        PlayerToken occupyingToken = targetHex.GetOccupyingToken();
        bool occupiedByAnotherToken = occupyingToken != null && occupyingToken != token;
        if (occupiedByAnotherToken || (targetHex.isAttackOccupied || targetHex.isDefenseOccupied) && occupyingToken != token)
        {
            Debug.LogWarning($"{targetHex.coordinates} is occupied. Select an empty hex.");
            return false;
        }

        if (!IsTeamOwnHalfOrMidline(token, targetHex))
        {
            Debug.LogWarning($"{token.name} can only move in their own half, including the halfway line.");
            return false;
        }

        return true;
    }

    private bool IsTeamOwnHalfOrMidline(PlayerToken token, HexCell targetHex)
    {
        if (token == null || targetHex == null)
        {
            return false;
        }

        if (targetHex.coordinates.x == 0)
        {
            return true;
        }

        MatchManager matchManager = MatchManager.Instance;
        MatchManager.TeamAttackingDirection teamDirection = token.isHomeTeam
            ? matchManager.homeTeamDirection
            : matchManager.awayTeamDirection;

        return teamDirection == MatchManager.TeamAttackingDirection.LeftToRight
            ? targetHex.coordinates.x <= 0
            : targetHex.coordinates.x >= 0;
    }

    private void ConfirmInitialSetup()
    {
        setupConfirmCount++;
        Debug.Log($"Player confirmed setup ({setupConfirmCount}/2)");

        if (setupConfirmCount >= 2)
        {
            ValidateAndStartGame();
        }
    }

    private void ConfirmPostGoalSetup()
    {
        if (isMovingSetupToken)
        {
            Debug.LogWarning("Wait for the current setup move to finish before confirming kick-off setup.");
            return;
        }

        selectedToken = null;
        setupConfirmCount++;
        Debug.Log($"Post-goal kick-off setup confirmed ({setupConfirmCount}/2)");

        if (setupConfirmCount >= 2)
        {
            ShotManager shotManager = MatchManager.Instance != null
                ? MatchManager.Instance.shotManager
                : FindAnyObjectByType<ShotManager>();
            if (shotManager != null && shotManager.HasDeferredShotActionResolution)
            {
                shotManager.CompleteDeferredShotActionResolution(BeginKickoffTakerSelection);
                return;
            }

            if (MatchManager.Instance != null
                && MatchManager.Instance.TryEnterExtraActionsRollBeforeRestart(BeginKickoffTakerSelection))
            {
                return;
            }

            BeginKickoffTakerSelection();
        }
    }

    private void BeginKickoffTakerSelection()
    {
        MatchManager.Instance?.ClearGoalKickRestartTaker();
        setupConfirmCount = 0;
        selectedToken = null;
        flowPhase = KickoffFlowPhase.TakerSelection;
        MatchManager.Instance.currentState = MatchManager.GameState.KickOffTakerSelection;
        Debug.Log("Kick-off taker selection: select an attacking token to move to (0,0).");
    }

    private void ValidateAndStartGame()
    {
        bool hasAttackerOnKickoff = playerTokenManager.allTokens
            .Any(t => t.isAttacker && t.GetCurrentHex() == ball.GetCurrentHex());

        if (!hasAttackerOnKickoff)
        {
            Debug.LogWarning("An attacker must be on the kick-off hex to start the game!");
            setupConfirmCount = 1; // Reset to wait for one more press after correction
            return;
        }

        Debug.Log("Kick-off confirmed! The match begins.");
        movementPhaseManager.ResetMovementPhase();
        headerManager.ResetHeader();
        MatchManager.Instance.SetLastToken(ball.GetCurrentHex().GetOccupyingToken());
        DeactivateSetup();
        MatchManager.Instance.StartMatch();
    }

    private IEnumerator SelectKickoffTakerAndStartPass(PlayerToken taker)
    {
        HexCell kickoffHex = hexGrid.GetHexCellAt(new Vector3Int(0, 0, 0));
        if (kickoffHex == null)
        {
            Debug.LogError("Cannot start kick-off pass because hex (0,0) was not found.");
            yield break;
        }

        PlayerToken occupyingToken = kickoffHex.GetOccupyingToken();
        if (occupyingToken != null && occupyingToken != taker)
        {
            Debug.LogError($"Cannot move {taker.name} to kick-off because {occupyingToken.name} is already on (0,0).");
            yield break;
        }

        isMovingKickoffTaker = true;
        selectedToken = taker;
        yield return StartCoroutine(freeKickManager.MoveTokenToHex(taker, kickoffHex));
        ball.PlaceAtCell(kickoffHex);
        movementPhaseManager.ResetMovementPhase();
        headerManager.ResetHeader();
        MatchManager.Instance.ClearLastTokenChain();
        MatchManager.Instance.SetLastToken(taker);
        GroundBallManager resolvedGroundBallManager = groundBallManager != null
            ? groundBallManager
            : MatchManager.Instance.groundBallManager;
        if (resolvedGroundBallManager == null)
        {
            Debug.LogError("Cannot start kick-off pass because GroundBallManager is not linked.");
            isMovingKickoffTaker = false;
            yield break;
        }

        resolvedGroundBallManager.ActivateKickoffGroundBall(taker);
        isMovingKickoffTaker = false;
        DeactivateSetup();
    }

    private void DeactivateSetup()
    {
        isActivated = false;
        selectedToken = null;
        setupConfirmCount = 0;
        flowPhase = KickoffFlowPhase.None;
        isMovingSetupToken = false;
        isMovingKickoffTaker = false;
    }

    public string GetInstructions()
    {
        if (!isActivated)
        {
            return string.Empty;
        }

        return flowPhase switch
        {
            KickoffFlowPhase.PostGoalSetup => selectedToken != null
                ? $"Post-goal setup: click an empty hex in {selectedToken.name}'s own half, including the halfway line, except the center spot. The kicker will be selected later. Defenders cannot stand in the center circle. Press [Enter] twice when both teams are ready ({setupConfirmCount}/2)."
                : $"Post-goal setup: click any token to reposition it in its own half, including the halfway line, except the center spot. The kicker will be selected later. Defenders cannot stand in the center circle. Press [Enter] twice when both teams are ready ({setupConfirmCount}/2).",
            KickoffFlowPhase.TakerSelection => "Kick-off taker selection: click an attacking token to move it to (0,0) and start the committed kick-off ground pass.",
            KickoffFlowPhase.InitialSetup => $"Pre-kickoff setup: click tokens to reposition them. Press [Space] twice to start ({setupConfirmCount}/2).",
            _ => string.Empty,
        };
    }

    public bool? IsInstructionExpectingHomeTeam()
    {
        if (!isActivated || MatchManager.Instance == null)
        {
            return null;
        }

        if (flowPhase == KickoffFlowPhase.TakerSelection)
        {
            return MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home;
        }

        return null;
    }
}
