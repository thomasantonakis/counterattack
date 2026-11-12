using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace CounterAttack.Online
{
    internal sealed partial class EOSGroundPassExchange
    {
        private IEnumerator ManualHost(OnlineGroundPassAuthority authority, OnlineMatchProtocol protocol, OnlineSharedPause pause)
        {
            EOSProofCheck.Require(board.controls != null, "Authored manual controls required");
            board.controls.Configure(OnlineTeam.Away);
            var version = protocol.Compatibility;
            var recovery = new OnlinePeerRecovery(protocol, version, pause, peerAccount, () => RecoveryNow);
            string nonce = null;
            string guestConnection = "guest-home";
            string snapshotDigest = null;
            double nextPublication = 0;
            void Publish(OnlineGroundPassState state, string status)
            {
                Send(new OnlineGroundPassMessage { kind = "state", state = state, status = status,
                    connectionGeneration = recovery.Generation, peerNonce = nonce });
            }
            void Dispatch(string connection, OnlineGroundPassMessage command, bool local)
            {
                var reply = command.kind == "control" ? pause.Submit(connection, command.ToCommand()) : authority.Submit(connection, command.ToCommand());
                var response = new OnlineGroundPassMessage { kind = command.kind == "control" ? "control-reply" : "reply",
                    sequence = command.sequence, revision = reply.Revision, status = reply.Status.ToString(),
                    connectionGeneration = recovery.Generation, peerNonce = nonce };
                if (local) board.controls.Reply(response); else Send(response);
            }
            void Local(OnlineGroundPassMessage command) => Dispatch("host-away", command, true);
            board.controls.Requested += Local;
            try
            {
                bool ready = false, done = false;
                long sent = -1;
                var deadline = RecoveryNow + 900;
                while (!done && RecoveryNow < deadline)
                {
                    Poll();
                    recovery.Tick();
                    while (messages.Count > 0)
                    {
                        var message = messages.Dequeue();
                        if (message.kind == "hello")
                        {
                            if (message.status == "heartbeat")
                                recovery.Heartbeat("guest:" + message.peerNonce, message.connectionGeneration, message.sequence);
                            else if (message.status == "resync-ack")
                            {
                                if (message.peerNonce == nonce && message.digest == snapshotDigest
                                    && recovery.AcknowledgeSnapshot(guestConnection, message.connectionGeneration)) ready = true;
                            }
                            else if (Guid.TryParseExact(message.peerNonce, "N", out _))
                            {
                                var candidate = "guest:" + message.peerNonce;
                                if (message.peerNonce == nonce && recovery.State == OnlinePeerState.Connected
                                    || recovery.BindAuthenticated(peerAccount, candidate, version))
                                {
                                    nonce = message.peerNonce; guestConnection = candidate; ready = false;
                                    var snapshot = pause.Capture(authority); snapshotDigest = Digest(snapshot);
                                    Publish(snapshot, "resync");
                                }
                            }
                            continue;
                        }
                        // No queued command or finish from a retired lifetime may mutate
                        // state, even when EOS still authenticates the same product user.
                        if (message.peerNonce != nonce || message.connectionGeneration != recovery.Generation || !recovery.CanSubmit) continue;
                        EOSProofCheck.Require(ready, "Manual handshake before commands");
                        if (message.kind == "command" || message.kind == "control") Dispatch(guestConnection, message, false);
                        else if (message.kind == "done")
                        {
                            var final = pause.Capture(authority);
                            EOSProofCheck.Require(CompletedManualAction(final) && message.digest == Digest(final), "Manual outcome and next decision agree");
                            Retain(final, "host-manual", recovery.Generation); Send(new OnlineGroundPassMessage { kind = "finish", digest = message.digest,
                                peerNonce = nonce, connectionGeneration = recovery.Generation }); done = true;
                        }
                        else throw new EOSProofFailure("Unexpected manual host message");
                    }
                    var state = pause.Capture(authority);
                    board.Display(state, "Authority Away | enter target X/Z; Home chooses the pass");
                    if (nonce != null && !done && (state.publicationSequence != sent || RecoveryNow >= nextPublication))
                    {
                        Publish(state, recovery.State == OnlinePeerState.TransportLost ? "rejoin-required" : ready ? null : "resync");
                        if (!ready && recovery.State == OnlinePeerState.Connected) snapshotDigest = Digest(state);
                        sent = state.publicationSequence; nextPublication = RecoveryNow + 1;
                    }
                    yield return null;
                }
                EOSProofCheck.Require(done, "Manual host action timed out");
            }
            finally { board.controls.Requested -= Local; board.controls.SetConnected(false); }
        }

        private IEnumerator ManualGuest(OnlineGroundPassReplica replica)
        {
            EOSProofCheck.Require(board.controls != null && !board.authorityRoot.activeSelf, "Authored inert guest manual controls");
            board.controls.Configure(OnlineTeam.Home); board.controls.SetConnected(false);
            string nonce = Guid.NewGuid().ToString("N");
            long generation = 0, heartbeat = 0;
            double lastHost = RecoveryNow, nextHeartbeat = 0;
            void SendCurrent(OnlineGroundPassMessage message)
            { message.peerNonce = nonce; message.connectionGeneration = generation; Send(message); }
            void Local(OnlineGroundPassMessage command) => SendCurrent(command);
            board.controls.Requested += Local;
            try
            {
                SendCurrent(new OnlineGroundPassMessage { kind = "hello" });
                bool doneSent = false, finished = false;
                var deadline = RecoveryNow + 900;
                while (!finished && RecoveryNow < deadline)
                {
                    Poll();
                    while (messages.Count > 0)
                    {
                        var message = messages.Dequeue();
                        if (message.peerNonce != nonce || message.connectionGeneration < generation) continue;
                        lastHost = RecoveryNow;
                        if (message.status == "rejoin-required")
                        {
                            board.controls.SetConnected(false); nonce = Guid.NewGuid().ToString("N"); generation = 0; heartbeat = 0; doneSent = false;
                            SendCurrent(new OnlineGroundPassMessage { kind = "hello" }); continue;
                        }
                        generation = message.connectionGeneration;
                        if (message.kind == "state")
                        {
                            EOSProofCheck.Require(replica.Apply(message.state), "Manual guest accepts ordered state");
                            board.Display(replica.State, "Replica Home | Start, enter X/Z, preview then confirm");
                            if (message.status == "resync")
                                SendCurrent(new OnlineGroundPassMessage { kind = "hello", status = "resync-ack", digest = Digest(message.state) });
                            board.controls.SetConnected(!message.state.connectionHeld && message.status != "resync");
                        }
                        else if (message.kind == "reply" || message.kind == "control-reply") board.controls.Reply(message);
                        else if (message.kind == "finish")
                        {
                            EOSProofCheck.Require(doneSent && message.digest == Digest(replica.State), "Manual final digest confirmed");
                            Retain(replica.State, "guest-manual", generation); finished = true;
                        }
                        else throw new EOSProofFailure("Unexpected manual guest message");
                    }
                    var state = replica.State;
                    if (RecoveryNow - lastHost >= 5)
                    {
                        board.controls.SetConnected(false); nonce = Guid.NewGuid().ToString("N"); generation = 0; heartbeat = 0; doneSent = false;
                        SendCurrent(new OnlineGroundPassMessage { kind = "hello" }); lastHost = RecoveryNow;
                    }
                    if (generation > 0 && RecoveryNow >= nextHeartbeat)
                    { SendCurrent(new OnlineGroundPassMessage { kind = "hello", status = "heartbeat", sequence = ++heartbeat }); nextHeartbeat = RecoveryNow + 1; }
                    if (!doneSent && state != null && CompletedManualAction(state))
                    { SendCurrent(new OnlineGroundPassMessage { kind = "done", digest = Digest(state) }); doneSent = true; }
                    yield return null;
                }
                EOSProofCheck.Require(finished, "Manual guest action timed out");
            }
            finally { board.controls.Requested -= Local; board.controls.SetConnected(false); }
        }

        private static bool CompletedManualAction(OnlineGroundPassState state) => !state.passActive && !state.ballMoving
            && !state.isSharedPaused && !state.connectionHeld && state.stage == "continuation"
            && state.tokens.Any(token => token.passesAttempted == 1);
    }
}
