using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum TokenStyleFamily
{
    Plain,
    VerticalThreeColors
}

public enum TokenCenterStripeMode
{
    None,
    Solid,
    BreakUnderNumber
}

public sealed class TokenStyleDefinition
{
    public TokenStyleFamily family;
    public Color bodyColor;
    public Color accentColor;
    public Color numberColor;
    public TokenCenterStripeMode centerStripeMode;
    public Color centerStripeColor;
    public Color centerStripePlateColor;

    public static TokenStyleDefinition Plain(Color bodyColor, Color ringColor, Color numberColor)
    {
        return new TokenStyleDefinition
        {
            family = TokenStyleFamily.Plain,
            bodyColor = bodyColor,
            accentColor = ringColor,
            numberColor = numberColor,
            centerStripeMode = TokenCenterStripeMode.None,
            centerStripeColor = Color.clear,
            centerStripePlateColor = Color.clear
        };
    }

    public static TokenStyleDefinition VerticalThreeColors(
        Color bodyColor,
        Color accentColor,
        Color numberColor,
        TokenCenterStripeMode centerStripeMode = TokenCenterStripeMode.None,
        Color? centerStripeColor = null,
        Color? centerStripePlateColor = null)
    {
        Color resolvedCenterStripeColor = centerStripeColor ?? bodyColor;
        return new TokenStyleDefinition
        {
            family = TokenStyleFamily.VerticalThreeColors,
            bodyColor = bodyColor,
            accentColor = accentColor,
            numberColor = numberColor,
            centerStripeMode = centerStripeMode,
            centerStripeColor = resolvedCenterStripeColor,
            centerStripePlateColor = centerStripePlateColor ?? resolvedCenterStripeColor
        };
    }

    public string GetCacheKey()
    {
        return $"{family}_{centerStripeMode}_{ColorUtility.ToHtmlStringRGBA(bodyColor)}_{ColorUtility.ToHtmlStringRGBA(accentColor)}_{ColorUtility.ToHtmlStringRGBA(numberColor)}_{ColorUtility.ToHtmlStringRGBA(centerStripeColor)}_{ColorUtility.ToHtmlStringRGBA(centerStripePlateColor)}";
    }
}

public class PlayerTokenVisuals : MonoBehaviour
{
    // World-space offset of the generated top-face quad above the token body.
    private const float TopDecalHeight = 1.01f;
    // Overall scale of the generated top-face quad relative to the token top.
    private const float TopDecalScale = 0.9f;
    // Resolution of the generated token-face texture.
    private const int DecalTextureSize = 256;

    // Plain style: outer radius of the thin ring around the jersey number, in normalized face-space (-1 to 1).
    private const float PlainRingRadius = 0.85f;
    // Plain style: thickness of that ring.
    private const float PlainRingThickness = 0.04f;

    // VerticalThreeColors: radius of the inner circular face area. Outside this radius, the token body color is shown.
    private const float VerticalInnerDiscRadius = 0.9f;
    // VerticalThreeColors: half-width of each vertical stripe. Increase for thicker stripes.
    private const float VerticalStripeHalfWidth = 0.25f;
    // VerticalThreeColors: horizontal distance of the left/right stripes from the center. Increase to push them farther apart.
    private const float VerticalStripeOffset = 0.4f;
    // BreakUnderNumber: half-height of the gap removed from the center stripe around the number.
    // Larger values create a taller broken opening in the stripe.
    private const float VerticalCenterStripeGapHalfHeight = 0.5f;
    // BreakUnderNumber: half-height of the colored plate behind the jersey number inside that gap.
    // Keep this at or below the gap height unless you intentionally want the plate to fill most of the face.
    private const float VerticalNumberPlateHalfHeight = 0.9f;

    private static readonly Dictionary<string, Material> SharedDecalMaterials = new Dictionary<string, Material>();

    private MeshRenderer bodyRenderer;
    private MeshRenderer topDecalRenderer;
    private Transform topDecalTransform;
    private MaterialPropertyBlock bodyPropertyBlock;

    private void Awake()
    {
        bodyRenderer = GetComponent<MeshRenderer>();
        EnsureBodyPropertyBlock();
        EnsureTopDecal();
    }

    private void OnValidate()
    {
        bodyRenderer = GetComponent<MeshRenderer>();
    }

    public void ApplyStyle(TokenStyleDefinition style)
    {
        if (style == null)
        {
            return;
        }

        if (bodyRenderer == null)
        {
            bodyRenderer = GetComponent<MeshRenderer>();
        }

        EnsureTopDecal();
        ApplyBodyColor(style.bodyColor);
        topDecalRenderer.sharedMaterial = GetOrCreateDecalMaterial(style);
    }

    public void ApplyNumberStyle(TextMeshPro numberText, TokenStyleDefinition style)
    {
        if (numberText == null || style == null)
        {
            return;
        }

        numberText.color = style.numberColor;
        numberText.alpha = 1f;
        if (style.family == TokenStyleFamily.Plain)
        {
            numberText.fontSize = 2.75f;
            return;
        }

        numberText.fontSize = 2f;
    }

    private void ApplyBodyColor(Color color)
    {
        if (bodyRenderer == null)
        {
            return;
        }

        EnsureBodyPropertyBlock();
        bodyRenderer.GetPropertyBlock(bodyPropertyBlock);
        bodyPropertyBlock.SetColor("_Color", color);
        bodyPropertyBlock.SetColor("_BaseColor", color);
        bodyRenderer.SetPropertyBlock(bodyPropertyBlock);
    }

