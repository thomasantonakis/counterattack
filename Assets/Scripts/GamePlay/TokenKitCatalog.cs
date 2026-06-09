using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public sealed class TokenKitPreset
{
    public string Id { get; }
    public string DisplayName { get; }
    public TokenStyleDefinition Style { get; }
    public IReadOnlyList<string> LegacyAliases { get; }

    public TokenKitPreset(string id, string displayName, TokenStyleDefinition style, params string[] legacyAliases)
    {
        Id = id;
        DisplayName = displayName;
        Style = style;
        LegacyAliases = legacyAliases ?? Array.Empty<string>();
    }
}

public readonly struct TokenKitInstructionPalette
{
    public Color Primary { get; }
    public Color Secondary { get; }

    public TokenKitInstructionPalette(Color primary, Color secondary)
    {
        Primary = primary;
        Secondary = secondary;
    }
}

public readonly struct TokenKitSimilarityBreakdown
{
    public float OverallScore { get; }
    public float BodyColorSimilarity { get; }
    public float TopFaceSimilarity { get; }
    public float TopFaceMeanColorSimilarity { get; }
    public bool IsClash { get; }

    public TokenKitSimilarityBreakdown(
        float overallScore,
        float bodyColorSimilarity,
        float topFaceSimilarity,
        float topFaceMeanColorSimilarity,
        bool isClash)
    {
        OverallScore = overallScore;
        BodyColorSimilarity = bodyColorSimilarity;
        TopFaceSimilarity = topFaceSimilarity;
        TopFaceMeanColorSimilarity = topFaceMeanColorSimilarity;
        IsClash = isClash;
    }
}

public static class TokenKitCatalog
{
    // Similarity scores are percentages from 0 to 100.
    public const float ClashThreshold = 77f;
    private const int FaceAverageSampleTextureSize = 64;
    private const int FaceDominantSampleTextureSize = 64;
    private const int FacePatternSampleTextureSize = 32;
    private const float FaceInnerRadius = 0.85f;
    public const float KitComponentClashThreshold = 77f;
    private const float BodySimilarityWeight = 2f / 3f;
    private const float TopFaceSimilarityWeight = 1f / 3f;
    private const float SharedWhiteTopFaceWeight = 0.20f;
    private const float WhiteishLuminanceThreshold = 0.78f;
    private const float WhiteishChromaThreshold = 0.16f;
    private const float SameColorTolerance = 0.015f;

    private const string SourceJsonRelativePath = "Tools/kit-picker-v1-11-supported.json";

    private static readonly List<TokenKitPreset> LegacyFallbackPresets = new List<TokenKitPreset>
    {
        new TokenKitPreset(
            "blue",
            "Blue",
            TokenStyleDefinition.Plain(HexToColor("#0F4E9B"), HexToColor("#0F4E9B"), HexToColor("#F4F6FA"), HexToColor("#F4F6FA")),
            "Blue",
            "Blues"),
        new TokenKitPreset(
            "red_white",
            "R&W",
            TokenStyleDefinition.VerticalStripes(
                HexToColor("#A71924"),
                HexToColor("#A71924"),
                HexToColor("#F4F6FA"),
                HexToColor("#A71924"),
                HexToColor("#F4F6FA"),
                HexToColor("#A71924"),
                HexToColor("#F4F6FA"),
                TokenCenterStripeMode.BreakUnderNumber,
                HexToColor("#F4F6FA")),
            "R&W",
            "Red and White Stripes"),
        new TokenKitPreset(
            "inter",
            "Inter",
            TokenStyleDefinition.VerticalStripes(
                HexToColor("#1E2027"),
                HexToColor("#1E2027"),
                HexToColor("#2E86F7"),
                HexToColor("#1E2027"),
                HexToColor("#2E86F7"),
                HexToColor("#1E2027"),
                HexToColor("#2E86F7"),
                TokenCenterStripeMode.None,
                HexToColor("#2E86F7")),
            "Inter"),
        new TokenKitPreset(
            "milan",
            "Milan",
            TokenStyleDefinition.VerticalStripes(
                HexToColor("#1E2027"),
                HexToColor("#1E2027"),
                HexToColor("#E25A62"),
                HexToColor("#1E2027"),
                HexToColor("#E25A62"),
                HexToColor("#1E2027"),
                HexToColor("#E25A62"),
                TokenCenterStripeMode.None,
                HexToColor("#E25A62")),
            "Milan"),
        new TokenKitPreset(
            "porto",
            "Porto",
            TokenStyleDefinition.VerticalStripes(
                HexToColor("#F4F6FA"),
                HexToColor("#F4F6FA"),
                HexToColor("#2E86F7"),
                HexToColor("#F4F6FA"),
                HexToColor("#2E86F7"),
                HexToColor("#F4F6FA"),
                HexToColor("#2E86F7"),
                TokenCenterStripeMode.None,
                HexToColor("#2E86F7")),
            "Porto"),
        new TokenKitPreset(
            "clarets",
            "Clarets",
            TokenStyleDefinition.Plain(HexToColor("#4B2735"), HexToColor("#4B2735"), HexToColor("#78C8F7"), HexToColor("#78C8F7")),
            "Clarets"),
        new TokenKitPreset(
            "olympiacos",
            "Olympiacos",
            TokenStyleDefinition.VerticalStripes(
                HexToColor("#A71924"),
                HexToColor("#A71924"),
                HexToColor("#F4F6FA"),
                HexToColor("#A71924"),
                HexToColor("#F4F6FA"),
                HexToColor("#A71924"),
                HexToColor("#F4F6FA"),
                TokenCenterStripeMode.BreakUnderNumber,
                HexToColor("#F4F6FA")),
            "Olympiacos")
    };

