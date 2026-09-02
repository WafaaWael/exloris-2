using Exloris.Runtime.Intro;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class IntroEvidenceOptionsTests
    {
        [TestCase("full", false)]
        [TestCase("reduced", true)]
        public void ExplicitEvidenceMotionModeIsParsedWithoutChangingShippingDefault(
            string value,
            bool expectedReduced)
        {
            var parsed = IntroEvidenceOptions.TryGetReducedMotion(
                new[] { "player", "-introEvidenceMotion", value },
                out var reduced);

            Assert.That(parsed, Is.True);
            Assert.That(reduced, Is.EqualTo(expectedReduced));
            Assert.That(IntroEvidenceOptions.TryGetReducedMotion(new[] { "player" }, out _), Is.False);
        }

        [Test]
        public void EvidenceCaptureOptionsRequireAnExplicitOutputAndAllowCaptionProof()
        {
            var args = new[]
            {
                "player", "-introCapture", "/tmp/intro.png", "-introCaptureAt", "2.4",
                "-introCaptions", "-introMuted"
            };

            Assert.That(IntroEvidenceOptions.TryGetCapture(args, out var capture), Is.True);
            Assert.That(capture.Path, Is.EqualTo("/tmp/intro.png"));
            Assert.That(capture.AtSeconds, Is.EqualTo(2.4d));
            Assert.That(capture.CaptionsEnabled, Is.True);
            Assert.That(capture.Muted, Is.True);
        }
    }
}
