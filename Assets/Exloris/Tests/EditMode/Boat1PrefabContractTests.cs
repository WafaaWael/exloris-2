using System;
using System.Linq;
using Exloris.Editor;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Recovery;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat1PrefabContractTests
    {
        [Test]
        public void PrefabsContainExactlyEightStageControlsAndOneHelpExitWithInertArt()
        {
            var stagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Boat1PrefabGenerator.StagePath);
            var helpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Boat1PrefabGenerator.HelpPath);
            Assert.That(stagePrefab, Is.Not.Null); Assert.That(helpPrefab, Is.Not.Null);
            Assert.That(stagePrefab.GetComponentsInChildren<Selectable>(true), Is.Empty);
            var stage = UnityEngine.Object.Instantiate(stagePrefab); var help = UnityEngine.Object.Instantiate(helpPrefab);
            var router = Router();
            try
            {
                var content = Boat1Content.LoadApproved(); var view = stage.GetComponent<Boat1StageView>(); view.Configure(router, content);
                Assert.That(view.ControlIds, Is.EquivalentTo(new[] { Boat1StageView.ReedsControlId, Boat1StageView.FollowIbisControlId,
                    Boat1StageView.VillageControlId, Boat1StageView.HelpControlId, Boat1StageView.RepeatControlId, Boat1StageView.SkipControlId,
                    Boat1StageView.PauseControlId, Boat1StageView.HomeControlId }));
                Assert.That(view.SemanticControlCount, Is.EqualTo(8)); Assert.That(view.DisplaySemanticControlCount, Is.Zero);
                Assert.That(view.GetComponentsInChildren<Collider>(true), Is.Empty); Assert.That(view.GetComponentsInChildren<Collider2D>(true), Is.Empty);
                Assert.That(view.AreTouchTargetsTransparent(), Is.True); Assert.That(view.VisibleArtRaycastsDisabled(), Is.True);
                Assert.That(view.CaptionTextColor, Is.EqualTo(new Color32(61, 43, 29, 255)),
                    "Boat 1 caption text must use the approved dark brown on its light cream backplate.");
                var helpView = help.GetComponent<Boat1HelpView>(); helpView.Configure(router, content);
                Assert.That(helpView.ControlIds, Is.EqualTo(new[] { Boat1HelpView.BackControlId })); Assert.That(helpView.DisplaySemanticControlCount, Is.Zero);
                Assert.That(helpView.DisplayArtRaycastsDisabled(), Is.True);
            }
            finally { router.Dispose(); UnityEngine.Object.DestroyImmediate(stage); UnityEngine.Object.DestroyImmediate(help); }
        }

        [Test]
        public void InterruptedHelpRemainsInputLockedAcrossPauseHelpReshow()
        {
            var helpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Boat1PrefabGenerator.HelpPath);
            var help = UnityEngine.Object.Instantiate(helpPrefab);
            var router = Router();
            try
            {
                var view = help.GetComponent<Boat1HelpView>();
                view.Configure(router, Boat1Content.LoadApproved());
                view.Show(default);
                view.SetInterruptionBlocked(true);
                view.Show(default);
                Assert.That(view.BackInputEnabled, Is.False,
                    "Pause Help must not re-enable the captured direct Help caller.");
                view.SetInterruptionBlocked(false);
                Assert.That(view.BackInputEnabled, Is.True);
            }
            finally { router.Dispose(); UnityEngine.Object.DestroyImmediate(help); }
        }

        private static Exloris.Runtime.Input.ChildInputRouter Router() => new Exloris.Runtime.Input.ChildInputRouter(
            new InputAction("Point", InputActionType.PassThrough), new InputAction("Press", InputActionType.Button));
    }
}
