using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DecisionTimingAuthoring
{
    public static string SeparateLoadedTimingDisplay()
    {
        int count = 0;
        foreach (var banner in Object.FindObjectsByType<MatchEventBannerController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (banner.timingVisibility != null) continue;
            var clone = Object.Instantiate(banner.gameObject, banner.transform.parent);
            clone.name = "DecisionTimingDisplay";
            Undo.RegisterCreatedObjectUndo(clone, "Author separate timing display");
            var copy = clone.GetComponent<MatchEventBannerController>();
            Undo.RecordObject(banner, "Bind timing display");
            banner.timingVisibility = copy.visibility; banner.timingContent = copy.content;
            banner.timingIcon = copy.icon; banner.timingCaption = copy.caption; banner.timingDetail = copy.detail;
            Object.DestroyImmediate(copy);
            var rect = (RectTransform)clone.transform;
            rect.anchoredPosition = ((RectTransform)banner.transform).anchoredPosition + new Vector2(rect.rect.width + 20, 0);
            banner.timingVisibility.alpha = 0;
            banner.timingVisibility.blocksRaycasts = banner.timingVisibility.interactable = false;
            EditorUtility.SetDirty(banner);
            EditorSceneManager.MarkSceneDirty(banner.gameObject.scene);
            count++;
        }
        return $"Authored {count} separate timing display(s).";
    }
    public static string AuthorLoadedScene()
    {
        const string path = "Assets/Art/UI/DecisionClock.png";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
        {
            var p = new Vector2(x - 63.5f, y - 63.5f);
            float radius = p.magnitude;
            bool ring = radius > 49 && radius < 56;
            bool hand = (Mathf.Abs(p.x) < 4 && p.y >= -3 && p.y < 34)
                || (p.x > -3 && p.x < 27 && Mathf.Abs(p.y + p.x * .6f) < 4);
            texture.SetPixel(x, y, ring || hand ? Color.white : Color.clear);
        }
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        int count = 0;
        foreach (var banner in Object.FindObjectsByType<MatchEventBannerController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!banner.gameObject.scene.IsValid()) continue;
            Undo.RecordObject(banner, "Bind decision clock artwork");
            banner.clockArtwork = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            EditorUtility.SetDirty(banner);
            EditorSceneManager.MarkSceneDirty(banner.gameObject.scene);
            count++;
        }
        return $"Bound clock PNG to {count} loaded scene banner(s).";
    }
}
