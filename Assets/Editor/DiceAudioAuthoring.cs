using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DiceAudioAuthoring
{
    public const string ClipPath = "Assets/Audio/UI/DiceRoll.wav";
    public static object Generate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires edit mode.");
        const int rate = 48000;
        var samples = new float[rate / 4];
        var random = new System.Random(5831);
        float low = 0, peak = 0;
        // Short dry wooden rattle: decreasing, irregular impact bursts, no gameplay RNG.
        float[] impacts = { .008f, .035f, .068f, .104f, .151f, .199f };
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate, noise = (float)(random.NextDouble() * 2 - 1);
            low += .32f * (noise - low);
            foreach (float start in impacts)
            {
                float age = t - start;
                if (age < 0) continue;
                float attack = Mathf.Min(1, age * 2400);
                samples[i] += attack * Mathf.Exp(-age * 180) * (1 - start * 2)
                    * (low * .7f + Mathf.Sin(age * 2 * Mathf.PI * 1450) * .3f);
            }
            samples[i] *= Mathf.Min(1, (samples.Length - 1 - i) / 240f);
            peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
        }
        using (var writer = new BinaryWriter(File.Create(ClipPath)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1);
            writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
            foreach (float sample in samples) writer.Write((short)Mathf.RoundToInt(sample / peak * .38f * short.MaxValue));
        }
        AssetDatabase.ImportAsset(ClipPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (AudioImporter)AssetImporter.GetAtPath(ClipPath);
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.PCM;
        importer.defaultSampleSettings = settings; importer.forceToMono = true; importer.SaveAndReimport();
        var library = AssetDatabase.LoadAssetAtPath<GameplayAudioLibrary>(GameplayAudioEditorTools.LibraryPath);
        library.pools.Single(p => p.kind == GameplayAudioPool.Dice).clips = new[] { AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath) };
        EditorUtility.SetDirty(library); AssetDatabase.SaveAssets();
        return new { clip = ClipPath, seconds = .25f, category = "Game" };
    }
}
