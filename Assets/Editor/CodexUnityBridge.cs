#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CounterAttack.Editor
{
    [InitializeOnLoad]
    public static class CodexUnityBridge
    {
        private const double PollIntervalSeconds = 0.5d;

        private static string BridgeRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "CodexUnityBridge"));
        private static string RequestsDirectory => Path.Combine(BridgeRoot, "requests");
        private static string ResponsesDirectory => Path.Combine(BridgeRoot, "responses");

        private static double nextPollTime;
        private static bool isProcessingRequest;

        static CodexUnityBridge()
        {
            EnsureDirectories();
            EditorApplication.update += PollForRequests;
        }

        [MenuItem("CounterAttack/Unity Bridge/Reveal Bridge Folder")]
        private static void RevealBridgeFolder()
        {
            EnsureDirectories();
            EditorUtility.RevealInFinder(BridgeRoot);
        }

        private static void PollForRequests()
        {
            if (EditorApplication.timeSinceStartup < nextPollTime)
            {
                return;
            }

            nextPollTime = EditorApplication.timeSinceStartup + PollIntervalSeconds;

            if (isProcessingRequest || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            EnsureDirectories();
            string requestPath = Directory.GetFiles(RequestsDirectory, "*.json")
                .OrderBy(path => path, StringComparer.Ordinal)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(requestPath))
            {
                return;
            }

            ProcessRequest(requestPath);
        }

        private static void ProcessRequest(string requestPath)
        {
            isProcessingRequest = true;
            BridgeResponse response = null;
            string responsePath = null;

            try
            {
                string requestJson = File.ReadAllText(requestPath);
                BridgeRequest request = JsonUtility.FromJson<BridgeRequest>(requestJson);
                if (request == null)
                {
                    throw new InvalidOperationException($"Could not deserialize bridge request: {requestPath}");
                }

                if (string.IsNullOrWhiteSpace(request.id))
                {
                    request.id = Path.GetFileNameWithoutExtension(requestPath);
                }

                responsePath = Path.Combine(ResponsesDirectory, $"{request.id}.json");
                response = ExecuteRequest(request);
            }
            catch (Exception ex)
            {
                string fallbackId = Path.GetFileNameWithoutExtension(requestPath);
                responsePath ??= Path.Combine(ResponsesDirectory, $"{fallbackId}.json");
                response = BridgeResponse.Error(fallbackId, "unknown", ex);
            }
            finally
            {
                try
                {
                    if (response != null && !string.IsNullOrWhiteSpace(responsePath))
                    {
                        File.WriteAllText(responsePath, JsonUtility.ToJson(response, true));
                    }
                }
                finally
                {
                    if (File.Exists(requestPath))
                    {
                        File.Delete(requestPath);
                    }
                    isProcessingRequest = false;
                }
            }
        }

        private static BridgeResponse ExecuteRequest(BridgeRequest request)
        {
            string command = (request.command ?? string.Empty).Trim().ToLowerInvariant();

            switch (command)
            {
                case "ping":
                    return BridgeResponse.Ok(request, "Unity bridge is alive.");
                case "get_status":
                    return BridgeResponse.Ok(request, "Unity editor status captured.", JsonUtility.ToJson(new BridgeStatusData
                    {
                        isPlaying = EditorApplication.isPlaying,
                        isCompiling = EditorApplication.isCompiling,
                        isUpdating = EditorApplication.isUpdating,
                        activeScenePath = EditorSceneManager.GetActiveScene().path,
                        selectedObjectName = Selection.activeGameObject != null ? Selection.activeGameObject.name : string.Empty
                    }));
                case "get_match_summary":
                    return BridgeResponse.Ok(request, "Match summary captured.", JsonUtility.ToJson(BuildMatchSummary()));
                case "rebuild_room_board":
                    EnsureEditorMode(command);
                    RoomBoardEditorTools.RebuildPitchBoard();
                    return BridgeResponse.Ok(request, "Room pitch board rebuilt.");
                case "rebuild_shooting_paths":
                    EnsureEditorMode(command);
                    RoomBoardEditorTools.RebuildShootingPathAssets();
                    return BridgeResponse.Ok(request, "Room shooting path assets rebuilt.");
                case "rebuild_room_board_and_paths":
                    EnsureEditorMode(command);
                    RoomBoardEditorTools.RebuildPitchBoardAndPathAssets();
                    return BridgeResponse.Ok(request, "Room pitch board and path assets rebuilt.");
                case "setup_create_new_game_kit_preview_ui":
                    EnsureEditorMode(command);
                    CreateNewGameSceneEditorTools.SetupKitPreviewUi();
                    return BridgeResponse.Ok(request, "Create New Game kit preview UI configured.");
                case "reload_kit_presets":
                    EnsureEditorMode(command);
                    TokenKitCatalog.ReloadFromSource();
                    CreateNewGameSceneEditorTools.SetupKitPreviewUi();
                    CreateNewGameManager createNewGameManager = FindSceneObject<CreateNewGameManager>();
                    if (createNewGameManager != null)
                    {
                        createNewGameManager.ReloadKitSelectionUi();
                        return BridgeResponse.Ok(request, $"Reloaded {TokenKitCatalog.GetAllPresets().Count} kit presets and refreshed Create New Game UI.");
                    }

                    return BridgeResponse.Ok(request, $"Reloaded {TokenKitCatalog.GetAllPresets().Count} kit presets. Open CreateNewHSGameScene to refresh its UI.");
                case "save_open_scenes":
                    EnsureEditorMode(command);
                    bool saved = EditorSceneManager.SaveOpenScenes();
                    return BridgeResponse.Ok(request, saved ? "Open scenes saved." : "Open scenes save returned false.");
                case "refresh_assets":
                    EnsureEditorMode(command);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    return BridgeResponse.Ok(request, "Unity assets refreshed synchronously.");
                case "reload_window_layout":
                    EnsureEditorMode(command);
                    string layoutPath = GetOptionalArg(request, "layout_path");
                    ReloadWindowLayout(layoutPath);
                    return BridgeResponse.Ok(request, string.IsNullOrWhiteSpace(layoutPath)
                        ? "Reloaded current window layout."
                        : $"Reloaded window layout from {layoutPath}.");
                case "open_scene":
                    EnsureEditorMode(command);
                    string scenePath = GetRequiredArg(request, "scene_path");
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    return BridgeResponse.Ok(request, $"Opened scene {scenePath}.");
                case "select_object":
                    EnsureEditorMode(command);
                    Transform targetTransform = ResolveSelectionTarget(request);
                    Selection.activeGameObject = targetTransform.gameObject;
                    EditorGUIUtility.PingObject(targetTransform.gameObject);
                    return BridgeResponse.Ok(request, $"Selected {GetHierarchyPath(targetTransform)}.");
                case "enter_play_mode":
                    if (!EditorApplication.isPlaying)
                    {
                        EditorApplication.isPlaying = true;
                    }
                    return BridgeResponse.Ok(request, "Entered play mode.");
                case "exit_play_mode":
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.isPlaying = false;
                    }
                    return BridgeResponse.Ok(request, "Exited play mode.");
                default:
                    throw new InvalidOperationException($"Unsupported Unity bridge command: {request.command}");
            }
        }

        private static void EnsureEditorMode(string command)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException($"{command} can only run while the editor is not in play mode.");
            }
        }

        private static void ReloadWindowLayout(string layoutPath)
        {
            Type windowLayoutType = Type.GetType("UnityEditor.WindowLayout, UnityEditor");
            if (windowLayoutType == null)
            {
                throw new InvalidOperationException("Could not resolve UnityEditor.WindowLayout.");
            }

            if (!string.IsNullOrWhiteSpace(layoutPath))
            {
                string fullLayoutPath = Path.GetFullPath(layoutPath);
                MethodInfo tryLoadMethod = windowLayoutType
                    .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(method => method.Name == "TryLoadWindowLayout");

                if (tryLoadMethod == null)
                {
                    throw new InvalidOperationException("Could not find UnityEditor.WindowLayout.TryLoadWindowLayout.");
                }

                ParameterInfo[] parameters = tryLoadMethod.GetParameters();
                object result;
                if (parameters.Length == 1)
                {
                    result = tryLoadMethod.Invoke(null, new object[] { fullLayoutPath });
                }
                else if (parameters.Length == 2)
                {
                    result = tryLoadMethod.Invoke(null, new object[] { fullLayoutPath, false });
                }
                else
                {
                    object[] args = new object[parameters.Length];
                    args[0] = fullLayoutPath;
                    for (int i = 1; i < parameters.Length; i++)
                    {
                        args[i] = parameters[i].ParameterType.IsValueType
                            ? Activator.CreateInstance(parameters[i].ParameterType)
                            : null;
                    }
                    result = tryLoadMethod.Invoke(null, args);
                }

                if (result is bool loaded && !loaded)
                {
                    throw new InvalidOperationException($"Unity declined to load layout file {fullLayoutPath}.");
                }

                return;
            }

            MethodInfo reloadMethod = windowLayoutType.GetMethod(
                "ReloadWindowLayoutMenu",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (reloadMethod == null)
            {
                throw new InvalidOperationException("Could not find UnityEditor.WindowLayout.ReloadWindowLayoutMenu.");
            }

            reloadMethod.Invoke(null, null);
        }

        private static Transform ResolveSelectionTarget(BridgeRequest request)
        {
            string hierarchyPath = GetOptionalArg(request, "path");
            if (!string.IsNullOrWhiteSpace(hierarchyPath))
            {
                Transform exactMatch = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(candidate => GetHierarchyPath(candidate).Equals(hierarchyPath, StringComparison.Ordinal));
                if (exactMatch != null)
                {
                    return exactMatch;
                }
            }

            string objectName = GetRequiredArg(request, "name");
            Transform nameMatch = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.name.Equals(objectName, StringComparison.Ordinal));
            if (nameMatch == null)
            {
                throw new InvalidOperationException($"Could not find a scene object named {objectName}.");
            }

            return nameMatch;
        }

        private static T FindSceneObject<T>() where T : UnityEngine.Object
        {
            return Resources.FindObjectsOfTypeAll<T>()
                .FirstOrDefault(candidate =>
                {
                    if (candidate is not Component component)
                    {
                        return false;
                    }

                    return component.gameObject.scene.IsValid();
                });
        }

        private static string GetRequiredArg(BridgeRequest request, string key)
        {
            string value = GetOptionalArg(request, key);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Unity bridge command {request.command} is missing required argument '{key}'.");
            }
            return value;
        }

        private static string GetOptionalArg(BridgeRequest request, string key)
        {
            if (request.args == null)
            {
                return null;
            }

            BridgeArg match = request.args.FirstOrDefault(arg => arg.key == key);
            return match?.value;
        }

        private static string GetHierarchyPath(Transform target)
        {
            string path = target.name;
            Transform current = target.parent;
            while (current != null)
            {
                path = $"{current.name}/{path}";
                current = current.parent;
            }
            return path;
        }

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(RequestsDirectory);
            Directory.CreateDirectory(ResponsesDirectory);
        }

        private static BridgeMatchSummary BuildMatchSummary()
        {
            MatchManager matchManager = UnityEngine.Object.FindFirstObjectByType<MatchManager>();
            if (matchManager == null)
            {
                return new BridgeMatchSummary
                {
                    hasMatchManager = false,
                    activeSavePath = ApplicationManager.Instance != null ? ApplicationManager.Instance.GetLastSavedFilePath() : string.Empty
                };
            }

            var data = matchManager.gameData;
            return new BridgeMatchSummary
            {
                hasMatchManager = true,
                activeSavePath = ApplicationManager.Instance != null ? ApplicationManager.Instance.GetLastSavedFilePath() : string.Empty,
                homeTeamName = data?.gameSettings?.homeTeamName ?? string.Empty,
                awayTeamName = data?.gameSettings?.awayTeamName ?? string.Empty,
                homeRosterCount = data?.rosters?.home?.Count ?? 0,
                awayRosterCount = data?.rosters?.away?.Count ?? 0
            };
        }

        [Serializable]
        private class BridgeRequest
        {
            public string id;
            public string command;
            public BridgeArg[] args;
            public string createdAtUtc;
        }

        [Serializable]
        private class BridgeArg
        {
            public string key;
            public string value;
        }

        [Serializable]
        private class BridgeStatusData
        {
            public bool isPlaying;
            public bool isCompiling;
            public bool isUpdating;
            public string activeScenePath;
            public string selectedObjectName;
        }

        [Serializable]
        private class BridgeMatchSummary
        {
            public bool hasMatchManager;
            public string activeSavePath;
            public string homeTeamName;
            public string awayTeamName;
            public int homeRosterCount;
            public int awayRosterCount;
        }

        [Serializable]
        private class BridgeResponse
        {
            public string id;
            public string command;
            public string status;
            public string message;
            public string dataJson;
            public string completedAtUtc;

            public static BridgeResponse Ok(BridgeRequest request, string message, string dataJson = "")
            {
                return new BridgeResponse
                {
                    id = request.id,
                    command = request.command,
                    status = "ok",
                    message = message,
                    dataJson = dataJson,
                    completedAtUtc = DateTime.UtcNow.ToString("O")
                };
            }

            public static BridgeResponse Error(string id, string command, Exception ex)
            {
                return new BridgeResponse
                {
                    id = id,
                    command = command,
                    status = "error",
                    message = ex.ToString(),
                    dataJson = string.Empty,
                    completedAtUtc = DateTime.UtcNow.ToString("O")
                };
            }
        }
    }
}
#endif
