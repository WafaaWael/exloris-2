using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Exloris.Editor;
using Exloris.Editor.AssetImport;
using Exloris.Runtime.Overview;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class JourneyOverviewContentContractTests
    {
        private const string ImportedRoot = "Assets/Exloris/Content/Imported/Overview";

        [Test]
        public void RuntimeFolderContainsExactlyThirtyThreeApprovedFilesWithFrozenBytes()
        {
            var required = RequiredContent().ToArray();
            var actual = Directory.Exists(ImportedRoot)
                ? Directory.GetFiles(ImportedRoot)
                    .Where(path => !path.EndsWith(".meta", StringComparison.Ordinal))
                    .Select(Path.GetFileName)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray()
                : Array.Empty<string>();

            CollectionAssert.AreEqual(
                required.Select(item => Path.GetFileName(item.destination)).OrderBy(name => name, StringComparer.Ordinal),
                actual);
            Assert.DoesNotThrow(() => JourneyOverviewContentGenerator.ValidateFileSet(actual));
            foreach (var item in required)
            {
                Assert.That(File.Exists(item.destination), Is.True, item.destination);
                Assert.That(Hash(item.source), Is.EqualTo(item.expectedHash), item.source);
                Assert.That(Hash(item.destination), Is.EqualTo(item.expectedHash), item.destination);
            }
        }

        [Test]
        public void EveryPngHasExactDecodedProfileVisibleInkAndNoHiddenRgb()
        {
            foreach (var item in RequiredContent().Where(item => item.destination.EndsWith(".png", StringComparison.Ordinal)))
            {
                var name = Path.GetFileName(item.destination);
                var bytes = File.ReadAllBytes(item.destination);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Assert.That(ImageConversion.LoadImage(texture, bytes), Is.True, name);
                var pixels = texture.GetPixels32();
                var visible = pixels.Count(pixel => pixel.a > 0);
                var hiddenRgb = pixels.Count(pixel => pixel.a == 0 && (pixel.r != 0 || pixel.g != 0 || pixel.b != 0));
                var distinct = pixels.Select(pixel => (pixel.r, pixel.g, pixel.b, pixel.a)).Distinct().Count();
                var expected = ExpectedImage(name);
                Assert.DoesNotThrow(() => JourneyOverviewContentGenerator.ValidatePngContract(
                    name,
                    texture.width,
                    texture.height,
                    bytes[24],
                    bytes[25],
                    visible,
                    pixels.Length,
                    hiddenRgb,
                    distinct,
                    false));
                Assert.That(new Vector2Int(texture.width, texture.height), Is.EqualTo(expected));
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void TimelineSourcesAreOriginalTextFreeVectorDocumentsWithExactCoverage()
        {
            var timelineRows = RequiredContent()
                .Where(item => Path.GetFileName(item.destination).StartsWith("timeline-", StringComparison.Ordinal))
                .ToArray();
            Assert.That(timelineRows, Has.Length.EqualTo(8));
            foreach (var row in timelineRows)
            {
                var source = Path.ChangeExtension(row.source.Replace("/runtime/", "/source/"), ".svg");
                Assert.That(File.Exists(source), Is.True, source);
                var svg = File.ReadAllText(source);
                Assert.That(Hash(source), Is.EqualTo(TimelineSourceHashes()[Path.GetFileName(row.destination)]), source);
                Assert.That(svg, Does.Contain("viewBox=\"0 0 1024 1024\""), source);
                Assert.That(svg, Does.Not.Contain("<text"), source);
                Assert.That(svg, Does.Not.Contain("hub").IgnoreCase, source);
                Assert.That(svg, Does.Not.Contain("prohibition").IgnoreCase, source);
                Assert.DoesNotThrow(() => JourneyOverviewContentGenerator.ValidateTimelineSource(
                    Path.GetFileName(row.destination), svg));
            }
        }

        [Test]
        public void EveryImportedTextureUsesTheExactSpriteProfile()
        {
            foreach (var name in ApprovedNames().Where(name => name.EndsWith(".png", StringComparison.Ordinal)))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(ImportedRoot + "/" + name);
                Assert.That(importer, Is.Not.Null, name);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), name);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), name);
                Assert.That(importer.sRGBTexture, Is.True, name);
                Assert.That(importer.alphaIsTransparency, Is.EqualTo(!name.StartsWith("bg-", StringComparison.Ordinal)), name);
                Assert.That(importer.mipmapEnabled, Is.False, name);
                Assert.That(importer.isReadable, Is.False, name);
                Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), name);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), name);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), name);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100f), name);
                Assert.That(importer.spritePivot, Is.EqualTo(new Vector2(0.5f, 0.5f)), name);
                Assert.That(importer.GetDefaultPlatformTextureSettings().maxTextureSize,
                    Is.EqualTo(name.StartsWith("bg-", StringComparison.Ordinal) ? 4096
                        : name == "ui-caption-backplate-opaque.png" || name.StartsWith("ve-", StringComparison.Ordinal) ? 1024 : 512), name);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), name);
                Assert.That(settings.spriteAlignment, Is.EqualTo((int)SpriteAlignment.Custom), name);
                foreach (var platformName in new[] { "iPhone", "Android" })
                {
                    var platform = importer.GetPlatformTextureSettings(platformName);
                    Assert.That(platform.overridden, Is.True, name + " " + platformName);
                    Assert.That(platform.format,
                        Is.EqualTo(name.StartsWith("bg-", StringComparison.Ordinal) || name.StartsWith("ve-", StringComparison.Ordinal)
                            ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4), name + " " + platformName);
                }
            }
            var backplate = (TextureImporter)AssetImporter.GetAtPath(ImportedRoot + "/ui-caption-backplate-opaque.png");
            Assert.That(backplate.spriteBorder, Is.EqualTo(new Vector4(28f, 28f, 28f, 28f)));
            Assert.That(AssetImportGuard.IsManagedAssetPath(ImportedRoot), Is.False);
            Assert.That(AssetImportGuard.IsManagedAssetPath(ImportedRoot + "/timeline-state-current.png"), Is.False);
        }

        [Test]
        public void VoiceMusicAndAmbienceUseExactIndependentProfiles()
        {
            AssertAudio("vo-overview-001-en.wav", AudioClipLoadType.CompressedInMemory, true, true);
            AssertAudio("mus-main-journey-loop.wav", AudioClipLoadType.Streaming, false, false);
            AssertAudio("amb-nile-day-loop.wav", AudioClipLoadType.Streaming, false, false);
            Assert.DoesNotThrow(() => JourneyOverviewContentGenerator.ValidateWaveContract(
                "vo-overview-001-en.wav", 9.000250d, 48000, 24, 1, FrozenHashes()["vo-overview-001-en.wav"]));
            Assert.DoesNotThrow(() => JourneyOverviewContentGenerator.ValidateWaveContract(
                "mus-main-journey-loop.wav", 48.000000d, 48000, 24, 2, FrozenHashes()["mus-main-journey-loop.wav"]));
            Assert.DoesNotThrow(() => JourneyOverviewContentGenerator.ValidateWaveContract(
                "amb-nile-day-loop.wav", 60.000000d, 48000, 24, 2, FrozenHashes()["amb-nile-day-loop.wav"]));
            foreach (var name in new[] { "vo-overview-001-en.wav", "mus-main-journey-loop.wav", "amb-nile-day-loop.wav" })
                Assert.DoesNotThrow(() => JourneyOverviewContentGenerator.ValidateWaveFile(name, ImportedRoot + "/" + name));
        }

        [Test]
        public void CaptionTimingContainsOnlyTheExactOverviewContractAtRuntime()
        {
            Assert.DoesNotThrow(() => JourneyOverviewContentGenerator.ValidateCaptionFile(
                ImportedRoot + "/vo-overview-001-caption.json"));
            Assert.DoesNotThrow(() => JourneyOverviewContentGenerator.ValidateCaptionContract(
                "VO-OVERVIEW-001",
                "vo-overview-001-en.wav",
                9000,
                new[] { "VO-OVERVIEW-001-C01", "VO-OVERVIEW-001-C02", "VO-OVERVIEW-001-C03" },
                new[] { 0, 3289, 6182 },
                new[] { 3139, 6032, 9000 },
                new[]
                {
                    "First, measure the river.",
                    "Next, read the river.",
                    "Then, help the village."
                }));
        }

        [Test]
        public void ApprovedResourceContainsOnlyTheThirtyThreeStageReferences()
        {
            var content = AssetDatabase.LoadAssetAtPath<JourneyOverviewContent>(JourneyOverviewContentGenerator.ContentAssetPath);
            Assert.That(content, Is.Not.Null);
            Assert.That(content.Backgrounds, Has.Length.EqualTo(3));
            Assert.That(content.TimelineIcons, Has.Length.EqualTo(3));
            Assert.That(content.TimelineConnector, Is.Not.Null);
            Assert.That(content.TimelineStates, Has.Length.EqualTo(4));
            Assert.That(content.OverviewControls, Has.Length.EqualTo(4));
            Assert.That(content.PauseControls, Has.Length.EqualTo(8));
            Assert.That(content.LeaveControls, Has.Length.EqualTo(3));
            Assert.That(content.PresentationSprites, Has.Length.EqualTo(3));
            Assert.That(content.Voice, Is.Not.Null);
            Assert.That(content.CaptionTiming, Is.Not.Null);
            Assert.That(content.Music, Is.Not.Null);
            Assert.That(content.Ambience, Is.Not.Null);
            Assert.That(content.AllReferences, Has.Length.EqualTo(33));
            Assert.That(content.AllReferences.Any(item => item == null), Is.False);
            Assert.That(content.AllReferences.Distinct().Count(), Is.EqualTo(33));
            CollectionAssert.AreEqual(new[]
            {
                "bg-journey-overview-4x3.png", "bg-journey-overview-16x9.png", "bg-journey-overview-24x10.png"
            }, content.Backgrounds.Select(item => Path.GetFileName(AssetDatabase.GetAssetPath(item))));
            CollectionAssert.AreEqual(new[]
            {
                "timeline-icon-measure.png", "timeline-icon-read-river.png", "timeline-icon-help-village.png"
            }, content.TimelineIcons.Select(item => Path.GetFileName(AssetDatabase.GetAssetPath(item))));
            Assert.That(Path.GetFileName(AssetDatabase.GetAssetPath(content.TimelineConnector)), Is.EqualTo("timeline-connector-boat.png"));
            CollectionAssert.AreEqual(new[]
            {
                "timeline-state-visited.png", "timeline-state-current.png", "timeline-state-next.png", "timeline-state-locked.png"
            }, content.TimelineStates.Select(item => Path.GetFileName(AssetDatabase.GetAssetPath(item))));
            CollectionAssert.AreEqual(new[] { "ui-skip.png", "ui-repeat.png", "ui-pause.png", "ui-home.png" },
                content.OverviewControls.Select(item => Path.GetFileName(AssetDatabase.GetAssetPath(item))));
            CollectionAssert.AreEqual(new[]
            {
                "ui-help.png", "ui-resume.png", "ui-sound-on.png", "ui-sound-off.png",
                "ui-captions.png", "ui-captions-off.png", "ui-reduced-motion.png", "ui-reduced-motion-off.png"
            }, content.PauseControls.Select(item => Path.GetFileName(AssetDatabase.GetAssetPath(item))));
            CollectionAssert.AreEqual(new[] { "ui-stay.png", "ui-back.png", "ui-leave.png" },
                content.LeaveControls.Select(item => Path.GetFileName(AssetDatabase.GetAssetPath(item))));
            CollectionAssert.AreEqual(new[]
            {
                "ui-caption-backplate-opaque.png", "ui-pressed-overlay.png", "ve-vo-overview-001.png"
            }, content.PresentationSprites.Select(item => Path.GetFileName(AssetDatabase.GetAssetPath(item))));
            Assert.That(Path.GetFileName(AssetDatabase.GetAssetPath(content.Voice)), Is.EqualTo("vo-overview-001-en.wav"));
            Assert.That(Path.GetFileName(AssetDatabase.GetAssetPath(content.CaptionTiming)), Is.EqualTo("vo-overview-001-caption.json"));
            Assert.That(Path.GetFileName(AssetDatabase.GetAssetPath(content.Music)), Is.EqualTo("mus-main-journey-loop.wav"));
            Assert.That(Path.GetFileName(AssetDatabase.GetAssetPath(content.Ambience)), Is.EqualTo("amb-nile-day-loop.wav"));
            Assert.That(content.AllReferences.All(item => AssetDatabase.GetAssetPath(item).StartsWith(ImportedRoot + "/", StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void NegativeFixturesRejectBlankHiddenRgbWrongRatioAndBakedText()
        {
            Assert.Throws<InvalidOperationException>(() => JourneyOverviewContentGenerator.ValidatePngContract(
                "timeline-icon-measure.png", 512, 512, 8, 6, 0, 512 * 512, 0, 1, false));
            Assert.Throws<InvalidOperationException>(() => JourneyOverviewContentGenerator.ValidatePngContract(
                "timeline-icon-measure.png", 512, 512, 8, 6, 1000, 512 * 512, 1, 32, false));
            Assert.Throws<InvalidOperationException>(() => JourneyOverviewContentGenerator.ValidatePngContract(
                "bg-journey-overview-16x9.png", 2048, 1536, 8, 2, 2048 * 1536, 2048 * 1536, 0, 32, false));
            Assert.Throws<InvalidOperationException>(() => JourneyOverviewContentGenerator.ValidatePngContract(
                "timeline-icon-read-river.png", 512, 512, 8, 6, 1000, 512 * 512, 0, 32, true));
        }

        [Test]
        public void NegativeFixturesRejectAlteredWaveMalformedCaptionInteractiveMedallionAndHubLabel()
        {
            Assert.Throws<InvalidOperationException>(() => JourneyOverviewContentGenerator.ValidateWaveContract(
                "vo-overview-001-en.wav", 9.000250d, 48000, 24, 1, "altered"));
            Assert.Throws<InvalidOperationException>(() => JourneyOverviewContentGenerator.ValidateCaptionContract(
                "VO-OVERVIEW-001", "vo-overview-001-en.wav", 9000,
                new[] { "VO-OVERVIEW-001-C01", "VO-OVERVIEW-001-C02", "VO-OVERVIEW-001-C03" },
                new[] { 0, 3189, 6182 }, new[] { 3139, 6032, 9000 },
                new[] { "First, measure the river.", "Next, read the river.", "Then, help the village." }));
            Assert.Throws<InvalidOperationException>(() => JourneyOverviewContentGenerator.ValidateTimelineSemantic(
                "timeline-state-current.png", true, true, true, false, false, true, null));
            Assert.Throws<InvalidOperationException>(() => JourneyOverviewContentGenerator.ValidateTimelineSemantic(
                "timeline-state-locked.png", true, true, false, false, false, false, "Hub"));
        }

        [Test]
        public void TimelineStatesUseShapeAndTreatmentWithoutRefusalOrInputOwnership()
        {
            Assert.DoesNotThrow(() => JourneyOverviewContentGenerator.ValidateTimelineSemantic(
                "timeline-state-visited.png", true, true, false, false, false, false, null));
            Assert.DoesNotThrow(() => JourneyOverviewContentGenerator.ValidateTimelineSemantic(
                "timeline-state-current.png", true, true, true, false, false, false, null));
            Assert.DoesNotThrow(() => JourneyOverviewContentGenerator.ValidateTimelineSemantic(
                "timeline-state-next.png", true, true, false, false, false, false, null));
            Assert.DoesNotThrow(() => JourneyOverviewContentGenerator.ValidateTimelineSemantic(
                "timeline-state-locked.png", true, true, false, false, false, false, null));
        }

        private static void AssertAudio(string name, AudioClipLoadType loadType, bool forceMono, bool preload)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(ImportedRoot + "/" + name);
            Assert.That(importer, Is.Not.Null, name);
            Assert.That(importer.forceToMono, Is.EqualTo(forceMono), name);
            Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(loadType), name);
            Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.Vorbis), name);
            Assert.That(importer.defaultSampleSettings.sampleRateSetting, Is.EqualTo(AudioSampleRateSetting.PreserveSampleRate), name);
            Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.EqualTo(preload), name);
            Assert.That(importer.loadInBackground, Is.EqualTo(!preload), name);
            Assert.That(importer.defaultSampleSettings.quality, Is.EqualTo(0.7f).Within(0.0001f), name);
        }

        private static Vector2Int ExpectedImage(string name)
        {
            if (name == "bg-journey-overview-4x3.png") return new Vector2Int(2048, 1536);
            if (name == "bg-journey-overview-16x9.png") return new Vector2Int(2560, 1440);
            if (name == "bg-journey-overview-24x10.png") return new Vector2Int(3456, 1440);
            if (name == "ui-caption-backplate-opaque.png") return new Vector2Int(1024, 1024);
            if (name == "ve-vo-overview-001.png") return new Vector2Int(1024, 512);
            return new Vector2Int(512, 512);
        }

        private static string[] ApprovedNames()
        {
            return RequiredContent().Select(item => Path.GetFileName(item.destination)).ToArray();
        }

        private static IEnumerable<(string source, string destination, string expectedHash)> RequiredContent()
        {
            var workspace = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            var hashes = FrozenHashes();
            foreach (var item in SourceRows())
            {
                yield return (
                    Path.Combine(workspace, item.source),
                    Path.Combine(ImportedRoot, item.name),
                    hashes[item.name]);
            }
        }

        private static IEnumerable<(string source, string name)> SourceRows()
        {
            yield return ("assets/step-06-environments/runtime/bg-journey-overview-4x3.png", "bg-journey-overview-4x3.png");
            yield return ("assets/step-06-environments/runtime/bg-journey-overview-16x9.png", "bg-journey-overview-16x9.png");
            yield return ("assets/step-06-environments/runtime/bg-journey-overview-24x10.png", "bg-journey-overview-24x10.png");
            foreach (var name in new[]
            {
                "ui-skip.png", "ui-repeat.png", "ui-pause.png", "ui-home.png",
                "ui-help.png", "ui-resume.png", "ui-sound-on.png", "ui-sound-off.png",
                "ui-captions.png", "ui-captions-off.png", "ui-reduced-motion.png", "ui-reduced-motion-off.png",
                "ui-stay.png", "ui-back.png", "ui-leave.png", "ui-caption-backplate-opaque.png", "ui-pressed-overlay.png"
            }) yield return ("assets/step-04-ui/runtime/" + name, name);
            foreach (var name in new[]
            {
                "timeline-icon-measure.png", "timeline-icon-read-river.png", "timeline-icon-help-village.png",
                "timeline-connector-boat.png", "timeline-state-visited.png", "timeline-state-current.png",
                "timeline-state-next.png", "timeline-state-locked.png"
            }) yield return ("assets/journey-overview/runtime/" + name, name);
            yield return ("production/step-10-voice-captions/voice/en/vo-overview-001-en.wav", "vo-overview-001-en.wav");
            yield return ("production/step-10-voice-captions/visual-equivalents/ve-vo-overview-001.png", "ve-vo-overview-001.png");
            yield return ("assets/journey-overview/source/vo-overview-001-caption.json", "vo-overview-001-caption.json");
            yield return ("production/step-11-audio/music/mus-main-journey-loop.wav", "mus-main-journey-loop.wav");
            yield return ("production/step-11-audio/ambience/amb-nile-day-loop.wav", "amb-nile-day-loop.wav");
        }

        private static Dictionary<string, string> FrozenHashes()
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

        private static Dictionary<string, string> TimelineSourceHashes()
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
    }
}
