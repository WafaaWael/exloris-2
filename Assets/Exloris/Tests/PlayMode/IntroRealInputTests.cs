using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Runtime.Input;
using Exloris.Runtime.Intro;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Exloris.Tests.PlayMode
{
    public sealed class IntroRealInputTests : InputTestFixture
    {
        private ChildInputRouter _router;
        private InputActionAsset _actions;
        private IntroStageView _view;
        private Mouse _mouse;

        public override void Setup()
        {
            base.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
            _actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = new InputActionMap("IntroTest");
            var point = map.AddAction("Point", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            point.AddBinding("<Mouse>/position");
            var press = map.AddAction("Press", InputActionType.Button, expectedControlLayout: "Button");
            press.AddBinding("<Mouse>/leftButton");
            _actions.AddActionMap(map);
            _router = new ChildInputRouter(point, press);
            _router.SetVisibleStateReady(true);
            _router.Enable();

            var owner = new GameObject("IntroRealInputTest");
            _view = owner.AddComponent<IntroStageView>();
            _view.Configure(_router, StartupIntroContent.LoadApproved());
            _view.ShowIntro(
                Guid.Parse("f74866f9-623e-4db2-a0bd-3d7a6e78575f"),
                IntroVariantSelector.Select(1920, 1080, reducedMotion: false, preferLowResolution: false),
                new IntroFirstFramePresentation());
            Canvas.ForceUpdateCanvases();
        }

        public override void TearDown()
        {
            if (_view != null) UnityEngine.Object.DestroyImmediate(_view.gameObject);
            _router?.Dispose();
            if (_actions != null) UnityEngine.Object.DestroyImmediate(_actions);
            base.TearDown();
        }

        [Test]
        public void FirstChildFacingFrameHasOnlySkipAndRealInputInvokesItOnce()
        {
            CollectionAssert.AreEqual(
                new[] { IntroFirstFramePresentation.SkipControlId },
                _view.ChildFacingControlIds);
            Assert.That(_view.VideoLayerPresent, Is.True);
            Assert.That(_view.IbisOverlayPresent, Is.True);
            Assert.That(_view.VisualEquivalentPresent, Is.True);
            Assert.That(_view.CaptionLayerPresent, Is.True);
            Assert.That(_view.SkipTouchAreaIsTransparent, Is.True);
            Assert.That(_view.SkipTargetContainsVisibleArt, Is.True);

            var commands = new List<SemanticInputCommand>();
            _router.SemanticCommandIssued += commands.Add;
            var centre = _view.SkipScreenBounds.center;
            currentTime = 100d;
            Set(_mouse.position, centre);
            Press(_mouse.leftButton);
            currentTime = 100.05d;
            Release(_mouse.leftButton);
            currentTime = 100.10d;
            Press(_mouse.leftButton);
            currentTime = 100.15d;
            Release(_mouse.leftButton);

            Assert.That(commands, Has.Count.EqualTo(1));
            Assert.That(commands[0].ControlId, Is.EqualTo(IntroFirstFramePresentation.SkipControlId));
        }

        [Test]
        public void InterruptionRequiresOneMappedResumeControlAndNeverAutoResumes()
        {
            _view.ShowInterruptionResume();

            CollectionAssert.AreEqual(new[] { IntroStageView.ResumeControlId }, _view.ChildFacingControlIds);
            Assert.That(_view.ResumeVisible, Is.True);

            var commands = new List<SemanticInputCommand>();
            _router.SemanticCommandIssued += commands.Add;
            var centre = _view.ResumeScreenBounds.center;
            currentTime = 200d;
            Set(_mouse.position, centre);
            Press(_mouse.leftButton);
            currentTime = 200.05d;
            Release(_mouse.leftButton);

            Assert.That(commands, Has.Count.EqualTo(1));
            Assert.That(commands[0].ControlId, Is.EqualTo(IntroStageView.ResumeControlId));

            _view.HideInterruptionResume();
            CollectionAssert.AreEqual(
                new[] { IntroFirstFramePresentation.SkipControlId },
                _view.ChildFacingControlIds);
        }

        [Test]
        public void VideoLayerUsesNeutralTintSoDecodedFramesRemainVisible()
        {
            var videoLayer = _view.GetComponentsInChildren<RawImage>(includeInactive: true)
                .Single(image => image.gameObject.name == "AspectSpecificVideo");

            Assert.That(videoLayer.color, Is.EqualTo(Color.white));
        }

        [Test]
        public void InterruptedPlaybackTokenCannotUpdateResumedSynchronizedLayers()
        {
            var oldToken = Guid.Parse("f74866f9-623e-4db2-a0bd-3d7a6e78575f");
            var replacementToken = Guid.Parse("bf824d44-e2ab-4743-a3d3-98f20a68fa90");

            Assert.That(_view.UpdateSynchronizedLayers(oldToken, 2.4d), Is.True);
            Assert.That(_view.ReplacePlaybackToken(oldToken, replacementToken), Is.True);
            var acceptedTime = _view.LastSynchronizedMediaTimeSeconds;

            Assert.That(_view.UpdateSynchronizedLayers(oldToken, 7.2d), Is.False);
            Assert.That(_view.LastSynchronizedMediaTimeSeconds, Is.EqualTo(acceptedTime));
            Assert.That(_view.UpdateSynchronizedLayers(replacementToken, 7.2d), Is.True);
            Assert.That(_view.LastSynchronizedMediaTimeSeconds, Is.EqualTo(7.2d));
        }

        [Test]
        public void SkipUsesFrozenBottomSingleSlotAndMeetsEveryRatioFloor()
        {
            Assert.That(_view.SkipAnchorMin, Is.EqualTo(new Vector2(0.5f, 0f)));
            Assert.That(_view.SkipAnchorMax, Is.EqualTo(new Vector2(0.5f, 0f)));
            Assert.That(_view.SkipCanvasAnchoredPosition, Is.EqualTo(new Vector2(0f, 112f)));
            Assert.That(_view.SkipCanvasSize, Is.EqualTo(new Vector2(224f, 224f)));

            foreach (var ratioFloor in new[] { 97f, 224f, 182f })
            {
                Assert.That(_view.SkipCanvasSize.x, Is.GreaterThanOrEqualTo(ratioFloor));
                Assert.That(_view.SkipCanvasSize.y, Is.GreaterThanOrEqualTo(ratioFloor));
            }
        }

        [Test]
        public void CaptionVisibilityNeverMovesReservedBandOrSkipGeometry()
        {
            _view.SetCaptionsEnabled(true);
            Assert.That(
                _view.UpdateSynchronizedLayers(
                    Guid.Parse("f74866f9-623e-4db2-a0bd-3d7a6e78575f"),
                    0d),
                Is.True);
            Canvas.ForceUpdateCanvases();
            var skipWithCaptions = _view.SkipScreenBounds;
            var bandWithCaptions = _view.CaptionBandScreenBounds;
            var ibis = _view.GetComponentsInChildren<Image>(includeInactive: true)
                .Single(image => image.gameObject.name == "SeparateIbisOverlay");
            var ibisWithCaptions = ToScreenRect(ibis.rectTransform);

            _view.SetCaptionsEnabled(false);
            Assert.That(
                _view.UpdateSynchronizedLayers(
                    Guid.Parse("f74866f9-623e-4db2-a0bd-3d7a6e78575f"),
                    0d),
                Is.True);
            Canvas.ForceUpdateCanvases();

            Assert.That(_view.SkipScreenBounds, Is.EqualTo(skipWithCaptions));
            Assert.That(_view.CaptionBandScreenBounds, Is.EqualTo(bandWithCaptions));
            Assert.That(_view.SkipScreenBounds.Overlaps(_view.CaptionBandScreenBounds), Is.False);
            Assert.That(
                bandWithCaptions.Overlaps(ibisWithCaptions),
                Is.False,
                "Runtime captions must not cover the synchronized ibis subject.");
        }

        [Test]
        public void RealInputAcceptsCentreAndEdgeButRejectsNearMiss()
        {
            var commands = new List<SemanticInputCommand>();
            _router.SemanticCommandIssued += commands.Add;
            var bounds = _view.SkipScreenBounds;

            Click(bounds.center, 300d);
            Click(new Vector2(bounds.xMin + 0.25f, bounds.center.y), 300.3d);
            Click(new Vector2(bounds.xMin - 1f, bounds.center.y), 300.6d);

            Assert.That(commands, Has.Count.EqualTo(2));
            Assert.That(commands, Has.All.Matches<SemanticInputCommand>(command =>
                command.ControlId == IntroFirstFramePresentation.SkipControlId));
        }

        private void Click(Vector2 position, double startedAt)
        {
            currentTime = startedAt;
            Set(_mouse.position, position);
            Press(_mouse.leftButton);
            currentTime = startedAt + 0.05d;
            Release(_mouse.leftButton);
        }

        private static Rect ToScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
