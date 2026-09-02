using System.Collections;
using Exloris.Core.Navigation;
using Exloris.Runtime.Avatar;
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
    public sealed class JourneyOverviewHostRealInputTests : InputTestFixture
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
        public IEnumerator RepeatPauseHelpBackNestedStayResumeAndSkipReachBoatBoundary()
        {
            var host = default(ScreenHost);
            yield return EnterJourney(result => host = result);
            var firstToken = host.CurrentJourneyOverviewToken;

            var inertCommandCount = host.JourneyAcceptedCommandCount;
            var inertLastControl = host.LastAcceptedControlId;
            for (var index = 0; index < 3; index++)
            {
                var bounds = host.ActiveJourneyOverviewView.GetTimelineMedallionScreenBounds(index);
                yield return Click(bounds.center);
                yield return Click(new Vector2(bounds.xMin + 2f, bounds.center.y));
            }
            for (var index = 0; index < 2; index++)
            {
                var bounds = host.ActiveJourneyOverviewView.GetTimelineConnectorScreenBounds(index);
                yield return Click(bounds.center);
            }
            Assert.That(host.JourneyAcceptedCommandCount, Is.EqualTo(inertCommandCount));
            Assert.That(host.LastAcceptedControlId, Is.EqualTo(inertLastControl));
            Assert.That(host.CurrentJourneyOverviewToken, Is.EqualTo(firstToken));
            Assert.That(host.ActiveNavigationDestination, Is.Null);

            yield return Click(host.ActiveJourneyOverviewView
                .GetControlScreenBounds(JourneyOverviewStageView.RepeatControlId).center);
            Assert.That(host.CurrentJourneyOverviewToken, Is.Not.EqualTo(firstToken));

            yield return Click(host.ActiveJourneyOverviewView
                .GetControlScreenBounds(JourneyOverviewStageView.PauseControlId).center);
            Assert.That(host.JourneyPauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.ActivePauseOverlayView.IsVisible, Is.True);
            var paused = host.ActivePauseOverlayView.Snapshot;

            yield return Click(host.ActivePauseOverlayView
                .GetControlScreenBounds(PauseOverlayView.HelpControlId).center);
            Assert.That(host.ActiveJourneyPauseHelpView.IsVisible, Is.True);
            inertCommandCount = host.JourneyAcceptedCommandCount;
            inertLastControl = host.LastAcceptedControlId;
            for (var index = 0; index < host.ActiveJourneyPauseHelpView.DisplayPictogramCount; index++)
            {
                var bounds = host.ActiveJourneyPauseHelpView.GetDisplayPictogramScreenBounds(index);
                yield return Click(bounds.center);
                yield return Click(new Vector2(bounds.xMin + 2f, bounds.center.y));
            }
            Assert.That(host.JourneyAcceptedCommandCount, Is.EqualTo(inertCommandCount));
            Assert.That(host.LastAcceptedControlId, Is.EqualTo(inertLastControl));
            Assert.That(host.JourneyPauseState, Is.EqualTo(PauseLayerState.Help));
            Assert.That(host.ActiveNavigationDestination, Is.Null);
            yield return Click(host.ActiveJourneyPauseHelpView
                .GetControlScreenBounds(JourneyOverviewPauseHelpView.BackControlId).center);
            Assert.That(host.JourneyPauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.ActivePauseOverlayView.Snapshot, Is.EqualTo(paused));

            yield return Click(host.ActivePauseOverlayView
                .GetControlScreenBounds(PauseOverlayView.HomeControlId).center);
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            yield return Click(host.ActiveLeaveConfirmationView
                .GetControlScreenBounds(LeaveConfirmationView.StayControlId).center);
            Assert.That(host.JourneyPauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.ActivePauseOverlayView.Snapshot, Is.EqualTo(paused));

            yield return Click(host.ActivePauseOverlayView
                .GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.JourneyPauseState, Is.EqualTo(PauseLayerState.Hidden));
            Assert.That(host.JourneyInputReady, Is.True);

            yield return Click(host.ActiveJourneyOverviewView
                .GetControlScreenBounds(JourneyOverviewStageView.SkipControlId).center);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Boat1));
            Assert.That(host.ActiveNavigationDestination, Is.Null);
            Assert.That(host.ActiveJourneyOverviewView, Is.Null);
            Assert.That(host.ActiveBoat1View, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator DiagnosticLeaveCaptureActuallyBuildsPauseThenLeaveStack()
        {
            var host = default(ScreenHost);
            yield return EnterJourney(result => host = result);

            Assert.That(host.PresentJourneyLeaveForDiagnostics(), Is.True);
            Assert.That(host.JourneyPauseState, Is.EqualTo(PauseLayerState.Leave));
            Assert.That(host.ActivePauseOverlayView.IsVisible, Is.True);
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator DirectHomeStayRestoresExactRevealAndLeaveReturnsToWelcome()
        {
            var host = default(ScreenHost);
            yield return EnterJourney(result => host = result);
            var before = host.JourneySnapshot;

            yield return Click(host.ActiveJourneyOverviewView
                .GetControlScreenBounds(JourneyOverviewStageView.HomeControlId).center);
            yield return Click(host.ActiveLeaveConfirmationView
                .GetControlScreenBounds(LeaveConfirmationView.StayControlId).center);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.JourneyOverview));
            Assert.That(host.JourneySnapshot.TokenId, Is.EqualTo(before.TokenId));
            Assert.That(host.JourneySnapshot.ElapsedSeconds,
                Is.EqualTo(before.ElapsedSeconds).Within(0.05d));
            Assert.That(host.JourneyInputReady, Is.True);

            currentTime += 0.3d;
            yield return Click(host.ActiveJourneyOverviewView
                .GetControlScreenBounds(JourneyOverviewStageView.HomeControlId).center);
            yield return Click(host.ActiveLeaveConfirmationView
                .GetControlScreenBounds(LeaveConfirmationView.LeaveControlId).center);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.SessionOpen, Is.True);
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.JourneyOverview));
            Assert.That(host.WelcomeInputReady, Is.True);
        }

        [UnityTest]
        public IEnumerator MutedNaturalCompletionEntersBoat1ExactlyOnce()
        {
            var host = default(ScreenHost);
            yield return EnterJourney(result => host = result);
            yield return Click(host.ActiveJourneyOverviewView
                .GetControlScreenBounds(JourneyOverviewStageView.PauseControlId).center);
            if (host.ActivePauseOverlayView.Snapshot.Settings.SoundEnabled)
            {
                yield return Click(host.ActivePauseOverlayView
                    .GetControlScreenBounds(PauseOverlayView.SoundControlId).center);
                Assert.That(host.LastAcceptedControlId, Is.EqualTo(PauseOverlayView.SoundControlId));
            }
            Assert.That(host.ActivePauseOverlayView.Snapshot.Settings.SoundEnabled, Is.False);
            yield return Click(host.ActivePauseOverlayView
                .GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);

            var deadline = Time.realtimeSinceStartup + 10.5f;
            while (host.CurrentRoute != RouteId.Boat1
                && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Boat1));
            Assert.That(host.ActiveNavigationDestination, Is.Null);
            Assert.That(host.ActiveJourneyOverviewView, Is.Null);
            Assert.That(Object.FindObjectsByType<Exloris.Runtime.Boat.Boat1StageView>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SettingsAndInterruptionsRemainPausedUntilMappedResume()
        {
            var host = default(ScreenHost);
            yield return EnterJourney(result => host = result);
            yield return Click(host.ActiveJourneyOverviewView
                .GetControlScreenBounds(JourneyOverviewStageView.PauseControlId).center);
            var before = host.ActivePauseOverlayView.Snapshot;
            yield return Click(host.ActivePauseOverlayView
                .GetControlScreenBounds(PauseOverlayView.SoundControlId).center);
            Assert.That(host.LastAcceptedControlId, Is.EqualTo(PauseOverlayView.SoundControlId));
            yield return Click(host.ActivePauseOverlayView
                .GetControlScreenBounds(PauseOverlayView.CaptionsControlId).center);
            yield return Click(host.ActivePauseOverlayView
                .GetControlScreenBounds(PauseOverlayView.MotionControlId).center);
            yield return null;
            var after = host.ActivePauseOverlayView.Snapshot;
            Assert.That(after.CallerSnapshot.TokenId, Is.EqualTo(before.CallerSnapshot.TokenId));
            Assert.That(after.CallerSnapshot.ElapsedSeconds,
                Is.EqualTo(before.CallerSnapshot.ElapsedSeconds));
            Assert.That(after.Settings.SoundEnabled, Is.Not.EqualTo(before.Settings.SoundEnabled));
            Assert.That(after.Settings.CaptionsEnabled, Is.Not.EqualTo(before.Settings.CaptionsEnabled));
            Assert.That(after.Settings.ReducedMotion, Is.EqualTo(host.ReducedMotionEnabled));
            Assert.That(host.ActiveJourneyOverviewView.VisibleBeatCount, Is.EqualTo(3));
            yield return Click(host.ActivePauseOverlayView
                .GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);

            host.SendMessage("OnApplicationPause", true);
            yield return null;
            Assert.That(host.JourneyPauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.AudioInterrupted, Is.True);
            host.HandleFocusRestoredForTests();
            yield return null;
            Assert.That(host.JourneyPauseState, Is.EqualTo(PauseLayerState.Pause));
            currentTime += 0.3d;
            yield return Click(host.ActivePauseOverlayView
                .GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.AudioInterrupted, Is.False);

            yield return Click(host.ActiveJourneyOverviewView
                .GetControlScreenBounds(JourneyOverviewStageView.RepeatControlId).center);
            host.HandleAudioRouteChangeForTests();
            yield return null;
            Assert.That(host.JourneyPauseState, Is.EqualTo(PauseLayerState.Pause));
            host.HandleFocusRestoredForTests();
            yield return null;
            Assert.That(host.JourneyPauseState, Is.EqualTo(PauseLayerState.Pause));
        }

        [UnityTest]
        public IEnumerator SameFrameSkipAndRepeatAcceptOnlyPrimarySkip()
        {
            var host = default(ScreenHost);
            yield return EnterJourney(result => host = result);
            var beforeCount = host.JourneyAcceptedCommandCount;

            yield return MultiTouch(
                host.ActiveJourneyOverviewView
                    .GetControlScreenBounds(JourneyOverviewStageView.SkipControlId).center,
                host.ActiveJourneyOverviewView
                    .GetControlScreenBounds(JourneyOverviewStageView.RepeatControlId).center);

            Assert.That(host.JourneyAcceptedCommandCount, Is.EqualTo(beforeCount + 1));
            Assert.That(host.LastAcceptedControlId, Is.EqualTo(JourneyOverviewStageView.SkipControlId));
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Boat1));
            Assert.That(host.ActiveNavigationDestination, Is.Null);
        }

        [UnityTest]
        public IEnumerator SameFramePauseAndHomeAcceptOnlyPrimaryPause()
        {
            var host = default(ScreenHost);
            yield return EnterJourney(result => host = result);
            var beforeCount = host.JourneyAcceptedCommandCount;

            yield return MultiTouch(
                host.ActiveJourneyOverviewView
                    .GetControlScreenBounds(JourneyOverviewStageView.PauseControlId).center,
                host.ActiveJourneyOverviewView
                    .GetControlScreenBounds(JourneyOverviewStageView.HomeControlId).center);

            Assert.That(host.JourneyAcceptedCommandCount, Is.EqualTo(beforeCount + 1));
            Assert.That(host.LastAcceptedControlId, Is.EqualTo(JourneyOverviewStageView.PauseControlId));
            Assert.That(host.JourneyPauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.False);
            Assert.That(host.JourneyMediaPaused, Is.True);
        }

        [UnityTest]
        public IEnumerator SameFrameDuplicateSkipContactsNavigateOnlyOnce()
        {
            var host = default(ScreenHost);
            yield return EnterJourney(result => host = result);
            var beforeCount = host.JourneyAcceptedCommandCount;
            var skip = host.ActiveJourneyOverviewView
                .GetControlScreenBounds(JourneyOverviewStageView.SkipControlId).center;

            yield return MultiTouch(skip, skip + new Vector2(1f, 1f));

            Assert.That(host.JourneyAcceptedCommandCount, Is.EqualTo(beforeCount + 1));
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Boat1));
            Assert.That(host.ActiveNavigationDestination, Is.Null);
        }

        [UnityTest]
        public IEnumerator SameFrameRepeatAndMedallionAcceptOnlyRepeat()
        {
            var host = default(ScreenHost);
            yield return EnterJourney(result => host = result);
            var beforeCount = host.JourneyAcceptedCommandCount;
            var beforeToken = host.CurrentJourneyOverviewToken;

            yield return MultiTouch(
                host.ActiveJourneyOverviewView
                    .GetControlScreenBounds(JourneyOverviewStageView.RepeatControlId).center,
                host.ActiveJourneyOverviewView.GetTimelineMedallionScreenBounds(0).center);

            Assert.That(host.JourneyAcceptedCommandCount, Is.EqualTo(beforeCount + 1));
            Assert.That(host.LastAcceptedControlId, Is.EqualTo(JourneyOverviewStageView.RepeatControlId));
            Assert.That(host.CurrentJourneyOverviewToken, Is.Not.EqualTo(beforeToken));
            Assert.That(host.ActiveNavigationDestination, Is.Null);
        }

        [UnityTest]
        public IEnumerator EvidenceConfigurationSelectsFullOrReducedMotionDeterministically()
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();

            host.ConfigureWelcomeEvidenceForDiagnostics(
                soundEnabled: true, captionsEnabled: true, reducedMotion: false);
            Assert.That(host.ReducedMotionEnabled, Is.False);

            host.ConfigureWelcomeEvidenceForDiagnostics(
                soundEnabled: true, captionsEnabled: true, reducedMotion: true);
            Assert.That(host.ReducedMotionEnabled, Is.True);

            host.ConfigureWelcomeEvidenceForDiagnostics(
                soundEnabled: true, captionsEnabled: true, reducedMotion: false);
        }

        private IEnumerator EnterJourney(System.Action<ScreenHost> capture)
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();
            host.ConfigureAvatarInitialLeftForTests(AvatarChoice.Girl);
            yield return Click(host.ActiveWelcomeView
                .GetControlScreenBounds(WelcomeStageView.PlayControlId).center);
            yield return null;
            yield return Click(host.ActiveAvatarSelectView
                .GetControlScreenBounds(AvatarSelectView.GirlControlId).center);
            yield return null;
            yield return Click(host.ActiveAvatarPreviewView
                .GetControlScreenBounds(AvatarPreviewView.ConfirmControlId).center);
            yield return null;
            Assert.That(host.JourneyInputReady, Is.True);
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

        private IEnumerator MultiTouch(Vector2 primary, Vector2 secondary)
        {
            var touchscreen = InputSystem.AddDevice<Touchscreen>();
            currentTime += 0.3d;
            BeginTouch(1, primary, screen: touchscreen);
            BeginTouch(2, secondary, screen: touchscreen);
            EndTouch(2, secondary, screen: touchscreen);
            EndTouch(1, primary, screen: touchscreen);
            yield return null;
        }
    }
}
