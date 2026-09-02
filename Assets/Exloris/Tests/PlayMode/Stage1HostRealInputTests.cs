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

namespace Exloris.Tests.PlayMode
{
    public sealed class Stage1HostRealInputTests : InputTestFixture
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
        public IEnumerator MouseCompletesStage1AndStopsAtOneBoat2Boundary()
        {
            var host = default(ScreenHost);
            yield return EnterStage1(result => host = result);

            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.PlayControlId).center);
            Assert.That(host.Stage1Snapshot.State, Is.EqualTo(Stage1State.Playing));
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.UpControlId).center);
            Assert.That(host.Stage1Snapshot.MarkerAttached, Is.False);
            Assert.That(host.ActiveStage1View.LastMediaCue.Mode, Is.EqualTo(Stage1MediaMode.Attach));

            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.MarkerControlId).center);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.SocketControlId).center);
            Assert.That(host.Stage1Snapshot.MarkerAttached, Is.True);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.UpControlId).center);
            Assert.That(host.Stage1Snapshot.MarkerPosition, Is.EqualTo(0.75d));
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.ConfirmControlId).center);
            Assert.That(host.Stage1Snapshot.State, Is.EqualTo(Stage1State.OutcomePlaying));
            Assert.That(host.ActiveStage1View.LastMediaCue.CaptionVisible, Is.False);

            Assert.That(host.CompleteStage1OutcomeForDiagnostics(), Is.True);
            Assert.That(host.Stage1Snapshot.State, Is.EqualTo(Stage1State.OutcomeReady));
            Assert.That(host.Stage1Snapshot.Outcome, Is.EqualTo(Stage1OutcomeKind.InRange));
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.WatchAgainControlId).center);
            Assert.That(host.Stage1Snapshot.State, Is.EqualTo(Stage1State.OutcomePlaying));
            Assert.That(host.CompleteStage1OutcomeForDiagnostics(), Is.True);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.ContinueControlId).center);

            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Stage1));
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Stage1));
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Boat2));
            Assert.That(host.Stage1BoundaryLoading, Is.True);
            Assert.That(host.Stage1InputReady, Is.False);
            Assert.That(host.Stage1Snapshot.CompletionRecorded, Is.True);
        }

        [UnityTest]
        public IEnumerator DragHelpPauseLeaveAndInterruptionRestoreExactStage1State()
        {
            var host = default(ScreenHost);
            yield return EnterStage1(result => host = result, reducedMotion: true);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.PlayControlId).center);

            yield return Drag(
                host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.MarkerControlId).center,
                host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.SocketControlId).center);
            Assert.That(host.Stage1Snapshot.MarkerAttached, Is.True);
            yield return Drag(
                host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.MarkerControlId).center,
                host.ActiveStage1View.GetMarkerScreenPoint(0.1d));
            Assert.That(host.Stage1Snapshot.MarkerPosition, Is.EqualTo(0.1d).Within(0.02d));
            var playing = host.Stage1Snapshot;

            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.HelpControlId).center);
            Assert.That(host.ActiveStage1HelpView.IsVisible, Is.True);
            yield return Click(host.ActiveStage1HelpView.GetControlScreenBounds(Stage1HelpView.BackControlId).center);
            Assert.That(host.Stage1Snapshot, Is.EqualTo(playing));

            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.PauseControlId).center);
            Assert.That(host.Stage1PauseState, Is.EqualTo(PauseLayerState.Pause));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.HomeControlId).center);
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(LeaveConfirmationView.StayControlId).center);
            Assert.That(host.Stage1PauseState, Is.EqualTo(PauseLayerState.Pause));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Stage1Snapshot.MarkerPosition, Is.EqualTo(playing.MarkerPosition));

            host.HandleAudioRouteChangeForTests();
            yield return null;
            Assert.That(host.Stage1PauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.Stage1LastPauseReason, Is.EqualTo(Stage1PauseReason.AudioFocusLoss));
            host.HandleFocusRestoredForTests();
            yield return null;
            Assert.That(host.Stage1PauseState, Is.EqualTo(PauseLayerState.Pause));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Stage1Snapshot.MarkerPosition, Is.EqualTo(playing.MarkerPosition));

            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.HomeControlId).center);
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(LeaveConfirmationView.CancelControlId).center);
            Assert.That(host.Stage1Snapshot.MarkerPosition, Is.EqualTo(playing.MarkerPosition));
            Assert.That(host.ReducedMotionEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator TouchAndPenUseTheSameSemanticMarkerPathOnFreshRoutes()
        {
            var host = default(ScreenHost);
            yield return EnterStage1(result => host = result);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.PlayControlId).center);
            var touch = InputSystem.AddDevice<Touchscreen>();
            yield return Touch(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.MarkerControlId).center, touch);
            yield return Touch(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.SocketControlId).center, touch);
            Assert.That(host.Stage1Snapshot.MarkerAttached, Is.True);

            Object.Destroy(PersistentCompositionRoot.Instance.gameObject);
            yield return null;
            yield return EnterStage1(result => host = result);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.PlayControlId).center);
            var pen = InputSystem.AddDevice<Pen>();
            yield return PenClick(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.MarkerControlId).center, pen);
            yield return PenClick(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.SocketControlId).center, pen);
            Assert.That(host.Stage1Snapshot.MarkerAttached, Is.True);
        }

        private IEnumerator EnterStage1(System.Action<ScreenHost> capture, bool reducedMotion = false)
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();
            host.ConfigureWelcomeEvidenceForDiagnostics(true, true, reducedMotion);
            host.ConfigureAvatarInitialLeftForTests(AvatarChoice.Girl);
            yield return Click(host.ActiveWelcomeView.GetControlScreenBounds(WelcomeStageView.PlayControlId).center);
            yield return null;
            yield return Click(host.ActiveAvatarSelectView.GetControlScreenBounds(AvatarSelectView.GirlControlId).center);
            yield return null;
            yield return Click(host.ActiveAvatarPreviewView.GetControlScreenBounds(AvatarPreviewView.ConfirmControlId).center);
            yield return null;
            yield return Click(host.ActiveJourneyOverviewView.GetControlScreenBounds(JourneyOverviewStageView.SkipControlId).center);
            yield return null;
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.SkipControlId).center);
            Assert.That(host.CompleteBoat1ArrivalForDiagnostics(), Is.True);
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Stage1), host.Stage1PreparationFailure);
            Assert.That(host.Stage1Prepared, Is.True, host.Stage1PreparationFailure);
            Assert.That(host.PendingStage1EntryTransactionId, Is.EqualTo(host.ActiveNavigationId));
            var deadline = Time.realtimeSinceStartup + 3f;
            while ((!host.Stage1InputReady || host.CurrentRoute != RouteId.Stage1)
                && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Stage1));
            Assert.That(host.Stage1InputReady, Is.True);
            capture(host);
        }

        private static IEnumerator LoadFreshWelcome()
        {
            if (!Application.isBatchMode) Screen.SetResolution(1280, 720, false);
            yield return null;
            if (PersistentCompositionRoot.Instance != null)
            {
                Object.Destroy(PersistentCompositionRoot.Instance.gameObject);
                yield return null;
            }
            SceneManager.LoadScene(0, LoadSceneMode.Single);
            yield return null;
            var host = Object.FindFirstObjectByType<ScreenHost>();
            var deadline = Time.realtimeSinceStartup + 8f;
            while (host.CurrentRoute == RouteId.StartupLoading && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(host.SkipIntroForTests(), Is.True);
            yield return null;
            yield return null;
        }

        private IEnumerator Click(Vector2 position)
        {
            var time = currentTime + 0.30d;
            Set(_mouse.position, position, time); Press(_mouse.leftButton, time + 0.01d); Release(_mouse.leftButton, time + 0.06d);
            currentTime = time + 0.07d; yield return null;
        }

        private IEnumerator Drag(Vector2 start, Vector2 end)
        {
            var time = currentTime + 0.30d;
            Set(_mouse.position, start, time); Press(_mouse.leftButton, time + 0.01d);
            Set(_mouse.position, end, time + 0.05d); Release(_mouse.leftButton, time + 0.08d);
            currentTime = time + 0.09d; yield return null;
        }

        private IEnumerator Touch(Vector2 position, Touchscreen screen)
        {
            currentTime += 0.30d; BeginTouch(1, position, screen: screen); EndTouch(1, position, screen: screen); yield return null;
        }

        private IEnumerator PenClick(Vector2 position, Pen pen)
        {
            currentTime += 0.30d; Set(pen.position, position, currentTime); Press(pen.tip, currentTime + 0.01d); Release(pen.tip, currentTime + 0.06d); yield return null;
        }
    }
}
