using Exloris.Runtime.Diagnostics;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class ReleaseSoakGateTests
    {
        [Test]
        public void ThirtyMinuteBoundaryWithCompleteRouteAndHealthyMetricsPasses()
        {
            Assert.That(ReleaseSoakGate.Passes(
                1800d, 15, 8, 8, 33.3f, 50f, 2, 512f, 1, false), Is.True);
        }

        [TestCase(1799.999d, 15, 8, 8, 20f, 30f, 0, 300f, 1, false)]
        [TestCase(1800d, 14, 8, 8, 20f, 30f, 0, 300f, 1, false)]
        [TestCase(1800d, 15, 0, 8, 20f, 30f, 0, 300f, 1, false)]
        [TestCase(1800d, 15, 8, 0, 20f, 30f, 0, 300f, 1, false)]
        [TestCase(1800d, 15, 8, 8, 33.31f, 30f, 0, 300f, 1, false)]
        [TestCase(1800d, 15, 8, 8, 20f, 50.01f, 0, 300f, 1, false)]
        [TestCase(1800d, 15, 8, 8, 20f, 30f, 3, 300f, 1, false)]
        [TestCase(1800d, 15, 8, 8, 20f, 30f, 0, 512.01f, 1, false)]
        [TestCase(1800d, 15, 8, 8, 20f, 30f, 0, 300f, 2, false)]
        [TestCase(1800d, 15, 8, 8, 20f, 30f, 0, 300f, 1, true)]
        public void AnyReleaseBoundaryViolationFails(
            double elapsed, int routes, int pauses, int interruptions, float p95, float p99,
            int consecutiveSlow, float reservedMiB, int activeVoices, bool routeDrift)
        {
            Assert.That(ReleaseSoakGate.Passes(
                elapsed, routes, pauses, interruptions, p95, p99,
                consecutiveSlow, reservedMiB, activeVoices, routeDrift), Is.False);
        }

        [Test]
        public void StartupLoadingCaptureAloneHoldsTheStartupRoute()
        {
            Assert.That(ReleaseCandidateEvidenceCapture.ShouldHoldStartupLoading(new[]
            {
                "Exloris2", "-releaseCapture", "/capture.png",
                "-releaseTarget", "startup_loading",
            }), Is.True);
            Assert.That(ReleaseCandidateEvidenceCapture.ShouldHoldStartupLoading(new[]
            {
                "Exloris2", "-releaseCapture", "/capture.png",
                "-releaseTarget", "intro_video",
            }), Is.False);
            Assert.That(ReleaseCandidateEvidenceCapture.ShouldHoldStartupLoading(new[]
            {
                "Exloris2", "-releaseTarget", "startup_loading",
            }), Is.False);
        }

        [Test]
        public void ReleaseCaptureKeepsTheEvidencePlayerRenderingWhenAutomationLosesFocus()
        {
            Assert.That(ReleaseCandidateEvidenceCapture.ShouldRunInBackgroundForEvidence(new[]
            {
                "Exloris2", "-releaseCapture", "/capture.png",
            }), Is.True);
            Assert.That(ReleaseCandidateEvidenceCapture.ShouldRunInBackgroundForEvidence(new[]
            {
                "Exloris2",
            }), Is.False);
        }

        [Test]
        public void SoakFrameSamplingBeginsOnlyAfterResolutionWarmup()
        {
            Assert.That(ReleaseCandidateEvidenceCapture.ShouldSampleSoakFrame(true, false), Is.False);
            Assert.That(ReleaseCandidateEvidenceCapture.ShouldSampleSoakFrame(false, true), Is.False);
            Assert.That(ReleaseCandidateEvidenceCapture.ShouldSampleSoakFrame(true, true), Is.True);
        }
    }
}
