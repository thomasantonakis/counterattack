using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A cosmetic D12 sequence. It must not consume UnityEngine.Random.</summary>
public sealed class DuelDieView : MonoBehaviour
{
    public Image background;
    public Image face;
    public Outline border;
    public TMP_Text fallbackText;
    [Tooltip("Replace these six sprites to redesign the numeric faces, in order 1–6.")]
    public Sprite[] numericFaces = new Sprite[6];
    public Sprite jackpotFace;
    [Min(.03f)] public float secondsPerFace = .09f;
    private bool animating;
    private bool jackpotAllowed;
    private float nextFaceTime;
    private int faceIndex;
    public bool IsAnimating => animating;
    public bool ShowsJackpot { get; private set; }
    public int DisplayedRoll { get; private set; }

    public void SetTeamColor(Color color) => SetTeamColors(color, Color.white);

    public void SetTeamColors(Color color, Color secondary)
    {
        if (background != null) background.color = color;
        if (face != null) face.color = secondary;
        if (fallbackText != null) fallbackText.color = secondary;
        RefreshBorderContrast();
    }

    public void RefreshBorderContrast()
    {
        if (border == null || background == null) return;
        Color secondary = face != null ? face.color : fallbackText != null ? fallbackText.color : Color.white;
        Color surface = SurfaceColor();
        float primaryLuminance = Luminance(background.color);
        float surfaceLuminance = Luminance(surface);
        float contrast = (Mathf.Max(primaryLuminance, surfaceLuminance) + .05f)
            / (Mathf.Min(primaryLuminance, surfaceLuminance) + .05f);
        border.effectColor = contrast < 1.8f ? Color.black : secondary;
    }

    private Color SurfaceColor()
    {
        for (var parent = transform.parent; parent != null; parent = parent.parent)
        {
            var white = parent.Find("WhiteSurface") as RectTransform;
            if (white != null && white.gameObject.activeSelf && white.rect.Contains(white.InverseTransformPoint(transform.position)))
            {
                var surface = white.GetComponent<Image>();
                if (surface != null && surface.color.a > .9f) return surface.color;
            }
            var image = parent.GetComponent<Image>();
            if (image != null && image.color.a > .9f) return image.color;
        }
        return Color.white;
    }

    private static float Luminance(Color color)
    {
        Color linear = color.linear;
        return .2126f * linear.r + .7152f * linear.g + .0722f * linear.b;
    }

    public void Clear()
    {
        animating = false;
        ShowsJackpot = false;
        DisplayedRoll = 0;
        if (face != null) face.enabled = false;
        if (fallbackText != null) { fallbackText.enabled = true; fallbackText.text = "—"; }
    }

    public void Begin(bool allowsJackpot)
    {
        jackpotAllowed = allowsJackpot;
        animating = true;
        faceIndex = 0;
        Show(1, false);
        nextFaceTime = Time.unscaledTime + secondsPerFace;
    }

    public void Resolve(int roll, bool isJackpot)
    {
        animating = false;
        Show(roll, isJackpot);
    }

    private void Update()
    {
        if (!animating || Time.unscaledTime < nextFaceTime) return;
        faceIndex = (faceIndex + 1) % 12;
        var next = AnimationFace(faceIndex, jackpotAllowed);
        Show(next.roll, next.isJackpot);
        nextFaceTime = Time.unscaledTime + secondsPerFace;
    }

    public static (int roll, bool isJackpot) AnimationFace(int step, bool allowsJackpot)
    {
        int index = (step % 12 + 12) % 12;
        return (index % 6 + 1, allowsJackpot && index == 11);
    }

    private void Show(int roll, bool star)
    {
        DisplayedRoll = roll;
        ShowsJackpot = star;
        Sprite sprite = star ? jackpotFace
            : numericFaces != null && roll >= 1 && roll <= numericFaces.Length ? numericFaces[roll - 1] : null;
        if (face != null) { face.sprite = sprite; face.enabled = sprite != null; }
        if (fallbackText != null)
        {
            fallbackText.enabled = sprite == null;
            fallbackText.text = star ? "★" : roll.ToString();
        }
    }
}
