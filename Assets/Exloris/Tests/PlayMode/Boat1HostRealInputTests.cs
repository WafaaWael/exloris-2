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
    public sealed class Boat1HostRealInputTests : InputTestFixture
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
        public IEnumerator MouseExercisesEveryOverlayPathThenStopsAtStage1Boundary()
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result);

            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.HelpControlId).center);
            Assert.That(host.ActiveBoat1HelpView.IsVisible, Is.True);
            yield return Click(host.ActiveBoat1HelpView.GetControlScreenBounds(Boat1HelpView.BackControlId).center);
            Assert.That(host.ActiveBoat1HelpView.IsVisible, Is.False);

            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.RepeatControlId).center);
            Assert.That(host.LastAcceptedControlId, Is.EqualTo(Boat1StageView.RepeatControlId));

            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.PauseControlId).center);
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Pause));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.HelpControlId).center);
            Assert.That(host.ActiveBoat1HelpView.IsVisible, Is.True);
            yield return Click(host.ActiveBoat1HelpView.GetControlScreenBounds(Boat1HelpView.BackControlId).center);
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Pause));
            var settingsBefore = host.ActivePauseOverlayView.Snapshot.Settings;
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.SoundControlId).center);
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.CaptionsControlId).center);
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.MotionControlId).center);
            var settingsAfter = host.ActivePauseOverlayView.Snapshot.Settings;
            Assert.That(settingsAfter.SoundEnabled, Is.Not.EqualTo(settingsBefore.SoundEnabled));
            Assert.That(settingsAfter.CaptionsEnabled, Is.Not.EqualTo(settingsBefore.CaptionsEnabled));
            Assert.That(settingsAfter.ReducedMotion, Is.Not.EqualTo(settingsBefore.ReducedMotion));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.HomeControlId).center);
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(LeaveConfirmationView.StayControlId).center);
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Pause));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Hidden));

            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.HomeControlId).center);
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(LeaveConfirmationView.StayControlId).center);
            Assert.That(host.Boat1InputReady, Is.True);

            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.ReedsControlId).center);
            Assert.That(host.Boat1Snapshot.Choice, Is.EqualTo(Boat1Choice.Reeds));
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Traveling));
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.SkipControlId).center);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Boat1));
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Arriving));
            var deadline = Time.realtimeSinceStartup + 2f;
            while (host.ActiveNavigationDestination != RouteId.Stage1
                && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Stage1));
            Assert.That(host.Boat1BoundaryLoading, Is.True);
            Assert.That(host.Boat1InputReady, Is.False);
        }

        [UnityTest]
        public IEnumerator TouchSelectsVillageAndPenSelectsFollowIbisOnFreshRoutes()
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result);
            var touch = InputSystem.AddDevice<Touchscreen>();
            yield return Touch(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.VillageControlId).center, touch);
            Assert.That(host.Boat1Snapshot.Choice, Is.EqualTo(Boat1Choice.Village));

            Object.Destroy(PersistentCompositionRoot.Instance.gameObject);
            yield return null;
            yield return EnterBoat1(result => host = result);
            var pen = InputSystem.AddDevice<Pen>();
            yield return PenClick(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.FollowIbisControlId).center, pen);
            Assert.That(host.Boat1Snapshot.Choice, Is.EqualTo(Boat1Choice.FollowIbis));
            Assert.That(host.Boat1IbisSfxPlaying, Is.True);
        }

        [UnityTest]
        public IEnumerator InactivityDoesNotChooseAndInterruptionNeedsMappedResume()
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result, reducedMotion: true);
            Assert.That(host.ReducedMotionEnabled, Is.True);

            var deadline = Time.realtimeSinceStartup + 0.6f;
            while (Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Choice));
            Assert.That(host.Boat1Snapshot.Choice, Is.EqualTo(Boat1Choice.None));
            Assert.That(host.ActiveNavigationDestination, Is.Null);

            host.SendMessage("OnApplicationPause", true);
            yield return null;
            var paused = host.ActivePauseOverlayView.Snapshot;
            host.HandleFocusRestoredForTests();
            yield return null;
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.ActivePauseOverlayView.Snapshot, Is.EqualTo(paused));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Hidden));
            Assert.That(host.Boat1InputReady, Is.True);
        }

        [UnityTest]
        public IEnumerator DirectLeaveReturnsToWelcomeAndPreservesBoatSession()
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result);
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.HomeControlId).center);
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(LeaveConfirmationView.LeaveControlId).center);
            yield return null;

            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.SessionOpen, Is.True);
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Boat1));
            Assert.That(host.ActiveBoat1View, Is.Null);
            Assert.That(host.WelcomeInputReady, Is.True);
        }

        [UnityTest]
        public IEnumerator NaturalArrivalCreatesOneStage1Boundary()
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result);
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.VillageControlId).center);
            var deadline = Time.realtimeSinceStartup + 7f;
            while (host.Boat1Snapshot.State != Boat1State.Arriving
                && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Arriving));
            Assert.That(host.Boat1Snapshot.ArrivalCommitted, Is.False);
            Assert.That(host.Boat1ArrivalSfxPlaying, Is.True);
            Assert.That(host.Boat1BoundaryLoading, Is.False);

            deadline = Time.realtimeSinceStartup + 2f;
            while (host.ActiveNavigationDestination != RouteId.Stage1
                && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Stage1));
            Assert.That(host.Boat1Snapshot.ArrivalCommitted, Is.True);
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Navigating));
            Assert.That(host.Boat1BoundaryLoading, Is.True);
        }

        [UnityTest]
        public IEnumerator ManualPauseDuringArrivalRestoresArrivalWithoutTechnicalRecovery()
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result);
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.SkipControlId).center);
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Arriving));
            var arriving = host.Boat1Snapshot;

            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.PauseControlId).center);
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.ActiveBoat1View.LastPauseReason, Is.EqualTo(Boat1PauseReason.Manual));
            Assert.That(host.Boat1RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Hidden));
            Assert.That(host.RecoveryVisible, Is.False);
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Arriving));
            Assert.That(host.Boat1Snapshot.Choice, Is.EqualTo(arriving.Choice));
            Assert.That(host.Boat1Snapshot.ArrivalCommitted, Is.False);
        }

        [UnityTest]
        public IEnumerator DirectHomeDuringArrivalStayRestoresArrivalWithoutNavigation()
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result);
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.SkipControlId).center);
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Arriving));

            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.HomeControlId).center);
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(LeaveConfirmationView.StayControlId).center);
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Arriving));
            Assert.That(host.Boat1Snapshot.ArrivalCommitted, Is.False);
            Assert.That(host.ActiveNavigationDestination, Is.Null);
            Assert.That(host.Boat1RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Hidden));
        }

        [UnityTest]
        public IEnumerator AppBackgroundDuringArrivalUsesMappedPauseUntilResume()
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result);
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.SkipControlId).center);
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Arriving));

            host.SendMessage("OnApplicationPause", true);
            yield return null;
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.ActiveBoat1View.LastPauseReason, Is.EqualTo(Boat1PauseReason.AppBackground));
            Assert.That(host.Boat1RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Hidden));
            Assert.That(host.RecoveryVisible, Is.False);
            host.HandleFocusRestoredForTests();
            yield return null;
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Pause));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Arriving));
        }

        [UnityTest]
        public IEnumerator AudioFocusLossDuringArrivalUsesMappedPauseUntilResume()
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result);
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.SkipControlId).center);
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Arriving));

            host.HandleAudioRouteChangeForTests();
            yield return null;
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.ActiveBoat1View.LastPauseReason, Is.EqualTo(Boat1PauseReason.AudioFocusLoss));
            Assert.That(host.Boat1RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Hidden));
            Assert.That(host.RecoveryVisible, Is.False);
            host.HandleFocusRestoredForTests();
            yield return null;
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Pause));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Arriving));
        }

        [UnityTest]
        public IEnumerator AppBackgroundDuringDirectHelpRestoresExactHelpLayer()
        {
            yield return InterruptionDuringDirectHelp(Boat1PauseReason.AppBackground);
        }

        [UnityTest]
        public IEnumerator AudioFocusLossDuringDirectHelpRestoresExactHelpLayer()
        {
            yield return InterruptionDuringDirectHelp(Boat1PauseReason.AudioFocusLoss);
        }

        [UnityTest]
        public IEnumerator AppBackgroundDuringDirectLeaveRestoresExactLeaveLayer()
        {
            yield return InterruptionDuringDirectLeave(Boat1PauseReason.AppBackground);
        }

        [UnityTest]
        public IEnumerator AudioFocusLossDuringDirectLeaveRestoresExactLeaveLayer()
        {
            yield return InterruptionDuringDirectLeave(Boat1PauseReason.AudioFocusLoss);
        }

        [UnityTest]
        public IEnumerator InterruptedHelpKeepsPauseHelpAndNestedLeaveMapped()
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result);
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.HelpControlId).center);
            host.SendMessage("OnApplicationPause", true);
            yield return null;

            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.HelpControlId).center);
            Assert.That(host.ActiveBoat1HelpView.IsVisible, Is.True);
            yield return Click(host.ActiveBoat1HelpView.GetControlScreenBounds(Boat1HelpView.BackControlId).center);
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Pause));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.HomeControlId).center);
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Leave));
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(LeaveConfirmationView.StayControlId).center);
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Pause));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Help));
            Assert.That(host.Boat1InputReady, Is.True);
        }

        [UnityTest]
        public IEnumerator InterruptedHelpPauseHelpBackKeepsCallerLockedAndMotionLive()
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result);
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.HelpControlId).center);
            host.SendMessage("OnApplicationPause", true);
            yield return null;

            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.HelpControlId).center);
            yield return Click(host.ActiveBoat1HelpView.GetControlScreenBounds(Boat1HelpView.BackControlId).center);
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.ActiveBoat1HelpView.BackInputEnabled, Is.False);
            Assert.That(host.EnabledSemanticControlsOverlap, Is.False);

            var reducedBefore = host.ActivePauseOverlayView.Snapshot.Settings.ReducedMotion;
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.MotionControlId).center);
            Assert.That(host.ActivePauseOverlayView.Snapshot.Settings.ReducedMotion,
                Is.Not.EqualTo(reducedBefore));
            Assert.That(host.EnabledSemanticControlsOverlap, Is.False);

            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.HomeControlId).center);
            Assert.That(host.EnabledSemanticControlsOverlap, Is.False);
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(LeaveConfirmationView.StayControlId).center);
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.ActiveBoat1HelpView.BackInputEnabled, Is.False);
            Assert.That(host.EnabledSemanticControlsOverlap, Is.False);

            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Help));
            Assert.That(host.ActiveBoat1HelpView.IsVisible, Is.True);
            Assert.That(host.Boat1InputReady, Is.True);
            Assert.That(host.ActiveBoat1HelpView.BackInputEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator InterruptedDirectLeaveKeepsPauseHelpAndHomeMapped()
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result);
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.HomeControlId).center);
            host.HandleAudioRouteChangeForTests();
            yield return null;

            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.HelpControlId).center);
            Assert.That(host.ActiveBoat1HelpView.IsVisible, Is.True);
            yield return Click(host.ActiveBoat1HelpView.GetControlScreenBounds(Boat1HelpView.BackControlId).center);
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.ActiveBoat1HelpView.BackInputEnabled, Is.False);
            Assert.That(host.EnabledSemanticControlsOverlap, Is.False);
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.HomeControlId).center);
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Hidden));
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Leaving));
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            Assert.That(host.Boat1InputReady, Is.True);
            Assert.That(host.EnabledSemanticControlsOverlap, Is.False);
        }

        [UnityTest]
        public IEnumerator SameFramePrimaryCommandsWinOnce()
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result);
            var before = host.Boat1AcceptedCommandCount;
            yield return MultiTouch(
                host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.PauseControlId).center,
                host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.HomeControlId).center);
            Assert.That(host.Boat1AcceptedCommandCount, Is.EqualTo(before + 1));
            Assert.That(host.LastAcceptedControlId, Is.EqualTo(Boat1StageView.PauseControlId));
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Pause));

            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            before = host.Boat1AcceptedCommandCount;
            yield return MultiTouch(
                host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.SkipControlId).center,
                host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.RepeatControlId).center);
            Assert.That(host.Boat1AcceptedCommandCount, Is.EqualTo(before + 1));
            Assert.That(host.LastAcceptedControlId, Is.EqualTo(Boat1StageView.SkipControlId));
            var deadline = Time.realtimeSinceStartup + 2f;
            while (host.ActiveNavigationDestination != RouteId.Stage1
                && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Stage1));
        }

        private IEnumerator EnterBoat1(System.Action<ScreenHost> capture, bool reducedMotion = false)
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();
            host.ConfigureWelcomeEvidenceForDiagnostics(
                soundEnabled: true, captionsEnabled: true, reducedMotion: reducedMotion);
            host.ConfigureAvatarInitialLeftForTests(AvatarChoice.Girl);
            yield return Click(host.ActiveWelcomeView.GetControlScreenBounds(WelcomeStageView.PlayControlId).center);
            yield return null;
            yield return Click(host.ActiveAvatarSelectView.GetControlScreenBounds(AvatarSelectView.GirlControlId).center);
            yield return null;
            yield return Click(host.ActiveAvatarPreviewView.GetControlScreenBounds(AvatarPreviewView.ConfirmControlId).center);
            yield return null;
            yield return Click(host.ActiveJourneyOverviewView.GetControlScreenBounds(JourneyOverviewStageView.SkipControlId).center);
            yield return null;
            Assert.That(host.Boat1InputReady, Is.True);
            capture(host);
        }

        private IEnumerator InterruptionDuringDirectHelp(Boat1PauseReason reason)
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result);
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.HelpControlId).center);
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Help));
            Assert.That(host.ActiveBoat1HelpView.IsVisible, Is.True);

            RaiseInterruption(host, reason);
            yield return null;
            AssertMappedPause(host, reason, Boat1State.Help);
            host.HandleFocusRestoredForTests();
            yield return null;
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);

            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Hidden));
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Help));
            Assert.That(host.ActiveBoat1HelpView.IsVisible, Is.True);
            Assert.That(host.Boat1InputReady, Is.True);
            yield return Click(host.ActiveBoat1HelpView.GetControlScreenBounds(Boat1HelpView.BackControlId).center);
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Choice));
        }

        private IEnumerator InterruptionDuringDirectLeave(Boat1PauseReason reason)
        {
            var host = default(ScreenHost);
            yield return EnterBoat1(result => host = result);
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.HomeControlId).center);
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Leaving));
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);

            RaiseInterruption(host, reason);
            yield return null;
            AssertMappedPause(host, reason, Boat1State.Leaving);
            host.HandleFocusRestoredForTests();
            yield return null;
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);

            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Hidden));
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Leaving));
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            Assert.That(host.Boat1InputReady, Is.True);
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(LeaveConfirmationView.StayControlId).center);
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Choice));
        }

        private static void RaiseInterruption(ScreenHost host, Boat1PauseReason reason)
        {
            if (reason == Boat1PauseReason.AppBackground) host.SendMessage("OnApplicationPause", true);
            else host.HandleAudioRouteChangeForTests();
        }

        private static void AssertMappedPause(ScreenHost host, Boat1PauseReason reason, Boat1State caller)
        {
            Assert.That(host.Boat1PauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.ActiveBoat1View.LastPauseReason, Is.EqualTo(reason));
            Assert.That(host.Boat1PauseCallerState, Is.EqualTo(caller));
            Assert.That(host.RecoveryVisible, Is.False);
            Assert.That(host.Boat1RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Hidden));
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
            var time = currentTime + 0.30d;
            Set(_mouse.position, position, time);
            Press(_mouse.leftButton, time + 0.01d);
            Release(_mouse.leftButton, time + 0.06d);
            currentTime = time + 0.07d;
            yield return null;
        }

        private IEnumerator Touch(Vector2 position, Touchscreen touch)
        {
            currentTime += 0.30d;
            BeginTouch(1, position, screen: touch);
            EndTouch(1, position, screen: touch);
            yield return null;
        }

        private IEnumerator PenClick(Vector2 position, Pen pen)
        {
            currentTime += 0.30d;
            Set(pen.position, position, currentTime);
            Press(pen.tip, currentTime + 0.01d);
            Release(pen.tip, currentTime + 0.06d);
            yield return null;
        }

        private IEnumerator MultiTouch(Vector2 primary, Vector2 secondary)
        {
            var touchscreen = InputSystem.AddDevice<Touchscreen>();
            currentTime += 0.30d;
            BeginTouch(1, primary, screen: touchscreen);
            BeginTouch(2, secondary, screen: touchscreen);
            EndTouch(2, secondary, screen: touchscreen);
            EndTouch(1, primary, screen: touchscreen);
            yield return null;
        }
    }
}
