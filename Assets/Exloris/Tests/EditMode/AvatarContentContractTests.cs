using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Exloris.Editor;
using Exloris.Runtime.Avatar;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class AvatarContentContractTests
    {
        private const string ImportedRoot = "Assets/Exloris/Content/Imported/Avatar";

        [Test]
        public void RuntimeAvatarContentMatchesEveryExactApprovedSourceByte()
        {
            foreach (var item in RequiredContent())
            {
                Assert.That(File.Exists(item.destination), Is.True, item.destination);
                Assert.That(Hash(item.destination), Is.EqualTo(Hash(item.source)), item.destination);
            }
        }

        [Test]
        public void AvatarFolderContainsExactlyThirtySevenApprovedFiles()
        {
            var actual = Directory.Exists(ImportedRoot)
                ? Directory.GetFiles(ImportedRoot)
                    .Where(path => !path.EndsWith(".meta", StringComparison.Ordinal))
                    .Select(Path.GetFileName)
                    .OrderBy(name => name)
                    .ToArray()
                : Array.Empty<string>();

            CollectionAssert.AreEqual(ApprovedNames().OrderBy(name => name), actual);
            Assert.DoesNotThrow(() => AvatarContentGenerator.ValidateFileSet(actual));
        }

        [Test]
        public void ImportedPngsHaveExactDecodedDimensionsAndNonblankEightBitPixels()
        {
            foreach (var expected in ExpectedImages())
            {
                var path = Path.Combine(ImportedRoot, expected.Key);
                var bytes = File.ReadAllBytes(path);
                Assert.That(bytes[24], Is.EqualTo(8), expected.Key + " bit depth");
                Assert.That(bytes[25], Is.EqualTo(expected.Value.alpha ? 6 : 2), expected.Key + " color type");
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Assert.That(ImageConversion.LoadImage(texture, bytes), Is.True, expected.Key);
                Assert.That(new Vector2Int(texture.width, texture.height),
                    Is.EqualTo(new Vector2Int(expected.Value.width, expected.Value.height)), expected.Key);
                var pixels = texture.GetPixels32();
                Assert.That(pixels.Any(pixel => pixel.a > 0), Is.True, expected.Key + " blank alpha");
                Assert.That(pixels.Select(pixel => (pixel.r, pixel.g, pixel.b, pixel.a)).Distinct().Take(2).Count(),
                    Is.EqualTo(2), expected.Key + " single-colour blank");
                Assert.DoesNotThrow(() => AvatarContentGenerator.ValidatePixelData(
                    expected.Key,
                    texture.width,
                    texture.height,
                    bytes[24],
                    bytes[25] == 6,
                    true));
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void CharacterImportsShareExactProfileAndLowFootPivot()
        {
            foreach (var name in ApprovedNames().Where(name => name.StartsWith("chr-", StringComparison.Ordinal)))
            {
                AssertTexture(name, 1024, true, new Vector2(0.5f, 0.08f));
            }
        }

        [Test]
        public void BackgroundUiAndVisualImportsUseExactProfilesWithoutMipmaps()
        {
            foreach (var name in ApprovedNames().Where(name => name.StartsWith("bg-", StringComparison.Ordinal)))
                AssertTexture(name, 4096, false, new Vector2(0.5f, 0.5f));
            foreach (var name in ApprovedNames().Where(name => name == "frame-card.png" || name.StartsWith("ui-", StringComparison.Ordinal)))
                AssertTexture(name, name == "frame-card.png" || name == "ui-caption-backplate-opaque.png" ? 1024 : 512, true, new Vector2(0.5f, 0.5f));
            foreach (var name in ApprovedNames().Where(name => name.StartsWith("ve-", StringComparison.Ordinal)))
                AssertTexture(name, 1024, true, new Vector2(0.5f, 0.5f));
        }

        [Test]
        public void VoiceAndSfxImportsUseTheirApprovedMatchingProfiles()
        {
            foreach (var name in ApprovedNames().Where(name => name.StartsWith("vo-", StringComparison.Ordinal)))
                AssertAudio(name, AudioClipLoadType.CompressedInMemory, AudioCompressionFormat.Vorbis);
            foreach (var name in ApprovedNames().Where(name => name.StartsWith("sfx-", StringComparison.Ordinal)))
                AssertAudio(name, AudioClipLoadType.DecompressOnLoad, AudioCompressionFormat.PCM);
        }

        [Test]
        public void ApprovedAvatarResourceContainsOnlyCompleteActiveStageReferences()
        {
            var content = AssetDatabase.LoadAssetAtPath<AvatarContent>(AvatarContentGenerator.ContentAssetPath);

            Assert.That(content, Is.Not.Null);
            Assert.That(content.Backgrounds, Has.Length.EqualTo(3));
            Assert.That(content.BoyMotionSprites, Has.Length.EqualTo(6));
            Assert.That(content.GirlMotionSprites, Has.Length.EqualTo(6));
            Assert.That(content.MotionContracts, Has.Length.EqualTo(4));
            Assert.That(content.UiSprites, Has.Length.EqualTo(9));
            Assert.That(content.VoiceClips, Has.Length.EqualTo(3));
            Assert.That(content.VisualEquivalents, Has.Length.EqualTo(3));
            Assert.That(content.SfxClips, Has.Length.EqualTo(3));
            Assert.That(content.AllReferences.Any(item => item == null), Is.False);
        }

        [Test]
        public void NegativeFixturesRejectBlankWrongRatioAndNonEightBitArt()
        {
            Assert.Throws<InvalidOperationException>(() =>
                AvatarContentGenerator.ValidatePixelData("chr-boy-idle-01.png", 1024, 1024, 8, true, false));
            Assert.Throws<InvalidOperationException>(() =>
                AvatarContentGenerator.ValidatePixelData("bg-03-traveller-16x9.png", 2048, 1536, 8, false, true));
            Assert.Throws<InvalidOperationException>(() =>
                AvatarContentGenerator.ValidatePixelData("chr-girl-wave.png", 1024, 1024, 16, true, true));
        }

        [Test]
        public void NegativeFixturesRejectUnequalProfilePivotAndBakedText()
        {
            Assert.Throws<InvalidOperationException>(() => AvatarContentGenerator.ValidateTextureProfile(
                "chr-girl-idle-01.png", 512, true, false, new Vector2(0.5f, 0.08f), false));
            Assert.Throws<InvalidOperationException>(() => AvatarContentGenerator.ValidateTextureProfile(
                "chr-girl-idle-01.png", 1024, true, false, new Vector2(0.5f, 0.5f), false));
            Assert.Throws<InvalidOperationException>(() => AvatarContentGenerator.ValidateTextureProfile(
                "ui-confirm.png", 512, true, false, new Vector2(0.5f, 0.5f), true));
        }

        [Test]
        public void NegativeFixturesRejectMismatchedMotionThirdAvatarAndUnapprovedFile()
        {
            Assert.Throws<InvalidOperationException>(() => AvatarContentGenerator.ValidateMotionOwner(
                "anim-boy-idle.motion.json", "girl traveller avatar idle"));
            var thirdAvatar = ApprovedNames().Concat(new[] { "chr-third-idle-01.png" }).ToArray();
            var unapproved = ApprovedNames().Concat(new[] { "debug-overlay.png" }).ToArray();
            Assert.Throws<InvalidOperationException>(() => AvatarContentGenerator.ValidateFileSet(thirdAvatar));
            Assert.Throws<InvalidOperationException>(() => AvatarContentGenerator.ValidateFileSet(unapproved));
        }

        private static void AssertTexture(string name, int maxSize, bool alpha, Vector2 pivot)
        {
            var path = ImportedRoot + "/" + name;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer, Is.Not.Null, name);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), name);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), name);
            Assert.That(importer.sRGBTexture, Is.True, name);
            Assert.That(importer.alphaIsTransparency, Is.EqualTo(alpha), name);
            Assert.That(importer.mipmapEnabled, Is.False, name);
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), name);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), name);
            Assert.That(importer.GetDefaultPlatformTextureSettings().maxTextureSize, Is.EqualTo(maxSize), name);
            Assert.That(importer.spritePivot.x, Is.EqualTo(pivot.x).Within(0.0001f), name);
            Assert.That(importer.spritePivot.y, Is.EqualTo(pivot.y).Within(0.0001f), name);
            Assert.DoesNotThrow(() => AvatarContentGenerator.ValidateTextureProfile(
                name, maxSize, importer.sRGBTexture, importer.mipmapEnabled, importer.spritePivot, false));
        }

        private static void AssertAudio(string name, AudioClipLoadType loadType, AudioCompressionFormat format)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(ImportedRoot + "/" + name);
            Assert.That(importer, Is.Not.Null, name);
            Assert.That(importer.forceToMono, Is.True, name);
            Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(loadType), name);
            Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(format), name);
            Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.True, name);
        }

        private static Dictionary<string, (int width, int height, bool alpha)> ExpectedImages()
        {
            var images = new Dictionary<string, (int, int, bool)>
            {
                ["bg-03-traveller-4x3.png"] = (2048, 1536, false),
                ["bg-03-traveller-16x9.png"] = (2560, 1440, false),
                ["bg-03-traveller-24x10.png"] = (3456, 1440, false),
                ["frame-card.png"] = (1024, 1024, true),
                ["ui-confirm.png"] = (512, 512, true),
                ["ui-home.png"] = (512, 512, true),
                ["ui-repeat.png"] = (512, 512, true),
                ["ui-choose-other.png"] = (512, 512, true),
                ["ui-stay.png"] = (512, 512, true),
                ["ui-leave.png"] = (512, 512, true),
                ["ui-caption-backplate-opaque.png"] = (1024, 1024, true),
                ["ui-pressed-overlay.png"] = (512, 512, true),
                ["ve-vo-avatar-select.png"] = (1024, 512, true),
                ["ve-vo-avatar-preview.png"] = (1024, 512, true),
                ["ve-vo-leave.png"] = (1024, 512, true),
            };
            foreach (var avatar in new[] { "boy", "girl" })
            foreach (var pose in new[] { "idle-01", "idle-02", "blink", "turn", "wave", "static-reduced" })
                images[$"chr-{avatar}-{pose}.png"] = (1024, 1024, true);
            return images;
        }

        private static string[] ApprovedNames()
        {
            return RequiredContent().Select(item => Path.GetFileName(item.destination)).ToArray();
        }

        private static IEnumerable<(string source, string destination)> RequiredContent()
        {
            var workspace = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            var items = new List<(string, string)>
            {
                ("assets/step-06-environments/runtime/bg-03-traveller-4x3.png", "bg-03-traveller-4x3.png"),
                ("assets/step-06-environments/runtime/bg-03-traveller-16x9.png", "bg-03-traveller-16x9.png"),
                ("assets/step-06-environments/runtime/bg-03-traveller-24x10.png", "bg-03-traveller-24x10.png"),
                ("assets/step-04-ui/runtime/frame-card.png", "frame-card.png"),
                ("assets/step-04-ui/runtime/ui-confirm.png", "ui-confirm.png"),
                ("assets/step-04-ui/runtime/ui-home.png", "ui-home.png"),
                ("assets/step-04-ui/runtime/ui-repeat.png", "ui-repeat.png"),
                ("assets/step-04-ui/runtime/ui-choose-other.png", "ui-choose-other.png"),
                ("assets/step-04-ui/runtime/ui-stay.png", "ui-stay.png"),
                ("assets/step-04-ui/runtime/ui-leave.png", "ui-leave.png"),
                ("assets/step-04-ui/runtime/ui-caption-backplate-opaque.png", "ui-caption-backplate-opaque.png"),
                ("assets/step-04-ui/runtime/ui-pressed-overlay.png", "ui-pressed-overlay.png"),
                ("production/step-10-voice-captions/voice/en/vo-avatar-select-en.wav", "vo-avatar-select-en.wav"),
                ("production/step-10-voice-captions/voice/en/vo-avatar-preview-en.wav", "vo-avatar-preview-en.wav"),
                ("production/step-10-voice-captions/voice/en/vo-leave-en.wav", "vo-leave-en.wav"),
                ("production/step-10-voice-captions/visual-equivalents/ve-vo-avatar-select.png", "ve-vo-avatar-select.png"),
                ("production/step-10-voice-captions/visual-equivalents/ve-vo-avatar-preview.png", "ve-vo-avatar-preview.png"),
                ("production/step-10-voice-captions/visual-equivalents/ve-vo-leave.png", "ve-vo-leave.png"),
                ("production/step-11-audio/sfx/sfx-ui-press.wav", "sfx-ui-press.wav"),
                ("production/step-11-audio/sfx/sfx-ui-confirm.wav", "sfx-ui-confirm.wav"),
                ("production/step-11-audio/sfx/sfx-ui-back.wav", "sfx-ui-back.wav"),
            };
            foreach (var avatar in new[] { "boy", "girl" })
            foreach (var pose in new[] { "idle-01", "idle-02", "blink", "turn", "wave", "static-reduced" })
                items.Add(($"assets/step-05-characters/runtime/chr-{avatar}-{pose}.png", $"chr-{avatar}-{pose}.png"));
            foreach (var avatar in new[] { "boy", "girl" })
            foreach (var motion in new[] { "idle", "preview" })
                items.Add(($"assets/step-05-characters/motion/anim-{avatar}-{motion}.motion.json", $"anim-{avatar}-{motion}.motion.json"));
            return items.Select(item => (
                Path.Combine(workspace, item.Item1),
                Path.Combine(ImportedRoot, item.Item2)));
        }

        private static string Hash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }
    }
}
