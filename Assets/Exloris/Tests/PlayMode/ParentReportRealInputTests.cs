using System;
using System.Collections;
using System.Collections.Generic;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Completion;
using Exloris.Runtime.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class ParentReportRealInputTests : InputTestFixture
    {
        private Mouse _mouse;
        public override void Setup() { base.Setup(); _mouse = InputSystem.AddDevice<Mouse>(); }

        [UnityTest]
        public IEnumerator CloseRestartAndScrollUseRealTargetsWhileNearMissIsInert()
        {
            var point = new InputAction("Point", InputActionType.PassThrough, "<Pointer>/position");
            var press = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
            var router = new ChildInputRouter(point, press);
            var owner = new GameObject("ParentReportRealInput");
            var commands = new List<SemanticInputCommand>();
            try
            {
                var view = owner.AddComponent<ParentReportView>();
                view.Configure(router, CompletionAdultReportContent.LoadApproved());
                view.Present(new ParentReportSnapshot(
                    Guid.NewGuid(), ParentReportState.Ready,
                    Array.Empty<Exloris.Core.Session.ParentReportCard>(), 1f, null));
                view.SetInputReady(true);
                router.SemanticCommandIssued += command => commands.Add(command);
                router.SetVisibleStateReady(true);
                router.Enable();
                yield return null;

                yield return Click(view.GetControlScreenBounds(ParentReportView.CloseControlId).center);
                yield return Click(view.GetControlScreenBounds(ParentReportView.RestartControlId).center);
                var scroll = view.GetControlScreenBounds(ParentReportView.ScrollControlId);
                yield return Drag(scroll.center, scroll.center + Vector2.up * 40f);
                var count = commands.Count;
                yield return Click(new Vector2(scroll.xMax + 2f, scroll.center.y));

                Assert.That(commands.Count, Is.EqualTo(count));
                Assert.That(commands[0].ControlId, Is.EqualTo(ParentReportView.CloseControlId));
                Assert.That(commands[1].ControlId, Is.EqualTo(ParentReportView.RestartControlId));
                Assert.That(commands[2].ControlId, Is.EqualTo(ParentReportView.ScrollControlId));
                Assert.That(commands[2].Kind, Is.EqualTo(SemanticInputKind.Drag));
            }
            finally
            {
                router.Dispose();
                UnityEngine.Object.Destroy(owner);
            }
        }

        private IEnumerator Click(Vector2 position)
        {
            var time = currentTime + 0.30d;
            Set(_mouse.position, position, time);
            Press(_mouse.leftButton, time + 0.01d);
            Release(_mouse.leftButton, time + 0.06d);
            currentTime = time + 0.07d;
            yield return null;
        }

        private IEnumerator Drag(Vector2 start, Vector2 end)
        {
            var time = currentTime + 0.30d;
            Set(_mouse.position, start, time);
            Press(_mouse.leftButton, time + 0.01d);
            Set(_mouse.position, end, time + 0.06d);
            Release(_mouse.leftButton, time + 0.09d);
            currentTime = time + 0.10d;
            yield return null;
        }
    }
}
