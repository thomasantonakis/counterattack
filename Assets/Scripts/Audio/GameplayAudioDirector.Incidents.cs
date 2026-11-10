using UnityEngine;

public sealed partial class GameplayAudioDirector
{
    public AudioSource announcements;
    public AudioSource homePreparation;
    public AudioSource homePreparationNext;
    public AudioSource awayPreparation;
    public AudioSource awayPreparationNext;

    private readonly GameplayAudioPolicy incidentDelivery = new GameplayAudioPolicy(1);
    private GameplayAudioChatterLoop homePreparationLoop;
    private GameplayAudioChatterLoop awayPreparationLoop;
    private bool preparing;
    private bool preparationSuppressed;
    private bool snapshotWasAvailable;
    private PlayerToken snapshotOwner;
    private string pendingSnapshotId;
    private bool pendingSnapshotHome;
    private float nextHomeSnapshotAt;
    private float nextAwaySnapshotAt;
    private int pendingHomeSubs;
    private int pendingAwaySubs;
    private bool pendingHomeInjury;
    private bool pendingAwayInjury;
    private MatchManager pendingSubMatch;
    private int stadiumCuePriority;
    private float whistleIntensity = 1f;

    private void OnIncidentConfirmed(GameplayAudioEvents.IncidentCue cue)
    {
        if (!stadiumActive || cue.Source != MatchManager.Instance || !incidentDelivery.AcceptEvent(cue.IncidentId)) return;
        SetPaused(cue.Source.IsPauseMenuOpen);
        if (cue.Kind == GameplayAudioEvents.IncidentKind.SnapshotAvailable)
        {
            pendingSnapshotId = cue.IncidentId;
            pendingSnapshotHome = cue.HomeTeam;
            TryPlayPendingSnapshot();
            return;
        }
        if (cue.Kind == GameplayAudioEvents.IncidentKind.Substitution)
        {
            if (pendingSubMatch != cue.Source) ClearPendingSubstitutions();
            pendingSubMatch = cue.Source;
            if (cue.HomeTeam) { pendingHomeSubs = Mathf.Min(4, pendingHomeSubs + 1); pendingHomeInjury |= cue.Injury; }
            else { pendingAwaySubs = Mathf.Min(4, pendingAwaySubs + 1); pendingAwayInjury |= cue.Injury; }
            return;
        }
        switch (cue.Kind)
        {
            case GameplayAudioEvents.IncidentKind.KickOff:
                PlayStadiumCue(cue.IncidentId, GameplayAudioPool.KickOff, 10); break;
            case GameplayAudioEvents.IncidentKind.HalfTime:
                PlayStadiumCue(cue.IncidentId, GameplayAudioPool.HalfTime, 20); break;
            case GameplayAudioEvents.IncidentKind.FullTime:
                PlayStadiumCue(cue.IncidentId, GameplayAudioPool.FullTime, 30); break;
            default:
                PlayReaction(cue.IncidentId, GameplayAudioConfig.Incident(cue.Kind, cue.HomeTeam)); break;
        }
    }

