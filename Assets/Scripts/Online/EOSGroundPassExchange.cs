using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using UnityEngine;

namespace CounterAttack.Online
{
    // Automated independent-process gameplay probe. EOS peer identity is checked
    // on every fragment; clients never supply a claimed team or roll override.
    internal sealed class EOSGroundPassExchange
    {
        private const byte Channel = 8;
        private readonly OnlineGroundPassBoard board;
        private readonly string output;
        private readonly OnlineGroundPassWire wire = new();
        private readonly byte[] receiveBuffer = new byte[1170];
        private readonly Queue<OnlineGroundPassMessage> messages = new();
        private EOSStandaloneSession session;
        private ProductUserId remote;
        private SocketId socket;
        private string matchId;

        internal EOSGroundPassExchange(OnlineGroundPassBoard board, string output) { this.board = board; this.output = output; }

        private void SetPeer(EOSStandaloneSession session, ProductUserId remote, SocketId socket, string lobby)
        { this.session = session; this.remote = remote; this.socket = socket; matchId = "ground-" + lobby; }

        private void Send(OnlineGroundPassMessage message)
        {
            message.matchId = matchId;
            foreach (var fragment in OnlineGroundPassWire.Encode(message))
            {
                var send = new SendPacketOptions { LocalUserId = session.UserId, RemoteUserId = remote,
                    SocketId = socket, Channel = Channel, Data = new ArraySegment<byte>(fragment),
                    AllowDelayedDelivery = true, Reliability = PacketReliability.ReliableOrdered };
                EOSProofCheck.Require(session.Platform.GetP2PInterface().SendPacket(ref send) == Result.Success, "Sending gameplay fragment");
            }
        }

        private void Poll()
        {
            session.Platform.Tick();
            var options = new ReceivePacketOptions { LocalUserId = session.UserId, MaxDataSizeBytes = (uint)receiveBuffer.Length, RequestedChannel = Channel };
            for (var i = 0; i < 128; i++)
            {
                ProductUserId sender = null; var receivedSocket = new SocketId();
                var result = session.Platform.GetP2PInterface().ReceivePacket(ref options, ref sender, ref receivedSocket,
                    out var channel, new ArraySegment<byte>(receiveBuffer), out var written);
                if (result == Result.NotFound) break;
                EOSProofCheck.Require(result == Result.Success, "Receiving gameplay fragment");
                EOSProofCheck.Require(sender?.ToString() == remote.ToString() && channel == Channel
                    && receivedSocket.SocketName == socket.SocketName, "Gameplay peer/socket/channel identity");
                var message = wire.Receive(receiveBuffer, (int)written);
                if (message == null) continue;
                EOSProofCheck.Require(message.matchId == matchId, "Gameplay match identity");
                EOSProofCheck.Require(messages.Count < 64, "Gameplay receive queue bound");
                messages.Enqueue(message);
            }
        }

