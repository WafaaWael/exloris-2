using System;
using System.Collections;
using Exloris.Core.Navigation;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Bootstrap;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Recovery;
using Exloris.Runtime.Stage1;
using Exloris.Runtime.UI;
using Exloris.Runtime.Welcome;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Exloris.Tests.PlayMode
{
    public sealed class Boat2EntryIntegrationTests : InputTestFixture
    {
        private Mouse _mouse;
        public override void Setup() { base.Setup(); _mouse = InputSystem.AddDevice<Mouse>(); }
        public override void TearDown() { if (PersistentCompositionRoot.Instance != null) Object.DestroyImmediate(PersistentCompositionRoot.Instance.gameObject); base.TearDown(); }

        [UnityTest]
        public IEnumerator Boat2CreatesOneStage2EntryTransactionBeforeStage2InputIsEnabled()
        {
            var host = default(ScreenHost);
            yield return EnterBoat2(result => host = result);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Boat2));
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Boat2));
            Assert.That(host.ActiveStage1View, Is.Null);
            Assert.That(host.ActiveBoat2View, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<Boat2StageView>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(host.Boat2Snapshot.State, Is.EqualTo(Boat2State.Choice));

            Assert.That(host.PresentBoat2BoundaryForDiagnostics(), Is.True);
            Assert.That(host.Boat2Snapshot.State, Is.EqualTo(Boat2State.Arriving));
            Assert.That(host.CompleteBoat2ArrivalForDiagnostics(), Is.True);
            var retained = host.Boat2Snapshot;
            var pending = host.ActiveNavigationId;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Boat2));
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Boat2));
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Stage2));
            Assert.That(host.Boat2BoundaryLoading, Is.True);
            Assert.That(host.Boat2Snapshot.State, Is.EqualTo(Boat2State.Navigating));
            Assert.That(host.PendingStage2EntryTransactionId, Is.EqualTo(pending));
            Assert.That(host.Stage2InputReady, Is.False);
            Assert.That(Object.FindObjectsByType<ScreenHost>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(host.CompleteBoat2ArrivalForDiagnostics(), Is.False);
            Assert.That(host.ActiveNavigationId, Is.EqualTo(pending));
            Assert.That(host.Boat2Snapshot, Is.EqualTo(retained));
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!host.Stage2InputReady && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Stage2));
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Stage2));
            Assert.That(host.Stage2InputReady, Is.True);
        }

        [UnityTest]
        public IEnumerator HelpPauseLeaveAndAudioInterruptionRestoreExactBoat2Snapshot()
        {
            var host = default(ScreenHost);
            yield return EnterBoat2(result => host = result);
            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.FieldBankControlId).center);
            var traveling = host.Boat2Snapshot;

            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.HelpControlId).center);
            Assert.That(host.ActiveBoat2HelpView.IsVisible, Is.True);
            var helpCaller = host.Boat2HelpCallerSnapshot;
            yield return Click(host.ActiveBoat2HelpView.GetControlScreenBounds(Boat2HelpView.BackControlId).center);
            Assert.That(host.Boat2LastHelpRestoreSnapshot, Is.EqualTo(helpCaller));
            Assert.That(host.Boat2Snapshot.State, Is.EqualTo(helpCaller.State));
            Assert.That(host.Boat2Snapshot.Choice, Is.EqualTo(helpCaller.Choice));
            Assert.That(host.Boat2Snapshot.TravelElapsedSeconds, Is.GreaterThanOrEqualTo(helpCaller.TravelElapsedSeconds));

            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.PauseControlId).center);
            Assert.That(host.Boat2PauseState, Is.EqualTo(Exloris.Runtime.Recovery.PauseLayerState.Pause));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(Exloris.Runtime.Recovery.PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Boat2Snapshot.Choice, Is.EqualTo(traveling.Choice));

            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.HomeControlId).center);
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(Exloris.Runtime.Recovery.LeaveConfirmationView.CancelControlId).center);
            Assert.That(host.Boat2Snapshot.Choice, Is.EqualTo(traveling.Choice));

            host.HandleAudioRouteChangeForTests(); yield return null;
            Assert.That(host.Boat2PauseState, Is.EqualTo(Exloris.Runtime.Recovery.PauseLayerState.Pause));
            Assert.That(host.Boat2LastPauseReason, Is.EqualTo(Boat2PauseReason.AudioFocusLoss));
        }

        [UnityTest]
        public IEnumerator Stage2TimeoutRetryUsesFreshTransactionAndRetainsBoat2Snapshot()
        {
            var host = default(ScreenHost);
            yield return EnterBoat2(result => host = result);
            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.DockControlId).center);
            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.SkipControlId).center);
            Assert.That(host.CompleteBoat2ArrivalForDiagnostics(), Is.True);
            var retained = host.Boat2Snapshot;
            var failedId = host.ActiveNavigationId.Value;

            host.ForceNavigationTimeoutForTests();
            Assert.That(host.Boat2RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.Boat2Snapshot.State, Is.EqualTo(Boat2State.Recovery));
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(
                LeaveConfirmationView.RetryControlId).center);

            var retryId = host.ActiveNavigationId.Value;
            Assert.That(retryId, Is.Not.EqualTo(failedId));
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Stage2));
            Assert.That(host.Boat2Snapshot.State, Is.EqualTo(retained.State));
            Assert.That(host.Boat2Snapshot.Choice, Is.EqualTo(retained.Choice));
            Assert.That(host.Boat2Snapshot.TravelElapsedSeconds, Is.EqualTo(retained.TravelElapsedSeconds));
            Assert.That(host.TryCompleteNavigation(failedId), Is.False);
            Assert.That(host.ActiveNavigationId, Is.EqualTo(retryId));
        }

        [UnityTest]
        public IEnumerator PauseHelpNestedLeaveAndResumeRestoreTravelingCaller()
        {
            var host = default(ScreenHost);
            yield return EnterBoat2(result => host = result);
            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.FieldBankControlId).center);
            var caller = host.Boat2Snapshot;

            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.PauseControlId).center);
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.HelpControlId).center);
            Assert.That(host.ActiveBoat2HelpView.IsVisible, Is.True);
            yield return Click(host.ActiveBoat2HelpView.GetControlScreenBounds(Boat2HelpView.BackControlId).center);
            Assert.That(host.Boat2PauseState, Is.EqualTo(PauseLayerState.Pause));

            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.HomeControlId).center);
            Assert.That(host.Boat2PauseState, Is.EqualTo(PauseLayerState.Leave));
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(LeaveConfirmationView.StayControlId).center);
            Assert.That(host.Boat2PauseState, Is.EqualTo(PauseLayerState.Pause));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Boat2Snapshot.State, Is.EqualTo(caller.State));
            Assert.That(host.Boat2Snapshot.Choice, Is.EqualTo(caller.Choice));
            Assert.That(host.Boat2InputReady, Is.True);
        }

        [UnityTest]
        public IEnumerator DirectHelpAndLeaveInterruptionsRestoreTheirExactLayers()
        {
            var host = default(ScreenHost);
            yield return EnterBoat2(result => host = result);

            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.HelpControlId).center);
            host.HandleAudioRouteChangeForTests();
            yield return null;
            Assert.That(host.Boat2PauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.Boat2LastPauseReason, Is.EqualTo(Boat2PauseReason.AudioFocusLoss));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Boat2Snapshot.State, Is.EqualTo(Boat2State.Help));
            Assert.That(host.ActiveBoat2HelpView.IsVisible, Is.True);
            yield return Click(host.ActiveBoat2HelpView.GetControlScreenBounds(Boat2HelpView.BackControlId).center);

            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.HomeControlId).center);
            host.SendMessage("OnApplicationPause", true);
            yield return null;
            Assert.That(host.Boat2PauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.Boat2LastPauseReason, Is.EqualTo(Boat2PauseReason.AppBackground));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Boat2Snapshot.State, Is.EqualTo(Boat2State.Leaving));
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            Assert.That(host.EnabledSemanticControlsOverlap, Is.False);
        }

        [UnityTest]
        public IEnumerator Stage2BoundaryLayoutFailureRetainsChoiceAndHomeReturnsToWelcome()
        {
            var host = default(ScreenHost);
            yield return EnterBoat2(result => host = result);
            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.DockControlId).center);
            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.SkipControlId).center);
            Assert.That(host.CompleteBoat2ArrivalForDiagnostics(), Is.True);
            var retained = host.Boat2Snapshot;

            Assert.That(host.TryApplyBoat2Layout(360f, 182f, 360f, 182f, 429f, 144), Is.False);
            Assert.That(host.Boat2RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.Boat2Snapshot.Choice, Is.EqualTo(retained.Choice));
            Assert.That(host.Boat2Snapshot.TravelElapsedSeconds, Is.EqualTo(retained.TravelElapsedSeconds));
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(
                LeaveConfirmationView.HomeControlId).center);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.SessionOpen, Is.True);
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Boat2));
            Assert.That(host.ActiveBoat2View, Is.Null);
        }

        [UnityTest]
        public IEnumerator Stage2BoundaryAssetFailureUsesTheSameRecoverySurface()
        {
            var host = default(ScreenHost);
            yield return EnterBoat2(result => host = result);
            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.FieldBankControlId).center);
            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.SkipControlId).center);
            Assert.That(host.CompleteBoat2ArrivalForDiagnostics(), Is.True);
            var retained = host.Boat2Snapshot;

            Assert.That(host.ReportBoat2BoundaryAssetFailure(), Is.True);
            Assert.That(host.Boat2RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.Boat2Snapshot.Choice, Is.EqualTo(retained.Choice));
            Assert.That(host.Boat2Snapshot.TravelElapsedSeconds, Is.EqualTo(retained.TravelElapsedSeconds));
        }

        private IEnumerator EnterBoat2(Action<ScreenHost> capture)
        {
            var host = default(ScreenHost);
            yield return EnterStage1(result => host = result);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.PlayControlId).center);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.MarkerControlId).center);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.SocketControlId).center);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.ConfirmControlId).center);
            Assert.That(host.CompleteStage1OutcomeForDiagnostics(), Is.True);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.ContinueControlId).center);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Stage1));
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Boat2));
            Assert.That(host.PendingBoat2EntryTransactionId, Is.EqualTo(host.ActiveNavigationId));
            var deadline = Time.realtimeSinceStartup + 3f;
            while ((!host.Boat2InputReady || host.CurrentRoute != RouteId.Boat2) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(host.Boat2InputReady, Is.True);
            capture(host);
        }

        private IEnumerator EnterStage1(Action<ScreenHost> capture)
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>(); host.ConfigureAvatarInitialLeftForTests(AvatarChoice.Girl);
            yield return Click(host.ActiveWelcomeView.GetControlScreenBounds(WelcomeStageView.PlayControlId).center); yield return null;
            yield return Click(host.ActiveAvatarSelectView.GetControlScreenBounds(AvatarSelectView.GirlControlId).center); yield return null;
            yield return Click(host.ActiveAvatarPreviewView.GetControlScreenBounds(AvatarPreviewView.ConfirmControlId).center); yield return null;
            yield return Click(host.ActiveJourneyOverviewView.GetControlScreenBounds(JourneyOverviewStageView.SkipControlId).center); yield return null;
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.SkipControlId).center);
            Assert.That(host.CompleteBoat1ArrivalForDiagnostics(), Is.True);
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!host.Stage1InputReady && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Stage1)); Assert.That(host.Stage1InputReady, Is.True); capture(host);
        }

        private static IEnumerator LoadFreshWelcome()
        {
            if (!Application.isBatchMode) Screen.SetResolution(1280,720,false); yield return null;
            if (PersistentCompositionRoot.Instance != null) { Object.Destroy(PersistentCompositionRoot.Instance.gameObject); yield return null; }
            SceneManager.LoadScene(0,LoadSceneMode.Single); yield return null;
            var host = Object.FindFirstObjectByType<ScreenHost>(); var deadline = Time.realtimeSinceStartup + 8f;
            while (host.CurrentRoute == RouteId.StartupLoading && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(host.SkipIntroForTests(), Is.True); yield return null; yield return null;
        }

        private IEnumerator Click(Vector2 position)
        {
            var time=currentTime+0.30d; Set(_mouse.position,position,time); Press(_mouse.leftButton,time+0.01d); Release(_mouse.leftButton,time+0.06d); currentTime=time+0.07d; yield return null;
        }
    }
}
