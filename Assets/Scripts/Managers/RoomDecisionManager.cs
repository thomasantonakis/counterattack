using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

public class RoomDecisionManager : MonoBehaviour
{
    public TextMeshProUGUI decisionText;
    public GameInputManager gameInputManager;
    public GameObject thinkingPanel;
    [SerializeField, Range(1f, 12f)] private float decisionFrameBudgetMilliseconds = 6f;
    [SerializeField] private float longestDecisionBatchMilliseconds;
    private Coroutine automatedAnalysis;
    private string automatedAnalysisStateKey;
    private GameplayInstructionSnapshot executionInstruction;
    public bool IsThinking => automatedAnalysis != null;

    public void SuspendThinking()
    {
        if (thinkingPanel != null) thinkingPanel.SetActive(false);
        DisarmAutomatedExecution();
    }

    private bool CanDispatchAutomatedCandidate(RoomActionCandidate candidate)
    {
        var match = MatchManager.Instance;
        if (match == null || match.IsGameplayInputBlocked || CoinTossPanelController.IsRoomVisible || HeaderChallengePanelController.IsHoldingContinuation || DuelResolutionPanelController.IsHoldingContinuation || StoppageActionsPanelController.IsHoldingContinuation || match.freeKickManager?.IsResolvingSetup == true
            || match.movementPhaseManager?.IsResolvingReposition == true
            || IsMovementBlockingDecision(executionInstruction) || match.movementPhaseManager?.goalKeeperManager?.IsResolvingMove == true
            || match.shotManager?.IsMovingSnapshotBlocker == true)
            return false;
        var live = GameDebugMonitor.Instance?.GetCurrentInstructionSnapshotForLog();
        if (live == null || !live.isAwaitingInput || executionInstruction == null
            || live.expectedTeam != executionInstruction.expectedTeam || live.manager != executionInstruction.manager) return false;
        if (!string.IsNullOrEmpty(activeKickoffAutomationTeam)) return candidate.manager == nameof(KickoffManager);
        return live.manager?.Split(',').Select(name => name.Trim()).Contains(candidate.manager) == true
            && AIManager.ResolveRoomPersona(match.gameData?.gameSettings, live.expectedTeam) != AIManager.RoomPersona.AskHuman;
    }

    private static bool IsMovementBlockingDecision(GameplayInstructionSnapshot instruction)
    {
        var match = MatchManager.Instance;
        var movement = match?.movementPhaseManager;
        if (movement?.isPlayerMoving != true) return false;
        var loose = match.looseBallManager;
        if (instruction?.manager?.Split(',').Contains(nameof(LooseBallManager)) == true
            && loose?.isActivated == true
            && (loose.isWaitingForDirectionRoll || loose.isWaitingForDistanceRoll || loose.isWaitingForInterceptionRoll))
            return false;
        // The outer dribble coroutine waits for these choices. Blocking their
        // input on that coroutine's moving flag creates a circular wait.
        if (instruction?.manager == nameof(GoalKeeperManager)
            && match.movementPhaseManager.goalKeeperManager?.isActivated == true
            && !match.movementPhaseManager.goalKeeperManager.IsResolvingMove) return false;
        if (instruction?.manager == nameof(MovementPhaseManager)
            && (movement.isWaitingForReposition || movement.isWaitingForFoulDecision
                || movement.isWaitingForYellowCardRoll || movement.isWaitingForInjuryRoll)) return false;
        return true;
    }

    [Header("Decision Inspector")]
    [SerializeField] private string lastManager;
    [SerializeField] private string lastExpectedTeam;
    [SerializeField] private string lastPersona;
    [SerializeField] private string lastAvailableActions;
    [SerializeField] private string lastSelectedAction;
    [SerializeField] private List<string> lastActionCandidates = new();
    [SerializeField] private List<string> lastValidKeys = new();
    [SerializeField] private List<string> lastActionSummaries = new();
    [SerializeField] private List<string> lastCodexTacticianScores = new();
    [SerializeField] private List<string> recentCodexTacticianDecisionHistory = new();
    [SerializeField] private string lastCodexTacticianPlan;
    [SerializeField] private RoomAITacticalWeights codexTacticianWeights = new();
    [SerializeField] private string lastCodexTacticianDifficulty;
    [SerializeField] private string lastCodexTacticianReason;
    [SerializeField] private float lastCodexTacticianDurationMilliseconds;
    [SerializeField] private int lastCodexTacticianCandidateCount;
    [SerializeField] private int lastCodexTacticianSelectedScore;
    private readonly List<GameplayRoomDecisionCandidate> lastCodexPersistedAlternatives = new();
    [Header("Tactical difficulty override")]
    [SerializeField] private bool forceBothCodexTeamsUltraHard = true;
    [Header("Human Shadow calibration")]
    [SerializeField] private bool evaluateAskHumanWithCodexShadow = true;
    [SerializeField] private string lastHumanShadowChoice;
    [SerializeField] private int lastHumanShadowRank;
    [SerializeField] private int lastHumanShadowScoreDelta;
    [Header("Decision Performance")]
    [SerializeField] private int tacticalEvaluationCount;
    [SerializeField] private int tacticalEvaluationCacheHits;
    [SerializeField] private float lastTacticalEvaluationMilliseconds;
    [SerializeField] private int lastTacticalEvaluationCandidateCount;
    [SerializeField] private string lastTacticalEvaluationStateKey;
    [SerializeField, Min(0f)] private float slowEvaluationWarningMilliseconds = 16f;
    [SerializeField] private List<PlayerToken> lastValidTokens = new();
    [SerializeField] private List<HexCell> lastValidHexes = new();
    [SerializeField] private string inspectorStatus;
    [SerializeField] private string lastCommittedBranch;
    [SerializeField] private PlayerToken lastCommittedActor;
    [SerializeField] private string lastExecutionLock;

    private string lastLoggedFingerprint = string.Empty;
    private string lastAutomatedExecutionFingerprint = string.Empty;
    private string lastAutomatedSelectedAction = string.Empty;
    private string committedManager = string.Empty;
    private RoomActionType committedActionType = RoomActionType.Unknown;
    private RoomActionType executionLockActionType = RoomActionType.Unknown;
    private RoomAIAttackPlan currentCodexTacticianPlan;
    private RoomAIAttackPlan currentHumanShadowPlan;
    private RoomAIProviderDecision pendingHumanShadowDecision;
    private RoomAIDecisionSnapshot pendingHumanShadowSnapshot;
    private string pendingHumanShadowStateKey = string.Empty;
    private Coroutine humanShadowAnalysis;
    private RoomAIHumanShadowCache humanShadowCache;
    [SerializeField, Range(1f, 12f)] private float humanShadowFrameBudgetMilliseconds = 6f;
    [SerializeField] private float lastHumanShadowLongestBatchMilliseconds;
    private bool hasCachedDecisionEvaluation;
    private string cachedDecisionDisplayText = string.Empty;
    private string evaluatingDecisionStateKey = string.Empty;
    private string armedAutomatedDecisionStateKey = string.Empty;
    private int armedAutomatedDecisionFrame = -1;
    private string armedCodexCandidateIdentity = string.Empty;
    private string preparedCodexStateKey = string.Empty;
    private string codexDecisionTimingStateKey = string.Empty;
    private readonly System.Diagnostics.Stopwatch codexDecisionWall = new();
    private int codexCandidateEnumerationSkips;
    private RoomAIDecisionSnapshot preparedCodexSnapshot;
    private RoomAIProviderDecision preparedCodexDecision;
    private string selectedGroundTargetBoardKey = string.Empty;
    private RoomHexCoordinates selectedGroundTargetHex;
    private bool groundConfirmationReopened;
    private string groundConfirmationReplanReason = string.Empty;
    private string movementReplanKey = string.Empty;
    private readonly List<RoomAITacticalEvaluation> movementReplanActors = new();
    private readonly HashSet<string> movementReplanTried = new();
    private RoomActionType pendingSuccessfulTackleAction = RoomActionType.Unknown;
    private string pendingSuccessfulTackleTeam = string.Empty;
    private string pendingSuccessfulTackleReceiverKey = string.Empty;
    private RoomHexCoordinates pendingSuccessfulTackleTargetHex;
    private RoomHexCoordinates pendingDribbleStopHex;
    private string pendingDribbleRouteTeam = string.Empty;
    private string pendingDribbleRouteTokenKey = string.Empty;
    private string pendingDribbleEnvironmentKey = string.Empty;
    private readonly List<RoomHexCoordinates> pendingDribbleRoute = new();
    private CommittedStagedDecision committedStagedDecision;
    private string activeKickoffAutomationTeam = string.Empty;
    private RoomAIDefensivePhasePlan committedDefensivePhasePlan;
    private string committedDefensivePhaseTeam = string.Empty;
    private RoomHexCoordinates committedDefensivePhaseBall;
    private readonly List<string> recentCodexMovementAttackStateSignatures = new();
    private const int RecentCodexMovementStateLimit = 8;

    [System.Serializable]
    private sealed class CommittedStagedDecision
    {
        public string manager;
        public RoomActionType actionType;
        public string actorTokenKey;
        public string targetTokenKey;
        public RoomHexCoordinates exactTarget;
        public readonly List<RoomDecisionStep> requiredIntermediateClicks = new();
        public string expectedTacticalOutcome;
        public string possessionSupportTokenKey;
        public RoomHexCoordinates possessionSupportDestination;
        public string likelyTackleWinnerTokenKey;
        public RoomHexCoordinates likelyTackleWinnerExit;
        public string boardStateKey;
        public string invalidationConditions;
    }

    private void OnValidate()
    {
        InvalidateDecisionEvaluationCache();
    }

    private void OnEnable()
    {
        GameInputManager.OnPhysicalClickCompleted += OnPhysicalClickCompleted;
        GameInputManager.OnPhysicalKeyPressCompleted += OnPhysicalKeyPressCompleted;
    }

    private void OnDisable()
    {
        CancelAutomatedAnalysis();
        CancelHumanShadowAnalysis();
        humanShadowCache = null;
        ClearPendingHumanShadowDecision();
        GameInputManager.OnPhysicalClickCompleted -= OnPhysicalClickCompleted;
        GameInputManager.OnPhysicalKeyPressCompleted -= OnPhysicalKeyPressCompleted;
    }

    public void ClearDecisionState()
    {
        CancelAutomatedAnalysis();
        CancelHumanShadowAnalysis();
        humanShadowCache = null;
        if (decisionText != null)
        {
            decisionText.text = string.Empty;
        }

        ClearInspectorState();
        ClearPendingHumanShadowDecision();
        ClearPendingDribbleRoute();
        ClearCommittedStagedDecision();
        ClearCommittedDefensivePhasePlan();
        lastLoggedFingerprint = string.Empty;
        DisarmAutomatedExecution();
        InvalidateDecisionEvaluationCache();
    }

    public void UpdateDecision(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context,
        MatchManager.GameSettings settings,
        bool noDecisionNeeded = false,
        string noDecisionReason = "")
    {
        if (MatchManager.Instance?.IsGameplayInputBlocked == true || CoinTossPanelController.IsRoomVisible || HeaderChallengePanelController.IsHoldingContinuation || DuelResolutionPanelController.IsHoldingContinuation || StoppageActionsPanelController.IsHoldingContinuation)
        {
            SuspendThinking();
            return;
        }
        executionInstruction = snapshot;
        if (MatchManager.Instance?.freeKickManager?.IsResolvingSetup == true
            || MatchManager.Instance?.movementPhaseManager?.IsResolvingReposition == true
            || IsMovementBlockingDecision(snapshot)
            || MatchManager.Instance?.movementPhaseManager?.goalKeeperManager?.IsResolvingMove == true
            || MatchManager.Instance?.shotManager?.IsMovingSnapshotBlocker == true)
        {
            CancelAutomatedAnalysis();
            DisarmAutomatedExecution();
            return;
        }
        if (decisionText == null)
        {
            return;
        }

        if (noDecisionNeeded)
        {
            CancelHumanShadowAnalysis();
            activeKickoffAutomationTeam = string.Empty;
            ClearCommittedDefensivePhasePlanIfPhaseEnded();
            DisarmAutomatedExecution();
            ClearPendingHumanShadowDecision();
            InvalidateDecisionEvaluationCache();
            decisionText.text = string.IsNullOrWhiteSpace(noDecisionReason)
                ? "No Decision needed."
                : $"No Decision needed. {noDecisionReason}.";
            inspectorStatus = string.IsNullOrWhiteSpace(noDecisionReason)
                ? "No decision needed. Last AI decision retained for inspection."
                : $"No decision needed ({noDecisionReason}). Last AI decision retained for inspection.";

            string noDecisionFingerprint = $"no_decision|{noDecisionReason}";
            if (noDecisionFingerprint != lastLoggedFingerprint)
            {
                Debug.Log($"Room decision not needed: {noDecisionReason}");
                lastLoggedFingerprint = noDecisionFingerprint;
            }

            return;
        }

        if (snapshot == null || !snapshot.isAwaitingInput)
        {
            CancelHumanShadowAnalysis();
            activeKickoffAutomationTeam = string.Empty;
            ClearCommittedDefensivePhasePlanIfPhaseEnded();
            DisarmAutomatedExecution();
            ClearPendingHumanShadowDecision();
            InvalidateDecisionEvaluationCache();
            decisionText.text = "No Decision needed.";
            inspectorStatus = "No decision pending. Last AI decision retained for inspection.";
            lastLoggedFingerprint = string.Empty;
            return;
        }

        activeKickoffAutomationTeam = ResolveKickoffAutomationTeam(settings, snapshot);
        string personaTeam = !string.IsNullOrWhiteSpace(activeKickoffAutomationTeam)
            ? activeKickoffAutomationTeam
            : snapshot.expectedTeam;
        AIManager.RoomPersona persona = AIManager.ResolveRoomPersona(settings, personaTeam);
        if (persona == AIManager.RoomPersona.AskHuman && !ShouldEvaluateHumanShadow(settings))
        {
            CancelAutomatedAnalysis();
            DisarmAutomatedExecution();
            if (decisionText != null) decisionText.text = "Your turn";
            return;
        }
        AIManager.CodexTacticianDifficulty codexTacticianDifficulty =
            AIManager.ResolveCodexTacticianDifficulty(settings, personaTeam);
        if (forceBothCodexTeamsUltraHard
            && (persona == AIManager.RoomPersona.CodexTactician
                || (persona == AIManager.RoomPersona.AskHuman && evaluateAskHumanWithCodexShadow)))
        {
            codexTacticianDifficulty = AIManager.CodexTacticianDifficulty.UltraHard;
        }
        evaluatingDecisionStateKey = BuildDecisionStateKey(
            snapshot,
            context,
            persona,
            codexTacticianDifficulty);
        bool automatedExecutionPending = IsAutomatedPersona(persona)
            && !string.Equals(
                evaluatingDecisionStateKey,
                lastAutomatedExecutionFingerprint,
                System.StringComparison.Ordinal);
        if (hasCachedDecisionEvaluation
            && !automatedExecutionPending
            && string.Equals(
                evaluatingDecisionStateKey,
                lastTacticalEvaluationStateKey,
                System.StringComparison.Ordinal))
        {
            tacticalEvaluationCacheHits++;
            decisionText.text = cachedDecisionDisplayText;
            inspectorStatus = "Current/last AI decision (cached; board state unchanged).";
            return;
        }

        System.Diagnostics.Stopwatch evaluationTimer = System.Diagnostics.Stopwatch.StartNew();
        string availableActions = BuildAvailableActionsSummary(snapshot, context);
        string selectedAction = BuildSelectedActionSummary(
            snapshot,
            context,
            persona,
            codexTacticianDifficulty,
            availableActions);

        decisionText.text = BuildDecisionDisplayText(persona, selectedAction);
        UpdateInspectorState(snapshot, context, persona, availableActions, selectedAction);
        evaluationTimer.Stop();
        tacticalEvaluationCount++;
        lastTacticalEvaluationMilliseconds = (float)evaluationTimer.Elapsed.TotalMilliseconds;
        lastTacticalEvaluationCandidateCount = context != null ? context.actions.Count : 0;
        lastTacticalEvaluationStateKey = evaluatingDecisionStateKey;
        cachedDecisionDisplayText = decisionText.text;
        hasCachedDecisionEvaluation = true;
        if (slowEvaluationWarningMilliseconds > 0f
            && lastTacticalEvaluationMilliseconds >= slowEvaluationWarningMilliseconds)
        {
            Debug.LogWarning(
                $"Room tactical evaluation took {lastTacticalEvaluationMilliseconds:F2} ms "
                + $"for {lastTacticalEvaluationCandidateCount} candidate(s) in {snapshot.manager}. "
                + $"stateKey={lastTacticalEvaluationStateKey}");
        }

        string fingerprint = BuildFingerprint(snapshot, persona, availableActions);
        if (fingerprint != lastLoggedFingerprint)
        {
            int candidateCount = context != null ? context.actions.Count : 0;
            Debug.Log($"Room decision needed: manager={snapshot.manager}, team={snapshot.expectedTeam}, persona={AIManager.FormatRoomPersona(persona)}, candidateCount={candidateCount}");
            lastLoggedFingerprint = fingerprint;
        }
    }

    private static string BuildDecisionDisplayText(AIManager.RoomPersona persona, string selectedAction)
    {
        if (persona == AIManager.RoomPersona.AskHuman)
        {
            return "Input needed! Ask Human!";
        }

        string personaLabel = AIManager.FormatRoomPersona(persona);
        return string.IsNullOrWhiteSpace(selectedAction)
            ? $"Input needed! Profile selected: {personaLabel}."
            : $"Input needed! Profile selected: {personaLabel}. {selectedAction}";
    }

    public static AIManager.RoomPersona ResolveDecisionPersona(
        MatchManager.GameSettings settings,
        GameplayInstructionSnapshot snapshot)
    {
        // Setup is shared by both teams. Its Neutral prompt must still reach the
        // unconfirmed CPU, even when the match's default controller is human.
        string setupTeam = ResolveKickoffAutomationTeam(settings, snapshot);
        return AIManager.ResolveRoomPersona(settings,
            string.IsNullOrEmpty(setupTeam) ? snapshot?.expectedTeam : setupTeam);
    }

