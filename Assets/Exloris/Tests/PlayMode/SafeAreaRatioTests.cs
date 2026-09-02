using Exloris.Runtime.Layout;
using NUnit.Framework;
using UnityEngine;

namespace Exloris.Tests.PlayMode
{
    public sealed class SafeAreaRatioTests
    {
        [Test]
        public void EffectiveSafeAreaCombinesOsSafeAreaGestureInsetsAndEdgeCutouts()
        {
            var display = new Rect(0f, 0f, 2400f, 1080f);
            var osSafeArea = new Rect(40f, 20f, 2320f, 1040f);
            var gestureInsets = new EdgeInsets(0f, 30f, 0f, 0f);
            var cutouts = new[]
            {
                new Rect(0f, 400f, 80f, 280f),
                new Rect(2360f, 0f, 40f, 1080f)
            };

            var result = SafeAreaController.CalculateEffectiveSafeArea(
                display,
                osSafeArea,
                cutouts,
                gestureInsets);

            Assert.That(result.xMin, Is.EqualTo(80f));
            Assert.That(result.yMin, Is.EqualTo(30f));
            Assert.That(result.xMax, Is.EqualTo(2360f));
            Assert.That(result.yMax, Is.EqualTo(1060f));
        }

        [TestCase(1440f, 1080f)]
        [TestCase(1920f, 1080f)]
        [TestCase(2592f, 1080f)]
        public void SafeAreaAnchorsRemainNormalizedAtEveryFrozenRatio(float width, float height)
        {
            var display = new Rect(0f, 0f, width, height);
            var safe = new Rect(32f, 32f, width - 64f, height - 64f);

            var anchors = SafeAreaController.CalculateNormalizedAnchors(display, safe);

            Assert.That(anchors.Min.x, Is.InRange(0f, 1f));
            Assert.That(anchors.Min.y, Is.InRange(0f, 1f));
            Assert.That(anchors.Max.x, Is.InRange(0f, 1f));
            Assert.That(anchors.Max.y, Is.InRange(0f, 1f));
            Assert.That(anchors.Min.x, Is.LessThan(anchors.Max.x));
            Assert.That(anchors.Min.y, Is.LessThan(anchors.Max.y));
        }

        [Test]
        public void InvalidSafeAreaIsRejectedBeforePresentation()
        {
            Assert.Throws<System.InvalidOperationException>(() =>
                SafeAreaController.CalculateEffectiveSafeArea(
                    new Rect(0f, 0f, 100f, 100f),
                    new Rect(0f, 0f, 100f, 100f),
                    new Rect[0],
                    new EdgeInsets(60f, 0f, 60f, 0f)));
        }
    }
}
