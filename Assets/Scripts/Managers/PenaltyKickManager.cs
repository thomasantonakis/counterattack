using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class PenaltyKickManager : MonoBehaviour
{
    [Header("Dependencies")]
    public MatchManager matchManager;
    public HexGrid hexGrid;
    public Ball ball;
    public MovementPhaseManager movementPhaseManager;
    public ShotManager shotManager;

    [Header("Flags")]
    public bool isActivated;
    public bool isWaitingForKickerSelection;
    public bool isWaitingForSetupPhase;
    public bool isWaitingForExecution;

    [Header("Runtime Items")]
    public int takingSide;
    public PlayerToken selectedKicker;
    public PlayerToken selectedToken;
    public HexCell penaltySpot;
    public HexCell defendingGKSpot;

    private bool isMovingToken;
    private bool isMovingRequiredSpotToken;
    private bool kickerOwnershipSet;
    private HexCell hoveredSetupMoveHex;

    private void OnEnable()
    {
        GameInputManager.OnClick += OnClickReceived;
        GameInputManager.OnHover += OnHoverReceived;
        GameInputManager.OnKeyPress += OnKeyReceived;
    }

    private void OnDisable()
    {
        GameInputManager.OnClick -= OnClickReceived;
        GameInputManager.OnHover -= OnHoverReceived;
        GameInputManager.OnKeyPress -= OnKeyReceived;
        ClearSetupMoveHover();
    }

    public void Configure(
        MatchManager configuredMatchManager,
        HexGrid configuredHexGrid,
        Ball configuredBall,
        MovementPhaseManager configuredMovementPhaseManager,
        ShotManager configuredShotManager)
    {
        matchManager = configuredMatchManager != null ? configuredMatchManager : MatchManager.Instance;
        hexGrid = configuredHexGrid != null ? configuredHexGrid : FindAnyObjectByType<HexGrid>();
        ball = configuredBall != null ? configuredBall : FindAnyObjectByType<Ball>();
        movementPhaseManager = configuredMovementPhaseManager != null ? configuredMovementPhaseManager : FindAnyObjectByType<MovementPhaseManager>();
        shotManager = configuredShotManager != null ? configuredShotManager : FindAnyObjectByType<ShotManager>();
    }

    public void StartPenaltyPreparation()
    {
        ResolveDependencies();
        isActivated = true;
        isWaitingForKickerSelection = true;
        isWaitingForSetupPhase = false;
        isWaitingForExecution = false;
        selectedKicker = null;
        selectedToken = null;
        kickerOwnershipSet = false;
        ClearSetupMoveHover();
        takingSide = GetCurrentTakingSide();
        penaltySpot = hexGrid.GetHexCellAt(new Vector3Int(14 * takingSide, 0, 0));
        defendingGKSpot = hexGrid.GetHexCellAt(new Vector3Int(18 * takingSide, 0, 0));
        matchManager.currentState = MatchManager.GameState.PenaltyKickerSelect;
        hexGrid.ClearHighlightedHexes();
        Debug.Log($"Penalty awarded. Select the penalty taker. Spot: {penaltySpot?.coordinates}, defending GK spot: {defendingGKSpot?.coordinates}.");
        TryMoveRequiredTokensToSpots();
    }

    private void ResolveDependencies()
    {
        matchManager ??= MatchManager.Instance;
        hexGrid ??= FindAnyObjectByType<HexGrid>();
        ball ??= FindAnyObjectByType<Ball>();
        movementPhaseManager ??= FindAnyObjectByType<MovementPhaseManager>();
        shotManager ??= FindAnyObjectByType<ShotManager>();
    }

    private int GetCurrentTakingSide()
    {
        MatchManager.TeamAttackingDirection direction = matchManager.teamInAttack == MatchManager.TeamInAttack.Home
            ? matchManager.homeTeamDirection
            : matchManager.awayTeamDirection;
        return direction == MatchManager.TeamAttackingDirection.LeftToRight ? 1 : -1;
    }

    private void OnClickReceived(PlayerToken token, HexCell hex)
    {
        if (!isActivated || isMovingToken || isMovingRequiredSpotToken)
        {
            return;
        }

        if (isWaitingForKickerSelection)
        {
            HandleKickerSelection(token);
            return;
        }

        if (!isWaitingForSetupPhase)
        {
            return;
        }

        if (selectedToken != null && hex != null)
        {
            StartCoroutine(HandleSetupHexSelection(hex));
            return;
        }

        if (token != null)
        {
            HandleSetupTokenSelection(token);
            return;
        }

        Debug.LogWarning("Penalty setup: select an eligible player, then an available legal hex.");
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (keyData.isConsumed || !isActivated || isMovingToken || isMovingRequiredSpotToken)
        {
            return;
        }

        if (isWaitingForSetupPhase && keyData.key == KeyCode.X)
        {
            Debug.LogWarning("Penalty setup confirmation uses [Enter], not [X].");
            keyData.isConsumed = true;
            return;
        }

        if (isWaitingForSetupPhase && (keyData.key == KeyCode.Return || keyData.key == KeyCode.KeypadEnter))
        {
            keyData.isConsumed = true;
            AttemptToAdvanceSetupPhase();
        }
    }

    private void OnHoverReceived(PlayerToken token, HexCell hex)
    {
        if (!CanShowSetupMoveHover(hex))
        {
            ClearSetupMoveHover();
            return;
        }

        if (hoveredSetupMoveHex == hex)
        {
            return;
        }

        ClearSetupMoveHover();
        hoveredSetupMoveHex = hex;
        hoveredSetupMoveHex.HighlightHex("FreeKickSelectedMoveHover");
    }

    private bool CanShowSetupMoveHover(HexCell hex)
    {
        return isActivated
            && isWaitingForSetupPhase
            && selectedToken != null
            && hex != null
            && !isMovingToken
            && !isMovingRequiredSpotToken
            && matchManager != null
            && matchManager.difficulty_level < 3
            && IsLegalSetupDestination(hex);
    }

    private void ClearSetupMoveHover()
    {
        if (hoveredSetupMoveHex == null)
        {
            return;
        }

        HexCell previousHover = hoveredSetupMoveHex;
        hoveredSetupMoveHex = null;
        previousHover.ResetHighlight();
    }

    private void HandleKickerSelection(PlayerToken token)
    {
        if (token == null)
        {
            Debug.LogWarning("Penalty setup: click an attacking outfield player to take the penalty.");
            return;
        }

        if (!token.isAttacker || token.IsGoalKeeper)
        {
            Debug.LogWarning($"{token.name} cannot take the penalty. Select an attacking outfield player.");
            return;
        }

        selectedKicker = token;
        isWaitingForKickerSelection = false;
        Debug.Log($"{selectedKicker.name} selected as penalty taker.");
        TryMoveRequiredTokensToSpots();
        BeginSetupPhase(MatchManager.GameState.PenaltyDef1);
    }

    private void BeginSetupPhase(MatchManager.GameState state)
    {
        matchManager.currentState = state;
        isWaitingForSetupPhase = true;
        selectedToken = null;
        ClearSetupMoveHover();
        hexGrid.ClearHighlightedHexes();
        TryMoveRequiredTokensToSpots();
        Debug.Log($"Penalty setup phase started: {state}.");
    }

    private void AttemptToAdvanceSetupPhase()
    {
        TryMoveRequiredTokensToSpots();
        List<PlayerToken> invalidTokens = GetInvalidTokensForCurrentSetupTeam();
        if (invalidTokens.Count > 0)
        {
            Debug.LogWarning($"Penalty setup cannot advance. Move these players out of the forbidden penalty setup hexes first: {FormatTokenNames(invalidTokens)}.");
            return;
        }

        selectedToken = null;
        ClearSetupMoveHover();
        hexGrid.ClearHighlightedHexes();
        switch (matchManager.currentState)
        {
            case MatchManager.GameState.PenaltyDef1:
                BeginSetupPhase(MatchManager.GameState.PenaltyAtt);
                break;
            case MatchManager.GameState.PenaltyAtt:
                BeginSetupPhase(MatchManager.GameState.PenaltyDef2);
                break;
            case MatchManager.GameState.PenaltyDef2:
                BeginExecution();
                break;
        }
    }

    private void BeginExecution()
    {
        TryMoveRequiredTokensToSpots();
        if (!RequiredSpotsAreReady())
        {
            Debug.LogWarning("Penalty execution cannot start until the penalty spot and defending GK spot are clear.");
            return;
        }

        isWaitingForSetupPhase = false;
        isWaitingForExecution = true;
        matchManager.currentState = MatchManager.GameState.PenaltyExecution;
        Debug.Log("Penalty setup complete. Select a shot target.");
        shotManager.StartPenaltyShotProcess(selectedKicker, penaltySpot);
        isActivated = false;
        isWaitingForExecution = false;
    }

    private bool RequiredSpotsAreReady()
    {
        return selectedKicker != null
            && selectedKicker.GetCurrentHex() == penaltySpot
            && hexGrid.GetDefendingGK()?.GetCurrentHex() == defendingGKSpot
            && ball.GetCurrentHex() == penaltySpot;
    }

    private void HandleSetupTokenSelection(PlayerToken token)
    {
        if (token == null || token.IsGoalKeeper)
        {
            Debug.LogWarning("Penalty setup: only outfield players can be moved.");
            return;
        }

        bool expectsAttack = matchManager.currentState == MatchManager.GameState.PenaltyAtt;
        if (expectsAttack != token.isAttacker)
        {
            Debug.LogWarning(expectsAttack
                ? $"{token.name} is not an attacker. Select an attacking outfield player."
                : $"{token.name} is not a defender. Select a defending outfield player.");
            return;
        }

        if (token == selectedKicker && token.GetCurrentHex() == penaltySpot)
        {
            Debug.LogWarning($"{token.name} is already placed on the penalty spot and cannot be moved during setup.");
            return;
        }

        selectedToken = token;
        ClearSetupMoveHover();
        hexGrid.ClearHighlightedHexes();
        Debug.Log($"Penalty setup selected {selectedToken.name}. Click any legal unoccupied setup hex.");
    }

    private IEnumerator HandleSetupHexSelection(HexCell hex)
    {
        if (selectedToken == null)
        {
            yield break;
        }

        if (!IsLegalSetupDestination(hex))
        {
            Debug.LogWarning(hex != null
                ? $"Penalty setup destination {hex.coordinates} is illegal or occupied."
                : "Penalty setup destination is invalid.");
            yield break;
        }

        PlayerToken tokenToMove = selectedToken;
        selectedToken = null;
        ClearSetupMoveHover();
        isMovingToken = true;
        yield return StartCoroutine(MoveTokenToHex(tokenToMove, hex, moveBallWithToken: false));
        isMovingToken = false;
        TryMoveRequiredTokensToSpots();
    }

    private bool IsLegalSetupDestination(HexCell hex)
    {
        return hex != null
            && !hex.isOutOfBounds
            && hex.isInGoal == 0
            && !hex.isAttackOccupied
            && !hex.isDefenseOccupied
            && hex.GetOccupyingToken() == null
            && !IsForbiddenPenaltySetupHex(hex);
    }

    private bool IsForbiddenPenaltySetupHex(HexCell hex)
    {
        if (hex == null)
        {
            return true;
        }

        return hex.isInPenaltyBox == takingSide || IsTakingSidePenaltyArc(hex);
    }

    private bool IsTakingSidePenaltyArc(HexCell hex)
    {
        if (hex == null)
        {
            return false;
        }

        bool explicitArc = hex.coordinates.x == 11 * takingSide
            && hex.coordinates.z >= -2
            && hex.coordinates.z <= 1;
        return hex.isInCircle == takingSide || explicitArc;
    }

    private List<PlayerToken> GetInvalidTokensForCurrentSetupTeam()
    {
        bool expectsAttack = matchManager.currentState == MatchManager.GameState.PenaltyAtt;
        IEnumerable<PlayerToken> tokens = expectsAttack ? GetAttackers() : hexGrid.GetDefenders();
        return tokens
            .Where(token => token != null
                && !token.IsGoalKeeper
                && !(token == selectedKicker && token.GetCurrentHex() == penaltySpot)
                && IsForbiddenPenaltySetupHex(token.GetCurrentHex()))
            .ToList();
    }

    private IEnumerable<PlayerToken> GetAttackers()
    {
        return hexGrid.GetAttackerHexes()
            .Select(hex => hex != null ? hex.GetOccupyingToken() : null)
            .Where(token => token != null);
    }

    private void TryMoveRequiredTokensToSpots()
    {
        if (isMovingRequiredSpotToken || hexGrid == null)
        {
            return;
        }

        if (selectedKicker != null && penaltySpot != null && selectedKicker.GetCurrentHex() != penaltySpot && IsSpotAvailableForToken(penaltySpot, selectedKicker))
        {
            StartCoroutine(MoveRequiredTokenToSpot(selectedKicker, penaltySpot, moveBallWithToken: true));
            return;
        }

        PlayerToken defendingGK = hexGrid.GetDefendingGK();
        if (defendingGK != null && defendingGKSpot != null && defendingGK.GetCurrentHex() != defendingGKSpot && IsSpotAvailableForToken(defendingGKSpot, defendingGK))
        {
            StartCoroutine(MoveRequiredTokenToSpot(defendingGK, defendingGKSpot, moveBallWithToken: false));
        }
    }

    private bool IsSpotAvailableForToken(HexCell spot, PlayerToken token)
    {
        if (spot == null || token == null)
        {
            return false;
        }

        PlayerToken occupyingToken = spot.GetOccupyingToken();
        return occupyingToken == null || occupyingToken == token;
    }

    private IEnumerator MoveRequiredTokenToSpot(PlayerToken token, HexCell spot, bool moveBallWithToken)
    {
        isMovingRequiredSpotToken = true;
        Debug.Log($"Moving {token.name} to required penalty spot {spot.coordinates}.");
        yield return StartCoroutine(MoveTokenToHex(token, spot, moveBallWithToken));
        if (moveBallWithToken)
        {
            ball.SetCurrentHex(spot);
            ball.AdjustBallHeightBasedOnOccupancy();
            if (!kickerOwnershipSet)
            {
                matchManager.ClearLastTokenChain();
                matchManager.SetLastToken(token);
                kickerOwnershipSet = true;
            }
        }
        isMovingRequiredSpotToken = false;
        TryMoveRequiredTokensToSpots();
    }

    private IEnumerator MoveTokenToHex(PlayerToken token, HexCell targetHex, bool moveBallWithToken)
    {
        if (token == null || targetHex == null)
        {
            yield break;
        }

        HexCell tokenHex = token.GetCurrentHex();
        if (tokenHex != null)
        {
            if (token.isAttacker) tokenHex.isAttackOccupied = false;
            else tokenHex.isDefenseOccupied = false;
            tokenHex.ResetHighlight();
        }

        if (token.isAttacker) targetHex.isAttackOccupied = true;
        else targetHex.isDefenseOccupied = true;

        yield return StartCoroutine(token.JumpToHex(targetHex));
        if (moveBallWithToken)
        {
            ball.SetCurrentHex(targetHex);
        }
        ball.AdjustBallHeightBasedOnOccupancy();
        targetHex.HighlightHex(token.isAttacker ? "isAttackOccupied" : "isDefenseOccupied");
    }

    public string GetDebugStatus()
    {
        StringBuilder sb = new();
        sb.Append("PK: ");
        if (isActivated) sb.Append("isActivated, ");
        if (isWaitingForKickerSelection) sb.Append("isWaitingForKickerSelection, ");
        if (isWaitingForSetupPhase) sb.Append("isWaitingForSetupPhase, ");
        if (isWaitingForExecution) sb.Append("isWaitingForExecution, ");
        if (selectedKicker != null) sb.Append($"selectedKicker: {selectedKicker.name}, ");
        if (selectedToken != null) sb.Append($"selectedToken: {selectedToken.name}, ");
        if (penaltySpot != null) sb.Append($"penaltySpot: {penaltySpot.coordinates}, ");
        if (defendingGKSpot != null) sb.Append($"defGKSpot: {defendingGKSpot.coordinates}, ");
        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2;
        return sb.ToString();
    }

    public string GetInstructions()
    {
        if (!isActivated)
        {
            return "";
        }

        StringBuilder sb = new();
        sb.Append("PK: ");
        if (isWaitingForKickerSelection)
        {
            sb.Append("Click an attacking outfield player to take the penalty. ");
        }
        else if (isWaitingForSetupPhase)
        {
            List<PlayerToken> invalidTokens = GetInvalidTokensForCurrentSetupTeam();
            bool attackPhase = matchManager.currentState == MatchManager.GameState.PenaltyAtt;
            sb.Append(attackPhase ? "Attack setup. " : "Defense setup. ");
            if (invalidTokens.Count > 0)
            {
                sb.Append($"Move these players out of the penalty box/arc: {FormatTokenNames(invalidTokens)}. ");
            }
            if (selectedToken != null)
            {
                sb.Append($"Click a legal unoccupied hex to move {selectedToken.name}, or click another eligible player. ");
            }
            else if (invalidTokens.Count > 0)
            {
                sb.Append("Click an eligible outfield player to move. ");
            }
            else
            {
                sb.Append("Click an eligible outfield player to move, or press [Enter] to confirm setup. ");
            }
        }
        return sb.ToString();
    }

    public bool? IsInstructionExpectingHomeTeam()
    {
        if (!isActivated || matchManager == null)
        {
            return null;
        }

        bool attackingTeamIsHome = matchManager.teamInAttack == MatchManager.TeamInAttack.Home;
        return matchManager.currentState == MatchManager.GameState.PenaltyDef1
            || matchManager.currentState == MatchManager.GameState.PenaltyDef2
                ? !attackingTeamIsHome
                : attackingTeamIsHome;
    }

    private static string FormatTokenNames(IEnumerable<PlayerToken> tokens)
    {
        List<string> names = tokens
            .Where(token => token != null)
            .Select(token => !string.IsNullOrWhiteSpace(token.playerName) ? token.playerName : token.name)
            .ToList();
        return names.Count > 0 ? string.Join(", ", names) : "none";
    }
}
