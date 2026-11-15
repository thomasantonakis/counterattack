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
        public static string EmergencyKeeperAction(int jersey)=>"keeper.nominate:"+jersey;
        private static bool ParseEmergencyKeeper(string action,out int jersey)
        {
            jersey=0;if(action==null)return false;var parts=action.Split(':');
            return parts.Length==2 && parts[0]=="keeper.nominate" && int.TryParse(parts[1],NumberStyles.None,CultureInfo.InvariantCulture,out jersey)
                && jersey is >=1 and <=18 && action==EmergencyKeeperAction(jersey);
        }
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
            SynchronizeRequiredKeeperHold();
            if(protocol.Compatibility.Catalog==OnlineFullRoomBootstrap.Catalog && match.OnlineShootoutManager?.RefreshOnlineOrders()==true)Revision=checked(Revision+1);
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
            bool shootout=control.ActionId.StartsWith("shootout.",StringComparison.Ordinal);
            if (control.DecisionId != (shootout?"shootout-order":"shared-pause")) return Reply(OnlineCommandStatus.WrongDecision);
            bool substitution=ParseSubstitution(control.ActionId,out int outgoing,out int incoming);
            bool nomination=ParseEmergencyKeeper(control.ActionId,out int nominee);
            bool full=protocol.Compatibility.Catalog==OnlineFullRoomBootstrap.Catalog;
            var requiredTeam=default(OnlineTeam);bool required=full && match.TryGetOnlineRequiredKeeperTeam(out requiredTeam);
            if (control.ActionId != RequestAction && control.ActionId != ResumeAction && !substitution && !nomination && !shootout) return Reply(OnlineCommandStatus.IllegalAction);
            if(shootout && (!full || IsPaused || ConnectionHeld || match.OnlineShootoutManager?.OnlineOrderSelection!=true))return Reply(OnlineCommandStatus.NotReady);
            if((substitution || nomination) && required && requiredTeam!=team)return Reply(OnlineCommandStatus.WrongTeam);
            // Required team choices do not transfer another manager's user pause.
            if (IsPaused && Requester != team && !((substitution || nomination) && required && requiredTeam==team)) return Reply(OnlineCommandStatus.WrongTeam);
            if (control.ActionId == RequestAction && (IsPaused || match.IsPauseMenuOpen && !ConnectionHeld)
                || control.ActionId == ResumeAction && !IsPaused) return Reply(OnlineCommandStatus.NotReady);
            if(substitution && (!IsPaused || ConnectionHeld || !match.AreSubstitutionsAvailable))return Reply(OnlineCommandStatus.NotReady);
            if(nomination && (!IsPaused || ConnectionHeld))return Reply(OnlineCommandStatus.NotReady);
            if(control.ActionId==ResumeAction && full && match.HasGoalkeeperActionBlockingResume())return Reply(OnlineCommandStatus.NotReady);

            var nextRevision = checked(Revision + 1);
            if(shootout)
            {
                try{if(!match.OnlineShootoutManager.TryExecuteOnlineOrder(team==OnlineTeam.Home,control.ActionId))return Reply(OnlineCommandStatus.IllegalAction);}
                catch{faulted=true;throw;}
            }
            else if(substitution)
            {
                if(protocol.Compatibility.Catalog!=OnlineFullRoomBootstrap.Catalog)return Reply(OnlineCommandStatus.IllegalAction);
                var roster=match.playerTokenManager?.AuthoredOnlineRoster;
                var playerOff=roster?.FirstOrDefault(t=>t.isHomeTeam==(team==OnlineTeam.Home) && t.jerseyNumber==outgoing);
                var playerOn=roster?.FirstOrDefault(t=>t.isHomeTeam==(team==OnlineTeam.Home) && t.jerseyNumber==incoming);
                if(required)
                {
                    bool home=team==OnlineTeam.Home;
                    if(match.IsGoalkeeperReplacementRequired(home))
                    {if(playerOff==null || playerOff.IsGoalKeeper || playerOn==null || !playerOn.IsGoalKeeper)return Reply(OnlineCommandStatus.IllegalAction);}
                    else if(match.IsForcedGoalkeeperSubstitutionRequired(home))
                    {if(playerOff!=match.GetForcedGoalkeeperSubstitutionOutgoing(home) || playerOn==null || !playerOn.IsGoalKeeper)return Reply(OnlineCommandStatus.IllegalAction);}
                    else return Reply(OnlineCommandStatus.IllegalAction);
                }
                try{if(!match.TryApplyOnlineSubstitution(playerOff,playerOn,out _))return Reply(OnlineCommandStatus.IllegalAction);}
                catch{faulted=true;throw;}
            }
            else if(nomination)
            {
                if(!full || !required || !match.IsEmergencyGoalkeeperNominationRequiredForTeam(team==OnlineTeam.Home))return Reply(OnlineCommandStatus.IllegalAction);
                var token=match.playerTokenManager?.AuthoredOnlineRoster?.FirstOrDefault(t=>t.isHomeTeam==(team==OnlineTeam.Home) && t.jerseyNumber==nominee);
                try{if(!match.TryNominateOnlineEmergencyKeeper(token,out _))return Reply(OnlineCommandStatus.IllegalAction);}
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
            SynchronizeRequiredKeeperHold();
            if(protocol.Compatibility.Catalog==OnlineFullRoomBootstrap.Catalog && match.OnlineShootoutManager?.RefreshOnlineOrders()==true)Revision=checked(Revision+1);
            return authority.Capture(this);
        }
        private void SynchronizeRequiredKeeperHold()
        {
            if(disposed || faulted || IsPaused || protocol.Compatibility.Catalog!=OnlineFullRoomBootstrap.Catalog || OnlineRoomSession.IsGuest
                || OnlineRoomSession.Current?.HasInitialData!=true || !match.TryGetOnlineRequiredKeeperTeam(out var team))return;
            Revision=checked(Revision+1);Requester=team;IsPaused=true;ApplyHold();
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
