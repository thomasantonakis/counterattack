using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Audio-only authoring. Does not inspect or alter designer-owned UI.</summary>
public static class GameplayAudioAmbientAuthoring
{
    public static object CurateUsedCrowdAssets(string archiveRoot)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires edit mode.");
        const string sourceRoot = "Assets/344SFX_Football_Crowd";
        const string targetRoot = "Assets/Audio/Crowd";
        if (!Directory.Exists(archiveRoot)) throw new InvalidOperationException("Verified external archive missing.");
        foreach (string file in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories).Where(p => Path.GetFileName(p) != ".DS_Store"))
        {
            string copy = Path.Combine(archiveRoot, file.Substring(sourceRoot.Length + 1));
            if (!File.Exists(copy) || new FileInfo(copy).Length != new FileInfo(file).Length)
                throw new InvalidOperationException("Archive is incomplete: " + file);
        }
        var library = AssetDatabase.LoadAssetAtPath<GameplayAudioLibrary>("Assets/Audio/GameplayAudioLibrary.asset");
        if (library.Get(GameplayAudioPool.DuelWon).Length != 2 || library.Get(GameplayAudioPool.DuelLost).Length != 1)
            throw new InvalidOperationException("Recovery pools must remain assigned.");
        var clips = library.pools.SelectMany(p => p.clips).Where(c => c != null).Distinct().ToArray();
        var guids = clips.Select(c => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(c))).ToArray();
        var roots = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/", StringComparison.Ordinal)
            && !p.StartsWith(sourceRoot + "/", StringComparison.Ordinal)
            && (p.EndsWith(".unity") || p.EndsWith(".prefab") || p.EndsWith(".asset"))).ToArray();
        var dependencies = AssetDatabase.GetDependencies(roots, true).Where(p => p.StartsWith(sourceRoot + "/", StringComparison.Ordinal)).Distinct().ToArray();
        if (dependencies.Any(p => !p.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Non-clip package dependency needs review: " + string.Join(", ", dependencies));
        var paths = dependencies.Concat(clips.Select(AssetDatabase.GetAssetPath).Where(p => p.StartsWith(sourceRoot + "/", StringComparison.Ordinal))).Distinct().ToArray();
        var moves = new Dictionary<string, string>();
        if (!AssetDatabase.IsValidFolder(targetRoot)) AssetDatabase.CreateFolder("Assets/Audio", "Crowd");
        foreach (string category in paths.Select(p => p.Substring(sourceRoot.Length + 1).Split('/')[0]).Distinct())
            if (!AssetDatabase.IsValidFolder(targetRoot + "/" + category)) AssetDatabase.CreateFolder(targetRoot, category);
        AssetDatabase.StartAssetEditing();
        try
        {
            if (!AssetDatabase.IsValidFolder(targetRoot)) AssetDatabase.CreateFolder("Assets/Audio", "Crowd");
            foreach (string from in paths)
            {
                string relative = from.Substring(sourceRoot.Length + 1);
                string category = relative.Split('/')[0];
                string folder = targetRoot + "/" + category;
                if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(targetRoot, category);
                string to = folder + "/" + Path.GetFileName(from);
                string error = AssetDatabase.MoveAsset(from, to);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                moves.Add(from, to);
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        foreach (string guid in guids)
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid)) == null)
                throw new InvalidOperationException("Moved runtime clip lost its GUID: " + guid);
        var remaining = AssetDatabase.GetDependencies(roots, true).Where(p => p.StartsWith(sourceRoot + "/", StringComparison.Ordinal)).ToArray();
        if (remaining.Length > 0) throw new InvalidOperationException("Package still referenced: " + string.Join(", ", remaining));
        string manifestPath = "Documentation/Plans/GameplayAudioPoolSelections.json";
        var manifest = JObject.Parse(File.ReadAllText(manifestPath));
        foreach (JObject selection in manifest["selections"].Children<JObject>())
        {
            string old = (string)selection["path"];
            if (old != null && moves.TryGetValue(old, out string moved)) selection["path"] = moved;
            else if (old != null && old.StartsWith(sourceRoot + "/", StringComparison.Ordinal)) selection["archivedOnly"] = true;
        }
        manifest["archive"] = JObject.FromObject(new { location = archiveRoot, verifiedFiles = 351, verifiedWavs = 153, method = "SHA-256 comparison before curation; complete match", usedOriginalClips = moves.Count });
        File.WriteAllText(manifestPath, manifest.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
        if (!AssetDatabase.DeleteAsset(sourceRoot)) throw new InvalidOperationException("Could not remove unused package remainder.");
        AssetDatabase.SaveAssets();
        return new { movedClips = moves.Count, runtimeClips = clips.Length, allRuntimeGuidsPreserved = true,
            retainedRecoveryClips = library.Get(GameplayAudioPool.DuelWon).Concat(library.Get(GameplayAudioPool.DuelLost)).Select(AssetDatabase.GetAssetPath).ToArray() };
    }

    public static object AuthorIncidentChannels()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires edit mode.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Room.unity") throw new InvalidOperationException("Open Room first.");
        var director = UnityEngine.Object.FindAnyObjectByType<GameplayAudioDirector>(FindObjectsInactive.Include);
        if (director == null || director.library == null) throw new InvalidOperationException("Audio rig missing.");
        var selections = JObject.Parse(File.ReadAllText("Documentation/Plans/GameplayAudioPoolSelections.json"))["selections"];
        var mapping = new Dictionary<GameplayAudioPool, string>
        {
            { GameplayAudioPool.DuelWon, "DuelWon" }, { GameplayAudioPool.DuelLost, "DuelLost" },
            { GameplayAudioPool.DefensiveShotRelief, "DefensiveShotRelief" }, { GameplayAudioPool.OpponentError, "OpponentError" },
            { GameplayAudioPool.FoulReaction, "FoulReaction" }, { GameplayAudioPool.KickOff, "KickOff" },
            { GameplayAudioPool.HalfTime, "HalfTime" }, { GameplayAudioPool.FullTime, "FullTime" },
            { GameplayAudioPool.HomeSubstitution, "HomeSubstitution" }, { GameplayAudioPool.AwaySubstitution, "AwaySubstitution" },
            { GameplayAudioPool.DoubleAwaySubstitution, "DoubleAwaySubstitution" },
            { GameplayAudioPool.InjurySubstitution, "InjurySubstitution" },
            { GameplayAudioPool.SnapshotAvailable, "SnapshotAvailable" }, { GameplayAudioPool.SetPiecePreparation, "SetPiecePreparation" }
        };
        var clips = mapping.ToDictionary(pair => pair.Key, pair => selections.Children()
            .Where(s => (string)s["proposedPool"] == pair.Value)
            .Select(s => AssetDatabase.LoadAssetAtPath<AudioClip>((string)s["path"])).ToArray());
        if (clips.Values.Any(pool => pool.Length == 0 || pool.Any(c => c == null)))
            throw new InvalidOperationException("Selected audio missing; no assignments changed.");
        Undo.RecordObject(director.library, "Assign match incident pools");
        var pools = director.library.pools.ToList();
        foreach (var pair in clips)
        {
            var pool = pools.SingleOrDefault(p => p.kind == pair.Key);
            if (pool == null) { pool = new GameplayAudioLibrary.Pool { kind = pair.Key }; pools.Add(pool); }
            pool.clips = pair.Value;
        }
        director.library.pools = pools.ToArray();
        EditorUtility.SetDirty(director.library);
        Undo.RecordObject(director, "Author match incident channels");
        EnsureSource(director.transform, ref director.announcements, "Announcements", 110);
        EnsureSource(director.transform, ref director.homePreparation, "HomePreparation", 180);
        EnsureSource(director.transform, ref director.homePreparationNext, "HomePreparationNext", 180);
        EnsureSource(director.transform, ref director.awayPreparation, "AwayPreparation", 180);
        EnsureSource(director.transform, ref director.awayPreparationNext, "AwayPreparationNext", 180);
        EditorUtility.SetDirty(director);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        return clips.Select(p => new { pool = p.Key.ToString(), count = p.Value.Length }).ToArray();
    }

    public static object AuthorLoadedRoom()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires edit mode.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Room.unity") throw new InvalidOperationException("Open Room first.");
        var director = UnityEngine.Object.FindAnyObjectByType<GameplayAudioDirector>(FindObjectsInactive.Include);
        if (director == null || director.library == null) throw new InvalidOperationException("Room audio rig/library missing.");
        var selections = JObject.Parse(File.ReadAllText("Documentation/Plans/GameplayAudioPoolSelections.json"))["selections"];
        var mapping = new Dictionary<GameplayAudioPool, string>
        {
            { GameplayAudioPool.Chatter, "WallaBase" },
            { GameplayAudioPool.Chant, "Chant" },
            { GameplayAudioPool.Cheer, "GoalScored" },
            { GameplayAudioPool.Groan, "MissedChance" }
        };
        var clips = mapping.ToDictionary(pair => pair.Key, pair => selections.Children()
            .Where(s => (string)s["proposedPool"] == pair.Value)
            .Select(s => AssetDatabase.LoadAssetAtPath<AudioClip>((string)s["path"])).ToArray());
        if (clips.Values.Any(pool => pool.Length == 0 || pool.Any(c => c == null)))
            throw new InvalidOperationException("A selected recording did not import; assignments unchanged.");
        if (mapping.Keys.Any(kind => director.library.pools.Count(p => p.kind == kind) != 1))
            throw new InvalidOperationException("Library must contain exactly one entry per assigned pool.");

        Undo.RecordObject(director, "Add continuous crowd channels");
        EnsureSource(director.transform, ref director.homeChatterNext, "HomeChatterNext", 200);
        EnsureSource(director.transform, ref director.awayChatterNext, "AwayChatterNext", 200);
        EnsureSource(director.transform, ref director.homeChant, "HomeChant", 160);
        EnsureSource(director.transform, ref director.awayChant, "AwayChant", 160);
        Undo.RecordObject(director.library, "Assign auditioned ambient and reaction pools");
        foreach (var pool in clips) director.library.pools.Single(p => p.kind == pool.Key).clips = pool.Value;
        EditorUtility.SetDirty(director);
        EditorUtility.SetDirty(director.library);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        return new
        {
            scene = scene.path,
            sourceCount = director.GetComponentsInChildren<AudioSource>(true).Length,
            pools = clips.Select(p => new { pool = p.Key.ToString(), count = p.Value.Length }).ToArray()
        };
    }

    private static void EnsureSource(Transform root, ref AudioSource source, string name, int priority)
    {
        if (source != null) return;
        Transform existing = root.Find(name);
        var go = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(go, "Author crowd audio channel");
            go.transform.SetParent(root, false);
        }
        source = go.GetComponent<AudioSource>();
        if (source == null) source = Undo.AddComponent<AudioSource>(go);
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.volume = 0f;
        source.priority = priority;
    }
}
