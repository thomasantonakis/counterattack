using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class RoomSaveService
{
    public const int SaveSchemaVersion = 1;
    private const string JsonExtension = ".json";
    private const string ProtectedRoomFixtureSaveFileName = "gv10-dHYf-vRVz-oLwz_2024-11-26_00-28__Single Player__Inverness Caledonian Thistle__Aurora F.C..json";

    public static bool SaveInPlace(MatchManager matchManager, out string savedPath, out string message)
    {
        savedPath = string.Empty;
        ApplicationManager.EnsureInstanceExists();
        string filePath = ApplicationManager.Instance.GetLastSavedFilePath();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            message = "No active save file is selected for this match.";
            return false;
        }

        return SaveToPath(matchManager, filePath, overwrite: true, out savedPath, out message);
    }

    public static bool SaveAs(MatchManager matchManager, string requestedName, bool overwrite, out string savedPath, out string message)
    {
        savedPath = string.Empty;
        string filePath = BuildSaveAsPath(requestedName);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            message = "Enter a save name.";
            return false;
        }

        return SaveToPath(matchManager, filePath, overwrite, out savedPath, out message);
    }

    public static bool SaveToPath(MatchManager matchManager, string filePath, bool overwrite, out string savedPath, out string message)
    {
        savedPath = string.Empty;
        if (matchManager == null)
        {
            message = "MatchManager is not available.";
            return false;
        }

        if (!matchManager.CanCreateRuntimeSnapshot(out string guardReason))
        {
            message = guardReason;
            return false;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            message = "Save path is empty.";
            return false;
        }

        if (IsProtectedSavePath(filePath))
        {
            message = "This save file is a protected editor playtest fixture and cannot be overwritten.";
            return false;
        }

        string directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            message = "Save path has no directory.";
            return false;
        }

        if (File.Exists(filePath) && !overwrite)
        {
            message = "A save with that name already exists.";
            return false;
        }

        try
        {
            Directory.CreateDirectory(directory);
            DateTime nowUtc = DateTime.UtcNow;
            JObject root = File.Exists(filePath)
                ? JObject.Parse(File.ReadAllText(filePath))
                : new JObject();

            MatchManager.GameData gameData = matchManager.gameData ?? new MatchManager.GameData();
            matchManager.EnsureGameplayEventLogInitialized();
            RoomRuntimeSnapshot snapshot = matchManager.CreateRuntimeSnapshot();
            snapshot.savedAtUtc = nowUtc.ToString("o");

            string createdUtc = root.Value<string>("createdUtc");
            if (string.IsNullOrWhiteSpace(createdUtc))
            {
                createdUtc = File.Exists(filePath)
                    ? File.GetCreationTimeUtc(filePath).ToString("o")
                    : nowUtc.ToString("o");
            }

            gameData.saveSchemaVersion = SaveSchemaVersion;
            gameData.createdUtc = createdUtc;
            gameData.lastSavedUtc = nowUtc.ToString("o");
            gameData.runtimeSnapshot = snapshot;
            gameData.eventLogSchemaVersion = GameplayEvent.CurrentSchemaVersion;
            gameData.events ??= new List<GameplayEvent>();

            root["saveSchemaVersion"] = SaveSchemaVersion;
            root["eventLogSchemaVersion"] = gameData.eventLogSchemaVersion;
            root["createdUtc"] = gameData.createdUtc;
            root["lastSavedUtc"] = gameData.lastSavedUtc;
            root["gameSettings"] = JToken.FromObject(gameData.gameSettings ?? new MatchManager.GameSettings());
            root["rosters"] = JToken.FromObject(gameData.rosters ?? new MatchManager.Rosters());
            root["stats"] = JToken.FromObject(gameData.stats ?? new MatchManager.Stats());
            root["gameLog"] = JObject.FromObject(new RoomGameLogJson { entries = snapshot.gameLog ?? new List<string>() });
            root["events"] = JToken.FromObject(gameData.events);
            root["runtimeSnapshot"] = JToken.FromObject(snapshot);

            File.WriteAllText(filePath, root.ToString(Formatting.Indented));
            ApplicationManager.Instance.SetActiveSaveFilePath(filePath);
            PlayerPrefs.SetString("currentGameSettings", filePath);
            PlayerPrefs.Save();

            savedPath = filePath;
            message = $"Saved match to {Path.GetFileName(filePath)}.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Failed to save match: {ex.Message}";
            return false;
        }
    }

    public static string BuildSaveAsPath(string requestedName)
    {
        ApplicationManager.EnsureInstanceExists();
        string sanitizedName = SanitizeFileName(requestedName);
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            return string.Empty;
        }

        if (!sanitizedName.EndsWith(JsonExtension, StringComparison.OrdinalIgnoreCase))
        {
            sanitizedName += JsonExtension;
        }

        return Path.Combine(ApplicationManager.Instance.GetSaveFolderPath(), sanitizedName);
    }

    public static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        string sanitized = new string(value
            .Trim()
            .Select(character => invalidChars.Contains(character) ? '_' : character)
            .ToArray());

        while (sanitized.Contains("__"))
        {
            sanitized = sanitized.Replace("__", "_");
        }

        return sanitized.Trim(' ', '_', '.');
    }

    public static bool SaveFileExistsForName(string requestedName)
    {
        string path = BuildSaveAsPath(requestedName);
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
    }

    public static List<RoomSaveSummary> GetEligibleSaveSummaries(string requiredGameMode = null)
    {
        ApplicationManager.EnsureInstanceExists();
        string saveFolderPath = ApplicationManager.Instance.GetSaveFolderPath();
        if (!Directory.Exists(saveFolderPath))
        {
            return new List<RoomSaveSummary>();
        }

        IEnumerable<string> saveFiles = Directory.GetFiles(saveFolderPath, "*.json", SearchOption.TopDirectoryOnly);

        return saveFiles
            .Where(path => !IsProtectedSavePath(path))
            .Select(path => TryReadSummary(path, requiredGameMode))
            .Where(summary => summary != null)
            .OrderByDescending(summary => summary.LastActivitySortUtc)
            .ToList();
    }

    public static bool IsProtectedSavePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        return string.Equals(Path.GetFileName(filePath), ProtectedRoomFixtureSaveFileName, StringComparison.OrdinalIgnoreCase);
    }

    private static RoomSaveSummary TryReadSummary(string filePath, string requiredGameMode)
    {
        try
        {
            JObject root = JObject.Parse(File.ReadAllText(filePath));
            JObject settings = root["gameSettings"] as JObject;
            if (!IsRoomSave(settings, requiredGameMode) || !HasBothTeamNames(settings))
            {
                return null;
            }

            JObject rosters = root["rosters"] as JObject;
            int homeRosterCount = (rosters?["home"] as JObject)?.Count ?? 0;
            int awayRosterCount = (rosters?["away"] as JObject)?.Count ?? 0;
            if (homeRosterCount <= 10 || awayRosterCount <= 10)
            {
                return null;
            }

            JObject runtime = root["runtimeSnapshot"] as JObject;
            JObject stats = runtime?["stats"] as JObject ?? root["stats"] as JObject;
            JObject homeTeamStats = stats?["homeTeamStats"] as JObject;
            JObject awayTeamStats = stats?["awayTeamStats"] as JObject;
            JObject clock = runtime?["clock"] as JObject;

            DateTime lastWriteUtc = File.GetLastWriteTimeUtc(filePath);
            string createdUtc = root.Value<string>("createdUtc");
            string lastSavedUtc = root.Value<string>("lastSavedUtc");
            string lastLogWrittenUtc = root.Value<string>("lastLogWrittenUtc");
            DateTime lastSavedSortUtc = ParseUtcOrFallback(lastSavedUtc, lastWriteUtc);
            DateTime lastLogWrittenSortUtc = ParseUtcOrFallback(lastLogWrittenUtc, DateTime.MinValue);
            DateTime lastActivitySortUtc = MaxUtc(lastSavedSortUtc, lastLogWrittenSortUtc, lastWriteUtc);
            bool hasRuntimeSnapshot = runtime != null && runtime.Type != JTokenType.Null;
            bool isMatchComplete = clock?.Value<bool?>("isMatchComplete") ?? false;

            int homeGoals = homeTeamStats?.Value<int?>("totalGoals") ?? 0;
            int awayGoals = awayTeamStats?.Value<int?>("totalGoals") ?? 0;
            int halfDuration = settings?.Value<int?>("halfDuration") ?? 0;
            int numberOfHalfs = settings?.Value<int?>("numberOfHalfs") ?? 0;

            return new RoomSaveSummary
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                Teams = $"{ReadString(settings, "homeTeamName", "Home")} - {ReadString(settings, "awayTeamName", "Away")}",
                Clock = hasRuntimeSnapshot ? FormatClock(clock, halfDuration) : "-",
                Score = $"{homeGoals}-{awayGoals}",
                HalfLength = halfDuration > 0 ? halfDuration.ToString() : "-",
                Halves = numberOfHalfs > 0 ? numberOfHalfs.ToString() : "-",
                MatchDuration = halfDuration > 0 && numberOfHalfs > 0 ? $"{numberOfHalfs}x{halfDuration}" : "-",
                Tie = FormatTieBreaker(ReadString(settings, "tiebreaker", "-")),
                CreatedUtc = string.IsNullOrWhiteSpace(createdUtc) ? File.GetCreationTimeUtc(filePath).ToString("o") : createdUtc,
                LastSavedUtc = string.IsNullOrWhiteSpace(lastSavedUtc) ? lastWriteUtc.ToString("o") : lastSavedUtc,
                LastLogWrittenUtc = lastLogWrittenUtc,
                LastActivityUtc = lastActivitySortUtc == DateTime.MinValue ? lastWriteUtc.ToString("o") : lastActivitySortUtc.ToString("o"),
                Status = FormatSummaryStatus(hasRuntimeSnapshot, isMatchComplete),
                HasRuntimeSnapshot = hasRuntimeSnapshot,
                LastSavedSortUtc = lastSavedSortUtc,
                LastActivitySortUtc = lastActivitySortUtc,
                LastWriteUtc = lastWriteUtc
            };
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not read save summary for {filePath}: {ex.Message}");
            return null;
        }
    }

    private static string FormatSummaryStatus(bool hasRuntimeSnapshot, bool isMatchComplete)
        => !hasRuntimeSnapshot ? "Setup Only" : isMatchComplete ? "Match Complete" : "Runtime Snapshot";

    private static DateTime MaxUtc(params DateTime[] values)
    {
        return values
            .Where(value => value != DateTime.MinValue)
            .Select(value => value.ToUniversalTime())
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();
    }

    private static bool IsRoomSave(JObject settings, string requiredGameMode)
    {
        string gameMode = settings?.Value<string>("gameMode");
        string normalizedGameMode = NormalizeSaveType(gameMode);
        if (string.IsNullOrWhiteSpace(normalizedGameMode))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(requiredGameMode)
            || normalizedGameMode == NormalizeSaveType(requiredGameMode);
    }

    private static bool HasBothTeamNames(JObject settings)
    {
        return !string.IsNullOrWhiteSpace(settings?.Value<string>("homeTeamName"))
            && !string.IsNullOrWhiteSpace(settings?.Value<string>("awayTeamName"));
    }

    private static string NormalizeSaveType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        char[] buffer = new char[value.Length];
        int writeIndex = 0;
        foreach (char character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                buffer[writeIndex] = char.ToLowerInvariant(character);
                writeIndex++;
            }
        }

        return new string(buffer, 0, writeIndex);
    }

    private static DateTime ParseUtcOrFallback(string value, DateTime fallbackUtc)
    {
        if (DateTime.TryParse(value, out DateTime parsed))
        {
            return parsed.ToUniversalTime();
        }

        return fallbackUtc;
    }

    private static string ReadString(JObject source, string key, string fallback)
    {
        string value = source?.Value<string>(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string FormatClock(JObject clock, int configuredHalfDurationMinutes)
    {
        if (clock == null)
        {
            return "-";
        }

        string pendingGoalTimeLabel = clock.Value<string>("pendingShotGoalTimeLabel");
        if (!string.IsNullOrWhiteSpace(pendingGoalTimeLabel))
        {
            return pendingGoalTimeLabel;
        }

        int currentHalf = Mathf.Max(1, clock.Value<int?>("currentHalf") ?? 1);
        int halfDurationMinutes = configuredHalfDurationMinutes > 0 ? configuredHalfDurationMinutes : 45;
        bool isHalfExpired = clock.Value<bool?>("isHalfExpired") ?? false;
        bool extraActionsDetermined = clock.Value<bool?>("extraActionsDetermined") ?? false;

        if (isHalfExpired && extraActionsDetermined)
        {
            int extraMinute = ResolveDisplayedExtraMinute(clock);
            if (extraMinute > 0)
            {
                return $"{currentHalf * halfDurationMinutes}'+{extraMinute}'";
            }
        }

        float currentHalfSeconds = Mathf.Max(0f, clock.Value<float?>("currentHalfRegulationSeconds") ?? 0f);
        int priorHalfSeconds = (currentHalf - 1) * halfDurationMinutes * 60;
        return FormatClockSeconds(priorHalfSeconds + Mathf.FloorToInt(currentHalfSeconds));
    }

    private static int ResolveDisplayedExtraMinute(JObject clock)
    {
        int committedExtraActionNumber = clock.Value<int?>("committedExtraActionNumber") ?? 0;
        if (committedExtraActionNumber > 0)
        {
            return committedExtraActionNumber;
        }

        int extraActionsTotal = Mathf.Max(0, clock.Value<int?>("extraActionsTotal") ?? 0);
        int extraActionsRemaining = Mathf.Max(0, clock.Value<int?>("extraActionsRemaining") ?? 0);
        return Mathf.Clamp(extraActionsTotal - extraActionsRemaining, 0, extraActionsTotal);
    }

    private static string FormatClockSeconds(int totalSeconds)
    {
        int safeSeconds = Mathf.Max(0, totalSeconds);
        int minutes = safeSeconds / 60;
        int seconds = safeSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    private static string FormatTieBreaker(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "-")
        {
            return "-";
        }

        string normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "none" => "N",
            "extra time" => "E",
            "penalties" => "P",
            "extra time & penalties" => "EP",
            "extra time + penalties" => "EP",
            _ => value
        };
    }

    private sealed class RoomGameLogJson
    {
        public List<string> entries = new();
    }
}

