using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class RoomLiveLogService
{
    private const string LogCapturesFolderName = "LogCaptures";

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
        ApplicationManager.EnsureInstanceExists();
        filePath = ApplicationManager.Instance.GetLastSavedFilePath();
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
