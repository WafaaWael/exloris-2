using System.IO;
using Exloris.Editor;
using Exloris.Runtime.Stage3;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage3BuildTests
    {
        [Test]
        public void MacBuildUsesStage3OutputAndFrozenSafetyCeilings()
        {
            var root = Path.Combine(Path.GetTempPath(), "Exloris2Stage3Build");
            var options = Stage3Build.CreateMacOptions(root);
            Assert.That(options.target, Is.EqualTo(BuildTarget.StandaloneOSX));
            Assert.That(options.locationPathName,
                Is.EqualTo(Path.Combine(root, "builds/stage-3/macos/Exloris2.app")));
            Assert.That(options.options, Is.EqualTo(BuildOptions.CompressWithLz4HC));
            CollectionAssert.AreEqual(new[]
            {
                "Assets/Exloris/Scenes/Bootstrap.unity",
                "Assets/Exloris/Scenes/FoundationHarness.unity",
            }, options.scenes);
            Assert.That(Stage3Build.MinimumFreeBytes, Is.EqualTo(15L * 1024L * 1024L * 1024L));
            Assert.That(Stage3Build.MaximumInstalledBytes, Is.EqualTo(200_000_000L));
            Assert.That(Stage3Build.MacEvidenceArchitecture, Is.EqualTo("ARM64"));
        }

        [Test]
        public void ApprovedContentValidationRejectsIncompleteStage3Content()
        {
            var approved = AssetDatabase.LoadAssetAtPath<Stage3Content>(Stage3ContentGenerator.ContentAssetPath);
            var incomplete = ScriptableObject.CreateInstance<Stage3Content>();
            try
            {
                Assert.That(Stage3Build.HasApprovedContent(approved), Is.True);
                Assert.That(Stage3Build.HasApprovedContent(incomplete), Is.False);
                Assert.That(Stage3Build.HasApprovedContent(null), Is.False);
            }
            finally { Object.DestroyImmediate(incomplete); }
        }

        [Test]
        public void PackagingRemovesOnlyManagedDebugSymbols()
        {
            var root = Path.Combine(Path.GetTempPath(), "Exloris2Stage3Symbols", System.Guid.NewGuid().ToString("N"));
            var managed = Path.Combine(root, "Contents", "Resources", "Data", "Managed");
            var other = Path.Combine(root, "Contents", "Resources");
            Directory.CreateDirectory(managed);
            File.WriteAllBytes(Path.Combine(managed, "runtime.pdb"), new byte[13]);
            File.WriteAllBytes(Path.Combine(managed, "runtime.dll"), new byte[17]);
            File.WriteAllBytes(Path.Combine(other, "keep.pdb"), new byte[19]);
            try
            {
                Assert.That(Stage3Build.RemoveManagedDebugSymbols(root), Is.EqualTo(13));
                Assert.That(File.Exists(Path.Combine(managed, "runtime.pdb")), Is.False);
                Assert.That(File.Exists(Path.Combine(managed, "runtime.dll")), Is.True);
                Assert.That(File.Exists(Path.Combine(other, "keep.pdb")), Is.True);
            }
            finally { Directory.Delete(root, true); }
        }
    }
}
