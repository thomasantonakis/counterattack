using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class RoomDecisionManager : MonoBehaviour
{
    public TextMeshProUGUI decisionText;
    public GameInputManager gameInputManager;

    [Header("Decision Inspector")]
    [SerializeField] private string lastManager;
    [SerializeField] private string lastExpectedTeam;
    [SerializeField] private string lastPersona;
    [SerializeField] private string lastAvailableActions;
    [SerializeField] private string lastSelectedAction;
    [SerializeField] private List<string> lastActionCandidates = new();
    [SerializeField] private List<string> lastValidKeys = new();
    [SerializeField] private List<string> lastActionSummaries = new();
    [SerializeField] private List<PlayerToken> lastValidTokens = new();
    [SerializeField] private List<HexCell> lastValidHexes = new();
    [SerializeField] private string lastRandomCommittedBranch;
    [SerializeField] private PlayerToken lastRandomCommittedActor;
    [SerializeField] private string lastRandomExecutionLock;

    private string lastLoggedFingerprint = string.Empty;
    private string lastRandomExecutedDecisionFingerprint = string.Empty;
    private string lastRandomSelectedAction = string.Empty;
    private string randomCommittedManager = string.Empty;
    private RoomActionType randomCommittedActionType = RoomActionType.Unknown;
    private RoomActionType randomExecutionLockActionType = RoomActionType.Unknown;

    public void ClearDecisionState()
    {
        if (decisionText != null)
        {
            decisionText.text = string.Empty;
        }

        ClearInspectorState();
        lastLoggedFingerprint = string.Empty;
    }

    public void UpdateDecision(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context,
        MatchManager.GameSettings settings,
        bool noDecisionNeeded = false,
        string noDecisionReason = "")
    {
        if (decisionText == null)
        {
            return;
        }

        if (noDecisionNeeded)
        {
            decisionText.text = string.IsNullOrWhiteSpace(noDecisionReason)
                ? "No Decision needed."
                : $"No Decision needed. {noDecisionReason}.";
            ClearInspectorState();

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
            decisionText.text = "No Decision needed.";
            ClearInspectorState();
            lastLoggedFingerprint = string.Empty;
            return;
        }

        AIManager.RoomPersona persona = AIManager.ResolveRoomPersona(settings, snapshot.expectedTeam);
        string availableActions = BuildAvailableActionsSummary(snapshot, context);
        string selectedAction = BuildSelectedActionSummary(snapshot, context, persona, availableActions);

        decisionText.text = BuildDecisionDisplayText(persona, selectedAction);
        UpdateInspectorState(snapshot, context, persona, availableActions, selectedAction);

        string fingerprint = BuildFingerprint(snapshot, persona, availableActions);
        if (fingerprint != lastLoggedFingerprint)
        {
            int candidateCount = context != null ? context.actions.Count : 0;
            Debug.Log($"Room decision needed: manager={snapshot.manager}, team={snapshot.expectedTeam}, persona={persona}, candidateCount={candidateCount}");
            lastLoggedFingerprint = fingerprint;
        }
    }

    private static string BuildDecisionDisplayText(AIManager.RoomPersona persona, string selectedAction)
    {
        if (persona == AIManager.RoomPersona.AskHuman)
        {
            return "Input needed! Ask Human!";
        }

        return string.IsNullOrWhiteSpace(selectedAction)
            ? $"Input needed! Profile selected: {persona}."
            : $"Input needed! Profile selected: {persona}. {selectedAction}";
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
        string availableActions)
    {
        if (persona == AIManager.RoomPersona.AskHuman)
        {
            lastRandomExecutedDecisionFingerprint = string.Empty;
            lastRandomSelectedAction = string.Empty;
            ClearRandomCommittedBranch();
            ClearRandomExecutionLock();
            return "Ask Human!";
        }

        if (persona == AIManager.RoomPersona.Random)
        {
            List<RoomActionCandidate> candidates = BuildRandomExecutableCandidates(snapshot, context);
            if (candidates.Count == 0)
            {
                return "Random persona found no executable key, token, or hex candidates yet.";
            }

            string fingerprint = BuildFingerprint(snapshot, persona, availableActions);
            if (fingerprint != lastRandomExecutedDecisionFingerprint)
            {
                int selectedIndex = Random.Range(0, candidates.Count);
                RoomActionCandidate selected = candidates[selectedIndex];
                lastRandomSelectedAction = FormatActionCandidate(selected);
                lastRandomExecutedDecisionFingerprint = fingerprint;
                Debug.Log($"Room Random selected {selectedIndex + 1}/{candidates.Count} {BuildCandidatePoolKind(candidates)} candidate(s): {lastRandomSelectedAction}");
                ExecuteRandomCandidate(selected);
            }

            return $"Random executed: {lastRandomSelectedAction}";
        }

        lastRandomExecutedDecisionFingerprint = string.Empty;
        lastRandomSelectedAction = string.Empty;
        ClearRandomCommittedBranch();
        ClearRandomExecutionLock();
        return "No automated room action implemented yet.";
    }

    private List<RoomActionCandidate> BuildRandomExecutableCandidates(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context)
    {
        List<RoomActionCandidate> committedBranchCandidates = BuildCommittedRandomBranchCandidates(context);
        if (committedBranchCandidates.Count > 0)
        {
            return RemoveForfeitCandidates(committedBranchCandidates);
        }

        List<RoomActionCandidate> lockedExecutionCandidates = BuildLockedRandomExecutionCandidates(context);
        if (lockedExecutionCandidates.Count > 0)
        {
            return RemoveForfeitCandidates(lockedExecutionCandidates);
        }

        if (HasRandomExecutionLock())
        {
            return new List<RoomActionCandidate>();
        }

        List<RoomActionCandidate> kickoffCandidates = BuildKickoffRandomCandidates(context);
        if (kickoffCandidates.Count > 0)
        {
            return kickoffCandidates;
        }

        List<RoomActionCandidate> penaltyKickCandidates = BuildPenaltyKickRandomCandidates(context);
        if (penaltyKickCandidates.Count > 0)
        {
            return penaltyKickCandidates;
        }

        List<RoomActionCandidate> goalkeeperCandidates = BuildGoalkeeperRandomCandidates(context);
        if (goalkeeperCandidates.Count > 0)
        {
            return goalkeeperCandidates;
        }

        List<RoomActionCandidate> throwInCandidates = BuildThrowInRandomCandidates(context);
        if (throwInCandidates.Count > 0)
        {
            return throwInCandidates;
        }

        List<RoomActionCandidate> freeKickCandidates = BuildFreeKickRandomCandidates(context);
        if (freeKickCandidates.Count > 0 || IsFreeKickDecisionContext(snapshot, context))
        {
            return freeKickCandidates;
        }

        List<RoomActionCandidate> movementPhaseCandidates = BuildMovementPhaseRandomCandidates(context);
        if (movementPhaseCandidates.Count > 0)
        {
            return movementPhaseCandidates;
        }

        List<RoomActionCandidate> groundBallCandidates = BuildGroundBallRandomCandidates(context);
        if (groundBallCandidates.Count > 0)
        {
            return groundBallCandidates;
        }

        List<RoomActionCandidate> firstTimePassCandidates = BuildFirstTimePassRandomCandidates(context);
        if (firstTimePassCandidates.Count > 0)
        {
            return firstTimePassCandidates;
        }

        List<RoomActionCandidate> headerCandidates = BuildHeaderRandomCandidates(context);
        if (headerCandidates.Count > 0)
        {
            return headerCandidates;
        }

        List<RoomActionCandidate> highPassCandidates = BuildHighPassRandomCandidates(context);
        if (highPassCandidates.Count > 0)
        {
            return highPassCandidates;
        }

        List<RoomActionCandidate> longBallCandidates = BuildLongBallRandomCandidates(context);
        if (longBallCandidates.Count > 0)
        {
            return longBallCandidates;
        }

        List<RoomActionCandidate> keyCandidates = RemoveForfeitCandidates(BuildExecutableKeyCandidates(snapshot, context));
        if (keyCandidates.Count > 0)
        {
            return keyCandidates;
        }

        List<RoomActionCandidate> tokenCandidates = RemoveForfeitCandidates(BuildExecutableTokenCandidates(context));
        if (tokenCandidates.Count > 0)
        {
            return tokenCandidates;
        }

        return RemoveForfeitCandidates(BuildExecutableHexCandidates(context));
    }

    private static List<RoomActionCandidate> BuildKickoffRandomCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> kickoffCandidates = GetExecutableManagerCandidates(
            context,
            nameof(KickoffManager),
            excludeForfeits: true);
        if (kickoffCandidates.Count == 0)
        {
            return kickoffCandidates;
        }

        List<RoomActionCandidate> setupConfirmKeys = FilterCandidates(
            kickoffCandidates,
            action => action.actionType == RoomActionType.Kickoff
                && action.step == RoomDecisionStep.Confirm
                && !string.IsNullOrWhiteSpace(action.key));
        if (setupConfirmKeys.Count > 0)
        {
            return setupConfirmKeys;
        }

        List<RoomActionCandidate> takerTokens = FilterCandidates(
            kickoffCandidates,
            action => action.actionType == RoomActionType.Kickoff
                && action.step == RoomDecisionStep.ChooseActor
                && GetCandidateToken(action) != null);
        return takerTokens.Count > 0 ? takerTokens : new List<RoomActionCandidate>();
    }

    private static List<RoomActionCandidate> BuildPenaltyKickRandomCandidates(RoomDecisionContext context)
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

    private static List<RoomActionCandidate> BuildGoalkeeperRandomCandidates(RoomDecisionContext context)
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

    private static List<RoomActionCandidate> BuildThrowInRandomCandidates(RoomDecisionContext context)
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

    private static List<RoomActionCandidate> BuildFreeKickRandomCandidates(RoomDecisionContext context)
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
        if (setupDestinations.Count > 0 || goalkeeperRepositionSkips.Count > 0)
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

        foreach (RoomActionCandidate action in context.actions)
        {
            if (action != null && string.Equals(action.manager, nameof(FreeKickManager), System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static List<RoomActionCandidate> BuildMovementPhaseRandomCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> movementCandidates = GetExecutableManagerCandidates(
            context,
            nameof(MovementPhaseManager),
            excludeForfeits: true);
        if (movementCandidates.Count == 0)
        {
            return movementCandidates;
        }

        if (HasOnlyRootActionChoiceCandidates(movementCandidates))
        {
            return new List<RoomActionCandidate>();
        }

        List<RoomActionCandidate> snapshotKeys = FilterCandidates(
            movementCandidates,
            action => action.actionType == RoomActionType.Shot
                && action.step == RoomDecisionStep.InterruptionChoice
                && string.Equals(action.key, "S", System.StringComparison.OrdinalIgnoreCase));
        if (snapshotKeys.Count > 0)
        {
            return snapshotKeys;
        }

        List<RoomActionCandidate> rollKeys = FilterCandidates(
            movementCandidates,
            action => action.actionType == RoomActionType.Roll && !string.IsNullOrWhiteSpace(action.key));
        if (rollKeys.Count > 0)
        {
            return rollKeys;
        }

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

    private static List<RoomActionCandidate> BuildGroundBallRandomCandidates(RoomDecisionContext context)
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

    private static List<RoomActionCandidate> BuildFirstTimePassRandomCandidates(RoomDecisionContext context)
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

    private static List<RoomActionCandidate> BuildHeaderRandomCandidates(RoomDecisionContext context)
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

    private static List<RoomActionCandidate> BuildHighPassRandomCandidates(RoomDecisionContext context)
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

    private static List<RoomActionCandidate> BuildLongBallRandomCandidates(RoomDecisionContext context)
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

    private List<RoomActionCandidate> BuildLockedRandomExecutionCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> candidates = new List<RoomActionCandidate>();
        if (!HasRandomExecutionLock() || context == null)
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

            if (!MatchesRandomExecutionLock(action))
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
            ClearRandomExecutionLock();
        }

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
            if (candidate != null && !candidate.isForfeit)
            {
                filteredCandidates.Add(candidate);
            }
        }

        return filteredCandidates;
    }

    private List<RoomActionCandidate> BuildCommittedRandomBranchCandidates(RoomDecisionContext context)
    {
        List<RoomActionCandidate> candidates = new List<RoomActionCandidate>();
        if (!HasRandomCommittedBranch() || context == null)
        {
            return candidates;
        }

        bool hasBranchCandidates = false;
        foreach (RoomActionCandidate action in context.actions)
        {
            if (!MatchesRandomCommittedBranch(action))
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
            ClearRandomCommittedBranch();
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

        if (snapshot != null && snapshot.expectedKeys != null)
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
            && GetCandidateToken(action) != null;
    }

    private static bool IsExecutableHexCandidate(RoomActionCandidate action)
    {
        return action != null
            && action.isExecutableNow
            && string.IsNullOrWhiteSpace(action.key)
            && GetCandidateToken(action) == null
            && action.targetHex != null;
    }

    private bool IsDownstreamCommittedBranchCandidate(RoomActionCandidate action)
    {
        return action != null
            && action.isExecutableNow
            && string.IsNullOrWhiteSpace(action.key)
            && GetCandidateToken(action) == null
            && action.step != RoomDecisionStep.ChooseActor
            && action.step != RoomDecisionStep.Unknown;
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

    private void ExecuteRandomCandidate(RoomActionCandidate selected)
    {
        if (selected == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(selected.key))
        {
            ClearRandomCommittedBranch();
            ExecuteRandomKeyCandidate(selected);
            return;
        }

        PlayerToken token = GetCandidateToken(selected);
        if (token != null)
        {
            ExecuteRandomTokenCandidate(selected, token);
            return;
        }

        if (selected.targetHex != null)
        {
            ExecuteRandomHexCandidate(selected);
        }
    }

    private void ExecuteRandomKeyCandidate(RoomActionCandidate selected)
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
            Debug.LogWarning($"Room Random could not execute key [{selected.key}] because GameInputManager was not found.");
            return;
        }

        gameInputManager = inputManager;
        if (inputManager.TryExecuteSyntheticKeyPress(selected.key, "Room Random", out string error))
        {
            if (ShouldLockRandomExecution(selected))
            {
                CommitRandomExecutionLock(selected);
            }
        }
        else
        {
            Debug.LogWarning($"Room Random could not execute key [{selected.key}]: {error}");
        }
    }

    private void ExecuteRandomTokenCandidate(RoomActionCandidate selected, PlayerToken token)
    {
        GameInputManager inputManager = gameInputManager != null
            ? gameInputManager
            : FindAnyObjectByType<GameInputManager>();
        if (inputManager == null)
        {
            Debug.LogWarning($"Room Random could not execute token [{FormatTokenName(token)}] because GameInputManager was not found.");
            return;
        }

        gameInputManager = inputManager;
        if (inputManager.TryExecuteSyntheticTokenClick(token, "Room Random", out string error))
        {
            CommitRandomBranch(selected, token);
            if (ShouldLockRandomExecution(selected))
            {
                CommitRandomExecutionLock(selected);
            }
        }
        else
        {
            Debug.LogWarning($"Room Random could not execute token [{FormatActionCandidate(selected)}]: {error}");
        }
    }

    private void ExecuteRandomHexCandidate(RoomActionCandidate selected)
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
            Debug.LogWarning($"Room Random could not execute hex [{selected.targetHex.coordinates}] because GameInputManager was not found.");
            return;
        }

        gameInputManager = inputManager;
        if (inputManager.TryExecuteSyntheticHexClick(selected.targetHex, "Room Random", out string error))
        {
            ClearRandomCommittedBranch();
        }
        else
        {
            Debug.LogWarning($"Room Random could not execute hex [{FormatActionCandidate(selected)}]: {error}");
        }
    }

    private bool HasRandomCommittedBranch()
    {
        return !string.IsNullOrWhiteSpace(randomCommittedManager)
            && randomCommittedActionType != RoomActionType.Unknown;
    }

    private bool HasRandomExecutionLock()
    {
        return randomExecutionLockActionType != RoomActionType.Unknown;
    }

    private bool MatchesRandomCommittedBranch(RoomActionCandidate action)
    {
        return action != null
            && string.Equals(action.manager, randomCommittedManager, System.StringComparison.Ordinal)
            && action.actionType == randomCommittedActionType;
    }

    private bool MatchesRandomExecutionLock(RoomActionCandidate action)
    {
        return action != null
            && action.actionType == randomExecutionLockActionType;
    }

    private void CommitRandomBranch(RoomActionCandidate selected, PlayerToken actor)
    {
        if (selected == null || actor == null)
        {
            ClearRandomCommittedBranch();
            return;
        }

        randomCommittedManager = selected.manager;
        randomCommittedActionType = selected.actionType;
        lastRandomCommittedActor = actor;
        lastRandomCommittedBranch = $"{randomCommittedManager}/{randomCommittedActionType}/{FormatTokenName(actor)}";
    }

    private void CommitRandomExecutionLock(RoomActionCandidate selected)
    {
        if (selected == null || selected.actionType == RoomActionType.Unknown)
        {
            ClearRandomExecutionLock();
            return;
        }

        randomExecutionLockActionType = selected.actionType;
        lastRandomExecutionLock = randomExecutionLockActionType.ToString();
    }

    private void ClearRandomCommittedBranch()
    {
        randomCommittedManager = string.Empty;
        randomCommittedActionType = RoomActionType.Unknown;
        lastRandomCommittedActor = null;
        lastRandomCommittedBranch = string.Empty;
    }

    private void ClearRandomExecutionLock()
    {
        randomExecutionLockActionType = RoomActionType.Unknown;
        lastRandomExecutionLock = string.Empty;
    }

    private static bool ShouldLockRandomExecution(RoomActionCandidate selected)
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
            or RoomActionType.GoalkeeperSave
            or RoomActionType.FinalThird;
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

    private void UpdateInspectorState(
        GameplayInstructionSnapshot snapshot,
        RoomDecisionContext context,
        AIManager.RoomPersona persona,
        string availableActions,
        string selectedAction)
    {
        lastManager = snapshot.manager;
        lastExpectedTeam = snapshot.expectedTeam;
        lastPersona = persona.ToString();
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
    }

    private void ClearInspectorState()
    {
        lastManager = string.Empty;
        lastExpectedTeam = string.Empty;
        lastPersona = string.Empty;
        lastAvailableActions = string.Empty;
        lastSelectedAction = string.Empty;
        lastActionCandidates.Clear();
        lastValidKeys.Clear();
        lastActionSummaries.Clear();
        lastValidTokens.Clear();
        lastValidHexes.Clear();
        lastRandomExecutedDecisionFingerprint = string.Empty;
        lastRandomSelectedAction = string.Empty;
        ClearRandomCommittedBranch();
        ClearRandomExecutionLock();
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
