using Exloris.Runtime.Audio;
using Exloris.Runtime.Input;
using Exloris.Runtime.Welcome;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Exloris.Tests.PlayMode
{
    public sealed class WelcomeMediaPlayModeTests : InputTestFixture
    {
        private InputActionAsset _actions;
        private ChildInputRouter _router;
        private WelcomeStageView _welcome;
        private SessionChoiceView _sessionChoice;
        private UnityAudioBackend _backend;
        private WelcomeMediaPresenter _presenter;

        public override void Setup()
        {
            base.Setup();
            _actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = new InputActionMap("WelcomeMediaTest");
            var point = map.AddAction("Point", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            var press = map.AddAction("Press", InputActionType.Button, expectedControlLayout: "Button");
            _actions.AddActionMap(map);
            _router = new ChildInputRouter(point, press);
            var content = WelcomeContent.LoadApproved();
            _sessionChoice = new GameObject("SessionChoiceMediaTest").AddComponent<SessionChoiceView>();
            _sessionChoice.Configure(_router, content);
            _welcome = new GameObject("WelcomeMediaTest").AddComponent<WelcomeStageView>();
            _welcome.Configure(_router, content, _sessionChoice);
            _backend = new UnityAudioBackend(_welcome.gameObject);
            _presenter = new WelcomeMediaPresenter(
                new AudioDirector(_backend),
                _backend,
                content,
                _welcome);
            Canvas.ForceUpdateCanvases();
        }

        public override void TearDown()
        {
            _backend?.Dispose();
            if (_welcome != null) Object.DestroyImmediate(_welcome.gameObject);
            if (_sessionChoice != null) Object.DestroyImmediate(_sessionChoice.gameObject);
            _router?.Dispose();
            if (_actions != null) Object.DestroyImmediate(_actions);
            base.TearDown();
        }

        [Test]
        public void WelcomeUsesApprovedVisualAndRuntimeCaptionCuesWithExactTiming()
        {
            var voice = _presenter.Present(WelcomePresentationState.Welcome, true, true, 10d);

            Assert.That(_backend.GetTokenClip(voice.Id), Is.SameAs(WelcomeContent.LoadApproved().WelcomeVoice));
            Assert.That(_welcome.VisualEquivalentVisible, Is.True);
            Assert.That(_welcome.CaptionVisible, Is.True);
            Assert.That(_welcome.CurrentCaptionId, Is.EqualTo(WelcomeMediaPresenter.WelcomeCaption1Id));
            Assert.That(_welcome.CurrentCaptionText, Is.EqualTo("Tap Play when you're ready"));

            Assert.That(_presenter.Advance(12.95d), Is.True);
            Assert.That(_welcome.CaptionVisible, Is.False);
            Assert.That(_presenter.Advance(13.082d), Is.True);
            Assert.That(_welcome.CurrentCaptionId, Is.EqualTo(WelcomeMediaPresenter.WelcomeCaption2Id));
            Assert.That(_welcome.CurrentCaptionText, Is.EqualTo("to explore the Nile."));
            Assert.That(_welcome.CaptionVisible, Is.True);
        }

        [Test]
        public void MutedSessionChoiceKeepsVisualCueAndNoCaptionOrAudio()
        {
            _welcome.Show(WelcomePresentationState.SessionChoice);
            var voice = _presenter.Present(WelcomePresentationState.SessionChoice, false, false, 20d);

            Assert.That(voice, Is.Null);
            Assert.That(_sessionChoice.VisualEquivalentVisible, Is.True);
            Assert.That(_sessionChoice.CaptionVisible, Is.False);
            Assert.That(_sessionChoice.CurrentVisualSprite,
                Is.SameAs(WelcomeContent.LoadApproved().SessionChoiceVisualEquivalent));
        }

        [Test]
        public void InterruptionFreezesCueUntilMappedResumeAndKeepsGeometryStable()
        {
            _presenter.Present(WelcomePresentationState.Welcome, true, true, 30d);
            var band = _welcome.CaptionBandCanvasRect;

            Assert.That(_presenter.Interrupt(), Is.True);
            Assert.That(_welcome.MediaCuePaused, Is.True);
            Assert.That(_presenter.Advance(34d), Is.False);
            Assert.That(_presenter.ResumeAfterMappedUserAction(34d), Is.True);
            Assert.That(_welcome.MediaCuePaused, Is.False);
            Assert.That(_welcome.CaptionBandCanvasRect, Is.EqualTo(band));
        }
    }
}
