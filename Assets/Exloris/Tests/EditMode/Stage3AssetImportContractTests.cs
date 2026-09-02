using System;
using System.IO;
using System.Linq;
using Exloris.Content.AssetCatalog;
using Exloris.Editor;
using Exloris.Editor.AssetImport;
using Exloris.Runtime.Stage3;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage3AssetImportContractTests
    {
        [Test]
        public void CatalogueMatchesEveryFrozenStage3LedgerRow()
        {
            var ledger = Stage3ContentContractTests.Load();
            var catalogue = AssetDatabase.LoadAssetAtPath<FinalAssetCatalogue>(FinalAssetInventoryImporter.CatalogueAssetPath);
            Assert.That(catalogue, Is.Not.Null);
            var ids = ledger.assets.Select(row => row.asset_id).ToArray();
            var actual = catalogue.Entries.Where(entry => ids.Contains(entry.AssetId)).ToArray();
            Assert.That(actual, Has.Length.EqualTo(24));

            foreach (var row in ledger.assets)
            {
                var entry = actual.Single(item => item.AssetId == row.asset_id);
                Assert.That(entry.State, Is.EqualTo("Final"), row.asset_id);
                Assert.That(entry.IsActive, Is.True, row.asset_id);
                Assert.That(entry.IsPlaceholder, Is.False, row.asset_id);
                Assert.That(entry.SourcePath, Is.EqualTo(row.source_path), row.asset_id);
                Assert.That(entry.Bytes, Is.EqualTo(row.bytes), row.asset_id);
                Assert.That(entry.Sha256, Is.EqualTo(row.sha256), row.asset_id);
                Assert.That(entry.ProfileId, Is.EqualTo(row.profile_id), row.asset_id);
                Assert.That(entry.Pivot, Is.EqualTo(row.pivot), row.asset_id);
                Assert.That(entry.ProvenanceLicenceOwner, Is.Not.Empty, row.asset_id);
                Assert.That(entry.RuntimePath.IndexOf("hub", StringComparison.OrdinalIgnoreCase), Is.LessThan(0), row.asset_id);
            }
        }

        [Test]
        public void ImportedFolderAndContentContainExactlyThirtyNineUniqueApprovedReferences()
        {
            var names = Directory.GetFiles(Stage3ContentGenerator.ImportedRoot)
                .Where(path => !path.EndsWith(".meta", StringComparison.Ordinal))
                .Select(Path.GetFileName)
                .ToArray();
            Stage3ContentGenerator.ValidateFileSet(names);
            Assert.That(names, Has.Length.EqualTo(39));
            Assert.That(names.Any(name => name.StartsWith("out-low-", StringComparison.OrdinalIgnoreCase)), Is.False);

            var content = Stage3Content.LoadApproved();
            Assert.That(content.Backgrounds, Has.Length.EqualTo(6));
            Assert.That(content.Plans, Has.Length.EqualTo(3));
            Assert.That(content.OutcomeLayers, Has.Length.EqualTo(6));
            Assert.That(content.ChildSprites, Has.Length.EqualTo(2));
            Assert.That(content.Controls, Has.Length.EqualTo(10));
            Assert.That(content.VisualEquivalents, Has.Length.EqualTo(4));
            Assert.That(content.PresentationSprites, Has.Length.EqualTo(2));
            Assert.That(content.VoiceClips, Has.Length.EqualTo(2));
            Assert.That(content.AllReferences, Has.Length.EqualTo(39));
            Assert.That(content.AllReferences.Distinct().Count(), Is.EqualTo(39));
            Assert.That(content.AllReferences.Any(item => item == null), Is.False);
        }

        [Test]
        public void FilteredCaptionAssetContainsOnlyExactStage3Cues()
        {
            var content = Stage3Content.LoadApproved();
            var wrapper = JsonUtility.FromJson<CaptionWrapper>("{\"items\":" + content.CaptionTimings.text + "}");
            Assert.That(wrapper, Is.Not.Null);
            Assert.That(wrapper.items, Has.Length.EqualTo(2));
            CollectionAssert.AreEqual(new[] { "VO-S3-INTRO", "VO-S3-OUTCOME" }, wrapper.items.Select(item => item.string_id).ToArray());
            CollectionAssert.AreEqual(new[] { 8032, 6735 }, wrapper.items.Select(item => item.duration_ms).ToArray());
            CollectionAssert.AreEqual(new[] { 0, 3922 }, wrapper.items[0].cues.Select(cue => cue.start_ms).ToArray());
            CollectionAssert.AreEqual(new[] { 3772, 8032 }, wrapper.items[0].cues.Select(cue => cue.end_ms).ToArray());
            CollectionAssert.AreEqual(new[] { 0, 3000 }, wrapper.items[1].cues.Select(cue => cue.start_ms).ToArray());
            CollectionAssert.AreEqual(new[] { 2850, 6735 }, wrapper.items[1].cues.Select(cue => cue.end_ms).ToArray());
        }

        [TestCase("BG3", AssetKind.Texture, 4096, false, 6)]
        [TestCase("PROP-S", AssetKind.Texture, 512, true, 8)]
        [TestCase("OUT-LAYER", AssetKind.Texture, 1024, true, 8)]
        [TestCase("VISUAL-EQUIVALENT", AssetKind.Texture, 1024, true, 6)]
        [TestCase("MOT", AssetKind.Data, 0, false, 0)]
        public void Stage3DataAndTextureProfilesAreFrozen(string profileId, AssetKind kind, int maxSize, bool alpha, int astcBlock)
        {
            var policy = AssetImportPolicy.ForProfile(profileId);
            Assert.That(policy.Kind, Is.EqualTo(kind));
            if (kind != AssetKind.Texture) return;
            Assert.That(policy.Srgb, Is.True);
            Assert.That(policy.AlphaIsTransparency, Is.EqualTo(alpha));
            Assert.That(policy.Mipmaps, Is.False);
            Assert.That(policy.WrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(policy.FilterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(policy.MaxTextureSize, Is.EqualTo(maxSize));
            Assert.That(policy.AstcBlockSize, Is.EqualTo(astcBlock));
        }

        [TestCase("AUD-VO", AudioClipLoadType.CompressedInMemory)]
        [TestCase("AUD-MUS", AudioClipLoadType.Streaming)]
        [TestCase("AUD-AMB", AudioClipLoadType.Streaming)]
        public void Stage3AudioProfilesPreserveSamplesAndUseApprovedLoadTypes(string profileId, AudioClipLoadType loadType)
        {
            var policy = AssetImportPolicy.ForProfile(profileId);
            Assert.That(policy.Kind, Is.EqualTo(AssetKind.Audio));
            Assert.That(policy.AudioLoadType, Is.EqualTo(loadType));
            Assert.That(policy.AudioCompressionFormat, Is.EqualTo(AudioCompressionFormat.Vorbis));
            Assert.That(policy.PreserveSampleRate, Is.True);
        }

        [Test]
        public void Stage3CopiesAreStageScopedAndNeverFallThroughToTheGlobalCatalogueGuard()
        {
            Assert.That(AssetImportGuard.IsManagedAssetPath(Stage3ContentGenerator.ImportedRoot + "/out-equal-grain-4x3.png"), Is.False);
            Assert.That(AssetImportGuard.IsManagedAssetPath("Assets/Exloris/Content/Imported/out-equal-grain-4x3.png"), Is.True);
        }

        [Test]
        public void AlteredDuplicateReviewAndWrongHashLedgerRowsFailBeforeImport()
        {
            var ledger = Stage3ContentGenerator.ReadLedger();
            Assert.DoesNotThrow(() => Stage3ContentGenerator.ValidateLedger(ledger));

            var first = ledger.assets[0];
            var invalid = new Stage3ContentGenerator.AssetLedger
            {
                status = "PASS",
                assets = ledger.assets.Select(row => new Stage3ContentGenerator.AssetRow
                {
                    source_path = row.source_path,
                    runtime_name = row.runtime_name,
                    bytes = row.bytes,
                    sha256 = row.sha256,
                }).ToArray(),
            };
            invalid.assets[0].sha256 = "wrong";
            Assert.Throws<InvalidOperationException>(() => Stage3ContentGenerator.ValidateLedger(invalid));
            invalid.assets[0].sha256 = first.sha256;
            invalid.assets[1].runtime_name = invalid.assets[0].runtime_name;
            Assert.Throws<InvalidOperationException>(() => Stage3ContentGenerator.ValidateLedger(invalid));
            invalid.assets[1].runtime_name = ledger.assets[1].runtime_name;
            invalid.assets[0].source_path = "assets/step-07-gameplay-art/review/outcomes/stage3/out-low-grain-final-4x3.png";
            Assert.Throws<InvalidOperationException>(() => Stage3ContentGenerator.ValidateLedger(invalid));
        }

        [Serializable] private sealed class CaptionWrapper { public CaptionItem[] items; }
        [Serializable] private sealed class CaptionItem { public string string_id; public int duration_ms; public CaptionCue[] cues; }
        [Serializable] private sealed class CaptionCue { public int start_ms; public int end_ms; }
    }
}
