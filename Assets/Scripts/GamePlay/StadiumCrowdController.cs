using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StadiumCrowdSection
{
    public int section;
    public bool upperTier;
    public Renderer seats;
    public Renderer spectators;
    public Color32[] neutralTops;
    public Color32[] neutralTrousers;
}

/// <summary>Colours authored crowd geometry once per match; never creates scene objects or meshes.</summary>
public sealed class StadiumCrowdController : MonoBehaviour
{
    [SerializeField] private StadiumCrowdSection[] sections = Array.Empty<StadiumCrowdSection>();
    [Tooltip("Reference block (zero-based), counted as position 1. Away fans occupy the following blocks counterclockwise in BOTH tiers.")]
    [SerializeField, Range(8, 13)] private int clubAwaySection = 10;
    [Tooltip("Number of consecutive blocks counterclockwise from the reference, in each tier. 3 means positions 2–4, six sections total.")]
    [SerializeField, Range(1, 6)] private int clubAwaySectionCount = 3;
    private readonly List<Texture2D> outfitTextures = new();
    private bool applied;
    private static readonly int EnabledId = Shader.PropertyToID("_PaletteEnabled");
    private static readonly int SeatId = Shader.PropertyToID("_SeatColor");
    private static readonly int TextureId = Shader.PropertyToID("_OutfitTex");
    private static readonly int WidthId = Shader.PropertyToID("_OutfitWidth");

    public IReadOnlyList<StadiumCrowdSection> Sections => sections;
    public int AppliedSeed { get; private set; }
    public bool HasAppliedColours => applied;

    public void Configure(StadiumCrowdSection[] authoredSections) => sections = authoredSections;

    public bool IsClubAwaySection(int sectionIndex)
    {
        // Authored sections increase clockwise around the pitch. Exclude the
        // reference itself (position 1); select positions 2 through count + 1.
        int counterclockwiseDistance = (clubAwaySection - sectionIndex + 32) % 32;
        return counterclockwiseDistance >= 1 && counterclockwiseDistance <= clubAwaySectionCount;
    }

    [ContextMenu("Preview Colours/Fiorentina vs Real Madrid")]
    public void PreviewFiorentina()
    {
        if (Application.isPlaying) return;
        ApplyMatchColours(new MatchManager.GameSettings {
            matchType = "Regular", homeTeamName = "Fiorentina", awayTeamName = "Real Madrid",
            homeKit = "042", awayKit = "029"
        }, 271828);
    }

    [ContextMenu("Preview Colours/Germany vs Netherlands")]
    public void PreviewGermany()
    {
        if (Application.isPlaying) return;
        ApplyMatchColours(new MatchManager.GameSettings {
            matchType = "International", homeTeamName = "Germany", awayTeamName = "Netherlands",
            homeKit = "geraway", awayKit = "nedaway2"
        }, 271828);
    }

    [ContextMenu("Preview Colours/Clear Edit Mode Preview")]
    private void ClearPreview() { if (!Application.isPlaying) ClearColours(); }

    private IEnumerator Start()
    {
        // MatchManager loads the selected save in Start. Wait beyond all initial Starts.
        yield return null;
        while (MatchManager.Instance?.gameData?.gameSettings == null
            || string.IsNullOrWhiteSpace(MatchManager.Instance.gameData.gameSettings.homeKit)
            || string.IsNullOrWhiteSpace(MatchManager.Instance.gameData.gameSettings.awayKit))
            yield return null;
        ApplyMatchColours(MatchManager.Instance.gameData.gameSettings, Guid.NewGuid().GetHashCode());
    }

