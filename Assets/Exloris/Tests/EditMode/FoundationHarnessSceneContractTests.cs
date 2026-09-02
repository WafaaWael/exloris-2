using System.Linq;
using Exloris.Runtime.Bootstrap;
using Exloris.Runtime.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exloris.Tests.EditMode
{
    public sealed class FoundationHarnessSceneContractTests
    {
        [Test]
        public void BuildSettingsContainOnlyBootstrapAndFoundationHarness()
        {
            var enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();

            Assert.That(enabled, Is.EqualTo(new[]
            {
                "Assets/Exloris/Scenes/Bootstrap.unity",
                "Assets/Exloris/Scenes/FoundationHarness.unity"
            }));
        }

        [Test]
        public void HarnessScenesAndRuntimeTypesExist()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Exloris/Scenes/Bootstrap.unity"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Exloris/Scenes/FoundationHarness.unity"), Is.Not.Null);
            Assert.That(typeof(Bootstrapper), Is.Not.Null);
            Assert.That(typeof(ScreenHost), Is.Not.Null);
            Assert.That(Bootstrapper.HarnessSceneBuildIndex, Is.EqualTo(1));
        }

        [Test]
        public void FoundationHarnessHasAnEnabledRenderCamera()
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/Exloris/Scenes/FoundationHarness.unity",
                OpenSceneMode.Single);
            var cameras = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .ToArray();

            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(cameras[0].enabled, Is.True);
            Assert.That(cameras[0].gameObject.name, Is.EqualTo("FoundationCamera"));
        }
    }
}
