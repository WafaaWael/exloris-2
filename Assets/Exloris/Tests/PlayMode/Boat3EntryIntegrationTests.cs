using System;
using System.Collections;
using Exloris.Core.Navigation;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Bootstrap;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Recovery;
using Exloris.Runtime.Stage1;
using Exloris.Runtime.Stage2;
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
    public sealed class Boat3EntryIntegrationTests : InputTestFixture
    {
        private Mouse _mouse;
        public override void Setup() { base.Setup(); _mouse = InputSystem.AddDevice<Mouse>(); }
        public override void TearDown() { if (PersistentCompositionRoot.Instance != null) Object.DestroyImmediate(PersistentCompositionRoot.Instance.gameObject); base.TearDown(); }

        [UnityTest]
        public IEnumerator Boat3StopsAtOnePendingStage3TransactionWithOnlyAHiddenPreparedStage3()
        {
            var host = default(ScreenHost);
            yield return EnterBoat3(result => host = result);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Boat3));
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Boat3));
            Assert.That(host.ActiveStage2View, Is.Null);
            Assert.That(host.ActiveBoat3View, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<Boat3StageView>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(host.Boat3Snapshot.State, Is.EqualTo(Boat3State.Choice));

            Assert.That(host.PresentBoat3BoundaryForDiagnostics(), Is.True);
            Assert.That(host.Boat3Snapshot.State, Is.EqualTo(Boat3State.Arriving));
            Assert.That(host.CompleteBoat3ArrivalForDiagnostics(), Is.True);
            var retained = host.Boat3Snapshot;
            var pending = host.ActiveNavigationId;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Boat3));
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Boat3));
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Stage3));
            Assert.That(host.Boat3BoundaryLoading, Is.True);
            Assert.That(host.Boat3Snapshot.State, Is.EqualTo(Boat3State.Navigating));
            Assert.That(host.PendingStage3EntryTransactionId, Is.EqualTo(pending));
            Assert.That(Object.FindObjectsByType<ScreenHost>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(host.ActiveStage3View, Is.Not.Null);
            Assert.That(host.Stage3InputReady, Is.False);
            Assert.That(host.Stage3Snapshot.State, Is.EqualTo(Exloris.Runtime.Stage3.Stage3State.Hidden));
            Assert.That(host.CompleteBoat3ArrivalForDiagnostics(), Is.False);
            Assert.That(host.ActiveNavigationId, Is.EqualTo(pending));
            Assert.That(host.Boat3Snapshot, Is.EqualTo(retained));
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!host.Stage3InputReady && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Stage3));
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Stage3));
            Assert.That(host.PendingStage3EntryTransactionId, Is.Null);
            Assert.That(host.LastCompletedStage3EntryTransactionId, Is.EqualTo(pending));
            Assert.That(host.ActiveBoat3View, Is.Null);
            Assert.That(host.Stage3InputReady, Is.True);
        }

        [UnityTest]
        public IEnumerator HelpPauseLeaveAndAudioInterruptionRestoreExactBoat3Snapshot()
        {
            var host = default(ScreenHost);
            yield return EnterBoat3(result => host = result);
            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.GrainStoreControlId).center);
            var traveling = host.Boat3Snapshot;

            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.HelpControlId).center);
            Assert.That(host.ActiveBoat3HelpView.IsVisible, Is.True);
            var helpCaller = host.Boat3HelpCallerSnapshot;
            yield return Click(host.ActiveBoat3HelpView.GetControlScreenBounds(Boat3HelpView.BackControlId).center);
            Assert.That(host.Boat3LastHelpRestoreSnapshot, Is.EqualTo(helpCaller));
            Assert.That(host.Boat3Snapshot.State, Is.EqualTo(helpCaller.State));
            Assert.That(host.Boat3Snapshot.Choice, Is.EqualTo(helpCaller.Choice));
            Assert.That(host.Boat3Snapshot.TravelElapsedSeconds, Is.GreaterThanOrEqualTo(helpCaller.TravelElapsedSeconds));

            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.PauseControlId).center);
            Assert.That(host.Boat3PauseState, Is.EqualTo(Exloris.Runtime.Recovery.PauseLayerState.Pause));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(Exloris.Runtime.Recovery.PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Boat3Snapshot.Choice, Is.EqualTo(traveling.Choice));

            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.HomeControlId).center);
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(Exloris.Runtime.Recovery.LeaveConfirmationView.CancelControlId).center);
            Assert.That(host.Boat3Snapshot.Choice, Is.EqualTo(traveling.Choice));

            host.HandleAudioRouteChangeForTests(); yield return null;
            Assert.That(host.Boat3PauseState, Is.EqualTo(Exloris.Runtime.Recovery.PauseLayerState.Pause));
            Assert.That(host.Boat3LastPauseReason, Is.EqualTo(Boat3PauseReason.AudioFocusLoss));
        }

        [UnityTest]
        public IEnumerator Stage3TimeoutRetryUsesFreshTransactionAndRetainsBoat3Snapshot()
        {
            var host = default(ScreenHost);
            yield return EnterBoat3(result => host = result);
            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.IrrigationControlId).center);
            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.SkipControlId).center);
            Assert.That(host.CompleteBoat3ArrivalForDiagnostics(), Is.True);
            var retained = host.Boat3Snapshot;
            var failedId = host.ActiveNavigationId.Value;

            host.ForceNavigationTimeoutForTests();
            Assert.That(host.Boat3RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.Boat3Snapshot.State, Is.EqualTo(Boat3State.Recovery));
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(
                LeaveConfirmationView.RetryControlId).center);

            var retryId = host.ActiveNavigationId.Value;
            Assert.That(retryId, Is.Not.EqualTo(failedId));
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Stage3));
            Assert.That(host.Boat3Snapshot.State, Is.EqualTo(retained.State));
            Assert.That(host.Boat3Snapshot.Choice, Is.EqualTo(retained.Choice));
            Assert.That(host.Boat3Snapshot.TravelElapsedSeconds, Is.EqualTo(retained.TravelElapsedSeconds));
            Assert.That(host.TryCompleteNavigation(failedId), Is.False);
            Assert.That(host.ActiveNavigationId, Is.EqualTo(retryId));
        }

        [UnityTest]
        public IEnumerator PauseHelpNestedLeaveAndResumeRestoreTravelingCaller()
        {
            var host = default(ScreenHost);
            yield return EnterBoat3(result => host = result);
            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.GrainStoreControlId).center);
            var caller = host.Boat3Snapshot;

            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.PauseControlId).center);
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.HelpControlId).center);
            Assert.That(host.ActiveBoat3HelpView.IsVisible, Is.True);
            yield return Click(host.ActiveBoat3HelpView.GetControlScreenBounds(Boat3HelpView.BackControlId).center);
            Assert.That(host.Boat3PauseState, Is.EqualTo(PauseLayerState.Pause));

            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.HomeControlId).center);
            Assert.That(host.Boat3PauseState, Is.EqualTo(PauseLayerState.Leave));
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(LeaveConfirmationView.StayControlId).center);
            Assert.That(host.Boat3PauseState, Is.EqualTo(PauseLayerState.Pause));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Boat3Snapshot.State, Is.EqualTo(caller.State));
            Assert.That(host.Boat3Snapshot.Choice, Is.EqualTo(caller.Choice));
            Assert.That(host.Boat3InputReady, Is.True);
        }

        [UnityTest]
        public IEnumerator DirectHelpAndLeaveInterruptionsRestoreTheirExactLayers()
        {
            var host = default(ScreenHost);
            yield return EnterBoat3(result => host = result);

            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.HelpControlId).center);
            host.HandleAudioRouteChangeForTests();
            yield return null;
            Assert.That(host.Boat3PauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.Boat3LastPauseReason, Is.EqualTo(Boat3PauseReason.AudioFocusLoss));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Boat3Snapshot.State, Is.EqualTo(Boat3State.Help));
            Assert.That(host.ActiveBoat3HelpView.IsVisible, Is.True);
            yield return Click(host.ActiveBoat3HelpView.GetControlScreenBounds(Boat3HelpView.BackControlId).center);

            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.HomeControlId).center);
            host.SendMessage("OnApplicationPause", true);
            yield return null;
            Assert.That(host.Boat3PauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.Boat3LastPauseReason, Is.EqualTo(Boat3PauseReason.AppBackground));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Boat3Snapshot.State, Is.EqualTo(Boat3State.Leaving));
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            Assert.That(host.EnabledSemanticControlsOverlap, Is.False);
        }

        [UnityTest]
        public IEnumerator Stage3BoundaryLayoutFailureRetainsChoiceAndHomeReturnsToWelcome()
        {
            var host = default(ScreenHost);
            yield return EnterBoat3(result => host = result);
            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.IrrigationControlId).center);
            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.SkipControlId).center);
            Assert.That(host.CompleteBoat3ArrivalForDiagnostics(), Is.True);
            var retained = host.Boat3Snapshot;

            Assert.That(host.TryApplyBoat3Layout(360f, 182f, 360f, 182f, 429f, 144), Is.False);
            Assert.That(host.Boat3RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.Boat3Snapshot.Choice, Is.EqualTo(retained.Choice));
            Assert.That(host.Boat3Snapshot.TravelElapsedSeconds, Is.EqualTo(retained.TravelElapsedSeconds));
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(
                LeaveConfirmationView.HomeControlId).center);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.SessionOpen, Is.True);
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Boat3));
            Assert.That(host.ActiveBoat3View, Is.Null);
        }

        [UnityTest]
        public IEnumerator Stage3BoundaryAssetFailureUsesTheSameRecoverySurface()
        {
            var host = default(ScreenHost);
            yield return EnterBoat3(result => host = result);
            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.GrainStoreControlId).center);
            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.SkipControlId).center);
            Assert.That(host.CompleteBoat3ArrivalForDiagnostics(), Is.True);
            var retained = host.Boat3Snapshot;

            Assert.That(host.ReportBoat3BoundaryAssetFailure(), Is.True);
            Assert.That(host.Boat3RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.Boat3Snapshot.Choice, Is.EqualTo(retained.Choice));
            Assert.That(host.Boat3Snapshot.TravelElapsedSeconds, Is.EqualTo(retained.TravelElapsedSeconds));
        }

        private IEnumerator EnterBoat3(Action<ScreenHost> capture)
        {
            var host = default(ScreenHost);
            yield return EnterStage2(result => host = result);
            Assert.That(host.PresentStage2PlayForDiagnostics(), Is.True);
            Assert.That(host.PresentStage2RiverForDiagnostics(Stage2River.High), Is.True);
            foreach (var hotspot in new[] { Stage2Hotspot.Field, Stage2Hotspot.Path, Stage2Hotspot.Boat })
            {
                Assert.That(host.PresentStage2HotspotForDiagnostics(hotspot), Is.True);
                Assert.That(host.CompleteStage2ConsequenceForDiagnostics(), Is.True);
            }
            Assert.That(host.PresentStage2FinishForDiagnostics(), Is.True);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Stage2));
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Boat3));
            Assert.That(host.PendingBoat3EntryTransactionId, Is.EqualTo(host.ActiveNavigationId));
            var deadline = Time.realtimeSinceStartup + 3f;
            while ((!host.Boat3InputReady || host.CurrentRoute != RouteId.Boat3) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(host.Boat3InputReady, Is.True);
            capture(host);
        }

        private IEnumerator EnterStage2(Action<ScreenHost> capture)
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
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.PlayControlId).center);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.MarkerControlId).center);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.SocketControlId).center);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.ConfirmControlId).center);
            Assert.That(host.CompleteStage1OutcomeForDiagnostics(), Is.True);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.ContinueControlId).center);
            deadline = Time.realtimeSinceStartup + 3f;
            while (!host.Boat2InputReady && Time.realtimeSinceStartup < deadline) yield return null;
            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.DockControlId).center);
            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.SkipControlId).center);
            Assert.That(host.CompleteBoat2ArrivalForDiagnostics(), Is.True);
            deadline = Time.realtimeSinceStartup + 3f;
            while (!host.Stage2InputReady && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Stage2));
            Assert.That(host.Stage2InputReady, Is.True); capture(host);
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
