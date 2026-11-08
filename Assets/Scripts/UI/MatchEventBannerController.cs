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

    private void Update() => Advance(Time.unscaledDeltaTime);

    public void Enqueue(MatchBannerEvent kind, string description)
    {
        if (visibility == null || caption == null || icon == null) return;
        pending.Enqueue((kind, description));
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
        else Clear();
    }

    [ContextMenu("Hide preview / clear queue")]
    public void Clear()
    {
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
