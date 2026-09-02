using System.IO;
using Exloris.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Exloris.Tests.EditMode
{
    public sealed class AvatarBuildTests
    {
        [Test]
        public void MacBuildUsesAvatarOutputAndFrozenSafetyCeilings()
        {
            var root = Path.Combine(Path.GetTempPath(), "Exloris2AvatarBuild");

            var options = AvatarBuild.CreateMacOptions(root);

            Assert.That(options.target, Is.EqualTo(BuildTarget.StandaloneOSX));
            Assert.That(options.locationPathName,
                Is.EqualTo(Path.Combine(root, "builds/avatar-select-preview/macos/Exloris2.app")));
            Assert.That(options.options, Is.EqualTo(BuildOptions.None));
            CollectionAssert.AreEqual(new[]
            {
                "Assets/Exloris/Scenes/Bootstrap.unity",
                "Assets/Exloris/Scenes/FoundationHarness.unity"
            }, options.scenes);
            Assert.That(AvatarBuild.MinimumFreeBytes,
                Is.EqualTo(15L * 1024L * 1024L * 1024L));
            Assert.That(AvatarBuild.MaximumInstalledBytes, Is.EqualTo(200_000_000L));
            Assert.That(AvatarBuild.MacEvidenceArchitecture, Is.EqualTo("ARM64"),
                "The local evidence player is ARM64-only; shipping mobile builds have separate gates.");
        }
    }
}
