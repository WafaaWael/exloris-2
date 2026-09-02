using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Exloris.Editor;
using Exloris.Editor.AssetImport;
using Exloris.Runtime.Boat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat3AssetImportContractTests
    {
        [Test]
        public void ImportedFolderHasExactlyFiftySourceEquivalentFiles()
        {
            var ledger = Boat3ContentGenerator.ReadLedger();
            var files = Directory.GetFiles(Boat3ContentGenerator.ImportedRoot)
                .Where(path => !path.EndsWith(".meta", StringComparison.Ordinal)).ToArray();
            Assert.That(files, Has.Length.EqualTo(50));
            Assert.That(files.Select(Path.GetFileName), Is.EquivalentTo(ledger.assets.Select(row => row.runtime_name)));
            var root = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            foreach (var row in ledger.assets)
            {
                var imported = Path.Combine(Boat3ContentGenerator.ImportedRoot, row.runtime_name);
                Assert.That(Hash(imported), Is.EqualTo(row.sha256), row.runtime_name);
                Assert.That(Hash(Path.Combine(root, row.source_path)), Is.EqualTo(row.sha256), row.source_path);
            }
        }

        [Test]
        public void EveryImportedPngUsesApprovedInertSpriteProfile()
        {
            foreach (var name in Boat3ContentGenerator.ReadLedger().assets
                .Select(row => row.runtime_name).Where(name => name.EndsWith(".png", StringComparison.Ordinal)))
            {
                var importer = AssetImporter.GetAtPath(Boat3ContentGenerator.ImportedRoot + "/" + name) as TextureImporter;
                Assert.That(importer, Is.Not.Null, name);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), name);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), name);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100f), name);
                Assert.That(importer.mipmapEnabled, Is.False, name);
                Assert.That(importer.isReadable, Is.False, name);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), name);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), name);
                Assert.That(importer.spritePivot, Is.EqualTo(Boat3ContentGenerator.PivotFor(name)), name);
                foreach (var platform in new[] { "iPhone", "Android" })
                    Assert.That(importer.GetPlatformTextureSettings(platform).overridden, Is.True, name + platform);
            }
        }

        [Test]
        public void EveryImportedPngUsesHighQualityStandaloneCrunch()
        {
            foreach (var name in Boat3ContentGenerator.ReadLedger().assets
                .Select(row => row.runtime_name).Where(name => name.EndsWith(".png", StringComparison.Ordinal)))
            {
                var importer = AssetImporter.GetAtPath(Boat3ContentGenerator.ImportedRoot + "/" + name) as TextureImporter;
                Assert.That(importer, Is.Not.Null, name);
                var settings = importer.GetPlatformTextureSettings("Standalone");
                Assert.That(settings.overridden, Is.True, name);
                Assert.That(settings.crunchedCompression, Is.True, name);
                Assert.That(settings.compressionQuality, Is.EqualTo(100), name);
                Assert.That(settings.textureCompression, Is.EqualTo(TextureImporterCompression.CompressedHQ), name);
                Assert.That(new[] { TextureImporterFormat.DXT1Crunched, TextureImporterFormat.DXT5Crunched },
                    Does.Contain(settings.format), name);
            }
        }

        [Test]
        public void Boat3CopiesAreStageScopedAndNeverFallThroughToTheGlobalCatalogueGuard()
        {
            Assert.That(AssetImportGuard.IsManagedAssetPath(Boat3ContentGenerator.ImportedRoot + "/boat-hull.png"), Is.False);
            Assert.That(AssetImportGuard.IsManagedAssetPath("Assets/Exloris/Content/Imported/boat-hull.png"), Is.True);
        }

        [Test]
        public void ResourceHasFiftyUniqueApprovedReferencesAndLeftCue()
        {
            var content = AssetDatabase.LoadAssetAtPath<Boat3Content>(Boat3ContentGenerator.ContentAssetPath);
            Assert.That(content, Is.Not.Null);
            Assert.That(content.AllReferences, Has.Length.EqualTo(50));
            Assert.That(content.AllReferences.Any(item => item == null), Is.False);
            Assert.That(content.AllReferences.Distinct().Count(), Is.EqualTo(50));
            Assert.That(content.AllReferences.All(item =>
                AssetDatabase.GetAssetPath(item).StartsWith(Boat3ContentGenerator.ImportedRoot + "/", StringComparison.Ordinal)), Is.True);
            Assert.That(content.Backgrounds, Has.Length.EqualTo(9));
            Assert.That(content.Controls, Has.Length.EqualTo(10));
            Assert.That(content.InitialIbisDirection, Is.EqualTo(BoatDirection.Left));
            Assert.That(content.RouteDurationSeconds, Is.EqualTo(6d));
            CollectionAssert.AreEqual(new[] { 0d, 2d, 4d, 6d }, content.ProgressCutSeconds);
        }

        [Test]
        public void ReviewHubDuplicateMissingAndAlteredRowsFailClosed()
        {
            var ledger = Boat3ContentGenerator.ReadLedger();
            Assert.DoesNotThrow(() => Boat3ContentGenerator.ValidateLedger(ledger));
            var first = ledger.assets[0];
            first.source_path = "assets/review/forbidden.png";
            Assert.Throws<InvalidOperationException>(() => Boat3ContentGenerator.ValidateLedger(ledger));
            first.source_path = "assets/step-06-environments/runtime/bg-boat-deck-4x3.png";
            first.runtime_name = "hub-boat.png";
            Assert.Throws<InvalidOperationException>(() => Boat3ContentGenerator.ValidateLedger(ledger));
            first.runtime_name = ledger.assets[1].runtime_name;
            Assert.Throws<InvalidOperationException>(() => Boat3ContentGenerator.ValidateLedger(ledger));
            first.runtime_name = "bg-boat-deck-4x3.png";
            first.sha256 = "altered";
            Assert.Throws<InvalidOperationException>(() => Boat3ContentGenerator.ValidateLedger(ledger));
        }

        private static string Hash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
