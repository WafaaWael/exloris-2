using System;
using System.Collections;
using Exloris.Core.Navigation;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Bootstrap;
using Exloris.Runtime.Completion;
using Exloris.Runtime.Overview;
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
    public sealed class CompletionAdultReportIntegrationTests : InputTestFixture
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
        public IEnumerator DirectRouteSupportsExactReturnShortHoldCancelAndConfirmedReset()
        {
            ScreenHost host = null;
            yield return EnterCompletion(value => host = value);
            yield return WaitFor(() => host.ClosingCompletionSnapshot.State == CompletionState.Ready, 5f);
            var completionToken = host.ClosingCompletionSnapshot.TokenId;
            var completionElapsed = host.ClosingCompletionSnapshot.ElapsedSeconds;

            host.SendMessage("OnApplicationPause", true);
            yield return null;
            yield return null;
            Assert.That(host.ClosingCompletionSnapshot.State, Is.EqualTo(CompletionState.Paused));
            Assert.That(host.ClosingPauseState, Is.EqualTo(Exloris.Runtime.Recovery.PauseLayerState.Pause));
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(
                Exloris.Runtime.Recovery.PauseOverlayView.ResumeControlId).center);
            Assert.That(host.ClosingCompletionSnapshot.TokenId, Is.EqualTo(completionToken));
            Assert.That(host.ClosingCompletionSnapshot.ElapsedSeconds, Is.EqualTo(completionElapsed));
            Assert.That(host.ClosingCompletionSnapshot.State, Is.EqualTo(CompletionState.Ready));

            yield return Click(host.ActiveCompletionView.GetControlScreenBounds(
                CompletionStageView.AdultControlId).center);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AdultGate));
            yield return Hold(host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.HoldControlId).center, 3.1d);
            Assert.That(host.ClosingAdultSnapshot.State, Is.EqualTo(AdultGateState.AwaitingConfirm));
            yield return Click(host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.ConfirmControlId).center);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.ParentReport));
            Assert.That(host.ActiveParentReportView, Is.Not.Null);
            var reportToken = host.ClosingParentReportSnapshot.TokenId;

            yield return Click(host.ActiveParentReportView.GetControlScreenBounds(
                ParentReportView.CloseControlId).center);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Completion));
            Assert.That(host.ClosingCompletionSnapshot.TokenId, Is.EqualTo(completionToken));
            Assert.That(host.ClosingCompletionSnapshot.State, Is.EqualTo(CompletionState.Ready));

            yield return Click(host.ActiveCompletionView.GetControlScreenBounds(
                CompletionStageView.AdultControlId).center);
            yield return Hold(host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.HoldControlId).center, 3.1d);
            yield return Click(host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.ConfirmControlId).center);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.ParentReport));
            reportToken = host.ClosingParentReportSnapshot.TokenId;

            yield return Click(host.ActiveParentReportView.GetControlScreenBounds(
                ParentReportView.RestartControlId).center);
            yield return Hold(host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.HoldControlId).center, 1d);
            Assert.That(host.ClosingAdultSnapshot.State, Is.EqualTo(AdultGateState.Holding));
            Assert.That(host.ClosingAdultSnapshot.HoldProgress, Is.EqualTo(0d));
            yield return Hold(host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.HoldControlId).center, 3.1d);
            yield return Click(host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.ConfirmControlId).center);
            Assert.That(host.ClosingAdultSnapshot.State, Is.EqualTo(AdultGateState.ResetConfirmation));
            yield return Click(host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.CancelControlId).center);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.ParentReport));
            Assert.That(host.ClosingParentReportSnapshot.TokenId, Is.EqualTo(reportToken));
            Assert.That(host.ClosingParentReportSnapshot.State, Is.EqualTo(ParentReportState.Ready));

            yield return Click(host.ActiveParentReportView.GetControlScreenBounds(
                ParentReportView.RestartControlId).center);
            yield return Hold(host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.HoldControlId).center, 3.1d);
            yield return Click(host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.ConfirmControlId).center);
            yield return Click(host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.ConfirmControlId).center);

            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.StartupLoading));
            Assert.That(host.SessionOpen, Is.False);
            yield return WaitFor(() => host.CurrentRoute != RouteId.StartupLoading, 8f);
            Assert.That(host.SkipIntroForTests(), Is.True);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
        }

        [UnityTest]
        public IEnumerator AdultGateRealHoldAdvancesProgressAndPointerExitResetsIt()
        {
            ScreenHost host = null;
            yield return EnterCompletion(value => host = value);
            yield return WaitFor(() => host.ClosingCompletionSnapshot.State == CompletionState.Ready, 5f);
            yield return Click(host.ActiveCompletionView.GetControlScreenBounds(
                CompletionStageView.AdultControlId).center);

            var holdPosition = host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.HoldControlId).center;
            var start = currentTime + 0.30d;
            Set(_mouse.position, holdPosition, start);
            Press(_mouse.leftButton, start + 0.01d);
            yield return null;
            Set(_mouse.position, holdPosition + Vector2.right, start + 0.76d);
            yield return null;

            Assert.That(host.ClosingAdultSnapshot.State, Is.EqualTo(AdultGateState.Holding));
            Assert.That(host.ClosingAdultSnapshot.HoldProgress, Is.EqualTo(0.25d).Within(0.02d));

            host.SendMessage("OnApplicationPause", true);
            yield return null;
            Assert.That(host.ClosingAdultSnapshot.HoldProgress, Is.EqualTo(0d));
            Assert.That(host.ClosingAdultSnapshot.ActivePointerId, Is.EqualTo(-1));
            Release(_mouse.leftButton, start + 0.77d);
            yield return null;

            start += 1d;
            Set(_mouse.position, holdPosition, start);
            Press(_mouse.leftButton, start + 0.01d);
            yield return null;
            Set(_mouse.position, holdPosition + Vector2.right, start + 0.76d);
            yield return null;

            Set(_mouse.position, new Vector2(-100f, -100f), start + 0.80d);
            yield return null;
            Assert.That(host.ClosingAdultSnapshot.HoldProgress, Is.EqualTo(0d));
            Assert.That(host.ClosingAdultSnapshot.ActivePointerId, Is.EqualTo(-1));
            Release(_mouse.leftButton, start + 0.81d);
            currentTime = start + 0.82d;
            yield return null;
        }

        private IEnumerator EnterCompletion(Action<ScreenHost> capture)
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
            yield return WaitFor(() => host.Stage2InputReady, 3f);
            Assert.That(host.PresentStage2PlayForDiagnostics(), Is.True);
            Assert.That(host.PresentStage2RiverForDiagnostics(Stage2River.High), Is.True);
            foreach (var hotspot in new[] { Stage2Hotspot.Field, Stage2Hotspot.Path, Stage2Hotspot.Boat })
            {
                Assert.That(host.PresentStage2HotspotForDiagnostics(hotspot), Is.True);
                Assert.That(host.CompleteStage2ConsequenceForDiagnostics(), Is.True);
            }
            Assert.That(host.PresentStage2FinishForDiagnostics(), Is.True);
            yield return WaitFor(() => host.Boat3InputReady, 3f);
            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(Boat3StageView.SkipControlId).center);
            Assert.That(host.CompleteBoat3ArrivalForDiagnostics(), Is.True);
            yield return WaitFor(() => host.Stage3InputReady, 3f);
            Assert.That(host.PresentStage3PlayForDiagnostics(), Is.True);
            Assert.That(host.PresentStage3PlanForDiagnostics(Stage3Plan.Channel), Is.True);
            Assert.That(host.CompleteStage3OutcomeForDiagnostics(), Is.True);
            Assert.That(host.PresentStage3FinishForDiagnostics(), Is.True);
            yield return WaitFor(() => host.CurrentRoute == RouteId.Completion, 3f);
            Assert.That(host.ActiveCompletionView, Is.Not.Null);
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
            yield return null;
        }

        private IEnumerator Hold(Vector2 position, double seconds)
        {
            var time = currentTime + 0.30d;
            Set(_mouse.position, position, time);
            Press(_mouse.leftButton, time + 0.01d);
            Release(_mouse.leftButton, time + 0.01d + seconds);
            currentTime = time + 0.02d + seconds;
            yield return null;
            yield return null;
        }
    }
}
