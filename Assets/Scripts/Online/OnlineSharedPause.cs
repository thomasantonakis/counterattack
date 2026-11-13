using System;
using System.Collections.Generic;
using UnityEngine;

namespace CounterAttack.Online
{
    /// <summary>
    /// Shared pause lane for the isolated authority board. Control sequences and
    /// revisions are separate from gameplay commands; both survive peer rebinds.
    /// The authority owns scaled time, while EOS polling uses unscaled time.
    /// </summary>
    public sealed class OnlineSharedPause : IDisposable
    {
        public const string RequestAction = "pause.request";
        public const string ResumeAction = "pause.resume";
        private readonly OnlineMatchProtocol protocol;
        private readonly MatchManager match;
        private readonly Dictionary<OnlineTeam, Dictionary<long, OnlineMatchCommand>> receipts = new();
        private readonly Dictionary<OnlineTeam, Queue<long>> order = new();
        private readonly Dictionary<OnlineTeam, long> lastSequence = new();
        private readonly Dictionary<(OnlineTeam, long), long> acceptedRevision = new();
        private float previousScale;
        private bool disposed;
        private bool holdingTime;
        public bool ConnectionHeld { get; private set; }

        public bool IsPaused { get; private set; }
        public OnlineTeam? Requester { get; private set; }
        public long Revision { get; private set; }
        public long LastAcceptedSequence(OnlineTeam team) => lastSequence[team];

        public OnlineSharedPause(OnlineMatchProtocol protocol, MatchManager match)
        {
            this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            this.match = match != null ? match : throw new ArgumentNullException(nameof(match));
            foreach (OnlineTeam team in Enum.GetValues(typeof(OnlineTeam)))
            { receipts[team] = new(); order[team] = new(); lastSequence[team] = 0; }
        }

        public OnlineCommandReply Submit(string connection, OnlineMatchCommand control)
        {
            OnlineCommandReply Reply(OnlineCommandStatus status) => new(status, Revision);
            if (disposed) return Reply(OnlineCommandStatus.NotReady);
            if (control == null || !control.IsValid) return Reply(OnlineCommandStatus.InvalidCommand);
            if (control.MatchId != protocol.MatchId) return Reply(OnlineCommandStatus.WrongMatch);
            if (!protocol.TryGetTeam(connection, out var team)) return Reply(OnlineCommandStatus.UnknownConnection);
            if (receipts[team].TryGetValue(control.Sequence, out var prior))
                return prior.SameRequest(control)
                    ? new OnlineCommandReply(OnlineCommandStatus.Duplicate, acceptedRevision[(team, control.Sequence)])
                    : Reply(OnlineCommandStatus.SequenceConflict);
            if (control.Sequence <= lastSequence[team]) return Reply(OnlineCommandStatus.StaleSequence);
            if (!protocol.BothConnected) return Reply(OnlineCommandStatus.Disconnected);
            if (control.Revision != Revision) return Reply(OnlineCommandStatus.StaleRevision);
            if (control.DecisionId != "shared-pause") return Reply(OnlineCommandStatus.WrongDecision);
            if (control.ActionId != RequestAction && control.ActionId != ResumeAction) return Reply(OnlineCommandStatus.IllegalAction);
            if (IsPaused && Requester != team) return Reply(OnlineCommandStatus.WrongTeam);
            if (control.ActionId == RequestAction && (IsPaused || match.IsPauseMenuOpen && !ConnectionHeld)
                || control.ActionId == ResumeAction && !IsPaused) return Reply(OnlineCommandStatus.NotReady);

            var nextRevision = checked(Revision + 1);
            if (control.ActionId == RequestAction)
            {
                Requester = team; IsPaused = true;
            }
            else
            {
                IsPaused = false; Requester = null;
            }
            ApplyHold();
            Revision = nextRevision;
            lastSequence[team] = control.Sequence;
            receipts[team].Add(control.Sequence, control); order[team].Enqueue(control.Sequence);
            acceptedRevision[(team, control.Sequence)] = Revision;
            if (order[team].Count > 64)
            {
                var expired = order[team].Dequeue(); receipts[team].Remove(expired); acceptedRevision.Remove((team, expired));
            }
            return Reply(OnlineCommandStatus.Accepted);
        }

        public OnlineGroundPassState Capture(OnlineGroundPassAuthority authority)
        {
            return authority.Capture(this);
        }

        // A transport hold is independent of requester-owned pause. Releasing
        // either one cannot release the other's scaled-time/coroutine hold.
        public void SetConnectionHold(bool held)
        {
            if (disposed) throw new ObjectDisposedException(nameof(OnlineSharedPause));
            if (ConnectionHeld == held) return;
            ConnectionHeld = held;
            ApplyHold();
        }

        private void ApplyHold()
        {
            var hold = IsPaused || ConnectionHeld;
            if (hold && !holdingTime) previousScale = Time.timeScale;
            match.SetPauseMenuOpen(hold);
            if (hold) Time.timeScale = 0;
            else if (holdingTime) Time.timeScale = previousScale;
            holdingTime = hold;
        }

        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (!holdingTime) return;
            if (match != null) match.SetPauseMenuOpen(false);
            Time.timeScale = previousScale; IsPaused = false; Requester = null;
            holdingTime = false; ConnectionHeld = false;
        }
    }
}
