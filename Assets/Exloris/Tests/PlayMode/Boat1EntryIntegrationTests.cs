using System.Collections;
using Exloris.Core.Navigation;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Bootstrap;
using Exloris.Runtime.Overview;
using Exloris.Runtime.UI;
using Exloris.Runtime.Welcome;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class Boat1EntryIntegrationTests : InputTestFixture
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
        public IEnumerator JourneySkipCreatesExactlyOneBoatAndPreservesSessionChoice()
        {
            var host = default(ScreenHost);
            yield return EnterJourney(result => host = result);
            var root = PersistentCompositionRoot.Instance;

            yield return Click(host.ActiveJourneyOverviewView
                .GetControlScreenBounds(JourneyOverviewStageView.SkipControlId).center);

            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Boat1));
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.Boat1));
            Assert.That(host.SessionSelectedAvatarId, Is.EqualTo("avatar_girl"));
            Assert.That(host.ActiveJourneyOverviewView, Is.Null);
            Assert.That(host.ActiveBoat1View, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<Boat1StageView>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(PersistentCompositionRoot.Instance, Is.SameAs(root));
            Assert.That(host.Boat1InputReady, Is.False,
                "Boat 1 input must wait one complete visible frame.");

            yield return null;
            Assert.That(host.Boat1InputReady, Is.True);
            Assert.That(host.Boat1Snapshot.State, Is.EqualTo(Boat1State.Choice));

            var completed = host.LastCompletedBoat1EntryTransactionId.Value;
            Assert.That(host.TryCompleteBoat1EntryForTests(completed), Is.False);
            Assert.That(host.TryCompleteBoat1EntryForTests(System.Guid.NewGuid()), Is.False);
            Assert.That(Object.FindObjectsByType<Boat1StageView>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
        }

        private IEnumerator EnterJourney(System.Action<ScreenHost> capture)
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();
            host.ConfigureAvatarInitialLeftForTests(AvatarChoice.Girl);
            yield return Click(host.ActiveWelcomeView
                .GetControlScreenBounds(WelcomeStageView.PlayControlId).center);
            yield return null;
            yield return Click(host.ActiveAvatarSelectView
                .GetControlScreenBounds(AvatarSelectView.GirlControlId).center);
            yield return null;
            yield return Click(host.ActiveAvatarPreviewView
                .GetControlScreenBounds(AvatarPreviewView.ConfirmControlId).center);
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
            Assert.That(host.WelcomeInputReady, Is.True);
        }

        private IEnumerator Click(Vector2 position)
        {
            var startedAt = currentTime + 0.30d;
            Set(_mouse.position, position, startedAt);
            Press(_mouse.leftButton, startedAt + 0.01d);
            Release(_mouse.leftButton, startedAt + 0.06d);
            currentTime = startedAt + 0.07d;
            yield return null;
        }
    }
}
