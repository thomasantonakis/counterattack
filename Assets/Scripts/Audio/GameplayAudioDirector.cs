using UnityEngine;

/// <summary>
/// Presentation sink. Sources are authored in a scene/prefab, never created at runtime.
/// Gameplay submits confirmed events; no gameplay callbacks are invoked from audio.
/// </summary>
public sealed partial class GameplayAudioDirector : MonoBehaviour
{
    public GameplayAudioLibrary library;
    public AudioSource homeChatter;
    public AudioSource awayChatter;
    public AudioSource homeChatterNext;
    public AudioSource awayChatterNext;
    public AudioSource homeChant;
    public AudioSource awayChant;
    public AudioSource homeReaction;
    public AudioSource awayReaction;
    public AudioSource whistle;
    public AudioSource gameEffects;
    public AudioSource diceWiggleEffects;
    public AudioSource music;
    public bool stadiumActive = true;

    private GameplayAudioPolicy policy;
    private GameplayAudioPolicy Policy => policy ??= new GameplayAudioPolicy(System.Environment.TickCount);
    private int homePriority;
    private int awayPriority;
    private float homeIntensity;
    private float awayIntensity;
    private GameplayAudioPool homeReactionPool;
    private GameplayAudioPool awayReactionPool;
    private string CurrentMatchType => stadiumActive ? MatchManager.Instance?.gameData?.gameSettings?.matchType : null;
    private bool homeFillRemaining;
    private bool awayFillRemaining;
    private GameplayAudioChatterLoop homeLoop;
    private GameplayAudioChatterLoop awayLoop;
    private sealed class ChantState
    {
        public float NextAt;
        public float Envelope = 1f;
        public bool WasPlaying;
        public bool Fading;
    }
    private readonly ChantState homeChantState = new ChantState();
    private readonly ChantState awayChantState = new ChantState();
    private bool paused;
    private GameplayAudioPool gamePool;
    private string diceWiggleRollId;

    private void Awake() => policy = new GameplayAudioPolicy(System.Environment.TickCount);
    private void OnEnable()
    {
        GameplayAudioPreferences.Local.Changed += RefreshVolumes;
        GameplayAudioEvents.GoalConfirmed += OnGoalConfirmed;
        GameplayAudioEvents.IncidentConfirmed += OnIncidentConfirmed;
    }
    private void OnDisable()
    {
        GameplayAudioPreferences.Local.Changed -= RefreshVolumes;
        GameplayAudioEvents.GoalConfirmed -= OnGoalConfirmed;
        GameplayAudioEvents.IncidentConfirmed -= OnIncidentConfirmed;
        ClearPendingSubstitutions();
        StopAll();
        diceWiggleRollId = null;
    }

    private void OnGoalConfirmed(GameplayAudioEvents.GoalCue cue)
    {
        if (!stadiumActive || cue.Source != MatchManager.Instance) return;
        SetPaused(cue.Source.IsPauseMenuOpen);
        PlayGoal(cue.IncidentId, cue.HomeScored);
    }

    private void Start()
    {
        ScheduleChants();
        RestartChatter();
    }

    private void Update()
    {
        if (stadiumActive && MatchManager.Instance != null)
            SetPaused(MatchManager.Instance.IsPauseMenuOpen);
        ObserveMatchAudioState();
        SyncDiceWiggleWaitAudio();
        AdvanceAudio(Time.unscaledDeltaTime);
    }

    public void SyncDiceWiggleWaitAudio()
    {
        MatchManager match = stadiumActive ? MatchManager.Instance : null;
        bool waiting = match != null && match.DecisionTiming.HasRollWait;
        string rollId = waiting ? match.DecisionTiming.RollId : null;
        if (!waiting) diceWiggleRollId = null;

        bool shouldPlay = waiting && !paused && !match.IsPauseMenuOpen;
        if (!shouldPlay)
        {
            StopDiceWiggle();
            return;
        }

        if (string.IsNullOrEmpty(rollId) || rollId == diceWiggleRollId) return;
        diceWiggleRollId = rollId;
        PlayDiceWiggle();
    }

