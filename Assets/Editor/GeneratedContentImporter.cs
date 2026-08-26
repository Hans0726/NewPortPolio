using System.IO;
using UnityEditor;
using UnityEngine;

internal sealed class GeneratedContentImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        bool isCardImage = assetPath.StartsWith("Assets/Resources/CardImage/");
        bool isFieldSprite = assetPath.StartsWith("Assets/Resources/FieldSprites/");
        if ((!isCardImage && !isFieldSprite)) return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.spritePivot = isFieldSprite ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
    }

    private void OnPreprocessAudio()
    {
        bool isBgm = assetPath.StartsWith("Assets/Resources/Audio/BGM/");
        bool isSfx = assetPath.StartsWith("Assets/Resources/Audio/SFX/");
        if (!isBgm && !isSfx) return;

        AudioImporter importer = (AudioImporter)assetImporter;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = isBgm
            ? AudioClipLoadType.Streaming
            : AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = isBgm ? 0.65f : 0.8f;
        settings.preloadAudioData = isSfx;
    }
}
