using UnityEngine;

namespace CounterAttack.Online
{
    [DefaultExecutionOrder(-32000)]
    public sealed class OnlineRoomStartup : MonoBehaviour
    {
        [Tooltip("Explicit scene-owned authority roots. Never include designer-owned Stats or personal cameras.")]
        public GameObject[] authorityRoots;
        public bool GuestConfigured { get; private set; }

        private void Awake()
        {
            if (!OnlineRoomSession.IsGuest) return;
            foreach (var root in authorityRoots)
                if (root != null) root.SetActive(false);
            GuestConfigured = true;
        }
    }
}
