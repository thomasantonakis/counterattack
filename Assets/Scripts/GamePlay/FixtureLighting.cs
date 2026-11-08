using System;
using System.Collections.Generic;

public enum FixtureLighting { MidAfternoon, Sunset, LateNight }

public static class FixtureLightingOptions
{
    public static readonly List<string> Labels = new() { "Mid afternoon", "Sunset", "Late night" };

    public static FixtureLighting Parse(string value) => value switch
    {
        "Sunset" => FixtureLighting.Sunset,
        "LateNight" => FixtureLighting.LateNight,
        _ => FixtureLighting.MidAfternoon
    };

    public static string Normalize(string value) => Parse(value).ToString();
    public static string FromIndex(int index) => index >= 0 && index < 3
        ? ((FixtureLighting)index).ToString() : nameof(FixtureLighting.MidAfternoon);
    public static string Label(string value) => Labels[(int)Parse(value)];
}