    private static IReadOnlyList<TokenKitPreset> activePresets;
    private static string loadedSourcePath;
    private static DateTime loadedSourceWriteUtc;
    private static readonly Dictionary<string, TokenKitInstructionPalette> InstructionPaletteCache = new Dictionary<string, TokenKitInstructionPalette>();

    public static void ReloadFromSource()
    {
        activePresets = null;
        loadedSourcePath = null;
        loadedSourceWriteUtc = DateTime.MinValue;
        EnsureLoaded();
    }

    public static IReadOnlyList<TokenKitPreset> GetAllPresets()
    {
        EnsureLoaded();
        return activePresets;
    }

    public static TokenKitPreset GetPresetByIdOrAlias(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        EnsureLoaded();
        string normalizedValue = value.Trim();
        TokenKitPreset preset = FindPreset(activePresets, normalizedValue);
        if (preset != null)
        {
            return preset;
        }

        return FindPreset(LegacyFallbackPresets, normalizedValue);
    }

    public static TokenStyleDefinition ResolveStyle(string presetIdOrAlias)
    {
        TokenKitPreset preset = GetPresetByIdOrAlias(presetIdOrAlias);
        if (preset != null)
        {
            return preset.Style;
        }

        Debug.LogWarning($"Unknown kit preset '{presetIdOrAlias}'. Falling back to blue token style.");
        return GetBlueUtilityPreset().Style;
    }

    public static TokenKitInstructionPalette ResolveInstructionPalette(string presetIdOrAlias, Color fallbackPrimary, Color fallbackSecondary)
    {
        TokenKitPreset preset = GetPresetByIdOrAlias(presetIdOrAlias);
        if (preset?.Style == null)
        {
            return new TokenKitInstructionPalette(fallbackPrimary, EnsureDistinctSecondary(fallbackPrimary, fallbackSecondary));
        }

        return GetInstructionPalette(preset.Style);
    }

    public static TokenKitInstructionPalette GetInstructionPalette(TokenStyleDefinition style)
    {
        if (style == null)
        {
            return new TokenKitInstructionPalette(Color.black, Color.white);
        }

        string cacheKey = style.GetCacheKey();
        if (InstructionPaletteCache.TryGetValue(cacheKey, out TokenKitInstructionPalette cachedPalette))
        {
            return cachedPalette;
        }

        Color primary = style.bodyColor;
        Color secondary = GetDominantFaceColorExcludingBody(style);
        secondary = EnsureDistinctSecondary(primary, secondary);

        TokenKitInstructionPalette palette = new TokenKitInstructionPalette(primary, secondary);
        InstructionPaletteCache[cacheKey] = palette;
        return palette;
    }

    public static float GetSimilarityScore(string presetIdOrAliasA, string presetIdOrAliasB)
    {
        return GetSimilarityBreakdown(presetIdOrAliasA, presetIdOrAliasB).OverallScore;
    }

    public static TokenKitSimilarityBreakdown GetSimilarityBreakdown(string presetIdOrAliasA, string presetIdOrAliasB)
    {
        TokenKitPreset presetA = GetPresetByIdOrAlias(presetIdOrAliasA);
        TokenKitPreset presetB = GetPresetByIdOrAlias(presetIdOrAliasB);
        if (presetA == null || presetB == null)
        {
            return new TokenKitSimilarityBreakdown(0f, 0f, 0f, 0f, false);
        }

        TokenStyleDefinition styleA = presetA.Style;
        TokenStyleDefinition styleB = presetB.Style;

        float bodyScore = GetColorSimilarity(styleA.bodyColor, styleB.bodyColor);
        float faceMeanScore = GetColorSimilarity(GetAverageFaceColor(styleA), GetAverageFaceColor(styleB));
        float topFaceScore = GetTopFaceSimilarity(styleA, styleB, faceMeanScore);

        float weightedScore = (bodyScore * BodySimilarityWeight) + (topFaceScore * TopFaceSimilarityWeight);
        bool isClash = bodyScore > KitComponentClashThreshold
            || topFaceScore > KitComponentClashThreshold
            || weightedScore >= ClashThreshold;

        return new TokenKitSimilarityBreakdown(
            weightedScore,
            bodyScore,
            topFaceScore,
            faceMeanScore,
            isClash);
    }

