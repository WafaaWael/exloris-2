using System;
using System.Linq;
using Exloris.Runtime.Layout;
using Exloris.Runtime.Overview;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class JourneyOverviewPrefabContractTests
    {
        private const string PrefabPath = "Assets/Exloris/UI/Screens/JourneyOverview.prefab";

        [Test]
        public void PrefabContainsOnlyFourApprovedControlsAndNoTimelineInteraction()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, PrefabPath);
            Assert.That(prefab.GetComponent<JourneyOverviewStageView>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Selectable>(true), Is.Empty);
            var instance = UnityEngine.Object.Instantiate(prefab);
            var router = CreateRouter();
            try
            {
                var content = Content();
                var view = instance.GetComponent<JourneyOverviewStageView>();
                view.Configure(router, content);

                Assert.That(view.ControlIds, Is.EquivalentTo(new[]
                {
                    JourneyOverviewStageView.SkipControlId,
                    JourneyOverviewStageView.RepeatControlId,
                    JourneyOverviewStageView.PauseControlId,
                    JourneyOverviewStageView.HomeControlId,
                }));
                Assert.That(view.GetVisibleSprite(JourneyOverviewStageView.SkipControlId),
                    Is.SameAs(content.OverviewControls[0]));
                Assert.That(view.GetVisibleSprite(JourneyOverviewStageView.RepeatControlId),
                    Is.SameAs(content.OverviewControls[1]));
                Assert.That(view.GetVisibleSprite(JourneyOverviewStageView.PauseControlId),
                    Is.SameAs(content.OverviewControls[2]));
                Assert.That(view.GetVisibleSprite(JourneyOverviewStageView.HomeControlId),
                    Is.SameAs(content.OverviewControls[3]));
                Assert.That(view.TimelineSemanticControlCount, Is.Zero);
                Assert.That(view.TimelineHasCollider, Is.False);
                Assert.That(view.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(view.GetComponentsInChildren<Collider2D>(true), Is.Empty);
                Assert.That(view.AreTouchTargetsTransparent(), Is.True);
                Assert.That(view.VisibleArtRaycastsDisabled(), Is.True);
                Assert.That(view.GetComponentInChildren<SafeAreaController>(true), Is.Not.Null);
            }
            finally
            {
                router.Dispose();
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void RevealUsesCurrentMeasureAndEncouragingLockedReadAndHelpArt()
        {
            var owner = new GameObject("JourneyOverviewView");
            var router = CreateRouter();
            try
            {
                var content = Content();
                var view = owner.AddComponent<JourneyOverviewStageView>();
                view.Configure(router, content);
                var presenter = new JourneyOverviewRevealPresenter(content, view);
                var token = Guid.NewGuid();
                presenter.Present(new JourneyOverviewSnapshot(token,
                    JourneyOverviewRevealState.Ready, 0d, 3), true);
                presenter.SetInputReady(true);

                Assert.That(view.GetTimelineIcon(0), Is.SameAs(content.MeasureIcon));
                Assert.That(view.GetTimelineIcon(1), Is.SameAs(content.ReadIcon));
                Assert.That(view.GetTimelineIcon(2), Is.SameAs(content.HelpIcon));
                Assert.That(view.GetTimelineState(0), Is.SameAs(content.CurrentTimelineState));
                Assert.That(view.GetTimelineState(1), Is.SameAs(content.LockedTimelineState));
                Assert.That(view.GetTimelineState(2), Is.SameAs(content.LockedTimelineState));
                Assert.That(view.VisibleBeatCount, Is.EqualTo(3));
                Assert.That(view.CurrentPulseEnabled, Is.False);
                Assert.That(view.ControlsEnabled, Is.True);
                Assert.That(view.AllTimelineImagesRaycastDisabled(), Is.True);
                Assert.That(view.GetComponentsInChildren<Component>(true)
                    .Any(component => component.GetType().Name.Contains("Semantic")
                        && component.transform.name.Contains("Timeline")), Is.False);
            }
            finally
            {
                router.Dispose();
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        private static JourneyOverviewContent Content() =>
            AssetDatabase.LoadAssetAtPath<JourneyOverviewContent>(
                "Assets/Exloris/Resources/Content/JourneyOverviewContent.asset");

        private static Exloris.Runtime.Input.ChildInputRouter CreateRouter() =>
            new Exloris.Runtime.Input.ChildInputRouter(
                new InputAction("Point", InputActionType.PassThrough),
                new InputAction("Press", InputActionType.Button));
    }
}
