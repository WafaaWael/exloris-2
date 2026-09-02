using System.Collections;
using System.Linq;
using Exloris.Runtime.Bootstrap;
using Exloris.Runtime.Layout;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Exloris.Tests.PlayMode
{
    public sealed class ReleaseCandidateStartupTests
    {
        [UnityTest]
        public IEnumerator StartupLoadingUsesTheApprovedResponsiveBg01Artwork()
        {
            if (PersistentCompositionRoot.Instance != null)
            {
                Object.Destroy(PersistentCompositionRoot.Instance.gameObject);
                yield return null;
            }
            SceneManager.LoadScene(0, LoadSceneMode.Single);
            yield return null;
            yield return null;

            var background = Object.FindObjectsByType<Image>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Single(image => image.gameObject.name == "TechnicalLoadingBackground");
            Assert.That(background.sprite, Is.Not.Null);
            Assert.That(background.GetComponent<ResponsiveBackgroundPresenter>(), Is.Not.Null);
        }

        [TearDown]
        public void TearDown()
        {
            if (PersistentCompositionRoot.Instance != null)
                Object.DestroyImmediate(PersistentCompositionRoot.Instance.gameObject);
        }
    }
}
