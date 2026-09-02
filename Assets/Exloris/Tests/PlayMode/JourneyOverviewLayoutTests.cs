using System.Collections;
using Exloris.Runtime.Layout;
using Exloris.Runtime.Overview;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class JourneyOverviewLayoutTests
    {
        [TestCase(2160f, 1620f, 1598.77f, 1183.08f, 264f, 88,
            320f, 420f, 399.39f, 799.39f, 1199.39f)]
        [TestCase(1920f, 1080f, 1856f, 1016f, 326f, 88,
            360f, 224f, 474f, 928f, 1382f)]
        [TestCase(2400f, 1000f, 2038.84f, 825.52f, 429f, 144,
            360f, 182f, 569.42f, 1019.42f, 1469.42f)]
        public void FrozenRatiosMatchExactCard3AndControlGeometry(
            float screenWidth, float screenHeight, float safeWidth, float safeHeight,
            float ppi, int platformMinimum,
            float cardWidth, float cardHeight, float x0, float x1, float x2)
        {
            var metrics = JourneyOverviewLayoutMetrics.Calculate(
                screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimum);

            Assert.That(metrics.CardSize.x, Is.EqualTo(cardWidth).Within(0.01f));
            Assert.That(metrics.CardSize.y, Is.EqualTo(cardHeight).Within(0.01f));
            Assert.That(metrics.CardCenters[0].x, Is.EqualTo(x0).Within(0.02f));
            Assert.That(metrics.CardCenters[1].x, Is.EqualTo(x1).Within(0.02f));
            Assert.That(metrics.CardCenters[2].x, Is.EqualTo(x2).Within(0.02f));
            Assert.That(metrics.Target, Is.GreaterThanOrEqualTo(
                ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(
                    ResponsiveLayoutProfile.CalculateTargetPhysicalPixels(ppi, platformMinimum),
                    ResponsiveLayoutProfile.CalculateCanvasScale(screenWidth, screenHeight))));
            Assert.That(metrics.Gap, Is.GreaterThanOrEqualTo(metrics.Target * 0.25f));
        }

        [UnityTest]
        public IEnumerator ViewKeepsAllControlsInsideSafeAreaWithNoOverlapAtEveryRatio()
        {
            var ratios = new[]
            {
                new[] { 2160f, 1620f, 1598.77f, 1183.08f, 264f, 88f },
                new[] { 1920f, 1080f, 1856f, 1016f, 326f, 88f },
                new[] { 2400f, 1000f, 2038.84f, 825.52f, 429f, 144f },
            };
            var owner = new GameObject("JourneyOverviewLayout");
            var router = new Exloris.Runtime.Input.ChildInputRouter(
                new InputAction("Point", InputActionType.PassThrough),
                new InputAction("Press", InputActionType.Button));
            try
            {
                var view = owner.AddComponent<JourneyOverviewStageView>();
                view.Configure(router, JourneyOverviewContent.LoadApproved());
                foreach (var ratio in ratios)
                {
                    Assert.That(view.TryApplyResponsiveLayout(
                        ratio[0], ratio[1], ratio[2], ratio[3], ratio[4], (int)ratio[5]), Is.True);
                    Assert.That(view.AreAllTargetsInsideSafeArea(), Is.True);
                    Assert.That(view.DoTouchTargetsOverlap(), Is.False);
                    Assert.That(view.CaptionInsideSafeArea(), Is.True);
                    Assert.That(view.TimelineInsideSafeArea(), Is.True);
                }
                yield return null;
            }
            finally
            {
                router.Dispose();
                UnityEngine.Object.Destroy(owner);
            }
        }

        [Test]
        public void InfeasibleSafeAreaFailsBeforeMutatingLastValidLayout()
        {
            var owner = new GameObject("JourneyOverviewNegativeLayout");
            var router = new Exloris.Runtime.Input.ChildInputRouter(
                new InputAction("Point", InputActionType.PassThrough),
                new InputAction("Press", InputActionType.Button));
            try
            {
                var view = owner.AddComponent<JourneyOverviewStageView>();
                view.Configure(router, JourneyOverviewContent.LoadApproved());
                var before = view.GetControlCanvasRect(JourneyOverviewStageView.SkipControlId);

                Assert.That(view.TryApplyResponsiveLayout(
                    360f, 182f, 360f, 182f, 429f, 144), Is.False);
                Assert.That(view.GetControlCanvasRect(JourneyOverviewStageView.SkipControlId),
                    Is.EqualTo(before));
            }
            finally
            {
                router.Dispose();
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
    }
}
