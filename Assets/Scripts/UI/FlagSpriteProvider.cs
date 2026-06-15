using System;
using System.Collections.Generic;
using UnityEngine;

public static class FlagSpriteProvider
{
    private const string MapResourcePath = "FlagCountryMap";

    private static Dictionary<string, string> countryToResourcePath;
    private static readonly Dictionary<string, Sprite> SpriteCache = new(StringComparer.OrdinalIgnoreCase);

    public static Sprite GetFlagSprite(string country)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            return null;
        }

        EnsureMapLoaded();

        string key = country.Trim();
        if (!countryToResourcePath.TryGetValue(key, out string resourcePath)
            || string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        if (SpriteCache.TryGetValue(resourcePath, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        SpriteCache[resourcePath] = sprite;
        return sprite;
    }

    private static void EnsureMapLoaded()
    {
        if (countryToResourcePath != null)
        {
            return;
        }

        countryToResourcePath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        TextAsset mapAsset = Resources.Load<TextAsset>(MapResourcePath);
        if (mapAsset == null)
        {
            return;
        }

        FlagCountryMap map = JsonUtility.FromJson<FlagCountryMap>(mapAsset.text);
        if (map?.flags == null)
        {
            return;
        }

        foreach (FlagCountryEntry entry in map.flags)
        {
            if (entry == null
                || string.IsNullOrWhiteSpace(entry.country)
                || string.IsNullOrWhiteSpace(entry.assetResourcePath))
            {
                continue;
            }

            countryToResourcePath[entry.country.Trim()] = entry.assetResourcePath.Trim();
        }
    }

    [Serializable]
    private sealed class FlagCountryMap
    {
        public FlagCountryEntry[] flags;
    }

    [Serializable]
    private sealed class FlagCountryEntry
    {
        public string country;
        public string assetResourcePath;
    }
}
