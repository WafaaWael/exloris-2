using System.Linq;
using Exloris.Runtime.Layout;
using Exloris.Runtime.Welcome;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class WelcomePrefabContractTests
    {
        [Test]
        public void ApprovedWelcomeAndSessionChoicePrefabsExistWithoutBakedTextOrSelectableControls()
        {
            var welcome = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Exloris/UI/Screens/Welcome.prefab");
            var sessionChoice = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Exloris/UI/Overlays/SessionChoice.prefab");

            Assert.That(welcome, Is.Not.Null);
            Assert.That(welcome.GetComponent<WelcomeStageView>(), Is.Not.Null);
            Assert.That(welcome.GetComponentsInChildren<Selectable>(true), Is.Empty);
            Assert.That(welcome.GetComponentsInChildren<Component>(true).Any(IsTextComponent), Is.False);
            Assert.That(sessionChoice, Is.Not.Null);
            Assert.That(sessionChoice.GetComponent<SessionChoiceView>(), Is.Not.Null);
            Assert.That(sessionChoice.GetComponentsInChildren<Selectable>(true), Is.Empty);
            Assert.That(sessionChoice.GetComponentsInChildren<Component>(true).Any(IsTextComponent), Is.False);
        }

        [Test]
        public void RuntimeViewsExposeOnlyExactSemanticControlsWithApprovedArtAndInvisibleTargets()
        {
            var content = AssetDatabase.LoadAssetAtPath<WelcomeContent>(
                "Assets/Exloris/Resources/Content/WelcomeContent.asset");
            var welcomePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Exloris/UI/Screens/Welcome.prefab");
            var sessionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Exloris/UI/Overlays/SessionChoice.prefab");
            var welcomeObject = Object.Instantiate(welcomePrefab);
            var sessionObject = Object.Instantiate(sessionPrefab);
            var welcome = welcomeObject.GetComponent<WelcomeStageView>();
            var session = sessionObject.GetComponent<SessionChoiceView>();
            var welcomeRouter = CreateRouter();
            var sessionRouter = CreateRouter();

            try
            {
                welcome.Configure(welcomeRouter, content);
                session.Configure(sessionRouter, content);

                Assert.That(welcome.ControlIds, Is.EquivalentTo(new[]
                {
                    WelcomeStageView.AdultControlId,
                    WelcomeStageView.SoundControlId,
                    WelcomeStageView.CaptionsControlId,
                    WelcomeStageView.MotionControlId,
                    WelcomeStageView.PlayControlId,
                }));
                Assert.That(session.ControlIds, Is.EquivalentTo(new[]
                {
                    SessionChoiceView.ContinueControlId,
                    SessionChoiceView.RestartControlId,
                    SessionChoiceView.BackControlId,
                }));
                Assert.That(welcome.GetVisibleSprite(WelcomeStageView.PlayControlId), Is.SameAs(content.Play));
                Assert.That(welcome.GetVisibleSprite(WelcomeStageView.AdultControlId), Is.SameAs(content.Adult));
                Assert.That(welcome.TechnicalControlIds, Is.EquivalentTo(new[]
                {
                    WelcomeStageView.ResumeControlId,
                    WelcomeStageView.RetryControlId,
                    WelcomeStageView.HomeControlId,
                }));
                Assert.That(welcome.GetVisibleSprite(WelcomeStageView.ResumeControlId), Is.SameAs(content.Resume));
                Assert.That(welcome.GetVisibleSprite(WelcomeStageView.RetryControlId), Is.SameAs(content.Retry));
                Assert.That(welcome.GetVisibleSprite(WelcomeStageView.HomeControlId), Is.SameAs(content.Home));
                Assert.That(session.GetVisibleSprite(SessionChoiceView.ContinueControlId), Is.SameAs(content.Continue));
                Assert.That(session.GetVisibleSprite(SessionChoiceView.RestartControlId), Is.SameAs(content.Restart));
                Assert.That(session.GetVisibleSprite(SessionChoiceView.BackControlId), Is.SameAs(content.Back));
                Assert.That(welcome.AreTouchTargetsTransparent(), Is.True);
                Assert.That(session.AreTouchTargetsTransparent(), Is.True);
                Assert.That(welcome.VisibleArtRaycastsDisabled(), Is.True);
                Assert.That(session.VisibleArtRaycastsDisabled(), Is.True);
                Assert.That(welcome.GetComponentsInChildren<Selectable>(true), Is.Empty);
                Assert.That(session.GetComponentsInChildren<Selectable>(true), Is.Empty);
                Assert.That(welcome.GetComponentsInChildren<Transform>(true)
                    .Any(item => item.name.ToLowerInvariant().Contains("progress")), Is.False);
                Assert.That(session.GetComponentsInChildren<Transform>(true)
                    .Any(item => item.name.ToLowerInvariant().Contains("hold")), Is.False);

                welcome.ShowMediaCue(
                    WelcomePresentationState.Welcome,
                    content.WelcomeVisualEquivalent,
                    WelcomeMediaPresenter.WelcomeCaption1Id,
                    false,
                    0d);
                Assert.That(welcome.VisualEquivalentVisible, Is.True);

                welcome.ShowTechnicalOverlay(WelcomeTechnicalOverlayState.Paused);
                Assert.That(welcome.NormalControlsEnabled, Is.False);
                Assert.That(welcome.NormalControlsVisible, Is.False);
                Assert.That(welcome.VisualEquivalentVisible, Is.False,
                    "The non-reading Play cue must not look tappable during interruption.");
                Assert.That(welcome.IsTechnicalControlEnabled(WelcomeStageView.ResumeControlId), Is.True);
                Assert.That(welcome.IsTechnicalControlEnabled(WelcomeStageView.RetryControlId), Is.False);
                Assert.That(welcome.IsTechnicalControlEnabled(WelcomeStageView.HomeControlId), Is.False);
                Assert.That(welcome.IsTechnicalControlVisible(WelcomeStageView.ResumeControlId), Is.True);
                Assert.That(welcome.IsTechnicalControlVisible(WelcomeStageView.RetryControlId), Is.False);
                Assert.That(welcome.IsTechnicalControlVisible(WelcomeStageView.HomeControlId), Is.False);

                welcome.ShowTechnicalOverlay(WelcomeTechnicalOverlayState.Recovery);
                Assert.That(welcome.NormalControlsVisible, Is.False);
                Assert.That(welcome.VisualEquivalentVisible, Is.False,
                    "The non-reading Play cue must not become a dead control during recovery.");
                Assert.That(welcome.IsTechnicalControlVisible(WelcomeStageView.ResumeControlId), Is.False);
                Assert.That(welcome.IsTechnicalControlVisible(WelcomeStageView.RetryControlId), Is.True);
                Assert.That(welcome.IsTechnicalControlVisible(WelcomeStageView.HomeControlId), Is.True);

                welcome.HideTechnicalOverlay(WelcomePresentationState.Welcome);
                Assert.That(welcome.NormalControlsVisible, Is.True);
                welcome.ShowMediaCue(
                    WelcomePresentationState.Welcome,
                    content.WelcomeVisualEquivalent,
                    WelcomeMediaPresenter.WelcomeCaption1Id,
                    false,
                    1d);
                Assert.That(welcome.VisualEquivalentVisible, Is.True);
            }
            finally
            {
                welcomeRouter.Dispose();
                sessionRouter.Dispose();
                Object.DestroyImmediate(welcomeObject);
                Object.DestroyImmediate(sessionObject);
            }
        }

        [Test]
        public void RuntimeLayoutsFollowFrozenRatioTargetGapAndContainmentFormulas()
        {
            var cases = new[]
            {
                new LayoutCase(2160f, 1620f, 1598.77f, 1183.08f, 264f, 88, 97f, 41f, ResponsiveBackgroundVariant.FourByThree),
                new LayoutCase(1334f, 750f, 1856f, 1016f, 326f, 88, 224f, 94f, ResponsiveBackgroundVariant.SixteenByNine),
                new LayoutCase(2400f, 1080f, 2038.84f, 825.52f, 429f, 144, 182f, 77f, ResponsiveBackgroundVariant.UltraWide),
            };
            var content = AssetDatabase.LoadAssetAtPath<WelcomeContent>(
                "Assets/Exloris/Resources/Content/WelcomeContent.asset");
            var welcomePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Exloris/UI/Screens/Welcome.prefab");
            var sessionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Exloris/UI/Overlays/SessionChoice.prefab");

            foreach (var item in cases)
            {
                var welcomeObject = Object.Instantiate(welcomePrefab);
                var sessionObject = Object.Instantiate(sessionPrefab);
                var welcome = welcomeObject.GetComponent<WelcomeStageView>();
                var session = sessionObject.GetComponent<SessionChoiceView>();
                var welcomeRouter = CreateRouter();
                var sessionRouter = CreateRouter();
                try
                {
                    welcome.Configure(welcomeRouter, content);
                    session.Configure(sessionRouter, content);
                    welcome.ApplyResponsiveLayout(
                        item.ScreenWidth,
                        item.ScreenHeight,
                        item.SafeWidth,
                        item.SafeHeight,
                        item.Ppi,
                        item.PlatformMinimumPixels);
                    session.ApplyResponsiveLayout(
                        item.ScreenWidth,
                        item.ScreenHeight,
                        item.SafeWidth,
                        item.SafeHeight,
                        item.Ppi,
                        item.PlatformMinimumPixels);

                    Assert.That(welcome.CurrentTargetSize, Is.EqualTo(item.Target).Within(0.01f));
                    Assert.That(welcome.CurrentGap, Is.EqualTo(item.Gap).Within(0.01f));
                    Assert.That(welcome.BackgroundVariant, Is.EqualTo(item.Background));
                    Assert.That(welcome.GetControlCanvasRect(WelcomeStageView.PlayControlId).size,
                        Is.EqualTo(new Vector2(2f * item.Target + item.Gap, item.Target)));
                    var playRect = welcome.GetControlCanvasRect(WelcomeStageView.PlayControlId);
                    var cueRect = welcome.VisualEquivalentCanvasRect;
                    Assert.That(cueRect.xMin, Is.GreaterThanOrEqualTo(playRect.xMin));
                    Assert.That(cueRect.yMin, Is.GreaterThanOrEqualTo(playRect.yMin));
                    Assert.That(cueRect.xMax, Is.LessThanOrEqualTo(playRect.xMax));
                    Assert.That(cueRect.yMax, Is.LessThanOrEqualTo(playRect.yMax));
                    Assert.That(welcome.DoTouchTargetsOverlap(), Is.False);
                    Assert.That(welcome.AreAllTargetsInsideSafeArea(), Is.True);
                    Assert.That(welcome.DoesEveryTargetContainVisibleArt(), Is.True);
                    Assert.That(session.CurrentTargetSize, Is.EqualTo(item.Target).Within(0.01f));
                    Assert.That(session.CurrentGap, Is.EqualTo(item.Gap).Within(0.01f));
                    Assert.That(session.DoTouchTargetsOverlap(), Is.False);
                    Assert.That(session.AreAllTargetsInsideSafeArea(), Is.True);
                    Assert.That(session.DoesEveryTargetContainVisibleArt(), Is.True);
                }
                finally
                {
                    welcomeRouter.Dispose();
                    sessionRouter.Dispose();
                    Object.DestroyImmediate(welcomeObject);
                    Object.DestroyImmediate(sessionObject);
                }
            }
        }

        [Test]
        public void SettingStateChangesSwapApprovedArtWithoutMovingTargetsOrCaptionBand()
        {
            var content = AssetDatabase.LoadAssetAtPath<WelcomeContent>(
                "Assets/Exloris/Resources/Content/WelcomeContent.asset");
            var welcomePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Exloris/UI/Screens/Welcome.prefab");
            var welcomeObject = Object.Instantiate(welcomePrefab);
            var welcome = welcomeObject.GetComponent<WelcomeStageView>();
            var router = CreateRouter();
            try
            {
                welcome.Configure(router, content);
                welcome.ApplyResponsiveLayout(1334f, 750f, 1856f, 1016f, 326f, 88);
                var soundRect = welcome.GetControlCanvasRect(WelcomeStageView.SoundControlId);
                var captionsRect = welcome.GetControlCanvasRect(WelcomeStageView.CaptionsControlId);
                var motionRect = welcome.GetControlCanvasRect(WelcomeStageView.MotionControlId);
                var captionBand = welcome.CaptionBandCanvasRect;

                welcome.SetSoundEnabled(false);
                welcome.SetCaptionsEnabled(false);
                welcome.SetReducedMotionEnabled(false);
                Assert.That(welcome.GetVisibleSprite(WelcomeStageView.SoundControlId), Is.SameAs(content.SoundOff));
                Assert.That(welcome.GetVisibleSprite(WelcomeStageView.CaptionsControlId), Is.SameAs(content.CaptionsOff));
                Assert.That(welcome.GetVisibleSprite(WelcomeStageView.MotionControlId), Is.SameAs(content.ReducedMotionOff));
                Assert.That(welcome.GetControlCanvasRect(WelcomeStageView.SoundControlId), Is.EqualTo(soundRect));
                Assert.That(welcome.GetControlCanvasRect(WelcomeStageView.CaptionsControlId), Is.EqualTo(captionsRect));
                Assert.That(welcome.GetControlCanvasRect(WelcomeStageView.MotionControlId), Is.EqualTo(motionRect));
                Assert.That(welcome.CaptionBandCanvasRect, Is.EqualTo(captionBand));
                Assert.That(captionBand.yMin, Is.GreaterThan(welcome.CurrentTargetSize + welcome.CurrentGap));
                Assert.That(captionBand.yMax, Is.LessThanOrEqualTo(1016f - welcome.CurrentTargetSize - welcome.CurrentGap));
            }
            finally
            {
                router.Dispose();
                Object.DestroyImmediate(welcomeObject);
            }
        }

        private static Exloris.Runtime.Input.ChildInputRouter CreateRouter()
        {
            return new Exloris.Runtime.Input.ChildInputRouter(
                new InputAction("Point", InputActionType.PassThrough),
                new InputAction("Press", InputActionType.Button));
        }

        private readonly struct LayoutCase
        {
            public LayoutCase(
                float screenWidth,
                float screenHeight,
                float safeWidth,
                float safeHeight,
                float ppi,
                int platformMinimumPixels,
                float target,
                float gap,
                ResponsiveBackgroundVariant background)
            {
                ScreenWidth = screenWidth;
                ScreenHeight = screenHeight;
                SafeWidth = safeWidth;
                SafeHeight = safeHeight;
                Ppi = ppi;
                PlatformMinimumPixels = platformMinimumPixels;
                Target = target;
                Gap = gap;
                Background = background;
            }

            public float ScreenWidth { get; }
            public float ScreenHeight { get; }
            public float SafeWidth { get; }
            public float SafeHeight { get; }
            public float Ppi { get; }
            public int PlatformMinimumPixels { get; }
            public float Target { get; }
            public float Gap { get; }
            public ResponsiveBackgroundVariant Background { get; }
        }

        private static bool IsTextComponent(Component component)
        {
            var type = component.GetType();
            return type.Namespace == "TMPro" || type == typeof(Text);
        }
    }
}
