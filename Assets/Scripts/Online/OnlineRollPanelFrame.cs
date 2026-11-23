using System;
using System.Linq;
using UnityEngine;

namespace CounterAttack.Online
{
    [Serializable]
    public sealed class OnlineRollPanelFrame
    {
        public int panel;
        public long resultId;
        public bool[] active;
        public string[] labels;
        public Color32[] labelColors, imageColors;
        public bool[] imageEnabled;
        public int[] dice;
        public bool duelExpanded, recoveryAttached;
        public float attackColumnX = -1f;

        internal bool Valid() => panel is >= 0 and < 3 && resultId >= 0
            && active != null && active.Length <= 512
            && labels != null && labels.Length <= 160 && labels.All(label => label?.Length <= 512)
            && labelColors?.Length == labels.Length && imageColors != null && imageColors.Length <= 160
            && (imageEnabled==null || imageEnabled.Length==imageColors.Length)
            && dice != null && dice.Length <= 32 && dice.All(roll => roll is >= -1 and <= 6)
            && attackColumnX is -1f or 0f or 0.5f;
    }
}
