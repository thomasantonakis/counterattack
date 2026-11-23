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

        internal bool Valid() => icon is >= 0 and <= 2 && Finite(x) && Finite(z);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && Mathf.Abs(value) <= 100f;
    }
}
