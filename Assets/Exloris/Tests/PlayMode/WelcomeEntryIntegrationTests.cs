using System.Collections;
using Exloris.Core.Navigation;
using Exloris.Runtime.Bootstrap;
using Exloris.Runtime.Intro;
using Exloris.Runtime.UI;
using Exloris.Runtime.Welcome;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class WelcomeEntryIntegrationTests
    {
        [UnityTest]
        public IEnumerator IntroSkipCreatesExactlyOneWelcomeAndRejectsStaleCompletion()
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
            var oldToken = host.ActiveIntroToken.Value;
            var root = PersistentCompositionRoot.Instance;
            Assert.That(host.SkipIntroForTests(), Is.True);
            yield return null;

            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.ActiveWelcomeView, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<WelcomeStageView>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<IntroStageView>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(PersistentCompositionRoot.Instance, Is.SameAs(root));
            Assert.That(root.GetComponentInChildren<ScreenHost>(true), Is.SameAs(host));
            Assert.That(host.HandleStaleIntroCompletionForTests(oldToken), Is.False);
            Assert.That(Object.FindObjectsByType<WelcomeStageView>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
        }

        [UnityTest]
        public IEnumerator IntroFailureConsumesTokenAndCreatesExactlyOneWelcomeWithoutRecoveryOverlay()
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

            Assert.That(host.FailIntroForTests(), Is.True);
            yield return null;

            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.ActiveWelcomeView, Is.Not.Null);
            Assert.That(host.RecoveryVisible, Is.False);
            Assert.That(Object.FindObjectsByType<WelcomeStageView>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator WelcomeInterruptionPausesInputAndMediaUntilMappedResume()
        {
            yield return LoadFreshWelcome();
            var host = Object.FindFirstObjectByType<ScreenHost>();

            Assert.That(host.HandleWelcomeInterruptionForTests(), Is.True);
            Assert.That(host.WelcomeInterruptionResumePending, Is.True);
            Assert.That(host.ActiveWelcomeView.TechnicalOverlayState,
                Is.EqualTo(WelcomeTechnicalOverlayState.Paused));
            Assert.That(host.ActiveWelcomeView.NormalControlsEnabled, Is.False);
            Assert.That(host.InputsActive, Is.True);

            host.HandleFocusRestoredForTests();
            Assert.That(host.WelcomeInterruptionResumePending, Is.True);
            Assert.That(host.ResumeWelcomeAfterMappedUserActionForTests(), Is.True);
            Assert.That(host.WelcomeInterruptionResumePending, Is.False);
            Assert.That(host.ActiveWelcomeView.TechnicalOverlayState,
                Is.EqualTo(WelcomeTechnicalOverlayState.Hidden));
            Assert.That(host.ActiveWelcomeView.NormalControlsEnabled, Is.True);
            Assert.That(host.WelcomeInputReady, Is.True);
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
            {
                yield return null;
            }

            Assert.That(host.SkipIntroForTests(), Is.True);
            yield return null;
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(host.WelcomeInputReady, Is.True);
        }
    }
}
