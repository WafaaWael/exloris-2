using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Exloris.Editor;
using Exloris.Runtime.Boat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat1ContentContractTests
    {
        private const string ImportedRoot = "Assets/Exloris/Content/Imported/Boat1";

        [Test]
        public void RuntimeFolderContainsExactlyFiftyApprovedFilesWithSourceByteCoverage()
        {
            var rows = SourceRows().ToArray();
            var expected = rows.Select(row => row.name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            var actual = Directory.GetFiles(ImportedRoot)
                .Where(path => !path.EndsWith(".meta", StringComparison.Ordinal))
                .Select(Path.GetFileName).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(expected, actual);
            Assert.That(actual, Has.Length.EqualTo(50));
            Assert.DoesNotThrow(() => Boat1ContentGenerator.ValidateFileSet(actual));

            var workspace = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            foreach (var row in rows)
            {
                var source = Path.Combine(workspace, row.source);
                var imported = Path.Combine(ImportedRoot, row.name);
                Assert.That(File.Exists(source), Is.True, source);
                Assert.That(File.Exists(imported), Is.True, imported);
                Assert.That(Hash(imported), Is.EqualTo(Hash(source)), row.name);
            }
        }

        [Test]
        public void EveryPngHasApprovedDecodedProfileAndCleanAlpha()
        {
            foreach (var name in ApprovedNames().Where(IsPng))
            {
                var bytes = File.ReadAllBytes(Path.Combine(ImportedRoot, name));
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Assert.That(ImageConversion.LoadImage(texture, bytes), Is.True, name);
                var pixels = texture.GetPixels32();
                Assert.DoesNotThrow(() => Boat1ContentGenerator.ValidatePngContract(
                    name, texture.width, texture.height, bytes[24], bytes[25],
                    pixels.Count(pixel => pixel.a > 0), pixels.Length,
                    pixels.Count(pixel => pixel.a == 0 && (pixel.r != 0 || pixel.g != 0 || pixel.b != 0)),
                    CountDistinctColors(pixels), false));
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void EveryTextureUsesApprovedSpriteProfilePivotAndMobileCompression()
        {
            foreach (var name in ApprovedNames().Where(IsPng))
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
                Assert.That(importer.spritePivot, Is.EqualTo(ExpectedPivot(name)), name);
                Assert.That(importer.GetDefaultPlatformTextureSettings().maxTextureSize, Is.EqualTo(ExpectedMaxSize(name)), name);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), name);
                Assert.That(settings.spriteAlignment, Is.EqualTo((int)SpriteAlignment.Custom), name);
                foreach (var platformName in new[] { "iPhone", "Android" })
                {
                    var platform = importer.GetPlatformTextureSettings(platformName);
                    Assert.That(platform.overridden, Is.True, name + " " + platformName);
                    Assert.That(platform.format, Is.EqualTo(
                        name.StartsWith("bg-", StringComparison.Ordinal) || name.StartsWith("ve-", StringComparison.Ordinal)
                            ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4), name + " " + platformName);
                }
            }
            var backplate = (TextureImporter)AssetImporter.GetAtPath(ImportedRoot + "/ui-caption-backplate-opaque.png");
            Assert.That(backplate.spriteBorder, Is.EqualTo(new Vector4(28f, 28f, 28f, 28f)));
        }

        [Test]
        public void VoiceMusicAmbienceAndSfxUseApprovedIndependentProfiles()
        {
            AssertAudio("vo-boat-choice-en.wav", AudioClipLoadType.CompressedInMemory, true, true, false);
            AssertAudio("vo-boat-help-en.wav", AudioClipLoadType.CompressedInMemory, true, true, false);
            AssertAudio("mus-main-journey-loop.wav", AudioClipLoadType.Streaming, false, false, true);
            AssertAudio("amb-boat-water-loop.wav", AudioClipLoadType.Streaming, false, false, true);
            AssertAudio("sfx-boat-arrival.wav", AudioClipLoadType.CompressedInMemory, false, true, false);
            AssertAudio("sfx-ibis-cue.wav", AudioClipLoadType.CompressedInMemory, false, true, false);
            foreach (var name in WaveHashes().Keys)
                Assert.DoesNotThrow(() => Boat1ContentGenerator.ValidateWaveFile(name, Path.Combine(ImportedRoot, name)), name);
        }

        [Test]
        public void CaptionFileHasExactChoiceHelpIntervalsAndGaps()
        {
            Assert.DoesNotThrow(() => Boat1ContentGenerator.ValidateCaptionFile(
                Path.Combine(ImportedRoot, "boat-1-caption-timings.json")));
        }

        [Test]
        public void ApprovedResourceContainsOnlyFiftyStageReferencesAndFixedRouteConstants()
        {
            var content = AssetDatabase.LoadAssetAtPath<Boat1Content>(Boat1ContentGenerator.ContentAssetPath);
            Assert.That(content, Is.Not.Null);
            Assert.That(content.Backgrounds, Has.Length.EqualTo(9));
            Assert.That(content.BoatSprites, Has.Length.EqualTo(5));
            Assert.That(content.ChildSprites, Has.Length.EqualTo(2));
            Assert.That(content.BoatkeeperSprites, Has.Length.EqualTo(4));
            Assert.That(content.IbisSprites, Has.Length.EqualTo(9));
            Assert.That(content.Controls, Has.Length.EqualTo(10));
            Assert.That(content.PresentationSprites, Has.Length.EqualTo(4));
            Assert.That(content.VoiceClips, Has.Length.EqualTo(2));
            Assert.That(content.CaptionTimings, Is.Not.Null);
            Assert.That(content.Music, Is.Not.Null);
            Assert.That(content.Ambience, Is.Not.Null);
            Assert.That(content.ArrivalSfx, Is.Not.Null);
            Assert.That(content.IbisSfx, Is.Not.Null);
            Assert.That(content.InitialIbisDirection, Is.EqualTo(BoatDirection.Right));
            Assert.That(content.RouteDurationSeconds, Is.EqualTo(6d));
            CollectionAssert.AreEqual(new[] { 0d, 2d, 4d, 6d }, content.ProgressCutSeconds);
            Assert.That(content.AllReferences, Has.Length.EqualTo(50));
            Assert.That(content.AllReferences.Any(item => item == null), Is.False);
            Assert.That(content.AllReferences.Distinct().Count(), Is.EqualTo(50));
            Assert.That(content.AllReferences.All(item => AssetDatabase.GetAssetPath(item).StartsWith(ImportedRoot + "/", StringComparison.Ordinal)), Is.True);
            CollectionAssert.AreEqual(new[] { "chr-boy-seated.png", "chr-girl-seated.png" }, AssetNames(content.ChildSprites));
            CollectionAssert.AreEqual(new[]
            {
                "chr-boatkeeper-steer-01.png", "chr-boatkeeper-steer-02.png",
                "chr-boatkeeper-idle.png", "chr-boatkeeper-point-safe-direction.png"
            }, AssetNames(content.BoatkeeperSprites));
            Assert.That(AssetNames(content.IbisSprites).Last(), Is.EqualTo("ibis-static-right-reduced.png"));
        }

        [Test]
        public void NegativePngFixturesRejectBlankHiddenRgbWrongDepthRatioPivotReviewAndHubAssets()
        {
            Assert.Throws<InvalidOperationException>(() => Boat1ContentGenerator.ValidatePngContract(
                "boat-hull.png", 2048, 1024, 8, 6, 0, 2048 * 1024, 0, 32, false));
            Assert.Throws<InvalidOperationException>(() => Boat1ContentGenerator.ValidatePngContract(
                "boat-hull.png", 2048, 1024, 8, 6, 1000, 2048 * 1024, 1, 32, false));
            Assert.Throws<InvalidOperationException>(() => Boat1ContentGenerator.ValidatePngContract(
                "boat-hull.png", 2048, 1024, 16, 6, 1000, 2048 * 1024, 0, 32, false));
            Assert.Throws<InvalidOperationException>(() => Boat1ContentGenerator.ValidatePngContract(
                "bg-05-boat-reeds-16x9.png", 2048, 1536, 8, 2, 2048 * 1536, 2048 * 1536, 0, 256, false));
            Assert.Throws<InvalidOperationException>(() => Boat1ContentGenerator.ValidatePngContract(
                "boat-hull.png", 2048, 1024, 8, 6, 1000, 2048 * 1024, 0, 32, true));
            Assert.Throws<InvalidOperationException>(() => Boat1ContentGenerator.ValidatePngContract(
                "boat-1-review-composite.png", 512, 512, 8, 6, 1000, 512 * 512, 0, 32, false));
            Assert.Throws<InvalidOperationException>(() => Boat1ContentGenerator.ValidatePngContract(
                "hub-boat-choice.png", 512, 512, 8, 6, 1000, 512 * 512, 0, 32, false));
            var boatImporter = (TextureImporter)AssetImporter.GetAtPath(ImportedRoot + "/boat-hull.png");
            Assert.That(boatImporter.spritePivot, Is.EqualTo(new Vector2(0.5f, 0.18f)));
        }

        [Test]
        public void NegativeMediaFixturesRejectAlteredWaveMalformedCaptionMissingRolesAndWrongIbisDirection()
        {
            Assert.Throws<InvalidOperationException>(() => Boat1ContentGenerator.ValidateWaveContract(
                "vo-boat-choice-en.wav", 7.077875d, 48000, 24, 1, "altered"));
            var malformed = Path.GetTempFileName();
            try
            {
                File.WriteAllText(malformed,
                    "[{\"string_id\":\"VO-BOAT-CHOICE\",\"audio_file\":\"vo-boat-choice-en.wav\",\"duration_ms\":7078,\"cues\":[]}," +
                    "{\"string_id\":\"VO-BOAT-HELP\",\"audio_file\":\"vo-boat-help-en.wav\",\"duration_ms\":6635,\"cues\":[]}]");
                Assert.Throws<InvalidOperationException>(() => Boat1ContentGenerator.ValidateCaptionFile(malformed));
            }
            finally { File.Delete(malformed); }
            var content = AssetDatabase.LoadAssetAtPath<Boat1Content>(Boat1ContentGenerator.ContentAssetPath);
            Assert.That(content.ChildSprites.Length, Is.EqualTo(2), "Both seated child choices are mandatory.");
            Assert.That(content.BoatkeeperSprites.Length, Is.EqualTo(4), "All boatkeeper semantic poses are mandatory.");
            Assert.That(content.InitialIbisDirection, Is.Not.EqualTo(BoatDirection.Left));
        }

        private static void AssertAudio(string name, AudioClipLoadType loadType, bool forceMono, bool preload, bool background)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(ImportedRoot + "/" + name);
            Assert.That(importer, Is.Not.Null, name);
            Assert.That(importer.forceToMono, Is.EqualTo(forceMono), name);
            Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(loadType), name);
            Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.Vorbis), name);
            Assert.That(importer.defaultSampleSettings.sampleRateSetting, Is.EqualTo(AudioSampleRateSetting.PreserveSampleRate), name);
            Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.EqualTo(preload), name);
            Assert.That(importer.loadInBackground, Is.EqualTo(background), name);
        }

        private static string[] AssetNames(IEnumerable<UnityEngine.Object> assets) =>
            assets.Select(item => Path.GetFileName(AssetDatabase.GetAssetPath(item))).ToArray();

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

        private static Vector2 ExpectedPivot(string name)
        {
            if (BoatArtNames().Contains(name)) return new Vector2(0.5f, 0.18f);
            if (name.StartsWith("chr-", StringComparison.Ordinal)) return new Vector2(0.5f, 0.08f);
            return new Vector2(0.5f, 0.5f);
        }

        private static int ExpectedMaxSize(string name)
        {
            if (name.StartsWith("bg-", StringComparison.Ordinal)) return 4096;
            if (BoatArtNames().Contains(name)) return 2048;
            if (name.StartsWith("chr-", StringComparison.Ordinal) || name.StartsWith("ibis-", StringComparison.Ordinal)
                || name == "ui-caption-backplate-opaque.png" || name.StartsWith("ve-", StringComparison.Ordinal)) return 1024;
            return 512;
        }

        private static string[] BoatArtNames() => new[]
        {
            "boat-hull.png", "boat-rail-near.png", "boat-steering-oar.png", "boat-wake.png", "boat-static-reduced.png"
        };

        private static bool IsPng(string name) => name.EndsWith(".png", StringComparison.Ordinal);
        private static string[] ApprovedNames() => SourceRows().Select(row => row.name).ToArray();

        private static IEnumerable<(string source, string name)> SourceRows()
        {
            foreach (var name in new[]
            {
                "bg-05-boat-reeds-4x3.png", "bg-05-boat-reeds-16x9.png", "bg-05-boat-reeds-24x10.png",
                "bg-06-boat-village-4x3.png", "bg-06-boat-village-16x9.png", "bg-06-boat-village-24x10.png",
                "bg-08-boat-arrival-4x3.png", "bg-08-boat-arrival-16x9.png", "bg-08-boat-arrival-24x10.png"
            }) yield return ("assets/step-06-environments/runtime/" + name, name);
            foreach (var name in BoatArtNames().Concat(new[]
            {
                "chr-boatkeeper-steer-01.png", "chr-boatkeeper-steer-02.png", "chr-boatkeeper-idle.png", "chr-boatkeeper-point-safe-direction.png",
                "ibis-idle.png", "ibis-wings-open.png", "ibis-head-lift.png", "ibis-beak-left.png", "ibis-beak-right.png",
                "ibis-calm-settle.png", "ibis-help.png", "ibis-static-left-reduced.png", "ibis-static-right-reduced.png"
            })) yield return ("assets/step-07-gameplay-art/runtime/" + name, name);
            foreach (var name in new[] { "chr-boy-seated.png", "chr-girl-seated.png" })
                yield return ("assets/step-05-characters/runtime/" + name, name);
            foreach (var name in new[]
            {
                "boat-1-left-reeds.png", "boat-1-right-village.png", "boat-direction-left.png", "boat-direction-right.png", "boat-follow-ibis.png",
                "ui-help.png", "ui-repeat.png", "ui-skip.png", "ui-pause.png", "ui-home.png", "ui-caption-backplate-opaque.png", "ui-pressed-overlay.png"
            }) yield return ("assets/step-04-ui/runtime/" + name, name);
            foreach (var name in new[] { "ve-vo-boat-choice.png", "ve-vo-boat-help.png" })
                yield return ("production/step-10-voice-captions/visual-equivalents/" + name, name);
            foreach (var name in new[] { "vo-boat-choice-en.wav", "vo-boat-help-en.wav" })
                yield return ("production/step-10-voice-captions/voice/en/" + name, name);
            yield return ("assets/boat-1/source/boat-1-caption-timings.json", "boat-1-caption-timings.json");
            yield return ("production/step-11-audio/music/mus-main-journey-loop.wav", "mus-main-journey-loop.wav");
            yield return ("production/step-11-audio/ambience/amb-boat-water-loop.wav", "amb-boat-water-loop.wav");
            yield return ("production/step-11-audio/sfx/sfx-boat-arrival.wav", "sfx-boat-arrival.wav");
            yield return ("production/step-11-audio/sfx/sfx-ibis-cue.wav", "sfx-ibis-cue.wav");
        }

        private static Dictionary<string, string> WaveHashes() => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["vo-boat-choice-en.wav"] = "78f145e11b787fbab0019183330dff2c2ece8fb67d26e40059cd0b6044f5c3d2",
            ["vo-boat-help-en.wav"] = "a67be07ab3bb697f5c21d81764bfce90877990019a8a85dbdd6cf0bd8de15f6b",
            ["mus-main-journey-loop.wav"] = "a6612a0cfce3928e7ea01652bb397218f49f524dcc77994d9293736b39806e4e",
            ["amb-boat-water-loop.wav"] = "d9dea3635cfbb9ec52f884ab6e931310b5237122d3bb3c4a6070088f85c69b60",
            ["sfx-boat-arrival.wav"] = "f5d35c0569e2a3b8f67cff3a5be3296d624fc251a8dd05c2e06cbdcb2042f5b2",
            ["sfx-ibis-cue.wav"] = "9a26e04515348aca82b4b1c079c0879c6c48dca17cc897665ecde52e8bb7760f"
        };

        private static string Hash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
