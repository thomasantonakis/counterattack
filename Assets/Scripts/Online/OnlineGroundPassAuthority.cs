using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CounterAttack.Online
{
    [Serializable]
    public sealed class OnlineGroundPassTokenState
    {
        public string id;
        public int x, z;
        public bool onBoard;
        public int passesAttempted, passesCompleted, interceptionsAttempted, interceptionsMade, possessionWon, possessionLost;
        public int aerialPassesAttempted, aerialPassesTargeted, aerialPassesCompleted;
    }

    /// <summary>Detached values for the first authority slice, not a Room restore/save format.</summary>
    [Serializable]
    public sealed class OnlineGroundPassState
    {
        public string matchId, decisionId, stage, owner, gameState, possessionTeam, lastTouch, pauseRequester, catalogId;
        public long revision, publicationSequence, rollSequence, sharedPauseRevision;
        public long homeAcceptedSequence, awayAcceptedSequence, homePauseSequence, awayPauseSequence;
        public bool isSharedPaused, connectionHeld;
        public bool ballOnBoard, ballMoving, attackHasPossession, passActive, awaitingTarget, awaitingRoll;
        public bool movementAvailable, firstTimePassAvailable, groundPassAvailable;
        public bool movementActive, playerMoving;
        public string selectedPlayer;
        public int ballX, ballZ, selectedX, selectedZ, interceptorX, interceptorZ;
        public bool hasSelectedTarget, hasInterceptor;
        public string[] legalActions;
        public OnlineGroundPassTokenState[] tokens;
        public DecisionTimingSnapshot timing;
        public float regulationSeconds;
        public int half;
        public bool clockRunning;
        public MatchManager.TeamStats homeStats, awayStats;
        public OnlineRollPublication[] rolls;
        public OnlineHeaderObservation header;
    }

    /// <summary>
    /// Isolated authority adapter for ordinary ground passes. The caller authenticates
    /// connections before binding the protocol and calls this on Unity's main thread.
    /// No network message can name a method, synthetic key, team or rigged roll.
    /// Not installed into Room until guest startup/input and clock ownership are wired.
    /// </summary>
    public sealed partial class OnlineGroundPassAuthority
    {
        public const string StartAction = "ground.start";
        public const string RollAction = "ground.roll";
        private readonly MatchManager match;
        private readonly GroundBallManager ground;
        private readonly OnlineMatchProtocol protocol;
        private readonly Dictionary<string, HexCell> targetActions = new(StringComparer.Ordinal);
        private string fingerprint, decisionId;
        private long decisionSequence, publicationSequence;
        private bool executing, faulted;
        private OnlineSharedPause sharedPause;
        private string boundaryFingerprint;

        [Serializable] private sealed class Boundary
        {
            public string stage, owner, gameState, possession, lastTouch;
            public bool ballOnBoard, ballMoving, selected, interceptor, paused, connectionHeld;
            public bool movementActive, playerMoving;
            public string selectedPlayer;
            public int ballX, ballZ, selectedX, selectedZ, interceptorX, interceptorZ;
            public long pauseRevision;
            public string[] actions;
            public OnlineGroundPassTokenState[] tokens;
            public OnlineHeaderObservation header;
        }

        public OnlineGroundPassAuthority(MatchManager match, OnlineMatchProtocol protocol)
        {
            this.match = match != null ? match : throw new ArgumentNullException(nameof(match));
            this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            ground = match.groundBallManager != null ? match.groundBallManager
                : throw new ArgumentException("Ground pass manager is required", nameof(match));
            if (match.hexGrid == null || match.ball == null || match.playerTokenManager == null)
                throw new ArgumentException("A fully wired authority board is required", nameof(match));
        }

        public static string TargetAction(int x, int z) => "ground.target:" + x + ":" + z;
        private static string TokenId(PlayerToken token) => OnlineRollPublication.ActorId(token);

        internal OnlineGroundPassState Capture(OnlineSharedPause pause)
        { sharedPause = pause; return Capture(); }

        public OnlineGroundPassState Capture()
        {
            var state = ReadState();
            var next = JsonUtility.ToJson(state);
            var boundary = JsonUtility.ToJson(new Boundary { stage=state.stage, owner=state.owner, gameState=state.gameState,
                possession=state.possessionTeam, lastTouch=state.lastTouch, ballOnBoard=state.ballOnBoard, ballMoving=state.ballMoving,
                ballX=state.ballX, ballZ=state.ballZ, selected=state.hasSelectedTarget, selectedX=state.selectedX, selectedZ=state.selectedZ,
                interceptor=state.hasInterceptor, interceptorX=state.interceptorX, interceptorZ=state.interceptorZ,
                paused=state.isSharedPaused, pauseRevision=state.sharedPauseRevision, connectionHeld=state.connectionHeld,
                movementActive=state.movementActive,playerMoving=state.playerMoving,selectedPlayer=state.selectedPlayer,
                actions=state.legalActions, tokens=state.tokens, header=state.header });
            bool boundaryChanged = boundary != boundaryFingerprint
                || decisionId != null && !protocol.HasPendingDecision && state.legalActions.Length > 0;
            if (next != fingerprint || boundaryChanged)
            {
                publicationSequence = checked(publicationSequence + 1);
                fingerprint = next;
            }
            // Clock/budget publications must not revoke an otherwise unchanged
            // legal command merely because it spent time in transit.
            if (boundaryChanged)
            {
                protocol.WithdrawDecision();
                decisionId = null;
                if (!faulted && state.legalActions.Length > 0)
                {
                    decisionId = "ground-" + checked(++decisionSequence);
                    protocol.PublishDecision(decisionId, state.owner == "Home" ? OnlineTeam.Home : OnlineTeam.Away, state.legalActions);
                }
                boundaryFingerprint = boundary;
            }
            state.matchId = protocol.MatchId;
            state.decisionId = decisionId;
            state.revision = protocol.Revision;
            // Coroutine outcomes can change with no pending command decision.
            // Order publications independently of command admission revisions.
            state.publicationSequence = publicationSequence;
            return state;
        }

        public OnlineCommandReply Submit(string authenticatedConnection, OnlineMatchCommand command)
        {
            if (executing || faulted) return new OnlineCommandReply(OnlineCommandStatus.NotReady, protocol.Revision);
            Capture(); // Revalidate live target legality and pending roll before admission.
            var reply = protocol.Admit(authenticatedConnection, command);
            if (reply.Status != OnlineCommandStatus.Accepted) return reply;
            executing = true;
            try
            {
                if (command.ActionId == StartAction) match.TriggerStandardPass();
                else if (command.ActionId == RollAction) ground.PerformGroundInterceptionDiceRoll();
                else if (additionalActions.TryGetValue(command.ActionId,out var execute)) execute();
                else ground.HandleGroundBallPath(targetActions[command.ActionId]);
            }
            catch
            {
                // Accepted commands must never be retried after partial mutation.
                faulted = true;
                throw;
            }
            finally
            {
                executing = false;
                fingerprint = null;
                Capture();
            }
            return reply;
        }

        private OnlineGroundPassState ReadState()
        {
            targetActions.Clear();
            additionalActions.Clear();
            var ballHex = match.ball.GetCurrentHex();
            var selected = match.highPassManager?.isActivated == true ? match.highPassManager.currentTargetHex
                : match.longBallManager?.isActivated == true ? match.longBallManager.currentTargetHex : ground.currentTargetHex;
            var interceptor = ground.PendingOnlineInterceptionHex;
            var attacking = match.teamInAttack == MatchManager.TeamInAttack.Home ? "Home" : "Away";
            var state = new OnlineGroundPassState
            {
                catalogId = protocol.Compatibility.Catalog,
                rolls = match.CaptureOnlineRolls(),
                header = CaptureHeaderObservation(),
                homeAcceptedSequence = protocol.LastAcceptedSequence(OnlineTeam.Home),
                awayAcceptedSequence = protocol.LastAcceptedSequence(OnlineTeam.Away),
                homePauseSequence = sharedPause?.LastAcceptedSequence(OnlineTeam.Home) ?? 0,
                awayPauseSequence = sharedPause?.LastAcceptedSequence(OnlineTeam.Away) ?? 0,
                timing = match.CaptureOnlineDecisionTiming(), regulationSeconds = match.OnlineRegulationSeconds,
                half = match.currentHalf, clockRunning = match.isClockRunning,
                homeStats = JsonUtility.FromJson<MatchManager.TeamStats>(JsonUtility.ToJson(match.gameData.stats.homeTeamStats)),
                awayStats = JsonUtility.FromJson<MatchManager.TeamStats>(JsonUtility.ToJson(match.gameData.stats.awayTeamStats)),
                stage = "continuation", owner = attacking, gameState = match.currentState.ToString(),
                possessionTeam = attacking, lastTouch = TokenId(match.LastTokenToTouchTheBallOnPurpose),
                rollSequence = match.TimingRollSequence,
                sharedPauseRevision = sharedPause?.Revision ?? 0,
                isSharedPaused = sharedPause?.IsPaused == true,
                pauseRequester = sharedPause?.Requester?.ToString(),
                connectionHeld = sharedPause?.ConnectionHeld == true,
                ballOnBoard = ballHex != null, ballX = ballHex == null ? 0 : ballHex.coordinates.x,
                ballZ = ballHex == null ? 0 : ballHex.coordinates.z,
                ballMoving = match.ball.isMoving || match.ball.IsCellMoveInProgress || match.longBallManager?.OnlineFlightInProgress == true || match.highPassManager?.OnlineFlightInProgress==true,
                attackHasPossession = match.attackHasPossession,
                passActive = ground.isActivated || match.longBallManager?.isActivated == true || match.highPassManager?.isActivated==true
                    || HeaderRules?.isActivated==true || match.looseBallManager?.isActivated==true || match.shotManager?.isActivated==true, awaitingTarget = ground.isAwaitingTargetSelection,
                awaitingRoll = ground.isWaitingForDiceRoll,
                hasSelectedTarget = selected != null, selectedX = selected == null ? 0 : selected.coordinates.x,
                selectedZ = selected == null ? 0 : selected.coordinates.z,
                hasInterceptor = interceptor != null, interceptorX = interceptor == null ? 0 : interceptor.coordinates.x,
                interceptorZ = interceptor == null ? 0 : interceptor.coordinates.z,
                movementAvailable = match.movementPhaseManager?.isAvailable == true,
                movementActive = match.movementPhaseManager?.isActivated == true,
                playerMoving = match.movementPhaseManager?.isPlayerMoving == true
                    || match.goalFlowManager?.isActivated == true || match.kickoffManager?.IsWaitingForMovementToComplete() == true,
                selectedPlayer = TokenId(match.movementPhaseManager?.selectedToken),
                firstTimePassAvailable = match.firstTimePassManager?.isAvailable == true,
                groundPassAvailable = ground.isAvailable,
                tokens = match.playerTokenManager.allTokens.Where(t => t != null).Select(ReadToken)
                    .OrderBy(t => t.id, StringComparer.Ordinal).ToArray()
            };
            var actions = new List<string>();
            if (faulted) state.stage = "faulted";
            // Pause/intro need their own shared authority adapter. Never bypass them
            // by accepting a command in this initial, unwired proof slice.
            else if (match.IsGameplayInputBlocked) state.stage = "blocked";
            else if (match.goalFlowManager?.isActivated == true)
            { state.stage="goal.resolving";state.owner="None";state.awaitingRoll=false;state.awaitingTarget=false; }
            else if (match.kickoffManager?.OnlineActive == true) ReadKickoffDecision(state,actions);
            else if (match.longBallManager != null && match.longBallManager.isActivated)
                ReadLongPassDecision(state, actions, attacking);
            else if(match.highPassManager?.isActivated==true) ReadHighPassDecision(state,actions);
            else if (state.ballMoving) state.stage = "resolving";
            else if(match.looseBallManager?.isActivated==true)ReadLooseBallDecision(state,actions);
            else if(match.shotManager?.isWaitingForSaveandHoldScenario==true)ReadKeeperDistribution(state,actions);
            else if (match.finalThirdManager?.isActivated == true) ReadFinalThirdDecision(state,actions);
            else if(match.shotManager?.isActivated==true)ReadShotDecision(state,actions);
            else if(match.shotManager?.isWaitingForShotCommitConfirmation==true)ReadShotDecision(state,actions);
            else if(HeaderRules?.isActivated==true) ReadHeaderDecision(state,actions,HeaderRules);
            else if (state.movementActive) ReadMovementDecision(state,actions);
            else if (ground.isActivated && ground.isWaitingForDiceRoll && interceptor != null)
            {
                state.stage = "interception";
                state.owner = attacking == "Home" ? "Away" : "Home";
                actions.Add(RollAction);
            }
            else if (ground.isActivated && ground.isAwaitingTargetSelection)
            {
                state.stage = "target";
                foreach (var hex in ground.GetLegalGroundPassTargetsForDecisionPreview())
                {
                    var id = TargetAction(hex.coordinates.x, hex.coordinates.z);
                    targetActions.Add(id, hex);
                    actions.Add(id);
                }
            }
            else if (ground.isActivated) state.stage = "resolving";
            else if (ground.isAvailable || match.movementPhaseManager?.isAvailable==true || match.longBallManager?.isAvailable==true
                || match.shotManager?.OnlineCanSelectShot==true
                || match.highPassManager?.isAvailable==true && match.currentState!=MatchManager.GameState.GoalKick)
            {
                state.stage = "action";
                if(ground.isAvailable && match.currentState==MatchManager.GameState.QuickThrow)
                    AddAdditional(actions,KeeperQuickStartAction,()=>match.TriggerQuickThrowPass());
                else if(ground.isAvailable)actions.Add(StartAction);
            }
            if ((state.stage == "action" || state.stage == "target" || state.stage == "long.target" || state.stage=="high.target") && match.longBallManager?.isAvailable == true)
                AddAdditional(actions, LongStartAction, () => match.TriggerLongPass());
            if ((state.stage == "long.target" || state.stage=="high.target") && ground.isAvailable) actions.Add(StartAction);
            bool preview = state.stage == "action" || state.stage == "target" || state.stage == "long.target" || state.stage=="high.target";
            if(preview && match.shotManager?.OnlineCanSelectShot==true)AddShotCommand(actions,ShotStartAction,OnlineShotIntent.Select);
            bool canHigh=match.highPassManager?.isAvailable==true && match.currentState!=MatchManager.GameState.GoalKick;
            if(preview && canHigh)AddAdditional(actions,HighStartAction,()=>match.TriggerHighPass());
            if(preview && match.movementPhaseManager?.isAvailable == true && !state.movementActive)
                AddAdditional(actions,MovementStartAction,()=>match.TriggerMovement());
            if(state.movementActive && !match.movementPhaseManager.isCommitted && match.movementPhaseManager.OnlineBasicMovementAvailable)
            {
                if(ground.isAvailable) actions.Add(StartAction);
                if(match.longBallManager?.isAvailable == true) AddAdditional(actions,LongStartAction,()=>match.TriggerLongPass());
                if(canHigh)AddAdditional(actions,HighStartAction,()=>match.TriggerHighPass());
            }
            state.legalActions = actions.OrderBy(a => a, StringComparer.Ordinal).ToArray();
            return state;
        }

        private OnlineGroundPassTokenState ReadToken(PlayerToken token)
        {
            var hex = token.GetCurrentHex();
            // Reading a publication must not lazily create or rewrite live stats.
            var stats = match.gameData.stats.playerStats.FirstOrDefault(
                entry => entry.playerKey == MatchManager.GetStableTokenKey(token))?.stats;
            return new OnlineGroundPassTokenState
            {
                id = TokenId(token), onBoard = hex != null, x = hex == null ? 0 : hex.coordinates.x,
                z = hex == null ? 0 : hex.coordinates.z,
                passesAttempted = stats?.passesAttempted ?? 0, passesCompleted = stats?.passesCompleted ?? 0,
                interceptionsAttempted = stats?.interceptionsAttempted ?? 0, interceptionsMade = stats?.interceptionsMade ?? 0,
                possessionWon = stats?.possessionWon ?? 0, possessionLost = stats?.possessionLost ?? 0
                , aerialPassesAttempted = stats?.aerialPassesAttempted ?? 0, aerialPassesTargeted = stats?.aerialPassesTargeted ?? 0,
                aerialPassesCompleted = stats?.aerialPassesCompleted ?? 0
            };
        }
    }
}