    private static Color GetDominantFaceColorExcludingBody(TokenStyleDefinition style)
    {
        Texture2D faceTexture = TokenFacePreviewUtility.GetOrCreateFaceTexture(style, FaceDominantSampleTextureSize);
        if (faceTexture == null)
        {
            return GetFirstDistinctConfiguredColor(style);
        }

        Color[] pixels = faceTexture.GetPixels();
        if (pixels == null || pixels.Length == 0)
        {
            return GetFirstDistinctConfiguredColor(style);
        }

        Dictionary<int, ColorBucket> buckets = new Dictionary<int, ColorBucket>();
        foreach (Color pixel in pixels)
        {
            if (pixel.a <= 0f || AreColorsEquivalent(pixel, style.bodyColor))
            {
                continue;
            }

            int key = QuantizeRgb(pixel);
            if (buckets.TryGetValue(key, out ColorBucket bucket))
            {
                bucket.Count++;
                bucket.Accumulated += new Vector3(pixel.r, pixel.g, pixel.b);
                buckets[key] = bucket;
            }
            else
            {
                buckets[key] = new ColorBucket
                {
                    Count = 1,
                    Accumulated = new Vector3(pixel.r, pixel.g, pixel.b)
                };
            }
        }

        int dominantCount = 0;
        Vector3 dominantAccumulated = Vector3.zero;
        foreach (ColorBucket bucket in buckets.Values)
        {
            if (bucket.Count <= dominantCount)
            {
                continue;
            }

            dominantCount = bucket.Count;
            dominantAccumulated = bucket.Accumulated;
        }

        if (dominantCount <= 0)
        {
            return GetFirstDistinctConfiguredColor(style);
        }

        Vector3 average = dominantAccumulated / dominantCount;
        return new Color(average.x, average.y, average.z, 1f);
    }

    private static Color GetFirstDistinctConfiguredColor(TokenStyleDefinition style)
    {
        Color[] candidates =
        {
            style.accentColor,
            style.ringColor,
            style.leftStripeColor,
            style.leftMidStripeColor,
            style.centerStripeColor,
            style.rightMidStripeColor,
            style.rightStripeColor,
            style.numberColor
        };

        foreach (Color candidate in candidates)
        {
            if (candidate.a > 0f && !AreColorsEquivalent(candidate, style.bodyColor))
            {
                return candidate;
            }
        }

        return GetReadableContrastColor(style.bodyColor);
    }

    private static Color EnsureDistinctSecondary(Color primary, Color secondary)
    {
        return AreColorsEquivalent(primary, secondary)
            ? GetReadableContrastColor(primary)
            : secondary;
    }

    private static Color GetReadableContrastColor(Color color)
    {
        float luminance = GetLuminance(color);
        return luminance >= 0.5f ? Color.black : Color.white;
    }

    private static float GetLuminance(Color color)
    {
        return (0.2126f * color.r) + (0.7152f * color.g) + (0.0722f * color.b);
    }

