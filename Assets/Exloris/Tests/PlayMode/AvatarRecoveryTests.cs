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
    public sealed class AvatarRecoveryTests : InputTestFixture
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
        public IEnumerator SelectToPreviewTimeoutRejectsStaleAndRetryUsesFreshTransaction()
        {
            var host = default(ScreenHost);
            yield return EnterAvatar(result => host = result);
            host.DeferNextAvatarTransitionForTests();
            yield return Click(host.ActiveAvatarSelectView
                .GetControlScreenBounds(AvatarSelectView.BoyControlId).center);
            var failedId = host.ActiveNavigationId.Value;

            host.ForceNavigationTimeoutForTests();
            Assert.That(host.AvatarRecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.TryCompleteNavigation(failedId), Is.False);
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(
                LeaveConfirmationView.RetryControlId).center);

            Assert.That(host.ActiveNavigationId, Is.Not.Null);
            Assert.That(host.ActiveNavigationId.Value, Is.Not.EqualTo(failedId));
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.AvatarPreview));
            Assert.That(host.AvatarBoundaryLoading, Is.True);
            Assert.That(host.CompletePendingAvatarTransitionForTests(), Is.True);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AvatarPreview));
            Assert.That(host.AvatarPreviewChoice, Is.EqualTo(AvatarChoice.Boy));
        }

        [UnityTest]
        public IEnumerator PreviewToSelectAssetFailureUsesMappedRecoveryHomeAndPreservesSession()
        {
            var host = default(ScreenHost);
            yield return EnterPreview(result => host = result);
            host.DeferNextAvatarTransitionForTests();
            yield return Click(host.ActiveAvatarPreviewView
                .GetControlScreenBounds(AvatarPreviewView.OtherControlId).center);
            var failedId = host.ActiveNavigationId.Value;

            Assert.That(host.FailActiveAvatarTransitionForTests(), Is.True);
            Assert.That(host.AvatarRecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.TryCompleteNavigation(failedId), Is.False);
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(
                LeaveConfirmationView.HomeControlId).center);
            yield return null;

            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.SessionOpen, Is.True);
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.AvatarPreview));
            Assert.That(host.WelcomeInputReady, Is.True);
        }

        [UnityTest]
        public IEnumerator PreviewInterruptionNeverAutoResumesAndMappedResumeUsesFreshToken()
        {
            var host = default(ScreenHost);
            yield return EnterPreview(result => host = result);
            var token = host.CurrentAvatarPreviewToken;

            Assert.That(host.HandleAvatarInterruptionForTests(), Is.True);
            Assert.That(host.AvatarRecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Paused));
            Assert.That(host.AvatarMotionPaused, Is.True);
            host.HandleFocusRestoredForTests();
            Assert.That(host.AvatarRecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Paused));
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(
                LeaveConfirmationView.ResumeControlId).center);

            Assert.That(host.AvatarRecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Hidden));
            Assert.That(host.AvatarMotionPaused, Is.False);
            Assert.That(host.CurrentAvatarPreviewToken, Is.GreaterThan(token));
            Assert.That(host.AvatarPreviewChoice, Is.EqualTo(AvatarChoice.Girl));
            Assert.That(host.ActiveAvatarPreviewView.ControlsEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator PreviewToJourneyTimeoutRetainsBoundaryAndRetryUsesFreshTransaction()
        {
            var host = default(ScreenHost);
            yield return EnterPreview(result => host = result);
            host.DeferNextAvatarTransitionForTests();
            yield return Click(host.ActiveAvatarPreviewView
                .GetControlScreenBounds(AvatarPreviewView.ConfirmControlId).center);
            var failedId = host.ActiveNavigationId.Value;

            host.ForceNavigationTimeoutForTests();
            Assert.That(host.AvatarRecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.TryCompleteNavigation(failedId), Is.False);
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(
                LeaveConfirmationView.RetryControlId).center);

            Assert.That(host.ActiveNavigationId, Is.Not.Null);
            Assert.That(host.ActiveNavigationId.Value, Is.Not.EqualTo(failedId));
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.JourneyOverview));
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AvatarPreview));
            Assert.That(host.AvatarBoundaryLoading, Is.True);
        }

        [UnityTest]
        public IEnumerator LeaveToWelcomeAssetFailureRejectsStaleAndRetryCompletesOnce()
        {
            var host = default(ScreenHost);
            yield return EnterAvatar(result => host = result);
            yield return Click(host.ActiveAvatarSelectView
                .GetControlScreenBounds(AvatarSelectView.HomeControlId).center);
            host.DeferNextAvatarTransitionForTests();
            yield return Click(host.ActiveLeaveConfirmationView
                .GetControlScreenBounds(LeaveConfirmationView.LeaveControlId).center);
            var failedId = host.ActiveNavigationId.Value;

            Assert.That(host.FailActiveAvatarTransitionForTests(), Is.True);
            Assert.That(host.TryCompleteNavigation(failedId), Is.False);
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(
                LeaveConfirmationView.RetryControlId).center);
            var retryId = host.ActiveNavigationId.Value;
            Assert.That(retryId, Is.Not.EqualTo(failedId));
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.CompletePendingAvatarTransitionForTests(), Is.True);
            yield return null;

            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.TryCompleteNavigation(retryId), Is.False);
            Assert.That(host.SessionOpen, Is.True);
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.AvatarSelect));
        }

        [UnityTest]
        public IEnumerator SelectInterruptionBlocksNormalInputUntilMappedResume()
        {
            var host = default(ScreenHost);
            yield return EnterAvatar(result => host = result);

            Assert.That(host.HandleAvatarInterruptionForTests(), Is.True);
            Assert.That(host.AudioInterrupted, Is.True);
            Assert.That(host.AvatarMotionPaused, Is.True);
            Assert.That(host.ActiveAvatarSelectView.ControlsEnabled, Is.False);
            host.HandleFocusRestoredForTests();
            Assert.That(host.AvatarRecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Paused));
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(
                LeaveConfirmationView.ResumeControlId).center);

            Assert.That(host.AudioInterrupted, Is.False);
            Assert.That(host.AvatarMotionPaused, Is.False);
            Assert.That(host.ActiveAvatarSelectView.ControlsEnabled, Is.True);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AvatarSelect));
        }

        [UnityTest]
        public IEnumerator LeaveInterruptionRestoresExactLeaveCallerAndNotUnderlyingInput()
        {
            var host = default(ScreenHost);
            yield return EnterPreview(result => host = result);
            yield return Click(host.ActiveAvatarPreviewView
                .GetControlScreenBounds(AvatarPreviewView.HomeControlId).center);
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);

            Assert.That(host.HandleAvatarInterruptionForTests(), Is.True);
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(
                LeaveConfirmationView.ResumeControlId).center);

            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            Assert.That(host.ActiveAvatarPreviewView.ControlsEnabled, Is.False);
            Assert.That(host.ActiveLeaveConfirmationView.CurrentCaller, Is.EqualTo(RouteId.AvatarPreview));
        }

        private IEnumerator EnterPreview(System.Action<ScreenHost> capture)
        {
            var host = default(ScreenHost);
            yield return EnterAvatar(result => host = result);
            yield return Click(host.ActiveAvatarSelectView
                .GetControlScreenBounds(AvatarSelectView.GirlControlId).center);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AvatarPreview));
            capture(host);
        }

        private IEnumerator EnterAvatar(System.Action<ScreenHost> capture)
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
            yield return Click(host.ActiveWelcomeView
                .GetControlScreenBounds(WelcomeStageView.PlayControlId).center);
            yield return null;
            Assert.That(host.AvatarInputReady, Is.True);
            capture(host);
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
