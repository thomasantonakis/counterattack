using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class PenaltyShootoutManager : MonoBehaviour
{
    private const string OrderPanelResourcePath = "UI/PenaltyShootoutOrderPanel";
    private static readonly Vector3Int PenaltySpotCoordinates = new(-14, 0, 0);
    private static readonly Vector3Int DefendingGoalkeeperCoordinates = new(-18, 0, 0);
    private static readonly Vector3Int HomeGoalkeeperRestCoordinates = new(-18, 0, -9);
    private static readonly Vector3Int AwayGoalkeeperRestCoordinates = new(-18, 0, 9);

    public static PenaltyShootoutManager ActiveShootout { get; private set; }

    [Header("Dependencies")]
    public MatchManager matchManager;
    public HexGrid hexGrid;
    public Ball ball;
    public PlayerTokenManager playerTokenManager;
    public MovementPhaseManager movementPhaseManager;
    public GroundBallManager groundBallManager;
    public ShotManager shotManager;

    [Header("State")]
    public List<PlayerToken> homeOrder = new();
    public List<PlayerToken> awayOrder = new();
    public int homeShootoutScore;
    public int awayShootoutScore;
    public int homePreShootoutGoals;
    public int awayPreShootoutGoals;
    public int homeAttempts;
    public int awayAttempts;
    public bool isSuddenDeath;
    public bool isActive;
    public bool isComplete;
    public bool winnerIsHome;
    public PlayerToken currentShooter;
    public PlayerToken currentDefendingGoalkeeper;

    private readonly List<bool> homeResults = new();
    private readonly List<bool> awayResults = new();
    private readonly Dictionary<PlayerToken, HexCell> stagingHexes = new();
    private PenaltyShootoutOrderPanelController orderPanel;
    private PlayerToken previousShooter;
    private PlayerToken previousDefendingGoalkeeper;
    private bool orderSelectionStarted;
    private bool orderSelectionCompleted;
    private bool nextKickIsHome = true;
    private bool resolvingShot;
    private bool transitionInstructionActive;
    private bool goalFlashInstructionActive;
    private bool goalFlashInstructionHome;
    private bool winnerFlashInstructionActive;
    private bool winnerFlashInstructionHome;
    private bool preShootoutScoreCaptured;
    private PlayerToken transitionInstructionShooter;

    public bool IsOrderSelectionOrShootoutStarted => orderSelectionStarted || orderSelectionCompleted || isActive || isComplete;
    public int HomePreShootoutGoals => preShootoutScoreCaptured ? homePreShootoutGoals : GetCurrentHomeGoals();
    public int AwayPreShootoutGoals => preShootoutScoreCaptured ? awayPreShootoutGoals : GetCurrentAwayGoals();

    public void Configure(MatchManager configuredMatchManager)
    {
        matchManager = configuredMatchManager != null ? configuredMatchManager : MatchManager.Instance;
        hexGrid = matchManager != null && matchManager.hexGrid != null ? matchManager.hexGrid : FindAnyObjectByType<HexGrid>();
        ball = matchManager != null && matchManager.ball != null ? matchManager.ball : FindAnyObjectByType<Ball>();
        playerTokenManager = matchManager != null && matchManager.playerTokenManager != null ? matchManager.playerTokenManager : FindAnyObjectByType<PlayerTokenManager>();
        movementPhaseManager = matchManager != null && matchManager.movementPhaseManager != null ? matchManager.movementPhaseManager : FindAnyObjectByType<MovementPhaseManager>();
        groundBallManager = matchManager != null && matchManager.groundBallManager != null ? matchManager.groundBallManager : FindAnyObjectByType<GroundBallManager>();
        shotManager = matchManager != null && matchManager.shotManager != null ? matchManager.shotManager : FindAnyObjectByType<ShotManager>();
    }

    public void ShowOrderSelection()
    {
        Configure(matchManager);
        if (matchManager == null || playerTokenManager == null)
        {
            Debug.LogError("[Shootout] Cannot show order selection because match/player token dependencies are missing.");
            return;
        }

        if (IsOrderSelectionOrShootoutStarted)
        {
            if (orderSelectionCompleted || isActive || isComplete)
            {
                HideOrderPanel();
            }

            if (!isComplete)
            {
                matchManager.MarkPenaltyShootoutInProgress();
            }

            Debug.Log("[Shootout] Ignoring duplicate order selection request because the shootout is already underway.");
            return;
        }

        int listLength = Mathf.Min(GetEligiblePlayingTokens(true).Count, GetEligiblePlayingTokens(false).Count);
        if (listLength <= 0)
        {
            Debug.LogError("[Shootout] Cannot start because at least one team has no eligible playing tokens.");
            return;
        }

        homeOrder = BuildDefaultOrder(true).Take(listLength).ToList();
        awayOrder = BuildDefaultOrder(false).Take(listLength).ToList();
        CapturePreShootoutScore();
        orderSelectionStarted = true;
        orderSelectionCompleted = false;
        matchManager.currentState = MatchManager.GameState.PenaltyShootoutOrderSelection;
        matchManager.MarkPenaltyShootoutInProgress();
        EnsureOrderPanel();
        orderPanel.Configure(this, homeOrder, awayOrder);
        Debug.Log($"[Shootout] Order selection opened with {listLength} nominees per team.");
    }

    public void UpdateOrdersFromPanel(List<PlayerToken> nextHomeOrder, List<PlayerToken> nextAwayOrder)
    {
        if (nextHomeOrder != null && nextHomeOrder.Count > 0)
        {
            homeOrder = nextHomeOrder.Where(token => token != null).ToList();
        }

        if (nextAwayOrder != null && nextAwayOrder.Count > 0)
        {
            awayOrder = nextAwayOrder.Where(token => token != null).ToList();
        }
    }

    public void BeginShootoutFromOrderPanel(List<PlayerToken> selectedHomeOrder, List<PlayerToken> selectedAwayOrder)
    {
        UpdateOrdersFromPanel(selectedHomeOrder, selectedAwayOrder);
        if (homeOrder.Count == 0 || awayOrder.Count == 0)
        {
            Debug.LogError("[Shootout] Cannot start because a team order is empty.");
            return;
        }

        orderSelectionCompleted = true;
        HideOrderPanel();

        StartCoroutine(BeginShootoutFlow());
    }

    public void OnPenaltyShotResolved(bool scored)
    {
        if (!isActive || resolvingShot)
        {
            return;
        }

        StartCoroutine(HandlePenaltyShotResolved(scored));
    }

    public string BuildResultText()
    {
        string homeName = matchManager?.gameData?.gameSettings?.homeTeamName ?? "Home";
        string awayName = matchManager?.gameData?.gameSettings?.awayTeamName ?? "Away";
        StringBuilder sb = new();
        sb.AppendLine($"{homeName} {HomePreShootoutGoals} - {AwayPreShootoutGoals} {awayName}");
        sb.AppendLine();
        sb.AppendLine($"Penalties: {homeName} {homeShootoutScore} - {awayShootoutScore} {awayName}");
        sb.AppendLine($"{(winnerIsHome ? homeName : awayName)} win on penalties.");
        sb.AppendLine();
        sb.AppendLine($"{homeName}: {FormatResults(homeResults)}");
        sb.AppendLine($"{awayName}: {FormatResults(awayResults)}");
        return sb.ToString();
    }

    private void CapturePreShootoutScore()
    {
        homePreShootoutGoals = GetCurrentHomeGoals();
        awayPreShootoutGoals = GetCurrentAwayGoals();
        preShootoutScoreCaptured = true;
    }

    private int GetCurrentHomeGoals()
    {
        return matchManager?.gameData?.stats?.homeTeamStats?.totalGoals ?? 0;
    }

    private int GetCurrentAwayGoals()
    {
        return matchManager?.gameData?.stats?.awayTeamStats?.totalGoals ?? 0;
    }

    private IEnumerator BeginShootoutFlow()
    {
        isActive = true;
        isComplete = false;
        resolvingShot = false;
        ActiveShootout = this;
        orderSelectionStarted = false;
        CapturePreShootoutScore();
        homeShootoutScore = 0;
        awayShootoutScore = 0;
        homeAttempts = 0;
        awayAttempts = 0;
        isSuddenDeath = false;
        nextKickIsHome = true;
        homeResults.Clear();
        awayResults.Clear();
        stagingHexes.Clear();
        matchManager.currentState = MatchManager.GameState.PenaltyShootoutSetup;
        matchManager.SetSubstitutionsAvailable(false, "Penalty shootout");
        yield return StartCoroutine(StageShootoutTokens());
        yield return StartCoroutine(PrepareNextKick());
    }

    private IEnumerator StageShootoutTokens()
    {
        List<PlayerToken> homeTokens = GetEligiblePlayingTokens(true).OrderBy(token => token.jerseyNumber).ToList();
        List<PlayerToken> awayTokens = GetEligiblePlayingTokens(false).OrderBy(token => token.jerseyNumber).ToList();
        PlayerToken homeGk = homeTokens.FirstOrDefault(token => token.IsGoalKeeper);
        PlayerToken awayGk = awayTokens.FirstOrDefault(token => token.IsGoalKeeper);
        RegisterTeamStaging(homeTokens.Where(token => !token.IsGoalKeeper).ToList(), true);
        RegisterTeamStaging(awayTokens.Where(token => !token.IsGoalKeeper).ToList(), false);

        if (homeGk != null)
        {
            stagingHexes[homeGk] = Hex(HomeGoalkeeperRestCoordinates);
        }

        if (awayGk != null)
        {
            stagingHexes[awayGk] = Hex(AwayGoalkeeperRestCoordinates);
        }

        List<Coroutine> setupMoves = new();
        AddTeamStagingMoves(setupMoves, homeTokens.Where(token => !token.IsGoalKeeper).ToList(), true);
        AddTeamStagingMoves(setupMoves, awayTokens.Where(token => !token.IsGoalKeeper).ToList(), false);
        if (homeGk != null && Hex(HomeGoalkeeperRestCoordinates) != null)
        {
            setupMoves.Add(StartCoroutine(MoveTokenStraightToHex(homeGk, Hex(HomeGoalkeeperRestCoordinates), false, true)));
        }

        if (awayGk != null && Hex(AwayGoalkeeperRestCoordinates) != null)
        {
            setupMoves.Add(StartCoroutine(MoveTokenStraightToHex(awayGk, Hex(AwayGoalkeeperRestCoordinates), false, true)));
        }

        HexCell penaltySpot = Hex(PenaltySpotCoordinates);
        if (groundBallManager != null && penaltySpot != null)
        {
            setupMoves.Add(StartCoroutine(groundBallManager.HandleGroundBallMovement(penaltySpot, speed: 2, allowGKBoxMove: false)));
        }
        else if (ball != null && penaltySpot != null)
        {
            setupMoves.Add(StartCoroutine(MoveBallStraightToHex(penaltySpot)));
        }

        foreach (Coroutine setupMove in setupMoves)
        {
            yield return setupMove;
        }

        PlaceBallAtPenaltySpot(onGround: true);
    }

    private void RegisterTeamStaging(List<PlayerToken> tokens, bool home)
    {
        int max = Mathf.Min(tokens.Count, 10);
        for (int i = 0; i < max; i++)
        {
            Vector3Int coords = new(0, 0, home ? -(i + 1) : i + 1);
            stagingHexes[tokens[i]] = Hex(coords);
        }
    }

    private void AddTeamStagingMoves(List<Coroutine> moves, List<PlayerToken> tokens, bool home)
    {
        int max = Mathf.Min(tokens.Count, 10);
        for (int i = 0; i < max; i++)
        {
            Vector3Int coords = new(0, 0, home ? -(i + 1) : i + 1);
            HexCell target = Hex(coords);
            if (target != null)
            {
                moves.Add(StartCoroutine(MoveTokenStraightToHex(tokens[i], target, false, false)));
            }
        }
    }

    private IEnumerator PrepareNextKick()
    {
        HideOrderPanel();
        bool shooterIsHome = nextKickIsHome;
        currentShooter = ResolveShooter(shooterIsHome);
        currentDefendingGoalkeeper = GetEligiblePlayingTokens(!shooterIsHome).FirstOrDefault(token => token.IsGoalKeeper);

        if (currentShooter == null || currentDefendingGoalkeeper == null)
        {
            Debug.LogError("[Shootout] Cannot prepare kick because shooter or defending goalkeeper is missing.");
            yield break;
        }

        SetShootoutAttackContext(shooterIsHome);
        matchManager.currentState = MatchManager.GameState.PenaltyShootoutTransition;
        transitionInstructionShooter = currentShooter;
        transitionInstructionActive = true;

        List<Coroutine> moves = new();
        if (previousShooter != null
            && previousShooter != currentShooter
            && previousShooter != currentDefendingGoalkeeper)
        {
            moves.Add(StartCoroutine(MoveTokenStraightToHex(previousShooter, ResolveRestHex(previousShooter), false, false)));
        }

        if (previousDefendingGoalkeeper != null
            && previousDefendingGoalkeeper != currentDefendingGoalkeeper
            && previousDefendingGoalkeeper != currentShooter)
        {
            moves.Add(StartCoroutine(MoveTokenStraightToHex(previousDefendingGoalkeeper, ResolveRestHex(previousDefendingGoalkeeper), false, true)));
        }

        HexCell penaltySpot = Hex(PenaltySpotCoordinates);
        HexCell defendingGoalkeeperHex = Hex(DefendingGoalkeeperCoordinates);
        Coroutine shooterMove = null;
        Coroutine ballMove = null;
        bool shooterMustVacateGoalkeeperLine = currentShooter.GetCurrentHex() == defendingGoalkeeperHex;
        if (shooterMustVacateGoalkeeperLine)
        {
            shooterMove = StartCoroutine(MoveTokenStraightToHex(currentShooter, penaltySpot, false, false));
            yield return shooterMove;
            moves.Add(StartCoroutine(MoveTokenStraightToHex(currentDefendingGoalkeeper, defendingGoalkeeperHex, false, true)));
        }
        else
        {
            moves.Add(StartCoroutine(MoveTokenStraightToHex(currentDefendingGoalkeeper, defendingGoalkeeperHex, false, true)));
            shooterMove = StartCoroutine(MoveTokenStraightToHex(currentShooter, penaltySpot, false, false));
        }

        if (ball != null && ball.GetCurrentHex() != penaltySpot)
        {
            ballMove = StartCoroutine(MoveBallStraightToHex(penaltySpot));
        }

        if (!shooterMustVacateGoalkeeperLine && shooterMove != null)
        {
            yield return shooterMove;
        }

        if (ballMove != null)
        {
            yield return ballMove;
        }

        if (currentShooter.GetCurrentHex() != penaltySpot)
        {
            Debug.LogWarning($"[Shootout] Correcting shooter {currentShooter.name} from {currentShooter.GetCurrentHex()?.coordinates.ToString() ?? "null"} to penalty spot before kick.");
            currentShooter.SetCurrentHex(penaltySpot);
        }

        PlaceBallAtPenaltySpot(onGround: false);

        foreach (Coroutine move in moves)
        {
            yield return move;
        }

        transitionInstructionActive = false;
        matchManager.ClearLastTokenChain();
        matchManager.SetLastToken(currentShooter);
        matchManager.currentState = MatchManager.GameState.PenaltyShootoutKickExecution;
        Debug.Log($"[Shootout] {(shooterIsHome ? "Home" : "Away")} kick {homeAttempts + awayAttempts + 1}: {currentShooter.name} shoots at the west goal.");
        shotManager.StartPenaltyShotProcess(currentShooter, Hex(PenaltySpotCoordinates));
    }

    private IEnumerator HandlePenaltyShotResolved(bool scored)
    {
        HideOrderPanel();
        resolvingShot = true;
        bool shooterWasHome = currentShooter != null && currentShooter.isHomeTeam;
        if (shooterWasHome)
        {
            homeAttempts++;
            homeResults.Add(scored);
            if (scored) homeShootoutScore++;
        }
        else
        {
            awayAttempts++;
            awayResults.Add(scored);
            if (scored) awayShootoutScore++;
        }

        Debug.Log($"[Shootout] {(shooterWasHome ? "Home" : "Away")} {(scored ? "score" : "miss")}. Shootout score {homeShootoutScore}-{awayShootoutScore}.");
        previousShooter = currentShooter;
        previousDefendingGoalkeeper = currentDefendingGoalkeeper;
        currentShooter = null;
        currentDefendingGoalkeeper = null;

        if (TryResolveWinner())
        {
            yield return StartCoroutine(CompleteShootout());
            resolvingShot = false;
            yield break;
        }

        if (scored)
        {
            Debug.Log("[Shootout] Goal celebration flash.");
            goalFlashInstructionHome = shooterWasHome;
            goalFlashInstructionActive = true;
            yield return new WaitForSeconds(3.25f);
            goalFlashInstructionActive = false;
        }

        nextKickIsHome = !shooterWasHome;
        isSuddenDeath = homeAttempts >= 5 && awayAttempts >= 5;
        resolvingShot = false;
        yield return StartCoroutine(PrepareNextKick());
    }

    private IEnumerator ReturnBallToSpot()
    {
        HexCell spot = Hex(PenaltySpotCoordinates);
        if (ball != null && spot != null)
        {
            PlaceBallAtPenaltySpot(onGround: true);
        }

        yield return null;
    }

    private bool TryResolveWinner()
    {
        int homeRemaining = Mathf.Max(0, 5 - homeAttempts);
        int awayRemaining = Mathf.Max(0, 5 - awayAttempts);
        if (homeAttempts < 5 || awayAttempts < 5)
        {
            if (homeShootoutScore > awayShootoutScore + awayRemaining)
            {
                winnerIsHome = true;
                return true;
            }

            if (awayShootoutScore > homeShootoutScore + homeRemaining)
            {
                winnerIsHome = false;
                return true;
            }
        }

        if (homeAttempts >= 5 && awayAttempts >= 5 && homeAttempts == awayAttempts && homeShootoutScore != awayShootoutScore)
        {
            winnerIsHome = homeShootoutScore > awayShootoutScore;
            return true;
        }

        return false;
    }

    private IEnumerator CompleteShootout()
    {
        isComplete = true;
        isActive = false;
        matchManager.currentState = MatchManager.GameState.PenaltyShootoutComplete;
        winnerFlashInstructionHome = winnerIsHome;
        winnerFlashInstructionActive = true;
        List<PlayerToken> winners = GetEligiblePlayingTokens(winnerIsHome);
        List<HexCell> celebrationHexes = BuildWinnerCelebrationHexes(winnerIsHome);
        List<Coroutine> celebrationMoves = new();
        for (int i = 0; i < winners.Count && i < celebrationHexes.Count; i++)
        {
            celebrationMoves.Add(StartCoroutine(MoveTokenStraightToHex(winners[i], celebrationHexes[i], false, false)));
        }

        foreach (Coroutine celebrationMove in celebrationMoves)
        {
            yield return celebrationMove;
        }

        yield return new WaitForSeconds(1f);
        winnerFlashInstructionActive = false;
        matchManager.MarkPenaltyShootoutComplete();
        ActiveShootout = null;
        EndGamePanelManager.ShowPenaltyShootoutCompletePanel(BuildFinalScoreLine());
    }

    private List<HexCell> BuildWinnerCelebrationHexes(bool home)
    {
        int z = home ? -12 : 12;
        return new List<Vector3Int>
        {
            new(-18, 0, z),
            new(-18, 0, z > 0 ? 11 : -11),
            new(-18, 0, z > 0 ? 10 : -10),
            new(-17, 0, z),
            new(-17, 0, z > 0 ? 11 : -11),
            new(-17, 0, z > 0 ? 10 : -10),
            new(-16, 0, z),
            new(-16, 0, z > 0 ? 11 : -11),
            new(-16, 0, z > 0 ? 10 : -10),
            new(-15, 0, z),
            new(-15, 0, z > 0 ? 11 : -11),
        }.Select(Hex).Where(hex => hex != null).ToList();
    }

    public string GetInstructions()
    {
        if (winnerFlashInstructionActive)
        {
            string teamName = winnerFlashInstructionHome
                ? matchManager?.gameData?.gameSettings?.homeTeamName ?? "Home"
                : matchManager?.gameData?.gameSettings?.awayTeamName ?? "Away";
            return $"{teamName} WIN THE SHOOTOUT!!!";
        }

        if (goalFlashInstructionActive)
        {
            string teamName = goalFlashInstructionHome
                ? matchManager?.gameData?.gameSettings?.homeTeamName ?? "Home"
                : matchManager?.gameData?.gameSettings?.awayTeamName ?? "Away";
            return $"GOAL FOR {teamName}!!!";
        }

        if (!transitionInstructionActive || transitionInstructionShooter == null)
        {
            return string.Empty;
        }

        string homeName = matchManager?.gameData?.gameSettings?.homeTeamName ?? "Home";
        string awayName = matchManager?.gameData?.gameSettings?.awayTeamName ?? "Away";
        string shooterName = GetTokenDisplayName(transitionInstructionShooter);
        bool shooterIsHome = transitionInstructionShooter.isHomeTeam;
        string stakes = BuildKickStakesInstruction(shooterIsHome, shooterName, shooterIsHome ? homeName : awayName);
        string nextKickInfo = $"{shooterName} walks to take the Pen";
        if (!string.IsNullOrWhiteSpace(stakes))
        {
            nextKickInfo += $". {stakes}";
        }

        int nameWidth = Mathf.Max(homeName.Length, awayName.Length);
        int cellCount = Mathf.Max(5, Mathf.Max(homeResults.Count, awayResults.Count));
        string homeLine = FormatShootoutScoreLine(homeName, homeResults, nameWidth, cellCount);
        string awayLine = FormatShootoutScoreLine(awayName, awayResults, nameWidth, cellCount);
        return $"{homeLine}{(shooterIsHome ? $" - {nextKickInfo}" : string.Empty)}\n"
            + $"{awayLine}{(!shooterIsHome ? $" - {nextKickInfo}" : string.Empty)}";
    }

    public bool? IsInstructionExpectingHomeTeam()
    {
        if (winnerFlashInstructionActive)
        {
            return winnerFlashInstructionHome;
        }

        if (goalFlashInstructionActive)
        {
            return goalFlashInstructionHome;
        }

        if (transitionInstructionActive && transitionInstructionShooter != null)
        {
            return transitionInstructionShooter.isHomeTeam;
        }

        return null;
    }

    public bool ShouldFlashInstructionColors()
    {
        return goalFlashInstructionActive || winnerFlashInstructionActive;
    }

    private string BuildKickStakesInstruction(bool shooterIsHome, string shooterName, string shootingTeamName)
    {
        List<string> parts = new();
        if (WouldResolveWinnerAfterKick(shooterIsHome, true, out bool scoreWinnerIsHome)
            && scoreWinnerIsHome == shooterIsHome)
        {
            parts.Add($"{shootingTeamName} win if {shooterName} scores");
        }

        if (WouldResolveWinnerAfterKick(shooterIsHome, false, out bool missWinnerIsHome)
            && missWinnerIsHome != shooterIsHome)
        {
            parts.Add($"{shootingTeamName} lose if {shooterName} misses");
        }

        return string.Join(". ", parts);
    }

    private bool WouldResolveWinnerAfterKick(bool shooterIsHome, bool scored, out bool resolvedWinnerIsHome)
    {
        int projectedHomeScore = homeShootoutScore + (shooterIsHome && scored ? 1 : 0);
        int projectedAwayScore = awayShootoutScore + (!shooterIsHome && scored ? 1 : 0);
        int projectedHomeAttempts = homeAttempts + (shooterIsHome ? 1 : 0);
        int projectedAwayAttempts = awayAttempts + (!shooterIsHome ? 1 : 0);
        return TryResolveWinnerFor(
            projectedHomeScore,
            projectedAwayScore,
            projectedHomeAttempts,
            projectedAwayAttempts,
            out resolvedWinnerIsHome);
    }

    private bool TryResolveWinnerFor(int projectedHomeScore, int projectedAwayScore, int projectedHomeAttempts, int projectedAwayAttempts, out bool resolvedWinnerIsHome)
    {
        int homeRemaining = Mathf.Max(0, 5 - projectedHomeAttempts);
        int awayRemaining = Mathf.Max(0, 5 - projectedAwayAttempts);
        if (projectedHomeAttempts < 5 || projectedAwayAttempts < 5)
        {
            if (projectedHomeScore > projectedAwayScore + awayRemaining)
            {
                resolvedWinnerIsHome = true;
                return true;
            }

            if (projectedAwayScore > projectedHomeScore + homeRemaining)
            {
                resolvedWinnerIsHome = false;
                return true;
            }
        }

        if (projectedHomeAttempts >= 5 && projectedAwayAttempts >= 5 && projectedHomeAttempts == projectedAwayAttempts && projectedHomeScore != projectedAwayScore)
        {
            resolvedWinnerIsHome = projectedHomeScore > projectedAwayScore;
            return true;
        }

        resolvedWinnerIsHome = false;
        return false;
    }

    private string BuildFinalScoreLine()
    {
        string homeName = matchManager?.gameData?.gameSettings?.homeTeamName ?? "Home";
        string awayName = matchManager?.gameData?.gameSettings?.awayTeamName ?? "Away";
        return $"{homeName} a.p. {homeShootoutScore} ({HomePreShootoutGoals}-{AwayPreShootoutGoals}) {awayShootoutScore} {awayName}";
    }

    private bool TieBreakerIncludesExtraTime()
    {
        return (matchManager?.gameData?.gameSettings?.tiebreaker ?? string.Empty)
            .IndexOf("Extra Time", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private (int homeGoals, int awayGoals) CountRegulationGoals()
    {
        int homeGoals = matchManager?.homeScorers?.Count(goal => goal != null && goal.minute <= 90) ?? 0;
        int awayGoals = matchManager?.awayScorers?.Count(goal => goal != null && goal.minute <= 90) ?? 0;
        return (homeGoals, awayGoals);
    }

    private static string FormatShootoutScoreLine(string teamName, List<bool> results, int nameWidth, int cellCount)
    {
        int scoreStartPx = Mathf.Max(96, Mathf.CeilToInt((nameWidth * 9.5f) + 12f));
        return $"{teamName}<pos={scoreStartPx}px>- {FormatShootoutCells(results, cellCount)}";
    }

    private static string FormatShootoutCells(List<bool> results, int cellCount)
    {
        List<string> cells = new();
        for (int i = 0; i < cellCount; i++)
        {
            if (i >= results.Count)
            {
                cells.Add("[ ]");
            }
            else
            {
                cells.Add(results[i] ? "[O]" : "[X]");
            }
        }

        return string.Join(" ", cells);
    }

    private static string GetTokenDisplayName(PlayerToken token)
    {
        if (token == null)
        {
            return "Unknown";
        }

        string name = !string.IsNullOrWhiteSpace(token.playerName) ? token.playerName : token.name;
        return token.jerseyNumber > 0 ? $"{token.jerseyNumber}.{name}" : name;
    }

    private PlayerToken ResolveShooter(bool home)
    {
        List<PlayerToken> order = home ? homeOrder : awayOrder;
        int attempts = home ? homeAttempts : awayAttempts;
        return order.Count == 0 ? null : order[attempts % order.Count];
    }

    private List<PlayerToken> BuildDefaultOrder(bool home)
    {
        return GetEligiblePlayingTokens(home)
            .OrderByDescending(token => token.shooting)
            .ThenByDescending(token => token.jerseyNumber)
            .ToList();
    }

    private List<PlayerToken> GetEligiblePlayingTokens(bool home)
    {
        return playerTokenManager.GetPlayingTokens(home)
            .Where(token => token != null && token.isPlaying)
            .ToList();
    }

    private void SetShootoutAttackContext(bool shooterIsHome)
    {
        matchManager.teamInAttack = shooterIsHome ? MatchManager.TeamInAttack.Home : MatchManager.TeamInAttack.Away;
        matchManager.attackHasPossession = true;
        matchManager.homeTeamDirection = shooterIsHome
            ? MatchManager.TeamAttackingDirection.RightToLeft
            : MatchManager.TeamAttackingDirection.LeftToRight;
        matchManager.awayTeamDirection = shooterIsHome
            ? MatchManager.TeamAttackingDirection.LeftToRight
            : MatchManager.TeamAttackingDirection.RightToLeft;

        foreach (PlayerToken token in playerTokenManager.allTokens)
        {
            if (token != null)
            {
                token.isAttacker = token.isHomeTeam == shooterIsHome;
            }
        }

        RefreshAllOccupancyHighlights();
    }

    private IEnumerator MoveTokenToHex(PlayerToken token, HexCell target, bool carryBall)
    {
        if (token == null || target == null || token.GetCurrentHex() == target)
        {
            yield break;
        }

        if (target.GetOccupyingToken() != null && target.GetOccupyingToken() != token)
        {
            PlayerToken occupyingToken = target.GetOccupyingToken();
            HexCell occupantRest = ResolveRestHex(occupyingToken);
            if (occupantRest == null || occupantRest == target || (occupantRest.GetOccupyingToken() != null && occupantRest.GetOccupyingToken() != occupyingToken))
            {
                occupantRest = FindFreeHoldingHex();
            }

            yield return StartCoroutine(MoveTokenToHex(occupyingToken, occupantRest, false));
        }

        if (movementPhaseManager != null)
        {
            yield return StartCoroutine(movementPhaseManager.MoveTokenToHex(target, token, false, false, carryBall));
        }
        else
        {
            yield return StartCoroutine(token.JumpToHex(target));
        }

        if (carryBall && ball != null)
        {
            ball.PlaceAtCell(target);
        }
    }

    private IEnumerator MoveTokenStraightToHex(PlayerToken token, HexCell target, bool carryBall, bool isDefense)
    {
        if (token == null || target == null || token.GetCurrentHex() == target)
        {
            if (carryBall && ball != null && target != null)
            {
                ball.PlaceAtCell(target);
            }
            yield break;
        }

        if (target.GetOccupyingToken() != null && target.GetOccupyingToken() != token)
        {
            PlayerToken occupyingToken = target.GetOccupyingToken();
            HexCell occupantRest = ResolveRestHex(occupyingToken);
            if (occupantRest == null || occupantRest == target || (occupantRest.GetOccupyingToken() != null && occupantRest.GetOccupyingToken() != occupyingToken))
            {
                occupantRest = FindFreeHoldingHex();
            }

            yield return StartCoroutine(MoveTokenStraightToHex(occupyingToken, occupantRest, false, isDefense));
        }

        HexCell previousHex = token.GetCurrentHex();
        if (previousHex != null)
        {
            if (previousHex.occupyingToken == token)
            {
                previousHex.occupyingToken = null;
            }

            previousHex.isAttackOccupied = false;
            previousHex.isDefenseOccupied = false;
            previousHex.ResetHighlight();
        }

        Vector3 startPos = token.transform.position;
        Vector3 endPos = target.transform.position;
        float fixedY = startPos.y;
        endPos.y = fixedY;
        float distance = Vector3.Distance(startPos, endPos);
        float speed = 2f * (1 + ((token.pace - 3) * 0.3f));
        speed = Mathf.Max(0.5f, speed);

        float duration = speed > 0f ? distance / speed : 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            Vector3 interpolated = Vector3.Lerp(startPos, endPos, t);
            interpolated.y = fixedY;
            token.transform.position = interpolated;
            elapsed += Time.deltaTime;
            yield return null;
        }

        Vector3 finalPosition = endPos;
        finalPosition.y = fixedY;
        token.transform.position = finalPosition;
        token.SetCurrentHex(target);
        target.ResetHighlight();
        target.HighlightHex(token.isAttacker ? "isAttackOccupied" : "isDefenseOccupied");

        if (carryBall && ball != null)
        {
            ball.PlaceAtCell(target);
        }
    }

    private IEnumerator MoveBallStraightToHex(HexCell target)
    {
        if (ball == null || target == null)
        {
            yield break;
        }

        Vector3 startPos = ball.transform.position;
        Vector3 endPos = target.GetHexCenter();
        endPos.y += ball.groundHeightOffset;
        float distance = Vector3.Distance(startPos, endPos);
        float speed = 10f;
        float duration = speed > 0f ? distance / speed : 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            ball.transform.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        ball.transform.position = endPos;
        PlaceBallAtHex(target, onGround: true);
    }

    private void PlaceBallAtPenaltySpot(bool onGround)
    {
        PlaceBallAtHex(Hex(PenaltySpotCoordinates), onGround);
    }

    private void PlaceBallAtHex(HexCell target, bool onGround)
    {
        if (ball == null || target == null)
        {
            return;
        }

        if (!onGround)
        {
            ball.PlaceAtCell(target);
            return;
        }

        ball.SetCurrentHex(target);
        Vector3 groundPosition = target.GetHexCenter();
        groundPosition.y += ball.groundHeightOffset;
        ball.transform.position = groundPosition;
    }

    private HexCell ResolveRestHex(PlayerToken token)
    {
        if (token != null && stagingHexes.TryGetValue(token, out HexCell hex) && hex != null)
        {
            return hex;
        }

        if (token != null && token.IsGoalKeeper)
        {
            return Hex(token.isHomeTeam ? HomeGoalkeeperRestCoordinates : AwayGoalkeeperRestCoordinates);
        }

        return token != null ? token.GetCurrentHex() : null;
    }

    private HexCell FindFreeHoldingHex()
    {
        if (hexGrid == null)
        {
            return null;
        }

        for (int x = -2; x <= 2; x++)
        {
            for (int z = -12; z <= 12; z++)
            {
                HexCell candidate = Hex(new Vector3Int(x, 0, z));
                if (candidate != null
                    && !candidate.isOutOfBounds
                    && candidate.isInGoal == 0
                    && candidate.GetOccupyingToken() == null)
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private void RefreshAllOccupancyHighlights()
    {
        if (hexGrid?.cells == null)
        {
            return;
        }

        foreach (HexCell hex in hexGrid.cells)
        {
            if (hex == null)
            {
                continue;
            }

            PlayerToken token = hex.GetOccupyingToken();
            hex.isAttackOccupied = token != null && token.isAttacker;
            hex.isDefenseOccupied = token != null && !token.isAttacker;
            hex.ResetHighlight();
            if (token != null)
            {
                hex.HighlightHex(token.isAttacker ? "isAttackOccupied" : "isDefenseOccupied");
            }
        }
    }

    private void EnsureOrderPanel()
    {
        if (orderPanel != null)
        {
            Canvas existingTargetCanvas = ResolveMainGameCanvas();
            if (existingTargetCanvas != null && orderPanel.transform.parent != existingTargetCanvas.transform)
            {
                orderPanel.transform.SetParent(existingTargetCanvas.transform, false);
            }

            return;
        }

        Canvas canvas = ResolveMainGameCanvas();
        if (canvas == null)
        {
            GameObject canvasObject = new("PenaltyShootoutCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        orderPanel = canvas.GetComponentsInChildren<PenaltyShootoutOrderPanelController>(true)
            .FirstOrDefault(controller => controller != null && controller.name == "PenaltyShootoutOrderPanel");
        if (orderPanel != null)
        {
            orderPanel.transform.SetParent(canvas.transform, false);
            return;
        }

        PenaltyShootoutOrderPanelController prefab = Resources.Load<PenaltyShootoutOrderPanelController>(OrderPanelResourcePath);
        orderPanel = prefab != null
            ? Instantiate(prefab, canvas.transform)
            : new GameObject("PenaltyShootoutOrderPanel", typeof(RectTransform), typeof(Image), typeof(PenaltyShootoutOrderPanelView), typeof(PenaltyShootoutOrderPanelController)).GetComponent<PenaltyShootoutOrderPanelController>();

        if (prefab == null)
        {
            orderPanel.transform.SetParent(canvas.transform, false);
        }
    }

    private void HideOrderPanel()
    {
        if (orderPanel != null && orderPanel.gameObject.activeSelf)
        {
            orderPanel.gameObject.SetActive(false);
        }
    }

    private static Canvas ResolveMainGameCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        Canvas namedCanvas = canvases.FirstOrDefault(candidate => candidate != null && candidate.name == "Canvas");
        if (namedCanvas != null)
        {
            return namedCanvas;
        }

        return canvases.FirstOrDefault(candidate =>
            candidate != null
            && candidate.isRootCanvas
            && candidate.name != "HoveredTokenNameCanvas");
    }

    private HexCell Hex(Vector3Int coordinates)
    {
        return hexGrid != null ? hexGrid.GetHexCellAt(coordinates) : null;
    }

    private static string FormatResults(List<bool> results)
    {
        return results.Count == 0 ? "-" : string.Join(" ", results.Select(result => result ? "G" : "M"));
    }
}
