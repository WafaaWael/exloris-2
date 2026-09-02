using System;
using System.IO;
using System.Linq;
using Exloris.Editor;
using Exloris.Runtime.Input;
using Exloris.Runtime.Stage2;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage2PrefabContractTests
    {
        [Test]
        public void ApprovedContentHasOnlyFrozenStageOwnedAndSharedReferences()
        {
            var content = Stage2Content.LoadApproved();
            Assert.That(content.Backgrounds, Has.Length.EqualTo(9));
            Assert.That(content.Stones, Has.Length.EqualTo(3));
            Assert.That(content.Hotspots, Has.Length.EqualTo(3));
            Assert.That(content.OutcomeLayers, Has.Length.EqualTo(9));
            Assert.That(content.ChildSprites, Has.Length.EqualTo(2));
            Assert.That(content.Controls, Has.Length.EqualTo(7));
            Assert.That(content.PresentationSprites, Has.Length.EqualTo(4));
            Assert.That(content.VoiceClips, Has.Length.EqualTo(2));
            Assert.That(content.StoneSfx, Has.Length.EqualTo(3));
            Assert.That(content.AllReferences, Has.Length.EqualTo(46));
            Assert.That(content.AllReferences.Any(item => item == null), Is.False);
            Assert.That(content.AllReferences.Distinct().Count(), Is.EqualTo(46));

            var stageReferences = content.AllReferences
                .Where(item => item != content.CaptionTimings)
                .Select(AssetDatabase.GetAssetPath).ToArray();
            Assert.That(stageReferences, Has.Length.EqualTo(45));
            Assert.That(stageReferences.All(path => path.StartsWith(
                Stage2ContentGenerator.ImportedRoot + "/", StringComparison.Ordinal)), Is.True);
            Assert.That(AssetDatabase.GetAssetPath(content.CaptionTimings),
                Is.EqualTo("Assets/Exloris/Content/Imported/captions/caption-timings-en-v01.json"));
        }

        [Test]
        public void PrefabOwnsOneViewAndTransparentSemanticTargetsOnly()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Stage2PrefabGenerator.StagePath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<Stage2StageView>(), Is.Not.Null);
            Assert.That(prefab.GetComponents<Component>(), Has.Length.EqualTo(2));
            Assert.That(prefab.GetComponentsInChildren<Selectable>(true), Is.Empty);

            var instance = UnityEngine.Object.Instantiate(prefab);
            var router = Router();
            try
            {
                var view = instance.GetComponent<Stage2StageView>();
                view.Configure(router, Stage2Content.LoadApproved(), false);
                Assert.That(view.ControlIds, Is.EquivalentTo(new[]
                {
                    Stage2StageView.LowControlId, Stage2StageView.MiddleControlId,
                    Stage2StageView.HighControlId, Stage2StageView.FieldControlId,
                    Stage2StageView.PathControlId, Stage2StageView.BoatControlId,
                    Stage2StageView.PlayControlId, Stage2StageView.HelpControlId,
                    Stage2StageView.RepeatControlId, Stage2StageView.PauseControlId,
                    Stage2StageView.HomeControlId, Stage2StageView.CompareControlId,
                    Stage2StageView.FinishControlId
                }));
                Assert.That(view.SemanticControlCount, Is.EqualTo(13));
                Assert.That(view.DisplaySemanticControlCount, Is.Zero);
                Assert.That(view.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(view.GetComponentsInChildren<Collider2D>(true), Is.Empty);
                Assert.That(view.AreTouchTargetsTransparent(), Is.True);
                Assert.That(view.VisibleArtRaycastsDisabled(), Is.True);
                Assert.That(view.CaptionTextColor, Is.EqualTo(new Color32(61, 43, 29, 255)));
            }
            finally { router.Dispose(); UnityEngine.Object.DestroyImmediate(instance); }
        }

        [Test]
        public void StateSpecificControlsNeverOverlapAndStayInsideEverySafeArea()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Stage2PrefabGenerator.StagePath);
            var instance = UnityEngine.Object.Instantiate(prefab);
            var router = Router();
            try
            {
                var view = instance.GetComponent<Stage2StageView>();
                view.Configure(router, Stage2Content.LoadApproved(), false);
                foreach (var ratio in new[]
                {
                    new Rect(0, 40, 2048, 1456),
                    new Rect(40, 0, 2480, 1400),
                    new Rect(80, 0, 3296, 1400)
                })
                {
                    var screenWidth = ratio.width < 2400 ? 2048f : ratio.width < 3000 ? 2560f : 3456f;
                    var screenHeight = ratio.width < 2400 ? 1536f : 1440f;
                    Assert.That(view.TryApplyResponsiveLayout(
                        screenWidth, screenHeight, ratio.x, ratio.y, ratio.width, ratio.height, 326, 88), Is.True);
                    foreach (var snapshot in StateSnapshots())
                    {
                        view.Present(snapshot, false);
                        view.SetInputReady(true);
                        Assert.That(view.DoEnabledTouchTargetsOverlap(), Is.False, snapshot.State.ToString());
                        Assert.That(view.AreAllEnabledTargetsInsideSafeArea(), Is.True, snapshot.State.ToString());
                        Assert.That(view.VisibleArtContainedByTargets(), Is.True, snapshot.State.ToString());
                    }
                }
            }
            finally { router.Dispose(); UnityEngine.Object.DestroyImmediate(instance); }
        }

        [Test]
        public void BoyAndGirlUseEqualGeometryAndDifferentApprovedSprites()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Stage2PrefabGenerator.StagePath);
            var boy = UnityEngine.Object.Instantiate(prefab);
            var girl = UnityEngine.Object.Instantiate(prefab);
            var boyRouter = Router(); var girlRouter = Router();
            try
            {
                var content = Stage2Content.LoadApproved();
                var boyView = boy.GetComponent<Stage2StageView>();
                var girlView = girl.GetComponent<Stage2StageView>();
                boyView.Configure(boyRouter, content, false);
                girlView.Configure(girlRouter, content, true);
                var snapshot = Snapshot(Stage2State.Inspecting, Stage2River.Middle, Stage2Hotspot.Field,
                    Stage2ObservedHotspots.Field);
                boyView.Present(snapshot, false); girlView.Present(snapshot, false);
                Assert.That(boyView.SelectedChildCanvasRect, Is.EqualTo(girlView.SelectedChildCanvasRect));
                Assert.That(boyView.SelectedChildSprite, Is.Not.EqualTo(girlView.SelectedChildSprite));
                Assert.That(Path.GetFileName(AssetDatabase.GetAssetPath(boyView.SelectedChildSprite)),
                    Is.EqualTo("chr-boy-stage-reaction-neutral.png"));
                Assert.That(Path.GetFileName(AssetDatabase.GetAssetPath(girlView.SelectedChildSprite)),
                    Is.EqualTo("chr-girl-stage-reaction-neutral.png"));
            }
            finally
            {
                boyRouter.Dispose(); girlRouter.Dispose();
                UnityEngine.Object.DestroyImmediate(boy); UnityEngine.Object.DestroyImmediate(girl);
            }
        }

        private static Stage2Snapshot[] StateSnapshots() => new[]
        {
            Snapshot(Stage2State.Intro),
            Snapshot(Stage2State.Choosing),
            Snapshot(Stage2State.Inspecting, Stage2River.Low),
            Snapshot(Stage2State.ConsequencePlaying, Stage2River.Middle, Stage2Hotspot.Path),
            Snapshot(Stage2State.Inspecting, Stage2River.High, Stage2Hotspot.Boat, Stage2ObservedHotspots.All)
        };

        private static Stage2Snapshot Snapshot(
            Stage2State state, Stage2River river = Stage2River.None,
            Stage2Hotspot hotspot = Stage2Hotspot.None,
            Stage2ObservedHotspots observed = Stage2ObservedHotspots.None) =>
            new Stage2Snapshot(
                Guid.Parse("10000000-0000-0000-0000-000000000001"), state, river,
                river == Stage2River.Low ? observed : Stage2ObservedHotspots.None,
                river == Stage2River.Middle ? observed : Stage2ObservedHotspots.None,
                river == Stage2River.High ? observed : Stage2ObservedHotspots.None,
                hotspot,
                Guid.Parse("20000000-0000-0000-0000-000000000001"),
                Guid.Parse("30000000-0000-0000-0000-000000000001"),
                0, state == Stage2State.ConsequencePlaying ? 1.2 : 0, null, 0, false);

        private static ChildInputRouter Router() => new ChildInputRouter(
            new InputAction("Point", InputActionType.PassThrough),
            new InputAction("Press", InputActionType.Button));
    }
}
