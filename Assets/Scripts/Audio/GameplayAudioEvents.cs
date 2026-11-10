using System;
using UnityEngine;

/// <summary>Live presentation notifications; historical score restoration does not publish these.</summary>
public static class GameplayAudioEvents
{
    public enum IncidentKind { DuelWon, DuelLost, OffTarget, BlockedShot, Save, Foul, OpponentError,
        SnapshotAvailable, KickOff, HalfTime, FullTime, Substitution, InjurySubstitution, CardShown }

    public readonly struct IncidentCue
    {
        public readonly string IncidentId;
        public readonly MatchManager Source;
        public readonly IncidentKind Kind;
        public readonly bool HomeTeam;
        public readonly bool Injury;
        public IncidentCue(string incidentId, MatchManager source, IncidentKind kind, bool homeTeam, bool injury = false)
        { IncidentId = incidentId; Source = source; Kind = kind; HomeTeam = homeTeam; Injury = injury; }
    }
    public static event Action<IncidentCue> IncidentConfirmed;

    public static void PublishIncident(MatchManager source, IncidentKind kind, bool homeTeam = false, bool injury = false) =>
        DeliverIncident(new IncidentCue(Guid.NewGuid().ToString("N"), source, kind, homeTeam, injury));

    public static void DeliverIncident(IncidentCue cue)
    {
        if (!Application.isPlaying || cue.Source == null || string.IsNullOrWhiteSpace(cue.IncidentId)) return;
        var handlers = IncidentConfirmed;
        if (handlers == null) return;
        foreach (Action<IncidentCue> handler in handlers.GetInvocationList())
        {
            try { handler(cue); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
    }

    public static void PublishAction(MatchManager source, PlayerToken actor, MatchManager.ActionType action,
        int value, string tackleType, string recoveryType, string saveType, PlayerToken connectedToken = null)
    {
        if (actor == null || value <= 0) return;
        switch (action)
        {
            // Foul audio is emitted at the tackler/recovery roll, before this later statistic.
            case MatchManager.ActionType.GroundDuelWon when tackleType == "successful" || tackleType == "nutmeg" || tackleType == "keep":
                PublishIncident(source, IncidentKind.DuelWon, actor.isHomeTeam); break;
            case MatchManager.ActionType.InterceptionSuccess:
                PublishIncident(source, IncidentKind.DuelWon, actor.isHomeTeam); break;
            case MatchManager.ActionType.BallRecovery when recoveryType != "tackle" && !actor.IsGoalKeeper:
                // Tackles and goalkeeper saves already publish their own reaction.
                // Teammate receptions are not defensive recoveries.
                if (connectedToken != null ? connectedToken.isHomeTeam != actor.isHomeTeam : !actor.isAttacker)
                    PublishIncident(source, IncidentKind.DuelWon, actor.isHomeTeam);
                break;
            case MatchManager.ActionType.ShotOffTarget:
                PublishIncident(source, IncidentKind.OffTarget, actor.isHomeTeam); break;
            case MatchManager.ActionType.ShotBlocked:
                // GK saves have their own row; ShotBlockMade would duplicate this cue.
                if (connectedToken != null && !connectedToken.IsGoalKeeper)
                    PublishIncident(source, IncidentKind.BlockedShot, actor.isHomeTeam);
                break;
            case MatchManager.ActionType.KickOffPassAttempt:
                PublishIncident(source, IncidentKind.KickOff); break;
            case MatchManager.ActionType.YellowCardShown:
            case MatchManager.ActionType.RedCardShown:
                if (connectedToken != null && connectedToken.isHomeTeam != actor.isHomeTeam)
                    PublishIncident(source, IncidentKind.CardShown, connectedToken.isHomeTeam);
                break;
            case MatchManager.ActionType.SaveMade when saveType == "held" || saveType == "loose" || saveType == "corner":
                PublishIncident(source, IncidentKind.Save, actor.isHomeTeam); break;
        }
    }
    public readonly struct GoalCue
    {
        public readonly string IncidentId;
        public readonly MatchManager Source;
        public readonly bool HomeScored;

        public GoalCue(string incidentId, MatchManager source, bool homeScored)
        {
            IncidentId = incidentId;
            Source = source;
            HomeScored = homeScored;
        }
    }

    public static event Action<GoalCue> GoalConfirmed;

    public static void PublishGoalConfirmed(MatchManager source, bool homeScored)
    {
        if (!Application.isPlaying || source == null) return;
        DeliverGoalConfirmed(new GoalCue(Guid.NewGuid().ToString("N"), source, homeScored));
    }

    // Redelivery preserves the original incident ID. Preferences remain at the local sink.
    public static void DeliverGoalConfirmed(GoalCue cue)
    {
        if (!Application.isPlaying || cue.Source == null || string.IsNullOrWhiteSpace(cue.IncidentId)) return;
        var handlers = GoalConfirmed;
        if (handlers == null) return;
        foreach (Action<GoalCue> handler in handlers.GetInvocationList())
        {
            try { handler(cue); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
    }
}
