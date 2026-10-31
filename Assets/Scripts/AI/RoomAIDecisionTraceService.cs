using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Persists a readable, run-scoped companion file for Codex Tactician decisions.
/// The ordinary room save remains compact; this trace retains the leading options
/// that would otherwise only be visible briefly in the Inspector.
/// </summary>
public static class RoomAIDecisionTraceService
{
    public const int MaximumAlternativesPerDecision = 24;
    private const int MinimumCloseScoreDelta = 8;
    private const float RelativeCloseScoreDelta = 0.05f;
    public const string TraceFileSuffix = "__AI Decisions.json";
    public const string TraceIndexFileSuffix = "__AI Decisions Index.json";
    private const int TraceSegmentMinutes = 10;

    private static string activeTracePath = string.Empty;
    private static string activeSourceSavePath = string.Empty;
    private static string activeIndexPath = string.Empty;
    private static RoomAIDecisionTraceDocument activeDocument;
    private static RoomAIDecisionTraceIndex activeIndex;
    private static readonly List<RoomAIDecisionTraceEntry> recentDecisions = new();
    private static string lastAutomatedState;

    public static List<RoomAIDecisionTraceEntry> GetLatestDecisions(int count = 5)
    {
        if (!EnsureCurrentDocument()) return new();
        IEnumerable<RoomAIDecisionTraceEntry> entries = recentDecisions.Concat(activeDocument.decisions);
        var result = entries.GroupBy(entry => entry.decisionNumber).Select(group => group.Last())
            .OrderByDescending(entry => entry.decisionNumber).Take(count).ToList();
        // On reopening a match, a fresh segment can contain fewer than five.
        if (result.Count < count && activeIndex?.segmentFiles != null)
            foreach (string segment in activeIndex.segmentFiles.AsEnumerable().Reverse())
            {
                string path = Path.Combine(Path.GetDirectoryName(activeTracePath) ?? "", segment);
                if (path == activeTracePath || !File.Exists(path)) continue;
                var document = JsonConvert.DeserializeObject<RoomAIDecisionTraceDocument>(File.ReadAllText(path));
                result = result.Concat(document?.decisions ?? new()).GroupBy(entry => entry.decisionNumber)
                    .Select(group => group.Last()).OrderByDescending(entry => entry.decisionNumber).Take(count).ToList();
                if (result.Count >= count) break;
            }
        return result;
    }

    private static void RememberRecent(RoomAIDecisionTraceEntry entry)
    {
        recentDecisions.Add(entry);
        if (recentDecisions.Count > 5) recentDecisions.RemoveAt(0);
    }

    public static void RecordHumanInput(bool? home, string action, string manager, string outcome)
    {
        var match = MatchManager.Instance;
        string team = home == true ? "Home" : home == false ? "Away" : null;
        if (team == null || match == null || match.IsGameplayInputBlocked
            || AIManager.ResolveRoomPersona(match.gameData?.gameSettings, team) != AIManager.RoomPersona.AskHuman) return;
        try
        {
            if (!EnsureCurrentDocument()) return;
            var runtime = match.CreateRuntimeSnapshot();
            EnsureDocument(BuildSegmentTracePath(activeSourceSavePath, new() { clock = runtime?.clock }), activeSourceSavePath);
            var entry = new RoomAIDecisionTraceEntry
            {
                mode = "HumanInput", analysisComplete = false, decisionNumber = ReserveDecisionNumber(),
                recordedUtc = DateTime.UtcNow.ToString("o"), unityFrame = Time.frameCount, clockDisplay = MatchManager.Instance?.GetClockDisplayText().Replace("\n", " "),
                precedingGameplaySequence = match.gameData?.events?.LastOrDefault()?.sequenceNumber ?? 0,
                currentState = match.currentState.ToString(), manager = manager, expectedTeam = team,
                selectedAction = action, humanSelectedAction = action, humanInputConsumer = manager,
                humanInputOutcome = outcome, selectedReason = "Human action recorded without tactical search"
            };
            activeDocument.decisions.Add(entry);
            RememberRecent(entry);
            WriteActiveDocument(entry.recordedUtc);
        }
        catch (Exception error) { Debug.LogWarning($"Could not record human action: {error.Message}"); }
    }

    public static bool TrySaveTacticalNote(string comment, IReadOnlyList<int> displayedDecisionIds, out string message)
    {
        if (string.IsNullOrWhiteSpace(comment)) { message = "Write a note and identify the decision it concerns."; return false; }
        try
        {
            if (!EnsureCurrentDocument()) { message = "No active match save is available."; return false; }
            string path = Path.Combine(Path.GetDirectoryName(activeSourceSavePath) ?? "",
                Path.GetFileNameWithoutExtension(activeSourceSavePath) + "__Tactical Notes.json");
            var notes = File.Exists(path) ? JsonConvert.DeserializeObject<List<RoomAITacticalNote>>(File.ReadAllText(path)) : null;
            notes ??= new();
            notes.Add(new RoomAITacticalNote { recordedUtc = DateTime.UtcNow.ToString("o"),
                comment = comment.Trim(), displayedDecisionIds = displayedDecisionIds?.ToArray() ?? Array.Empty<int>(),
                currentState = MatchManager.Instance?.currentState.ToString(), sourceGameplaySave = Path.GetFileName(activeSourceSavePath) });
            File.WriteAllText(path, JsonConvert.SerializeObject(notes, Formatting.Indented));
            message = "Tactical note saved with the displayed decision numbers.";
            return true;
        }
        catch (Exception error) { message = $"Could not save tactical note: {error.Message}"; return false; }
    }

