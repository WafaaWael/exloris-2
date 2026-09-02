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
    public sealed class Stage2RecoveryTests : InputTestFixture
    {
        private Mouse _mouse;
        public override void Setup() { base.Setup(); _mouse = InputSystem.AddDevice<Mouse>(); }
        public override void TearDown() { if (PersistentCompositionRoot.Instance != null) Object.DestroyImmediate(PersistentCompositionRoot.Instance.gameObject); base.TearDown(); }

        [UnityTest]
        public IEnumerator FullRouteReachesAllThreeCurrentRiverOutcomesThenOnePreparedBoat3Boundary()
        {
            ScreenHost host = null; yield return EnterStage2(value => host = value);
            yield return Click(host.ActiveStage2View.GetControlScreenBounds(Stage2StageView.PlayControlId).center);
            yield return Click(host.ActiveStage2View.GetControlScreenBounds(Stage2StageView.LowControlId).center);
            foreach (var id in new[] { Stage2StageView.FieldControlId, Stage2StageView.PathControlId, Stage2StageView.BoatControlId })
            {
                yield return Click(host.ActiveStage2View.GetControlScreenBounds(id).center);
                Assert.That(host.CompleteStage2ConsequenceForDiagnostics(), Is.True, id);
            }
            Assert.That(host.Stage2Snapshot.LowObserved, Is.EqualTo(Stage2ObservedHotspots.All));
            yield return Click(host.ActiveStage2View.GetControlScreenBounds(Stage2StageView.FinishControlId).center);
            var pending = host.ActiveNavigationId;
            if (host.CurrentRoute != RouteId.Stage2)
                Assert.Fail($"route={host.CurrentRoute} session={host.SessionLocation} frame={Time.frameCount} readyFrame={host.Stage2EntryReadyFrame} pending={host.PendingStage2EntryTransactionId} active={host.ActiveNavigationId} completed={host.LastCompletedStage2EntryTransactionId} stage2View={(host.ActiveStage2View != null)} boat2View={(host.ActiveBoat2View != null)}");
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Stage2));
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Boat3));
            Assert.That(host.Stage2BoundaryLoading, Is.True);
            Assert.That(host.Stage2Snapshot.State, Is.EqualTo(Stage2State.Navigating));
            Assert.That(host.PresentStage2FinishForDiagnostics(), Is.False);
            Assert.That(host.ActiveNavigationId, Is.EqualTo(pending));
            Assert.That(host.PendingBoat3EntryTransactionId, Is.EqualTo(pending));
            Assert.That(Type.GetType("Exloris.Runtime.Boat.Boat3StageHost, Exloris.Runtime"), Is.Not.Null);
            Assert.That(Type.GetType("Exloris.Runtime.Stage3.Stage3StageHost, Exloris.Runtime"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator HelpPauseNestedLeaveAndInterruptionRestoreExactStage2Caller()
        {
            ScreenHost host = null; yield return EnterStage2(value => host = value);
            yield return Click(host.ActiveStage2View.GetControlScreenBounds(Stage2StageView.PlayControlId).center);
            yield return Click(host.ActiveStage2View.GetControlScreenBounds(Stage2StageView.MiddleControlId).center);
            var caller = host.Stage2Snapshot;
            yield return Click(host.ActiveStage2View.GetControlScreenBounds(Stage2StageView.HelpControlId).center);
            Assert.That(host.ActiveStage2HelpView.IsVisible, Is.True);
            yield return Click(host.ActiveStage2HelpView.GetControlScreenBounds(Stage2HelpView.BackControlId).center);
            Assert.That(host.Stage2Snapshot.SelectedRiver, Is.EqualTo(caller.SelectedRiver));
            yield return Click(host.ActiveStage2View.GetControlScreenBounds(Stage2StageView.PauseControlId).center);
            Assert.That(host.Stage2PauseState, Is.EqualTo(PauseLayerState.Pause));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.HelpControlId).center);
            yield return Click(host.ActiveStage2HelpView.GetControlScreenBounds(Stage2HelpView.BackControlId).center);
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.HomeControlId).center);
            Assert.That(host.Stage2PauseState, Is.EqualTo(PauseLayerState.Leave));
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(LeaveConfirmationView.StayControlId).center);
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Stage2Snapshot.SelectedRiver, Is.EqualTo(caller.SelectedRiver));
            Assert.That(host.Stage2InputReady, Is.True);
            host.HandleAudioRouteChangeForTests(); yield return null;
            Assert.That(host.Stage2PauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.Stage2LastPauseReason, Is.EqualTo(Stage2PauseReason.AudioFocusLoss));
        }

        [UnityTest]
        public IEnumerator Boat3TimeoutRetryUsesFreshTransactionAndStaleCompletionIsInert()
        {
            ScreenHost host = null; yield return EnterCompletedStage2(value => host = value);
            var failed = host.ActiveNavigationId.Value; var retained = host.Stage2Snapshot;
            host.ForceNavigationTimeoutForTests();
            Assert.That(host.Stage2RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.Stage2Snapshot.State, Is.EqualTo(Stage2State.Recovery));
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(LeaveConfirmationView.RetryControlId).center);
            Assert.That(host.ActiveNavigationId.Value, Is.Not.EqualTo(failed));
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Boat3));
            Assert.That(host.Stage2Snapshot.SelectedRiver, Is.EqualTo(retained.SelectedRiver));
            Assert.That(host.TryCompleteNavigation(failed), Is.False);
        }

        [UnityTest]
        public IEnumerator Boat3AssetFailureHomeReturnsWelcomeWithoutClosingSession()
        {
            ScreenHost host = null; yield return EnterCompletedStage2(value => host = value);
            Assert.That(host.ReportStage2BoundaryAssetFailure(), Is.True);
            Assert.That(host.Stage2RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(LeaveConfirmationView.HomeControlId).center);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.SessionOpen, Is.True);
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Stage2));
            Assert.That(host.ActiveStage2View, Is.Null);
        }

        private IEnumerator EnterCompletedStage2(Action<ScreenHost> capture)
        {
            ScreenHost host = null; yield return EnterStage2(value => host = value);
            Assert.That(host.PresentStage2PlayForDiagnostics(), Is.True);
            Assert.That(host.PresentStage2RiverForDiagnostics(Stage2River.High), Is.True);
            foreach (var hotspot in new[] { Stage2Hotspot.Field, Stage2Hotspot.Path, Stage2Hotspot.Boat })
            { Assert.That(host.PresentStage2HotspotForDiagnostics(hotspot), Is.True); Assert.That(host.CompleteStage2ConsequenceForDiagnostics(), Is.True); }
            Assert.That(host.PresentStage2FinishForDiagnostics(), Is.True);
            capture(host);
        }

        private IEnumerator EnterStage2(Action<ScreenHost> capture)
        {
            ScreenHost host = null; yield return EnterBoat2(value => host = value);
            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.DockControlId).center);
            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.SkipControlId).center);
            Assert.That(host.CompleteBoat2ArrivalForDiagnostics(), Is.True);
            if (!host.ActiveNavigationId.HasValue)
                Assert.Fail($"preparation={host.Stage2PreparationFailure} recovery={host.Boat2RecoveryState}");
            Assert.That(host.PendingStage2EntryTransactionId, Is.EqualTo(host.ActiveNavigationId));
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!host.Stage2InputReady && Time.realtimeSinceStartup < deadline) yield return null;
            if (host.CurrentRoute != RouteId.Stage2)
                Assert.Fail($"route={host.CurrentRoute} session={host.SessionLocation} frame={Time.frameCount} readyFrame={host.Stage2EntryReadyFrame} pending={host.PendingStage2EntryTransactionId} active={host.ActiveNavigationId} completed={host.LastCompletedStage2EntryTransactionId} stage2View={(host.ActiveStage2View != null)} boat2View={(host.ActiveBoat2View != null)}");
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Stage2));
            Assert.That(host.Stage2InputReady, Is.True); capture(host);
        }

        private IEnumerator EnterBoat2(Action<ScreenHost> capture)
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
            Assert.That(host.Boat2InputReady, Is.True); capture(host);
        }

        private static IEnumerator LoadFreshWelcome()
        {
            if (!Application.isBatchMode) Screen.SetResolution(1280, 720, false); yield return null;
            if (PersistentCompositionRoot.Instance != null) { Object.Destroy(PersistentCompositionRoot.Instance.gameObject); yield return null; }
            SceneManager.LoadScene(0, LoadSceneMode.Single); yield return null;
            var host = Object.FindFirstObjectByType<ScreenHost>(); var deadline = Time.realtimeSinceStartup + 8f;
            while (host.CurrentRoute == RouteId.StartupLoading && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(host.SkipIntroForTests(), Is.True); yield return null; yield return null;
        }

        private IEnumerator Click(Vector2 position)
        {
            var time = currentTime + 0.30d; Set(_mouse.position, position, time);
            Press(_mouse.leftButton, time + 0.01d); Release(_mouse.leftButton, time + 0.06d);
            currentTime = time + 0.07d; yield return null;
        }
    }
}
