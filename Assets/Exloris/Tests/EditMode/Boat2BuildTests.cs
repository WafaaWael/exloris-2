using System.IO;
using Exloris.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat2BuildTests
    {
        [Test]
        public void MacBuildUsesBoatOutputAndFrozenSafetyCeilings()
        {
            var root = Path.Combine(Path.GetTempPath(), "Exloris2Boat2Build");
            var options = Boat2Build.CreateMacOptions(root);

            Assert.That(options.target, Is.EqualTo(BuildTarget.StandaloneOSX));
            Assert.That(options.locationPathName,
                Is.EqualTo(Path.Combine(root, "builds/boat-2/macos/Exloris2.app")));
            Assert.That(options.options, Is.EqualTo(BuildOptions.CompressWithLz4HC));
            CollectionAssert.AreEqual(new[]
            {
                "Assets/Exloris/Scenes/Bootstrap.unity",
                "Assets/Exloris/Scenes/FoundationHarness.unity"
            }, options.scenes);
            Assert.That(Boat2Build.MinimumFreeBytes, Is.EqualTo(15L * 1024L * 1024L * 1024L));
            Assert.That(Boat2Build.MaximumInstalledBytes, Is.EqualTo(200_000_000L));
            Assert.That(Boat2Build.MacEvidenceArchitecture, Is.EqualTo("ARM64"));
        }

        [Test]
        public void BuildRequiresApprovedContentAndLocalPrivacyValidation()
        {
            var source = File.ReadAllText(Path.Combine(
                UnityEngine.Application.dataPath, "Exloris", "Editor", "Boat2Build.cs"));
            Assert.That(source, Does.Contain("ValidateBoat2Content"));
            Assert.That(source, Does.Contain("FoundationPrivacyValidator.ValidateBuildArtifact"));
            Assert.That(source, Does.Contain("StartupIntroBuild.ValidateRuntimeMediaSet"));
            Assert.That(source, Does.Not.Contain("Development"));
            Assert.That(source, Does.Not.Contain("ConnectWithProfiler"));
        }
    }
}
