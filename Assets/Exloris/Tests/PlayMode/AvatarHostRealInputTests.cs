using System.Collections;
using Exloris.Core.Navigation;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Bootstrap;
using Exloris.Runtime.Recovery;
using Exloris.Runtime.UI;
using Exloris.Runtime.Welcome;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class AvatarHostRealInputTests : InputTestFixture
    {
        private Mouse _mouse;

        public override void Setup()
        {
            base.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            if (PersistentCompositionRoot.Instance != null)
                Object.DestroyImmediate(PersistentCompositionRoot.Instance.gameObject);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator GirlFirstOtherBoyRepeatAndConfirmEnterJourneyOverview()
        {
            var host = default(ScreenHost);
            yield return EnterAvatar(AvatarChoice.Girl, result => host = result);
            Assert.That(host.AvatarLeftChoice, Is.EqualTo(AvatarChoice.Girl));

            yield return Click(host.ActiveAvatarSelectView
                .GetControlScreenBounds(AvatarSelectView.GirlControlId).center);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AvatarPreview));
            Assert.That(host.AvatarPreviewChoice, Is.EqualTo(AvatarChoice.Girl));
            Assert.That(host.SessionSelectedAvatarId, Is.Null);
            var firstToken = host.CurrentAvatarPreviewToken;

            yield return Click(host.ActiveAvatarPreviewView
                .GetControlScreenBounds(AvatarPreviewView.RepeatControlId).center);
            Assert.That(host.CurrentAvatarPreviewToken, Is.GreaterThan(firstToken));
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AvatarPreview));

            yield return Click(host.ActiveAvatarPreviewView
                .GetControlScreenBounds(AvatarPreviewView.OtherControlId).center);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AvatarSelect));
            Assert.That(host.AvatarPreviewChoice, Is.Null);
            Assert.That(host.AvatarLeftChoice, Is.EqualTo(AvatarChoice.Boy),
                "Re-entry alternates order without changing prominence.");

            yield return Click(host.ActiveAvatarSelectView
                .GetControlScreenBounds(AvatarSelectView.BoyControlId).center);
            yield return null;
            yield return Click(host.ActiveAvatarPreviewView
                .GetControlScreenBounds(AvatarPreviewView.ConfirmControlId).center);

            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.JourneyOverview));
            Assert.That(host.ActiveNavigationDestination, Is.Null);
            Assert.That(host.SessionSelectedAvatarId, Is.EqualTo("avatar_boy"));
            Assert.That(host.ActiveJourneyOverviewView, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator PreviewLeaveStayCancelAndWelcomeResumeRestoreExactPreview()
        {
            var host = default(ScreenHost);
            yield return EnterAvatar(AvatarChoice.Boy, result => host = result);
            yield return Click(host.ActiveAvatarSelectView
                .GetControlScreenBounds(AvatarSelectView.GirlControlId).center);
            yield return null;
            var previewToken = host.CurrentAvatarPreviewToken;
            var previewChoice = host.AvatarPreviewChoice;

            yield return Click(host.ActiveAvatarPreviewView
                .GetControlScreenBounds(AvatarPreviewView.HomeControlId).center);
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            Assert.That(host.ActiveAvatarPreviewView.ControlsEnabled, Is.False);
            yield return Click(host.ActiveLeaveConfirmationView
                .GetControlScreenBounds(LeaveConfirmationView.StayControlId).center);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AvatarPreview));
            Assert.That(host.AvatarPreviewChoice, Is.EqualTo(previewChoice));
            Assert.That(host.CurrentAvatarPreviewToken, Is.EqualTo(previewToken));
            Assert.That(host.ActiveAvatarPreviewView.ControlsEnabled, Is.True);

            yield return Click(host.ActiveAvatarPreviewView
                .GetControlScreenBounds(AvatarPreviewView.HomeControlId).center);
            yield return Click(host.ActiveLeaveConfirmationView
                .GetControlScreenBounds(LeaveConfirmationView.CancelControlId).center);
            Assert.That(host.AvatarPreviewChoice, Is.EqualTo(previewChoice));
            Assert.That(host.CurrentAvatarPreviewToken, Is.EqualTo(previewToken));

            yield return Click(host.ActiveAvatarPreviewView
                .GetControlScreenBounds(AvatarPreviewView.HomeControlId).center);
            yield return Click(host.ActiveLeaveConfirmationView
                .GetControlScreenBounds(LeaveConfirmationView.LeaveControlId).center);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.SessionOpen, Is.True);
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.AvatarPreview));
            Assert.That(host.WelcomeInputReady, Is.True);

            currentTime += 0.3d;
            yield return Click(host.ActiveWelcomeView
                .GetControlScreenBounds(WelcomeStageView.PlayControlId).center);
            yield return Click(host.ActiveSessionChoiceView
                .GetControlScreenBounds(SessionChoiceView.ContinueControlId).center);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AvatarPreview));
            Assert.That(host.AvatarPreviewChoice, Is.EqualTo(previewChoice));
            Assert.That(host.CurrentAvatarPreviewToken, Is.EqualTo(previewToken));
            Assert.That(host.ActiveAvatarPreviewView.ControlsEnabled, Is.True);
        }

        private IEnumerator EnterAvatar(AvatarChoice initialLeft, System.Action<ScreenHost> capture)
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();
            host.ConfigureAvatarInitialLeftForTests(initialLeft);
            yield return Click(host.ActiveWelcomeView
                .GetControlScreenBounds(WelcomeStageView.PlayControlId).center);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AvatarSelect));
            Assert.That(host.AvatarInputReady, Is.True);
            capture(host);
        }

        private static IEnumerator LoadFreshWelcome()
        {
            if (PersistentCompositionRoot.Instance != null)
            {
                Object.Destroy(PersistentCompositionRoot.Instance.gameObject);
                yield return null;
            }
            SceneManager.LoadScene(0, LoadSceneMode.Single);
            yield return null;
            var host = Object.FindFirstObjectByType<ScreenHost>();
            var deadline = Time.realtimeSinceStartup + 8f;
            while (host.CurrentRoute == RouteId.StartupLoading && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(host.SkipIntroForTests(), Is.True);
            yield return null;
            yield return null;
            Assert.That(host.WelcomeInputReady, Is.True);
        }

        private IEnumerator Click(Vector2 position)
        {
            var startedAt = currentTime + 0.01d;
            Set(_mouse.position, position, startedAt);
            Press(_mouse.leftButton, startedAt + 0.01d);
            Release(_mouse.leftButton, startedAt + 0.06d);
            currentTime = startedAt + 0.07d;
            yield return null;
        }
    }
}