    private void EnsureBodyPropertyBlock()
    {
        if (bodyPropertyBlock == null)
        {
            bodyPropertyBlock = new MaterialPropertyBlock();
        }
    }

    private void EnsureTopDecal()
    {
        if (topDecalTransform != null && topDecalRenderer != null)
        {
            return;
        }

        var existingChild = transform.Find("TopDecal");
        if (existingChild != null)
        {
            topDecalTransform = existingChild;
            topDecalRenderer = existingChild.GetComponent<MeshRenderer>();
            return;
        }

        GameObject topDecal = GameObject.CreatePrimitive(PrimitiveType.Quad);
        topDecal.name = "TopDecal";
        topDecal.transform.SetParent(transform, false);
        topDecal.transform.localPosition = new Vector3(0f, TopDecalHeight, 0f);
        topDecal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        topDecal.transform.localScale = new Vector3(TopDecalScale, TopDecalScale, TopDecalScale);
        topDecal.layer = gameObject.layer;

        Collider decalCollider = topDecal.GetComponent<Collider>();
        if (decalCollider != null)
        {
            Destroy(decalCollider);
        }

        topDecalTransform = topDecal.transform;
        topDecalRenderer = topDecal.GetComponent<MeshRenderer>();
        topDecalRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        topDecalRenderer.receiveShadows = false;
    }

    private static Material GetOrCreateDecalMaterial(TokenStyleDefinition style)
    {
        string cacheKey = style.GetCacheKey();
        if (SharedDecalMaterials.TryGetValue(cacheKey, out Material material))
        {
            return material;
        }

        Shader decalShader = Shader.Find("Unlit/Transparent");
        if (decalShader == null)
        {
            decalShader = Shader.Find("Sprites/Default");
        }

        material = new Material(decalShader)
        {
            name = $"TokenDecal_{cacheKey}"
        };
        material.mainTexture = BuildDecalTexture(style);
        SharedDecalMaterials[cacheKey] = material;
        return material;
    }

    private static Texture2D BuildDecalTexture(TokenStyleDefinition style)
    {
        Texture2D texture = new Texture2D(DecalTextureSize, DecalTextureSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = $"TokenDecalTexture_{style.GetCacheKey()}"
        };

        Color[] pixels = new Color[DecalTextureSize * DecalTextureSize];
        for (int y = 0; y < DecalTextureSize; y++)
        {
            for (int x = 0; x < DecalTextureSize; x++)
            {
                pixels[(y * DecalTextureSize) + x] = GetDecalPixelColor(style, x, y);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static Color GetDecalPixelColor(TokenStyleDefinition style, int x, int y)
    {
        float u = ((x + 0.5f) / DecalTextureSize * 2f) - 1f;
        float v = ((y + 0.5f) / DecalTextureSize * 2f) - 1f;
        float distance = Mathf.Sqrt((u * u) + (v * v));

        if (distance > 0.98f)
        {
            return Color.clear;
        }

        switch (style.family)
        {
            case TokenStyleFamily.Plain:
                return GetPlainPixelColor(style, distance);

            case TokenStyleFamily.VerticalThreeColors:
                return GetVerticalThreeColorsPixelColor(style, u, v, distance);

            default:
                return style.bodyColor;
        }
    }

    private static Color GetPlainPixelColor(TokenStyleDefinition style, float distance)
    {
        float ringOuterRadius = PlainRingRadius;
        float ringInnerRadius = ringOuterRadius - PlainRingThickness;

        if (distance <= ringOuterRadius && distance >= ringInnerRadius)
        {
            return style.accentColor;
        }

        return style.bodyColor;
    }

    private static Color GetVerticalThreeColorsPixelColor(TokenStyleDefinition style, float u, float v, float distance)
    {
        if (distance > 0.98f)
        {
            return Color.clear;
        }
        
        if (distance > VerticalInnerDiscRadius)
        {
            return style.bodyColor;
        }

        if (Mathf.Abs(u - VerticalStripeOffset) <= VerticalStripeHalfWidth
            || Mathf.Abs(u + VerticalStripeOffset) <= VerticalStripeHalfWidth)
        {
            return style.bodyColor;
        }

        if (TryGetCenterStripeColor(style, u, v, out Color centerStripeColor))
        {
            return centerStripeColor;
        }

        return style.accentColor;
    }

    private static bool TryGetCenterStripeColor(TokenStyleDefinition style, float u, float v, out Color centerStripeColor)
    {
        centerStripeColor = Color.clear;

        if (style.centerStripeMode == TokenCenterStripeMode.None)
        {
            return false;
        }

        if (Mathf.Abs(u) > VerticalStripeHalfWidth)
        {
            return false;
        }

        if (style.centerStripeMode == TokenCenterStripeMode.Solid)
        {
            centerStripeColor = style.centerStripeColor;
            return true;
        }

        if (style.centerStripeMode == TokenCenterStripeMode.BreakUnderNumber)
        {
            if (Mathf.Abs(v) > VerticalCenterStripeGapHalfHeight)
            {
                centerStripeColor = style.centerStripeColor;
                return true;
            }

            if (Mathf.Abs(v) <= VerticalNumberPlateHalfHeight)
            {
                centerStripeColor = style.centerStripePlateColor;
                return true;
            }
        }

        return false;
    }

    private static bool ApproximatelySameColor(Color a, Color b)
    {
        const float tolerance = 0.01f;
        return Mathf.Abs(a.r - b.r) <= tolerance
            && Mathf.Abs(a.g - b.g) <= tolerance
            && Mathf.Abs(a.b - b.b) <= tolerance
            && Mathf.Abs(a.a - b.a) <= tolerance;
    }
}
