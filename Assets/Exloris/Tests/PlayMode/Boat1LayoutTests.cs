using System;
using System.Collections;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Layout;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class Boat1LayoutTests
    {
        [Test]
        [TestCase(2160f, 1620f, 1598.77f, 1183.08f, 264f, 88)]
        [TestCase(1920f, 1080f, 1856f, 1016f, 326f, 88)]
        [TestCase(2400f, 1000f, 2038.84f, 825.52f, 429f, 144)]
        public void EveryApprovedRatioMeetsPhysicalTargetsGapsAndIbisScale(float sw, float sh, float safeW, float safeH, float ppi, int minimum)
        {
            var metrics = Boat1LayoutMetrics.Calculate(sw, sh, safeW, safeH, ppi, minimum);
            var required = ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(
                ResponsiveLayoutProfile.CalculateTargetPhysicalPixels(ppi, minimum), ResponsiveLayoutProfile.CalculateCanvasScale(sw, sh));
            Assert.That(metrics.Target, Is.GreaterThanOrEqualTo(required)); Assert.That(metrics.Gap, Is.GreaterThanOrEqualTo(metrics.Target * 0.25f));
            Assert.That(metrics.IbisHeight / safeH, Is.InRange(0.22f, 0.28f)); Assert.That(metrics.Fits(safeW, safeH), Is.True);
        }

        [UnityTest]
        public IEnumerator ViewContainsAllTargetsWithoutOverlapAtEveryRatio()
        {
            var ratios = new[] { new[] {2160f,1620f,1598.77f,1183.08f,264f,88f}, new[] {1920f,1080f,1856f,1016f,326f,88f}, new[] {2400f,1000f,2038.84f,825.52f,429f,144f} };
            var owner = new GameObject("Boat1Layout"); var router = Router();
            try
            {
                var view = owner.AddComponent<Boat1StageView>(); view.Configure(router, Boat1Content.LoadApproved());
                foreach (var r in ratios) { Assert.That(view.TryApplyResponsiveLayout(r[0],r[1],r[2],r[3],r[4],(int)r[5]), Is.True); Assert.That(view.AreAllTargetsInsideSafeArea(), Is.True); Assert.That(view.DoTouchTargetsOverlap(), Is.False); }
                yield return null;
            }
            finally { router.Dispose(); UnityEngine.Object.Destroy(owner); }
        }

        [Test]
        public void RestrictiveOffsetSafeAreaIsRejectedWithoutMutatingLastValidLayout()
        {
            var owner = new GameObject("Boat1Negative"); var router = Router();
            try { var view = owner.AddComponent<Boat1StageView>(); view.Configure(router, Boat1Content.LoadApproved()); var before = view.GetControlCanvasRect(Boat1StageView.SkipControlId);
                Assert.That(view.TryApplyResponsiveLayout(360,182,360,182,429,144), Is.False); Assert.That(view.GetControlCanvasRect(Boat1StageView.SkipControlId), Is.EqualTo(before)); }
            finally { router.Dispose(); UnityEngine.Object.DestroyImmediate(owner); }
        }

        private static Exloris.Runtime.Input.ChildInputRouter Router() => new Exloris.Runtime.Input.ChildInputRouter(
            new InputAction("Point", InputActionType.PassThrough), new InputAction("Press", InputActionType.Button));
    }
}
