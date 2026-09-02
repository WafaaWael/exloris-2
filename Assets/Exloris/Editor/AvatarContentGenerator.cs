using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exloris.Runtime.Avatar;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor
{
    public static class AvatarContentGenerator
    {
        public const string ContentAssetPath = "Assets/Exloris/Resources/Content/AvatarContent.asset";
        private const string ImportedRoot = "Assets/Exloris/Content/Imported/Avatar";

        private static readonly string[] RequiredNames =
        {
            "bg-03-traveller-4x3.png", "bg-03-traveller-16x9.png", "bg-03-traveller-24x10.png",
            "chr-boy-idle-01.png", "chr-boy-idle-02.png", "chr-boy-blink.png", "chr-boy-turn.png", "chr-boy-wave.png", "chr-boy-static-reduced.png",
            "chr-girl-idle-01.png", "chr-girl-idle-02.png", "chr-girl-blink.png", "chr-girl-turn.png", "chr-girl-wave.png", "chr-girl-static-reduced.png",
            "anim-boy-idle.motion.json", "anim-boy-preview.motion.json", "anim-girl-idle.motion.json", "anim-girl-preview.motion.json",
            "frame-card.png", "ui-confirm.png", "ui-home.png", "ui-repeat.png", "ui-choose-other.png", "ui-stay.png", "ui-leave.png", "ui-caption-backplate-opaque.png", "ui-pressed-overlay.png",
            "vo-avatar-select-en.wav", "vo-avatar-preview-en.wav", "vo-leave-en.wav",
            "ve-vo-avatar-select.png", "ve-vo-avatar-preview.png", "ve-vo-leave.png",
            "sfx-ui-press.wav", "sfx-ui-confirm.wav", "sfx-ui-back.wav",
        };

        [MenuItem("Exloris/Avatar/Generate Approved Content Set")]
        public static void Generate()
        {
            var files = Directory.GetFiles(ImportedRoot)
                .Where(path => !path.EndsWith(".meta", StringComparison.Ordinal))
                .Select(Path.GetFileName)
                .ToArray();
            ValidateFileSet(files);
            ConfigureImporters();
            ValidateImportedAssets();
            EnsureFolder("Assets/Exloris/Resources/Content");

            var content = AssetDatabase.LoadAssetAtPath<AvatarContent>(ContentAssetPath);
            if (content == null)
            {
                content = ScriptableObject.CreateInstance<AvatarContent>();
                AssetDatabase.CreateAsset(content, ContentAssetPath);
            }

            content.ConfigureApproved(
                LoadSprites("bg-03-traveller-4x3.png", "bg-03-traveller-16x9.png", "bg-03-traveller-24x10.png"),
                LoadSprites("chr-boy-idle-01.png", "chr-boy-idle-02.png", "chr-boy-blink.png", "chr-boy-turn.png", "chr-boy-wave.png", "chr-boy-static-reduced.png"),
                LoadSprites("chr-girl-idle-01.png", "chr-girl-idle-02.png", "chr-girl-blink.png", "chr-girl-turn.png", "chr-girl-wave.png", "chr-girl-static-reduced.png"),
                LoadAssets<TextAsset>("anim-boy-idle.motion.json", "anim-boy-preview.motion.json", "anim-girl-idle.motion.json", "anim-girl-preview.motion.json"),
                LoadSprites("frame-card.png", "ui-confirm.png", "ui-home.png", "ui-repeat.png", "ui-choose-other.png", "ui-stay.png", "ui-leave.png", "ui-caption-backplate-opaque.png", "ui-pressed-overlay.png"),
                LoadAssets<AudioClip>("vo-avatar-select-en.wav", "vo-avatar-preview-en.wav", "vo-leave-en.wav"),
                LoadSprites("ve-vo-avatar-select.png", "ve-vo-avatar-preview.png", "ve-vo-leave.png"),
                LoadAssets<AudioClip>("sfx-ui-press.wav", "sfx-ui-confirm.wav", "sfx-ui-back.wav"));
            EditorUtility.SetDirty(content);
            AssetDatabase.SaveAssets();
            Debug.Log("Generated approved Avatar content set.");
        }

        public static void ValidateFileSet(IEnumerable<string> names)
        {
            var actual = names.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            var expected = RequiredNames.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
                throw new InvalidOperationException("Avatar import folder must contain exactly the approved 37 files.");
        }

        public static void ValidatePixelData(
            string name, int width, int height, int bitDepth, bool hasAlpha, bool hasVisiblePixels)
        {
            var expected = ExpectedImage(name);
            if (width != expected.x || height != expected.y || bitDepth != 8 ||
                hasAlpha != !name.StartsWith("bg-", StringComparison.Ordinal) || !hasVisiblePixels)
                throw new InvalidOperationException("Avatar image contract failed: " + name);
        }

        public static void ValidateTextureProfile(
            string name, int maxSize, bool sRgb, bool mipmaps, Vector2 pivot, bool hasBakedText)
        {
            var expectedMax = name.StartsWith("bg-", StringComparison.Ordinal) ? 4096 :
                name == "frame-card.png" || name == "ui-caption-backplate-opaque.png" || name.StartsWith("chr-", StringComparison.Ordinal) || name.StartsWith("ve-", StringComparison.Ordinal) ? 1024 : 512;
            var expectedPivot = name.StartsWith("chr-", StringComparison.Ordinal)
                ? new Vector2(0.5f, 0.08f)
                : new Vector2(0.5f, 0.5f);
            if (maxSize != expectedMax || !sRgb || mipmaps || hasBakedText ||
                Vector2.Distance(pivot, expectedPivot) > 0.0001f)
                throw new InvalidOperationException("Avatar texture profile failed: " + name);
        }

        public static void ValidateMotionOwner(string name, string semanticOwner)
        {
            var expectedAvatar = name.Contains("-boy-") ? "boy" : name.Contains("-girl-") ? "girl" : null;
            var expectedMotion = name.Contains("-idle.") ? "idle" : name.Contains("-preview.") ? "preview" : null;
            if (expectedAvatar == null || expectedMotion == null ||
                semanticOwner != expectedAvatar + " traveller avatar " + expectedMotion)
                throw new InvalidOperationException("Avatar motion owner failed: " + name);
        }

        private static void ConfigureImporters()
        {
            foreach (var name in RequiredNames.Where(name => name.EndsWith(".png", StringComparison.Ordinal)))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(PathFor(name));
                if (importer == null) throw new InvalidOperationException("Missing Avatar texture: " + name);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.spritePivot = name.StartsWith("chr-", StringComparison.Ordinal)
                    ? new Vector2(0.5f, 0.08f)
                    : new Vector2(0.5f, 0.5f);
                var textureSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteMeshType = SpriteMeshType.FullRect;
                textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
                textureSettings.spritePivot = importer.spritePivot;
                importer.SetTextureSettings(textureSettings);
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = !name.StartsWith("bg-", StringComparison.Ordinal);
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                var settings = importer.GetDefaultPlatformTextureSettings();
                settings.maxTextureSize = name.StartsWith("bg-", StringComparison.Ordinal) ? 4096 :
                    name == "frame-card.png" || name == "ui-caption-backplate-opaque.png" || name.StartsWith("chr-", StringComparison.Ordinal) || name.StartsWith("ve-", StringComparison.Ordinal) ? 1024 : 512;
                importer.SetPlatformTextureSettings(settings);
                if (name == "frame-card.png") importer.spriteBorder = new Vector4(80f, 80f, 80f, 80f);
                if (name == "ui-caption-backplate-opaque.png") importer.spriteBorder = new Vector4(28f, 28f, 28f, 28f);
                importer.SaveAndReimport();
            }

            foreach (var name in RequiredNames.Where(name => name.EndsWith(".wav", StringComparison.Ordinal)))
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(PathFor(name));
                if (importer == null) throw new InvalidOperationException("Missing Avatar audio: " + name);
                importer.forceToMono = true;
                var settings = importer.defaultSampleSettings;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                settings.preloadAudioData = true;
                if (name.StartsWith("vo-", StringComparison.Ordinal))
                {
                    settings.loadType = AudioClipLoadType.CompressedInMemory;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = 0.7f;
                }
                else
                {
                    settings.loadType = AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat = AudioCompressionFormat.PCM;
                }
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        private static void ValidateImportedAssets()
        {
            foreach (var name in RequiredNames.Where(name => name.EndsWith(".png", StringComparison.Ordinal)))
            {
                var bytes = File.ReadAllBytes(PathFor(name));
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(texture, bytes)) throw new InvalidOperationException("Cannot decode " + name);
                var visible = texture.GetPixels32().Any(pixel => pixel.a > 0);
                ValidatePixelData(name, texture.width, texture.height, bytes[24], bytes[25] == 6, visible);
                UnityEngine.Object.DestroyImmediate(texture);
                var importer = (TextureImporter)AssetImporter.GetAtPath(PathFor(name));
                ValidateTextureProfile(name, importer.GetDefaultPlatformTextureSettings().maxTextureSize,
                    importer.sRGBTexture, importer.mipmapEnabled, importer.spritePivot, false);
            }

            foreach (var name in RequiredNames.Where(name => name.EndsWith(".json", StringComparison.Ordinal)))
            {
                var contract = JsonUtility.FromJson<MotionContract>(File.ReadAllText(PathFor(name)));
                ValidateMotionOwner(name, contract.semantic_owner);
            }
        }

        private static Vector2Int ExpectedImage(string name)
        {
            if (name == "bg-03-traveller-4x3.png") return new Vector2Int(2048, 1536);
            if (name == "bg-03-traveller-16x9.png") return new Vector2Int(2560, 1440);
            if (name == "bg-03-traveller-24x10.png") return new Vector2Int(3456, 1440);
            if (name.StartsWith("chr-", StringComparison.Ordinal) || name == "frame-card.png" || name == "ui-caption-backplate-opaque.png") return new Vector2Int(1024, 1024);
            if (name.StartsWith("ve-", StringComparison.Ordinal)) return new Vector2Int(1024, 512);
            if (name.StartsWith("ui-", StringComparison.Ordinal)) return new Vector2Int(512, 512);
            throw new InvalidOperationException("Unapproved Avatar image: " + name);
        }

        private static Sprite[] LoadSprites(params string[] names) => LoadAssets<Sprite>(names);

        private static T[] LoadAssets<T>(params string[] names) where T : UnityEngine.Object
        {
            return names.Select(name =>
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(PathFor(name));
                if (asset == null) throw new InvalidOperationException("Missing approved Avatar asset: " + name);
                return asset;
            }).ToArray();
        }

        private static string PathFor(string name) => ImportedRoot + "/" + name;

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        [Serializable]
        private sealed class MotionContract
        {
            public string semantic_owner;
        }
    }
}
