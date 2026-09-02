using System.Reflection;
using Exloris.Runtime.Diagnostics;
using Exloris.Runtime.Input;
using NUnit.Framework;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat1EvidenceOptionsTests
    {
        [Test]
        public void ParsesEveryFrozenBoatEvidenceAxis()
        {
            var arguments = new[]
            {
                "player", "-boatCapture", "/tmp/boat.png", "-boatRatio", "24x10",
                "-boatChild", "girl", "-boatChoice", "ibis", "-boatSound", "off",
                "-boatCaptions", "on", "-boatMotion", "reduced", "-boatBeat", "travel-4",
                "-boatOverlay", "pause-help", "-boatFailure", "none", "-boatCaptureDelay", "1.5",
            };

            Assert.That(Boat1EvidenceOptions.TryGetCapture(arguments, out var options), Is.True);
            Assert.That(options.Path, Is.EqualTo("/tmp/boat.png"));
            Assert.That(options.Ratio, Is.EqualTo(BoatEvidenceRatio.UltraWide));
            Assert.That(options.Child, Is.EqualTo(BoatEvidenceChild.Girl));
            Assert.That(options.Choice, Is.EqualTo(BoatEvidenceChoice.Ibis));
            Assert.That(options.SoundEnabled, Is.False);
            Assert.That(options.CaptionsEnabled, Is.True);
            Assert.That(options.ReducedMotion, Is.True);
            Assert.That(options.Beat, Is.EqualTo(BoatEvidenceBeat.Travel4));
            Assert.That(options.Overlay, Is.EqualTo(BoatEvidenceOverlay.PauseHelp));
            Assert.That(options.DelaySeconds, Is.EqualTo(1.5d));
        }

        [TestCase("4x3", BoatEvidenceRatio.FourThree)]
        [TestCase("16x9", BoatEvidenceRatio.SixteenNine)]
        [TestCase("24x10", BoatEvidenceRatio.UltraWide)]
        public void AcceptsOnlyFrozenRatios(string value, BoatEvidenceRatio expected)
        {
            Assert.That(Parse("-boatRatio", value, out var options), Is.True);
            Assert.That(options.Ratio, Is.EqualTo(expected));
        }

        [TestCase("boy", BoatEvidenceChild.Boy)]
        [TestCase("girl", BoatEvidenceChild.Girl)]
        public void AcceptsBothSelectedChildren(string value, BoatEvidenceChild expected)
        {
            Assert.That(Parse("-boatChild", value, out var options), Is.True);
            Assert.That(options.Child, Is.EqualTo(expected));
        }

        [TestCase("reeds", BoatEvidenceChoice.Reeds)]
        [TestCase("village", BoatEvidenceChoice.Village)]
        [TestCase("ibis", BoatEvidenceChoice.Ibis)]
        public void AcceptsAllEqualBoatChoices(string value, BoatEvidenceChoice expected)
        {
            Assert.That(Parse("-boatChoice", value, out var options), Is.True);
            Assert.That(options.Choice, Is.EqualTo(expected));
        }

        [TestCase("app-pause", BoatEvidenceOverlay.AppBackgroundPause)]
        [TestCase("audio-focus-pause", BoatEvidenceOverlay.AudioFocusPause)]
        [TestCase("help-app-pause", BoatEvidenceOverlay.HelpAppBackgroundPause)]
        [TestCase("help-audio-focus-pause", BoatEvidenceOverlay.HelpAudioFocusPause)]
        [TestCase("leave-app-pause", BoatEvidenceOverlay.LeaveAppBackgroundPause)]
        [TestCase("leave-audio-focus-pause", BoatEvidenceOverlay.LeaveAudioFocusPause)]
        [TestCase("help-app-pause-help-back", BoatEvidenceOverlay.HelpAppPauseHelpBack)]
        public void AcceptsEveryMappedInterruptionCapture(
            string value,
            BoatEvidenceOverlay expected)
        {
            Assert.That(Parse("-boatOverlay", value, out var options), Is.True);
            Assert.That(options.Overlay, Is.EqualTo(expected));
        }

        [TestCase("-boatRatio", "portrait")]
        [TestCase("-boatChild", "profile")]
        [TestCase("-boatChoice", "automatic")]
        [TestCase("-boatBeat", "score")]
        [TestCase("-boatOverlay", "parent-report")]
        [TestCase("-boatFailure", "hub")]
        [TestCase("-boatUnknown", "value")]
        public void RejectsUnknownOrForbiddenEvidenceValues(string option, string value)
        {
            Assert.That(Parse(option, value, out _), Is.False);
        }

        [Test]
        public void FailureCaptureRequiresRecoveryOverlay()
        {
            Assert.That(Boat1EvidenceOptions.TryGetCapture(new[]
            {
                "player", "-boatCapture", "/tmp/boat.png", "-boatFailure", "stage1-timeout",
                "-boatOverlay", "none",
            }, out _), Is.False);
            Assert.That(Boat1EvidenceOptions.TryGetCapture(new[]
            {
                "player", "-boatCapture", "/tmp/boat.png", "-boatFailure", "stage1-timeout",
                "-boatOverlay", "recovery",
            }, out _), Is.True);
        }

        [Test]
        public void ProductEvidenceCaptureOwnsNoChildFacingInputControl()
        {
            var fields = typeof(Boat1EvidenceCapture).GetFields(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                Assert.That(typeof(ChildInputRouter).IsAssignableFrom(field.FieldType), Is.False);
                Assert.That(typeof(SemanticControl).IsAssignableFrom(field.FieldType), Is.False);
                Assert.That(typeof(Button).IsAssignableFrom(field.FieldType), Is.False);
            }
        }

        private static bool Parse(string option, string value, out Boat1EvidenceCaptureOptions options)
        {
            return Boat1EvidenceOptions.TryGetCapture(new[]
            {
                "player", "-boatCapture", "/tmp/boat.png", option, value,
            }, out options);
        }
    }
}