    public void ApplyMatchColours(MatchManager.GameSettings settings, int seed)
    {
        if (settings == null || (Application.isPlaying && applied)) return;
        ClearColours();
        AppliedSeed = seed;
        bool international = string.Equals(settings.matchType, "International", StringComparison.OrdinalIgnoreCase);
        var home = international ? NationalKit(settings.homeTeamName, false) : ClubKit(settings.homeKit);
        var away = international ? NationalKit(settings.awayTeamName, false) : ClubKit(settings.awayKit);
        var homeAlternate = international ? NationalKit(settings.homeTeamName, true) : Swap(home);
        var awayAlternate = international ? NationalKit(settings.awayTeamName, true) : Swap(away);
        var random = new System.Random(seed);
        Color seatColour = international ? RandomSeatColour(random) : home.Primary;

        foreach (var section in sections)
        {
            if (section.seats == null || section.spectators == null || section.neutralTops == null
                || section.neutralTrousers == null || section.neutralTops.Length != section.neutralTrousers.Length)
                throw new InvalidOperationException("Stadium section is missing its authored renderer or clothing metadata. Rebuild the stadium.");

            bool west = section.section >= 23 && section.section <= 30;
            bool east = section.section >= 7 && section.section <= 14;
            TokenKitInstructionPalette[] palettes;
            int[] percentages;
            // A null/neutral palette is represented by the last category, keeping
            // the original varied clothes, skin, hair and shoes intact.
            if (international && west)
            {
                palettes = new[] {home, homeAlternate}; percentages = new[] {80, 15, 5};
            }
            else if (international && east)
            {
                palettes = new[] {away, awayAlternate}; percentages = new[] {80, 15, 5};
            }
            else if (international)
            {
                palettes = new[] {home, homeAlternate, away, awayAlternate};
                percentages = new[] {30, 18, 20, 12, 20};
            }
            else if (west)
            {
                palettes = new[] {home, homeAlternate}; percentages = new[] {95, 5, 0};
            }
            else if (IsClubAwaySection(section.section))
            {
                palettes = new[] {away, awayAlternate}; percentages = new[] {80, 15, 5};
            }
            else
            {
                palettes = new[] {home, homeAlternate}; percentages = new[] {50, 30, 20};
            }

            int count = section.neutralTops.Length;
            int[] categories = AllocateAndShuffle(count, percentages, random);
            int width = Mathf.NextPowerOfTwo(Mathf.Max(1, count));
            var texture = new Texture2D(width, 2, TextureFormat.RGBA32, false, true)
            {
                name = $"Stadium Outfits {(section.upperTier ? "Upper" : "Lower")} {section.section + 1}",
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            var pixels = new Color32[width * 2];
            for (int i = 0; i < count; i++)
            {
                int category = categories[i];
                pixels[i] = category < palettes.Length ? (Color32)palettes[category].Primary : section.neutralTops[i];
                pixels[width + i] = category < palettes.Length ? (Color32)palettes[category].Secondary : section.neutralTrousers[i];
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            outfitTextures.Add(texture);
            var block = new MaterialPropertyBlock();
            block.SetFloat(EnabledId, 1);
            block.SetColor(SeatId, seatColour);
            block.SetTexture(TextureId, texture);
            block.SetFloat(WidthId, width);
            section.spectators.SetPropertyBlock(block);
            section.seats.SetPropertyBlock(block);
        }
        applied = true;
    }

    // Exact quotas to the nearest whole spectator, then Fisher-Yates shuffle.
    // This uses its own RNG and never advances Unity's gameplay random sequence.
    public static int[] AllocateAndShuffle(int count, int[] percentages, System.Random random)
    {
        int sum = 0;
        foreach (int percentage in percentages) sum += percentage;
        if (sum != 100) throw new ArgumentException("Crowd percentages must total 100.");
        var quotas = new int[percentages.Length];
        var remainders = new int[percentages.Length];
        int assigned = 0;
        for (int i = 0; i < quotas.Length; i++)
        {
            quotas[i] = count * percentages[i] / 100;
            remainders[i] = count * percentages[i] % 100;
            assigned += quotas[i];
        }
        while (assigned < count)
        {
            int best = 0;
            for (int i = 1; i < quotas.Length; i++) if (remainders[i] > remainders[best]) best = i;
            quotas[best]++; remainders[best] = -1; assigned++;
        }
        var result = new int[count];
        int cursor = 0;
        for (int i = 0; i < quotas.Length; i++)
            for (int j = 0; j < quotas[i]; j++) result[cursor++] = i;
        for (int i = count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }
        return result;
    }

    private static TokenKitInstructionPalette ClubKit(string kit) =>
        TokenKitCatalog.ResolveInstructionPalette(kit, Hex("244B8A"), Color.white);

    public static TokenKitInstructionPalette NationalKit(string nation, bool alternate)
    {
        // Explicit clothing palettes requested by the designer. Token artwork has
        // different accent colours (e.g. Germany's gold stripe), so leave it alone.
        string name = (nation ?? string.Empty).Trim();
        if (name.Equals("Germany", StringComparison.OrdinalIgnoreCase))
            return new TokenKitInstructionPalette(alternate ? Hex("4CA76F") : Hex("E6E6E6"), alternate ? Hex("E6E6E6") : Hex("1A1A18"));
        if (name.Equals("Netherlands", StringComparison.OrdinalIgnoreCase))
            return new TokenKitInstructionPalette(alternate ? Hex("E6E6E6") : Hex("EF782E"), alternate ? Hex("EF782E") : Hex("E6E6E6"));

        string expected = alternate ? name + " Away" : name;
        foreach (var preset in TokenKitCatalog.GetAllPresets())
            if (string.Equals(preset.DisplayName, expected, StringComparison.OrdinalIgnoreCase))
                return TokenKitCatalog.GetInstructionPalette(preset.Style);
        // A nation without an alternate uses reversed main colours, never the
        // selected match/token kit or its opponent's identity.
        return alternate ? Swap(NationalKit(name, false)) : ClubKit(name);
    }

    private static TokenKitInstructionPalette Swap(TokenKitInstructionPalette palette) => new(palette.Secondary, palette.Primary);
    private static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color result); return result; }
    private static Color RandomSeatColour(System.Random random)
    {
        string[] colours = {"244B8A", "8B282B", "3B6948", "C19A3A", "64527C", "68777D"};
        return Hex(colours[random.Next(colours.Length)]);
    }

    public void ClearColours()
    {
        foreach (var section in sections)
        {
            if (section.seats != null) section.seats.SetPropertyBlock(null);
            if (section.spectators != null) section.spectators.SetPropertyBlock(null);
        }
        foreach (var texture in outfitTextures)
            if (texture != null) { if (Application.isPlaying) Destroy(texture); else DestroyImmediate(texture); }
        outfitTextures.Clear();
        applied = false;
    }

    private void OnDestroy() => ClearColours();
}
