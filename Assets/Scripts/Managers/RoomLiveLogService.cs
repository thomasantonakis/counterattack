using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using UnityEngine;

public static class RoomLiveLogService
{
    private const string LogCapturesFolderName = "LogCaptures";
    private const string LiveEventIndexSuffix = "__Live Events Index.json";
    private const int LiveEventSegmentMinutes = 10;
    private static readonly Dictionary<string, int> LastWrittenSequenceBySegment = new();
    private static readonly HashSet<string> ActiveSegmentPaths = new(StringComparer.Ordinal);
    private static string activeSourcePath = string.Empty;
    private static int lastWrittenSequence;

    public static void ResetSession()
    {
        LastWrittenSequenceBySegment.Clear();
        ActiveSegmentPaths.Clear();
        activeSourcePath = string.Empty;
        lastWrittenSequence = 0;
    }

    /// <summary>
    /// Appends only newly produced events to bounded ten-minute JSONL files.
    /// This is the frequent live path; it deliberately avoids reparsing and
    /// rewriting the growing resumable save after every action.
    /// </summary>
    public static bool AppendActiveLiveEvents(
        MatchManager matchManager,
        out string writtenPath,
        out string message)
    {
        writtenPath = string.Empty;
        if (!TryResolveWritableActiveSavePath(out string savePath, out message))
        {
            return false;
        }

        if (matchManager?.gameData?.events == null)
        {
            message = "No gameplay events are available yet.";
            return true;
        }

        try
        {
            if (!string.Equals(activeSourcePath, savePath, StringComparison.Ordinal))
            {
                LastWrittenSequenceBySegment.Clear();
                ActiveSegmentPaths.Clear();
                activeSourcePath = savePath;
                lastWrittenSequence = LoadLiveEventIndexState(savePath, ActiveSegmentPaths);
                // Inherited events remain in the resumable save as history,
                // but this run's live segments contain only its new actions.
                lastWrittenSequence = Math.Max(lastWrittenSequence, matchManager.gameData.inheritedEventSequence);
            }

            List<GameplayEvent> pendingEvents = matchManager.gameData.events
                .Where(gameplayEvent => gameplayEvent != null
                    && gameplayEvent.sequenceNumber > lastWrittenSequence)
                .OrderBy(gameplayEvent => gameplayEvent.sequenceNumber)
                .ToList();
            Dictionary<string, List<GameplayEvent>> eventsBySegment = pendingEvents
                .GroupBy(gameplayEvent => BuildLiveEventSegmentPath(savePath, gameplayEvent))
                .ToDictionary(group => group.Key, group => group.ToList());
            List<string> updatedSegments = new();
            int appendedCount = 0;

            foreach (KeyValuePair<string, List<GameplayEvent>> segment in eventsBySegment)
            {
                int lastWrittenSequence = GetLastWrittenSequence(segment.Key);
                List<GameplayEvent> pending = segment.Value
                    .Where(gameplayEvent => gameplayEvent.sequenceNumber > lastWrittenSequence)
                    .ToList();
                if (pending.Count == 0)
                {
                    continue;
                }

                StringBuilder builder = new();
                foreach (GameplayEvent gameplayEvent in pending)
                {
                    builder.AppendLine(JsonConvert.SerializeObject(gameplayEvent, Formatting.None));
                }

                File.AppendAllText(segment.Key, builder.ToString());
                LastWrittenSequenceBySegment[segment.Key] = pending[^1].sequenceNumber;
                ActiveSegmentPaths.Add(segment.Key);
                appendedCount += pending.Count;
                updatedSegments.Add(segment.Key);
                writtenPath = segment.Key;
            }

            if (pendingEvents.Count > 0)
            {
                lastWrittenSequence = Math.Max(lastWrittenSequence, pendingEvents[^1].sequenceNumber);
            }

            WriteLiveEventIndex(
                savePath,
                matchManager.gameData.events.Count,
                lastWrittenSequence,
                ActiveSegmentPaths);
            message = appendedCount > 0
                ? $"Appended {appendedCount} live event(s) across {updatedSegments.Count} segment(s)."
                : "Live event segments are already up to date.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Failed to append live events: {ex.Message}";
            return false;
        }
    }

