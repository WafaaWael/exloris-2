using Exloris.Runtime.Intro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class StartupIntroPrefabContractTests
    {
        [Test]
        public void StartupAndIntroPrefabsExistAtFrozenPathsWithoutWelcomeImplementation()
        {
            var startup = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Exloris/UI/Screens/StartupLoading.prefab");
            var intro = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Exloris/UI/Screens/IntroVideo.prefab");

            Assert.That(startup, Is.Not.Null);
            Assert.That(startup.GetComponentInChildren<Canvas>(true), Is.Not.Null);
            Assert.That(startup.GetComponentsInChildren<Selectable>(true), Is.Empty);
            Assert.That(intro, Is.Not.Null);
            Assert.That(intro.GetComponent<IntroStageView>(), Is.Not.Null);
            Assert.That(intro.GetComponentsInChildren<Selectable>(true), Is.Empty);
            Assert.That(intro.transform.Find("Welcome"), Is.Null);
        }
    }
}