        private static string Digest(OnlineGroundPassState state)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(state)))).Replace("-", "");
        }

        private void Retain(OnlineGroundPassState state, string role)
        {
            File.WriteAllText(Path.Combine(output, "final-state.json"), JsonUtility.ToJson(state, true));
            File.WriteAllText(Path.Combine(output, "gameplay-evidence.json"), JsonUtility.ToJson(new Evidence
            {
                role = role, digest = Digest(state), publicationSequence = state.publicationSequence,
                rollSequence = state.rollSequence, guestRulesInactive = !board.authorityRoot.activeSelf,
                sharedPauseRevision = state.sharedPauseRevision
            }, true));
        }

        [Serializable] private sealed class Evidence
        { public string role, digest; public long publicationSequence, rollSequence, sharedPauseRevision; public bool guestRulesInactive; }

        internal IEnumerator Local()
        {
            board.InitializeAuthority();
            var version = new OnlineCompatibility("ground-wire-v2", "ground-rules-v1", "prepared-board-v1");
            var protocol = new OnlineMatchProtocol("local-ground", "home", "away", "away", version);
            protocol.BindAuthenticatedAccount("home", "guest", version); protocol.BindAuthenticatedAccount("away", "host", version);
            var authority = new OnlineGroundPassAuthority(board.match, protocol);
            using var pause = new OnlineSharedPause(protocol, board.match);
            void Submit(string connection, long sequence, string action)
            {
                var state = authority.Capture();
                var reader = new OnlineGroundPassWire(); OnlineGroundPassMessage received = null;
                foreach (var packet in OnlineGroundPassWire.Encode(OnlineGroundPassMessage.Command(state, sequence, action)))
                    received = reader.Receive(packet, packet.Length);
                EOSProofCheck.Require(authority.Submit(connection, received.ToCommand()).Status == OnlineCommandStatus.Accepted, "Local semantic gameplay command");
            }
            Submit("guest", 1, OnlineGroundPassAuthority.StartAction);
            Submit("guest", 2, OnlineGroundPassAuthority.TargetAction(0, 6));
            var request = new OnlineMatchCommand(protocol.MatchId, 1, pause.Revision, "shared-pause", OnlineSharedPause.RequestAction);
            EOSProofCheck.Require(pause.Submit("guest", request).Status == OnlineCommandStatus.Accepted, "Local guest pause");
            var paused = pause.Capture(authority); board.Display(paused, "Local shared pause check");
            EOSProofCheck.Require(paused.isSharedPaused && Time.timeScale == 0, "Local shared pause applied");
            EOSProofCheck.Require(pause.Submit("host", new OnlineMatchCommand(protocol.MatchId, 1, pause.Revision,
                "shared-pause", OnlineSharedPause.ResumeAction)).Status == OnlineCommandStatus.WrongTeam, "Local opponent cannot resume");
            for (var frame = 0; frame < 5; frame++) yield return null;
            EOSProofCheck.Require(pause.Submit("guest", new OnlineMatchCommand(protocol.MatchId, 2, pause.Revision,
                "shared-pause", OnlineSharedPause.ResumeAction)).Status == OnlineCommandStatus.Accepted, "Local requester resumes");
            Submit("guest", 3, OnlineGroundPassAuthority.TargetAction(0, 6));
            var deadline = Time.realtimeSinceStartupAsDouble + 30;
            while (!board.match.groundBallManager.isWaitingForDiceRoll && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            EOSProofCheck.Require(board.match.groundBallManager.isWaitingForDiceRoll, "Local authored interception boundary");
            Submit("host", 1, OnlineGroundPassAuthority.RollAction);
            while (board.match.groundBallManager.isActivated && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            var final = pause.Capture(authority);
            EOSProofCheck.Require(!final.passActive && final.rollSequence == 1, "Local authored gameplay outcome");
            var replica = new OnlineGroundPassReplica("local-ground");
            EOSProofCheck.Require(replica.Apply(final), "Local detached replica accepts outcome");
            board.Display(replica.State, "Local packaged gameplay check"); Retain(final, "local");
        }

        internal IEnumerator Host(EOSStandaloneSession session, ProductUserId remote, SocketId socket, string lobby)
        {
            SetPeer(session, remote, socket, lobby);
            board.InitializeAuthority();
            var version = new OnlineCompatibility("ground-wire-v2", "ground-rules-v1", "prepared-board-v1");
            var protocol = new OnlineMatchProtocol(matchId, remote.ToString(), session.UserId.ToString(), session.UserId.ToString(), version);
            EOSProofCheck.Require(protocol.BindAuthenticatedAccount(remote.ToString(), "guest-home", version), "Binding guest Home identity");
            EOSProofCheck.Require(protocol.BindAuthenticatedAccount(session.UserId.ToString(), "host-away", version), "Binding host Away identity");
            var authority = new OnlineGroundPassAuthority(board.match, protocol);
            using var pause = new OnlineSharedPause(protocol, board.match);
            var first = authority.Capture();
            EOSProofCheck.Require(authority.Submit("host-away", new OnlineMatchCommand(matchId, 1, first.revision,
                first.decisionId, OnlineGroundPassAuthority.StartAction)).Status == OnlineCommandStatus.WrongTeam,
                "Host cannot start the guest team's pass");
            bool ready = false, duplicateConfirmed = false, rejectedGuestRoll = false, rolled = false, done = false;
            bool opponentResumeRejected = false, duplicatePause = false, staleConfirmation = false;
            long sent = -1;
            var deadline = Time.realtimeSinceStartupAsDouble + 180;
            while (!done && Time.realtimeSinceStartupAsDouble < deadline)
            {
                Poll();
                while (messages.Count > 0)
                {
                    var message = messages.Dequeue();
                    if (message.kind == "hello") { ready = true; continue; }
                    EOSProofCheck.Require(ready, "Gameplay handshake before command");
                    if (message.kind == "command")
                    {
                        var reply = authority.Submit("guest-home", message.ToCommand());
                        duplicateConfirmed |= message.sequence == 3 && reply.Status == OnlineCommandStatus.Duplicate;
                        rejectedGuestRoll |= message.actionId == OnlineGroundPassAuthority.RollAction && reply.Status == OnlineCommandStatus.WrongTeam;
                        staleConfirmation |= message.sequence == 3 && reply.Status == OnlineCommandStatus.StaleRevision;
                        EOSProofCheck.Require(reply.Status == OnlineCommandStatus.Accepted || reply.Status == OnlineCommandStatus.Duplicate
                            || reply.Status == OnlineCommandStatus.WrongTeam || reply.Status == OnlineCommandStatus.StaleRevision,
                            "Unexpected gameplay command rejection: " + reply.Status);
                        Send(new OnlineGroundPassMessage { kind = "reply", sequence = message.sequence, status = reply.Status.ToString(), revision = reply.Revision });
                    }
                    else if (message.kind == "control")
                    {
                        var reply = pause.Submit("guest-home", message.ToCommand());
                        EOSProofCheck.Require(reply.Status == OnlineCommandStatus.Accepted || reply.Status == OnlineCommandStatus.Duplicate, "Shared pause control accepted");
                        duplicatePause |= message.actionId == OnlineSharedPause.RequestAction && reply.Status == OnlineCommandStatus.Duplicate;
                        if (pause.IsPaused && !opponentResumeRejected)
                        {
                            EOSProofCheck.Require(pause.Submit("host-away", new OnlineMatchCommand(matchId, 1, pause.Revision,
                                "shared-pause", OnlineSharedPause.ResumeAction)).Status == OnlineCommandStatus.WrongTeam, "Host cannot resume guest-owned pause");
                            opponentResumeRejected = true;
                        }
                        Send(new OnlineGroundPassMessage { kind = "control-reply", sequence = message.sequence, status = reply.Status.ToString(), revision = reply.Revision });
                    }
                    else if (message.kind == "done")
                    {
                        var final = pause.Capture(authority);
                        EOSProofCheck.Require(rolled && duplicateConfirmed && rejectedGuestRoll && !final.passActive && final.rollSequence == 1,
                            "Authority outcome and negative-command checks completed");
                        EOSProofCheck.Require(opponentResumeRejected && duplicatePause && staleConfirmation && pause.Revision == 2 && !pause.IsPaused,
                            "Shared pause ownership, retry and stale-confirmation checks completed");
                        EOSProofCheck.Require(message.digest == Digest(final), "Guest final-state digest matches authority");
                        Retain(final, "host");
                        Send(new OnlineGroundPassMessage { kind = "finish", digest = message.digest }); done = true;
                    }
                    else throw new EOSProofFailure("Unexpected host gameplay message kind");
                }
                var state = pause.Capture(authority);
                if (!rolled && rejectedGuestRoll && state.stage == "interception")
                {
                    var command = new OnlineMatchCommand(matchId, 1, state.revision, state.decisionId, OnlineGroundPassAuthority.RollAction);
                    EOSProofCheck.Require(authority.Submit("host-away", command).Status == OnlineCommandStatus.Accepted, "Authority defender roll");
                    EOSProofCheck.Require(authority.Submit("host-away", command).Status == OnlineCommandStatus.Duplicate, "Defender retry cannot roll twice");
                    rolled = true; state = pause.Capture(authority);
                }
                board.Display(state, "Windows/Mac authority: Away");
                if (ready && !done && state.publicationSequence != sent)
                { Send(new OnlineGroundPassMessage { kind = "state", state = state }); sent = state.publicationSequence; }
                yield return null;
            }
            EOSProofCheck.Require(done, "Gameplay host exchange timed out");
        }

        internal IEnumerator Guest(EOSStandaloneSession session, ProductUserId remote, SocketId socket, string lobby)
        {
            SetPeer(session, remote, socket, lobby);
            EOSProofCheck.Require(!board.authorityRoot.activeSelf, "Guest gameplay managers must remain inactive");
            var replica = new OnlineGroundPassReplica(matchId);
            Send(new OnlineGroundPassMessage { kind = "hello" });
            bool started = false, previewed = false, confirmed = false, wrongRollSent = false;
            bool duplicateReply = false, wrongTeamReply = false, doneSent = false, finished = false;
            bool pauseSent = false, resumeSent = false, duplicatePauseReply = false, staleReply = false;
            double pausedAt = 0;
            var deadline = Time.realtimeSinceStartupAsDouble + 180;
            while (!finished && Time.realtimeSinceStartupAsDouble < deadline)
            {
                Poll();
                while (messages.Count > 0)
                {
                    var message = messages.Dequeue();
                    if (message.kind == "state")
                    {
                        EOSProofCheck.Require(replica.Apply(message.state), "Guest accepts ordered compatible state");
                        board.Display(replica.State, "Guest replica: Home");
                    }
                    else if (message.kind == "reply")
                    {
                        duplicateReply |= message.sequence == 3 && message.status == "Duplicate";
                        wrongTeamReply |= message.sequence == 4 && message.status == "WrongTeam";
                        staleReply |= message.sequence == 3 && message.status == "StaleRevision";
                    }
                    else if (message.kind == "control-reply")
                        duplicatePauseReply |= message.sequence == 1 && message.status == "Duplicate";
                    else if (message.kind == "finish")
                    {
                        EOSProofCheck.Require(doneSent && message.digest == Digest(replica.State), "Authority confirms guest final state");
                        Retain(replica.State, "guest"); finished = true;
                    }
                    else throw new EOSProofFailure("Unexpected guest gameplay message kind");
                }
                var state = replica.State;
                if (state != null && !doneSent)
                {
                    if (!started && state.stage == "action")
                    { Send(OnlineGroundPassMessage.Command(state, 1, OnlineGroundPassAuthority.StartAction)); started = true; }
                    else if (!previewed && state.stage == "target" && !state.hasSelectedTarget)
                    { Send(OnlineGroundPassMessage.Command(state, 2, OnlineGroundPassAuthority.TargetAction(0, 6))); previewed = true; }
                    else if (!pauseSent && previewed && state.stage == "target" && state.hasSelectedTarget)
                    {
                        var pause = new OnlineGroundPassMessage { kind = "control", sequence = 1, revision = state.sharedPauseRevision,
                            decisionId = "shared-pause", actionId = OnlineSharedPause.RequestAction };
                        Send(pause); Send(pause);
                        Send(OnlineGroundPassMessage.Command(state, 3, OnlineGroundPassAuthority.TargetAction(0, 6)));
                        pauseSent = true;
                    }
                    else if (pauseSent && !resumeSent && state.isSharedPaused)
                    {
                        EOSProofCheck.Require(state.pauseRequester == "Home" && board.sharedPausePanel.activeSelf
                            && board.requesterPauseMessage.activeSelf && !board.passivePauseMessage.activeSelf, "Guest sees its own authored pause presentation");
                        if (pausedAt == 0) pausedAt = Time.realtimeSinceStartupAsDouble;
                        if (duplicatePauseReply && staleReply && Time.realtimeSinceStartupAsDouble - pausedAt >= 1)
                        {
                            Send(new OnlineGroundPassMessage { kind = "control", sequence = 2, revision = state.sharedPauseRevision,
                                decisionId = "shared-pause", actionId = OnlineSharedPause.ResumeAction }); resumeSent = true;
                        }
                    }
                    else if (!confirmed && resumeSent && !state.isSharedPaused && state.sharedPauseRevision == 2
                        && state.stage == "target" && state.hasSelectedTarget)
                    {
                        var confirm = OnlineGroundPassMessage.Command(state, 3, OnlineGroundPassAuthority.TargetAction(0, 6));
                        Send(confirm); Send(confirm); confirmed = true;
                    }
                    else if (!wrongRollSent && state.stage == "interception")
                    { Send(OnlineGroundPassMessage.Command(state, 4, OnlineGroundPassAuthority.RollAction)); wrongRollSent = true; }
                    else if (confirmed && duplicateReply && wrongTeamReply && duplicatePauseReply && staleReply
                        && state.sharedPauseRevision == 2 && !state.passActive && state.rollSequence == 1)
                    {
                        EOSProofCheck.Require(state.tokens.Single(t => t.id == "home:9").passesAttempted == 1,
                            "Exactly one authoritative pass attempt");
                        EOSProofCheck.Require(state.tokens.Single(t => t.id == "away:8").interceptionsAttempted == 1,
                            "Exactly one authoritative interception attempt");
                        EOSProofCheck.Require(!board.authorityRoot.activeSelf, "Guest did not start local gameplay");
                        Send(new OnlineGroundPassMessage { kind = "done", digest = Digest(state) }); doneSent = true;
                    }
                }
                yield return null;
            }
            EOSProofCheck.Require(finished, "Gameplay guest exchange timed out");
        }
    }
}