    public static bool PatchActiveLiveLog(MatchManager matchManager, out string patchedPath, out string message)
    {
        patchedPath = string.Empty;
        if (!TryResolveWritableActiveSavePath(out string filePath, out message))
        {
            return false;
        }

        if (matchManager == null)
        {
            message = "MatchManager is not available.";
            return false;
        }

        MatchManager.GameData gameData = matchManager.gameData;
        if (gameData == null)
        {
            message = "Game data is not loaded.";
            return false;
        }

        try
        {
            JObject root = JObject.Parse(File.ReadAllText(filePath));
            PatchLiveLogFields(root, gameData);
            File.WriteAllText(filePath, root.ToString(Formatting.Indented));

            patchedPath = filePath;
            message = $"Live log updated in {Path.GetFileName(filePath)}.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Failed to update live log: {ex.Message}";
            return false;
        }
    }

    public static bool CaptureActiveLogFile(
        MatchManager matchManager,
        bool openFolder,
        out string captureFolderPath,
        out string capturedFilePath,
        out string message)
    {
        captureFolderPath = string.Empty;
        capturedFilePath = string.Empty;

        if (!PatchActiveLiveLog(matchManager, out string activeSavePath, out message))
        {
            return false;
        }

        try
        {
            string captureRoot = Path.Combine(ApplicationManager.Instance.GetSaveFolderPath(), LogCapturesFolderName);
            Directory.CreateDirectory(captureRoot);

            captureFolderPath = CreateUniqueCaptureFolder(captureRoot, BuildCaptureFolderName(matchManager));
            Directory.CreateDirectory(captureFolderPath);

            capturedFilePath = Path.Combine(captureFolderPath, Path.GetFileName(activeSavePath));
            File.Copy(activeSavePath, capturedFilePath, overwrite: true);

            if (openFolder && !TryOpenFolder(captureFolderPath, out string openMessage))
            {
                message = $"Logfile copied to {captureFolderPath}. {openMessage}";
                return true;
            }

            message = $"Logfile copied to {captureFolderPath}.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Failed to capture logfile: {ex.Message}";
            return false;
        }
    }

