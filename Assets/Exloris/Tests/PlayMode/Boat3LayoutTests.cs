using System.Collections;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Layout;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class Boat3LayoutTests
    {
        [TestCase(2160f,1620f,1598.77f,1183.08f,264f,88)]
        [TestCase(1920f,1080f,1856f,1016f,326f,88)]
        [TestCase(2400f,1000f,2038.84f,825.52f,429f,144)]
        public void ApprovedRatiosMeetPhysicalTargetsGapsAndIbisScale(float sw,float sh,float safeW,float safeH,float ppi,int minimum)
        {
            var metrics = Boat3LayoutMetrics.Calculate(sw,sh,safeW,safeH,ppi,minimum);
            var required = ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(ResponsiveLayoutProfile.CalculateTargetPhysicalPixels(ppi,minimum), ResponsiveLayoutProfile.CalculateCanvasScale(sw,sh));
            Assert.That(metrics.Target, Is.GreaterThanOrEqualTo(required)); Assert.That(metrics.Gap, Is.GreaterThanOrEqualTo(metrics.Target * 0.25f));
            Assert.That(metrics.IbisHeight / safeH, Is.InRange(0.22f,0.28f)); Assert.That(metrics.Fits(safeW,safeH), Is.True);
        }

        [UnityTest]
        public IEnumerator ViewContainsAllEightTargetsAtEveryRatio()
        {
            var ratios = new[] { new[] {2160f,1620f,1598.77f,1183.08f,264f,88f}, new[] {1920f,1080f,1856f,1016f,326f,88f}, new[] {2400f,1000f,2038.84f,825.52f,429f,144f} };
            var owner = new GameObject("Boat3Layout"); var router = Router();
            try
            {
                var view = owner.AddComponent<Boat3StageView>(); view.Configure(router, Boat3Content.LoadApproved());
                Assert.That(view.SemanticControlCount, Is.EqualTo(8)); Assert.That(view.AreTouchTargetsTransparent(), Is.True); Assert.That(view.VisibleArtRaycastsDisabled(), Is.True);
                foreach (var r in ratios) { Assert.That(view.TryApplyResponsiveLayout(r[0],r[1],r[2],r[3],r[4],(int)r[5]), Is.True); Assert.That(view.AreAllTargetsInsideSafeArea(), Is.True); Assert.That(view.DoTouchTargetsOverlap(), Is.False); }
                yield return null;
            }
            finally { router.Dispose(); Object.Destroy(owner); }
        }

        [Test]
        public void InfeasibleSafeAreaDoesNotMutateTheLastValidLayout()
        {
            var owner = new GameObject("Boat3Negative"); var router = Router();
            try { var view = owner.AddComponent<Boat3StageView>(); view.Configure(router, Boat3Content.LoadApproved()); var before = view.GetControlCanvasRect(Boat3StageView.SkipControlId); Assert.That(view.TryApplyResponsiveLayout(360,182,360,182,429,144), Is.False); Assert.That(view.GetControlCanvasRect(Boat3StageView.SkipControlId), Is.EqualTo(before)); }
            finally { router.Dispose(); Object.DestroyImmediate(owner); }
        }

        [Test]
        public void BoyAndGirlUseExactlyTheSameSeatedGeometry()
        {
            var boyOwner = new GameObject("Boat3Boy"); var girlOwner = new GameObject("Boat3Girl");
            var boyRouter = Router(); var girlRouter = Router();
            try
            {
                var boy = boyOwner.AddComponent<Boat3StageView>(); boy.Configure(boyRouter, Boat3Content.LoadApproved(), false);
                var girl = girlOwner.AddComponent<Boat3StageView>(); girl.Configure(girlRouter, Boat3Content.LoadApproved(), true);
                Assert.That(girl.SelectedChildRenderedSize, Is.EqualTo(boy.SelectedChildRenderedSize));
                Assert.That(girl.SelectedChildAnchoredPosition, Is.EqualTo(boy.SelectedChildAnchoredPosition));
            }
            finally { boyRouter.Dispose(); girlRouter.Dispose(); Object.DestroyImmediate(boyOwner); Object.DestroyImmediate(girlOwner); }
        }
        private static Exloris.Runtime.Input.ChildInputRouter Router() => new Exloris.Runtime.Input.ChildInputRouter(new InputAction("Point",InputActionType.PassThrough),new InputAction("Press",InputActionType.Button));
    }
}
