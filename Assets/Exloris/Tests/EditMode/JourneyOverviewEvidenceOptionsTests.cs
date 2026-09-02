using System;
using System.Reflection;
using Exloris.Runtime.Diagnostics;
using Exloris.Runtime.Input;
using NUnit.Framework;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class JourneyOverviewEvidenceOptionsTests
    {
        [Test]
        public void CaptureRequiresPathAndParsesEveryRequestedProofAxis()
        {
            var arguments = new[]
            {
                "player", "-journeyCapture", "/tmp/journey.png",
                "-journeyRatio", "24x10", "-journeySound", "off",
                "-journeyCaptions", "on", "-journeyMotion", "reduced",
                "-journeyBeat", "read", "-journeyOverlay", "help",
                "-journeyFailure", "none", "-journeyProbe", "help-pictograms",
                "-journeyCaptureDelay", "5.5",
            };

            Assert.That(JourneyOverviewEvidenceOptions.TryGetCapture(
                arguments, out var options), Is.True);
            Assert.That(options.Path, Is.EqualTo("/tmp/journey.png"));
            Assert.That(options.Ratio, Is.EqualTo(JourneyEvidenceRatio.UltraWide));
            Assert.That(options.SoundEnabled, Is.False);
            Assert.That(options.CaptionsEnabled, Is.True);
            Assert.That(options.ReducedMotion, Is.True);
            Assert.That(options.Beat, Is.EqualTo(JourneyEvidenceBeat.Read));
            Assert.That(options.Overlay, Is.EqualTo(JourneyEvidenceOverlay.Help));
            Assert.That(options.Failure, Is.EqualTo(JourneyEvidenceFailure.None));
            Assert.That(options.Probe, Is.EqualTo(JourneyEvidenceProbe.HelpPictograms));
            Assert.That(options.DelaySeconds, Is.EqualTo(5.5d));
            Assert.That(JourneyOverviewEvidenceOptions.TryGetCapture(
                new[] { "player" }, out _), Is.False);
        }

        [TestCase("4x3", JourneyEvidenceRatio.FourThree)]
        [TestCase("16x9", JourneyEvidenceRatio.SixteenNine)]
        [TestCase("24x10", JourneyEvidenceRatio.UltraWide)]
        public void OnlyFrozenRatiosAreAccepted(string value, JourneyEvidenceRatio expected)
        {
            Assert.That(Parse("-journeyRatio", value, out var options), Is.True);
            Assert.That(options.Ratio, Is.EqualTo(expected));
        }

        [TestCase("portrait")]
        [TestCase("device")]
        [TestCase("0")]
        [TestCase("FourThree")]
        public void UnknownOrNumericRatiosAreRejected(string value)
        {
            Assert.That(Parse("-journeyRatio", value, out _), Is.False);
        }

        [TestCase("-journeyBeat", "boat1")]
        [TestCase("-journeyOverlay", "parent-report")]
        [TestCase("-journeyFailure", "avatar")]
        [TestCase("-journeyProbe", "hub")]
        [TestCase("-journeySound", "mute")]
        [TestCase("-journeyCaptions", "1")]
        [TestCase("-journeyMotion", "0")]
        [TestCase("-journeyUnknown", "value")]
        public void UnknownEvidenceValuesAndOptionsAreRejected(string option, string value)
        {
            Assert.That(Parse(option, value, out _), Is.False);
        }

        [TestCase("-0.1")]
        [TestCase("30.1")]
        [TestCase("NaN")]
        [TestCase("Infinity")]
        [TestCase("not-a-number")]
        public void DelayOutsideZeroToThirtySecondsOrNonFiniteIsRejected(string value)
        {
            Assert.That(Parse("-journeyCaptureDelay", value, out _), Is.False);
        }

        [Test]
        public void ImpossibleEvidenceCombinationsAreRejected()
        {
            Assert.That(JourneyOverviewEvidenceOptions.TryGetCapture(new[]
            {
                "player", "-journeyCapture", "/tmp/journey.png",
                "-journeyProbe", "help-pictograms", "-journeyOverlay", "pause",
            }, out _), Is.False);
            Assert.That(JourneyOverviewEvidenceOptions.TryGetCapture(new[]
            {
                "player", "-journeyCapture", "/tmp/journey.png",
                "-journeyProbe", "locked-not-yet", "-journeyBeat", "ready",
            }, out _), Is.False);
            Assert.That(JourneyOverviewEvidenceOptions.TryGetCapture(new[]
            {
                "player", "-journeyCapture", "/tmp/journey.png",
                "-journeyFailure", "layout", "-journeyOverlay", "none",
            }, out _), Is.False);
        }

        [Test]
        public void ProductEvidenceCaptureOwnsNoChildFacingInputControl()
        {
            var fields = typeof(JourneyOverviewEvidenceCapture).GetFields(
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                Assert.That(typeof(ChildInputRouter).IsAssignableFrom(field.FieldType), Is.False);
                Assert.That(typeof(SemanticControl).IsAssignableFrom(field.FieldType), Is.False);
                Assert.That(typeof(Button).IsAssignableFrom(field.FieldType), Is.False);
            }
        }

        private static bool Parse(
            string option,
            string value,
            out JourneyOverviewEvidenceCaptureOptions options)
        {
            return JourneyOverviewEvidenceOptions.TryGetCapture(new[]
            {
                "player", "-journeyCapture", "/tmp/journey.png", option, value,
            }, out options);
        }
    }
}
