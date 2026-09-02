using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Profiling;

namespace Exloris.Runtime.Diagnostics
{
    [DisallowMultipleComponent]
    public sealed class FoundationPerformanceProbe : MonoBehaviour
    {
        private const int WarmupFrames = 180;
        private const int SampleFrames = 600;
        private readonly List<float> _frameMilliseconds = new List<float>(SampleFrames);
        private int _warmupRemaining = WarmupFrames;
        private bool _reported;

        private void Update()
        {
            if (_reported) return;
            if (_warmupRemaining-- > 0) return;

            _frameMilliseconds.Add(Time.unscaledDeltaTime * 1000f);
            if (_frameMilliseconds.Count < SampleFrames) return;

            var report = FoundationPerformanceMetrics.Summarize(
                _frameMilliseconds,
                Profiler.GetTotalAllocatedMemoryLong(),
                Profiler.GetTotalReservedMemoryLong(),
                Screen.width,
                Screen.height);
            var invariant = CultureInfo.InvariantCulture;
            Debug.Log(string.Format(
                invariant,
                "FOUNDATION_METRICS frames={0} avg_frame_ms={1:F3} p95_frame_ms={2:F3} p99_frame_ms={3:F3} max_frame_ms={4:F3} max_consecutive_over_33_3_ms={5} allocated_mib={6:F2} reserved_mib={7:F2} resolution={8}x{9} frame_budget_pass={10} memory_budget_pass={11}",
                report.SampleCount,
                report.AverageFrameMilliseconds,
                report.P95FrameMilliseconds,
                report.P99FrameMilliseconds,
                report.MaximumFrameMilliseconds,
                report.MaximumConsecutiveFramesOver33Milliseconds,
                report.AllocatedMemoryMiB,
                report.ReservedMemoryMiB,
                report.Width,
                report.Height,
                report.MeetsFrameBudget,
                report.MeetsMemoryBudget));
            _reported = true;
        }
    }
}
