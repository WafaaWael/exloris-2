using System.Reflection;
using Exloris.Runtime.Diagnostics;
using Exloris.Runtime.Input;
using NUnit.Framework;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage3EvidenceOptionsTests
    {
        [Test]
        public void ParsesEveryFrozenStage3EvidenceAxis()
        {
            var arguments = new[]
            {
                "player", "-stage3Capture", "/tmp/stage3.png",
                "-stage3Ratio", "24x10", "-stage3Child", "girl",
                "-stage3River", "high", "-stage3Plan", "boat",
                "-stage3Sound", "off", "-stage3Captions", "on",
                "-stage3Motion", "reduced", "-stage3Beat", "outcome-need",
                "-stage3Overlay", "pause-help", "-stage3Failure", "none",
                "-stage3Recovery", "none", "-stage3CaptureDelay", "1.5",
            };
            Assert.That(Stage3EvidenceOptions.TryGetCapture(arguments, out var options), Is.True);
            Assert.That(options.Path, Is.EqualTo("/tmp/stage3.png"));
            Assert.That(options.Ratio, Is.EqualTo(Stage3EvidenceRatio.UltraWide));
            Assert.That(options.Child, Is.EqualTo(Stage3EvidenceChild.Girl));
            Assert.That(options.River, Is.EqualTo(Stage3EvidenceRiver.High));
            Assert.That(options.Plan, Is.EqualTo(Stage3EvidencePlan.Boat));
            Assert.That(options.SoundEnabled, Is.False);
            Assert.That(options.CaptionsEnabled, Is.True);
            Assert.That(options.ReducedMotion, Is.True);
            Assert.That(options.Beat, Is.EqualTo(Stage3EvidenceBeat.OutcomeNeed));
            Assert.That(options.Overlay, Is.EqualTo(Stage3EvidenceOverlay.PauseHelp));
            Assert.That(options.Failure, Is.EqualTo(Stage3EvidenceFailure.None));
            Assert.That(options.RecoveryAction, Is.EqualTo(Stage3EvidenceRecoveryAction.None));
            Assert.That(options.DelaySeconds, Is.EqualTo(1.5d));
        }

        [TestCase("4x3", Stage3EvidenceRatio.FourThree)]
        [TestCase("16x9", Stage3EvidenceRatio.SixteenNine)]
        [TestCase("24x10", Stage3EvidenceRatio.UltraWide)]
        public void AcceptsOnlyFrozenRatios(string value, Stage3EvidenceRatio expected)
        {
            Assert.That(Parse("-stage3Ratio", value, out var options), Is.True);
            Assert.That(options.Ratio, Is.EqualTo(expected));
        }

        [TestCase("low", Stage3EvidenceRiver.Low)]
        [TestCase("middle", Stage3EvidenceRiver.Middle)]
        [TestCase("high", Stage3EvidenceRiver.High)]
        public void AcceptsAllEqualRivers(string value, Stage3EvidenceRiver expected)
        {
            Assert.That(Parse("-stage3River", value, out var options), Is.True);
            Assert.That(options.River, Is.EqualTo(expected));
        }

        [TestCase("grain", Stage3EvidencePlan.Grain)]
        [TestCase("channel", Stage3EvidencePlan.Channel)]
        [TestCase("boat", Stage3EvidencePlan.Boat)]
        public void AcceptsAllEqualPlans(string value, Stage3EvidencePlan expected)
        {
            Assert.That(Parse("-stage3Plan", value, out var options), Is.True);
            Assert.That(options.Plan, Is.EqualTo(expected));
        }

        [TestCase("intro", Stage3EvidenceBeat.Intro)]
        [TestCase("choosing-plan", Stage3EvidenceBeat.ChoosingPlan)]
        [TestCase("outcome-start", Stage3EvidenceBeat.OutcomeStart)]
        [TestCase("outcome-reveal", Stage3EvidenceBeat.OutcomeReveal)]
        [TestCase("outcome-benefit", Stage3EvidenceBeat.OutcomeBenefit)]
        [TestCase("outcome-need", Stage3EvidenceBeat.OutcomeNeed)]
        [TestCase("outcome-ready", Stage3EvidenceBeat.OutcomeReady)]
        [TestCase("watch-again", Stage3EvidenceBeat.WatchAgain)]
        [TestCase("another-plan", Stage3EvidenceBeat.AnotherPlan)]
        [TestCase("change-river", Stage3EvidenceBeat.ChangeRiver)]
        [TestCase("river-back", Stage3EvidenceBeat.RiverBack)]
        [TestCase("completion-loading", Stage3EvidenceBeat.CompletionLoading)]
        public void AcceptsEveryFrozenBeat(string value, Stage3EvidenceBeat expected)
        {
            Assert.That(Parse("-stage3Beat", value, out var options), Is.True);
            Assert.That(options.Beat, Is.EqualTo(expected));
        }

        [TestCase("entry-timeout", Stage3EvidenceFailure.EntryTimeout)]
        [TestCase("entry-asset", Stage3EvidenceFailure.EntryAsset)]
        [TestCase("entry-layout", Stage3EvidenceFailure.EntryLayout)]
        [TestCase("completion-timeout", Stage3EvidenceFailure.CompletionTimeout)]
        [TestCase("completion-asset", Stage3EvidenceFailure.CompletionAsset)]
        [TestCase("completion-layout", Stage3EvidenceFailure.CompletionLayout)]
        public void AcceptsEveryMappedFailure(string value, Stage3EvidenceFailure expected)
        {
            var arguments = Append(Defaults(), "-stage3Failure", value);
            arguments = Append(arguments, "-stage3Overlay", "recovery");
            if (value.StartsWith("completion-", System.StringComparison.Ordinal))
                arguments = Append(arguments, "-stage3Beat", "completion-loading");
            Assert.That(Stage3EvidenceOptions.TryGetCapture(arguments, out var options), Is.True);
            Assert.That(options.Failure, Is.EqualTo(expected));
        }

        [Test]
        public void RejectsUnknownDuplicateAndContradictoryRequests()
        {
            Assert.That(Parse("-stage3Ratio", "portrait", out _), Is.False);
            Assert.That(Parse("-stage3River", "automatic", out _), Is.False);
            Assert.That(Parse("-stage3Plan", "recommended", out _), Is.False);
            Assert.That(Parse("-stage3Unknown", "value", out _), Is.False);
            Assert.That(Stage3EvidenceOptions.TryGetCapture(new[]
            {
                "player", "-stage3Capture", "/tmp/a.png", "-stage3Capture", "/tmp/b.png",
            }, out _), Is.False);
            Assert.That(Stage3EvidenceOptions.TryGetCapture(
                Append(Defaults(), "-stage3Failure", "completion-timeout"), out _), Is.False);
        }

        [Test]
        public void ProductEvidenceCaptureOwnsNoChildFacingInputControl()
        {
            var fields = typeof(Stage3EvidenceCapture).GetFields(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                Assert.That(typeof(ChildInputRouter).IsAssignableFrom(field.FieldType), Is.False);
                Assert.That(typeof(SemanticControl).IsAssignableFrom(field.FieldType), Is.False);
                Assert.That(typeof(Button).IsAssignableFrom(field.FieldType), Is.False);
            }
        }

        private static bool Parse(string option, string value, out Stage3EvidenceCaptureOptions options) =>
            Stage3EvidenceOptions.TryGetCapture(Append(Defaults(), option, value), out options);

        private static string[] Defaults() => new[]
        {
            "player", "-stage3Capture", "/tmp/stage3.png",
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
