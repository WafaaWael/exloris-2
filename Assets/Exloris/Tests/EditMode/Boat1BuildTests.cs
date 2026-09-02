using System.IO;
using Exloris.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat1BuildTests
    {
        [Test]
        public void MacBuildUsesBoatOutputAndFrozenSafetyCeilings()
        {
            var root = Path.Combine(Path.GetTempPath(), "Exloris2Boat1Build");
            var options = Boat1Build.CreateMacOptions(root);

            Assert.That(options.target, Is.EqualTo(BuildTarget.StandaloneOSX));
            Assert.That(options.locationPathName,
                Is.EqualTo(Path.Combine(root, "builds/boat-1/macos/Exloris2.app")));
            Assert.That(options.options, Is.EqualTo(BuildOptions.CompressWithLz4HC));
            CollectionAssert.AreEqual(new[]
            {
                "Assets/Exloris/Scenes/Bootstrap.unity",
                "Assets/Exloris/Scenes/FoundationHarness.unity"
            }, options.scenes);
            Assert.That(Boat1Build.MinimumFreeBytes, Is.EqualTo(15L * 1024L * 1024L * 1024L));
            Assert.That(Boat1Build.MaximumInstalledBytes, Is.EqualTo(200_000_000L));
            Assert.That(Boat1Build.MacEvidenceArchitecture, Is.EqualTo("ARM64"));
        }

        [Test]
        public void BuildRequiresApprovedContentAndLocalPrivacyValidation()
        {
            var source = File.ReadAllText(Path.Combine(
                UnityEngine.Application.dataPath, "Exloris", "Editor", "Boat1Build.cs"));
            Assert.That(source, Does.Contain("ValidateBoat1Content"));
            Assert.That(source, Does.Contain("FoundationPrivacyValidator.ValidateBuildArtifact"));
            Assert.That(source, Does.Contain("StartupIntroBuild.ValidateRuntimeMediaSet"));
            Assert.That(source, Does.Not.Contain("Development"));
            Assert.That(source, Does.Not.Contain("ConnectWithProfiler"));
        }
    }
}
