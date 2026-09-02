using Exloris.Runtime.Diagnostics;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class FoundationPerformanceMetricsTests
    {
        [Test]
        public void Summarize_ComputesPercentilesAndBudgetResults()
        {
            var frameTimes = new float[100];
            for (var index = 0; index < frameTimes.Length; index++)
            {
                frameTimes[index] = index + 1;
            }

            var report = FoundationPerformanceMetrics.Summarize(
                frameTimes,
                allocatedBytes: 256L * 1024L * 1024L,
                reservedBytes: 384L * 1024L * 1024L,
                width: 1920,
                height: 1080);

            Assert.That(report.SampleCount, Is.EqualTo(100));
            Assert.That(report.AverageFrameMilliseconds, Is.EqualTo(50.5f).Within(0.001f));
            Assert.That(report.P95FrameMilliseconds, Is.EqualTo(95f).Within(0.001f));
            Assert.That(report.P99FrameMilliseconds, Is.EqualTo(99f).Within(0.001f));
            Assert.That(report.AllocatedMemoryMiB, Is.EqualTo(256f).Within(0.001f));
            Assert.That(report.ReservedMemoryMiB, Is.EqualTo(384f).Within(0.001f));
            Assert.That(report.Width, Is.EqualTo(1920));
            Assert.That(report.Height, Is.EqualTo(1080));
            Assert.That(report.MeetsFrameBudget, Is.False);
            Assert.That(report.MeetsMemoryBudget, Is.True);
        }

        [Test]
        public void Summarize_AcceptsHealthyFoundationSample()
        {
            var frameTimes = new[] { 12f, 13f, 14f, 15f, 16f };

            var report = FoundationPerformanceMetrics.Summarize(
                frameTimes,
                allocatedBytes: 128L * 1024L * 1024L,
                reservedBytes: 160L * 1024L * 1024L,
                width: 1440,
                height: 1080);

            Assert.That(report.MeetsFrameBudget, Is.True);
            Assert.That(report.MeetsMemoryBudget, Is.True);
        }
    }
}
