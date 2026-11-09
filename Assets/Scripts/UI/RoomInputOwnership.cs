using UnityEngine;

/// <summary>Presentation owns physical input; inspection never suspends AI or actions.</summary>
public static class RoomInputOwnership
{
    public static bool IntroActive => RoomMatchIntro.Instance != null && RoomMatchIntro.Instance.IsHoldingMatch;
    public static bool CardOpen => RoomInspectionCard.Instance != null && RoomInspectionCard.Instance.IsOpen;
    public static bool BlocksHover => IntroActive || CardOpen || RulesViewer.IsOpen;
    public static bool BlocksCamera => IntroActive || RulesViewer.IsOpen;
    public static bool ReservePauseEscape => IntroActive || RulesViewer.ReserveEscape || RoomMatchIntro.Instance?.IsConsumingSkipRelease == true;

    public static bool ConsumeKey(KeyPressData key, bool physical, PlayerToken hovered)
    {
        if (key == null || key.isConsumed) return key?.isConsumed == true;
        if (RulesViewer.IsOpen && physical) { key.Consume(nameof(RulesViewer)); return true; }
        if (IntroActive || RoomMatchIntro.Instance?.IsConsumingSkipRelease == true)
        {
            if (key.key == KeyCode.Escape) RoomMatchIntro.Instance.Skip();
            key.Consume(nameof(RoomMatchIntro)); return true;
        }
        if (GameInputManager.IsPresentationModalActive) return false;
        if (key.ctrl && key.shift && key.key is KeyCode.S or KeyCode.H)
        {
            key.Consume(nameof(RoomHexEligibilityOverlay)); return true;
        }
        if (key.key == KeyCode.Space && RoomInspectionCard.Instance != null)
        {
            RoomInspectionCard.Instance.Toggle(hovered);
            key.Consume(nameof(RoomInspectionCard)); return true;
        }
        if (!physical || !CardOpen) return false;
        if (key.key == KeyCode.Escape) return false;
        var camera = RoomInspectionCard.Instance.CameraController;
        if (camera != null && camera.TryHandlePresetKey(key)) return true;
        key.Consume(nameof(RoomInspectionCard)); return true;
    }
}