    private static string ResolveKickoffAutomationTeam(
        MatchManager.GameSettings settings,
        GameplayInstructionSnapshot snapshot)
    {
        if (settings == null
            || snapshot == null
            || !string.Equals(snapshot.manager, nameof(KickoffManager), System.StringComparison.Ordinal)
            || !string.Equals(snapshot.expectedTeam, "Neutral", System.StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        KickoffManager kickoff = MatchManager.Instance?.kickoffManager;
        foreach (string team in new[] { "Home", "Away" })
        {
            if (IsAutomatedPersona(AIManager.ResolveRoomPersona(settings, team))
                && kickoff?.IsSetupConfirmedForTeam(team) != true)
            {
                return team;
            }
        }

        return string.Empty;
    }

    private static string BuildAvailableActionsSummary(GameplayInstructionSnapshot snapshot, RoomDecisionContext context)
    {
        List<string> actions = new List<string>();
        bool hasStructuredClickOptions = false;
        if (context != null)
        {
            foreach (RoomActionCandidate action in context.actions)
            {
                string summary = FormatActionCandidate(action);
                if (!string.IsNullOrWhiteSpace(summary))
                {
                    AddDistinct(actions, summary);
                    if (action.actor != null || action.targetToken != null || action.targetHex != null)
                    {
                        hasStructuredClickOptions = true;
                    }
                }
            }

            foreach (string summary in context.validActionSummaries)
            {
                if (!string.IsNullOrWhiteSpace(summary))
                {
                    AddDistinct(actions, summary);
                    hasStructuredClickOptions = true;
                }
            }

            foreach (string key in context.validKeys)
            {
                if (!string.IsNullOrWhiteSpace(key) && !HasKeyActionSummary(actions, key))
                {
                    AddDistinct(actions, $"Press [{key.Trim()}]");
                }
            }

            if (context.validActionSummaries.Count == 0)
            {
                foreach (PlayerToken token in context.validTokens)
                {
                    AddDistinct(actions, FormatTokenAction(token));
                    hasStructuredClickOptions = true;
                }

                foreach (HexCell hex in context.validHexes)
                {
                    AddDistinct(actions, FormatHexAction(hex));
                    hasStructuredClickOptions = true;
                }
            }
        }

        if (actions.Count == 0 && snapshot.expectedKeys != null)
        {
            foreach (string key in snapshot.expectedKeys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    AddDistinct(actions, $"Press [{key.Trim()}]");
                }
            }
        }

        if (!hasStructuredClickOptions)
        {
            AddClickActions(actions, snapshot.expectedInput, snapshot.instructionText);
        }

        return actions.Count > 0
            ? string.Join(", ", actions)
            : "None detected";
    }

    private string BuildSelectedActionSummary(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context,
        AIManager.RoomPersona persona,
        AIManager.CodexTacticianDifficulty codexTacticianDifficulty,
        string availableActions)
    {
        if (persona == AIManager.RoomPersona.AskHuman)
        {
            DisarmAutomatedExecution();
            lastAutomatedExecutionFingerprint = string.Empty;
            lastAutomatedSelectedAction = string.Empty;
            ClearCommittedBranch();
            ClearExecutionLock();
            // Shadow calibration is for the explicitly documented Human/Human
            // mode. A human's turn against the CPU needs no tactical search.
            return ShouldEvaluateHumanShadow(MatchManager.Instance?.gameData?.gameSettings)
                ? BuildHumanShadowSelectedActionSummary(snapshot, context)
                : "Ask Human!";
        }

        if (persona == AIManager.RoomPersona.CodexTactician)
        {
            return BuildCodexTacticianSelectedActionSummary(
                snapshot,
                context,
                codexTacticianDifficulty);
        }

        lastAutomatedExecutionFingerprint = string.Empty;
        lastAutomatedSelectedAction = string.Empty;
        ClearCodexTacticianState();
        ClearCommittedBranch();
        ClearExecutionLock();
        return "No automated room action implemented yet.";
    }

    public bool ShouldAnalyzeHumanTurn(MatchManager.GameSettings settings) => ShouldEvaluateHumanShadow(settings);

    private bool ShouldEvaluateHumanShadow(MatchManager.GameSettings settings)
    {
        // Human actions are recorded cheaply for tactical notes. Automatic
        // calibration search must not consume a human turn, including Human/Human.
        return false;
    }

    private string BuildHumanShadowSelectedActionSummary(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context)
    {
        List<RoomActionCandidate> candidates = BuildTacticalExecutableCandidates(
            snapshot,
            context,
            preferCuratedCandidates: false);
        if (candidates.Count == 0)
        {
            CancelHumanShadowAnalysis();
            ClearPendingHumanShadowDecision();
            lastCodexTacticianScores.Clear();
            return "Ask Human! Human Shadow found no executable candidate for this input stage.";
        }

        CancelHumanShadowAnalysis();
        bool groundPass = context?.manager == nameof(GroundBallManager);
        string boardKey = groundPass ? BuildHumanShadowBoardKey(snapshot.expectedTeam) : null;
        if (groundPass && humanShadowCache?.boardKey != boardKey)
            humanShadowCache = new RoomAIHumanShadowCache { boardKey = boardKey };
        if (!groundPass) humanShadowCache = null;
        // Large Human Shadow target searches must never run inside the input/UI
        // update. Automated teams retain their original synchronous evaluation.
        if (groundPass && candidates.Count > 32)
        {
            var deferredSnapshot = RoomAIDecisionSnapshotBuilder.Build(snapshot, context, candidates,
                includeCandidateExplanations: false);
            humanShadowCache.Attach(deferredSnapshot);
            pendingHumanShadowSnapshot = deferredSnapshot;
            pendingHumanShadowStateKey = evaluatingDecisionStateKey;
            pendingHumanShadowDecision = new RoomAIProviderDecision
            {
                providerName = CodexTacticianProvider.DisplayName,
                difficulty = AIManager.CodexTacticianDifficulty.UltraHard,
                analysisComplete = false,
                legalCandidateCount = candidates.Count,
                reasonSummary = "Human Shadow analysis in progress; no complete ranking yet."
            };
            lastCodexTacticianScores.Clear();
            lastHumanShadowLongestBatchMilliseconds = 0f;
            humanShadowAnalysis = StartCoroutine(AnalyzeHumanShadowGroundPass(
                deferredSnapshot, candidates, humanShadowCache, evaluatingDecisionStateKey));
            return $"Ask Human! Human Shadow is analyzing {candidates.Count} legal option(s) without blocking input.";
        }

        RoomAIDecisionSnapshot decisionSnapshot = RoomAIDecisionSnapshotBuilder.Build(
            snapshot,
            context,
            candidates);
        if (groundPass)
        {
            // Warm target-stage caches from the preceding root-action analysis.
            humanShadowCache.Remember(decisionSnapshot);
            foreach (var facts in decisionSnapshot.candidateExplanations)
                if (facts.actionType != RoomActionType.HighPass && facts.actionType != RoomActionType.GoalkeeperKick
                    || facts.highPassTargetEvaluationComplete)
                    humanShadowCache.explanations[RoomAIHumanShadowCache.CandidateKey(facts.candidate)] = facts.ForCandidate(facts.candidate);
        }
        RoomAIProviderDecision decision = CodexTacticianProvider.Instance.Decide(
            decisionSnapshot,
            candidates,
            currentHumanShadowPlan,
            codexTacticianWeights,
            AIManager.CodexTacticianDifficulty.UltraHard);
        currentHumanShadowPlan = decision.plan;
        pendingHumanShadowDecision = decision;
        pendingHumanShadowSnapshot = decisionSnapshot;
        pendingHumanShadowStateKey = evaluatingDecisionStateKey;

        lastCodexTacticianPlan = FormatTacticalPlan(currentHumanShadowPlan);
        lastCodexTacticianScores = FormatCodexTacticianCandidates(
            decision.rankedCandidates,
            decision.selectedEvaluation,
            decisionSnapshot);
        lastCodexTacticianDifficulty = decision.difficulty.ToString();
        lastCodexTacticianReason = decision.reasonSummary;
        lastCodexTacticianDurationMilliseconds = (float)decision.durationMilliseconds;
        lastCodexTacticianCandidateCount = decision.candidateCount;
        lastCodexTacticianSelectedScore = decision.selectedEvaluation?.score ?? 0;

        return decision.selectedCandidate == null
            ? "Ask Human! Human Shadow could not rank this input stage."
            : $"Ask Human! Human Shadow evaluated {decision.rankedCandidates.Count} legal option(s).";
    }

    private IEnumerator AnalyzeHumanShadowGroundPass(RoomAIDecisionSnapshot snapshot,
        List<RoomActionCandidate> candidates, RoomAIHumanShadowCache cache, string stateKey)
    {
        var timer = new System.Diagnostics.Stopwatch();
        double elapsedWork = 0;
        var work = RoomAIDecisionSnapshotBuilder.BuildCandidateExplanationsIncrementally(snapshot, cache);
        var batches = RoomAIHumanShadowCache.InFrameBatches(work,
            System.Math.Max(1f, humanShadowFrameBudgetMilliseconds), () => timer.Elapsed.TotalMilliseconds);
        while (true)
        {
            if (MatchManager.Instance?.IsGameplayInputBlocked == true) { yield return null; continue; }
            if (pendingHumanShadowStateKey != stateKey
                || cache.boardKey != BuildHumanShadowBoardKey(snapshot.expectedTeam)) break;
            timer.Restart();
            bool more = batches.MoveNext();
            timer.Stop();
            elapsedWork += timer.Elapsed.TotalMilliseconds;
            lastHumanShadowLongestBatchMilliseconds = System.Math.Max(lastHumanShadowLongestBatchMilliseconds,
                (float)timer.Elapsed.TotalMilliseconds);
            if (!more)
            {
                var decision = CodexTacticianProvider.Instance.Decide(snapshot, candidates,
                    currentHumanShadowPlan, codexTacticianWeights, AIManager.CodexTacticianDifficulty.UltraHard);
                decision.legalCandidateCount = candidates.Count;
                decision.durationMilliseconds += elapsedWork;
                currentHumanShadowPlan = decision.plan;
                pendingHumanShadowDecision = decision;
                lastCodexTacticianPlan = FormatTacticalPlan(currentHumanShadowPlan);
                lastCodexTacticianScores = FormatCodexTacticianCandidates(decision.rankedCandidates,
                    decision.selectedEvaluation, snapshot);
                lastCodexTacticianDurationMilliseconds = (float)decision.durationMilliseconds;
                lastCodexTacticianCandidateCount = decision.candidateCount;
                lastCodexTacticianSelectedScore = decision.selectedEvaluation?.score ?? 0;
                lastCodexTacticianDifficulty = decision.difficulty.ToString();
                lastCodexTacticianReason = decision.reasonSummary;
                Debug.Log($"[Human Shadow] Ground pass analysis complete: options={candidates.Count}, "
                    + $"workMs={decision.durationMilliseconds:0.00}, longestBatchMs={lastHumanShadowLongestBatchMilliseconds:0.00}, "
                    + $"state={stateKey}");
                break;
            }
            lastCodexTacticianReason = $"Human Shadow analyzing {snapshot.candidateExplanations.Count}/{candidates.Count}; input remains available.";
            yield return null;
        }
        (work as System.IDisposable)?.Dispose();
        (batches as System.IDisposable)?.Dispose();
        humanShadowAnalysis = null;
    }

    private void CancelHumanShadowAnalysis()
    {
        if (humanShadowAnalysis != null) StopCoroutine(humanShadowAnalysis);
        humanShadowAnalysis = null;
    }

    private string BuildHumanShadowBoardKey(string team)
    {
        // Deliberately exclude hovered/selected targets, prompt wording, and the
        // confirm actions. Include the live board, player attributes and rules.
        string board = BuildDecisionStateKeyCore(new GameplayInstructionSnapshot
        {
            manager = nameof(GroundBallManager), expectedTeam = team
        }, null, AIManager.RoomPersona.AskHuman,
            AIManager.CodexTacticianDifficulty.UltraHard, includePassSelection: false);
        ulong hash = 14695981039346656037UL;
        AddDecisionHash(ref hash, board);
        var match = MatchManager.Instance;
        AddDecisionHash(ref hash, match?.GetEntityId().ToString());
        AddDecisionHash(ref hash, match?.hexGrid?.GetEntityId().ToString());
        AddDecisionHash(ref hash, match?.difficulty_level ?? 0);
        AddDecisionHash(ref hash, match?.highPassManager?.isGoalkeeperKick ?? false);
        AddDecisionHash(ref hash, match?.highPassManager?.isCornerKick ?? false);
        AddDecisionHash(ref hash, match?.highPassManager?.minPassDistance ?? 0);
        var pass = match?.groundBallManager;
        AddDecisionHash(ref hash, pass?.isQuickThrow ?? false);
        AddDecisionHash(ref hash, pass?.isKickoffPass ?? false);
        AddDecisionHash(ref hash, pass?.imposedDistance ?? 0);
        AddDecisionHash(ref hash, match?.isHalfExpired ?? false);
        AddDecisionHash(ref hash, match?.extraActionsDetermined ?? false);
        AddDecisionHash(ref hash, match?.extraActionsRemaining ?? 0);
        if (match?.movementPhaseManager != null)
        {
            foreach (var token in match.movementPhaseManager.movedTokens) AddDecisionHash(ref hash, token);
            foreach (var token in match.movementPhaseManager.stunnedTokens) AddDecisionHash(ref hash, token);
        }
        return hash.ToString("x16");
    }

    private void OnPhysicalClickCompleted(ClickInputData input)
    {
        if (input == null || !input.isConsumed)
        {
            return;
        }

        CancelAutomatedAnalysis();
        preparedCodexDecision = null;
        RoomActionCandidate candidate = MatchHumanClickCandidate(input.token, input.hex);
        if (pendingHumanShadowSnapshot == null)
        {
            RoomAIDecisionTraceService.RecordHumanInput(input.consumedByHomeTeam,
                BuildHumanClickValue(input.token, input.hex), input.consumedBy, input.outcome);
            return;
        }
        RecordHumanShadowChoice(
            candidate,
            "click",
            BuildHumanClickValue(input.token, input.hex),
            input.consumedBy,
            input.outcome);
    }

    private void OnPhysicalKeyPressCompleted(KeyPressData input)
    {
        if (input == null || !input.isConsumed)
        {
            return;
        }

        // Camera presets/navigation consume the same physical key stream as
        // gameplay, but they are not tactical decisions and must not clear or
        // pollute the pending Human Shadow comparison.
        if (string.Equals(
                input.consumedBy,
                nameof(CameraController),
                System.StringComparison.Ordinal))
        {
            return;
        }

        CancelAutomatedAnalysis();
        preparedCodexDecision = null;
        RoomActionCandidate candidate = MatchHumanKeyCandidate(input.key);
        if (pendingHumanShadowSnapshot == null)
        {
            RoomAIDecisionTraceService.RecordHumanInput(input.consumedByHomeTeam,
                $"Press [{input.key}] ({input.consumedBy})", input.consumedBy, "consumed");
            return;
        }
        RecordHumanShadowChoice(
            candidate,
            "key",
            input.key.ToString(),
            input.consumedBy,
            "consumed");
    }

    private void RecordHumanShadowChoice(
        RoomActionCandidate humanCandidate,
        string inputKind,
        string inputValue,
        string consumedBy,
        string outcome)
    {
        if (pendingHumanShadowDecision == null || pendingHumanShadowSnapshot == null)
        {
            return;
        }

        try
        {
            RoomAITacticalEvaluation humanEvaluation = pendingHumanShadowDecision.rankedCandidates?
                .Find(evaluation => ReferenceEquals(evaluation?.candidate, humanCandidate));
            RoomAITacticalEvaluation best = pendingHumanShadowDecision.rankedCandidates != null
                && pendingHumanShadowDecision.rankedCandidates.Count > 0
                    ? pendingHumanShadowDecision.rankedCandidates[0]
                    : null;
            lastHumanShadowChoice = humanCandidate?.label ?? inputValue;
            lastHumanShadowRank = pendingHumanShadowDecision.analysisComplete && humanEvaluation != null
                ? pendingHumanShadowDecision.rankedCandidates.IndexOf(humanEvaluation) + 1
                : 0;
            lastHumanShadowScoreDelta = pendingHumanShadowDecision.analysisComplete && humanEvaluation != null && best != null
                && humanEvaluation.tier == best.tier
                ? best.score - humanEvaluation.score
                : 0;

            RoomAIDecisionTraceService.RecordHumanChoice(
                pendingHumanShadowDecision,
                pendingHumanShadowSnapshot,
                humanCandidate,
                inputKind,
                inputValue,
                consumedBy,
                outcome);
            Debug.Log(
                $"[Human Shadow] state={pendingHumanShadowStateKey}, human={lastHumanShadowChoice}, "
                + $"rank={lastHumanShadowRank}, scoreDelta={lastHumanShadowScoreDelta}, "
                + $"ai={pendingHumanShadowDecision.selectedCandidate?.label}");
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"Could not record Human Shadow choice: {exception.Message}");
        }
        finally
        {
            CancelHumanShadowAnalysis();
            ClearPendingHumanShadowDecision();
        }
    }

