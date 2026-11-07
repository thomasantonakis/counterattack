using UnityEngine;
using TMPro;

/// <summary>Uses authored copies of the normal card prefabs, independent of Stats.</summary>
public sealed class RoomInspectionCard : MonoBehaviour
{
    [System.Serializable] public sealed class AttributeBadge
    {
        public TMP_Text value;
        public UnityEngine.UI.Image background;
    }
    public static RoomInspectionCard Instance { get; private set; }
    [SerializeField] private PlayerCard outfieldCard;
    [SerializeField] private GoalkeeperCard goalkeeperCard;
    [SerializeField] private RectTransform cardArea;
    [SerializeField] private MatchStatsUI statsUI;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private AttributeBadge[] outfieldBadges;
    [SerializeField] private AttributeBadge[] goalkeeperBadges;
    public CameraController CameraController => cameraController;
    public PlayerToken LockedPlayer { get; private set; }
    public bool IsOpen { get; private set; }

    private void Awake() { Instance = this; Close(); }
    private void OnDisable() { Close(); if (Instance == this) Instance = null; }

    public void InspectPreview(PlayerToken token)
    {
        if (IsOpen || token == null || RoomInputOwnership.ReservePauseEscape
            || GameInputManager.IsPresentationModalActive || MatchManager.Instance?.IsGameplayInputBlocked == true) return;
        Toggle(token);
    }

    public void Toggle(PlayerToken token)
    {
        if (IsOpen) { Close(); return; }
        if (token == null || (token.IsGoalKeeper ? goalkeeperCard == null : outfieldCard == null)) return;
        LockedPlayer = token;
        IsOpen = true;
        string label = statsUI != null ? statsUI.GetInspectionNationality(token) : string.Empty;
        if (token.IsGoalKeeper) { goalkeeperCard.UpdateFromToken(token, label); goalkeeperCard.gameObject.SetActive(true); }
        else { outfieldCard.UpdateFromToken(token, label); outfieldCard.gameObject.SetActive(true); }
        RefreshBadges(token.IsGoalKeeper ? goalkeeperBadges : outfieldBadges);
        Resize();
    }

    private static void RefreshBadges(AttributeBadge[] badges)
    {
        if (badges == null) return;
        foreach (var badge in badges)
            if (badge.value != null && badge.background != null && int.TryParse(badge.value.text, out int value))
                badge.background.color = MatchStatsUI.GetInspectionAttributeColor(value);
    }

    public void Close()
    {
        LockedPlayer = null;
        IsOpen = false;
        if (outfieldCard != null) outfieldCard.gameObject.SetActive(false);
        if (goalkeeperCard != null) goalkeeperCard.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (cardArea != null && cardArea.TryGetComponent<CanvasGroup>(out var group))
            group.alpha = GameInputManager.IsPresentationModalActive ? 0f : 1f;
        if (IsOpen) Resize();
    }
    private void Resize()
    {
        if (cardArea == null) return;
        ScaleCard(outfieldCard); ScaleCard(goalkeeperCard);
        var badges = goalkeeperCard != null && goalkeeperCard.gameObject.activeSelf ? goalkeeperBadges : outfieldBadges;
        if (badges != null)
            foreach (var badge in badges)
                if (badge.value != null && badge.background != null)
                    badge.background.rectTransform.position = badge.value.rectTransform.TransformPoint(badge.value.rectTransform.rect.center);
    }
    private void ScaleCard(Component card)
    {
        if (card == null) return;
        var rect = (RectTransform)card.transform;
        // cardArea spans 60% of the full Canvas height, centered on both axes.
        float scale = cardArea.rect.height / Mathf.Max(1f, rect.rect.height);
        rect.localScale = Vector3.one * scale;
    }
}
