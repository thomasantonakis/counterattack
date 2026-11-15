using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        public static string SubstitutionAction(int outgoing,int incoming)=>"roster.substitute:"+outgoing+":"+incoming;
        private static bool ParseSubstitution(string action,out int outgoing,out int incoming)
        {
            outgoing=incoming=0;if(action==null)return false;var parts=action.Split(':');
            return parts.Length==3 && parts[0]=="roster.substitute"
                && int.TryParse(parts[1],NumberStyles.None,CultureInfo.InvariantCulture,out outgoing)
                && int.TryParse(parts[2],NumberStyles.None,CultureInfo.InvariantCulture,out incoming)
                && outgoing is >=1 and <=18 && incoming is >=1 and <=18 && action==SubstitutionAction(outgoing,incoming);
        }
        private readonly OnlineMatchProtocol protocol;
        private readonly MatchManager match;
        private readonly Dictionary<OnlineTeam, Dictionary<long, OnlineMatchCommand>> receipts = new();
        private readonly Dictionary<OnlineTeam, Queue<long>> order = new();
        private readonly Dictionary<OnlineTeam, long> lastSequence = new();
        private readonly Dictionary<(OnlineTeam, long), long> acceptedRevision = new();
        private float previousScale;
        private bool disposed;
        private bool faulted;
        private bool holdingTime;
        private readonly DuelResolutionPanelController resultPanel;
        private readonly HeaderChallengePanelController headerResultPanel;
        public bool ConnectionHeld { get; private set; }

        public bool IsPaused { get; private set; }
        public OnlineTeam? Requester { get; private set; }
        public long Revision { get; private set; }
        public long LastAcceptedSequence(OnlineTeam team) => lastSequence[team];

        public OnlineSharedPause(OnlineMatchProtocol protocol, MatchManager match)
        {
            this.protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            this.match = match != null ? match : throw new ArgumentNullException(nameof(match));
            resultPanel = match.movementPhaseManager?.duelResolutionPanel;
            resultPanel?.SetOnlineAuthorityControl(true, false);
            headerResultPanel = (match.highPassManager?.headerManager ?? match.shotManager?.headerManager)?.headerChallengePanel;
            headerResultPanel?.SetOnlineAuthorityControl(true, false);
            foreach (OnlineTeam team in Enum.GetValues(typeof(OnlineTeam)))
            { receipts[team] = new(); order[team] = new(); lastSequence[team] = 0; }
            match.RegisterOnlineAuthorityScope();
        }

        public OnlineCommandReply Submit(string connection, OnlineMatchCommand control)
        {
            OnlineCommandReply Reply(OnlineCommandStatus status) => new(status, Revision);
            if (disposed || faulted) return Reply(OnlineCommandStatus.NotReady);
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
            bool substitution=ParseSubstitution(control.ActionId,out int outgoing,out int incoming);
            if (control.ActionId != RequestAction && control.ActionId != ResumeAction && !substitution) return Reply(OnlineCommandStatus.IllegalAction);
            if (IsPaused && Requester != team) return Reply(OnlineCommandStatus.WrongTeam);
            if (control.ActionId == RequestAction && (IsPaused || match.IsPauseMenuOpen && !ConnectionHeld)
                || control.ActionId == ResumeAction && !IsPaused) return Reply(OnlineCommandStatus.NotReady);
            if(substitution && (!IsPaused || ConnectionHeld || !match.AreSubstitutionsAvailable))return Reply(OnlineCommandStatus.NotReady);

            var nextRevision = checked(Revision + 1);
            if(substitution)
            {
                if(protocol.Compatibility.Catalog!=OnlineFullRoomBootstrap.Catalog)return Reply(OnlineCommandStatus.IllegalAction);
                var roster=match.playerTokenManager?.AuthoredOnlineRoster;
                var playerOff=roster?.FirstOrDefault(t=>t.isHomeTeam==(team==OnlineTeam.Home) && t.jerseyNumber==outgoing);
                var playerOn=roster?.FirstOrDefault(t=>t.isHomeTeam==(team==OnlineTeam.Home) && t.jerseyNumber==incoming);
                try{if(!match.TryApplyOnlineSubstitution(playerOff,playerOn,out _))return Reply(OnlineCommandStatus.IllegalAction);}
                catch{faulted=true;throw;}
            }
            else if (control.ActionId == RequestAction)
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
            resultPanel?.SetOnlineAuthorityControl(true, hold);
            headerResultPanel?.SetOnlineAuthorityControl(true, hold);
            if (hold && !holdingTime) previousScale = Time.timeScale;
            match.SetPauseMenuOpen(hold);
            if (hold) Time.timeScale = 0;
            else if (holdingTime) Time.timeScale = previousScale;
            holdingTime = hold;
        }

        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (match != null) match.UnregisterOnlineAuthorityScope();
            resultPanel?.SetOnlineAuthorityControl(false, false);
            headerResultPanel?.SetOnlineAuthorityControl(false, false);
            if (!holdingTime) return;
            if (match != null) match.SetPauseMenuOpen(false);
            Time.timeScale = previousScale; IsPaused = false; Requester = null;
            holdingTime = false; ConnectionHeld = false;
        }
    }
}
