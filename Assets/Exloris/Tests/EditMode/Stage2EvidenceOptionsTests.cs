using System.Reflection;
using Exloris.Runtime.Diagnostics;
using Exloris.Runtime.Input;
using NUnit.Framework;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage2EvidenceOptionsTests
    {
        [Test]
        public void ParsesEveryFrozenStage2EvidenceAxis()
        {
            var arguments = new[]
            {
                "player", "-stage2Capture", "/tmp/stage2.png",
                "-stage2Ratio", "24x10", "-stage2Child", "girl",
                "-stage2River", "high", "-stage2Hotspot", "boat",
                "-stage2Sound", "off", "-stage2Captions", "on",
                "-stage2Motion", "reduced", "-stage2Beat", "consequence-settle",
                "-stage2Visited", "2", "-stage2Repeat", "on",
                "-stage2Finish", "off", "-stage2Overlay", "pause-help",
                "-stage2Failure", "none", "-stage2Recovery", "none",
                "-stage2CaptureDelay", "1.5",
            };

            Assert.That(Stage2EvidenceOptions.TryGetCapture(arguments, out var options), Is.True);
            Assert.That(options.Path, Is.EqualTo("/tmp/stage2.png"));
            Assert.That(options.Ratio, Is.EqualTo(Stage2EvidenceRatio.UltraWide));
            Assert.That(options.Child, Is.EqualTo(Stage2EvidenceChild.Girl));
            Assert.That(options.River, Is.EqualTo(Stage2EvidenceRiver.High));
            Assert.That(options.Hotspot, Is.EqualTo(Stage2EvidenceHotspot.Boat));
            Assert.That(options.SoundEnabled, Is.False);
            Assert.That(options.CaptionsEnabled, Is.True);
            Assert.That(options.ReducedMotion, Is.True);
            Assert.That(options.Beat, Is.EqualTo(Stage2EvidenceBeat.ConsequenceSettle));
            Assert.That(options.VisitedBefore, Is.EqualTo(2));
            Assert.That(options.Repeat, Is.True);
            Assert.That(options.AttemptFinish, Is.False);
            Assert.That(options.Overlay, Is.EqualTo(Stage2EvidenceOverlay.PauseHelp));
            Assert.That(options.Failure, Is.EqualTo(Stage2EvidenceFailure.None));
            Assert.That(options.RecoveryAction, Is.EqualTo(Stage2EvidenceRecoveryAction.None));
            Assert.That(options.DelaySeconds, Is.EqualTo(1.5d));
        }

        [TestCase("4x3", Stage2EvidenceRatio.FourThree)]
        [TestCase("16x9", Stage2EvidenceRatio.SixteenNine)]
        [TestCase("24x10", Stage2EvidenceRatio.UltraWide)]
        public void AcceptsOnlyFrozenRatios(string value, Stage2EvidenceRatio expected)
        {
            Assert.That(Parse("-stage2Ratio", value, out var options), Is.True);
            Assert.That(options.Ratio, Is.EqualTo(expected));
        }

        [TestCase("low", Stage2EvidenceRiver.Low)]
        [TestCase("middle", Stage2EvidenceRiver.Middle)]
        [TestCase("high", Stage2EvidenceRiver.High)]
        public void AcceptsAllEqualRivers(string value, Stage2EvidenceRiver expected)
        {
            Assert.That(Parse("-stage2River", value, out var options), Is.True);
            Assert.That(options.River, Is.EqualTo(expected));
        }

        [TestCase("field", Stage2EvidenceHotspot.Field)]
        [TestCase("path", Stage2EvidenceHotspot.Path)]
        [TestCase("boat", Stage2EvidenceHotspot.Boat)]
        public void AcceptsAllEqualHotspots(string value, Stage2EvidenceHotspot expected)
        {
            Assert.That(Parse("-stage2Hotspot", value, out var options), Is.True);
            Assert.That(options.Hotspot, Is.EqualTo(expected));
        }

        [TestCase("intro", Stage2EvidenceBeat.Intro)]
        [TestCase("choosing", Stage2EvidenceBeat.Choosing)]
        [TestCase("inspecting", Stage2EvidenceBeat.Inspecting)]
        [TestCase("consequence-start", Stage2EvidenceBeat.ConsequenceStart)]
        [TestCase("consequence-reveal", Stage2EvidenceBeat.ConsequenceReveal)]
        [TestCase("consequence-settle", Stage2EvidenceBeat.ConsequenceSettle)]
        [TestCase("consequence-complete", Stage2EvidenceBeat.ConsequenceComplete)]
        [TestCase("compared", Stage2EvidenceBeat.Compared)]
        [TestCase("finish-guard", Stage2EvidenceBeat.FinishGuard)]
        [TestCase("boat3-loading", Stage2EvidenceBeat.Boat3Loading)]
        public void AcceptsEveryFrozenBeat(string value, Stage2EvidenceBeat expected)
        {
            var arguments = Defaults();
            arguments = Append(arguments, "-stage2Beat", value);
            if (expected == Stage2EvidenceBeat.Compared)
                arguments = Append(arguments, "-stage2Visited", "1");
            if (expected == Stage2EvidenceBeat.FinishGuard)
            {
                arguments = Append(arguments, "-stage2Visited", "2");
                arguments = Append(arguments, "-stage2Finish", "on");
            }
            if (expected == Stage2EvidenceBeat.Boat3Loading)
            {
                arguments = Append(arguments, "-stage2Visited", "3");
                arguments = Append(arguments, "-stage2Finish", "on");
            }
            Assert.That(Stage2EvidenceOptions.TryGetCapture(arguments, out var options), Is.True);
            Assert.That(options.Beat, Is.EqualTo(expected));
        }

        [TestCase("entry-timeout", Stage2EvidenceFailure.EntryTimeout)]
        [TestCase("entry-asset", Stage2EvidenceFailure.EntryAsset)]
        [TestCase("entry-layout", Stage2EvidenceFailure.EntryLayout)]
        [TestCase("boat3-timeout", Stage2EvidenceFailure.Boat3Timeout)]
        [TestCase("boat3-asset", Stage2EvidenceFailure.Boat3Asset)]
        [TestCase("boat3-layout", Stage2EvidenceFailure.Boat3Layout)]
        public void AcceptsEveryMappedFailure(string value, Stage2EvidenceFailure expected)
        {
            var arguments = Append(Defaults(), "-stage2Failure", value);
            arguments = Append(arguments, "-stage2Overlay", "recovery");
            if (value.StartsWith("boat3-", System.StringComparison.Ordinal))
            {
                arguments = Append(arguments, "-stage2Beat", "boat3-loading");
                arguments = Append(arguments, "-stage2Visited", "3");
                arguments = Append(arguments, "-stage2Finish", "on");
            }
            Assert.That(Stage2EvidenceOptions.TryGetCapture(arguments, out var options), Is.True);
            Assert.That(options.Failure, Is.EqualTo(expected));
        }

        [Test]
        public void RejectsUnknownDuplicateAndContradictoryRequests()
        {
            Assert.That(Parse("-stage2Ratio", "portrait", out _), Is.False);
            Assert.That(Parse("-stage2River", "automatic", out _), Is.False);
            Assert.That(Parse("-stage2Hotspot", "score", out _), Is.False);
            Assert.That(Parse("-stage2Unknown", "value", out _), Is.False);
            Assert.That(Stage2EvidenceOptions.TryGetCapture(new[]
            {
                "player", "-stage2Capture", "/tmp/a.png", "-stage2Capture", "/tmp/b.png",
            }, out _), Is.False);
            Assert.That(Stage2EvidenceOptions.TryGetCapture(Append(Defaults(),
                "-stage2Failure", "boat3-timeout"), out _), Is.False);
            var impossibleFinish = Append(Defaults(), "-stage2Beat", "finish-guard");
            impossibleFinish = Append(impossibleFinish, "-stage2Visited", "3");
            impossibleFinish = Append(impossibleFinish, "-stage2Finish", "on");
            Assert.That(Stage2EvidenceOptions.TryGetCapture(impossibleFinish, out _), Is.False);
        }

        [Test]
        public void ProductEvidenceCaptureOwnsNoChildFacingInputControl()
        {
            var fields = typeof(Stage2EvidenceCapture).GetFields(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                Assert.That(typeof(ChildInputRouter).IsAssignableFrom(field.FieldType), Is.False);
                Assert.That(typeof(SemanticControl).IsAssignableFrom(field.FieldType), Is.False);
                Assert.That(typeof(Button).IsAssignableFrom(field.FieldType), Is.False);
            }
        }

        private static bool Parse(string option, string value, out Stage2EvidenceCaptureOptions options) =>
            Stage2EvidenceOptions.TryGetCapture(Append(Defaults(), option, value), out options);

        private static string[] Defaults() => new[]
        {
            "player", "-stage2Capture", "/tmp/stage2.png",
        };

        private static string[] Append(string[] source, string option, string value)
        {
            var result = new string[source.Length + 2];
            System.Array.Copy(source, result, source.Length);
            result[result.Length - 2] = option;
            result[result.Length - 1] = value;
            return result;
        }
    }
}
