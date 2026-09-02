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
    public sealed class Boat1RecoveryTests : InputTestFixture
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
        public IEnumerator EntryTimeoutRetryUsesFreshTransactionAndCreatesOneBoat()
        {
            var host = default(ScreenHost);
            yield return EnterJourney(result => host = result);
            var journeyToken = host.CurrentJourneyOverviewToken;
            host.DeferNextJourneyBoundaryForTests();
            Assert.That(host.PresentJourneyBoundaryForDiagnostics(), Is.True);
            var failedId = host.ActiveNavigationId.Value;

            host.ForceNavigationTimeoutForTests();
            Assert.That(host.JourneyRecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.CurrentJourneyOverviewToken, Is.EqualTo(journeyToken));
            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(
                LeaveConfirmationView.RetryControlId).center);
            var retryId = host.ActiveNavigationId.Value;
            Assert.That(retryId, Is.Not.EqualTo(failedId));
            Assert.That(host.TryCompleteBoat1EntryForTests(retryId), Is.True);
            yield return null;

            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Boat1));
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Boat1));
            Assert.That(Object.FindObjectsByType<Boat1StageView>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(host.TryCompleteBoat1EntryForTests(failedId), Is.False);
            Assert.That(host.TryCompleteBoat1EntryForTests(retryId), Is.False);
        }

        [UnityTest]
        public IEnumerator EntryAssetAndLayoutFailuresFailClosedWithoutPartialBoat()
        {
            var host = default(ScreenHost);
            yield return EnterJourney(result => host = result);
            host.DeferNextJourneyBoundaryForTests();
            Assert.That(host.PresentJourneyBoundaryForDiagnostics(), Is.True);
            Assert.That(host.ReportJourneyBoundaryAssetFailure(), Is.True);
            Assert.That(host.JourneyRecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.ActiveBoat1View, Is.Null);
            Assert.That(Object.FindObjectsByType<Boat1StageView>(FindObjectsSortMode.None), Is.Empty);

            yield return Click(host.ActiveLeaveConfirmationView.GetTechnicalControlScreenBounds(
                LeaveConfirmationView.HomeControlId).center);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.JourneyOverview));

            Object.Destroy(PersistentCompositionRoot.Instance.gameObject);
            yield return null;
            yield return EnterJourney(result => host = result);
            host.DeferNextJourneyBoundaryForTests();
            Assert.That(host.PresentJourneyBoundaryForDiagnostics(), Is.True);
            Assert.That(host.TryApplyJourneyLayout(360f, 182f, 360f, 182f, 326f, 88), Is.False);
            Assert.That(host.JourneyRecoveryState, Is.EqualTo(AvatarTechnicalOverlayState.Recovery));
            Assert.That(host.ActiveBoat1View, Is.Null);
            Assert.That(host.ActiveNavigationId, Is.Null);
        }

        private IEnumerator EnterJourney(System.Action<ScreenHost> capture)
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
            var time = currentTime + 0.30d;
            Set(_mouse.position, position, time);
            Press(_mouse.leftButton, time + 0.01d);
            Release(_mouse.leftButton, time + 0.06d);
            currentTime = time + 0.07d;
            yield return null;
        }
    }
}