[Serializable]
public class RoomSaveSummary
{
    public string FilePath;
    public string FileName;
    public string Teams;
    public string Clock;
    public string Score;
    public string HalfLength;
    public string Halves;
    public string MatchDuration;
    public string Tie;
    public string CreatedUtc;
    public string LastSavedUtc;
    public string LastLogWrittenUtc;
    public string LastActivityUtc;
    public string Status;
    public bool HasRuntimeSnapshot;
    public DateTime LastSavedSortUtc;
    public DateTime LastActivitySortUtc;
    public DateTime LastWriteUtc;
}

[Serializable]
public class RoomRuntimeSnapshot
{
    public int version = RoomSaveService.SaveSchemaVersion;
    public string savedAtUtc;
    public string currentState;
    public string teamInAttack;
    public bool attackHasPossession;
    public string homeTeamDirection;
    public string awayTeamDirection;
    public RoomClockSnapshot clock = new();
    public RoomHexCoordinates ball;
    public List<RoomTokenSnapshot> tokens = new();
    public RoomTouchReferences touchReferences = new();
    public RoomSubstitutionSnapshot substitutions = new();
    public MatchManager.Stats stats;
    public List<MatchManager.GoalEvent> homeScorers = new();
    public List<MatchManager.GoalEvent> awayScorers = new();
    public List<string> gameLog = new();
}

