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
    public sealed class Boat2RealInputTests : InputTestFixture
    {
        private Mouse _mouse;
        public override void Setup() { base.Setup(); _mouse = InputSystem.AddDevice<Mouse>(); }

        [UnityTest]
        public IEnumerator MouseHitsEveryTargetWhileCenterAndNearMissAreInert()
        {
            var point = new InputAction("Point",InputActionType.PassThrough,"<Pointer>/position"); var press = new InputAction("Press",InputActionType.Button,"<Pointer>/press");
            var router = new ChildInputRouter(point,press); var owner = new GameObject("Boat2Input"); var commands = new List<string>();
            try
            {
                var view = owner.AddComponent<Boat2StageView>(); view.Configure(router,Boat2Content.LoadApproved()); view.SetInputReady(true);
                router.SemanticCommandIssued += command => commands.Add(command.ControlId); router.SetVisibleStateReady(true); router.Enable(); yield return null;
                foreach (var id in view.ControlIds) { yield return Click(view.GetControlScreenBounds(id).center); Assert.That(commands.Last(), Is.EqualTo(id)); }
                var count = commands.Count; yield return Click(new Vector2(Screen.width * 0.5f,Screen.height * 0.5f)); Assert.That(commands.Count,Is.EqualTo(count));
                var field = view.GetControlScreenBounds(Boat2StageView.FieldBankControlId); yield return Click(new Vector2(field.xMin - 2f,field.center.y)); Assert.That(commands.Count,Is.EqualTo(count));
            }
            finally { router.Dispose(); Object.Destroy(owner); }
        }

        [UnityTest]
        public IEnumerator PenAndTouchUseTheSameSemanticSurface()
        {
            var point = new InputAction("Point",InputActionType.PassThrough,"<Pointer>/position"); var press = new InputAction("Press",InputActionType.Button,"<Pointer>/press");
            var router = new ChildInputRouter(point,press); var owner = new GameObject("Boat2PenTouch"); var commands = new List<string>();
            try
            {
                var view = owner.AddComponent<Boat2StageView>(); view.Configure(router,Boat2Content.LoadApproved()); view.SetInputReady(true);
                router.SemanticCommandIssued += command => commands.Add(command.ControlId); router.SetVisibleStateReady(true); router.Enable(); yield return null;
                var pen = InputSystem.AddDevice<Pen>(); var follow = view.GetControlScreenBounds(Boat2StageView.FollowIbisControlId).center;
                currentTime += 0.3d; Set(pen.position,follow,currentTime); Press(pen.tip,currentTime+0.01d); Release(pen.tip,currentTime+0.06d); yield return null; Assert.That(commands.Last(),Is.EqualTo(Boat2StageView.FollowIbisControlId));
                var touch = InputSystem.AddDevice<Touchscreen>(); var dock = view.GetControlScreenBounds(Boat2StageView.DockControlId).center;
                currentTime += 0.3d; BeginTouch(1,dock,screen:touch); EndTouch(1,dock,screen:touch); yield return null; Assert.That(commands.Last(),Is.EqualTo(Boat2StageView.DockControlId));
            }
            finally { router.Dispose(); Object.Destroy(owner); }
        }

        private IEnumerator Click(Vector2 position) { var time = currentTime + 0.30d; Set(_mouse.position,position,time); Press(_mouse.leftButton,time+0.01d); Release(_mouse.leftButton,time+0.06d); currentTime=time+0.07d; yield return null; }
    }
}