    private bool PlayStadiumCue(string id, GameplayAudioPool pool, int priority)
    {
        if (!Policy.AcceptEvent("stadium:" + id)) return false;
        if (!GameplayAudioPolicy.CanReplace(priority, IsPlaying(whistle), stadiumCuePriority)) return false;
        FadeChantsForEvent();
        StopPreparation();
        if (!Play(whistle, pool, false, GameplayAudioConfig.ReactionIntensity *
            GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Stadium) * GameplayAudioConfig.CrowdOutputGain)) return false;
        stadiumCuePriority = priority;
        whistleIntensity = GameplayAudioConfig.ReactionIntensity;
        return true;
    }

    public void ObserveMatchAudioState()
    {
        var match = MatchManager.Instance;
        if (!stadiumActive || match == null)
        {
            StopPreparation();
            pendingSnapshotId = null;
            snapshotWasAvailable = false;
            snapshotOwner = null;
            return;
        }
        string state = match.currentState.ToString();
        bool wantsPreparation = (state.StartsWith("FreeKick", System.StringComparison.Ordinal) && state != "FreeKickExecution")
            || (state.StartsWith("Penalty", System.StringComparison.Ordinal) && !state.StartsWith("PenaltyShootout", System.StringComparison.Ordinal) && state != "PenaltyExecution")
            || match.currentState == MatchManager.GameState.WaitingForThrowInTaker
            || match.currentState == MatchManager.GameState.WaitingForGoalKickFinalThirds
            || match.currentState == MatchManager.GameState.GoalKick;
        if (!wantsPreparation) { preparationSuppressed = false; StopPreparation(); }
        else if (!paused && !preparing && !preparationSuppressed && !IsPlaying(homeReaction) && !IsPlaying(awayReaction))
            StartPreparation();

        var shot = match.shotManager;
        var movement = match.movementPhaseManager;
        bool movementOffer = movement != null && movement.isActivated && movement.isWaitingForSnapshotDecision;
        bool shotOffer = shot != null && shot.isAvailable && !shot.isActivated;
        bool available = (movementOffer || shotOffer) && !wantsPreparation
            && !(shot?.isActivated ?? false)
            && !match.IsFinalExtraMovementPhaseSnapshotSuppressed()
            && match.currentState != MatchManager.GameState.EndOfMovementPhase
            && match.currentState != MatchManager.GameState.MatchEnded
            && match.currentState != MatchManager.GameState.HalfTime
            && match.currentState != MatchManager.GameState.KickOffSetup
            && match.currentState != MatchManager.GameState.PostGoalKickOffSetup
            && match.currentState != MatchManager.GameState.KickOffTakerSelection;
        PlayerToken owner = available ? match.ball?.GetCurrentHex()?.GetOccupyingToken() : null;
        bool newOffer = available && owner != null && (!snapshotWasAvailable || owner != snapshotOwner);
        if (!available || owner == null) pendingSnapshotId = null;
        snapshotWasAvailable = available;
        snapshotOwner = owner;
        if (newOffer) GameplayAudioEvents.PublishIncident(match, GameplayAudioEvents.IncidentKind.SnapshotAvailable, owner.isHomeTeam);
        TryPlayPendingSnapshot();
    }

    private void TryPlayPendingSnapshot()
    {
        if (pendingSnapshotId == null) return;
        if (paused || !snapshotWasAvailable || snapshotOwner == null || snapshotOwner.isHomeTeam != pendingSnapshotHome)
        { pendingSnapshotId = null; return; }
        float gain = GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Stadium);
        if (gain > 0f)
        {
            var match = MatchManager.Instance;
            var movement = match?.movementPhaseManager;
            var keeper = movement?.goalKeeperManager;
            if ((keeper != null && (keeper.isActivated || keeper.IsResolvingMove))
                || (movement != null && movement.isPlayerMoving)
                || (match?.finalThirdManager?.isActivated ?? false)) return;
            if (Time.unscaledTime < (pendingSnapshotHome ? nextHomeSnapshotAt : nextAwaySnapshotAt)) return;
            var source = pendingSnapshotHome ? homeReaction : awayReaction;
            if (!GameplayAudioPolicy.CanReplace(GameplayAudioConfig.SnapshotPriority, IsPlaying(source),
                pendingSnapshotHome ? homePriority : awayPriority)) return;
        }
        string id = pendingSnapshotId;
        pendingSnapshotId = null;
        if (PlayReaction(id, GameplayAudioConfig.Incident(GameplayAudioEvents.IncidentKind.SnapshotAvailable, pendingSnapshotHome)))
        {
            float next = Time.unscaledTime + GameplayAudioConfig.SnapshotRepeatCooldownSeconds;
            if (pendingSnapshotHome) nextHomeSnapshotAt = next;
            else nextAwaySnapshotAt = next;
        }
    }

    private void StartPreparation()
    {
        FadeChantsForEvent();
        homePreparationLoop ??= new GameplayAudioChatterLoop(homePreparation, homePreparationNext, true,
            previous => Policy.Pick(GameplayAudioPool.SetPiecePreparation, library?.Get(GameplayAudioPool.SetPiecePreparation), previous),
            () => GameplayAudioConfig.SetPieceIntensity, () => CurrentMatchType);
        awayPreparationLoop ??= new GameplayAudioChatterLoop(awayPreparation, awayPreparationNext, false,
            previous => Policy.Pick(GameplayAudioPool.SetPiecePreparation, library?.Get(GameplayAudioPool.SetPiecePreparation), previous),
            () => GameplayAudioConfig.SetPieceIntensity, () => CurrentMatchType);
        preparing = homePreparationLoop.Start() | awayPreparationLoop.Start();
    }

    private void TickPreparation(float delta, float gain)
    {
        homePreparationLoop?.Tick(delta, gain);
        awayPreparationLoop?.Tick(delta, gain);
    }

    private void StopPreparation()
    {
        homePreparationLoop?.Stop();
        awayPreparationLoop?.Stop();
        preparing = false;
    }

    private void PlayPendingSubstitutions()
    {
        if (paused || IsPlaying(announcements)) return;
        if (pendingSubMatch != MatchManager.Instance) { ClearPendingSubstitutions(); return; }
        bool home = pendingHomeSubs > 0;
        int count = home ? pendingHomeSubs : pendingAwaySubs;
        if (count == 0) return;
        bool injury = home ? pendingHomeInjury : pendingAwayInjury;
        var pool = home ? GameplayAudioPool.HomeSubstitution
            : count > 1 ? GameplayAudioPool.DoubleAwaySubstitution : GameplayAudioPool.AwaySubstitution;
        if (home) { pendingHomeSubs = 0; pendingHomeInjury = false; }
        else { pendingAwaySubs = 0; pendingAwayInjury = false; }
        float gain = GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Stadium);
        FadeChantsForEvent();
        Play(announcements, pool, false, GameplayAudioConfig.ReactionIntensity * gain * GameplayAudioConfig.CrowdOutputGain);
        if (injury) PlayReaction(System.Guid.NewGuid().ToString("N"),
            GameplayAudioConfig.Incident(GameplayAudioEvents.IncidentKind.InjurySubstitution, home));
    }

    private void ClearPendingSubstitutions()
    {
        pendingHomeSubs = pendingAwaySubs = 0;
        pendingHomeInjury = pendingAwayInjury = false;
        pendingSubMatch = null;
    }
}
