using System;
using System.Collections.Generic;

namespace CounterAttack.Online
{
    public enum OnlineTeam { Home, Away }

    public enum OnlineCommandStatus
    {
        Accepted, Duplicate, InvalidCommand, WrongMatch, UnknownConnection,
        Disconnected, StaleSequence, SequenceConflict, StaleRevision,
        WrongDecision, WrongTeam, IllegalAction, NotReady
    }

    /// <summary>Exact compatibility is intentional until migrations are explicitly supported.</summary>
    public sealed class OnlineCompatibility
    {
        public string Protocol { get; }
        public string Rules { get; }
        public string Catalog { get; }

        public OnlineCompatibility(string protocol, string rules, string catalog)
        {
            OnlineProtocolIds.Require(protocol, nameof(protocol));
            OnlineProtocolIds.Require(rules, nameof(rules));
            OnlineProtocolIds.Require(catalog, nameof(catalog));
            Protocol = protocol;
            Rules = rules;
            Catalog = catalog;
        }

        public bool Matches(OnlineCompatibility other) => other != null
            && Protocol == other.Protocol && Rules == other.Rules && Catalog == other.Catalog;
    }

    /// <summary>No client-supplied team, account, method name or screen coordinates.</summary>
    public sealed class OnlineMatchCommand
    {
        public string MatchId { get; }
        public long Sequence { get; }
        public long Revision { get; }
        public string DecisionId { get; }
        public string ActionId { get; }

        public OnlineMatchCommand(string matchId, long sequence, long revision, string decisionId, string actionId)
        {
            MatchId = matchId;
            Sequence = sequence;
            Revision = revision;
            DecisionId = decisionId;
            ActionId = actionId;
        }

        internal bool IsValid => OnlineProtocolIds.IsValid(MatchId) && Sequence > 0
            && Revision >= 0 && OnlineProtocolIds.IsValid(DecisionId) && OnlineProtocolIds.IsValid(ActionId);

        internal bool SameRequest(OnlineMatchCommand other) => other != null
            && MatchId == other.MatchId && Sequence == other.Sequence && Revision == other.Revision
            && DecisionId == other.DecisionId && ActionId == other.ActionId;
    }

    public readonly struct OnlineCommandReply
    {
        public OnlineCommandStatus Status { get; }
        public long Revision { get; }
        public OnlineCommandReply(OnlineCommandStatus status, long revision)
        {
            Status = status;
            Revision = revision;
        }
    }

    /// <summary>
    /// Provider-independent authorization/admission state, confined to the authority's thread.
    /// Not an authenticator, transport, gameplay executor or persistent recovery store.
    /// The provider must authenticate before binding; the gameplay adapter must supply only
    /// legal semantic action IDs, execute each Accepted admission once, then publish a decision.
    /// </summary>
    public sealed class OnlineMatchProtocol
    {
        private sealed class Participant
        {
            public readonly OnlineTeam Team;
            public string Connection;
            public long LastSequence;
            public readonly Dictionary<long, (OnlineMatchCommand Command, long Revision)> Receipts = new();
            public readonly Queue<long> ReceiptOrder = new();
            public Participant(OnlineTeam team) => Team = team;
        }

        private const int ReceiptLimit = 64;
        private readonly Dictionary<string, Participant> accounts = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Participant> connections = new(StringComparer.Ordinal);
        // Old connection IDs may never be reused: queued traffic must not acquire new authority.
        private readonly HashSet<string> retiredConnections = new(StringComparer.Ordinal);
        private readonly HashSet<string> actions = new(StringComparer.Ordinal);
        private OnlineTeam decisionOwner;
        private string decisionId;

        public string MatchId { get; }
        public string HostAccountId { get; }
        public OnlineCompatibility Compatibility { get; }
        public long Revision { get; private set; }
        public bool HasPendingDecision => decisionId != null;
        public bool BothConnected
        {
            get
            {
                foreach (Participant participant in accounts.Values)
                    if (participant.Connection == null) return false;
                return true;
            }
        }

        public OnlineMatchProtocol(string matchId, string homeAccount, string awayAccount,
            string hostAccount, OnlineCompatibility compatibility)
        {
            OnlineProtocolIds.Require(matchId, nameof(matchId));
            OnlineProtocolIds.Require(homeAccount, nameof(homeAccount));
            OnlineProtocolIds.Require(awayAccount, nameof(awayAccount));
            if (homeAccount == awayAccount) throw new ArgumentException("Two distinct accounts are required.");
            if (hostAccount != homeAccount && hostAccount != awayAccount)
                throw new ArgumentException("The host must be a participant.", nameof(hostAccount));
            Compatibility = compatibility ?? throw new ArgumentNullException(nameof(compatibility));
            MatchId = matchId;
            HostAccountId = hostAccount;
            accounts.Add(homeAccount, new Participant(OnlineTeam.Home));
            accounts.Add(awayAccount, new Participant(OnlineTeam.Away));
        }

