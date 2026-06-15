using System;
using UnityEditor;
using UnityEngine;

public sealed class FlagSpriteImporter : AssetPostprocessor
{
    private const string FlagsFolder = "Assets/Resources/Flags/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(FlagsFolder, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.wrapMode = TextureWrapMode.Clamp;
    }
}
