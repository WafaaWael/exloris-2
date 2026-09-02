using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Exloris.Runtime.Boat;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor
{
    public static class Boat2ContentGenerator
    {
        public const string ImportedRoot = "Assets/Exloris/Content/Imported/Boat2";
        public const string ContentAssetPath = "Assets/Exloris/Resources/Content/Boat2Content.asset";

        [Serializable] public sealed class AssetLedger { public string status; public AssetRow[] assets; }
        [Serializable] public sealed class AssetRow { public string source_path; public string runtime_name; public long bytes; public string sha256; }

        [MenuItem("Exloris/Boat 2/Import and Generate Approved Content")]
        public static void ImportAndGenerate()
        {
            var ledger = ReadLedger();
            ValidateLedger(ledger);
            EnsureFolder(ImportedRoot);
            foreach (var row in ledger.assets)
                File.Copy(Path.Combine(WorkspaceRoot(), row.source_path), ProjectPath(row.runtime_name), true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImporters(ledger);
            GenerateResource();
            AssetDatabase.SaveAssets();
            Debug.Log("Imported and generated approved Boat 2 content.");
        }

        public static AssetLedger ReadLedger() => JsonUtility.FromJson<AssetLedger>(
            File.ReadAllText(Path.Combine(WorkspaceRoot(), "evidence/boat-2/assets/ASSET-VALIDATION.json")));

        public static void ValidateLedger(AssetLedger ledger)
        {
            if (ledger == null || ledger.status != "PASS" || ledger.assets == null || ledger.assets.Length != 50)
                throw new InvalidOperationException("Boat 2 ledger must contain exactly 50 passing rows.");
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in ledger.assets)
            {
                if (row == null || string.IsNullOrEmpty(row.source_path) || string.IsNullOrEmpty(row.runtime_name)
                    || row.bytes <= 0 || row.sha256 == null || row.sha256.Length != 64
                    || row.source_path.IndexOf("/review/", StringComparison.OrdinalIgnoreCase) >= 0
                    || row.runtime_name.IndexOf("hub", StringComparison.OrdinalIgnoreCase) >= 0
                    || !names.Add(row.runtime_name))
                    throw new InvalidOperationException("Invalid Boat 2 ledger row.");
                var path = Path.Combine(WorkspaceRoot(), row.source_path);
                if (!File.Exists(path) || new FileInfo(path).Length != row.bytes || Hash(path) != row.sha256)
                    throw new InvalidOperationException("Boat 2 source changed: " + row.source_path);
            }
        }

        public static Vector2 PivotFor(string name)
        {
            if (new[] { "boat-hull.png", "boat-rail-near.png", "boat-steering-oar.png", "boat-wake.png", "boat-static-reduced.png" }.Contains(name))
                return new Vector2(0.5f, 0.18f);
            if (name.StartsWith("chr-", StringComparison.Ordinal)) return new Vector2(0.5f, 0.08f);
            return new Vector2(0.5f, 0.5f);
        }

        private static void ConfigureImporters(AssetLedger ledger)
        {
            foreach (var name in ledger.assets.Select(row => row.runtime_name))
            {
                var path = ImportedRoot + "/" + name;
                if (name.EndsWith(".png", StringComparison.Ordinal))
                {
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) throw new InvalidOperationException("Missing Boat 2 texture: " + name);
                    importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 100f; importer.spritePivot = PivotFor(name);
                    var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = importer.spritePivot;
                    importer.SetTextureSettings(settings); importer.sRGBTexture = true;
                    importer.alphaIsTransparency = !name.StartsWith("bg-", StringComparison.Ordinal);
                    importer.mipmapEnabled = false; importer.isReadable = false; importer.npotScale = TextureImporterNPOTScale.None;
                    importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
                    var max = MaxTextureSize(name); var defaults = importer.GetDefaultPlatformTextureSettings(); defaults.maxTextureSize = max; importer.SetPlatformTextureSettings(defaults);
                    foreach (var platform in new[] { "iPhone", "Android" }) importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                    {
                        name = platform, overridden = true, maxTextureSize = max,
                        format = name.StartsWith("bg-", StringComparison.Ordinal) || name.StartsWith("ve-", StringComparison.Ordinal)
                            ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4,
                        textureCompression = TextureImporterCompression.Compressed, compressionQuality = 100,
                        androidETC2FallbackOverride = AndroidETC2FallbackOverride.Quality32Bit
                    });
                    if (name == "ui-caption-backplate-opaque.png") importer.spriteBorder = new Vector4(28f, 28f, 28f, 28f);
                    importer.SaveAndReimport();
                }
                else if (name.EndsWith(".wav", StringComparison.Ordinal))
                {
                    var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                    if (importer == null) throw new InvalidOperationException("Missing Boat 2 audio: " + name);
                    var voice = name.StartsWith("vo-", StringComparison.Ordinal);
                    var streaming = name.StartsWith("mus-", StringComparison.Ordinal) || name.StartsWith("amb-", StringComparison.Ordinal);
                    importer.forceToMono = voice; importer.loadInBackground = streaming;
                    var audio = importer.defaultSampleSettings; audio.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                    audio.compressionFormat = AudioCompressionFormat.Vorbis; audio.quality = voice ? 0.75f : 0.7f;
                    audio.loadType = streaming ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory; audio.preloadAudioData = !streaming;
                    importer.defaultSampleSettings = audio; importer.SaveAndReimport();
                }
            }
        }

        private static void GenerateResource()
        {
            EnsureFolder("Assets/Exloris/Resources/Content");
            var content = AssetDatabase.LoadAssetAtPath<Boat2Content>(ContentAssetPath);
            if (content == null) { content = ScriptableObject.CreateInstance<Boat2Content>(); AssetDatabase.CreateAsset(content, ContentAssetPath); }
            content.ConfigureApproved(
                Sprites("bg-boat-deck-4x3.png", "bg-boat-deck-16x9.png", "bg-boat-deck-24x10.png",
                    "bg-07-boat-approach-4x3.png", "bg-07-boat-approach-16x9.png", "bg-07-boat-approach-24x10.png",
                    "bg-08-boat-arrival-4x3.png", "bg-08-boat-arrival-16x9.png", "bg-08-boat-arrival-24x10.png"),
                Sprites("boat-hull.png", "boat-rail-near.png", "boat-steering-oar.png", "boat-wake.png", "boat-static-reduced.png"),
                Sprites("chr-boy-seated.png", "chr-girl-seated.png"),
                Sprites("chr-boatkeeper-steer-01.png", "chr-boatkeeper-steer-02.png", "chr-boatkeeper-idle.png", "chr-boatkeeper-point-safe-direction.png"),
                Sprites("ibis-idle.png", "ibis-wings-open.png", "ibis-head-lift.png", "ibis-beak-left.png", "ibis-beak-right.png", "ibis-calm-settle.png", "ibis-help.png", "ibis-static-left-reduced.png", "ibis-static-right-reduced.png"),
                Sprites("boat-2-left-field-bank.png", "boat-2-right-dock.png", "boat-direction-left.png", "boat-direction-right.png", "boat-follow-ibis.png", "ui-help.png", "ui-repeat.png", "ui-skip.png", "ui-pause.png", "ui-home.png"),
                Sprites("ui-caption-backplate-opaque.png", "ui-pressed-overlay.png", "ve-vo-boat-choice.png", "ve-vo-boat-help.png"),
                Audio("vo-boat-choice-en.wav", "vo-boat-help-en.wav"), Load<TextAsset>("boat-2-caption-timings.json"),
                Load<AudioClip>("mus-main-journey-loop.wav"), Load<AudioClip>("amb-boat-water-loop.wav"),
                Load<AudioClip>("sfx-boat-arrival.wav"), Load<AudioClip>("sfx-ibis-cue.wav"));
            EditorUtility.SetDirty(content);
        }

        private static Sprite[] Sprites(params string[] names) => names.Select(Load<Sprite>).ToArray();
        private static AudioClip[] Audio(params string[] names) => names.Select(Load<AudioClip>).ToArray();
        private static T Load<T>(string name) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(ImportedRoot + "/" + name);
            if (asset == null) throw new InvalidOperationException("Missing approved Boat 2 asset: " + name);
            return asset;
        }
        private static int MaxTextureSize(string name) => name.StartsWith("bg-", StringComparison.Ordinal) ? 4096
            : new[] { "boat-hull.png", "boat-rail-near.png", "boat-steering-oar.png", "boat-wake.png", "boat-static-reduced.png" }.Contains(name) ? 2048
            : name.StartsWith("chr-", StringComparison.Ordinal) || name.StartsWith("ibis-", StringComparison.Ordinal)
                || name.StartsWith("ve-", StringComparison.Ordinal) || name == "ui-caption-backplate-opaque.png" ? 1024 : 512;
        private static string WorkspaceRoot() => Directory.GetParent(Path.GetFullPath(Path.Combine(Application.dataPath, ".."))).FullName;
        private static string ProjectPath(string name) => Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), ImportedRoot, name);
        private static string Hash(string path) { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        private static void EnsureFolder(string path) { var parts = path.Split('/'); var current = parts[0]; for (var i = 1; i < parts.Length; i++) { var next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; } }
    }
}
