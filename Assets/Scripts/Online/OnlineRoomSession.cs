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
        public bool HasInitialData=>initialData!=null;
        public string InitialPlaySessionId=>initialData?.playSessionId;
        public string AuthoritySavePath {get;}
        private readonly MatchManager.GameData initialData;
        private bool disposed;

        public OnlineRoomSession(string matchId,bool guestReplica):this(matchId,guestReplica,null,null){}
        public OnlineRoomSession(string matchId, bool guestReplica,MatchManager.GameData bootstrap,string authoritySavePath=null)
        {
            OnlineProtocolIds.Require(matchId, nameof(matchId));
            if (Current != null) throw new InvalidOperationException("An online Room session is already active");
            if(bootstrap!=null && !guestReplica && (string.IsNullOrEmpty(authoritySavePath) || !System.IO.Path.IsPathFullyQualified(authoritySavePath)))
                throw new ArgumentException("An online authority requires its own absolute session save path",nameof(authoritySavePath));
            initialData=OnlineRoomLaunchData.Copy(bootstrap);AuthoritySavePath=guestReplica?null:authoritySavePath;
            MatchId = matchId; GuestReplica = guestReplica; Current = this;
        }
        public MatchManager.GameData CaptureInitialData()=>OnlineRoomLaunchData.Copy(initialData);
        public void Dispose()
        { if (disposed) return; disposed = true; if (Current == this) Current = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Current = null;
    }
}
