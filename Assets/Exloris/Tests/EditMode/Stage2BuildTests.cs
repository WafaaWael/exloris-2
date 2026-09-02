using System.IO;
using Exloris.Editor;
using Exloris.Runtime.Stage2;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage2BuildTests
    {
        [Test]
        public void MacBuildUsesStage2OutputAndFrozenSafetyCeilings()
        {
            var root = Path.Combine(Path.GetTempPath(), "Exloris2Stage2Build");
            var options = Stage2Build.CreateMacOptions(root);

            Assert.That(options.target, Is.EqualTo(BuildTarget.StandaloneOSX));
            Assert.That(options.locationPathName,
                Is.EqualTo(Path.Combine(root, "builds/stage-2/macos/Exloris2.app")));
            Assert.That(options.options, Is.EqualTo(BuildOptions.CompressWithLz4HC));
            CollectionAssert.AreEqual(new[]
            {
                "Assets/Exloris/Scenes/Bootstrap.unity",
                "Assets/Exloris/Scenes/FoundationHarness.unity",
            }, options.scenes);
            Assert.That(Stage2Build.MinimumFreeBytes, Is.EqualTo(15L * 1024L * 1024L * 1024L));
            Assert.That(Stage2Build.MaximumInstalledBytes, Is.EqualTo(200_000_000L));
            Assert.That(Stage2Build.MacEvidenceArchitecture, Is.EqualTo("ARM64"));
        }

        [Test]
        public void ApprovedContentValidationRejectsIncompleteStage2Content()
        {
            var approved = AssetDatabase.LoadAssetAtPath<Stage2Content>(
                "Assets/Exloris/Resources/Content/Stage2Content.asset");
            var incomplete = ScriptableObject.CreateInstance<Stage2Content>();
            try
            {
                Assert.That(Stage2Build.HasApprovedContent(approved), Is.True);
                Assert.That(Stage2Build.HasApprovedContent(incomplete), Is.False);
                Assert.That(Stage2Build.HasApprovedContent(null), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(incomplete);
            }
        }
    }
}
