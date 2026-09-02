using System.Reflection;
using Exloris.Runtime.Diagnostics;
using Exloris.Runtime.Input;
using NUnit.Framework;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat2EvidenceOptionsTests
    {
        [Test]
        public void ParsesEveryFrozenBoat2EvidenceAxis()
        {
            var arguments = new[]
            {
                "player", "-boat2Capture", "/tmp/boat.png", "-boat2Ratio", "24x10",
                "-boat2Child", "girl", "-boat2Choice", "ibis", "-boat2Sound", "off",
                "-boat2Captions", "on", "-boat2Motion", "reduced", "-boat2Beat", "travel-4",
                "-boat2Overlay", "pause-help", "-boat2Failure", "none", "-boat2CaptureDelay", "1.5",
            };

            Assert.That(Boat2EvidenceOptions.TryGetCapture(arguments, out var options), Is.True);
            Assert.That(options.Path, Is.EqualTo("/tmp/boat.png"));
            Assert.That(options.Ratio, Is.EqualTo(Boat2EvidenceRatio.UltraWide));
            Assert.That(options.Child, Is.EqualTo(Boat2EvidenceChild.Girl));
            Assert.That(options.Choice, Is.EqualTo(Boat2EvidenceChoice.Ibis));
            Assert.That(options.SoundEnabled, Is.False);
            Assert.That(options.CaptionsEnabled, Is.True);
            Assert.That(options.ReducedMotion, Is.True);
            Assert.That(options.Beat, Is.EqualTo(Boat2EvidenceBeat.Travel4));
            Assert.That(options.Overlay, Is.EqualTo(Boat2EvidenceOverlay.PauseHelp));
            Assert.That(options.DelaySeconds, Is.EqualTo(1.5d));
        }

        [TestCase("4x3", Boat2EvidenceRatio.FourThree)]
        [TestCase("16x9", Boat2EvidenceRatio.SixteenNine)]
        [TestCase("24x10", Boat2EvidenceRatio.UltraWide)]
        public void AcceptsOnlyFrozenRatios(string value, Boat2EvidenceRatio expected)
        {
            Assert.That(Parse("-boat2Ratio", value, out var options), Is.True);
            Assert.That(options.Ratio, Is.EqualTo(expected));
        }

        [TestCase("boy", Boat2EvidenceChild.Boy)]
        [TestCase("girl", Boat2EvidenceChild.Girl)]
        public void AcceptsBothSelectedChildren(string value, Boat2EvidenceChild expected)
        {
            Assert.That(Parse("-boat2Child", value, out var options), Is.True);
            Assert.That(options.Child, Is.EqualTo(expected));
        }

        [TestCase("field-bank", Boat2EvidenceChoice.FieldBank)]
        [TestCase("dock", Boat2EvidenceChoice.Dock)]
        [TestCase("ibis", Boat2EvidenceChoice.Ibis)]
        public void AcceptsAllEqualBoatChoices(string value, Boat2EvidenceChoice expected)
        {
            Assert.That(Parse("-boat2Choice", value, out var options), Is.True);
            Assert.That(options.Choice, Is.EqualTo(expected));
        }

        [TestCase("app-pause", Boat2EvidenceOverlay.AppBackgroundPause)]
        [TestCase("audio-focus-pause", Boat2EvidenceOverlay.AudioFocusPause)]
        [TestCase("help-app-pause", Boat2EvidenceOverlay.HelpAppBackgroundPause)]
        [TestCase("help-audio-focus-pause", Boat2EvidenceOverlay.HelpAudioFocusPause)]
        [TestCase("leave-app-pause", Boat2EvidenceOverlay.LeaveAppBackgroundPause)]
        [TestCase("leave-audio-focus-pause", Boat2EvidenceOverlay.LeaveAudioFocusPause)]
        [TestCase("help-app-pause-help-back", Boat2EvidenceOverlay.HelpAppPauseHelpBack)]
        public void AcceptsEveryMappedInterruptionCapture(
            string value,
            Boat2EvidenceOverlay expected)
        {
            Assert.That(Parse("-boat2Overlay", value, out var options), Is.True);
            Assert.That(options.Overlay, Is.EqualTo(expected));
        }

        [TestCase("entry-timeout", Boat2EvidenceFailure.EntryTimeout)]
        [TestCase("entry-asset", Boat2EvidenceFailure.EntryAsset)]
        [TestCase("entry-layout", Boat2EvidenceFailure.EntryLayout)]
        [TestCase("stage2-timeout", Boat2EvidenceFailure.Stage2Timeout)]
        [TestCase("stage2-asset", Boat2EvidenceFailure.Stage2Asset)]
        [TestCase("stage2-layout", Boat2EvidenceFailure.Stage2Layout)]
        public void AcceptsEveryMappedFailure(string value, Boat2EvidenceFailure expected)
        {
            Assert.That(Boat2EvidenceOptions.TryGetCapture(new[]
            {
                "player", "-boat2Capture", "/tmp/boat.png", "-boat2Failure", value,
                "-boat2Overlay", "recovery",
            }, out var options), Is.True);
            Assert.That(options.Failure, Is.EqualTo(expected));
        }

        [TestCase("-boat2Ratio", "portrait")]
        [TestCase("-boat2Child", "profile")]
        [TestCase("-boat2Choice", "automatic")]
        [TestCase("-boat2Beat", "score")]
        [TestCase("-boat2Overlay", "parent-report")]
        [TestCase("-boat2Failure", "hub")]
        [TestCase("-boat2Unknown", "value")]
        public void RejectsUnknownOrForbiddenEvidenceValues(string option, string value)
        {
            Assert.That(Parse(option, value, out _), Is.False);
        }

        [Test]
        public void FailureCaptureRequiresRecoveryOverlay()
        {
            Assert.That(Boat2EvidenceOptions.TryGetCapture(new[]
            {
                "player", "-boat2Capture", "/tmp/boat.png", "-boat2Failure", "stage2-timeout",
                "-boat2Overlay", "none",
            }, out _), Is.False);
            Assert.That(Boat2EvidenceOptions.TryGetCapture(new[]
            {
                "player", "-boat2Capture", "/tmp/boat.png", "-boat2Failure", "stage2-timeout",
                "-boat2Overlay", "recovery",
            }, out _), Is.True);
        }

        [Test]
        public void ProductEvidenceCaptureOwnsNoChildFacingInputControl()
        {
            var fields = typeof(Boat2EvidenceCapture).GetFields(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                Assert.That(typeof(ChildInputRouter).IsAssignableFrom(field.FieldType), Is.False);
                Assert.That(typeof(SemanticControl).IsAssignableFrom(field.FieldType), Is.False);
                Assert.That(typeof(Button).IsAssignableFrom(field.FieldType), Is.False);
            }
        }

        private static bool Parse(string option, string value, out Boat2EvidenceCaptureOptions options)
        {
            return Boat2EvidenceOptions.TryGetCapture(new[]
            {
                "player", "-boat2Capture", "/tmp/boat.png", option, value,
            }, out options);
        }
    }
}