    private static bool AreColorsEquivalent(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) <= SameColorTolerance
            && Mathf.Abs(a.g - b.g) <= SameColorTolerance
            && Mathf.Abs(a.b - b.b) <= SameColorTolerance;
    }

    private static int QuantizeRgb(Color color)
    {
        int r = Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255f);
        int g = Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255f);
        int b = Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255f);
        return (r << 16) | (g << 8) | b;
    }

    private struct ColorBucket
    {
        public int Count;
        public Vector3 Accumulated;
    }

    private readonly struct TopFacePalette
    {
        public Color Primary { get; }
        public Color Secondary { get; }
        public bool IsValid { get; }

        public TopFacePalette(Color primary, Color secondary, bool isValid)
        {
            Primary = primary;
            Secondary = secondary;
            IsValid = isValid;
        }
    }

    private static Color GetAverageFaceColor(TokenStyleDefinition style)
    {
        Texture2D faceTexture = TokenFacePreviewUtility.GetOrCreateFaceTexture(style, FaceAverageSampleTextureSize);
        if (faceTexture == null)
        {
            return style.bodyColor;
        }

        Color[] pixels = faceTexture.GetPixels();
        if (pixels == null || pixels.Length == 0)
        {
            return style.bodyColor;
        }

        Vector4 accumulated = Vector4.zero;
        float contributingPixelCount = 0f;

        for (int i = 0; i < pixels.Length; i++)
        {
            if (!IsInnerFacePixel(i, FaceDominantSampleTextureSize))
            {
                continue;
            }

            Color pixel = pixels[i];
            if (pixel.a <= 0f)
            {
                continue;
            }

            accumulated.x += pixel.r;
            accumulated.y += pixel.g;
            accumulated.z += pixel.b;
            accumulated.w += pixel.a;
            contributingPixelCount += 1f;
        }

        if (contributingPixelCount <= 0f)
        {
            return style.bodyColor;
        }

        return new Color(
            accumulated.x / contributingPixelCount,
            accumulated.y / contributingPixelCount,
            accumulated.z / contributingPixelCount,
            accumulated.w / contributingPixelCount);
    }

    private static void EnsureLoaded()
    {
        string sourcePath = ResolveSourceJsonPath();
        DateTime writeUtc = File.Exists(sourcePath)
            ? File.GetLastWriteTimeUtc(sourcePath)
            : DateTime.MinValue;

        if (activePresets != null
            && string.Equals(loadedSourcePath, sourcePath, StringComparison.Ordinal)
            && loadedSourceWriteUtc == writeUtc)
        {
            return;
        }

        activePresets = BuildActivePresets(sourcePath);
        loadedSourcePath = sourcePath;
        loadedSourceWriteUtc = writeUtc;
    }

    private static IReadOnlyList<TokenKitPreset> BuildActivePresets(string sourcePath)
    {
        List<TokenKitPreset> presets = LoadPresetsFromJson(sourcePath);
        if (presets.Count == 0)
        {
            presets = new List<TokenKitPreset>(LegacyFallbackPresets);
        }

        EnsureAlias(presets, "028", "R&W", "Red and White Stripes");

        return presets;
    }

    private static List<TokenKitPreset> LoadPresetsFromJson(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return new List<TokenKitPreset>();
        }

        try
        {
            JObject root = JObject.Parse(File.ReadAllText(sourcePath));
            List<TokenKitPreset> presets = new List<TokenKitPreset>();
            foreach (JProperty property in root.Properties())
            {
                if (property.Value is not JObject presetObject)
                {
                    continue;
                }

                TokenStyleDefinition style = ParseStyleDefinition(presetObject);
                if (style == null)
                {
                    continue;
                }

                string displayName = BuildDisplayName(property.Name, presetObject);
                List<string> aliases = ExtractAliases(presetObject);
                presets.Add(new TokenKitPreset(property.Name, displayName, style, aliases.ToArray()));
            }

            return presets;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to load kit presets from {sourcePath}: {ex.Message}");
            return new List<TokenKitPreset>();
        }
    }

    private static TokenStyleDefinition ParseStyleDefinition(JObject presetObject)
    {
        string pattern = presetObject.Value<string>("pattern") ?? string.Empty;
        JObject config = presetObject["config"] as JObject;
        if (config == null)
        {
            return null;
        }

        TokenNumberFont font = ParseFont(config.Value<string>("font"));
        switch (pattern)
        {
            case "Plain":
                Color plainBody = ParseColor(config.Value<string>("bodyColor"), Color.white);
                return TokenStyleDefinition.Plain(
                    plainBody,
                    ParseColor(config.Value<string>("accentColor"), plainBody),
                    ParseColor(config.Value<string>("ringColor"), plainBody),
                    ParseColor(config.Value<string>("numberColor"), Color.white),
                    font);

            case "VerticalStripes":
                Color verticalBody = ParseColor(config.Value<string>("bodyColor"), Color.white);
                return TokenStyleDefinition.VerticalStripes(
                    verticalBody,
                    ParseColor(config.Value<string>("ringColor"), verticalBody),
                    ParseColor(config.Value<string>("leftStripeColor"), verticalBody),
                    ParseColor(config.Value<string>("leftMidStripeColor"), verticalBody),
                    ParseColor(config.Value<string>("centerStripeColor"), verticalBody),
                    ParseColor(config.Value<string>("rightMidStripeColor"), verticalBody),
                    ParseColor(config.Value<string>("rightStripeColor"), verticalBody),
                    ParseCenterStripeMode(config.Value<string>("centerStripeMode")),
                    ParseColor(config.Value<string>("numberColor"), Color.white),
                    font);

            case "HorizontalStripes":
                Color horizontalBody = ParseColor(config.Value<string>("bodyColor"), Color.white);
                return TokenStyleDefinition.HorizontalStripes(
                    horizontalBody,
                    ParseColor(config.Value<string>("ringColor"), horizontalBody),
                    ParseColor(config.Value<string>("topStripeColor"), ParseColor(config.Value<string>("leftStripeColor"), horizontalBody)),
                    ParseColor(config.Value<string>("topMidStripeColor"), ParseColor(config.Value<string>("leftMidStripeColor"), horizontalBody)),
                    ParseColor(config.Value<string>("centerStripeColor"), horizontalBody),
                    ParseColor(config.Value<string>("bottomMidStripeColor"), ParseColor(config.Value<string>("rightMidStripeColor"), horizontalBody)),
                    ParseColor(config.Value<string>("bottomStripeColor"), ParseColor(config.Value<string>("rightStripeColor"), horizontalBody)),
                    ParseCenterStripeMode(config.Value<string>("centerStripeMode")),
                    ParseColor(config.Value<string>("numberColor"), Color.white),
                    font);

            case "VerticalThreeColors":
                Color legacyBody = ParseColor(config.Value<string>("bodyColor"), Color.white);
                Color legacyAccent = ParseColor(config.Value<string>("accentColor"), legacyBody);
                return TokenStyleDefinition.VerticalThreeColors(
                    legacyBody,
                    legacyAccent,
                    ParseColor(config.Value<string>("numberColor"), Color.white),
                    ParseCenterStripeMode(config.Value<string>("centerStripeMode")),
                    ParseColor(config.Value<string>("centerStripeColor"), legacyAccent),
                    font);

            case "HorizontalThreeColors":
                Color horizontalLegacyBody = ParseColor(config.Value<string>("bodyColor"), Color.white);
                Color horizontalLegacyAccent = ParseColor(config.Value<string>("accentColor"), horizontalLegacyBody);
                return TokenStyleDefinition.HorizontalThreeColors(
                    horizontalLegacyBody,
                    horizontalLegacyAccent,
                    ParseColor(config.Value<string>("numberColor"), Color.white),
                    ParseCenterStripeMode(config.Value<string>("centerStripeMode")),
                    ParseColor(config.Value<string>("centerStripeColor"), horizontalLegacyAccent),
                    font);

            case "singleStriped":
            case "SingleStriped":
                return ParseSingleStripedStyle(config, font);

            case "Sleeved":
                if (LooksLikeSingleStripedConfig(config))
                {
                    return ParseSingleStripedStyle(config, font);
                }

                Color sleevedBody = ParseColor(config.Value<string>("bodyColor"), Color.white);
                Color sleeveColor = ParseColor(config.Value<string>("sleeveColor"), sleevedBody);
                return TokenStyleDefinition.Sleeved(
                    sleevedBody,
                    sleeveColor,
                    ParseColor(config.Value<string>("numberColor"), sleeveColor),
                    font);

            case "Roma":
                Color romaBody = ParseColor(config.Value<string>("bodyColor"), Color.white);
                return TokenStyleDefinition.Roma(
                    romaBody,
                    ParseColor(config.Value<string>("topStripeColor"), romaBody),
                    ParseColor(config.Value<string>("bottomStripeColor"), romaBody),
                    ParseColor(config.Value<string>("numberColor"), Color.white),
                    font);

            default:
                Debug.LogWarning($"Unsupported kit pattern '{pattern}'.");
                return null;
        }
    }

    private static bool LooksLikeSingleStripedConfig(JObject config)
    {
        return config["stripeColor"] != null || config["stripeWidth"] != null || config["stripeRotation"] != null || config["stripeDirection"] != null;
    }

    private static TokenStyleDefinition ParseSingleStripedStyle(JObject config, TokenNumberFont font)
    {
        Color bodyColor = ParseColor(config.Value<string>("bodyColor"), Color.white);
        float stripeWidth = ParseStripeWidth(config["stripeWidth"]);
        float stripeDirectionDegrees = ParseStripeDirectionDegrees(config["stripeDirection"], config["stripeRotation"]);

        return TokenStyleDefinition.SingleStriped(
            bodyColor,
            ParseColor(config.Value<string>("stripeColor"), bodyColor),
            ParseColor(config.Value<string>("numberColor"), Color.white),
            stripeWidth,
            stripeDirectionDegrees,
            font);
    }

    private static List<string> ExtractAliases(JObject presetObject)
    {
        List<string> aliases = new List<string>();

        JToken exampleToken = presetObject["example"];
        if (exampleToken != null)
        {
            AddAliasToken(aliases, exampleToken);
        }

        JToken examplesToken = presetObject["examples"];
        if (examplesToken != null)
        {
            AddAliasToken(aliases, examplesToken);
        }

        return aliases
            .Where(alias => !string.IsNullOrWhiteSpace(alias))
            .Select(alias => alias.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildDisplayName(string presetId, JObject presetObject)
    {
        string exampleLabel = GetPrimaryExampleLabel(presetObject);
        if (string.IsNullOrWhiteSpace(exampleLabel))
        {
            return presetId;
        }

        return exampleLabel;
    }

    private static string GetPrimaryExampleLabel(JObject presetObject)
    {
        JToken exampleToken = presetObject["example"] ?? presetObject["examples"];
        if (exampleToken == null)
        {
            return string.Empty;
        }

        switch (exampleToken.Type)
        {
            case JTokenType.String:
                return exampleToken.Value<string>()?.Trim() ?? string.Empty;
            case JTokenType.Array:
                foreach (JToken child in exampleToken.Children())
                {
                    if (child.Type == JTokenType.String)
                    {
                        string value = child.Value<string>()?.Trim() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }
                }
                break;
        }

        return string.Empty;
    }

    private static void AddAliasToken(List<string> aliases, JToken token)
    {
        switch (token.Type)
        {
            case JTokenType.String:
                aliases.Add(token.Value<string>());
                break;
            case JTokenType.Array:
                foreach (JToken child in token.Children())
                {
                    if (child.Type == JTokenType.String)
                    {
                        aliases.Add(child.Value<string>());
                    }
                }
                break;
        }
    }

    private static TokenKitPreset FindPreset(IEnumerable<TokenKitPreset> presets, string normalizedValue)
    {
        foreach (TokenKitPreset preset in presets)
        {
            if (preset.Id.Equals(normalizedValue, StringComparison.OrdinalIgnoreCase)
                || preset.DisplayName.Equals(normalizedValue, StringComparison.OrdinalIgnoreCase))
            {
                return preset;
            }

            foreach (string alias in preset.LegacyAliases)
            {
                if (alias.Equals(normalizedValue, StringComparison.OrdinalIgnoreCase))
                {
                    return preset;
                }
            }
        }

        return null;
    }

    private static void EnsureUtilityPreset(List<TokenKitPreset> presets, TokenKitPreset utilityPreset)
    {
        if (FindPreset(presets, utilityPreset.Id) != null)
        {
            return;
        }

        presets.Add(utilityPreset);
    }

    private static void EnsureAlias(List<TokenKitPreset> presets, string presetId, params string[] aliasesToAdd)
    {
        for (int i = 0; i < presets.Count; i++)
        {
            TokenKitPreset preset = presets[i];
            if (!preset.Id.Equals(presetId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            List<string> mergedAliases = new List<string>(preset.LegacyAliases);
            foreach (string alias in aliasesToAdd)
            {
                if (!string.IsNullOrWhiteSpace(alias) && !mergedAliases.Any(existing => existing.Equals(alias, StringComparison.OrdinalIgnoreCase)))
                {
                    mergedAliases.Add(alias);
                }
            }

            presets[i] = new TokenKitPreset(preset.Id, preset.DisplayName, preset.Style, mergedAliases.ToArray());
            return;
        }
    }

    private static TokenKitPreset GetBlueUtilityPreset()
    {
        return new TokenKitPreset(
            "000",
            "000 - Blue",
            TokenStyleDefinition.Plain(HexToColor("#355EAF"), HexToColor("#355EAF"), HexToColor("#F4F6FA"), HexToColor("#F4F6FA")),
            "Blue",
            "Blues");
    }

    private static TokenKitPreset GetRedUtilityPreset()
    {
        return new TokenKitPreset(
            "001",
            "001 - Red",
            TokenStyleDefinition.Plain(HexToColor("#A71924"), HexToColor("#A71924"), HexToColor("#F4F6FA"), HexToColor("#F4F6FA")),
            "Red");
    }

    private static string ResolveSourceJsonPath()
    {
        string baseDir = Directory.GetCurrentDirectory();
        if (string.IsNullOrWhiteSpace(baseDir))
        {
            return SourceJsonRelativePath;
        }

        return Path.Combine(baseDir, SourceJsonRelativePath);
    }

    private static float GetColorSimilarity(Color a, Color b)
    {
        float normalizedDistance = Mathf.Clamp01(GetColorDistance(a, b));
        float closeness = 1f - normalizedDistance;
        return closeness * closeness * 100f;
    }

    private static float GetTopFaceSimilarity(TokenStyleDefinition styleA, TokenStyleDefinition styleB, float faceMeanScore)
    {
        float directPixelScore = GetTopFacePixelSimilarity(styleA, styleB, swapSecondPalette: false);
        float swappedPixelScore = GetTopFacePixelSimilarity(styleA, styleB, swapSecondPalette: true);
        float structuredScore = GetStructuredTopFaceSimilarity(styleA, styleB);

        float renderedScore = Mathf.Max(directPixelScore, swappedPixelScore);
        float renderedMeanScore = (renderedScore * 0.90f) + (faceMeanScore * 0.10f);
        return Mathf.Clamp(Mathf.Max(structuredScore, renderedMeanScore), 0f, 100f);
    }

    private static float GetTopFacePixelSimilarity(TokenStyleDefinition styleA, TokenStyleDefinition styleB, bool swapSecondPalette)
    {
        Texture2D faceTextureA = TokenFacePreviewUtility.GetOrCreateFaceTexture(styleA, FacePatternSampleTextureSize);
        Texture2D faceTextureB = TokenFacePreviewUtility.GetOrCreateFaceTexture(styleB, FacePatternSampleTextureSize);
        if (faceTextureA == null || faceTextureB == null)
        {
            return 0f;
        }

        Color[] pixelsA = faceTextureA.GetPixels();
        Color[] pixelsB = faceTextureB.GetPixels();
        if (pixelsA == null || pixelsB == null || pixelsA.Length == 0 || pixelsA.Length != pixelsB.Length)
        {
            return 0f;
        }

        TopFacePalette paletteB = swapSecondPalette ? GetTopFacePalette(styleB) : default;
        float accumulatedSimilarity = 0f;
        float contributingPixelCount = 0f;
        for (int i = 0; i < pixelsA.Length; i++)
        {
            if (!IsInnerFacePixel(i, FacePatternSampleTextureSize))
            {
                continue;
            }

            Color pixelA = pixelsA[i];
            Color pixelB = pixelsB[i];
            if (pixelA.a <= 0f || pixelB.a <= 0f)
            {
                continue;
            }

            if (swapSecondPalette && paletteB.IsValid)
            {
                pixelB = SwapPaletteColor(pixelB, paletteB);
            }

            float comparisonWeight = GetTopFaceColorPairWeight(pixelA, pixelB);
            accumulatedSimilarity += GetColorSimilarity(pixelA, pixelB) * comparisonWeight;
            contributingPixelCount += comparisonWeight;
        }

        return contributingPixelCount <= 0f ? 0f : accumulatedSimilarity / contributingPixelCount;
    }

    private static float GetStructuredTopFaceSimilarity(TokenStyleDefinition styleA, TokenStyleDefinition styleB)
    {
        if (styleA == null || styleB == null)
        {
            return 0f;
        }

        if (styleA.family == styleB.family
            && (styleA.family == TokenStyleFamily.VerticalStripes || styleA.family == TokenStyleFamily.HorizontalStripes))
        {
            float stripeScore = GetStripeSlotSimilarity(styleA, styleB);
            float modeScore = styleA.centerStripeMode == styleB.centerStripeMode ? 100f : 35f;
            return (stripeScore * 0.92f) + (modeScore * 0.08f);
        }

        if (styleA.family == styleB.family)
        {
            return GetSurfacePatternSimilarity(styleA, styleB);
        }

        return 0f;
    }

    private static float GetStripeSlotSimilarity(TokenStyleDefinition styleA, TokenStyleDefinition styleB)
    {
        Color[] slotsA = GetStripeSlots(styleA);
        Color[] slotsB = GetStripeSlots(styleB);
        float directScore = GetAverageSlotSimilarity(slotsA, slotsB);

        TopFacePalette paletteB = GetTopFacePalette(styleB);
        if (!paletteB.IsValid)
        {
            return directScore;
        }

        Color[] swappedSlotsB = new Color[slotsB.Length];
        for (int i = 0; i < slotsB.Length; i++)
        {
            swappedSlotsB[i] = SwapPaletteColor(slotsB[i], paletteB);
        }

        float swappedScore = GetAverageSlotSimilarity(slotsA, swappedSlotsB);
        return Mathf.Max(directScore, swappedScore);
    }

    private static Color[] GetStripeSlots(TokenStyleDefinition style)
    {
        return new[]
        {
            style.leftStripeColor,
            style.leftMidStripeColor,
            style.centerStripeColor,
            style.rightMidStripeColor,
            style.rightStripeColor
        };
    }

    private static float GetAverageSlotSimilarity(Color[] slotsA, Color[] slotsB)
    {
        if (slotsA == null || slotsB == null || slotsA.Length == 0 || slotsA.Length != slotsB.Length)
        {
            return 0f;
        }

        float accumulated = 0f;
        float totalWeight = 0f;
        for (int i = 0; i < slotsA.Length; i++)
        {
            float comparisonWeight = GetTopFaceColorPairWeight(slotsA[i], slotsB[i]);
            accumulated += GetColorSimilarity(slotsA[i], slotsB[i]) * comparisonWeight;
            totalWeight += comparisonWeight;
        }

        return totalWeight <= 0f ? 0f : accumulated / totalWeight;
    }

    private static float GetTopFaceColorPairWeight(Color a, Color b)
    {
        return IsWhiteishTopFaceColor(a) && IsWhiteishTopFaceColor(b)
            ? SharedWhiteTopFaceWeight
            : 1f;
    }

    private static bool IsWhiteishTopFaceColor(Color color)
    {
        float luminance = GetLuminance(color);
        float maxChannel = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        float minChannel = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        float chroma = maxChannel - minChannel;
        return luminance >= WhiteishLuminanceThreshold && chroma <= WhiteishChromaThreshold;
    }

    private static TopFacePalette GetTopFacePalette(TokenStyleDefinition style)
    {
        Texture2D faceTexture = TokenFacePreviewUtility.GetOrCreateFaceTexture(style, FaceDominantSampleTextureSize);
        if (faceTexture == null)
        {
            return new TopFacePalette(style.bodyColor, style.accentColor, true);
        }

        Color[] pixels = faceTexture.GetPixels();
        if (pixels == null || pixels.Length == 0)
        {
            return new TopFacePalette(style.bodyColor, style.accentColor, true);
        }

        Dictionary<int, ColorBucket> buckets = new Dictionary<int, ColorBucket>();
        for (int i = 0; i < pixels.Length; i++)
        {
            if (!IsInnerFacePixel(i, FaceAverageSampleTextureSize))
            {
                continue;
            }

            Color pixel = pixels[i];
            if (pixel.a <= 0f)
            {
                continue;
            }

            int key = QuantizeRgb(pixel);
            if (buckets.TryGetValue(key, out ColorBucket bucket))
            {
                bucket.Count++;
                bucket.Accumulated += new Vector3(pixel.r, pixel.g, pixel.b);
                buckets[key] = bucket;
            }
            else
            {
                buckets[key] = new ColorBucket
                {
                    Count = 1,
                    Accumulated = new Vector3(pixel.r, pixel.g, pixel.b)
                };
            }
        }

        List<Color> dominantColors = buckets.Values
            .OrderByDescending(bucket => bucket.Count)
            .Select(bucket => new Color(
                bucket.Accumulated.x / bucket.Count,
                bucket.Accumulated.y / bucket.Count,
                bucket.Accumulated.z / bucket.Count,
                1f))
            .ToList();

        if (dominantColors.Count == 0)
        {
            return new TopFacePalette(style.bodyColor, style.accentColor, true);
        }

        Color primary = dominantColors[0];
        Color secondary = style.accentColor;
        bool foundSecondary = false;
        foreach (Color candidate in dominantColors)
        {
            if (AreColorsEquivalent(candidate, primary))
            {
                continue;
            }

            secondary = candidate;
            foundSecondary = true;
            break;
        }

        if (!foundSecondary)
        {
            secondary = GetFirstDistinctConfiguredColor(style);
        }

        return new TopFacePalette(primary, secondary, true);
    }

    private static Color SwapPaletteColor(Color pixel, TopFacePalette palette)
    {
        float primaryDistance = GetColorDistance(pixel, palette.Primary);
        float secondaryDistance = GetColorDistance(pixel, palette.Secondary);
        return primaryDistance <= secondaryDistance ? palette.Secondary : palette.Primary;
    }

    private static float GetColorDistance(Color a, Color b)
    {
        return Mathf.Sqrt(
            (0.2126f * Mathf.Pow(a.r - b.r, 2f)) +
            (0.7152f * Mathf.Pow(a.g - b.g, 2f)) +
            (0.0722f * Mathf.Pow(a.b - b.b, 2f)));
    }

    private static bool IsInnerFacePixel(int pixelIndex, int textureSize)
    {
        int x = pixelIndex % textureSize;
        int y = pixelIndex / textureSize;
        float u = ((x + 0.5f) / textureSize * 2f) - 1f;
        float v = ((y + 0.5f) / textureSize * 2f) - 1f;
        return Mathf.Sqrt((u * u) + (v * v)) <= FaceInnerRadius;
    }

    private static float GetSurfacePatternSimilarity(TokenStyleDefinition styleA, TokenStyleDefinition styleB)
    {
        if (styleA == null || styleB == null)
        {
            return 0f;
        }

        if (styleA.family != styleB.family)
        {
            return 0f;
        }

        if (styleA.family == TokenStyleFamily.Plain)
        {
            return GetColorSimilarity(styleA.accentColor, styleB.accentColor);
        }

        if (styleA.family == TokenStyleFamily.Sleeved)
        {
            return (GetColorSimilarity(styleA.bodyColor, styleB.bodyColor) + GetColorSimilarity(styleA.ringColor, styleB.ringColor)) / 2f;
        }

        if (styleA.family == TokenStyleFamily.SingleStriped)
        {
            float colorScore = GetColorSimilarity(styleA.accentColor, styleB.accentColor);
            float widthScore = 100f - (Mathf.Abs(styleA.stripeWidth - styleB.stripeWidth) * 100f);
            float wrappedDirectionDifference = Mathf.Abs(styleA.stripeDirectionDegrees - styleB.stripeDirectionDegrees);
            wrappedDirectionDifference = Mathf.Min(wrappedDirectionDifference, 180f - wrappedDirectionDifference);
            float directionScore = 100f - ((wrappedDirectionDifference / 90f) * 100f);
            return (colorScore + Mathf.Clamp(widthScore, 0f, 100f) + Mathf.Clamp(directionScore, 0f, 100f)) / 3f;
        }

        if (styleA.family == TokenStyleFamily.Roma)
        {
            float topStripeScore = GetColorSimilarity(styleA.accentColor, styleB.accentColor);
            float bottomStripeScore = GetColorSimilarity(styleA.ringColor, styleB.ringColor);
            return (topStripeScore + bottomStripeScore) / 2f;
        }

        float leftScore = GetColorSimilarity(styleA.leftStripeColor, styleB.leftStripeColor);
        float leftMidScore = GetColorSimilarity(styleA.leftMidStripeColor, styleB.leftMidStripeColor);
        float centerScore = GetColorSimilarity(styleA.centerStripeColor, styleB.centerStripeColor);
        float rightMidScore = GetColorSimilarity(styleA.rightMidStripeColor, styleB.rightMidStripeColor);
        float rightScore = GetColorSimilarity(styleA.rightStripeColor, styleB.rightStripeColor);
        return (leftScore + leftMidScore + centerScore + rightMidScore + rightScore) / 5f;
    }

    private static TokenCenterStripeMode ParseCenterStripeMode(string rawValue)
    {
        if (Enum.TryParse(rawValue, ignoreCase: true, out TokenCenterStripeMode mode))
        {
            return mode;
        }

        return TokenCenterStripeMode.None;
    }

    private static TokenNumberFont ParseFont(string rawValue)
    {
        // # @thomas The JSON config uses strings like "Default" or "Getafe" here.
        // The value must match a TokenNumberFont enum member name to resolve successfully.
        if (Enum.TryParse(rawValue, ignoreCase: true, out TokenNumberFont font))
        {
            return font;
        }

        return TokenNumberFont.Default;
    }

    private static float ParseStripeWidth(JToken rawValue)
    {
        if (rawValue == null)
        {
            return 0.25f;
        }

        if (rawValue.Type == JTokenType.Float || rawValue.Type == JTokenType.Integer)
        {
            return Mathf.Max(0f, rawValue.Value<float>());
        }

        if (float.TryParse(rawValue.Value<string>(), out float parsed))
        {
            return Mathf.Max(0f, parsed);
        }

        return 0.25f;
    }

    private static float ParseStripeDirectionDegrees(JToken stripeDirectionToken, JToken stripeRotationToken)
    {
        JToken source = stripeDirectionToken ?? stripeRotationToken;
        if (source == null)
        {
            return 0f;
        }

        float parsed = 0f;
        if (source.Type == JTokenType.Float || source.Type == JTokenType.Integer)
        {
            parsed = source.Value<float>();
        }
        else if (!float.TryParse(source.Value<string>(), out parsed))
        {
            return 0f;
        }

        parsed %= 180f;
        if (parsed < 0f)
        {
            parsed += 180f;
        }

        return parsed;
    }

    private static Color ParseColor(string rawValue, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return fallback;
        }

        string normalized = rawValue.Trim();
        while (normalized.StartsWith("##", StringComparison.Ordinal))
        {
            normalized = normalized.Substring(1);
        }

        if (!normalized.StartsWith("#", StringComparison.Ordinal))
        {
            normalized = $"#{normalized}";
        }

        if (ColorUtility.TryParseHtmlString(normalized, out Color parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }

        return Color.white;
    }
}
