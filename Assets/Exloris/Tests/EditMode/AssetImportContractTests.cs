using System.IO;
using System.Linq;
using Exloris.Content.AssetCatalog;
using Exloris.Editor.AssetImport;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class AssetImportContractTests
    {
        private const string CataloguePath = FinalAssetInventoryImporter.CatalogueAssetPath;

        [Test]
        public void GeneratedCatalogue_HasExactly253FinalActiveUniqueEntries()
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<FinalAssetCatalogue>(CataloguePath);

            Assert.That(catalogue, Is.Not.Null, "The deterministic Final inventory catalogue must exist.");
            Assert.That(catalogue.Entries.Count, Is.EqualTo(253));
            Assert.That(catalogue.Entries.Select(entry => entry.AssetId).Distinct().Count(), Is.EqualTo(253));
            Assert.That(catalogue.Entries.Select(entry => entry.RuntimePath).Distinct().Count(), Is.EqualTo(253));
            Assert.That(catalogue.Entries.All(entry => entry.State == "Final"), Is.True);
            Assert.That(catalogue.Entries.All(entry => entry.IsActive), Is.True);
            Assert.That(catalogue.Entries.All(entry => !entry.IsPlaceholder), Is.True);
            Assert.That(catalogue.Entries.All(entry => !string.IsNullOrWhiteSpace(entry.Sha256)), Is.True);
            Assert.That(catalogue.Entries.All(entry => !string.IsNullOrWhiteSpace(entry.ProvenanceLicenceOwner)), Is.True);
        }

        [Test]
        public void GeneratedCatalogue_HasNoRemovedRouteOrOrphanEntries()
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<FinalAssetCatalogue>(CataloguePath);

            Assert.That(catalogue.Entries.Any(entry => entry.RuntimePath.Contains("hub")), Is.False);
            Assert.That(catalogue.Entries.Any(entry => entry.RuntimePath.Contains("replay")), Is.False);
            Assert.That(catalogue.Entries.All(entry => File.Exists(FinalAssetInventoryImporter.ResolveWorkspacePath(entry.SourcePath))), Is.True);
        }

        [Test]
        public void BackgroundImportPolicyIsExact()
        {
            var policy = AssetImportPolicy.ForProfile("BG3");

            Assert.That(policy.Kind, Is.EqualTo(AssetKind.Texture));
            Assert.That(policy.Srgb, Is.True);
            Assert.That(policy.AlphaIsTransparency, Is.False);
            Assert.That(policy.Mipmaps, Is.False);
            Assert.That(policy.FilterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(policy.WrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(policy.MaxTextureSize, Is.EqualTo(4096));
            Assert.That(policy.PixelsPerUnit, Is.EqualTo(100));
        }

        [TestCase("UI-I", 512, true)]
        [TestCase("UI-9", 1024, true)]
        [TestCase("CHR", 1024, true)]
        [TestCase("BOAT", 2048, true)]
        [TestCase("IBIS", 1024, true)]
        [TestCase("PROP-L", 1024, true)]
        [TestCase("PROP-S", 512, true)]
        [TestCase("OUT-LAYER", 1024, true)]
        public void TexturePolicies_AreDeterministic(string profile, int maxSize, bool alpha)
        {
            var policy = AssetImportPolicy.ForProfile(profile);

            Assert.That(policy.Kind, Is.EqualTo(AssetKind.Texture));
            Assert.That(policy.Srgb, Is.True);
            Assert.That(policy.AlphaIsTransparency, Is.EqualTo(alpha));
            Assert.That(policy.Mipmaps, Is.False);
            Assert.That(policy.MaxTextureSize, Is.EqualTo(maxSize));
        }

        [TestCase("AUD-VO", AudioClipLoadType.CompressedInMemory, AudioCompressionFormat.Vorbis)]
        [TestCase("AUD-MUS", AudioClipLoadType.Streaming, AudioCompressionFormat.Vorbis)]
        [TestCase("AUD-AMB", AudioClipLoadType.Streaming, AudioCompressionFormat.Vorbis)]
        [TestCase("AUD-SFX", AudioClipLoadType.DecompressOnLoad, AudioCompressionFormat.ADPCM)]
        public void AudioPolicies_AreDeterministic(string profile, AudioClipLoadType loadType, AudioCompressionFormat format)
        {
            var policy = AssetImportPolicy.ForProfile(profile);

            Assert.That(policy.Kind, Is.EqualTo(AssetKind.Audio));
            Assert.That(policy.AudioLoadType, Is.EqualTo(loadType));
            Assert.That(policy.AudioCompressionFormat, Is.EqualTo(format));
            Assert.That(policy.PreserveSampleRate, Is.True);
        }

        [TestCase(".exe")]
        [TestCase(".dll")]
        [TestCase(".shader")]
        [TestCase(".mat")]
        [TestCase(".psd")]
        public void UnauthorizedFileTypes_AreRejected(string extension)
        {
            Assert.That(AssetImportGuard.IsAllowedExtension(extension), Is.False);
        }

        [Test]
        public void UnlistedPlaceholderInactiveAndUnlicensedEntries_AreRejected()
        {
            Assert.That(AssetImportGuard.Validate(null), Is.EqualTo(AssetImportRejection.Unlisted));
            Assert.That(AssetImportGuard.Validate(new FinalAssetEntry { State = "Draft", IsActive = true, Sha256 = "x", ProvenanceLicenceOwner = "x" }), Is.EqualTo(AssetImportRejection.NotFinal));
            Assert.That(AssetImportGuard.Validate(new FinalAssetEntry { State = "Final", IsActive = false, Sha256 = "x", ProvenanceLicenceOwner = "x" }), Is.EqualTo(AssetImportRejection.Inactive));
            Assert.That(AssetImportGuard.Validate(new FinalAssetEntry { State = "Final", IsActive = true, IsPlaceholder = true, Sha256 = "x", ProvenanceLicenceOwner = "x" }), Is.EqualTo(AssetImportRejection.Placeholder));
            Assert.That(AssetImportGuard.Validate(new FinalAssetEntry { State = "Final", IsActive = true, Sha256 = "", ProvenanceLicenceOwner = "x" }), Is.EqualTo(AssetImportRejection.MissingHash));
            Assert.That(AssetImportGuard.Validate(new FinalAssetEntry { State = "Final", IsActive = true, Sha256 = "x", ProvenanceLicenceOwner = "" }), Is.EqualTo(AssetImportRejection.Unlicensed));
        }

        [Test]
        public void UnsupportedOrPinkShaders_AreRejected()
        {
            Assert.That(AssetImportGuard.IsAllowedShader("Universal Render Pipeline/2D/Sprite-Lit-Default"), Is.True);
            Assert.That(AssetImportGuard.IsAllowedShader("Sprites/Default"), Is.False);
            Assert.That(AssetImportGuard.IsAllowedShader("Hidden/InternalErrorShader"), Is.False);
            Assert.That(AssetImportGuard.IsAllowedShader("Custom/Unapproved"), Is.False);
        }

        [Test]
        public void PostprocessorScope_IsLimitedToManagedExlorisContent()
        {
            Assert.That(AssetImportGuard.IsManagedAssetPath("Assets/Exloris/Content/Imported/ui/icon.png"), Is.True);
            Assert.That(AssetImportGuard.IsManagedAssetPath("Packages/com.unity.inputsystem/icon.png"), Is.False);
            Assert.That(AssetImportGuard.IsManagedAssetPath("Assets/Exloris/UI/Overlays/LoadingOverlay.prefab"), Is.False);
        }

        [Test]
        public void StageScopedVerifiedCopiesUseTheirStageValidatorInsteadOfTheGlobalCatalogue()
        {
            Assert.That(AssetImportGuard.IsManagedAssetPath(
                "Assets/Exloris/Content/Imported/Welcome/ui-play.png"), Is.False);
            Assert.That(AssetImportGuard.IsManagedAssetPath(
                "Assets/Exloris/Content/Imported/Avatar/ui-confirm.png"), Is.False);
            Assert.That(AssetImportGuard.IsManagedAssetPath(
                "Assets/Exloris/Content/Imported/Boat1/ui-skip.png"), Is.False);
            Assert.That(AssetImportGuard.IsManagedAssetPath(
                "Assets/Exloris/Content/Imported/Stage1/prop-marker.png"), Is.False);
            Assert.That(AssetImportGuard.IsManagedAssetPath(
                "Assets/Exloris/Content/Imported/Stage2/prop-stone-low.png"), Is.False);
            Assert.That(AssetImportGuard.IsManagedAssetPath(
                "Assets/Exloris/Content/Imported/ui/ui-confirm.png"), Is.True);
        }
    }
}
