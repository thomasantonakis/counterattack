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
    }

    /// <summary>Detached values for the first authority slice, not a Room restore/save format.</summary>
    [Serializable]
    public sealed class OnlineGroundPassState
    {
        public string matchId, decisionId, stage, owner, gameState, possessionTeam, lastTouch, pauseRequester;
        public long revision, publicationSequence, rollSequence, sharedPauseRevision;
        public bool isSharedPaused, connectionHeld;
        public bool ballOnBoard, ballMoving, attackHasPossession, passActive, awaitingTarget, awaitingRoll;
        public bool movementAvailable, firstTimePassAvailable, groundPassAvailable;
        public int ballX, ballZ, selectedX, selectedZ, interceptorX, interceptorZ;
        public bool hasSelectedTarget, hasInterceptor;
        public string[] legalActions;
        public OnlineGroundPassTokenState[] tokens;
    }

    /// <summary>
    /// Isolated authority adapter for ordinary ground passes. The caller authenticates
    /// connections before binding the protocol and calls this on Unity's main thread.
    /// No network message can name a method, synthetic key, team or rigged roll.
    /// Not installed into Room until guest startup/input and clock ownership are wired.
    /// </summary>
    public sealed class OnlineGroundPassAuthority
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
        private static string TokenId(PlayerToken token) => token == null ? null
            : (token.isHomeTeam ? "home:" : "away:") + token.jerseyNumber;

        internal OnlineGroundPassState Capture(OnlineSharedPause pause)
        { sharedPause = pause; return Capture(); }

        public OnlineGroundPassState Capture()
        {
            var state = ReadState();
            var next = JsonUtility.ToJson(state);
            if (next != fingerprint || decisionId != null && !protocol.HasPendingDecision && state.legalActions.Length > 0)
            {
                publicationSequence = checked(publicationSequence + 1);
                protocol.WithdrawDecision();
                decisionId = null;
                if (!faulted && state.legalActions.Length > 0)
                {
                    decisionId = "ground-" + checked(++decisionSequence);
                    protocol.PublishDecision(decisionId, state.owner == "Home" ? OnlineTeam.Home : OnlineTeam.Away, state.legalActions);
                }
                fingerprint = next;
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
            var ballHex = match.ball.GetCurrentHex();
            var selected = ground.currentTargetHex;
            var interceptor = ground.PendingOnlineInterceptionHex;
            var attacking = match.teamInAttack == MatchManager.TeamInAttack.Home ? "Home" : "Away";
            var state = new OnlineGroundPassState
            {
                stage = "continuation", owner = attacking, gameState = match.currentState.ToString(),
                possessionTeam = attacking, lastTouch = TokenId(match.LastTokenToTouchTheBallOnPurpose),
                rollSequence = match.TimingRollSequence,
                sharedPauseRevision = sharedPause?.Revision ?? 0,
                isSharedPaused = sharedPause?.IsPaused == true,
                pauseRequester = sharedPause?.Requester?.ToString(),
                connectionHeld = sharedPause?.ConnectionHeld == true,
                ballOnBoard = ballHex != null, ballX = ballHex == null ? 0 : ballHex.coordinates.x,
                ballZ = ballHex == null ? 0 : ballHex.coordinates.z,
                ballMoving = match.ball.isMoving || match.ball.IsCellMoveInProgress,
                attackHasPossession = match.attackHasPossession,
                passActive = ground.isActivated, awaitingTarget = ground.isAwaitingTargetSelection,
                awaitingRoll = ground.isWaitingForDiceRoll,
                hasSelectedTarget = selected != null, selectedX = selected == null ? 0 : selected.coordinates.x,
                selectedZ = selected == null ? 0 : selected.coordinates.z,
                hasInterceptor = interceptor != null, interceptorX = interceptor == null ? 0 : interceptor.coordinates.x,
                interceptorZ = interceptor == null ? 0 : interceptor.coordinates.z,
                movementAvailable = match.movementPhaseManager?.isAvailable == true,
                firstTimePassAvailable = match.firstTimePassManager?.isAvailable == true,
                groundPassAvailable = ground.isAvailable,
                tokens = match.playerTokenManager.allTokens.Where(t => t != null).Select(ReadToken)
                    .OrderBy(t => t.id, StringComparer.Ordinal).ToArray()
            };
            var actions = new List<string>();
            if (faulted) state.stage = "faulted";
            // Pause/intro need their own shared authority adapter. Never bypass them
            // by accepting a command in this initial, unwired proof slice.
            else if (match.IsGameplayInputBlocked || ground.isQuickThrow || ground.isKickoffPass) state.stage = "blocked";
            else if (state.ballMoving) state.stage = "resolving";
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
            else if (ground.isAvailable)
            {
                state.stage = "action";
                actions.Add(StartAction);
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
            };
        }
    }
}
