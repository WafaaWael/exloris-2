using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Content.AssetCatalog;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor.AssetImport
{
    public enum AssetKind
    {
        Texture,
        Audio,
        Video,
        Font,
        Data
    }

    public enum AssetImportRejection
    {
        None,
        Unlisted,
        NotFinal,
        Inactive,
        Placeholder,
        MissingHash,
        Unlicensed,
        UnauthorizedFileType
    }

    public sealed class AssetImportPolicy
    {
        private static readonly Dictionary<string, AssetImportPolicy> Policies = BuildPolicies();

        public AssetKind Kind { get; private set; }
        public bool Srgb { get; private set; }
        public bool AlphaIsTransparency { get; private set; }
        public bool Mipmaps { get; private set; }
        public FilterMode FilterMode { get; private set; }
        public TextureWrapMode WrapMode { get; private set; }
        public int MaxTextureSize { get; private set; }
        public int PixelsPerUnit { get; private set; }
        public int AstcBlockSize { get; private set; }
        public AudioClipLoadType AudioLoadType { get; private set; }
        public AudioCompressionFormat AudioCompressionFormat { get; private set; }
        public bool PreserveSampleRate { get; private set; }
        public bool ForceMono { get; private set; }
        public bool Loop { get; private set; }
        public bool ImportVideoAudio { get; private set; }
        public bool TranscodeVideo { get; private set; }

        public static AssetImportPolicy ForProfile(string profileId)
        {
            if (!Policies.TryGetValue(profileId, out var policy))
            {
                throw new ArgumentException("No frozen import policy for profile " + profileId, nameof(profileId));
            }

            return policy;
        }

        private static Dictionary<string, AssetImportPolicy> BuildPolicies()
        {
            var result = new Dictionary<string, AssetImportPolicy>(StringComparer.Ordinal);

            AddTexture(result, "BG3", 4096, false, 6, 100);
            AddTexture(result, "UI-I", 512, true, 4);
            AddTexture(result, "UI-9", 1024, true, 4);
            AddTexture(result, "UI-OVERLAY", 512, true, 4);
            AddTexture(result, "CAP-BACKPLATE", 1024, true, 4);
            AddTexture(result, "CHR", 1024, true, 6, 100);
            AddTexture(result, "BOAT", 2048, true, 8);
            AddTexture(result, "IBIS", 1024, true, 8);
            AddTexture(result, "PROP-L", 1024, true, 8);
            AddTexture(result, "PROP-S", 512, true, 8);
            AddTexture(result, "OUT-LAYER", 1024, true, 8);
            AddTexture(result, "VISUAL-EQUIVALENT", 1024, true, 6);

            result["AUD-VO"] = Audio(AudioClipLoadType.CompressedInMemory, AudioCompressionFormat.Vorbis, true, false);
            result["AUD-MUS"] = Audio(AudioClipLoadType.Streaming, AudioCompressionFormat.Vorbis, false, true);
            result["AUD-AMB"] = Audio(AudioClipLoadType.Streaming, AudioCompressionFormat.Vorbis, false, true);
            result["AUD-SFX"] = Audio(AudioClipLoadType.DecompressOnLoad, AudioCompressionFormat.ADPCM, true, false);

            result["VID-FULL"] = Video();
            result["VID-RED"] = Video();
            result["FONT"] = new AssetImportPolicy { Kind = AssetKind.Font };
            result["CAP"] = Data();
            result["MOT"] = Data();
            result["POLICY-DATA"] = Data();
            return result;
        }

        private static void AddTexture(
            IDictionary<string, AssetImportPolicy> result,
            string profile,
            int maxSize,
            bool alpha,
            int astcBlockSize,
            int pixelsPerUnit = 100)
        {
            result[profile] = new AssetImportPolicy
            {
                Kind = AssetKind.Texture,
                Srgb = true,
                AlphaIsTransparency = alpha,
                Mipmaps = false,
                FilterMode = FilterMode.Bilinear,
                WrapMode = TextureWrapMode.Clamp,
                MaxTextureSize = maxSize,
                PixelsPerUnit = pixelsPerUnit,
                AstcBlockSize = astcBlockSize
            };
        }

        private static AssetImportPolicy Audio(
            AudioClipLoadType loadType,
            AudioCompressionFormat format,
            bool forceMono,
            bool loop)
        {
            return new AssetImportPolicy
            {
                Kind = AssetKind.Audio,
                AudioLoadType = loadType,
                AudioCompressionFormat = format,
                PreserveSampleRate = true,
                ForceMono = forceMono,
                Loop = loop
            };
        }

        private static AssetImportPolicy Video()
        {
            return new AssetImportPolicy
            {
                Kind = AssetKind.Video,
                ImportVideoAudio = false,
                TranscodeVideo = false
            };
        }

        private static AssetImportPolicy Data()
        {
            return new AssetImportPolicy { Kind = AssetKind.Data };
        }
    }

    public static class AssetImportGuard
    {
        private static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".wav", ".mp4", ".json", ".ttf"
        };

        private static readonly HashSet<string> AllowedShaders = new HashSet<string>(StringComparer.Ordinal)
        {
            "Universal Render Pipeline/2D/Sprite-Lit-Default",
            "Universal Render Pipeline/2D/Sprite-Unlit-Default"
        };

        private static readonly string[] StageScopedCopyRoots =
        {
            FinalAssetInventoryImporter.ImportedAssetRoot + "Welcome/",
            FinalAssetInventoryImporter.ImportedAssetRoot + "Avatar/",
            FinalAssetInventoryImporter.ImportedAssetRoot + "Overview/",
            FinalAssetInventoryImporter.ImportedAssetRoot + "Boat1/",
            FinalAssetInventoryImporter.ImportedAssetRoot + "Stage1/",
            FinalAssetInventoryImporter.ImportedAssetRoot + "Boat2/",
            FinalAssetInventoryImporter.ImportedAssetRoot + "Stage2/",
            FinalAssetInventoryImporter.ImportedAssetRoot + "Boat3/",
            FinalAssetInventoryImporter.ImportedAssetRoot + "Stage3/",
            FinalAssetInventoryImporter.ImportedAssetRoot + "CompletionAdultReport/",
        };

        public static bool IsAllowedExtension(string extension)
        {
            return AllowedExtensions.Contains(extension ?? string.Empty);
        }

        public static bool IsAllowedShader(string shaderName)
        {
            return AllowedShaders.Contains(shaderName ?? string.Empty);
        }

        public static bool IsManagedAssetPath(string path)
        {
            return path != null
                && path.StartsWith(FinalAssetInventoryImporter.ImportedAssetRoot, StringComparison.Ordinal)
                && !StageScopedCopyRoots.Any(root =>
                    path.StartsWith(root, StringComparison.Ordinal)
                    || string.Equals(path, root.TrimEnd('/'), StringComparison.Ordinal));
        }

        public static AssetImportRejection Validate(FinalAssetEntry entry)
        {
            if (entry == null) return AssetImportRejection.Unlisted;
            if (!string.Equals(entry.State, "Final", StringComparison.Ordinal)) return AssetImportRejection.NotFinal;
            if (!entry.IsActive) return AssetImportRejection.Inactive;
            if (entry.IsPlaceholder) return AssetImportRejection.Placeholder;
            if (string.IsNullOrWhiteSpace(entry.Sha256)) return AssetImportRejection.MissingHash;
            if (string.IsNullOrWhiteSpace(entry.ProvenanceLicenceOwner)) return AssetImportRejection.Unlicensed;
            if (!IsAllowedExtension(System.IO.Path.GetExtension(entry.RuntimePath))) return AssetImportRejection.UnauthorizedFileType;
            return AssetImportRejection.None;
        }
    }
}
