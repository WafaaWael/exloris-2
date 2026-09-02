using System;
using Exloris.Runtime.Stage2;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage2LayoutTests
    {
        [TestCase(2048f, 1536f, 0f, 40f, 2048f, 1456f, 326f, 88)]
        [TestCase(2560f, 1440f, 40f, 0f, 2480f, 1400f, 326f, 88)]
        [TestCase(3456f, 1440f, 80f, 0f, 3296f, 1400f, 326f, 88)]
        [TestCase(1280f, 720f, 0f, 0f, 1280f, 720f, 326f, 88)]
        public void FrozenRatiosProduceEqualSafeChoiceAndHotspotGeometry(
            float screenWidth, float screenHeight, float safeX, float safeY,
            float safeWidth, float safeHeight, float ppi, int platformMinimum)
        {
            var metrics = Stage2LayoutMetrics.Calculate(
                screenWidth, screenHeight, safeX, safeY, safeWidth, safeHeight, ppi, platformMinimum);

            Assert.That(metrics.Fits, Is.True);
            Assert.That(metrics.Target, Is.GreaterThanOrEqualTo(60f));
            Assert.That(metrics.Gap, Is.GreaterThanOrEqualTo(18f));
            Assert.That(metrics.TopCenters, Has.Length.EqualTo(4));
            Assert.That(metrics.ChoiceCenters, Has.Length.EqualTo(3));
            Assert.That(metrics.HotspotCenters, Has.Length.EqualTo(3));
            Assert.That(metrics.ChoiceSize.x, Is.EqualTo(metrics.ChoiceSize.y).Within(0.001f));
            Assert.That(metrics.HotspotSize.x, Is.EqualTo(metrics.HotspotSize.y).Within(0.001f));
            Assert.That(metrics.ChoiceCenters[1].x - metrics.ChoiceCenters[0].x,
                Is.EqualTo(metrics.ChoiceCenters[2].x - metrics.ChoiceCenters[1].x).Within(0.001f));
            Assert.That(metrics.HotspotCenters[1].x - metrics.HotspotCenters[0].x,
                Is.EqualTo(metrics.HotspotCenters[2].x - metrics.HotspotCenters[1].x).Within(0.001f));
            Assert.That(metrics.CaptionRect.width, Is.GreaterThan(0f));
            Assert.That(metrics.OutcomeRect.width, Is.GreaterThan(0f));
            Assert.That(metrics.ChildRect.width, Is.GreaterThan(0f));
        }

        [TestCase(2048f, 1536f, 0)]
        [TestCase(2560f, 1440f, 1)]
        [TestCase(3456f, 1440f, 2)]
        public void ExactRatioBackgroundIsSelectedBeforeDisplay(float width, float height, int expected)
        {
            Assert.That(Stage2StageView.SelectRatioIndex(width, height), Is.EqualTo(expected));
        }

        [Test]
        public void InvalidAndInfeasibleLayoutsFailClosed()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Stage2LayoutMetrics.Calculate(0, 1080, 0, 0, 1000, 700, 326, 88));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Stage2LayoutMetrics.Calculate(1920, 1080, -1, 0, 1000, 700, 326, 88));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Stage2LayoutMetrics.Calculate(1920, 1080, 0, 0, 1000, 700, 0, 88));
            Assert.That(Stage2LayoutMetrics.Calculate(1920, 1080, 0, 0, 280, 180, 326, 88).Fits, Is.False);
        }
    }
}
