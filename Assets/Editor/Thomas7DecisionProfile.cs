using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Cooperative, isolated Edit Mode profile of the recorded kickoff
/// pass board. This measures candidate preparation and provider work, not the
/// live prompt-to-input interval. It never dispatches gameplay input.</summary>
public static class Thomas7DecisionProfile
{
    private static ReportedMovementReplay replay;
    private static RoomAIDecisionSnapshot snapshot;
    private static IEnumerator work;
    private static readonly Stopwatch wall = new();
    private static RoomAIProviderDecision decision;
    private static double preparationEnd;
    private static double longestUpdate;
    private static double longestStep;
    private static string longestStepDescription;
    private static double longestPreparationStep;
    private static double longestProviderStep;
    private static string longestProviderDescription;
    private static double activeWorkMilliseconds;
    private static double setupMilliseconds;
    private static int updates;
    private static int stage;
    private static bool aerialMode;
    private static RoomAIActionSequencePreview aerialResult;
    public static string Status { get; private set; } = "idle";

    public static string Start()
    {
        if (replay != null) return "A profile is already running.";
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            return "The profile requires stopped Edit Mode.";
        try
        {
            aerialMode = false;
            wall.Restart();
            replay = new ReportedMovementReplay("Thomas7_UH7_KickoffLatency", 281, false);
            var match = replay.match;
            match.currentState = MatchManager.GameState.StandardPass;
            var ground = match.groundBallManager;
            ground.isActivated = true;
            ground.isAwaitingTargetSelection = true;
            ground.isKickoffPass = true;
            match.movementPhaseManager.isActivated = false;
            snapshot = replay.BoardSnapshot();
            snapshot.currentState = "StandardPass";
            snapshot.manager = nameof(GroundBallManager);
            snapshot.expectedTeam = snapshot.teamInAttack;
            snapshot.splitProviderProfileStages = true;
            var context = new RoomDecisionContext { manager = snapshot.manager, expectedTeam = snapshot.expectedTeam, expectedInput = "hex" };
            ground.PopulateRoomDecisionContext(context);
            snapshot.legalActions.AddRange(context.actions.Where(candidate => candidate.isExecutableNow));
            if (snapshot.legalActions.Count == 0) throw new InvalidOperationException("The reconstructed board exposed no executable pass targets.");
            work = RoomAIDecisionSnapshotBuilder.BuildCandidateExplanationsIncrementally(snapshot, new RoomAIHumanShadowCache());
            stage = 0; preparationEnd = 0; longestUpdate = 0; longestStep = 0; longestStepDescription = "";
            longestPreparationStep = 0; longestProviderStep = 0; longestProviderDescription = "";
            activeWorkMilliseconds = 0; updates = 0; decision = null;
            setupMilliseconds = wall.Elapsed.TotalMilliseconds;
            Status = $"running: {snapshot.legalActions.Count} candidates";
            EditorApplication.update += Tick;
            AssemblyReloadEvents.beforeAssemblyReload += Cancel;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            return Status;
        }
        catch (Exception exception)
        {
            Status = "failed: " + exception.Message;
            Cleanup();
            return Status;
        }
    }

    public static string StartAerial()
    {
        if (replay != null) return "A profile is already running.";
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            return "The profile requires stopped Edit Mode.";
        try
        {
            aerialMode = true;
            wall.Restart();
            replay = new ReportedMovementReplay("Thomas7_UH7_AerialSequence", 106, false);
            replay.match.highPassManager = replay.match.gameObject.AddComponent<HighPassManager>();
            snapshot = replay.BoardSnapshot();
            snapshot.currentState = "HighPass";
            var action = new RoomActionCandidate { manager = nameof(HighPassManager), actionType = RoomActionType.HighPass,
                step = RoomDecisionStep.ChooseTarget, targetHex = replay.Hex(new RoomHexCoordinates { x = 0, z = -8 }) };
            snapshot.legalActions.Add(action);
            aerialResult = null;
            work = EvaluateAerial(action);
            stage = 2; preparationEnd = 0; longestUpdate = 0; longestStep = 0; longestStepDescription = "";
            longestPreparationStep = 0; longestProviderStep = 0; longestProviderDescription = "";
            activeWorkMilliseconds = 0; updates = 0; decision = null;
            setupMilliseconds = wall.Elapsed.TotalMilliseconds;
            Status = "running: #106 single aerial target";
            EditorApplication.update += Tick;
            AssemblyReloadEvents.beforeAssemblyReload += Cancel;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            return Status;
        }
        catch (Exception exception)
        {
            Status = "failed: " + exception.Message;
            Cleanup();
            return Status;
        }
    }