    private RoomActionCandidate MatchHumanKeyCandidate(KeyCode key)
    {
        if (pendingHumanShadowSnapshot == null)
        {
            return null;
        }

        string normalizedInput = NormalizeInputKey(key.ToString());
        foreach (RoomActionCandidate candidate in pendingHumanShadowSnapshot.legalActions)
        {
            if (string.Equals(
                    NormalizeInputKey(candidate?.key),
                    normalizedInput,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    private RoomActionCandidate MatchHumanClickCandidate(PlayerToken token, HexCell hex)
    {
        if (pendingHumanShadowSnapshot == null)
        {
            return null;
        }

        RoomActionCandidate bestMatch = null;
        int bestQuality = 0;
        foreach (RoomActionCandidate candidate in pendingHumanShadowSnapshot.legalActions)
        {
            if (candidate == null)
            {
                continue;
            }

            int quality = 0;
            if (token != null && ReferenceEquals(candidate.targetToken, token))
            {
                quality = 6;
            }
            else if (token != null && ReferenceEquals(candidate.actor, token)
                && candidate.targetHex == null)
            {
                quality = 5;
            }

            if (hex != null && ReferenceEquals(candidate.targetHex, hex))
            {
                quality = System.Math.Max(quality, 4);
            }

            if (quality > bestQuality)
            {
                bestQuality = quality;
                bestMatch = candidate;
            }
        }

        return bestMatch;
    }

    private static string NormalizeInputKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        string normalized = key.Trim();
        return normalized.ToLowerInvariant() switch
        {
            "return" => "enter",
            "keypadenter" => "enter",
            "spacebar" => "space",
            _ => normalized
        };
    }

    private static string BuildHumanClickValue(PlayerToken token, HexCell hex)
    {
        string tokenKey = GetTokenKey(token);
        string hexKey = hex != null ? $"({hex.coordinates.x},{hex.coordinates.z})" : "none";
        return $"token={tokenKey}, hex={hexKey}";
    }

    private void ClearPendingHumanShadowDecision()
    {
        pendingHumanShadowDecision = null;
        pendingHumanShadowSnapshot = null;
        pendingHumanShadowStateKey = string.Empty;
    }

    private string BuildCodexTacticianSelectedActionSummary(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context,
        AIManager.CodexTacticianDifficulty difficulty)
    {
        if (!string.Equals(codexDecisionTimingStateKey, evaluatingDecisionStateKey, System.StringComparison.Ordinal))
        {
            codexDecisionTimingStateKey = evaluatingDecisionStateKey;
            codexDecisionWall.Restart();
            codexCandidateEnumerationSkips = 0;
        }
        // The running analysis already owns the candidate list for this exact
        // state. Rebuilding it on every update cannot affect that analysis and
        // can repeatedly perform expensive target enumeration while it yields.
        // A changed state still falls through to cancel and rebuild below.
        if (automatedAnalysis != null
            && string.Equals(automatedAnalysisStateKey, evaluatingDecisionStateKey, System.StringComparison.Ordinal))
        {
            codexCandidateEnumerationSkips++;
            return "Thinking…";
        }

        var candidateTimer = System.Diagnostics.Stopwatch.StartNew();
        List<RoomActionCandidate> candidates = BuildTacticalExecutableCandidates(
            snapshot,
            context,
            preferCuratedCandidates: true);
        candidateTimer.Stop();
        if (candidates.Count == 0)
        {
            DisarmAutomatedExecution();
            lastCodexTacticianScores.Clear();
            lastAutomatedExecutionFingerprint = string.Empty;
            lastAutomatedSelectedAction = string.Empty;
            return groundConfirmationReopened
                ? $"{groundConfirmationReplanReason} No legal executable pass target is available yet."
                : $"{CodexTacticianProvider.DisplayName} {difficulty} found no legal executable candidates yet.";
        }

        bool reusePreparedDecision = preparedCodexDecision != null
            && string.Equals(preparedCodexStateKey, evaluatingDecisionStateKey, System.StringComparison.Ordinal);
        if (!reusePreparedDecision)
        {
            if (automatedAnalysis != null && automatedAnalysisStateKey != evaluatingDecisionStateKey)
                CancelAutomatedAnalysis();
            if (automatedAnalysis == null)
            {
                automatedAnalysisStateKey = evaluatingDecisionStateKey;
                automatedAnalysis = StartCoroutine(AnalyzeAutomatedDecision(snapshot, context, candidates,
                    difficulty, evaluatingDecisionStateKey, candidateTimer.Elapsed.TotalMilliseconds));
            }
            return "Thinking…";
        }
        RoomAIDecisionSnapshot decisionSnapshot = preparedCodexSnapshot;
        RoomAIProviderDecision decision = preparedCodexDecision;
        string movementAttackStateSignature = BuildMovementAttackStateSignature(decisionSnapshot, candidates);
        bool appliedCommittedDribbleRoute = ApplyPendingDribbleRoute(decision, decisionSnapshot);
        bool appliedSuccessfulTackleContinuation = ApplyPendingSuccessfulTackleContinuation(
            decision,
            decisionSnapshot);
        ApplyCommittedStagedDecision(decision, decisionSnapshot);
        RestoreArmedCodexCandidate(decision);
        ApplyMovementActorReplan(decision, decisionSnapshot);
        if (groundConfirmationReopened)
            decision.reasonSummary = groundConfirmationReplanReason + "; " + decision.reasonSummary;
        currentCodexTacticianPlan = decision.plan;
        lastCodexTacticianPlan = FormatTacticalPlan(currentCodexTacticianPlan);
        lastCodexTacticianScores = FormatCodexTacticianCandidates(
            decision.rankedCandidates,
            decision.selectedEvaluation,
            decisionSnapshot);
        lastCodexTacticianDifficulty = decision.difficulty.ToString();
        lastCodexTacticianReason = decision.reasonSummary;
        lastCodexTacticianDurationMilliseconds = (float)decision.durationMilliseconds;
        lastCodexTacticianCandidateCount = decision.candidateCount;
        lastCodexTacticianSelectedScore = decision.selectedEvaluation?.score ?? 0;
        PersistAdaptiveCodexAlternatives(decision);

        string fingerprint = evaluatingDecisionStateKey;
        if (fingerprint != lastAutomatedExecutionFingerprint)
        {
            if (decision.selectedCandidate == null)
            {
                return $"{CodexTacticianProvider.DisplayName} {difficulty} could not select a legal candidate.";
            }

            lastAutomatedSelectedAction = FormatActionCandidate(decision.selectedCandidate);
            if (ShouldDeferAutomatedExecution(fingerprint))
            {
                armedCodexCandidateIdentity = BuildCandidateIdentity(decision.selectedCandidate);
                return $"{CodexTacticianProvider.DisplayName} {difficulty} decided: "
                    + $"{lastAutomatedSelectedAction}. Waiting for the input state to settle.";
            }

            if (ShouldAwaitManualKickoffConfirmation(snapshot, decision.selectedCandidate))
            {
                lastAutomatedExecutionFingerprint = fingerprint;
                RecordCodexTacticianDecisionHistory(decision, lastAutomatedSelectedAction, decisionSnapshot);
                RoomAIDecisionTraceService.Record(decision, decisionSnapshot);
                return $"{CodexTacticianProvider.DisplayName} {difficulty} arranged the kick-off setup. "
                    + "Waiting for your two confirmation keys.";
            }

            lastAutomatedExecutionFingerprint = fingerprint;
            RememberMovementActorAlternatives(decision, decisionSnapshot);
            RememberGroundTargetSelection(decision.selectedCandidate, decisionSnapshot);
            RememberCommittedStagedDecision(decision.selectedEvaluation, decisionSnapshot);
            if (appliedCommittedDribbleRoute)
            {
                ConsumePendingDribbleRouteStep();
            }
            RememberDribbleRoute(
                decision.selectedEvaluation,
                decisionSnapshot,
                appliedCommittedDribbleRoute);
            RememberSuccessfulTackleContinuation(decision.selectedEvaluation, decisionSnapshot);
            RecordCodexTacticianDecisionHistory(decision, lastAutomatedSelectedAction, decisionSnapshot);
            RoomAIDecisionTraceService.Record(decision, decisionSnapshot);
            RememberMovementAttackState(movementAttackStateSignature, decision.selectedCandidate);
            Debug.Log(
                $"[Codex Tactician] decision difficulty={difficulty}, durationMs={decision.durationMilliseconds:F3}, "
                + $"wallFromFirstObservedMs={codexDecisionWall.Elapsed.TotalMilliseconds:F3}, "
                + $"candidateEnumerationSkips={codexCandidateEnumerationSkips}, "
                + $"candidates={decision.candidateCount}, action={lastAutomatedSelectedAction}, "
                + $"plan={decision.plan?.planType}, reason={decision.reasonSummary}");
            ExecuteCandidate(decision.selectedCandidate);
            if (appliedSuccessfulTackleContinuation
                && (pendingSuccessfulTackleAction != RoomActionType.GroundPass
                    || decision.selectedCandidate.step == RoomDecisionStep.ChooseTarget))
            {
                ClearSuccessfulTackleContinuation();
            }
        }

        return $"{CodexTacticianProvider.DisplayName} {difficulty} executed: {lastAutomatedSelectedAction}";
    }

    private void CancelAutomatedAnalysis()
    {
        if (automatedAnalysis != null) StopCoroutine(automatedAnalysis);
        automatedAnalysis = null;
        automatedAnalysisStateKey = null;
        if (thinkingPanel != null) thinkingPanel.SetActive(false);
    }

    private IEnumerator AnalyzeAutomatedDecision(GameplayInstructionSnapshot instruction,
        RoomDecisionContext context, List<RoomActionCandidate> candidates,
        AIManager.CodexTacticianDifficulty difficulty, string stateKey, double candidateEnumerationMilliseconds)
    {
        // Paint the Thinking panel before any expensive work starts.
        yield return null;
        while (MatchManager.Instance?.IsGameplayInputBlocked == true) yield return null;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        double snapshotMilliseconds = 0d;
        double explanationsCompletedMilliseconds = 0d;
        float longestBatch = 0f;
        RoomAIDecisionSnapshot snapshot = null;
        RoomAIProviderDecision result = null;
        IEnumerator work = null;
        try
        {
            if (MatchManager.Instance?.IsGameplayInputBlocked != true && thinkingPanel != null)
                thinkingPanel.SetActive(true);
            snapshot = RoomAIDecisionSnapshotBuilder.Build(instruction, context, candidates, false);
            snapshotMilliseconds = watch.Elapsed.TotalMilliseconds;
            if (!string.IsNullOrEmpty(activeKickoffAutomationTeam)) snapshot.expectedTeam = activeKickoffAutomationTeam;
            var cache = new RoomAIHumanShadowCache();
            work = RoomAIDecisionSnapshotBuilder.BuildCandidateExplanationsIncrementally(snapshot, cache);
            for (int stage = 0; stage < 2; stage++)
            {
                bool done = false;
                while (!done)
                {
                    if (MatchManager.Instance?.IsGameplayInputBlocked == true)
                    {
                        if (thinkingPanel != null) thinkingPanel.SetActive(false);
                        yield return null;
                        continue;
                    }
                    if (thinkingPanel != null) thinkingPanel.SetActive(true);
                    double start = watch.Elapsed.TotalMilliseconds;
                    do { if (!work.MoveNext()) { done = true; break; } }
                    while (watch.Elapsed.TotalMilliseconds - start < decisionFrameBudgetMilliseconds);
                    longestDecisionBatchMilliseconds = Mathf.Max(longestDecisionBatchMilliseconds,
                        (float)(watch.Elapsed.TotalMilliseconds - start));
                    longestBatch = Mathf.Max(longestBatch, (float)(watch.Elapsed.TotalMilliseconds - start));
                    yield return null;
                }
                (work as System.IDisposable)?.Dispose();
                if (stage == 0)
                {
                    explanationsCompletedMilliseconds = watch.Elapsed.TotalMilliseconds;
                    PrepareCommittedDefensivePhasePlan(snapshot);
                    string signature = BuildMovementAttackStateSignature(snapshot, candidates);
                    snapshot.repeatsNoProgressMovementState = !string.IsNullOrEmpty(signature)
                        && recentCodexMovementAttackStateSignatures.Contains(signature);
                    work = CodexTacticianProvider.Instance.DecideIncrementally(snapshot, candidates,
                        currentCodexTacticianPlan, codexTacticianWeights, difficulty, decision => result = decision);
                }
            }
            if (automatedAnalysisStateKey == stateKey)
            {
                Debug.Log($"[Codex Tactician] preparation timing: manager={snapshot.manager}, candidates={candidates.Count}, "
                    + $"enumerationMs={candidateEnumerationMilliseconds:F3}, snapshotMs={snapshotMilliseconds:F3}, explanationsWallMs={explanationsCompletedMilliseconds - snapshotMilliseconds:F3}, "
                    + $"providerWallMs={watch.Elapsed.TotalMilliseconds - explanationsCompletedMilliseconds:F3}, "
                    + $"analysisWallMs={watch.Elapsed.TotalMilliseconds:F3}, longestBatchMs={longestBatch:F3}; "
                    + "the initial frame and time before candidate enumeration are not included");
                preparedCodexStateKey = stateKey;
                preparedCodexSnapshot = snapshot;
                preparedCodexDecision = result;
            }
        }
        finally
        {
            (work as System.IDisposable)?.Dispose();
            automatedAnalysis = null;
            if (thinkingPanel != null) thinkingPanel.SetActive(false);
        }
    }

    private bool ShouldAwaitManualKickoffConfirmation(
        GameplayInstructionSnapshot snapshot,
        RoomActionCandidate candidate)
    {
        if (snapshot == null || candidate == null)
        {
            return false;
        }

        bool isSetupState = string.Equals(
                snapshot.manager,
                nameof(KickoffManager),
                System.StringComparison.Ordinal)
            && candidate.actionType == RoomActionType.Kickoff;
        bool isConfirmation = candidate.step == RoomDecisionStep.Confirm
            && (string.Equals(candidate.key, "Space", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidate.key, "Enter", System.StringComparison.OrdinalIgnoreCase));
        return isSetupState
            && isConfirmation
            && string.IsNullOrWhiteSpace(activeKickoffAutomationTeam);
    }

    private static string BuildMovementAttackStateSignature(
        RoomAIDecisionSnapshot snapshot,
        IReadOnlyList<RoomActionCandidate> candidates)
    {
        bool isRootMovementChoice = candidates != null
            && candidates.Any(candidate => candidate != null
                && candidate.actionType == RoomActionType.StartMovement
                && candidate.step == RoomDecisionStep.ChooseActionType);
        if (!isRootMovementChoice
            || snapshot == null
            || string.IsNullOrWhiteSpace(snapshot.expectedTeam)
            || !string.Equals(
                snapshot.expectedTeam,
                snapshot.teamInAttack,
                System.StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        StringBuilder signature = new();
        signature.Append(snapshot.expectedTeam)
            .Append('|')
            .Append(snapshot.attackHasPossession)
            .Append('|')
            .Append(snapshot.ball?.x ?? int.MinValue)
            .Append(',')
            .Append(snapshot.ball?.z ?? int.MinValue);

        foreach (RoomAITokenSnapshot token in snapshot.tokens
            .Where(token => token != null
                && token.isPlaying
                && string.Equals(
                    token.teamSide,
                    snapshot.expectedTeam,
                    System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(token => token.tokenKey, System.StringComparer.Ordinal))
        {
            signature.Append('|')
                .Append(token.tokenKey)
                .Append('@')
                .Append(token.currentHex?.x ?? int.MinValue)
                .Append(',')
                .Append(token.currentHex?.z ?? int.MinValue);
        }

        return signature.ToString();
    }

    private void RememberMovementAttackState(
        string signature,
        RoomActionCandidate selectedCandidate)
    {
        // Non-root continuation decisions must not erase the root movement
        // history which is needed when the phase returns to M/P/C/L.
        if (string.IsNullOrWhiteSpace(signature))
        {
            return;
        }

        bool selectedMovement = selectedCandidate != null
            && selectedCandidate.actionType == RoomActionType.StartMovement
            && selectedCandidate.step == RoomDecisionStep.ChooseActionType;
        if (!selectedMovement)
        {
            recentCodexMovementAttackStateSignatures.Clear();
            return;
        }

        recentCodexMovementAttackStateSignatures.Add(signature);
        if (recentCodexMovementAttackStateSignatures.Count > RecentCodexMovementStateLimit)
        {
            recentCodexMovementAttackStateSignatures.RemoveAt(0);
        }
    }

    private void RememberSuccessfulTackleContinuation(
        RoomAITacticalEvaluation selectedEvaluation,
        RoomAIDecisionSnapshot snapshot)
    {
        RoomAICandidateExplanation explanation = selectedEvaluation?.explanation;
        if (explanation?.isSuccessfulTackleReposition != true
            || string.IsNullOrWhiteSpace(explanation.plannedNextAction))
        {
            return;
        }

        pendingSuccessfulTackleAction = explanation.plannedNextAction switch
        {
            "StandardPass" => RoomActionType.GroundPass,
            "Shot" => RoomActionType.Shot,
            "Movement" => RoomActionType.StartMovement,
            _ => RoomActionType.Unknown
        };
        pendingSuccessfulTackleTeam = snapshot?.expectedTeam ?? string.Empty;
        pendingSuccessfulTackleReceiverKey = explanation.plannedNextReceiverTokenKey ?? string.Empty;
        pendingSuccessfulTackleTargetHex = explanation.plannedNextActionTargetHex;
    }

    private bool ApplyPendingDribbleRoute(
        RoomAIProviderDecision decision,
        RoomAIDecisionSnapshot snapshot)
    {
        MovementPhaseManager movement = MatchManager.Instance?.movementPhaseManager;
        PlayerToken dribbler = movement?.selectedToken;
        string dribblerKey = GetTokenKey(dribbler);
        if (pendingDribbleRoute.Count == 0 && pendingDribbleStopHex == null) return false;

        if (movement == null
            || !movement.isDribblerRunning
            || dribbler == null
            || !string.Equals(pendingDribbleRouteTeam, snapshot?.expectedTeam, System.StringComparison.OrdinalIgnoreCase)
            || !string.Equals(pendingDribbleRouteTokenKey, dribblerKey, System.StringComparison.OrdinalIgnoreCase)
            || pendingDribbleEnvironmentKey != BuildDribbleEnvironmentKey(snapshot, dribblerKey))
        {
            ClearPendingDribbleRoute();
            return false;
        }

        if (pendingDribbleRoute.Count == 0)
        {
            var freshRoute = decision?.selectedEvaluation?.explanation;
            if (freshRoute?.isFullPaceDribbleRoute == true && freshRoute.fullPaceDribbleProgress > 0
                && freshRoute.fullPaceDribbleReachProbability >= .9999f
                && freshRoute.fullPaceDribbleTurnoverProbability <= .0001f
                && freshRoute.fullPaceDribbleEndpointPressure == 0)
            {
                ClearPendingDribbleRoute();
                return false;
            }
            var stop = decision?.rankedCandidates?.FirstOrDefault(item => item.candidate?.isForfeit == true
                && item.candidate.manager == nameof(MovementPhaseManager) && item.candidate.key == "X");
            if (MatchesCoordinates(dribbler.GetCurrentHex(), pendingDribbleStopHex) && stop != null
                && !movement.isWaitingForSnapshotDecision && !movement.isWaitingForNutmegDecision)
            {
                decision.selectedEvaluation = stop; decision.selectedCandidate = stop.candidate;
                decision.reasonSummary = "Reached the evaluated dribble endpoint; preserve position instead of spending unused Pace backwards.";
                ClearPendingDribbleRoute();
                return true;
            }
            ClearPendingDribbleRoute();
            return false;
        }
        RoomHexCoordinates nextStep = pendingDribbleRoute[0];
        RoomAITacticalEvaluation continuation = decision?.rankedCandidates?.FirstOrDefault(evaluation =>
            evaluation?.candidate?.actionType == RoomActionType.StartMovement
            && evaluation.candidate.step == RoomDecisionStep.ChooseTarget
            && MatchesCoordinates(evaluation.candidate.targetHex, nextStep));
        if (continuation == null)
        {
            // A tackle, interception, compulsory decision, or changed board state
            // invalidates the committed route. The fresh ranking then takes over.
            ClearPendingDribbleRoute();
            return false;
        }

        var fresh = decision.selectedEvaluation;
        if (fresh != null && fresh != continuation
            && (fresh.safetyPriority < continuation.safetyPriority
                || fresh.safetyPriority == continuation.safetyPriority
                    && fresh.explanation?.isFullPaceDribbleRoute == true
                    && continuation.explanation?.isFullPaceDribbleRoute == true
                    && (fresh.explanation.fullPaceDribbleTurnoverProbability + .05f
                            < continuation.explanation.fullPaceDribbleTurnoverProbability
                        || fresh.explanation.fullPaceDribbleExpectedGoals
                            > continuation.explanation.fullPaceDribbleExpectedGoals + .05f)))
        {
            ClearPendingDribbleRoute();
            return false;
        }
        decision.selectedEvaluation = continuation;
        decision.selectedCandidate = continuation.candidate;
        decision.reasonSummary = $"Committed full-Pace dribble route step to ({nextStep.x}, {nextStep.z})";
        return true;
    }

    private void ConsumePendingDribbleRouteStep()
    {
        if (pendingDribbleRoute.Count > 0)
        {
            pendingDribbleRoute.RemoveAt(0);
        }

        // Retain the endpoint until the next choice can stop unused Pace.

    }

    private void RememberDribbleRoute(
        RoomAITacticalEvaluation selectedEvaluation,
        RoomAIDecisionSnapshot snapshot,
        bool appliedCommittedRoute)
    {
        if (appliedCommittedRoute)
        {
            return;
        }

        RoomAICandidateExplanation explanation = selectedEvaluation?.explanation;
        if (explanation?.isFullPaceDribbleRoute != true
            || explanation.fullPaceDribblePath == null
            || explanation.fullPaceDribblePath.Count == 0)
        {
            ClearPendingDribbleRoute();
            return;
        }

        PlayerToken dribbler = MatchManager.Instance?.movementPhaseManager?.selectedToken;
        pendingDribbleStopHex = CloneCoordinates(explanation.fullPaceDribbleDestination);
        pendingDribbleRouteTeam = snapshot?.expectedTeam ?? string.Empty;
        pendingDribbleRouteTokenKey = GetTokenKey(dribbler);
        pendingDribbleEnvironmentKey = BuildDribbleEnvironmentKey(snapshot, pendingDribbleRouteTokenKey);
        pendingDribbleRoute.Clear();
        // The selected candidate executes path[0] now; retain the subsequent
        // adjacent clicks so the engine follows the chosen destination route.
        for (int index = 1; index < explanation.fullPaceDribblePath.Count; index++)
        {
            RoomHexCoordinates step = explanation.fullPaceDribblePath[index];
            pendingDribbleRoute.Add(new RoomHexCoordinates { x = step.x, z = step.z });
        }
    }

    private void ClearPendingDribbleRoute()
    {
        pendingDribbleRouteTeam = string.Empty;
        pendingDribbleRouteTokenKey = string.Empty;
        pendingDribbleEnvironmentKey = string.Empty;
        pendingDribbleStopHex = null;
        pendingDribbleRoute.Clear();
    }

    private static string BuildDribbleEnvironmentKey(RoomAIDecisionSnapshot snapshot, string dribblerKey)
    {
        if (snapshot == null) return string.Empty;
        // Ignore only the expected ball/carrier step. A GK checkpoint, defender
        // move, possession change or dismissal must invalidate the old route.
        return $"{snapshot.expectedTeam}|{snapshot.teamInAttack}|{snapshot.attackHasPossession}|{snapshot.homeScore}:{snapshot.awayScore}|"
            + string.Join("|", snapshot.tokens.Where(token => token != null && token.tokenKey != dribblerKey)
                .OrderBy(token => token.tokenKey, System.StringComparer.Ordinal)
                .Select(token => $"{token.tokenKey}@{token.currentHex?.x},{token.currentHex?.z}:{token.isPlaying}:{token.isSentOff}"));
    }

    private bool ApplyPendingSuccessfulTackleContinuation(
        RoomAIProviderDecision decision,
        RoomAIDecisionSnapshot snapshot)
    {
        if (pendingSuccessfulTackleAction == RoomActionType.Unknown
            || decision?.rankedCandidates == null
            || !string.Equals(
                pendingSuccessfulTackleTeam,
                snapshot?.expectedTeam,
                System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        RoomAITacticalEvaluation continuation = null;
        foreach (RoomAITacticalEvaluation evaluation in decision.rankedCandidates)
        {
            RoomActionCandidate candidate = evaluation?.candidate;
            if (candidate == null || candidate.actionType != pendingSuccessfulTackleAction)
            {
                continue;
            }

            if (candidate.step == RoomDecisionStep.ChooseActionType)
            {
                continuation = evaluation;
                break;
            }

            if (pendingSuccessfulTackleAction == RoomActionType.GroundPass
                && candidate.step == RoomDecisionStep.ChooseTarget
                && (string.Equals(
                        GetTokenKey(candidate.targetToken ?? candidate.actor),
                        pendingSuccessfulTackleReceiverKey,
                        System.StringComparison.OrdinalIgnoreCase)
                    || MatchesCoordinates(candidate.targetHex, pendingSuccessfulTackleTargetHex)))
            {
                continuation = evaluation;
                break;
            }
        }

        if (continuation == null)
        {
            bool continuationStageIsPresent = decision.rankedCandidates.Exists(evaluation =>
                evaluation?.candidate?.step == RoomDecisionStep.ChooseActionType
                || (pendingSuccessfulTackleAction == RoomActionType.GroundPass
                    && evaluation?.candidate?.actionType == RoomActionType.GroundPass
                    && evaluation.candidate.step == RoomDecisionStep.ChooseTarget));
            if (continuationStageIsPresent)
            {
                ClearSuccessfulTackleContinuation();
            }

            return false;
        }

        // The forecast selected a reposition, not a licence to override a
        // better action found on the resulting live board.
        if (decision.rankedCandidates.IndexOf(continuation) > 0)
        {
            ClearSuccessfulTackleContinuation();
            return false;
        }
        decision.selectedEvaluation = continuation;
        decision.selectedCandidate = continuation.candidate;
        decision.reasonSummary = "Committed successful-tackle continuation: "
            + string.Join("; ", continuation.reasons);
        return true;
    }

    private static bool MatchesCoordinates(HexCell hex, RoomHexCoordinates coordinates)
    {
        return hex != null
            && coordinates != null
            && hex.coordinates.x == coordinates.x
            && hex.coordinates.z == coordinates.z;
    }

    private void ClearSuccessfulTackleContinuation()
    {
        pendingSuccessfulTackleAction = RoomActionType.Unknown;
        pendingSuccessfulTackleTeam = string.Empty;
        pendingSuccessfulTackleReceiverKey = string.Empty;
        pendingSuccessfulTackleTargetHex = null;
    }

    private static bool IsAutomatedPersona(AIManager.RoomPersona persona)
    {
        return persona == AIManager.RoomPersona.CodexTactician;
    }

    private bool ShouldDeferAutomatedExecution(string decisionStateKey)
    {
        if (!HasObservedDecisionStateForFullFrame(
                decisionStateKey,
                armedAutomatedDecisionStateKey,
                armedAutomatedDecisionFrame,
                Time.frameCount))
        {
            armedAutomatedDecisionStateKey = decisionStateKey;
            armedAutomatedDecisionFrame = Time.frameCount;
            return true;
        }

        DisarmAutomatedExecution();
        return false;
    }

    private static bool HasObservedDecisionStateForFullFrame(
        string decisionStateKey,
        string armedStateKey,
        int armedFrame,
        int currentFrame)
    {
        return !string.IsNullOrWhiteSpace(decisionStateKey)
            && string.Equals(decisionStateKey, armedStateKey, System.StringComparison.Ordinal)
            && armedFrame >= 0
            && currentFrame > armedFrame;
    }

    private void DisarmAutomatedExecution()
    {
        armedAutomatedDecisionStateKey = string.Empty;
        armedAutomatedDecisionFrame = -1;
        armedCodexCandidateIdentity = string.Empty;
    }

    private string BuildMovementReplanKey(RoomAIDecisionSnapshot snapshot, MovementPhaseManager movement)
    {
        return BuildTacticalBoardStateKey(snapshot) + "|movement|"
            + movement.isMovementPhaseAttack + ":" + movement.isMovementPhaseDef + ":" + movement.isMovementPhase2f2
            + ":" + movement.attackersMoved + ":" + movement.defendersMoved + ":" + movement.attackersMovedIn2f2;
    }

    private void RememberMovementActorAlternatives(RoomAIProviderDecision decision, RoomAIDecisionSnapshot snapshot)
    {
        var match = MatchManager.Instance;
        var movement = match?.movementPhaseManager;
        var selected = decision?.selectedCandidate;
        if (match == null || match.difficulty_level >= 3 || movement == null || snapshot?.currentState != "MovementPhase"
            || decision?.rankedCandidates == null
            || selected?.manager != nameof(MovementPhaseManager) || selected.actionType != RoomActionType.StartMovement
            || selected.step != RoomDecisionStep.ChooseActor || selected.actor == null) return;
        string key = BuildMovementReplanKey(snapshot, movement);
        if (movement.selectedToken == null || movementReplanKey != key)
        {
            movementReplanKey = key;
            movementReplanTried.Clear();
            movementReplanActors.Clear();
            movementReplanActors.AddRange(decision.rankedCandidates.Where(e => e?.candidate?.actor != null
                && e.candidate.manager == nameof(MovementPhaseManager)
                && e.candidate.actionType == RoomActionType.StartMovement && e.candidate.step == RoomDecisionStep.ChooseActor
                && e.safetyPriority == 0 && e.explanation?.supportsConcreteContinuation == true
                && e.explanation.targetHex != null));
        }
        movementReplanTried.Add(GetTokenKey(selected.actor));
    }

    private void ApplyMovementActorReplan(RoomAIProviderDecision decision, RoomAIDecisionSnapshot snapshot)
    {
        var match = MatchManager.Instance;
        var movement = match?.movementPhaseManager;
        if (match == null || match.difficulty_level >= 3 || movement == null || snapshot?.currentState != "MovementPhase"
            || !snapshot.attackHasPossession || snapshot.expectedTeam != snapshot.teamInAttack
            || !movement.isAwaitingHexDestination || !movement.isAwaitingTokenSelection
            || movement.selectedToken == null || movement.isDribblerRunning
            || movement.selectedToken.IsDribbler || decision?.selectedCandidate?.isForfeit != true
            || decision.selectedCandidate.manager != nameof(MovementPhaseManager) || decision.selectedCandidate.key != "X"
            || movementReplanKey != BuildMovementReplanKey(snapshot, movement)) return;

        var selectable = movement.GetSelectableMovementTokensForDecisionPreview();
        var alternate = movementReplanActors.FirstOrDefault(e => e?.candidate?.actor != null
            && e.candidate.actor != movement.selectedToken && selectable.Contains(e.candidate.actor)
            && !movementReplanTried.Contains(GetTokenKey(e.candidate.actor)));
        if (alternate == null)
        {
            decision.reasonSummary = "No untried eligible actor with a supported continuation remains; X forfeits the remaining phase moves.";
            return;
        }
        if (!decision.rankedCandidates.Contains(alternate)) decision.rankedCandidates.Insert(0, alternate);
        decision.selectedEvaluation = alternate;
        decision.selectedCandidate = alternate.candidate;
        decision.reasonSummary = "Replanned after the selected actor had no useful destination; try another eligible actor without forfeiting the phase.";
    }

    private void RestoreArmedCodexCandidate(RoomAIProviderDecision decision)
    {
        if (decision?.rankedCandidates == null || string.IsNullOrWhiteSpace(armedCodexCandidateIdentity))
        {
            return;
        }

        foreach (RoomAITacticalEvaluation evaluation in decision.rankedCandidates)
        {
            if (evaluation != null
                && string.Equals(
                    BuildCandidateIdentity(evaluation.candidate),
                    armedCodexCandidateIdentity,
                    System.StringComparison.Ordinal))
            {
                if (HasFreshSafetyVeto(decision, evaluation))
                {
                    armedCodexCandidateIdentity = string.Empty;
                    return;
                }
                decision.selectedEvaluation = evaluation;
                decision.selectedCandidate = evaluation.candidate;
                decision.reasonSummary = $"Frozen full-frame decision: {string.Join("; ", evaluation.reasons)}";
                return;
            }
        }
    }

    private static bool HasFreshSafetyVeto(RoomAIProviderDecision decision, RoomAITacticalEvaluation candidate)
    {
        if (candidate?.candidate == null || candidate.candidate.isForfeit || decision?.rankedCandidates == null)
            return false;

        // A previously selected target may remain legal while a fresh analysis
        // has identified a safer alternative. Keep stable commitments when the
        // safety class is unchanged, but never restore a vetoed target over a
        // currently available alternative at the same input stage.
        return decision.rankedCandidates.Any(other => other?.candidate != null
            && !other.candidate.isForfeit
            && (other.candidate.step == candidate.candidate.step
                || candidate.candidate.actionType == RoomActionType.GroundPass
                    && candidate.candidate.step == RoomDecisionStep.Confirm
                    && other.candidate.actionType == RoomActionType.GroundPass
                    && other.candidate.step == RoomDecisionStep.ChooseTarget)
            && other.safetyPriority < candidate.safetyPriority);
    }

    private void RememberGroundTargetSelection(RoomActionCandidate candidate, RoomAIDecisionSnapshot snapshot)
    {
        if (candidate?.manager != nameof(GroundBallManager) || candidate.actionType != RoomActionType.GroundPass
            || candidate.step != RoomDecisionStep.ChooseTarget) return;
        var target = candidate.targetHex ?? candidate.targetToken?.GetCurrentHex();
        selectedGroundTargetHex = RoomHexCoordinates.FromHex(target);
        selectedGroundTargetBoardKey = BuildTacticalBoardStateKey(snapshot);
    }

    private static string BuildCandidateIdentity(RoomActionCandidate candidate)
    {
        if (candidate == null)
        {
            return string.Empty;
        }

        PlayerToken token = candidate.actor ?? candidate.targetToken;
        HexCell target = candidate.targetHex;
        return $"{candidate.manager}|{candidate.actionType}|{candidate.step}|{candidate.key?.Trim()}|"
            + $"{GetTokenKey(token)}|{target?.coordinates.x}:{target?.coordinates.z}";
    }

    private void RememberCommittedStagedDecision(
        RoomAITacticalEvaluation evaluation,
        RoomAIDecisionSnapshot snapshot)
    {
        RoomActionCandidate candidate = evaluation?.candidate;
        RoomAICandidateExplanation explanation = evaluation?.explanation;
        if (candidate == null || explanation == null || candidate.isForfeit)
        {
            return;
        }

        RoomHexCoordinates exactTarget = explanation.snapshotDefenseBestMoveHex
            ?? explanation.defensivePlanDestination
            ?? explanation.highPassBestTargetHex
            ?? explanation.freeHeaderBestTargetHex
            ?? explanation.fullPaceDribbleDestination
            ?? explanation.targetHex;
        if (candidate.actionType is RoomActionType.GroundPass or RoomActionType.QuickThrow)
            exactTarget = explanation.groundPassContinuationTargetHex ?? explanation.targetHex;
        string actorKey = !string.IsNullOrWhiteSpace(explanation.actorTokenKey)
            ? explanation.actorTokenKey
            : GetTokenKey(candidate.actor);
        string targetTokenKey = !string.IsNullOrWhiteSpace(explanation.targetTokenKey)
            ? explanation.targetTokenKey
            : GetTokenKey(candidate.targetToken);
        bool predictsLaterInput = candidate.step == RoomDecisionStep.ChooseActionType
            || candidate.step == RoomDecisionStep.ChooseActor;
        if (!predictsLaterInput
            || (exactTarget == null
                && string.IsNullOrWhiteSpace(actorKey)
                && string.IsNullOrWhiteSpace(targetTokenKey)))
        {
            return;
        }

        if (candidate.actionType == RoomActionType.StartMovement && candidate.step == RoomDecisionStep.ChooseActor
            && (!explanation.isAttackingOffBallMove || !explanation.supportsConcreteContinuation))
        {
            ClearCommittedStagedDecision();
            return;
        }
        committedStagedDecision = new CommittedStagedDecision
        {
            manager = candidate.manager,
            actionType = candidate.actionType,
            actorTokenKey = actorKey,
            targetTokenKey = targetTokenKey,
            exactTarget = CloneCoordinates(exactTarget),
            expectedTacticalOutcome = BuildCommittedOutcomeSummary(explanation),
            possessionSupportTokenKey = explanation.possessionPlanSupportTokenKey,
            possessionSupportDestination = CloneCoordinates(explanation.possessionPlanSupportDestinationHex),
            likelyTackleWinnerTokenKey = explanation.possessionPlanLikelyTackleWinnerTokenKey,
            likelyTackleWinnerExit = CloneCoordinates(explanation.possessionPlanLikelyTackleWinnerExitHex),
            boardStateKey = BuildTacticalBoardStateKey(snapshot),
            invalidationConditions = "actor/target becomes illegal, ball or token positions change, possession changes, or score changes"
        };
        if (candidate.step == RoomDecisionStep.ChooseActionType
            && candidate.actionType == RoomActionType.StartMovement
            && !string.IsNullOrWhiteSpace(actorKey))
        {
            committedStagedDecision.requiredIntermediateClicks.Add(RoomDecisionStep.ChooseActor);
        }
        if (exactTarget != null || !string.IsNullOrWhiteSpace(targetTokenKey))
        {
            committedStagedDecision.requiredIntermediateClicks.Add(RoomDecisionStep.ChooseTarget);
        }
    }

    private bool ApplyCommittedStagedDecision(
        RoomAIProviderDecision decision,
        RoomAIDecisionSnapshot snapshot)
    {
        if (committedStagedDecision == null || decision?.rankedCandidates == null)
        {
            return false;
        }

        if (!string.Equals(
                committedStagedDecision.boardStateKey,
                BuildTacticalBoardStateKey(snapshot),
                System.StringComparison.Ordinal))
        {
            ClearCommittedStagedDecision();
            return false;
        }

        RoomAITacticalEvaluation match = decision.rankedCandidates.FirstOrDefault(evaluation =>
            MatchesCommittedStagedDecision(evaluation?.candidate));
        if (match == null)
        {
            bool expectedStagePresent = decision.rankedCandidates.Any(evaluation =>
                IsCommittedDecisionStage(evaluation?.candidate));
            if (expectedStagePresent)
            {
                ClearCommittedStagedDecision();
            }

            return false;
        }

        // Staging is not permission to bypass a fresh safety veto. In particular,
        // keep the useful support destination only while its continuation still
        // exists; an intervening action must not force an obsolete click.
        if (HasFreshSafetyVeto(decision, match)
            || (match.explanation?.isAttackingOffBallMove == true
                && (!match.explanation.supportsConcreteContinuation || match.safetyPriority > 0)))
        {
            ClearCommittedStagedDecision();
            decision.reasonSummary = "Replanned: the committed continuation failed the fresh safety or support check.";
            return false;
        }

        decision.selectedEvaluation = match;
        decision.selectedCandidate = match.candidate;
        decision.reasonSummary = $"Committed staged decision: {committedStagedDecision.expectedTacticalOutcome}";
        return true;
    }

    private bool MatchesCommittedStagedDecision(RoomActionCandidate candidate)
    {
        if (!IsCommittedDecisionStage(candidate))
        {
            return false;
        }

        RoomDecisionStep targetStage = candidate.step == RoomDecisionStep.Confirm ? RoomDecisionStep.ChooseTarget : candidate.step;
        if (committedStagedDecision.requiredIntermediateClicks.Count > 0
            && targetStage != committedStagedDecision.requiredIntermediateClicks[0])
        {
            return false;
        }

        if (candidate.step == RoomDecisionStep.ChooseActor)
        {
            string candidateKey = GetTokenKey(candidate.actor ?? candidate.targetToken);
            return string.IsNullOrWhiteSpace(committedStagedDecision.actorTokenKey)
                || string.Equals(candidateKey, committedStagedDecision.actorTokenKey, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidateKey, committedStagedDecision.targetTokenKey, System.StringComparison.OrdinalIgnoreCase);
        }

        if (targetStage == RoomDecisionStep.ChooseTarget && committedStagedDecision.exactTarget != null)
        {
            return MatchesCoordinates(candidate.targetHex ?? candidate.targetToken?.GetCurrentHex(), committedStagedDecision.exactTarget);
        }

        if (targetStage == RoomDecisionStep.ChooseTarget
            && !string.IsNullOrWhiteSpace(committedStagedDecision.targetTokenKey))
        {
            string candidateKey = GetTokenKey(candidate.targetToken ?? candidate.actor);
            return string.Equals(
                candidateKey,
                committedStagedDecision.targetTokenKey,
                System.StringComparison.OrdinalIgnoreCase);
        }

        return candidate.step == RoomDecisionStep.ChooseActionType;
    }

    private bool IsCommittedDecisionStage(RoomActionCandidate candidate)
    {
        if (candidate == null
            || !string.Equals(candidate.manager, committedStagedDecision.manager, System.StringComparison.Ordinal))
        {
            return false;
        }

        bool compatibleAction = candidate.actionType == committedStagedDecision.actionType
            || (candidate.actionType == RoomActionType.SetupMove
                && candidate.step != RoomDecisionStep.ChooseActionType);
        return compatibleAction
            && candidate.step is RoomDecisionStep.ChooseActionType
                or RoomDecisionStep.ChooseActor
                or RoomDecisionStep.ChooseTarget
                or RoomDecisionStep.Confirm;
    }

    private static string BuildCommittedOutcomeSummary(RoomAICandidateExplanation explanation)
    {
        if (explanation.snapshotDefenseBestMoveHex != null)
        {
            return $"snapshot defense to ({explanation.snapshotDefenseBestMoveHex.x},{explanation.snapshotDefenseBestMoveHex.z})"
                + $" limiting maximum xG to {explanation.snapshotDefenseMaximumExpectedGoals:0.000}";
        }

        RoomHexCoordinates target = explanation.highPassBestTargetHex
            ?? explanation.freeHeaderBestTargetHex
            ?? explanation.fullPaceDribbleDestination
            ?? explanation.targetHex;
        string outcome = target == null
            ? explanation.label
            : $"{explanation.actionType} to ({target.x},{target.z})";
        if (explanation.possessionPlanSupportBlocksTackleWinnerExit)
        {
            outcome += $"; {explanation.possessionPlanSupportTokenKey} supports at "
                + $"({explanation.possessionPlanSupportDestinationHex.x},{explanation.possessionPlanSupportDestinationHex.z}) "
                + $"against {explanation.possessionPlanLikelyTackleWinnerTokenKey}";
        }

        return outcome;
    }

    private static string BuildTacticalBoardStateKey(RoomAIDecisionSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        builder.Append(snapshot.expectedTeam).Append('|')
            .Append(snapshot.teamInAttack).Append('|')
            .Append(snapshot.attackHasPossession).Append('|')
            .Append(snapshot.homeScore).Append(':').Append(snapshot.awayScore).Append('|')
            .Append(snapshot.ball?.x).Append(',').Append(snapshot.ball?.z);
        var match = MatchManager.Instance;
        var liveTokens = match?.playerTokenManager?.allTokens?.Where(token => token != null)
            .GroupBy(GetTokenKey).ToDictionary(group => group.Key, group => group.First());
        foreach (RoomAITokenSnapshot token in snapshot.tokens
            .Where(token => token != null)
            .OrderBy(token => token.tokenKey, System.StringComparer.Ordinal))
        {
            builder.Append('|').Append(token.tokenKey).Append('@')
                .Append(token.currentHex?.x).Append(',').Append(token.currentHex?.z)
                .Append(':').Append(token.isPlaying).Append(':').Append(token.isBooked)
                .Append(':').Append(token.isSentOff).Append(':').Append(token.isGoalKeeper)
                .Append(':').Append(token.isInjured).Append(':').Append(token.requiresSubstitution)
                .Append(':').Append(token.pace).Append(':').Append(token.tackling)
                .Append(':').Append(token.dribbling).Append(':').Append(token.heading)
                .Append(':').Append(token.highPass).Append(':').Append(token.shooting)
                .Append(':').Append(token.aerial).Append(':').Append(token.saving).Append(':').Append(token.handling);
            if (liveTokens != null && liveTokens.TryGetValue(token.tokenKey, out var live))
                builder.Append(':').Append(match.CanTokenCollectHangingPass(live))
                    .Append(':').Append(match.movementPhaseManager?.stunnedTokens.Contains(live) == true)
                    .Append(':').Append(match.movementPhaseManager?.stunnedforNext.Contains(live) == true);
        }

        return builder.ToString();
    }

    private static RoomHexCoordinates CloneCoordinates(RoomHexCoordinates source)
    {
        return source == null ? null : new RoomHexCoordinates { x = source.x, z = source.z };
    }

    private void ClearCommittedStagedDecision()
    {
        committedStagedDecision = null;
    }

    private void PrepareCommittedDefensivePhasePlan(RoomAIDecisionSnapshot snapshot)
    {
        bool isDefensiveMovement = snapshot != null
            && string.Equals(snapshot.currentState, "MovementPhase", System.StringComparison.OrdinalIgnoreCase)
            && (!string.Equals(snapshot.expectedTeam, snapshot.teamInAttack, System.StringComparison.OrdinalIgnoreCase)
                || snapshot.expectedOpponentCollection)
            && !string.IsNullOrWhiteSpace(snapshot.expectedTeam);
        if (!isDefensiveMovement)
        {
            ClearCommittedDefensivePhasePlan();
            return;
        }

        // Snapshot analysis has already allocated the remaining moves on the
        // current board. Never overwrite it with a stale phase commitment.
        committedDefensivePhasePlan = snapshot.defensivePhasePlan;
        committedDefensivePhaseTeam = snapshot.expectedTeam;
        committedDefensivePhaseBall = CloneCoordinates(snapshot.ball);
    }

    private void ClearCommittedDefensivePhasePlan()
    {
        committedDefensivePhasePlan = null;
        committedDefensivePhaseTeam = string.Empty;
        committedDefensivePhaseBall = null;
    }

    private void ClearCommittedDefensivePhasePlanIfPhaseEnded()
    {
        if (MatchManager.Instance == null
            || MatchManager.Instance.currentState != MatchManager.GameState.MovementPhase)
        {
            ClearCommittedDefensivePhasePlan();
        }
    }

    private void AdvanceCommittedStagedDecision(RoomActionCandidate executed)
    {
        if (committedStagedDecision == null || executed == null)
        {
            return;
        }

        RoomDecisionStep stage = executed.step == RoomDecisionStep.Confirm ? RoomDecisionStep.ChooseTarget : executed.step;
        if (committedStagedDecision.requiredIntermediateClicks.Count > 0
            && stage == committedStagedDecision.requiredIntermediateClicks[0])
        {
            committedStagedDecision.requiredIntermediateClicks.RemoveAt(0);
        }

        if (committedStagedDecision.requiredIntermediateClicks.Count == 0
            && (stage == RoomDecisionStep.ChooseTarget
                || committedStagedDecision.exactTarget == null))
        {
            ClearCommittedStagedDecision();
        }
    }

    private static string GetTokenKey(PlayerToken token)
    {
        return MatchManager.GetStableTokenKey(token) ?? string.Empty;
    }

    private static List<string> FormatCodexTacticianCandidates(
        List<RoomAITacticalEvaluation> evaluations,
        RoomAITacticalEvaluation selectedEvaluation,
        RoomAIDecisionSnapshot decisionSnapshot)
    {
        List<string> summaries = new();
        if (decisionSnapshot != null)
        {
            summaries.Add(FormatDecisionSnapshotSummary(decisionSnapshot));
        }

        if (evaluations == null)
        {
            return summaries;
        }

        int displayedCount = System.Math.Min(12, evaluations.Count);
        for (int index = 0; index < displayedCount; index++)
        {
            RoomAITacticalEvaluation evaluation = evaluations[index];
            string marker = evaluation == selectedEvaluation ? "selected / " : string.Empty;
            string reasons = evaluation.reasons.Count > 0
                ? $" / reasons: {string.Join("; ", evaluation.reasons)}"
                : string.Empty;
            summaries.Add(
                $"{marker}{index + 1}. tier={evaluation.tier}, score={evaluation.score}, "
                + $"{FormatActionCandidate(evaluation.candidate)}"
                + $"{FormatCandidateExplanation(evaluation.explanation)}{reasons}");
        }

        if (evaluations.Count > displayedCount)
        {
            summaries.Add($"{evaluations.Count - displayedCount} lower-ranked candidate(s) omitted from inspector output.");
        }

        return summaries;
    }

    private void RecordCodexTacticianDecisionHistory(
        RoomAIProviderDecision decision,
        string selectedAction,
        RoomAIDecisionSnapshot decisionSnapshot)
    {
        string stateSummary = decisionSnapshot != null
            ? FormatDecisionSnapshotSummary(decisionSnapshot)
            : "state unavailable";
        string entry = $"frame={Time.frameCount}, provider={decision.providerName}, difficulty={decision.difficulty}, "
            + $"durationMs={decision.durationMilliseconds:F3}, plan={decision.plan?.planType}, "
            + $"tier={decision.selectedEvaluation?.tier}, score={decision.selectedEvaluation?.score}, "
            + $"candidates={decision.candidateCount}, action={selectedAction}, reason={decision.reasonSummary}, {stateSummary}";
        recentCodexTacticianDecisionHistory.Insert(0, entry);

        const int maxHistoryEntries = 30;
        while (recentCodexTacticianDecisionHistory.Count > maxHistoryEntries)
        {
            recentCodexTacticianDecisionHistory.RemoveAt(recentCodexTacticianDecisionHistory.Count - 1);
        }
    }

    private void ClearCodexTacticianState()
    {
        lastCodexTacticianScores.Clear();
        lastCodexPersistedAlternatives.Clear();
        currentCodexTacticianPlan = null;
        lastCodexTacticianPlan = string.Empty;
        lastCodexTacticianDifficulty = string.Empty;
        lastCodexTacticianReason = string.Empty;
        lastCodexTacticianDurationMilliseconds = 0f;
        lastCodexTacticianCandidateCount = 0;
        lastCodexTacticianSelectedScore = 0;
    }

    public void EnrichRoomDecisionSnapshotForLog(GameplayRoomDecisionSnapshot snapshot)
    {
        if (snapshot == null
            || !string.Equals(
                snapshot.persona,
                CodexTacticianProvider.DisplayName,
                System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        snapshot.providerName = CodexTacticianProvider.DisplayName;
        snapshot.difficulty = lastCodexTacticianDifficulty;
        snapshot.selectedAction = lastAutomatedSelectedAction;
        snapshot.selectedScore = lastCodexTacticianSelectedScore;
        snapshot.decisionDurationMilliseconds = lastCodexTacticianDurationMilliseconds;
        snapshot.reasonSummary = lastCodexTacticianReason;
        snapshot.candidates.Clear();
        snapshot.candidates.AddRange(lastCodexPersistedAlternatives);
        if (lastCodexTacticianCandidateCount > 0)
        {
            snapshot.candidateCount = lastCodexTacticianCandidateCount;
        }
    }

    private void PersistAdaptiveCodexAlternatives(RoomAIProviderDecision decision)
    {
        lastCodexPersistedAlternatives.Clear();
        if (decision?.rankedCandidates == null || decision.rankedCandidates.Count == 0)
        {
            return;
        }

        RoomAITacticalEvaluation best = decision.rankedCandidates[0];
        int epsilon = System.Math.Max(2, (int)System.Math.Ceiling(System.Math.Abs(best.score) * 0.03f));
        int nearOptimalLimit = decision.selectedEvaluation == best ? 10 : 9;
        for (int index = 0; index < decision.rankedCandidates.Count; index++)
        {
            RoomAITacticalEvaluation evaluation = decision.rankedCandidates[index];
            bool selected = evaluation == decision.selectedEvaluation;
            bool onNearOptimalFrontier = evaluation.tier == best.tier
                && best.score - evaluation.score <= epsilon;
            if (!selected && !onNearOptimalFrontier)
            {
                continue;
            }

            if (!selected && lastCodexPersistedAlternatives.Count >= nearOptimalLimit)
            {
                continue;
            }

            RoomActionCandidate candidate = evaluation.candidate;
            RoomAICandidateExplanation explanation = evaluation.explanation;
            lastCodexPersistedAlternatives.Add(new GameplayRoomDecisionCandidate
            {
                id = candidate?.id,
                manager = candidate?.manager,
                actionType = candidate?.actionType.ToString(),
                step = candidate?.step.ToString(),
                label = candidate?.label,
                key = candidate?.key,
                actorTokenKey = GetTokenKey(candidate?.actor),
                targetTokenKey = GetTokenKey(candidate?.targetToken),
                targetHex = RoomHexCoordinates.FromHex(candidate?.targetHex),
                isExecutableNow = candidate?.isExecutableNow ?? false,
                isForfeit = candidate?.isForfeit ?? false,
                executionCommand = candidate?.executionCommand,
                reason = string.Join("; ", evaluation.reasons),
                rank = index + 1,
                tacticalTier = evaluation.tier.ToString(),
                score = evaluation.score,
                scoreDeltaFromBest = best.score - evaluation.score,
                expectedGoals = explanation?.firstTimePassCombinedExpectedGoals >= 0f
                    ? explanation.firstTimePassCombinedExpectedGoals
                    : explanation?.bestReachableSnapshotExpectedGoals >= 0f
                        ? explanation.bestReachableSnapshotExpectedGoals
                        : explanation?.expectedGoals ?? -1f,
                expectedGoalsAgainst = explanation?.snapshotDefenseMaximumExpectedGoals >= 0f
                    ? explanation.snapshotDefenseMaximumExpectedGoals
                    : explanation?.goalkeeperPositioningMaximumExpectedGoals >= 0f
                        ? explanation.goalkeeperPositioningMaximumExpectedGoals
                        : explanation?.defensiveExpectedGoalsAgainst ?? -1f,
                selected = selected
            });
        }
    }

    private List<RoomActionCandidate> BuildTacticalExecutableCandidates(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context,
        bool preferCuratedCandidates)
    {
        groundConfirmationReopened = false;
        groundConfirmationReplanReason = string.Empty;
        if (!string.IsNullOrWhiteSpace(activeKickoffAutomationTeam)
            && string.Equals(snapshot?.manager, nameof(KickoffManager), System.StringComparison.Ordinal))
        {
            KickoffManager kickoff = MatchManager.Instance?.kickoffManager;
            if (kickoff != null)
            {
                return kickoff.BuildAutomatedSetupCandidates(activeKickoffAutomationTeam)
                    .Where(candidate => candidate != null && candidate.isExecutableNow)
                    .ToList();
            }
        }

        List<RoomActionCandidate> candidates = BuildTacticalExecutableCandidatesCore(
            snapshot,
            context,
            preferCuratedCandidates);
        var ground = MatchManager.Instance?.groundBallManager;
        if (ground?.isActivated == true && ground.currentTargetHex != null
            && candidates.Any(candidate => candidate.manager == nameof(GroundBallManager)
                && candidate.actionType == RoomActionType.GroundPass && candidate.step == RoomDecisionStep.Confirm))
        {
            var currentBoard = RoomAIDecisionSnapshotBuilder.Build(snapshot, context, candidates, false);
            bool targetLegal = ground.IsGroundPassTargetLegalForDecision(ground.currentTargetHex);
            bool knownTarget = selectedGroundTargetHex != null && MatchesCoordinates(ground.currentTargetHex, selectedGroundTargetHex);
            bool boardUnchanged = knownTarget && string.Equals(selectedGroundTargetBoardKey,
                BuildTacticalBoardStateKey(currentBoard), System.StringComparison.Ordinal);
            if (!targetLegal || !boardUnchanged)
            {
                // Only reopen target choice when relevant board/eligibility
                // evidence changed. An unchanged target keeps its stable second
                // click, so near-equal passes cannot oscillate indefinitely.
                AddTacticalCandidates(candidates, context.actions.Where(candidate => candidate != null
                    && candidate.isExecutableNow && candidate.manager == nameof(GroundBallManager)
                    && candidate.actionType == RoomActionType.GroundPass
                    && candidate.step == RoomDecisionStep.ChooseTarget).ToList());
                if (!targetLegal)
                    candidates.RemoveAll(candidate => candidate.manager == nameof(GroundBallManager)
                        && candidate.actionType == RoomActionType.GroundPass && candidate.step == RoomDecisionStep.Confirm);
                groundConfirmationReopened = true;
                groundConfirmationReplanReason = !targetLegal
                    ? "Replanned: the selected ground-pass target is no longer legal"
                    : !knownTarget ? "Re-evaluated the pass target because its selection evidence is unavailable"
                    : "Re-evaluated the pass target because board or collection eligibility changed";
            }
        }
        AddTacticalCandidates(candidates, BuildExecutableForfeitCandidates(context));
        FilterKickoffCandidatesForAutomatedTeam(candidates);
        if (MatchManager.Instance?.groundBallManager?.isActivated == true)
            candidates.RemoveAll(action => action.actionType == RoomActionType.GroundPass
                && action.step == RoomDecisionStep.ChooseActionType);
        return candidates;
    }

    private void FilterKickoffCandidatesForAutomatedTeam(List<RoomActionCandidate> candidates)
    {
        if (candidates == null || string.IsNullOrWhiteSpace(activeKickoffAutomationTeam))
        {
            return;
        }

        bool expectsHome = string.Equals(activeKickoffAutomationTeam, "Home", System.StringComparison.OrdinalIgnoreCase);
        candidates.RemoveAll(candidate =>
        {
            if (candidate == null
                || !string.Equals(candidate.manager, nameof(KickoffManager), System.StringComparison.Ordinal))
            {
                return false;
            }

            PlayerToken token = candidate.actor ?? candidate.targetToken;
            return token != null && token.isHomeTeam != expectsHome;
        });
    }

    private List<RoomActionCandidate> BuildTacticalExecutableCandidatesCore(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context,
        bool preferCuratedCandidates)
    {
        var confirmations = BuildPendingTargetConfirmations(context);
        if (confirmations.Count > 0) return confirmations;
        var interruption = BuildMovementInterruptionCandidates(context);
        if (interruption.Count > 0)
        {
            ClearCommittedBranch();
            ClearExecutionLock();
            return interruption;
        }
        List<RoomActionCandidate> committedBranchCandidates = BuildCommittedBranchCandidates(context);
        if (committedBranchCandidates.Count > 0)
        {
            return RemoveForfeitCandidates(committedBranchCandidates);
        }

        List<RoomActionCandidate> lockedExecutionCandidates = BuildLockedExecutionCandidates(context);
        if (lockedExecutionCandidates.Count > 0)
        {
            return RemoveForfeitCandidates(lockedExecutionCandidates);
        }

        if (HasExecutionLock())
        {
            return new List<RoomActionCandidate>();
        }

        if (!preferCuratedCandidates)
        {
            return BuildAllExecutableCandidates(snapshot, context);
        }

        List<RoomActionCandidate> curatedCandidates = BuildExecutableCandidates(snapshot, context);
        if (curatedCandidates.Count > 0)
        {
            return curatedCandidates;
        }

        return BuildAllExecutableCandidates(snapshot, context);
    }

    private static List<RoomActionCandidate> BuildAllExecutableCandidates(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context)
    {
        List<RoomActionCandidate> candidates = new List<RoomActionCandidate>();
        AddTacticalCandidates(candidates, RemoveForfeitCandidates(BuildExecutableKeyCandidates(snapshot, context)));
        AddTacticalCandidates(candidates, RemoveForfeitCandidates(BuildExecutableTokenCandidates(context)));
        AddTacticalCandidates(candidates, RemoveForfeitCandidates(BuildExecutableHexCandidates(context)));
        return candidates;
    }

    private static void AddTacticalCandidates(List<RoomActionCandidate> candidates, List<RoomActionCandidate> additions)
    {
        if (candidates == null || additions == null)
        {
            return;
        }

        foreach (RoomActionCandidate addition in additions)
        {
            if (addition != null && !ContainsEquivalentCandidate(candidates, addition))
            {
                candidates.Add(addition);
            }
        }
    }

    private static bool ContainsEquivalentCandidate(List<RoomActionCandidate> candidates, RoomActionCandidate addition)
    {
        if (candidates == null || addition == null)
        {
            return false;
        }

        foreach (RoomActionCandidate candidate in candidates)
        {
            if (candidate == addition)
            {
                return true;
            }

            if (candidate == null)
            {
                continue;
            }

            string candidateKey = !string.IsNullOrWhiteSpace(candidate.key) ? candidate.key.Trim() : string.Empty;
            string additionKey = !string.IsNullOrWhiteSpace(addition.key) ? addition.key.Trim() : string.Empty;
            if (candidate.actionType == addition.actionType
                && candidate.step == addition.step
                && string.Equals(candidateKey, additionKey, System.StringComparison.OrdinalIgnoreCase)
                && GetCandidateToken(candidate) == GetCandidateToken(addition)
                && candidate.targetHex == addition.targetHex)
            {
                return true;
            }
        }

        return false;
    }

    private static RoomAICandidateExplanation FindCandidateExplanation(
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate candidate)
    {
        if (snapshot == null || candidate == null)
        {
            return null;
        }

        foreach (RoomAICandidateExplanation explanation in snapshot.candidateExplanations)
        {
            if (explanation != null && explanation.candidate == candidate)
            {
                return explanation;
            }
        }

        return null;
    }

    private static string FormatCandidateExplanation(RoomAICandidateExplanation explanation)
    {
        if (explanation == null)
        {
            return string.Empty;
        }

        List<string> facts = new List<string>
        {
            $"type={explanation.actionType}",
            $"step={explanation.step}"
        };

        if (!string.IsNullOrWhiteSpace(explanation.key))
        {
            facts.Add($"key={explanation.key}");
        }

        if (!string.IsNullOrWhiteSpace(explanation.actorTokenKey))
        {
            facts.Add($"actor={explanation.actorTokenKey}@{FormatCoordinates(explanation.actorHex)}");
        }

        if (!string.IsNullOrWhiteSpace(explanation.targetTokenKey))
        {
            facts.Add($"targetToken={explanation.targetTokenKey}@{FormatCoordinates(explanation.targetTokenHex)}");
        }

        if (explanation.targetHex != null)
        {
            facts.Add($"targetHex={FormatCoordinates(explanation.targetHex)}");
        }

        if (explanation.actorDistanceToBall >= 0)
        {
            facts.Add($"actorBallDist={explanation.actorDistanceToBall}");
        }

        if (explanation.targetDistanceToBall >= 0)
        {
            facts.Add($"targetBallDist={explanation.targetDistanceToBall}");
        }

        facts.Add($"forwardDelta={explanation.forwardProgressDelta}");
        facts.Add($"advances={explanation.advancesTowardOpponentGoal}");
        facts.Add($"canShootFrom={explanation.targetCanShootFrom}");
        facts.Add($"canHeadFrom={explanation.targetCanHeadFrom}");
        facts.Add($"walkInGoal={explanation.targetIsInOpponentGoal}");
        if (explanation.walkInExpectedGoalProbability >= 0f)
        {
            facts.Add($"walkInXG={explanation.walkInExpectedGoalProbability:0.000}");
        }
        facts.Add($"thirdGkWallHex={explanation.targetIsThirdGoalkeeperWallHex}");
        facts.Add($"opponentDistance={explanation.nearestOpponentDistance}");
        facts.Add($"tackleReach={explanation.opponentsWithinTackleReach}");
        facts.Add($"bestReachableTackling={explanation.bestReachableOpponentTackling}");
        facts.Add($"receiverDribbling={explanation.receiverDribbling}");
        facts.Add($"tackleMismatch={explanation.tackleToDribblingMismatch}");
        facts.Add($"support={explanation.friendlySupportWithinRange}");
        facts.Add($"targetInF3={explanation.targetIsInFinalThird}");
        facts.Add($"stepsToBox={explanation.minimumStepsToOpponentPenaltyBox}");
        facts.Add($"receiverCanReachBox={explanation.receiverCanReachOpponentPenaltyBoxNextMovement}");
        facts.Add($"stepsToShot={explanation.minimumStepsToShootingHex}");
        facts.Add($"receiverCanReachShot={explanation.receiverCanReachShootingHexNextMovement}");
        facts.Add($"groundPassInterceptors={explanation.groundPassInterceptionCandidates}");
        if (explanation.isGroundPassContinuation)
        {
            facts.Add($"groundPassCollector={explanation.groundPassCollectorTokenKey}");
            facts.Add($"groundPassPickupDistance={explanation.groundPassCollectorPickupDistance}");
            facts.Add($"groundPassSecondMovementPickup={explanation.groundPassCollectorNeedsSecondMovement}");
            facts.Add($"groundPassSelfCollector={explanation.groundPassCollectorIsPasser}");
            facts.Add($"groundPassEscapesTackle={explanation.groundPassEscapesImmediateTacklePressure}");
            facts.Add($"groundPassPrePickupChallenges={explanation.groundPassPrePickupChallengeCount}");
            facts.Add($"groundPassRetention={explanation.groundPassRetentionProbability:0.000}");
        }
        if (explanation.isFirstTimePassShotSetup)
        {
            facts.Add($"ftpExtraPaceUnlock={explanation.firstTimePassExtraPaceUnlocksShot}");
            facts.Add($"ftpSetupShooter={explanation.firstTimePassSetupShooterTokenKey}");
            facts.Add($"ftpShooterStats=S{explanation.firstTimePassSetupShooterShooting}/P{explanation.firstTimePassSetupShooterPace}");
            facts.Add($"ftpSetupHex={FormatCoordinates(explanation.firstTimePassBestSetupHex)}");
            facts.Add($"ftpShotHex={FormatCoordinates(explanation.firstTimePassReachableShotHex)}");
            facts.Add($"ftpShotDistance={explanation.firstTimePassShotDistanceBefore}->{explanation.firstTimePassShotDistanceAfter}");
            facts.Add($"ftpPassRetention={explanation.firstTimePassPassRetentionProbability:0.000}");
            facts.Add($"ftpReachShot={explanation.firstTimePassReachShotProbability:0.000}");
            facts.Add($"ftpRawShotXG={explanation.firstTimePassRawShotExpectedGoals:0.000}");
            facts.Add($"ftpCombinedXG={explanation.firstTimePassCombinedExpectedGoals:0.000}");
        }
        if (explanation.isSetPieceSetup)
        {
            facts.Add($"cornerSetup={explanation.isCornerSetup}");
            facts.Add($"cornerFree3={explanation.isCornerFreeThreeMove}");
            facts.Add($"headerRank={explanation.setPieceActorHeaderRank}");
            facts.Add($"cornerAnchor={explanation.targetIsCornerHeaderAnchor}");
            facts.Add($"adjacentGoodHeaders={explanation.adjacentVeryGoodAttackingHeaders}");
            facts.Add($"adjacentMarkedHeading={explanation.adjacentMarkedAttackerHeading}");
            facts.Add($"twoHexMarkedHeading={explanation.twoHexMarkedAttackerHeading}");
        }
        if (explanation.isSetPieceHighPassPlan)
        {
            facts.Add($"setPieceHpAccuracy={explanation.setPieceHighPassAccuracyProbability:0.000}");
            facts.Add($"setPieceHeaderXG={explanation.setPieceHeaderGoalProbability:0.000}");
            facts.Add($"setPieceInaccuracyXG={explanation.setPieceInaccurateHighPassExpectedGoals:0.000}");
            facts.Add($"setPieceInaccuracyOutcomes={explanation.setPieceInaccuracyOutcomesEvaluated}");
        }
        if (explanation.isHighPassGoalkeeperDecision)
        {
            facts.Add($"gkRushesForHp={explanation.goalkeeperRushesForHighPass}");
            facts.Add($"defensiveHeaderXGA={explanation.defensiveExpectedGoalsAgainst:0.000}");
        }
        if (explanation.expectedGoals >= 0f)
        {
            facts.Add($"xG={explanation.expectedGoals:0.000}");
            facts.Add($"shotPenalty={explanation.shootingPenalty}");
            facts.Add($"shotBlockers={explanation.shotBlockerCount}");
            facts.Add($"gkPenalty={explanation.goalkeeperPenalty}");
        }
        if (explanation.bestReachableSnapshotExpectedGoals >= 0f)
        {
            facts.Add($"bestReachableSnapshotRouteXG={explanation.bestReachableSnapshotExpectedGoals:0.000}");
            facts.Add($"bestReachableSnapshotRawXG={explanation.bestReachableSnapshotRawExpectedGoals:0.000}");
            facts.Add($"bestReachableSnapshotReach={explanation.bestReachableSnapshotReachProbability:0.000}");
            facts.Add($"bestReachableSnapshotTurnover={explanation.bestReachableSnapshotTurnoverProbability:0.000}");
            facts.Add($"bestReachableSnapshotHex={FormatCoordinates(explanation.bestReachableSnapshotHex)}");
            facts.Add($"reachableSnapshotHexes={explanation.reachableSnapshotHexesEvaluated}");
            facts.Add($"snapshotXGFrontier={explanation.reachableSnapshotExpectedGoalsFrontier}");
        }
        if (explanation.isFullPaceDribbleRoute)
        {
            facts.Add($"dribbleDestination={FormatCoordinates(explanation.fullPaceDribbleDestination)}");
            facts.Add($"dribbleSteps={explanation.fullPaceDribbleSteps}");
            facts.Add($"dribbleProgress={explanation.fullPaceDribbleProgress}");
            facts.Add($"dribbleReach={explanation.fullPaceDribbleReachProbability:0.000}");
            facts.Add($"dribbleTurnover={explanation.fullPaceDribbleTurnoverProbability:0.000}");
            facts.Add($"dribbleUtility={explanation.fullPaceDribbleUtility}");
            facts.Add($"dribblePath={string.Join("->", explanation.fullPaceDribblePath.Select(FormatCoordinates))}");
        }
        if (explanation.isSnapshotDefensiveMove)
        {
            facts.Add($"snapshotDefenseMaxXGA={explanation.snapshotDefenseMaximumExpectedGoals:0.000}");
            facts.Add($"snapshotDefenseWorstTarget={FormatCoordinates(explanation.snapshotDefenseWorstTargetHex)}");
            facts.Add($"snapshotDefenseBestMove={FormatCoordinates(explanation.snapshotDefenseBestMoveHex)}");
            facts.Add($"snapshotDefenseGKPenalty={explanation.snapshotDefenseGoalkeeperPenalty}");
        }
        if (explanation.isGoalkeeperPositioning)
        {
            facts.Add($"gkPositioningMaxXGA={explanation.goalkeeperPositioningMaximumExpectedGoals:0.000}");
            facts.Add($"gkPositioningWorstTarget={FormatCoordinates(explanation.goalkeeperPositioningWorstTargetHex)}");
            facts.Add($"gkPositioningBlockers={explanation.goalkeeperPositioningBlockerCount}");
            facts.Add($"gkPositioningPenalty={explanation.goalkeeperPositioningGoalkeeperPenalty}");
        }
        if (explanation.isRegularFinalThirdReorganization)
        {
            facts.Add($"regularF3=true");
            facts.Add($"secondDefenderLine={explanation.currentSecondDefenderLineX}->{explanation.proposedSecondDefenderLineX}");
            facts.Add($"offsideLineRaise={explanation.offsideLineRaise}");
            facts.Add($"actorDefinesSecondDefenderLine={explanation.actorDefinesSecondDefenderLine}");
            facts.Add($"reachableOnsideOpponents={explanation.reachableOnsideOpponents}");
            facts.Add($"gkOwnGoalDistanceDelta={explanation.goalkeeperOwnGoalDistanceDelta}");
        }
        if (explanation.possessionChain != null)
        {
            facts.Add($"chainGoal={explanation.possessionChain.goalProbability:0.000}");
            facts.Add($"chainAttackKeeps={explanation.possessionChain.attackRetainsPossessionProbability:0.000}");
            facts.Add($"chainDefenseWins={explanation.possessionChain.defenseWinsPossessionProbability:0.000}");
            facts.Add($"chainAttackRestart={explanation.possessionChain.attackingRestartProbability:0.000}");
            facts.Add($"chainDefenseRestart={explanation.possessionChain.defendingRestartProbability:0.000}");
            facts.Add($"chainUnresolved={explanation.possessionChain.unresolvedProbability:0.000}");
            facts.Add($"chainBranches={explanation.possessionChain.evaluatedBranchCount}");
        }
        if (explanation.isHeaderControlChoice)
        {
            facts.Add($"headerControlToken={explanation.bestHeaderControllerTokenKey}");
            facts.Add($"headerControlDribbling={explanation.bestHeaderControllerDribbling}");
            facts.Add($"headerControlPenalty={explanation.headerControlPenalty}");
            facts.Add($"headerControlSuccess={explanation.headerControlSuccessProbability:0.000}");
            facts.Add($"headerControlContinuationValue={explanation.headerControlContinuationValue}");
            facts.Add($"headerControlValue={explanation.headerControlExpectedValue}");
        }
        if (explanation.isFreeHeaderChoice)
        {
            facts.Add($"freeHeaderTarget={FormatCoordinates(explanation.freeHeaderBestTargetHex)}");
            facts.Add($"freeHeaderReceiver={explanation.freeHeaderBestReceiverTokenKey}");
            facts.Add($"freeHeaderPossession={explanation.freeHeaderExpectedPossessionProbability:0.000}");
            facts.Add($"freeHeaderXG={explanation.freeHeaderExpectedGoals:0.000}");
            facts.Add($"freeHeaderValue={explanation.freeHeaderExpectedValue}");
            facts.Add($"freeHeaderTargets={explanation.freeHeaderTargetsEvaluated}");
        }
        if (explanation.isHeadedPassTarget)
        {
            facts.Add($"headedPassToPlayer={explanation.headedPassTargetsPlayer}");
            facts.Add($"headedPassToSpace={explanation.headedPassTargetsSpace}");
            facts.Add($"headedPassOffside={explanation.headedPassTargetIsOffside}");
            facts.Add($"headedPassInterceptors={explanation.headedPassInterceptors}");
            facts.Add($"headedPassInterception={explanation.headedPassInterceptionProbability:0.000}");
            facts.Add($"headedPassSnapshotXG={explanation.headedPassImmediateSnapshotExpectedGoals:0.000}");
            facts.Add($"headedPassSnapshotPenalty={explanation.headedPassSnapshotPenalty}");
            facts.Add($"headedPassSnapshotBlockers={explanation.headedPassSnapshotBlockers}");
            facts.Add($"headedPassSnapshotGkPenalty={explanation.headedPassSnapshotGoalkeeperPenalty}");
            facts.Add($"headedPassSpaceCollector={explanation.headedPassSpaceCollectorTokenKey}");
            facts.Add($"headedPassSpaceSnapshotHex={FormatCoordinates(explanation.headedPassSpaceSnapshotHex)}");
            facts.Add($"headedPassSpacePickupDistance={explanation.headedPassSpacePickupDistance}");
            facts.Add($"headedPassSpaceRemainingPace={explanation.headedPassSpaceRemainingPace}");
            facts.Add($"headedPassSpaceTurnover={explanation.headedPassSpaceMovementTurnoverProbability:0.000}");
            facts.Add($"headedPassSpaceFoul={explanation.headedPassSpaceFoulProbability:0.000}");
            facts.Add($"headedPassSpaceReach={explanation.headedPassSpaceReachSnapshotProbability:0.000}");
            facts.Add($"headedPassSpaceSnapshotXG={explanation.headedPassSpaceSnapshotExpectedGoals:0.000}");
            facts.Add($"headedPassSpaceRouteXG={explanation.headedPassSpaceRouteExpectedGoals:0.000}");
            facts.Add($"headedPassSpaceRoutes={explanation.headedPassSpaceRoutesEvaluated}");
        }
        if (explanation.isSnapshotDecision)
        {
            facts.Add($"fullShotXG={explanation.fullShotExpectedGoals:0.000}");
            facts.Add($"retention={explanation.estimatedBallRetentionProbability:0.000}");
            facts.Add($"followUpTime={explanation.timeAllowsFollowUpShot}");
            facts.Add($"gkCanChallenge={explanation.goalkeeperCanChallengeBeforeFullShot}");
            facts.Add($"penaltyChance={explanation.estimatedPenaltyProbability:0.000}");
            facts.Add($"penaltyXG={explanation.penaltyExpectedGoals:0.000}");
            facts.Add($"challengerBooked={explanation.likelyChallengerIsBooked}");
            facts.Add($"dangerousChallenge={explanation.likelyChallengeIsDangerous}");
            facts.Add($"challengeCount={explanation.forecastChallengeCount}");
            facts.Add($"challengeSequence={explanation.forecastChallengeSequence}");
            facts.Add($"secondBookingChance={explanation.secondBookingProbability:0.000}");
            facts.Add($"walkInPhases={explanation.estimatedMovementPhasesToWalkIn}");
            facts.Add($"walkInRouteXG={explanation.estimatedWalkInExpectedGoals:0.000}");
        }

        return $" / {string.Join(", ", facts)}";
    }

    private static string FormatCoordinates(RoomHexCoordinates coordinates)
    {
        return coordinates != null ? $"({coordinates.x},{coordinates.z})" : "<none>";
    }

    private static string FormatDecisionSnapshotSummary(RoomAIDecisionSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return "state unavailable";
        }

        string ball = snapshot.ball != null ? $"({snapshot.ball.x},{snapshot.ball.z})" : "<none>";
        string tactics = FormatTacticalMatchup(snapshot.tacticalMatchup);
        return $"state={snapshot.currentState}, teamInAttack={snapshot.teamInAttack}, attackHasPossession={snapshot.attackHasPossession}, score={snapshot.homeScore}-{snapshot.awayScore}, ball={ball}, tokens={snapshot.tokens.Count}, legal={snapshot.legalActions.Count}, {tactics}";
    }

    private static string FormatTacticalMatchup(RoomAIMatchupTacticalProfile matchup)
    {
        if (matchup?.ownTeam == null)
        {
            return "tacticalProfile=<none>";
        }

        RoomAITeamTacticalProfile own = matchup.ownTeam;
        List<string> threats = matchup.defensiveThreats.Count > 0
            ? matchup.defensiveThreats
            : new List<string> { "none" };
        return $"identity={matchup.suggestedAttackingIdentity}, "
            + $"carrier={FormatTacticalPlayer(own.primaryBallCarrier)}, "
            + $"shooter={FormatTacticalPlayer(own.primaryShooter)}, "
            + $"header={FormatTacticalPlayer(own.primaryHeader)}, "
            + $"highPasser={FormatTacticalPlayer(own.primaryHighPasser)}, "
            + $"opponentPoorHandling={matchup.opponentGoalkeeperHasPoorHandling}, "
            + $"shootingThreat={FormatTacticalPlayer(matchup.primaryShootingThreat)}, "
            + $"fastestThreat={FormatTacticalPlayer(matchup.fastestScoringThreat)}, "
            + $"recoveryDefender={FormatTacticalPlayer(matchup.bestRecoveryDefender)}, "
            + $"lineAbsX={matchup.recommendedDefensiveLineAbsoluteX}, "
            + $"secondDefenderLineX={matchup.secondDeepestDefenderLineX}, "
            + $"effectiveOffsideLineX={matchup.currentOffsideLineX}, "
            + $"ballDefinesLine={matchup.ballDefinesOffsideLine}, "
            + $"linePlayer={FormatTacticalPlayer(matchup.offsideLineDefiningPlayer)}, "
            + $"threats={string.Join(" | ", threats)}";
    }

    private static string FormatTacticalPlayer(RoomAITokenSnapshot token)
    {
        return token == null
            ? "<none>"
            : $"{token.playerName}#{token.jerseyNumber}";
    }

    private static string FormatTacticalPlan(RoomAIAttackPlan plan)
    {
        return plan == null
            ? "No active plan."
            : $"{plan.planType} for {plan.team} (revision {plan.revision}): {plan.rationale}";
    }

    private List<RoomActionCandidate> BuildExecutableCandidates(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context)
    {
        List<RoomActionCandidate> candidates = BuildExecutableCandidatesCore(snapshot, context);
        AddTacticalCandidates(candidates, BuildExecutableForfeitCandidates(context));
        return candidates;
    }

    private List<RoomActionCandidate> BuildExecutableCandidatesCore(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context)
    {
        var confirmations = BuildPendingTargetConfirmations(context);
        if (confirmations.Count > 0) return confirmations;
        var interruption = BuildMovementInterruptionCandidates(context);
        if (interruption.Count > 0)
        {
            ClearCommittedBranch();
            ClearExecutionLock();
            return interruption;
        }
        List<RoomActionCandidate> committedBranchCandidates = BuildCommittedBranchCandidates(context);
        if (committedBranchCandidates.Count > 0)
        {
            return RemoveForfeitCandidates(committedBranchCandidates);
        }

        List<RoomActionCandidate> lockedExecutionCandidates = BuildLockedExecutionCandidates(context);
        if (lockedExecutionCandidates.Count > 0)
        {
            return RemoveForfeitCandidates(lockedExecutionCandidates);
        }

        if (HasExecutionLock())
        {
            return new List<RoomActionCandidate>();
        }

        List<RoomActionCandidate> kickoffCandidates = BuildKickoffExecutableCandidates(context);
        if (kickoffCandidates.Count > 0)
        {
            return kickoffCandidates;
        }

        List<RoomActionCandidate> penaltyKickCandidates = BuildPenaltyKickExecutableCandidates(context);
        if (penaltyKickCandidates.Count > 0)
        {
            return penaltyKickCandidates;
        }

        List<RoomActionCandidate> goalkeeperCandidates = BuildGoalkeeperExecutableCandidates(context);
        if (goalkeeperCandidates.Count > 0)
        {
            return goalkeeperCandidates;
        }

        List<RoomActionCandidate> throwInCandidates = BuildThrowInExecutableCandidates(context);
        if (throwInCandidates.Count > 0)
        {
            return throwInCandidates;
        }

        List<RoomActionCandidate> freeKickCandidates = BuildFreeKickExecutableCandidates(context);
        if (freeKickCandidates.Count > 0 || IsFreeKickDecisionContext(snapshot, context))
        {
            return freeKickCandidates;
        }

        List<RoomActionCandidate> finalThirdCandidates = BuildFinalThirdExecutableCandidates(snapshot, context);
        if (finalThirdCandidates.Count > 0 || IsFinalThirdDecisionContext(snapshot, context))
        {
            return finalThirdCandidates;
        }

        List<RoomActionCandidate> movementPhaseCandidates = BuildMovementPhaseExecutableCandidates(context);
        if (movementPhaseCandidates.Count > 0)
        {
            return movementPhaseCandidates;
        }

        List<RoomActionCandidate> groundBallCandidates = BuildGroundBallExecutableCandidates(context);
        if (groundBallCandidates.Count > 0)
        {
            return groundBallCandidates;
        }

        List<RoomActionCandidate> firstTimePassCandidates = BuildFirstTimePassExecutableCandidates(context);
        if (firstTimePassCandidates.Count > 0)
        {
            return firstTimePassCandidates;
        }

        List<RoomActionCandidate> headerCandidates = BuildHeaderExecutableCandidates(context);
        if (headerCandidates.Count > 0)
        {
            return headerCandidates;
        }

        List<RoomActionCandidate> highPassCandidates = BuildHighPassExecutableCandidates(context);
        if (highPassCandidates.Count > 0)
        {
            return highPassCandidates;
        }

        List<RoomActionCandidate> longBallCandidates = BuildLongBallExecutableCandidates(context);
        if (longBallCandidates.Count > 0)
        {
            return longBallCandidates;
        }

        List<RoomActionCandidate> keyCandidates = RemoveForfeitCandidates(BuildExecutableKeyCandidates(snapshot, context));
        if (keyCandidates.Count > 0)
        {
            return keyCandidates;
        }

        // A selected Snapshot defender owns a destination stage. Switching
        // actors is optional; it must not hide every legal destination.
        var blockerTargets = context?.actions.Where(action => action != null && action.isExecutableNow
            && action.manager == nameof(ShotManager) && action.actionType == RoomActionType.Block
            && action.step == RoomDecisionStep.ChooseTarget && action.targetHex != null).ToList();
        if (blockerTargets?.Count > 0) return blockerTargets;
        List<RoomActionCandidate> tokenCandidates = RemoveForfeitCandidates(BuildExecutableTokenCandidates(context));
        if (tokenCandidates.Count > 0)
        {
            return tokenCandidates;
        }

        return RemoveForfeitCandidates(BuildExecutableHexCandidates(context));
    }

    private static List<RoomActionCandidate> BuildFinalThirdExecutableCandidates(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context)
    {
        if (!IsFinalThirdDecisionContext(snapshot, context))
        {
            return new List<RoomActionCandidate>();
        }

        List<RoomActionCandidate> candidates = GetExecutableManagerCandidates(
            context,
            nameof(FinalThirdManager),
            excludeForfeits: false);
        List<RoomActionCandidate> forfeitCandidates = FilterCandidates(
            candidates,
            action => action.isForfeit && !string.IsNullOrWhiteSpace(action.key));
        List<RoomActionCandidate> tokenCandidates = FilterCandidates(
            candidates,
            action => action.actionType == RoomActionType.FinalThird
                && action.step == RoomDecisionStep.ChooseActor
                && GetCandidateToken(action) != null);
        if (tokenCandidates.Count > 0)
        {
            List<RoomActionCandidate> choices = new(forfeitCandidates);
            choices.AddRange(tokenCandidates);
            return choices;
        }

        List<RoomActionCandidate> hexCandidates = FilterCandidates(
            candidates,
            action => action.actionType == RoomActionType.FinalThird
                && action.step == RoomDecisionStep.ChooseTarget
                && action.targetHex != null);
        if (hexCandidates.Count > 0)
        {
            List<RoomActionCandidate> choices = new(forfeitCandidates);
            choices.AddRange(hexCandidates);
            return choices;
        }

        return FilterCandidates(
            candidates,
            action => !string.IsNullOrWhiteSpace(action.key));
    }

    private static bool IsFinalThirdDecisionContext(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context)
    {
        return string.Equals(context?.manager, nameof(FinalThirdManager), System.StringComparison.Ordinal)
            || string.Equals(snapshot?.manager, nameof(FinalThirdManager), System.StringComparison.Ordinal);
    }

    private static List<RoomActionCandidate> BuildKickoffExecutableCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> kickoffCandidates = GetExecutableManagerCandidates(
            context,
            nameof(KickoffManager),
            excludeForfeits: true);
        if (kickoffCandidates.Count == 0)
        {
            return kickoffCandidates;
        }

        List<RoomActionCandidate> setupChoices = FilterCandidates(
            kickoffCandidates,
            action => action.actionType == RoomActionType.SetupMove
                && (action.step == RoomDecisionStep.ChooseActor
                    || action.step == RoomDecisionStep.ChooseTarget));
        List<RoomActionCandidate> setupConfirmKeys = FilterCandidates(
            kickoffCandidates,
            action => action.actionType == RoomActionType.Kickoff
                && action.step == RoomDecisionStep.Confirm
                && !string.IsNullOrWhiteSpace(action.key));
        if (setupChoices.Count > 0 || setupConfirmKeys.Count > 0)
        {
            // Setup moves and confirmation must be ranked together. Returning the
            // confirmation alone made Ultra Hard press Space before it had seen a
            // single legal repositioning candidate.
            List<RoomActionCandidate> choices = new(setupChoices);
            choices.AddRange(setupConfirmKeys);
            return choices;
        }

        List<RoomActionCandidate> takerTokens = FilterCandidates(
            kickoffCandidates,
            action => action.actionType == RoomActionType.Kickoff
                && action.step == RoomDecisionStep.ChooseActor
                && GetCandidateToken(action) != null);
        return takerTokens.Count > 0 ? takerTokens : new List<RoomActionCandidate>();
    }

    private static List<RoomActionCandidate> BuildPenaltyKickExecutableCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> penaltyCandidates = GetExecutableManagerCandidates(
            context,
            nameof(PenaltyKickManager),
            excludeForfeits: true);
        if (penaltyCandidates.Count == 0)
        {
            return penaltyCandidates;
        }

        List<RoomActionCandidate> setupDestinations = FilterCandidates(
            penaltyCandidates,
            action => action.actionType == RoomActionType.SetupMove
                && action.step == RoomDecisionStep.ChooseTarget
                && action.targetHex != null);
        if (setupDestinations.Count > 0)
        {
            return setupDestinations;
        }

        List<RoomActionCandidate> requiredSetupTokens = FilterCandidates(
            penaltyCandidates,
            action => action.actionType == RoomActionType.SetupMove
                && action.step == RoomDecisionStep.ChooseActor
                && string.Equals(action.reason, "invalid_penalty_setup", System.StringComparison.Ordinal)
                && GetCandidateToken(action) != null);
        if (requiredSetupTokens.Count > 0)
        {
            return requiredSetupTokens;
        }

        List<RoomActionCandidate> confirmKeys = FilterCandidates(
            penaltyCandidates,
            action => action.actionType == RoomActionType.PenaltyKick
                && action.step == RoomDecisionStep.Confirm
                && !string.IsNullOrWhiteSpace(action.key));
        if (confirmKeys.Count > 0)
        {
            return confirmKeys;
        }

        List<RoomActionCandidate> kickerTokens = FilterCandidates(
            penaltyCandidates,
            action => action.actionType == RoomActionType.PenaltyKick
                && action.step == RoomDecisionStep.ChooseActor
                && GetCandidateToken(action) != null);
        return kickerTokens.Count > 0 ? kickerTokens : new List<RoomActionCandidate>();
    }

    private static List<RoomActionCandidate> BuildGoalkeeperExecutableCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> goalkeeperCandidates = GetExecutableManagerCandidates(
            context,
            nameof(GoalKeeperManager),
            excludeForfeits: true);
        if (goalkeeperCandidates.Count == 0)
        {
            return goalkeeperCandidates;
        }

        List<RoomActionCandidate> movementDestinations = FilterCandidates(
            goalkeeperCandidates,
            action => action.actionType == RoomActionType.GoalkeeperSave
                && action.step == RoomDecisionStep.ChooseTarget
                && action.targetHex != null);
        return movementDestinations.Count > 0 ? movementDestinations : goalkeeperCandidates;
    }

    private static List<RoomActionCandidate> BuildThrowInExecutableCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> throwInCandidates = GetExecutableManagerCandidates(
            context,
            nameof(ThrowInManager),
            excludeForfeits: true);
        if (throwInCandidates.Count == 0)
        {
            return throwInCandidates;
        }

        List<RoomActionCandidate> confirmTargets = FilterCandidates(
            throwInCandidates,
            action => action.actionType == RoomActionType.ThrowIn
                && action.step == RoomDecisionStep.Confirm
                && (action.targetHex != null || action.targetToken != null));
        if (confirmTargets.Count > 0)
        {
            return confirmTargets;
        }

        List<RoomActionCandidate> throwTargets = FilterCandidates(
            throwInCandidates,
            action => action.actionType == RoomActionType.ThrowIn
                && action.step == RoomDecisionStep.ChooseTarget
                && (action.targetHex != null || action.targetToken != null));
        if (throwTargets.Count > 0)
        {
            return throwTargets;
        }

        List<RoomActionCandidate> takerTokens = FilterCandidates(
            throwInCandidates,
            action => action.actionType == RoomActionType.ThrowIn
                && action.step == RoomDecisionStep.ChooseActor
                && GetCandidateToken(action) != null);
        if (takerTokens.Count > 0)
        {
            return takerTokens;
        }

        List<RoomActionCandidate> throwTypeKeys = FilterCandidates(
            throwInCandidates,
            action => action.step == RoomDecisionStep.ChooseActionType
                && !string.IsNullOrWhiteSpace(action.key)
                && action.actionType is RoomActionType.ThrowIn
                    or RoomActionType.GroundPass
                    or RoomActionType.HighPass
                    or RoomActionType.StartMovement);
        return throwTypeKeys.Count > 0 ? throwTypeKeys : throwInCandidates;
    }

    private static List<RoomActionCandidate> BuildFreeKickExecutableCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> freeKickCandidates = GetExecutableManagerCandidates(
            context,
            nameof(FreeKickManager),
            excludeForfeits: true);
        if (freeKickCandidates.Count == 0)
        {
            return freeKickCandidates;
        }

        List<RoomActionCandidate> setupDestinations = FilterCandidates(
            freeKickCandidates,
            action => action.actionType == RoomActionType.SetupMove
                && action.step == RoomDecisionStep.ChooseTarget
                && action.targetHex != null);

        List<RoomActionCandidate> goalkeeperRepositionSkips = FilterCandidates(
            freeKickCandidates,
            action => action.actionType == RoomActionType.FreeKick
                && action.step == RoomDecisionStep.InterruptionChoice
                && string.Equals(action.executionCommand, "goalkeeper_reposition_skip", System.StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(action.key));
        if (setupDestinations.Count > 0)
        {
            List<RoomActionCandidate> setupDestinationChoices = new(goalkeeperRepositionSkips);
            setupDestinationChoices.AddRange(setupDestinations);
            return setupDestinationChoices;
        }

        List<RoomActionCandidate> setupActors = FilterCandidates(
            freeKickCandidates,
            action => action.actionType == RoomActionType.FreeKick
                && action.step == RoomDecisionStep.ChooseActor
                && GetCandidateToken(action) != null);

        List<RoomActionCandidate> goalkeeperSelectionSkips = FilterCandidates(
            freeKickCandidates,
            action => action.actionType == RoomActionType.FreeKick
                && action.step == RoomDecisionStep.InterruptionChoice
                && string.Equals(action.executionCommand, "goalkeeper_reposition_skip", System.StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(action.key));
        // Selecting the keeper opens the destination comparison; it does not
        // spend the move. Compare keeping position against every destination there.
        if (setupActors.Any(action => action.actor?.IsGoalKeeper == true)) return setupActors;
        if (setupActors.Count > 0 || goalkeeperSelectionSkips.Count > 0)
        {
            List<RoomActionCandidate> setupActorChoices = new(goalkeeperSelectionSkips);
            setupActorChoices.AddRange(setupActors);
            return setupActorChoices;
        }

        List<RoomActionCandidate> executionKeys = FilterCandidates(
            freeKickCandidates,
            action => action.step == RoomDecisionStep.ChooseActionType
                && !string.IsNullOrWhiteSpace(action.key)
                && (action.actionType is RoomActionType.GroundPass
                    or RoomActionType.HighPass
                    or RoomActionType.LongBall
                    or RoomActionType.Shot));
        if (executionKeys.Count > 0)
        {
            return executionKeys;
        }

        return freeKickCandidates;
    }

    private static bool IsFreeKickDecisionContext(GameplayInstructionSnapshot snapshot, RoomDecisionContext context)
    {
        if (snapshot != null && string.Equals(snapshot.manager, nameof(FreeKickManager), System.StringComparison.Ordinal))
        {
            return true;
        }

        if (context == null)
        {
            return false;
        }

        if (string.Equals(context.manager, nameof(FreeKickManager), System.StringComparison.Ordinal))
        {
            return true;
        }

        foreach (RoomActionCandidate action in context?.actions ?? new List<RoomActionCandidate>())
        {
            if (action != null && string.Equals(action.manager, nameof(FreeKickManager), System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static List<RoomActionCandidate> BuildMovementInterruptionCandidates(RoomDecisionContext context)
    {
        var looseRolls = context?.actions.Where(action => action != null && action.isExecutableNow
            && action.manager == nameof(LooseBallManager) && action.actionType == RoomActionType.Roll).ToList();
        if (looseRolls?.Count > 0) return looseRolls;
        var keeperChoices = context?.actions.Where(action => action != null && action.isExecutableNow
            && (action.manager == nameof(GoalKeeperManager)
                || action.manager == nameof(HighPassManager)
                    && action.actionType is RoomActionType.GoalkeeperSave or RoomActionType.Decline)).ToList();
        if (keeperChoices?.Any(action => action.actionType == RoomActionType.GoalkeeperSave) == true)
            return keeperChoices;
        var moves = context?.actions.Where(action => action != null && action.isExecutableNow
            && action.manager == nameof(MovementPhaseManager)
            && action.actionType == RoomActionType.SetupMove
            && action.step == RoomDecisionStep.ChooseTarget && action.targetHex != null).ToList()
            ?? new List<RoomActionCandidate>();
        if (moves.Count > 0)
        {
            // Reposition is a new choice, not a continuation of StartMovement or
            // Roll. Keep the legal stay option alongside all legal destinations.
            moves.AddRange(context.actions.Where(action => action != null && action.isExecutableNow
                && action.manager == nameof(MovementPhaseManager) && action.key == "X" && action.isForfeit));
            return moves;
        }
        return context?.actions.Where(action => action != null && action.isExecutableNow
            && action.manager == nameof(MovementPhaseManager) && action.key == "V"
            && action.actionType == RoomActionType.StartMovement).ToList() ?? moves;
    }

    private static List<RoomActionCandidate> BuildMovementPhaseExecutableCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> movementCandidates = GetExecutableManagerCandidates(
            context,
            nameof(MovementPhaseManager),
            excludeForfeits: true);
        foreach (RoomActionCandidate action in context?.actions ?? new List<RoomActionCandidate>())
        {
            if (action != null
                && action.isExecutableNow
                && action.actionType == RoomActionType.ContinueWithoutShot
                && !movementCandidates.Contains(action))
            {
                movementCandidates.Add(action);
            }
        }
        if (movementCandidates.Count == 0)
        {
            return movementCandidates;
        }

        if (HasOnlyRootActionChoiceCandidates(movementCandidates))
        {
            return new List<RoomActionCandidate>();
        }

        List<RoomActionCandidate> snapshotDecisionKeys = FilterCandidates(
            movementCandidates,
            action => (action.actionType == RoomActionType.Shot
                    || action.actionType == RoomActionType.ContinueWithoutShot)
                && action.step == RoomDecisionStep.InterruptionChoice
                && !string.IsNullOrWhiteSpace(action.key));
        if (snapshotDecisionKeys.Exists(action => action.actionType == RoomActionType.Shot))
        {
            return snapshotDecisionKeys;
        }

        List<RoomActionCandidate> rollKeys = FilterCandidates(
            movementCandidates,
            action => action.actionType == RoomActionType.Roll && !string.IsNullOrWhiteSpace(action.key));
        if (rollKeys.Count > 0)
        {
            return rollKeys;
        }

        var collect = FilterCandidates(movementCandidates, action => action.actionType == RoomActionType.StartMovement
            && action.step == RoomDecisionStep.InterruptionChoice && action.key == "V");
        if (collect.Count > 0) return collect;

        List<RoomActionCandidate> interruptionKeys = FilterCandidates(
            movementCandidates,
            action => action.step == RoomDecisionStep.InterruptionChoice
                && !string.IsNullOrWhiteSpace(action.key)
                && action.actionType != RoomActionType.StartMovement);
        if (interruptionKeys.Count > 0)
        {
            return interruptionKeys;
        }

        List<RoomActionCandidate> nutmegTargetTokens = FilterCandidates(
            movementCandidates,
            action => action.actionType == RoomActionType.Nutmeg
                && action.step == RoomDecisionStep.ChooseTarget
                && action.targetToken != null);
        if (nutmegTargetTokens.Count > 0)
        {
            return nutmegTargetTokens;
        }

        List<RoomActionCandidate> repositionHexes = FilterCandidates(
            movementCandidates,
            action => action.actionType == RoomActionType.SetupMove
                && action.step == RoomDecisionStep.ChooseTarget
                && action.targetHex != null);
        if (repositionHexes.Count > 0)
        {
            return repositionHexes;
        }

        List<RoomActionCandidate> destinationHexes = FilterCandidates(
            movementCandidates,
            action => action.actionType == RoomActionType.StartMovement
                && action.step == RoomDecisionStep.ChooseTarget
                && action.targetHex != null);
        if (destinationHexes.Count > 0)
        {
            return destinationHexes;
        }

        List<RoomActionCandidate> actorTokens = FilterCandidates(
            movementCandidates,
            action => action.actionType == RoomActionType.StartMovement
                && action.step == RoomDecisionStep.ChooseActor
                && GetCandidateToken(action) != null);
        if (actorTokens.Count > 0)
        {
            return actorTokens;
        }

        List<RoomActionCandidate> startMovementKeys = FilterCandidates(
            movementCandidates,
            action => action.actionType == RoomActionType.StartMovement
                && action.step == RoomDecisionStep.ChooseActionType
                && !string.IsNullOrWhiteSpace(action.key));
        return startMovementKeys.Count > 0 ? startMovementKeys : movementCandidates;
    }

    private static List<RoomActionCandidate> BuildPendingTargetConfirmations(RoomDecisionContext context)
    {
        // Managers define the number of clicks for the current gameplay
        // difficulty. Never assume that selecting a target completes the action.
        return context?.actions.Where(action => action != null && action.isExecutableNow
            && action.step == RoomDecisionStep.Confirm
            && (action.targetHex != null || action.targetToken != null)
            && action.actionType is RoomActionType.GroundPass or RoomActionType.QuickThrow
                or RoomActionType.HighPass or RoomActionType.GoalkeeperKick or RoomActionType.LongBall
                or RoomActionType.FirstTimePass or RoomActionType.ThrowIn or RoomActionType.Shot)
            .ToList() ?? new List<RoomActionCandidate>();
    }

    private static List<RoomActionCandidate> BuildGroundBallExecutableCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> groundBallCandidates = GetExecutableManagerCandidates(
            context,
            nameof(GroundBallManager),
            excludeForfeits: true);
        if (groundBallCandidates.Count == 0)
        {
            return groundBallCandidates;
        }

        if (HasOnlyRootActionChoiceCandidates(groundBallCandidates))
        {
            return new List<RoomActionCandidate>();
        }

        List<RoomActionCandidate> rollKeys = FilterCandidates(
            groundBallCandidates,
            action => action.actionType == RoomActionType.Roll
                && action.step == RoomDecisionStep.Roll
                && !string.IsNullOrWhiteSpace(action.key));
        if (rollKeys.Count > 0)
        {
            return rollKeys;
        }

        List<RoomActionCandidate> confirmTargets = FilterCandidates(
            groundBallCandidates,
            action => action.actionType == RoomActionType.GroundPass
                && action.step == RoomDecisionStep.Confirm
                && (action.targetHex != null || action.targetToken != null));
        if (confirmTargets.Count > 0)
        {
            return confirmTargets;
        }

        List<RoomActionCandidate> passTargets = FilterCandidates(
            groundBallCandidates,
            action => action.actionType == RoomActionType.GroundPass
                && action.step == RoomDecisionStep.ChooseTarget
                && (action.targetHex != null || action.targetToken != null));
        if (passTargets.Count > 0)
        {
            return passTargets;
        }

        List<RoomActionCandidate> startPassKeys = FilterCandidates(
            groundBallCandidates,
            action => action.actionType == RoomActionType.GroundPass
                && action.step == RoomDecisionStep.ChooseActionType
                && !string.IsNullOrWhiteSpace(action.key));
        return startPassKeys.Count > 0 ? startPassKeys : groundBallCandidates;
    }

    private static List<RoomActionCandidate> BuildFirstTimePassExecutableCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> firstTimePassCandidates = GetExecutableManagerCandidates(
            context,
            nameof(FirstTimePassManager),
            excludeForfeits: true);
        if (firstTimePassCandidates.Count == 0)
        {
            return firstTimePassCandidates;
        }

        if (HasOnlyRootActionChoiceCandidates(firstTimePassCandidates))
        {
            return new List<RoomActionCandidate>();
        }

        List<RoomActionCandidate> rollKeys = FilterCandidates(
            firstTimePassCandidates,
            action => action.actionType == RoomActionType.Roll
                && action.step == RoomDecisionStep.Roll
                && !string.IsNullOrWhiteSpace(action.key));
        if (rollKeys.Count > 0)
        {
            return rollKeys;
        }

        List<RoomActionCandidate> confirmTargets = FilterCandidates(
            firstTimePassCandidates,
            action => action.actionType == RoomActionType.FirstTimePass
                && action.step == RoomDecisionStep.Confirm
                && (action.targetHex != null || action.targetToken != null));
        if (confirmTargets.Count > 0)
        {
            return confirmTargets;
        }

        List<RoomActionCandidate> movementDestinations = FilterCandidates(
            firstTimePassCandidates,
            action => action.actionType == RoomActionType.SetupMove
                && action.step == RoomDecisionStep.ChooseTarget
                && action.targetHex != null);
        if (movementDestinations.Count > 0)
        {
            return movementDestinations;
        }

        List<RoomActionCandidate> movementActors = FilterCandidates(
            firstTimePassCandidates,
            action => action.actionType == RoomActionType.SetupMove
                && action.step == RoomDecisionStep.ChooseActor
                && GetCandidateToken(action) != null);
        if (movementActors.Count > 0)
        {
            return movementActors;
        }

        List<RoomActionCandidate> passTargets = FilterCandidates(
            firstTimePassCandidates,
            action => action.actionType == RoomActionType.FirstTimePass
                && action.step == RoomDecisionStep.ChooseTarget
                && (action.targetHex != null || action.targetToken != null));
        if (passTargets.Count > 0)
        {
            return passTargets;
        }

        List<RoomActionCandidate> startPassKeys = FilterCandidates(
            firstTimePassCandidates,
            action => action.actionType == RoomActionType.FirstTimePass
                && action.step == RoomDecisionStep.ChooseActionType
                && !string.IsNullOrWhiteSpace(action.key));
        return startPassKeys.Count > 0 ? startPassKeys : firstTimePassCandidates;
    }

    private static List<RoomActionCandidate> BuildHeaderExecutableCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> headerCandidates = GetExecutableManagerCandidates(
            context,
            nameof(HeaderManager),
            excludeForfeits: true);
        if (headerCandidates.Count == 0)
        {
            return headerCandidates;
        }

        List<RoomActionCandidate> rollKeys = FilterCandidates(
            headerCandidates,
            action => action.actionType == RoomActionType.Roll
                && action.step == RoomDecisionStep.Roll
                && !string.IsNullOrWhiteSpace(action.key));
        if (rollKeys.Count > 0)
        {
            return rollKeys;
        }

        List<RoomActionCandidate> headerAtGoalChoices = FilterCandidates(
            headerCandidates,
            action => (action.actionType == RoomActionType.Header
                    && action.step == RoomDecisionStep.ChooseActionType
                    && string.Equals(action.key, "H", System.StringComparison.OrdinalIgnoreCase))
                || (action.actionType == RoomActionType.Shot
                    && action.step == RoomDecisionStep.ChooseTarget
                    && action.targetHex != null));
        if (headerAtGoalChoices.Count > 0)
        {
            return headerAtGoalChoices;
        }

        List<RoomActionCandidate> headerPassTargets = FilterCandidates(
            headerCandidates,
            action => action.actionType == RoomActionType.Header
                && action.step == RoomDecisionStep.ChooseTarget
                && (action.targetHex != null || action.targetToken != null));
        if (headerPassTargets.Count > 0)
        {
            return headerPassTargets;
        }

        List<RoomActionCandidate> freeHeaderOrControlKeys = FilterCandidates(
            headerCandidates,
            action => action.step == RoomDecisionStep.InterruptionChoice
                && !string.IsNullOrWhiteSpace(action.key)
                && action.actionType is RoomActionType.Header or RoomActionType.SelectToken);
        if (freeHeaderOrControlKeys.Count > 0)
        {
            return freeHeaderOrControlKeys;
        }

        List<RoomActionCandidate> nominationConfirmKeys = FilterCandidates(
            headerCandidates,
            action => action.actionType == RoomActionType.Header
                && action.step == RoomDecisionStep.Confirm
                && !string.IsNullOrWhiteSpace(action.key));
        if (nominationConfirmKeys.Count > 0)
        {
            return nominationConfirmKeys;
        }

        List<RoomActionCandidate> nominationActors = FilterCandidates(
            headerCandidates,
            action => action.actionType == RoomActionType.Header
                && action.step == RoomDecisionStep.ChooseActor
                && GetCandidateToken(action) != null);
        if (nominationActors.Count > 0)
        {
            return nominationActors;
        }

        return FilterCandidates(
            headerCandidates,
            action => action.actionType != RoomActionType.Header
                || action.step != RoomDecisionStep.Setup);
    }

    private static List<RoomActionCandidate> BuildHighPassExecutableCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> highPassCandidates = GetExecutableManagerCandidates(
            context,
            nameof(HighPassManager),
            excludeForfeits: true);
        if (highPassCandidates.Count == 0)
        {
            return highPassCandidates;
        }

        if (HasOnlyRootActionChoiceCandidates(highPassCandidates))
        {
            return new List<RoomActionCandidate>();
        }

        List<RoomActionCandidate> confirmTargets = FilterCandidates(
            highPassCandidates,
            action => IsHighPassTargetAction(action)
                && action.step == RoomDecisionStep.Confirm
                && (action.targetHex != null || action.targetToken != null));
        if (confirmTargets.Count > 0)
        {
            return confirmTargets;
        }

        List<RoomActionCandidate> goalkeeperRushKeys = FilterCandidates(
            highPassCandidates,
            action => (action.actionType is RoomActionType.GoalkeeperSave or RoomActionType.Decline)
                && action.step == RoomDecisionStep.InterruptionChoice
                && !string.IsNullOrWhiteSpace(action.key));

        List<RoomActionCandidate> goalkeeperRushHexes = FilterCandidates(
            highPassCandidates,
            action => action.actionType == RoomActionType.GoalkeeperSave
                && action.step == RoomDecisionStep.ChooseTarget
                && action.targetHex != null);

        if (goalkeeperRushKeys.Count > 0 || goalkeeperRushHexes.Count > 0)
        {
            List<RoomActionCandidate> goalkeeperRushChoices = new(goalkeeperRushKeys);
            goalkeeperRushChoices.AddRange(goalkeeperRushHexes);
            return goalkeeperRushChoices;
        }

        List<RoomActionCandidate> movementDestinations = FilterCandidates(
            highPassCandidates,
            action => action.actionType == RoomActionType.SetupMove
                && action.step == RoomDecisionStep.ChooseTarget
                && action.targetHex != null);
        if (movementDestinations.Count > 0)
        {
            return movementDestinations;
        }

        List<RoomActionCandidate> movementActors = FilterCandidates(
            highPassCandidates,
            action => action.actionType == RoomActionType.SetupMove
                && action.step == RoomDecisionStep.ChooseActor
                && GetCandidateToken(action) != null);
        if (movementActors.Count > 0)
        {
            return movementActors;
        }

        List<RoomActionCandidate> passTargets = FilterCandidates(
            highPassCandidates,
            action => IsHighPassTargetAction(action)
                && action.step == RoomDecisionStep.ChooseTarget
                && (action.targetHex != null || action.targetToken != null));
        if (passTargets.Count > 0)
        {
            return passTargets;
        }

        List<RoomActionCandidate> rollKeys = FilterCandidates(
            highPassCandidates,
            action => action.actionType == RoomActionType.Roll
                && action.step == RoomDecisionStep.Roll
                && !string.IsNullOrWhiteSpace(action.key));
        if (rollKeys.Count > 0)
        {
            return rollKeys;
        }

        List<RoomActionCandidate> startHighPassKeys = FilterCandidates(
            highPassCandidates,
            action => IsHighPassTargetAction(action)
                && action.step == RoomDecisionStep.ChooseActionType
                && !string.IsNullOrWhiteSpace(action.key));
        return startHighPassKeys.Count > 0 ? startHighPassKeys : highPassCandidates;
    }

    private static List<RoomActionCandidate> BuildLongBallExecutableCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> longBallCandidates = GetExecutableManagerCandidates(
            context,
            nameof(LongBallManager),
            excludeForfeits: true);
        if (longBallCandidates.Count == 0)
        {
            return longBallCandidates;
        }

        if (HasOnlyRootActionChoiceCandidates(longBallCandidates))
        {
            return new List<RoomActionCandidate>();
        }

        List<RoomActionCandidate> rollKeys = FilterCandidates(
            longBallCandidates,
            action => action.actionType == RoomActionType.Roll
                && action.step == RoomDecisionStep.Roll
                && !string.IsNullOrWhiteSpace(action.key));
        if (rollKeys.Count > 0)
        {
            return rollKeys;
        }

        List<RoomActionCandidate> confirmTargets = FilterCandidates(
            longBallCandidates,
            action => action.actionType == RoomActionType.LongBall
                && action.step == RoomDecisionStep.Confirm
                && (action.targetHex != null || action.targetToken != null));
        if (confirmTargets.Count > 0)
        {
            return confirmTargets;
        }

        List<RoomActionCandidate> claimKeys = FilterCandidates(
            longBallCandidates,
            action => action.actionType == RoomActionType.LongBall
                && action.step == RoomDecisionStep.InterruptionChoice
                && !string.IsNullOrWhiteSpace(action.key));

        List<RoomActionCandidate> goalkeeperMoveHexes = FilterCandidates(
            longBallCandidates,
            action => action.actionType == RoomActionType.SetupMove
                && action.step == RoomDecisionStep.ChooseTarget
                && action.targetHex != null);

        if (claimKeys.Count > 0 || goalkeeperMoveHexes.Count > 0)
        {
            List<RoomActionCandidate> goalkeeperInterruptionChoices = new(claimKeys);
            goalkeeperInterruptionChoices.AddRange(goalkeeperMoveHexes);
            return goalkeeperInterruptionChoices;
        }

        List<RoomActionCandidate> passTargets = FilterCandidates(
            longBallCandidates,
            action => action.actionType == RoomActionType.LongBall
                && action.step == RoomDecisionStep.ChooseTarget
                && (action.targetHex != null || action.targetToken != null));
        if (passTargets.Count > 0)
        {
            return passTargets;
        }

        List<RoomActionCandidate> startLongBallKeys = FilterCandidates(
            longBallCandidates,
            action => action.actionType == RoomActionType.LongBall
                && action.step == RoomDecisionStep.ChooseActionType
                && !string.IsNullOrWhiteSpace(action.key));
        return startLongBallKeys.Count > 0 ? startLongBallKeys : longBallCandidates;
    }

    private static bool HasOnlyRootActionChoiceCandidates(List<RoomActionCandidate> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return false;
        }

        foreach (RoomActionCandidate candidate in candidates)
        {
            if (candidate == null
                || candidate.step != RoomDecisionStep.ChooseActionType
                || string.IsNullOrWhiteSpace(candidate.key)
                || GetCandidateToken(candidate) != null
                || candidate.targetHex != null)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsHighPassTargetAction(RoomActionCandidate candidate)
    {
        return candidate != null
            && candidate.actionType is RoomActionType.HighPass or RoomActionType.GoalkeeperKick;
    }

    private static List<RoomActionCandidate> GetExecutableManagerCandidates(
        RoomDecisionContext context,
        string managerName,
        bool excludeForfeits)
    {
        List<RoomActionCandidate> candidates = new List<RoomActionCandidate>();
        if (context == null || string.IsNullOrWhiteSpace(managerName))
        {
            return candidates;
        }

        foreach (RoomActionCandidate action in context.actions)
        {
            if (action == null
                || !action.isExecutableNow
                || !string.Equals(action.manager, managerName, System.StringComparison.Ordinal))
            {
                continue;
            }

            if (excludeForfeits && action.isForfeit)
            {
                continue;
            }

            candidates.Add(action);
        }

        return candidates;
    }

    private static List<RoomActionCandidate> FilterCandidates(
        List<RoomActionCandidate> candidates,
        System.Predicate<RoomActionCandidate> predicate)
    {
        List<RoomActionCandidate> filteredCandidates = new List<RoomActionCandidate>();
        if (candidates == null || predicate == null)
        {
            return filteredCandidates;
        }

        foreach (RoomActionCandidate candidate in candidates)
        {
            if (candidate != null && predicate(candidate))
            {
                filteredCandidates.Add(candidate);
            }
        }

        return filteredCandidates;
    }

    private List<RoomActionCandidate> BuildLockedExecutionCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> candidates = new List<RoomActionCandidate>();
        if (!HasExecutionLock() || context == null)
        {
            return candidates;
        }

        bool hasLockedActionCandidates = false;
        foreach (RoomActionCandidate action in context.actions)
        {
            if (IsFreeKickExecutionChoiceCandidate(action))
            {
                continue;
            }

            if (!MatchesExecutionLock(action))
            {
                continue;
            }

            // A root action choice with the same type is a new action, not the
            // continuation of the one which established this lock. Keeping the
            // lock here collapses M/P/C/L to the previous action forever.
            if (action.step == RoomDecisionStep.ChooseActionType)
            {
                continue;
            }

            hasLockedActionCandidates = true;
            if (action.isExecutableNow)
            {
                candidates.Add(action);
            }
        }

        if (!hasLockedActionCandidates)
        {
            ClearExecutionLock();
        }

        // A lock owns the action, not its completed target-selection step.
        var confirms = candidates.Where(action => action.step == RoomDecisionStep.Confirm).ToList();
        if (confirms.Count > 0) return confirms;

        return candidates;
    }

    private static bool IsFreeKickExecutionChoiceCandidate(RoomActionCandidate action)
    {
        return action != null
            && string.Equals(action.manager, nameof(FreeKickManager), System.StringComparison.Ordinal)
            && action.step == RoomDecisionStep.ChooseActionType
            && !string.IsNullOrWhiteSpace(action.key)
            && action.actionType is RoomActionType.GroundPass
                or RoomActionType.HighPass
                or RoomActionType.LongBall
                or RoomActionType.Shot;
    }

    private static List<RoomActionCandidate> RemoveForfeitCandidates(List<RoomActionCandidate> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return new List<RoomActionCandidate>();
        }

        List<RoomActionCandidate> filteredCandidates = new List<RoomActionCandidate>();
        foreach (RoomActionCandidate candidate in candidates)
        {
            if (candidate != null
                && (!candidate.isForfeit
                    || candidate.actionType == RoomActionType.ContinueWithoutShot))
            {
                filteredCandidates.Add(candidate);
            }
        }

        return filteredCandidates;
    }

    private static List<RoomActionCandidate> BuildExecutableForfeitCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> candidates = new List<RoomActionCandidate>();
        foreach (RoomActionCandidate action in context?.actions ?? new List<RoomActionCandidate>())
        {
            if (action != null
                && action.isExecutableNow
                && (action.isForfeit || action.actionType == RoomActionType.ContinueWithoutShot))
            {
                candidates.Add(action);
            }
        }

        return candidates;
    }

    private List<RoomActionCandidate> BuildCommittedBranchCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> candidates = new List<RoomActionCandidate>();
        if (!HasCommittedBranch() || context == null)
        {
            return candidates;
        }

        bool hasBranchCandidates = false;
        foreach (RoomActionCandidate action in context.actions)
        {
            if (!MatchesCommittedBranch(action))
            {
                continue;
            }

            hasBranchCandidates = true;
            if (IsDownstreamCommittedBranchCandidate(action))
            {
                candidates.Add(action);
            }
        }

        if (!hasBranchCandidates)
        {
            ClearCommittedBranch();
        }

        return candidates;
    }

    private static List<RoomActionCandidate> BuildExecutableKeyCandidates(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context)
    {
        List<RoomActionCandidate> candidates = new List<RoomActionCandidate>();
        if (context != null)
        {
            foreach (RoomActionCandidate action in context.actions)
            {
                if (IsExecutableKeyCandidate(action))
                {
                    candidates.Add(action);
                }
            }

            foreach (string key in context.validKeys)
            {
                AddFallbackKeyCandidate(candidates, context.manager, key);
            }
        }

        // A populated manager context is authoritative. Empty contexts during
        // animation are deliberately empty, never a request to synthesize X.
        if (context == null && snapshot != null && snapshot.expectedKeys != null)
        {
            foreach (string key in snapshot.expectedKeys)
            {
                AddFallbackKeyCandidate(candidates, snapshot.manager, key);
            }
        }

        return candidates;
    }

    private static List<RoomActionCandidate> BuildExecutableTokenCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> candidates = new List<RoomActionCandidate>();
        if (context == null)
        {
            return candidates;
        }

        foreach (RoomActionCandidate action in context.actions)
        {
            if (IsExecutableTokenCandidate(action))
            {
                candidates.Add(action);
            }
        }

        foreach (PlayerToken token in context.validTokens)
        {
            AddFallbackTokenCandidate(candidates, context.manager, token);
        }

        return candidates;
    }

    private static List<RoomActionCandidate> BuildExecutableHexCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> candidates = new List<RoomActionCandidate>();
        if (context == null)
        {
            return candidates;
        }

        foreach (RoomActionCandidate action in context.actions)
        {
            if (IsExecutableHexCandidate(action))
            {
                candidates.Add(action);
            }
        }

        foreach (HexCell hex in context.validHexes)
        {
            AddFallbackHexCandidate(candidates, context.manager, hex);
        }

        return candidates;
    }

