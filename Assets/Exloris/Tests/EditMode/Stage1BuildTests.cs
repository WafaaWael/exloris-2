using System.IO;
using Exloris.Editor;
using NUnit.Framework;
using UnityEditor;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage1BuildTests
    {
        [Test]
        public void MacBuildUsesStage1OutputAndFrozenSafetyCeilings()
        {
            var root = Path.Combine(Path.GetTempPath(), "Exloris2Stage1Build");
            var options = Stage1Build.CreateMacOptions(root);
            Assert.That(options.target, Is.EqualTo(BuildTarget.StandaloneOSX));
            Assert.That(options.locationPathName,
                Is.EqualTo(Path.Combine(root, "builds/stage-1/macos/Exloris2.app")));
            Assert.That(options.options, Is.EqualTo(BuildOptions.CompressWithLz4HC));
            Assert.That(Stage1Build.MinimumFreeBytes, Is.EqualTo(15L * 1024L * 1024L * 1024L));
            Assert.That(Stage1Build.MaximumInstalledBytes, Is.EqualTo(200_000_000L));
            Assert.That(Stage1Build.MacEvidenceArchitecture, Is.EqualTo("ARM64"));
        }

        [Test]
        public void BuildRequiresApprovedStageContentAndLocalPrivacyValidation()
        {
            var source = File.ReadAllText(Path.Combine(
                UnityEngine.Application.dataPath, "Exloris", "Editor", "Stage1Build.cs"));
            Assert.That(source, Does.Contain("ValidateStage1Content"));
            Assert.That(source, Does.Contain("FoundationPrivacyValidator.ValidateBuildArtifact"));
            Assert.That(source, Does.Contain("StartupIntroBuild.ValidateRuntimeMediaSet"));
            Assert.That(source, Does.Not.Contain("Development"));
            Assert.That(source, Does.Not.Contain("ConnectWithProfiler"));
        }
    }
}
