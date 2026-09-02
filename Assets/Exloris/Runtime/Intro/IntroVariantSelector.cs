using System;

namespace Exloris.Runtime.Intro
{
    public enum IntroTransitionMode
    {
        Cut,
        OpacityFade
    }

    public sealed class IntroMediaVariant
    {
        public IntroMediaVariant(
            string fileName,
            int frameRate,
            int frameCount,
            double durationSeconds,
            int videoWidth,
            int videoHeight,
            long maximumDecodeBufferBytes,
            IntroTransitionMode transitionMode,
            double maximumTransitionDurationSeconds,
            bool hasSpatialMotion)
        {
            FileName = fileName;
            FrameRate = frameRate;
            FrameCount = frameCount;
            DurationSeconds = durationSeconds;
            VideoWidth = videoWidth;
            VideoHeight = videoHeight;
            MaximumDecodeBufferBytes = maximumDecodeBufferBytes;
            TransitionMode = transitionMode;
            MaximumTransitionDurationSeconds = maximumTransitionDurationSeconds;
            HasSpatialMotion = hasSpatialMotion;
        }

        public string FileName { get; }
        public int FrameRate { get; }
        public int FrameCount { get; }
        public double DurationSeconds { get; }
        public int VideoWidth { get; }
        public int VideoHeight { get; }
        public long MaximumDecodeBufferBytes { get; }
        public IntroTransitionMode TransitionMode { get; }
        public double MaximumTransitionDurationSeconds { get; }
        public bool HasSpatialMotion { get; }

        public void EnsureDecodeBufferWithinCeiling(long requiredBytes)
        {
            if (requiredBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredBytes));
            }

            if (requiredBytes > MaximumDecodeBufferBytes)
            {
                throw new InvalidOperationException(
                    $"Intro decode buffer requires {requiredBytes} bytes, above the {MaximumDecodeBufferBytes}-byte tier ceiling.");
            }
        }
    }

    public static class IntroVariantSelector
    {
        public const int FullFrameCount = 288;
        public const int ReducedFrameCount = 120;
        public const int FrameRate = 30;
        public const double FullDurationSeconds = 9.6d;
        public const double ReducedDurationSeconds = 4.0d;

        public static IntroMediaVariant Select(
            int width,
            int height,
            bool reducedMotion,
            bool preferLowResolution)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            var aspect = (double)width / height;
            var ratio = aspect <= 1.5d ? "4x3" : aspect <= 2.05d ? "16x9" : "24x10";
            var low = preferLowResolution && ratio == "16x9" ? "-low" : string.Empty;
            var videoWidth = ratio == "4x3" ? 1920 : ratio == "24x10" ? 2592 : low.Length > 0 ? 1920 : 2560;
            var videoHeight = ratio == "4x3" ? 1440 : ratio == "24x10" ? 1080 : low.Length > 0 ? 1080 : 1440;
            var fileName = reducedMotion
                ? $"intro-reduced-{ratio}{low}.mp4"
                : $"intro-{ratio}{low}.mp4";
            return new IntroMediaVariant(
                fileName,
                FrameRate,
                reducedMotion ? ReducedFrameCount : FullFrameCount,
                reducedMotion ? ReducedDurationSeconds : FullDurationSeconds,
                videoWidth,
                videoHeight,
                low.Length > 0 ? 48_000_000L : 64_000_000L,
                reducedMotion ? IntroTransitionMode.OpacityFade : IntroTransitionMode.Cut,
                reducedMotion ? 0.150d : 0d,
                hasSpatialMotion: !reducedMotion);
        }
    }
}