[Serializable]
public class RoomClockSnapshot
{
    public int currentHalf;
    public float currentHalfRegulationSeconds;
    public int completedActionsThisHalf;
    public int totalCompletedActions;
    public bool isClockRunning;
    public bool isHalfExpired;
    public bool extraActionsDetermined;
    public int extraActionsTotal;
    public int extraActionsRemaining;
    public bool isWaitingForExtraActionsRoll;
    public bool isMatchComplete;
    public string currentCommittedActionKind;
    public bool hasUnresolvedCommittedExtraAction;
    public int committedExtraActionNumber;
    public string pendingShotGoalTimeLabel;
    public int pendingShotGoalExtraActionNumber;
    public int pendingGroundBallDistance;
}

[Serializable]
public class RoomTokenSnapshot
{
    public string tokenKey;
    public string teamSide;
    public int jerseyNumber;
    public string playerName;
    public RoomHexCoordinates currentHex;
    public bool isPlaying;
    public bool wasSubbedOn;
    public bool wasSubbedOff;
    public bool isSentOff;
    public bool isBooked;
    public bool isInjured;
    public bool requiresSubstitution;
    public bool isGoalKeeper;
    public bool isAttacker;
    public int pace;
    public int dribbling;
    public int highPass;
    public int resilience;
    public int heading;
    public int shooting;
    public int tackling;
    public int aerial;
    public int saving;
    public int handling;
}