    private static bool TryResolveWritableActiveSavePath(out string filePath, out string message)
    {
        filePath = string.Empty;
        if (CounterAttack.Online.OnlineRoomSession.IsGuest) { message = "Online replicas cannot write match logs."; return false; }
        // Reading save context must never resurrect a destroyed application during
        // MatchManager.OnDestroy / Play Mode shutdown.
        var application = ApplicationManager.Instance;
        filePath = application != null ? application.GetLastSavedFilePath() : string.Empty;
        if (string.IsNullOrWhiteSpace(filePath))
        {
            message = "No active save file is selected for this match.";
            return false;
        }

        if (RoomSaveService.IsProtectedSavePath(filePath))
        {
            message = "The protected editor playtest fixture cannot receive live logs.";
            return false;
        }

        if (!File.Exists(filePath))
        {
            message = $"Active save file does not exist: {filePath}";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static string BuildLiveEventSegmentPath(string savePath, GameplayEvent gameplayEvent)
    {
        string directory = Path.GetDirectoryName(savePath) ?? string.Empty;
        string baseName = Path.GetFileNameWithoutExtension(savePath);
        int minute = Math.Max(0, gameplayEvent.matchClockSeconds / 60);
        int segmentStart = (minute / LiveEventSegmentMinutes) * LiveEventSegmentMinutes;
        int segmentEnd = segmentStart + LiveEventSegmentMinutes - 1;
        return Path.Combine(
            directory,
            $"{baseName}__Live Events__H{Math.Max(1, gameplayEvent.currentHalf)}_{segmentStart:00}-{segmentEnd:00}.jsonl");
    }

    private static int GetLastWrittenSequence(string segmentPath)
    {
        if (LastWrittenSequenceBySegment.TryGetValue(segmentPath, out int sequence))
        {
            return sequence;
        }

        int lastSequence = 0;
        if (File.Exists(segmentPath))
        {
            string lastLine = File.ReadLines(segmentPath)
                .LastOrDefault(line => !string.IsNullOrWhiteSpace(line));
            if (!string.IsNullOrWhiteSpace(lastLine))
            {
                try
                {
                    lastSequence = JObject.Parse(lastLine).Value<int>("sequenceNumber");
                }
                catch (JsonException)
                {
                    lastSequence = 0;
                }
            }
        }

        LastWrittenSequenceBySegment[segmentPath] = lastSequence;
        return lastSequence;
    }

    private static int LoadLiveEventIndexState(string savePath, ISet<string> segmentPaths)
    {
        string directory = Path.GetDirectoryName(savePath) ?? string.Empty;
        string baseName = Path.GetFileNameWithoutExtension(savePath);
        string indexPath = Path.Combine(directory, $"{baseName}{LiveEventIndexSuffix}");
        if (!File.Exists(indexPath))
        {
            return 0;
        }

        try
        {
            RoomLiveEventIndex index = JsonConvert.DeserializeObject<RoomLiveEventIndex>(
                File.ReadAllText(indexPath));
            foreach (string segmentFile in index?.segmentFiles ?? new List<string>())
            {
                if (!string.IsNullOrWhiteSpace(segmentFile))
                {
                    segmentPaths.Add(Path.Combine(directory, segmentFile));
                }
            }

            return Math.Max(0, index?.lastSequenceNumber ?? 0);
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogWarning(
                $"Could not reopen live event index; rebuilding it from event segments: {exception.Message}");
            return 0;
        }
    }

    private static void WriteLiveEventIndex(
        string savePath,
        int eventCount,
        int latestSequenceNumber,
        IEnumerable<string> segmentPaths)
    {
        string directory = Path.GetDirectoryName(savePath) ?? string.Empty;
        string baseName = Path.GetFileNameWithoutExtension(savePath);
        string indexPath = Path.Combine(directory, $"{baseName}{LiveEventIndexSuffix}");
        RoomLiveEventIndex index = new()
        {
            schemaVersion = GameplayEvent.CurrentSchemaVersion,
            sourceGameplaySave = Path.GetFileName(savePath),
            updatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            eventCount = eventCount,
            lastSequenceNumber = latestSequenceNumber,
            segmentFiles = segmentPaths
                .Select(Path.GetFileName)
                .Distinct()
                .OrderBy(fileName => fileName)
                .ToList()
        };
        File.WriteAllText(indexPath, JsonConvert.SerializeObject(index, Formatting.Indented));
    }

    private static void PatchLiveLogFields(JObject root, MatchManager.GameData gameData)
    {
        gameData.eventLogSchemaVersion = GameplayEvent.CurrentSchemaVersion;
        gameData.events ??= new List<GameplayEvent>();

        root["eventLogSchemaVersion"] = gameData.eventLogSchemaVersion;
        root["events"] = JToken.FromObject(gameData.events);
        root["stats"] = JToken.FromObject(gameData.stats ?? new MatchManager.Stats());
        root["gameLog"] = JObject.FromObject(new RoomGameLogJson
        {
            entries = gameData.gameLog != null
                ? gameData.gameLog.GetGameLog()
                : new List<string>()
        });
        root["lastLogWrittenUtc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
    }

    private static string BuildCaptureFolderName(MatchManager matchManager)
    {
        string clockLabel = "H1_clock";
        if (matchManager != null)
        {
            clockLabel = $"H{Mathf.Max(1, matchManager.currentHalf)}_{matchManager.GetClockDisplayText()}";
        }

        string timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH_mm", CultureInfo.InvariantCulture);
        return $"room_{timestamp}_{SanitizePathPart(clockLabel)}";
    }

    private static string CreateUniqueCaptureFolder(string captureRoot, string baseFolderName)
    {
        string folderPath = Path.Combine(captureRoot, baseFolderName);
        int suffix = 2;
        while (Directory.Exists(folderPath))
        {
            folderPath = Path.Combine(captureRoot, $"{baseFolderName}_{suffix}");
            suffix++;
        }

        return folderPath;
    }

    private static bool TryOpenFolder(string folderPath, out string message)
    {
        message = string.Empty;
        try
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            StartProcess("explorer.exe", QuoteArgument(folderPath));
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            StartProcess("open", QuoteArgument(folderPath));
#else
            StartProcess("xdg-open", QuoteArgument(folderPath));
#endif
            return true;
        }
        catch (Exception ex)
        {
            message = $"Could not open folder automatically: {ex.Message}";
            return false;
        }
    }

    private static void StartProcess(string fileName, string arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false
        };
        Process.Start(startInfo);
    }

    private static string SanitizePathPart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "log";
        }

        HashSet<char> invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
        char[] characters = value
            .Trim()
            .Select(character => invalidCharacters.Contains(character) || !char.IsLetterOrDigit(character)
                ? '_'
                : character)
            .ToArray();
        string sanitized = new(characters);
        while (sanitized.Contains("__"))
        {
            sanitized = sanitized.Replace("__", "_");
        }

        return sanitized.Trim('_');
    }

    private static string QuoteArgument(string value)
    {
        return $"\"{value.Replace("\"", "\\\"")}\"";
    }

    private sealed class RoomGameLogJson
    {
        public List<string> entries = new();
    }
}

[Serializable]
public sealed class RoomLiveEventIndex
{
    public int schemaVersion;
    public string sourceGameplaySave;
    public string updatedUtc;
    public int eventCount;
    public int lastSequenceNumber;
    public List<string> segmentFiles = new();
}
