using System;
using System.Globalization;
using System.IO;
using Exloris.Content.AssetCatalog;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor.AssetImport
{
    public sealed class ExlorisAssetPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!AssetImportGuard.IsManagedAssetPath(assetPath)) return;
            var entry = RequireEntry();
            var policy = AssetImportPolicy.ForProfile(entry.ProfileId);
            if (policy.Kind != AssetKind.Texture) throw Reject(entry, "Texture file uses non-texture profile.");

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = policy.PixelsPerUnit;
            importer.sRGBTexture = policy.Srgb;
            importer.alphaIsTransparency = policy.AlphaIsTransparency;
            importer.mipmapEnabled = policy.Mipmaps;
            importer.isReadable = false;
            importer.wrapMode = policy.WrapMode;
            importer.filterMode = policy.FilterMode;
            importer.maxTextureSize = policy.MaxTextureSize;
            importer.spritePivot = ParsePivot(entry.Pivot);

            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            textureSettings.spritePivot = importer.spritePivot;
            importer.SetTextureSettings(textureSettings);

            ApplyPlatformTextureSettings(importer, "iPhone", policy);
            ApplyPlatformTextureSettings(importer, "Android", policy);
        }

        private void OnPreprocessAudio()
        {
            if (!AssetImportGuard.IsManagedAssetPath(assetPath)) return;
            var entry = RequireEntry();
            var policy = AssetImportPolicy.ForProfile(entry.ProfileId);
            if (policy.Kind != AssetKind.Audio) throw Reject(entry, "Audio file uses non-audio profile.");

            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = policy.ForceMono;
            importer.loadInBackground = policy.AudioLoadType == AudioClipLoadType.Streaming;
            importer.defaultSampleSettings = new AudioImporterSampleSettings
            {
                loadType = policy.AudioLoadType,
                compressionFormat = policy.AudioCompressionFormat,
                quality = policy.AudioCompressionFormat == AudioCompressionFormat.Vorbis ? 0.7f : 1f,
                sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                preloadAudioData = policy.AudioLoadType != AudioClipLoadType.Streaming
            };
        }

        private void OnPreprocessAsset()
        {
            if (!AssetImportGuard.IsManagedAssetPath(assetPath)) return;
            var entry = RequireEntry();
            var rejection = AssetImportGuard.Validate(entry);
            if (rejection != AssetImportRejection.None) throw Reject(entry, rejection.ToString());

            if (assetImporter is VideoClipImporter videoImporter)
            {
                var policy = AssetImportPolicy.ForProfile(entry.ProfileId);
                if (policy.Kind != AssetKind.Video) throw Reject(entry, "Video file uses non-video profile.");
                videoImporter.importAudio = policy.ImportVideoAudio;
                var settings = videoImporter.defaultTargetSettings;
                settings.enableTranscoding = policy.TranscodeVideo;
                videoImporter.defaultTargetSettings = settings;
            }
        }

        private FinalAssetEntry RequireEntry()
        {
            if (!AssetImportGuard.IsManagedAssetPath(assetPath)) return null;
            var catalogue = AssetDatabase.LoadAssetAtPath<FinalAssetCatalogue>(FinalAssetInventoryImporter.CatalogueAssetPath);
            if (catalogue == null) throw new InvalidDataException("Final asset catalogue is missing; managed content import is blocked.");

            var runtimePath = assetPath.Substring(FinalAssetInventoryImporter.ImportedAssetRoot.Length);
            var entry = catalogue.FindByRuntimePath(runtimePath);
            if (entry == null) throw new InvalidDataException("Unlisted managed asset is blocked: " + assetPath);
            return entry;
        }

        private static InvalidDataException Reject(FinalAssetEntry entry, string reason)
        {
            return new InvalidDataException((entry == null ? "Unlisted asset" : entry.AssetId) + " rejected: " + reason);
        }

        private static Vector2 ParsePivot(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "n/a", StringComparison.OrdinalIgnoreCase))
            {
                return new Vector2(0.5f, 0.5f);
            }

            var parts = value.Trim('(', ')').Split(',');
            if (parts.Length != 2 ||
                !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
                !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
            {
                throw new InvalidDataException("Invalid frozen sprite pivot: " + value);
            }

            return new Vector2(x, y);
        }

        private static void ApplyPlatformTextureSettings(TextureImporter importer, string platform, AssetImportPolicy policy)
        {
            var format = policy.AstcBlockSize == 4
                ? TextureImporterFormat.ASTC_4x4
                : policy.AstcBlockSize == 6
                    ? TextureImporterFormat.ASTC_6x6
                    : TextureImporterFormat.ASTC_8x8;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = platform,
                overridden = true,
                maxTextureSize = policy.MaxTextureSize,
                format = format,
                textureCompression = TextureImporterCompression.Compressed,
                compressionQuality = 100,
                androidETC2FallbackOverride = AndroidETC2FallbackOverride.Quality32Bit
            });
        }
    }
}
