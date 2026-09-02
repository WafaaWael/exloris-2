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
    public static class Boat1ContentGenerator
    {
        public const string ContentAssetPath = "Assets/Exloris/Resources/Content/Boat1Content.asset";
        public const string ImportedRoot = "Assets/Exloris/Content/Imported/Boat1";

        [MenuItem("Exloris/Boat 1/Import and Generate Approved Content")]
        public static void ImportAndGenerate()
        {
            EnsureFolder(ImportedRoot);
            ValidateSourceAssets();
            CopyFrozenSources();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateFileSet(Directory.GetFiles(ProjectPathForRoot())
                .Where(path => !path.EndsWith(".meta", StringComparison.Ordinal))
                .Select(Path.GetFileName));
            ConfigureImporters();
            ValidateImportedAssets();
            GenerateResource();
            Debug.Log("Imported and generated approved Boat 1 content.");
        }

        public static void ValidateFileSet(IEnumerable<string> names)
        {
            var actual = names.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            var expected = RequiredRows().Select(row => row.Name)
                .OrderBy(name => name, StringComparer.Ordinal).ToArray();
            if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
                throw new InvalidOperationException("Boat 1 import folder must contain exactly the approved 50 files.");
        }

        public static void ValidatePngContract(
            string name,
            int width,
            int height,
            int bitDepth,
            int colorType,
            int visiblePixels,
            int totalPixels,
            int hiddenRgbPixels,
            int distinctColors,
            bool hasBakedText)
        {
            var expected = ExpectedImage(name);
            var isBackground = name.StartsWith("bg-", StringComparison.Ordinal);
            var expectedColorType = isBackground ? 2 : 6;
            var distinctFloor = isBackground ? 256 : 8;
            if (width != expected.x || height != expected.y
                || bitDepth != 8 || colorType != expectedColorType
                || totalPixels != width * height || visiblePixels <= 0
                || (isBackground && visiblePixels != totalPixels)
                || (!isBackground && visiblePixels >= totalPixels)
                || hiddenRgbPixels != 0 || distinctColors < distinctFloor || hasBakedText)
            {
                throw new InvalidOperationException("Boat 1 PNG contract failed: " + name);
            }
        }

        public static void ValidateWaveContract(
            string name, double durationSeconds, int sampleRate, int bitsPerSample, int channels, string hash)
        {
            var expected = ExpectedWave(name);
            if (Math.Abs(durationSeconds - expected.Duration) > 0.001d
                || sampleRate != 48000 || bitsPerSample != 24 || channels != expected.Channels
                || !string.Equals(hash, ExpectedHashes()[name], StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Boat 1 WAV contract failed: " + name);
            }
        }

        public static void ValidateCaptionFile(string path)
        {
            var json = File.ReadAllText(path);
            var collection = JsonUtility.FromJson<CaptionCollection>("{\"items\":" + json + "}");
            if (collection == null || collection.items == null || collection.items.Length != 2)
                throw new InvalidOperationException("Boat 1 captions must contain exactly two items.");
            ValidateCaptionItem(
                collection.items[0],
                "VO-BOAT-CHOICE",
                "vo-boat-choice-en.wav",
                7078,
                new[] { "VO-BOAT-CHOICE-C01", "VO-BOAT-CHOICE-C02", "VO-BOAT-CHOICE-C03" },
                new[] { 0, 2561, 4917 },
                new[] { 2411, 4767, 7078 },
                new[] { "Choose a landmark,", "follow the bird,", "or tap Skip." });
            ValidateCaptionItem(
                collection.items[1],
                "VO-BOAT-HELP",
                "vo-boat-help-en.wav",
                6635,
                new[] { "VO-BOAT-HELP-C01", "VO-BOAT-HELP-C02" },
                new[] { 0, 4375 },
                new[] { 4225, 6635 },
                new[] { "Watch the bird and the direction sign.", "Now you choose." });
        }

        public static void ValidateWaveFile(string name, string path)
        {
            var wave = ReadWave(path);
            ValidateWaveContract(name, wave.Duration, wave.SampleRate, wave.BitsPerSample, wave.Channels, Hash(path));
        }

        private static void ValidateSourceAssets()
        {
            foreach (var row in RequiredRows())
            {
                var path = Path.Combine(WorkspaceRoot(), row.Source);
                if (!File.Exists(path)) throw new InvalidOperationException("Missing Boat 1 source: " + row.Source);
                if (Hash(path) != row.Hash) throw new InvalidOperationException("Boat 1 source hash changed: " + row.Source);
                if (row.Name.EndsWith(".png", StringComparison.Ordinal)) ValidatePngFile(row.Name, path);
                if (row.Name.EndsWith(".wav", StringComparison.Ordinal)) ValidateWaveFile(row.Name, path);
            }
            ValidateCaptionFile(Path.Combine(WorkspaceRoot(), "assets/boat-1/source/boat-1-caption-timings.json"));
        }

        private static void CopyFrozenSources()
        {
            foreach (var row in RequiredRows())
            {
                var source = Path.Combine(WorkspaceRoot(), row.Source);
                File.Copy(source, ProjectPathFor(row.Name), true);
            }
        }

        private static void ConfigureImporters()
        {
            foreach (var name in RequiredRows().Select(row => row.Name).Where(IsPng))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(PathFor(name));
                if (importer == null) throw new InvalidOperationException("Missing Boat 1 texture: " + name);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.spritePivot = PivotFor(name);
                var textureSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteMeshType = SpriteMeshType.FullRect;
                textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
                textureSettings.spritePivot = importer.spritePivot;
                importer.SetTextureSettings(textureSettings);
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = !name.StartsWith("bg-", StringComparison.Ordinal);
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                var platform = importer.GetDefaultPlatformTextureSettings();
                platform.maxTextureSize = MaxTextureSize(name);
                importer.SetPlatformTextureSettings(platform);
                ConfigurePlatformTexture(importer, "iPhone", name);
                ConfigurePlatformTexture(importer, "Android", name);
                if (name == "ui-caption-backplate-opaque.png") importer.spriteBorder = new Vector4(28f, 28f, 28f, 28f);
                importer.SaveAndReimport();
            }

            foreach (var name in RequiredRows().Select(row => row.Name).Where(IsWave))
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(PathFor(name));
                if (importer == null) throw new InvalidOperationException("Missing Boat 1 audio: " + name);
                var voice = name.StartsWith("vo-", StringComparison.Ordinal);
                var streaming = name.StartsWith("mus-", StringComparison.Ordinal) || name.StartsWith("amb-", StringComparison.Ordinal);
                importer.forceToMono = voice;
                importer.loadInBackground = streaming;
                var settings = importer.defaultSampleSettings;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = voice ? 0.75f : 0.7f;
                settings.loadType = streaming ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
                settings.preloadAudioData = !streaming;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigurePlatformTexture(TextureImporter importer, string platformName, string name)
        {
            var useSixBySix = name.StartsWith("bg-", StringComparison.Ordinal) || name.StartsWith("ve-", StringComparison.Ordinal);
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = platformName,
                overridden = true,
                maxTextureSize = MaxTextureSize(name),
                format = useSixBySix ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4,
                textureCompression = TextureImporterCompression.Compressed,
                compressionQuality = 100,
                androidETC2FallbackOverride = AndroidETC2FallbackOverride.Quality32Bit
            });
        }

        private static void ValidateImportedAssets()
        {
            foreach (var row in RequiredRows())
            {
                var path = ProjectPathFor(row.Name);
                if (Hash(path) != row.Hash) throw new InvalidOperationException("Imported Boat 1 byte hash changed: " + row.Name);
                if (IsPng(row.Name)) ValidatePngFile(row.Name, path);
                if (IsWave(row.Name)) ValidateWaveFile(row.Name, path);
            }
            ValidateCaptionFile(ProjectPathFor("boat-1-caption-timings.json"));
        }

        private static void ValidatePngFile(string name, string path)
        {
            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, bytes)) throw new InvalidOperationException("Cannot decode " + name);
            var pixels = texture.GetPixels32();
            ValidatePngContract(
                name,
                texture.width,
                texture.height,
                bytes[24],
                bytes[25],
                pixels.Count(pixel => pixel.a > 0),
                pixels.Length,
                pixels.Count(pixel => pixel.a == 0 && (pixel.r != 0 || pixel.g != 0 || pixel.b != 0)),
                CountDistinctColors(pixels),
                false);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static int CountDistinctColors(IEnumerable<Color32> pixels)
        {
            var colors = new HashSet<uint>();
            foreach (var pixel in pixels)
            {
                colors.Add((uint)(pixel.r | pixel.g << 8 | pixel.b << 16 | pixel.a << 24));
                if (colors.Count >= 256) break;
            }
            return colors.Count;
        }

        private static void GenerateResource()
        {
            EnsureFolder("Assets/Exloris/Resources/Content");
            var content = AssetDatabase.LoadAssetAtPath<Boat1Content>(ContentAssetPath);
            if (content == null)
            {
                content = ScriptableObject.CreateInstance<Boat1Content>();
                AssetDatabase.CreateAsset(content, ContentAssetPath);
            }
            content.ConfigureApproved(
                LoadSprites(
                    "bg-05-boat-reeds-4x3.png", "bg-05-boat-reeds-16x9.png", "bg-05-boat-reeds-24x10.png",
                    "bg-06-boat-village-4x3.png", "bg-06-boat-village-16x9.png", "bg-06-boat-village-24x10.png",
                    "bg-08-boat-arrival-4x3.png", "bg-08-boat-arrival-16x9.png", "bg-08-boat-arrival-24x10.png"),
                LoadSprites("boat-hull.png", "boat-rail-near.png", "boat-steering-oar.png", "boat-wake.png", "boat-static-reduced.png"),
                LoadSprites("chr-boy-seated.png", "chr-girl-seated.png"),
                LoadSprites("chr-boatkeeper-steer-01.png", "chr-boatkeeper-steer-02.png", "chr-boatkeeper-idle.png", "chr-boatkeeper-point-safe-direction.png"),
                LoadSprites(
                    "ibis-idle.png", "ibis-wings-open.png", "ibis-head-lift.png", "ibis-beak-left.png", "ibis-beak-right.png",
                    "ibis-calm-settle.png", "ibis-help.png", "ibis-static-left-reduced.png", "ibis-static-right-reduced.png"),
                LoadSprites(
                    "boat-1-left-reeds.png", "boat-1-right-village.png", "boat-direction-left.png", "boat-direction-right.png", "boat-follow-ibis.png",
                    "ui-help.png", "ui-repeat.png", "ui-skip.png", "ui-pause.png", "ui-home.png"),
                LoadSprites("ui-caption-backplate-opaque.png", "ui-pressed-overlay.png", "ve-vo-boat-choice.png", "ve-vo-boat-help.png"),
                LoadAudio("vo-boat-choice-en.wav", "vo-boat-help-en.wav"),
                Load<TextAsset>("boat-1-caption-timings.json"),
                Load<AudioClip>("mus-main-journey-loop.wav"),
                Load<AudioClip>("amb-boat-water-loop.wav"),
                Load<AudioClip>("sfx-boat-arrival.wav"),
                Load<AudioClip>("sfx-ibis-cue.wav"));
            EditorUtility.SetDirty(content);
            AssetDatabase.SaveAssets();
        }

        private static Sprite[] LoadSprites(params string[] names) => names.Select(Load<Sprite>).ToArray();
        private static AudioClip[] LoadAudio(params string[] names) => names.Select(Load<AudioClip>).ToArray();

        private static T Load<T>(string name) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(PathFor(name));
            if (asset == null) throw new InvalidOperationException("Missing approved Boat 1 asset: " + name);
            return asset;
        }

        private static Vector2 PivotFor(string name)
        {
            if (name.StartsWith("boat-", StringComparison.Ordinal) && name != "boat-follow-ibis.png"
                && !name.StartsWith("boat-1-", StringComparison.Ordinal)
                && !name.StartsWith("boat-direction-", StringComparison.Ordinal)) return new Vector2(0.5f, 0.18f);
            if (name.StartsWith("chr-", StringComparison.Ordinal)) return new Vector2(0.5f, 0.08f);
            return new Vector2(0.5f, 0.5f);
        }

        private static int MaxTextureSize(string name)
        {
            if (name.StartsWith("bg-", StringComparison.Ordinal)) return 4096;
            if (name.StartsWith("boat-", StringComparison.Ordinal) && ExpectedImage(name).x == 2048) return 2048;
            if (ExpectedImage(name).x >= 1024 || ExpectedImage(name).y >= 1024) return 1024;
            return 512;
        }

        private static Vector2Int ExpectedImage(string name)
        {
            if (name.StartsWith("bg-", StringComparison.Ordinal))
            {
                if (name.EndsWith("-4x3.png", StringComparison.Ordinal)) return new Vector2Int(2048, 1536);
                if (name.EndsWith("-16x9.png", StringComparison.Ordinal)) return new Vector2Int(2560, 1440);
                if (name.EndsWith("-24x10.png", StringComparison.Ordinal)) return new Vector2Int(3456, 1440);
            }
            if (name.StartsWith("boat-", StringComparison.Ordinal)
                && new[] { "boat-hull.png", "boat-rail-near.png", "boat-steering-oar.png", "boat-wake.png", "boat-static-reduced.png" }.Contains(name))
                return new Vector2Int(2048, 1024);
            if (name == "ve-vo-boat-choice.png" || name == "ve-vo-boat-help.png") return new Vector2Int(1024, 512);
            if (name == "ui-caption-backplate-opaque.png" || name.StartsWith("chr-", StringComparison.Ordinal) || name.StartsWith("ibis-", StringComparison.Ordinal))
                return new Vector2Int(1024, 1024);
            if (RequiredRows().Any(row => row.Name == name && IsPng(name))) return new Vector2Int(512, 512);
            throw new InvalidOperationException("Unapproved Boat 1 image: " + name);
        }

        private static (double Duration, int Channels) ExpectedWave(string name)
        {
            if (name == "vo-boat-choice-en.wav") return (7.077875d, 1);
            if (name == "vo-boat-help-en.wav") return (6.634542d, 1);
            if (name == "mus-main-journey-loop.wav") return (48d, 2);
            if (name == "amb-boat-water-loop.wav") return (60d, 2);
            if (name == "sfx-boat-arrival.wav") return (0.7d, 1);
            if (name == "sfx-ibis-cue.wav") return (0.9d, 1);
            throw new InvalidOperationException("Unapproved Boat 1 audio: " + name);
        }

        private static WaveFacts ReadWave(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream))
            {
                if (new string(reader.ReadChars(4)) != "RIFF") throw new InvalidOperationException("Invalid RIFF WAV: " + path);
                reader.ReadInt32();
                if (new string(reader.ReadChars(4)) != "WAVE") throw new InvalidOperationException("Invalid WAVE header: " + path);
                int channels = 0, sampleRate = 0, bits = 0, dataBytes = 0;
                while (stream.Position + 8 <= stream.Length)
                {
                    var id = new string(reader.ReadChars(4));
                    var size = reader.ReadInt32();
                    var next = stream.Position + size + (size & 1);
                    if (id == "fmt ")
                    {
                        var format = reader.ReadInt16();
                        channels = reader.ReadInt16();
                        sampleRate = reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadInt16();
                        bits = reader.ReadInt16();
                        var pcm = format == 1;
                        if (format == unchecked((short)0xfffe) && size >= 40)
                        {
                            reader.ReadInt16();
                            reader.ReadInt16();
                            reader.ReadInt32();
                            pcm = reader.ReadInt16() == 1;
                        }
                        if (!pcm) throw new InvalidOperationException("Boat 1 WAV must be PCM: " + path);
                    }
                    else if (id == "data") dataBytes = size;
                    stream.Position = Math.Min(next, stream.Length);
                }
                if (channels <= 0 || sampleRate <= 0 || bits <= 0 || dataBytes <= 0)
                    throw new InvalidOperationException("Incomplete Boat 1 WAV: " + path);
                return new WaveFacts
                {
                    Channels = channels,
                    SampleRate = sampleRate,
                    BitsPerSample = bits,
                    Duration = dataBytes / (double)(sampleRate * channels * (bits / 8))
                };
            }
        }

        private static void ValidateCaptionItem(
            CaptionItem item, string stringId, string audioFile, int durationMs,
            string[] cueIds, int[] starts, int[] ends, string[] texts)
        {
            if (item == null || item.string_id != stringId || item.audio_file != audioFile || item.duration_ms != durationMs
                || item.cues == null
                || !item.cues.Select(cue => cue.cue_id).SequenceEqual(cueIds, StringComparer.Ordinal)
                || !item.cues.Select(cue => cue.start_ms).SequenceEqual(starts)
                || !item.cues.Select(cue => cue.end_ms).SequenceEqual(ends)
                || !item.cues.Select(cue => cue.text).SequenceEqual(texts, StringComparer.Ordinal))
                throw new InvalidOperationException("Boat 1 caption contract failed: " + stringId);
        }

        private static IEnumerable<SourceRow> RequiredRows()
        {
            var hashes = ExpectedHashes();
            foreach (var name in new[]
            {
                "bg-05-boat-reeds-4x3.png", "bg-05-boat-reeds-16x9.png", "bg-05-boat-reeds-24x10.png",
                "bg-06-boat-village-4x3.png", "bg-06-boat-village-16x9.png", "bg-06-boat-village-24x10.png",
                "bg-08-boat-arrival-4x3.png", "bg-08-boat-arrival-16x9.png", "bg-08-boat-arrival-24x10.png"
            }) yield return Row("assets/step-06-environments/runtime/" + name, name, hashes);
            foreach (var name in new[]
            {
                "boat-hull.png", "boat-rail-near.png", "boat-steering-oar.png", "boat-wake.png", "boat-static-reduced.png",
                "chr-boatkeeper-steer-01.png", "chr-boatkeeper-steer-02.png", "chr-boatkeeper-idle.png", "chr-boatkeeper-point-safe-direction.png",
                "ibis-idle.png", "ibis-wings-open.png", "ibis-head-lift.png", "ibis-beak-left.png", "ibis-beak-right.png",
                "ibis-calm-settle.png", "ibis-help.png", "ibis-static-left-reduced.png", "ibis-static-right-reduced.png"
            }) yield return Row("assets/step-07-gameplay-art/runtime/" + name, name, hashes);
            foreach (var name in new[] { "chr-boy-seated.png", "chr-girl-seated.png" })
                yield return Row("assets/step-05-characters/runtime/" + name, name, hashes);
            foreach (var name in new[]
            {
                "boat-1-left-reeds.png", "boat-1-right-village.png", "boat-direction-left.png", "boat-direction-right.png", "boat-follow-ibis.png",
                "ui-help.png", "ui-repeat.png", "ui-skip.png", "ui-pause.png", "ui-home.png",
                "ui-caption-backplate-opaque.png", "ui-pressed-overlay.png"
            }) yield return Row("assets/step-04-ui/runtime/" + name, name, hashes);
            foreach (var name in new[] { "ve-vo-boat-choice.png", "ve-vo-boat-help.png" })
                yield return Row("production/step-10-voice-captions/visual-equivalents/" + name, name, hashes);
            foreach (var name in new[] { "vo-boat-choice-en.wav", "vo-boat-help-en.wav" })
                yield return Row("production/step-10-voice-captions/voice/en/" + name, name, hashes);
            yield return Row("assets/boat-1/source/boat-1-caption-timings.json", "boat-1-caption-timings.json", hashes);
            yield return Row("production/step-11-audio/music/mus-main-journey-loop.wav", "mus-main-journey-loop.wav", hashes);
            yield return Row("production/step-11-audio/ambience/amb-boat-water-loop.wav", "amb-boat-water-loop.wav", hashes);
            yield return Row("production/step-11-audio/sfx/sfx-boat-arrival.wav", "sfx-boat-arrival.wav", hashes);
            yield return Row("production/step-11-audio/sfx/sfx-ibis-cue.wav", "sfx-ibis-cue.wav", hashes);
        }

        private static SourceRow Row(string source, string name, IDictionary<string, string> hashes)
        {
            return new SourceRow { Source = source, Name = name, Hash = hashes[name] };
        }

        private static Dictionary<string, string> ExpectedHashes()
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bg-05-boat-reeds-4x3.png"] = "761f0a4f5c3c68477906acf0c06be6a7d2580f2a42c06cc99f5aee33f34a09ca",
                ["bg-05-boat-reeds-16x9.png"] = "895e3e7086fbf14e0ff22917dc503b24baf2a8c55620aa0bb8daa608269d7355",
                ["bg-05-boat-reeds-24x10.png"] = "09ddbaee829e1d09888caf0618c493723086a2e68dfb6fe5c6e979dcb1677987",
                ["bg-06-boat-village-4x3.png"] = "1e0c0a491470630de076ba2e6ddd5e56819fecbc1a9f2c460bd76a9d8ec15e09",
                ["bg-06-boat-village-16x9.png"] = "e6cbc31d6aba79e97f9201bdf996f77f6526bd89ca71f401e69059bf93cbc9ee",
                ["bg-06-boat-village-24x10.png"] = "fe2311d0bc500837f34b4dce876c9892995a0deb21274eaaf33ac5e9e71091ff",
                ["bg-08-boat-arrival-4x3.png"] = "3fe6e5a9be6223e1b108a1a8241aeeab36ac337e31ea61a7908d0d46645dbed0",
                ["bg-08-boat-arrival-16x9.png"] = "91552ce13eee2fc4716523157155a5d763a2b06f8c625d8a1287a44618df3081",
                ["bg-08-boat-arrival-24x10.png"] = "c97c900abcbaf77f5b5bd3e4e7e8062b9c4ae725c90c5a1b2aba2a3d1abc3ba0",
                ["boat-hull.png"] = "3031dc7949a5846546f169aa157cb5a2499e3e189309f18cceab4e7975a88241",
                ["boat-rail-near.png"] = "978716e039df68ff12d91645e001607298f64e959eab93f9bbb88688807b7f1f",
                ["boat-steering-oar.png"] = "740d0c9da596bafd6e49b084c86ed6e460ca033caf84215e0e61436154d5ecfd",
                ["boat-wake.png"] = "0fb1c3f351e16955250492594bd298df5375f56b2d3a0fe11483404422ac6f12",
                ["boat-static-reduced.png"] = "fea899186e36cbf40f707eb4163b0e8d17e6a0864d5a4b09eab82363d1648906",
                ["chr-boy-seated.png"] = "7d22c32b416589a244b7ba16774498c0c8c3f3e91fdf3eb3b8c911d2ceec1d59",
                ["chr-girl-seated.png"] = "b1f94f3edb2f640788f28d814ee59fbe4888fe78110e96ebfd0597ba85240074",
                ["chr-boatkeeper-steer-01.png"] = "9a8d95a4824a6b991749e234f498baf5b73421ba12b3f5664950cc45c96a6917",
                ["chr-boatkeeper-steer-02.png"] = "6573278831a2475cf7c739e5219b34590045c8ff76b7edddbd578a4fb91e6d0e",
                ["chr-boatkeeper-idle.png"] = "a83d4a6362399561d270c256fdb9fd61dd024ee224660867e81c46dd5bc4938d",
                ["chr-boatkeeper-point-safe-direction.png"] = "f6ead5a54ecaff2d711fd1c88bdae22a6ca4f121573f91495170732d68275e3f",
                ["ibis-idle.png"] = "30266041ca54f8ef7ee698780e26d077f81151862a0f66984b822079688fca9b",
                ["ibis-wings-open.png"] = "9e176edaf43b6f588ba3f60ceff7fd1b87b15016e906606c5bb316a3ecefff59",
                ["ibis-head-lift.png"] = "5f6494f19a6a883fe7b0bf2ba9dcafd36a77d4334475620762105ad0ad1175f8",
                ["ibis-beak-left.png"] = "595effe986223ee8d877654a20f94f9068fab6f280218a2328a5a6a562bbd6f3",
                ["ibis-beak-right.png"] = "6474ed0c0cb7dfda439bfb7f223d2944c529b4f69d2dfc5b84d9cab78dac7f2f",
                ["ibis-calm-settle.png"] = "672509c2084b9b462f3371f8adb49bb0b778061e769b428499da41bd087b3d0f",
                ["ibis-help.png"] = "eb63fe308380c2ed3252e815bcbe6326b6d07c6684e1cb1f9eedd9f6a302bfae",
                ["ibis-static-left-reduced.png"] = "93023901c81bdde8c89c20fd20df818c417c293756e269b64842f3327542baba",
                ["ibis-static-right-reduced.png"] = "dc567ae9b389ec096feb70483bd7c69460718b7d2f251c85672830f29ad005d2",
                ["boat-1-left-reeds.png"] = "5c389292cceca6fc2618ca2d894bf17cec5a27c7413837a183894c4b67b0b1d6",
                ["boat-1-right-village.png"] = "03bb5f0df6596236fe47163aaa49a797299ad13fa181dccb761dc2fd1e5daa3b",
                ["boat-direction-left.png"] = "019c8e8ea1a43235db2d33a45c7f66e9ac1e02d025ce13916af2074856d932f8",
                ["boat-direction-right.png"] = "83864a2414b763e74d5c4177d4c14c7b2ac5b4e50b820065984aa0e2d9700e43",
                ["boat-follow-ibis.png"] = "645612dcee63bbbe13186e0d99882fa9ec3b71c420bc857f9be1a59eedb05f31",
                ["ui-help.png"] = "afdcc054c5db7ccf383305f500d69a9f0ade0bad64a7df594ae25cd3aef490a4",
                ["ui-repeat.png"] = "5f42294abbb697f39031321d58fd102550989cf7f063d7f9e6278335e5c1e04f",
                ["ui-skip.png"] = "7e74eae88eae2d8b0b8014fbf403d35258910a23e96b26c84953037ac7801491",
                ["ui-pause.png"] = "b91cb79e400f7342e18afa316b43978c75a871ce1e6583ec39f9d0348ecd5027",
                ["ui-home.png"] = "8616a22329052c902a8d39699fc1f967e4288374f40b456f58638e1b60109892",
                ["ui-caption-backplate-opaque.png"] = "78ced1460341850756eafdb5be39d6c49857cfe1597e006ff799a36d25d4a0fe",
                ["ui-pressed-overlay.png"] = "e135e35f98fc0a6816f195194bf97ced62be2f79cf60a868c7dab07f545b7666",
                ["ve-vo-boat-choice.png"] = "cc4ba1381836bdee2fb41f2d1fef4fa0598104396173feeb31b8b521b5b8d3eb",
                ["ve-vo-boat-help.png"] = "2af22c1c7cbfc5fa02c8d84e75e21799ada1ffd8264b86b125af62c23d5105c7",
                ["vo-boat-choice-en.wav"] = "78f145e11b787fbab0019183330dff2c2ece8fb67d26e40059cd0b6044f5c3d2",
                ["vo-boat-help-en.wav"] = "a67be07ab3bb697f5c21d81764bfce90877990019a8a85dbdd6cf0bd8de15f6b",
                ["boat-1-caption-timings.json"] = "8160077b5f1bf3f470547affd6813eb4dfec2a26c23cc2c5e53359836fe1f6af",
                ["mus-main-journey-loop.wav"] = "a6612a0cfce3928e7ea01652bb397218f49f524dcc77994d9293736b39806e4e",
                ["amb-boat-water-loop.wav"] = "d9dea3635cfbb9ec52f884ab6e931310b5237122d3bb3c4a6070088f85c69b60",
                ["sfx-boat-arrival.wav"] = "f5d35c0569e2a3b8f67cff3a5be3296d624fc251a8dd05c2e06cbdcb2042f5b2",
                ["sfx-ibis-cue.wav"] = "9a26e04515348aca82b4b1c079c0879c6c48dca17cc897665ecde52e8bb7760f"
            };
        }

        private static bool IsPng(string name) => name.EndsWith(".png", StringComparison.Ordinal);
        private static bool IsWave(string name) => name.EndsWith(".wav", StringComparison.Ordinal);

        private static string WorkspaceRoot()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Directory.GetParent(projectRoot).FullName;
        }

        private static string ProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string PathFor(string name) => ImportedRoot + "/" + name;
        private static string ProjectPathFor(string name) => Path.Combine(ProjectRoot(), PathFor(name));
        private static string ProjectPathForRoot() => Path.Combine(ProjectRoot(), ImportedRoot);

        private static string Hash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }

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

        private sealed class SourceRow
        {
            public string Source;
            public string Name;
            public string Hash;
        }

        private sealed class WaveFacts
        {
            public double Duration;
            public int SampleRate;
            public int BitsPerSample;
            public int Channels;
        }

        [Serializable]
        private sealed class CaptionCollection
        {
            public CaptionItem[] items;
        }

        [Serializable]
        private sealed class CaptionItem
        {
            public string string_id;
            public string audio_file;
            public int duration_ms;
            public CaptionCue[] cues;
        }

        [Serializable]
        private sealed class CaptionCue
        {
            public string cue_id;
            public int start_ms;
            public int end_ms;
            public string text;
        }
    }
}
