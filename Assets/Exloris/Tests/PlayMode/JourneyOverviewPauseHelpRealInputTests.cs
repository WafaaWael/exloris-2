using System;
using System.Collections.Generic;
using Exloris.Runtime.Input;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Recovery;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Exloris.Tests.PlayMode
{
    public sealed class JourneyOverviewPauseHelpRealInputTests : InputTestFixture
    {
        private Mouse _mouse;
        private readonly List<SemanticInputCommand> _commands = new List<SemanticInputCommand>();

        public override void Setup()
        {
            base.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            _commands.Clear();
            base.TearDown();
        }

        [Test]
        public void PauseAcceptsAllSixControlsAndHelpAcceptsBackAlone()
        {
            using (var context = Create())
            {
                var ids = new[]
                {
                    PauseOverlayView.HelpControlId,
                    PauseOverlayView.SoundControlId,
                    PauseOverlayView.CaptionsControlId,
                    PauseOverlayView.ResumeControlId,
                    PauseOverlayView.MotionControlId,
                    PauseOverlayView.HomeControlId,
                };
                var snapshot = Snapshot();
                context.Pause.ShowAndBlockCaller(snapshot);
                Canvas.ForceUpdateCanvases();
                for (var index = 0; index < ids.Length; index++)
                    Click(context.Pause.GetControlScreenBounds(ids[index]).center, 10d + index * 0.3d);
                Assert.That(ControlIds(), Is.EqualTo(ids));

                _commands.Clear();
                context.Help.ShowAbovePause(snapshot);
                Canvas.ForceUpdateCanvases();
                var back = context.Help.GetControlScreenBounds(
                    JourneyOverviewPauseHelpView.BackControlId);
                ProbeFiveEdges(back, 20d);
                Assert.That(_commands, Has.Count.EqualTo(5));
                Assert.That(_commands, Has.All.Matches<SemanticInputCommand>(command =>
                    command.ControlId == JourneyOverviewPauseHelpView.BackControlId));
            }
        }

        private Context Create()
        {
            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = new InputActionMap("JourneyPauseHelpInput");
            var point = map.AddAction("Point", InputActionType.PassThrough,
                expectedControlLayout: "Vector2");
            point.AddBinding("<Mouse>/position");
            var press = map.AddAction("Press", InputActionType.Button,
                expectedControlLayout: "Button");
            press.AddBinding("<Mouse>/leftButton");
            actions.AddActionMap(map);
            var router = new ChildInputRouter(point, press);
            router.SemanticCommandIssued += _commands.Add;
            var content = JourneyOverviewContent.LoadApproved();
            var journeyObject = new GameObject("JourneyCaller");
            var pauseObject = new GameObject("PauseInput");
            var helpObject = new GameObject("PauseHelpInput");
            var journey = journeyObject.AddComponent<JourneyOverviewStageView>();
            journey.Configure(router, content);
            journey.SetInputReady(true);
            var pause = pauseObject.AddComponent<PauseOverlayView>();
            pause.Configure(router, content, journey);
            var help = helpObject.AddComponent<JourneyOverviewPauseHelpView>();
            help.Configure(router, content, pause);
            router.Enable();
            router.SetVisibleStateReady(true);
            Canvas.ForceUpdateCanvases();
            return new Context(actions, router, journeyObject, pauseObject, helpObject, pause, help);
        }

        private static PauseOverlaySnapshot Snapshot() => new PauseOverlaySnapshot(
            Guid.NewGuid(),
            new JourneyOverviewSnapshot(Guid.NewGuid(),
                JourneyOverviewRevealState.RevealingMeasure, 0d, 1),
            new PauseSettings(true, false, false));

        private void ProbeFiveEdges(Rect bounds, double startedAt)
        {
            Click(bounds.center, startedAt);
            Click(new Vector2(bounds.xMin + 0.25f, bounds.center.y), startedAt + 0.3d);
            Click(new Vector2(bounds.xMax - 0.25f, bounds.center.y), startedAt + 0.6d);
            Click(new Vector2(bounds.center.x, bounds.yMin + 0.25f), startedAt + 0.9d);
            Click(new Vector2(bounds.center.x, bounds.yMax - 0.25f), startedAt + 1.2d);
        }

        private void Click(Vector2 position, double startedAt)
        {
            currentTime = startedAt;
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = position });
            InputSystem.Update();
            Press(_mouse.leftButton);
            currentTime = startedAt + 0.05d;
            Release(_mouse.leftButton);
        }

        private string[] ControlIds()
        {
            var result = new string[_commands.Count];
            for (var index = 0; index < result.Length; index++)
                result[index] = _commands[index].ControlId;
            return result;
        }

        private sealed class Context : IDisposable
        {
            public Context(InputActionAsset actions, ChildInputRouter router,
                GameObject journeyObject, GameObject pauseObject, GameObject helpObject,
                PauseOverlayView pause, JourneyOverviewPauseHelpView help)
            {
                Actions = actions; Router = router; JourneyObject = journeyObject;
                PauseObject = pauseObject; HelpObject = helpObject; Pause = pause; Help = help;
            }
            private InputActionAsset Actions { get; }
            private ChildInputRouter Router { get; }
            private GameObject JourneyObject { get; }
            private GameObject PauseObject { get; }
            private GameObject HelpObject { get; }
            public PauseOverlayView Pause { get; }
            public JourneyOverviewPauseHelpView Help { get; }
            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(HelpObject);
                UnityEngine.Object.DestroyImmediate(PauseObject);
                UnityEngine.Object.DestroyImmediate(JourneyObject);
                Router.Dispose();
                UnityEngine.Object.DestroyImmediate(Actions);
            }
        }
    }
}
