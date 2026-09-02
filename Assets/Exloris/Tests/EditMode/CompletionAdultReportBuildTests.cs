using Exloris.Editor;
using Exloris.Runtime.Completion;
using NUnit.Framework;
using UnityEditor;

namespace Exloris.Tests.EditMode
{
    public sealed class CompletionAdultReportBuildTests
    {
        [Test]
        public void MacBuildUsesFinalArm64Lz4HcAndApprovedContent()
        {
            var options = CompletionAdultReportBuild.CreateMacOptions("/tmp/exloris-closing");
            Assert.That(options.locationPathName, Does.EndWith("builds/completion-adult-report/macos/Exloris2.app"));
            Assert.That(options.options, Is.EqualTo(BuildOptions.CompressWithLz4HC));
            Assert.That(CompletionAdultReportBuild.MacEvidenceArchitecture, Is.EqualTo("ARM64"));
            Assert.That(CompletionAdultReportBuild.MaximumInstalledBytes, Is.EqualTo(200_000_000L));
            var content = AssetDatabase.LoadAssetAtPath<CompletionAdultReportContent>(
                CompletionAdultReportContentGenerator.ContentAssetPath);
            Assert.That(CompletionAdultReportBuild.HasApprovedContent(content), Is.True);
        }

        [Test]
        public void FinalBuildContainsOnlyTheApprovedBootstrapAndHarnessScenes()
        {
            var options = CompletionAdultReportBuild.CreateMacOptions("/tmp/exloris-closing");
            Assert.That(options.scenes, Is.EqualTo(new[]
            {
                "Assets/Exloris/Scenes/Bootstrap.unity",
                "Assets/Exloris/Scenes/FoundationHarness.unity",
            }));
        }
    }
}
