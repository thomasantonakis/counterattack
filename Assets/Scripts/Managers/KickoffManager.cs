using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class KickoffManager : MonoBehaviour
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
    private bool homeSetupConfirmed = false;
    private bool awaySetupConfirmed = false;
    private string pendingAutomatedConfirmationTeam = string.Empty;
    private string pendingAutomatedInputTeam = string.Empty;
    private KickoffFlowPhase flowPhase = KickoffFlowPhase.None;
    private bool isMovingSetupToken = false;
    private bool isMovingKickoffTaker = false;
    private readonly Dictionary<string, HashSet<string>> automatedVisitedSetupStates = new();
    private readonly Dictionary<string, string> automatedPreviousSetupState = new();
    private readonly Dictionary<string, int> automatedSetupMoveCounts = new();
    private readonly Dictionary<string, HashSet<PlayerToken>> automatedMovedActors = new();
    private readonly Dictionary<string, RoomAIKickoffPlan> automatedOpeningPlans = new();
    private const int AutomatedSetupMoveBudget = 24;

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
            if (isMovingSetupToken)
            {
                RegisterNextSetupSelection(token);
                return;
            }

            HandleInitialSetupClick(token, hex);
            return;
        }

        if (currentState == MatchManager.GameState.PostGoalKickOffSetup)
        {
            if (isMovingSetupToken)
            {
                RegisterNextSetupSelection(token);
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

    private void RegisterNextSetupSelection(PlayerToken token)
    {
        if (token == null || !token.isPlaying)
        {
            return;
        }

        // Occupancy is committed only when the current animation completes. A token
        // click during that window therefore means "select this player next", never
        // "swap with the player that is still moving".
        SelectToken(token);
        Debug.Log($"Queued {token.name} as the next kick-off setup selection while another token is moving.");
    }

    private void HandleInitialSetupClick(PlayerToken token, HexCell hex)
    {
        if (token != null)
        {
            if (!CanCurrentControllerSelectSetupToken(token))
            {
                return;
            }
            if (TrySwapSelectedWithTeammate(token, isPostGoalSetup: false))
            {
                return;
            }

            if (token == selectedToken)
            {
                return;
            }

            SelectToken(token);
            return;
        }

        if (hex != null)
        {
            StartCoroutine(TryMoveInitialSetupToken(hex));
        }
    }

    private void HandlePostGoalSetupClick(PlayerToken token, HexCell hex)
    {
        if (token != null)
        {
            if (!CanCurrentControllerSelectSetupToken(token))
            {
                return;
            }
            if (TrySwapSelectedWithTeammate(token, isPostGoalSetup: true))
            {
                return;
            }

            if (token == selectedToken)
            {
                return;
            }

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
        if (currentState == MatchManager.GameState.KickOffSetup
            && (keyData.key == KeyCode.Return || keyData.key == KeyCode.KeypadEnter))
        {
            ConfirmInitialSetup();
            keyData.Consume(nameof(KickoffManager));
            return;
        }

        if (currentState == MatchManager.GameState.PostGoalKickOffSetup
            && (keyData.key == KeyCode.Return || keyData.key == KeyCode.KeypadEnter))
        {
            ConfirmPostGoalSetup();
            keyData.Consume(nameof(KickoffManager));
        }
    }

    public void StartPreKickoffPhase()
    {
        isActivated = true;
        flowPhase = KickoffFlowPhase.InitialSetup;
        Debug.Log("Pre-Kickoff Formation: Click tokens to reposition them. Press Enter twice to continue to kick-off taker selection.");
        setupConfirmCount = 0;
        ResetSetupConfirmations();
    }

    public void StartPostGoalKickoffSetupPhase()
    {
        isActivated = true;
        flowPhase = KickoffFlowPhase.PostGoalSetup;
        setupConfirmCount = 0;
        ResetSetupConfirmations();
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
            PlayerToken movingToken = selectedToken;
            if (!IsValidInitialSetupDestination(movingToken, targetHex))
            {
                yield break;
            }

            selectedToken = null;
            isMovingSetupToken = true;
            yield return StartCoroutine(freeKickManager.MoveTokenToHex(movingToken, targetHex));
            isMovingSetupToken = false;
            Debug.Log($"{movingToken.name} moved to {targetHex.coordinates}");
        }
    }

    private bool TrySwapSelectedWithTeammate(PlayerToken teammate, bool isPostGoalSetup)
    {
        if (selectedToken == null || teammate == null || teammate == selectedToken)
        {
            return false;
        }

        if (!IsTeammate(selectedToken, teammate))
        {
            return false;
        }

        HexCell selectedHex = selectedToken.GetCurrentHex();
        HexCell teammateHex = teammate.GetCurrentHex();
        if (selectedHex == null || teammateHex == null)
        {
            Debug.LogWarning("Both players must be on the pitch to swap kick-off setup positions.");
            return true;
        }

        bool isValidSwap = isPostGoalSetup
            ? IsValidPostGoalSetupDestination(selectedToken, teammateHex, teammate)
                && IsValidPostGoalSetupDestination(teammate, selectedHex, selectedToken)
            : IsValidInitialSetupDestination(selectedToken, teammateHex, teammate)
                && IsValidInitialSetupDestination(teammate, selectedHex, selectedToken);

        if (!isValidSwap)
        {
            return true;
        }

        StartCoroutine(AnimateAndSwapSelectedTokenWithTeammate(teammate, selectedHex, teammateHex));
        return true;
    }

    private bool IsTeammate(PlayerToken first, PlayerToken second)
    {
        return first != null
            && second != null
            && first != second
            && first.isHomeTeam == second.isHomeTeam;
    }

    private bool IsValidInitialSetupDestination(PlayerToken token, HexCell targetHex, PlayerToken allowedOccupant = null)
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

        PlayerToken occupyingToken = targetHex.GetOccupyingToken();
        if (occupyingToken != null && occupyingToken != token && occupyingToken != allowedOccupant)
        {
            Debug.LogWarning($"{targetHex.coordinates} is occupied. Select an empty hex or a teammate to swap with.");
            return false;
        }

        if (!IsTeamOwnHalfOrMidline(token, targetHex))
        {
            Debug.LogWarning($"{token.name} cannot move outside their half!");
            return false;
        }

        if (!token.isAttacker && targetHex.isInCircle == 5)
        {
            Debug.LogWarning("Defenders should not be placed on the KickOff Circle!");
            return false;
        }

        return true;
    }

    private IEnumerator AnimateAndSwapSelectedTokenWithTeammate(PlayerToken teammate, HexCell selectedHex, HexCell teammateHex)
    {
        PlayerToken firstToken = selectedToken;
        PlayerToken secondToken = teammate;
        selectedToken = null;
        isMovingSetupToken = true;

        Coroutine firstMove = StartCoroutine(AnimateTokenToHex(firstToken, teammateHex));
        yield return StartCoroutine(AnimateTokenToHex(secondToken, selectedHex));
        yield return firstMove;

        CommitSetupSwap(firstToken, secondToken, selectedHex, teammateHex);
        isMovingSetupToken = false;
    }

    private IEnumerator AnimateTokenToHex(PlayerToken token, HexCell targetHex)
    {
        if (token == null || targetHex == null)
        {
            yield break;
        }

        using var movement = token.TrackMovement();
        Vector3 startPosition = token.transform.position;
        Vector3 targetPosition = targetHex.GetHexCenter();
        targetPosition.y = Mathf.Max(targetPosition.y, startPosition.y);

        const float travelDuration = 1.0f;
        const float jumpHeight = 1.0f;
        float elapsedTime = 0f;

        while (elapsedTime < travelDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / travelDuration);
            Vector3 flatPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            flatPosition.y += jumpHeight * Mathf.Sin(Mathf.PI * progress);
            token.transform.position = flatPosition;
            yield return null;
        }

        token.transform.position = targetPosition;
    }

    private void CommitSetupSwap(PlayerToken firstToken, PlayerToken secondToken, HexCell selectedHex, HexCell teammateHex)
    {
        if (firstToken == null || secondToken == null || selectedHex == null || teammateHex == null)
        {
            return;
        }

        bool firstWasAttacker = firstToken.isAttacker;
        bool secondWasAttacker = secondToken.isAttacker;

        ClearHexOccupancy(selectedHex);
        ClearHexOccupancy(teammateHex);
        ApplyOccupancyFlag(selectedHex, secondWasAttacker);
        ApplyOccupancyFlag(teammateHex, firstWasAttacker);
        selectedHex.occupyingToken = secondToken;
        teammateHex.occupyingToken = firstToken;

        firstToken.ApplySetupSwapHex(teammateHex, "kickoff_setup_swap");
        secondToken.ApplySetupSwapHex(selectedHex, "kickoff_setup_swap");
        SnapTokenToHex(firstToken, teammateHex);
        SnapTokenToHex(secondToken, selectedHex);
        RefreshHexHighlight(selectedHex);
        RefreshHexHighlight(teammateHex);
        ball?.AdjustBallHeightBasedOnOccupancy();

        Debug.Log($"Swapped kick-off setup positions: {firstToken.name} <-> {secondToken.name}.");
    }

    private void ClearHexOccupancy(HexCell hex)
    {
        if (hex == null)
        {
            return;
        }

        hex.occupyingToken = null;
        hex.isAttackOccupied = false;
        hex.isDefenseOccupied = false;
    }

    private void ApplyOccupancyFlag(HexCell hex, bool isAttacker)
    {
        if (hex == null)
        {
            return;
        }

        hex.isAttackOccupied = isAttacker;
        hex.isDefenseOccupied = !isAttacker;
    }

    private void SnapTokenToHex(PlayerToken token, HexCell hex)
    {
        if (token == null || hex == null)
        {
            return;
        }

        Vector3 hexCenter = hex.GetHexCenter();
        token.transform.position = new Vector3(hexCenter.x, token.transform.position.y, hexCenter.z);
    }

    private void RefreshHexHighlight(HexCell hex)
    {
        if (hex == null)
        {
            return;
        }

        if (hex.isAttackOccupied)
        {
            hex.HighlightHex("isAttackOccupied");
        }
        else if (hex.isDefenseOccupied)
        {
            hex.HighlightHex("isDefenseOccupied");
        }
        else
        {
            hex.ResetHighlight();
        }
    }

    private IEnumerator TryMovePostGoalSetupToken(HexCell targetHex)
    {
        if (selectedToken == null)
        {
            Debug.LogWarning("Please select a token first.");
            yield break;
        }

        PlayerToken movingToken = selectedToken;
        if (!IsValidPostGoalSetupDestination(movingToken, targetHex))
        {
            yield break;
        }

        HexCell currentHex = movingToken.GetCurrentHex();
        if (currentHex == targetHex)
        {
            selectedToken = null;
            yield break;
        }

        selectedToken = null;
        isMovingSetupToken = true;
        yield return StartCoroutine(freeKickManager.MoveTokenToHex(movingToken, targetHex));
        isMovingSetupToken = false;
        Debug.Log($"{movingToken.name} moved to {targetHex.coordinates}");
    }

    private bool IsValidPostGoalSetupDestination(PlayerToken token, HexCell targetHex, PlayerToken allowedOccupant = null)
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
        bool occupiedByAnotherToken = occupyingToken != null
            && occupyingToken != token
            && occupyingToken != allowedOccupant;
        bool hasUnknownOccupancy = (targetHex.isAttackOccupied || targetHex.isDefenseOccupied)
            && occupyingToken != token
            && occupyingToken != allowedOccupant;
        if (occupiedByAnotherToken || hasUnknownOccupancy)
        {
            Debug.LogWarning($"{targetHex.coordinates} is occupied. Select an empty hex or a teammate to swap with.");
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
        string team = ResolveConfirmingSetupTeam();
        if (!TryConfirmSetupTeam(team))
        {
            return;
        }
        Debug.Log($"{team} confirmed setup ({setupConfirmCount}/2)");

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
        string team = ResolveConfirmingSetupTeam();
        if (!TryConfirmSetupTeam(team))
        {
            return;
        }
        Debug.Log($"{team} post-goal kick-off setup confirmed ({setupConfirmCount}/2)");

        if (setupConfirmCount >= 2)
        {
            ConfirmCompletedPostGoalSetup();
        }
    }

    private void ConfirmCompletedPostGoalSetup()
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

    private void BeginKickoffTakerSelection()
    {
        MatchManager.Instance?.ClearGoalKickRestartTaker();
        setupConfirmCount = 0;
        ResetSetupConfirmations();
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
            BeginKickoffTakerSelection();
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

        MatchManager.Instance.SetSubstitutionsAvailable(false, "Kick-off Standard Pass committed");
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
        ResetSetupConfirmations();
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
                ? $"Post-goal setup: click an empty hex in {selectedToken.name}'s own half, or click a teammate to swap positions. The center spot stays empty until taker selection. Defenders cannot stand in the center circle. Press [Enter] twice when both teams are ready ({setupConfirmCount}/2)."
                : $"Post-goal setup: click any token to reposition it in its own half, including the halfway line, except the center spot. The kicker will be selected later. Defenders cannot stand in the center circle. Press [Enter] twice when both teams are ready ({setupConfirmCount}/2).",
            KickoffFlowPhase.TakerSelection => "Kick-off taker selection: click an attacking token to move it to (0,0) and start the committed kick-off ground pass.",
            KickoffFlowPhase.InitialSetup => selectedToken != null
                ? $"Pre-kickoff setup: click an empty hex in {selectedToken.name}'s own half, or click a teammate to swap positions. Defenders cannot stand in the center circle. Press [Enter] twice to continue ({setupConfirmCount}/2)."
                : $"Pre-kickoff setup: click tokens to reposition them. Press [Enter] twice to continue ({setupConfirmCount}/2).",
            _ => string.Empty,
        };
    }

    public void PopulateRoomDecisionContext(RoomDecisionContext context)
    {
        if (context == null || !isActivated)
        {
            return;
        }

        if (isMovingSetupToken)
        {
            context.AddActionSummary("No Kick-off setup decision while a token is moving");
            return;
        }

        if (isMovingKickoffTaker)
        {
            context.AddActionSummary("No Kick-off taker decision while the taker is moving");
            return;
        }

        switch (flowPhase)
        {
            case KickoffFlowPhase.InitialSetup:
                context.AddKeyActionCandidate(
                    nameof(KickoffManager),
                    RoomActionType.Kickoff,
                    RoomDecisionStep.Confirm,
                    "Enter",
                    $"Press [Enter] to confirm pre-kickoff setup ({setupConfirmCount}/2)");
                PopulateSetupDecisionContext(context, isPostGoalSetup: false);
                break;
            case KickoffFlowPhase.PostGoalSetup:
                context.AddKeyActionCandidate(
                    nameof(KickoffManager),
                    RoomActionType.Kickoff,
                    RoomDecisionStep.Confirm,
                    "Enter",
                    $"Press [Enter] to confirm post-goal kick-off setup ({setupConfirmCount}/2)");
                PopulateSetupDecisionContext(context, isPostGoalSetup: true);
                break;
            case KickoffFlowPhase.TakerSelection:
                context.AddActionSummary("Click an attacking token to take the kick-off");
                AddKickoffTakerCandidates(context);
                break;
        }
    }

    public bool IsWaitingForMovementToComplete()
    {
        return isActivated && (isMovingSetupToken || isMovingKickoffTaker);
    }

    public List<RoomActionCandidate> BuildAutomatedSetupCandidates(string team)
    {
        List<RoomActionCandidate> candidates = new();
        team = NormalizeTeam(team);
        if (!isActivated || string.IsNullOrWhiteSpace(team) || IsSetupConfirmedForTeam(team))
        {
            return candidates;
        }

        string boardStateKey = BuildSetupBoardStateKey(team);
        GetVisitedStates(team).Add(boardStateKey);
        bool home = team == "Home";
        bool postGoal = flowPhase == KickoffFlowPhase.PostGoalSetup;
        bool budgetReached = GetAutomatedMoveCount(team) >= AutomatedSetupMoveBudget;
        if (!isMovingSetupToken && !budgetReached && playerTokenManager?.allTokens != null)
        {
            foreach (PlayerToken actor in playerTokenManager.allTokens)
            {
                if (actor == null || !actor.isPlaying || actor.isHomeTeam != home
                    || automatedMovedActors.TryGetValue(team, out var movedActors) && movedActors.Contains(actor))
                {
                    continue;
                }

                foreach (HexCell destination in GetAllGridHexes())
                {
                    if (destination == actor.GetCurrentHex())
                    {
                        continue;
                    }

                    bool legal = postGoal
                        ? IsValidPostGoalSetupDecisionDestination(actor, destination)
                        : IsValidInitialSetupDecisionDestination(actor, destination);
                    if (!legal)
                    {
                        continue;
                    }

                    candidates.Add(new RoomActionCandidate
                    {
                        manager = nameof(KickoffManager),
                        actionType = RoomActionType.SetupMove,
                        step = RoomDecisionStep.ChooseTarget,
                        actor = actor,
                        targetHex = destination,
                        setupTeam = team,
                        setupBoardStateKey = boardStateKey,
                        isExecutableNow = true,
                        label = $"Move {FormatTokenName(actor)} atomically to {destination.coordinates}"
                    });
                }
            }
        }

        candidates.Add(new RoomActionCandidate
        {
            manager = nameof(KickoffManager),
            actionType = RoomActionType.Confirm,
            step = RoomDecisionStep.Confirm,
            setupTeam = team,
            setupBoardStateKey = boardStateKey,
            key = "Enter",
            isExecutableNow = !isMovingSetupToken,
            label = $"Confirm {team} kick-off setup"
        });
        if (budgetReached)
        {
            candidates[candidates.Count - 1].setupTerminalReason = "move budget reached";
        }
        return candidates;
    }

    public bool TryExecuteAutomatedSetupCandidate(RoomActionCandidate candidate, out string error)
    {
        error = string.Empty;
        if (candidate == null || candidate.manager != nameof(KickoffManager))
        {
            error = "The committed command is not a kick-off setup command.";
            return false;
        }

        string team = NormalizeTeam(candidate.setupTeam);
        if (string.IsNullOrWhiteSpace(team) || IsSetupConfirmedForTeam(team))
        {
            error = $"{team} is not eligible for another kick-off setup command.";
            return false;
        }

        string currentStateKey = BuildSetupBoardStateKey(team);
        if (!string.Equals(candidate.setupBoardStateKey, currentStateKey, System.StringComparison.Ordinal))
        {
            error = $"stale board revision expected={candidate.setupBoardStateKey} actual={currentStateKey}";
            return false;
        }

        if (candidate.actionType == RoomActionType.Confirm)
        {
            if (isMovingSetupToken || !TryConfirmSetupTeam(team))
            {
                error = "confirmation was unavailable or had already been applied";
                return false;
            }

            Debug.Log($"[Kickoff AI] team={team} confirmed exactly once reason={candidate.setupTerminalReason} moves={GetAutomatedMoveCount(team)}");
            if (setupConfirmCount >= 2)
            {
                if (flowPhase == KickoffFlowPhase.InitialSetup) ValidateAndStartGame();
                else ConfirmCompletedPostGoalSetup();
            }
            return true;
        }

        PlayerToken actor = candidate.actor;
        HexCell destination = candidate.targetHex;
        if (isMovingSetupToken || actor == null || destination == null || actor.isHomeTeam != (team == "Home"))
        {
            error = "actor, destination, team ownership, or movement availability changed";
            return false;
        }
        if (automatedMovedActors.TryGetValue(team, out var alreadyMoved) && alreadyMoved.Contains(actor))
        {
            error = "actor already arranged during this setup revision";
            return false;
        }

        bool legalDestination = flowPhase == KickoffFlowPhase.PostGoalSetup
            ? IsValidPostGoalSetupDecisionDestination(actor, destination)
            : IsValidInitialSetupDecisionDestination(actor, destination);
        if (!legalDestination)
        {
            error = "destination is no longer legal";
            return false;
        }

        string projectedStateKey = BuildSetupBoardStateKey(team, actor, destination);
        bool visited = GetVisitedStates(team).Contains(projectedStateKey);
        bool immediateReversal = automatedPreviousSetupState.TryGetValue(team, out string previous)
            && string.Equals(previous, projectedStateKey, System.StringComparison.Ordinal);
        if ((visited || immediateReversal)
            && candidate.setupFormationPenaltyAfter >= candidate.setupFormationPenaltyBefore)
        {
            error = visited ? "visited-state oscillation rejected" : "immediate reversal rejected";
            Debug.Log($"[Kickoff AI] team={team} actor={FormatTokenName(actor)} destination={destination.coordinates} rejected={error}");
            return false;
        }

        if (GetAutomatedMoveCount(team) >= AutomatedSetupMoveBudget)
        {
            error = "setup move budget reached";
            return false;
        }

        automatedPreviousSetupState[team] = currentStateKey;
        automatedSetupMoveCounts[team] = GetAutomatedMoveCount(team) + 1;
        if (!automatedMovedActors.TryGetValue(team, out var actors))
            automatedMovedActors[team] = actors = new HashSet<PlayerToken>();
        actors.Add(actor);
        Debug.Log($"[Kickoff AI] commit team={team} actor={FormatTokenName(actor)} destination={destination.coordinates} penalty={candidate.setupFormationPenaltyBefore}->{candidate.setupFormationPenaltyAfter} revision={currentStateKey}");
        StartCoroutine(ExecuteAutomatedSetupMove(team, actor, destination, projectedStateKey));
        return true;
    }

    private IEnumerator ExecuteAutomatedSetupMove(string team, PlayerToken actor, HexCell destination, string projectedStateKey)
    {
        isMovingSetupToken = true;
        yield return StartCoroutine(freeKickManager.MoveTokenToHex(actor, destination));
        isMovingSetupToken = false;
        GetVisitedStates(team).Add(projectedStateKey);
        Debug.Log($"[Kickoff AI] complete team={team} actor={FormatTokenName(actor)} destination={destination.coordinates} revision={BuildSetupBoardStateKey(team)}");
    }

    public string BuildSetupBoardStateKey(string team)
    {
        return BuildSetupBoardStateKey(team, null, null);
    }

    public RoomAIKickoffPlan GetAutomatedOpeningPlan(string team, System.Func<RoomAIKickoffPlan> build)
    {
        if (!automatedOpeningPlans.TryGetValue(team, out var plan))
        {
            plan = build();
            if (plan != null) automatedOpeningPlans[team] = plan;
        }
        return plan;
    }

    private string BuildSetupBoardStateKey(string team, PlayerToken movedActor, HexCell destination)
    {
        team = NormalizeTeam(team);
        IEnumerable<PlayerToken> tokens = playerTokenManager?.allTokens ?? Enumerable.Empty<PlayerToken>();
        string coordinates = string.Join(";", tokens
            .Where(token => token != null && token.isPlaying && (token.isHomeTeam ? "Home" : "Away") == team)
            .OrderBy(token => token.jerseyNumber)
            .Select(token =>
            {
                HexCell hex = token == movedActor ? destination : token.GetCurrentHex();
                return $"{token.jerseyNumber}:{hex?.coordinates.x},{hex?.coordinates.z}";
            }));
        return $"{team}|{coordinates}|H={homeSetupConfirmed}|A={awaySetupConfirmed}";
    }

    private HashSet<string> GetVisitedStates(string team)
    {
        if (!automatedVisitedSetupStates.TryGetValue(team, out HashSet<string> states))
        {
            states = new HashSet<string>();
            automatedVisitedSetupStates[team] = states;
        }
        return states;
    }

    private int GetAutomatedMoveCount(string team)
    {
        return automatedSetupMoveCounts.TryGetValue(team, out int count) ? count : 0;
    }

    private void PopulateSetupDecisionContext(RoomDecisionContext context, bool isPostGoalSetup)
    {
        if (selectedToken == null)
        {
            context.AddActionSummary("Click any playing token to reposition it for kick-off setup");
            AddPlayingTokens(context);
            return;
        }

        string tokenName = FormatTokenName(selectedToken);
        context.AddActionSummary($"Click a legal setup hex to move {tokenName}");
        context.AddActionSummary($"Click a teammate to swap setup positions with {tokenName}");
        AddValidSetupHexes(context, selectedToken, isPostGoalSetup);
        AddValidSetupSwapTeammates(context, selectedToken, isPostGoalSetup);
    }

    private void AddPlayingTokens(RoomDecisionContext context)
    {
        if (playerTokenManager == null)
        {
            return;
        }

        foreach (PlayerToken token in playerTokenManager.allTokens)
        {
            if (token != null && token.isPlaying)
            {
                context.AddTokenActionCandidate(
                    nameof(KickoffManager),
                    RoomActionType.SetupMove,
                    RoomDecisionStep.ChooseActor,
                    token,
                    $"Select {FormatTokenName(token)} to reposition for kick-off setup");
            }
        }
    }

    private void AddKickoffTakerCandidates(RoomDecisionContext context)
    {
        if (playerTokenManager == null)
        {
            return;
        }

        foreach (PlayerToken token in playerTokenManager.allTokens)
        {
            if (token != null && token.isPlaying && token.isAttacker)
            {
                context.AddTokenActionCandidate(
                    nameof(KickoffManager),
                    RoomActionType.Kickoff,
                    RoomDecisionStep.ChooseActor,
                    token,
                    $"Select {FormatTokenName(token)} to take the kick-off");
            }
        }
    }

    private void AddValidSetupHexes(RoomDecisionContext context, PlayerToken token, bool isPostGoalSetup)
    {
        foreach (HexCell hex in GetAllGridHexes())
        {
            if (hex == token.GetCurrentHex())
            {
                continue;
            }

            bool isValid = isPostGoalSetup
                ? IsValidPostGoalSetupDecisionDestination(token, hex)
                : IsValidInitialSetupDecisionDestination(token, hex);
            if (isValid)
            {
                context.AddHexActionCandidate(
                    nameof(KickoffManager),
                    RoomActionType.SetupMove,
                    RoomDecisionStep.ChooseTarget,
                    hex,
                    $"Move {FormatTokenName(token)} to kick-off setup hex {hex.coordinates}");
            }
        }
    }

    private void AddValidSetupSwapTeammates(RoomDecisionContext context, PlayerToken token, bool isPostGoalSetup)
    {
        if (playerTokenManager == null || token == null)
        {
            return;
        }

        HexCell selectedHex = token.GetCurrentHex();
        if (selectedHex == null)
        {
            return;
        }

        foreach (PlayerToken teammate in playerTokenManager.allTokens)
        {
            if (!IsTeammate(token, teammate))
            {
                continue;
            }

            HexCell teammateHex = teammate.GetCurrentHex();
            if (teammateHex == null)
            {
                continue;
            }

            bool isValidSwap = isPostGoalSetup
                ? IsValidPostGoalSetupDecisionDestination(token, teammateHex, teammate)
                    && IsValidPostGoalSetupDecisionDestination(teammate, selectedHex, token)
                : IsValidInitialSetupDecisionDestination(token, teammateHex, teammate)
                    && IsValidInitialSetupDecisionDestination(teammate, selectedHex, token);
            if (isValidSwap)
            {
                context.AddTokenActionCandidate(
                    nameof(KickoffManager),
                    RoomActionType.SetupMove,
                    RoomDecisionStep.ChooseActor,
                    teammate,
                    $"Swap kick-off setup positions with {FormatTokenName(teammate)}");
            }
        }
    }

    private bool IsValidInitialSetupDecisionDestination(PlayerToken token, HexCell targetHex, PlayerToken allowedOccupant = null)
    {
        if (token == null || targetHex == null)
        {
            return false;
        }

        if (targetHex.isOutOfBounds || targetHex.isInGoal != 0)
        {
            return false;
        }

        PlayerToken occupyingToken = targetHex.GetOccupyingToken();
        if (occupyingToken != null && occupyingToken != token && occupyingToken != allowedOccupant)
        {
            return false;
        }

        if (!IsTeamOwnHalfOrMidline(token, targetHex))
        {
            return false;
        }

        return token.isAttacker || targetHex.isInCircle != 5;
    }

    private bool IsValidPostGoalSetupDecisionDestination(PlayerToken token, HexCell targetHex, PlayerToken allowedOccupant = null)
    {
        if (token == null || targetHex == null)
        {
            return false;
        }

        if (targetHex.isOutOfBounds || targetHex.isInGoal != 0)
        {
            return false;
        }

        if (targetHex.coordinates.x == 0 && targetHex.coordinates.z == 0)
        {
            return false;
        }

        if (!token.isAttacker && targetHex.isInCircle == 5)
        {
            return false;
        }

        PlayerToken occupyingToken = targetHex.GetOccupyingToken();
        bool occupiedByAnotherToken = occupyingToken != null
            && occupyingToken != token
            && occupyingToken != allowedOccupant;
        bool hasUnknownOccupancy = (targetHex.isAttackOccupied || targetHex.isDefenseOccupied)
            && occupyingToken != token
            && occupyingToken != allowedOccupant;
        if (occupiedByAnotherToken || hasUnknownOccupancy)
        {
            return false;
        }

        return IsTeamOwnHalfOrMidline(token, targetHex);
    }

    private IEnumerable<HexCell> GetAllGridHexes()
    {
        if (hexGrid == null || hexGrid.cells == null)
        {
            yield break;
        }

        foreach (HexCell hex in hexGrid.cells)
        {
            if (hex != null)
            {
                yield return hex;
            }
        }
    }

    private static string FormatTokenName(PlayerToken token)
    {
        if (token == null)
        {
            return "the selected player";
        }

        return !string.IsNullOrWhiteSpace(token.playerName) ? token.playerName : token.name;
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

    public bool IsSetupConfirmedForTeam(string team)
    {
        return string.Equals(team, "Home", System.StringComparison.OrdinalIgnoreCase)
            ? homeSetupConfirmed
            : string.Equals(team, "Away", System.StringComparison.OrdinalIgnoreCase)
                && awaySetupConfirmed;
    }

    public void SetPendingAutomatedConfirmationTeam(string team)
    {
        pendingAutomatedConfirmationTeam = string.Equals(team, "Home", System.StringComparison.OrdinalIgnoreCase)
            ? "Home"
            : string.Equals(team, "Away", System.StringComparison.OrdinalIgnoreCase)
                ? "Away"
                : string.Empty;
    }

    public void SetPendingAutomatedInputTeam(string team)
    {
        pendingAutomatedInputTeam = NormalizeTeam(team);
    }

    private bool CanCurrentControllerSelectSetupToken(PlayerToken token)
    {
        string automatedTeam = pendingAutomatedInputTeam;
        pendingAutomatedInputTeam = string.Empty;
        string tokenTeam = token.isHomeTeam ? "Home" : "Away";
        if (!string.IsNullOrWhiteSpace(automatedTeam))
        {
            return string.Equals(automatedTeam, tokenTeam, System.StringComparison.OrdinalIgnoreCase);
        }

        MatchManager.GameSettings settings = MatchManager.Instance?.gameData?.gameSettings;
        bool isHumanTeam = AIManager.ResolveRoomPersona(settings, tokenTeam) == AIManager.RoomPersona.AskHuman;
        if (!isHumanTeam)
        {
            Debug.LogWarning($"{tokenTeam} kick-off preparation is controlled by AI.");
        }
        return isHumanTeam;
    }

    private string ResolveConfirmingSetupTeam()
    {
        if (!string.IsNullOrWhiteSpace(pendingAutomatedConfirmationTeam))
        {
            string team = pendingAutomatedConfirmationTeam;
            pendingAutomatedConfirmationTeam = string.Empty;
            return team;
        }

        MatchManager.GameSettings settings = MatchManager.Instance?.gameData?.gameSettings;
        bool homeHuman = AIManager.ResolveRoomPersona(settings, "Home") == AIManager.RoomPersona.AskHuman;
        bool awayHuman = AIManager.ResolveRoomPersona(settings, "Away") == AIManager.RoomPersona.AskHuman;
        if (homeHuman && !homeSetupConfirmed)
        {
            return "Home";
        }
        if (awayHuman && !awaySetupConfirmed)
        {
            return "Away";
        }

        Debug.LogWarning("The remaining kick-off setup confirmation belongs to an AI-controlled team.");
        return string.Empty;
    }

    private bool TryConfirmSetupTeam(string team)
    {
        if (string.Equals(team, "Home", System.StringComparison.OrdinalIgnoreCase))
        {
            if (homeSetupConfirmed)
            {
                Debug.LogWarning("Home has already confirmed kick-off setup.");
                return false;
            }
            homeSetupConfirmed = true;
        }
        else if (string.Equals(team, "Away", System.StringComparison.OrdinalIgnoreCase))
        {
            if (awaySetupConfirmed)
            {
                Debug.LogWarning("Away has already confirmed kick-off setup.");
                return false;
            }
            awaySetupConfirmed = true;
        }
        else
        {
            return false;
        }

        setupConfirmCount = (homeSetupConfirmed ? 1 : 0) + (awaySetupConfirmed ? 1 : 0);
        if (AIManager.ResolveRoomPersona(MatchManager.Instance?.gameData?.gameSettings, team) == AIManager.RoomPersona.AskHuman)
        {
            automatedMovedActors.Clear();
            automatedOpeningPlans.Clear();
        }
        return true;
    }

    private void ResetSetupConfirmations()
    {
        homeSetupConfirmed = false;
        awaySetupConfirmed = false;
        pendingAutomatedConfirmationTeam = string.Empty;
        pendingAutomatedInputTeam = string.Empty;
        automatedVisitedSetupStates.Clear();
        automatedPreviousSetupState.Clear();
        automatedSetupMoveCounts.Clear();
        automatedMovedActors.Clear();
        automatedOpeningPlans.Clear();
    }

    private static string NormalizeTeam(string team)
    {
        return string.Equals(team, "Home", System.StringComparison.OrdinalIgnoreCase)
            ? "Home"
            : string.Equals(team, "Away", System.StringComparison.OrdinalIgnoreCase)
                ? "Away"
                : string.Empty;
    }
}
