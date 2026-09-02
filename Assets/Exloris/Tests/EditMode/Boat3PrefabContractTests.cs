using Exloris.Editor;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Recovery;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat3PrefabContractTests
    {
        [Test]
        public void StageAndHelpPrefabsHaveDedicatedOwnersWithoutSelectableOrColliderObjects()
        {
            var stage = AssetDatabase.LoadAssetAtPath<GameObject>(Boat3PrefabGenerator.StagePath);
            var help = AssetDatabase.LoadAssetAtPath<GameObject>(Boat3PrefabGenerator.HelpPath);
            Assert.That(stage, Is.Not.Null);
            Assert.That(help, Is.Not.Null);
            Assert.That(stage.GetComponent<Boat3StageView>(), Is.Not.Null);
            Assert.That(help.GetComponent<Boat3HelpView>(), Is.Not.Null);
            Assert.That(stage.GetComponentsInChildren<Selectable>(true), Is.Empty);
            Assert.That(stage.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(stage.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            Assert.That(help.GetComponentsInChildren<Selectable>(true), Is.Empty);
        }
    }
}
