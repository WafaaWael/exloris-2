using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Exloris.Editor.Validation
{
    public sealed class AcceptanceMatrixSnapshot
    {
        public string[] MatrixItemIds { get; set; }
        public string[] MatrixTestIds { get; set; }
        public string[] LedgerItemIds { get; set; }
        public string[] LedgerTestIds { get; set; }
        public string[] MatrixAssetIds { get; set; }
        public string[] InventoryAssetIds { get; set; }
        public string[] MatrixKnownFailureIds { get; set; }
        public string[] FailureResultIds { get; set; }
        public string[] Route { get; set; }
        public string[] EvidencePaths { get; set; }

        public AcceptanceMatrixSnapshot Clone() => new AcceptanceMatrixSnapshot
        {
            MatrixItemIds = Clone(MatrixItemIds), MatrixTestIds = Clone(MatrixTestIds),
            LedgerItemIds = Clone(LedgerItemIds), LedgerTestIds = Clone(LedgerTestIds),
            MatrixAssetIds = Clone(MatrixAssetIds), InventoryAssetIds = Clone(InventoryAssetIds),
            MatrixKnownFailureIds = Clone(MatrixKnownFailureIds), FailureResultIds = Clone(FailureResultIds),
            Route = Clone(Route), EvidencePaths = Clone(EvidencePaths),
        };

        private static string[] Clone(string[] values) => values == null ? null : (string[])values.Clone();
    }

    public readonly struct AcceptanceValidationResult
    {
        public AcceptanceValidationResult(
            int matrixRows, int ledgerRows, int shippingAssets, int knownFailures, int routeScreens)
        {
            MatrixRows = matrixRows; LedgerRows = ledgerRows; ShippingAssets = shippingAssets;
            KnownFailures = knownFailures; RouteScreens = routeScreens;
        }
        public int MatrixRows { get; }
        public int LedgerRows { get; }
        public int ShippingAssets { get; }
        public int KnownFailures { get; }
        public int RouteScreens { get; }
    }

    public static class AcceptanceMatrixValidator
    {
        private static readonly string[] RequiredRoute =
        {
            "startup_loading", "intro_video", "welcome", "avatar_select", "avatar_preview",
            "journey_overview", "boat_1", "stage_1", "boat_2", "stage_2", "boat_3",
            "stage_3", "completion", "adult_gate", "parent_report",
        };

        public static AcceptanceValidationResult ValidateWorkspace(
            string workspaceRoot, bool requireEvidenceFiles)
        {
            if (string.IsNullOrWhiteSpace(workspaceRoot))
                throw new ArgumentException("Workspace root is required.", nameof(workspaceRoot));
            var root = Path.GetFullPath(workspaceRoot);
            var snapshot = LoadSnapshot(root);
            return ValidateSnapshot(snapshot, requireEvidenceFiles,
                relative => File.Exists(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar))));
        }

        public static AcceptanceMatrixSnapshot LoadSnapshot(string workspaceRoot)
        {
            var matrix = ReadCsv(Path.Combine(workspaceRoot,
                "production/step-13-verification-pack/source/acceptance-matrix-v01.csv"));
            var ledger = ReadCsv(Path.Combine(workspaceRoot,
                "evidence/release-candidate/ACCEPTANCE-LEDGER.csv"));
            var inventory = ReadCsv(Path.Combine(workspaceRoot,
                "production/step-12-final-export/source/final-export-inventory-v01.csv"));
            var failures = ReadCsv(Path.Combine(workspaceRoot,
                "evidence/release-candidate/KNOWN-FAILURE-RESULTS.csv"));
            var routeJson = File.ReadAllText(Path.Combine(workspaceRoot,
                "production/step-13-verification-pack/source/direct-route-contract-v01.json"));
            var route = JsonUtility.FromJson<RouteContract>(routeJson);
            if (route == null || route.main_route == null)
                throw new InvalidDataException("Direct route contract cannot be decoded.");

            RequireColumns(matrix, "item_type", "item_id", "acceptance_test_id");
            RequireColumns(ledger, "item_type", "item_id", "acceptance_test_id",
                "verification_owner", "evidence_path");
            RequireColumns(inventory, "asset_id", "state", "distribution_role");
            RequireColumns(failures, "failure_id", "result", "evidence");
            if (ledger.Any(row => string.IsNullOrWhiteSpace(row["verification_owner"])
                || string.IsNullOrWhiteSpace(row["evidence_path"])))
                throw new InvalidDataException("Every ledger row requires a verification owner and evidence path.");

            return new AcceptanceMatrixSnapshot
            {
                MatrixItemIds = Column(matrix, "item_id"),
                MatrixTestIds = Column(matrix, "acceptance_test_id"),
                LedgerItemIds = Column(ledger, "item_id"),
                LedgerTestIds = Column(ledger, "acceptance_test_id"),
                MatrixAssetIds = matrix.Where(row => row["item_type"] == "asset")
                    .Select(row => row["item_id"]).ToArray(),
                InventoryAssetIds = inventory.Where(row => row["state"] == "Final")
                    .Select(row => row["asset_id"]).ToArray(),
                MatrixKnownFailureIds = matrix.Where(row => row["item_type"] == "known_failure")
                    .Select(row => row["acceptance_test_id"]).ToArray(),
                FailureResultIds = failures.Where(row => row["result"] == "DETECTED_PASS")
                    .Select(row => row["failure_id"]).ToArray(),
                Route = route.main_route,
                EvidencePaths = Column(ledger, "evidence_path"),
            };
        }

        public static AcceptanceValidationResult ValidateSnapshot(
            AcceptanceMatrixSnapshot snapshot,
            bool requireEvidenceFiles,
            Func<string, bool> evidenceExists)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            RequireExactUnique("matrix item IDs", snapshot.MatrixItemIds, 592);
            RequireExactUnique("matrix test IDs", snapshot.MatrixTestIds, 592);
            RequireExactUnique("ledger item IDs", snapshot.LedgerItemIds, 592);
            RequireExactUnique("ledger test IDs", snapshot.LedgerTestIds, 592);
            RequireSameSet("matrix and ledger item IDs", snapshot.MatrixItemIds, snapshot.LedgerItemIds);
            RequireSameSet("matrix and ledger test IDs", snapshot.MatrixTestIds, snapshot.LedgerTestIds);
            RequireExactUnique("matrix shipping assets", snapshot.MatrixAssetIds, 253);
            RequireExactUnique("inventory shipping assets", snapshot.InventoryAssetIds, 253);
            RequireSameSet("matrix and inventory shipping assets",
                snapshot.MatrixAssetIds, snapshot.InventoryAssetIds);
            RequireExactUnique("matrix known failures", snapshot.MatrixKnownFailureIds, 18);
            RequireExactUnique("known failure results", snapshot.FailureResultIds, 18);
            RequireSameSet("canonical known failures", ReleaseKnownFailureValidator.CanonicalIds,
                snapshot.MatrixKnownFailureIds);
            RequireSameSet("executed known failures", ReleaseKnownFailureValidator.CanonicalIds,
                snapshot.FailureResultIds);
            if (snapshot.Route == null || !snapshot.Route.SequenceEqual(RequiredRoute, StringComparer.Ordinal))
                throw new InvalidDataException("The direct route must be the exact 15-screen frozen sequence.");
            if (snapshot.EvidencePaths == null || snapshot.EvidencePaths.Length != 592)
                throw new InvalidDataException("Every acceptance row requires an evidence path.");
            if (requireEvidenceFiles)
            {
                if (evidenceExists == null) throw new ArgumentNullException(nameof(evidenceExists));
                foreach (var joined in snapshot.EvidencePaths)
                {
                    foreach (var path in joined.Split('|'))
                    {
                        if (!evidenceExists(path.Trim()))
                            throw new FileNotFoundException("Acceptance evidence is missing.", path.Trim());
                    }
                }
            }
            return new AcceptanceValidationResult(592, 592, 253, 18, 15);
        }

        private static void RequireExactUnique(string label, string[] values, int expected)
        {
            if (values == null || values.Length != expected)
                throw new InvalidDataException(label + " count differs from " + expected + ".");
            if (values.Any(string.IsNullOrWhiteSpace)
                || values.Distinct(StringComparer.Ordinal).Count() != expected)
                throw new InvalidDataException(label + " contains a blank or duplicate value.");
        }

        private static void RequireSameSet(string label, IEnumerable<string> left, IEnumerable<string> right)
        {
            var first = new HashSet<string>(left, StringComparer.Ordinal);
            var second = new HashSet<string>(right, StringComparer.Ordinal);
            if (!first.SetEquals(second)) throw new InvalidDataException(label + " do not reconcile.");
        }

        private static string[] Column(
            IReadOnlyList<Dictionary<string, string>> rows, string name) =>
            rows.Select(row => row[name]).ToArray();

        private static void RequireColumns(
            IReadOnlyList<Dictionary<string, string>> rows, params string[] columns)
        {
            if (rows == null || rows.Count == 0) throw new InvalidDataException("CSV contains no data rows.");
            foreach (var column in columns)
                if (!rows[0].ContainsKey(column)) throw new InvalidDataException("CSV column is missing: " + column);
        }

        private static List<Dictionary<string, string>> ReadCsv(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Acceptance source is missing.", path);
            var parsed = ParseCsv(File.ReadAllText(path));
            if (parsed.Count < 2) throw new InvalidDataException("CSV must contain a header and data rows: " + path);
            var headers = parsed[0].ToArray();
            if (headers.Any(string.IsNullOrWhiteSpace)
                || headers.Distinct(StringComparer.Ordinal).Count() != headers.Length)
                throw new InvalidDataException("CSV headers are blank or duplicated: " + path);
            var rows = new List<Dictionary<string, string>>();
            for (var rowIndex = 1; rowIndex < parsed.Count; rowIndex++)
            {
                var values = parsed[rowIndex];
                if (values.Count == 1 && string.IsNullOrEmpty(values[0])) continue;
                if (values.Count != headers.Length)
                    throw new InvalidDataException("CSV row width differs from header: " + path);
                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var index = 0; index < headers.Length; index++) row.Add(headers[index], values[index]);
                rows.Add(row);
            }
            return rows;
        }

        private static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new System.Text.StringBuilder();
            var quoted = false;
            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                if (quoted)
                {
                    if (character == '"')
                    {
                        if (index + 1 < text.Length && text[index + 1] == '"')
                        { field.Append('"'); index++; }
                        else quoted = false;
                    }
                    else field.Append(character);
                    continue;
                }
                if (character == '"') { quoted = true; continue; }
                if (character == ',') { row.Add(field.ToString()); field.Length = 0; continue; }
                if (character == '\r') continue;
                if (character == '\n')
                {
                    row.Add(field.ToString()); field.Length = 0; rows.Add(row); row = new List<string>();
                    continue;
                }
                field.Append(character);
            }
            if (quoted) throw new InvalidDataException("CSV ends inside a quoted field.");
            if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); rows.Add(row); }
            return rows;
        }

        [Serializable]
        private sealed class RouteContract { public string[] main_route; }
    }
}
