using System.Collections;
using Exloris.Core.Navigation;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Bootstrap;
using Exloris.Runtime.Overview;
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
    public sealed class JourneyOverviewRecoveryTests : InputTestFixture
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
        public IEnumerator EntryTimeoutRetryCompletesWithFreshTransactionAndRevealToken()
        {
            var host = default(ScreenHost);
            yield return EnterPreview(result => host = result);
            host.DeferNextAvatarTransitionForTests();
            yield return Click(host.ActiveAvatarPreviewView
                .GetControlScreenBounds(AvatarPreviewView.ConfirmControlId).center);
            var failedId = host.ActiveNavigationId.Value;

            host.ForceNavigationTimeoutForTests();
            Assert.That(host.TryCompleteNavigation(failedId), Is.False);
            Assert.That(host.AvatarRecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.ActiveAvatarPreviewView.CaptionVisible, Is.False);
            Assert.That(host.ActiveAvatarPreviewView.CaptionBackplateVisible, Is.False);
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(
                LeaveConfirmationView.RetryControlId).center);
            var retryId = host.ActiveNavigationId.Value;

            Assert.That(retryId, Is.Not.EqualTo(failedId));
            Assert.That(host.CompletePendingAvatarTransitionForTests(), Is.True);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.JourneyOverview));
            Assert.That(host.LastCompletedJourneyEntryTransactionId, Is.EqualTo(retryId));
            Assert.That(host.CurrentJourneyOverviewToken, Is.Not.EqualTo(System.Guid.Empty));
            Assert.That(host.TryCompleteNavigation(failedId), Is.False);
        }

        [UnityTest]
        public IEnumerator EntryAssetFailureRecoveryHomeReturnsWelcomeWithoutPartialJourney()
        {
            var host = default(ScreenHost);
            yield return EnterPreview(result => host = result);
            host.DeferNextAvatarTransitionForTests();
            yield return Click(host.ActiveAvatarPreviewView
                .GetControlScreenBounds(AvatarPreviewView.ConfirmControlId).center);
            var failedId = host.ActiveNavigationId.Value;

            Assert.That(host.FailActiveAvatarTransitionForTests(), Is.True);
            Assert.That(host.AvatarRecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.ActiveJourneyOverviewView, Is.Null);
            Assert.That(host.TryCompleteNavigation(failedId), Is.False);
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(
                LeaveConfirmationView.HomeControlId).center);
            yield return null;

            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.SessionOpen, Is.True);
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.AvatarPreview));
            Assert.That(host.WelcomeInputReady, Is.True);
            Assert.That(host.ActiveJourneyOverviewView, Is.Null);
            Assert.That(host.TryCompleteNavigation(failedId), Is.False);
        }

        [UnityTest]
        public IEnumerator Stage1BoundaryTimeoutRetryCreatesFreshTransactionAndRejectsStaleCompletion()
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result);
            var boatToken = host.Boat1Snapshot.TokenId;
            yield return Click(host.ActiveBoat1View
                .GetControlScreenBounds(Boat1StageView.SkipControlId).center);
            Assert.That(host.CompleteBoat1ArrivalForDiagnostics(), Is.True);
            Assert.That(host.Boat1BoundaryLoading, Is.True);
            Assert.That(host.ActiveNavigationId.HasValue, Is.True);
            var failedId = host.ActiveNavigationId.Value;

            host.ForceNavigationTimeoutForTests();
            Assert.That(host.Boat1RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Recovery));
            Assert.That(host.Boat1InputReady, Is.False);
            Assert.That(host.TryCompleteNavigation(failedId), Is.False);
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(
                LeaveConfirmationView.RetryControlId).center);

            Assert.That(host.ActiveNavigationId, Is.Not.Null);
            Assert.That(host.ActiveNavigationId.Value, Is.Not.EqualTo(failedId));
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Stage1));
            Assert.That(host.Boat1Snapshot.TokenId, Is.EqualTo(boatToken));
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Navigating));
            Assert.That(host.Boat1RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Loading));
            Assert.That(host.TryCompleteNavigation(failedId), Is.False);
        }

        [UnityTest]
        public IEnumerator Stage1AssetFailureRecoveryHomeReturnsWelcomeAndPreservesSession()
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result);
            yield return Click(host.ActiveBoat1View
                .GetControlScreenBounds(Boat1StageView.SkipControlId).center);
            Assert.That(host.CompleteBoat1ArrivalForDiagnostics(), Is.True);
            Assert.That(host.Boat1BoundaryLoading, Is.True);
            Assert.That(host.ActiveNavigationId.HasValue, Is.True);
            var failedId = host.ActiveNavigationId.Value;

            Assert.That(host.ReportBoat1BoundaryAssetFailure(), Is.True);
            Assert.That(host.Boat1RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Recovery));
            Assert.That(host.TryCompleteNavigation(failedId), Is.False);
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(
                LeaveConfirmationView.HomeControlId).center);
            yield return null;

            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.SessionOpen, Is.True);
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Boat1));
            Assert.That(host.WelcomeInputReady, Is.True);
            Assert.That(host.ActiveBoat1View, Is.Null);
        }

        [UnityTest]
        public IEnumerator InfeasibleStage1BoundaryLayoutEntersMappedRecoveryWithoutMutation()
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result);
            var boatToken = host.Boat1Snapshot.TokenId;
            yield return Click(host.ActiveBoat1View
                .GetControlScreenBounds(Boat1StageView.SkipControlId).center);
            Assert.That(host.CompleteBoat1ArrivalForDiagnostics(), Is.True);
            Assert.That(host.Boat1BoundaryLoading, Is.True);
            Assert.That(host.ActiveNavigationId.HasValue, Is.True);

            Assert.That(host.TryApplyBoat1Layout(
                360f, 182f, 360f, 182f, 326f, 88), Is.False);

            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Boat1));
            Assert.That(host.Boat1Snapshot.TokenId, Is.EqualTo(boatToken));
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Recovery));
            Assert.That(host.Boat1RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.Boat1InputReady, Is.False);
            Assert.That(host.ActiveNavigationId, Is.Null);
            Assert.That(host.ActiveNavigationDestination, Is.Null);
        }

        [UnityTest]
        public IEnumerator RepeatedInterruptionsPreserveTheExactProductPauseLayer()
        {
            var host = default(ScreenHost);
            yield return EnterJourney(result => host = result);

            host.SendMessage("OnApplicationPause", true);
            yield return null;
            var appPause = host.ActivePauseOverlayView.Snapshot;
            host.SendMessage("OnApplicationPause", true);
            host.HandleFocusRestoredForTests();
            yield return null;

            Assert.That(host.JourneyPauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.ActivePauseOverlayView.Snapshot, Is.EqualTo(appPause));
            Assert.That(host.JourneyRecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Hidden));
            Assert.That(host.RecoveryVisible, Is.False);

            yield return Click(host.ActivePauseOverlayView
                .GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            yield return Click(host.ActiveJourneyOverviewView
                .GetControlScreenBounds(JourneyOverviewStageView.PauseControlId).center);
            yield return Click(host.ActivePauseOverlayView
                .GetControlScreenBounds(PauseOverlayView.HelpControlId).center);
            var helpPause = host.ActivePauseOverlayView.Snapshot;

            host.HandleAudioRouteChangeForTests();
            host.HandleFocusRestoredForTests();
            yield return null;

            Assert.That(host.JourneyPauseState, Is.EqualTo(PauseLayerState.Help));
            Assert.That(host.ActiveJourneyPauseHelpView.IsVisible, Is.True);
            Assert.That(host.ActivePauseOverlayView.Snapshot, Is.EqualTo(helpPause));
            Assert.That(host.JourneyRecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Hidden));
            Assert.That(host.RecoveryVisible, Is.False);
        }

        private IEnumerator EnterJourney(System.Action<ScreenHost> capture)
        {
            var host = default(ScreenHost);
            yield return EnterPreview(result => host = result);
            yield return Click(host.ActiveAvatarPreviewView
                .GetControlScreenBounds(AvatarPreviewView.ConfirmControlId).center);
            yield return null;
            Assert.That(host.JourneyInputReady, Is.True);
            capture(host);
        }

        private IEnumerator EnterBoat1(System.Action<ScreenHost> capture)
        {
            var host = default(ScreenHost);
            yield return EnterJourney(result => host = result);
            yield return Click(host.ActiveJourneyOverviewView
                .GetControlScreenBounds(JourneyOverviewStageView.SkipControlId).center);
            yield return null;
            Assert.That(host.Boat1InputReady, Is.True);
            capture(host);
        }

        private IEnumerator EnterPreview(System.Action<ScreenHost> capture)
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();
            host.ConfigureWelcomeEvidenceForDiagnostics(
                soundEnabled: true, captionsEnabled: true, reducedMotion: false);
            host.ConfigureAvatarInitialLeftForTests(AvatarChoice.Girl);
            yield return Click(host.ActiveWelcomeView
                .GetControlScreenBounds(WelcomeStageView.PlayControlId).center);
            yield return null;
            yield return Click(host.ActiveAvatarSelectView
                .GetControlScreenBounds(AvatarSelectView.GirlControlId).center);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AvatarPreview));
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
