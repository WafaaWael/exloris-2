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
    public sealed class Stage1RecoveryTests : InputTestFixture
    {
        private Mouse _mouse;
        public override void Setup() { base.Setup(); _mouse = InputSystem.AddDevice<Mouse>(); }
        public override void TearDown() { if (PersistentCompositionRoot.Instance != null) Object.DestroyImmediate(PersistentCompositionRoot.Instance.gameObject); base.TearDown(); }

        [UnityTest]
        public IEnumerator Boat2TimeoutRetryUsesFreshTransactionAndStaleCompletionIsInert()
        {
            var host = default(ScreenHost);
            yield return EnterCompletedStage1(result => host = result);
            var failedId = host.ActiveNavigationId.Value;
            host.ForceNavigationTimeoutForTests();
            Assert.That(host.Stage1RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(LeaveConfirmationView.RetryControlId).center);
            var retryId = host.ActiveNavigationId ?? host.LastCompletedBoat2EntryTransactionId;
            Assert.That(retryId.HasValue, Is.True);
            Assert.That(retryId.Value, Is.Not.EqualTo(failedId));
            Assert.That(host.TryCompleteNavigation(failedId), Is.False);
            if (host.CurrentRoute == RouteId.Stage1)
            {
                Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Boat2));
                Assert.That(host.Stage1Snapshot.State, Is.EqualTo(Stage1State.Navigating));
            }
            else
            {
                Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Boat2));
                Assert.That(host.LastCompletedBoat2EntryTransactionId, Is.EqualTo(retryId));
            }
        }

        [UnityTest]
        public IEnumerator BoundaryAssetFailureHomeReturnsToWelcomeWithStage1SessionPreserved()
        {
            var host = default(ScreenHost);
            yield return EnterCompletedStage1(result => host = result);
            Assert.That(host.ReportStage1BoundaryAssetFailure(), Is.True);
            Assert.That(host.Stage1RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(LeaveConfirmationView.HomeControlId).center);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.SessionOpen, Is.True);
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Stage1));
            Assert.That(host.ActiveStage1View, Is.Null);
        }

        [UnityTest]
        public IEnumerator EntryAssetFailureRetryRepreparesFreshTransactionAndEntersStage1()
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();
            host.ConfigureAvatarInitialLeftForTests(AvatarChoice.Girl);
            yield return Click(host.ActiveWelcomeView.GetControlScreenBounds(WelcomeStageView.PlayControlId).center); yield return null;
            yield return Click(host.ActiveAvatarSelectView.GetControlScreenBounds(AvatarSelectView.GirlControlId).center); yield return null;
            yield return Click(host.ActiveAvatarPreviewView.GetControlScreenBounds(AvatarPreviewView.ConfirmControlId).center); yield return null;
            yield return Click(host.ActiveJourneyOverviewView.GetControlScreenBounds(JourneyOverviewStageView.SkipControlId).center); yield return null;
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.SkipControlId).center);
            Assert.That(host.CompleteBoat1ArrivalForDiagnostics(), Is.True);
            var failedId = host.ActiveNavigationId.Value;
            Assert.That(host.ReportBoat1BoundaryAssetFailure(), Is.True);
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(LeaveConfirmationView.RetryControlId).center);
            var retryId = host.ActiveNavigationId.Value;
            Assert.That(retryId, Is.Not.EqualTo(failedId));
            Assert.That(host.PendingStage1EntryTransactionId, Is.EqualTo(retryId));
            Assert.That(host.TryCompleteNavigation(failedId), Is.False);
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!host.Stage1InputReady && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Stage1));
            Assert.That(host.Stage1InputReady, Is.True);
            Assert.That(host.Stage1RecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Hidden));
            Assert.That(host.ActiveLeaveConfirmationView.IsVisible, Is.False);
        }

        private IEnumerator EnterCompletedStage1(System.Action<ScreenHost> capture)
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();
            host.ConfigureAvatarInitialLeftForTests(AvatarChoice.Girl);
            yield return Click(host.ActiveWelcomeView.GetControlScreenBounds(WelcomeStageView.PlayControlId).center); yield return null;
            yield return Click(host.ActiveAvatarSelectView.GetControlScreenBounds(AvatarSelectView.GirlControlId).center); yield return null;
            yield return Click(host.ActiveAvatarPreviewView.GetControlScreenBounds(AvatarPreviewView.ConfirmControlId).center); yield return null;
            yield return Click(host.ActiveJourneyOverviewView.GetControlScreenBounds(JourneyOverviewStageView.SkipControlId).center); yield return null;
            yield return Click(host.ActiveBoat1View.GetControlScreenBounds(Boat1StageView.SkipControlId).center);
            host.CompleteBoat1ArrivalForDiagnostics();
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!host.Stage1InputReady && Time.realtimeSinceStartup < deadline) yield return null;
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.PlayControlId).center);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.MarkerControlId).center);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.SocketControlId).center);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.ConfirmControlId).center);
            Assert.That(host.CompleteStage1OutcomeForDiagnostics(), Is.True);
            yield return Click(host.ActiveStage1View.GetControlScreenBounds(Stage1StageView.ContinueControlId).center);
            Assert.That(host.ActiveNavigationDestination, Is.EqualTo(RouteId.Boat2));
            capture(host);
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
            var time = currentTime + 0.30d; Set(_mouse.position, position, time); Press(_mouse.leftButton, time + 0.01d); Release(_mouse.leftButton, time + 0.06d); currentTime = time + 0.07d; yield return null;
        }
    }
}
