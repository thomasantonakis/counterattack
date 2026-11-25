using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Discovers music without moving, copying or renaming source files.</summary>
public static class MusicLibraryAuthoring
{
    public const string LibraryPath = "Assets/Resources/CounterAttackMusicLibrary.asset";

    [MenuItem("Counter Attack/Audio/Refresh Music Library")]
    public static void Refresh()
    {
        AssetDatabase.Refresh();
        var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Music" })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .Select(AssetDatabase.LoadAssetAtPath<AudioClip>).Where(c => c != null).ToArray();
        Directory.CreateDirectory("Assets/Resources");
        var library = AssetDatabase.LoadAssetAtPath<MusicLibrary>(LibraryPath);
        bool create = library == null;
        if (create) { library = ScriptableObject.CreateInstance<MusicLibrary>(); AssetDatabase.CreateAsset(library, LibraryPath); }
        library.catalog = clips;
        if (create)
        {
            AudioClip Pick(string part) => clips.FirstOrDefault(c => AssetDatabase.GetAssetPath(c).IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0);
            AudioClip[] One(AudioClip c) => c == null ? Array.Empty<AudioClip>() : new[] { c };
            library.playlists = new[]
            {
                new MusicLibrary.Playlist { label = "Startup", scenes = new[] { "SplashScreen" }, tracks = One(Pick("sports-epic-intro")) },
                new MusicLibrary.Playlist { label = "Main menu", scenes = new[] { "MainMenu", "CreateLoadRoom" }, tracks = One(Pick("epic-opening")) },
                new MusicLibrary.Playlist { label = "Team selection", scenes = new[] { "CreateNewGameScene", "Draft", "FreeDraft" }, tracks = One(Pick("determined_pursuit_loop.wav")), repeat = MusicLibrary.RepeatMode.RepeatTrack },
                new MusicLibrary.Playlist { label = "Pre-match (assign scenes when available)" },
                new MusicLibrary.Playlist { label = "Victory (assign scenes when available)" }
            };
        }
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        Selection.activeObject = library;
        Debug.Log($"Music library contains {clips.Length} AudioClip assets. Existing playlist assignments were preserved.");
    }
}

[CustomEditor(typeof(MusicLibrary))]
public sealed class MusicLibraryInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Select clips in playlists to assign them to scenes. Use Refresh Music Library to discover new files without changing assignments. Room is silent unless explicitly assigned.", MessageType.Info);
        if (GUILayout.Button("Refresh catalog from Assets/Audio/Music")) MusicLibraryAuthoring.Refresh();
        if (GUILayout.Button("Stop audition")) InvokeAudioUtil("StopAllPreviewClips", null);
        foreach (var clip in ((MusicLibrary)target).catalog)
        {
            if (clip != null && GUILayout.Button("Audition: " + clip.name)) InvokeAudioUtil("PlayPreviewClip", clip);
        }
    }

    private static void InvokeAudioUtil(string method, AudioClip clip)
    {
        var type = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        var info = type?.GetMethod(method, BindingFlags.Static | BindingFlags.Public, null,
            clip == null ? Type.EmptyTypes : new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
        if (info == null) { Debug.LogWarning("Audio audition is unavailable in this Unity version."); return; }
        if (clip != null) InvokeAudioUtil("StopAllPreviewClips", null);
        info.Invoke(null, clip == null ? null : new object[] { clip, 0, false });
    }
}
