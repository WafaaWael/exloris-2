using System;
using System.Collections;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Bootstrap;
using Exloris.Runtime.Completion;
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
    public sealed class FullRouteRealInputTests : InputTestFixture
    {
        private Mouse _mouse;
        private int _originalWidth;
        private int _originalHeight;

        public override void Setup()
        {
            base.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
            _originalWidth = Screen.width;
            _originalHeight = Screen.height;
        }

        public override void TearDown()
        {
            if (PersistentCompositionRoot.Instance != null)
                Object.DestroyImmediate(PersistentCompositionRoot.Instance.gameObject);
            // Batchmode uses one graphics process for the entire suite. Restoring
            // its original 640x480 Game View makes Unity ignore later resolution
            // requests and leaks an infeasible safe area into following fixtures.
            if (!Application.isBatchMode)
                Screen.SetResolution(_originalWidth, _originalHeight, false);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator GirlLowRiverGrainTraversesExactFifteenScreenRoute() =>
            TraverseRoute(AvatarChoice.Girl, Stage1OutcomeKind.OutsideLow, Stage2River.Low, Stage3Plan.Grain);

        [UnityTest]
        public IEnumerator BoyMiddleRiverChannelTraversesExactFifteenScreenRoute() =>
            TraverseRoute(AvatarChoice.Boy, Stage1OutcomeKind.InRange, Stage2River.Middle, Stage3Plan.Channel);

        [UnityTest]
        public IEnumerator GirlHighRiverBoatTraversesExactFifteenScreenRoute() =>
            TraverseRoute(AvatarChoice.Girl, Stage1OutcomeKind.OutsideHigh, Stage2River.High, Stage3Plan.Boat);

        private IEnumerator TraverseRoute(
            AvatarChoice avatar,
            Stage1OutcomeKind stage1Outcome,
            Stage2River stage2River,
            Stage3Plan stage3Plan)
        {
            if (!Application.isBatchMode)
                Screen.SetResolution(1280, 720, false);
            yield return null;
            if (PersistentCompositionRoot.Instance != null)
            {
                Object.Destroy(PersistentCompositionRoot.Instance.gameObject);
                yield return null;
            }
            SceneManager.LoadScene(0, LoadSceneMode.Single);
            yield return null;
            var host = Object.FindFirstObjectByType<ScreenHost>();
            Assert.That(host, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<ScreenHost>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            var route = new List<RouteId> { RouteId.StartupLoading };

            yield return WaitFor(() => host.CurrentRoute != RouteId.StartupLoading, 8f);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.IntroVideo));
            AddRoute(route, host.CurrentRoute);
            Assert.That(host.ActiveIntroView, Is.Not.Null);
            yield return Click(host.ActiveIntroView.SkipScreenBounds.center);
            yield return WaitFor(() => host.CurrentRoute == RouteId.Welcome, 3f);
            AddRoute(route, host.CurrentRoute);

            host.ConfigureAvatarInitialLeftForTests(avatar);
            yield return Click(host.ActiveWelcomeView.GetControlScreenBounds(
                WelcomeStageView.PlayControlId).center);
            AddRoute(route, host.CurrentRoute);
            yield return Click(host.ActiveAvatarSelectView.GetControlScreenBounds(
                avatar == AvatarChoice.Girl
                    ? AvatarSelectView.GirlControlId
                    : AvatarSelectView.BoyControlId).center);
            AddRoute(route, host.CurrentRoute);
            yield return Click(host.ActiveAvatarPreviewView.GetControlScreenBounds(
                AvatarPreviewView.ConfirmControlId).center);
            AddRoute(route, host.CurrentRoute);

            yield return Click(host.ActiveJourneyOverviewView.GetControlScreenBounds(
                JourneyOverviewStageView.HomeControlId).center);
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(
                LeaveConfirmationView.StayControlId).center);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.JourneyOverview));
            yield return Click(host.ActiveJourneyOverviewView.GetControlScreenBounds(
                JourneyOverviewStageView.PauseControlId).center);
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(
                PauseOverlayView.ResumeControlId).center);
            yield return Click(host.ActiveJourneyOverviewView.GetControlScreenBounds(
                JourneyOverviewStageView.SkipControlId).center);
            yield return WaitFor(() => host.CurrentRoute == RouteId.Boat1 && host.Boat1InputReady, 3f);
            AddRoute(route, host.CurrentRoute);

            yield return PauseAndResume(host, host.ActiveBoat1View.GetControlScreenBounds(
                Boat1StageView.PauseControlId).center);
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(
                Boat1StageView.ReedsControlId).center);
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(
                Boat1StageView.SkipControlId).center);
            yield return WaitFor(() => host.CurrentRoute == RouteId.Stage1 && host.Stage1InputReady, 4f);
            AddRoute(route, host.CurrentRoute);

            yield return PauseAndResume(host, host.ActiveStage1View.GetControlScreenBounds(
                Stage1StageView.PauseControlId).center);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(
                Stage1StageView.PlayControlId).center);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(
                Stage1StageView.MarkerControlId).center);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(
                Stage1StageView.SocketControlId).center);
            var stage1Adjustment = stage1Outcome == Stage1OutcomeKind.OutsideLow
                ? Stage1StageView.DownControlId
                : Stage1StageView.UpControlId;
            var stage1AdjustmentCount = stage1Outcome == Stage1OutcomeKind.InRange ? 0 : 2;
            for (var index = 0; index < stage1AdjustmentCount; index++)
            {
                yield return Click(host.ActiveStage1View.GetControlScreenBounds(stage1Adjustment).center);
            }
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(
                Stage1StageView.ConfirmControlId).center);
            yield return WaitFor(() => host.Stage1Snapshot.State == Stage1State.OutcomeReady, 5f);
            Assert.That(host.Stage1Snapshot.Outcome, Is.EqualTo(stage1Outcome));
            if (stage1Outcome != Stage1OutcomeKind.InRange)
            {
                yield return Click(host.ActiveStage1View.GetControlScreenBounds(
                    Stage1StageView.TryAgainControlId).center);
                yield return Click(host.ActiveStage1View.GetControlScreenBounds(
                    stage1Outcome == Stage1OutcomeKind.OutsideLow
                        ? Stage1StageView.UpControlId
                        : Stage1StageView.DownControlId).center);
                yield return Click(host.ActiveStage1View.GetControlScreenBounds(
                    Stage1StageView.ConfirmControlId).center);
                yield return WaitFor(() => host.Stage1Snapshot.State == Stage1State.OutcomeReady, 5f);
                Assert.That(host.Stage1Snapshot.Outcome, Is.EqualTo(Stage1OutcomeKind.InRange));
            }
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(
                Stage1StageView.ContinueControlId).center);
            yield return WaitFor(() => host.CurrentRoute == RouteId.Boat2 && host.Boat2InputReady, 4f);
            AddRoute(route, host.CurrentRoute);

            yield return PauseAndResume(host, host.ActiveBoat2View.GetControlScreenBounds(
                Boat2StageView.PauseControlId).center);
            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(
                Boat2StageView.DockControlId).center);
            yield return Click(host.ActiveBoat2View.GetControlScreenBounds(
                Boat2StageView.SkipControlId).center);
            yield return WaitFor(() => host.CurrentRoute == RouteId.Stage2 && host.Stage2InputReady, 4f);
            AddRoute(route, host.CurrentRoute);

            yield return PauseAndResume(host, host.ActiveStage2View.GetControlScreenBounds(
                Stage2StageView.PauseControlId).center);
            yield return Click(host.ActiveStage2View.GetControlScreenBounds(
                Stage2StageView.PlayControlId).center);
            yield return Click(host.ActiveStage2View.GetControlScreenBounds(Stage2Control(stage2River)).center);
            foreach (var controlId in new[]
            {
                Stage2StageView.FieldControlId,
                Stage2StageView.PathControlId,
                Stage2StageView.BoatControlId,
            })
            {
                yield return Click(host.ActiveStage2View.GetControlScreenBounds(controlId).center);
                yield return WaitFor(() => host.Stage2Snapshot.State == Stage2State.Inspecting, 4f);
            }
            yield return Click(host.ActiveStage2View.GetControlScreenBounds(
                Stage2StageView.FinishControlId).center);
            yield return WaitFor(() => host.CurrentRoute == RouteId.Boat3 && host.Boat3InputReady, 4f);
            AddRoute(route, host.CurrentRoute);

            yield return PauseAndResume(host, host.ActiveBoat3View.GetControlScreenBounds(
                Boat3StageView.PauseControlId).center);
            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(
                Boat3StageView.GrainStoreControlId).center);
            yield return Click(host.ActiveBoat3View.GetControlScreenBounds(
                Boat3StageView.SkipControlId).center);
            yield return WaitFor(() => host.CurrentRoute == RouteId.Stage3 && host.Stage3InputReady, 4f);
            AddRoute(route, host.CurrentRoute);

            yield return PauseAndResume(host, host.ActiveStage3View.GetControlScreenBounds(
                Stage3StageView.PauseControlId).center);
            yield return Click(host.ActiveStage3View.GetControlScreenBounds(
                Stage3StageView.PlayControlId).center);
            var stage3Plans = new List<Stage3Plan>(Stage3PlansStartingWith(stage3Plan));
            for (var planIndex = 0; planIndex < stage3Plans.Count; planIndex++)
            {
                var plan = stage3Plans[planIndex];
                yield return Click(host.ActiveStage3View.GetControlScreenBounds(Stage3Control(plan)).center);
                yield return WaitFor(() => host.Stage3Snapshot.State == Stage3State.OutcomeReady, 9f);
                Assert.That(host.Stage3Snapshot.SelectedPlan, Is.EqualTo(plan));
                if (planIndex < stage3Plans.Count - 1)
                {
                    yield return Click(host.ActiveStage3View.GetControlScreenBounds(
                        Stage3StageView.AnotherPlanControlId).center);
                }
            }
            yield return Click(host.ActiveStage3View.GetControlScreenBounds(
                Stage3StageView.FinishControlId).center);
            yield return WaitFor(() => host.CurrentRoute == RouteId.Completion, 4f);
            AddRoute(route, host.CurrentRoute);
            yield return WaitFor(() => host.ClosingCompletionSnapshot.State == CompletionState.Ready, 5f);

            yield return PauseAndResume(host, host.ActiveCompletionView.GetControlScreenBounds(
                CompletionStageView.PauseControlId).center);
            yield return Click(host.ActiveCompletionView.GetControlScreenBounds(
                CompletionStageView.AdultControlId).center);
            AddRoute(route, host.CurrentRoute);
            yield return Hold(host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.HoldControlId).center, 3.1d);
            yield return Click(host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.ConfirmControlId).center);
            AddRoute(route, host.CurrentRoute);

            Assert.That(route, Is.EqualTo(new[]
            {
                RouteId.StartupLoading,
                RouteId.IntroVideo,
                RouteId.Welcome,
                RouteId.AvatarSelect,
                RouteId.AvatarPreview,
                RouteId.JourneyOverview,
                RouteId.Boat1,
                RouteId.Stage1,
                RouteId.Boat2,
                RouteId.Stage2,
                RouteId.Boat3,
                RouteId.Stage3,
                RouteId.Completion,
                RouteId.AdultGate,
                RouteId.ParentReport,
            }));
            Assert.That(host.ActiveParentReportView, Is.Not.Null);
            Assert.That(host.ClosingParentReportSnapshot.State, Is.EqualTo(ParentReportState.Ready));

            yield return Click(host.ActiveParentReportView.GetControlScreenBounds(
                ParentReportView.RestartControlId).center);
            yield return Hold(host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.HoldControlId).center, 3.1d);
            yield return Click(host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.ConfirmControlId).center);
            Assert.That(host.ClosingAdultSnapshot.State, Is.EqualTo(AdultGateState.ResetConfirmation));
            yield return Click(host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.ConfirmControlId).center);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.StartupLoading));
            Assert.That(host.SessionOpen, Is.False);
            Assert.That(host.SessionSelectedAvatarId, Is.Null);
            yield return WaitFor(() => host.CurrentRoute == RouteId.IntroVideo, 8f);
            yield return Click(host.ActiveIntroView.SkipScreenBounds.center);
            yield return WaitFor(() => host.CurrentRoute == RouteId.Welcome, 3f);
            Assert.That(host.SessionOpen, Is.False);
        }

        private static string Stage2Control(Stage2River river)
        {
            switch (river)
            {
                case Stage2River.Low: return Stage2StageView.LowControlId;
                case Stage2River.Middle: return Stage2StageView.MiddleControlId;
                case Stage2River.High: return Stage2StageView.HighControlId;
                default: throw new ArgumentOutOfRangeException(nameof(river));
            }
        }

        private static string Stage3Control(Stage3Plan plan)
        {
            switch (plan)
            {
                case Stage3Plan.Grain: return Stage3StageView.GrainControlId;
                case Stage3Plan.Channel: return Stage3StageView.ChannelControlId;
                case Stage3Plan.Boat: return Stage3StageView.BoatPlanControlId;
                default: throw new ArgumentOutOfRangeException(nameof(plan));
            }
        }

        private static IEnumerable<Stage3Plan> Stage3PlansStartingWith(Stage3Plan first)
        {
            yield return first;
            foreach (var plan in new[] { Stage3Plan.Grain, Stage3Plan.Channel, Stage3Plan.Boat })
            {
                if (plan != first) yield return plan;
            }
        }

        private IEnumerator PauseAndResume(ScreenHost host, Vector2 pausePosition)
        {
            var caller = host.CurrentRoute;
            yield return Click(pausePosition);
            Assert.That(host.ActivePauseOverlayView, Is.Not.Null);
            yield return Click(host.ActivePauseOverlayView.GetControlScreenBounds(
                PauseOverlayView.ResumeControlId).center);
            Assert.That(host.CurrentRoute, Is.EqualTo(caller));
        }

        private static void AddRoute(ICollection<RouteId> route, RouteId current)
        {
            Assert.That(new List<RouteId>(route).Contains(current), Is.False);
            route.Add(current);
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

        private static IEnumerator WaitFor(Func<bool> predicate, float seconds)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (!predicate() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(predicate(), Is.True);
        }
    }
}
