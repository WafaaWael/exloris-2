using System;
using System.Collections.Generic;
using UnityEngine;

namespace Exloris.Content.AssetCatalog
{
    [Serializable]
    public sealed class FinalAssetEntry
    {
        public string AssetId;
        public string SourcePath;
        public string RuntimePath;
        public long Bytes;
        public string Sha256;
        public string State;
        public string ProfileId;
        public string Pivot;
        public string ImportCandidate;
        public string ProvenanceLicenceOwner;
        public string MetadataJson;
        public bool IsActive;
        public bool IsPlaceholder;
    }

    public sealed class FinalAssetCatalogue : ScriptableObject
    {
        public string InventorySha256;
        public List<FinalAssetEntry> Entries = new List<FinalAssetEntry>();

        public FinalAssetEntry FindByRuntimePath(string runtimePath)
        {
            return Entries.Find(entry => string.Equals(entry.RuntimePath, runtimePath, StringComparison.Ordinal));
        }
    }
}
