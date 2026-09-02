using System.Collections;
using Exloris.Runtime.Bootstrap;
using Exloris.Runtime.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class BootstrapSceneLoadTests
    {
        [UnityTest]
        public IEnumerator BootstrapPersistsWhileFoundationHarnessLoadsAdditively()
        {
            SceneManager.LoadScene(0, LoadSceneMode.Single);
            yield return null;
            yield return null;

            Assert.That(SceneManager.GetSceneByBuildIndex(0).isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByBuildIndex(1).isLoaded, Is.True);
            var hosts = Object.FindObjectsByType<ScreenHost>(FindObjectsSortMode.None);
            Assert.That(hosts, Has.Length.EqualTo(1));
            Assert.That(hosts[0].gameObject.scene.buildIndex, Is.EqualTo(-1));
            Assert.That(hosts[0].gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
            Assert.That(Application.targetFrameRate, Is.EqualTo(60));
            Assert.That(QualitySettings.vSyncCount, Is.Zero);
            Assert.That(Bootstrapper.StartupOrientation, Is.EqualTo(ScreenOrientation.AutoRotation));
            Assert.That(Screen.autorotateToLandscapeLeft, Is.True);
            Assert.That(Screen.autorotateToLandscapeRight, Is.True);
            Assert.That(Screen.autorotateToPortrait, Is.False);
            Assert.That(Screen.autorotateToPortraitUpsideDown, Is.False);
        }

        [UnityTest]
        public IEnumerator CompositionRootSurvivesSingleSceneReplacementAndRejectsDuplicateBootstrap()
        {
            SceneManager.LoadScene(0, LoadSceneMode.Single);
            yield return null;
            yield return null;

            var originalRoot = GameObject.Find("ExlorisCompositionRoot");
            Assert.That(originalRoot, Is.Not.Null);
            Assert.That(originalRoot.GetComponentInChildren<ScreenHost>(true), Is.Not.Null);

            SceneManager.LoadScene(1, LoadSceneMode.Single);
            yield return null;

            Assert.That(GameObject.Find("ExlorisCompositionRoot"), Is.SameAs(originalRoot));
            Assert.That(Object.FindObjectsByType<ScreenHost>(FindObjectsSortMode.None), Has.Length.EqualTo(1));

            SceneManager.LoadScene(0, LoadSceneMode.Additive);
            yield return null;
            yield return null;

            Assert.That(Object.FindObjectsByType<ScreenHost>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(GameObject.Find("ExlorisCompositionRoot"), Is.SameAs(originalRoot));
        }

        [UnityTest]
        public IEnumerator ShippedColdStartReachesPreparedIntroWithSkipOnly()
        {
            SceneManager.LoadScene(0, LoadSceneMode.Single);
            yield return null;
            var host = Object.FindFirstObjectByType<ScreenHost>();
            Assert.That(host, Is.Not.Null);
            Assert.That(host.StartupIntroEnabled, Is.True);

            var deadline = Time.realtimeSinceStartup + 8f;
            while (host.CurrentRoute == Exloris.Core.Navigation.RouteId.StartupLoading &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(host.CurrentRoute, Is.EqualTo(Exloris.Core.Navigation.RouteId.IntroVideo));
            Assert.That(host.ActiveIntroView, Is.Not.Null);
            Assert.That(host.ActiveIntroView.gameObject.name, Is.EqualTo("IntroVideo(Clone)"));
            CollectionAssert.AreEqual(
                new[] { Exloris.Runtime.Intro.IntroFirstFramePresentation.SkipControlId },
                host.ActiveIntroView.ChildFacingControlIds);
            Assert.That(host.IntroFirstChildFrameElapsedSeconds, Is.GreaterThanOrEqualTo(0d));
            Assert.That(host.IntroFirstChildFrameElapsedSeconds, Is.LessThanOrEqualTo(5.0d));
        }

        [UnityTest]
        public IEnumerator ShippedIntroInterruptionNeverResumesUntilMappedAction()
        {
            SceneManager.LoadScene(0, LoadSceneMode.Single);
            yield return null;
            var host = Object.FindFirstObjectByType<ScreenHost>();
            var deadline = Time.realtimeSinceStartup + 8f;
            while (host.CurrentRoute == Exloris.Core.Navigation.RouteId.StartupLoading &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(host.CurrentRoute, Is.EqualTo(Exloris.Core.Navigation.RouteId.IntroVideo));

            host.HandleIntroInterruptionForTests();
            host.HandleFocusRestoredForTests();

            Assert.That(host.IntroResumePending, Is.True);
            Assert.That(host.ActiveIntroView.ResumeVisible, Is.True);
            Assert.That(host.CurrentRoute, Is.EqualTo(Exloris.Core.Navigation.RouteId.IntroVideo));

            Assert.That(host.ResumeIntroAfterMappedUserActionForTests(), Is.True);
            Assert.That(host.IntroResumePending, Is.False);
            Assert.That(host.ActiveIntroView.ResumeVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator ShippedReducedIntroPrepares120FrameVariantAndSkipStopsAtWelcomeBoundary()
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
            while (host.CurrentRoute == Exloris.Core.Navigation.RouteId.StartupLoading &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(host.CurrentRoute, Is.EqualTo(Exloris.Core.Navigation.RouteId.IntroVideo));
            Assert.That(host.PreparedIntroFrameCount, Is.EqualTo(120));
            var oldToken = host.ActiveIntroToken.Value;
            Assert.That(host.SkipIntroForTests(), Is.True);
            yield return null;
            Assert.That(host.CurrentRoute, Is.EqualTo(Exloris.Core.Navigation.RouteId.Welcome));
            Assert.That(host.ActiveIntroView, Is.Null);
            Assert.That(host.ActiveWelcomeView, Is.Not.Null);
            Assert.That(host.HandleStaleIntroCompletionForTests(oldToken), Is.False);
        }
    }
}
