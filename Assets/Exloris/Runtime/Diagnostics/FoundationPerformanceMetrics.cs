using System;
using System.Collections.Generic;

namespace Exloris.Runtime.Diagnostics
{
    public readonly struct FoundationPerformanceReport
    {
        public FoundationPerformanceReport(
            int sampleCount,
            float averageFrameMilliseconds,
            float p95FrameMilliseconds,
            float p99FrameMilliseconds,
            float maximumFrameMilliseconds,
            int maximumConsecutiveFramesOver33Milliseconds,
            float allocatedMemoryMiB,
            float reservedMemoryMiB,
            int width,
            int height)
        {
            SampleCount = sampleCount;
            AverageFrameMilliseconds = averageFrameMilliseconds;
            P95FrameMilliseconds = p95FrameMilliseconds;
            P99FrameMilliseconds = p99FrameMilliseconds;
            MaximumFrameMilliseconds = maximumFrameMilliseconds;
            MaximumConsecutiveFramesOver33Milliseconds = maximumConsecutiveFramesOver33Milliseconds;
            AllocatedMemoryMiB = allocatedMemoryMiB;
            ReservedMemoryMiB = reservedMemoryMiB;
            Width = width;
            Height = height;
        }

        public int SampleCount { get; }
        public float AverageFrameMilliseconds { get; }
        public float P95FrameMilliseconds { get; }
        public float P99FrameMilliseconds { get; }
        public float MaximumFrameMilliseconds { get; }
        public int MaximumConsecutiveFramesOver33Milliseconds { get; }
        public float AllocatedMemoryMiB { get; }
        public float ReservedMemoryMiB { get; }
        public int Width { get; }
        public int Height { get; }
        public bool MeetsFrameBudget =>
            P95FrameMilliseconds <= FoundationPerformanceMetrics.P95FrameBudgetMilliseconds &&
            P99FrameMilliseconds <= FoundationPerformanceMetrics.P99FrameBudgetMilliseconds &&
            MaximumConsecutiveFramesOver33Milliseconds < 2;
        public bool MeetsMemoryBudget => ReservedMemoryMiB <= FoundationPerformanceMetrics.MemoryBudgetMiB;
    }

    public static class FoundationPerformanceMetrics
    {
        public const float P95FrameBudgetMilliseconds = 16.7f;
        public const float P99FrameBudgetMilliseconds = 25f;
        public const float RepeatedLongFrameThresholdMilliseconds = 33.3f;
        public const float MemoryBudgetMiB = 512f;

        public static FoundationPerformanceReport Summarize(
            IReadOnlyList<float> frameMilliseconds,
            long allocatedBytes,
            long reservedBytes,
            int width,
            int height)
        {
            if (frameMilliseconds == null) throw new ArgumentNullException(nameof(frameMilliseconds));
            if (frameMilliseconds.Count == 0) throw new ArgumentException("At least one frame sample is required.", nameof(frameMilliseconds));

            var sorted = new float[frameMilliseconds.Count];
            var sum = 0f;
            var currentLongFrameRun = 0;
            var maximumLongFrameRun = 0;
            for (var index = 0; index < frameMilliseconds.Count; index++)
            {
                var value = frameMilliseconds[index];
                if (value < 0f || float.IsNaN(value) || float.IsInfinity(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(frameMilliseconds), "Frame samples must be finite and non-negative.");
                }

                sorted[index] = value;
                sum += value;
                currentLongFrameRun = value > RepeatedLongFrameThresholdMilliseconds ? currentLongFrameRun + 1 : 0;
                maximumLongFrameRun = Math.Max(maximumLongFrameRun, currentLongFrameRun);
            }

            Array.Sort(sorted);
            return new FoundationPerformanceReport(
                frameMilliseconds.Count,
                sum / frameMilliseconds.Count,
                Percentile(sorted, 0.95d),
                Percentile(sorted, 0.99d),
                sorted[sorted.Length - 1],
                maximumLongFrameRun,
                BytesToMiB(allocatedBytes),
                BytesToMiB(reservedBytes),
                width,
                height);
        }

        private static float Percentile(IReadOnlyList<float> sorted, double percentile)
        {
            var rank = Math.Max(1, (int)Math.Ceiling(percentile * sorted.Count));
            return sorted[rank - 1];
        }

        private static float BytesToMiB(long bytes) => bytes / (1024f * 1024f);
    }
}
