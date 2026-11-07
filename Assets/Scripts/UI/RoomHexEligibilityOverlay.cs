using System;
using System.Collections.Generic;
using UnityEngine;

public enum RoomEligibilityKind { None, Shooting, Heading }

/// <summary>Held chord arbitration, separate from gameplay events and hex highlighting.</summary>
public sealed class RoomEligibilityChordState
{
    private bool shooting, heading, suppressUntilReleased;
    private RoomEligibilityKind latest;
    public RoomEligibilityKind Visible { get; private set; }
    public void Clear() { shooting = heading = false; suppressUntilReleased = true; Visible = RoomEligibilityKind.None; }
    public RoomEligibilityKind Update(bool modifiers, bool s, bool h, bool blocked)
    {
        if (blocked) { Clear(); return Visible; }
        if (suppressUntilReleased)
        {
            if (s || h) return RoomEligibilityKind.None;
            suppressUntilReleased = false;
        }
        bool nextS = modifiers && s, nextH = modifiers && h;
        if (nextS && !shooting) latest = RoomEligibilityKind.Shooting;
        if (nextH && !heading) latest = RoomEligibilityKind.Heading;
        shooting = nextS; heading = nextH;
        Visible = shooting && heading ? latest : shooting ? RoomEligibilityKind.Shooting : heading ? RoomEligibilityKind.Heading : RoomEligibilityKind.None;
        return Visible;
    }
}

public sealed class RoomHexEligibilityOverlay : MonoBehaviour
{
    [Serializable] public sealed class Icon
    {
        public Vector3Int coordinates;
        public SpriteRenderer shooting;
        public SpriteRenderer heading;
        public SpriteRenderer shootingOutline;
        public SpriteRenderer headingOutline;
    }
    [SerializeField] private HexGrid grid;
    [SerializeField] private Icon[] icons = Array.Empty<Icon>();
    [SerializeField] private float footprintPadding = 0.18f;
    private readonly RoomEligibilityChordState chords = new();
    private readonly Dictionary<PlayerToken, Renderer[]> tokenRenderers = new();
    public RoomEligibilityKind Visible => chords.Visible;

    private void Update()
    {
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var kind = chords.Update(ctrl && shift, Input.GetKey(KeyCode.S), Input.GetKey(KeyCode.H),
            !Application.isFocused || GameInputManager.IsPresentationModalActive || RoomInputOwnership.BlocksHover || MatchManager.Instance?.IsGameplayInputBlocked == true);
        Render(kind);
    }

    public void Render(RoomEligibilityKind kind)
    {
        Camera camera = Camera.main;
        bool snow = MatchManager.Instance?.gameData?.gameSettings?.weatherConditions?.StartsWith("Snow", StringComparison.OrdinalIgnoreCase) == true;
        foreach (Icon icon in icons)
        {
            HexCell cell = kind != RoomEligibilityKind.None && grid != null ? grid.GetHexCellAt(icon.coordinates) : null;
            if (cell != null && cell.GetOccupyingToken() != null) cell = null;
            bool shot = cell != null && cell.CanShootFrom && kind == RoomEligibilityKind.Shooting;
            bool head = cell != null && cell.CanHeadFrom && kind == RoomEligibilityKind.Heading;
            if (icon.shooting != null) icon.shooting.enabled = shot;
            if (icon.heading != null) icon.heading.enabled = head;
            if (icon.shootingOutline != null) icon.shootingOutline.enabled = shot;
            if (icon.headingOutline != null) icon.headingOutline.enabled = head;
            SpriteRenderer renderer = shot ? icon.shooting : head ? icon.heading : null;
            if (renderer == null || camera == null) continue;
            renderer.color = snow ? Color.black : Color.white;
            Vector3 position = cell.GetHexCenter() + Vector3.up * 0.12f;
            var token = cell.GetOccupyingToken();
            if (token != null)
            {
                // Move in screen-up space beyond every visible corner of the token.
                Vector3 screen = camera.WorldToScreenPoint(position);
                float top = screen.y;
                if (!tokenRenderers.TryGetValue(token, out Renderer[] parts))
                { parts = token.GetComponentsInChildren<Renderer>(); tokenRenderers[token] = parts; }
                foreach (Renderer part in parts)
                {
                    if (part == null || !part.enabled) continue;
                    Bounds bounds = part.bounds;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        Vector3 point = bounds.center + Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                        top = Mathf.Max(top, camera.WorldToScreenPoint(point).y);
                    }
                }
                float iconPixels = Mathf.Abs(camera.WorldToScreenPoint(position + camera.transform.up * (renderer.sprite.bounds.size.y * renderer.transform.lossyScale.y * 0.5f + footprintPadding)).y - screen.y);
                screen.y = top + iconPixels; position = camera.ScreenToWorldPoint(screen);
            }
            renderer.transform.SetPositionAndRotation(position, camera.transform.rotation);
            SpriteRenderer outline = shot ? icon.shootingOutline : icon.headingOutline;
            if (outline != null) outline.color = snow ? Color.white : Color.black;
        }
    }
    private void OnApplicationFocus(bool focused) { if (!focused) { chords.Clear(); Render(RoomEligibilityKind.None); } }
    private void OnDisable() { chords.Clear(); Render(RoomEligibilityKind.None); tokenRenderers.Clear(); }
}