    public static string LastTracePath => activeTracePath;
    public static int LatestHumanDecisionNumber => activeDocument?.decisions?
        .LastOrDefault(entry => string.Equals(entry?.mode, "HumanShadow", StringComparison.Ordinal))?
        .decisionNumber ?? 0;

    public static string LatestHumanDecisionSummary
    {
        get
        {
            RoomAIDecisionTraceEntry entry = activeDocument?.decisions?
                .LastOrDefault(candidate => string.Equals(candidate?.mode, "HumanShadow", StringComparison.Ordinal));
            if (entry == null)
            {
                return "No human decision has been captured yet.";
            }

            string rank = !entry.analysisComplete
                ? "analysis incomplete when input arrived; no rank assigned"
                : entry.humanSelectionMatched
                ? $"rank {entry.humanSelectedRank}/{entry.evaluatedCandidateCount}, delta {entry.humanScoreDeltaFromBest}"
                : "not matched to a ranked candidate";
            return $"Decision #{entry.decisionNumber}: {entry.humanSelectedAction} ({rank})";
        }
    }

    /// <summary>
    /// Releases the previous match's retained decision graph. This matters when
    /// Enter Play Mode Options disables domain reload: static fields otherwise
    /// keep a large completed-match trace alive during the next match.
    /// </summary>
    public static void ResetSession()
    {
        recentDecisions.Clear();
        lastAutomatedState = null;
        activeTracePath = string.Empty;
        activeSourceSavePath = string.Empty;
        activeIndexPath = string.Empty;
        activeDocument = null;
        activeIndex = null;
    }

    public static void Record(RoomAIProviderDecision decision, RoomAIDecisionSnapshot snapshot)
    {
        if (decision?.selectedEvaluation == null
            || decision.rankedCandidates == null
            || decision.rankedCandidates.Count == 0
            || snapshot == null)
        {
            return;
        }

        try
        {
            ApplicationManager.EnsureInstanceExists();
            string savePath = ApplicationManager.Instance.GetLastSavedFilePath();
            if (string.IsNullOrWhiteSpace(savePath))
            {
                return;
            }

            string tracePath = BuildSegmentTracePath(savePath, snapshot);
            EnsureDocument(tracePath, savePath);

            string state = snapshot.currentState + "|" + snapshot.expectedTeam + "|" + snapshot.instruction?.instructionText
                + "|" + decision.selectedCandidate?.id + "|" + snapshot.ball?.x + "," + snapshot.ball?.z
                + "|" + string.Join(";", snapshot.tokens.Select(token => token.tokenKey + "@" + token.currentHex?.x + "," + token.currentHex?.z));
            if (state == lastAutomatedState) return;
            lastAutomatedState = state;

            int precedingGameplaySequence = MatchManager.Instance?.gameData?.events?
                .Where(gameplayEvent => gameplayEvent != null)
                .Select(gameplayEvent => gameplayEvent.sequenceNumber)
                .DefaultIfEmpty(0)
                .Max() ?? 0;
            RoomAITacticalEvaluation best = decision.rankedCandidates[0];
            List<RoomAITacticalEvaluation> retainedEvaluations = SelectCompetitiveAlternatives(
                decision.rankedCandidates);
            if (!retainedEvaluations.Contains(decision.selectedEvaluation))
            {
                retainedEvaluations.Add(decision.selectedEvaluation);
            }

            var movement = MatchManager.Instance?.movementPhaseManager;
            bool endsDribble = decision.selectedCandidate?.isForfeit == true
                && decision.selectedCandidate.manager == nameof(MovementPhaseManager)
                && decision.selectedCandidate.key == "X" && movement?.isDribblerRunning == true
                && !movement.isWaitingForGKWallDiveDecision && !movement.isWaitingForReposition
                && !movement.isWaitingForNutmegDecision && !movement.isWaitingForNutmegDecisionWithoutMoving
                && !movement.isWaitingForInterceptionDiceRoll && !movement.isWaitingForTackleRoll;

            RoomAIDecisionTraceEntry entry = new()
            {
                mode = "Automated",
                decisionNumber = ReserveDecisionNumber(),
                recordedUtc = DateTime.UtcNow.ToString("o"),
                unityFrame = Time.frameCount, clockDisplay = MatchManager.Instance?.GetClockDisplayText().Replace("\n", " "),
                precedingGameplaySequence = precedingGameplaySequence,
                currentState = snapshot.currentState,
                manager = snapshot.manager,
                expectedTeam = snapshot.expectedTeam,
                teamInAttack = snapshot.teamInAttack,
                attackHasPossession = snapshot.attackHasPossession,
                expectedOpponentCollection = snapshot.expectedOpponentCollection,
                ball = snapshot.ball,
                score = $"{snapshot.homeScore}-{snapshot.awayScore}",
                provider = decision.providerName,
                difficulty = decision.difficulty.ToString(),
                plan = decision.plan?.planType.ToString() ?? RoomAIAttackPlanType.None.ToString(),
                planRationale = decision.plan?.rationale,
                attackingOptions = decision.plan?.rosterAttackPlan,
                kickoffPlan = snapshot.kickoffPlan,
                evaluatedCandidateCount = decision.candidateCount,
                retainedAlternativeCount = retainedEvaluations.Count,
                selectedAction = decision.selectedCandidate?.label,
                dribbleStopTokenKey = endsDribble ? MatchManager.GetStableTokenKey(movement.selectedToken) : null,
                dribbleStopHex = endsDribble ? RoomHexCoordinates.FromHex(movement.selectedToken?.GetCurrentHex()) : null,
                dribblePaceForfeited = endsDribble ? movement.remainingDribblerPace : -1,
                dribbleSnapshotDeclined = endsDribble && movement.isWaitingForSnapshotDecision,
                selectedRank = decision.rankedCandidates.IndexOf(decision.selectedEvaluation) + 1,
                selectedTier = decision.selectedEvaluation.tier.ToString(),
                selectedScore = decision.selectedEvaluation.score,
                selectedReason = decision.reasonSummary,
                alternatives = retainedEvaluations
                    .Select((evaluation, index) => BuildCandidate(
                        evaluation,
                        decision.rankedCandidates.IndexOf(evaluation) + 1,
                        best.tier,
                        best.score,
                        evaluation == decision.selectedEvaluation))
                    .ToList()
            };

            activeDocument.decisions.Add(entry);
            RememberRecent(entry);
            WriteActiveDocument(entry.recordedUtc);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not persist AI decision trace: {exception.Message}");
        }
    }

