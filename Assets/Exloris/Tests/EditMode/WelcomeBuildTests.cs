using System.IO;
using Exloris.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Exloris.Tests.EditMode
{
    public sealed class WelcomeBuildTests
    {
        [Test]
        public void MacBuildUsesWelcomeOutputWithoutCleaningTheSharedLibrary()
        {
            var root = Path.Combine(Path.GetTempPath(), "Exloris2WelcomeBuild");

            var options = WelcomeBuild.CreateMacOptions(root);

            Assert.That(options.target, Is.EqualTo(BuildTarget.StandaloneOSX));
            Assert.That(options.locationPathName,
                Is.EqualTo(Path.Combine(root, "builds/welcome/macos/Exloris2.app")));
            Assert.That(options.options, Is.EqualTo(BuildOptions.None));
            CollectionAssert.AreEqual(new[]
            {
                "Assets/Exloris/Scenes/Bootstrap.unity",
                "Assets/Exloris/Scenes/FoundationHarness.unity"
            }, options.scenes);
            Assert.That(WelcomeBuild.MinimumFreeBytes,
                Is.EqualTo(15L * 1024L * 1024L * 1024L));
            Assert.That(WelcomeBuild.MaximumInstalledBytes, Is.EqualTo(200_000_000L));
        }
    }
}
