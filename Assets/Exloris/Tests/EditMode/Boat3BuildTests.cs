using System.IO;
using Exloris.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat3BuildTests
    {
        [Test]
        public void MacBuildUsesBoatOutputAndFrozenSafetyCeilings()
        {
            var root = Path.Combine(Path.GetTempPath(), "Exloris2Boat3Build");
            var options = Boat3Build.CreateMacOptions(root);

            Assert.That(options.target, Is.EqualTo(BuildTarget.StandaloneOSX));
            Assert.That(options.locationPathName,
                Is.EqualTo(Path.Combine(root, "builds/boat-3/macos/Exloris2.app")));
            Assert.That(options.options, Is.EqualTo(BuildOptions.CompressWithLz4HC));
            CollectionAssert.AreEqual(new[]
            {
                "Assets/Exloris/Scenes/Bootstrap.unity",
                "Assets/Exloris/Scenes/FoundationHarness.unity"
            }, options.scenes);
            Assert.That(Boat3Build.MinimumFreeBytes, Is.EqualTo(15L * 1024L * 1024L * 1024L));
            Assert.That(Boat3Build.MaximumInstalledBytes, Is.EqualTo(200_000_000L));
            Assert.That(Boat3Build.MacEvidenceArchitecture, Is.EqualTo("ARM64"));
        }

        [Test]
        public void BuildRequiresApprovedContentAndLocalPrivacyValidation()
        {
            var source = File.ReadAllText(Path.Combine(
                UnityEngine.Application.dataPath, "Exloris", "Editor", "Boat3Build.cs"));
            Assert.That(source, Does.Contain("ValidateBoat3Content"));
            Assert.That(source, Does.Contain("FoundationPrivacyValidator.ValidateBuildArtifact"));
            Assert.That(source, Does.Contain("StartupIntroBuild.ValidateRuntimeMediaSet"));
            Assert.That(source, Does.Contain("SetManagedStrippingLevel"));
            Assert.That(source, Does.Contain("ManagedStrippingLevel.High"));
            Assert.That(source, Does.Contain("stripEngineCode"));
            Assert.That(source, Does.Not.Contain("ApplyBoat2StandaloneCompression"));
            Assert.That(source, Does.Not.Contain("Development"));
            Assert.That(source, Does.Not.Contain("ConnectWithProfiler"));
        }
    }
}
