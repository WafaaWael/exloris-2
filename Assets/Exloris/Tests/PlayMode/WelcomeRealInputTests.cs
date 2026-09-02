using System.Collections.Generic;
using Exloris.Runtime.Input;
using Exloris.Runtime.Welcome;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Exloris.Tests.PlayMode
{
    public sealed class WelcomeRealInputTests : InputTestFixture
    {
        private InputActionAsset _actions;
        private ChildInputRouter _router;
        private WelcomeStageView _welcome;
        private SessionChoiceView _sessionChoice;
        private Mouse _mouse;
        private readonly List<SemanticInputCommand> _commands = new List<SemanticInputCommand>();

        public override void Setup()
        {
            base.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
            _actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = new InputActionMap("WelcomeTest");
            var point = map.AddAction("Point", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            point.AddBinding("<Mouse>/position");
            var press = map.AddAction("Press", InputActionType.Button, expectedControlLayout: "Button");
            press.AddBinding("<Mouse>/leftButton");
            _actions.AddActionMap(map);
            _router = new ChildInputRouter(point, press);
            _router.SemanticCommandIssued += _commands.Add;

            var content = WelcomeContent.LoadApproved();
            _sessionChoice = new GameObject("SessionChoiceTest").AddComponent<SessionChoiceView>();
            _sessionChoice.Configure(_router, content);
            _welcome = new GameObject("WelcomeTest").AddComponent<WelcomeStageView>();
            _welcome.Configure(_router, content, _sessionChoice);
            _router.Enable();
            Canvas.ForceUpdateCanvases();
        }

        public override void TearDown()
        {
            if (_welcome != null) Object.DestroyImmediate(_welcome.gameObject);
            if (_sessionChoice != null) Object.DestroyImmediate(_sessionChoice.gameObject);
            _router?.Dispose();
            if (_actions != null) Object.DestroyImmediate(_actions);
            _commands.Clear();
            base.TearDown();
        }

        [Test]
        public void PlayAcceptsCentreAndFourEdgesButRejectsNearMiss()
        {
            var bounds = _welcome.GetControlScreenBounds(WelcomeStageView.PlayControlId);

            Click(bounds.center, 10d);
            Click(new Vector2(bounds.xMin + 0.25f, bounds.center.y), 10.3d);
            Click(new Vector2(bounds.xMax - 0.25f, bounds.center.y), 10.6d);
            Click(new Vector2(bounds.center.x, bounds.yMin + 0.25f), 10.9d);
            Click(new Vector2(bounds.center.x, bounds.yMax - 0.25f), 11.2d);
            Click(new Vector2(bounds.xMin - 1f, bounds.center.y), 11.5d);

            Assert.That(_commands, Has.Count.EqualTo(5));
            Assert.That(_commands, Has.All.Matches<SemanticInputCommand>(command =>
                command.ControlId == WelcomeStageView.PlayControlId));
        }

        [Test]
        public void SessionChoiceDisablesWelcomeTargetsAndAcceptsOnlyItsExactControls()
        {
            var oldPlay = _welcome.GetControlScreenBounds(WelcomeStageView.PlayControlId);
            _welcome.Show(WelcomePresentationState.SessionChoice);
            Canvas.ForceUpdateCanvases();

            Click(new Vector2(oldPlay.center.x - oldPlay.width * 0.28f, oldPlay.center.y), 20d);
            Click(_sessionChoice.GetControlScreenBounds(SessionChoiceView.ContinueControlId).center, 20.3d);
            Click(_sessionChoice.GetControlScreenBounds(SessionChoiceView.RestartControlId).center, 20.6d);
            Click(_sessionChoice.GetControlScreenBounds(SessionChoiceView.BackControlId).center, 20.9d);

            Assert.That(_commands, Has.Count.EqualTo(3));
            Assert.That(_commands[0].ControlId, Is.EqualTo(SessionChoiceView.ContinueControlId));
            Assert.That(_commands[1].ControlId, Is.EqualTo(SessionChoiceView.RestartControlId));
            Assert.That(_commands[2].ControlId, Is.EqualTo(SessionChoiceView.BackControlId));
        }

        [Test]
        public void AdultIsOneOrdinaryTapWithNoHoldOrProgressLifecycle()
        {
            var adult = _welcome.GetControlScreenBounds(WelcomeStageView.AdultControlId);
            Click(adult.center, 30d);

            Assert.That(_commands, Has.Count.EqualTo(1));
            Assert.That(_commands[0].ControlId, Is.EqualTo(WelcomeStageView.AdultControlId));
            Assert.That(_commands[0].Kind, Is.EqualTo(SemanticInputKind.Tap));
        }

        private void Click(Vector2 position, double startedAt)
        {
            currentTime = startedAt;
            Set(_mouse.position, position);
            Press(_mouse.leftButton);
            currentTime = startedAt + 0.05d;
            Release(_mouse.leftButton);
        }
    }
}
