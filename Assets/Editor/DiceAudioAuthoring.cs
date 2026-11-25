using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DiceAudioAuthoring
{
    public const string ClipPath = "Assets/Audio/UI/DiceRoll.wav";
    public static readonly string[] ClipPaths =
    {
        "Assets/Audio/UI/DiceRoll1.wav", "Assets/Audio/UI/DiceRoll2.wav",
        "Assets/Audio/UI/DiceRoll3.wav", "Assets/Audio/UI/DiceRoll4.wav",
        "Assets/Audio/UI/DiceRoll5.wav", "Assets/Audio/UI/DiceRoll6.wav"
    };
    public const string WiggleClipPath = "Assets/Audio/UI/DiceWiggle.wav";
    public static object Generate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires edit mode.");
        var clips = new UnityEngine.AudioClip[ClipPaths.Length];
        for (int i = 0; i < ClipPaths.Length; i++)
        {
            AssetDatabase.ImportAsset(ClipPaths[i], ImportAssetOptions.ForceSynchronousImport);
            var importer = (AudioImporter)AssetImporter.GetAtPath(ClipPaths[i]);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
            clips[i] = AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>(ClipPaths[i]);
        }
        var library = AssetDatabase.LoadAssetAtPath<GameplayAudioLibrary>(GameplayAudioEditorTools.LibraryPath);
        var pools = library.pools.ToList();
        var dicePool = pools.Single(p => p.kind == GameplayAudioPool.Dice);
        dicePool.clips = clips;
        var wigglePool = pools.SingleOrDefault(p => p.kind == GameplayAudioPool.DiceWiggle);
        if (wigglePool == null)
        {
            wigglePool = new GameplayAudioLibrary.Pool { kind = GameplayAudioPool.DiceWiggle };
            pools.Add(wigglePool);
        }
        AssetDatabase.ImportAsset(WiggleClipPath, ImportAssetOptions.ForceSynchronousImport);
        var wiggleImporter = (AudioImporter)AssetImporter.GetAtPath(WiggleClipPath);
        var wiggleSettings = wiggleImporter.defaultSampleSettings;
        wiggleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
        wiggleSettings.compressionFormat = AudioCompressionFormat.PCM;
        wiggleImporter.defaultSampleSettings = wiggleSettings;
        wiggleImporter.forceToMono = true;
        wiggleImporter.SaveAndReimport();
        wigglePool.clips = new[] { AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>(WiggleClipPath) };
        library.pools = pools.ToArray();
        EditorUtility.SetDirty(library); AssetDatabase.SaveAssets();
        return new { clips = ClipPaths, wiggle = WiggleClipPath, seconds = "0.72 per roll; 4.8 second wait cue", category = "Game" };
    }
}
