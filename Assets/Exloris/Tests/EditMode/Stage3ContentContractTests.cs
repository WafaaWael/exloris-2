using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage3ContentContractTests
    {
        internal const string LedgerPath = "../evidence/stage-3/assets/ASSET-VALIDATION.json";

        [Test]
        public void FrozenSourceLedgerHasExactlyTwentyFourApprovedStage3Assets()
        {
            var ledger = Load();
            Assert.That(ledger.status, Is.EqualTo("PASS"));
            Assert.That(ledger.product_authority_sha256, Is.EqualTo("c1aa7a6c18fb8024633345b3cfe90fc8891217d9dc111772714b504a8fedac24"));
            Assert.That(ledger.final_inventory_sha256, Is.EqualTo("fac1ae2fa3af5877c86c9e4a1145f34d143b587d241f2246eef11de36e7232d2"));
            Assert.That(ledger.assets, Has.Length.EqualTo(24));
            Assert.That(ledger.assets.Select(row => row.asset_id).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(24));
            Assert.That(ledger.assets.Select(row => row.runtime_name).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(24));
            Assert.That(ledger.assets.Any(row => row.source_path.Contains("/review/", StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(ledger.assets.Any(row => row.runtime_name.StartsWith("out-low-", StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(ledger.assets.Any(row => row.runtime_name.Contains("hub", StringComparison.OrdinalIgnoreCase)), Is.False);

            var root = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            foreach (var row in ledger.assets)
            {
                var path = Path.Combine(root, row.source_path);
                Assert.That(File.Exists(path), Is.True, row.asset_id);
                Assert.That(new FileInfo(path).Length, Is.EqualTo(row.bytes), row.asset_id);
                Assert.That(Hash(path), Is.EqualTo(row.sha256), row.asset_id);
                Assert.That(row.state, Is.EqualTo("Final"), row.asset_id);
                Assert.That(row.distribution_role, Is.EqualTo("ship"), row.asset_id);
            }
        }

        [Test]
        public void LedgerCoversResponsiveVillagesPlansOutcomeLayersAndSemanticMedia()
        {
            var rows = Load().assets;
            Assert.That(rows.Count(row => row.runtime_name.StartsWith("bg-14-village-", StringComparison.Ordinal)), Is.EqualTo(3));
            Assert.That(rows.Count(row => row.runtime_name.StartsWith("bg-15-village-after-", StringComparison.Ordinal)), Is.EqualTo(3));

            foreach (var plan in new[] { "grain", "channel", "boat" })
            {
                Assert.That(rows.Any(row => row.runtime_name == "prop-plan-" + plan + ".png"), Is.True, plan);
                Assert.That(rows.Any(row => row.runtime_name == "out-s3-benefit-" + plan + "-layer.png"), Is.True, plan);
            }

            foreach (var need in new[] { "water-route", "grain-storage", "field-water" })
                Assert.That(rows.Any(row => row.runtime_name == "out-s3-need-" + need + "-layer.png"), Is.True, need);

            foreach (var name in new[]
            {
                "anim-stage3-outcome.motion.json", "vo-s3-intro-en.wav", "vo-s3-outcome-en.wav",
                "ve-vo-s3-intro.png", "ve-vo-s3-outcome-grain.png",
                "ve-vo-s3-outcome-channel.png", "ve-vo-s3-outcome-boat.png",
                "mus-stage3-loop.wav", "amb-village-loop.wav"
            })
                Assert.That(rows.Any(row => row.runtime_name == name), Is.True, name);
        }

        [Test]
        public void LedgerRecordsExactDecodedPngWaveAndMotionFacts()
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
                    Assert.That(row.decoded.frames, Is.GreaterThan(0), row.runtime_name);
                }
                else
                    Assert.That(row.decoded.json_valid, Is.True, row.runtime_name);
            }

            var motion = rows.Single(row => row.runtime_name == "anim-stage3-outcome.motion.json");
            Assert.That(motion.decoded.duration_seconds, Is.EqualTo(3.6d).Within(0.000001d));
            CollectionAssert.AreEqual(new[] { 0d, 0.8d, 2d, 3.6d }, motion.decoded.key_times_seconds);
            CollectionAssert.AreEqual(new[] { "choice", "prepared", "final" }, motion.decoded.reduced_motion_states);

            Assert.That(rows.Single(row => row.runtime_name == "vo-s3-intro-en.wav").decoded.frames, Is.EqualTo(385512));
            Assert.That(rows.Single(row => row.runtime_name == "vo-s3-outcome-en.wav").decoded.frames, Is.EqualTo(323296));
            Assert.That(rows.Single(row => row.runtime_name == "mus-stage3-loop.wav").decoded.frames, Is.EqualTo(2304000));
            Assert.That(rows.Single(row => row.runtime_name == "amb-village-loop.wav").decoded.frames, Is.EqualTo(2880000));
        }

        [Test]
        public void AlteredBytesWrongHashDuplicateNameReviewCompositeAndHubFailClosed()
        {
            var valid = new AssetRow { source_path = "assets/x.png", runtime_name = "x.png", bytes = 3, sha256 = new string('a', 64) };
            Assert.That(IsValidRow(valid), Is.True);
            Assert.That(IsValidRow(new AssetRow { source_path = valid.source_path, runtime_name = valid.runtime_name, bytes = 0, sha256 = valid.sha256 }), Is.False);
            Assert.That(IsValidRow(new AssetRow { source_path = valid.source_path, runtime_name = valid.runtime_name, bytes = 3, sha256 = "wrong" }), Is.False);
            Assert.That(IsValidRow(new AssetRow { source_path = "assets/review/out-low-grain-choice-4x3.png", runtime_name = valid.runtime_name, bytes = 3, sha256 = valid.sha256 }), Is.False);
            Assert.That(IsValidRow(new AssetRow { source_path = valid.source_path, runtime_name = "out-low-grain-choice-4x3.png", bytes = 3, sha256 = valid.sha256 }), Is.False);
            Assert.That(IsValidRow(new AssetRow { source_path = valid.source_path, runtime_name = "hub-stage3.png", bytes = 3, sha256 = valid.sha256 }), Is.False);
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
            && !row.runtime_name.StartsWith("out-low-", StringComparison.OrdinalIgnoreCase)
            && !row.runtime_name.Contains("hub", StringComparison.OrdinalIgnoreCase);

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
            public string[] reduced_motion_states;
        }
    }
}
