using System;
using System.Linq;
using Exloris.Content.AssetCatalog;
using Exloris.Editor;
using Exloris.Editor.AssetImport;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage2AssetImportContractTests
    {
        [Test]
        public void CatalogueMatchesEveryFrozenStage2LedgerRow()
        {
            var ledger = Stage2ContentContractTests.Load();
            var catalogue = AssetDatabase.LoadAssetAtPath<FinalAssetCatalogue>(FinalAssetInventoryImporter.CatalogueAssetPath);
            Assert.That(catalogue, Is.Not.Null);
            var ids = ledger.assets.Select(row => row.asset_id).ToArray();
            var actual = catalogue.Entries.Where(entry => ids.Contains(entry.AssetId)).ToArray();
            Assert.That(actual, Has.Length.EqualTo(34));

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

        [TestCase("BG3", AssetKind.Texture, 4096, false, 6)]
        [TestCase("PROP-S", AssetKind.Texture, 512, true, 8)]
        [TestCase("OUT-LAYER", AssetKind.Texture, 1024, true, 8)]
        [TestCase("VISUAL-EQUIVALENT", AssetKind.Texture, 1024, true, 6)]
        [TestCase("MOT", AssetKind.Data, 0, false, 0)]
        public void DataAndTextureProfilesAreFrozen(string profileId, AssetKind kind, int maxSize, bool alpha, int astcBlock)
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

        [TestCase("AUD-VO", AudioClipLoadType.CompressedInMemory, AudioCompressionFormat.Vorbis)]
        [TestCase("AUD-MUS", AudioClipLoadType.Streaming, AudioCompressionFormat.Vorbis)]
        [TestCase("AUD-AMB", AudioClipLoadType.Streaming, AudioCompressionFormat.Vorbis)]
        [TestCase("AUD-SFX", AudioClipLoadType.DecompressOnLoad, AudioCompressionFormat.ADPCM)]
        public void AudioProfilesAreFrozen(string profileId, AudioClipLoadType loadType, AudioCompressionFormat compression)
        {
            var policy = AssetImportPolicy.ForProfile(profileId);
            Assert.That(policy.Kind, Is.EqualTo(AssetKind.Audio));
            Assert.That(policy.AudioLoadType, Is.EqualTo(loadType));
            Assert.That(policy.AudioCompressionFormat, Is.EqualTo(compression));
            Assert.That(policy.PreserveSampleRate, Is.True);
        }

        [Test]
        public void Stage2TexturesUseHighQualityStandaloneCrunchWithoutChangingSourceDimensions()
        {
            foreach (var row in Stage2ContentContractTests.Load().assets
                         .Where(item => item.runtime_name.EndsWith(".png", StringComparison.Ordinal)))
            {
                var name = System.IO.Path.GetFileName(row.source_path);
                var path = Stage2ContentGenerator.ImportedRoot + "/" + name;
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer, Is.Not.Null, name);

                var standalone = importer.GetPlatformTextureSettings("Standalone");
                Assert.That(standalone.overridden, Is.True, name);
                Assert.That(standalone.crunchedCompression, Is.True, name);
                Assert.That(standalone.compressionQuality, Is.EqualTo(100), name);
                Assert.That(standalone.maxTextureSize,
                    Is.EqualTo(AssetImportPolicy.ForProfile(row.profile_id).MaxTextureSize), name);
            }
        }
    }
}
