using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage1ContentContractTests
    {
        [Test]
        public void FinalShipSourceSliceHasExactBytesHashesAndDecodedFacts()
        {
            var rows = Stage1AssetContractFixture.Rows;
            Assert.That(rows, Has.Length.EqualTo(24));
            Assert.That(rows.Select(row => row.AssetId).Distinct().Count(), Is.EqualTo(24));

            foreach (var row in rows)
            {
                var path = Stage1AssetContractFixture.WorkspacePath(row.SourcePath);
                Assert.That(File.Exists(path), Is.True, row.AssetId);
                var bytes = File.ReadAllBytes(path);
                Assert.DoesNotThrow(() => Stage1AssetContractFixture.ValidateFrozenBytes(row, bytes), row.AssetId);
            }
        }

        [Test]
        public void AlteredPngAlteredWaveAndWrongHashFixturesFailClosed()
        {
            var png = Stage1AssetContractFixture.Rows.Single(row => row.AssetId == "EX2-S7-PROP-MARKER");
            var pngBytes = File.ReadAllBytes(Stage1AssetContractFixture.WorkspacePath(png.SourcePath));
            pngBytes[pngBytes.Length - 1] ^= 0x01;
            Assert.Throws<InvalidDataException>(() => Stage1AssetContractFixture.ValidateFrozenBytes(png, pngBytes));

            var wave = Stage1AssetContractFixture.Rows.Single(row => row.AssetId == "EX2-S11-SFX-MARKER-STEP");
            var waveBytes = File.ReadAllBytes(Stage1AssetContractFixture.WorkspacePath(wave.SourcePath));
            waveBytes[waveBytes.Length - 1] ^= 0x01;
            Assert.Throws<InvalidDataException>(() => Stage1AssetContractFixture.ValidateFrozenBytes(wave, waveBytes));

            var wrongHash = png.CopyWithHash(new string('0', 64));
            var original = File.ReadAllBytes(Stage1AssetContractFixture.WorkspacePath(png.SourcePath));
            Assert.Throws<InvalidDataException>(() => Stage1AssetContractFixture.ValidateFrozenBytes(wrongHash, original));
        }

        [Test]
        public void FrozenMarkerBoundariesTimingAndSupportOffsetsAreExact()
        {
            CollectionAssert.AreEqual(new[] { 0d, 0.25d, 0.5d, 0.75d, 1d }, Stage1AssetContractFixture.AuthoredPositions);
            Assert.That(Stage1AssetContractFixture.InclusiveLow, Is.EqualTo(0.25d));
            Assert.That(Stage1AssetContractFixture.InclusiveHigh, Is.EqualTo(0.75d));
            Assert.That(Stage1AssetContractFixture.ArrowStep, Is.EqualTo(0.25d));
            Assert.That(Stage1AssetContractFixture.OutcomeSeconds, Is.EqualTo(3.2d));
            CollectionAssert.AreEqual(new[] { 0d, 0.8d, 2.4d, 3.2d }, Stage1AssetContractFixture.OutcomeKeys);
            Assert.That(Stage1AssetContractFixture.LowTierThreePosition, Is.EqualTo(0d));
            Assert.That(Stage1AssetContractFixture.HighTierThreePosition, Is.EqualTo(1d));
        }
    }

    internal static class Stage1AssetContractFixture
    {
        internal const double InclusiveLow = 0.25d;
        internal const double InclusiveHigh = 0.75d;
        internal const double ArrowStep = 0.25d;
        internal const double OutcomeSeconds = 3.2d;
        internal const double LowTierThreePosition = 0d;
        internal const double HighTierThreePosition = 1d;
        internal static readonly double[] AuthoredPositions = { 0d, 0.25d, 0.5d, 0.75d, 1d };
        internal static readonly double[] OutcomeKeys = { 0d, 0.8d, 2.4d, 3.2d };

        internal static readonly Stage1AssetRow[] Rows =
        {
            Png("EX2-S6-BG-11-RIVER-LOW-4X3", "assets/step-06-environments/runtime/bg-11-river-low-4x3.png", 3021684, "905ac900697860d89699167e08734d8b292f43490bf8b4aa9c47e06171332125", "BG3", "(0.5,0.5)", 2048, 1536, 2),
            Png("EX2-S6-BG-11-RIVER-LOW-16X9", "assets/step-06-environments/runtime/bg-11-river-low-16x9.png", 3779445, "cc477aed9e3d7a22eba728de9176ada61cb663cacf516b7e7cb1ea4bb16cccdb", "BG3", "(0.5,0.5)", 2560, 1440, 2),
            Png("EX2-S6-BG-11-RIVER-LOW-24X10", "assets/step-06-environments/runtime/bg-11-river-low-24x10.png", 5472030, "b26586b0ffe24ae8b3dfea0d1f2eaa86d10f9afe95a2ec7eee053b08ca88c184", "BG3", "(0.5,0.5)", 3456, 1440, 2),
            Png("EX2-S6-BG-12-RIVER-MIDDLE-4X3", "assets/step-06-environments/runtime/bg-12-river-middle-4x3.png", 2919637, "cc651dd0d6b93468cbe14d274ec9502d0d34a843e47ac7cf548ccbb56d6cfd9e", "BG3", "(0.5,0.5)", 2048, 1536, 2),
            Png("EX2-S6-BG-12-RIVER-MIDDLE-16X9", "assets/step-06-environments/runtime/bg-12-river-middle-16x9.png", 3648533, "f4bbe29dd273c83b83eb5f819098c6cab792025595a6542d9ef350887ba39d18", "BG3", "(0.5,0.5)", 2560, 1440, 2),
            Png("EX2-S6-BG-12-RIVER-MIDDLE-24X10", "assets/step-06-environments/runtime/bg-12-river-middle-24x10.png", 5358468, "936930978f77792827dc610edbc1310592b30167afc140e3cf6fcd4825904036", "BG3", "(0.5,0.5)", 3456, 1440, 2),
            Png("EX2-S6-BG-13-RIVER-HIGH-V2-4X3", "assets/step-06-environments/runtime/bg-13-river-high-v2-4x3.png", 2764868, "b59cd7b2479dc62fac98a16039897548d96892b48e5eabc6fc742c3811c69134", "BG3", "(0.5,0.5)", 2048, 1536, 2),
            Png("EX2-S6-BG-13-RIVER-HIGH-V2-16X9", "assets/step-06-environments/runtime/bg-13-river-high-v2-16x9.png", 3419754, "0d15dc8343a1755f2ff80bb960ccedeb93dabb31d250af1bf40cab5c43bc48fd", "BG3", "(0.5,0.5)", 2560, 1440, 2),
            Png("EX2-S6-BG-13-RIVER-HIGH-V2-24X10", "assets/step-06-environments/runtime/bg-13-river-high-v2-24x10.png", 4951855, "055dc6fedb0f96436bbbca30eda4199a9b09c5df765cdc6cf8ac071d7124539a", "BG3", "(0.5,0.5)", 3456, 1440, 2),
            Png("EX2-S7-PROP-MARKER", "assets/step-07-gameplay-art/runtime/prop-marker.png", 412887, "72a1a74924cf4ead2995373dc43769158657299f2d4ef7a4b7c1f2f75584f595", "PROP-L", "(0.5,0.12)", 1024, 1024, 6),
            Png("EX2-S7-PROP-SOCKET", "assets/step-07-gameplay-art/runtime/prop-socket.png", 780380, "129ae0d632779e7d3646e7b243bafa3acb550ef4397740e8faddbf2409b3cb02", "PROP-L", "(0.5,0.5)", 1024, 1024, 6),
            Png("EX2-S7-OUT-S1-IN-RANGE-LAYER", "assets/step-07-gameplay-art/runtime/out-s1-in-range-layer.png", 199549, "0d9e5b4aba1cccf0f063204ce0b65d39b49e94a4cb9c051b55fe92cd6a064f32", "OUT-LAYER", "(0.5,0.5)", 1024, 512, 6),
            Png("EX2-S7-OUT-S1-OUTSIDE-LOW-LAYER", "assets/step-07-gameplay-art/runtime/out-s1-outside-low-layer.png", 253498, "5b95977b8eb9523009944a08f95ba55969f74f7fa706885bf41ef2ec03af40a2", "OUT-LAYER", "(0.5,0.5)", 1024, 512, 6),
            Png("EX2-S7-OUT-S1-OUTSIDE-HIGH-LAYER", "assets/step-07-gameplay-art/runtime/out-s1-outside-high-layer.png", 254604, "40439ac082916a677fec2e5fa1fc888a51d58fe289390bec83d15867cbc824da", "OUT-LAYER", "(0.5,0.5)", 1024, 512, 6),
            Png("EX2-S10-VE-VO-S1-INTRO", "production/step-10-voice-captions/visual-equivalents/ve-vo-s1-intro.png", 173647, "c011ec182942681d671af05340f8af066e108a91ea87600f52ed6cfd8613091c", "VISUAL-EQUIVALENT", "(0.5,0.5)", 1024, 512, 6),
            Png("EX2-S10-VE-VO-S1-ATTACH", "production/step-10-voice-captions/visual-equivalents/ve-vo-s1-attach.png", 142522, "0984460f387ee5d7e98c589392b3d5a81afa571d21393f8d6661c162a0adc0ee", "VISUAL-EQUIVALENT", "(0.5,0.5)", 1024, 512, 6),
            Png("EX2-S10-VE-VO-S1-OUTSIDE", "production/step-10-voice-captions/visual-equivalents/ve-vo-s1-outside.png", 105149, "688e4d075fe6317680ca250e4e112d9a74fb3e220d4656aa0873cb904fd37bd6", "VISUAL-EQUIVALENT", "(0.5,0.5)", 1024, 512, 6),
            Wave("EX2-S10-VO-S1-INTRO-EN", "production/step-10-voice-captions/voice/en/vo-s1-intro-en.wav", 1184760, "32bce9356595995197d95143a45fef37e0ab222d05705b636bdd805abbad90e7", "AUD-VO", 1, 394886),
            Wave("EX2-S10-VO-S1-ATTACH-EN", "production/step-10-voice-captions/voice/en/vo-s1-attach-en.wav", 571242, "71e5a7673f0c8e256dcf8fe85819649e7990e1be1f5664e6cb6c36c9cb6c70ab", "AUD-VO", 1, 190380),
            Wave("EX2-S10-VO-S1-OUTSIDE-EN", "production/step-10-voice-captions/voice/en/vo-s1-outside-en.wav", 1373892, "b4773ab8a0801a08cd4c20d739556d94ad8e94a295551d35f6526c4b603cd29a", "AUD-VO", 1, 457930),
            Wave("EX2-S11-MUS-STAGE1-LOOP", "production/step-11-audio/music/mus-stage1-loop.wav", 15360102, "0cda252feb8768bf8f3f85e9741fa0df9bd13a35532e1505b24f2b1e99512851", "AUD-MUS", 2, 2560000),
            Wave("EX2-S11-AMB-RIVER-LOOP", "production/step-11-audio/ambience/amb-river-loop.wav", 17280102, "b65cfe56cfbd4cae1d198c50261da367060754a72ae51cb6f317d496839ad907", "AUD-AMB", 2, 2880000),
            Wave("EX2-S11-SFX-MARKER-ATTACH", "production/step-11-audio/sfx/sfx-marker-attach.wav", 57702, "92b9453f71396dd747cd73df9d1c71ac8195c7b1fb7852a7e8de17054e3fc8ac", "AUD-SFX", 1, 19200),
            Wave("EX2-S11-SFX-MARKER-STEP", "production/step-11-audio/sfx/sfx-marker-step.wav", 36102, "6949a93a0dc1df6a6dab222ceefd71eb28776fd4d426b293c53e1d4578bcccd0", "AUD-SFX", 1, 12000)
        };

        internal static string WorkspacePath(string relativePath)
        {
            var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath);
            if (projectRoot == null || projectRoot.Parent == null) throw new DirectoryNotFoundException("Cannot resolve workspace root.");
            return Path.GetFullPath(Path.Combine(projectRoot.Parent.FullName, relativePath));
        }

        internal static void ValidateFrozenBytes(Stage1AssetRow row, byte[] bytes)
        {
            if (bytes == null || bytes.LongLength != row.Bytes || !string.Equals(Hash(bytes), row.Sha256, StringComparison.Ordinal))
                throw new InvalidDataException(row.AssetId + " frozen bytes/hash changed.");

            if (row.Kind == "png")
            {
                if (bytes.Length < 26 || bytes[0] != 137 || bytes[1] != 80 || bytes[2] != 78 || bytes[3] != 71)
                    throw new InvalidDataException(row.AssetId + " is not PNG.");
                var width = ReadBigEndianInt32(bytes, 16);
                var height = ReadBigEndianInt32(bytes, 20);
                if (width != row.Width || height != row.Height || bytes[24] != 8 || bytes[25] != row.ColorType)
                    throw new InvalidDataException(row.AssetId + " decoded PNG contract changed.");
            }
            else
            {
                var facts = ReadWave(bytes);
                if (facts.SampleRate != 48000 || facts.BitsPerSample != 24 || facts.Channels != row.Channels || facts.Frames != row.Frames)
                    throw new InvalidDataException(row.AssetId + " decoded WAV contract changed.");
            }
        }

        private static Stage1AssetRow Png(string id, string path, long bytes, string hash, string profile, string pivot, int width, int height, int colorType) =>
            new Stage1AssetRow(id, path, bytes, hash, profile, pivot, "png", width, height, colorType, 0, 0);

        private static Stage1AssetRow Wave(string id, string path, long bytes, string hash, string profile, int channels, int frames) =>
            new Stage1AssetRow(id, path, bytes, hash, profile, "n/a", "wav", 0, 0, 0, channels, frames);

        private static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(bytes).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static int ReadBigEndianInt32(byte[] bytes, int offset) =>
            bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];

        private static WaveFacts ReadWave(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes, false))
            using (var reader = new BinaryReader(stream))
            {
                if (new string(reader.ReadChars(4)) != "RIFF") throw new InvalidDataException("Invalid RIFF.");
                reader.ReadInt32();
                if (new string(reader.ReadChars(4)) != "WAVE") throw new InvalidDataException("Invalid WAVE.");
                var facts = new WaveFacts();
                var blockAlign = 0;
                var dataBytes = 0;
                while (stream.Position + 8 <= stream.Length)
                {
                    var id = new string(reader.ReadChars(4));
                    var size = reader.ReadInt32();
                    var next = stream.Position + size + (size & 1);
                    if (id == "fmt ")
                    {
                        var format = reader.ReadUInt16();
                        facts.Channels = reader.ReadUInt16();
                        facts.SampleRate = reader.ReadInt32();
                        reader.ReadInt32();
                        blockAlign = reader.ReadUInt16();
                        facts.BitsPerSample = reader.ReadUInt16();
                        if (format != 1 && format != 0xfffe) throw new InvalidDataException("WAV is not PCM.");
                    }
                    else if (id == "data") dataBytes = size;
                    stream.Position = Math.Min(next, stream.Length);
                }
                if (facts.Channels <= 0 || facts.SampleRate <= 0 || facts.BitsPerSample <= 0 || blockAlign <= 0 || dataBytes <= 0)
                    throw new InvalidDataException("Incomplete WAV.");
                facts.Frames = dataBytes / blockAlign;
                return facts;
            }
        }

        private sealed class WaveFacts
        {
            internal int Channels;
            internal int SampleRate;
            internal int BitsPerSample;
            internal int Frames;
        }
    }

    internal sealed class Stage1AssetRow
    {
        internal readonly string AssetId;
        internal readonly string SourcePath;
        internal readonly long Bytes;
        internal readonly string Sha256;
        internal readonly string ProfileId;
        internal readonly string Pivot;
        internal readonly string Kind;
        internal readonly int Width;
        internal readonly int Height;
        internal readonly int ColorType;
        internal readonly int Channels;
        internal readonly int Frames;

        internal Stage1AssetRow(string assetId, string sourcePath, long bytes, string sha256, string profileId, string pivot, string kind, int width, int height, int colorType, int channels, int frames)
        {
            AssetId = assetId;
            SourcePath = sourcePath;
            Bytes = bytes;
            Sha256 = sha256;
            ProfileId = profileId;
            Pivot = pivot;
            Kind = kind;
            Width = width;
            Height = height;
            ColorType = colorType;
            Channels = channels;
            Frames = frames;
        }

        internal Stage1AssetRow CopyWithHash(string hash) =>
            new Stage1AssetRow(AssetId, SourcePath, Bytes, hash, ProfileId, Pivot, Kind, Width, Height, ColorType, Channels, Frames);
    }
}
