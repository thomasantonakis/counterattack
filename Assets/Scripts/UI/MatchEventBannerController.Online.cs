using System.Collections.Generic;
using System.Linq;
using CounterAttack.Online;
using UnityEngine;

public partial class MatchEventBannerController
{
    private readonly Queue<OnlineRoomBannerEvent> onlineEvents = new();
    private long publishedEventSequence;
    private long displayedEventSequence;

    private void RetainOnlineEvent(MatchBannerEvent kind, string detail)
    {
        int limit = OnlineRoomBannerPublication.DetailLimit;
        onlineEvents.Enqueue(new OnlineRoomBannerEvent
        {
            sequence = ++publishedEventSequence,
            kind = kind,
            detail = Bound(detail, limit)
        });
        if (onlineEvents.Count > OnlineRoomBannerPublication.EventLimit) onlineEvents.Dequeue();
    }

    public OnlineRoomBannerPublication CaptureOnlinePublication()
    {
        var snapshot = new OnlineRoomBannerPublication
        {
            events = onlineEvents.Select(entry => new OnlineRoomBannerEvent
                { sequence = entry.sequence, kind = entry.kind, detail = entry.detail }).ToArray()
        };
        if (timingVisible && timingCaption != null)
        {
            snapshot.timerCaption = Bound(timingCaption.text, OnlineRoomBannerPublication.CaptionLimit);
            snapshot.timerDetail = Bound(timingDetail?.text, OnlineRoomBannerPublication.DetailLimit);
            snapshot.timerWarning = timingCaption.color != timingCaptionColor;
        }
        return snapshot;
    }

    private static string Bound(string value, int limit)
        => value?.Length > limit ? value.Substring(0, limit) : value;

    public void ApplyOnlinePublication(OnlineRoomBannerPublication snapshot)
    {
        if (snapshot?.events != null)
        {
            foreach (var entry in snapshot.events)
            {
                if (entry.sequence <= displayedEventSequence) continue;
                displayedEventSequence = entry.sequence;
                Enqueue(entry.kind, entry.detail);
            }
        }

        if (timingVisibility == null || timingCaption == null || timingIcon == null) return;
        timingVisible = !string.IsNullOrEmpty(snapshot?.timerCaption);
        timingVisibility.alpha = timingVisible ? 1f : 0f;
        if (!timingVisible) return;
        timingVisibility.blocksRaycasts = false;
        timingVisibility.interactable = false;
        if (timingContent != null) timingContent.localScale = restingScale;
        timingIcon.sprite = clockArtwork;
        timingIcon.enabled = clockArtwork != null;
        timingCaption.text = snapshot.timerCaption;
        timingCaption.color = snapshot.timerWarning ? new Color(1f, 0.68f, 0.18f) : timingCaptionColor;
        if (timingDetail != null) timingDetail.text = snapshot.timerDetail;
    }
}