    private static bool IsExecutableKeyCandidate(RoomActionCandidate action)
    {
        return action != null
            && action.isExecutableNow
            && !string.IsNullOrWhiteSpace(action.key);
    }

    private static bool IsExecutableTokenCandidate(RoomActionCandidate action)
    {
        return action != null
            && action.isExecutableNow
            && string.IsNullOrWhiteSpace(action.key)
            && action.targetHex == null
            && GetCandidateToken(action) != null;
    }

    private static bool IsExecutableHexCandidate(RoomActionCandidate action)
    {
        return action != null
            && action.isExecutableNow
            && string.IsNullOrWhiteSpace(action.key)
            && action.targetHex != null;
    }

    private static bool IsDownstreamCommittedBranchCandidate(RoomActionCandidate action)
    {
        return action != null
            && action.isExecutableNow
            && string.IsNullOrWhiteSpace(action.key)
            && action.step != RoomDecisionStep.ChooseActor
            && action.step != RoomDecisionStep.Unknown
            && (action.targetHex != null || action.targetToken != null);
    }

    private static void AddFallbackKeyCandidate(List<RoomActionCandidate> candidates, string manager, string key)
    {
        if (string.IsNullOrWhiteSpace(key) || ContainsKeyCandidate(candidates, key))
        {
            return;
        }

        candidates.Add(new RoomActionCandidate
        {
            manager = manager,
            actionType = RoomActionType.Unknown,
            step = RoomDecisionStep.Unknown,
            key = key.Trim(),
            label = $"Press [{key.Trim()}]",
            isExecutableNow = true
        });
    }

