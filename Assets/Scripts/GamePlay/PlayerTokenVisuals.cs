using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum TokenStyleFamily
{
    Plain,
    VerticalStripes,
    HorizontalStripes,
    Sleeved,
    SingleStriped,
    Roma
}

public enum TokenCenterStripeMode
{
    None,
    Solid,
    BreakUnderNumber
}

public enum TokenNumberFont
{
    Default,
    Getafe
}

public sealed class TokenStyleDefinition
{
    public TokenStyleFamily family;
    public Color bodyColor;
    // Plain only: fill color inside the top-face circle.
    public Color accentColor;
    // Shared: ring drawn around the top-face inner circle.
    public Color ringColor;
    // VerticalStripes only: colors for the five vertical bands inside the top-face circle.
    public Color leftStripeColor;
    public Color leftMidStripeColor;
    public Color centerStripeColor;
    public Color rightMidStripeColor;
    public Color rightStripeColor;
    public float stripeWidth;
    public float stripeDirectionDegrees;
    public float numberFaceOffset;
    public Color numberColor;
    public TokenCenterStripeMode centerStripeMode;
    public TokenNumberFont numberFont;

    public static TokenStyleDefinition Plain(
        Color bodyColor,
        Color accentColor,
        Color ringColor,
        Color numberColor,
        TokenNumberFont numberFont = TokenNumberFont.Default)
    {
        return new TokenStyleDefinition
        {
            family = TokenStyleFamily.Plain,
            bodyColor = bodyColor,
            accentColor = accentColor,
            ringColor = ringColor,
            leftStripeColor = Color.clear,
            leftMidStripeColor = Color.clear,
            centerStripeColor = Color.clear,
            rightMidStripeColor = Color.clear,
            rightStripeColor = Color.clear,
            stripeWidth = 0f,
            stripeDirectionDegrees = 0f,
            numberFaceOffset = 0f,
            numberColor = numberColor,
            centerStripeMode = TokenCenterStripeMode.None,
            numberFont = numberFont
        };
    }

    public static TokenStyleDefinition VerticalStripes(
        Color bodyColor,
        Color ringColor,
        Color leftStripeColor,
        Color leftMidStripeColor,
        Color centerStripeColor,
        Color rightMidStripeColor,
        Color rightStripeColor,
        TokenCenterStripeMode centerStripeMode,
        Color numberColor,
        TokenNumberFont numberFont = TokenNumberFont.Default)
    {
        return new TokenStyleDefinition
        {
            family = TokenStyleFamily.VerticalStripes,
            bodyColor = bodyColor,
            accentColor = Color.clear,
            ringColor = ringColor,
            leftStripeColor = leftStripeColor,
            leftMidStripeColor = leftMidStripeColor,
            centerStripeColor = centerStripeColor,
            rightMidStripeColor = rightMidStripeColor,
            rightStripeColor = rightStripeColor,
            stripeWidth = 0f,
            stripeDirectionDegrees = 0f,
            numberFaceOffset = 0f,
            numberColor = numberColor,
            centerStripeMode = centerStripeMode,
            numberFont = numberFont
        };
    }

    public static TokenStyleDefinition HorizontalStripes(
        Color bodyColor,
        Color ringColor,
        Color topStripeColor,
        Color topMidStripeColor,
        Color centerStripeColor,
        Color bottomMidStripeColor,
        Color bottomStripeColor,
        TokenCenterStripeMode centerStripeMode,
        Color numberColor,
        TokenNumberFont numberFont = TokenNumberFont.Default)
    {
        return new TokenStyleDefinition
        {
            family = TokenStyleFamily.HorizontalStripes,
            bodyColor = bodyColor,
            accentColor = Color.clear,
            ringColor = ringColor,
            leftStripeColor = topStripeColor,
            leftMidStripeColor = topMidStripeColor,
            centerStripeColor = centerStripeColor,
            rightMidStripeColor = bottomMidStripeColor,
            rightStripeColor = bottomStripeColor,
            stripeWidth = 0f,
            stripeDirectionDegrees = 0f,
            numberFaceOffset = 0f,
            numberColor = numberColor,
            centerStripeMode = centerStripeMode,
            numberFont = numberFont
        };
    }

    public static TokenStyleDefinition Sleeved(
        Color bodyColor,
        Color sleeveColor,
        Color numberColor,
        TokenNumberFont numberFont = TokenNumberFont.Default)
    {
        return new TokenStyleDefinition
        {
            family = TokenStyleFamily.Sleeved,
            bodyColor = bodyColor,
            accentColor = bodyColor,
            ringColor = sleeveColor,
            leftStripeColor = Color.clear,
            leftMidStripeColor = Color.clear,
            centerStripeColor = Color.clear,
            rightMidStripeColor = Color.clear,
            rightStripeColor = Color.clear,
            stripeWidth = 0f,
            stripeDirectionDegrees = 0f,
            numberFaceOffset = 0f,
            numberColor = numberColor,
            centerStripeMode = TokenCenterStripeMode.None,
            numberFont = numberFont
        };
    }

