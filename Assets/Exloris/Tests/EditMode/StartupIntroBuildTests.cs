using System.IO;
using System.Linq;
using Exloris.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Exloris.Tests.EditMode
{
    public sealed class StartupIntroBuildTests
    {
        [Test]
        public void MacBuildUsesActiveStageNvmeLinkedOutputWithoutCleaningSharedLibrary()
        {
            var root = Path.Combine(Path.GetTempPath(), "Exloris2StartupIntroBuild");

            var options = StartupIntroBuild.CreateMacOptions(root);

            Assert.That(options.target, Is.EqualTo(BuildTarget.StandaloneOSX));
            Assert.That(
                options.locationPathName,
                Is.EqualTo(Path.Combine(root, "builds/startup-intro/macos/Exloris2.app")));
            Assert.That(options.options, Is.EqualTo(BuildOptions.None));
            CollectionAssert.AreEqual(
                new[]
                {
                    "Assets/Exloris/Scenes/Bootstrap.unity",
                    "Assets/Exloris/Scenes/FoundationHarness.unity"
                },
                options.scenes);
        }

        [Test]
        public void RuntimeMediaGuardAcceptsOnlyTheExactEightFileSet()
        {
            var directory = CreateTemporaryMediaDirectory();
            try
            {
                foreach (var fileName in StartupIntroBuild.ApprovedRuntimeVideoFileNames)
                {
                    File.WriteAllBytes(Path.Combine(directory, fileName), new byte[] { 1 });
                }

                Assert.DoesNotThrow(() => StartupIntroBuild.ValidateRuntimeMediaSet(directory));
                File.WriteAllBytes(Path.Combine(directory, "intro-preview-16x9.mp4"), new byte[] { 1 });
                Assert.Throws<System.InvalidOperationException>(() =>
                    StartupIntroBuild.ValidateRuntimeMediaSet(directory));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void RuntimeMediaGuardRejectsPngSequencesAndMissingVideos()
        {
            var directory = CreateTemporaryMediaDirectory();
            try
            {
                foreach (var fileName in StartupIntroBuild.ApprovedRuntimeVideoFileNames.Skip(1))
                {
                    File.WriteAllBytes(Path.Combine(directory, fileName), new byte[] { 1 });
                }
                File.WriteAllBytes(Path.Combine(directory, "intro-0001.png"), new byte[] { 1 });

                Assert.Throws<System.InvalidOperationException>(() =>
                    StartupIntroBuild.ValidateRuntimeMediaSet(directory));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void BuildGuardsFreezeDiskInstalledSizeAndContinuousFrameCadence()
        {
            Assert.That(StartupIntroBuild.MinimumFreeBytes, Is.EqualTo(15L * 1024L * 1024L * 1024L));
            Assert.DoesNotThrow(() => StartupIntroBuild.EnsureInstalledSizeBelowCeiling(199_999_999L));
            Assert.Throws<System.InvalidOperationException>(() =>
                StartupIntroBuild.EnsureInstalledSizeBelowCeiling(200_000_000L));
            Assert.DoesNotThrow(() => StartupIntroBuild.ValidateMotionCadence(
                reducedMotion: false,
                frameRate: 30,
                frameCount: 288,
                uniqueFrameCount: 288,
                longestIdenticalRun: 1));
            Assert.Throws<System.InvalidOperationException>(() => StartupIntroBuild.ValidateMotionCadence(
                reducedMotion: false,
                frameRate: 30,
                frameCount: 288,
                uniqueFrameCount: 48,
                longestIdenticalRun: 6));
            Assert.DoesNotThrow(() => StartupIntroBuild.ValidateMotionCadence(
                reducedMotion: true,
                frameRate: 30,
                frameCount: 120,
                uniqueFrameCount: 111,
                longestIdenticalRun: 10));
        }

        private static string CreateTemporaryMediaDirectory()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "Exloris2IntroMedia-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }
    }
}
