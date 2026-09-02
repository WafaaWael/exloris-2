using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Exloris.Runtime.Overview;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor
{
    public static class JourneyOverviewContentGenerator
    {
        public const string ContentAssetPath = "Assets/Exloris/Resources/Content/JourneyOverviewContent.asset";
        private const string ImportedRoot = "Assets/Exloris/Content/Imported/Overview";

        [MenuItem("Exloris/Journey Overview/Import and Generate Approved Content")]
        public static void ImportAndGenerate()
        {
            EnsureFolder(ImportedRoot);
            ValidateSourceAssets();
            CopyFrozenSources();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidateFileSet(Directory.GetFiles(ImportedRoot)
                .Where(path => !path.EndsWith(".meta", StringComparison.Ordinal))
                .Select(Path.GetFileName));
            ConfigureImporters();
            ValidateImportedAssets();
            GenerateResource();
            Debug.Log("Imported and generated approved Journey Overview content.");
        }

        public static void ValidateFileSet(IEnumerable<string> names)
        {
            var actual = names.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            var expected = RequiredRows().Select(row => row.Name)
                .OrderBy(name => name, StringComparer.Ordinal).ToArray();
            if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
                throw new InvalidOperationException("Journey Overview import folder must contain exactly the approved 33 files.");
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
            var expectedColorType = name.StartsWith("bg-", StringComparison.Ordinal) ? 2 : 6;
            var distinctFloor = name.StartsWith("bg-", StringComparison.Ordinal) ? 256
                : name.StartsWith("timeline-", StringComparison.Ordinal) ? 16 : 8;
            var expectsAlpha = !name.StartsWith("bg-", StringComparison.Ordinal);
            if (width != expected.x || height != expected.y
                || bitDepth != 8 || colorType != expectedColorType
                || totalPixels != width * height || visiblePixels <= 0
                || (expectsAlpha && visiblePixels >= totalPixels)
                || (!expectsAlpha && visiblePixels != totalPixels)
                || hiddenRgbPixels != 0 || distinctColors < distinctFloor || hasBakedText)
            {
                throw new InvalidOperationException("Journey Overview PNG contract failed: " + name);
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
                throw new InvalidOperationException("Journey Overview WAV contract failed: " + name);
            }
        }

        public static void ValidateCaptionContract(
            string stringId,
            string audioFile,
            int durationMs,
            string[] cueIds,
            int[] starts,
            int[] ends,
            string[] texts)
        {
            var expectedCueIds = new[]
            {
                "VO-OVERVIEW-001-C01", "VO-OVERVIEW-001-C02", "VO-OVERVIEW-001-C03"
            };
            var expectedStarts = new[] { 0, 3289, 6182 };
            var expectedEnds = new[] { 3139, 6032, 9000 };
            var expectedTexts = new[]
            {
                "First, measure the river.",
                "Next, read the river.",
                "Then, help the village."
            };
            if (stringId != "VO-OVERVIEW-001" || audioFile != "vo-overview-001-en.wav" || durationMs != 9000
                || cueIds == null || starts == null || ends == null || texts == null
                || !cueIds.SequenceEqual(expectedCueIds, StringComparer.Ordinal)
                || !starts.SequenceEqual(expectedStarts)
                || !ends.SequenceEqual(expectedEnds)
                || !texts.SequenceEqual(expectedTexts, StringComparer.Ordinal))
            {
                throw new InvalidOperationException("Journey Overview caption timing contract failed.");
            }
        }

        public static void ValidateWaveFile(string name, string path)
        {
            var wave = ReadWave(path);
            ValidateWaveContract(name, wave.Duration, wave.SampleRate, wave.BitsPerSample, wave.Channels, Hash(path));
        }

        public static void ValidateCaptionFile(string path)
        {
            var item = JsonUtility.FromJson<CaptionItem>(File.ReadAllText(path));
            if (item == null) throw new InvalidOperationException("Overview caption item is missing.");
            ValidateCaptionContract(
                item.string_id,
                item.audio_file,
                item.duration_ms,
                item.cues.Select(cue => cue.cue_id).ToArray(),
                item.cues.Select(cue => cue.start_ms).ToArray(),
                item.cues.Select(cue => cue.end_ms).ToArray(),
                item.cues.Select(cue => cue.text).ToArray());
        }

        public static void ValidateTimelineSource(string name, string svg)
        {
            var requiredIds = new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["timeline-icon-measure.png"] = new[] { "measure-ruler", "river-cue" },
                ["timeline-icon-read-river.png"] = new[] { "observation-eye", "river-observation-cue" },
                ["timeline-icon-help-village.png"] = new[] { "village-house", "supporting-hands" },
                ["timeline-connector-boat.png"] = new[] { "journey-route", "boat-hull" },
                ["timeline-state-visited.png"] = new[] { "state-base", "state-treatment", "corner-cue", "visited-check" },
                ["timeline-state-current.png"] = new[] { "state-base", "state-treatment", "corner-cue", "static-current-ring", "current-locator" },
                ["timeline-state-next.png"] = new[] { "state-base", "state-treatment", "corner-cue", "next-chevron" },
                ["timeline-state-locked.png"] = new[] { "state-base", "state-treatment", "corner-cue", "growing-bud" },
            };
            if (!requiredIds.TryGetValue(name, out var ids)
                || !svg.Contains("viewBox=\"0 0 1024 1024\"")
                || ids.Any(id => !svg.Contains("id=\"" + id + "\""))
                || ForbiddenTimelineTokens().Any(token => svg.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                || (name == "timeline-state-current.png" && svg.IndexOf("#176B73", StringComparison.OrdinalIgnoreCase) < 0))
            {
                throw new InvalidOperationException("Journey Overview vector source contract failed: " + name);
            }
        }

        public static void ValidateTimelineSemantic(
            string name,
            bool hasShapeCue,
            bool hasPictogramOrTreatment,
            bool isStaticCurrentRing,
            bool hasForbiddenRefusalMark,
            bool hasBakedText,
            bool hasRaycastOwnership,
            string semanticOwner)
        {
            var approved = new[]
            {
                "timeline-state-visited.png", "timeline-state-current.png",
                "timeline-state-next.png", "timeline-state-locked.png"
            };
            if (!approved.Contains(name, StringComparer.Ordinal)
                || !hasShapeCue || !hasPictogramOrTreatment
                || (name == "timeline-state-current.png" && !isStaticCurrentRing)
                || (name != "timeline-state-current.png" && isStaticCurrentRing)
                || hasForbiddenRefusalMark || hasBakedText || hasRaycastOwnership
                || !string.IsNullOrEmpty(semanticOwner))
            {
                throw new InvalidOperationException("Journey Overview timeline semantic contract failed: " + name);
            }
        }

        private static void CopyFrozenSources()
        {
            var workspace = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            foreach (var row in RequiredRows())
            {
                var source = Path.Combine(workspace, row.Source);
                if (!File.Exists(source)) throw new InvalidOperationException("Missing Journey Overview source: " + row.Source);
                var hash = Hash(source);
                if (!string.Equals(hash, row.Hash, StringComparison.Ordinal))
                    throw new InvalidOperationException("Journey Overview source hash changed: " + row.Source);
                File.Copy(source, Path.Combine(ImportedRoot, row.Name), true);
            }
        }

        private static void ValidateSourceAssets()
        {
            var workspace = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            foreach (var row in RequiredRows())
            {
                var path = Path.Combine(workspace, row.Source);
                if (!File.Exists(path)) throw new InvalidOperationException("Missing Journey Overview source: " + row.Source);
                if (Hash(path) != row.Hash) throw new InvalidOperationException("Journey Overview source hash changed: " + row.Source);
                if (row.Name.EndsWith(".png", StringComparison.Ordinal)) ValidateImportedPng(row.Name, path);
                if (row.Name.EndsWith(".wav", StringComparison.Ordinal)) ValidateWaveFile(row.Name, path);
            }
            ValidateCaptionFile(Path.Combine(workspace, "assets/journey-overview/source/vo-overview-001-caption.json"));
            ValidateTimelineSources();
        }

        private static void ConfigureImporters()
        {
            foreach (var name in RequiredRows().Select(row => row.Name).Where(name => name.EndsWith(".png", StringComparison.Ordinal)))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(PathFor(name));
                if (importer == null) throw new InvalidOperationException("Missing Journey Overview texture: " + name);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
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
                platform.maxTextureSize = name.StartsWith("bg-", StringComparison.Ordinal) ? 4096
                    : name == "ui-caption-backplate-opaque.png" || name.StartsWith("ve-", StringComparison.Ordinal) ? 1024 : 512;
                importer.SetPlatformTextureSettings(platform);
                ConfigurePlatformTexture(importer, "iPhone", name);
                ConfigurePlatformTexture(importer, "Android", name);
                if (name == "ui-caption-backplate-opaque.png") importer.spriteBorder = new Vector4(28f, 28f, 28f, 28f);
                importer.SaveAndReimport();
            }

            foreach (var name in RequiredRows().Select(row => row.Name).Where(name => name.EndsWith(".wav", StringComparison.Ordinal)))
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(PathFor(name));
                if (importer == null) throw new InvalidOperationException("Missing Journey Overview audio: " + name);
                var voice = name.StartsWith("vo-", StringComparison.Ordinal);
                importer.forceToMono = voice;
                importer.loadInBackground = !voice;
                var settings = importer.defaultSampleSettings;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                settings.loadType = voice ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.Streaming;
                settings.preloadAudioData = voice;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigurePlatformTexture(TextureImporter importer, string platformName, string name)
        {
            var maxSize = name.StartsWith("bg-", StringComparison.Ordinal) ? 4096
                : name == "ui-caption-backplate-opaque.png" || name.StartsWith("ve-", StringComparison.Ordinal) ? 1024 : 512;
            var blockSize = name.StartsWith("bg-", StringComparison.Ordinal) || name.StartsWith("ve-", StringComparison.Ordinal) ? 6 : 4;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = platformName,
                overridden = true,
                maxTextureSize = maxSize,
                format = blockSize == 4 ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6,
                textureCompression = TextureImporterCompression.Compressed,
                compressionQuality = 100,
                androidETC2FallbackOverride = AndroidETC2FallbackOverride.Quality32Bit
            });
        }

        private static string[] ForbiddenTimelineTokens()
        {
            return new[]
            {
                "<text", "foreignObject", "onload=", "onclick=", "onpointer", "hub",
                "prohibition", "refusal", "error", "cross-mark", "stop-sign", "padlock"
            };
        }

        private static void ValidateImportedAssets()
        {
            foreach (var row in RequiredRows())
            {
                var path = PathFor(row.Name);
                if (Hash(path) != row.Hash) throw new InvalidOperationException("Imported byte hash changed: " + row.Name);
                if (row.Name.EndsWith(".png", StringComparison.Ordinal)) ValidateImportedPng(row.Name, path);
                if (row.Name.EndsWith(".wav", StringComparison.Ordinal)) ValidateImportedWave(row.Name, path);
            }
            ValidateImportedCaption(PathFor("vo-overview-001-caption.json"));
            ValidateTimelineSources();
        }

        private static void ValidateImportedPng(string name, string path)
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
                pixels.Select(pixel => (pixel.r, pixel.g, pixel.b, pixel.a)).Distinct().Count(),
                false);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void ValidateImportedWave(string name, string path)
        {
            ValidateWaveFile(name, path);
        }

        private static void ValidateImportedCaption(string path)
        {
            ValidateCaptionFile(path);
        }

        private static void ValidateTimelineSources()
        {
            var workspace = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            foreach (var name in RequiredRows().Select(row => row.Name).Where(name => name.StartsWith("timeline-", StringComparison.Ordinal)))
            {
                var path = Path.Combine(workspace, "assets/journey-overview/source", Path.ChangeExtension(name, ".svg"));
                if (!File.Exists(path)) throw new InvalidOperationException("Missing Journey Overview vector source: " + name);
                if (Hash(path) != ExpectedTimelineSourceHashes()[name])
                    throw new InvalidOperationException("Journey Overview vector source hash changed: " + name);
                var svg = File.ReadAllText(path);
                ValidateTimelineSource(name, svg);
            }
        }

        private static void GenerateResource()
        {
            EnsureFolder("Assets/Exloris/Resources/Content");
            var content = AssetDatabase.LoadAssetAtPath<JourneyOverviewContent>(ContentAssetPath);
            if (content == null)
            {
                content = ScriptableObject.CreateInstance<JourneyOverviewContent>();
                AssetDatabase.CreateAsset(content, ContentAssetPath);
            }
            content.ConfigureApproved(
                LoadSprites("bg-journey-overview-4x3.png", "bg-journey-overview-16x9.png", "bg-journey-overview-24x10.png"),
                LoadSprites("timeline-icon-measure.png", "timeline-icon-read-river.png", "timeline-icon-help-village.png"),
                Load<Sprite>("timeline-connector-boat.png"),
                LoadSprites("timeline-state-visited.png", "timeline-state-current.png", "timeline-state-next.png", "timeline-state-locked.png"),
                LoadSprites("ui-skip.png", "ui-repeat.png", "ui-pause.png", "ui-home.png"),
                LoadSprites("ui-help.png", "ui-resume.png", "ui-sound-on.png", "ui-sound-off.png", "ui-captions.png", "ui-captions-off.png", "ui-reduced-motion.png", "ui-reduced-motion-off.png"),
                LoadSprites("ui-stay.png", "ui-back.png", "ui-leave.png"),
                LoadSprites("ui-caption-backplate-opaque.png", "ui-pressed-overlay.png", "ve-vo-overview-001.png"),
                Load<AudioClip>("vo-overview-001-en.wav"),
                Load<TextAsset>("vo-overview-001-caption.json"),
                Load<AudioClip>("mus-main-journey-loop.wav"),
                Load<AudioClip>("amb-nile-day-loop.wav"));
            EditorUtility.SetDirty(content);
            AssetDatabase.SaveAssets();
        }

        private static Sprite[] LoadSprites(params string[] names) => names.Select(Load<Sprite>).ToArray();

        private static T Load<T>(string name) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(PathFor(name));
            if (asset == null) throw new InvalidOperationException("Missing approved Journey Overview asset: " + name);
            return asset;
        }

        private static Vector2Int ExpectedImage(string name)
        {
            if (name == "bg-journey-overview-4x3.png") return new Vector2Int(2048, 1536);
            if (name == "bg-journey-overview-16x9.png") return new Vector2Int(2560, 1440);
            if (name == "bg-journey-overview-24x10.png") return new Vector2Int(3456, 1440);
            if (name == "ui-caption-backplate-opaque.png") return new Vector2Int(1024, 1024);
            if (name == "ve-vo-overview-001.png") return new Vector2Int(1024, 512);
            if (RequiredRows().Any(row => row.Name == name && name.EndsWith(".png", StringComparison.Ordinal)))
                return new Vector2Int(512, 512);
            throw new InvalidOperationException("Unapproved Journey Overview image: " + name);
        }

        private static (double Duration, int Channels) ExpectedWave(string name)
        {
            if (name == "vo-overview-001-en.wav") return (9.000250d, 1);
            if (name == "mus-main-journey-loop.wav") return (48.000000d, 2);
            if (name == "amb-nile-day-loop.wav") return (60.000000d, 2);
            throw new InvalidOperationException("Unapproved Journey Overview audio: " + name);
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
                        if (!pcm) throw new InvalidOperationException("Journey Overview WAV must be PCM: " + path);
                    }
                    else if (id == "data") dataBytes = size;
                    stream.Position = Math.Min(next, stream.Length);
                }
                if (channels <= 0 || sampleRate <= 0 || bits <= 0 || dataBytes <= 0)
                    throw new InvalidOperationException("Incomplete Journey Overview WAV: " + path);
                return new WaveFacts
                {
                    Channels = channels,
                    SampleRate = sampleRate,
                    BitsPerSample = bits,
                    Duration = dataBytes / (double)(sampleRate * channels * (bits / 8))
                };
            }
        }

        private static IEnumerable<SourceRow> RequiredRows()
        {
            var hashes = ExpectedHashes();
            yield return Row("assets/step-06-environments/runtime/bg-journey-overview-4x3.png", "bg-journey-overview-4x3.png", hashes);
            yield return Row("assets/step-06-environments/runtime/bg-journey-overview-16x9.png", "bg-journey-overview-16x9.png", hashes);
            yield return Row("assets/step-06-environments/runtime/bg-journey-overview-24x10.png", "bg-journey-overview-24x10.png", hashes);
            foreach (var name in new[]
            {
                "ui-skip.png", "ui-repeat.png", "ui-pause.png", "ui-home.png",
                "ui-help.png", "ui-resume.png", "ui-sound-on.png", "ui-sound-off.png",
                "ui-captions.png", "ui-captions-off.png", "ui-reduced-motion.png", "ui-reduced-motion-off.png",
                "ui-stay.png", "ui-back.png", "ui-leave.png", "ui-caption-backplate-opaque.png", "ui-pressed-overlay.png"
            }) yield return Row("assets/step-04-ui/runtime/" + name, name, hashes);
            foreach (var name in new[]
            {
                "timeline-icon-measure.png", "timeline-icon-read-river.png", "timeline-icon-help-village.png",
                "timeline-connector-boat.png", "timeline-state-visited.png", "timeline-state-current.png",
                "timeline-state-next.png", "timeline-state-locked.png"
            }) yield return Row("assets/journey-overview/runtime/" + name, name, hashes);
            yield return Row("production/step-10-voice-captions/voice/en/vo-overview-001-en.wav", "vo-overview-001-en.wav", hashes);
            yield return Row("production/step-10-voice-captions/visual-equivalents/ve-vo-overview-001.png", "ve-vo-overview-001.png", hashes);
            yield return Row("assets/journey-overview/source/vo-overview-001-caption.json", "vo-overview-001-caption.json", hashes);
            yield return Row("production/step-11-audio/music/mus-main-journey-loop.wav", "mus-main-journey-loop.wav", hashes);
            yield return Row("production/step-11-audio/ambience/amb-nile-day-loop.wav", "amb-nile-day-loop.wav", hashes);
        }

        private static SourceRow Row(string source, string name, Dictionary<string, string> hashes)
        {
            return new SourceRow { Source = source, Name = name, Hash = hashes[name] };
        }

        private static Dictionary<string, string> ExpectedHashes()
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bg-journey-overview-4x3.png"] = "87e797e94ea8a118b37ba85d539c6ce09f3b43da89acc134558f63ef796c06c3",
                ["bg-journey-overview-16x9.png"] = "49b0b11f7b04de4f130f3dc94ea9879b6c7027f96b28111166e9d4c51bbec373",
                ["bg-journey-overview-24x10.png"] = "10765afb4450d2f6cdfccf86eec7f787c30e43e045bf740772a90b05da0f643f",
                ["ui-skip.png"] = "7e74eae88eae2d8b0b8014fbf403d35258910a23e96b26c84953037ac7801491",
                ["ui-repeat.png"] = "5f42294abbb697f39031321d58fd102550989cf7f063d7f9e6278335e5c1e04f",
                ["ui-pause.png"] = "b91cb79e400f7342e18afa316b43978c75a871ce1e6583ec39f9d0348ecd5027",
                ["ui-home.png"] = "8616a22329052c902a8d39699fc1f967e4288374f40b456f58638e1b60109892",
                ["ui-help.png"] = "afdcc054c5db7ccf383305f500d69a9f0ade0bad64a7df594ae25cd3aef490a4",
                ["ui-resume.png"] = "b37f4e683a2b5bb1101f9ca14fb0418a39def46a42528886a1c6b5fa8ce26563",
                ["ui-sound-on.png"] = "a4d0c0d9d347c0d1a39344d5f366f5b044d40e748799d417db80a70e3f6637cf",
                ["ui-sound-off.png"] = "07577dbf1c4b1b519406e75a2a584084fd073d86d4516bf058b727d3bc5d635f",
                ["ui-captions.png"] = "556a93096bf461599acaaa384a98a88d1aa5acd9138d3fae031a012cb5aa5591",
                ["ui-captions-off.png"] = "406ffe164d2065d285e1a53f0fe0256af38d05b1c759023ea5cc7c1d8eb78e9f",
                ["ui-reduced-motion.png"] = "f587dd87c17b15665cc19c5490c21df76fc12bd6cc264242d2e43ed33d87f268",
                ["ui-reduced-motion-off.png"] = "9e7c6c09b13c8606f699f26c7885344722c2e180fe00c95d681ce518419cb48c",
                ["ui-stay.png"] = "7796c74a745e9d4475ea8747a9f81c296de7a72fe1a4b262df1fdac24ce9026b",
                ["ui-back.png"] = "ce7d9212c18ed0b72501f5b867483d4afcb201f88536455c7a897454e115eac7",
                ["ui-leave.png"] = "727e9a3ee17d07e9d2ed557bf2d8ed6ec767b18067725257d6c6ed3711e06193",
                ["ui-caption-backplate-opaque.png"] = "78ced1460341850756eafdb5be39d6c49857cfe1597e006ff799a36d25d4a0fe",
                ["ui-pressed-overlay.png"] = "e135e35f98fc0a6816f195194bf97ced62be2f79cf60a868c7dab07f545b7666",
                ["timeline-icon-measure.png"] = "3fc16fbf620c670bbde82fa6ba6413bf2304fbfdc06e41e8eb3a58e4ac254b75",
                ["timeline-icon-read-river.png"] = "4206f8355bd04a401b22c83b9977f2c5c88419289456108034b18cfb27ef7e5f",
                ["timeline-icon-help-village.png"] = "8a8412639598964e4dafed5e80ef6700fbeaac7d48e137f256c1166c5f62acba",
                ["timeline-connector-boat.png"] = "e5cba468442e7980934188f371ad417f8bc8839b8f9c287a24e768690d359c1c",
                ["timeline-state-visited.png"] = "982da633a216ee8709406e38072576753b7168bbaa37f0ad9604b6a4f3969389",
                ["timeline-state-current.png"] = "73ef999cdb1499c54696142ef29ae5b7bda7fa834221e93376fd69588c9fd40c",
                ["timeline-state-next.png"] = "f6fc253e530c87f4246d8e08f9cc6d7c13b254591c25b985e4f39f200d958f7a",
                ["timeline-state-locked.png"] = "5f0d23581a6b2ba7f32e1880db8f2ec66e6cf257a8549a948d6580a8ff47cb3e",
                ["vo-overview-001-en.wav"] = "cc5a943b1017a6191668c421609afcc9523212a22cb8747d5ee6193b72b3a553",
                ["ve-vo-overview-001.png"] = "6dad6c985fcdf5b88713cfa8880e61b3813debfdeddfcf3fb5fcd2aded999d09",
                ["vo-overview-001-caption.json"] = "25811b9b738e87855f0b51c5102e63a478cfa18d2bd5e776449a104966be4c82",
                ["mus-main-journey-loop.wav"] = "a6612a0cfce3928e7ea01652bb397218f49f524dcc77994d9293736b39806e4e",
                ["amb-nile-day-loop.wav"] = "0c8f92c72159c89ad711a5c9fb547770d4173e734806c881fe64381264dca6e0"
            };
        }

        private static Dictionary<string, string> ExpectedTimelineSourceHashes()
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["timeline-icon-measure.png"] = "09ebce2dc2500a624bb73b15e57e470d3beda912b9ecd975273785a137457097",
                ["timeline-icon-read-river.png"] = "7c5e23f26ce1f6e026a7ab1ea7887c50b56bd338a23bfe1144ce5e339b337191",
                ["timeline-icon-help-village.png"] = "904b47d3d0beef58b68bf49c8a08bdf7499ab418ece00ea0b60ca3413374a2d0",
                ["timeline-connector-boat.png"] = "640b0181c55a08c6158c0d3eb087819432bbc801b0aaf0fdbf47fdc52c61f00f",
                ["timeline-state-visited.png"] = "f5bb104017bb2ef36800ea07cfab4a03cc908878d7d5d1176120fa1a7a75887b",
                ["timeline-state-current.png"] = "14042019a8f05a0207a84d4effeb751cb96da229465d3ece00e1d08dd15d8b73",
                ["timeline-state-next.png"] = "6def6e7475461f9dbbf3a7b64155e5587732cfcbeb69f9143f0e35fe88a0c635",
                ["timeline-state-locked.png"] = "ebd4a618558d208137b342c952e8be07294fb324fe70eb48589e9b7be27220c4"
            };
        }

        private static string Hash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
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