        /// <summary>Authority-only API. Connection IDs identify a unique connection lifetime.</summary>
        public bool BindAuthenticatedAccount(string accountId, string connectionId, OnlineCompatibility compatibility)
        {
            if (!OnlineProtocolIds.IsValid(accountId) || !OnlineProtocolIds.IsValid(connectionId)
                || !Compatibility.Matches(compatibility) || retiredConnections.Contains(connectionId)
                || !accounts.TryGetValue(accountId, out Participant participant)) return false;
            if (connections.TryGetValue(connectionId, out Participant bound)) return bound == participant;
            RetireConnection(participant);
            participant.Connection = connectionId;
            connections.Add(connectionId, participant);
            return true;
        }

        public void Disconnect(string connectionId)
        {
            if (connectionId != null && connections.TryGetValue(connectionId, out Participant participant))
                RetireConnection(participant);
        }

        public bool TryGetTeam(string connectionId, out OnlineTeam team)
        {
            team = default;
            if (connectionId == null || !connections.TryGetValue(connectionId, out Participant participant)) return false;
            team = participant.Team;
            return true;
        }

        /// <summary>Authority publishes legal actions after rules have reached an input boundary.</summary>
        public void PublishDecision(string id, OnlineTeam owner, IEnumerable<string> legalActionIds)
        {
            OnlineProtocolIds.Require(id, nameof(id));
            if (owner != OnlineTeam.Home && owner != OnlineTeam.Away) throw new ArgumentOutOfRangeException(nameof(owner));
            if (legalActionIds == null) throw new ArgumentNullException(nameof(legalActionIds));
            var nextActions = new HashSet<string>(StringComparer.Ordinal);
            foreach (string action in legalActionIds)
            {
                OnlineProtocolIds.Require(action, nameof(legalActionIds));
                nextActions.Add(action);
            }
            if (nextActions.Count == 0) throw new ArgumentException("A decision needs legal actions.", nameof(legalActionIds));
            long nextRevision = checked(Revision + 1);
            actions.Clear();
            actions.UnionWith(nextActions);
            decisionId = id;
            decisionOwner = owner;
            Revision = nextRevision;
        }

        /// <summary>
        /// Claims exactly one action at the current boundary. An Accepted result must be
        /// dispatched once by the authority adapter. Duplicate replies acknowledge admission,
        /// not completed gameplay; clients obtain outcomes from the state publisher.
        /// </summary>
        public OnlineCommandReply Admit(string connectionId, OnlineMatchCommand command)
        {
            if (command == null || !command.IsValid) return Reply(OnlineCommandStatus.InvalidCommand);
            if (command.MatchId != MatchId) return Reply(OnlineCommandStatus.WrongMatch);
            if (connectionId == null || !connections.TryGetValue(connectionId, out Participant participant))
                return Reply(OnlineCommandStatus.UnknownConnection);
            if (participant.Receipts.TryGetValue(command.Sequence, out var receipt))
                return command.SameRequest(receipt.Command)
                    ? new OnlineCommandReply(OnlineCommandStatus.Duplicate, receipt.Revision)
                    : Reply(OnlineCommandStatus.SequenceConflict);
            if (command.Sequence <= participant.LastSequence) return Reply(OnlineCommandStatus.StaleSequence);
            if (!BothConnected) return Reply(OnlineCommandStatus.Disconnected);
            if (command.Revision != Revision) return Reply(OnlineCommandStatus.StaleRevision);
            if (decisionId == null) return Reply(OnlineCommandStatus.NotReady);
            if (command.DecisionId != decisionId) return Reply(OnlineCommandStatus.WrongDecision);
            if (participant.Team != decisionOwner) return Reply(OnlineCommandStatus.WrongTeam);
            if (!actions.Contains(command.ActionId)) return Reply(OnlineCommandStatus.IllegalAction);

            long acceptedRevision = checked(Revision + 1);
            participant.LastSequence = command.Sequence;
            participant.Receipts.Add(command.Sequence, (command, acceptedRevision));
            participant.ReceiptOrder.Enqueue(command.Sequence);
            if (participant.ReceiptOrder.Count > ReceiptLimit)
                participant.Receipts.Remove(participant.ReceiptOrder.Dequeue());
            decisionId = null;
            actions.Clear();
            Revision = acceptedRevision;
            return Reply(OnlineCommandStatus.Accepted);
        }

        private OnlineCommandReply Reply(OnlineCommandStatus status) => new(status, Revision);

        private void RetireConnection(Participant participant)
        {
            if (participant.Connection == null) return;
            connections.Remove(participant.Connection);
            retiredConnections.Add(participant.Connection);
            participant.Connection = null;
        }
    }

    internal static class OnlineProtocolIds
    {
        internal static bool IsValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128) return false;
            foreach (char character in value)
                if (char.IsControl(character) || char.IsWhiteSpace(character)) return false;
            return true;
        }

        internal static void Require(string value, string name)
        {
            if (!IsValid(value)) throw new ArgumentException("Expected a nonblank ID of at most 128 characters, without whitespace/control characters.", name);
        }
    }
}
