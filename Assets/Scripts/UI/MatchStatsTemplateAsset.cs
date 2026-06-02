using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CounterAttack/UI/Match Stats Template", fileName = "MatchStatsTemplate")]
public sealed class MatchStatsTemplateAsset : ScriptableObject
{
    public enum RowKind
    {
        Header,
        Section,
        Metric,
    }

    public enum TextAlignment
    {
        Left,
        Center,
        Right,
    }

    [Serializable]
    public sealed class RowDefinition
    {
        public RowKind kind;
        public string centerLabel;

        public RowDefinition()
        {
        }

        public RowDefinition(RowKind kind, string centerLabel)
        {
            this.kind = kind;
            this.centerLabel = centerLabel;
        }
    }

    public List<RowDefinition> statsRows = BuildDefaultRows();
    public bool showLineups = true;
    public string lineupHeaderLabel = "XI";
    public TextAlignment scoreboardLeftColumnAlignment = TextAlignment.Right;
    public TextAlignment scoreboardCenterColumnAlignment = TextAlignment.Center;
    public TextAlignment scoreboardRightColumnAlignment = TextAlignment.Left;
    public TextAlignment lineupLeftColumnAlignment = TextAlignment.Right;
    public TextAlignment lineupCenterColumnAlignment = TextAlignment.Center;
    public TextAlignment lineupRightColumnAlignment = TextAlignment.Left;

    public static List<RowDefinition> BuildDefaultRows()
    {
        return new List<RowDefinition>
        {
            new(RowKind.Section, "ATTACKING"),
            new(RowKind.Metric, "Total Shots / xG"),
            new(RowKind.Metric, "On Target / Corners"),
            new(RowKind.Metric, "Blocked / Off Target"),
            new(RowKind.Section, "PASSING"),
            new(RowKind.Metric, "Assists"),
            new(RowKind.Metric, "Ground (Att./Made)"),
            new(RowKind.Metric, "Aerial(Att./Trg./Made)"),
            new(RowKind.Section, "Game Play"),
            new(RowKind.Metric, "Distance Covered"),
            new(RowKind.Metric, "Possession"),
            new(RowKind.Section, "DUELS"),
            new(RowKind.Metric, "Ground Att. / Won"),
            new(RowKind.Metric, "Air Att. / Won"),
            new(RowKind.Section, "DISCIPLINE"),
            new(RowKind.Metric, "Yellow / Red Cards"),
            new(RowKind.Metric, "Injuries / Subs Used"),
            new(RowKind.Section, "OPTA STATS"),
            new(RowKind.Metric, "xRecoveries / Made"),
            new(RowKind.Metric, "xDribbles / Made"),
            new(RowKind.Metric, "xTackles / Made"),
        };
    }

    private void Reset()
    {
        statsRows = BuildDefaultRows();
    }
}
