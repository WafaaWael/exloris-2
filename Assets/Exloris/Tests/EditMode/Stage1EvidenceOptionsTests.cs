using System.Reflection;
using Exloris.Runtime.Diagnostics;
using Exloris.Runtime.Input;
using NUnit.Framework;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage1EvidenceOptionsTests
    {
        [Test]
        public void ParsesEveryFrozenStage1EvidenceAxis()
        {
            var arguments = new[]
            {
                "player", "-stage1Capture", "/tmp/stage1.png",
                "-stage1Ratio", "24x10", "-stage1Child", "girl",
                "-stage1Sound", "off", "-stage1Captions", "on",
                "-stage1Motion", "reduced", "-stage1Beat", "playing",
                "-stage1Attach", "drag", "-stage1Position", "maximum",
                "-stage1Support", "3", "-stage1Overlay", "pause-help",
                "-stage1Failure", "none", "-stage1Recovery", "none",
                "-stage1CaptureDelay", "1.25"
            };

            Assert.That(Stage1EvidenceOptions.TryGetCapture(arguments, out var options), Is.True);
            Assert.That(options.Ratio, Is.EqualTo(Stage1EvidenceRatio.UltraWide));
            Assert.That(options.Child, Is.EqualTo(Stage1EvidenceChild.Girl));
            Assert.That(options.SoundEnabled, Is.False);
            Assert.That(options.CaptionsEnabled, Is.True);
            Assert.That(options.ReducedMotion, Is.True);
            Assert.That(options.Beat, Is.EqualTo(Stage1EvidenceBeat.Playing));
            Assert.That(options.Attach, Is.EqualTo(Stage1EvidenceAttach.Drag));
            Assert.That(options.Position, Is.EqualTo(Stage1EvidencePosition.Maximum));
            Assert.That(options.SupportTier, Is.EqualTo(3));
            Assert.That(options.Overlay, Is.EqualTo(Stage1EvidenceOverlay.PauseHelp));
            Assert.That(options.Failure, Is.EqualTo(Stage1EvidenceFailure.None));
            Assert.That(options.RecoveryAction, Is.EqualTo(Stage1EvidenceRecoveryAction.None));
            Assert.That(options.DelaySeconds, Is.EqualTo(1.25d));
        }

        [TestCase("4x3", Stage1EvidenceRatio.FourThree)]
        [TestCase("16x9", Stage1EvidenceRatio.SixteenNine)]
        [TestCase("24x10", Stage1EvidenceRatio.UltraWide)]
        public void AcceptsOnlyFrozenRatios(string value, Stage1EvidenceRatio expected)
        {
            Assert.That(Parse("-stage1Ratio", value, out var options), Is.True);
            Assert.That(options.Ratio, Is.EqualTo(expected));
        }

        [TestCase("minimum", Stage1EvidencePosition.Minimum)]
        [TestCase("low-boundary", Stage1EvidencePosition.LowBoundary)]
        [TestCase("middle", Stage1EvidencePosition.Middle)]
        [TestCase("high-boundary", Stage1EvidencePosition.HighBoundary)]
        [TestCase("maximum", Stage1EvidencePosition.Maximum)]
        public void AcceptsFrozenMarkerPositions(string value, Stage1EvidencePosition expected)
        {
            Assert.That(Parse("-stage1Position", value, out var options), Is.True);
            Assert.That(options.Position, Is.EqualTo(expected));
        }

        [TestCase("entry-timeout", Stage1EvidenceFailure.EntryTimeout)]
        [TestCase("entry-asset", Stage1EvidenceFailure.EntryAsset)]
        [TestCase("entry-layout", Stage1EvidenceFailure.EntryLayout)]
        [TestCase("boat2-timeout", Stage1EvidenceFailure.Boat2Timeout)]
        [TestCase("boat2-asset", Stage1EvidenceFailure.Boat2Asset)]
        [TestCase("boat2-layout", Stage1EvidenceFailure.Boat2Layout)]
        public void AcceptsEveryMappedFailure(string value, Stage1EvidenceFailure expected)
        {
            var arguments = Defaults();
            arguments[System.Array.IndexOf(arguments, "none", 1)] = value;
            System.Array.Resize(ref arguments, arguments.Length + 2);
            arguments[arguments.Length - 2] = "-stage1Overlay";
            arguments[arguments.Length - 1] = "recovery";
            Assert.That(Stage1EvidenceOptions.TryGetCapture(arguments, out var options), Is.True);
            Assert.That(options.Failure, Is.EqualTo(expected));
        }

        [Test]
        public void RejectsUnknownDuplicateOrContradictoryArguments()
        {
            Assert.That(Parse("-stage1Ratio", "square", out _), Is.False);
            Assert.That(Stage1EvidenceOptions.TryGetCapture(new[]
            {
                "player", "-stage1Capture", "/tmp/a.png", "-stage1Capture", "/tmp/b.png"
            }, out _), Is.False);
            Assert.That(Stage1EvidenceOptions.TryGetCapture(new[]
            {
                "player", "-stage1Capture", "/tmp/a.png", "-stage1Unknown", "x"
            }, out _), Is.False);
            Assert.That(Stage1EvidenceOptions.TryGetCapture(new[]
            {
                "player", "-stage1Capture", "/tmp/a.png", "-stage1Failure", "none",
                "-stage1Recovery", "retry"
            }, out _), Is.False);
        }

        [Test]
        public void ProductEvidenceCaptureOwnsNoChildFacingInputControl()
        {
            var fields = typeof(Stage1EvidenceCapture).GetFields(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                Assert.That(typeof(ChildInputRouter).IsAssignableFrom(field.FieldType), Is.False);
                Assert.That(typeof(SemanticControl).IsAssignableFrom(field.FieldType), Is.False);
                Assert.That(typeof(Button).IsAssignableFrom(field.FieldType), Is.False);
            }
        }

        private static bool Parse(string option, string value, out Stage1EvidenceCaptureOptions result)
        {
            return Stage1EvidenceOptions.TryGetCapture(new[]
            {
                "player", "-stage1Capture", "/tmp/stage1.png", option, value
            }, out result);
        }

        private static string[] Defaults() => new[]
        {
            "player", "-stage1Capture", "/tmp/stage1.png", "-stage1Failure", "none"
        };
    }
}