    private static IEnumerator EvaluateAerial(RoomActionCandidate action)
    {
        yield return null;
        aerialResult = RoomAIActionSequence.Evaluate(snapshot, new RoomAICandidateExplanation { candidate = action });
    }

    public static void Cancel()
    {
        if (replay == null) return;
        Status = "cancelled";
        Cleanup();
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingEditMode) Cancel();
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Cancel(); return; }
            var update = Stopwatch.StartNew();
            bool hasMore;
            do
            {
                int before = snapshot.candidateExplanations.Count;
                long started = Stopwatch.GetTimestamp();
                hasMore = work.MoveNext();
                double elapsed = (Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency;
                if (stage == 0) longestPreparationStep = Math.Max(longestPreparationStep, elapsed);
                else if (elapsed > longestProviderStep)
                {
                    longestProviderStep = elapsed;
                    longestProviderDescription = snapshot.ProviderWorkStage ?? "unknown";
                }
                if (elapsed > longestStep)
                {
                    longestStep = elapsed;
                    int after = snapshot.candidateExplanations.Count;
                    string label = after > before ? snapshot.candidateExplanations[after - 1].candidate?.label : null;
                    longestStepDescription = stage == 0
                        ? $"preparation, explanations {before}->{after}, candidate={label ?? "other work"}"
                        : $"provider: {snapshot.ProviderWorkStage ?? "unknown"}";
                }
            } while (hasMore && update.Elapsed.TotalMilliseconds < 6d);
            updates++;
            longestUpdate = Math.Max(longestUpdate, update.Elapsed.TotalMilliseconds);
            activeWorkMilliseconds += update.Elapsed.TotalMilliseconds;
            if (hasMore) return;
            (work as IDisposable)?.Dispose();
            work = null;
            if (stage++ == 0)
            {
                preparationEnd = wall.Elapsed.TotalMilliseconds;
                work = CodexTacticianProvider.Instance.DecideIncrementally(snapshot, snapshot.legalActions,
                    null, new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard, result => decision = result);
                return;
            }
            wall.Stop();
            if (aerialMode)
            {
                Status = $"complete: #106 single aerial target, totalWallMs={wall.Elapsed.TotalMilliseconds:F1}, "
                    + $"setupMs={setupMilliseconds:F1}, activeWorkMs={activeWorkMilliseconds:F1}, "
                    + $"longestUpdateMs={longestUpdate:F1}, longestStepMs={longestStep:F1}, "
                    + $"alternatives={aerialResult?.alternativesEvaluated ?? 0}, "
                    + $"projected={aerialResult?.usesProjectedAerialFreeMoves == true}, "
                    + $"responders={string.Join(",", aerialResult?.aerialFreeMoveRespondersEvaluated ?? new System.Collections.Generic.List<string>())}";
                Cleanup();
                return;
            }
            Status = $"complete: candidates={snapshot.legalActions.Count}, preparationWallMs={preparationEnd:F1}, "
                + $"providerWallMs={wall.Elapsed.TotalMilliseconds - preparationEnd:F1}, totalWallMs={wall.Elapsed.TotalMilliseconds:F1}, "
                + $"setupMs={setupMilliseconds:F1}, activeWorkMs={activeWorkMilliseconds:F1}, "
                + $"longestUpdateMs={longestUpdate:F1}, longestStepMs={longestStep:F1} ({longestStepDescription}), "
                + $"preparationMaxStepMs={longestPreparationStep:F1}, providerMaxStepMs={longestProviderStep:F1} ({longestProviderDescription}), "
                + $"updates={updates}, selected={decision?.selectedCandidate?.label ?? "none"}";
            Cleanup();
        }
        catch (Exception exception)
        {
            Status = "failed: " + exception;
            Cleanup();
        }
    }

    private static void Cleanup()
    {
        EditorApplication.update -= Tick;
        AssemblyReloadEvents.beforeAssemblyReload -= Cancel;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        (work as IDisposable)?.Dispose();
        work = null;
        replay?.Dispose();
        replay = null;
        snapshot = null;
    }
}