    /// <summary>
    /// Persists a human choice beside the competitive leading Ultra Hard options
    /// computed before that input was made. The provider's selected candidate is
    /// the AI recommendation; humanSelected* identifies what the player chose.
    /// </summary>
    public static void RecordHumanChoice(
        RoomAIProviderDecision decision,
        RoomAIDecisionSnapshot snapshot,
        RoomActionCandidate humanCandidate,
        string inputKind,
        string inputValue,
        string consumedBy,
        string outcome)
    {
        if (decision == null
            || decision.rankedCandidates == null
            || snapshot == null)
        {
            return;
        }

        try
        {
            ApplicationManager.EnsureInstanceExists();
            string savePath = ApplicationManager.Instance.GetLastSavedFilePath();
            if (string.IsNullOrWhiteSpace(savePath))
            {
                return;
            }

            EnsureDocument(BuildSegmentTracePath(savePath, snapshot), savePath);
            int precedingGameplaySequence = MatchManager.Instance?.gameData?.events?
                .Where(gameplayEvent => gameplayEvent != null)
                .Select(gameplayEvent => gameplayEvent.sequenceNumber)
                .DefaultIfEmpty(0)
                .Max() ?? 0;
            RoomAITacticalEvaluation best = decision.rankedCandidates.FirstOrDefault();
            RoomAITacticalEvaluation humanEvaluation = decision.rankedCandidates
                .FirstOrDefault(evaluation => ReferenceEquals(evaluation?.candidate, humanCandidate));
            int humanRank = decision.analysisComplete && humanEvaluation != null
                ? decision.rankedCandidates.IndexOf(humanEvaluation) + 1
                : 0;
            List<RoomAITacticalEvaluation> retainedEvaluations = SelectCompetitiveAlternatives(
                decision.rankedCandidates);
            if (humanEvaluation != null && !retainedEvaluations.Contains(humanEvaluation))
            {
                retainedEvaluations.Add(humanEvaluation);
            }

            RoomAIDecisionTraceEntry entry = new()
            {
                mode = "HumanShadow",
                decisionNumber = ReserveDecisionNumber(),
                recordedUtc = DateTime.UtcNow.ToString("o"),
                unityFrame = Time.frameCount, clockDisplay = MatchManager.Instance?.GetClockDisplayText().Replace("\n", " "),
                precedingGameplaySequence = precedingGameplaySequence,
                currentState = snapshot.currentState,
                manager = snapshot.manager,
                expectedTeam = snapshot.expectedTeam,
                teamInAttack = snapshot.teamInAttack,
                attackHasPossession = snapshot.attackHasPossession,
                expectedOpponentCollection = snapshot.expectedOpponentCollection,
                ball = snapshot.ball,
                score = $"{snapshot.homeScore}-{snapshot.awayScore}",
                provider = decision.providerName,
                difficulty = decision.difficulty.ToString(),
                plan = decision.plan?.planType.ToString() ?? RoomAIAttackPlanType.None.ToString(),
                planRationale = decision.plan?.rationale,
                attackingOptions = decision.plan?.rosterAttackPlan,
                evaluatedCandidateCount = decision.rankedCandidates.Count,
                kickoffPlan = snapshot.kickoffPlan,
                analysisComplete = decision.analysisComplete,
                legalCandidateCount = decision.legalCandidateCount > 0
                    ? decision.legalCandidateCount : snapshot.legalActions.Count,
                retainedAlternativeCount = retainedEvaluations.Count,
                selectedAction = decision.selectedCandidate?.label,
                selectedRank = decision.rankedCandidates.IndexOf(decision.selectedEvaluation) + 1,
                selectedTier = decision.selectedEvaluation?.tier.ToString(),
                selectedScore = decision.selectedEvaluation?.score ?? 0,
                selectedReason = decision.reasonSummary,
                humanInputKind = inputKind,
                humanInputValue = inputValue,
                humanInputConsumer = consumedBy,
                humanInputOutcome = outcome,
                humanSelectionMatched = decision.analysisComplete && humanEvaluation != null,
                humanSelectedAction = humanCandidate?.label ?? inputValue,
                humanSelectedRank = humanRank,
                humanSelectedTier = humanEvaluation?.tier.ToString(),
                humanSelectedScore = humanEvaluation?.score ?? 0,
                humanTierDeltaFromBest = decision.analysisComplete && humanEvaluation != null && best != null
                    ? (int)humanEvaluation.tier - (int)best.tier
                    : 0,
                humanScoreDeltaFromBest = decision.analysisComplete && humanEvaluation != null && best != null && humanEvaluation.tier == best.tier
                    ? best.score - humanEvaluation.score
                    : 0,
                alternatives = retainedEvaluations
                    .Select(evaluation => BuildCandidate(
                        evaluation,
                        decision.rankedCandidates.IndexOf(evaluation) + 1,
                        best?.tier ?? default,
                        best?.score ?? 0,
                        evaluation == decision.selectedEvaluation,
                        evaluation == humanEvaluation))
                    .ToList()
            };

            activeDocument.decisions.Add(entry);
            WriteActiveDocument(entry.recordedUtc);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not persist Human Shadow decision: {exception.Message}");
        }
    }

