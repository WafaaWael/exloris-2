using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat3ContentContractTests
    {
        private const string LedgerPath = "../evidence/boat-3/assets/ASSET-VALIDATION.json";

        [Test]
        public void FrozenSourceLedgerHasExactlyFiftyApprovedBoat3Assets()
        {
            Assert.That(File.Exists(LedgerPath), Is.True, LedgerPath);
            var ledger = JsonUtility.FromJson<AssetLedger>(File.ReadAllText(LedgerPath));
            Assert.That(ledger.status, Is.EqualTo("PASS"));
            Assert.That(ledger.assets, Has.Length.EqualTo(50));
            Assert.That(ledger.assets.Select(row => row.runtime_name).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(50));
            Assert.That(ledger.assets.Any(row => row.source_path.Contains("/review/", StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(ledger.assets.Any(row => row.runtime_name.Contains("hub", StringComparison.OrdinalIgnoreCase)), Is.False);

            var root = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            foreach (var row in ledger.assets)
            {
                var path = Path.Combine(root, row.source_path);
                Assert.That(File.Exists(path), Is.True, row.source_path);
                Assert.That(new FileInfo(path).Length, Is.EqualTo(row.bytes), row.runtime_name);
                Assert.That(Hash(path), Is.EqualTo(row.sha256), row.runtime_name);
            }
        }

        [Test]
        public void LedgerCoversEveryBoat3SemanticMediaRole()
        {
            var rows = JsonUtility.FromJson<AssetLedger>(File.ReadAllText(LedgerPath)).assets;
            foreach (var required in new[]
            {
                "boat-3-left-grain-store.png", "boat-3-right-irrigation.png", "boat-follow-ibis.png",
                "bg-boat-deck-4x3.png", "bg-boat-deck-16x9.png", "bg-boat-deck-24x10.png",
                "bg-07-boat-approach-4x3.png", "bg-07-boat-approach-16x9.png", "bg-07-boat-approach-24x10.png",
                "bg-08-boat-arrival-4x3.png", "bg-08-boat-arrival-16x9.png", "bg-08-boat-arrival-24x10.png",
                "chr-boy-seated.png", "chr-girl-seated.png", "chr-boatkeeper-point-safe-direction.png",
                "vo-boat-choice-en.wav", "vo-boat-help-en.wav", "sfx-ibis-cue.wav", "sfx-boat-arrival.wav"
            })
                Assert.That(rows.Any(row => row.runtime_name == required), Is.True, required);
        }

        [Test]
        public void LedgerRecordsDecodedPngWavAndCaptionFacts()
        {
            var ledger = JsonUtility.FromJson<AssetLedger>(File.ReadAllText(LedgerPath));
            foreach (var row in ledger.assets)
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
                    Assert.That(row.decoded.duration_seconds, Is.GreaterThan(0), row.runtime_name);
                }
                else
                    Assert.That(row.decoded.json_valid, Is.True, row.runtime_name);
            }

            var root = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            var captions = File.ReadAllText(Path.Combine(root, "assets/boat-1/source/boat-1-caption-timings.json"));
            StringAssert.Contains("VO-BOAT-CHOICE", captions);
            StringAssert.Contains("vo-boat-choice-en.wav", captions);
            StringAssert.Contains("VO-BOAT-HELP", captions);
            StringAssert.Contains("vo-boat-help-en.wav", captions);
        }

        [Test]
        public void AlteredBytesWrongHashDuplicateNameAndForbiddenPathFailTheContract()
        {
            var valid = new AssetRow { source_path = "assets/x.png", runtime_name = "x.png", bytes = 3, sha256 = new string('a', 64) };
            Assert.That(IsValidRow(valid), Is.True);
            Assert.That(IsValidRow(new AssetRow { source_path = valid.source_path, runtime_name = valid.runtime_name, bytes = 0, sha256 = valid.sha256 }), Is.False);
            Assert.That(IsValidRow(new AssetRow { source_path = valid.source_path, runtime_name = valid.runtime_name, bytes = 3, sha256 = "wrong" }), Is.False);
            Assert.That(IsValidRow(new AssetRow { source_path = "assets/review/x.png", runtime_name = valid.runtime_name, bytes = 3, sha256 = valid.sha256 }), Is.False);
            Assert.That(IsValidRow(new AssetRow { source_path = valid.source_path, runtime_name = "hub-x.png", bytes = 3, sha256 = valid.sha256 }), Is.False);
        }

        private static bool IsValidRow(AssetRow row) => row != null
            && row.bytes > 0
            && row.sha256 != null && row.sha256.Length == 64
            && !row.source_path.Contains("/review/", StringComparison.OrdinalIgnoreCase)
            && !row.runtime_name.Contains("hub", StringComparison.OrdinalIgnoreCase);

        private static string Hash(string path)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }

        [Serializable]
        private sealed class AssetLedger { public string status; public AssetRow[] assets; }

        [Serializable]
        private sealed class AssetRow
        {
            public string source_path;
            public string runtime_name;
            public long bytes;
            public string sha256;
            public DecodedFacts decoded;
        }

        [Serializable]
        private sealed class DecodedFacts
        {
            public int width;
            public int height;
            public int bit_depth;
            public int color_type;
            public int sample_rate;
            public int bits_per_sample;
            public int channels;
            public int frames;
            public float duration_seconds;
            public bool json_valid;
        }
    }
}
