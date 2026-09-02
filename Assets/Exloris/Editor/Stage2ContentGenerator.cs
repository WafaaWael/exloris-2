using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Exloris.Content.AssetCatalog;
using Exloris.Editor.AssetImport;
using Exloris.Runtime.Stage2;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor
{
    public static class Stage2ContentGenerator
    {
        public const string ContentAssetPath = "Assets/Exloris/Resources/Content/Stage2Content.asset";
        public const string ImportedRoot = "Assets/Exloris/Content/Imported/Stage2";
        private const string CaptionPath = "Assets/Exloris/Content/Imported/captions/caption-timings-en-v01.json";

        [MenuItem("Exloris/Stage 2/Import and Generate Approved Content")]
        public static void ImportAndGenerate()
        {
            EnsureFolder(ImportedRoot);
            var rows = RequiredRows().ToArray();
            ValidateSourceAssets(rows);
            foreach (var row in rows) File.Copy(WorkspacePath(row.SourcePath), ProjectPath(row.Name), true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateFileSet(Directory.GetFiles(ProjectRootPath())
                .Where(path => !path.EndsWith(".meta", StringComparison.Ordinal)).Select(Path.GetFileName));
            ConfigureImporters(rows);
            ValidateImportedAssets(rows);
            GenerateResource();
            Debug.Log("Imported and generated approved Stage 2 content.");
        }

        public static void ValidateFileSet(IEnumerable<string> names)
        {
            var actual = names.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            var expected = RequiredSourcePaths().Select(Path.GetFileName)
                .OrderBy(name => name, StringComparer.Ordinal).ToArray();
            if (!actual.SequenceEqual(expected, StringComparer.Ordinal) || actual.Length != 45)
                throw new InvalidOperationException("Stage 2 import folder must contain exactly the approved 45 files.");
        }

        private static IEnumerable<SourceRow> RequiredRows()
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<FinalAssetCatalogue>(FinalAssetInventoryImporter.CatalogueAssetPath);
            if (catalogue == null) throw new InvalidOperationException("Final asset catalogue is missing.");
            foreach (var sourcePath in RequiredSourcePaths())
            {
                var entry = catalogue.Entries.SingleOrDefault(item =>
                    string.Equals(item.SourcePath, sourcePath, StringComparison.Ordinal));
                if (entry == null || entry.State != "Final" || !entry.IsActive || entry.IsPlaceholder)
                    throw new InvalidOperationException("Stage 2 source is not an active Final row: " + sourcePath);
                yield return new SourceRow
                {
                    SourcePath = sourcePath,
                    Name = Path.GetFileName(sourcePath),
                    Bytes = entry.Bytes,
                    Sha256 = entry.Sha256,
                    ProfileId = entry.ProfileId,
                    Pivot = entry.Pivot
                };
            }
        }

        private static IEnumerable<string> RequiredSourcePaths()
        {
            foreach (var level in new[] { "bg-11-river-low", "bg-12-river-middle", "bg-13-river-high-v2" })
            foreach (var ratio in new[] { "4x3", "16x9", "24x10" })
                yield return "assets/step-06-environments/runtime/" + level + "-" + ratio + ".png";
            foreach (var name in new[]
            {
                "prop-stone-low.png", "prop-stone-middle.png", "prop-stone-high.png",
                "hotspot-field.png", "hotspot-path.png", "hotspot-boat.png",
                "out-s2-low-field-layer.png", "out-s2-low-path-layer.png", "out-s2-low-boat-layer.png",
                "out-s2-middle-field-layer.png", "out-s2-middle-path-layer.png", "out-s2-middle-boat-layer.png",
                "out-s2-high-field-layer.png", "out-s2-high-path-layer.png", "out-s2-high-boat-layer.png"
            }) yield return "assets/step-07-gameplay-art/runtime/" + name;
            foreach (var name in new[] { "chr-boy-stage-reaction-neutral.png", "chr-girl-stage-reaction-neutral.png" })
                yield return "assets/step-05-characters/runtime/" + name;
            foreach (var name in new[]
            {
                "ui-play.png", "ui-help.png", "ui-repeat.png", "ui-pause.png", "ui-home.png",
                "ui-compare-another.png", "ui-finish.png", "ui-caption-backplate-opaque.png", "ui-pressed-overlay.png"
            }) yield return "assets/step-04-ui/runtime/" + name;
            foreach (var name in new[] { "ve-vo-s2-intro.png", "ve-vo-s2-help.png" })
                yield return "production/step-10-voice-captions/visual-equivalents/" + name;
            foreach (var name in new[] { "vo-s2-intro-en.wav", "vo-s2-help-en.wav" })
                yield return "production/step-10-voice-captions/voice/en/" + name;
            yield return "production/step-09-animation/manifests/anim-stage2-outcome.motion.json";
            yield return "production/step-11-audio/music/mus-stage2-loop.wav";
            yield return "production/step-11-audio/ambience/amb-river-loop.wav";
            yield return "production/step-11-audio/sfx/sfx-stone-select-low.wav";
            yield return "production/step-11-audio/sfx/sfx-stone-select-middle.wav";
            yield return "production/step-11-audio/sfx/sfx-stone-select-high.wav";
        }

        private static void ValidateSourceAssets(IEnumerable<SourceRow> rows)
        {
            foreach (var row in rows)
            {
                var path = WorkspacePath(row.SourcePath);
                if (!File.Exists(path)) throw new FileNotFoundException("Stage 2 source is missing.", path);
                var info = new FileInfo(path);
                if (info.Length != row.Bytes || !string.Equals(Hash(path), row.Sha256, StringComparison.Ordinal))
                    throw new InvalidDataException("Stage 2 source bytes changed: " + row.SourcePath);
            }
        }

        private static void ConfigureImporters(IEnumerable<SourceRow> rows)
        {
            foreach (var row in rows)
            {
                var assetPath = ImportedRoot + "/" + row.Name;
                var policy = AssetImportPolicy.ForProfile(row.ProfileId);
                if (policy.Kind == AssetKind.Texture) ConfigureTexture(assetPath, row, policy);
                else if (policy.Kind == AssetKind.Audio) ConfigureAudio(assetPath, policy);
                else if (policy.Kind != AssetKind.Data)
                    throw new InvalidDataException("Stage 2 row has an unsupported import profile: " + row.ProfileId);
            }
        }

        private static void ConfigureTexture(string assetPath, SourceRow row, AssetImportPolicy policy)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (importer == null) throw new InvalidOperationException("Missing Stage 2 texture importer: " + assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = policy.PixelsPerUnit;
            importer.spritePivot = ParsePivot(row.Pivot);
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = importer.spritePivot;
            importer.SetTextureSettings(settings);
            importer.sRGBTexture = policy.Srgb;
            importer.alphaIsTransparency = policy.AlphaIsTransparency;
            importer.mipmapEnabled = policy.Mipmaps;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = policy.WrapMode;
            importer.filterMode = policy.FilterMode;
            var defaults = importer.GetDefaultPlatformTextureSettings();
            defaults.maxTextureSize = policy.MaxTextureSize;
            importer.SetPlatformTextureSettings(defaults);
            ConfigureStandalonePackaging(importer, policy);
            ConfigurePlatform(importer, "iPhone", policy);
            ConfigurePlatform(importer, "Android", policy);
            if (row.Name == "ui-caption-backplate-opaque.png") importer.spriteBorder = new Vector4(28, 28, 28, 28);
            importer.SaveAndReimport();
        }

        private static void ConfigureStandalonePackaging(
            TextureImporter importer,
            AssetImportPolicy policy)
        {
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Standalone",
                overridden = true,
                maxTextureSize = policy.MaxTextureSize,
                format = policy.AlphaIsTransparency
                    ? TextureImporterFormat.DXT5Crunched
                    : TextureImporterFormat.DXT1Crunched,
                textureCompression = TextureImporterCompression.CompressedHQ,
                compressionQuality = 100,
                crunchedCompression = true
            });
        }

        private static void ConfigurePlatform(TextureImporter importer, string platformName, AssetImportPolicy policy)
        {
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = platformName,
                overridden = true,
                maxTextureSize = policy.MaxTextureSize,
                format = policy.AstcBlockSize == 4 ? TextureImporterFormat.ASTC_4x4
                    : policy.AstcBlockSize == 6 ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_8x8,
                textureCompression = TextureImporterCompression.Compressed,
                compressionQuality = 100,
                androidETC2FallbackOverride = AndroidETC2FallbackOverride.Quality32Bit
            });
        }

        private static void ConfigureAudio(string assetPath, AssetImportPolicy policy)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(assetPath);
            if (importer == null) throw new InvalidOperationException("Missing Stage 2 audio importer: " + assetPath);
            importer.forceToMono = policy.ForceMono;
            importer.loadInBackground = policy.AudioLoadType == AudioClipLoadType.Streaming;
            var settings = importer.defaultSampleSettings;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.compressionFormat = policy.AudioCompressionFormat;
            settings.quality = policy.AudioCompressionFormat == AudioCompressionFormat.Vorbis ? 0.75f : 1f;
            settings.loadType = policy.AudioLoadType;
            settings.preloadAudioData = policy.AudioLoadType != AudioClipLoadType.Streaming;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static void ValidateImportedAssets(IEnumerable<SourceRow> rows)
        {
            foreach (var row in rows)
                if (!string.Equals(Hash(ProjectPath(row.Name)), row.Sha256, StringComparison.Ordinal))
                    throw new InvalidDataException("Imported Stage 2 bytes changed: " + row.Name);
        }

        private static void GenerateResource()
        {
            EnsureFolder("Assets/Exloris/Resources/Content");
            var content = AssetDatabase.LoadAssetAtPath<Stage2Content>(ContentAssetPath);
            if (content == null)
            {
                content = ScriptableObject.CreateInstance<Stage2Content>();
                AssetDatabase.CreateAsset(content, ContentAssetPath);
            }
            content.ConfigureApproved(
                LoadSprites(
                    "bg-11-river-low-4x3.png", "bg-11-river-low-16x9.png", "bg-11-river-low-24x10.png",
                    "bg-12-river-middle-4x3.png", "bg-12-river-middle-16x9.png", "bg-12-river-middle-24x10.png",
                    "bg-13-river-high-v2-4x3.png", "bg-13-river-high-v2-16x9.png", "bg-13-river-high-v2-24x10.png"),
                LoadSprites("prop-stone-low.png", "prop-stone-middle.png", "prop-stone-high.png"),
                LoadSprites("hotspot-field.png", "hotspot-path.png", "hotspot-boat.png"),
                LoadSprites(
                    "out-s2-low-field-layer.png", "out-s2-low-path-layer.png", "out-s2-low-boat-layer.png",
                    "out-s2-middle-field-layer.png", "out-s2-middle-path-layer.png", "out-s2-middle-boat-layer.png",
                    "out-s2-high-field-layer.png", "out-s2-high-path-layer.png", "out-s2-high-boat-layer.png"),
                LoadSprites("chr-boy-stage-reaction-neutral.png", "chr-girl-stage-reaction-neutral.png"),
                LoadSprites("ui-play.png", "ui-help.png", "ui-repeat.png", "ui-pause.png", "ui-home.png", "ui-compare-another.png", "ui-finish.png"),
                LoadSprites("ui-caption-backplate-opaque.png", "ui-pressed-overlay.png", "ve-vo-s2-intro.png", "ve-vo-s2-help.png"),
                LoadAudio("vo-s2-intro-en.wav", "vo-s2-help-en.wav"),
                AssetDatabase.LoadAssetAtPath<TextAsset>(CaptionPath),
                Load<TextAsset>("anim-stage2-outcome.motion.json"),
                Load<AudioClip>("mus-stage2-loop.wav"), Load<AudioClip>("amb-river-loop.wav"),
                LoadAudio("sfx-stone-select-low.wav", "sfx-stone-select-middle.wav", "sfx-stone-select-high.wav"));
            EditorUtility.SetDirty(content);
            AssetDatabase.SaveAssets();
        }

        private static Sprite[] LoadSprites(params string[] names) => names.Select(Load<Sprite>).ToArray();
        private static AudioClip[] LoadAudio(params string[] names) => names.Select(Load<AudioClip>).ToArray();
        private static T Load<T>(string name) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(ImportedRoot + "/" + name);
            if (asset == null) throw new InvalidOperationException("Missing approved Stage 2 asset: " + name);
            return asset;
        }

        private static Vector2 ParsePivot(string pivot)
        {
            var values = pivot.Trim('(', ')').Split(',');
            if (values.Length != 2) throw new InvalidDataException("Invalid frozen pivot: " + pivot);
            return new Vector2(float.Parse(values[0], CultureInfo.InvariantCulture),
                float.Parse(values[1], CultureInfo.InvariantCulture));
        }

        private static string WorkspacePath(string relative) => Path.Combine(Directory.GetParent(ProjectRoot()).FullName, relative);
        private static string ProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string ProjectRootPath() => Path.Combine(ProjectRoot(), ImportedRoot);
        private static string ProjectPath(string name) => Path.Combine(ProjectRootPath(), name);
        private static string Hash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(stream)
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/'); var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private sealed class SourceRow
        {
            internal string SourcePath;
            internal string Name;
            internal long Bytes;
            internal string Sha256;
            internal string ProfileId;
            internal string Pivot;
        }
    }
}
