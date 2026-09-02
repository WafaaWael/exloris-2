using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Exloris.Content.AssetCatalog;
using Exloris.Editor.AssetImport;
using Exloris.Runtime.Stage3;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor
{
    public static class Stage3ContentGenerator
    {
        public const string ImportedRoot = "Assets/Exloris/Content/Imported/Stage3";
        public const string ContentAssetPath = "Assets/Exloris/Resources/Content/Stage3Content.asset";
        public const string CaptionName = "stage-3-caption-timings.json";

        [Serializable] public sealed class AssetLedger { public string status; public AssetRow[] assets; }
        [Serializable] public sealed class AssetRow { public string source_path; public string runtime_name; public long bytes; public string sha256; }
        [Serializable] private sealed class CaptionWrapper { public CaptionItem[] items; }
        [Serializable] private sealed class CaptionItem { public string string_id; public string audio_file; public int duration_ms; public CaptionCue[] cues; }
        [Serializable] private sealed class CaptionCue { public string cue_id; public int start_ms; public int end_ms; public string text; }

        [MenuItem("Exloris/Stage 3/Import and Generate Approved Content")]
        public static void ImportAndGenerate()
        {
            var ledger = ReadLedger();
            ValidateLedger(ledger);
            var rows = RequiredRows(ledger).ToArray();
            EnsureFolder(ImportedRoot);
            foreach (var row in rows)
                File.Copy(Path.Combine(WorkspaceRoot(), row.SourcePath), ProjectPath(row.Name), true);
            GenerateFilteredCaptions();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateFileSet(Directory.GetFiles(ProjectRootPath())
                .Where(path => !path.EndsWith(".meta", StringComparison.Ordinal)).Select(Path.GetFileName));
            ConfigureImporters(rows);
            ValidateImportedAssets(rows);
            GenerateResource();
            AssetDatabase.SaveAssets();
            Debug.Log("Imported and generated approved Stage 3 content.");
        }

        public static AssetLedger ReadLedger() => JsonUtility.FromJson<AssetLedger>(
            File.ReadAllText(Path.Combine(WorkspaceRoot(), "evidence/stage-3/assets/ASSET-VALIDATION.json")));

        public static void ValidateLedger(AssetLedger ledger)
        {
            if (ledger == null || ledger.status != "PASS" || ledger.assets == null || ledger.assets.Length != 24)
                throw new InvalidOperationException("Stage 3 ledger must contain exactly 24 passing rows.");
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in ledger.assets)
            {
                if (row == null || string.IsNullOrEmpty(row.source_path) || string.IsNullOrEmpty(row.runtime_name)
                    || row.bytes <= 0 || row.sha256 == null || row.sha256.Length != 64
                    || row.source_path.IndexOf("/review/", StringComparison.OrdinalIgnoreCase) >= 0
                    || row.runtime_name.StartsWith("out-low-", StringComparison.OrdinalIgnoreCase)
                    || row.runtime_name.IndexOf("hub", StringComparison.OrdinalIgnoreCase) >= 0
                    || !names.Add(row.runtime_name))
                    throw new InvalidOperationException("Invalid Stage 3 ledger row.");
                var path = Path.Combine(WorkspaceRoot(), row.source_path);
                if (!File.Exists(path) || new FileInfo(path).Length != row.bytes || Hash(path) != row.sha256)
                    throw new InvalidOperationException("Stage 3 source changed: " + row.source_path);
            }
        }

        public static void ValidateFileSet(IEnumerable<string> names)
        {
            var actual = names.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            var expected = ExpectedRuntimeNames().OrderBy(name => name, StringComparer.Ordinal).ToArray();
            if (!actual.SequenceEqual(expected, StringComparer.Ordinal) || actual.Length != 39)
                throw new InvalidOperationException("Stage 3 import folder must contain exactly the approved 39 files.");
        }

        private static IEnumerable<SourceRow> RequiredRows(AssetLedger ledger)
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<FinalAssetCatalogue>(FinalAssetInventoryImporter.CatalogueAssetPath);
            if (catalogue == null) throw new InvalidOperationException("Final asset catalogue is missing.");
            foreach (var sourcePath in ledger.assets.Select(row => row.source_path).Concat(SharedSourcePaths()))
            {
                var entry = catalogue.Entries.SingleOrDefault(item => string.Equals(item.SourcePath, sourcePath, StringComparison.Ordinal));
                if (entry == null || entry.State != "Final" || !entry.IsActive || entry.IsPlaceholder)
                    throw new InvalidOperationException("Stage 3 source is not an active Final row: " + sourcePath);
                if (sourcePath.IndexOf("/review/", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new InvalidOperationException("Review-only source cannot enter Stage 3: " + sourcePath);
                yield return new SourceRow
                {
                    SourcePath = sourcePath,
                    Name = Path.GetFileName(sourcePath),
                    Bytes = entry.Bytes,
                    Sha256 = entry.Sha256,
                    ProfileId = entry.ProfileId,
                    Pivot = entry.Pivot,
                };
            }
        }

        private static IEnumerable<string> SharedSourcePaths()
        {
            foreach (var name in new[] { "chr-boy-stage-reaction-neutral.png", "chr-girl-stage-reaction-neutral.png" })
                yield return "assets/step-05-characters/runtime/" + name;
            foreach (var name in new[]
            {
                "ui-play.png", "ui-help.png", "ui-repeat.png", "ui-pause.png", "ui-home.png",
                "ui-finish.png", "ui-another-preparation.png", "ui-another-river.png",
                "ui-watch-again.png", "ui-back.png", "ui-caption-backplate-opaque.png", "ui-pressed-overlay.png"
            })
                yield return "assets/step-04-ui/runtime/" + name;
        }

        private static IEnumerable<string> ExpectedRuntimeNames()
        {
            foreach (var name in ReadLedger().assets.Select(row => row.runtime_name)) yield return name;
            foreach (var path in SharedSourcePaths()) yield return Path.GetFileName(path);
            yield return CaptionName;
        }

        private static void GenerateFilteredCaptions()
        {
            var sourcePath = Path.Combine(WorkspaceRoot(), "production/step-10-voice-captions/captions/caption-timings-en-v01.json");
            var wrapper = JsonUtility.FromJson<CaptionWrapper>("{\"items\":" + File.ReadAllText(sourcePath) + "}");
            if (wrapper?.items == null) throw new InvalidDataException("Canonical caption source is invalid.");
            var selected = new[] { "VO-S3-INTRO", "VO-S3-OUTCOME" }
                .Select(id => wrapper.items.SingleOrDefault(item => item.string_id == id)
                    ?? throw new InvalidDataException("Missing Stage 3 caption owner: " + id))
                .ToArray();
            var serialized = "[\n" + string.Join(",\n", selected.Select(item => JsonUtility.ToJson(item, true))) + "\n]\n";
            File.WriteAllText(ProjectPath(CaptionName), serialized);
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
                    throw new InvalidDataException("Unsupported Stage 3 import profile: " + row.ProfileId);
            }
        }

        private static void ConfigureTexture(string assetPath, SourceRow row, AssetImportPolicy policy)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing Stage 3 texture importer: " + assetPath);
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
            var defaults = importer.GetDefaultPlatformTextureSettings(); defaults.maxTextureSize = policy.MaxTextureSize;
            importer.SetPlatformTextureSettings(defaults);
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Standalone", overridden = true, maxTextureSize = policy.MaxTextureSize,
                format = policy.AlphaIsTransparency ? TextureImporterFormat.DXT5Crunched : TextureImporterFormat.DXT1Crunched,
                textureCompression = TextureImporterCompression.CompressedHQ,
                compressionQuality = 100, crunchedCompression = true,
            });
            foreach (var platform in new[] { "iPhone", "Android" })
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = platform, overridden = true, maxTextureSize = policy.MaxTextureSize,
                    format = policy.AstcBlockSize == 4 ? TextureImporterFormat.ASTC_4x4
                        : policy.AstcBlockSize == 6 ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_8x8,
                    textureCompression = TextureImporterCompression.Compressed,
                    compressionQuality = 100,
                    androidETC2FallbackOverride = AndroidETC2FallbackOverride.Quality32Bit,
                });
            if (row.Name == "ui-caption-backplate-opaque.png") importer.spriteBorder = new Vector4(28f, 28f, 28f, 28f);
            importer.SaveAndReimport();
        }

        private static void ConfigureAudio(string assetPath, AssetImportPolicy policy)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null) throw new InvalidOperationException("Missing Stage 3 audio importer: " + assetPath);
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
                    throw new InvalidDataException("Imported Stage 3 bytes changed: " + row.Name);
        }

        private static void GenerateResource()
        {
            EnsureFolder("Assets/Exloris/Resources/Content");
            var content = AssetDatabase.LoadAssetAtPath<Stage3Content>(ContentAssetPath);
            if (content == null)
            {
                content = ScriptableObject.CreateInstance<Stage3Content>();
                AssetDatabase.CreateAsset(content, ContentAssetPath);
            }
            content.ConfigureApproved(
                Sprites(
                    "bg-14-village-4x3.png", "bg-14-village-16x9.png", "bg-14-village-24x10.png",
                    "bg-15-village-after-4x3.png", "bg-15-village-after-16x9.png", "bg-15-village-after-24x10.png"),
                Sprites("prop-plan-grain.png", "prop-plan-channel.png", "prop-plan-boat.png"),
                Sprites(
                    "out-s3-benefit-grain-layer.png", "out-s3-benefit-channel-layer.png", "out-s3-benefit-boat-layer.png",
                    "out-s3-need-water-route-layer.png", "out-s3-need-grain-storage-layer.png", "out-s3-need-field-water-layer.png"),
                Sprites("chr-boy-stage-reaction-neutral.png", "chr-girl-stage-reaction-neutral.png"),
                Sprites(
                    "ui-play.png", "ui-help.png", "ui-repeat.png", "ui-pause.png", "ui-home.png", "ui-finish.png",
                    "ui-another-preparation.png", "ui-another-river.png", "ui-watch-again.png", "ui-back.png"),
                Sprites("ve-vo-s3-intro.png", "ve-vo-s3-outcome-grain.png", "ve-vo-s3-outcome-channel.png", "ve-vo-s3-outcome-boat.png"),
                Sprites("ui-caption-backplate-opaque.png", "ui-pressed-overlay.png"),
                Audio("vo-s3-intro-en.wav", "vo-s3-outcome-en.wav"),
                Load<TextAsset>(CaptionName),
                Load<TextAsset>("anim-stage3-outcome.motion.json"),
                Load<AudioClip>("mus-stage3-loop.wav"),
                Load<AudioClip>("amb-village-loop.wav"));
            EditorUtility.SetDirty(content);
        }

        private static Sprite[] Sprites(params string[] names) => names.Select(Load<Sprite>).ToArray();
        private static AudioClip[] Audio(params string[] names) => names.Select(Load<AudioClip>).ToArray();
        private static T Load<T>(string name) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(ImportedRoot + "/" + name);
            if (asset == null) throw new InvalidOperationException("Missing approved Stage 3 asset: " + name);
            return asset;
        }

        private static Vector2 ParsePivot(string pivot)
        {
            var values = pivot.Trim('(', ')').Split(',');
            if (values.Length != 2) throw new InvalidDataException("Invalid frozen pivot: " + pivot);
            return new Vector2(float.Parse(values[0], CultureInfo.InvariantCulture),
                float.Parse(values[1], CultureInfo.InvariantCulture));
        }

        private static string WorkspaceRoot() => Directory.GetParent(ProjectRoot()).FullName;
        private static string ProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string ProjectRootPath() => Path.Combine(ProjectRoot(), ImportedRoot);
        private static string ProjectPath(string name) => Path.Combine(ProjectRootPath(), name);
        private static string Hash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
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
