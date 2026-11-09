using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MatchEventBannerController : MonoBehaviour
{
    [Serializable]
    public class Artwork
    {
        public MatchBannerEvent occasion;
        public Sprite sprite;
    }

    [Header("Scene-authored presentation")]
    public CanvasGroup visibility;
    public RectTransform content;
    public UnityEngine.UI.Image icon;
    public TMP_Text caption;
    public TMP_Text detail;
    public Sprite clockArtwork;
    [Header("Separate scene-authored timing display")]
    public CanvasGroup timingVisibility;
    public RectTransform timingContent;
    public UnityEngine.UI.Image timingIcon;
    public TMP_Text timingCaption, timingDetail;
    private DecisionTimingService lastTiming;
    private bool timingVisible;
    private Color timingCaptionColor = Color.white;
    public bool IsTimingVisible => timingVisible;
    public Artwork[] artwork = Array.Empty<Artwork>();
    [Range(3f, 5f)] public float displaySeconds = 4f;
    [Header("Edit Mode artwork preview")]
    public MatchBannerEvent previewEvent;
    private readonly Queue<(MatchBannerEvent kind, string detail)> pending = new();
    private bool showing;
    private float elapsed;
    private Vector3 restingScale = Vector3.one;
    public int PendingCount => pending.Count;
    public bool IsShowing => showing;
    public MatchBannerEvent CurrentEvent { get; private set; }

    private void OnEnable()
    {
        if (content != null) restingScale = content.localScale;
        Clear();
        MatchEventAnnouncements.Published += Enqueue;
    }

    private void OnDisable()
    {
        MatchEventAnnouncements.Published -= Enqueue;
        Clear();
    }

    private void Update()
    {
        SetTimingStatus(MatchManager.Instance?.DecisionTiming);
        Advance(Time.unscaledDeltaTime);
    }

    public void SetTimingStatus(DecisionTimingService timing)
    {
        lastTiming = timing;
        var visibility = timingVisibility;
        var caption = timingCaption;
        var detail = timingDetail;
        var icon = timingIcon;
        var content = timingContent;
        var current = timing?.Current ?? timing?.ExpiredDecision;
        bool halfway = current != null && current.budget > 0
            && current.remaining <= current.budget * (1f - DecisionTimingConfig.CountdownDisplaySpentFraction);
        bool visible = !showing && pending.Count == 0 && timing != null
            && (timing.BlockingResult || (!timing.GameClockStopped && halfway));
        if (visibility == null || caption == null || icon == null) return;
        if (!visible)
        {
            timingVisible = false;
            visibility.alpha = 0;
            return;
        }
        if (!timingVisible)
        {
            caption.color = timingCaptionColor;
        }
        timingVisible = true;
        visibility.alpha = 1;
        visibility.blocksRaycasts = false;
        visibility.interactable = false;
        if (content != null) content.localScale = restingScale;
        icon.sprite = clockArtwork;
        icon.enabled = clockArtwork != null;
        caption.color = current != null && current.remaining <= DecisionTimingConfig.CountdownWarningSeconds
            ? new Color(1f, 0.68f, 0.18f) : timingCaptionColor;
        string description;
        if (timing.BlockingResult) { caption.text = "Match clock stopped"; description = "Reviewing result."; }
        else if (timing.HoldsMatchClock)
        {
            caption.text = "Match clock stopped";
            description = current != null ? $"Awaiting {current.owner} · {FormatDecision(current.kind)}." : "Awaiting decision.";
        }
        else
        {
            caption.text = $"{Mathf.CeilToInt(current.remaining)}s remaining";
            description = $"{current.owner} · {FormatDecision(current.kind)}";
            if (timing.HasPendingRoll) description += $" · Auto roll in {Mathf.CeilToInt(timing.RollRemaining)}s";
        }
        if (detail != null) detail.text = description;
    }

    private static string FormatDecision(DecisionKind kind)
        => System.Text.RegularExpressions.Regex.Replace(kind.ToString(), "([a-z])([A-Z])", "$1 $2");

    [ContextMenu("Preview decision timing in Edit Mode")]
    private void PreviewDecisionTiming()
    {
        if (Application.isPlaying) return;
        var timing = new DecisionTimingService();
        timing.BeginDecision("preview", DecisionKind.SelectNextAction, "Home", 1);
        timing.Select("preview");
        timing.Advance(DecisionTimingConfig.GetBudget(DecisionKind.SelectNextAction, 1));
        SetTimingStatus(timing);
    }

    public void Enqueue(MatchBannerEvent kind, string description)
    {
        if (visibility == null || caption == null || icon == null) return;
        pending.Enqueue((kind, description));
        SetTimingStatus(lastTiming);
        if (!showing) ShowNext();
    }

    private void ShowNext()
    {
        var next = pending.Dequeue();
        CurrentEvent = next.kind;
        caption.text = MatchEventAnnouncements.Caption(next.kind);
        if (detail != null) detail.text = next.detail;
        icon.sprite = null;
        foreach (var entry in artwork)
        {
            if (entry != null && entry.occasion == next.kind) { icon.sprite = entry.sprite; break; }
        }
        icon.enabled = icon.sprite != null;
        elapsed = 0f;
        showing = true;
        visibility.alpha = 0f;
        visibility.blocksRaycasts = false;
        visibility.interactable = false;
    }

    // Explicit elapsed-time seam keeps queue and transition tests independent of wall time.
    public void Advance(float seconds)
    {
        if (!showing) return;
        elapsed += Mathf.Max(0f, seconds);
        float duration = Mathf.Clamp(displaySeconds, 3f, 5f);
        float entrance = Mathf.Clamp01(elapsed / 0.18f);
        visibility.alpha = Mathf.Min(entrance, Mathf.Clamp01((duration - elapsed) / 0.25f));
        if (content != null) content.localScale = restingScale * Mathf.Lerp(0.94f, 1f, entrance);
        if (elapsed < duration) return;
        if (pending.Count > 0) ShowNext();
        else
        {
            showing = false;
            visibility.alpha = 0;
            SetTimingStatus(lastTiming);
        }
    }

    [ContextMenu("Hide preview / clear queue")]
    public void Clear()
    {
        timingVisible = false;
        lastTiming = null;
        if (timingVisibility != null) timingVisibility.alpha = 0;
        pending.Clear();
        showing = false;
        elapsed = 0f;
        if (visibility != null)
        {
            visibility.alpha = 0f;
            visibility.blocksRaycasts = false;
            visibility.interactable = false;
        }
        if (content != null) content.localScale = restingScale;
    }

    [ContextMenu("Preview selected event in Edit Mode")]
    private void PreviewSelectedEvent()
    {
        if (Application.isPlaying) return;
        Clear();
        Enqueue(previewEvent, "Player / team");
        Advance(0.2f);
    }

    [ContextMenu("Preview queued match events (Play Mode)")]
    private void PreviewEvents()
    {
        if (!Application.isPlaying) return;
        foreach (MatchBannerEvent kind in Enum.GetValues(typeof(MatchBannerEvent))) Enqueue(kind, "Preview");
    }
}