    public static TokenStyleDefinition SingleStriped(
        Color bodyColor,
        Color stripeColor,
        Color numberColor,
        float stripeWidth,
        float stripeDirectionDegrees,
        TokenNumberFont numberFont = TokenNumberFont.Default)
    {
        return new TokenStyleDefinition
        {
            family = TokenStyleFamily.SingleStriped,
            bodyColor = bodyColor,
            accentColor = stripeColor,
            ringColor = stripeColor,
            leftStripeColor = Color.clear,
            leftMidStripeColor = Color.clear,
            centerStripeColor = Color.clear,
            rightMidStripeColor = Color.clear,
            rightStripeColor = Color.clear,
            stripeWidth = stripeWidth,
            stripeDirectionDegrees = stripeDirectionDegrees,
            numberFaceOffset = 0f,
            numberColor = numberColor,
            centerStripeMode = TokenCenterStripeMode.None,
            numberFont = numberFont
        };
    }

    public static TokenStyleDefinition Roma(
        Color bodyColor,
        Color topStripeColor,
        Color bottomStripeColor,
        Color numberColor,
        TokenNumberFont numberFont = TokenNumberFont.Default)
    {
        return new TokenStyleDefinition
        {
            family = TokenStyleFamily.Roma,
            bodyColor = bodyColor,
            accentColor = topStripeColor,
            ringColor = bottomStripeColor,
            leftStripeColor = Color.clear,
            leftMidStripeColor = Color.clear,
            centerStripeColor = Color.clear,
            rightMidStripeColor = Color.clear,
            rightStripeColor = Color.clear,
            stripeWidth = 0f,
            stripeDirectionDegrees = 0f,
            numberFaceOffset = -0.1f,
            numberColor = numberColor,
            centerStripeMode = TokenCenterStripeMode.None,
            numberFont = numberFont
        };
    }

    // Legacy helper that maps the older "body + center fill" model onto the richer 5-band layout.
    public static TokenStyleDefinition VerticalThreeColors(
        Color bodyColor,
        Color accentColor,
        Color numberColor,
        TokenCenterStripeMode centerStripeMode = TokenCenterStripeMode.None,
        Color? centerStripeColor = null,
        TokenNumberFont numberFont = TokenNumberFont.Default)
    {
        Color resolvedCenterStripeColor = centerStripeColor ?? accentColor;
        return VerticalStripes(
            bodyColor,
            bodyColor,
            bodyColor,
            accentColor,
            resolvedCenterStripeColor,
            accentColor,
            bodyColor,
            centerStripeMode,
            numberColor,
            numberFont);
    }

    // Rotated counterpart of VerticalThreeColors using the same 5-band model.
    public static TokenStyleDefinition HorizontalThreeColors(
        Color bodyColor,
        Color accentColor,
        Color numberColor,
        TokenCenterStripeMode centerStripeMode = TokenCenterStripeMode.None,
        Color? centerStripeColor = null,
        TokenNumberFont numberFont = TokenNumberFont.Default)
    {
        Color resolvedCenterStripeColor = centerStripeColor ?? accentColor;
        return HorizontalStripes(
            bodyColor,
            bodyColor,
            bodyColor,
            accentColor,
            resolvedCenterStripeColor,
            accentColor,
            bodyColor,
            centerStripeMode,
            numberColor,
            numberFont);
    }

    public string GetCacheKey()
    {
        return $"{family}_{centerStripeMode}_{numberFont}_{ColorUtility.ToHtmlStringRGBA(bodyColor)}_{ColorUtility.ToHtmlStringRGBA(accentColor)}_{ColorUtility.ToHtmlStringRGBA(ringColor)}_{ColorUtility.ToHtmlStringRGBA(leftStripeColor)}_{ColorUtility.ToHtmlStringRGBA(leftMidStripeColor)}_{ColorUtility.ToHtmlStringRGBA(centerStripeColor)}_{ColorUtility.ToHtmlStringRGBA(rightMidStripeColor)}_{ColorUtility.ToHtmlStringRGBA(rightStripeColor)}_{stripeWidth:F3}_{stripeDirectionDegrees:F1}_{numberFaceOffset:F3}_{ColorUtility.ToHtmlStringRGBA(numberColor)}";
    }
}

public class PlayerTokenVisuals : MonoBehaviour
{
    // World-space offset of the generated top-face quad above the token body.
    private const float TopDecalHeight = 1.01f;
    // Overall scale of the generated top-face quad relative to the token top.
    private const float TopDecalScale = 0.9f;

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
        TokenFacePreviewUtility.ApplyNumberStyle(numberText, style, 2.75f, 2f);
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
        material.mainTexture = TokenFacePreviewUtility.GetOrCreateFaceTexture(style);
        SharedDecalMaterials[cacheKey] = material;
        return material;
    }
}
