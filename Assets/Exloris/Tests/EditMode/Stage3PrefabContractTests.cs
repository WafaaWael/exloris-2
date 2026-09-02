using System;
using System.Linq;
using Exloris.Editor;
using Exloris.Runtime.Input;
using Exloris.Runtime.Recovery;
using Exloris.Runtime.Stage2;
using Exloris.Runtime.Stage3;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage3PrefabContractTests
    {
        [Test]
        public void PrefabsOwnOnlyTheirRequiredRuntimeComponents()
        {
            var stage = AssetDatabase.LoadAssetAtPath<GameObject>(Stage3PrefabGenerator.StagePath);
            var help = AssetDatabase.LoadAssetAtPath<GameObject>(Stage3PrefabGenerator.HelpPath);
            Assert.That(stage, Is.Not.Null);
            Assert.That(help, Is.Not.Null);
            Assert.That(stage.GetComponent<Stage3StageView>(), Is.Not.Null);
            Assert.That(help.GetComponent<Stage3HelpView>(), Is.Not.Null);
            Assert.That(stage.GetComponents<Component>(), Has.Length.EqualTo(2));
            Assert.That(help.GetComponents<Component>(), Has.Length.EqualTo(2));
        }

        [Test]
        public void StageUsesTransparentSemanticTargetsAndInertVisibleArt()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Stage3PrefabGenerator.StagePath);
            var instance = UnityEngine.Object.Instantiate(prefab);
            var router = Router();
            try
            {
                var view = instance.GetComponent<Stage3StageView>();
                view.Configure(router, Stage3Content.LoadApproved(), false);
                Assert.That(view.SemanticControlCount, Is.EqualTo(16));
                Assert.That(view.DisplaySemanticControlCount, Is.Zero);
                Assert.That(view.ControlIds, Is.EquivalentTo(new[]
                {
                    Stage3StageView.GrainControlId, Stage3StageView.ChannelControlId,
                    Stage3StageView.BoatPlanControlId, Stage3StageView.LowControlId,
                    Stage3StageView.MiddleControlId, Stage3StageView.HighControlId,
                    Stage3StageView.PlayControlId, Stage3StageView.HelpControlId,
                    Stage3StageView.RepeatControlId, Stage3StageView.PauseControlId,
                    Stage3StageView.HomeControlId, Stage3StageView.FinishControlId,
                    Stage3StageView.AnotherPlanControlId, Stage3StageView.AnotherRiverControlId,
                    Stage3StageView.WatchAgainControlId, Stage3StageView.RiverBackControlId
                }));
                Assert.That(view.GetComponentsInChildren<Selectable>(true), Is.Empty);
                Assert.That(view.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(view.GetComponentsInChildren<Collider2D>(true), Is.Empty);
                Assert.That(view.AreTouchTargetsTransparent(), Is.True);
                Assert.That(view.VisibleArtRaycastsDisabled(), Is.True);
            }
            finally { router.Dispose(); UnityEngine.Object.DestroyImmediate(instance); }
        }

        [Test]
        public void StateControlsStayInsideSafeAreaWithoutOverlapAtAllRatios()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Stage3PrefabGenerator.StagePath);
            var instance = UnityEngine.Object.Instantiate(prefab); var router = Router();
            try
            {
                var view = instance.GetComponent<Stage3StageView>();
                view.Configure(router, Stage3Content.LoadApproved(), false);
                foreach (var item in new[]
                {
                    new[] { 2048f, 1536f, 0f, 40f, 2048f, 1456f },
                    new[] { 2560f, 1440f, 40f, 0f, 2480f, 1400f },
                    new[] { 3456f, 1440f, 80f, 0f, 3296f, 1400f }
                })
                {
                    Assert.That(view.TryApplyResponsiveLayout(item[0], item[1], item[2], item[3], item[4], item[5], 326, 88), Is.True);
                    foreach (var snapshot in StateSnapshots())
                    {
                        view.Present(snapshot, false); view.SetInputReady(true);
                        Assert.That(view.DoEnabledTouchTargetsOverlap(), Is.False, snapshot.State.ToString());
                        Assert.That(view.AreAllEnabledTargetsInsideSafeArea(), Is.True, snapshot.State.ToString());
                        Assert.That(view.VisibleArtContainedByTargets(), Is.True, snapshot.State.ToString());
                    }
                }
            }
            finally { router.Dispose(); UnityEngine.Object.DestroyImmediate(instance); }
        }

        [Test]
        public void AllNineOutcomesUseEqualBenefitNeedGeometryAndBoyGirlParity()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Stage3PrefabGenerator.StagePath);
            var boy = UnityEngine.Object.Instantiate(prefab); var girl = UnityEngine.Object.Instantiate(prefab);
            var boyRouter = Router(); var girlRouter = Router();
            try
            {
                var content = Stage3Content.LoadApproved();
                var boyView = boy.GetComponent<Stage3StageView>(); var girlView = girl.GetComponent<Stage3StageView>();
                boyView.Configure(boyRouter, content, false); girlView.Configure(girlRouter, content, true);
                foreach (var river in new[] { Stage2River.Low, Stage2River.Middle, Stage2River.High })
                foreach (var plan in new[] { Stage3Plan.Grain, Stage3Plan.Channel, Stage3Plan.Boat })
                {
                    var snapshot = Snapshot(Stage3State.OutcomePlaying, river, plan);
                    boyView.Present(snapshot, false); girlView.Present(snapshot, false);
                    Assert.That(boyView.SelectedPlanSprite, Is.SameAs(content.Plans[(int)plan - 1]));
                    Assert.That(boyView.SelectedRiverLabel, Is.Not.Empty);
                    boyView.Present(new Stage3OutcomePresentation(snapshot.CurrentOutcome, Stage3OutcomeVisualState.Final, 2.0, false));
                    Assert.That(boyView.OutcomeLayerOpacity, Is.Zero.Within(0.001f));
                    boyView.Present(new Stage3OutcomePresentation(snapshot.CurrentOutcome, Stage3OutcomeVisualState.Final, 3.6, false));
                    girlView.Present(new Stage3OutcomePresentation(snapshot.CurrentOutcome, Stage3OutcomeVisualState.Final, 3.6, false));
                    Assert.That(boyView.OutcomeLayerOpacity, Is.EqualTo(1f).Within(0.001f));
                    Assert.That(boyView.BenefitCanvasRect, Is.EqualTo(boyView.NeedCanvasRect));
                    Assert.That(boyView.OutcomeLayerColorsEqual, Is.True);
                    Assert.That(boyView.SelectedChildCanvasRect, Is.EqualTo(girlView.SelectedChildCanvasRect));
                    Assert.That(boyView.SelectedChildSprite, Is.Not.EqualTo(girlView.SelectedChildSprite));
                }
            }
            finally
            {
                boyRouter.Dispose(); girlRouter.Dispose();
                UnityEngine.Object.DestroyImmediate(boy); UnityEngine.Object.DestroyImmediate(girl);
            }
        }

        private static Stage3Snapshot[] StateSnapshots() => new[]
        {
            Snapshot(Stage3State.Intro), Snapshot(Stage3State.ChoosingPlan, Stage2River.Middle),
            Snapshot(Stage3State.OutcomePlaying, Stage2River.Low, Stage3Plan.Grain),
            Snapshot(Stage3State.OutcomeReady, Stage2River.High, Stage3Plan.Boat),
            Snapshot(Stage3State.ChoosingRiver, Stage2River.Middle)
        };

        private static Stage3Snapshot Snapshot(Stage3State state, Stage2River river = Stage2River.None,
            Stage3Plan plan = Stage3Plan.None)
        {
            var outcome = plan == Stage3Plan.None ? default : new Stage3OutcomeKey(river, plan,
                (Stage3Benefit)(int)plan, plan == Stage3Plan.Grain ? Stage3Need.WaterRoute
                    : plan == Stage3Plan.Channel ? Stage3Need.GrainStorage : Stage3Need.FieldWater);
            return new Stage3Snapshot(Guid.NewGuid(), state, river, plan, outcome,
                Stage3TriedCombinations.None, plan == Stage3Plan.None ? Guid.Empty : Guid.NewGuid(),
                Guid.NewGuid(), 0, 0, null, 1, false);
        }

        private static ChildInputRouter Router() => new ChildInputRouter(
            new InputAction("Point", InputActionType.PassThrough),
            new InputAction("Press", InputActionType.Button));
    }
}
