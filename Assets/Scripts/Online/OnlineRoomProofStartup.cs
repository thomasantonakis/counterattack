using System;
using System.Linq;
using UnityEngine;

namespace CounterAttack.Online
{
    // Installed only in the owned deterministic Room proof, not ordinary Room.
    [DefaultExecutionOrder(-32700)]
    public sealed class OnlineRoomProofStartup : MonoBehaviour
    {
        private OnlineRoomSession owned;
        private void Awake()
        {
            if (OnlineRoomSession.Current != null) return;
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-ca-eos-role");
            var authority = index >= 0 && index + 1 < args.Length && new[] { "host", "local" }.Contains(args[index + 1]);
            owned = new OnlineRoomSession("owned-room-proof", !authority);
        }
        private void OnDestroy() => owned?.Dispose();
    }
}