    public static bool TryAttachCommentToLatestHumanDecision(string comment, out string message)
    {
        message = string.Empty;
        if (string.IsNullOrWhiteSpace(comment))
        {
            message = "Enter a short tactical note first.";
            return false;
        }

        try
        {
            if (!EnsureCurrentDocument())
            {
                message = "No active match save is available for the note.";
                return false;
            }

            RoomAIDecisionTraceEntry entry = activeDocument.decisions
                .LastOrDefault(candidate => string.Equals(candidate?.mode, "HumanShadow", StringComparison.Ordinal));
            if (entry == null)
            {
                message = "Make one human gameplay decision before adding a note.";
                return false;
            }

            entry.humanComment = comment.Trim();
            entry.humanCommentedUtc = DateTime.UtcNow.ToString("o");
            WriteActiveDocument(entry.humanCommentedUtc);
            message = $"Saved note for decision #{entry.decisionNumber}.";
            return true;
        }
        catch (Exception exception)
        {
            message = $"Could not save tactical note: {exception.Message}";
            Debug.LogWarning(message);
            return false;
        }
    }

    private static List<RoomAITacticalEvaluation> SelectCompetitiveAlternatives(
        IReadOnlyList<RoomAITacticalEvaluation> rankedCandidates)
    {
        List<RoomAITacticalEvaluation> retained = new();
        if (rankedCandidates == null || rankedCandidates.Count == 0)
        {
            return retained;
        }

        RoomAITacticalEvaluation best = rankedCandidates[0];
        retained.Add(best);
        int closeScoreDelta = Math.Max(
            MinimumCloseScoreDelta,
            (int)Math.Ceiling(Math.Abs(best.score) * RelativeCloseScoreDelta));
        for (int index = 1;
            index < rankedCandidates.Count && retained.Count < MaximumAlternativesPerDecision;
            index++)
        {
            RoomAITacticalEvaluation alternative = rankedCandidates[index];
            if (alternative == null
                || alternative.tier != best.tier
                || best.score - alternative.score > closeScoreDelta)
            {
                break;
            }

            retained.Add(alternative);
        }

        // Preserve unused moves when X wins, including options rejected by a
        // safety tier; their absence previously hid premature phase termination.
        if (best.candidate?.isForfeit == true && best.candidate.manager == nameof(MovementPhaseManager))
            foreach (var alternative in rankedCandidates.Where(item => item?.candidate != null
                && !item.candidate.isForfeit && item.candidate.actionType == RoomActionType.StartMovement).Take(3))
                if (!retained.Contains(alternative) && retained.Count < MaximumAlternativesPerDecision) retained.Add(alternative);

        return retained;
    }

    public static string BuildTracePath(string savePath)
    {
        if (string.IsNullOrWhiteSpace(savePath))
        {
            return string.Empty;
        }

        string directory = Path.GetDirectoryName(savePath) ?? string.Empty;
        string baseName = Path.GetFileNameWithoutExtension(savePath);
        return Path.Combine(directory, $"{baseName}{TraceFileSuffix}");
    }

    private static string BuildSegmentTracePath(string savePath, RoomAIDecisionSnapshot snapshot)
    {
        string directory = Path.GetDirectoryName(savePath) ?? string.Empty;
        string baseName = Path.GetFileNameWithoutExtension(savePath);
        int half = Math.Max(1, snapshot?.clock?.currentHalf ?? MatchManager.Instance?.currentHalf ?? 1);
        int minute = Math.Max(0, (int)Math.Floor((snapshot?.clock?.currentHalfRegulationSeconds ?? 0f) / 60f));
        int segmentStart = (minute / TraceSegmentMinutes) * TraceSegmentMinutes;
        int segmentEnd = segmentStart + TraceSegmentMinutes - 1;
        return Path.Combine(
            directory,
            $"{baseName}__AI Decisions__H{half}_{segmentStart:00}-{segmentEnd:00}.json");
    }

    private static string BuildIndexPath(string savePath)
    {
        string directory = Path.GetDirectoryName(savePath) ?? string.Empty;
        string baseName = Path.GetFileNameWithoutExtension(savePath);
        return Path.Combine(directory, $"{baseName}{TraceIndexFileSuffix}");
    }

    private static bool EnsureCurrentDocument()
    {
        ApplicationManager.EnsureInstanceExists();
        string savePath = ApplicationManager.Instance.GetLastSavedFilePath();
        if (string.IsNullOrWhiteSpace(savePath))
        {
            return false;
        }

        if (activeDocument != null
            && string.Equals(activeSourceSavePath, savePath, StringComparison.Ordinal))
        {
            return true;
        }

        RoomRuntimeSnapshot runtime = MatchManager.Instance?.CreateRuntimeSnapshot();
        RoomAIDecisionSnapshot snapshot = new() { clock = runtime?.clock };
        EnsureDocument(BuildSegmentTracePath(savePath, snapshot), savePath);
        return activeDocument != null;
    }

