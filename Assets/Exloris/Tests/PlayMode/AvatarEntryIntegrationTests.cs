using System.Collections;
using Exloris.Core.Navigation;
using Exloris.Runtime.Avatar;
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
    public sealed class AvatarEntryIntegrationTests : InputTestFixture
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
        public IEnumerator WelcomeTransactionCreatesOnePreparedSelectAndRemovesWelcomeOwnership()
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();
            var root = PersistentCompositionRoot.Instance;
            host.ConfigureAvatarInitialLeftForTests(AvatarChoice.Girl);

            yield return Click(host.ActiveWelcomeView
                .GetControlScreenBounds(WelcomeStageView.PlayControlId).center);

            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.AvatarSelect));
            Assert.That(host.ActiveAvatarSelectView, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<AvatarSelectView>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<WelcomeStageView>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindObjectsByType<SessionChoiceView>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(host.ActiveWelcomeView, Is.Null);
            Assert.That(host.WelcomeMediaActive, Is.False);
            Assert.That(host.SessionOpen, Is.True);
            Assert.That(host.SessionLocation, Is.EqualTo(RouteId.AvatarSelect));
            Assert.That(host.AvatarLeftChoice, Is.EqualTo(AvatarChoice.Girl));
            Assert.That(host.AvatarRightChoice, Is.EqualTo(AvatarChoice.Boy));
            Assert.That(PersistentCompositionRoot.Instance, Is.SameAs(root));
            Assert.That(root.GetComponentInChildren<ScreenHost>(true), Is.SameAs(host));
            Assert.That(host.AccessibilityCoordinatorConnected, Is.True);
            Assert.That(host.AvatarInputReady, Is.False,
                "Input must wait one complete visible frame.");

            yield return null;
            Assert.That(host.AvatarInputReady, Is.True);
            var completedId = host.LastCompletedAvatarEntryTransactionId.Value;
            Assert.That(host.TryCompleteAvatarEntryForTests(completedId), Is.False);
            Assert.That(Object.FindObjectsByType<AvatarSelectView>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
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
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.WelcomeInputReady, Is.True);
        }

        private IEnumerator Click(Vector2 position)
        {
            var startedAt = currentTime + 0.01d;
            Set(_mouse.position, position, startedAt);
            Press(_mouse.leftButton, startedAt + 0.01d);
            Release(_mouse.leftButton, startedAt + 0.06d);
            currentTime = startedAt + 0.07d;
            yield return null;
        }
    }
}
