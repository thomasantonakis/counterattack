using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CounterAttack.Online
{
    internal static class OnlineRoomSettings
    {
        internal const string Prefix = "settings:";
        internal enum Field { Tiebreaker, Assistance, Weather, Lighting, Ball, HomeKit, AwayKit, HomeKeeper, AwayKeeper }
        internal static readonly Field[] Fields = (Field[])Enum.GetValues(typeof(Field));

        private static readonly string[] Tiebreakers = { "None", "Penalties", "Extra Time", "Extra Time & Penalties" };
        private static readonly string[] Weathers = { "Clear", "Rain", "Snow" };
        private static readonly string[] Lights = { "MidAfternoon", "Sunset", "LateNight" };
        private static readonly string[] Balls = { "White", "Orange", "Yellow" };
        private static readonly string[] Assistance = { "Novice", "Intermediate", "Experienced" };

        internal static string[] Capture(MatchManager.GameSettings settings) => new[]
        {
            Array.IndexOf(Tiebreakers, settings.tiebreaker).ToString(CultureInfo.InvariantCulture),
            settings.playerAssistance.ToString(CultureInfo.InvariantCulture),
            settings.weatherConditions, FixtureLightingOptions.Normalize(settings.fixtureLighting), settings.ballColor,
            KitId(settings.homeKit), KitId(settings.awayKit), KitId(settings.homeGKKit), KitId(settings.awayGKKit)
        };

        private static string KitId(string name) => TokenKitCatalog.GetPresetByIdOrAlias(name)?.Id;

        internal static string Action(Field field, string value) => Prefix + (int)field + ":" + value;

        internal static IEnumerable<(string value, string label)> Options(Field field, MatchManager.GameSettings settings)
        {
            switch (field)
            {
                case Field.Tiebreaker:
                    int count = settings.halfDuration == 45 && settings.numberOfHalfs == 2 ? Tiebreakers.Length : 2;
                    return Tiebreakers.Take(count).Select((label, index) => (index.ToString(CultureInfo.InvariantCulture), label));
                case Field.Assistance:
                    return Assistance.Select((label, index) => ((index + 1).ToString(CultureInfo.InvariantCulture), label));
                case Field.Weather: return Weathers.Select(value => (value, value));
                case Field.Lighting: return Lights.Select(value => (value, FixtureLightingOptions.Label(value)));
                case Field.Ball:
                    return Balls.Where(value => settings.weatherConditions != "Snow" || value == "Orange").Select(value => (value, value));
                default:
                    bool keeper = field is Field.HomeKeeper or Field.AwayKeeper;
                    var kits = TokenKitCatalog.GetAllPresets().Where(kit =>
                        kit != null && kit.Id.StartsWith("gk", StringComparison.OrdinalIgnoreCase) == keeper);
                    if (!keeper && settings.matchType == "International")
                    {
                        string team = field == Field.HomeKit ? settings.homeTeamName : settings.awayTeamName;
                        var national = kits.Where(kit => string.Equals(kit.DisplayName, team, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(kit.DisplayName, team + " Away", StringComparison.OrdinalIgnoreCase)).ToArray();
                        if (national.Length > 0) kits = national;
                    }
                    return kits.Select(kit => (kit.Id, kit.DisplayName));
            }
        }

        internal static bool TryApplyCommand(MatchManager match, string action)
        {
            var parts = action.Split(':');
            if (parts.Length != 3 || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int index)
                || index < 0 || index >= Fields.Length || action != Action((Field)index, parts[2])) return false;
            var field = (Field)index;
            var settings = match.gameData.gameSettings;
            if (field == Field.Tiebreaker && !match.CanChangeTiebreaker()
                || !Options(field, settings).Any(option => option.value == parts[2])) return false;
            var values = Capture(settings);
            values[index] = parts[2];
            if (field == Field.Weather && parts[2] == "Snow") values[(int)Field.Ball] = "Orange";
            return Apply(match, values);
        }

        internal static bool Valid(string[] values)
        {
            if (values == null) return true;
            if (values.Length != Fields.Length || !int.TryParse(values[0], out int tiebreaker) || tiebreaker is < 0 or > 3
                || !int.TryParse(values[1], out int assistance) || assistance is < 1 or > 3
                || !Weathers.Contains(values[2]) || !Lights.Contains(values[3]) || !Balls.Contains(values[4])
                || values[2] == "Snow" && values[4] != "Orange") return false;
            var home = TokenKitCatalog.GetPresetByIdOrAlias(values[5]);
            var away = TokenKitCatalog.GetPresetByIdOrAlias(values[6]);
            var homeKeeper = TokenKitCatalog.GetPresetByIdOrAlias(values[7]);
            var awayKeeper = TokenKitCatalog.GetPresetByIdOrAlias(values[8]);
            return home != null && away != null && homeKeeper != null && awayKeeper != null
                && !home.Id.StartsWith("gk") && !away.Id.StartsWith("gk")
                && homeKeeper.Id.StartsWith("gk") && awayKeeper.Id.StartsWith("gk");
        }

        internal static bool Apply(MatchManager match, string[] values)
        {
            if (values == null || !Valid(values)) return false;
            var settings = match.gameData.gameSettings;
            if (Capture(settings).SequenceEqual(values)) return true;
            if (!string.IsNullOrEmpty(CreateNewGameManager.ValidateSelectedKits(
                    TokenKitCatalog.GetPresetByIdOrAlias(values[5]), TokenKitCatalog.GetPresetByIdOrAlias(values[6]),
                    TokenKitCatalog.GetPresetByIdOrAlias(values[7]), TokenKitCatalog.GetPresetByIdOrAlias(values[8]),
                    TokenKitCatalog.GetSimilarityBreakdown(values[5], values[6])))) return false;
            settings.tiebreaker = Tiebreakers[int.Parse(values[0], CultureInfo.InvariantCulture)];
            settings.playerAssistance = int.Parse(values[1], CultureInfo.InvariantCulture);
            settings.weatherConditions = values[2];
            settings.fixtureLighting = values[3];
            settings.ballColor = values[4];
            settings.homeKit = values[5]; settings.awayKit = values[6];
            settings.homeGKKit = values[7]; settings.awayGKKit = values[8];
            match.ApplyLiveGameSettingsChanges();
            return true;
        }
    }
}
