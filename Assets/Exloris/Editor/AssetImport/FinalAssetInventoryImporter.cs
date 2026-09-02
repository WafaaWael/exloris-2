using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Exloris.Content.AssetCatalog;
using UnityEditor;
using UnityEngine;

namespace Exloris.Editor.AssetImport
{
    public static class FinalAssetInventoryImporter
    {
        public const string CatalogueAssetPath = "Assets/Exloris/Content/AssetCatalog/FinalAssetCatalogue.asset";
        public const string ImportedAssetRoot = "Assets/Exloris/Content/Imported/";
        private const string InventoryRelativePath = "production/step-12-final-export/source/final-export-inventory-v01.csv";

        [MenuItem("Exloris/Foundation/Generate Final Asset Catalogue")]
        public static void GenerateCatalogue()
        {
            var inventoryPath = ResolveWorkspacePath(InventoryRelativePath);
            var rows = CsvReader.Read(inventoryPath);
            if (rows.Count != 253)
            {
                throw new InvalidDataException("Final inventory must contain exactly 253 active rows; found " + rows.Count + ".");
            }

            var entries = rows.Select(CreateAndValidateEntry).ToList();
            ValidateUnique(entries, entry => entry.AssetId, "asset_id");
            ValidateUnique(entries, entry => entry.RuntimePath, "runtime_path");

            var directory = Path.GetDirectoryName(CatalogueAssetPath);
            if (!AssetDatabase.IsValidFolder(directory))
            {
                EnsureAssetFolder(directory);
            }

            var catalogue = AssetDatabase.LoadAssetAtPath<FinalAssetCatalogue>(CatalogueAssetPath);
            if (catalogue == null)
            {
                catalogue = ScriptableObject.CreateInstance<FinalAssetCatalogue>();
                AssetDatabase.CreateAsset(catalogue, CatalogueAssetPath);
            }

            catalogue.InventorySha256 = ComputeSha256(inventoryPath);
            catalogue.Entries = entries;
            EditorUtility.SetDirty(catalogue);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Final asset catalogue with 253 verified active entries.");
        }

        public static string ResolveWorkspacePath(string relativePath)
        {
            var unityProject = Directory.GetParent(Application.dataPath);
            var workspace = unityProject == null ? null : unityProject.Parent;
            if (workspace == null)
            {
                throw new DirectoryNotFoundException("Cannot resolve Exloris 2 workspace root from Application.dataPath.");
            }

            return Path.GetFullPath(Path.Combine(workspace.FullName, relativePath));
        }

        public static string ImportedAssetPath(string runtimePath)
        {
            return ImportedAssetRoot + runtimePath.Replace('\\', '/');
        }

        private static FinalAssetEntry CreateAndValidateEntry(IReadOnlyDictionary<string, string> row)
        {
            var entry = new FinalAssetEntry
            {
                AssetId = Required(row, "asset_id"),
                SourcePath = Required(row, "source_path"),
                RuntimePath = Required(row, "runtime_path").Replace('\\', '/'),
                Bytes = long.Parse(Required(row, "bytes"), CultureInfo.InvariantCulture),
                Sha256 = Required(row, "sha256").ToLowerInvariant(),
                State = Required(row, "state"),
                ProfileId = Required(row, "profile_id"),
                Pivot = Required(row, "pivot"),
                ImportCandidate = Required(row, "import_candidate"),
                ProvenanceLicenceOwner = Required(row, "provenance_licence_owner"),
                MetadataJson = Required(row, "metadata_json"),
                IsActive = true,
                IsPlaceholder = ContainsPlaceholderMarker(row)
            };

            AssetImportPolicy.ForProfile(entry.ProfileId);
            var rejection = AssetImportGuard.Validate(entry);
            if (rejection != AssetImportRejection.None)
            {
                throw new InvalidDataException(entry.AssetId + " rejected: " + rejection);
            }

            if (entry.RuntimePath.Contains("hub") || entry.RuntimePath.Contains("replay"))
            {
                throw new InvalidDataException(entry.AssetId + " targets a removed route.");
            }

            var source = ResolveWorkspacePath(entry.SourcePath);
            if (!File.Exists(source))
            {
                throw new FileNotFoundException(entry.AssetId + " source is missing.", source);
            }

            var info = new FileInfo(source);
            if (info.Length != entry.Bytes)
            {
                throw new InvalidDataException(entry.AssetId + " byte size does not match the frozen inventory.");
            }

            if (!string.Equals(ComputeSha256(source), entry.Sha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException(entry.AssetId + " hash does not match the frozen inventory.");
            }

            return entry;
        }

        private static bool ContainsPlaceholderMarker(IReadOnlyDictionary<string, string> row)
        {
            return row.Values.Any(value =>
                value != null &&
                (value.IndexOf("placeholder", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 value.IndexOf("draft-only", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static string Required(IReadOnlyDictionary<string, string> row, string key)
        {
            if (!row.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException("Inventory field is missing: " + key);
            }

            return value.Trim();
        }

        private static void ValidateUnique(
            IEnumerable<FinalAssetEntry> entries,
            Func<FinalAssetEntry, string> selector,
            string field)
        {
            var duplicate = entries.GroupBy(selector, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
            {
                throw new InvalidDataException("Duplicate " + field + ": " + duplicate.Key);
            }
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(stream);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var value in hash) builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static void EnsureAssetFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static class CsvReader
        {
            public static List<IReadOnlyDictionary<string, string>> Read(string path)
            {
                var records = Parse(File.ReadAllText(path, Encoding.UTF8));
                if (records.Count < 2) throw new InvalidDataException("Inventory is empty.");
                var headers = records[0];
                var rows = new List<IReadOnlyDictionary<string, string>>(records.Count - 1);
                for (var rowIndex = 1; rowIndex < records.Count; rowIndex++)
                {
                    var values = records[rowIndex];
                    if (values.Count == 1 && values[0].Length == 0) continue;
                    if (values.Count != headers.Count)
                    {
                        throw new InvalidDataException("CSV row " + (rowIndex + 1) + " has " + values.Count + " fields; expected " + headers.Count + ".");
                    }

                    var row = new Dictionary<string, string>(StringComparer.Ordinal);
                    for (var index = 0; index < headers.Count; index++) row.Add(headers[index], values[index]);
                    rows.Add(row);
                }

                return rows;
            }

            private static List<List<string>> Parse(string text)
            {
                var records = new List<List<string>>();
                var record = new List<string>();
                var field = new StringBuilder();
                var quoted = false;

                for (var index = 0; index < text.Length; index++)
                {
                    var value = text[index];
                    if (quoted)
                    {
                        if (value == '"' && index + 1 < text.Length && text[index + 1] == '"')
                        {
                            field.Append('"');
                            index++;
                        }
                        else if (value == '"') quoted = false;
                        else field.Append(value);
                    }
                    else if (value == '"') quoted = true;
                    else if (value == ',')
                    {
                        record.Add(field.ToString());
                        field.Length = 0;
                    }
                    else if (value == '\n')
                    {
                        record.Add(field.ToString().TrimEnd('\r'));
                        field.Length = 0;
                        records.Add(record);
                        record = new List<string>();
                    }
                    else field.Append(value);
                }

                if (quoted) throw new InvalidDataException("CSV has an unterminated quoted field.");
                if (field.Length > 0 || record.Count > 0)
                {
                    record.Add(field.ToString().TrimEnd('\r'));
                    records.Add(record);
                }

                return records;
            }
        }
    }
}
