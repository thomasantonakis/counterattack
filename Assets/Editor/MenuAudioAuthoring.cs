using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Explicit edit-mode audio authoring; never changes menu geometry or creates runtime UI.</summary>
public static class MenuAudioAuthoring
{
    public const string PrefabPath = "Assets/Audio/UI/MenuAudio.prefab";
    public static object Generate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires edit mode.");
        Directory.CreateDirectory("Assets/Audio/UI");
        var kinds = new[] { GameplayAudioPool.MenuHover, GameplayAudioPool.MenuSelect, GameplayAudioPool.MenuAdvance, GameplayAudioPool.MenuBack };
        var durations = new[] { .065f, .085f, .18f, .16f };
        var peaks = new[] { .2f, .24f, .30f, .27f };
        var library = AssetDatabase.LoadAssetAtPath<GameplayAudioLibrary>(GameplayAudioEditorTools.LibraryPath);
        var pools = library.pools.ToList();
        for (int k = 0; k < kinds.Length; k++)
        {
            string path = "Assets/Audio/UI/" + kinds[k] + ".wav";
            WriteWave(path, k, durations[k], peaks[k]);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
            var pool = pools.SingleOrDefault(p => p.kind == kinds[k]);
            if (pool == null) { pool = new GameplayAudioLibrary.Pool { kind = kinds[k] }; pools.Add(pool); }
            pool.clips = new[] { AssetDatabase.LoadAssetAtPath<AudioClip>(path) };
        }
        library.pools = pools.ToArray();
        EditorUtility.SetDirty(library);
        var root = new GameObject("MenuAudio");
        try
        {
            var source = root.AddComponent<AudioSource>();
            source.playOnAwake = false; source.loop = false; source.spatialBlend = 0f;
            source.ignoreListenerPause = true; source.dopplerLevel = 0f; source.priority = 64;
            var player = root.AddComponent<MenuAudioPlayer>();
            player.library = library; player.voice = source;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssets();
        return new { generated = kinds.Select(k => k.ToString()).ToArray(), prefab = PrefabPath };
    }

    private static void WriteWave(string path, int kind, float duration, float peak)
    {
        const int rate = 48000;
        var samples = new float[Mathf.RoundToInt(duration * rate)];
        var random = new System.Random(4701 + kind);
        float low = 0f, slower = 0f, phase = 0f, max = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float u = i / (float)(samples.Length - 1);
            float noise = (float)(random.NextDouble() * 2 - 1);
            low += .18f * (noise - low); slower += .025f * (noise - slower);
            float air = low - slower;
            float envelope = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 2f);
            if (kind == 0) samples[i] = air * envelope;
            else
            {
                float frequency = kind == 1 ? 1150f : kind == 2 ? Mathf.Lerp(620f, 930f, u) : Mathf.Lerp(860f, 560f, u);
                phase += 2f * Mathf.PI * frequency / rate;
                float body = Mathf.Sin(phase) + .12f * Mathf.Sin(phase * 2f);
                float decay = kind == 1 ? Mathf.Exp(-9f * u) : Mathf.Exp(-2.8f * u);
                samples[i] = envelope * decay * (body * .7f + air * .3f);
            }
            max = Mathf.Max(max, Mathf.Abs(samples[i]));
        }
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1);
            writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
            foreach (float sample in samples) writer.Write((short)Mathf.RoundToInt(sample / Mathf.Max(.0001f, max) * peak * short.MaxValue));
        }
    }

    public static object AuthorLoadedMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires edit mode.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        string[] supported = { "MainMenu", "CreateLoadRoom", "CreateNewGameScene", "Draft", "FreeDraft", "Room" };
        if (!supported.Contains(scene.name)) throw new InvalidOperationException("Not a menu-flow scene.");
        var rects = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RectTransform>(true)).ToArray();
        var before = rects.Select(Snapshot).ToArray();
        if (!scene.GetRootGameObjects().Any(g => g.GetComponent<MenuAudioPlayer>() != null))
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
        IEnumerable<UnityEngine.UI.Selectable> controls;
        var ownedNavigationButtons = new HashSet<UnityEngine.UI.Button>();
        if (scene.name == "Room")
        {
            var pause = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PauseMenuManager>(true)).Single();
            var roots = new[] { pause.pausePanel, pause.editSettingsPanel, pause.saveAsOverlay, pause.tacticalNoteOverlay };
            ownedNavigationButtons.UnionWith(new[] { pause.resumeButton, pause.editSettings, pause.backToMainMenuButton,
                pause.editSettingsBackButton, pause.substitutionsButton, pause.saveAsCancelButton });
            controls = roots.Where(g => g != null).SelectMany(g => g.GetComponentsInChildren<UnityEngine.UI.Selectable>(true));
            controls = controls.Concat(scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SubstitutionMenuView>(true))
                .SelectMany(v => v.GetComponentsInChildren<UnityEngine.UI.Selectable>(true)));
        }
        else controls = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UnityEngine.UI.Selectable>(true));
        int count = 0;
        foreach (var control in controls.Distinct())
        {
            if (AnimationUtility.CalculateTransformPath(control.transform, null).Contains("/MatchStatsUI/Stats"))
                throw new InvalidOperationException("Refusing designer-owned Stats subtree.");
            var old = control.GetComponent<GameplayAudioHoverFeedback>();
            if (old != null) Undo.DestroyObjectImmediate(old);
            var feedback = control.GetComponent<MenuAudioFeedback>();
            if (feedback == null) feedback = Undo.AddComponent<MenuAudioFeedback>(control.gameObject);
            if (control is UnityEngine.UI.Button button)
            {
                var methods = Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Select(button.onClick.GetPersistentMethodName);
                feedback.buttonSelection = !ownedNavigationButtons.Contains(button) && !methods.Any(m => m.Contains("Back") || m.Contains("Start") || m.Contains("Create")
                    || m.Contains("Load") || m.Contains("Confirm") || m.Contains("Play") || m.Contains("Resume"));
            }
            EditorUtility.SetDirty(feedback);
            count++;
        }
        if (!before.SequenceEqual(rects.Select(Snapshot))) throw new InvalidOperationException("Menu layout changed during audio authoring.");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return new { scene = scene.path, controls = count, layoutUnchanged = true, rectTransforms = rects.Length };
    }

    private static string Snapshot(RectTransform r) => AnimationUtility.CalculateTransformPath(r, null) + ":" + r.anchoredPosition.ToString("R")
        + r.sizeDelta.ToString("R") + r.anchorMin.ToString("R") + r.anchorMax.ToString("R") + r.pivot.ToString("R");
}
