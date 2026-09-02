using System;
using System.IO;
using System.Linq;
using Exloris.Editor;
using Exloris.Runtime.Input;
using Exloris.Runtime.Stage1;
using Exloris.Runtime.Recovery;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage1PrefabContractTests
    {
        [Test]
        public void StageAndHelpPrefabsContainOnlyTheirFocusedRuntimeOwners()
        {
            var stage = AssetDatabase.LoadAssetAtPath<GameObject>(Stage1PrefabGenerator.StagePath);
            var help = AssetDatabase.LoadAssetAtPath<GameObject>(Stage1PrefabGenerator.HelpPath);
            Assert.That(stage, Is.Not.Null);
            Assert.That(help, Is.Not.Null);
            Assert.That(stage.GetComponent<Stage1StageView>(), Is.Not.Null);
            Assert.That(help.GetComponent<Stage1HelpView>(), Is.Not.Null);
            Assert.That(stage.GetComponents<Component>(), Has.Length.EqualTo(2));
            Assert.That(help.GetComponents<Component>(), Has.Length.EqualTo(2));
        }

        [Test]
        public void ApprovedContentHasThirtyNineStageCopiesAndOneSharedCaptionCatalogue()
        {
            var content = Stage1Content.LoadApproved();
            Assert.That(content.Backgrounds, Has.Length.EqualTo(9));
            Assert.That(content.Props, Has.Length.EqualTo(2));
            Assert.That(content.OutcomeLayers, Has.Length.EqualTo(3));
            Assert.That(content.ChildSprites, Has.Length.EqualTo(2));
            Assert.That(content.Controls, Has.Length.EqualTo(11));
            Assert.That(content.PresentationSprites, Has.Length.EqualTo(5));
            Assert.That(content.VoiceClips, Has.Length.EqualTo(3));
            Assert.That(content.AllReferences, Has.Length.EqualTo(40));
            Assert.That(content.AllReferences.Any(item => item == null), Is.False);
            Assert.That(content.AllReferences.Distinct().Count(), Is.EqualTo(40));

            var stageReferences = content.AllReferences
                .Where(item => item != content.CaptionTimings)
                .Select(AssetDatabase.GetAssetPath).ToArray();
            Assert.That(stageReferences, Has.Length.EqualTo(39));
            Assert.That(stageReferences.All(path => path.StartsWith(Stage1ContentGenerator.ImportedRoot + "/", StringComparison.Ordinal)), Is.True);
            Assert.That(AssetDatabase.GetAssetPath(content.CaptionTimings), Is.EqualTo("Assets/Exloris/Content/Imported/captions/caption-timings-en-v01.json"));
        }

        [Test]
        public void PrefabContainsOnlyTransparentSemanticTargetsAndRaycastFreeArt()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Stage1PrefabGenerator.StagePath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Selectable>(true), Is.Empty);
            var instance = UnityEngine.Object.Instantiate(prefab);
            var router = Router();
            try
            {
                var view = instance.GetComponent<Stage1StageView>();
                view.Configure(router, Stage1Content.LoadApproved(), false);
                Assert.That(view.ControlIds, Is.EquivalentTo(new[]
                {
                    Stage1StageView.MarkerControlId, Stage1StageView.SocketControlId,
                    Stage1StageView.PlayControlId, Stage1StageView.HelpControlId,
                    Stage1StageView.RepeatControlId, Stage1StageView.PauseControlId,
                    Stage1StageView.HomeControlId, Stage1StageView.UpControlId,
                    Stage1StageView.DownControlId, Stage1StageView.ConfirmControlId,
                    Stage1StageView.ContinueControlId, Stage1StageView.TryAgainControlId,
                    Stage1StageView.WatchAgainControlId
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
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Stage1PrefabGenerator.StagePath);
            var instance = UnityEngine.Object.Instantiate(prefab);
            var router = Router();
            try
            {
                var view = instance.GetComponent<Stage1StageView>();
                view.Configure(router, Stage1Content.LoadApproved(), false);
                foreach (var ratio in new[]
                {
                    new Rect(0, 40, 2048, 1456),
                    new Rect(40, 0, 2480, 1400),
                    new Rect(80, 0, 3296, 1400)
                })
                {
                    var screenWidth = ratio.width < 2400 ? 2048f : ratio.width < 3000 ? 2560f : 3456f;
                    var screenHeight = ratio.width < 2400 ? 1536f : 1440f;
                    Assert.That(view.TryApplyResponsiveLayout(screenWidth, screenHeight, ratio.x, ratio.y, ratio.width, ratio.height, 326, 88), Is.True);
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
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Stage1PrefabGenerator.StagePath);
            var boy = UnityEngine.Object.Instantiate(prefab); var girl = UnityEngine.Object.Instantiate(prefab);
            var boyRouter = Router(); var girlRouter = Router();
            try
            {
                var boyView = boy.GetComponent<Stage1StageView>(); var girlView = girl.GetComponent<Stage1StageView>();
                var content = Stage1Content.LoadApproved();
                boyView.Configure(boyRouter, content, false); girlView.Configure(girlRouter, content, true);
                boyView.Present(Snapshot(Stage1State.OutcomeReady, true, 0.5, Stage1OutcomeKind.InRange), false);
                girlView.Present(Snapshot(Stage1State.OutcomeReady, true, 0.5, Stage1OutcomeKind.InRange), false);
                Assert.That(boyView.SelectedChildCanvasRect, Is.EqualTo(girlView.SelectedChildCanvasRect));
                Assert.That(boyView.SelectedChildSprite, Is.Not.EqualTo(girlView.SelectedChildSprite));
                Assert.That(Path.GetFileName(AssetDatabase.GetAssetPath(boyView.SelectedChildSprite)), Is.EqualTo("chr-boy-stage-reaction-neutral.png"));
                Assert.That(Path.GetFileName(AssetDatabase.GetAssetPath(girlView.SelectedChildSprite)), Is.EqualTo("chr-girl-stage-reaction-neutral.png"));
            }
            finally
            {
                boyRouter.Dispose(); girlRouter.Dispose();
                UnityEngine.Object.DestroyImmediate(boy); UnityEngine.Object.DestroyImmediate(girl);
            }
        }

        [Test]
        public void ThreeSupportTiersPublishTheFrozenNonReadingVisualLadder()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Stage1PrefabGenerator.StagePath);
            var instance = UnityEngine.Object.Instantiate(prefab);
            var router = Router();
            try
            {
                var view = instance.GetComponent<Stage1StageView>();
                view.Configure(router, Stage1Content.LoadApproved(), false);

                view.Present(Snapshot(Stage1State.Playing, true, 0.0, Stage1OutcomeKind.None, 0), false);
                Assert.That(view.SupportCueVisible, Is.False);
                Assert.That(view.RelationshipHighlightVisible, Is.False);

                view.Present(Snapshot(Stage1State.Playing, true, 0.0, Stage1OutcomeKind.None, 1), false);
                Assert.That(view.SupportCueVisible, Is.True);
                Assert.That(view.RelationshipHighlightVisible, Is.False);

                view.Present(Snapshot(Stage1State.Playing, true, 0.0, Stage1OutcomeKind.None, 2), false);
                Assert.That(view.SupportCueVisible, Is.True);
                Assert.That(view.RelationshipHighlightVisible, Is.True);

                view.Present(Snapshot(Stage1State.Playing, true, 0.0, Stage1OutcomeKind.None, 3), false);
                Assert.That(view.SupportCueVisible, Is.True);
                Assert.That(view.RelationshipHighlightVisible, Is.True);
                Assert.That(view.SupportCueStrength, Is.GreaterThan(view.RelationshipHighlightStrength));
                Assert.That(view.VisibleArtRaycastsDisabled(), Is.True);
            }
            finally { router.Dispose(); UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static Stage1Snapshot[] StateSnapshots() => new[]
        {
            Snapshot(Stage1State.Intro, false, 0.5, Stage1OutcomeKind.None),
            Snapshot(Stage1State.Playing, false, 0.5, Stage1OutcomeKind.None),
            Snapshot(Stage1State.Playing, true, 0.25, Stage1OutcomeKind.None),
            Snapshot(Stage1State.OutcomePlaying, true, 0.1, Stage1OutcomeKind.OutsideLow),
            Snapshot(Stage1State.OutcomeReady, true, 0.1, Stage1OutcomeKind.OutsideLow),
            Snapshot(Stage1State.OutcomeReady, true, 0.5, Stage1OutcomeKind.InRange),
            Snapshot(Stage1State.OutcomeReady, true, 0.9, Stage1OutcomeKind.OutsideHigh)
        };

        private static Stage1Snapshot Snapshot(Stage1State state, bool attached, double position, Stage1OutcomeKind outcome, int supportTier = 0) =>
            new Stage1Snapshot(Guid.Parse("10000000-0000-0000-0000-000000000001"), state, attached, position, supportTier, 1, outcome,
                Guid.Parse("20000000-0000-0000-0000-000000000001"), Guid.Parse("30000000-0000-0000-0000-000000000001"),
                0, state == Stage1State.OutcomeReady ? 3.2 : 0, null, state == Stage1State.OutcomeReady, false);

        private static ChildInputRouter Router() => new ChildInputRouter(
            new InputAction("Point", InputActionType.PassThrough), new InputAction("Press", InputActionType.Button));
    }
}
