using System;

namespace Exloris.Runtime.Intro
{
    public enum IntroIbisPose
    {
        Hidden,
        WingsOpen,
        HeadLift,
        BeakDirection,
        CalmSettle,
        StaticReduced
    }

    public readonly struct IntroCueSnapshot
    {
        public IntroCueSnapshot(
            double mediaTimeSeconds,
            IntroIbisPose ibisPose,
            bool voiceCueActive,
            bool captionVisible,
            bool visualEquivalentVisible)
        {
            MediaTimeSeconds = mediaTimeSeconds;
            IbisPose = ibisPose;
            VoiceCueActive = voiceCueActive;
            CaptionVisible = captionVisible;
            VisualEquivalentVisible = visualEquivalentVisible;
        }

        public double MediaTimeSeconds { get; }
        public IntroIbisPose IbisPose { get; }
        public bool VoiceCueActive { get; }
        public bool CaptionVisible { get; }
        public bool VisualEquivalentVisible { get; }
    }

    public static class IntroSynchronizationContract
    {
        public const double MaximumOverlayDriftSeconds = 0.0333d;
        public const double MaximumAccessibleCueDriftSeconds = 0.100d;
        public const double FullIbisEntrySeconds = 2.4d;
        public const double FullIbisHeadLiftSeconds = 2.67d;
        public const double FullIbisDirectionSeconds = 2.94d;
        public const double FullIbisSettleSeconds = 7.2d;
        public const double ReducedStaticIbisSeconds = 2.8d;
        public const double AccessibleCueEndSeconds = 3.324d;

        private const double ComparisonEpsilon = 0.000000001d;

        public static bool IsOverlaySynchronized(double videoSeconds, double overlaySeconds)
        {
            return Math.Abs(videoSeconds - overlaySeconds) <= MaximumOverlayDriftSeconds + ComparisonEpsilon;
        }

        public static bool AreAccessibleCuesSynchronized(
            double voiceSeconds,
            double captionSeconds,
            double visualSeconds)
        {
            return Math.Abs(voiceSeconds - captionSeconds) <= MaximumAccessibleCueDriftSeconds + ComparisonEpsilon &&
                   Math.Abs(voiceSeconds - visualSeconds) <= MaximumAccessibleCueDriftSeconds + ComparisonEpsilon;
        }

        public static double FrameTimeSeconds(int frameIndex)
        {
            if (frameIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frameIndex));
            }

            return frameIndex / (double)IntroVariantSelector.FrameRate;
        }

        public static IntroCueSnapshot Evaluate(bool reducedMotion, double mediaTimeSeconds)
        {
            if (mediaTimeSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(mediaTimeSeconds));
            }

            var accessibleCueActive = mediaTimeSeconds <= AccessibleCueEndSeconds;
            IntroIbisPose pose;
            if (reducedMotion)
            {
                pose = mediaTimeSeconds < ReducedStaticIbisSeconds
                    ? IntroIbisPose.Hidden
                    : IntroIbisPose.StaticReduced;
            }
            else if (mediaTimeSeconds < FullIbisEntrySeconds)
            {
                pose = IntroIbisPose.Hidden;
            }
            else if (mediaTimeSeconds < FullIbisHeadLiftSeconds)
            {
                pose = IntroIbisPose.WingsOpen;
            }
            else if (mediaTimeSeconds < FullIbisDirectionSeconds)
            {
                pose = IntroIbisPose.HeadLift;
            }
            else if (mediaTimeSeconds < FullIbisSettleSeconds)
            {
                pose = IntroIbisPose.BeakDirection;
            }
            else
            {
                pose = IntroIbisPose.CalmSettle;
            }

            return new IntroCueSnapshot(
                mediaTimeSeconds,
                pose,
                accessibleCueActive,
                accessibleCueActive,
                accessibleCueActive);
        }
    }
}