    private static bool ContainsKeyCandidate(List<RoomActionCandidate> candidates, string key)
    {
        foreach (RoomActionCandidate candidate in candidates)
        {
            if (candidate != null
                && !string.IsNullOrWhiteSpace(candidate.key)
                && string.Equals(candidate.key.Trim(), key.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void AddFallbackTokenCandidate(List<RoomActionCandidate> candidates, string manager, PlayerToken token)
    {
        if (token == null || ContainsTokenCandidate(candidates, token))
        {
            return;
        }

        candidates.Add(new RoomActionCandidate
        {
            manager = manager,
            actionType = RoomActionType.SelectToken,
            step = RoomDecisionStep.ChooseActor,
            actor = token,
            label = FormatTokenAction(token),
            isExecutableNow = true
        });
    }

    private static bool ContainsTokenCandidate(List<RoomActionCandidate> candidates, PlayerToken token)
    {
        foreach (RoomActionCandidate candidate in candidates)
        {
            if (candidate != null && GetCandidateToken(candidate) == token)
            {
                return true;
            }
        }

        return false;
    }

    private static void AddFallbackHexCandidate(List<RoomActionCandidate> candidates, string manager, HexCell hex)
    {
        if (hex == null || ContainsHexCandidate(candidates, hex))
        {
            return;
        }

        candidates.Add(new RoomActionCandidate
        {
            manager = manager,
            actionType = RoomActionType.SelectHex,
            step = RoomDecisionStep.ChooseTarget,
            targetHex = hex,
            label = FormatHexAction(hex),
            isExecutableNow = true
        });
    }

    private static bool ContainsHexCandidate(List<RoomActionCandidate> candidates, HexCell hex)
    {
        foreach (RoomActionCandidate candidate in candidates)
        {
            if (candidate != null && candidate.targetHex == hex)
            {
                return true;
            }
        }

        return false;
    }

    private static PlayerToken GetCandidateToken(RoomActionCandidate candidate)
    {
        if (candidate == null)
        {
            return null;
        }

        return candidate.targetToken != null ? candidate.targetToken : candidate.actor;
    }

    private void ExecuteCandidate(RoomActionCandidate selected)
    {
        if (selected == null || !CanDispatchAutomatedCandidate(selected))
        {
            return;
        }

        if (string.Equals(selected.manager, nameof(KickoffManager), System.StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(selected.setupTeam))
        {
            KickoffManager kickoff = MatchManager.Instance?.kickoffManager;
            string kickoffError = "KickoffManager was not found.";
            if (kickoff != null && kickoff.TryExecuteAutomatedSetupCandidate(selected, out kickoffError))
            {
                ClearCommittedBranch();
                return;
            }

            Debug.LogWarning($"[Kickoff AI] atomic command invalidated: {kickoffError}");
            lastAutomatedExecutionFingerprint = string.Empty;
            DisarmAutomatedExecution();
            return;
        }

        if (!string.IsNullOrWhiteSpace(selected.key))
        {
            ClearCommittedBranch();
            ExecuteKeyCandidate(selected);
            return;
        }

        // Target-step actions can intentionally retain their actor so the tactical
        // scorer knows who is moving.  Execute the destination first; otherwise the
        // actor is clicked again and managers such as F3/GK remain stuck waiting for
        // their target hex.
        if (ShouldExecuteHexCandidateFirst(selected))
        {
            ExecuteHexCandidate(selected);
            return;
        }

        PlayerToken token = GetCandidateToken(selected);
        if (token != null)
        {
            ExecuteTokenCandidate(selected, token);
            return;
        }

        if (selected.targetHex != null)
        {
            ExecuteHexCandidate(selected);
        }
    }

    private static bool ShouldExecuteHexCandidateFirst(RoomActionCandidate selected)
    {
        return selected != null
            && selected.step is RoomDecisionStep.ChooseTarget or RoomDecisionStep.Confirm
            && selected.targetHex != null;
    }

    private void ExecuteKeyCandidate(RoomActionCandidate selected)
    {
        if (selected == null || string.IsNullOrWhiteSpace(selected.key))
        {
            return;
        }

        GameInputManager inputManager = gameInputManager != null
            ? gameInputManager
            : FindAnyObjectByType<GameInputManager>();
        if (inputManager == null)
        {
            Debug.LogWarning($"Room AI could not execute key [{selected.key}] because GameInputManager was not found.");
            return;
        }

        gameInputManager = inputManager;
        if (string.Equals(selected.manager, nameof(KickoffManager), System.StringComparison.Ordinal)
            && selected.step == RoomDecisionStep.Confirm
            && !string.IsNullOrWhiteSpace(activeKickoffAutomationTeam))
        {
            MatchManager.Instance?.kickoffManager?.SetPendingAutomatedConfirmationTeam(
                activeKickoffAutomationTeam);
        }
        if (inputManager.TryExecuteSyntheticKeyPress(selected.key, "Room AI", out string error))
        {
            AdvanceCommittedStagedDecision(selected);
            if (ShouldLockExecution(selected))
            {
                CommitExecutionLock(selected);
            }
        }
        else
        {
            lastAutomatedExecutionFingerprint = string.Empty;
            DisarmAutomatedExecution();
            preparedCodexDecision = null;
            Debug.LogWarning($"Room AI could not execute key [{selected.key}]: {error}");
        }
    }

    private void ExecuteTokenCandidate(RoomActionCandidate selected, PlayerToken token)
    {
        GameInputManager inputManager = gameInputManager != null
            ? gameInputManager
            : FindAnyObjectByType<GameInputManager>();
        if (inputManager == null)
        {
            Debug.LogWarning($"Room AI could not execute token [{FormatTokenName(token)}] because GameInputManager was not found.");
            return;
        }

        gameInputManager = inputManager;
        if (string.Equals(selected.manager, nameof(KickoffManager), System.StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(activeKickoffAutomationTeam))
        {
            MatchManager.Instance?.kickoffManager?.SetPendingAutomatedInputTeam(
                activeKickoffAutomationTeam);
        }
        if (inputManager.TryExecuteSyntheticTokenClick(token, "Room AI", out string error))
        {
            AdvanceCommittedStagedDecision(selected);
            CommitBranch(selected, token);
            if (ShouldLockExecution(selected))
            {
                CommitExecutionLock(selected);
            }
        }
        else
        {
            lastAutomatedExecutionFingerprint = string.Empty;
            DisarmAutomatedExecution();
            preparedCodexDecision = null;
            Debug.LogWarning($"Room AI could not execute token [{FormatActionCandidate(selected)}]: {error}");
        }
    }

    private void ExecuteHexCandidate(RoomActionCandidate selected)
    {
        if (selected == null || selected.targetHex == null)
        {
            return;
        }

        GameInputManager inputManager = gameInputManager != null
            ? gameInputManager
            : FindAnyObjectByType<GameInputManager>();
        if (inputManager == null)
        {
            Debug.LogWarning($"Room AI could not execute hex [{selected.targetHex.coordinates}] because GameInputManager was not found.");
            return;
        }

        gameInputManager = inputManager;
        if (inputManager.TryExecuteSyntheticHexClick(selected.targetHex, "Room AI", out string error))
        {
            AdvanceCommittedStagedDecision(selected);
            ClearCommittedBranch();
        }
        else
        {
            lastAutomatedExecutionFingerprint = string.Empty;
            DisarmAutomatedExecution();
            preparedCodexDecision = null;
            Debug.LogWarning($"Room AI could not execute hex [{FormatActionCandidate(selected)}]: {error}");
        }
    }

    private bool HasCommittedBranch()
    {
        return !string.IsNullOrWhiteSpace(committedManager)
            && committedActionType != RoomActionType.Unknown;
    }

    private bool HasExecutionLock()
    {
        return executionLockActionType != RoomActionType.Unknown;
    }

    private bool MatchesCommittedBranch(RoomActionCandidate action)
    {
        return action != null
            && string.Equals(action.manager, committedManager, System.StringComparison.Ordinal)
            && action.actionType == committedActionType;
    }

    private bool MatchesExecutionLock(RoomActionCandidate action)
    {
        return action != null
            && action.actionType == executionLockActionType;
    }

    private void CommitBranch(RoomActionCandidate selected, PlayerToken actor)
    {
        if (selected == null || actor == null)
        {
            ClearCommittedBranch();
            return;
        }

        committedManager = selected.manager;
        committedActionType = selected.actionType;
        lastCommittedActor = actor;
        lastCommittedBranch = $"{committedManager}/{committedActionType}/{FormatTokenName(actor)}";
    }

    private void CommitExecutionLock(RoomActionCandidate selected)
    {
        if (selected == null || selected.actionType == RoomActionType.Unknown)
        {
            ClearExecutionLock();
            return;
        }

        executionLockActionType = selected.actionType;
        lastExecutionLock = executionLockActionType.ToString();
    }

    private void ClearCommittedBranch()
    {
        committedManager = string.Empty;
        committedActionType = RoomActionType.Unknown;
        lastCommittedActor = null;
        lastCommittedBranch = string.Empty;
    }

    private void ClearExecutionLock()
    {
        executionLockActionType = RoomActionType.Unknown;
        lastExecutionLock = string.Empty;
    }

    private static bool ShouldLockExecution(RoomActionCandidate selected)
    {
        if (selected == null || selected.isForfeit)
        {
            return false;
        }

        return selected.actionType is RoomActionType.StartMovement
            or RoomActionType.GroundPass
            or RoomActionType.HighPass
            or RoomActionType.Header
            or RoomActionType.LongBall
            or RoomActionType.FirstTimePass
            or RoomActionType.Shot
            or RoomActionType.GoalkeeperKick
            or RoomActionType.ThrowIn
            or RoomActionType.Block
            or RoomActionType.GoalkeeperSave;
    }

    private static string FormatActionCandidate(RoomActionCandidate action)
    {
        if (action == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(action.label))
        {
            return action.label;
        }

        if (!string.IsNullOrWhiteSpace(action.key))
        {
            return $"Press [{action.key.Trim()}]";
        }

        if (action.targetToken != null)
        {
            return FormatTokenAction(action.targetToken);
        }

        if (action.actor != null)
        {
            return FormatTokenAction(action.actor);
        }

        if (action.targetHex != null)
        {
            return FormatHexAction(action.targetHex);
        }

        return action.actionType.ToString();
    }

    private static string BuildCandidatePoolKind(List<RoomActionCandidate> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return "executable";
        }

        bool hasKey = false;
        bool hasToken = false;
        bool hasHex = false;
        bool allKickoffConfirms = true;
        bool allKickoffTakers = true;
        bool allPenaltyKickers = true;
        bool allPenaltySetupMoves = true;
        bool allPenaltySetupActors = true;
        bool allPenaltyConfirms = true;
        bool allGoalkeeperMoves = true;
        bool allThrowInTargets = true;
        bool allThrowInConfirms = true;
        bool allThrowInTakers = true;
        bool allFreeKickActors = true;
        bool allFreeKickMoves = true;
        bool allFreeKickKeys = true;
        bool allHeaderNominations = true;
        bool allHeaderConfirms = true;
        bool allHeaderAtGoalChoices = true;
        bool allHeaderPassTargets = true;
        bool allHeaderRolls = true;
        bool allGroundPassTargets = true;
        bool allGroundPassConfirms = true;
        bool allFirstTimePassTargets = true;
        bool allFirstTimePassConfirms = true;
        bool allFirstTimePassMoves = true;
        bool allHighPassTargets = true;
        bool allHighPassConfirms = true;
        bool allHighPassMoves = true;
        bool allLongBallTargets = true;
        bool allLongBallConfirms = true;
        bool allLongBallMoves = true;
        foreach (RoomActionCandidate candidate in candidates)
        {
            if (candidate == null)
            {
                continue;
            }

            allKickoffConfirms &= candidate.manager == nameof(KickoffManager)
                && candidate.actionType == RoomActionType.Kickoff
                && candidate.step == RoomDecisionStep.Confirm;
            allKickoffTakers &= candidate.manager == nameof(KickoffManager)
                && candidate.actionType == RoomActionType.Kickoff
                && candidate.step == RoomDecisionStep.ChooseActor;
            allPenaltyKickers &= candidate.manager == nameof(PenaltyKickManager)
                && candidate.actionType == RoomActionType.PenaltyKick
                && candidate.step == RoomDecisionStep.ChooseActor;
            allPenaltySetupMoves &= candidate.manager == nameof(PenaltyKickManager)
                && candidate.actionType == RoomActionType.SetupMove
                && candidate.step == RoomDecisionStep.ChooseTarget;
            allPenaltySetupActors &= candidate.manager == nameof(PenaltyKickManager)
                && candidate.actionType == RoomActionType.SetupMove
                && candidate.step == RoomDecisionStep.ChooseActor;
            allPenaltyConfirms &= candidate.manager == nameof(PenaltyKickManager)
                && candidate.actionType == RoomActionType.PenaltyKick
                && candidate.step == RoomDecisionStep.Confirm;
            allGoalkeeperMoves &= candidate.manager == nameof(GoalKeeperManager)
                && candidate.actionType == RoomActionType.GoalkeeperSave
                && candidate.step == RoomDecisionStep.ChooseTarget;
            allThrowInTargets &= candidate.actionType == RoomActionType.ThrowIn
                && candidate.step == RoomDecisionStep.ChooseTarget;
            allThrowInConfirms &= candidate.actionType == RoomActionType.ThrowIn
                && candidate.step == RoomDecisionStep.Confirm;
            allThrowInTakers &= candidate.actionType == RoomActionType.ThrowIn
                && candidate.step == RoomDecisionStep.ChooseActor;
            allFreeKickActors &= candidate.manager == nameof(FreeKickManager)
                && candidate.actionType == RoomActionType.FreeKick
                && candidate.step == RoomDecisionStep.ChooseActor;
            allFreeKickMoves &= candidate.manager == nameof(FreeKickManager)
                && candidate.actionType == RoomActionType.SetupMove
                && candidate.step == RoomDecisionStep.ChooseTarget;
            allFreeKickKeys &= candidate.manager == nameof(FreeKickManager)
                && !string.IsNullOrWhiteSpace(candidate.key);
            allHeaderNominations &= candidate.actionType == RoomActionType.Header
                && candidate.step == RoomDecisionStep.ChooseActor;
            allHeaderConfirms &= candidate.actionType == RoomActionType.Header
                && candidate.step == RoomDecisionStep.Confirm;
            allHeaderAtGoalChoices &= candidate.manager == nameof(HeaderManager)
                && ((candidate.actionType == RoomActionType.Header
                        && candidate.step == RoomDecisionStep.ChooseActionType)
                    || (candidate.actionType == RoomActionType.Shot
                        && candidate.step == RoomDecisionStep.ChooseTarget));
            allHeaderPassTargets &= candidate.actionType == RoomActionType.Header
                && candidate.step == RoomDecisionStep.ChooseTarget;
            allHeaderRolls &= candidate.manager == nameof(HeaderManager)
                && candidate.actionType == RoomActionType.Roll
                && candidate.step == RoomDecisionStep.Roll;
            allGroundPassTargets &= candidate.actionType == RoomActionType.GroundPass
                && candidate.step == RoomDecisionStep.ChooseTarget;
            allGroundPassConfirms &= candidate.actionType == RoomActionType.GroundPass
                && candidate.step == RoomDecisionStep.Confirm;
            allFirstTimePassTargets &= candidate.actionType == RoomActionType.FirstTimePass
                && candidate.step == RoomDecisionStep.ChooseTarget;
            allFirstTimePassConfirms &= candidate.actionType == RoomActionType.FirstTimePass
                && candidate.step == RoomDecisionStep.Confirm;
            allFirstTimePassMoves &= candidate.actionType == RoomActionType.SetupMove
                && candidate.manager == nameof(FirstTimePassManager);
            allHighPassTargets &= IsHighPassTargetAction(candidate)
                && candidate.step == RoomDecisionStep.ChooseTarget;
            allHighPassConfirms &= IsHighPassTargetAction(candidate)
                && candidate.step == RoomDecisionStep.Confirm;
            allHighPassMoves &= candidate.actionType == RoomActionType.SetupMove
                && candidate.manager == nameof(HighPassManager);
            allLongBallTargets &= candidate.actionType == RoomActionType.LongBall
                && candidate.step == RoomDecisionStep.ChooseTarget;
            allLongBallConfirms &= candidate.actionType == RoomActionType.LongBall
                && candidate.step == RoomDecisionStep.Confirm;
            allLongBallMoves &= candidate.actionType == RoomActionType.SetupMove
                && candidate.manager == nameof(LongBallManager);

            if (!string.IsNullOrWhiteSpace(candidate.key))
            {
                hasKey = true;
            }

            if (GetCandidateToken(candidate) != null)
            {
                hasToken = true;
            }

            if (candidate.targetHex != null)
            {
                hasHex = true;
            }
        }

        int kindCount = (hasKey ? 1 : 0) + (hasToken ? 1 : 0) + (hasHex ? 1 : 0);
        if (allKickoffConfirms)
        {
            return "kick-off confirm";
        }

        if (allKickoffTakers)
        {
            return "kick-off taker";
        }

        if (allPenaltyKickers)
        {
            return "penalty taker";
        }

        if (allPenaltySetupMoves)
        {
            return "penalty setup move";
        }

        if (allPenaltySetupActors)
        {
            return "penalty setup token";
        }

        if (allPenaltyConfirms)
        {
            return "penalty setup confirm";
        }

        if (allGoalkeeperMoves)
        {
            return "goalkeeper move";
        }

        if (allThrowInTargets)
        {
            return "throw-in target";
        }

        if (allThrowInConfirms)
        {
            return "throw-in confirm";
        }

        if (allThrowInTakers)
        {
            return "throw-in taker";
        }

        if (allFreeKickMoves)
        {
            return "free-kick move";
        }

        if (allFreeKickActors)
        {
            return "free-kick setup";
        }

        if (allFreeKickKeys)
        {
            return "free-kick option";
        }

        if (allHeaderRolls)
        {
            return "header roll";
        }

        if (allHeaderAtGoalChoices)
        {
            return "header-at-goal";
        }

        if (allHeaderPassTargets)
        {
            return "headed-pass target";
        }

        if (allHeaderConfirms)
        {
            return "header confirm";
        }

        if (allHeaderNominations)
        {
            return "header nomination";
        }

        if (allGroundPassTargets)
        {
            return "ground-pass target";
        }

        if (allGroundPassConfirms)
        {
            return "ground-pass confirm";
        }

        if (allFirstTimePassTargets)
        {
            return "first-time-pass target";
        }

        if (allFirstTimePassConfirms)
        {
            return "first-time-pass confirm";
        }

        if (allFirstTimePassMoves)
        {
            return "first-time-pass move";
        }

        if (allHighPassTargets)
        {
            return "high-pass target";
        }

        if (allHighPassConfirms)
        {
            return "high-pass confirm";
        }

        if (allHighPassMoves)
        {
            return "high-pass move";
        }

        if (allLongBallTargets)
        {
            return "long-ball target";
        }

        if (allLongBallConfirms)
        {
            return "long-ball confirm";
        }

        if (allLongBallMoves)
        {
            return "long-ball move";
        }

        if (kindCount != 1)
        {
            return "executable";
        }

        if (hasKey)
        {
            return "key";
        }

        return hasToken ? "token" : "hex";
    }

    private static string FormatActionCandidateForInspector(RoomActionCandidate action)
    {
        if (action == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        builder.Append(action.actionType).Append(" / ").Append(action.step);
        if (action.isForfeit)
        {
            builder.Append(" / forfeit");
        }

        if (!string.IsNullOrWhiteSpace(action.key))
        {
            builder.Append(" / key=").Append(action.key);
        }

        if (action.actor != null)
        {
            builder.Append(" / actor=").Append(FormatTokenName(action.actor));
        }

        if (action.targetToken != null)
        {
            builder.Append(" / targetToken=").Append(FormatTokenName(action.targetToken));
        }

        if (action.targetHex != null)
        {
            builder.Append(" / targetHex=").Append(action.targetHex.coordinates);
        }

        if (!string.IsNullOrWhiteSpace(action.label))
        {
            builder.Append(" / ").Append(action.label);
        }

        return builder.ToString();
    }

    private static string FormatTokenAction(PlayerToken token)
    {
        string tokenName = FormatTokenName(token);
        return $"Click token {token.jerseyNumber}. {tokenName}";
    }

    private static string FormatTokenName(PlayerToken token)
    {
        if (token == null)
        {
            return string.Empty;
        }

        return !string.IsNullOrWhiteSpace(token.playerName) ? token.playerName : token.name;
    }

    private static string FormatHexAction(HexCell hex)
    {
        return $"Click hex {hex.coordinates}";
    }

    private static void AddClickActions(List<string> actions, string expectedInput, string instructionText)
    {
        if (string.IsNullOrWhiteSpace(expectedInput))
        {
            return;
        }

        string normalizedInput = expectedInput.ToLowerInvariant();
        string normalizedInstruction = string.IsNullOrWhiteSpace(instructionText)
            ? string.Empty
            : instructionText.ToLowerInvariant();

        if (normalizedInput.Contains("click_token"))
        {
            AddDistinct(actions, GetTokenClickLabel(normalizedInstruction));
        }
        else if (normalizedInput.Contains("click_hex"))
        {
            AddDistinct(actions, GetHexClickLabel(normalizedInstruction));
        }
        else if (normalizedInput.Contains("click"))
        {
            AddDistinct(actions, "Click a valid target");
        }

        if (normalizedInput.Contains("hover"))
        {
            AddDistinct(actions, "Hover a hex");
        }
    }

    private static bool HasKeyActionSummary(List<string> actions, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        string needle = $"Press [{key.Trim()}]";
        foreach (string action in actions)
        {
            if (!string.IsNullOrWhiteSpace(action)
                && action.StartsWith(needle, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetTokenClickLabel(string normalizedInstruction)
    {
        if (normalizedInstruction.Contains("defender"))
        {
            return "Click a defender";
        }

        if (normalizedInstruction.Contains("attacker"))
        {
            return "Click an attacker";
        }

        if (normalizedInstruction.Contains("free") || normalizedInstruction.Contains("valid"))
        {
            return "Click a free/valid token";
        }

        if (normalizedInstruction.Contains("player"))
        {
            return "Click a player";
        }

        return "Click a token";
    }

    private static string GetHexClickLabel(string normalizedInstruction)
    {
        if (normalizedInstruction.Contains("free") || normalizedInstruction.Contains("empty"))
        {
            return "Click a free hex";
        }

        if (normalizedInstruction.Contains("valid target") || normalizedInstruction.Contains("orange target"))
        {
            return "Click a valid target";
        }

        if (normalizedInstruction.Contains("highlighted"))
        {
            return "Click a highlighted hex";
        }

        return "Click a hex";
    }

    private static void AddDistinct(List<string> actions, string action)
    {
        if (!actions.Contains(action))
        {
            actions.Add(action);
        }
    }

    private static string BuildFingerprint(GameplayInstructionSnapshot snapshot, AIManager.RoomPersona persona, string availableActions)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(snapshot.manager).Append('|');
        builder.Append(snapshot.expectedTeam).Append('|');
        builder.Append(snapshot.expectedInput).Append('|');
        builder.Append(snapshot.instructionText).Append('|');
        builder.Append(availableActions).Append('|');
        builder.Append(persona);
        return builder.ToString();
    }

    /// <summary>
    /// Builds a cheap, deterministic identity for an AI choice point. The tactical
    /// planner is allowed to be expensive, so it must only run again when either
    /// the legal choice, match context, or a tactically relevant token state changes.
    /// </summary>
    private string BuildDecisionStateKey(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context,
        AIManager.RoomPersona persona,
        AIManager.CodexTacticianDifficulty codexTacticianDifficulty)
    {
        return BuildDecisionStateKeyCore(snapshot, context, persona,
            codexTacticianDifficulty, includePassSelection: true);
    }

    private string BuildDecisionStateKeyCore(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context,
        AIManager.RoomPersona persona,
        AIManager.CodexTacticianDifficulty codexTacticianDifficulty,
        bool includePassSelection)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        ulong hash = offsetBasis;

        AddDecisionHash(ref hash, snapshot?.manager);
        AddDecisionHash(ref hash, snapshot?.expectedTeam);
        AddDecisionHash(ref hash, snapshot?.expectedInput);
        // Hover text and preview decoration cannot invalidate a CPU search.
        // Committed targets, actors, legal actions and board state remain hashed.
        bool ignoreVisualOnlyState = true;
        if (!ignoreVisualOnlyState)
        {
            AddDecisionHash(ref hash, snapshot?.instructionText);
        }
        AddDecisionHash(ref hash, snapshot?.actorTokenKey);
        AddDecisionHash(ref hash, snapshot?.isAwaitingInput ?? false);
        AddDecisionHash(ref hash, (int)persona);
        AddDecisionHash(ref hash, (int)codexTacticianDifficulty);
        AddDecisionHash(ref hash, activeKickoffAutomationTeam);
        AddDecisionHash(ref hash, snapshot?.currentTargetHex);

        if (snapshot?.expectedKeys != null)
        {
            foreach (string key in snapshot.expectedKeys)
            {
                AddDecisionHash(ref hash, key);
            }
        }

        if (snapshot?.relatedTokenKeys != null)
        {
            foreach (string tokenKey in snapshot.relatedTokenKeys)
            {
                AddDecisionHash(ref hash, tokenKey);
            }
        }

        if (snapshot?.details != null)
        {
            foreach (KeyValuePair<string, string> detail in snapshot.details)
            {
                if (ignoreVisualOnlyState && IsVisualOnlyDecisionDetail(detail.Key))
                {
                    continue;
                }
                AddDecisionHash(ref hash, detail.Key);
                AddDecisionHash(ref hash, detail.Value);
            }
        }

        AddDecisionHash(ref hash, context?.manager);
        AddDecisionHash(ref hash, context?.expectedTeam);
        AddDecisionHash(ref hash, context?.expectedInput);
        AddDecisionHash(ref hash, context?.actions.Count ?? 0);
        if (context != null)
        {
            foreach (RoomActionCandidate action in context.actions)
            {
                AddDecisionHash(ref hash, action, ignoreVisualOnlyState);
            }

            foreach (string key in context.validKeys)
            {
                AddDecisionHash(ref hash, key);
            }

            foreach (string summary in context.validActionSummaries)
            {
                if (!ignoreVisualOnlyState)
                {
                    AddDecisionHash(ref hash, summary);
                }
            }

            foreach (PlayerToken token in context.validTokens)
            {
                AddDecisionHash(ref hash, token);
            }

            foreach (HexCell hex in context.validHexes)
            {
                AddDecisionHash(ref hash, hex);
            }
        }

        MatchManager matchManager = MatchManager.Instance;
        if (matchManager != null)
        {
            // Aggregated instruction fields may be overwritten by another
            // available manager. A selected pass target still changes the stage.
            GroundBallManager groundPass = matchManager.groundBallManager;
            AddDecisionHash(ref hash, groundPass?.isActivated ?? false);
            AddDecisionHash(ref hash, groundPass?.isAwaitingTargetSelection ?? false);
            AddDecisionHash(ref hash, groundPass?.isWaitingForDiceRoll ?? false);
            if (includePassSelection) AddDecisionHash(ref hash, groundPass?.currentTargetHex);
            AddDecisionHash(ref hash, (int)matchManager.currentState);
            AddDecisionHash(ref hash, (int)matchManager.teamInAttack);
            AddDecisionHash(ref hash, matchManager.attackHasPossession);
            AddDecisionHash(ref hash, (int)matchManager.homeTeamDirection);
            AddDecisionHash(ref hash, (int)matchManager.awayTeamDirection);
            AddDecisionHash(ref hash, matchManager.currentHalf);
            AddDecisionHash(ref hash, matchManager.isHalfExpired);
            AddDecisionHash(ref hash, matchManager.IsWaitingForExtraActionsRoll);
            // Automatic fallback can resolve a roll while CPU analysis is armed.
            // A later roll on the same board is a new decision, even if its
            // instruction/manager/team are otherwise identical.
            AddDecisionHash(ref hash, matchManager.TimingRollSequence);
            AddDecisionHash(ref hash, matchManager.completedActionsThisHalf);
            AddDecisionHash(ref hash, matchManager.totalCompletedActions);
            AddDecisionHash(ref hash, matchManager.ball?.GetCurrentHex());
            AddDecisionHash(ref hash, matchManager.LastTokenToTouchTheBallOnPurpose);
            AddDecisionHash(ref hash, matchManager.PreviousTokenToTouchTheBallOnPurpose);
            AddDecisionHash(ref hash, matchManager.hangingPassType);
            AddDecisionHash(ref hash, matchManager.hangingPassExcludedCollector);
            AddDecisionHash(ref hash, matchManager.setPieceTakerExcludedFromNextTouch);
            AddDecisionHash(ref hash, matchManager.clearPreviousOnNextBallCollection);
            AddDecisionHash(ref hash, matchManager.gameData?.stats?.homeTeamStats?.totalGoals ?? 0);
            AddDecisionHash(ref hash, matchManager.gameData?.stats?.awayTeamStats?.totalGoals ?? 0);

            if (matchManager.playerTokenManager?.allTokens != null)
            {
                foreach (PlayerToken token in matchManager.playerTokenManager.allTokens)
                {
                    AddDecisionHash(ref hash, token);
                }
            }
        }

        AddDecisionHash(ref hash, committedManager);
        AddDecisionHash(ref hash, (int)committedActionType);
        AddDecisionHash(ref hash, lastCommittedActor);
        AddDecisionHash(ref hash, (int)executionLockActionType);
        return hash.ToString("x16");
    }

    private static bool IsVisualOnlyDecisionDetail(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return key.IndexOf("hover", System.StringComparison.OrdinalIgnoreCase) >= 0
            || key.IndexOf("preview", System.StringComparison.OrdinalIgnoreCase) >= 0
            || key.IndexOf("highlight", System.StringComparison.OrdinalIgnoreCase) >= 0
            || key.IndexOf("instruction", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void AddDecisionHash(
        ref ulong hash,
        RoomActionCandidate action,
        bool ignorePresentation = false)
    {
        if (action == null)
        {
            AddDecisionHash(ref hash, 0);
            return;
        }

        AddDecisionHash(ref hash, action.id);
        AddDecisionHash(ref hash, action.manager);
        AddDecisionHash(ref hash, (int)action.actionType);
        AddDecisionHash(ref hash, (int)action.step);
        if (!ignorePresentation)
        {
            AddDecisionHash(ref hash, action.label);
        }
        AddDecisionHash(ref hash, action.key);
        AddDecisionHash(ref hash, action.actor);
        AddDecisionHash(ref hash, action.targetToken);
        AddDecisionHash(ref hash, action.targetHex);
        AddDecisionHash(ref hash, action.isExecutableNow);
        AddDecisionHash(ref hash, action.isForfeit);
        AddDecisionHash(ref hash, action.executionCommand);
        AddDecisionHash(ref hash, action.setupTeam);
        AddDecisionHash(ref hash, action.setupBoardStateKey);
        AddDecisionHash(ref hash, action.setupFormationPenaltyBefore);
        AddDecisionHash(ref hash, action.setupFormationPenaltyAfter);
        if (!ignorePresentation)
        {
            AddDecisionHash(ref hash, action.reason);
        }
    }

    private static void AddDecisionHash(ref ulong hash, PlayerToken token)
    {
        if (token == null)
        {
            AddDecisionHash(ref hash, 0);
            return;
        }

        AddDecisionHash(ref hash, token.isHomeTeam);
        AddDecisionHash(ref hash, token.jerseyNumber);
        AddDecisionHash(ref hash, token.GetCurrentHex());
        AddDecisionHash(ref hash, token.isPlaying);
        AddDecisionHash(ref hash, token.isAttacker);
        AddDecisionHash(ref hash, token.IsGoalKeeper);
        AddDecisionHash(ref hash, token.isBooked);
        AddDecisionHash(ref hash, token.isInjured);
        AddDecisionHash(ref hash, token.isSentOff);
        AddDecisionHash(ref hash, token.requiresSubstitution);
        AddDecisionHash(ref hash, token.pace);
        AddDecisionHash(ref hash, token.dribbling);
        AddDecisionHash(ref hash, token.highPass);
        AddDecisionHash(ref hash, token.resilience);
        AddDecisionHash(ref hash, token.heading);
        AddDecisionHash(ref hash, token.shooting);
        AddDecisionHash(ref hash, token.tackling);
        AddDecisionHash(ref hash, token.aerial);
        AddDecisionHash(ref hash, token.saving);
        AddDecisionHash(ref hash, token.handling);
    }

    private static void AddDecisionHash(ref ulong hash, HexCell hex)
    {
        if (hex == null)
        {
            AddDecisionHash(ref hash, int.MinValue);
            return;
        }

        AddDecisionHash(ref hash, hex.coordinates.x);
        AddDecisionHash(ref hash, hex.coordinates.z);
    }

    private static void AddDecisionHash(ref ulong hash, RoomHexCoordinates coordinates)
    {
        if (coordinates == null)
        {
            AddDecisionHash(ref hash, int.MinValue);
            return;
        }

        AddDecisionHash(ref hash, coordinates.x);
        AddDecisionHash(ref hash, coordinates.z);
    }

    private static void AddDecisionHash(ref ulong hash, bool value)
    {
        AddDecisionHash(ref hash, value ? 1 : 0);
    }

    private static void AddDecisionHash(ref ulong hash, int value)
    {
        unchecked
        {
            AddDecisionHashByte(ref hash, (byte)value);
            AddDecisionHashByte(ref hash, (byte)(value >> 8));
            AddDecisionHashByte(ref hash, (byte)(value >> 16));
            AddDecisionHashByte(ref hash, (byte)(value >> 24));
        }
    }

    private static void AddDecisionHash(ref ulong hash, string value)
    {
        if (value == null)
        {
            AddDecisionHash(ref hash, -1);
            return;
        }

        AddDecisionHash(ref hash, value.Length);
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            AddDecisionHashByte(ref hash, (byte)character);
            AddDecisionHashByte(ref hash, (byte)(character >> 8));
        }
    }

    private static void AddDecisionHashByte(ref ulong hash, byte value)
    {
        const ulong prime = 1099511628211UL;
        unchecked
        {
            hash ^= value;
            hash *= prime;
        }
    }

    private void InvalidateDecisionEvaluationCache()
    {
        preparedCodexStateKey = string.Empty;
        preparedCodexSnapshot = null;
        preparedCodexDecision = null;
        hasCachedDecisionEvaluation = false;
        cachedDecisionDisplayText = string.Empty;
        evaluatingDecisionStateKey = string.Empty;
    }

    private void UpdateInspectorState(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context,
        AIManager.RoomPersona persona,
        string availableActions,
        string selectedAction)
    {
        lastManager = snapshot.manager;
        lastExpectedTeam = snapshot.expectedTeam;
        lastPersona = AIManager.FormatRoomPersona(persona);
        lastAvailableActions = availableActions;
        lastSelectedAction = selectedAction;
        lastActionCandidates = new List<string>();
        if (context != null)
        {
            foreach (RoomActionCandidate action in context.actions)
            {
                string formattedAction = FormatActionCandidateForInspector(action);
                if (!string.IsNullOrWhiteSpace(formattedAction))
                {
                    lastActionCandidates.Add(formattedAction);
                }
            }

            AddFallbackKeyCandidatesForInspector(context, lastActionCandidates);
        }

        lastValidKeys = context != null ? new List<string>(context.validKeys) : new List<string>();
        lastActionSummaries = context != null ? new List<string>(context.validActionSummaries) : new List<string>();
        lastValidTokens = context != null ? new List<PlayerToken>(context.validTokens) : new List<PlayerToken>();
        lastValidHexes = context != null ? new List<HexCell>(context.validHexes) : new List<HexCell>();
        inspectorStatus = "Current/last AI decision.";
    }

    private void ClearInspectorState()
    {
        inspectorStatus = string.Empty;
        lastManager = string.Empty;
        lastExpectedTeam = string.Empty;
        lastPersona = string.Empty;
        lastAvailableActions = string.Empty;
        lastSelectedAction = string.Empty;
        lastActionCandidates.Clear();
        lastValidKeys.Clear();
        lastActionSummaries.Clear();
        recentCodexTacticianDecisionHistory.Clear();
        ClearCodexTacticianState();
        lastValidTokens.Clear();
        lastValidHexes.Clear();
        lastAutomatedExecutionFingerprint = string.Empty;
        lastAutomatedSelectedAction = string.Empty;
        ClearCommittedBranch();
        ClearExecutionLock();
        recentCodexMovementAttackStateSignatures.Clear();
    }

    private static void AddFallbackKeyCandidatesForInspector(RoomDecisionContext context, List<string> inspectorCandidates)
    {
        if (context == null || inspectorCandidates == null)
        {
            return;
        }

        foreach (string key in context.validKeys)
        {
            if (string.IsNullOrWhiteSpace(key) || ContextHasActionForKey(context, key))
            {
                continue;
            }

            inspectorCandidates.Add($"FallbackKey / {RoomDecisionStep.Unknown} / key={key.Trim()} / parsed from instruction");
        }
    }

    private static bool ContextHasActionForKey(RoomDecisionContext context, string key)
    {
        if (context == null || string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        foreach (RoomActionCandidate action in context.actions)
        {
            if (action != null
                && !string.IsNullOrWhiteSpace(action.key)
                && string.Equals(action.key.Trim(), key.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
