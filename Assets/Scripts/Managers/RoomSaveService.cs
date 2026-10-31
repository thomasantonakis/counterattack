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
    private const string PlaySessionsFolderName = "PlaySessions";
    private const string PlaySessionKind = "PlaySession";
    private const string ManualSaveKind = "Manual";
    private const string ProtectedRoomFixtureSaveFileName = "gv10-dHYf-vRVz-oLwz_2024-11-26_00-28__Single Player__Inverness Caledonian Thistle__Aurora F.C..json";

    // Only compact browser metadata is retained, never event/decision histories.
    private static readonly Dictionary<string, CachedSummaryMetadata> SummaryMetadataCache = new(StringComparer.Ordinal);

    private sealed class CachedSummaryMetadata
    {
        public long Length;
        public DateTime LastWriteUtc;
        public JObject Metadata;
    }

    /// <summary>
    /// Loading starts a new play-through. Never append its logs or autosaves to
    /// the selected seed, even when the same pre-kickoff save is loaded again.
    /// </summary>
    public static bool CreateLoadedRunCopy(string sourcePath, out string runPath, out string message)
    {
        runPath = string.Empty;
        try
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                message = "The selected match save no longer exists.";
                return false;
            }

            JObject root = JObject.Parse(File.ReadAllText(sourcePath));
            if (root["gameSettings"] is not JObject)
            {
                message = "The selected file is not a match save.";
                return false;
            }

            // Live logging patches top-level counters/events without changing
            // the resumable snapshot. Restore the saved baseline, not activity
            // from a previous play-through that happened after that snapshot.
            JObject snapshot = root["runtimeSnapshot"] as JObject;
            string savedAt = snapshot?.Value<string>("savedAtUtc");
            if (snapshot != null)
            {
                if (snapshot["stats"] is JObject stats) root["stats"] = stats.DeepClone();
                if (snapshot["gameLog"] is JArray log)
                    root["gameLog"] = new JObject { ["entries"] = log.DeepClone() };
                if (DateTimeOffset.TryParse(savedAt, out DateTimeOffset cutoff) && root["events"] is JArray events)
                {
                    root["events"] = new JArray(events.Where(entry =>
                        DateTimeOffset.TryParse(entry.Value<string>("timestampUtc"), out DateTimeOffset at)
                        && at <= cutoff).Select(entry => entry.DeepClone()));
                }
            }

            string id = Guid.NewGuid().ToString("N");
            DateTime now = DateTime.UtcNow;
            string stem = Path.GetFileNameWithoutExtension(sourcePath);
            int previousRun = stem.LastIndexOf("__Run_", StringComparison.Ordinal);
            if (previousRun >= 0) stem = stem.Substring(0, previousRun);
            string manualSavePath = ResolveManualSavePath(sourcePath);
            string sessionFolder = Path.Combine(Path.GetDirectoryName(manualSavePath), PlaySessionsFolderName);
            Directory.CreateDirectory(sessionFolder);
            runPath = Path.Combine(sessionFolder,
                $"{stem}__Run_{now:yyyyMMddTHHmmssfffZ}_{id.Substring(0, 8)}.json");
            root["saveKind"] = PlaySessionKind;
            root["manualSavePath"] = manualSavePath;
            root["playSessionId"] = id;
            root["playSessionStartedUtc"] = now.ToString("o");
            root["sourceSavePath"] = Path.GetFullPath(sourcePath);
            root["sourceSaveSavedAtUtc"] = savedAt;
            root["inheritedEventSequence"] = (root["events"] as JArray)?
                .Select(entry => entry.Value<int?>("sequenceNumber") ?? 0).DefaultIfEmpty(0).Max() ?? 0;
            root["createdUtc"] = now.ToString("o");
            root.Remove("lastLogWrittenUtc");
            using (var stream = new FileStream(runPath, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
                writer.Write(root.ToString(Formatting.Indented));

            message = $"Loaded a new play-through: {runPath} (seed unchanged: {sourcePath}).";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Could not create a separate play-through: {ex.Message}";
            runPath = string.Empty;
            return false;
        }
    }

    public static bool SaveInPlace(MatchManager matchManager, out string savedPath, out string message)
    {
        savedPath = string.Empty;
        ApplicationManager.EnsureInstanceExists();
        string activePath = ApplicationManager.Instance.GetLastSavedFilePath();
        if (string.IsNullOrWhiteSpace(activePath))
        {
            message = "No active save file is selected for this match.";
            return false;
        }

        try
        {
            return SaveToPath(matchManager, ResolveManualSavePath(activePath), overwrite: true, out savedPath, out message);
        }
        catch (Exception ex)
        {
            message = $"Could not resolve the original save slot: {ex.Message}";
            return false;
        }
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
            ApplicationManager.EnsureInstanceExists();
            string activePath = ApplicationManager.Instance.GetLastSavedFilePath();
            Directory.CreateDirectory(directory);
            DateTime nowUtc = DateTime.UtcNow;
            JObject root = File.Exists(filePath)
                ? JObject.Parse(File.ReadAllText(filePath))
                : new JObject();

            MatchManager.GameData gameData = matchManager.gameData ?? new MatchManager.GameData();
            TeamNameAbbreviator.EnsureAbbreviations(gameData.gameSettings);
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
            root["playSessionId"] = gameData.playSessionId;
            root["playSessionStartedUtc"] = gameData.playSessionStartedUtc;
            root["sourceSavePath"] = gameData.sourceSavePath;
            root["sourceSaveSavedAtUtc"] = gameData.sourceSaveSavedAtUtc;
            root["inheritedEventSequence"] = gameData.inheritedEventSequence;
            root["gameSettings"] = JToken.FromObject(gameData.gameSettings ?? new MatchManager.GameSettings());
            root["rosters"] = JToken.FromObject(gameData.rosters ?? new MatchManager.Rosters());
            root["stats"] = JToken.FromObject(gameData.stats ?? new MatchManager.Stats());
            root["gameLog"] = JObject.FromObject(new RoomGameLogJson { entries = snapshot.gameLog ?? new List<string>() });
            root["events"] = JToken.FromObject(gameData.events);
            root["runtimeSnapshot"] = JToken.FromObject(snapshot);

            string retainedActivePath = WriteManualSave(root, filePath, activePath);
            ApplicationManager.Instance.SetActiveSaveFilePath(retainedActivePath);
            PlayerPrefs.SetString("currentGameSettings", retainedActivePath);
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

        string[] saveFiles = Directory.GetFiles(saveFolderPath, "*.json", SearchOption.TopDirectoryOnly)
            .Where(path => !IsProtectedSavePath(path) && !IsDiagnosticCompanionPath(path))
            .ToArray();
        var currentPaths = new HashSet<string>(saveFiles, StringComparer.Ordinal);
        foreach (string cachedPath in SummaryMetadataCache.Keys.Where(path => !currentPaths.Contains(path)).ToArray())
        {
            SummaryMetadataCache.Remove(cachedPath);
        }

        return saveFiles
            .Select(path => TryReadSummary(path, requiredGameMode))
            .Where(summary => summary != null)
            .OrderByDescending(summary => summary.LastActivitySortUtc)
            .ToList();
    }

    public static bool DeleteManualSave(string filePath, string requiredGameMode, out string message)
    {
        ApplicationManager.EnsureInstanceExists();
        string folder = ApplicationManager.Instance.GetSaveFolderPath();
        if (!TryDeleteManualSave(filePath, folder, requiredGameMode, out message)) return false;

        string deletedPath = Path.GetFullPath(filePath);
        string activePath = ApplicationManager.Instance.GetLastSavedFilePath();
        if (!string.IsNullOrWhiteSpace(activePath) && Path.GetFullPath(activePath) == deletedPath)
            ApplicationManager.Instance.ClearActiveSaveContext();
        string preference = PlayerPrefs.GetString("currentGameSettings", string.Empty);
        if (!string.IsNullOrWhiteSpace(preference)
            && Path.GetFullPath(Path.IsPathRooted(preference) ? preference : Path.Combine(folder, preference)) == deletedPath)
        {
            PlayerPrefs.DeleteKey("currentGameSettings");
            PlayerPrefs.Save();
        }
        return true;
    }

    private static bool TryDeleteManualSave(string filePath, string saveFolder, string requiredGameMode, out string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath)
                || !string.Equals(Path.GetExtension(filePath), JsonExtension, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetDirectoryName(Path.GetFullPath(filePath)), Path.GetFullPath(saveFolder),
                    Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
                || IsProtectedSavePath(filePath) || IsDiagnosticCompanionPath(filePath))
            {
                message = "Only a saved match in this save list can be deleted.";
                return false;
            }
            if (!File.Exists(filePath))
            {
                SummaryMetadataCache.Remove(filePath);
                message = "This saved match has already been removed.";
                return false;
            }
            if (TryReadSummary(filePath, requiredGameMode) == null)
            {
                message = "This file is not an eligible saved match for this game mode.";
                return false;
            }

            File.Delete(filePath);
            SummaryMetadataCache.Remove(filePath);
            message = $"Deleted {Path.GetFileName(filePath)}.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Could not delete this saved match: {ex.Message}";
            return false;
        }
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
        if (IsDiagnosticCompanionPath(filePath))
        {
            return null;
        }

        try
        {
            JObject root = ReadSummaryMetadata(filePath);
            if (IsHiddenPlaySession(filePath, root))
            {
                return null;
            }
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
            if (runtime == null || runtime.Type == JTokenType.Null)
            {
                return null;
            }

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
                Clock = FormatClock(clock, halfDuration),
                Score = $"{homeGoals}-{awayGoals}",
                HalfLength = halfDuration > 0 ? halfDuration.ToString() : "-",
                Halves = numberOfHalfs > 0 ? numberOfHalfs.ToString() : "-",
                MatchDuration = halfDuration > 0 && numberOfHalfs > 0 ? $"{numberOfHalfs}x{halfDuration}" : "-",
                Tie = FormatTieBreaker(ReadString(settings, "tiebreaker", "-")),
                CreatedUtc = string.IsNullOrWhiteSpace(createdUtc) ? File.GetCreationTimeUtc(filePath).ToString("o") : createdUtc,
                LastSavedUtc = string.IsNullOrWhiteSpace(lastSavedUtc) ? lastWriteUtc.ToString("o") : lastSavedUtc,
                LastLogWrittenUtc = lastLogWrittenUtc,
                LastActivityUtc = lastActivitySortUtc == DateTime.MinValue ? lastWriteUtc.ToString("o") : lastActivitySortUtc.ToString("o"),
                Status = FormatSummaryStatus(true, isMatchComplete),
                HasRuntimeSnapshot = true,
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

    private static bool IsHiddenPlaySession(string filePath, JObject metadata)
    {
        if (string.Equals(Path.GetFileName(Path.GetDirectoryName(Path.GetFullPath(filePath))),
            PlaySessionsFolderName, StringComparison.OrdinalIgnoreCase))
            return true;

        string kind = metadata.Value<string>("saveKind");
        if (string.Equals(kind, ManualSaveKind, StringComparison.Ordinal)) return false;
        if (string.Equals(kind, PlaySessionKind, StringComparison.Ordinal)) return true;

        // Older loads were placed beside manual saves. Only explicit Save updated
        // lastSavedUtc; live logging changed lastLogWrittenUtc, not that timestamp.
        bool legacyRun = !string.IsNullOrWhiteSpace(metadata.Value<string>("playSessionId"))
            && System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(filePath),
                @"__Run_\d{8}T\d{9}Z_[a-fA-F0-9]{8}\.json$");
        if (!legacyRun) return false;
        bool explicitlySaved = DateTimeOffset.TryParse(metadata.Value<string>("playSessionStartedUtc"), out var started)
            && DateTimeOffset.TryParse(metadata.Value<string>("lastSavedUtc"), out var saved)
            && saved >= started;
        return !explicitlySaved;
    }

    private static string ResolveManualSavePath(string activePath)
    {
        string path = Path.GetFullPath(activePath);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (int depth = 0; depth < 32 && visited.Add(path); depth++)
        {
            if (!File.Exists(path)) return path;
            JObject metadata = ReadSummaryMetadata(path);
            if (!IsHiddenPlaySession(path, metadata)) return path;

            string target = metadata.Value<string>("manualSavePath");
            if (string.IsNullOrWhiteSpace(target)) target = metadata.Value<string>("sourceSavePath");
            if (string.IsNullOrWhiteSpace(target))
                throw new InvalidDataException("This play-through has no original save slot. Use Save As.");
            path = Path.GetFullPath(Path.IsPathRooted(target)
                ? target : Path.Combine(Path.GetDirectoryName(path), target));
        }
        throw new InvalidDataException("The play-through's save references form a cycle. Use Save As.");
    }

    private static string WriteManualSave(JObject root, string filePath, string activePath)
    {
        string destination = Path.GetFullPath(filePath);
        if (string.Equals(Path.GetFileName(Path.GetDirectoryName(destination)),
            PlaySessionsFolderName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Manual saves must be outside the play-through working folder.");
        bool keepSession = !string.IsNullOrWhiteSpace(activePath) && File.Exists(activePath)
            && IsHiddenPlaySession(activePath, ReadSummaryMetadata(activePath));
        if (keepSession && string.Equals(destination, Path.GetFullPath(activePath), StringComparison.Ordinal))
            throw new InvalidDataException("Choose a manual save slot outside the active play-through.");

        root["saveKind"] = ManualSaveKind;
        root["manualSavePath"] = destination;
        File.WriteAllText(destination, root.ToString(Formatting.Indented));
        SummaryMetadataCache.Remove(destination);
        if (!keepSession) return destination;

        // Saving is a checkpoint, not a new play-through. Keep the working copy
        // and all future diagnostics on its existing session identity/path.
        JObject session = (JObject)root.DeepClone();
        session["saveKind"] = PlaySessionKind;
        session["createdUtc"] = ReadSummaryMetadata(activePath)["createdUtc"]?.DeepClone();
        File.WriteAllText(activePath, session.ToString(Formatting.Indented));
        SummaryMetadataCache.Remove(activePath);
        return activePath;
    }

    private static bool IsDiagnosticCompanionPath(string filePath)
    {
        string name = Path.GetFileName(filePath);
        return name.EndsWith("__AI Decisions.json", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("__AI Decisions Index.json", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("__Tactical Notes.json", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("__Live Events Index.json", StringComparison.OrdinalIgnoreCase)
            || System.Text.RegularExpressions.Regex.IsMatch(name,
                @"__(AI Decisions|Live Events)__H\d+_\d+-\d+\.jsonl?$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    }

    private static JObject ReadSummaryMetadata(string filePath)
    {
        var file = new FileInfo(filePath);
        long length = file.Length;
        DateTime lastWriteUtc = file.LastWriteTimeUtc;
        if (SummaryMetadataCache.TryGetValue(filePath, out CachedSummaryMetadata cached)
            && cached.Length == length && cached.LastWriteUtc == lastWriteUtc)
        {
            return cached.Metadata;
        }

        using var stream = File.OpenText(filePath);
        using var reader = new JsonTextReader(stream);
        while (reader.Read() && reader.TokenType == JsonToken.Comment) { }
        if (reader.TokenType != JsonToken.StartObject)
        {
            throw new JsonReaderException("Match save must contain a JSON object.");
        }

        JObject metadata = ReadSummaryObject(reader, "root");
        // Preserve the full reader's rejection of non-comment content after the root.
        while (reader.Read())
        {
            if (reader.TokenType != JsonToken.Comment)
                throw new JsonReaderException("Unexpected content after the match save.");
        }
        SummaryMetadataCache[filePath] = new CachedSummaryMetadata
        {
            Length = length,
            LastWriteUtc = lastWriteUtc,
            Metadata = metadata
        };
        return metadata;
    }

    private static JObject ReadSummaryObject(JsonTextReader reader, string section)
    {
        var result = new JObject();
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndObject) return result;
            if (reader.TokenType == JsonToken.Comment) continue;
            if (reader.TokenType != JsonToken.PropertyName)
                throw new JsonReaderException("Expected a save metadata property.");

            string name = (string)reader.Value;
            if (!reader.Read()) throw new JsonReaderException("Incomplete save metadata.");
            while (reader.TokenType == JsonToken.Comment)
                if (!reader.Read()) throw new JsonReaderException("Incomplete save metadata.");

            bool nested = section == "root" && name is ("runtimeSnapshot" or "stats")
                || section == "runtimeSnapshot" && name == "stats";
            bool retain = section switch
            {
                "root" => name is "createdUtc" or "lastSavedUtc" or "lastLogWrittenUtc" or "gameSettings" or "rosters"
                    or "saveKind" or "manualSavePath" or "playSessionId" or "playSessionStartedUtc" or "sourceSavePath",
                "runtimeSnapshot" => name == "clock",
                "stats" => name is "homeTeamStats" or "awayTeamStats",
                _ => false
            };

            if (nested && reader.TokenType == JsonToken.StartObject)
                result[name] = ReadSummaryObject(reader, name);
            else if (nested || retain)
                result[name] = JToken.ReadFrom(reader);
            else
                reader.Skip();
        }
        throw new JsonReaderException("Incomplete save metadata object.");
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
    public RoomOffsideSnapshot offside = new();
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
public class RoomOffsideSnapshot
{
    public bool hasStoredAssessment;
    public bool assessedTeamIsHome;
    public int offsideLineX;
    public string assessedContext;
    public List<RoomTokenReference> offsideTokens = new();
    public List<OffsideManager.OffsideTokenPositionSnapshot> offsideTokenPositions = new();
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
    public RoomTokenReference forcedGoalkeeperToLeave;
    public string forcedGoalkeeperToLeaveName;
    public RoomHexCoordinates forcedGoalkeeperReplacementHex;
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
