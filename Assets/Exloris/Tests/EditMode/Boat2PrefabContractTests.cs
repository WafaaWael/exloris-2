using Exloris.Editor;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Recovery;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat2PrefabContractTests
    {
        [Test]
        public void StageAndHelpPrefabsHaveDedicatedOwnersWithoutSelectableOrColliderObjects()
        {
            var stage = AssetDatabase.LoadAssetAtPath<GameObject>(Boat2PrefabGenerator.StagePath);
            var help = AssetDatabase.LoadAssetAtPath<GameObject>(Boat2PrefabGenerator.HelpPath);
            Assert.That(stage, Is.Not.Null);
            Assert.That(help, Is.Not.Null);
            Assert.That(stage.GetComponent<Boat2StageView>(), Is.Not.Null);
            Assert.That(help.GetComponent<Boat2HelpView>(), Is.Not.Null);
            Assert.That(stage.GetComponentsInChildren<Selectable>(true), Is.Empty);
            Assert.That(stage.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(stage.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            Assert.That(help.GetComponentsInChildren<Selectable>(true), Is.Empty);
        }
    }
}
