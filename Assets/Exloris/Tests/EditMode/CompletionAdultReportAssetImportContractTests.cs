using System;
using System.IO;
using System.Linq;
using Exloris.Editor;
using Exloris.Runtime.Completion;
using Exloris.Editor.AssetImport;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class CompletionAdultReportAssetImportContractTests
    {
        private const string ImportedRoot = "Assets/Exloris/Content/Imported/CompletionAdultReport";

        [Test]
        public void ClosingRouteCopiesAreStageScopedAndNeverUseTheGlobalCatalogueGuard()
        {
            Assert.That(AssetImportGuard.IsManagedAssetPath(ImportedRoot + "/bg-16-parent-4x3.png"), Is.False);
            Assert.That(AssetImportGuard.IsManagedAssetPath(
                "Assets/Exloris/Content/Imported/bg-16-parent-4x3.png"), Is.True);
        }

        [Test]
        public void ImportedFolderAndContentAssetContainExactlyTwentySevenFrozenReferences()
        {
            var files = Directory.GetFiles(ImportedRoot)
                .Where(path => !path.EndsWith(".meta", StringComparison.Ordinal))
                .Select(Path.GetFileName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            Assert.That(files, Has.Length.EqualTo(27));
            CollectionAssert.AreEqual(
                CompletionAdultReportContentGenerator.ApprovedRuntimeNames.OrderBy(name => name, StringComparer.Ordinal),
                files);
            CompletionAdultReportContentGenerator.ValidateImportedFileSet(files);
            Assert.DoesNotThrow(CompletionAdultReportContentGenerator.ValidateFrozenImportedBytes);

            var content = AssetDatabase.LoadAssetAtPath<CompletionAdultReportContent>(
                CompletionAdultReportContentGenerator.ContentAssetPath);
            Assert.That(content, Is.Not.Null);
            Assert.That(content.AllReferences, Has.Length.EqualTo(27));
            Assert.That(content.AllReferences.All(item => item != null), Is.True);
            Assert.That(content.AllReferences.Distinct().Count(), Is.EqualTo(27));
            var allowedShared = new[]
            {
                "Assets/Exloris/Content/Imported/Stage3/bg-15-village-after-4x3.png",
                "Assets/Exloris/Content/Imported/Stage3/bg-15-village-after-16x9.png",
                "Assets/Exloris/Content/Imported/Stage3/bg-15-village-after-24x10.png",
                "Assets/Exloris/Content/Imported/Stage3/amb-village-loop.wav",
            };
            Assert.That(content.AllReferences.All(item =>
            {
                var path = AssetDatabase.GetAssetPath(item);
                return path.StartsWith(ImportedRoot + "/", StringComparison.Ordinal)
                    || allowedShared.Contains(path, StringComparer.Ordinal);
            }), Is.True);
            CollectionAssert.AreEqual(allowedShared.Take(3),
                content.CompletionBackgrounds.Select(AssetDatabase.GetAssetPath));
            Assert.That(AssetDatabase.GetAssetPath(content.VillageAmbience),
                Is.EqualTo(allowedShared[3]));
        }

        [Test]
        public void PngAndAudioImportersUseFrozenRuntimeProfiles()
        {
            foreach (var name in CompletionAdultReportContentGenerator.ApprovedRuntimeNames
                .Where(name => name.EndsWith(".png", StringComparison.Ordinal)))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(ImportedRoot + "/" + name);
                Assert.That(importer, Is.Not.Null, name);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), name);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), name);
                Assert.That(importer.mipmapEnabled, Is.False, name);
                Assert.That(importer.isReadable, Is.False, name);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), name);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), name);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100f), name);
                Assert.That(importer.alphaIsTransparency,
                    Is.EqualTo(!name.StartsWith("bg-", StringComparison.Ordinal)), name);
                var standalone = importer.GetPlatformTextureSettings("Standalone");
                Assert.That(standalone.overridden, Is.True, name);
                Assert.That(standalone.crunchedCompression, Is.True, name);
                Assert.That(standalone.compressionQuality, Is.EqualTo(100), name);
                if (name.StartsWith("frame-", StringComparison.Ordinal))
                    Assert.That(importer.spriteBorder, Is.EqualTo(new Vector4(48f, 48f, 48f, 48f)), name);
            }

            AssertAudio("vo-completion-en.wav", AudioClipLoadType.CompressedInMemory, true, true);
            AssertAudio("mus-completion-loop.wav", AudioClipLoadType.Streaming, false, false);
            AssertAudio("amb-village-loop.wav", AudioClipLoadType.Streaming, false, false);
        }

        private static void AssertAudio(string name, AudioClipLoadType loadType, bool mono, bool preload)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(ImportedRoot + "/" + name);
            Assert.That(importer, Is.Not.Null, name);
            Assert.That(importer.forceToMono, Is.EqualTo(mono), name);
            Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(loadType), name);
            Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.EqualTo(preload), name);
            Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.Vorbis), name);
        }
    }
}
