using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class StartupIntroContentContractTests
    {
        [Test]
        public void RuntimeContentContainsExactApprovedIntroMediaAndAccessibleLayers()
        {
            foreach (var item in RequiredContent())
            {
                Assert.That(File.Exists(item.destination), Is.True, "Missing runtime content: " + item.destination);
                Assert.That(Hash(item.destination), Is.EqualTo(Hash(item.source)), "Runtime copy changed: " + item.destination);
            }
        }

        [Test]
        public void RuntimeIntroDirectoryContainsOnlyTheEightApprovedVideos()
        {
            var directory = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets/StreamingAssets/Exloris/Intro");
            var runtimeFiles = Directory.GetFiles(directory)
                .Where(path => !path.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileName)
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();
            var expected = new[]
            {
                "intro-16x9-low.mp4", "intro-16x9.mp4", "intro-24x10.mp4", "intro-4x3.mp4",
                "intro-reduced-16x9-low.mp4", "intro-reduced-16x9.mp4",
                "intro-reduced-24x10.mp4", "intro-reduced-4x3.mp4"
            };

            CollectionAssert.AreEqual(expected, runtimeFiles);
            Assert.That(runtimeFiles, Has.None.Matches<string>(file => file.StartsWith("intro-preview-")));
            Assert.That(runtimeFiles, Has.None.Matches<string>(file =>
                string.Equals(Path.GetExtension(file), ".png", System.StringComparison.OrdinalIgnoreCase)));
        }

        private static IEnumerable<(string source, string destination)> RequiredContent()
        {
            var workspace = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            var assets = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
            var videos = new[]
            {
                "intro-4x3.mp4", "intro-16x9.mp4", "intro-16x9-low.mp4", "intro-24x10.mp4",
                "intro-reduced-4x3.mp4", "intro-reduced-16x9.mp4",
                "intro-reduced-16x9-low.mp4", "intro-reduced-24x10.mp4"
            };
            foreach (var file in videos)
            {
                yield return (
                    Path.Combine(workspace, "production/step-08-intro/renders", file),
                    Path.Combine(assets, "StreamingAssets/Exloris/Intro", file));
            }

            var imports = new[]
            {
                ("assets/step-04-ui/runtime/ui-skip.png", "ui/ui-skip.png"),
                ("assets/step-04-ui/runtime/ui-resume.png", "ui/ui-resume.png"),
                ("assets/step-04-ui/runtime/ui-caption-backplate-opaque.png", "ui/ui-caption-backplate-opaque.png"),
                ("assets/step-07-gameplay-art/runtime/ibis-wings-open.png", "gameplay/ibis-wings-open.png"),
                ("assets/step-07-gameplay-art/runtime/ibis-head-lift.png", "gameplay/ibis-head-lift.png"),
                ("assets/step-07-gameplay-art/runtime/ibis-beak-right.png", "gameplay/ibis-beak-right.png"),
                ("assets/step-07-gameplay-art/runtime/ibis-calm-settle.png", "gameplay/ibis-calm-settle.png"),
                ("assets/step-07-gameplay-art/runtime/ibis-static-right-reduced.png", "gameplay/ibis-static-right-reduced.png"),
                ("production/step-10-voice-captions/voice/en/vo-intro-001-en.wav", "voice/en/vo-intro-001-en.wav"),
                ("production/step-10-voice-captions/visual-equivalents/ve-vo-intro-001.png", "visual-equivalents/ve-vo-intro-001.png"),
                ("production/step-10-voice-captions/captions/caption-timings-en-v01.json", "captions/caption-timings-en-v01.json")
            };
            foreach (var item in imports)
            {
                yield return (
                    Path.Combine(workspace, item.Item1),
                    Path.Combine(assets, "Exloris/Content/Imported", item.Item2));
            }
        }

        private static string Hash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                return System.BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }
    }
}
