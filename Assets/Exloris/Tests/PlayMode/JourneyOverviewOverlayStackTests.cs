using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Input;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Recovery;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Exloris.Tests.PlayMode
{
    public sealed class JourneyOverviewOverlayStackTests : InputTestFixture
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
        public void HelpAndLeaveBlockEveryLowerLayerAndKeepFrozenLayerOrder()
        {
            using (var context = Create())
            {
                var snapshot = Snapshot();
                context.Pause.ShowAndBlockCaller(snapshot);
                context.Help.ShowAbovePause(snapshot);
                Canvas.ForceUpdateCanvases();

                Assert.That(context.Pause.SortingOrder, Is.LessThan(context.Help.SortingOrder));
                Assert.That(context.Help.SortingOrder, Is.LessThan(context.LeaveSortingOrder));
                Click(context.Journey.GetControlScreenBounds(JourneyOverviewStageView.RepeatControlId).center, 10d);
                Click(context.Pause.GetControlScreenBounds(PauseOverlayView.SoundControlId).center, 10.3d);
                Click(context.Help.GetControlScreenBounds(JourneyOverviewPauseHelpView.BackControlId).center, 10.6d);
                Assert.That(ControlIds(), Is.EqualTo(new[] { JourneyOverviewPauseHelpView.BackControlId }));

                context.Help.Hide();
                context.Leave.ShowAndBlockPause(Request(snapshot));
                Canvas.ForceUpdateCanvases();
                Click(context.Journey.GetControlScreenBounds(JourneyOverviewStageView.RepeatControlId).center, 11d);
                Click(context.Pause.GetControlScreenBounds(PauseOverlayView.HelpControlId).center, 11.3d);
                Click(context.Leave.GetControlScreenBounds(LeaveConfirmationView.StayControlId).center, 11.6d);
                Assert.That(ControlIds(), Is.EqualTo(new[]
                {
                    JourneyOverviewPauseHelpView.BackControlId,
                    LeaveConfirmationView.StayControlId,
                }));
            }
        }

        [Test]
        public void MedallionsConnectorsAndHelpPictogramsIssueNoSemanticCommand()
        {
            using (var context = Create())
            {
                for (var index = 0; index < 3; index++)
                    ProbeCenterAndEdge(context.Journey.GetTimelineMedallionScreenBounds(index), 20d + index);
                for (var index = 0; index < 2; index++)
                    ProbeCenterAndEdge(context.Journey.GetTimelineConnectorScreenBounds(index), 24d + index);
                Assert.That(_commands, Is.Empty);

                var snapshot = Snapshot();
                context.Pause.ShowAndBlockCaller(snapshot);
                context.Help.ShowAbovePause(snapshot);
                Canvas.ForceUpdateCanvases();
                for (var index = 0; index < 4; index++)
                    ProbeCenterAndEdge(context.Help.GetDisplayPictogramScreenBounds(index), 27d + index);
                Assert.That(_commands, Is.Empty);
            }
        }

        private Context Create()
        {
            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = new InputActionMap("JourneyOverlayTest");
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
            var journeyObject = new GameObject("JourneyOverlayCaller");
            var pauseObject = new GameObject("JourneyPauseOverlay");
            var helpObject = new GameObject("JourneyPauseHelp");
            var leaveObject = new GameObject("JourneyLeaveOverlay");
            var journey = journeyObject.AddComponent<JourneyOverviewStageView>();
            journey.Configure(router, content);
            journey.SetInputReady(true);
            var pause = pauseObject.AddComponent<PauseOverlayView>();
            pause.Configure(router, content, journey);
            var help = helpObject.AddComponent<JourneyOverviewPauseHelpView>();
            help.Configure(router, content, pause);
            var leave = leaveObject.AddComponent<LeaveConfirmationView>();
            leave.Configure(router, AvatarContent.LoadApproved());
            leave.BindJourneyCaller(journey, pause);
            router.Enable();
            router.SetVisibleStateReady(true);
            Canvas.ForceUpdateCanvases();
            return new Context(actions, router, journeyObject, pauseObject, helpObject,
                leaveObject, journey, pause, help, leave);
        }

        private static PauseOverlaySnapshot Snapshot() => new PauseOverlaySnapshot(
            Guid.NewGuid(),
            new JourneyOverviewSnapshot(Guid.NewGuid(),
                JourneyOverviewRevealState.RevealingMeasure, 0d, 1),
            new PauseSettings(true, false, false));

        private static LeaveRequest Request(PauseOverlaySnapshot snapshot) =>
            new LeaveRequest(RouteId.JourneyOverview, LeaveReturnLayer.PauseOverlay,
                snapshot.StackToken);

        private void ProbeCenterAndEdge(Rect bounds, double startedAt)
        {
            Click(bounds.center, startedAt);
            Click(new Vector2(bounds.xMin + 0.25f, bounds.center.y), startedAt + 0.3d);
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
                GameObject leaveObject, JourneyOverviewStageView journey,
                PauseOverlayView pause, JourneyOverviewPauseHelpView help,
                LeaveConfirmationView leave)
            {
                Actions = actions; Router = router; JourneyObject = journeyObject;
                PauseObject = pauseObject; HelpObject = helpObject; LeaveObject = leaveObject;
                Journey = journey; Pause = pause; Help = help; Leave = leave;
            }
            private InputActionAsset Actions { get; }
            private ChildInputRouter Router { get; }
            private GameObject JourneyObject { get; }
            private GameObject PauseObject { get; }
            private GameObject HelpObject { get; }
            private GameObject LeaveObject { get; }
            public JourneyOverviewStageView Journey { get; }
            public PauseOverlayView Pause { get; }
            public JourneyOverviewPauseHelpView Help { get; }
            public LeaveConfirmationView Leave { get; }
            public int LeaveSortingOrder =>
                Leave.GetComponentInChildren<Canvas>(true).sortingOrder;
            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(LeaveObject);
                UnityEngine.Object.DestroyImmediate(HelpObject);
                UnityEngine.Object.DestroyImmediate(PauseObject);
                UnityEngine.Object.DestroyImmediate(JourneyObject);
                Router.Dispose();
                UnityEngine.Object.DestroyImmediate(Actions);
            }
        }
    }
}
