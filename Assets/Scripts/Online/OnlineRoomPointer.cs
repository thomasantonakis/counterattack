using System;
using UnityEngine;

namespace CounterAttack.Online
{
    [Serializable]
    public sealed class OnlineRoomPointer
    {
        public bool visible;
        public float x, z;
        public int icon;
        public string hoveredToken;
        public int? goalX, goalZ;

        internal bool Valid() => icon is >= 0 and <= 2 && Finite(x) && Finite(z)
            && (hoveredToken == null || OnlineProtocolIds.IsValid(hoveredToken))
            && (goalX == null && goalZ == null || goalX is >= -25 and <= 24 && goalZ is >= -19 and <= 18);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && Mathf.Abs(value) <= 100f;
    }
}
