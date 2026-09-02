using System.Reflection;
using Exloris.Runtime.Diagnostics;
using Exloris.Runtime.Input;
using NUnit.Framework;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat3EvidenceOptionsTests
    {
        [Test]
        public void ParsesEveryFrozenBoat3EvidenceAxis()
        {
            var arguments = new[]
            {
                "player", "-boat3Capture", "/tmp/boat.png", "-boat3Ratio", "24x10",
                "-boat3Child", "girl", "-boat3Choice", "ibis", "-boat3Sound", "off",
                "-boat3Captions", "on", "-boat3Motion", "reduced", "-boat3Beat", "travel-4",
                "-boat3Overlay", "pause-help", "-boat3Failure", "none", "-boat3CaptureDelay", "1.5",
            };

            Assert.That(Boat3EvidenceOptions.TryGetCapture(arguments, out var options), Is.True);
            Assert.That(options.Path, Is.EqualTo("/tmp/boat.png"));
            Assert.That(options.Ratio, Is.EqualTo(Boat3EvidenceRatio.UltraWide));
            Assert.That(options.Child, Is.EqualTo(Boat3EvidenceChild.Girl));
            Assert.That(options.Choice, Is.EqualTo(Boat3EvidenceChoice.Ibis));
            Assert.That(options.SoundEnabled, Is.False);
            Assert.That(options.CaptionsEnabled, Is.True);
            Assert.That(options.ReducedMotion, Is.True);
            Assert.That(options.Beat, Is.EqualTo(Boat3EvidenceBeat.Travel4));
            Assert.That(options.Overlay, Is.EqualTo(Boat3EvidenceOverlay.PauseHelp));
            Assert.That(options.DelaySeconds, Is.EqualTo(1.5d));
        }

        [TestCase("4x3", Boat3EvidenceRatio.FourThree)]
        [TestCase("16x9", Boat3EvidenceRatio.SixteenNine)]
        [TestCase("24x10", Boat3EvidenceRatio.UltraWide)]
        public void AcceptsOnlyFrozenRatios(string value, Boat3EvidenceRatio expected)
        {
            Assert.That(Parse("-boat3Ratio", value, out var options), Is.True);
            Assert.That(options.Ratio, Is.EqualTo(expected));
        }

        [TestCase("boy", Boat3EvidenceChild.Boy)]
        [TestCase("girl", Boat3EvidenceChild.Girl)]
        public void AcceptsBothSelectedChildren(string value, Boat3EvidenceChild expected)
        {
            Assert.That(Parse("-boat3Child", value, out var options), Is.True);
            Assert.That(options.Child, Is.EqualTo(expected));
        }

        [TestCase("grain-store", Boat3EvidenceChoice.GrainStore)]
        [TestCase("irrigation", Boat3EvidenceChoice.Irrigation)]
        [TestCase("ibis", Boat3EvidenceChoice.Ibis)]
        public void AcceptsAllEqualBoatChoices(string value, Boat3EvidenceChoice expected)
        {
            Assert.That(Parse("-boat3Choice", value, out var options), Is.True);
            Assert.That(options.Choice, Is.EqualTo(expected));
        }

        [TestCase("app-pause", Boat3EvidenceOverlay.AppBackgroundPause)]
        [TestCase("audio-focus-pause", Boat3EvidenceOverlay.AudioFocusPause)]
        [TestCase("help-app-pause", Boat3EvidenceOverlay.HelpAppBackgroundPause)]
        [TestCase("help-audio-focus-pause", Boat3EvidenceOverlay.HelpAudioFocusPause)]
        [TestCase("leave-app-pause", Boat3EvidenceOverlay.LeaveAppBackgroundPause)]
        [TestCase("leave-audio-focus-pause", Boat3EvidenceOverlay.LeaveAudioFocusPause)]
        [TestCase("help-app-pause-help-back", Boat3EvidenceOverlay.HelpAppPauseHelpBack)]
        public void AcceptsEveryMappedInterruptionCapture(
            string value,
            Boat3EvidenceOverlay expected)
        {
            Assert.That(Parse("-boat3Overlay", value, out var options), Is.True);
            Assert.That(options.Overlay, Is.EqualTo(expected));
        }

        [TestCase("entry-timeout", Boat3EvidenceFailure.EntryTimeout)]
        [TestCase("entry-asset", Boat3EvidenceFailure.EntryAsset)]
        [TestCase("entry-layout", Boat3EvidenceFailure.EntryLayout)]
        [TestCase("stage3-timeout", Boat3EvidenceFailure.Stage3Timeout)]
        [TestCase("stage3-asset", Boat3EvidenceFailure.Stage3Asset)]
        [TestCase("stage3-layout", Boat3EvidenceFailure.Stage3Layout)]
        public void AcceptsEveryMappedFailure(string value, Boat3EvidenceFailure expected)
        {
            Assert.That(Boat3EvidenceOptions.TryGetCapture(new[]
            {
                "player", "-boat3Capture", "/tmp/boat.png", "-boat3Failure", value,
                "-boat3Overlay", "recovery",
            }, out var options), Is.True);
            Assert.That(options.Failure, Is.EqualTo(expected));
        }

        [TestCase("-boat3Ratio", "portrait")]
        [TestCase("-boat3Child", "profile")]
        [TestCase("-boat3Choice", "automatic")]
        [TestCase("-boat3Beat", "score")]
        [TestCase("-boat3Overlay", "parent-report")]
        [TestCase("-boat3Failure", "hub")]
        [TestCase("-boat3Unknown", "value")]
        public void RejectsUnknownOrForbiddenEvidenceValues(string option, string value)
        {
            Assert.That(Parse(option, value, out _), Is.False);
        }

        [Test]
        public void FailureCaptureRequiresRecoveryOverlay()
        {
            Assert.That(Boat3EvidenceOptions.TryGetCapture(new[]
            {
                "player", "-boat3Capture", "/tmp/boat.png", "-boat3Failure", "stage3-timeout",
                "-boat3Overlay", "none",
            }, out _), Is.False);
            Assert.That(Boat3EvidenceOptions.TryGetCapture(new[]
            {
                "player", "-boat3Capture", "/tmp/boat.png", "-boat3Failure", "stage3-timeout",
                "-boat3Overlay", "recovery",
            }, out _), Is.True);
        }

        [Test]
        public void ProductEvidenceCaptureOwnsNoChildFacingInputControl()
        {
            var fields = typeof(Boat3EvidenceCapture).GetFields(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                Assert.That(typeof(ChildInputRouter).IsAssignableFrom(field.FieldType), Is.False);
                Assert.That(typeof(SemanticControl).IsAssignableFrom(field.FieldType), Is.False);
                Assert.That(typeof(Button).IsAssignableFrom(field.FieldType), Is.False);
            }
        }

        private static bool Parse(string option, string value, out Boat3EvidenceCaptureOptions options)
        {
            return Boat3EvidenceOptions.TryGetCapture(new[]
            {
                "player", "-boat3Capture", "/tmp/boat.png", option, value,
            }, out options);
        }
    }
}