[Serializable]
public class RoomTouchReferences
{
    public RoomTokenReference lastTokenToTouchTheBallOnPurpose;
    public RoomTokenReference previousTokenToTouchTheBallOnPurpose;
    public RoomTokenReference pendingGoalKickRestartTaker;
    public RoomTokenReference setPieceTakerExcludedFromNextTouch;
    public string hangingPassType;
    public RoomTokenReference hangingPassExcludedCollector;
    public bool clearPreviousOnNextBallCollection;
}

[Serializable]
public class RoomSubstitutionSnapshot
{
    public int homeSubstitutionsUsed;
    public int awaySubstitutionsUsed;
    public bool extraTimeSubstitutionCreditGranted;
    public bool areSubstitutionsAvailable;
    public string substitutionsAvailabilityReason;
    public bool goalkeeperReplacementRequired;
    public bool goalkeeperReplacementTeamIsHome;
    public bool emergencyGoalkeeperNominationRequired;
    public bool emergencyGoalkeeperNominationTeamIsHome;
    public string emergencyGoalkeeperNominationReason;
}

[Serializable]
public class RoomTokenReference
{
    public string tokenKey;
    public string teamSide;
    public int jerseyNumber;
    public string playerName;
}

[Serializable]
public class RoomHexCoordinates
{
    public int x;
    public int z;

    public static RoomHexCoordinates FromHex(HexCell hex)
    {
        if (hex == null)
        {
            return null;
        }

        return new RoomHexCoordinates
        {
            x = hex.coordinates.x,
            z = hex.coordinates.z
        };
    }

    public Vector3Int ToVector3Int()
    {
        return new Vector3Int(x, 0, z);
    }
}
