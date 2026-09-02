using System.Linq;
using Exloris.Runtime.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class RecoveryPrefabContractTests
    {
        [Test]
        public void LoadingOverlayIsOpaqueToInputAndUsesARealSemanticGraphic()
        {
            var prefab = Load("LoadingOverlay");

            Assert.That(prefab.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
            Assert.That(prefab.GetComponent<Image>().raycastTarget, Is.True);
            var icon = prefab.GetComponentInChildren<SemanticIconGraphic>(true);
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.IconKind, Is.EqualTo(SemanticIconKind.Loading));
            Assert.That(icon.raycastTarget, Is.False);
        }

        [Test]
        public void RecoveryOverlayHasOnlyRetryAndHomeActionsAtTargetFloor()
        {
            var prefab = Load("RecoveryOverlay");
            var buttons = prefab.GetComponentsInChildren<Button>(true);

            Assert.That(buttons.Select(button => button.name), Is.EquivalentTo(new[] { "Retry", "Home" }));
            foreach (var button in buttons)
            {
                Assert.That(button.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(224f, 224f)));
                Assert.That(button.GetComponentInChildren<SemanticIconGraphic>(true), Is.Not.Null);
            }
        }

        [Test]
        public void OverlayPrefabsContainNoBakedText()
        {
            foreach (var name in new[] { "LoadingOverlay", "RecoveryOverlay" })
            {
                var prefab = Load(name);
                Assert.That(prefab.GetComponentsInChildren<Text>(true), Is.Empty);
            }
        }

        private static GameObject Load(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Exloris/UI/Overlays/{name}.prefab");
            Assert.That(prefab, Is.Not.Null, $"Missing generated prefab {name}");
            return prefab;
        }
    }
}
