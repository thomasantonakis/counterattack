using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>Stable, three-character scoreboard codes for both known and newly entered teams.</summary>
public static class TeamNameAbbreviator
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private static readonly HashSet<string> GenericWords = new(StringComparer.Ordinal)
    {
        "FC", "AFC", "CF", "SC", "AC", "AS", "FK", "SK", "KV", "UD", "IF", "USC", "CD", "FCD", "THE", "CLUB", "FOOTBALL", "UNITED", "XI"
    };
    private static readonly HashSet<string> IgnoredNameWords = new(StringComparer.Ordinal) { "UNITED", "FC", "XI" };
    private static readonly Dictionary<string, string> StandardExceptions = new(StringComparer.Ordinal)
    {
        ["SCONTARIO"] = "SCO",
        ["FCDVIGNAREAL"] = "FCD"
    };

    [Serializable]
    private sealed class Preset
    {
        public string teamName { get; set; }
        public string abbreviation { get; set; }
        public bool international { get; set; }
        public List<string> previousAbbreviations { get; set; }
    }

    private static readonly Dictionary<string, Preset> Presets = LoadPresets();
    private static readonly Dictionary<string, Preset> PresetAliases = Presets.Values
        .GroupBy(preset => IgnoredKey(preset.teamName), StringComparer.Ordinal)
        .Where(group => group.Key.Length > 0)
        .ToDictionary(group => group.Key, group => group.OrderByDescending(preset => preset.international).First(), StringComparer.Ordinal);

    public static string Abbreviate(string teamName)
    {
        return Candidates(teamName).First();
    }

    /// <summary>Identical codes, or codes differing in only one position, are too close.</summary>
    public static bool AreTooSimilar(string first, string second)
    {
        if (!IsValid(first) || !IsValid(second)) return true;
        int differences = 0;
        for (int i = 0; i < 3; i++)
        {
            if (VisualCharacter(first[i]) != VisualCharacter(second[i])) differences++;
        }
        return differences < 2;
    }

    public static (string home, string away) CreatePair(
        string homeName, string awayName, string savedHome = null, string savedAway = null)
    {
        bool homeKnown = TryGetPreset(homeName, out Preset homePreset);
        bool awayKnown = TryGetPreset(awayName, out Preset awayPreset);
        bool homeInternational = homeKnown && homePreset.international;
        bool awayInternational = awayKnown && awayPreset.international;
        string home = homeInternational ? homePreset.abbreviation : ResolveSavedCode(homeName, homePreset, savedHome);
        string away = awayInternational ? awayPreset.abbreviation : ResolveSavedCode(awayName, awayPreset, savedAway);

        // Repair only the old generated collision suffix, not a designer's saved
        // override. A literal short identity should not lose its letters/digits
        // when the longer opponent has a natural alternative.
        if (!awayInternational && IsLiteralCode(awayName) && away != Abbreviate(awayName)
            && away == FindDistinct(awayName, Abbreviate(homeName))) away = Abbreviate(awayName);
        if (!homeInternational && IsLiteralCode(homeName) && home != Abbreviate(homeName)
            && home == FindDistinct(homeName, Abbreviate(awayName))) home = Abbreviate(homeName);

        // Official football codes (e.g. BRA/FRA) take precedence over the similarity rule.
        if (!AreTooSimilar(home, away) || (homeInternational && awayInternational)) return (home, away);

        bool preserveAway = awayInternational
            || (!homeInternational && ((IsLiteralCode(awayName) && !IsLiteralCode(homeName))
                || (IsValid(savedAway) && !IsValid(savedHome)) || (awayKnown && !homeKnown)));
        // Exhaust natural alternatives on either eligible side before hashing a
        // name. Official international identities remain immutable.
        string alternative = preserveAway ? NaturalDistinct(homeName, away) : NaturalDistinct(awayName, home);
        if (alternative != null)
        {
            if (preserveAway) home = alternative; else away = alternative;
        }
        else if (preserveAway && !awayInternational && NaturalDistinct(awayName, home) is string awayAlternative)
            away = awayAlternative;
        else if (!preserveAway && !homeInternational && NaturalDistinct(homeName, away) is string homeAlternative)
            home = homeAlternative;
        else if (preserveAway) home = FindDistinct(homeName, away);
        else away = FindDistinct(awayName, home);
        return (home, away);
    }

    private static bool IsLiteralCode(string name) => IsValid(Normalize(name).Trim());

    private static string NaturalDistinct(string name, string other) => Candidates(name)
        .Distinct(StringComparer.Ordinal).FirstOrDefault(candidate => !AreTooSimilar(candidate, other));

    /// <summary>Backfills legacy saves in memory and retains already differentiated saved codes.</summary>
    public static void EnsureAbbreviations(MatchManager.GameSettings settings)
    {
        if (settings == null) return;
        var pair = CreatePair(settings.homeTeamName, settings.awayTeamName,
            settings.homeTeamAbbreviation, settings.awayTeamAbbreviation);
        settings.homeTeamAbbreviation = pair.home;
        settings.awayTeamAbbreviation = pair.away;
    }

    public static void EnsureAbbreviations(GameSettings settings)
    {
        if (settings == null) return;
        var pair = CreatePair(settings.homeTeamName, settings.awayTeamName,
            settings.homeTeamAbbreviation, settings.awayTeamAbbreviation);
        settings.homeTeamAbbreviation = pair.home;
        settings.awayTeamAbbreviation = pair.away;
    }

    private static string FindDistinct(string name, string other)
    {
        foreach (string candidate in Candidates(name).Distinct(StringComparer.Ordinal))
        {
            if (!AreTooSimilar(candidate, other)) return candidate;
        }

        // Very short or symbol-only names still have a bounded, repeatable fallback.
        char first = Abbreviate(name)[0];
        uint hash = 2166136261;
        foreach (char character in name ?? string.Empty) hash = unchecked((hash ^ character) * 16777619);
        int start = (int)(hash % (Alphabet.Length * Alphabet.Length));
        for (int i = 0; i < Alphabet.Length * Alphabet.Length; i++)
        {
            int suffix = (start + i) % (Alphabet.Length * Alphabet.Length);
            string candidate = new(new[] { first, Alphabet[suffix / Alphabet.Length], Alphabet[suffix % Alphabet.Length] });
            if (!AreTooSimilar(candidate, other)) return candidate;
        }
        throw new InvalidOperationException("Could not distinguish the scoreboard team codes.");
    }

    private static string ResolveSavedCode(string name, Preset preset, string saved)
    {
        // Refresh superseded base codes when rules change, retaining pair-specific alternatives.
        return IsValid(saved) && preset?.previousAbbreviations?.Contains(saved) != true
            ? saved : Abbreviate(name);
    }

    private static IEnumerable<string> Candidates(string name)
    {
        TryGetPreset(name, out Preset preset);
        string[] allWords = Regex.Matches(Normalize(name), "[A-Z0-9]+").Cast<Match>().Select(match => match.Value).ToArray();
        string[] words = allWords.Where(word => !GenericWords.Contains(word)).ToArray();
        bool hasMeaningfulWords = words.Length > 0;
        if (words.Length == 0) words = allWords.Length > 0 ? allWords : new[] { "TEAM" };
        string first = words[0];
        string last = words[words.Length - 1];
        string initials = string.Concat(words.Select(word => word[0]));
        if (preset?.international == true) yield return preset.abbreviation;
        string exceptionKey = string.Concat(allWords.Where(word => !IgnoredNameWords.Contains(word)));
        if (StandardExceptions.TryGetValue(exceptionKey, out string exception)) yield return exception;
        // Explicit ignored words and three-word initialisms take precedence over older presets.
        if (hasMeaningfulWords && (allWords.Any(IgnoredNameWords.Contains) || words.Length == 3))
        {
            if (words.Length >= 3) yield return Three(initials);
            else if (words.Length == 2) yield return Three(first.Substring(0, Math.Min(2, first.Length)) + last[0]);
            else yield return Three(first);
        }
        if (preset != null) yield return preset.abbreviation;
        if (words.Length >= 3) yield return Three(initials);
        else if (words.Length == 2) yield return Three(first.Substring(0, Math.Min(2, first.Length)) + last[0]);
        yield return Three(first);
        if (words.Length > 1)
        {
            yield return Three(words[0][0] + last.Substring(0, Math.Min(2, last.Length)));
            yield return Three(initials + string.Concat(words.SelectMany(word => word.Skip(1))));
            yield return Three(last);
        }
        yield return Three(first[0] + last.Substring(Math.Max(0, last.Length - 2)));
        // Try meaningful letters from the name before adding an alphanumeric suffix.
        string letters = new(string.Concat(words).Distinct().Take(24).ToArray());
        for (int i = 1; i < letters.Length - 1; i++)
        {
            for (int j = i + 1; j < letters.Length; j++)
                yield return new string(new[] { letters[0], letters[i], letters[j] });
        }
    }

    private static string Three(string letters) => (letters + "XXX").Substring(0, 3);

    private static bool IsValid(string code) => code != null && code.Length == 3
        && code.All(character => (character >= 'A' && character <= 'Z') || (character >= '0' && character <= '9'));

    private static char VisualCharacter(char character) => character switch
    {
        '0' => 'O', '1' => 'I', '5' => 'S', '8' => 'B', '2' => 'Z', '6' => 'G', _ => character
    };

    private static string Key(string name) => Regex.Replace(Normalize(name), "[^A-Z0-9]", string.Empty);

    private static string IgnoredKey(string name) => string.Concat(Regex.Matches(Normalize(name), "[A-Z0-9]+")
        .Cast<Match>().Select(match => match.Value).Where(word => !IgnoredNameWords.Contains(word)));

    private static bool TryGetPreset(string name, out Preset preset) => Presets.TryGetValue(Key(name), out preset)
        || PresetAliases.TryGetValue(IgnoredKey(name), out preset);

    private static string Normalize(string name)
    {
        string text = (name ?? string.Empty).Replace("ß", "SS").Replace("ẞ", "SS")
            .Replace("Æ", "AE").Replace("æ", "AE").Replace("Œ", "OE").Replace("œ", "OE")
            .Replace("Ø", "O").Replace("ø", "O").Replace("Ł", "L").Replace("ł", "L")
            .Replace("Đ", "D").Replace("đ", "D").Normalize(NormalizationForm.FormD).ToUpperInvariant();
        var builder = new StringBuilder(text.Length);
        foreach (char character in text)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            if (character == '.' || character == '\'' || character == '’') continue;
            builder.Append((character >= 'A' && character <= 'Z') || (character >= '0' && character <= '9') ? character : ' ');
        }
        return builder.ToString();
    }

    private static Dictionary<string, Preset> LoadPresets()
    {
        var result = new Dictionary<string, Preset>(StringComparer.Ordinal);
        TextAsset asset = Resources.Load<TextAsset>("UI/team-name-abbreviations");
        if (asset == null) return result;
        foreach (Preset preset in JsonConvert.DeserializeObject<List<Preset>>(asset.text))
        {
            if (!string.IsNullOrWhiteSpace(preset.teamName) && IsValid(preset.abbreviation))
                result.Add(Key(preset.teamName), preset);
        }
        return result;
    }
}
