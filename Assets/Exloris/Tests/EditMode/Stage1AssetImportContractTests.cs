using System;
using System.IO;
using System.Linq;
using Exloris.Content.AssetCatalog;
using Exloris.Editor;
using Exloris.Editor.AssetImport;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage1AssetImportContractTests
    {
        [Test]
        public void CatalogueContainsExactlyTheTwentyFourFinalShipStage1Rows()
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<FinalAssetCatalogue>(FinalAssetInventoryImporter.CatalogueAssetPath);
            Assert.That(catalogue, Is.Not.Null);
            var expected = Stage1AssetContractFixture.Rows.OrderBy(row => row.AssetId, StringComparer.Ordinal).ToArray();
            var ids = expected.Select(row => row.AssetId).ToArray();
            var actual = catalogue.Entries.Where(entry => ids.Contains(entry.AssetId)).OrderBy(entry => entry.AssetId, StringComparer.Ordinal).ToArray();
            Assert.That(actual, Has.Length.EqualTo(24));

            for (var index = 0; index < expected.Length; index++)
            {
                Assert.That(actual[index].State, Is.EqualTo("Final"), expected[index].AssetId);
                Assert.That(actual[index].IsActive, Is.True, expected[index].AssetId);
                Assert.That(actual[index].IsPlaceholder, Is.False, expected[index].AssetId);
                Assert.That(actual[index].SourcePath, Is.EqualTo(expected[index].SourcePath), expected[index].AssetId);
                Assert.That(actual[index].Bytes, Is.EqualTo(expected[index].Bytes), expected[index].AssetId);
                Assert.That(actual[index].Sha256, Is.EqualTo(expected[index].Sha256), expected[index].AssetId);
                Assert.That(actual[index].ProfileId, Is.EqualTo(expected[index].ProfileId), expected[index].AssetId);
                Assert.That(actual[index].Pivot, Is.EqualTo(expected[index].Pivot), expected[index].AssetId);
                Assert.That(actual[index].ProvenanceLicenceOwner, Is.Not.Empty, expected[index].AssetId);
                Assert.That(actual[index].RuntimePath.IndexOf("hub", StringComparison.OrdinalIgnoreCase), Is.LessThan(0), expected[index].AssetId);
                Assert.That(actual[index].RuntimePath.IndexOf("replay", StringComparison.OrdinalIgnoreCase), Is.LessThan(0), expected[index].AssetId);
            }
        }

        [TestCase("BG3", 4096, false, 6)]
        [TestCase("PROP-L", 1024, true, 8)]
        [TestCase("OUT-LAYER", 1024, true, 8)]
        [TestCase("VISUAL-EQUIVALENT", 1024, true, 6)]
        public void TextureProfilesAreFrozen(string profileId, int maxSize, bool alpha, int astcBlock)
        {
            var policy = AssetImportPolicy.ForProfile(profileId);
            Assert.That(policy.Kind, Is.EqualTo(AssetKind.Texture));
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
        public void StageScopedCopiesUseExactFrozenTextureAndAudioImporters()
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<FinalAssetCatalogue>(FinalAssetInventoryImporter.CatalogueAssetPath);
            var files = Directory.GetFiles(Stage1ContentGenerator.ImportedRoot)
                .Where(path => !path.EndsWith(".meta", StringComparison.Ordinal)).ToArray();
            Assert.That(files, Has.Length.EqualTo(39));

            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                var entry = catalogue.Entries.Single(item => Path.GetFileName(item.SourcePath) == name);
                var policy = AssetImportPolicy.ForProfile(entry.ProfileId);
                var assetPath = Stage1ContentGenerator.ImportedRoot + "/" + name;
                if (policy.Kind == AssetKind.Texture)
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                    Assert.That(importer, Is.Not.Null, name);
                    Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), name);
                    Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), name);
                    Assert.That(importer.sRGBTexture, Is.EqualTo(policy.Srgb), name);
                    Assert.That(importer.alphaIsTransparency, Is.EqualTo(policy.AlphaIsTransparency), name);
                    Assert.That(importer.mipmapEnabled, Is.EqualTo(policy.Mipmaps), name);
                    Assert.That(importer.isReadable, Is.False, name);
                    Assert.That(importer.wrapMode, Is.EqualTo(policy.WrapMode), name);
                    Assert.That(importer.filterMode, Is.EqualTo(policy.FilterMode), name);
                    Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(policy.PixelsPerUnit), name);
                    Assert.That(importer.GetDefaultPlatformTextureSettings().maxTextureSize, Is.EqualTo(policy.MaxTextureSize), name);
                    foreach (var platformName in new[] { "iPhone", "Android" })
                    {
                        var platform = importer.GetPlatformTextureSettings(platformName);
                        Assert.That(platform.overridden, Is.True, name + " " + platformName);
                        Assert.That(platform.format, Is.EqualTo(policy.AstcBlockSize == 4
                            ? TextureImporterFormat.ASTC_4x4 : policy.AstcBlockSize == 6
                                ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_8x8), name + " " + platformName);
                    }
                }
                else
                {
                    var importer = (AudioImporter)AssetImporter.GetAtPath(assetPath);
                    Assert.That(importer, Is.Not.Null, name);
                    Assert.That(importer.forceToMono, Is.EqualTo(policy.ForceMono), name);
                    Assert.That(importer.loadInBackground, Is.EqualTo(policy.AudioLoadType == AudioClipLoadType.Streaming), name);
                    Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(policy.AudioLoadType), name);
                    Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(policy.AudioCompressionFormat), name);
                    Assert.That(importer.defaultSampleSettings.sampleRateSetting, Is.EqualTo(AudioSampleRateSetting.PreserveSampleRate), name);
                }
            }

            var marker = (TextureImporter)AssetImporter.GetAtPath(Stage1ContentGenerator.ImportedRoot + "/prop-marker.png");
            Assert.That(marker.spritePivot, Is.EqualTo(new Vector2(0.5f, 0.12f)));
            var boy = (TextureImporter)AssetImporter.GetAtPath(Stage1ContentGenerator.ImportedRoot + "/chr-boy-stage-reaction-neutral.png");
            var girl = (TextureImporter)AssetImporter.GetAtPath(Stage1ContentGenerator.ImportedRoot + "/chr-girl-stage-reaction-neutral.png");
            Assert.That(boy.spritePivot, Is.EqualTo(new Vector2(0.5f, 0.08f)));
            Assert.That(girl.spritePivot, Is.EqualTo(boy.spritePivot));
        }
    }
}