    private static void EnsureDocument(string tracePath, string savePath)
    {
        if (!string.Equals(activeSourceSavePath, savePath, StringComparison.Ordinal))
        {
            recentDecisions.Clear();
            lastAutomatedState = null;
            activeTracePath = string.Empty;
            activeDocument = null;
            activeSourceSavePath = savePath;
            activeIndexPath = BuildIndexPath(savePath);
            activeIndex = LoadIndex(activeIndexPath, savePath);
        }

        if (activeDocument != null
            && string.Equals(activeTracePath, tracePath, StringComparison.Ordinal))
        {
            return;
        }

        activeTracePath = tracePath;
        activeDocument = null;
        if (File.Exists(tracePath))
        {
            try
            {
                activeDocument = JsonConvert.DeserializeObject<RoomAIDecisionTraceDocument>(File.ReadAllText(tracePath));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not reopen AI decision trace; starting a new document: {exception.Message}");
            }
        }

        activeDocument ??= new RoomAIDecisionTraceDocument
        {
            schemaVersion = 5,
            sourceGameplaySave = Path.GetFileName(savePath),
            createdUtc = DateTime.UtcNow.ToString("o"),
            updatedUtc = DateTime.UtcNow.ToString("o")
        };
        activeDocument.schemaVersion = 5;
        activeDocument.decisions ??= new List<RoomAIDecisionTraceEntry>();

        string directory = Path.GetDirectoryName(tracePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static RoomAIDecisionTraceIndex LoadIndex(string indexPath, string savePath)
    {
        RoomAIDecisionTraceIndex index = null;
        if (File.Exists(indexPath))
        {
            try
            {
                index = JsonConvert.DeserializeObject<RoomAIDecisionTraceIndex>(File.ReadAllText(indexPath));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not reopen AI decision trace index; starting a new index: {exception.Message}");
            }
        }

        index ??= new RoomAIDecisionTraceIndex
        {
            schemaVersion = 5,
            sourceGameplaySave = Path.GetFileName(savePath),
            createdUtc = DateTime.UtcNow.ToString("o"),
            nextDecisionNumber = 1
        };
        index.schemaVersion = 5;
        index.nextDecisionNumber = Math.Max(1, index.nextDecisionNumber);
        index.segmentFiles ??= new List<string>();
        return index;
    }

    private static int ReserveDecisionNumber()
    {
        activeIndex ??= new RoomAIDecisionTraceIndex { nextDecisionNumber = 1 };
        int decisionNumber = Math.Max(1, activeIndex.nextDecisionNumber);
        activeIndex.nextDecisionNumber = decisionNumber + 1;
        return decisionNumber;
    }

    private static RoomAIDecisionTraceCandidate BuildCandidate(
        RoomAITacticalEvaluation evaluation,
        int rank,
        RoomAITacticalTier bestTier,
        int bestScore,
        bool selected,
        bool selectedByHuman = false)
    {
        RoomActionCandidate candidate = evaluation?.candidate;
        RoomAICandidateExplanation explanation = evaluation?.explanation;
        return new RoomAIDecisionTraceCandidate
        {
            rank = rank,
            selected = selected,
            selectedByHuman = selectedByHuman,
            tacticalTier = evaluation?.tier.ToString(),
            score = evaluation?.score ?? 0,
            safetyPriority = evaluation?.safetyPriority ?? 0,
            tierDeltaFromBest = evaluation != null
                ? (int)evaluation.tier - (int)bestTier
                : 0,
            scoreDeltaFromBest = evaluation != null && evaluation.tier == bestTier
                ? bestScore - evaluation.score
                : 0,
            manager = candidate?.manager,
            actionType = candidate?.actionType.ToString(),
            step = candidate?.step.ToString(),
            label = candidate?.label,
            key = candidate?.key,
            actorTokenKey = MatchManager.GetStableTokenKey(candidate?.actor),
            targetTokenKey = MatchManager.GetStableTokenKey(candidate?.targetToken),
            targetHex = RoomHexCoordinates.FromHex(candidate?.targetHex),
            isForfeit = candidate?.isForfeit ?? false,
            reasons = evaluation?.reasons != null
                ? new List<string>(evaluation.reasons)
                : new List<string>(),
            expectedGoals = ResolveExpectedGoals(explanation),
            expectedGoalsAgainst = ResolveExpectedGoalsAgainst(explanation),
            bestReachableSnapshotHex = explanation?.bestReachableSnapshotHex,
            reachableSnapshotFrontier = explanation?.reachableSnapshotExpectedGoalsFrontier,
            worstShotTarget = explanation?.snapshotDefenseWorstTargetHex
                ?? explanation?.goalkeeperPositioningWorstTargetHex,
            actorDistanceToBall = explanation?.actorDistanceToBall ?? -1,
            targetDistanceToBall = explanation?.targetDistanceToBall ?? -1,
            opponentsWithinTackleReach = explanation?.opponentsWithinTackleReach ?? 0,
            friendlySupportWithinRange = explanation?.friendlySupportWithinRange ?? 0,
            groundPassCollectorTokenKey = explanation?.groundPassCollectorTokenKey,
            groundPassContinuationTargetHex = explanation?.groundPassContinuationTargetHex,
            groundPassCollectorPickupDistance = explanation?.groundPassCollectorPickupDistance ?? -1,
            groundPassCollectorRemainingPace = explanation?.groundPassCollectorRemainingPace ?? -1,
            groundPassDefensiveChallengeCount = explanation?.groundPassDefensiveChallengeCount ?? 0,
            groundPassPrePickupChallengeCount = explanation?.groundPassPrePickupChallengeCount ?? 0,
            groundPassIsSafeTwoPhaseSpacePlan = explanation?.groundPassIsSafeTwoPhaseSpacePlan ?? false,
            groundPassCreatesSecondMovementShot = explanation?.groundPassCreatesSecondMovementShot ?? false,
            groundPassCollectorNeedsSecondMovement = explanation?.groundPassCollectorNeedsSecondMovement ?? false,
            groundPassCollectorIsPasser = explanation?.groundPassCollectorIsPasser ?? false,
            groundPassEscapesImmediateTacklePressure = explanation?.groundPassEscapesImmediateTacklePressure ?? false,
            groundPassRetentionProbability = explanation?.groundPassRetentionProbability ?? -1f,
            actionSequence = explanation?.actionSequence,
            groundPassImmediateSnapshotExpectedGoals = explanation?.groundPassImmediateSnapshotExpectedGoals ?? -1f,
            groundPassRouteExpectedGoals = explanation?.groundPassRouteExpectedGoals ?? -1f,
            groundPassCreatesReachableShot = explanation?.groundPassCreatesReachableShot ?? false,
            isFirstTimePassShotSetup = explanation?.isFirstTimePassShotSetup ?? false,
            firstTimePassExtraPaceUnlocksShot = explanation?.firstTimePassExtraPaceUnlocksShot ?? false,
            firstTimePassSetupShooterTokenKey = explanation?.firstTimePassSetupShooterTokenKey,
            firstTimePassBestSetupHex = explanation?.firstTimePassBestSetupHex,
            firstTimePassReachableShotHex = explanation?.firstTimePassReachableShotHex,
            firstTimePassSetupShooterPace = explanation?.firstTimePassSetupShooterPace ?? 0,
            firstTimePassSetupShooterShooting = explanation?.firstTimePassSetupShooterShooting ?? 0,
            firstTimePassShotDistanceBefore = explanation?.firstTimePassShotDistanceBefore ?? -1,
            firstTimePassShotDistanceAfter = explanation?.firstTimePassShotDistanceAfter ?? -1,
            firstTimePassPassRetentionProbability = explanation?.firstTimePassPassRetentionProbability ?? -1f,
            firstTimePassReachShotProbability = explanation?.firstTimePassReachShotProbability ?? -1f,
            firstTimePassRawShotExpectedGoals = explanation?.firstTimePassRawShotExpectedGoals ?? -1f,
            firstTimePassCombinedExpectedGoals = explanation?.firstTimePassCombinedExpectedGoals ?? -1f,
            isSupportingPassLaneMove = explanation?.isSupportingPassLaneMove ?? false,
            isTackleDecision = explanation?.isTackleDecision ?? false,
            tackleAttempted = explanation?.tackleIsAttempted ?? false,
            tackleFromBehind = explanation?.tackleIsFromBehind ?? false,
            tackleDefenderBooked = explanation?.tackleDefenderIsBooked ?? false,
            tackleSuccessProbability = explanation?.tackleSuccessProbability ?? -1f,
            tackleFoulProbability = explanation?.tackleFoulProbability ?? -1f,
            tackleSendOffProbability = explanation?.tackleSendOffProbability ?? -1f,
            dribblerRetainsProbability = explanation?.dribblerRetainsProbability ?? -1f,
            dribblerBestRepositionHex = explanation?.dribblerBestRepositionHex,
            dribblerBestRepositionExpectedGoals = explanation?.dribblerBestRepositionExpectedGoals ?? -1f,
            dribblerBestRepositionProgress = explanation?.dribblerBestRepositionProgress ?? 0,
            isSuccessfulTackleReposition = explanation?.isSuccessfulTackleReposition ?? false,
            plannedNextAction = explanation?.plannedNextAction,
            plannedNextReceiverTokenKey = explanation?.plannedNextReceiverTokenKey,
            plannedNextActionTargetHex = explanation?.plannedNextActionTargetHex,
            plannedNextActionValue = explanation?.plannedNextActionValue ?? -1f,
            plannedNextActionExpectedGoals = explanation?.plannedNextActionExpectedGoals ?? -1f,
            plannedNextActionInterceptors = explanation?.plannedNextActionInterceptors ?? 0,
            highPassTargetEvaluationComplete = explanation?.highPassTargetEvaluationComplete ?? false,
            highPassTargetsEvaluated = explanation?.highPassTargetsEvaluated ?? 0,
            highPassBestTargetHex = explanation?.highPassBestTargetHex,
            highPassBestCollectorTokenKey = explanation?.highPassBestCollectorTokenKey,
            highPassAccuracyProbability = explanation?.highPassAccuracyProbability ?? -1f,
            highPassExpectedPossessionProbability = explanation?.highPassExpectedPossessionProbability ?? -1f,
            highPassTargetOutcomeValue = explanation?.highPassTargetOutcomeValue ?? -1f,
            highPassCollectorStartsOnTarget = explanation?.highPassCollectorStartsOnTarget ?? false,
            highPassCollectorDistance = explanation?.highPassCollectorDistance ?? -1,
            highPassDefendersContesting = explanation?.highPassDefendersContesting ?? 0,
            goalkeeperCurrentMaximumExpectedGoals = explanation?.goalkeeperPositioningCurrentMaximumExpectedGoals ?? -1f,
            goalkeeperWorstThreatTokenKey = explanation?.goalkeeperPositioningWorstThreatTokenKey,
            goalkeeperThreatOrigin = explanation?.goalkeeperPositioningThreatOrigin,
            goalkeeperThreatAction = explanation?.goalkeeperPositioningThreatAction,
            goalkeeperWallApplies = explanation?.goalkeeperPositioningWallApplies ?? false,
            goalkeeperSaveModifier = explanation?.goalkeeperPositioningGoalkeeperPenalty ?? 0,
            goalkeeperShootingPenalty = explanation?.goalkeeperPositioningShootingPenalty ?? 0,
            passRace = explanation?.passRace,
            supportsConcreteContinuation = explanation?.supportsConcreteContinuation ?? false,
            attackingContinuation = explanation?.attackingContinuation,
            attackingContinuationValue = explanation?.attackingContinuationValue ?? 0f,
            isCommittedAerialDefense = explanation?.isCommittedAerialDefense ?? false,
            committedAerialDefenseGain = explanation?.committedAerialDefenseGain ?? 0f,
            committedAerialRecoverySteps = explanation?.committedAerialRecoverySteps ?? 0,
            goalkeeperThreatCount = explanation?.goalkeeperPositioningThreatCount ?? 0,
            goalkeeperOffBallThreatCount = explanation?.goalkeeperPositioningOffBallThreatCount ?? 0,
            goalkeeperCurrentOutfieldBlockerCount = explanation?.goalkeeperPositioningCurrentOutfieldBlockerCount ?? 0,
            goalkeeperOwnGoalDistanceDelta = explanation?.goalkeeperOwnGoalDistanceDelta ?? 0,
            lateResultManagement = explanation?.isLateResultManagementDecision ?? false,
            lateResultScoreDifference = explanation?.lateResultScoreDifference ?? 0,
            lateResultActionsRemaining = explanation?.lateResultActionsRemaining ?? 0
        };
    }

    private static void WriteActiveDocument(string updatedUtc)
    {
        activeDocument.updatedUtc = updatedUtc;
        File.WriteAllText(
            activeTracePath,
            JsonConvert.SerializeObject(activeDocument, Formatting.Indented));

        if (activeIndex != null && !string.IsNullOrWhiteSpace(activeIndexPath))
        {
            string segmentFile = Path.GetFileName(activeTracePath);
            if (!activeIndex.segmentFiles.Contains(segmentFile))
            {
                activeIndex.segmentFiles.Add(segmentFile);
            }

            activeIndex.updatedUtc = updatedUtc;
            activeIndex.totalDecisionCount = Math.Max(
                activeIndex.totalDecisionCount,
                activeIndex.nextDecisionNumber - 1);
            File.WriteAllText(
                activeIndexPath,
                JsonConvert.SerializeObject(activeIndex, Formatting.Indented));
        }
    }

    private static float ResolveExpectedGoals(RoomAICandidateExplanation explanation)
    {
        if (explanation == null)
        {
            return -1f;
        }

        return explanation.firstTimePassCombinedExpectedGoals >= 0f
            ? explanation.firstTimePassCombinedExpectedGoals
            : explanation.bestReachableSnapshotExpectedGoals >= 0f
                ? explanation.bestReachableSnapshotExpectedGoals
                : explanation.expectedGoals;
    }

    private static float ResolveExpectedGoalsAgainst(RoomAICandidateExplanation explanation)
    {
        if (explanation == null)
        {
            return -1f;
        }

        if (explanation.snapshotDefenseMaximumExpectedGoals >= 0f)
        {
            return explanation.snapshotDefenseMaximumExpectedGoals;
        }

        if (explanation.goalkeeperPositioningMaximumExpectedGoals >= 0f)
        {
            return explanation.goalkeeperPositioningMaximumExpectedGoals;
        }

        return explanation.defensiveExpectedGoalsAgainst;
    }
}

[Serializable]
public sealed class RoomAIDecisionTraceDocument
{
    public int schemaVersion;
    public string sourceGameplaySave;
    public string createdUtc;
    public string updatedUtc;
    public List<RoomAIDecisionTraceEntry> decisions = new();
}

[Serializable]
public sealed class RoomAIDecisionTraceIndex
{
    public int schemaVersion = 5;
    public string sourceGameplaySave;
    public string createdUtc;
    public string updatedUtc;
    public int totalDecisionCount;
    public int nextDecisionNumber = 1;
    public List<string> segmentFiles = new();
}

[Serializable]
public sealed class RoomAIDecisionTraceEntry
{
    public bool analysisComplete = true;
    public int legalCandidateCount;
    public string mode;
    public int decisionNumber;
    public string recordedUtc;
    public string clockDisplay;
    public int unityFrame;
    public int precedingGameplaySequence;
    public string currentState;
    public string manager;
    public string expectedTeam;
    public string teamInAttack;
    public bool attackHasPossession;
    public bool expectedOpponentCollection;
    public RoomHexCoordinates ball;
    public string score;
    public string provider;
    public string difficulty;
    public string plan;
    public string planRationale;
    public RoomAIRosterAttackPlan attackingOptions;
    public RoomAIKickoffPlan kickoffPlan;
    public int evaluatedCandidateCount;
    public int retainedAlternativeCount;
    public string selectedAction;
    public string dribbleStopTokenKey;
    public RoomHexCoordinates dribbleStopHex;
    public int dribblePaceForfeited = -1;
    public bool dribbleSnapshotDeclined;
    public int selectedRank;
    public string selectedTier;
    public int selectedScore;
    public string selectedReason;
    public string humanInputKind;
    public string humanInputValue;
    public string humanInputConsumer;
    public string humanInputOutcome;
    public bool humanSelectionMatched;
    public string humanSelectedAction;
    public int humanSelectedRank;
    public string humanSelectedTier;
    public int humanSelectedScore;
    public int humanTierDeltaFromBest;
    public int humanScoreDeltaFromBest;
    public string humanComment;
    public string humanCommentedUtc;
    public List<RoomAIDecisionTraceCandidate> alternatives = new();
}

[Serializable]
public sealed class RoomAITacticalNote
{
    public string recordedUtc;
    public string sourceGameplaySave;
    public string currentState;
    public string comment;
    public int[] displayedDecisionIds;
}

[Serializable]
public sealed class RoomAIDecisionTraceCandidate
{
    public int safetyPriority;
    public bool supportsConcreteContinuation;
    public string attackingContinuation;
    public float attackingContinuationValue;
    public bool isCommittedAerialDefense;
    public float committedAerialDefenseGain;
    public int committedAerialRecoverySteps;
    public int rank;
    public bool selected;
    public bool selectedByHuman;
    public string tacticalTier;
    public int score;
    public int tierDeltaFromBest;
    public int scoreDeltaFromBest;
    public string manager;
    public string actionType;
    public string step;
    public string label;
    public string key;
    public string actorTokenKey;
    public string targetTokenKey;
    public RoomHexCoordinates targetHex;
    public bool isForfeit;
    public List<string> reasons = new();
    public float expectedGoals = -1f;
    public float expectedGoalsAgainst = -1f;
    public RoomHexCoordinates bestReachableSnapshotHex;
    public string reachableSnapshotFrontier;
    public RoomHexCoordinates worstShotTarget;
    public int actorDistanceToBall = -1;
    public int targetDistanceToBall = -1;
    public int opponentsWithinTackleReach;
    public int friendlySupportWithinRange;
    public string groundPassCollectorTokenKey;
    public RoomHexCoordinates groundPassContinuationTargetHex;
    public int groundPassCollectorPickupDistance = -1;
    public int groundPassCollectorRemainingPace = -1;
    public int groundPassDefensiveChallengeCount;
    public int groundPassPrePickupChallengeCount;
    public bool groundPassIsSafeTwoPhaseSpacePlan;
    public bool groundPassCreatesSecondMovementShot;
    public bool groundPassCollectorNeedsSecondMovement;
    public bool groundPassCollectorIsPasser;
    public bool groundPassEscapesImmediateTacklePressure;
    public float groundPassRetentionProbability = -1f;
    public RoomAIActionSequencePreview actionSequence;
    public float groundPassImmediateSnapshotExpectedGoals = -1f;
    public float groundPassRouteExpectedGoals = -1f;
    public bool groundPassCreatesReachableShot;
    public bool isFirstTimePassShotSetup;
    public bool firstTimePassExtraPaceUnlocksShot;
    public string firstTimePassSetupShooterTokenKey;
    public RoomHexCoordinates firstTimePassBestSetupHex;
    public RoomHexCoordinates firstTimePassReachableShotHex;
    public int firstTimePassSetupShooterPace;
    public int firstTimePassSetupShooterShooting;
    public int firstTimePassShotDistanceBefore = -1;
    public int firstTimePassShotDistanceAfter = -1;
    public float firstTimePassPassRetentionProbability = -1f;
    public float firstTimePassReachShotProbability = -1f;
    public float firstTimePassRawShotExpectedGoals = -1f;
    public float firstTimePassCombinedExpectedGoals = -1f;
    public bool isSupportingPassLaneMove;
    public bool isTackleDecision;
    public bool tackleAttempted;
    public bool tackleFromBehind;
    public bool tackleDefenderBooked;
    public float tackleSuccessProbability = -1f;
    public float tackleFoulProbability = -1f;
    public float tackleSendOffProbability = -1f;
    public float dribblerRetainsProbability = -1f;
    public RoomHexCoordinates dribblerBestRepositionHex;
    public float dribblerBestRepositionExpectedGoals = -1f;
    public int dribblerBestRepositionProgress;
    public bool isSuccessfulTackleReposition;
    public string plannedNextAction;
    public string plannedNextReceiverTokenKey;
    public RoomHexCoordinates plannedNextActionTargetHex;
    public float plannedNextActionValue = -1f;
    public float plannedNextActionExpectedGoals = -1f;
    public int plannedNextActionInterceptors;
    public bool highPassTargetEvaluationComplete;
    public int highPassTargetsEvaluated;
    public RoomHexCoordinates highPassBestTargetHex;
    public string highPassBestCollectorTokenKey;
    public float highPassAccuracyProbability = -1f;
    public float highPassExpectedPossessionProbability = -1f;
    public float highPassTargetOutcomeValue = -1f;
    public bool highPassCollectorStartsOnTarget;
    public int highPassCollectorDistance = -1;
    public int highPassDefendersContesting;
    public float goalkeeperCurrentMaximumExpectedGoals = -1f;
    public string goalkeeperWorstThreatTokenKey;
    public RoomHexCoordinates goalkeeperThreatOrigin;
    public string goalkeeperThreatAction;
    public bool goalkeeperWallApplies;
    public int goalkeeperSaveModifier;
    public int goalkeeperShootingPenalty;
    public RoomAIPassRacePreview passRace;
    public int goalkeeperThreatCount;
    public int goalkeeperOffBallThreatCount;
    public int goalkeeperCurrentOutfieldBlockerCount;
    public int goalkeeperOwnGoalDistanceDelta;
    public bool lateResultManagement;
    public int lateResultScoreDifference;
    public int lateResultActionsRemaining;
}
