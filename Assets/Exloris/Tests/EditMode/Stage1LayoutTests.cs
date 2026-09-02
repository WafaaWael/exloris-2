using System;
using Exloris.Runtime.Stage1;
using NUnit.Framework;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage1LayoutTests
    {
        [TestCase(2048f, 1536f, 0f, 40f, 2048f, 1456f, 326f, 88)]
        [TestCase(2560f, 1440f, 40f, 0f, 2480f, 1400f, 326f, 88)]
        [TestCase(3456f, 1440f, 80f, 0f, 3296f, 1400f, 326f, 88)]
        public void AllRatiosInsetsAndPpiProduceSafeChildGeometry(
            float screenWidth, float screenHeight, float safeX, float safeY,
            float safeWidth, float safeHeight, float ppi, int platformMinimum)
        {
            var metrics = Stage1LayoutMetrics.Calculate(
                screenWidth, screenHeight, safeX, safeY, safeWidth, safeHeight, ppi, platformMinimum);

            Assert.That(metrics.Fits, Is.True);
            Assert.That(metrics.Target, Is.GreaterThanOrEqualTo(60f));
            Assert.That(metrics.Gap, Is.GreaterThanOrEqualTo(18f));
            Assert.That(metrics.TopCenters, Has.Length.EqualTo(4));
            Assert.That(metrics.BottomCenters, Has.Length.EqualTo(3));
            Assert.That(metrics.DragRect.width, Is.GreaterThan(metrics.Target));
            Assert.That(metrics.DragRect.height, Is.GreaterThan(4f * metrics.MinimumMarkerTravel));
            Assert.That(metrics.CaptionRect.width, Is.GreaterThan(0f));
            Assert.That(metrics.OutcomeRect.width, Is.GreaterThan(0f));
            Assert.That(metrics.ChildRect.width, Is.GreaterThan(0f));
            Assert.That(metrics.SafeCanvasRect.x, Is.EqualTo(safeX / metrics.CanvasScale).Within(0.001f));
            Assert.That(metrics.SafeCanvasRect.y, Is.EqualTo(safeY / metrics.CanvasScale).Within(0.001f));
        }

        [Test]
        public void FiveAuthoredMarkerPositionsHaveFourEqualVisibleSteps()
        {
            var metrics = Stage1LayoutMetrics.Calculate(2560, 1440, 0, 0, 2560, 1400, 326, 88);
            var positions = new[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
            var first = metrics.MarkerCenter(positions[0]);
            var step = metrics.MarkerCenter(positions[1]).y - first.y;

            Assert.That(step, Is.GreaterThanOrEqualTo(metrics.MinimumMarkerTravel));
            for (var index = 1; index < positions.Length; index++)
                Assert.That(metrics.MarkerCenter(positions[index]).y - metrics.MarkerCenter(positions[index - 1]).y, Is.EqualTo(step).Within(0.001f));
            Assert.That(metrics.MarkerCenter(0f).x, Is.EqualTo(metrics.MarkerCenter(1f).x));
        }

        [TestCase(2048f, 1536f, 0)]
        [TestCase(2560f, 1440f, 1)]
        [TestCase(3456f, 1440f, 2)]
        public void ExactRatioBackgroundIsSelectedBeforeDisplay(float width, float height, int expected)
        {
            Assert.That(Stage1StageView.SelectRatioIndex(width, height), Is.EqualTo(expected));
        }

        [Test]
        public void InvalidAndInfeasibleLayoutsFailClosed()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Stage1LayoutMetrics.Calculate(0, 1080, 0, 0, 1000, 700, 326, 88));
            Assert.Throws<ArgumentOutOfRangeException>(() => Stage1LayoutMetrics.Calculate(1920, 1080, -1, 0, 1000, 700, 326, 88));
            Assert.Throws<ArgumentOutOfRangeException>(() => Stage1LayoutMetrics.Calculate(1920, 1080, 0, 0, 1000, 700, 0, 88));
            Assert.That(Stage1LayoutMetrics.Calculate(1920, 1080, 0, 0, 280, 180, 326, 88).Fits, Is.False);
        }
    }
}
