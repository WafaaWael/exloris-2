using Exloris.Runtime.Completion;
using Exloris.Runtime.Layout;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class CompletionAdultReportLayoutTests
    {
        [TestCase(2160f, 1620f, 1598.77f, 1183.08f, 264f, 88)]
        [TestCase(1920f, 1080f, 1856f, 1016f, 326f, 88)]
        [TestCase(2400f, 1000f, 2038.84f, 825.52f, 429f, 144)]
        public void FrozenRatiosMeetPhysicalTargetsGapsAndScrollableReportAtTwoHundredPercentText(
            float screenWidth, float screenHeight, float safeWidth, float safeHeight,
            float ppi, int platformMinimum)
        {
            var metrics = CompletionLayoutMetrics.Calculate(
                screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimum, 2f, 1.35f);

            Assert.That(metrics.Fits(safeWidth, safeHeight), Is.True);
            Assert.That(metrics.Target, Is.GreaterThanOrEqualTo(
                ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(
                    ResponsiveLayoutProfile.CalculateTargetPhysicalPixels(ppi, platformMinimum),
                    ResponsiveLayoutProfile.CalculateCanvasScale(screenWidth, screenHeight))));
            Assert.That(metrics.Gap, Is.GreaterThanOrEqualTo(metrics.Target * 0.25f));
            Assert.That(metrics.ReportViewportHeight, Is.GreaterThan(metrics.Target));
            Assert.That(metrics.ReportCardHeight, Is.GreaterThanOrEqualTo(440f));
            Assert.That(metrics.ReportContentWidth, Is.LessThanOrEqualTo(safeWidth));
        }

        [Test]
        public void InfeasibleSafeAreaFailsClosed()
        {
            var metrics = CompletionLayoutMetrics.Calculate(360f, 182f, 360f, 182f, 429f, 144, 2f, 1.35f);
            Assert.That(metrics.Fits(360f, 182f), Is.False);
        }
    }
}
