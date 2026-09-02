using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Exloris.Runtime.Input;
using Exloris.Runtime.Stage2;
using Exloris.Runtime.Stage3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class Stage3RealInputTests : InputTestFixture
    {
        private Mouse _mouse;
        public override void Setup() { base.Setup(); _mouse = InputSystem.AddDevice<Mouse>(); }

        [UnityTest]
        public IEnumerator MousePenTouchCentersAndEdgesWorkWhileNearMissesAndSameFrameRacesAreInert()
        {
            var point = new InputAction("Point", InputActionType.PassThrough, "<Pointer>/position");
            var press = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
            var router = new ChildInputRouter(point, press); var owner = new GameObject("Stage3Input");
            var commands = new List<string>();
            try
            {
                var view = owner.AddComponent<Stage3StageView>();
                view.Configure(router, Stage3Content.LoadApproved(), false); view.SetInputReady(true);
                view.Present(Snapshot(Stage3State.Intro), false);
                router.SemanticCommandIssued += command => commands.Add(command.ControlId);
                router.SetVisibleStateReady(true); router.Enable(); yield return null;
                yield return Click(view.GetControlScreenBounds(Stage3StageView.PlayControlId).center);
                Assert.That(commands.Last(), Is.EqualTo(Stage3StageView.PlayControlId));

                view.Present(Snapshot(Stage3State.ChoosingPlan, Stage2River.Middle), false);
                var pen = InputSystem.AddDevice<Pen>(); var grain = view.GetControlScreenBounds(Stage3StageView.GrainControlId);
                currentTime += 0.3d; Set(pen.position, new Vector2(grain.xMin, grain.center.y), currentTime);
                Press(pen.tip, currentTime + 0.01d); Release(pen.tip, currentTime + 0.06d); yield return null;
                Assert.That(commands.Last(), Is.EqualTo(Stage3StageView.GrainControlId));

                view.Present(Snapshot(Stage3State.ChoosingRiver, Stage2River.Middle), false);
                yield return Click(view.GetControlScreenBounds(Stage3StageView.RiverBackControlId).center);
                Assert.That(commands.Last(), Is.EqualTo(Stage3StageView.RiverBackControlId));

                view.Present(Snapshot(Stage3State.ChoosingRiver, Stage2River.Middle), false);
                var touch = InputSystem.AddDevice<Touchscreen>(); var high = view.GetControlScreenBounds(Stage3StageView.HighControlId).center;
                currentTime += 0.3d; BeginTouch(1, high, screen: touch); EndTouch(1, high, screen: touch); yield return null;
                Assert.That(commands.Last(), Is.EqualTo(Stage3StageView.HighControlId));

                var count = commands.Count; var bounds = view.GetControlScreenBounds(Stage3StageView.HighControlId);
                yield return Click(new Vector2(bounds.xMax + 2f, bounds.center.y));
                Assert.That(commands.Count, Is.EqualTo(count));

                currentTime += 0.3d; Set(_mouse.position, bounds.center, currentTime);
                Press(_mouse.leftButton, currentTime + 0.01d);
                BeginTouch(2, bounds.center, screen: touch, time: currentTime + 0.01d);
                Release(_mouse.leftButton, currentTime + 0.06d);
                EndTouch(2, bounds.center, screen: touch, time: currentTime + 0.06d); yield return null;
                Assert.That(commands.Count, Is.EqualTo(count + 1));
            }
            finally { router.Dispose(); Object.Destroy(owner); }
        }

        private IEnumerator Click(Vector2 position)
        {
            var time = currentTime + 0.30d; Set(_mouse.position, position, time);
            Press(_mouse.leftButton, time + 0.01d); Release(_mouse.leftButton, time + 0.06d);
            currentTime = time + 0.07d; yield return null;
        }

        private static Stage3Snapshot Snapshot(Stage3State state, Stage2River river = Stage2River.None) =>
            new Stage3Snapshot(System.Guid.NewGuid(), state, river, Stage3Plan.None, default,
                Stage3TriedCombinations.None, System.Guid.Empty, System.Guid.NewGuid(),
                0, 0, null, 0, false);
    }
}
