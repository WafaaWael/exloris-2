using System;
using System.Linq;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Editor;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Completion;
using Exloris.Runtime.Input;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class CompletionAdultReportPrefabContractTests
    {
        [Test]
        public void ThreePrefabsContainOnlyTheirApprovedViewsAndNoUnitySelectables()
        {
            AssertPrefab<CompletionStageView>(CompletionAdultReportPrefabGenerator.CompletionPath);
            AssertPrefab<AdultGateView>(CompletionAdultReportPrefabGenerator.AdultGatePath);
            AssertPrefab<ParentReportView>(CompletionAdultReportPrefabGenerator.ParentReportPath);
        }

        [Test]
        public void CompletionShowsExactlyOneEqualGeometrySelectedChildAndThreeSemanticControls()
        {
            var owner = new GameObject("CompletionViewTest");
            var router = Router();
            try
            {
                var view = owner.AddComponent<CompletionStageView>();
                view.Configure(router, Content(), "avatar_boy");
                Assert.That(view.ControlIds, Is.EquivalentTo(new[]
                {
                    CompletionStageView.AdultControlId,
                    CompletionStageView.PauseControlId,
                    CompletionStageView.HomeControlId,
                }));
                Assert.That(view.SelectedChildCount, Is.EqualTo(1));
                var boy = view.SelectedChildRect;
                view.SelectAvatar("avatar_girl");
                Assert.That(view.SelectedChildCount, Is.EqualTo(1));
                Assert.That(view.SelectedChildRect.size, Is.EqualTo(boy.size));
                Assert.That(view.VisibleArtRaycastsDisabled(), Is.True);
                Assert.That(view.AreTouchTargetsTransparent(), Is.True);
            }
            finally { router.Dispose(); UnityEngine.Object.DestroyImmediate(owner); }
        }

        [Test]
        public void AdultConfirmIsUnfocusableUntilHoldCompletesAndProgressSharesExactGeometry()
        {
            var owner = new GameObject("AdultGateViewTest");
            var router = Router();
            try
            {
                var view = owner.AddComponent<AdultGateView>();
                view.Configure(router, Content());
                var request = new AdultGateRequest(
                    AdultGatePurpose.Dashboard, AdultGateRequestOrigin.Completion,
                    Exloris.Core.Navigation.RouteId.Completion, Guid.NewGuid());
                view.Present(new AdultGateSnapshot(Guid.NewGuid(), AdultGateState.Holding, request, 0.5d, 1, 2d));
                view.SetInputReady(true);
                Assert.That(view.ConfirmFocusable, Is.False);
                Assert.That(view.ProgressGeometryAligned, Is.True);
                view.Present(new AdultGateSnapshot(Guid.NewGuid(), AdultGateState.AwaitingConfirm, request, 1d, -1, 0d));
                view.SetInputReady(true);
                Assert.That(view.ConfirmFocusable, Is.True);
                Assert.That(view.VisibleArtRaycastsDisabled(), Is.True);
            }
            finally { router.Dispose(); UnityEngine.Object.DestroyImmediate(owner); }
        }

        [Test]
        public void ParentReportSupportsEmptyAndMaximumScrollableCardsWithStableFocusOrder()
        {
            var owner = new GameObject("ParentReportViewTest");
            var router = Router();
            try
            {
                var view = owner.AddComponent<ParentReportView>();
                view.Configure(router, Content());
                var cards = Enumerable.Range(0, 12)
                    .Select(index => new ParentReportCard(
                        "parent_report.fact." + index,
                        new string('W', 48) + " factual session text " + index,
                        ParentReportCardKind.Fact))
                    .ToArray();
                view.Present(new ParentReportSnapshot(Guid.NewGuid(), ParentReportState.Ready, cards, 0f, ParentReportView.RestartControlId));
                view.SetInputReady(true);
                Assert.That(view.RenderedCardCount, Is.EqualTo(12));
                Assert.That(view.ScrollReachable, Is.True);
                CollectionAssert.AreEqual(new[]
                {
                    ParentReportView.CloseControlId,
                    ParentReportView.RestartControlId,
                    ParentReportView.ScrollControlId,
                }, view.FocusOrder);
                Assert.That(view.VisibleArtRaycastsDisabled(), Is.True);
            }
            finally { router.Dispose(); UnityEngine.Object.DestroyImmediate(owner); }
        }

        private static void AssertPrefab<T>(string path) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            Assert.That(prefab.GetComponent<T>(), Is.Not.Null, path);
            Assert.That(prefab.GetComponentsInChildren<Selectable>(true), Is.Empty, path);
        }

        private static CompletionAdultReportContent Content() =>
            AssetDatabase.LoadAssetAtPath<CompletionAdultReportContent>(
                CompletionAdultReportContentGenerator.ContentAssetPath);

        private static ChildInputRouter Router() => new ChildInputRouter(
            new InputAction("Point", InputActionType.PassThrough),
            new InputAction("Press", InputActionType.Button));
    }
}