    public void AdvanceAudio(float unscaledDeltaTime)
    {
        if (paused || !stadiumActive) return;
        if (float.IsNaN(unscaledDeltaTime) || float.IsInfinity(unscaledDeltaTime)) return;
        float delta = Mathf.Max(0f, unscaledDeltaTime);
        float gain = GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Stadium);
        homeLoop?.Tick(delta, gain);
        awayLoop?.Tick(delta, gain);
        TickChant(homeChant, homeChantState, true, delta, gain);
        TickChant(awayChant, awayChantState, false, delta, gain);
        TickPreparation(delta, gain);
        PlayPendingSubstitutions();
        RefreshReactionVolumes(gain);
        if (Time.unscaledTime >= homeChantState.NextAt)
        {
            if (HasChantPlayback(true)) TryPlayChant(true);
            else homeChantState.NextAt = Time.unscaledTime + Policy.NextChantDelay();
        }
        if (Time.unscaledTime >= awayChantState.NextAt)
        {
            if (HasChantPlayback(false)) TryPlayChant(false);
            else awayChantState.NextAt = Time.unscaledTime + Policy.NextChantDelay();
        }
    }

    public bool PlayGoal(string incidentId, bool homeScored) => PlayReaction(incidentId, GameplayAudioConfig.Goal(homeScored));

    public bool PlayPenaltyShootoutOutcome(string incidentId, bool shooterHome, bool scored) =>
        PlayReaction(incidentId, GameplayAudioConfig.PenaltyShootoutOutcome(shooterHome, scored));

    public bool PlayReaction(string incidentId, GameplayAudioConfig.CrowdReaction reaction)
    {
        if (string.IsNullOrWhiteSpace(incidentId) || !Policy.AcceptEvent("crowd:" + incidentId) || !stadiumActive) return false;
        FadeChantsForEvent();
        preparationSuppressed = preparing;
        StopPreparation();
        bool home = PlayStand(homeReaction, reaction.Home.Pool, true, reaction.Home.Intensity, reaction.Priority, reaction.FillRemaining);
        bool away = PlayStand(awayReaction, reaction.Away.Pool, false, reaction.Away.Intensity, reaction.Priority, reaction.FillRemaining);
        return home || away;
    }

    public bool PlayWhistle(string incidentId)
    {
        if (string.IsNullOrWhiteSpace(incidentId) || !Policy.AcceptEvent("whistle:" + incidentId) || !stadiumActive) return false;
        FadeChantsForEvent();
        bool played = Play(whistle, GameplayAudioPool.Whistle, false,
            GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Stadium) * GameplayAudioConfig.CrowdOutputGain);
        if (played) whistleIntensity = 1f;
        return played;
    }

    public bool TryPlayChant(bool home)
    {
        if (paused || !stadiumActive || preparing || IsPlaying(homeReaction) || IsPlaying(awayReaction)) return false;
        AudioSource source = home ? homeChant : awayChant;
        if (source == null || source.isPlaying) return false;
        float gain = GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Stadium);
        if (!Play(source, GameplayAudioPool.Chant, false,
            GameplayAudioConfig.StandGain(home, GameplayAudioConfig.ChantIntensity, gain, CurrentMatchType) * GameplayAudioConfig.CrowdOutputGain)) return false;
        ChantState state = home ? homeChantState : awayChantState;
        state.Envelope = 1f;
        state.Fading = false;
        state.WasPlaying = true;
        state.NextAt = float.PositiveInfinity;
        return true;
    }

    private bool HasChantPlayback(bool home)
    {
        if ((home ? homeChant : awayChant) == null || library == null) return false;
        AudioClip[] clips = library.Get(GameplayAudioPool.Chant);
        if (clips == null) return false;
        foreach (AudioClip clip in clips) if (clip != null) return true;
        return false;
    }

    public float SecondsUntilNextChant(bool home) =>
        Mathf.Max(0f, (home ? homeChantState : awayChantState).NextAt - Time.unscaledTime);

    public bool PlayGameCue(GameplayAudioPool pool)
    {
        if (!IsGameCue(pool) || (paused && pool == GameplayAudioPool.Dice)) return false;
        if (!Policy.AcceptCue(pool, Time.unscaledTimeAsDouble)) return false;
        // One shared effects voice prevents repeated hovers/dice from stacking indefinitely.
        // Decorative hover never interrupts a more meaningful cue.
        if (pool == GameplayAudioPool.MenuHover && gameEffects != null && gameEffects.isPlaying) return false;
        gamePool = pool;
        return Play(gameEffects, pool, false, GameplayAudioConfig.PoolGain(pool) *
            GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Game));
    }

    public bool PlayDiceWiggle()
    {
        if (paused || diceWiggleEffects == null || library == null
            || !Policy.AcceptCue(GameplayAudioPool.DiceWiggle, Time.unscaledTimeAsDouble)) return false;
        AudioClip clip = Policy.Pick(GameplayAudioPool.DiceWiggle, library.Get(GameplayAudioPool.DiceWiggle));
        if (clip == null) return false;
        diceWiggleEffects.Stop();
        diceWiggleEffects.clip = clip;
        diceWiggleEffects.loop = false;
        diceWiggleEffects.volume = GameplayAudioConfig.PoolGain(GameplayAudioPool.DiceWiggle)
            * GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Game);
        diceWiggleEffects.Play();
        return true;
    }

    public void StopDiceWiggle()
    {
        if (diceWiggleEffects != null && diceWiggleEffects.isPlaying) diceWiggleEffects.Stop();
    }

    public void SetPaused(bool value)
    {
        if (paused == value) return;
        paused = value;
        Policy.SetPaused(value);
        StopAll();
        if (value)
        {
            Play(music, GameplayAudioPool.PauseMusic, true,
                GameplayAudioConfig.PoolGain(GameplayAudioPool.PauseMusic) * GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Music));
            PlayGameCue(GameplayAudioPool.Pause);
        }
        else
        {
            ScheduleChants();
            RestartChatter();
            PlayGameCue(GameplayAudioPool.Resume);
        }
    }

    private bool PlayStand(AudioSource source, GameplayAudioPool pool, bool home, float intensity, int priority, bool fillRemaining)
    {
        if (!GameplayAudioPolicy.CanReplace(priority, source != null && source.isPlaying, home ? homePriority : awayPriority)) return false;
        float gain = GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Stadium);
        if (!Play(source, pool, false, ReactionVolume(home, intensity, fillRemaining, gain, pool))) return false;
        if (home) { homePriority = priority; homeIntensity = intensity; homeFillRemaining = fillRemaining; homeReactionPool = pool; }
        else { awayPriority = priority; awayIntensity = intensity; awayFillRemaining = fillRemaining; awayReactionPool = pool; }
        return true;
    }

    private float ReactionVolume(bool home, float intensity, bool fillRemaining, float gain, GameplayAudioPool pool)
    {
        float available = GameplayAudioConfig.StandGain(home, intensity, gain, CurrentMatchType) * GameplayAudioConfig.CrowdOutputGain;
        if (!fillRemaining) return Mathf.Clamp01(available * GameplayAudioConfig.PoolGain(pool));
        return Mathf.Max(0f, available - PlayingVolume(home ? homeChatter : awayChatter)
            - PlayingVolume(home ? homeChatterNext : awayChatterNext)
            - PlayingVolume(home ? homeChant : awayChant));
    }

    private static float PlayingVolume(AudioSource source) => IsPlaying(source) ? source.volume : 0f;

    private void RefreshReactionVolumes(float gain)
    {
        if (homeReaction != null) homeReaction.volume = ReactionVolume(true, homeIntensity, homeFillRemaining, gain, homeReactionPool);
        if (awayReaction != null) awayReaction.volume = ReactionVolume(false, awayIntensity, awayFillRemaining, gain, awayReactionPool);
    }

    private bool Play(AudioSource source, GameplayAudioPool pool, bool loop, float volume)
    {
        if (source == null || library == null || volume <= 0f) return false;
        AudioClip clip = Policy.Pick(pool, library.Get(pool));
        if (clip == null) return false;
        source.Stop();
        source.clip = clip;
        source.loop = loop;
        source.volume = volume;
        source.Play();
        return true;
    }

    private void RestartChatter()
    {
        if (paused || !stadiumActive) return;
        homeLoop ??= new GameplayAudioChatterLoop(homeChatter, homeChatterNext, true,
            previous => SelectWalla(previous, awayLoop), Policy.NextWallaIntensity, () => CurrentMatchType);
        awayLoop ??= new GameplayAudioChatterLoop(awayChatter, awayChatterNext, false,
            previous => SelectWalla(previous, homeLoop), Policy.NextWallaIntensity, () => CurrentMatchType);
        homeLoop.Start();
        awayLoop.Start();
    }

    private AudioClip SelectWalla(AudioClip previous, GameplayAudioChatterLoop other)
    {
        if (library == null) return null;
        return Policy.Pick(GameplayAudioPool.Chatter, library.Get(GameplayAudioPool.Chatter), previous,
            IsPlaying(other?.ActiveSource) ? other.ActiveSource.clip : null,
            IsPlaying(other?.IncomingSource) ? other.IncomingSource.clip : null);
    }

    private void FadeChantsForEvent()
    {
        if (IsPlaying(homeChant)) homeChantState.Fading = true;
        if (IsPlaying(awayChant)) awayChantState.Fading = true;
    }

    private void TickChant(AudioSource source, ChantState state, bool home, float delta, float gain)
    {
        if (source == null) return;
        if (state.Fading)
        {
            state.Envelope = Mathf.MoveTowards(state.Envelope, 0f, delta / GameplayAudioConfig.ChantFadeSeconds);
            if (state.Envelope <= 0f) { source.Stop(); state.Fading = false; }
        }
        source.volume = GameplayAudioConfig.StandGain(home, GameplayAudioConfig.ChantIntensity, gain, CurrentMatchType) *
            GameplayAudioConfig.CrowdOutputGain * state.Envelope;
        if (state.WasPlaying && !source.isPlaying)
        {
            state.NextAt = Time.unscaledTime + Policy.NextChantDelay();
            state.WasPlaying = false;
        }
    }

    private void ScheduleChants()
    {
        homeChantState.NextAt = Time.unscaledTime + Policy.NextChantDelay();
        awayChantState.NextAt = Time.unscaledTime + Policy.NextChantDelay();
    }

    private void RefreshVolumes()
    {
        float stadium = GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Stadium);
        homeLoop?.ApplyGain(stadium);
        awayLoop?.ApplyGain(stadium);
        TickChant(homeChant, homeChantState, true, 0f, stadium);
        TickChant(awayChant, awayChantState, false, 0f, stadium);
        RefreshReactionVolumes(stadium);
        homePreparationLoop?.ApplyGain(stadium);
        awayPreparationLoop?.ApplyGain(stadium);
        if (announcements != null) announcements.volume = GameplayAudioConfig.ReactionIntensity * stadium * GameplayAudioConfig.CrowdOutputGain;
        if (whistle != null) whistle.volume = whistleIntensity * stadium * GameplayAudioConfig.CrowdOutputGain;
        if (gameEffects != null) gameEffects.volume = GameplayAudioConfig.PoolGain(gamePool) * GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Game);
        if (diceWiggleEffects != null)
        {
            float gameGain = GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Game);
            diceWiggleEffects.volume = GameplayAudioConfig.PoolGain(GameplayAudioPool.DiceWiggle) * gameGain;
            if (gameGain <= 0f) diceWiggleEffects.Stop();
        }
        if (music != null) music.volume = GameplayAudioConfig.PoolGain(GameplayAudioPool.PauseMusic) * GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Music);
        if (paused && music != null && !music.isPlaying && GameplayAudioPreferences.Local.Gain(GameplayAudioCategory.Music) > 0f)
            Play(music, GameplayAudioPool.PauseMusic, true, music.volume);
    }

    private void StopAll()
    {
        pendingSnapshotId = null;
        homeLoop?.Stop(); awayLoop?.Stop();
        Stop(homeChatterNext); Stop(awayChatterNext); Stop(homeChant); Stop(awayChant);
        homeChantState.WasPlaying = awayChantState.WasPlaying = false;
        homeChantState.Fading = awayChantState.Fading = false;
        Stop(homeChatter); Stop(awayChatter); Stop(homeReaction); Stop(awayReaction);
        StopPreparation();
        Stop(announcements);
        Stop(whistle); Stop(gameEffects); Stop(diceWiggleEffects); Stop(music);
    }

    private static void Stop(AudioSource source) { if (source != null) source.Stop(); }
    private static bool IsPlaying(AudioSource source) => source != null && source.isPlaying;
    private static bool IsGameCue(GameplayAudioPool pool) => pool is GameplayAudioPool.Dice or
        GameplayAudioPool.DiceWiggle or
        GameplayAudioPool.Invalid or GameplayAudioPool.Pause or GameplayAudioPool.Resume or
        GameplayAudioPool.MenuHover or GameplayAudioPool.StatsShow or GameplayAudioPool.StatsHide;
}
