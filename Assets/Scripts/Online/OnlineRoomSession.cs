using System;
using UnityEngine;

namespace CounterAttack.Online
{
    /// <summary>Explicit startup role supplied by an authenticated online bootstrap before loading Room.</summary>
    public sealed class OnlineRoomSession : IDisposable
    {
        public static OnlineRoomSession Current { get; private set; }
        public static bool IsGuest => Current?.GuestReplica == true;
        public bool GuestReplica { get; }
        public string MatchId { get; }
        private bool disposed;

        public OnlineRoomSession(string matchId, bool guestReplica)
        {
            OnlineProtocolIds.Require(matchId, nameof(matchId));
            if (Current != null) throw new InvalidOperationException("An online Room session is already active");
            MatchId = matchId; GuestReplica = guestReplica; Current = this;
        }
        public void Dispose()
        { if (disposed) return; disposed = true; if (Current == this) Current = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Current = null;
    }
}
