using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Exloris.Runtime.Input;
using Exloris.Runtime.Stage2;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class Stage2HostRealInputTests : InputTestFixture
    {
        private Mouse _mouse;
        public override void Setup() { base.Setup(); _mouse = InputSystem.AddDevice<Mouse>(); }

        [UnityTest]
        public IEnumerator MousePenAndTouchShareOneSemanticSurfaceWithNearMissesInert()
        {
            var point = new InputAction("Point", InputActionType.PassThrough, "<Pointer>/position");
            var press = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
            var router = new ChildInputRouter(point, press); var owner = new GameObject("Stage2Input");
            var commands = new List<string>();
            try
            {
                var view = owner.AddComponent<Stage2StageView>();
                view.Configure(router, Stage2Content.LoadApproved(), false); view.SetInputReady(true);
                view.Present(Snapshot(Stage2State.Intro), false);
                router.SemanticCommandIssued += command => commands.Add(command.ControlId);
                router.SetVisibleStateReady(true); router.Enable(); yield return null;
                yield return Click(view.GetControlScreenBounds(Stage2StageView.PlayControlId).center);
                Assert.That(commands.Last(), Is.EqualTo(Stage2StageView.PlayControlId));
                view.Present(Snapshot(Stage2State.Choosing), false);
                var pen = InputSystem.AddDevice<Pen>(); var low = view.GetControlScreenBounds(Stage2StageView.LowControlId).center;
                currentTime += 0.3d; Set(pen.position, low, currentTime); Press(pen.tip, currentTime + 0.01d); Release(pen.tip, currentTime + 0.06d); yield return null;
                Assert.That(commands.Last(), Is.EqualTo(Stage2StageView.LowControlId));
                view.Present(Snapshot(Stage2State.Inspecting, Stage2River.Low), false);
                var touch = InputSystem.AddDevice<Touchscreen>(); var field = view.GetControlScreenBounds(Stage2StageView.FieldControlId).center;
                currentTime += 0.3d; BeginTouch(1, field, screen: touch); EndTouch(1, field, screen: touch); yield return null;
                Assert.That(commands.Last(), Is.EqualTo(Stage2StageView.FieldControlId));
                var count = commands.Count; var bounds = view.GetControlScreenBounds(Stage2StageView.FieldControlId);
                yield return Click(new Vector2(bounds.xMin - 2f, bounds.center.y));
                Assert.That(commands.Count, Is.EqualTo(count));
            }
            finally { router.Dispose(); Object.Destroy(owner); }
        }

        private IEnumerator Click(Vector2 position)
        {
            var time = currentTime + 0.30d; Set(_mouse.position, position, time);
            Press(_mouse.leftButton, time + 0.01d); Release(_mouse.leftButton, time + 0.06d);
            currentTime = time + 0.07d; yield return null;
        }

        private static Stage2Snapshot Snapshot(Stage2State state, Stage2River river = Stage2River.None) =>
            new Stage2Snapshot(System.Guid.NewGuid(), state, river, Stage2ObservedHotspots.None,
                Stage2ObservedHotspots.None, Stage2ObservedHotspots.None, Stage2Hotspot.None,
                System.Guid.NewGuid(), System.Guid.NewGuid(), 0, 0, null, 0, false);
    }
}
