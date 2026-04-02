using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class TokenFacePreviewUtility
{
    private const int DefaultTextureSize = 256;

    // Top-face outer circle radius, in normalized face-space (-1 to 1).
    private const float FaceCircleRadius = 0.90f;
    // Thickness of the circular ring separating the outer body color from the inner face area.
    private const float FaceRingThickness = 0.05f;
    // Effective radius of the inner face area after the ring is removed.
    private const float FaceInnerRadius = FaceCircleRadius - FaceRingThickness;
    // BreakUnderNumber: lower edge of the opening removed from the center stripe around the number.
    // More negative values make the lower broken gap taller and shorten the lower stripe segment.
    private const float VerticalCenterStripeGapBottom = -0.60f;
    // BreakUnderNumber: upper edge of the opening removed from the center stripe around the number.
    // Lower values leave a longer visible stripe segment above the number.
    private const float VerticalCenterStripeGapTop = 0.48f;
    // Sleeved: lowest point where the angled sleeve wedges are allowed to appear.
    private const float SleeveMinV = -0.06f;
    // Sleeved: diagonal inner-edge intercept for the sleeve wedges.
    private const float SleeveDiagonalBaseX = 0.60f;
    // Sleeved: diagonal inner-edge slope for the sleeve wedges.
    private const float SleeveDiagonalSlope = 0.45f;
    // Roma: top stripe vertical bounds.
    private const float RomaTopStripeMinV = 0.50f;
    private const float RomaTopStripeMaxV = 0.64f;
    // Roma: second stripe directly under the top stripe.
    private const float RomaBottomStripeMinV = 0.38f;
    private const float RomaBottomStripeMaxV = 0.50f;
    // UI preview offset multiplier applied to family-specific face offsets.
    private const float PreviewNumberOffsetPixelsPerUnit = 50f;

    private static readonly Dictionary<string, Texture2D> SharedFaceTextures = new Dictionary<string, Texture2D>();
    private static readonly Dictionary<int, TMP_FontAsset> OriginalFontAssets = new Dictionary<int, TMP_FontAsset>();
    private static TMP_FontAsset getafeFontAsset;

    public static Texture2D GetOrCreateFaceTexture(TokenStyleDefinition style, int textureSize = DefaultTextureSize)
    {
        if (style == null)
        {
            return null;
        }

        string cacheKey = $"{style.GetCacheKey()}_{textureSize}";
        if (SharedFaceTextures.TryGetValue(cacheKey, out Texture2D texture))
        {
            return texture;
        }

        texture = BuildFaceTexture(style, textureSize);
        SharedFaceTextures[cacheKey] = texture;
        return texture;
    }

    public static void ApplyNumberStyle(TMP_Text numberText, TokenStyleDefinition style, float plainFontSize, float verticalFontSize)
    {
        if (numberText == null || style == null)
        {
            return;
        }

        numberText.color = style.numberColor;
        numberText.alpha = 1f;
        numberText.fontStyle = FontStyles.Bold;
        numberText.font = ResolveFontAsset(numberText, style.numberFont);
        bool usesPlainNumberSizing =
            style.family == TokenStyleFamily.Plain ||
            style.family == TokenStyleFamily.Sleeved ||
            style.family == TokenStyleFamily.SingleStriped ||
            style.family == TokenStyleFamily.Roma;
        numberText.fontSize = usesPlainNumberSizing ? plainFontSize : verticalFontSize;
        ApplyNumberOffset(numberText, style);
    }

    private static Texture2D BuildFaceTexture(TokenStyleDefinition style, int textureSize)
    {
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = $"TokenDecalTexture_{style.GetCacheKey()}_{textureSize}"
        };

        Color[] pixels = new Color[textureSize * textureSize];
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                pixels[(y * textureSize) + x] = GetDecalPixelColor(style, x, y, textureSize);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Color GetDecalPixelColor(TokenStyleDefinition style, int x, int y, int textureSize)
    {
        float u = ((x + 0.5f) / textureSize * 2f) - 1f;
        float v = ((y + 0.5f) / textureSize * 2f) - 1f;
        float distance = Mathf.Sqrt((u * u) + (v * v));

        if (distance > 0.98f)
        {
            return Color.clear;
        }

        switch (style.family)
        {
            case TokenStyleFamily.Plain:
                return GetPlainPixelColor(style, distance);

            case TokenStyleFamily.VerticalStripes:
                return GetVerticalStripesPixelColor(style, u, v, distance);

            case TokenStyleFamily.HorizontalStripes:
                // Horizontal stripes are the vertical design rotated 90 degrees counterclockwise.
                return GetVerticalStripesPixelColor(style, v, -u, distance);

            case TokenStyleFamily.Sleeved:
                return GetSleevedPixelColor(style, u, v, distance);

            case TokenStyleFamily.SingleStriped:
                return GetSingleStripedPixelColor(style, u, v, distance);

            case TokenStyleFamily.Roma:
                return GetRomaPixelColor(style, u, v, distance);

            default:
                return style.bodyColor;
        }
    }

    private static Color GetPlainPixelColor(TokenStyleDefinition style, float distance)
    {
        if (distance > FaceCircleRadius)
        {
            return style.bodyColor;
        }

        if (distance > FaceInnerRadius)
        {
            return style.ringColor;
        }

        return style.accentColor;
    }

    private static Color GetVerticalStripesPixelColor(TokenStyleDefinition style, float u, float v, float distance)
    {
        if (distance > 0.98f)
        {
            return Color.clear;
        }

        if (distance > FaceCircleRadius)
        {
            return style.bodyColor;
        }

        if (distance > FaceInnerRadius)
        {
            return style.ringColor;
        }

        if (ShouldRevealBackgroundThroughCenterStripe(style, u, v))
        {
            return GetBrokenCenterBackgroundColor(style);
        }

        return GetVerticalStripeBandColor(style, u);
    }

    private static Color GetSleevedPixelColor(TokenStyleDefinition style, float u, float v, float distance)
    {
        if (distance > 0.98f)
        {
            return Color.clear;
        }

        if (distance > FaceCircleRadius)
        {
            return style.bodyColor;
        }

        if (IsInsideSleeveWedge(u, v))
        {
            return style.ringColor;
        }

        return style.bodyColor;
    }

    private static Color GetSingleStripedPixelColor(TokenStyleDefinition style, float u, float v, float distance)
    {
        if (distance > 0.98f)
        {
            return Color.clear;
        }

        if (distance > FaceCircleRadius)
        {
            return style.bodyColor;
        }

        float stripeHalfWidth = GetNormalizedStripeHalfWidth(style.stripeWidth);
        float stripeDirectionRadians = style.stripeDirectionDegrees * Mathf.Deg2Rad;
        Vector2 stripeDirection = new Vector2(Mathf.Cos(stripeDirectionRadians), -Mathf.Sin(stripeDirectionRadians));
        Vector2 stripeNormal = new Vector2(-stripeDirection.y, stripeDirection.x);
        float signedDistance = Vector2.Dot(new Vector2(u, v), stripeNormal);

        if (Mathf.Abs(signedDistance) <= stripeHalfWidth)
        {
            return style.accentColor;
        }

        return style.bodyColor;
    }

    private static Color GetRomaPixelColor(TokenStyleDefinition style, float u, float v, float distance)
    {
        if (distance > 0.98f)
        {
            return Color.clear;
        }

        if (distance > FaceCircleRadius)
        {
            return style.bodyColor;
        }

        if (v >= RomaTopStripeMinV && v <= RomaTopStripeMaxV)
        {
            return style.accentColor;
        }

        if (v >= RomaBottomStripeMinV && v <= RomaBottomStripeMaxV)
        {
            return style.ringColor;
        }

        return style.bodyColor;
    }

    private static float GetNormalizedStripeHalfWidth(float rawStripeWidth)
    {
        float normalizedWidth = rawStripeWidth <= 1f ? rawStripeWidth : rawStripeWidth / 100f;
        normalizedWidth = Mathf.Clamp01(normalizedWidth);
        return normalizedWidth * FaceCircleRadius;
    }

    private static void ApplyNumberOffset(TMP_Text numberText, TokenStyleDefinition style)
    {
        if (Mathf.Approximately(style.numberFaceOffset, 0f))
        {
            if (numberText is TextMeshProUGUI uguiText)
            {
                uguiText.rectTransform.anchoredPosition = Vector2.zero;
            }
            else
            {
                Vector3 localPosition = numberText.transform.localPosition;
                localPosition.z = 0f;
                numberText.transform.localPosition = localPosition;
            }
            return;
        }

        if (numberText is TextMeshProUGUI previewText)
        {
            previewText.rectTransform.anchoredPosition = new Vector2(0f, style.numberFaceOffset * PreviewNumberOffsetPixelsPerUnit);
            return;
        }

        Vector3 textLocalPosition = numberText.transform.localPosition;
        textLocalPosition.z = style.numberFaceOffset;
        numberText.transform.localPosition = textLocalPosition;
    }

    private static TMP_FontAsset ResolveFontAsset(TMP_Text numberText, TokenNumberFont fontKind)
    {
        int instanceId = numberText.GetInstanceID();
        if (!OriginalFontAssets.ContainsKey(instanceId))
        {
            OriginalFontAssets[instanceId] = numberText.font;
        }

        switch (fontKind)
        {
            case TokenNumberFont.Getafe:
                return GetOrCreateGetafeFontAsset() ?? GetDefaultFontAsset(instanceId, numberText);
            case TokenNumberFont.Default:
            default:
                return GetDefaultFontAsset(instanceId, numberText);
        }
    }

    private static TMP_FontAsset GetDefaultFontAsset(int instanceId, TMP_Text numberText)
    {
        if (OriginalFontAssets.TryGetValue(instanceId, out TMP_FontAsset originalFont) && originalFont != null)
        {
            return originalFont;
        }

        if (TMP_Settings.defaultFontAsset != null)
        {
            return TMP_Settings.defaultFontAsset;
        }

        return numberText.font;
    }

    private static TMP_FontAsset GetOrCreateGetafeFontAsset()
    {
        if (getafeFontAsset != null)
        {
            return getafeFontAsset;
        }

        // # @thomas To wire a new font from the JSON:
        // 1. Add a new TokenNumberFont enum member in PlayerTokenVisuals.cs.
        // 2. Use the same string in the JSON config "font" field.
        // 3. Load the TTF/OTF from Resources here and create/cache a TMP font asset for it.
        Font sourceFont = Resources.Load<Font>("Fonts/Getafe/GetafeDemo");
        if (sourceFont == null)
        {
            Debug.LogWarning("Could not load GetafeDemo.ttf from Resources/Fonts/Getafe.");
            return null;
        }

        getafeFontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            512,
            512);

        if (getafeFontAsset != null)
        {
            getafeFontAsset.name = "Getafe Dynamic SDF";
        }

        return getafeFontAsset;
    }

    private static bool IsInsideSleeveWedge(float u, float v)
    {
        if (v < SleeveMinV)
        {
            return false;
        }

        float absoluteU = Mathf.Abs(u);
        float sleeveInnerEdge = SleeveDiagonalBaseX - (SleeveDiagonalSlope * v);
        return absoluteU >= sleeveInnerEdge;
    }

    private static Color GetVerticalStripeBandColor(TokenStyleDefinition style, float u)
    {
        float stripeWidth = (FaceInnerRadius * 2f) / 5f;
        float normalizedU = Mathf.Clamp(u, -FaceInnerRadius, FaceInnerRadius);
        float bandStart = -FaceInnerRadius;

        if (normalizedU < bandStart + stripeWidth)
        {
            return style.leftStripeColor;
        }

        if (normalizedU < bandStart + (stripeWidth * 2f))
        {
            return style.leftMidStripeColor;
        }

        if (normalizedU < bandStart + (stripeWidth * 3f))
        {
            return style.centerStripeColor;
        }

        if (normalizedU < bandStart + (stripeWidth * 4f))
        {
            return style.rightMidStripeColor;
        }

        return style.rightStripeColor;
    }

    private static bool ShouldRevealBackgroundThroughCenterStripe(TokenStyleDefinition style, float u, float v)
    {
        if (style.centerStripeMode != TokenCenterStripeMode.BreakUnderNumber)
        {
            return false;
        }

        float stripeWidth = (FaceInnerRadius * 2f) / 5f;
        float centerStripeLeft = -FaceInnerRadius + (stripeWidth * 2f);
        float centerStripeRight = centerStripeLeft + stripeWidth;
        bool isInsideCenterStripe = u >= centerStripeLeft && u <= centerStripeRight;
        if (!isInsideCenterStripe)
        {
            return false;
        }

        return v >= VerticalCenterStripeGapBottom && v <= VerticalCenterStripeGapTop;
    }

    private static Color GetBrokenCenterBackgroundColor(TokenStyleDefinition style)
    {
        // BreakUnderNumber means the interrupted center stripe reveals the inner-face background
        // used by the left/right mid bands, not the outer token body.
        return style.leftMidStripeColor;
    }
}
