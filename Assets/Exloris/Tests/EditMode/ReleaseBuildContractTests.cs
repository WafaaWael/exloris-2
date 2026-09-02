using System.IO;
using System.Linq;
using Exloris.Editor.Build;
using Exloris.Editor.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class ReleaseBuildContractTests
    {
        [Test]
        public void ReleaseBuildUsesTwoApprovedScenesArm64Lz4HcAndFinalOutput()
        {
            var options = ReleaseEvidenceBuilder.CreateMacOptions("/tmp/exloris2-release");
            Assert.That(options.locationPathName,
                Does.EndWith("builds/release-candidate/macos/Exloris2.app"));
            Assert.That(options.options, Is.EqualTo(BuildOptions.CompressWithLz4HC));
            Assert.That(options.scenes, Is.EqualTo(new[]
            {
                "Assets/Exloris/Scenes/Bootstrap.unity",
                "Assets/Exloris/Scenes/FoundationHarness.unity",
            }));
            Assert.That(ReleaseEvidenceBuilder.MacArchitecture, Is.EqualTo("ARM64"));
            Assert.That(ReleaseEvidenceBuilder.MaximumInstalledBytes, Is.EqualTo(200_000_000L));
        }

        [Test]
        public void WorkspaceReconcilesAllRowsAssetsFailuresAndDirectRoute()
        {
            var result = AcceptanceMatrixValidator.ValidateWorkspace(WorkspaceRoot(), false);
            Assert.That(result.MatrixRows, Is.EqualTo(592));
            Assert.That(result.LedgerRows, Is.EqualTo(592));
            Assert.That(result.ShippingAssets, Is.EqualTo(253));
            Assert.That(result.KnownFailures, Is.EqualTo(18));
            Assert.That(result.RouteScreens, Is.EqualTo(15));
        }

        [Test]
        public void MissingDuplicateForbiddenRouteAndMissingEvidenceFailClosed()
        {
            var snapshot = AcceptanceMatrixValidator.LoadSnapshot(WorkspaceRoot());

            var missing = snapshot.Clone();
            missing.LedgerItemIds = missing.LedgerItemIds.Skip(1).ToArray();
            Assert.Throws<InvalidDataException>(() =>
                AcceptanceMatrixValidator.ValidateSnapshot(missing, false, _ => true));

            var duplicate = snapshot.Clone();
            duplicate.LedgerTestIds = duplicate.LedgerTestIds
                .Concat(new[] { duplicate.LedgerTestIds[0] }).ToArray();
            Assert.Throws<InvalidDataException>(() =>
                AcceptanceMatrixValidator.ValidateSnapshot(duplicate, false, _ => true));

            var forbidden = snapshot.Clone();
            forbidden.Route = forbidden.Route.Concat(new[] { "hub" }).ToArray();
            Assert.Throws<InvalidDataException>(() =>
                AcceptanceMatrixValidator.ValidateSnapshot(forbidden, false, _ => true));

            Assert.Throws<FileNotFoundException>(() =>
                AcceptanceMatrixValidator.ValidateSnapshot(snapshot, true, _ => false));
        }

        [TestCase("Assets/Exloris/Editor/Build/ReleaseEvidenceBuilder.cs")]
        [TestCase("Assets/Exloris/Editor/Stage3Build.cs")]
        [TestCase("Assets/Exloris/Editor/CompletionAdultReportBuild.cs")]
        public void MacBuildersResealAfterRemovingManagedSymbols(string assetPath)
        {
            var source = File.ReadAllText(Path.Combine(
                Path.GetFullPath(Path.Combine(Application.dataPath, "..")), assetPath));
            var removal = source.IndexOf("RemoveManagedDebugSymbols", System.StringComparison.Ordinal);
            var reseal = source.IndexOf("ReleaseEvidenceBuilder.ResealAdHoc", System.StringComparison.Ordinal);
            if (assetPath.EndsWith("ReleaseEvidenceBuilder.cs", System.StringComparison.Ordinal))
                reseal = source.IndexOf("ResealAdHoc(options.locationPathName)", System.StringComparison.Ordinal);
            var validation = source.IndexOf(
                "FoundationPrivacyValidator.ValidateBuildArtifact", System.StringComparison.Ordinal);

            Assert.That(removal, Is.GreaterThanOrEqualTo(0));
            Assert.That(reseal, Is.GreaterThan(removal));
            Assert.That(validation, Is.GreaterThan(reseal));
        }

        private static string WorkspaceRoot() =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
    }
}
