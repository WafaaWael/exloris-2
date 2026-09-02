using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Exloris.Runtime.Welcome;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class WelcomeContentContractTests
    {
        [Test]
        public void RuntimeWelcomeContentMatchesEveryExactApprovedSourceByte()
        {
            foreach (var item in RequiredContent())
            {
                Assert.That(
                    File.Exists(item.destination),
                    Is.True,
                    "Missing runtime Welcome content: " + item.destination);
                Assert.That(
                    Hash(item.destination),
                    Is.EqualTo(Hash(item.source)),
                    "Runtime Welcome copy changed: " + item.destination);
            }
        }

        [Test]
        public void ApprovedWelcomeContentResourceContainsEveryRequiredReference()
        {
            var content = AssetDatabase.LoadAssetAtPath<WelcomeContent>(
                "Assets/Exloris/Resources/Content/WelcomeContent.asset");

            Assert.That(content, Is.Not.Null);
            Assert.That(content.Background4x3, Is.Not.Null);
            Assert.That(content.Background16x9, Is.Not.Null);
            Assert.That(content.Background24x10, Is.Not.Null);
            Assert.That(content.Play, Is.Not.Null);
            Assert.That(content.Adult, Is.Not.Null);
            Assert.That(content.Continue, Is.Not.Null);
            Assert.That(content.Restart, Is.Not.Null);
            Assert.That(content.Back, Is.Not.Null);
            Assert.That(content.Resume, Is.Not.Null);
            Assert.That(content.Retry, Is.Not.Null);
            Assert.That(content.Home, Is.Not.Null);
            Assert.That(content.WelcomeVoice, Is.Not.Null);
            Assert.That(content.SessionChoiceVoice, Is.Not.Null);
            Assert.That(content.WelcomeVisualEquivalent, Is.Not.Null);
            Assert.That(content.SessionChoiceVisualEquivalent, Is.Not.Null);
            Assert.That(content.Music, Is.Not.Null);
            Assert.That(content.Ambience, Is.Not.Null);
        }

        [Test]
        public void ApprovedWelcomeContentResourceContainsPairedSettingsAndPresentationReferences()
        {
            var content = AssetDatabase.LoadAssetAtPath<WelcomeContent>(
                "Assets/Exloris/Resources/Content/WelcomeContent.asset");

            Assert.That(content, Is.Not.Null);
            Assert.That(content.SoundOn, Is.Not.Null);
            Assert.That(content.SoundOff, Is.Not.Null);
            Assert.That(content.CaptionsOn, Is.Not.Null);
            Assert.That(content.CaptionsOff, Is.Not.Null);
            Assert.That(content.ReducedMotionOn, Is.Not.Null);
            Assert.That(content.ReducedMotionOff, Is.Not.Null);
            Assert.That(content.CaptionBackplate, Is.Not.Null);
            Assert.That(content.PressedOverlay, Is.Not.Null);
        }

        [Test]
        public void ApprovedWelcomeContentUsesOnlyDeterministicCatalogueImports()
        {
            var content = AssetDatabase.LoadAssetAtPath<WelcomeContent>(
                "Assets/Exloris/Resources/Content/WelcomeContent.asset");

            Assert.That(AssetDatabase.GetAssetPath(content.Background4x3), Is.EqualTo("Assets/Exloris/Content/Imported/backgrounds/bg-01-launch-4x3.png"));
            Assert.That(AssetDatabase.GetAssetPath(content.Play), Is.EqualTo("Assets/Exloris/Content/Imported/ui/ui-play.png"));
            Assert.That(AssetDatabase.GetAssetPath(content.Resume), Is.EqualTo("Assets/Exloris/Content/Imported/ui/ui-resume.png"));
            Assert.That(AssetDatabase.GetAssetPath(content.Retry), Is.EqualTo("Assets/Exloris/Content/Imported/ui/ui-retry.png"));
            Assert.That(AssetDatabase.GetAssetPath(content.Home), Is.EqualTo("Assets/Exloris/Content/Imported/ui/ui-home.png"));
            Assert.That(AssetDatabase.GetAssetPath(content.WelcomeVoice), Is.EqualTo("Assets/Exloris/Content/Imported/voice/en/vo-welcome-001-en.wav"));
            Assert.That(AssetDatabase.GetAssetPath(content.WelcomeVisualEquivalent), Is.EqualTo("Assets/Exloris/Content/Imported/visual-equivalents/ve-vo-welcome-001.png"));
            Assert.That(AssetDatabase.GetAssetPath(content.Music), Is.EqualTo("Assets/Exloris/Content/Imported/music/mus-main-journey-loop.wav"));
            Assert.That(AssetDatabase.GetAssetPath(content.Ambience), Is.EqualTo("Assets/Exloris/Content/Imported/ambience/amb-nile-day-loop.wav"));
        }

        [Test]
        public void WelcomeStageCopyHasExactCountDecodedDimensionsAndVisiblePixels()
        {
            var root = "Assets/Exloris/Content/Imported/Welcome";
            var files = Directory.GetFiles(root)
                .Where(path => !path.EndsWith(".meta"))
                .OrderBy(path => path)
                .ToArray();
            Assert.That(files.Length, Is.EqualTo(22));

            var expected = new Dictionary<string, Vector2Int>
            {
                ["bg-01-launch-4x3.png"] = new Vector2Int(2048, 1536),
                ["bg-01-launch-16x9.png"] = new Vector2Int(2560, 1440),
                ["bg-01-launch-24x10.png"] = new Vector2Int(3456, 1440),
                ["ui-adult-hold.png"] = new Vector2Int(512, 512),
                ["ui-back.png"] = new Vector2Int(512, 512),
                ["ui-caption-backplate-opaque.png"] = new Vector2Int(1024, 1024),
                ["ui-captions-off.png"] = new Vector2Int(512, 512),
                ["ui-captions.png"] = new Vector2Int(512, 512),
                ["ui-continue.png"] = new Vector2Int(512, 512),
                ["ui-play.png"] = new Vector2Int(512, 512),
                ["ui-pressed-overlay.png"] = new Vector2Int(512, 512),
                ["ui-reduced-motion-off.png"] = new Vector2Int(512, 512),
                ["ui-reduced-motion.png"] = new Vector2Int(512, 512),
                ["ui-restart.png"] = new Vector2Int(512, 512),
                ["ui-sound-off.png"] = new Vector2Int(512, 512),
                ["ui-sound-on.png"] = new Vector2Int(512, 512),
                ["ve-vo-welcome-001.png"] = new Vector2Int(1024, 512),
                ["ve-vo-welcome-session.png"] = new Vector2Int(1024, 512),
            };

            foreach (var item in expected)
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Assert.That(ImageConversion.LoadImage(texture, File.ReadAllBytes(Path.Combine(root, item.Key))), Is.True);
                Assert.That(new Vector2Int(texture.width, texture.height), Is.EqualTo(item.Value), item.Key);
                Assert.That(texture.GetPixels32().Any(pixel => pixel.a > 0), Is.True, item.Key + " is blank");
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void CanonicalWelcomeImportsUseFrozenTextureAndAudioProfiles()
        {
            AssertTexture("backgrounds/bg-01-launch-16x9.png", 4096, false);
            AssertTexture("ui/ui-play.png", 512, true);
            AssertTexture("ui/ui-caption-backplate-opaque.png", 1024, true);
            AssertTexture("visual-equivalents/ve-vo-welcome-001.png", 1024, true);
            AssertAudio("voice/en/vo-welcome-001-en.wav", AudioClipLoadType.CompressedInMemory, true, true);
            AssertAudio("music/mus-main-journey-loop.wav", AudioClipLoadType.Streaming, false, false);
            AssertAudio("ambience/amb-nile-day-loop.wav", AudioClipLoadType.Streaming, false, false);
        }

        [Test]
        public void FrozenInventoryMarksWelcomeUiAndVisualCuesAsNoBakedText()
        {
            var workspace = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            var inventory = File.ReadAllLines(Path.Combine(
                workspace,
                "production/step-12-final-export/source/final-export-inventory-v01.csv"));
            var required = new[]
            {
                "ui-play.png", "ui-adult-hold.png", "ui-sound-on.png", "ui-sound-off.png",
                "ui-captions.png", "ui-captions-off.png", "ui-reduced-motion.png",
                "ui-reduced-motion-off.png", "ui-continue.png", "ui-restart.png", "ui-back.png",
                "ui-caption-backplate-opaque.png", "ui-pressed-overlay.png",
                "ve-vo-welcome-001.png", "ve-vo-welcome-session.png"
            };

            foreach (var filename in required)
            {
                var row = inventory.Single(line => line.Contains("/" + filename + ","));
                Assert.That(row, Does.Contain("no baked text"), filename);
            }
        }

        private static IEnumerable<(string source, string destination)> RequiredContent()
        {
            var workspace = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            var destination = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets/Exloris/Content/Imported/Welcome");
            var items = new[]
            {
                ("assets/step-06-environments/runtime/bg-01-launch-4x3.png", "bg-01-launch-4x3.png"),
                ("assets/step-06-environments/runtime/bg-01-launch-16x9.png", "bg-01-launch-16x9.png"),
                ("assets/step-06-environments/runtime/bg-01-launch-24x10.png", "bg-01-launch-24x10.png"),
                ("assets/step-04-ui/runtime/ui-play.png", "ui-play.png"),
                ("assets/step-04-ui/runtime/ui-adult-hold.png", "ui-adult-hold.png"),
                ("assets/step-04-ui/runtime/ui-sound-on.png", "ui-sound-on.png"),
                ("assets/step-04-ui/runtime/ui-sound-off.png", "ui-sound-off.png"),
                ("assets/step-04-ui/runtime/ui-captions.png", "ui-captions.png"),
                ("assets/step-04-ui/runtime/ui-captions-off.png", "ui-captions-off.png"),
                ("assets/step-04-ui/runtime/ui-reduced-motion.png", "ui-reduced-motion.png"),
                ("assets/step-04-ui/runtime/ui-reduced-motion-off.png", "ui-reduced-motion-off.png"),
                ("assets/step-04-ui/runtime/ui-continue.png", "ui-continue.png"),
                ("assets/step-04-ui/runtime/ui-restart.png", "ui-restart.png"),
                ("assets/step-04-ui/runtime/ui-back.png", "ui-back.png"),
                ("assets/step-04-ui/runtime/ui-caption-backplate-opaque.png", "ui-caption-backplate-opaque.png"),
                ("assets/step-04-ui/runtime/ui-pressed-overlay.png", "ui-pressed-overlay.png"),
                ("production/step-10-voice-captions/visual-equivalents/ve-vo-welcome-001.png", "ve-vo-welcome-001.png"),
                ("production/step-10-voice-captions/visual-equivalents/ve-vo-welcome-session.png", "ve-vo-welcome-session.png"),
                ("production/step-10-voice-captions/voice/en/vo-welcome-001-en.wav", "vo-welcome-001-en.wav"),
                ("production/step-10-voice-captions/voice/en/vo-welcome-session-en.wav", "vo-welcome-session-en.wav"),
                ("production/step-11-audio/music/mus-main-journey-loop.wav", "mus-main-journey-loop.wav"),
                ("production/step-11-audio/ambience/amb-nile-day-loop.wav", "amb-nile-day-loop.wav"),
            };

            foreach (var item in items)
            {
                yield return (
                    Path.Combine(workspace, item.Item1),
                    Path.Combine(destination, item.Item2));
            }
        }

        private static string Hash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                return System.BitConverter.ToString(sha.ComputeHash(stream))
                    .Replace("-", string.Empty);
            }
        }

        private static void AssertTexture(string runtimePath, int maxSize, bool alpha)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Exloris/Content/Imported/" + runtimePath);
            Assert.That(importer, Is.Not.Null, runtimePath);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), runtimePath);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), runtimePath);
            Assert.That(importer.sRGBTexture, Is.True, runtimePath);
            Assert.That(importer.alphaIsTransparency, Is.EqualTo(alpha), runtimePath);
            Assert.That(importer.mipmapEnabled, Is.False, runtimePath);
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), runtimePath);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), runtimePath);
            Assert.That(importer.GetDefaultPlatformTextureSettings().maxTextureSize, Is.EqualTo(maxSize), runtimePath);
        }

        private static void AssertAudio(string runtimePath, AudioClipLoadType loadType, bool forceMono, bool preload)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath("Assets/Exloris/Content/Imported/" + runtimePath);
            Assert.That(importer, Is.Not.Null, runtimePath);
            Assert.That(importer.forceToMono, Is.EqualTo(forceMono), runtimePath);
            Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(loadType), runtimePath);
            Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.Vorbis), runtimePath);
            Assert.That(importer.defaultSampleSettings.sampleRateSetting, Is.EqualTo(AudioSampleRateSetting.PreserveSampleRate), runtimePath);
            Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.EqualTo(preload), runtimePath);
        }
    }
}
