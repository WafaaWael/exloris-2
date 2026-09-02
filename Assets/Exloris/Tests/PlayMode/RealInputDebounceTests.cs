using System.Collections.Generic;
using Exloris.Runtime.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Exloris.Tests.PlayMode
{
    public sealed class RealInputDebounceTests : InputTestFixture
    {
        private InputActionAsset _actions;
        private ChildInputRouter _router;
        private readonly List<SemanticInputCommand> _commands =
            new List<SemanticInputCommand>();

        public override void Setup()
        {
            base.Setup();
            _actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = new InputActionMap("Child");
            var point = map.AddAction(
                "Point",
                InputActionType.PassThrough,
                "<Pointer>/position",
                expectedControlLayout: "Vector2");
            var press = map.AddAction(
                "Press",
                InputActionType.Button,
                "<Pointer>/press",
                expectedControlLayout: "Button");
            _actions.AddActionMap(map);

            _router = new ChildInputRouter(point, press);
            _router.Register(new SemanticControl(
                "play",
                new Rect(100f, 100f, 224f, 224f)));
            _router.SemanticCommandIssued += _commands.Add;
            _router.SetVisibleStateReady(true);
            _router.Enable();
        }

        public override void TearDown()
        {
            _router?.Dispose();
            if (_actions != null)
            {
                Object.DestroyImmediate(_actions);
            }

            _commands.Clear();
            base.TearDown();
        }

        [Test]
        public void OneMouseTapProducesOneSemanticCommand()
        {
            var mouse = InputSystem.AddDevice<Mouse>();

            Tap(mouse, new Vector2(200f, 200f), 0d);

            Assert.That(_commands, Has.Count.EqualTo(1));
            Assert.That(_commands[0].ControlId, Is.EqualTo("play"));
            Assert.That(_commands[0].Kind, Is.EqualTo(SemanticInputKind.Tap));
        }

        [Test]
        public void SameControlRejectsRapidRepeatBeforeTwoHundredFiftyMilliseconds()
        {
            var mouse = InputSystem.AddDevice<Mouse>();

            Tap(mouse, new Vector2(200f, 200f), 0d);
            Tap(mouse, new Vector2(200f, 200f), 0.249d);

            Assert.That(_commands, Has.Count.EqualTo(1));
        }

        [Test]
        public void SameControlAcceptsNextTapAtExactlyTwoHundredFiftyMilliseconds()
        {
            var mouse = InputSystem.AddDevice<Mouse>();

            Tap(mouse, new Vector2(200f, 200f), 0d);
            Tap(mouse, new Vector2(200f, 200f), 0.250d);

            Assert.That(_commands, Has.Count.EqualTo(2));
        }

        [Test]
        public void DebounceIsPerSemanticControl()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            _router.Register(new SemanticControl(
                "home",
                new Rect(500f, 100f, 224f, 224f)));

            Tap(mouse, new Vector2(200f, 200f), 0d);
            Tap(mouse, new Vector2(600f, 200f), 0.050d);

            Assert.That(_commands, Has.Count.EqualTo(2));
            Assert.That(_commands[1].ControlId, Is.EqualTo("home"));
        }

        [Test]
        public void SecondFingerCannotDuplicateAnActivePrimaryTouchGesture()
        {
            var touchscreen = InputSystem.AddDevice<Touchscreen>();
            currentTime = 0d;

            BeginTouch(1, new Vector2(200f, 200f), screen: touchscreen);
            BeginTouch(2, new Vector2(210f, 210f), screen: touchscreen);
            EndTouch(2, new Vector2(210f, 210f), screen: touchscreen);
            EndTouch(1, new Vector2(200f, 200f), screen: touchscreen);

            Assert.That(_commands, Has.Count.EqualTo(1));
        }

        [Test]
        public void InputsRemainDisabledUntilVisibleStateIsReady()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            _router.SetVisibleStateReady(false);

            Tap(mouse, new Vector2(200f, 200f), 0d);
            Assert.That(_commands, Is.Empty);

            _router.SetVisibleStateReady(true);
            Tap(mouse, new Vector2(200f, 200f), 0.300d);
            Assert.That(_commands, Has.Count.EqualTo(1));
        }

        [Test]
        public void HidingStateDuringGestureCancelsWithoutACommand()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            currentTime = 0d;
            Set(mouse.position, new Vector2(200f, 200f));
            Press(mouse.leftButton);

            _router.SetVisibleStateReady(false);
            Release(mouse.leftButton);

            Assert.That(_commands, Is.Empty);
        }

        [Test]
        public void ActiveGestureReportsLivePointerStateAndCanBeCancelled()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            currentTime = 10d;
            Set(mouse.position, new Vector2(200f, 200f), 10d);
            Press(mouse.leftButton, 10.01d);

            Assert.That(_router.TryGetActiveGesture(
                "play", out var startedAt, out var inputTime, out var pointerInside), Is.True);
            Assert.That(startedAt, Is.EqualTo(10.01d).Within(0.001d));
            Assert.That(inputTime, Is.GreaterThanOrEqualTo(startedAt));
            Assert.That(pointerInside, Is.True);

            Set(mouse.position, new Vector2(800f, 800f), 10.76d);
            Assert.That(_router.TryGetActiveGesture(
                "play", out _, out inputTime, out pointerInside), Is.True);
            Assert.That(inputTime, Is.EqualTo(10.76d).Within(0.001d));
            Assert.That(pointerInside, Is.False);
            Assert.That(_router.CancelActiveGesture("play"), Is.True);

            Release(mouse.leftButton, 10.77d);
            Assert.That(_commands, Is.Empty);
        }

        [Test]
        public void SupportedDragProducesOneDragCommandOnRelease()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            _router.ClearControls();
            _router.Register(new SemanticControl(
                "marker",
                new Rect(100f, 100f, 500f, 500f),
                supportsDrag: true));
            currentTime = 0d;
            Set(mouse.position, new Vector2(200f, 200f));
            Press(mouse.leftButton);
            Set(mouse.position, new Vector2(400f, 400f));
            Release(mouse.leftButton);

            Assert.That(_commands, Has.Count.EqualTo(1));
            Assert.That(_commands[0].Kind, Is.EqualTo(SemanticInputKind.Drag));
            Assert.That(_commands[0].StartPosition, Is.EqualTo(new Vector2(200f, 200f)));
            Assert.That(_commands[0].EndPosition, Is.EqualTo(new Vector2(400f, 400f)));
        }

        [Test]
        public void ReleaseOutsideTapTargetDoesNotIssueACommand()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            currentTime = 0d;
            Set(mouse.position, new Vector2(200f, 200f));
            Press(mouse.leftButton);
            Set(mouse.position, new Vector2(800f, 800f));
            Release(mouse.leftButton);

            Assert.That(_commands, Is.Empty);
        }

        private void Tap(Mouse mouse, Vector2 position, double time)
        {
            currentTime = time;
            Set(mouse.position, position);
            Press(mouse.leftButton);
            Release(mouse.leftButton);
        }
    }
}
