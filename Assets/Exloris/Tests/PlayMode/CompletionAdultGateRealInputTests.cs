using System;
using System.Collections;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Completion;
using Exloris.Runtime.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class CompletionAdultGateRealInputTests : InputTestFixture
    {
        private Mouse _mouse;
        public override void Setup() { base.Setup(); _mouse = InputSystem.AddDevice<Mouse>(); }

        [UnityTest]
        public IEnumerator RealTapAndHoldReportOneSemanticCommandWithExactDuration()
        {
            var point = new InputAction("Point", InputActionType.PassThrough, "<Pointer>/position");
            var press = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
            var router = new ChildInputRouter(point, press);
            var owner = new GameObject("CompletionAdultRealInput");
            var commands = new List<SemanticInputCommand>();
            try
            {
                var content = CompletionAdultReportContent.LoadApproved();
                var completion = owner.AddComponent<CompletionStageView>();
                completion.Configure(router, content, "avatar_girl");
                completion.Present(new CompletionSnapshot(
                    Guid.NewGuid(), CompletionState.Ready, Guid.NewGuid(), 0d,
                    CompletionController.SemanticSeconds, null, false));
                completion.SetInputReady(true);
                router.SemanticCommandIssued += command => commands.Add(command);
                router.SetVisibleStateReady(true);
                router.Enable();
                yield return null;

                yield return Click(completion.GetControlScreenBounds(CompletionStageView.AdultControlId).center);
                Assert.That(commands.Count, Is.EqualTo(1));
                Assert.That(commands[0].ControlId, Is.EqualTo(CompletionStageView.AdultControlId));

                completion.SetInputReady(false);
                var adult = owner.AddComponent<AdultGateView>();
                adult.Configure(router, content);
                adult.Present(new AdultGateSnapshot(
                    Guid.NewGuid(), AdultGateState.Holding,
                    new AdultGateRequest(AdultGatePurpose.Dashboard,
                        AdultGateRequestOrigin.Completion, RouteId.Completion, Guid.NewGuid()),
                    0d, -1, 0d));
                adult.SetInputReady(true);
                yield return Hold(adult.GetControlScreenBounds(AdultGateView.HoldControlId).center, 3.1d);

                Assert.That(commands.Count, Is.EqualTo(2));
                Assert.That(commands[1].ControlId, Is.EqualTo(AdultGateView.HoldControlId));
                Assert.That(commands[1].DurationSeconds, Is.EqualTo(3.1d).Within(0.001d));
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

        private IEnumerator Hold(Vector2 position, double seconds)
        {
            var time = currentTime + 0.30d;
            Set(_mouse.position, position, time);
            Press(_mouse.leftButton, time + 0.01d);
            Release(_mouse.leftButton, time + 0.01d + seconds);
            currentTime = time + 0.02d + seconds;
            yield return null;
        }
    }
}
