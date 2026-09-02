using System.Collections;
using Exloris.Core.Navigation;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Bootstrap;
using Exloris.Runtime.UI;
using Exloris.Runtime.Welcome;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class WelcomeHostRealInputTests : InputTestFixture
    {
        private Mouse _mouse;

        public override void Setup()
        {
            base.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
        }

        [UnityTest]
        public IEnumerator NoSessionPlayUsesRealInputAndEntersPreparedAvatarSelect()
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();
            var bounds = host.ActiveWelcomeView.GetControlScreenBounds(WelcomeStageView.PlayControlId);

            yield return Click(bounds.center);

            Assert.That(host.ActiveNavigationDestination, Is.Null);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AvatarSelect));
            Assert.That(host.SessionOpen, Is.True);
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.AvatarSelect));
            Assert.That(host.ActiveWelcomeView, Is.Null);
            Assert.That(host.ActiveAvatarSelectView, Is.Not.Null);
            Assert.That(GameObject.Find("AvatarSelect(Clone)"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator AdultRealTapEntersDashboardGateAndCancelRestoresWelcome()
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();
            var bounds = host.ActiveWelcomeView.GetControlScreenBounds(WelcomeStageView.AdultControlId);

            yield return Click(bounds.center);

            Assert.That(host.PendingAdultGateRequest.HasValue, Is.True);
            Assert.That(host.PendingAdultGateRequest.Value.Purpose, Is.EqualTo(AdultGatePurpose.Dashboard));
            Assert.That(host.PendingAdultGateRequest.Value.ReturnRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.PendingAdultGateRequest.Value.Origin, Is.EqualTo(AdultGateRequestOrigin.Welcome));
            Assert.That(host.PendingAdultGateRequest.Value.ReturnPresentationId, Is.EqualTo("welcome"));
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AdultGate));
            Assert.That(host.ActiveAdultGateView, Is.Not.Null);
            var hold = host.ActiveAdultGateView.GetControlScreenBounds(AdultGateView.HoldControlId);
            var cancel = host.ActiveAdultGateView.GetControlScreenBounds(AdultGateView.CancelControlId);
            var confirm = host.ActiveAdultGateView.GetControlScreenBounds(AdultGateView.ConfirmControlId);
            Assert.That(hold.Overlaps(cancel), Is.False, $"hold={hold}; cancel={cancel}");
            Assert.That(cancel.Overlaps(confirm), Is.False, $"cancel={cancel}; confirm={confirm}");
            Assert.That(host.EnabledSemanticControlsOverlap, Is.False);
            yield return null;

            yield return Click(host.ActiveAdultGateView
                .GetControlScreenBounds(AdultGateView.CancelControlId).center);
            Assert.That(host.LastAcceptedControlId, Is.EqualTo(AdultGateView.CancelControlId));
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.PendingAdultGateRequest, Is.Null);
            Assert.That(host.ActiveWelcomeView, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator InterruptionAcceptsOnlyTheVisibleMappedResumeTap()
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();
            Assert.That(host.HandleWelcomeInterruptionForTests(), Is.True);
            var bounds = host.ActiveWelcomeView.GetTechnicalControlScreenBounds(
                WelcomeStageView.ResumeControlId);

            yield return Click(bounds.center);

            Assert.That(host.LastAcceptedControlId, Is.EqualTo(WelcomeStageView.ResumeControlId));
            Assert.That(host.WelcomeInterruptionResumePending, Is.False);
            Assert.That(host.WelcomeInputReady, Is.True);
            Assert.That(host.ActiveWelcomeView.TechnicalOverlayState,
                Is.EqualTo(WelcomeTechnicalOverlayState.Hidden));
        }

        [UnityTest]
        public IEnumerator CompletedWelcomeTransactionRejectsStaleCompletionAfterAvatarEntry()
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();
            yield return Click(host.ActiveWelcomeView.GetControlScreenBounds(WelcomeStageView.PlayControlId).center);
            var completedId = host.LastCompletedAvatarEntryTransactionId.Value;
            Assert.That(host.TryCompleteNavigation(completedId), Is.False);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AvatarSelect));
            Assert.That(host.ActiveNavigationId, Is.Null);
            Assert.That(Object.FindObjectsByType<Exloris.Runtime.Avatar.AvatarSelectView>(
                FindObjectsSortMode.None), Has.Length.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AvatarSelectLeaveReturnsToUsableWelcome()
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();
            yield return Click(host.ActiveWelcomeView
                .GetControlScreenBounds(WelcomeStageView.PlayControlId).center);
            yield return null;
            yield return Click(host.ActiveAvatarSelectView.GetControlScreenBounds(
                Exloris.Runtime.Avatar.AvatarSelectView.HomeControlId).center);
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(
                Exloris.Runtime.Recovery.LeaveConfirmationView.LeaveControlId).center);
            yield return null;

            Assert.That(host.LastAcceptedControlId,
                Is.EqualTo(Exloris.Runtime.Recovery.LeaveConfirmationView.LeaveControlId));
            Assert.That(host.ActiveNavigationId, Is.Null);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.RecoveryVisible, Is.False);
            Assert.That(host.ActiveWelcomeView.TechnicalOverlayState,
                Is.EqualTo(WelcomeTechnicalOverlayState.Hidden));
            Assert.That(host.WelcomeInputReady, Is.True);
        }

        [UnityTest]
        public IEnumerator OpenSessionPlayBackAndContinueUseExactConditionalBoundary()
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();
            yield return ReturnOpenSessionToWelcome(host);

            currentTime += 0.3d;
            yield return Click(host.ActiveWelcomeView
                .GetControlScreenBounds(WelcomeStageView.PlayControlId).center);
            Assert.That(host.ActiveSessionChoiceView.IsVisible, Is.True);
            Assert.That(host.ActiveNavigationId, Is.Null);

            yield return Click(host.ActiveSessionChoiceView
                .GetControlScreenBounds(SessionChoiceView.BackControlId).center);
            Assert.That(host.ActiveSessionChoiceView.IsVisible, Is.False);
            Assert.That(host.WelcomeInputReady, Is.True);

            currentTime += 0.3d;
            yield return Click(host.ActiveWelcomeView
                .GetControlScreenBounds(WelcomeStageView.PlayControlId).center);
            yield return Click(host.ActiveSessionChoiceView
                .GetControlScreenBounds(SessionChoiceView.ContinueControlId).center);
            yield return null;

            Assert.That(host.ActiveNavigationDestination, Is.Null);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AvatarSelect));
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.AvatarSelect));
            Assert.That(host.ActiveWelcomeView, Is.Null);
            Assert.That(host.AvatarInputReady, Is.True);
        }

        [UnityTest]
        public IEnumerator OpenSessionRestartEntersGateAndCancelRestoresSessionChoice()
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();
            yield return ReturnOpenSessionToWelcome(host);

            currentTime += 0.3d;
            yield return Click(host.ActiveWelcomeView
                .GetControlScreenBounds(WelcomeStageView.PlayControlId).center);
            yield return Click(host.ActiveSessionChoiceView
                .GetControlScreenBounds(SessionChoiceView.RestartControlId).center);

            Assert.That(host.PendingAdultGateRequest.HasValue, Is.True);
            Assert.That(host.PendingAdultGateRequest.Value.Purpose, Is.EqualTo(AdultGatePurpose.Restart));
            Assert.That(host.PendingAdultGateRequest.Value.ReturnRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.PendingAdultGateRequest.Value.Origin, Is.EqualTo(AdultGateRequestOrigin.SessionChoice));
            Assert.That(host.PendingAdultGateRequest.Value.ReturnPresentationId, Is.EqualTo("session_choice"));
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AdultGate));
            Assert.That(host.ActiveNavigationId, Is.Null);
            Assert.That(host.ActiveAdultGateView, Is.Not.Null);
            Assert.That(host.EnabledSemanticControlsOverlap, Is.False);
            yield return null;

            yield return Click(host.ActiveAdultGateView
                .GetControlScreenBounds(AdultGateView.CancelControlId).center);
            Assert.That(host.LastAcceptedControlId, Is.EqualTo(AdultGateView.CancelControlId));
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.PendingAdultGateRequest, Is.Null);
            Assert.That(host.ActiveSessionChoiceView.IsVisible, Is.True);
        }

        private IEnumerator LoadFreshWelcome()
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
            {
                yield return null;
            }

            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.IntroVideo));
            Assert.That(host.SkipIntroForTests(), Is.True);
            yield return null;
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.WelcomeInputReady, Is.True);
        }

        private IEnumerator Click(Vector2 position)
        {
            // InputTestFixture queues events during a UnityTest; the player loop
            // processes them on the next frame.
            var startedAt = currentTime + 0.01d;
            Set(_mouse.position, position, startedAt);
            Press(_mouse.leftButton, startedAt + 0.01d);
            Release(_mouse.leftButton, startedAt + 0.06d);
            currentTime = startedAt + 0.07d;
            yield return null;
        }

        private IEnumerator ReturnOpenSessionToWelcome(ScreenHost host)
        {
            yield return Click(host.ActiveWelcomeView
                .GetControlScreenBounds(WelcomeStageView.PlayControlId).center);
            yield return null;
            yield return Click(host.ActiveAvatarSelectView.GetControlScreenBounds(
                Exloris.Runtime.Avatar.AvatarSelectView.HomeControlId).center);
            yield return Click(host.ActiveLeaveConfirmationView.GetControlScreenBounds(
                Exloris.Runtime.Recovery.LeaveConfirmationView.LeaveControlId).center);
            yield return null;
            Assert.That(host.SessionOpen, Is.True);
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.WelcomeInputReady, Is.True);
        }
    }
}
