using Exloris.Runtime.Intro;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class IntroSynchronizationTests
    {
        [Test]
        public void SynchronizationGuardEnforcesOverlayAndAccessibleCueDriftLimits()
        {
            Assert.That(IntroSynchronizationContract.MaximumOverlayDriftSeconds, Is.EqualTo(0.0333d));
            Assert.That(IntroSynchronizationContract.MaximumAccessibleCueDriftSeconds, Is.EqualTo(0.100d));
            Assert.That(IntroSynchronizationContract.FullIbisEntrySeconds, Is.EqualTo(2.4d));
            Assert.That(IntroSynchronizationContract.FullIbisSettleSeconds, Is.EqualTo(7.2d));
            Assert.That(IntroSynchronizationContract.ReducedStaticIbisSeconds, Is.EqualTo(2.8d));

            Assert.That(IntroSynchronizationContract.IsOverlaySynchronized(2.4d, 2.4333d), Is.True);
            Assert.That(IntroSynchronizationContract.IsOverlaySynchronized(2.4d, 2.4334d), Is.False);
            Assert.That(IntroSynchronizationContract.AreAccessibleCuesSynchronized(0d, 0.100d, 0.099d), Is.True);
            Assert.That(IntroSynchronizationContract.AreAccessibleCuesSynchronized(0d, 0.1001d, 0d), Is.False);
        }

        [TestCase(false, 288)]
        [TestCase(true, 120)]
        public void EveryAuthoredFrameUsesOneClockForIbisVoiceCaptionAndVisualEquivalent(
            bool reducedMotion,
            int frameCount)
        {
            var maximumMarkerError = 0d;
            for (var frame = 0; frame < frameCount; frame++)
            {
                var mediaTime = IntroSynchronizationContract.FrameTimeSeconds(frame);
                var snapshot = IntroSynchronizationContract.Evaluate(reducedMotion, mediaTime);

                Assert.That(snapshot.MediaTimeSeconds, Is.EqualTo(mediaTime).Within(0.0000001d));
                Assert.That(snapshot.CaptionVisible, Is.EqualTo(snapshot.VoiceCueActive));
                Assert.That(snapshot.VisualEquivalentVisible, Is.EqualTo(snapshot.VoiceCueActive));
            }

            var markers = reducedMotion
                ? new[] { IntroSynchronizationContract.ReducedStaticIbisSeconds }
                : new[]
                {
                    IntroSynchronizationContract.FullIbisEntrySeconds,
                    IntroSynchronizationContract.FullIbisHeadLiftSeconds,
                    IntroSynchronizationContract.FullIbisDirectionSeconds,
                    IntroSynchronizationContract.FullIbisSettleSeconds
                };
            foreach (var marker in markers)
            {
                var nearestFrame = (int)System.Math.Round(marker * IntroVariantSelector.FrameRate);
                var frameTime = IntroSynchronizationContract.FrameTimeSeconds(nearestFrame);
                maximumMarkerError = System.Math.Max(maximumMarkerError, System.Math.Abs(frameTime - marker));
            }

            Assert.That(
                maximumMarkerError,
                Is.LessThanOrEqualTo(IntroSynchronizationContract.MaximumOverlayDriftSeconds));
        }

        [Test]
        public void ReducedMotionUsesOnlyStaticIbisAndAccessibleCueParity()
        {
            var before = IntroSynchronizationContract.Evaluate(
                reducedMotion: true,
                IntroSynchronizationContract.ReducedStaticIbisSeconds - 0.001d);
            var atMarker = IntroSynchronizationContract.Evaluate(
                reducedMotion: true,
                IntroSynchronizationContract.ReducedStaticIbisSeconds);

            Assert.That(before.IbisPose, Is.EqualTo(IntroIbisPose.Hidden));
            Assert.That(atMarker.IbisPose, Is.EqualTo(IntroIbisPose.StaticReduced));
            Assert.That(atMarker.CaptionVisible, Is.EqualTo(atMarker.VoiceCueActive));
            Assert.That(atMarker.VisualEquivalentVisible, Is.EqualTo(atMarker.VoiceCueActive));
        }
    }
}
