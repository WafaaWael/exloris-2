using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class Boat1RealInputTests : InputTestFixture
    {
        private Mouse _mouse;
        public override void Setup() { base.Setup(); _mouse = InputSystem.AddDevice<Mouse>(); }

        [UnityTest]
        public IEnumerator MouseHitsEverySemanticTargetAndInertArtDoesNothing()
        {
            var point = new InputAction("Point", InputActionType.PassThrough, "<Pointer>/position");
            var press = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
            var router = new ChildInputRouter(point, press); var owner = new GameObject("Boat1Input"); var commands = new List<string>();
            try
            {
                var view = owner.AddComponent<Boat1StageView>(); view.Configure(router, Boat1Content.LoadApproved()); view.SetInputReady(true);
                router.SemanticCommandIssued += command => commands.Add(command.ControlId); router.SetVisibleStateReady(true); router.Enable(); yield return null;
                foreach (var id in view.ControlIds) { yield return Click(view.GetControlScreenBounds(id).center); Assert.That(commands[commands.Count - 1], Is.EqualTo(id)); }
                var count = commands.Count; yield return Click(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)); Assert.That(commands.Count, Is.EqualTo(count));
                var reeds = view.GetControlScreenBounds(Boat1StageView.ReedsControlId); yield return Click(new Vector2(reeds.xMin - 2f, reeds.center.y)); Assert.That(commands.Count, Is.EqualTo(count));
            }
            finally { router.Dispose(); UnityEngine.Object.Destroy(owner); }
        }

        [UnityTest]
        public IEnumerator PenAndTouchUseTheSameVisibleSemanticTarget()
        {
            var point = new InputAction("Point", InputActionType.PassThrough, "<Pointer>/position");
            var press = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
            var router = new ChildInputRouter(point, press); var owner = new GameObject("Boat1PenTouch"); var commands = new List<string>();
            try
            {
                var view = owner.AddComponent<Boat1StageView>(); view.Configure(router, Boat1Content.LoadApproved()); view.SetInputReady(true);
                router.SemanticCommandIssued += command => commands.Add(command.ControlId); router.SetVisibleStateReady(true); router.Enable(); yield return null;
                var pen = InputSystem.AddDevice<Pen>(); var help = view.GetControlScreenBounds(Boat1StageView.HelpControlId).center;
                currentTime += 0.3d; Set(pen.position, help, currentTime); Press(pen.tip, currentTime + 0.01d); Release(pen.tip, currentTime + 0.06d); yield return null;
                Assert.That(commands.Last(), Is.EqualTo(Boat1StageView.HelpControlId));
                var touch = InputSystem.AddDevice<Touchscreen>(); var skip = view.GetControlScreenBounds(Boat1StageView.SkipControlId).center;
                currentTime += 0.3d; BeginTouch(1, skip, screen: touch); EndTouch(1, skip, screen: touch); yield return null;
                Assert.That(commands.Last(), Is.EqualTo(Boat1StageView.SkipControlId));
            }
            finally { router.Dispose(); UnityEngine.Object.Destroy(owner); }
        }

        private IEnumerator Click(Vector2 position) { var t = currentTime + 0.30d; Set(_mouse.position, position, t); Press(_mouse.leftButton, t + 0.01d); Release(_mouse.leftButton, t + 0.06d); currentTime = t + 0.07d; yield return null; }
    }
}
