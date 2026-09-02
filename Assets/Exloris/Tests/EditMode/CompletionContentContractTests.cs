using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class CompletionContentContractTests
    {
        internal const string LedgerPath = "../evidence/completion-adult-report/assets/ASSET-VALIDATION.json";

        [Test]
        public void FrozenSourceLedgerHasExactlyTwentySevenApprovedAssets()
        {
            var ledger = Load();
            Assert.That(ledger.status, Is.EqualTo("PASS"));
            Assert.That(ledger.product_authority_sha256, Is.EqualTo("c1aa7a6c18fb8024633345b3cfe90fc8891217d9dc111772714b504a8fedac24"));
            Assert.That(ledger.final_inventory_sha256, Is.EqualTo("fac1ae2fa3af5877c86c9e4a1145f34d143b587d241f2246eef11de36e7232d2"));
            Assert.That(ledger.assets, Has.Length.EqualTo(27));
            Assert.That(ledger.assets.Select(row => row.asset_id).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(27));
            Assert.That(ledger.assets.Select(row => row.runtime_name).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(27));

            var root = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            foreach (var row in ledger.assets)
            {
                var path = Path.Combine(root, row.source_path);
                Assert.That(File.Exists(path), Is.True, row.asset_id);
                Assert.That(new FileInfo(path).Length, Is.EqualTo(row.bytes), row.asset_id);
                Assert.That(Hash(path), Is.EqualTo(row.sha256), row.asset_id);
                Assert.That(row.state, Is.EqualTo("Final"), row.asset_id);
                Assert.That(row.distribution_role, Is.EqualTo("ship"), row.asset_id);
                Assert.That(IsValidRow(row), Is.True, row.asset_id);
            }
        }

        [Test]
        public void LedgerCoversCompletionAdultGateReportAndSemanticMedia()
        {
            var rows = Load().assets;
            Assert.That(rows.Count(row => row.runtime_name.StartsWith("bg-15-village-after-", StringComparison.Ordinal)), Is.EqualTo(3));
            Assert.That(rows.Count(row => row.runtime_name.StartsWith("bg-16-parent-", StringComparison.Ordinal)), Is.EqualTo(3));
            Assert.That(rows.Count(row => row.runtime_name.Contains("completion-neutral", StringComparison.Ordinal)), Is.EqualTo(2));
            Assert.That(rows.Count(row => row.runtime_name.StartsWith("ui-adult-hold-progress-", StringComparison.Ordinal)), Is.EqualTo(5));

            foreach (var name in new[]
            {
                "anim-completion.motion.json", "vo-completion-en.wav", "ve-vo-completion.png",
                "mus-completion-loop.wav", "amb-village-loop.wav", "ui-adult-hold.png",
                "frame-card.png", "frame-slot-empty.png", "ui-home.png", "ui-close.png",
                "ui-restart.png", "ui-confirm.png", "ui-back.png", "ui-stay.png"
            })
                Assert.That(rows.Any(row => row.runtime_name == name), Is.True, name);
        }

        [Test]
        public void LedgerRecordsExactDecodedCompletionMediaFacts()
        {
            var rows = Load().assets;
            foreach (var row in rows)
            {
                Assert.That(row.decoded, Is.Not.Null, row.runtime_name);
                if (row.runtime_name.EndsWith(".png", StringComparison.Ordinal))
                {
                    Assert.That(row.decoded.width, Is.GreaterThan(0), row.runtime_name);
                    Assert.That(row.decoded.height, Is.GreaterThan(0), row.runtime_name);
                    Assert.That(row.decoded.bit_depth, Is.EqualTo(8), row.runtime_name);
                    Assert.That(new[] { 2, 6 }, Does.Contain(row.decoded.color_type), row.runtime_name);
                }
                else if (row.runtime_name.EndsWith(".wav", StringComparison.Ordinal))
                {
                    Assert.That(row.decoded.sample_rate, Is.EqualTo(48000), row.runtime_name);
                    Assert.That(row.decoded.bits_per_sample, Is.EqualTo(24), row.runtime_name);
                    Assert.That(new[] { 1, 2 }, Does.Contain(row.decoded.channels), row.runtime_name);
                }
                else
                    Assert.That(row.decoded.json_valid, Is.True, row.runtime_name);
            }

            var motion = rows.Single(row => row.runtime_name == "anim-completion.motion.json");
            Assert.That(motion.decoded.duration_seconds, Is.EqualTo(2.4d).Within(0.000001d));
            CollectionAssert.AreEqual(new[] { 0d, 0.6d, 1.8d, 2.4d }, motion.decoded.key_times_seconds);
            Assert.That(motion.decoded.static_meaning, Is.EqualTo("same warm neutral completion without celebration motion"));
            Assert.That(rows.Single(row => row.runtime_name == "vo-completion-en.wav").decoded.frames, Is.EqualTo(175002));
            Assert.That(rows.Single(row => row.runtime_name == "mus-completion-loop.wav").decoded.frames, Is.EqualTo(1280000));
            Assert.That(rows.Single(row => row.runtime_name == "amb-village-loop.wav").decoded.frames, Is.EqualTo(2880000));
        }

        [Test]
        public void AlteredDuplicateReviewHubProfileAndNonexistentCancelSourcesFailClosed()
        {
            var valid = new AssetRow { source_path = "assets/x.png", runtime_name = "x.png", bytes = 3, sha256 = new string('a', 64) };
            Assert.That(IsValidRow(valid), Is.True);
            Assert.That(IsValidRow(new AssetRow { source_path = valid.source_path, runtime_name = valid.runtime_name, bytes = 0, sha256 = valid.sha256 }), Is.False);
            Assert.That(IsValidRow(new AssetRow { source_path = valid.source_path, runtime_name = valid.runtime_name, bytes = 3, sha256 = "wrong" }), Is.False);
            Assert.That(IsValidRow(new AssetRow { source_path = "assets/review/x.png", runtime_name = valid.runtime_name, bytes = 3, sha256 = valid.sha256 }), Is.False);
            Assert.That(IsValidRow(new AssetRow { source_path = valid.source_path, runtime_name = "hub-parent.png", bytes = 3, sha256 = valid.sha256 }), Is.False);
            Assert.That(IsValidRow(new AssetRow { source_path = valid.source_path, runtime_name = "profile-picker.png", bytes = 3, sha256 = valid.sha256 }), Is.False);
            Assert.That(IsValidRow(new AssetRow { source_path = valid.source_path, runtime_name = "ui-cancel.png", bytes = 3, sha256 = valid.sha256 }), Is.False);
        }

        internal static AssetLedger Load()
        {
            Assert.That(File.Exists(LedgerPath), Is.True, LedgerPath);
            var ledger = JsonUtility.FromJson<AssetLedger>(File.ReadAllText(LedgerPath));
            Assert.That(ledger, Is.Not.Null);
            Assert.That(ledger.assets, Is.Not.Null);
            return ledger;
        }

        private static bool IsValidRow(AssetRow row) => row != null
            && row.bytes > 0
            && row.sha256 != null && row.sha256.Length == 64
            && !row.source_path.Contains("/review/", StringComparison.OrdinalIgnoreCase)
            && !row.runtime_name.Contains("hub", StringComparison.OrdinalIgnoreCase)
            && !row.runtime_name.Contains("profile", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(row.runtime_name, "ui-cancel.png", StringComparison.OrdinalIgnoreCase);

        private static string Hash(string path)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }

        [Serializable]
        internal sealed class AssetLedger
        {
            public string status;
            public string product_authority_sha256;
            public string final_inventory_sha256;
            public AssetRow[] assets;
        }

        [Serializable]
        internal sealed class AssetRow
        {
            public string asset_id;
            public string source_path;
            public string runtime_name;
            public long bytes;
            public string sha256;
            public string state;
            public string distribution_role;
            public string profile_id;
            public string pivot;
            public DecodedFacts decoded;
        }

        [Serializable]
        internal sealed class DecodedFacts
        {
            public int width;
            public int height;
            public int bit_depth;
            public int color_type;
            public int sample_rate;
            public int bits_per_sample;
            public int channels;
            public int frames;
            public double duration_seconds;
            public bool json_valid;
            public double[] key_times_seconds;
            public string static_meaning;
        }
    }
}
