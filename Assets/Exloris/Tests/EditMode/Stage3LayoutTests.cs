using System;
using Exloris.Runtime.Stage3;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage3LayoutTests
    {
        [TestCase(2048f, 1536f, 0f, 40f, 2048f, 1456f, 326f, 88)]
        [TestCase(2560f, 1440f, 40f, 0f, 2480f, 1400f, 326f, 88)]
        [TestCase(3456f, 1440f, 80f, 0f, 3296f, 1400f, 326f, 88)]
        [TestCase(1280f, 720f, 0f, 0f, 1280f, 720f, 326f, 88)]
        public void FrozenRatiosProduceEqualSafePlanRiverAndOutcomeGeometry(
            float screenWidth, float screenHeight, float safeX, float safeY,
            float safeWidth, float safeHeight, float ppi, int platformMinimum)
        {
            var metrics = Stage3LayoutMetrics.Calculate(
                screenWidth, screenHeight, safeX, safeY, safeWidth, safeHeight, ppi, platformMinimum);

            Assert.That(metrics.Fits, Is.True);
            Assert.That(metrics.Target, Is.GreaterThanOrEqualTo(60f));
            Assert.That(metrics.Gap, Is.GreaterThanOrEqualTo(18f));
            Assert.That(metrics.TopCenters, Has.Length.EqualTo(4));
            Assert.That(metrics.BottomCenters, Has.Length.EqualTo(4));
            Assert.That(metrics.PlanCenters, Has.Length.EqualTo(3));
            Assert.That(metrics.RiverCenters, Has.Length.EqualTo(3));
            Assert.That(metrics.PlanSize, Is.EqualTo(metrics.RiverSize));
            Assert.That(metrics.PlanCenters[1].x - metrics.PlanCenters[0].x,
                Is.EqualTo(metrics.PlanCenters[2].x - metrics.PlanCenters[1].x).Within(0.001f));
            Assert.That(metrics.RiverCenters[1].x - metrics.RiverCenters[0].x,
                Is.EqualTo(metrics.RiverCenters[2].x - metrics.RiverCenters[1].x).Within(0.001f));
            Assert.That(metrics.BenefitRect, Is.EqualTo(metrics.NeedRect));
            Assert.That(metrics.CaptionRect.width, Is.GreaterThan(0f));
            Assert.That(metrics.ChildRect.width, Is.GreaterThan(0f));
        }

        [TestCase(2048f, 1536f, 0)]
        [TestCase(2560f, 1440f, 1)]
        [TestCase(3456f, 1440f, 2)]
        public void ExactRatioBackgroundIsSelectedBeforeDisplay(float width, float height, int expected)
        {
            Assert.That(Stage3StageView.SelectRatioIndex(width, height), Is.EqualTo(expected));
        }

        [Test]
        public void InvalidAndInfeasibleLayoutsFailClosed()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Stage3LayoutMetrics.Calculate(0, 1080, 0, 0, 1000, 700, 326, 88));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Stage3LayoutMetrics.Calculate(1920, 1080, -1, 0, 1000, 700, 326, 88));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Stage3LayoutMetrics.Calculate(1920, 1080, 0, 0, 1000, 700, 0, 88));
            Assert.That(Stage3LayoutMetrics.Calculate(1920, 1080, 0, 0, 280, 180, 326, 88).Fits, Is.False);
        }
    }
}
