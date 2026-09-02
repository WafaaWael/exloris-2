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
using Exloris.Runtime.Stage3;
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
    public sealed class Stage3EntryIntegrationTests : InputTestFixture
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
        public IEnumerator FullRouteCarriesStage2RiverThroughOnePendingBoundaryIntoCompletion()
        {
            ScreenHost host = null;
            yield return EnterStage3(value => host = value, Stage2River.High);

            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Stage3));
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Stage3));
            Assert.That(host.Stage3Snapshot.SelectedRiver, Is.EqualTo(Stage2River.High));
            Assert.That(host.ActiveBoat3View, Is.Null);
            Assert.That(host.ActiveStage3View, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<Stage3StageView>(FindObjectsSortMode.None), Has.Length.EqualTo(1));

            Assert.That(host.PresentStage3PlayForDiagnostics(), Is.True);
            Assert.That(host.PresentStage3PlanForDiagnostics(Stage3Plan.Grain), Is.True);
            Assert.That(host.CompleteStage3OutcomeForDiagnostics(), Is.True);
            Assert.That(host.PresentStage3FinishForDiagnostics(), Is.True);

            var pending = host.ActiveNavigationId;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Stage3));
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Stage3));
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Completion));
            Assert.That(host.Stage3BoundaryLoading, Is.True);
            Assert.That(host.Stage3Snapshot.State, Is.EqualTo(Stage3State.Navigating));
            Assert.That(host.Stage3Snapshot.CompletionRecorded, Is.True);
            Assert.That(host.PresentStage3FinishForDiagnostics(), Is.False);
            Assert.That(host.ActiveNavigationId, Is.EqualTo(pending));
            Assert.That(host.PendingCompletionEntryTransactionId, Is.EqualTo(pending));

            yield return WaitFor(() => host.CurrentRoute == RouteId.Completion, 3f);
            Assert.That(host.ActiveNavigationId, Is.Null);
            Assert.That(host.ActiveStage3View, Is.Null);
            Assert.That(host.ActiveCompletionView, Is.Not.Null);
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Completion));
        }

        [UnityTest]
        public IEnumerator PauseHelpStackAndDirectHelpInterruptionRestoreExactUnderlyingHelp()
        {
            ScreenHost host = null;
            yield return EnterStage3(value => host = value, Stage2River.Middle);
            Assert.That(host.PresentStage3PlayForDiagnostics(), Is.True);
            var caller = host.Stage3Snapshot;

            yield return Click(host.ActiveStage3View.GetControlScreenBounds(Stage3StageView.HelpControlId).center);
            Assert.That(host.Stage3Snapshot.State, Is.EqualTo(Stage3State.Help));
            Assert.That(host.ActiveStage3HelpView.IsVisible, Is.True);
            Assert.That(host.ActiveStage3HelpView.LastMode, Is.EqualTo(Stage3HelpMode.EqualPlans));

            host.HandleAudioRouteChangeForTests();
            yield return null;
            Assert.That(host.Stage3PauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.Stage3LastPauseReason, Is.EqualTo(Stage3PauseReason.AudioFocusLoss));
            Assert.That(host.Stage3PauseCallerState, Is.EqualTo(Stage3State.Help));

            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.HelpControlId).center);
            yield return Click(host.ActiveStage3HelpView.GetControlScreenBounds(Stage3HelpView.BackControlId).center);
            Assert.That(host.Stage3PauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.ActiveStage3HelpView.IsVisible, Is.True);

            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Stage3Snapshot.State, Is.EqualTo(Stage3State.Help));
            Assert.That(host.ActiveStage3HelpView.IsVisible, Is.True);
            yield return Click(host.ActiveStage3HelpView.GetControlScreenBounds(Stage3HelpView.BackControlId).center);
            Assert.That(host.Stage3Snapshot.State, Is.EqualTo(caller.State));
            Assert.That(host.Stage3Snapshot.SelectedRiver, Is.EqualTo(caller.SelectedRiver));
        }

        [UnityTest]
        public IEnumerator RiverBackAndCallerSpecificHelpRestoreExactCompletedOutcome()
        {
            ScreenHost host = null;
            yield return EnterOutcomeReady(value => host = value);
            var completed = host.Stage3Snapshot;

            yield return Click(host.ActiveStage3View.GetControlScreenBounds(Stage3StageView.HelpControlId).center);
            Assert.That(host.ActiveStage3HelpView.LastMode, Is.EqualTo(Stage3HelpMode.OutcomeFacts));
            Assert.That(host.ActiveStage3HelpView.UsesOutcome(completed.CurrentOutcome), Is.True);
            yield return Click(host.ActiveStage3HelpView.GetControlScreenBounds(Stage3HelpView.BackControlId).center);

            yield return Click(host.ActiveStage3View.GetControlScreenBounds(Stage3StageView.AnotherRiverControlId).center);
            Assert.That(host.Stage3Snapshot.State, Is.EqualTo(Stage3State.ChoosingRiver));
            yield return Click(host.ActiveStage3View.GetControlScreenBounds(Stage3StageView.HelpControlId).center);
            Assert.That(host.ActiveStage3HelpView.LastMode, Is.EqualTo(Stage3HelpMode.RiverSelection));
            Assert.That(host.ActiveStage3HelpView.VisibleRiverLabelCount, Is.EqualTo(3));
            yield return Click(host.ActiveStage3HelpView.GetControlScreenBounds(Stage3HelpView.BackControlId).center);
            yield return Click(host.ActiveStage3View.GetControlScreenBounds(Stage3StageView.RiverBackControlId).center);

            AssertStage3ChoiceEquals(completed, host.Stage3Snapshot);
            Assert.That(host.Stage3Snapshot.OutcomeTokenId, Is.EqualTo(completed.OutcomeTokenId));
            Assert.That(host.Stage3Snapshot.InstructionTokenId, Is.EqualTo(completed.InstructionTokenId));
            Assert.That(host.Stage3Snapshot.OptionCount, Is.EqualTo(completed.OptionCount));
        }

        [UnityTest]
        public IEnumerator PauseNestedLeaveAndAppInterruptionRestoreExactOutcomeCaller()
        {
            ScreenHost host = null;
            yield return EnterOutcomeReady(value => host = value);
            var caller = host.Stage3Snapshot;

            yield return Click(host.ActiveStage3View.GetControlScreenBounds(Stage3StageView.PauseControlId).center);
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.HomeControlId).center);
            Assert.That(host.Stage3PauseState, Is.EqualTo(PauseLayerState.Leave));
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(LeaveConfirmationView.StayControlId).center);
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            AssertStage3ChoiceEquals(caller, host.Stage3Snapshot);

            yield return Click(host.ActiveStage3View.GetControlScreenBounds(Stage3StageView.HomeControlId).center);
            host.SendMessage("OnApplicationPause", true);
            yield return null;
            Assert.That(host.Stage3PauseState, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(host.Stage3LastPauseReason, Is.EqualTo(Stage3PauseReason.AppBackground));
            Assert.That(host.Stage3PauseCallerState, Is.EqualTo(Stage3State.Leaving));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(PauseOverlayView.ResumeControlId).center);
            Assert.That(host.Stage3Snapshot.State, Is.EqualTo(Stage3State.Leaving));
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.True);
            Assert.That(host.EnabledSemanticControlsOverlap, Is.False);
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(LeaveConfirmationView.CancelControlId).center);
            AssertStage3ChoiceEquals(caller, host.Stage3Snapshot);
        }

        [UnityTest]
        public IEnumerator CompletionTimeoutRetryUsesFreshTransactionAndRejectsStaleCompletion()
        {
            ScreenHost host = null;
            yield return EnterCompletedStage3(value => host = value);
            var failed = host.ActiveNavigationId.Value;
            var retained = host.Stage3Snapshot;

            host.ForceNavigationTimeoutForTests();
            Assert.That(host.Stage3RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.Stage3Snapshot.State, Is.EqualTo(Stage3State.Recovery));
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(LeaveConfirmationView.RetryControlId).center);

            var retry = host.ActiveNavigationId.Value;
            Assert.That(retry, Is.Not.EqualTo(failed));
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Completion));
            Assert.That(host.Stage3Snapshot.State, Is.EqualTo(Stage3State.Navigating));
            Assert.That(host.Stage3Snapshot.SelectedRiver, Is.EqualTo(retained.SelectedRiver));
            Assert.That(host.Stage3Snapshot.SelectedPlan, Is.EqualTo(retained.SelectedPlan));
            Assert.That(host.TryCompleteNavigation(failed), Is.False);
            Assert.That(host.ActiveNavigationId, Is.EqualTo(retry));
        }

        [UnityTest]
        public IEnumerator CompletionAssetAndLayoutFailuresRetainStage3AndHomeReturnsWelcome()
        {
            ScreenHost host = null;
            yield return EnterCompletedStage3(value => host = value);
            var retained = host.Stage3Snapshot;

            Assert.That(host.ReportStage3BoundaryAssetFailure(), Is.True);
            Assert.That(host.Stage3RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.Stage3Snapshot.SelectedPlan, Is.EqualTo(retained.SelectedPlan));
            Assert.That(host.Stage3Snapshot.SelectedRiver, Is.EqualTo(retained.SelectedRiver));
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(LeaveConfirmationView.RetryControlId).center);
            Assert.That(host.TryApplyStage3Layout(360f, 182f, 0f, 0f, 360f, 182f, 429f, 144), Is.False);
            Assert.That(host.Stage3RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));

            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(LeaveConfirmationView.HomeControlId).center);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.SessionOpen, Is.True);
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Stage3));
            Assert.That(host.ActiveStage3View, Is.Null);
        }

        private IEnumerator EnterCompletedStage3(Action<ScreenHost> capture)
        {
            ScreenHost host = null;
            yield return EnterOutcomeReady(value => host = value);
            Assert.That(host.PresentStage3FinishForDiagnostics(), Is.True);
            capture(host);
        }

        private IEnumerator EnterOutcomeReady(Action<ScreenHost> capture)
        {
            ScreenHost host = null;
            yield return EnterStage3(value => host = value, Stage2River.High);
            Assert.That(host.PresentStage3PlayForDiagnostics(), Is.True);
            Assert.That(host.PresentStage3PlanForDiagnostics(Stage3Plan.Channel), Is.True);
            Assert.That(host.CompleteStage3OutcomeForDiagnostics(), Is.True);
            Assert.That(host.Stage3Snapshot.State, Is.EqualTo(Stage3State.OutcomeReady));
            capture(host);
        }

        private IEnumerator EnterStage3(Action<ScreenHost> capture, Stage2River river)
        {
            ScreenHost host = null;
            yield return EnterStage2(value => host = value);
            Assert.That(host.PresentStage2PlayForDiagnostics(), Is.True);
            Assert.That(host.PresentStage2RiverForDiagnostics(river), Is.True);
            foreach (var hotspot in new[] { Stage2Hotspot.Field, Stage2Hotspot.Path, Stage2Hotspot.Boat })
            {
                Assert.That(host.PresentStage2HotspotForDiagnostics(hotspot), Is.True);
                Assert.That(host.CompleteStage2ConsequenceForDiagnostics(), Is.True);
            }
            Assert.That(host.PresentStage2FinishForDiagnostics(), Is.True);
            yield return WaitFor(() => host.Boat3InputReady && host.CurrentRoute == RouteId.Boat3, 3f);

            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.SkipControlId).center);
            Assert.That(host.CompleteBoat3ArrivalForDiagnostics(), Is.True);
            var pending = host.ActiveNavigationId;
            Assert.That(host.PendingStage3EntryTransactionId, Is.EqualTo(pending));
            Assert.That(host.TryCompleteStage3EntryForTests(Guid.NewGuid()), Is.False);
            Assert.That(host.ActiveNavigationId, Is.EqualTo(pending));
            yield return WaitFor(() => host.Stage3InputReady && host.CurrentRoute == RouteId.Stage3, 3f);
            Assert.That(host.TryCompleteStage3EntryForTests(pending.Value), Is.False);
            Assert.That(host.LastCompletedStage3EntryTransactionId, Is.EqualTo(pending));
            capture(host);
        }

        private IEnumerator EnterStage2(Action<ScreenHost> capture)
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();
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
            yield return WaitFor(() => host.Stage1InputReady, 3f);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.PlayControlId).center);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.MarkerControlId).center);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.SocketControlId).center);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.ConfirmControlId).center);
            Assert.That(host.CompleteStage1OutcomeForDiagnostics(), Is.True);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.ContinueControlId).center);
            yield return WaitFor(() => host.Boat2InputReady, 3f);
            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.DockControlId).center);
            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(Boat2StageView.SkipControlId).center);
            Assert.That(host.CompleteBoat2ArrivalForDiagnostics(), Is.True);
            yield return WaitFor(() => host.Stage2InputReady && host.CurrentRoute == RouteId.Stage2, 3f);
            capture(host);
        }

        private static IEnumerator LoadFreshWelcome()
        {
            if (!Application.isBatchMode)
            {
                Screen.SetResolution(1280, 720, false);
                yield return WaitFor(() => Screen.width == 1280 && Screen.height == 720, 3f);
            }
            if (PersistentCompositionRoot.Instance != null)
            {
                Object.Destroy(PersistentCompositionRoot.Instance.gameObject);
                yield return null;
            }
            SceneManager.LoadScene(0, LoadSceneMode.Single);
            yield return null;
            var host = Object.FindFirstObjectByType<ScreenHost>();
            yield return WaitFor(() => host.CurrentRoute != RouteId.StartupLoading, 8f);
            Assert.That(host.SkipIntroForTests(), Is.True);
            yield return null;
            yield return null;
        }

        private static IEnumerator WaitFor(Func<bool> predicate, float seconds)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!predicate() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(predicate(), Is.True);
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

        private static void AssertStage3ChoiceEquals(Stage3Snapshot expected, Stage3Snapshot actual)
        {
            Assert.That(actual.State, Is.EqualTo(expected.State));
            Assert.That(actual.SelectedRiver, Is.EqualTo(expected.SelectedRiver));
            Assert.That(actual.SelectedPlan, Is.EqualTo(expected.SelectedPlan));
            Assert.That(actual.CurrentOutcome, Is.EqualTo(expected.CurrentOutcome));
            Assert.That(actual.TriedCombinations, Is.EqualTo(expected.TriedCombinations));
            Assert.That(actual.OptionCount, Is.EqualTo(expected.OptionCount));
        }
    }
}
