using System.Linq;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Recovery;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class JourneyOverviewOverlayPrefabContractTests
    {
        [Test]
        public void PauseHasExactSixControlsApprovedSpritesAndBlockingScrim()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Exloris/UI/Overlays/PauseOverlay.prefab");
            Assert.That(prefab, Is.Not.Null);
            var instance = Object.Instantiate(prefab);
            var callerObject = new GameObject("JourneyCaller");
            var router = CreateRouter();
            try
            {
                var content = Content();
                var caller = callerObject.AddComponent<JourneyOverviewStageView>();
                caller.Configure(router, content);
                var view = instance.GetComponent<PauseOverlayView>();
                view.Configure(router, content, caller);

                Assert.That(view.ControlIds, Is.EquivalentTo(new[]
                {
                    PauseOverlayView.HelpControlId,
                    PauseOverlayView.SoundControlId,
                    PauseOverlayView.CaptionsControlId,
                    PauseOverlayView.ResumeControlId,
                    PauseOverlayView.MotionControlId,
                    PauseOverlayView.HomeControlId,
                }));
                Assert.That(view.GetVisibleSprite(PauseOverlayView.HelpControlId),
                    Is.SameAs(content.PauseControls[0]));
                Assert.That(view.GetVisibleSprite(PauseOverlayView.ResumeControlId),
                    Is.SameAs(content.PauseControls[1]));
                Assert.That(view.GetVisibleSprite(PauseOverlayView.HomeControlId),
                    Is.SameAs(content.OverviewControls[3]));
                view.ApplySettings(new PauseSettings(false, false, true));
                Assert.That(view.GetVisibleSprite(PauseOverlayView.SoundControlId),
                    Is.SameAs(content.PauseControls[3]));
                Assert.That(view.GetVisibleSprite(PauseOverlayView.CaptionsControlId),
                    Is.SameAs(content.PauseControls[5]));
                Assert.That(view.GetVisibleSprite(PauseOverlayView.MotionControlId),
                    Is.SameAs(content.PauseControls[6]));
                Assert.That(view.ScrimBlocksRaycasts, Is.True);
                Assert.That(view.NonScrimArtRaycastsDisabled(), Is.True);
                Assert.That(view.AreTouchTargetsTransparent(), Is.True);
                Assert.That(instance.GetComponentsInChildren<Selectable>(true), Is.Empty);
            }
            finally
            {
                router.Dispose();
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(callerObject);
            }
        }

        [Test]
        public void HelpHasBackAsSoleControlAndFourDisplayOnlyPictograms()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Exloris/UI/Overlays/JourneyOverviewPauseHelp.prefab");
            Assert.That(prefab, Is.Not.Null);
            var instance = Object.Instantiate(prefab);
            var pauseObject = new GameObject("Pause");
            var callerObject = new GameObject("Caller");
            var router = CreateRouter();
            try
            {
                var content = Content();
                var caller = callerObject.AddComponent<JourneyOverviewStageView>();
                caller.Configure(router, content);
                var pause = pauseObject.AddComponent<PauseOverlayView>();
                pause.Configure(router, content, caller);
                var help = instance.GetComponent<JourneyOverviewPauseHelpView>();
                help.Configure(router, content, pause);

                Assert.That(help.ControlIds,
                    Is.EqualTo(new[] { JourneyOverviewPauseHelpView.BackControlId }));
                Assert.That(help.DisplayPictogramCount, Is.EqualTo(4));
                for (var index = 0; index < 4; index++)
                    Assert.That(help.GetDisplayPictogram(index),
                        Is.SameAs(content.OverviewControls[index]));
                Assert.That(help.DisplaySemanticControlCount, Is.Zero);
                Assert.That(help.GetVisibleSprite(JourneyOverviewPauseHelpView.BackControlId),
                    Is.SameAs(content.LeaveControls[1]));
                Assert.That(help.DisplayArtRaycastsDisabled(), Is.True);
                Assert.That(instance.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(instance.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            }
            finally
            {
                router.Dispose();
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(pauseObject);
                Object.DestroyImmediate(callerObject);
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
